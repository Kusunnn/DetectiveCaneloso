using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Interrogatorio con candados: el sospechoso repite su declaración, se confronta una frase y se elige la prueba.
// Los árboles del guion siguen decidiendo qué pasa con cada prueba; aquí solo se elige cuál opción de su nodo toca.
// Las líneas de sistema nuevas están en Datos/Dialogos/caso1_sistema.json.
public partial class GuionCaso1
{
	public static class LineasSistema
	{
		public const string TonyBurla = "Mmh... ¿eso es todo lo que trae, detective?";
		public const string Confirmar = "Si me equivoco, un inocente irá a prisión.";
		public static readonly (string Bien, string Mal)[] Casillas =
		{
			("La hora y el lugar cuadran, Nya.", "Eso no lo pone en el embarcadero, Nya."),
			("El arma y la llave cuadran, Nya.", "Con eso no explicas de dónde salió el arma, Nya."),
			("Ahí está la razón, Nya.", "¿Y por qué lo haría? Eso no me lo dice, Nya."),
		};
	}

	// Lo que no es una pista del diario: lo que confesaron (id de la conclusión → cómo se ve en el selector y en el caso)
	public static readonly (string Id, string Titulo)[] Testimonios =
	{
		("juan_carlos_3", "Testimonio: la confesión de Juan Carlos"),
		("jc_vio_tony_sendero", "Testimonio: Juan Carlos vio a Tony en el sendero"),
		("foto_aya_2312", "Foto de Aya de las 23:12"),
		("foto_aya_2319", "Foto de Aya de las 23:19"),
		("mimita_oyo_silbido", "Testimonio: el silbido que oyó Mimita"),
		("miawrio_oyo_cita", "Testimonio: lo que oyó Miawrio"),
		("eriz_hora_crimen", "Testimonio: la hora que anotó Eriz"),
	};
	// La opción CONTAR del guion de Aya se llama distinto que la conclusión que pide
	private static readonly Dictionary<string, string> OpcionDeTestimonio = new() { ["juan_carlos_3"] = "juan_carlos" };

	// Las pruebas mínimas de la historia (oportunidad, medio y motivo): la Reconstrucción las pide con sus fichas
	public static readonly (string Nombre, string[][] Pide)[] Casillas =
	{
		("OPORTUNIDAD", new[] { new[] { "cuaderno_eriz" }, new[] { "jc_vio_tony_sendero", "foto_aya_2312" }, new[] { "botas_tony" } }),
		("MEDIO", new[] { new[] { "remo_sangre" }, new[] { "hueco_remo" } }),
		("MOTIVO", new[] { new[] { "libro_guardia_quemado" }, new[] { "nota_porky" }, new[] { "recorte_periodico" } }),
	};

	public const string Culpable = "tony";
	// La credibilidad baja, pero nunca corta el interrogatorio: solo cuenta para la calificación
	private const int CredibilidadMinima = 2;
	// Historia: desde el primer error; Experto: nunca
	private static int ErroresParaCorazonada => Dificultad switch { "historia" => 1, "experto" => int.MaxValue, _ => 2 };
	private static int ErroresParaTresPruebas => Dificultad switch { "historia" => 2, "experto" => int.MaxValue, _ => 3 };
	private const int MinimoRelacionadas = 4;

	// El juego está esperando al jugador (frases, pruebas, armar el caso): ahí se puede guardar
	public bool EnEspera { get; private set; }
	public Action<string, string> AnimoDe;        // quien, ánimo: lo que se le ve en la cara
	public Func<Task> AlAcertar;                  // Golpe de sonido y push-in
	public Func<int, Task> AlTumbarExcusa;        // Tony: cuántas excusas lleva tumbadas
	private string _ultimoGuardado;
	// Lo pone la sala de la estación; en el campamento y el lago se interroga ahí mismo
	public bool EnSala;
	private readonly HashSet<string> _frasesDichas = new();

	public static string NombreDe(string quien) => quien switch
	{
		"juan_carlos" => "Juan Carlos", "aya" => "Aya", "miawrio" => "Miawrio", "eriz" => "Eriz", "mimita" => "Mimita", "tony" => "Tony", _ => quien,
	};

	private static string IdDe(ArbolDialogo arbol) => arbol.Id.StartsWith("caso1_") ? arbol.Id.Substring("caso1_".Length) : arbol.Id;
	private static string ClaveErrores(GuiaCaso1.Candado c) => "errores:" + c.Id;

