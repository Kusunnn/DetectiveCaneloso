using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// El «¿qué sigue?» del Caso 1: una sola respuesta para el objetivo del HUD, el diario y (más adelante) las ayudas.
// No sabe de escenas: solo pregunta qué pistas, confesiones y llaves hay. Los candados se calculan de las conclusiones
// que ya guarda la partida, así que no agregan nada al guardado. Datos: Datos/Caso1/caso1_candados.json.
public sealed class GuiaCaso1
{
	public enum Escenario { Campamento, Lago, Sala }
	public enum Clase { Listo, FaltaPista, FaltaLlavero, TodoRoto }

	public sealed class Candado
	{
		public string Id, Quien, Previo, Nodo;
		public int Frase;
		public bool Oculta;
		public string[] Da = Array.Empty<string>(), Pistas = Array.Empty<string>(), Conclusiones = Array.Empty<string>();
		public bool EsDeTony => Quien == GuionCaso1.Culpable;
	}

	public sealed class Paso
	{
		public Clase Clase;
		public Candado Candado;   // Con quién hablar o para quién hace falta
		public string Pista;      // La pista que falta
		public string Zona;
		public string EscenarioPista;
	}

	private const string RutaCandados = "res://Datos/Caso1/caso1_candados.json";
	private const string RutaSistema = "res://Datos/Dialogos/caso1_sistema.json";

	public readonly List<Candado> Candados = new();
	private readonly Dictionary<string, (string Escenario, string Zona, bool Llavero, string Tipo)> _pistas = new();
	private readonly Dictionary<string, List<string>> _frases = new();
	private readonly Godot.Collections.Dictionary _sistema;
	private readonly Func<string, bool> _tienePista, _tieneConclusion;
	private readonly Func<bool> _tieneLlavero;

	public GuiaCaso1(Func<string, bool> tienePista, Func<string, bool> tieneConclusion, Func<bool> tieneLlavero)
	{
		_tienePista = tienePista;
		_tieneConclusion = tieneConclusion;
		_tieneLlavero = tieneLlavero;
		var datos = Leer(RutaCandados);
		foreach (var (id, valor) in datos["pistas"].AsGodotDictionary())
		{
			var p = valor.AsGodotDictionary();
			_pistas[id.AsString()] = (p["escenario"].AsString(), p["zona"].AsString(), p.TryGetValue("llavero", out var ll) && ll.AsBool(), p.TryGetValue("tipo", out var tipo) ? tipo.AsString() : "");
		}
		foreach (Variant v in datos["candados"].AsGodotArray())
		{
			var c = v.AsGodotDictionary();
			Candados.Add(new Candado
			{
				Id = c["id"].AsString(),
				Quien = c["quien"].AsString(),
				Previo = c.TryGetValue("previo", out var previo) ? previo.AsString() : null,
				Nodo = c["nodo"].AsString(),
				Frase = c["frase"].AsInt32(),
				Oculta = c.TryGetValue("oculta", out var oculta) && oculta.AsBool(),
				Da = Lista(c, "da"),
				Pistas = Lista(c, "pistas"),
				Conclusiones = Lista(c, "conclusiones"),
			});
		}
		foreach (var (quien, frases) in datos["declaraciones"].AsGodotDictionary())
			_frases[quien.AsString()] = frases.AsGodotArray().Select(f => f.AsString()).ToList();
		_sistema = Leer(RutaSistema);
	}

	private static string[] Lista(Godot.Collections.Dictionary d, string clave) =>
		d.TryGetValue(clave, out var v) ? v.AsGodotArray().Select(x => x.AsString()).ToArray() : Array.Empty<string>();

