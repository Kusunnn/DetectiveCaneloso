using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diccionario = Godot.Collections.Dictionary;
using Arreglo = Godot.Collections.Array;

// HU-27: lo que el jugador ya recorrió en los diálogos. Va dentro de la partida guardada:
//   "dialogos": { "opciones_visitadas": { "<árbol>": ["<nodo>/<opción>", ...] },
//                 "nodos_completados":  { "<árbol>": ["<nodo>", ...] },
//                 "conclusiones": ["<id>", ...] }
public class EstadoDialogos
{
	private readonly Dictionary<string, HashSet<string>> _opcionesVisitadas = new();
	private readonly Dictionary<string, HashSet<string>> _nodosCompletados = new();
	private readonly HashSet<string> _conclusiones = new();

	private static string Clave(string nodo, string opcion) => nodo + "/" + opcion;

	private static HashSet<string> Conjunto(Dictionary<string, HashSet<string>> tabla, string arbol)
	{
		if (!tabla.TryGetValue(arbol, out var conjunto)) tabla[arbol] = conjunto = new HashSet<string>();
		return conjunto;
	}

	public bool OpcionVisitada(string arbol, string nodo, string opcion) =>
		_opcionesVisitadas.TryGetValue(arbol, out var c) && c.Contains(Clave(nodo, opcion));

	public bool NodoCompletado(string arbol, string nodo) =>
		_nodosCompletados.TryGetValue(arbol, out var c) && c.Contains(nodo);

	public bool TieneConclusion(string id) => _conclusiones.Contains(id);

	// Devuelven true si es algo nuevo (para marcar la partida con cambios sin guardar)
	public bool MarcarOpcion(string arbol, string nodo, string opcion) => Conjunto(_opcionesVisitadas, arbol).Add(Clave(nodo, opcion));
	public bool MarcarNodo(string arbol, string nodo) => Conjunto(_nodosCompletados, arbol).Add(nodo);
	public bool DesbloquearConclusion(string id) => _conclusiones.Add(id);

	public IEnumerable<string> Conclusiones => _conclusiones;

	public Diccionario Capturar() => new()
	{
		{ "opciones_visitadas", Tabla(_opcionesVisitadas) },
		{ "nodos_completados", Tabla(_nodosCompletados) },
		{ "conclusiones", new Arreglo(_conclusiones.OrderBy(c => c).Select(c => Variant.From(c))) },
	};

	public void Restaurar(Diccionario datos)
	{
		_opcionesVisitadas.Clear();
		_nodosCompletados.Clear();
		_conclusiones.Clear();
		if (datos == null) return;
		LeerTabla(datos, "opciones_visitadas", _opcionesVisitadas);
		LeerTabla(datos, "nodos_completados", _nodosCompletados);
		if (datos.TryGetValue("conclusiones", out Variant conclusiones))
			foreach (Variant c in conclusiones.AsGodotArray()) _conclusiones.Add(c.AsString());
	}

	private static Diccionario Tabla(Dictionary<string, HashSet<string>> tabla)
	{
		var resultado = new Diccionario();
		foreach (var (arbol, conjunto) in tabla)
			resultado[arbol] = new Arreglo(conjunto.OrderBy(x => x).Select(x => Variant.From(x)));
		return resultado;
	}

	private static void LeerTabla(Diccionario datos, string clave, Dictionary<string, HashSet<string>> destino)
	{
		if (!datos.TryGetValue(clave, out Variant tabla)) return;
		foreach (var (arbol, lista) in tabla.AsGodotDictionary())
		{
			var conjunto = Conjunto(destino, arbol.AsString());
			foreach (Variant v in lista.AsGodotArray()) conjunto.Add(v.AsString());
		}
	}
}

// HU-27: recorre un ArbolDialogo con la caja de diálogo del juego (la misma del tutorial).
// El caso le dice cómo saber si se tiene una pista o una conclusión; el motor decide qué opciones
// se ven, cuáles están bloqueadas, marca las ya elegidas y apunta lo recorrido en Estado.
public class MotorDialogos
{
	public readonly EstadoDialogos Estado = new();