	private void PrepararInterrogatorios()
	{
		Motor.ElegirAccion = ElegirAccion;
		Motor.AnimoDeLinea = (quien, animo) => AnimoDe?.Invoke(quien, animo);
		Motor.AntesDeNodo = AntesDeNodoInterrogatorio;
		Motor.DecorarOpcion = DecorarOpcionDeSala;
	}

	private async Task AntesDeNodoInterrogatorio(ArbolDialogo arbol, NodoDialogo nodo)
	{
		// Lo que rompe un candado: la prueba correcta
		if (nodo.ConclusionesAlCompletar.Any(d => Guia.Candados.Any(c => c.Da[0] == d && !Guia.Roto(c))))
		{
			if (Estado.Contador("credibilidad") < CredibilidadInicial) Estado.Sumar("credibilidad", 1);
			Motor.Ventana.RomperCandado();
			if (AlAcertar != null) await AlAcertar();
			if (nodo.ConclusionesAlCompletar.Any(c => c.StartsWith("tony_")) && AlTumbarExcusa != null)
				await AlTumbarExcusa(ExcusasTumbadas() + 1);
		}
		if (IdDe(arbol) == Culpable && nodo.Id == "error") await Motor.Hablar(arbol, "Tony", "sarcástico", LineasSistema.TonyBurla);
	}

	// ---------- Confrontar ----------

	private async Task<OpcionDialogo> ElegirAccion(ArbolDialogo arbol, NodoDialogo nodo)
	{
		string quien = IdDe(arbol);
		var salir = nodo.Opciones.LastOrDefault(o => o.Fin);
		var personaje = Motor.HablanteDe(arbol, arbol.Personaje);
		personaje.Tiembla = false;
		while (true)
		{
			var propios = Guia.Candados.Where(c => c.Quien == quien).ToList();
			var pendientes = propios.Where(c => !Guia.Roto(c)).ToList();
			if (pendientes.Count == 0) return salir;
			Motor.Ventana.MostrarCandados(propios.Count, propios.Count - pendientes.Count, personaje.Nombre);
			// Sin orden fijo (Tony) todas se pueden confrontar; los demás, solo la que sigue en su árbol
			var abiertos = pendientes.Where(c => c.Previo == null || Guia.Roto(Guia.Buscar(c.Previo))).ToList();

			GuiaCaso1.Candado candado;
			if (pendientes.Count == 1)
			{
				// Solo queda una frase falsa: directo a la prueba
				candado = pendientes[0];
				if (_frasesDichas.Add(candado.Id))
					await Motor.Hablar(arbol, arbol.Personaje, "nervioso", Guia.Frase(candado));
			}
			else
			{
				var frases = Guia.FrasesDe(quien);
				var visibles = Enumerable.Range(0, frases.Count).Where(i => FraseVisible(i, propios)).ToList();
				int marcada = await MarcaDeCorazonada(arbol, abiertos, visibles);
				int inicio = Math.Max(visibles.IndexOf(marcada), 0);
				EnEspera = true;
				Motor.Cambio?.Invoke();
				GuardarEnEspera(arbol, nodo);
				int elegida = await Motor.Ventana.ElegirFrase(personaje, visibles.Select(i => frases[i]).ToList(), inicio, visibles.IndexOf(marcada), i => PistaDeTony(visibles[i], propios));
				EnEspera = false;
				if (elegida < 0) return salir;
				int frase = visibles[elegida];
				candado = abiertos.FirstOrDefault(c => c.Frase == frase);
				if (candado == null)
				{
					// Una frase verdadera: cuenta para la corazonada del candado que sigue
					Estado.Sumar(ClaveErrores(abiertos[0]), 1);
					Motor.Cambio?.Invoke();
					await Motor.Hablar(arbol, "Hecdog", "serio", Guia.Interrogatorio("no_es_esa"));
					continue;
				}
			}

			var nodoCandado = arbol.Nodo(candado.Nodo) ?? nodo;
			var (relacionadas, todas) = ListasDePruebas(nodoCandado, candado);
			EnEspera = true;
			Motor.Cambio?.Invoke();
			GuardarEnEspera(arbol, nodo);
			string id = await Motor.Ventana.ElegirEvidencia(Motor.HablanteDe(arbol, "Caneloso"), relacionadas, todas);
			EnEspera = false;
			if (id == null)
			{
				if (pendientes.Count == 1) return salir;
				continue;
			}
			var resultado = OpcionDePrueba(arbol, nodoCandado, id);
			if (resultado.Destino == "error" || resultado.Destino.StartsWith("e_"))
			{
				Estado.Sumar(ClaveErrores(candado), 1);
				Motor.Cambio?.Invoke();
			}
			return resultado;
		}
	}

