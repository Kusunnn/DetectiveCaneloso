using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Diccionario = Godot.Collections.Dictionary;
using Arreglo = Godot.Collections.Array;

// Escenarios del Capítulo 2 «La Trampa de la Garrapata». Solo el escenario: HUD del caso, pistas al diario,
// salidas a otros escenarios y el guardado repartido por escena (como PartidaEscenas del Caso 1).
// No hay guion: interrogatorios, tablero, preguntas de Tito, olfato y purga son otras tareas y se cuelgan
// de los nodos preparados (PuntoInterrogatorio, PuertaTito_N, Cruce_N…).
public partial class EscenarioCaso2 : Node, IEstadoPartida
{
	[Export] public Jugador Jugador;
	[Export] public CanvasLayer Hud;
	[Export] public VentanaDialogo Dialogo;
	[Export] public MusicaFondo Musica;
	[Export] public WorldEnvironment Entorno;
	// Nombre del lugar para el HUD y la ranura («Comisaría», «Torre Miawrio · Lobby»…)
	[Export] public string Lugar = "";
	// Salidas: cada PuntoInteraccion lleva a la escena y al Marker3D de su destino («res://…tscn|Entrada_X»)
	[Export] public PuntoInteraccion[] Salidas = Array.Empty<PuntoInteraccion>();
	[Export] public string[] Destinos = Array.Empty<string>();
	// Contenedores con una pista adentro: la pista no se ve ni se puede tomar hasta abrirlo
	[Export] public Contenedor[] ContenedoresConPista = Array.Empty<Contenedor>();
	[Export] public ObjetoPista[] PistasDentro = Array.Empty<ObjetoPista>();

	private const uint CapaInteractuable = 1u << 3;

	private readonly HashSet<string> _pistas = new();
	private Diccionario _escenas = new();
	private double _tiempoCaso;
	private bool _restaurando;

	private string Escena => GetTree().CurrentScene?.SceneFilePath ?? GetParent().SceneFilePath;

	public override void _Ready()
	{
		Hud.Set("caso", "02");
		Hud.Set("archivo", "La Trampa de la Garrapata");
		Hud.Set("lugar", Lugar);
		Jugador.EvidenciaRegistrada += id => { _pistas.Add(id); Cambio(); Autoguardar("Pista: " + id); };
		Jugador.ObjetoObtenido += _ => Cambio();
		Jugador.EvidenciaExaminada += _ => Cambio();

		for (int i = 0; i < ContenedoresConPista.Length && i < PistasDentro.Length; i++)
		{
			var pista = PistasDentro[i];
			Esconder(pista, true);
			ContenedoresConPista[i].Abierto += () => Esconder(pista, false);
		}
		for (int i = 0; i < Salidas.Length && i < Destinos.Length; i++)
		{
			string[] partes = Destinos[i].Split('|');
			string escena = partes[0], punto = partes.Length > 1 ? partes[1] : "";
			Salidas[i].Usado += () => IrA(escena, punto);
		}

		AddToGroup("estado_partida");
		foreach (var contenedor in Todos<Contenedor>()) contenedor.Abierto += Cambio;
		foreach (var puerta in Todos<Puerta>()) puerta.Abierta += Cambio;
		CallDeferred(MethodName.Empezar);
	}

	private void Empezar()
	{
		var gestor = GestorPartida.Instancia;
		if (gestor?.EstadoPendiente != null) RestaurarEstado(gestor.EstadoPendiente);
	}

	public override void _Process(double delta)
	{
		if (!_restaurando) _tiempoCaso += delta;
	}

	// Pasa a otro escenario del capítulo llevándose el progreso. Si la escena se abrió sola (acceso de prueba,
	// sin capítulo en curso), cambia directo y el jugador aparece en el punto de llegada.
	private void IrA(string escena, string punto)
	{
		if (!ResourceLoader.Exists(escena)) { Jugador.MostrarAviso("Este lugar todavía no está listo.", 2.5); return; }
		GestorPartida.Instancia?.CambiarEscenaDelCapitulo(escena, punto);
	}

	private static void Esconder(ObjetoPista pista, bool escondida)
	{
		if (pista.Encontrada && pista.SeRecoge) return;
		pista.Visible = !escondida;
		pista.CollisionLayer = escondida ? 0 : CapaInteractuable;
	}

	// ---------- Guardado ----------

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

	public bool PuedeGuardar =>
		!_restaurando && !Dialogo.Abierto && !Jugador.Examinando && !Todos<Contenedor>().Any(c => c.Animando);

	public Diccionario Resumen() => new()
	{
		{ "caso", "Capítulo 2" },
		{ "zona", Lugar },
		{ "pistas", PistasEnDiario() },
		{ "total_pistas", 8 },
		{ "tiempo", _tiempoCaso },
	};

	private int PistasEnDiario()
	{
		int total = 0;
		foreach (Variant pista in Hud.Get("pistas").AsGodotArray())
			if (pista.As<Resource>().Get("origen_id").AsStringName().ToString() == "") total++;
		return total;
	}

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
		escenas[Escena] = new Diccionario { { "contenedores", contenedores }, { "puertas", puertas } };
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
		PartidaEscenas.ColocarEnEntrada(GetParent(), Jugador);
		_escenas = estado.TryGetValue("escenas", out Variant es) ? es.AsGodotDictionary().Duplicate(true) : new Diccionario();
		var propio = _escenas.TryGetValue(Escena, out Variant d0) ? d0.AsGodotDictionary() : new Diccionario();

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
				_pistas.Add(id);
				objeto.MarcarEncontrada();
				Jugador.RestaurarEvidencia(objeto, pista["titulo"].AsString(), pista["descripcion"].AsString(), examinada);
			}
			else
			{
				string foto = $"res://Assets/Pistas/caso2/{id}.png";
				Jugador.RestaurarEvidencia(id, pista["titulo"].AsString(), pista["descripcion"].AsString(),
					ResourceLoader.Exists(foto) ? GD.Load<Texture2D>(foto) : null);
			}
		}
		foreach (Variant nota in estado["notas"].AsGodotArray()) Hud.Call("agregar_nota", nota.AsString());
		Hud.Set("_aviso", 0.0);

		var contenedores = propio.TryGetValue("contenedores", out Variant cs) ? cs.AsGodotDictionary() : new Diccionario();
		foreach (var c in Todos<Contenedor>())
			if (contenedores.TryGetValue(Ruta(c), out Variant d))
				c.RestaurarEstado(d.AsGodotDictionary()["abierto"].AsBool(), d.AsGodotDictionary()["revisado"].AsBool());
		var puertas = propio.TryGetValue("puertas", out Variant ps) ? ps.AsGodotDictionary() : new Diccionario();
		foreach (var p in Todos<Puerta>())
			if (puertas.TryGetValue(Ruta(p), out Variant d))
				p.RestaurarEstado(d.AsGodotDictionary()["bloqueada"].AsBool(), d.AsGodotDictionary()["abierta"].AsBool());
		for (int i = 0; i < ContenedoresConPista.Length && i < PistasDentro.Length; i++)
			if (ContenedoresConPista[i].EstaAbierto) Esconder(PistasDentro[i], false);
		_tiempoCaso = estado.TryGetValue("tiempo", out Variant t) ? t.AsDouble() : 0;

		_restaurando = false;
		GestorPartida.Instancia?.EstadoRestaurado();
	}
}