	private static Godot.Collections.Dictionary Leer(string ruta)
	{
		using var archivo = FileAccess.Open(ruta, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (archivo == null || json.Parse(archivo.GetAsText()) != Error.Ok)
		{
			GD.PushError($"GuiaCaso1: no se pudo leer {ruta}.");
			return new Godot.Collections.Dictionary();
		}
		return json.Data.AsGodotDictionary();
	}

	// ---------- Candados ----------

	public Candado Buscar(string id) => Candados.FirstOrDefault(c => c.Id == id);
	public bool Roto(Candado c) => c.Da.Length > 0 && _tieneConclusion(c.Da[0]);
	private bool Abierto(Candado c) => c.Previo == null || Roto(Buscar(c.Previo));
	private bool TieneTodo(Candado c) => c.Pistas.All(_tienePista) && c.Conclusiones.All(_tieneConclusion);

	// Ya se tiene con qué romperlo: su retrato brilla
	public bool Listo(Candado c) => !Roto(c) && Abierto(c) && TieneTodo(c);

	public int CandadosRotos => Candados.Count(Roto);
	public int CandadosTotales => Candados.Count;
	public bool Brilla(string quien) => Candados.Any(c => c.Quien == quien && Listo(c));

	// Lo primero que hay que conseguir para llegar a este candado
	private Paso QueFalta(Candado c, HashSet<string> vistos)
	{
		if (!vistos.Add(c.Id)) return null;
		if (c.Previo != null && !Roto(Buscar(c.Previo))) return QueFalta(Buscar(c.Previo), vistos);
		foreach (string pista in c.Pistas)
		{
			if (_tienePista(pista)) continue;
			var (escenario, zona, llavero, _) = _pistas[pista];
			bool cerrado = llavero && !_tieneLlavero();
			return new Paso { Clase = cerrado ? Clase.FaltaLlavero : Clase.FaltaPista, Candado = c, Pista = pista, Zona = zona, EscenarioPista = escenario };
		}
		foreach (string conclusion in c.Conclusiones)
		{
			if (_tieneConclusion(conclusion)) continue;
			var origen = Candados.FirstOrDefault(o => o.Da.Contains(conclusion));
			if (origen != null && QueFalta(origen, vistos) is { } paso) return paso;
		}
		return null;
	}

	// Lo que sigue. Nunca hay callejón sin salida: si no hay nada que romper, siempre hay una pista que buscar.
	public Paso Siguiente()
	{
		if (Candados.FirstOrDefault(Listo) is { } listo) return new Paso { Clase = Clase.Listo, Candado = listo };
		foreach (var candado in Candados)
		{
			if (Roto(candado)) continue;
			if (QueFalta(candado, new HashSet<string>()) is { } paso) return paso;
		}
		return new Paso { Clase = Clase.TodoRoto };
	}

	// ---------- Declaraciones ----------

	public IReadOnlyList<string> FrasesDe(string quien) => _frases[quien];
	public string Frase(Candado c) => _frases[c.Quien][c.Frase];
	public int IndiceFrase(Candado c) => c.Frase;
	public string NodoDe(Candado c) => c.Nodo;
	public bool FraseOculta(Candado c) => c.Oculta;
	public string Interrogatorio(string id) => Texto("interrogatorio", id);
	public string FaltaParaTony(Candado c) => Texto("candados_tony", c.Id);

	// ---------- Ayudas ----------

	// Antes de la sala, lo que falta de este escenario en el orden del recorrido; después, lo mismo que el objetivo
	public Paso SiguienteExploracion(Escenario donde)
	{
		string nombre = donde == Escenario.Lago ? "lago" : "campamento";
		foreach (Variant v in _sistema["exploracion"].AsGodotDictionary()[nombre].AsGodotDictionary()["pasos"].AsGodotArray())
			foreach (string pista in Lista(v.AsGodotDictionary(), "pistas"))
			{
				if (_tienePista(pista)) continue;
				var (escenario, zona, llavero, _) = _pistas[pista];
				return new Paso { Clase = llavero && !_tieneLlavero() ? Clase.FaltaLlavero : Clase.FaltaPista, Pista = pista, Zona = zona, EscenarioPista = escenario };
			}
		return Siguiente();
	}

	public static string ClaveAyuda(Paso p) => p.Clase switch
	{
		Clase.FaltaPista => "pista:" + p.Pista,
		Clase.FaltaLlavero => "llavero",
		Clase.Listo => "candado:" + p.Candado.Id,
		_ => "armar",
	};

	// Nivel 1: insinuación; 2: dirección; 3: respuesta. Los dos primeros salen de plantillas.
	public string Ayuda(Paso p, int nivel)
	{
		var ayudas = _sistema["ayudas"].AsGodotDictionary();
		string De(string grupo, string id) => ayudas[grupo].AsGodotDictionary()[id].AsGodotDictionary()["texto"].AsString();
		string Fija(string id) => ayudas["fijas"].AsGodotDictionary()[id].AsGodotArray()[nivel - 1].AsGodotDictionary()["texto"].AsString();
		switch (p.Clase)
		{
			case Clase.FaltaPista:
				if (nivel == 3) return De("nivel3", p.Pista);
				if (nivel == 1) return De("plantillas", "pista_1").Replace("{zona}", p.Zona);
				var (_, _, llavero, tipo) = _pistas[p.Pista];
				string plantilla = llavero && !_tieneLlavero() ? "pista_2_cerrado" : tipo != "" ? "pista_2_" + tipo : "pista_2";
				return De("plantillas", plantilla).Replace("{zona}", p.Zona);
			case Clase.FaltaLlavero:
				return Fija("llavero");
			case Clase.Listo:
				if (nivel == 3) return De("nivel3", p.Candado.Id);
				string quien = GuionCaso1.NombreDe(p.Candado.Quien);
				return nivel == 1 ? De("plantillas", "candado_1").Replace("{quien}", quien)
					: De("plantillas", "candado_2").Replace("{quien}", quien).Replace("{frase}", Frase(p.Candado));
			default:
				return Fija("armar");
		}
	}

	// Da el siguiente nivel que falte de este paso. false si ya se dieron todos los permitidos (texto: el último).
	public (bool Nueva, string Texto) PedirAyuda(Paso paso, SistemaAyudas cuaderno, int niveles)
	{
		string clave = ClaveAyuda(paso), ultimo = Ayuda(paso, 1);
		for (int nivel = 1; nivel <= niveles; nivel++)
		{
			ultimo = Ayuda(paso, nivel);
			if (cuaderno.Agregar($"caso1:{clave}:{nivel}", ultimo)) return (true, ultimo);
		}
		return (false, ultimo);
	}

	public string Aviso(string id) => _sistema["ayudas"].AsGodotDictionary()["avisos"].AsGodotDictionary()[id].AsGodotDictionary()["texto"].AsString();
	public string TextoDificultad(string id) => Texto("dificultad", id);

	// ---------- Textos ----------

	private string Texto(string grupo, string id) => _sistema[grupo].AsGodotDictionary()[id].AsGodotDictionary()["texto"].AsString();
	public bool TieneNivel3(string id) => _sistema["ayudas"].AsGodotDictionary()["nivel3"].AsGodotDictionary().ContainsKey(id);

	public string Objetivo(string id) => Texto("objetivos", id);

	public string ObjetivoInterrogatorios(Escenario donde)
	{
		var paso = Siguiente();
		switch (paso.Clase)
		{
			case Clase.Listo:
				return Objetivo(donde == Escenario.Sala ? "tengo_algo" : "tengo_algo_fuera").Replace("{quien}", GuionCaso1.NombreDe(paso.Candado.Quien));
			case Clase.FaltaLlavero:
				return Objetivo("falta_llavero").Replace("{zona}", paso.Zona);
			case Clase.FaltaPista:
				return Objetivo(paso.EscenarioPista == "lago" ? "falta_lago" : "falta_campamento").Replace("{zona}", paso.Zona);
			default:
				return Objetivo("armar_caso");
		}
	}

	private IEnumerable<string> PistasDe(string escenario) => _pistas.Where(p => p.Value.Escenario == escenario).Select(p => p.Key);
	public int PistasTotales(string escenario) => PistasDe(escenario).Count();
	public int PistasEncontradas(string escenario) => PistasDe(escenario).Count(_tienePista);

	// Antes de la sala: el mismo recorrido por zonas de siempre, en el orden en que se entiende la historia
	public string ObjetivoExploracion(Escenario donde)
	{
		string nombre = donde == Escenario.Lago ? "lago" : "campamento";
		var datos = _sistema["exploracion"].AsGodotDictionary()[nombre].AsGodotDictionary();
		string Dato(string clave) => datos[clave].AsString();
		int total = PistasTotales(nombre), encontradas = PistasEncontradas(nombre);
		string cuenta = donde == Escenario.Lago ? $" ({encontradas}/{total} pistas del lago)" : $" ({encontradas}/{total} pistas)";
		bool llavero = _tieneLlavero();

		if (donde == Escenario.Lago)
		{
			if (encontradas >= total) return Dato("completo");
			if (!llavero && !_tienePista("hueco_remo") && encontradas >= 5) return Dato("sin_llavero") + cuenta;
		}
		else if (encontradas >= total && llavero) return Dato("completo");

		foreach (Variant v in datos["pasos"].AsGodotArray())
		{
			var paso = v.AsGodotDictionary();
			string[] ids = Lista(paso, "pistas");
			if (donde != Escenario.Lago)
			{
				if (ids.Contains("taza_delantal") && _tienePista("taza_delantal") && !llavero) return Dato("delantal") + cuenta;
				if (ids.Contains("nota_porky") && llavero && !_tienePista("nota_porky")) return Dato("abrir") + cuenta;
			}
			if (ids.All(_tienePista)) continue;
			if (donde == Escenario.Lago && ids[0] == "hueco_remo" && llavero) return Dato("abrir") + cuenta;
			return paso["texto"].AsString() + cuenta;
		}
		return Dato("general") + cuenta;
	}

	// Los objetivos secundarios del diario
	public string TituloSecundarios => Texto("secundarios", "titulo");

	public string[] Secundarios() => new[]
	{
		Cuenta("pistas_campamento", PistasEncontradas("campamento"), PistasTotales("campamento")),
		Cuenta("pistas_lago", PistasEncontradas("lago"), PistasTotales("lago")),
		Cuenta("candados", CandadosRotos, CandadosTotales),
	};

	private string Cuenta(string id, int n, int total) => Texto("secundarios", id).Replace("{n}", n.ToString()).Replace("{total}", total.ToString());
}