	private void GuardarEnEspera(ArbolDialogo arbol, NodoDialogo nodo)
	{
		if (_ultimoGuardado == arbol.Id + nodo.Id) return;
		_ultimoGuardado = arbol.Id + nodo.Id;
		Autoguardado?.Invoke("Interrogatorio");
	}

	// Una frase que sale de una confesión no se ve hasta que se dijo. Las de candados ya rotos, tampoco.
	private bool FraseVisible(int indice, List<GuiaCaso1.Candado> propios)
	{
		var dueno = propios.FirstOrDefault(c => c.Frase == indice);
		if (dueno == null) return true;
		if (Guia.Roto(dueno)) return false;
		return !dueno.Oculta || Guia.Roto(Guia.Buscar(dueno.Previo));
	}

	// La frase que marca la corazonada de Caneloso (o -1). La primera vez, la piensa en voz alta.
	private async Task<int> MarcaDeCorazonada(ArbolDialogo arbol, List<GuiaCaso1.Candado> abiertos, List<int> visibles)
	{
		var candado = abiertos.FirstOrDefault(c => Estado.Contador(ClaveErrores(c)) >= ErroresParaCorazonada);
		if (candado == null || !visibles.Contains(candado.Frase)) return -1;
		if (_frasesDichas.Add("corazonada:" + candado.Id))
			await Motor.Hablar(arbol, "Caneloso", "firme", Guia.Interrogatorio("corazonada"));
		return candado.Frase;
	}

	// Tony: lo que le falta a cada excusa, dicho sin rodeos
	private string PistaDeTony(int frase, List<GuiaCaso1.Candado> propios)
	{
		var candado = propios.FirstOrDefault(c => c.Frase == frase);
		return candado == null || !candado.EsDeTony || Guia.Listo(candado) ? null : Guia.FaltaParaTony(candado);
	}

	// Primero lo que tiene que ver; las demás en «Todas». Con 3 errores, solo tres.
	private (List<(string Id, string Titulo, Texture2D Imagen)> Relacionadas, List<(string Id, string Titulo, Texture2D Imagen)> Todas) ListasDePruebas(NodoDialogo nodo, GuiaCaso1.Candado candado)
	{
		var disponibles = PruebasDisponibles();
		var opciones = nodo.Opciones.Where(MotorDialogos.EsPrueba).Select(o => IdEnEscena(o.Id));
		var cercanas = new HashSet<string>(opciones.Concat(candado.Pistas).Concat(candado.Conclusiones));
		var relacionadas = disponibles.Where(p => cercanas.Contains(p.Id)).ToList();
		// Que no queden tan pocas que la respuesta se regale
		foreach (var otra in disponibles.Where(p => !cercanas.Contains(p.Id)).OrderBy(p => p.Id))
		{
			if (relacionadas.Count >= MinimoRelacionadas) break;
			relacionadas.Add(otra);
		}
		relacionadas = relacionadas.OrderBy(p => p.Titulo).ToList();

		if (Estado.Contador(ClaveErrores(candado)) >= ErroresParaTresPruebas && Correcta(nodo) is { } correcta)
		{
			string idCorrecta = IdEnEscena(correcta.Id);
			if (disponibles.Any(p => p.Id == idCorrecta))
			{
				var senuelos = relacionadas.Where(p => p.Id != idCorrecta).Take(2);
				return (disponibles.Where(p => p.Id == idCorrecta).Concat(senuelos).OrderBy(p => p.Titulo).ToList(), null);
			}
		}
		return (relacionadas, disponibles.OrderBy(p => p.Titulo).ToList());
	}

	private static string IdEnEscena(string idGuion)
	{
		if (IdsEscena.TryGetValue(idGuion, out string enEscena)) return enEscena;
		return OpcionDeTestimonio.FirstOrDefault(p => p.Value == idGuion).Key ?? idGuion;
	}

	// La opción correcta del paso: las equivocadas van a nodos «e_...»
	private static OpcionDialogo Correcta(NodoDialogo nodo) => nodo.Opciones.FirstOrDefault(o => MotorDialogos.EsPrueba(o) && !o.Destino.StartsWith("e_"));

