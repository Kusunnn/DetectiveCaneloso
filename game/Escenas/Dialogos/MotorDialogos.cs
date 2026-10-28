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
//                 "conclusiones": ["<id>", ...],
//                 "contadores": { "<id>": <número>, ... } }
public class EstadoDialogos
{
	private readonly Dictionary<string, HashSet<string>> _opcionesVisitadas = new();
	private readonly Dictionary<string, HashSet<string>> _nodosCompletados = new();
	private readonly HashSet<string> _conclusiones = new();
	private readonly Dictionary<string, int> _contadores = new();

	// Valor de un contador que todavía no ha cambiado (p. ej. la credibilidad empieza en 5)
	public readonly Dictionary<string, int> Predeterminados = new();

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

	public int Contador(string id) =>
		_contadores.TryGetValue(id, out int valor) ? valor : Predeterminados.GetValueOrDefault(id);

	// Devuelven true si es algo nuevo (para marcar la partida con cambios sin guardar)
	public bool MarcarOpcion(string arbol, string nodo, string opcion) => Conjunto(_opcionesVisitadas, arbol).Add(Clave(nodo, opcion));
	public bool MarcarNodo(string arbol, string nodo) => Conjunto(_nodosCompletados, arbol).Add(nodo);
	public bool DesbloquearConclusion(string id) => _conclusiones.Add(id);
	public void Sumar(string id, int cantidad) => _contadores[id] = Contador(id) + cantidad;

	public IEnumerable<string> Conclusiones => _conclusiones;

	public Diccionario Capturar()
	{
		var contadores = new Diccionario();
		foreach (var (id, valor) in _contadores) contadores[id] = valor;
		return new()
		{
			{ "opciones_visitadas", Tabla(_opcionesVisitadas) },
			{ "nodos_completados", Tabla(_nodosCompletados) },
			{ "conclusiones", new Arreglo(_conclusiones.OrderBy(c => c).Select(c => Variant.From(c))) },
			{ "contadores", contadores },
		};
	}

