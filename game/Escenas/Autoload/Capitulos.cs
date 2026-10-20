using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Un capítulo (caso) tal como está en Datos/capitulos.json
public class InfoCapitulo
{
	public string Id = "";
	public int Numero;
	public string Titulo = "";
	public string Ilustracion = "";
	public string Escena = "";               // vacía = capítulo en preparación
	public int PistasTotales, DialogosClave, Secretos;
	public List<string> Requisitos = new();
	public List<string> Anteriormente = new();
	public string Resumen = "";

	public bool TieneEscena => !string.IsNullOrEmpty(Escena) && ResourceLoader.Exists(Escena);
	// "Caso 0" para el tutorial, "Capítulo N" para el resto
	public string Etiqueta => Numero == 0 ? "Caso 0" : $"Capítulo {Numero}";
	public Texture2D CargarIlustracion() =>
		!string.IsNullOrEmpty(Ilustracion) && ResourceLoader.Exists(Ilustracion) ? GD.Load<Texture2D>(Ilustracion) : null;
}

public class FaseCapitulo
{
	public string Id = "", Nombre = "", Objetivo = "";
}

// Mejor resultado de un capítulo resuelto (progreso global, no depende de la ranura)
public class ResultadoCapitulo
{
	public string Rango = "";   // S, A, B, C
	public int Lupas;           // 1 a 5
	public int Puntos;
	public int Porcentaje;      // pistas, diálogos clave y secretos
	public double Tiempo;
	public bool PrimerIntento;
	public int AyudasUtilizadas;
}

// Catálogo de capítulos (datos) y progreso del jugador: qué está resuelto, desbloqueado y sus mejores resultados.
// Agregar un capítulo nuevo solo pide editar Datos/capitulos.json.
public static class Capitulos
{
	public const string ArchivoDatos = "res://Datos/capitulos.json";
	public const string ArchivoProgreso = "user://progreso.json";

	private static List<InfoCapitulo> _lista;
	private static List<FaseCapitulo> _fases;
	private static Dictionary<string, ResultadoCapitulo> _resultados;
	// Capítulos que el jugador saltó ("Omitir tutorial"): cuentan como requisito cumplido, pero no como resueltos
	private static HashSet<string> _omitidos;

	public static IReadOnlyList<InfoCapitulo> Lista { get { Cargar(); return _lista; } }
	public static IReadOnlyList<FaseCapitulo> Fases { get { Cargar(); return _fases; } }

	public static InfoCapitulo Obtener(string id) => Lista.FirstOrDefault(c => c.Id == id);

	public static InfoCapitulo Siguiente(string id)
	{
		int i = Lista.ToList().FindIndex(c => c.Id == id);
		return i >= 0 && i + 1 < Lista.Count ? Lista[i + 1] : null;
	}

	public static FaseCapitulo Fase(string id) => Fases.FirstOrDefault(f => f.Id == id) ?? Fases.FirstOrDefault();

	private static void Cargar()
	{
		if (_lista != null) return;
		_lista = new();
		_fases = new();
		var json = new Json();
		using var archivo = FileAccess.Open(ArchivoDatos, FileAccess.ModeFlags.Read);
		if (archivo == null || json.Parse(archivo.GetAsText()) != Error.Ok)
		{
			GD.PushError("No se pudo leer " + ArchivoDatos);
			return;
		}
		var datos = json.Data.AsGodotDictionary();
		foreach (Variant v in datos["fases"].AsGodotArray())
		{
			var f = v.AsGodotDictionary();
			_fases.Add(new FaseCapitulo { Id = f["id"].AsString(), Nombre = f["nombre"].AsString(), Objetivo = f["objetivo"].AsString() });
		}
		foreach (Variant v in datos["capitulos"].AsGodotArray())
		{
			var c = v.AsGodotDictionary();
			_lista.Add(new InfoCapitulo
			{
				Id = c["id"].AsString(),
				Numero = c["numero"].AsInt32(),
				Titulo = c["titulo"].AsString(),
				Ilustracion = c["ilustracion"].AsString(),
				Escena = c["escena"].AsString(),
				PistasTotales = c["pistas_totales"].AsInt32(),
				DialogosClave = c["dialogos_clave"].AsInt32(),
				Secretos = c["secretos"].AsInt32(),
				Requisitos = c["requisitos"].AsGodotArray().Select(r => r.AsString()).ToList(),
				Anteriormente = c["anteriormente"].AsGodotArray().Select(r => r.AsString()).ToList(),
				Resumen = c["resumen"].AsString(),
			});
		}
	}

