using Godot;
using System;
using System.Collections.Generic;
using Diccionario = Godot.Collections.Dictionary;

// HU-27: árbol de diálogo leído de un archivo de datos (Datos/Dialogos/*.json).
// El motor no conoce ninguna historia: cualquier conversación se escribe como nodos y opciones.
//
// {
//   "id": "juan_carlos", "personaje": "Juan Carlos", "inicio": "saludo",
//   "nodos": {
//     "saludo": {
//       "lineas": ["Lo dice el personaje", { "habla": "Caneloso", "texto": "Lo dice otro" }],
//       "pregunta": "¿Qué le digo?",
//       "opciones": [
//         { "id": "luz", "tipo": "PREGUNTAR", "texto": "¿Dónde estabas?", "destino": "lena" },
//         { "id": "presionar", "tipo": "PRESIONAR", "texto": "...", "destino": "presion",
//           "requiere": { "pistas": ["lena_intacta"] }, "si_no_cumple": "bloquear", "motivo": "Me falta una prueba." },
//         { "id": "adios", "texto": "Seguir investigando", "fin": true }
//       ]
//     },
//     "lena": { "lineas": ["..."], "al_completar": { "conclusiones": ["..."], "notas": ["..."] } }
//   }
// }
//
// Un nodo sin opciones regresa al nodo anterior (o al que diga "volver"); "fin" termina la conversación.
public class ArbolDialogo
{
	public string Id = "";
	public string Personaje = "";
	public string Inicio = "";
	public float TonoVoz = 1f;
	public readonly Dictionary<string, NodoDialogo> Nodos = new();

	public NodoDialogo Nodo(string id) => id != null && Nodos.TryGetValue(id, out var nodo) ? nodo : null;

	public static ArbolDialogo Cargar(string ruta)
	{
		using var archivo = FileAccess.Open(ruta, FileAccess.ModeFlags.Read);
		if (archivo == null)
		{
			GD.PushError($"ArbolDialogo: no se pudo abrir {ruta} ({FileAccess.GetOpenError()}).");
			return null;
		}
		var json = new Json();
		if (json.Parse(archivo.GetAsText()) != Error.Ok)
		{
			GD.PushError($"ArbolDialogo: {ruta} no es un JSON válido (línea {json.GetErrorLine()}: {json.GetErrorMessage()}).");
			return null;
		}
		var arbol = DesdeDatos(json.Data.AsGodotDictionary());
		foreach (string error in arbol.Validar()) GD.PushError($"ArbolDialogo ({ruta}): {error}");
		return arbol;
	}

	public static ArbolDialogo DesdeDatos(Diccionario datos)
	{
		var arbol = new ArbolDialogo
		{
			Id = Texto(datos, "id"),
			Personaje = Texto(datos, "personaje"),
			Inicio = Texto(datos, "inicio"),
			TonoVoz = datos.TryGetValue("tono_voz", out Variant tono) ? tono.AsSingle() : 1f,
		};
		if (datos.TryGetValue("nodos", out Variant nodos))
		{
			foreach (var (clave, valor) in nodos.AsGodotDictionary())
				arbol.Nodos[clave.AsString()] = NodoDialogo.DesdeDatos(clave.AsString(), valor.AsGodotDictionary());
		}
		return arbol;
	}

	// Errores de escritura del archivo: destinos que no existen, ids repetidos, nodos sin salida...
	public List<string> Validar()
	{
		var errores = new List<string>();
		if (string.IsNullOrEmpty(Id)) errores.Add("falta \"id\".");
		if (Nodo(Inicio) == null) errores.Add($"el nodo de inicio «{Inicio}» no existe.");
		foreach (var (id, nodo) in Nodos)
		{
			if (!string.IsNullOrEmpty(nodo.Volver) && Nodo(nodo.Volver) == null)
				errores.Add($"el nodo «{id}» vuelve a «{nodo.Volver}», que no existe.");
			var ids = new HashSet<string>();
			foreach (var opcion in nodo.Opciones)
			{
				if (string.IsNullOrEmpty(opcion.Id)) errores.Add($"una opción del nodo «{id}» no tiene \"id\".");
				else if (!ids.Add(opcion.Id)) errores.Add($"la opción «{opcion.Id}» se repite en el nodo «{id}».");
				if (!opcion.Fin && Nodo(opcion.Destino) == null)
					errores.Add($"la opción «{opcion.Id}» del nodo «{id}» lleva a «{opcion.Destino}», que no existe.");
			}
		}
		return errores;
	}

	internal static string Texto(Diccionario datos, string clave) =>
		datos.TryGetValue(clave, out Variant valor) ? valor.AsString() : "";

	internal static List<string> Lista(Diccionario datos, string clave)
	{
		var lista = new List<string>();
		if (datos.TryGetValue(clave, out Variant valor))
			foreach (Variant v in valor.AsGodotArray()) lista.Add(v.AsString());
		return lista;
	}
}

// Una línea de diálogo. Sin "habla", la dice el personaje del árbol.
public class LineaDialogo
{
	public string Habla = "";
	public string Texto = "";
}

