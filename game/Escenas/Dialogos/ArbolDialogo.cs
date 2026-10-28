using Godot;
using System;
using System.Collections.Generic;
using Diccionario = Godot.Collections.Dictionary;

// HU-27: árbol de diálogo leído de un archivo de datos (Datos/Dialogos/*.json).
// El motor no conoce ninguna historia: cualquier conversación se escribe como nodos y opciones.
//
// {
//   "id": "juan_carlos", "personaje": "Juan Carlos", "inicio": "entrada",
//   "nodos": {
//     "entrada": { "ir_a": [ { "si": { "nodos_completados": ["mentira"] }, "destino": "regreso" }, { "destino": "mentira" } ] },
//     "mentira": {
//       "animo": "nervioso",
//       "lineas": [{ "habla": "Caneloso", "texto": "Platícame de anoche." }, { "gesto": "Truena los dedos", "texto": "Fui por leña." }],
//       "siguiente": "pruebas"
//     },
//     "regreso": { "variantes": [["¿Otra vez tú, bro?"], ["Ya te dije todo, wey."]], "siguiente": "pruebas" },
//     "pruebas": {
//       "pregunta": "¿Qué le enseño?",
//       "opciones": [
//         { "id": "lena", "tipo": "MOSTRAR", "texto": "Montón de leña intacto", "destino": "lena", "requiere": { "pistas": ["lena_intacta"] } },
//         { "id": "salir", "texto": "Seguir investigando", "fin": true, "destino": "despedida" }
//       ]
//     },
//     "lena": { "lineas": ["..."], "al_completar": { "conclusiones": ["juan_carlos_1"], "sumar": { "credibilidad": -1 } }, "siguiente": "pruebas_2" }
//   }
// }
//
// Cómo se mueve la conversación al terminar las líneas de un nodo:
//   "ir_a"       la primera entrada cuya condición "si" se cumple (sin "si" = la de siempre)
//   "fin"        termina la conversación; "salida" le dice al caso cómo terminó
//   "siguiente"  pasa a otro nodo y dice sus líneas
//   opciones     las muestra; si no hay, regresa al nodo de antes (o al de "volver") y muestra
//                sus opciones sin repetir sus líneas
// Una opción con "fin" termina la conversación; si trae "destino", antes se dicen las líneas de ese nodo.
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
			foreach (var (campo, destino) in new[] { ("volver", nodo.Volver), ("siguiente", nodo.Siguiente) })
				if (!string.IsNullOrEmpty(destino) && Nodo(destino) == null)
					errores.Add($"el nodo «{id}» tiene \"{campo}\": «{destino}», que no existe.");
			foreach (var salto in nodo.IrA)
				if (Nodo(salto.Destino) == null) errores.Add($"un \"ir_a\" del nodo «{id}» lleva a «{salto.Destino}», que no existe.");
			if (nodo.Lineas.Count == 0 && nodo.Variantes.Count == 0 && nodo.Opciones.Count == 0 && nodo.IrA.Count == 0
				&& !nodo.Fin && string.IsNullOrEmpty(nodo.Siguiente) && string.IsNullOrEmpty(nodo.Volver))
				errores.Add($"el nodo «{id}» está vacío.");
			var ids = new HashSet<string>();
			foreach (var opcion in nodo.Opciones)
			{
				if (string.IsNullOrEmpty(opcion.Id)) errores.Add($"una opción del nodo «{id}» no tiene \"id\".");
				else if (!ids.Add(opcion.Id)) errores.Add($"la opción «{opcion.Id}» se repite en el nodo «{id}».");
				bool sinDestino = opcion.Fin && string.IsNullOrEmpty(opcion.Destino);
				if (!sinDestino && Nodo(opcion.Destino) == null)
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

	internal static Dictionary<string, int> Numeros(Diccionario datos, string clave)
	{
		var tabla = new Dictionary<string, int>();
		if (datos.TryGetValue(clave, out Variant valor))
			foreach (var (k, v) in valor.AsGodotDictionary()) tabla[k.AsString()] = v.AsInt32();
		return tabla;
	}
}

