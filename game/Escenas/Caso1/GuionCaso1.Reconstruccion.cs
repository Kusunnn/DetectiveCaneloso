using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diccionario = Godot.Collections.Dictionary;

// Reconstrucción de la noche (reemplaza «Armar el caso»): 4 viñetas con huecos que se llenan con fichas juntadas
// durante el caso. Cada viñeta se comprueba sola al llenarla. Con las 4 selladas y Tony sin candados, Final A;
// si a Tony le quedan, Caneloso y Hecdog lo encaran. «Acusar ya» lleva al Final B (contra Tony, Hecdog lo frena).
public partial class GuionCaso1
{
	private const string RutaReconstruccion = "res://Datos/Caso1/caso1_reconstruccion.json";

	public sealed class Frase { public string Texto; public string[] Huecos, Respuestas; }
	public sealed class Vineta { public string Titulo; public List<Frase> Frases = new(); }
	public sealed class Ficha { public string Id, Texto, Tipo; public string[] Origen; }

	public readonly List<Vineta> Vinetas = new();
	public readonly Dictionary<string, Ficha> Fichas = new();
	// Lo que va armando el jugador: viaja en la partida
	public readonly Dictionary<string, string> Huecos = new();   // "v.f.h" → id de ficha
	public readonly HashSet<int> Selladas = new();
	public int IntentosReconstruccion;

	public Func<Task<string>> AbrirReconstruccion; // Lo pone la sala: "salir", "acusar" o "completa"
	public Action CerrarReconstruccion;
	public Action<string> Autoguardado;

	public bool ReconstruccionCompleta => Selladas.Count == Vinetas.Count && Vinetas.Count > 0;

