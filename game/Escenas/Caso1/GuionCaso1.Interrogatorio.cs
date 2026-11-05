using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Interrogatorio y acusación: preguntar, presionar y presentar pruebas, cortar sin acusar, armar el caso y el careo con Tony.
// Los textos del guion no cambian. Lo único nuevo son las líneas cortas de sistema de LineasSistema, que se pueden reemplazar.
public partial class GuionCaso1
{
	public static class LineasSistema
	{
		public const string Presionar = "Eso no me cuadra.";
		public const string PresionarConPrueba = "Pues enséñasela, Nya.";
		public const string PresionarSinPrueba = "Sin pruebas no, Caneloso, Nya.";
		public const string Corte = "Ya basta por ahora, Nya.";
		public const string TonySinCaso = "Para eso hay que armar el caso, Nya. Háblame cuando lo tengas.";
		public const string TonyBurla = "Mmh... ¿eso es todo lo que trae, detective?";
		public const string Explicacion = "Oportunidad, medio y motivo, Caneloso: llena las tres y me lo presentas, Nya.";
		public const string NoConvence = "Todavía no me convences, Caneloso.";
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

	// Las tres casillas: cada grupo se cumple con cualquiera de sus pruebas (ids de escena)
	public static readonly (string Nombre, string[][] Pide)[] Casillas =
	{
		("OPORTUNIDAD", new[] { new[] { "cuaderno_eriz" }, new[] { "jc_vio_tony_sendero", "foto_aya_2312" }, new[] { "botas_tony" } }),
		("MEDIO", new[] { new[] { "remo_sangre" }, new[] { "hueco_remo" } }),
		("MOTIVO", new[] { new[] { "libro_guardia_quemado" }, new[] { "nota_porky" }, new[] { "recorte_periodico" } }),
	};

	public const string Culpable = "tony";
	private const int CredibilidadTrasCorte = 2;

	// El caso que el jugador va armando: viaja en la partida
	public string CasoSospechoso = "";
	public readonly List<string>[] CasoPruebas = { new(), new(), new() };

	// El juego está esperando al jugador (menú del interrogatorio, armar el caso, turno del careo): ahí se puede guardar
	public bool EnEspera { get; private set; }
	public Func<Task<bool>> AbrirArmarCaso;      // Lo pone la sala: true si el jugador presentó el caso
	public Action<string, string> AnimoDe;        // quien, ánimo: lo que se le ve en la cara
	public Func<Task> AlAcertar;                  // Golpe de sonido y push-in
	public Func<int, Task> AlTumbarExcusa;        // Careo: cuántas excusas lleva tumbadas
	public Action<int, bool> SelloDeCasilla;      // Revisión: casilla y si encaja
	public Action Revisar;                        // Revisión: empieza
	private bool _enCareo;
	private string _ultimoGuardado;

	private static string NombreDe(string quien) => quien switch
	{
		"juan_carlos" => "Juan Carlos", "aya" => "Aya", "miawrio" => "Miawrio", "eriz" => "Eriz", "mimita" => "Mimita", "tony" => "Tony", _ => quien,
	};

	private void PrepararInterrogatorios()
	{
		Motor.ElegirAccion = ElegirAccion;
		Motor.AnimoDeLinea = (quien, animo) => AnimoDe?.Invoke(quien, animo);
		Motor.AntesDeNodo = AntesDeNodoInterrogatorio;
	}

	private async Task AntesDeNodoInterrogatorio(ArbolDialogo arbol, NodoDialogo nodo)
	{
		// Lo que hace confesar: la prueba correcta
		if (nodo.ConclusionesAlCompletar.Count > 0 && AlAcertar != null && arbol.Id != ArbolAcusacion)
		{
			if (Estado.Contador("credibilidad") < CredibilidadInicial) { Estado.Sumar("credibilidad", 1); AlCambiarContador("credibilidad", Estado.Contador("credibilidad")); }
			await AlAcertar();
		}
		// En el careo, si se equivoca, Tony se burla antes de que Hecdog lo regañe
		if (_enCareo && nodo.Id == "error") await Motor.Hablar(arbol, "Tony", "sarcástico", LineasSistema.TonyBurla);
	}

	// ---------- Las tres acciones ----------