// Una línea de diálogo. Sin "habla", la dice el personaje del árbol.
// "animo" se ve junto al nombre (JUAN CARLOS · NERVIOSO) mientras no haya voces; "gesto" es la
// acotación para animación y se lee entre paréntesis antes del texto.
public class LineaDialogo
{
	public string Habla = "";
	public string Texto = "";
	public string Animo = "";
	public string Gesto = "";

	public static LineaDialogo DesdeDatos(Variant v)
	{
		if (v.VariantType != Variant.Type.Dictionary) return new LineaDialogo { Texto = v.AsString() };
		var d = v.AsGodotDictionary();
		return new LineaDialogo
		{
			Habla = ArbolDialogo.Texto(d, "habla"),
			Texto = ArbolDialogo.Texto(d, "texto"),
			Animo = ArbolDialogo.Texto(d, "animo"),
			Gesto = ArbolDialogo.Texto(d, "gesto"),
		};
	}

	public static List<LineaDialogo> Varias(Variant lista)
	{
		var lineas = new List<LineaDialogo>();
		foreach (Variant v in lista.AsGodotArray()) lineas.Add(DesdeDatos(v));
		return lineas;
	}
}

// Un salto de "ir_a": sin condición, se toma siempre.
public class SaltoDialogo
{
	public CondicionDialogo Si = new();
	public string Destino = "";
}

public class NodoDialogo
{
	public string Id = "";
	public readonly List<LineaDialogo> Lineas = new();
	// Se dice una de estas tandas (distinta a la última vez), después de "lineas"
	public readonly List<List<LineaDialogo>> Variantes = new();
	public string Animo = "";        // Ánimo de las líneas del personaje que no traen el suyo
	public string Pregunta = "¿Qué le digo?";
	public readonly List<OpcionDialogo> Opciones = new();
	public readonly List<SaltoDialogo> IrA = new();
	public string Siguiente = "";    // Nodo que sigue (y dice sus líneas)
	public string Volver = "";       // Nodo al que regresa si no tiene opciones (por defecto, el anterior)
	public bool Fin;
	public string Salida = "";       // Cómo terminó la conversación, para el caso
	public bool Nervioso;            // El retrato tiembla mientras habla
	public readonly List<string> ConclusionesAlCompletar = new();
	public readonly List<string> NotasAlCompletar = new();
	// Cada vez que se dicen sus líneas (no solo la primera)
	public readonly Dictionary<string, int> Sumar = new();
	public readonly List<string> Eventos = new();

	public static NodoDialogo DesdeDatos(string id, Diccionario datos)
	{
		var nodo = new NodoDialogo
		{
			Id = id,
			Animo = ArbolDialogo.Texto(datos, "animo"),
			Siguiente = ArbolDialogo.Texto(datos, "siguiente"),
			Volver = ArbolDialogo.Texto(datos, "volver"),
			Fin = datos.TryGetValue("fin", out Variant fin) && fin.AsBool(),
			Salida = ArbolDialogo.Texto(datos, "salida"),
			Nervioso = datos.TryGetValue("nervioso", out Variant nervioso) && nervioso.AsBool(),
		};
		if (datos.TryGetValue("pregunta", out Variant pregunta)) nodo.Pregunta = pregunta.AsString();
		if (datos.TryGetValue("lineas", out Variant lineas)) nodo.Lineas.AddRange(LineaDialogo.Varias(lineas));
		if (datos.TryGetValue("variantes", out Variant variantes))
			foreach (Variant tanda in variantes.AsGodotArray()) nodo.Variantes.Add(LineaDialogo.Varias(tanda));
		if (datos.TryGetValue("opciones", out Variant opciones))
			foreach (Variant v in opciones.AsGodotArray()) nodo.Opciones.Add(OpcionDialogo.DesdeDatos(v.AsGodotDictionary()));
		if (datos.TryGetValue("ir_a", out Variant saltos))
		{
			foreach (Variant v in saltos.AsGodotArray())
			{
				var d = v.AsGodotDictionary();
				nodo.IrA.Add(new SaltoDialogo
				{
					Si = d.TryGetValue("si", out Variant si) ? CondicionDialogo.DesdeDatos(si.AsGodotDictionary()) : new CondicionDialogo(),
					Destino = ArbolDialogo.Texto(d, "destino"),
				});
			}
		}
		if (datos.TryGetValue("al_completar", out Variant efectos))
		{
			var e = efectos.AsGodotDictionary();
			nodo.ConclusionesAlCompletar.AddRange(ArbolDialogo.Lista(e, "conclusiones"));
			nodo.NotasAlCompletar.AddRange(ArbolDialogo.Lista(e, "notas"));
			foreach (var (contador, cantidad) in ArbolDialogo.Numeros(e, "sumar")) nodo.Sumar[contador] = cantidad;
			nodo.Eventos.AddRange(ArbolDialogo.Lista(e, "eventos"));
		}
		return nodo;
	}
}