public class NodoDialogo
{
	public string Id = "";
	public readonly List<LineaDialogo> Lineas = new();
	public string Pregunta = "¿Qué le digo?";
	public readonly List<OpcionDialogo> Opciones = new();
	public string Volver = "";       // Nodo al que regresa si no tiene opciones (por defecto, el anterior)
	public bool Nervioso;            // El retrato tiembla mientras habla
	public readonly List<string> ConclusionesAlCompletar = new();
	public readonly List<string> NotasAlCompletar = new();

	public static NodoDialogo DesdeDatos(string id, Diccionario datos)
	{
		var nodo = new NodoDialogo
		{
			Id = id,
			Volver = ArbolDialogo.Texto(datos, "volver"),
			Nervioso = datos.TryGetValue("nervioso", out Variant nervioso) && nervioso.AsBool(),
		};
		if (datos.TryGetValue("pregunta", out Variant pregunta)) nodo.Pregunta = pregunta.AsString();
		if (datos.TryGetValue("lineas", out Variant lineas))
		{
			foreach (Variant v in lineas.AsGodotArray())
			{
				if (v.VariantType == Variant.Type.Dictionary)
				{
					var d = v.AsGodotDictionary();
					nodo.Lineas.Add(new LineaDialogo { Habla = ArbolDialogo.Texto(d, "habla"), Texto = ArbolDialogo.Texto(d, "texto") });
				}
				else nodo.Lineas.Add(new LineaDialogo { Texto = v.AsString() });
			}
		}
		if (datos.TryGetValue("opciones", out Variant opciones))
			foreach (Variant v in opciones.AsGodotArray()) nodo.Opciones.Add(OpcionDialogo.DesdeDatos(v.AsGodotDictionary()));
		if (datos.TryGetValue("al_completar", out Variant efectos))
		{
			var e = efectos.AsGodotDictionary();
			nodo.ConclusionesAlCompletar.AddRange(ArbolDialogo.Lista(e, "conclusiones"));
			nodo.NotasAlCompletar.AddRange(ArbolDialogo.Lista(e, "notas"));
		}
		return nodo;
	}
}

public class OpcionDialogo
{
	public string Id = "";
	public string Texto = "";
	public string Tipo = "";          // PREGUNTAR, PRESIONAR...
	public string Destino = "";
	public bool Fin;                  // Termina la conversación (también la eligen Esc y clic derecho)
	public CondicionDialogo Requiere = new();
	public bool OcultarSiNoCumple;    // "si_no_cumple": "ocultar"; por defecto se muestra bloqueada
	public string Motivo = "";        // Por qué está bloqueada (se ve junto a la opción)

	public static OpcionDialogo DesdeDatos(Diccionario datos) => new()
	{
		Id = ArbolDialogo.Texto(datos, "id"),
		Texto = ArbolDialogo.Texto(datos, "texto"),
		Tipo = ArbolDialogo.Texto(datos, "tipo"),
		Destino = ArbolDialogo.Texto(datos, "destino"),
		Fin = datos.TryGetValue("fin", out Variant fin) && fin.AsBool(),
		Requiere = datos.TryGetValue("requiere", out Variant requiere) ? CondicionDialogo.DesdeDatos(requiere.AsGodotDictionary()) : new CondicionDialogo(),
		OcultarSiNoCumple = ArbolDialogo.Texto(datos, "si_no_cumple") == "ocultar",
		Motivo = ArbolDialogo.Texto(datos, "motivo"),
	};
}

// Condición de una opción: todas las listas deben cumplirse a la vez.
public class CondicionDialogo
{
	public readonly List<string> Pistas = new();             // Pistas obtenidas (ids del expediente)
	public readonly List<string> SinPistas = new();          // Pistas que todavía NO se tienen
	public readonly List<string> Conclusiones = new();       // Conclusiones desbloqueadas
	public readonly List<string> NodosCompletados = new();   // Nodos de este árbol ya recorridos

	public bool Vacia => Pistas.Count + SinPistas.Count + Conclusiones.Count + NodosCompletados.Count == 0;

	public static CondicionDialogo DesdeDatos(Diccionario datos)
	{
		var c = new CondicionDialogo();
		c.Pistas.AddRange(ArbolDialogo.Lista(datos, "pistas"));
		c.SinPistas.AddRange(ArbolDialogo.Lista(datos, "sin_pistas"));
		c.Conclusiones.AddRange(ArbolDialogo.Lista(datos, "conclusiones"));
		c.NodosCompletados.AddRange(ArbolDialogo.Lista(datos, "nodos_completados"));
		return c;
	}

	public bool Cumple(Func<string, bool> tienePista, Func<string, bool> tieneConclusion, Func<string, bool> nodoCompletado)
	{
		foreach (string p in Pistas) if (!tienePista(p)) return false;
		foreach (string p in SinPistas) if (tienePista(p)) return false;
		foreach (string c in Conclusiones) if (!tieneConclusion(c)) return false;
		foreach (string n in NodosCompletados) if (!nodoCompletado(n)) return false;
		return true;
	}
}
