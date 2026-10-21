using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Diccionario = Godot.Collections.Dictionary;
using Arreglo = Godot.Collections.Array;

// Guardado del Caso 1 (HU-07): qué se guarda del campamento y cómo se restaura al cargar.
// Igual que el Caso 0, GestorPartida lo encuentra por el grupo "estado_partida".
public partial class Caso1 : IEstadoPartida
{
	public const string IdCapitulo = "capitulo1";

	private bool _restaurando;
	private double _tiempoCaso;
	// Lo guardado de los otros escenarios del capítulo (Lago, sala): se conserva tal cual al guardar aquí
	private Diccionario _escenas = new();

	private void PrepararPartida()
	{
		AddToGroup("estado_partida");
		// Abrir un contenedor o una puerta también es progreso
		foreach (var contenedor in Todos<Contenedor>()) contenedor.Abierto += Cambio;
		foreach (var puerta in Todos<Puerta>()) puerta.Abierta += Cambio;
	}

	public override void _Process(double delta)
	{
		if (!_restaurando) _tiempoCaso += delta;
	}

	// Hay algo que se perdería al salir sin guardar
	private void Cambio()
	{
		if (!_restaurando && GestorPartida.Instancia != null) GestorPartida.Instancia.HayCambiosSinGuardar = true;
	}

	private void Autoguardar(string motivo)
	{
		if (!_restaurando) GestorPartida.Instancia?.Autoguardar(motivo);
	}

	private IEnumerable<T> Todos<T>() where T : Node
	{
		var pendientes = new Stack<Node>();
		pendientes.Push(GetParent());
		while (pendientes.Count > 0)
		{
			var nodo = pendientes.Pop();
			if (nodo is T encontrado) yield return encontrado;
			foreach (Node hijo in nodo.GetChildren()) pendientes.Push(hijo);
		}
	}

	private string Ruta(Node nodo) => GetParent().GetPathTo(nodo).ToString();

	// ---------- IEstadoPartida ----------

	public bool PuedeGuardar =>
		!_restaurando && !_enDialogo && !Tablero.Abierto && !Dialogo.Abierto && !Jugador.Examinando && !Todos<Contenedor>().Any(c => c.Animando);

	private string ZonaActual()
	{
		foreach (var zona in Zonas)
			if (zona.OverlapsBody(Jugador)) return NombreZona(zona.Name);
		return "Campamento";
	}

	private static string NombreZona(string zona) => zona switch
	{
		"Fogata" => "Fogata",
		"Almacen" => "Almacén",
		"Cocina" => "Cocina",
		"CabanaPorky" => "Cabaña de Porky",
		"Director" => "Cabaña del director",
		"Enfermeria" => "Enfermería",
		"Caseta" => "Caseta del generador",
		"Estacionamiento" => "Estacionamiento",
		"Sendero" => "Sendero del lago",
		_ => zona,
	};

	public Diccionario Resumen() => new()
	{
		{ "caso", "01" },
		{ "capitulo", IdCapitulo },
		{ "fase", Capitulos.Fase(_pistas.Count == 0 ? "introduccion" : "investigacion")?.Nombre ?? "" },
		{ "zona", ZonaActual() },
		{ "pistas", PartidaEscenas.PistasEnDiario(Hud) },
		{ "total_pistas", PartidaEscenas.TotalPistas },
		{ "tiempo", _tiempoCaso },
	};

	public Diccionario CapturarEstado()
	{
		var pistas = new Arreglo();
		foreach (Variant pista in Hud.Get("pistas").AsGodotArray())
		{
			var recurso = pista.As<Resource>();
			pistas.Add(new Diccionario
			{
				{ "id", recurso.Get("id").AsStringName().ToString() },
				{ "titulo", recurso.Get("titulo").AsString() },
				{ "descripcion", recurso.Get("descripcion").AsString() },
				{ "examinada", recurso.Get("examinada").AsBool() },
				{ "origen_id", recurso.Get("origen_id").AsStringName().ToString() },
			});
		}
		var contenedores = new Diccionario();
		foreach (var c in Todos<Contenedor>())
			contenedores[Ruta(c)] = new Diccionario { { "abierto", c.EstaAbierto }, { "revisado", c.Revisado } };
		var puertas = new Diccionario();
		foreach (var p in Todos<Puerta>())
			puertas[Ruta(p)] = new Diccionario { { "bloqueada", p.Bloqueada }, { "abierta", p.EstaAbierta } };

		var escenas = _escenas.Duplicate(true);
		escenas[PartidaEscenas.Campamento] = new Diccionario
		{
			{ "contenedores", contenedores },
			{ "puertas", puertas },
			{ "zonas_vistas", new Arreglo(_zonasVistas.Select(z => Variant.From(z))) },
		};
		return new Diccionario
		{
			{ "jugador", Jugador.CapturarEstado() },
			{ "pistas", pistas },
			{ "notas", Hud.Get("notas").AsGodotArray().Duplicate() },
			{ "tiempo", _tiempoCaso },
			{ "escenas", escenas },
		};
	}