	public Func<string, bool> TienePista = _ => false;
	public Func<string, bool> TieneConclusion = _ => false;
	public Action Cambio;                      // Algo nuevo que guardar
	public Action<string> NotaNueva;           // "al_completar": { "notas": [...] }

	private readonly VentanaDialogo _ventana;

	public MotorDialogos(VentanaDialogo ventana)
	{
		_ventana = ventana;
	}

	private bool ConclusionDisponible(string id) => Estado.TieneConclusion(id) || TieneConclusion(id);

	// Cómo se ve cada opción del nodo ahora mismo (sin las ocultas)
	public List<(OpcionDialogo Opcion, OpcionVista Vista)> OpcionesVisibles(ArbolDialogo arbol, NodoDialogo nodo)
	{
		var lista = new List<(OpcionDialogo, OpcionVista)>();
		foreach (var opcion in nodo.Opciones)
		{
			bool cumple = opcion.Requiere.Cumple(TienePista, ConclusionDisponible, n => Estado.NodoCompletado(arbol.Id, n));
			if (!cumple && opcion.OcultarSiNoCumple) continue;
			lista.Add((opcion, new OpcionVista(opcion.Texto, opcion.Tipo, Estado.OpcionVisitada(arbol.Id, nodo.Id, opcion.Id))
			{
				Bloqueada = !cumple,
				Motivo = cumple ? "" : opcion.Motivo,
			}));
		}
		return lista;
	}

	public async Task Recorrer(ArbolDialogo arbol)
	{
		if (arbol == null) return;
		var personaje = _ventana.HablantePorNombre(arbol.Personaje);
		personaje.TonoVoz = arbol.TonoVoz;
		var anteriores = new Stack<string>();
		var actual = arbol.Nodo(arbol.Inicio);

		_ventana.Retener();
		while (actual != null)
		{
			personaje.Tiembla = actual.Nervioso;
			foreach (var linea in actual.Lineas)
			{
				bool otro = !string.IsNullOrEmpty(linea.Habla) && linea.Habla != arbol.Personaje;
				await _ventana.Decir(otro ? _ventana.HablantePorNombre(linea.Habla) : personaje, linea.Texto);
			}
			Completar(arbol, actual);

			if (actual.Opciones.Count == 0)
			{
				// Sin opciones: regresa al nodo de antes, donde la opción elegida ya se ve marcada
				string volver = !string.IsNullOrEmpty(actual.Volver) ? actual.Volver : (anteriores.Count > 0 ? anteriores.Pop() : null);
				actual = arbol.Nodo(volver);
				continue;
			}

			var visibles = OpcionesVisibles(arbol, actual);
			if (visibles.Count == 0) break;
			personaje.Tiembla = false;
			int salir = visibles.FindLastIndex(v => v.Opcion.Fin);
			int elegida = await _ventana.Elegir(personaje, actual.Pregunta, visibles.Select(v => v.Vista).ToList(), salir);
			var opcion = visibles[elegida].Opcion;
			// «Seguir investigando» no se marca: siempre es la salida
			if (!opcion.Fin && Estado.MarcarOpcion(arbol.Id, actual.Id, opcion.Id)) Cambio?.Invoke();
			if (opcion.Fin) break;
			if (opcion.Destino != actual.Id) anteriores.Push(actual.Id);
			actual = arbol.Nodo(opcion.Destino);
		}
		_ventana.Soltar();
	}

	private void Completar(ArbolDialogo arbol, NodoDialogo nodo)
	{
		bool nuevo = Estado.MarcarNodo(arbol.Id, nodo.Id);
		foreach (string conclusion in nodo.ConclusionesAlCompletar)
			nuevo |= Estado.DesbloquearConclusion(conclusion);
		if (!nuevo) return;
		foreach (string nota in nodo.NotasAlCompletar) NotaNueva?.Invoke(nota);
		Cambio?.Invoke();
	}
}