	private void CargarReconstruccion()
	{
		using var archivo = FileAccess.Open(RutaReconstruccion, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (archivo == null || json.Parse(archivo.GetAsText()) != Error.Ok)
		{
			GD.PushError($"GuionCaso1: no se pudo leer {RutaReconstruccion}.");
			return;
		}
		var datos = json.Data.AsGodotDictionary();
		static string[] Lista(Variant v) => v.AsGodotArray().Select(x => x.AsString()).ToArray();
		foreach (Variant v in datos["vinetas"].AsGodotArray())
		{
			var d = v.AsGodotDictionary();
			var vineta = new Vineta { Titulo = d["titulo"].AsString() };
			foreach (Variant f in d["frases"].AsGodotArray())
			{
				var fd = f.AsGodotDictionary();
				vineta.Frases.Add(new Frase { Texto = fd["texto"].AsString(), Huecos = Lista(fd["huecos"]), Respuestas = Lista(fd["respuestas"]) });
			}
			Vinetas.Add(vineta);
		}
		foreach (var (id, valor) in datos["fichas"].AsGodotDictionary())
		{
			var d = valor.AsGodotDictionary();
			Fichas[id.AsString()] = new Ficha { Id = id.AsString(), Texto = d["texto"].AsString(), Tipo = d["tipo"].AsString(), Origen = Lista(d["origen"]) };
		}
	}

	public static string IdHueco(int vineta, int frase, int hueco) => $"{vineta}.{frase}.{hueco}";

	// Las fichas que ya juntó: los implicados siempre; lo demás, cuando tiene la pista o la confesión
	public bool TieneFicha(Ficha f) => f.Origen.Any(o => o == "siempre" || TienePista(o) || Estado.TieneConclusion(o));
	public IEnumerable<Ficha> FichasDe(string tipo) => Fichas.Values.Where(f => f.Tipo == tipo && TieneFicha(f));

	public bool VinetaLlena(int v) => HuecosDe(v).All(h => Huecos.ContainsKey(h.Id));

	public IEnumerable<(string Id, string Correcta)> HuecosDe(int v)
	{
		for (int f = 0; f < Vinetas[v].Frases.Count; f++)
			for (int h = 0; h < Vinetas[v].Frases[f].Huecos.Length; h++)
				yield return (IdHueco(v, f, h), Vinetas[v].Frases[f].Respuestas[h]);
	}

	// Errores de una viñeta llena: 0 la sella. No dice cuáles.
	public int Comprobar(int v)
	{
		int errores = HuecosDe(v).Count(h => !Huecos.TryGetValue(h.Id, out string puesta) || puesta != h.Correcta);
		IntentosReconstruccion++;
		if (errores == 0) Selladas.Add(v);
		Motor.Cambio?.Invoke();
		return errores;
	}

	public string LineaReconstruccion(string id) => Guia.TextoSistema("reconstruccion", id);

	// Desde la sala: true si el caso terminó en uno de los finales
	public async Task<bool> Reconstruir(Func<bool> sigue)
	{
		var sala = _arboles[ArbolSala];
		if (!Estado.NodoCompletado(ArbolSala, "explico_caso"))
		{
			Estado.MarcarNodo(ArbolSala, "explico_caso");
			await Motor.Hablar(sala, "Hecdog", "serio", LineaReconstruccion("explicacion"));
		}
		if (ReconstruccionCompleta) return await FinalDeReconstruccion(sigue);
		EnEspera = true;
		Autoguardado?.Invoke("Reconstrucción");
		string resultado = AbrirReconstruccion != null ? await AbrirReconstruccion() : "salir";
		EnEspera = false;
		CerrarReconstruccion?.Invoke();
		Motor.Cambio?.Invoke();
		if (!sigue()) return false;
		if (resultado == "acusar") return await AcusarYa(sigue);
		if (resultado == "completa") return await FinalDeReconstruccion(sigue);
		return false;
	}

	private async Task<bool> FinalDeReconstruccion(Func<bool> sigue)
	{
		if (!TonySinCandados) await Motor.Hablar(_arboles[ArbolSala], "Hecdog", "serio", LineaReconstruccion("tony_falta"));
		return await AcusarATony(sigue);
	}

	// «Acusar ya»: a quien sea, con confirmación. A Tony solo con la noche completa.
	public async Task<bool> AcusarYa(Func<bool> sigue)
	{
		var sala = _arboles[ArbolSala];
		var opciones = Sospechosos.Select(s => new OpcionVista("Acusar a " + NombreDe(s))).ToList();
		opciones.Add(new OpcionVista("Mejor no"));
		string pregunta = _arboles[ArbolAcusacion].Nodo("pregunta").Lineas[0].Texto;
		int elegida = await Motor.Ventana.Elegir(Motor.HablanteDe(sala, "Hecdog"), pregunta, opciones, opciones.Count - 1);
		if (elegida >= Sospechosos.Length || !sigue()) return false;
		string quien = Sospechosos[elegida];
		int seguro = await Motor.Ventana.Elegir(Motor.HablanteDe(sala, "Caneloso"), LineasSistema.Confirmar, new List<OpcionVista> { new("Mejor no"), new($"Acusar a {NombreDe(quien)}") }, 0);
		if (seguro == 0 || !sigue()) return false;
		if (quien == Culpable)
		{
			if (ReconstruccionCompleta) return await AcusarATony(sigue);
			await Motor.Hablar(sala, "Hecdog", "serio", LineaReconstruccion("rechazo_tony"));
			return false;
		}
		await TerminarAcusacion("a_" + quien, sigue);
		return true;
	}

	private Diccionario CapturarReconstruccion()
	{
		var huecos = new Diccionario();
		foreach (var (id, ficha) in Huecos) huecos[id] = ficha;
		return new Diccionario
		{
			{ "huecos", huecos },
			{ "selladas", new Godot.Collections.Array(Selladas.OrderBy(x => x).Select(x => Variant.From(x))) },
			{ "intentos", IntentosReconstruccion },
		};
	}

	private void RestaurarReconstruccion(Diccionario datos)
	{
		Huecos.Clear();
		Selladas.Clear();
		IntentosReconstruccion = 0;
		if (datos == null || !datos.TryGetValue("reconstruccion", out Variant r)) return;
		var d = r.AsGodotDictionary();
		foreach (var (id, ficha) in d["huecos"].AsGodotDictionary()) Huecos[id.AsString()] = ficha.AsString();
		foreach (Variant v in d["selladas"].AsGodotArray()) Selladas.Add(v.AsInt32());
		IntentosReconstruccion = d["intentos"].AsInt32();
	}
}