public class OpcionDialogo
{
	public string Id = "";
	public string Texto = "";
	public string Tipo = "";          // PREGUNTAR, MOSTRAR, CONTAR...
	public string Destino = "";
	public bool Fin;                  // Termina la conversación (también la eligen Esc y clic derecho)
	public CondicionDialogo Requiere = new();
	public bool OcultarSiNoCumple;    // "si_no_cumple": "ocultar"; por defecto se muestra bloqueada como «???»
	public string Motivo = "";        // Nota para quien escribe: qué falta. No se muestra (daría la pista)

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

// Condición de una opción o de un salto: todas las listas deben cumplirse a la vez.
public class CondicionDialogo
{
	public readonly List<string> Pistas = new();             // Pistas obtenidas (ids del expediente)
	public readonly List<string> SinPistas = new();          // Pistas que todavía NO se tienen
	public readonly List<string> Conclusiones = new();       // Conclusiones desbloqueadas
	public readonly List<string> SinConclusiones = new();    // Conclusiones que todavía NO se tienen
	public readonly List<string> NodosCompletados = new();   // Nodos de este árbol ya recorridos
	public readonly Dictionary<string, int> Minimo = new();  // contador >= valor
	public readonly Dictionary<string, int> Maximo = new();  // contador <= valor

	public bool Vacia => Pistas.Count + SinPistas.Count + Conclusiones.Count + SinConclusiones.Count
		+ NodosCompletados.Count + Minimo.Count + Maximo.Count == 0;

	public static CondicionDialogo DesdeDatos(Diccionario datos)
	{
		var c = new CondicionDialogo();
		c.Pistas.AddRange(ArbolDialogo.Lista(datos, "pistas"));
		c.SinPistas.AddRange(ArbolDialogo.Lista(datos, "sin_pistas"));
		c.Conclusiones.AddRange(ArbolDialogo.Lista(datos, "conclusiones"));
		c.SinConclusiones.AddRange(ArbolDialogo.Lista(datos, "sin_conclusiones"));
		c.NodosCompletados.AddRange(ArbolDialogo.Lista(datos, "nodos_completados"));
		foreach (var (k, v) in ArbolDialogo.Numeros(datos, "minimo")) c.Minimo[k] = v;
		foreach (var (k, v) in ArbolDialogo.Numeros(datos, "maximo")) c.Maximo[k] = v;
		return c;
	}

	public bool Cumple(Func<string, bool> tienePista, Func<string, bool> tieneConclusion, Func<string, bool> nodoCompletado, Func<string, int> contador)
	{
		foreach (string p in Pistas) if (!tienePista(p)) return false;
		foreach (string p in SinPistas) if (tienePista(p)) return false;
		foreach (string c in Conclusiones) if (!tieneConclusion(c)) return false;
		foreach (string c in SinConclusiones) if (tieneConclusion(c)) return false;
		foreach (string n in NodosCompletados) if (!nodoCompletado(n)) return false;
		foreach (var (k, v) in Minimo) if (contador(k) < v) return false;
		foreach (var (k, v) in Maximo) if (contador(k) > v) return false;
		return true;
	}
}
