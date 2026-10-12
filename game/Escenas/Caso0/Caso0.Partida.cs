using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Diccionario = Godot.Collections.Dictionary;
using Arreglo = Godot.Collections.Array;

// Guardado del Caso 0: qué se guarda de la investigación y cómo se restaura al cargar.
// GestorPartida no conoce el caso: lo encuentra por el grupo "estado_partida".
public partial class Caso0 : IEstadoPartida
{
	private bool _restaurando;

	private void PrepararPartida()
	{
		AddToGroup("estado_partida");
		// Abrir un contenedor o una puerta también es progreso
		foreach (var contenedor in Todos<Contenedor>()) contenedor.Abierto += Cambio;
		foreach (var puerta in Todos<Puerta>()) puerta.Abierta += Cambio;
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
		var escena = GetParent();
		var pendientes = new Stack<Node>();
		pendientes.Push(escena);
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
		_cerrandoCaso || (!_restaurando && !_enDialogo && !Jugador.Examinando && !Tablero.Abierto && !Dialogo.Abierto && !Todos<Contenedor>().Any(c => c.Animando));

	private string ZonaActual()
	{
		if (ZonaVestuario.OverlapsBody(Jugador)) return "Vestidores";
		if (ZonaRecepcion.OverlapsBody(Jugador)) return "Recepción";
		if (ZonaPasillo.OverlapsBody(Jugador)) return "Pasillo";
		return "Sala 1";
	}

	public Diccionario Resumen() => new()
	{
		{ "caso", "00" },
		{ "capitulo", IdCapitulo },
		{ "fase", Capitulos.Fase(FaseActual())?.Nombre ?? "" },
		{ "zona", ZonaActual() },
		{ "pistas", _pistas.Count },
		{ "total_pistas", PistasTotales },
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
				// HU-07: estado del examen y, en los detalles descubiertos, la evidencia de la que salieron
				{ "examinada", recurso.Get("examinada").AsBool() },
				{ "origen_id", recurso.Get("origen_id").AsStringName().ToString() },
			});
		}
		var personas = new Arreglo();
		foreach (Variant persona in Hud.Get("personas").AsGodotArray())
		{
			var datos = persona.AsGodotDictionary();
			personas.Add(new Diccionario { { "nombre", datos["nombre"] }, { "resumen", datos["resumen"] } });
		}
		var contenedores = new Diccionario();
		foreach (var c in Todos<Contenedor>())
			contenedores[Ruta(c)] = new Diccionario { { "abierto", c.EstaAbierto }, { "revisado", c.Revisado } };
		var puertas = new Diccionario();
		foreach (var p in Todos<Puerta>())
			puertas[Ruta(p)] = new Diccionario { { "bloqueada", p.Bloqueada }, { "abierta", p.EstaAbierta } };
		var testimonios = new Diccionario();
		foreach (var (id, carta) in _testimonios) testimonios[id] = carta.Titulo;

		return new Diccionario
		{
			{ "jugador", Jugador.CapturarEstado() },
			{ "pistas", pistas },
			{ "notas", Hud.Get("notas").AsGodotArray().Duplicate() },
			{ "personas", personas },
			{ "deducciones", Lista(_deducciones) },
			{ "conclusiones", Lista(_conclusiones) },
			{ "testimonios", testimonios },
			{ "confrontaciones", Lista(_confrontaciones) },
			{ "personajes", new Diccionario { { "Eriz", Capturar(_eriz) }, { "Porky", Capturar(_porky) }, { "Comisario", Capturar(_comisario) } } },
			{ "contenedores", contenedores },
			{ "puertas", puertas },
			{ "fase", new Diccionario
				{
					{ "autorizado", _autorizado }, { "resuelto", _resuelto }, { "vio_candado", _vioCandado },
					{ "intentos_acusacion", _intentosAcusacion }, { "tiempo", _tiempoCaso },
				} },
			{ "tutorial", new Diccionario
				{
					{ "indicaciones", Lista(_indicacionesDadas) }, { "hablados", Lista(_hablados) },
					{ "vio_expediente", _vioExpediente }, { "vio_libreta", _vioLibreta },
					{ "entro_pasillo", _entroPasillo }, { "entro_vestidores", _entroVestidores },
					{ "aviso_ayuda", _avisoAyudaDado }, { "ultima_indicacion", _ultimaIndicacion ?? "" },
				} },
		};
	}

	private static Arreglo Lista(IEnumerable<string> valores) => new Arreglo(valores.Select(v => Variant.From(v)));

	private static IEnumerable<string> Textos(Variant arreglo) => arreglo.AsGodotArray().Select(v => v.AsString());

	private static Diccionario Capturar(Personaje p) => new()
	{
		{ "animo", p.Animo }, { "confeso", p.Confeso }, { "leidas", Lista(p.Leidas) },
	};

	private void Restaurar(Personaje p, Diccionario d)
	{
		CambiarAnimo(p, d["animo"].AsInt32() - p.Animo);
		p.Confeso = d["confeso"].AsBool();
		foreach (string leida in Textos(d["leidas"])) p.Leidas.Add(leida);
	}

	private void RestaurarPendiente() => RestaurarEstado(GestorPartida.Instancia.EstadoPendiente);

	public void RestaurarEstado(Diccionario estado)
	{
		_restaurando = true;
		Jugador.RestaurarEstado(estado["jugador"].AsGodotDictionary());

		// Pistas: vuelven al expediente en el mismo orden y su objeto queda examinado (o recogido)
		var objetos = Todos<ObjetoPista>().GroupBy(o => o.IdPista).ToDictionary(g => g.Key, g => g.First());
		foreach (Variant v in estado["pistas"].AsGodotArray())
		{
			var pista = v.AsGodotDictionary();
			string id = pista["id"].AsString();
			string origen = pista.TryGetValue("origen_id", out Variant o) ? o.AsString() : "";
			bool examinada = pista.TryGetValue("examinada", out Variant e) && e.AsBool();
			if (!string.IsNullOrEmpty(origen))
			{
				Jugador.RestaurarDetalle(id, pista["titulo"].AsString(), pista["descripcion"].AsString(), origen);
				continue;
			}
			_pistas.Add(id);
			objetos.TryGetValue(id, out var objeto);
			objeto?.MarcarEncontrada();
			if (objeto != null) Jugador.RestaurarEvidencia(objeto, pista["titulo"].AsString(), pista["descripcion"].AsString(), examinada);
			else Jugador.RestaurarEvidencia(id, pista["titulo"].AsString(), pista["descripcion"].AsString());
		}
		foreach (string nota in Textos(estado["notas"])) Hud.Call("agregar_nota", nota);
		foreach (Variant v in estado["personas"].AsGodotArray())
		{
			var persona = v.AsGodotDictionary();
			string nombre = persona["nombre"].AsString();
			Hud.Call("registrar_persona", nombre, persona["resumen"], Dialogo.Retrato(nombre));
		}
		Hud.Set("_aviso", 0.0);

		// Tablero: conclusiones y testimonios
		foreach (string d in Textos(estado["deducciones"])) _deducciones.Add(d);
		_conclusiones.AddRange(Textos(estado["conclusiones"]));
		foreach (var (id, titulo) in estado["testimonios"].AsGodotDictionary())
		{
			string texto = titulo.AsString();
			_testimonios[id.AsString()] = new Carta(id.AsString(), texto, "TESTIMONIO", Dialogo.Retrato(texto.Split(' ')[0]));
		}
		foreach (string c in Textos(estado["confrontaciones"])) _confrontaciones.Add(c);

		// Personajes: ánimo, preguntas hechas y si ya confesaron
		var personajes = estado["personajes"].AsGodotDictionary();
		Restaurar(_eriz, personajes["Eriz"].AsGodotDictionary());
		Restaurar(_porky, personajes["Porky"].AsGodotDictionary());
		Restaurar(_comisario, personajes["Comisario"].AsGodotDictionary());

		// Escenario: contenedores y puertas
		var contenedores = estado["contenedores"].AsGodotDictionary();
		foreach (var c in Todos<Contenedor>())
			if (contenedores.TryGetValue(Ruta(c), out Variant d))
				c.RestaurarEstado(d.AsGodotDictionary()["abierto"].AsBool(), d.AsGodotDictionary()["revisado"].AsBool());
		var puertas = estado["puertas"].AsGodotDictionary();
		foreach (var p in Todos<Puerta>())
			if (puertas.TryGetValue(Ruta(p), out Variant d))
				p.RestaurarEstado(d.AsGodotDictionary()["bloqueada"].AsBool(), d.AsGodotDictionary()["abierta"].AsBool());

		// Fase de la investigación
		var fase = estado["fase"].AsGodotDictionary();
		_autorizado = fase["autorizado"].AsBool();
		_resuelto = fase["resuelto"].AsBool();
		_vioCandado = fase["vio_candado"].AsBool();
		_intentosAcusacion = fase["intentos_acusacion"].AsInt32();
		_tiempoCaso = fase["tiempo"].AsDouble();
		if (_autorizado) PuertaVestuario.Desbloquear();

		// Tutorial
		var tutorial = estado["tutorial"].AsGodotDictionary();
		foreach (string i in Textos(tutorial["indicaciones"])) _indicacionesDadas.Add(i);
		foreach (string h in Textos(tutorial["hablados"])) _hablados.Add(h);
		_vioExpediente = tutorial["vio_expediente"].AsBool();
		_vioLibreta = tutorial["vio_libreta"].AsBool();
		_entroPasillo = tutorial["entro_pasillo"].AsBool();
		_entroVestidores = tutorial["entro_vestidores"].AsBool();
		_avisoAyudaDado = tutorial["aviso_ayuda"].AsBool();
		_ultimaIndicacion = tutorial["ultima_indicacion"].AsString();

		// Lo que depende de la fase: música, sospechosos en la Sala 1 y objetivo
		if (Musica != null) Musica.Intensidad = _deducciones.Count / 4f;
		if (_pistas.Contains(Cinta))
		{
			Eriz.GlobalTransform = PuntoErizFinal.GlobalTransform;
			Porky.GlobalTransform = PuntoPorkyFinal.GlobalTransform;
			SillaAcusacion.Habilitar(!_resuelto, CapaInteractuable);
			if (Musica != null) Musica.Intensidad = 1f;
		}
		if (_resuelto) Objetivo("Caso cerrado... por ahora. ¿Quién es «La Garra»?");
		else ActualizarObjetivo();
		Progreso();

		_restaurando = false;
		GestorPartida.Instancia.HayCambiosSinGuardar = false;
		GestorPartida.Instancia.EstadoRestaurado();
	}
}