	private async Task<OpcionDialogo> ElegirAccion(ArbolDialogo arbol, NodoDialogo nodo)
	{
		string quien = arbol.Personaje;
		bool esTony = arbol.Id == "caso1_" + Culpable;
		bool antesDelCaso = esTony && !_enCareo;
		var salir = nodo.Opciones.LastOrDefault(o => o.Fin);
		var personaje = Motor.HablanteDe(arbol, quien);
		personaje.Tiembla = false;
		while (true)
		{
			var acciones = new List<OpcionVista>();
			var ids = new List<string>();
			void Agregar(string id, string etiqueta, string texto) { ids.Add(id); acciones.Add(new OpcionVista(texto, etiqueta)); }
			if (!_enCareo) Agregar("preguntar", "PREGUNTAR", "Repítame su versión");
			if (!antesDelCaso)
			{
				if (!_enCareo) Agregar("presionar", "PRESIONAR", "Eso no me cuadra");
				Agregar("presentar", "PRESENTAR PRUEBA", "Abrir el expediente");
			}
			if (salir != null) Agregar("salir", "", _enCareo ? "Dejarlo por ahora" : salir.Texto);

			EnEspera = true;
			Motor.Cambio?.Invoke();
			if (_ultimoGuardado != arbol.Id + nodo.Id)
			{
				_ultimoGuardado = arbol.Id + nodo.Id;
				Autoguardado?.Invoke("Interrogatorio");
			}
			int elegida = await Motor.Ventana.Elegir(personaje, string.IsNullOrEmpty(nodo.Pregunta) ? $"¿Qué le digo a {NombreDe(quien)}?" : nodo.Pregunta, acciones, ids.IndexOf("salir"));
			EnEspera = false;
			switch (ids[elegida])
			{
				case "salir":
					return salir;
				case "preguntar":
					await Motor.Repetir(arbol);
					if (antesDelCaso) await Motor.Hablar(arbol, "Hecdog", "serio", LineasSistema.TonySinCaso);
					break;
				case "presionar":
					if (await Presionar(arbol, nodo)) return new OpcionDialogo { Id = "corte", Destino = "paciencia", Fin = false };
					break;
				default:
					var resultado = await Presentar(arbol, nodo);
					if (resultado != null) return resultado;
					break;
			}
		}
	}

	// La opción correcta del paso: las equivocadas van a nodos «e_...»
	private static OpcionDialogo Correcta(NodoDialogo nodo) => nodo.Opciones.FirstOrDefault(o => MotorDialogos.EsPrueba(o) && !o.Destino.StartsWith("e_"));

	// Presionar sirve de pista: si ya tiene con qué, Hecdog le da pie; si no, cuesta credibilidad. true = Hecdog cortó
	private async Task<bool> Presionar(ArbolDialogo arbol, NodoDialogo nodo)
	{
		await Motor.Hablar(arbol, "Caneloso", "firme", LineasSistema.Presionar);
		var correcta = Correcta(nodo);
		if (correcta != null && Motor.Disponible(arbol, correcta))
		{
			AnimoDe?.Invoke(arbol.Personaje, "nervioso");
			await Motor.Hablar(arbol, "Hecdog", "serio", LineasSistema.PresionarConPrueba);
			return false;
		}
		Estado.Sumar("credibilidad", -1);
		AlCambiarContador("credibilidad", Estado.Contador("credibilidad"));
		Motor.Cambio?.Invoke();
		await Motor.Hablar(arbol, "Hecdog", "molesto", LineasSistema.PresionarSinPrueba);
		return Estado.Contador("credibilidad") <= 0;
	}

	// Abre el selector con todo lo que tiene. Lo que el guion contempla sigue su ruta; lo demás va al regaño («error»).
	private async Task<OpcionDialogo> Presentar(ArbolDialogo arbol, NodoDialogo nodo)
	{
		string id = await Motor.Ventana.ElegirEvidencia(Motor.HablanteDe(arbol, "Caneloso"), PruebasDisponibles());
		if (id == null) return null;
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

	// ---------- Después del interrogatorio ----------

	// Hecdog corta: el implicado sale y se puede volver a intentar. Nunca lleva a acusar.
	private async Task CortarInterrogatorio(ArbolDialogo arbol)
	{
		await Motor.Hablar(arbol, "Hecdog", "harto", LineasSistema.Corte);
		int falta = CredibilidadTrasCorte - Estado.Contador("credibilidad");
		if (falta > 0) Estado.Sumar("credibilidad", falta);
		AlCambiarContador("credibilidad", Estado.Contador("credibilidad"));
		Motor.Cambio?.Invoke();
	}
}