	// ---------- Progreso ----------

	private static Dictionary<string, ResultadoCapitulo> Resultados
	{
		get
		{
			if (_resultados != null) return _resultados;
			_resultados = new();
			_omitidos = new();
			var datos = GestorPartida.LeerArchivo(ArchivoProgreso) ?? GestorPartida.LeerArchivo(ArchivoProgreso + ".bak");
			if (datos != null && datos.TryGetValue("omitidos", out Variant omitidos))
				foreach (Variant id in omitidos.AsGodotArray()) _omitidos.Add(id.AsString());
			if (datos != null && datos.TryGetValue("capitulos", out Variant capitulos))
			{
				foreach (var (id, valor) in capitulos.AsGodotDictionary())
				{
					var r = valor.AsGodotDictionary();
					_resultados[id.AsString()] = new ResultadoCapitulo
					{
						AyudasUtilizadas = r.TryGetValue("ayudas_utilizadas", out var ayudas) ? ayudas.AsInt32() : 0,
						Rango = r["rango"].AsString(), Lupas = r["lupas"].AsInt32(), Puntos = r["puntos"].AsInt32(),
						Porcentaje = r["porcentaje"].AsInt32(), Tiempo = r["tiempo"].AsDouble(), PrimerIntento = r["primer_intento"].AsBool(),
					};
				}
			}
			return _resultados;
		}
	}

	public static bool Resuelto(string id) => Resultados.ContainsKey(id);
	public static ResultadoCapitulo Resultado(string id) => Resultados.TryGetValue(id, out var r) ? r : null;
	public static bool Omitido(string id) { _ = Resultados; return _omitidos.Contains(id); }
	public static bool Desbloqueado(InfoCapitulo c) => c.Requisitos.All(r => Resuelto(r) || Omitido(r));

	public static void MarcarOmitido(string id)
	{
		if (Omitido(id)) return;
		_omitidos.Add(id);
		GuardarProgreso();
	}

	// El primero desbloqueado que falta por resolver y no se omitió (null si ya no queda ninguno)
	public static InfoCapitulo Actual() => Lista.FirstOrDefault(c => Desbloqueado(c) && !Resuelto(c.Id) && !Omitido(c.Id));

	// Guarda el resultado si es el primero o si mejora al anterior (por puntos; el mejor tiempo y porcentaje se conservan aparte)
	public static void Registrar(string id, ResultadoCapitulo nuevo)
	{
		if (Resultados.TryGetValue(id, out var anterior))
		{
			if (nuevo.Puntos >= anterior.Puntos)
			{
				anterior.AyudasUtilizadas = nuevo.AyudasUtilizadas;
				anterior.Rango = nuevo.Rango;
				anterior.Lupas = nuevo.Lupas;
				anterior.Puntos = nuevo.Puntos;
			}
			anterior.Porcentaje = Math.Max(anterior.Porcentaje, nuevo.Porcentaje);
			anterior.Tiempo = Math.Min(anterior.Tiempo, nuevo.Tiempo);
			anterior.PrimerIntento |= nuevo.PrimerIntento;
		}
		else
		{
			Resultados[id] = nuevo;
		}
		GuardarProgreso();
	}

	private static void GuardarProgreso()
	{
		var capitulos = new Godot.Collections.Dictionary();
		foreach (var (id, r) in Resultados)
		{
			capitulos[id] = new Godot.Collections.Dictionary
			{
				{ "ayudas_utilizadas", r.AyudasUtilizadas },
				{ "rango", r.Rango }, { "lupas", r.Lupas }, { "puntos", r.Puntos },
				{ "porcentaje", r.Porcentaje }, { "tiempo", r.Tiempo }, { "primer_intento", r.PrimerIntento },
			};
		}
		var omitidos = new Godot.Collections.Array();
		foreach (string id in _omitidos ?? new HashSet<string>()) omitidos.Add(id);
		GestorPartida.EscribirArchivo(ArchivoProgreso, new Godot.Collections.Dictionary { { "capitulos", capitulos }, { "omitidos", omitidos } });
	}

	// Para las pruebas: vuelve a leer el progreso del disco
	public static void Recargar() { _resultados = null; _omitidos = null; }
}