	public void Restaurar(Diccionario datos)
	{
		_opcionesVisitadas.Clear();
		_nodosCompletados.Clear();
		_conclusiones.Clear();
		_contadores.Clear();
		if (datos == null) return;
		LeerTabla(datos, "opciones_visitadas", _opcionesVisitadas);
		LeerTabla(datos, "nodos_completados", _nodosCompletados);
		if (datos.TryGetValue("conclusiones", out Variant conclusiones))
			foreach (Variant c in conclusiones.AsGodotArray()) _conclusiones.Add(c.AsString());
		if (datos.TryGetValue("contadores", out Variant contadores))
			foreach (var (id, valor) in contadores.AsGodotDictionary()) _contadores[id.AsString()] = valor.AsInt32();
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
	public Action<string, int> ContadorCambiado; // "al_completar": { "sumar": {...} } (id, valor nuevo)
	public Action<string> Evento;              // "al_completar": { "eventos": [...] }
	// Quién habla: el caso puede dar retratos o tonos propios (por defecto, Assets/Retratos/<nombre>.png)
	public Func<string, Hablante> HablantePorNombre;

	private readonly VentanaDialogo _ventana;
	private readonly Dictionary<string, Hablante> _hablantes = new();
	private readonly Dictionary<string, int> _ultimaVariante = new();

	public MotorDialogos(VentanaDialogo ventana)
	{
		_ventana = ventana;
	}

	private bool ConclusionDisponible(string id) => Estado.TieneConclusion(id) || TieneConclusion(id);

	private bool Cumple(ArbolDialogo arbol, CondicionDialogo condicion) =>
		condicion.Cumple(TienePista, ConclusionDisponible, n => Estado.NodoCompletado(arbol.Id, n), Estado.Contador);

	// Cómo se ve cada opción del nodo ahora mismo (sin las ocultas)
	public List<(OpcionDialogo Opcion, OpcionVista Vista)> OpcionesVisibles(ArbolDialogo arbol, NodoDialogo nodo)
	{
		var lista = new List<(OpcionDialogo, OpcionVista)>();
		foreach (var opcion in nodo.Opciones)
		{
			bool cumple = Cumple(arbol, opcion.Requiere);
			if (!cumple && opcion.OcultarSiNoCumple) continue;
			lista.Add((opcion, new OpcionVista(opcion.Texto, opcion.Tipo, Estado.OpcionVisitada(arbol.Id, nodo.Id, opcion.Id))
			{
				Bloqueada = !cumple,
			}));
		}
		return lista;
	}

	// Recorre el árbol desde "inicio" (o desde otro nodo) y devuelve la "salida" del nodo donde terminó.
	public async Task<string> Recorrer(ArbolDialogo arbol, string desde = null)
	{
		if (arbol == null) return "";
		var anteriores = new Stack<string>();
		var actual = arbol.Nodo(string.IsNullOrEmpty(desde) ? arbol.Inicio : desde);
		bool regreso = false; // Volvió a un nodo con opciones: no repite lo que ya dijo
		string salida = "";

		_ventana.Retener();
		while (actual != null)
		{
			if (!(regreso && actual.Opciones.Count > 0))
			{
				await Decir(arbol, actual);
				Completar(arbol, actual);
				var salto = actual.IrA.FirstOrDefault(s => Cumple(arbol, s.Si));
				if (salto != null) { actual = arbol.Nodo(salto.Destino); regreso = false; continue; }
				if (actual.Fin) { salida = actual.Salida; break; }
				if (actual.Opciones.Count == 0)
				{
					regreso = string.IsNullOrEmpty(actual.Siguiente);
					string proximo = !regreso ? actual.Siguiente
						: !string.IsNullOrEmpty(actual.Volver) ? actual.Volver
						: anteriores.Count > 0 ? anteriores.Pop() : null;
					actual = arbol.Nodo(proximo);
					continue;
				}
			}
			regreso = false;

			var visibles = OpcionesVisibles(arbol, actual);
			if (visibles.Count == 0) break;
			var personaje = Hablante(arbol, arbol.Personaje);
			personaje.Tiembla = false;
			int salir = visibles.FindLastIndex(v => v.Opcion.Fin);
			int elegida = await _ventana.Elegir(personaje, actual.Pregunta, visibles.Select(v => v.Vista).ToList(), salir);
			var opcion = visibles[elegida].Opcion;
			// «Seguir investigando» no se marca: siempre es la salida
			if (!opcion.Fin && Estado.MarcarOpcion(arbol.Id, actual.Id, opcion.Id)) Cambio?.Invoke();
			if (opcion.Fin)
			{
				var despedida = arbol.Nodo(opcion.Destino);
				if (despedida != null)
				{
					await Decir(arbol, despedida);
					Completar(arbol, despedida);
					salida = despedida.Salida;
				}
				break;
			}
			if (opcion.Destino != actual.Id) anteriores.Push(actual.Id);
			actual = arbol.Nodo(opcion.Destino);
		}
		_ventana.Soltar();
		return salida;
	}

	private async Task Decir(ArbolDialogo arbol, NodoDialogo nodo)
	{
		foreach (var linea in LineasDe(arbol, nodo))
		{
			string quien = string.IsNullOrEmpty(linea.Habla) ? arbol.Personaje : linea.Habla;
			var hablante = Hablante(arbol, quien);
			string animo = !string.IsNullOrEmpty(linea.Animo) ? linea.Animo : quien == arbol.Personaje ? nodo.Animo : "";
			hablante.Animo = animo;
			hablante.ColorAnimo = ColorDeAnimo(animo);
			hablante.Tiembla = (nodo.Nervioso && quien == arbol.Personaje) || Tiembla(animo);
			string texto = string.IsNullOrEmpty(linea.Gesto) ? linea.Texto : $"({linea.Gesto}) {linea.Texto}";
			await _ventana.Decir(hablante, texto.Trim());
		}
	}

	// "lineas" y, si hay, una de las "variantes" (nunca la misma dos veces seguidas)
	private IEnumerable<LineaDialogo> LineasDe(ArbolDialogo arbol, NodoDialogo nodo)
	{
		if (nodo.Variantes.Count == 0) return nodo.Lineas;
		string clave = arbol.Id + "/" + nodo.Id;
		int ultima = _ultimaVariante.GetValueOrDefault(clave, -1);
		int elegida = (int)(GD.Randi() % (uint)nodo.Variantes.Count);
		if (elegida == ultima && nodo.Variantes.Count > 1) elegida = (elegida + 1) % nodo.Variantes.Count;
		_ultimaVariante[clave] = elegida;
		return nodo.Lineas.Concat(nodo.Variantes[elegida]);
	}

	private Hablante Hablante(ArbolDialogo arbol, string nombre)
	{
		if (_hablantes.TryGetValue(nombre, out var hablante)) return hablante;
		hablante = HablantePorNombre?.Invoke(nombre) ?? _ventana.HablantePorNombre(nombre);
		if (nombre == arbol.Personaje) hablante.TonoVoz = arbol.TonoVoz;
		return _hablantes[nombre] = hablante;
	}

	// Mientras no haya voces, el ánimo junto al nombre dice cómo lo dice
	public static Color ColorDeAnimo(string animo)
	{
		string a = animo.ToLower();
		bool Es(params string[] raices) => raices.Any(r => a.StartsWith(r));
		if (Es("nervios", "asustad", "ansios", "preocupad", "acorralad", "incómod")) return new Color(0.75f, 0.5f, 0.05f);
		if (Es("molest", "enojad", "alterad", "furios", "cortante", "impacient", "harto", "harta", "frí", "a la defensiva", "desafiante", "sarcástic")) return new Color(0.65f, 0.15f, 0.1f);
		if (Es("trist", "quebrad", "apenad", "dolid", "arrepentid", "culpable", "derrotad", "sombrí", "cansad")) return new Color(0.2f, 0.33f, 0.6f);
		if (Es("aliviad", "cooperativ", "emocionad", "animad", "content", "amable", "confiad", "orgullos", "alegre", "sincer", "satisfech")) return new Color(0.15f, 0.45f, 0.15f);
		return new Color(0.35f, 0.35f, 0.35f);
	}

	private static bool Tiembla(string animo)
	{
		string a = animo.ToLower();
		return a.StartsWith("nervios") || a.StartsWith("asustad");
	}

	private void Completar(ArbolDialogo arbol, NodoDialogo nodo)
	{
		foreach (var (contador, cantidad) in nodo.Sumar)
		{
			Estado.Sumar(contador, cantidad);
			ContadorCambiado?.Invoke(contador, Estado.Contador(contador));
			Cambio?.Invoke();
		}
		foreach (string evento in nodo.Eventos) Evento?.Invoke(evento);

		bool nuevo = Estado.MarcarNodo(arbol.Id, nodo.Id);
		foreach (string conclusion in nodo.ConclusionesAlCompletar)
			nuevo |= Estado.DesbloquearConclusion(conclusion);
		if (!nuevo) return;
		foreach (string nota in nodo.NotasAlCompletar) NotaNueva?.Invoke(nota);
		Cambio?.Invoke();
	}
}