	public void RestaurarEstado(Diccionario estado)
	{
		_restaurando = true;
		Jugador.RestaurarEstado(estado["jugador"].AsGodotDictionary());
		// Llegando desde el Lago o la sala: aparece en la entrada del sendero, no donde estaba allá
		PartidaEscenas.ColocarEnEntrada(GetParent(), Jugador);
		_escenas = PartidaEscenas.Escenas(estado);
		var propio = PartidaEscenas.DeEscena(estado, PartidaEscenas.Campamento) ?? new Diccionario();

		// Pistas: vuelven al expediente en el mismo orden y su objeto queda examinado (o recogido)
		var objetos = Todos<ObjetoPista>().GroupBy(o => o.IdPista).ToDictionary(g => g.Key, g => g.First());
		foreach (Variant v in estado["pistas"].AsGodotArray())
		{
			var pista = v.AsGodotDictionary();
			string id = pista["id"].AsString();
			string origen = pista.TryGetValue("origen_id", out Variant o) ? o.AsString() : "";
			if (!string.IsNullOrEmpty(origen))
			{
				Jugador.RestaurarDetalle(id, pista["titulo"].AsString(), pista["descripcion"].AsString(), origen);
				continue;
			}
			bool examinada = pista.TryGetValue("examinada", out Variant e) && e.AsBool();
			if (objetos.TryGetValue(id, out var objeto))
			{
				// Solo cuentan para el Campamento sus propias pistas; las del Lago van al diario igual
				_pistas.Add(id);
				objeto.MarcarEncontrada();
				Jugador.RestaurarEvidencia(objeto, pista["titulo"].AsString(), pista["descripcion"].AsString(), examinada);
			}
			else PartidaEscenas.RestaurarPistaDeOtroEscenario(Jugador, Hud, id, pista["titulo"].AsString(), pista["descripcion"].AsString());
		}
		foreach (Variant nota in estado["notas"].AsGodotArray()) Hud.Call("agregar_nota", nota.AsString());
		Hud.Set("_aviso", 0.0);

		// Escenario: contenedores y puertas (la cabaña de Porky queda abierta si ya se tenía el llavero)
		var contenedores = propio.TryGetValue("contenedores", out Variant cs) ? cs.AsGodotDictionary() : new Diccionario();
		foreach (var c in Todos<Contenedor>())
			if (contenedores.TryGetValue(Ruta(c), out Variant d))
				c.RestaurarEstado(d.AsGodotDictionary()["abierto"].AsBool(), d.AsGodotDictionary()["revisado"].AsBool());
		var puertas = propio.TryGetValue("puertas", out Variant ps) ? ps.AsGodotDictionary() : new Diccionario();
		foreach (var p in Todos<Puerta>())
			if (puertas.TryGetValue(Ruta(p), out Variant d))
				p.RestaurarEstado(d.AsGodotDictionary()["bloqueada"].AsBool(), d.AsGodotDictionary()["abierta"].AsBool());
		// Una pista dentro de un contenedor ya abierto se puede tomar (si no se tomó ya)
		for (int i = 0; i < ContenedoresConPista.Length && i < PistasDentro.Length; i++)
			if (ContenedoresConPista[i].EstaAbierto && !PistasDentro[i].Encontrada)
				PistasDentro[i].CollisionLayer = CapaInteractuable;

		if (propio.TryGetValue("zonas_vistas", out Variant zs))
			foreach (Variant zona in zs.AsGodotArray()) _zonasVistas.Add(zona.AsString());
		_tiempoCaso = estado["tiempo"].AsDouble();
		ActualizarObjetivo();

		_restaurando = false;
		GestorPartida.Instancia.EstadoRestaurado();
	}
}