	// Lo que el guion contempla sigue su ruta; lo demás va al regaño («error»)
	private OpcionDialogo OpcionDePrueba(ArbolDialogo arbol, NodoDialogo nodo, string id)
	{
		string enGuion = IdsEscena.FirstOrDefault(p => p.Value == id).Key ?? id;
		if (OpcionDeTestimonio.TryGetValue(id, out string otra)) enGuion = otra;
		var opcion = nodo.Opciones.FirstOrDefault(o => MotorDialogos.EsPrueba(o) && (o.Id == enGuion || o.Id == id));
		if (opcion != null && Motor.Disponible(arbol, opcion)) return opcion;
		return new OpcionDialogo { Id = id, Destino = "error" };
	}

	// Pistas del diario y testimonios conseguidos
	public List<(string Id, string Titulo, Texture2D Imagen)> PruebasDisponibles()
	{
		var lista = new List<(string, string, Texture2D)>();
		foreach (Variant pista in _hud.Get("pistas").AsGodotArray())
		{
			var r = pista.As<Resource>();
			if (r.Get("origen_id").AsStringName().ToString() != "") continue; // Los detalles no son pruebas sueltas
			lista.Add((r.Get("id").AsStringName().ToString(), r.Get("titulo").AsString(), r.Get("imagen").As<Texture2D>()));
		}
		foreach (var (id, titulo) in Testimonios)
			if (Estado.TieneConclusion(id)) lista.Add((id, titulo, null));
		return lista;
	}

	// ---------- En la sala y en el campamento ----------

	// «Interrogar a Aya (1/2 candados) · Tengo algo»
	private string DecorarOpcionDeSala(ArbolDialogo arbol, OpcionDialogo opcion)
	{
		if (arbol.Id != ArbolSala || !Sospechosos.Contains(opcion.Id)) return opcion.Texto;
		var propios = Guia.Candados.Where(c => c.Quien == opcion.Id).ToList();
		string texto = $"{opcion.Texto}  ({propios.Count(Guia.Roto)}/{propios.Count} candados)";
		return Guia.Brilla(opcion.Id) ? texto + "  ·  " + Guia.Interrogatorio("brilla") : texto;
	}

	public bool DioDeclaracion(string quien) => Estado.NodoCompletado("caso1_" + quien, NodoDeclaracion(quien));
	private static string NodoDeclaracion(string quien) => quien == Culpable ? "excusa_1" : "mentira";

	// Fuera de la sala: primero la conversación del guion (su versión, sin pruebas). Las pruebas son solo para el
	// interrogatorio, que se abre ahí mismo cuando el jugador lo elige.
	public async Task HablarCon(string quien, Func<bool> sigue)
	{
		bool sinSecretos = Guia.Candados.Where(c => c.Quien == quien).All(Guia.Roto);
		if (!DioDeclaracion(quien) || sinSecretos)
		{
			await Charla(quien);
			return;
		}
		var arbol = _arboles["caso1_" + quien];
		var opciones = new List<OpcionVista>
		{
			new(Guia.Interrogatorio("interrogar").Replace("{quien}", NombreDe(quien)), "INTERROGAR"),
			new(Guia.Interrogatorio("seguir")),
		};
		if (Guia.Brilla(quien)) opciones[0].Texto += "  ·  " + Guia.Interrogatorio("brilla");
		string pregunta = Guia.Interrogatorio("que_hago").Replace("{quien}", NombreDe(quien));
		int elegida = await Motor.Ventana.Elegir(Motor.HablanteDe(arbol, "Caneloso"), pregunta, opciones, 1);
		if (elegida == 0 && sigue()) await Interrogar(quien, sigue);
	}

	// Charla en el campamento: su versión, sin pruebas. Lo que ya dijo no se repite.
	public async Task Charla(string quien)
	{
		var arbol = _arboles["caso1_" + quien];
		var declaracion = arbol.Nodo(NodoDeclaracion(quien));
		if (!DioDeclaracion(quien))
		{
			foreach (var linea in declaracion.Lineas.Where(l => l.Habla == arbol.Personaje))
				await Motor.Hablar(arbol, linea.Habla, linea.Animo ?? "", linea.Texto);
			Estado.MarcarNodo(arbol.Id, declaracion.Id);
			foreach (string nota in declaracion.NotasAlCompletar) Motor.NotaNueva?.Invoke(nota);
			Motor.Cambio?.Invoke();
		}
		else
		{
			bool sinSecretos = Guia.Candados.Where(c => c.Quien == quien).All(Guia.Roto);
			await Motor.DecirNodo(arbol, arbol.Nodo(sinSecretos ? (quien == Culpable ? "cerrado" : "confeso") : "regreso_1"));
		}
		AlTerminar?.Invoke("Charla: " + quien);
	}
}
