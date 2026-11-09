using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diccionario = Godot.Collections.Dictionary;

// HU-27: el guion del Caso 1 (Datos/Dialogos/caso1_*.json) compartido por los tres escenarios.
// El Campamento (introducción e interrogatorios en el sitio), el Lago (comentarios de sus pistas) y la sala de
// la estación (Hecdog, los seis implicados y la acusación) usan el mismo estado, que viaja en la partida.
public partial class GuionCaso1
{
	// El guion de Yaya, capas 2 a 12: un archivo por conversación
	public const string ArbolIntroduccion = "caso1_introduccion";
	public const string ArbolSala = "caso1_sala";
	public const string ArbolAcusacion = "caso1_acusacion";
	private const string RutaDialogos = "res://Datos/Dialogos/";
	private const string RutaPistas = "res://Datos/Dialogos/caso1_pistas.json";
	public const int CredibilidadInicial = 5;
	public static readonly string[] Sospechosos = { "juan_carlos", "aya", "miawrio", "eriz", "mimita", "tony" };
	// Las cinco confesiones que abren el interrogatorio de Tony
	private static readonly string[] Confesiones = { "juan_carlos_3", "aya_2", "miawrio_2", "eriz_1", "mimita_2" };
	// Pistas del Lago que en la escena tienen otro id que en el guion (guion → escena)
	private static readonly Dictionary<string, string> IdsEscena = new()
	{
		["disfraz_ciervo"] = "cornamenta_zancos",
		["soporte_remos"] = "hueco_remo",
		["silbato_gs"] = "silbato_ts",
	};

	public readonly MotorDialogos Motor;
	private readonly CanvasLayer _hud;
	private readonly Dictionary<string, ArbolDialogo> _arboles = new();
	private readonly Dictionary<string, (string Comentario, string Nota)> _pistas = new();
	private readonly Dictionary<string, string> _notasDetalles = new();

	// Lo pide el escenario: cada interrogatorio terminado (para autoguardar y el objetivo)
	public Action<string> AlTerminar;
	// Antes y después de cada interrogatorio (la sala sienta al implicado en su silla)
	public Func<string, Task> AntesDeInterrogar = _ => Task.CompletedTask;
	public Func<string, Task> DespuesDeInterrogar = _ => Task.CompletedTask;
	// La escena donde se juega (para las cinemáticas)
	public Node NodoEscena;
	public bool AbrirExpediente;

	public EstadoDialogos Estado => Motor.Estado;
	// El «qué sigue» del caso: lo pregunta cada escenario para su objetivo
	public readonly GuiaCaso1 Guia;
	// El cuaderno de ayudas del escenario (HU-10): Hecdog anota ahí lo que dice
	public SistemaAyudas Ayudas;
	public static string Dificultad => Configuracion.Instancia?.Dificultad ?? "detective";
	// Lo pone el escenario: el llavero viaja con el jugador
	public Func<bool> TieneLlavero = () => false;
	public bool IntroduccionVista => Estado.NodoCompletado(ArbolIntroduccion, "ir");
	public bool SalaVista => Estado.NodoCompletado(ArbolSala, "llegada");
	public bool TonyDisponible => Confesiones.All(Estado.TieneConclusion);
	public bool Resuelto => Estado.NodoCompletado(ArbolAcusacion, "final_a") || Estado.NodoCompletado(ArbolAcusacion, "final_b");

	public GuionCaso1(VentanaDialogo dialogo, CanvasLayer hud, Hablante caneloso, Action cambio)
	{
		_hud = hud;
		Motor = new MotorDialogos(dialogo)
		{
			TienePista = TienePista,
			Cambio = cambio,
			NotaNueva = nota => hud.Call("agregar_nota", nota),
			ContadorCambiado = AlCambiarContador,
			Evento = evento => { if (evento == "abrir_expediente") AbrirExpediente = true; },
			HablantePorNombre = nombre => nombre switch
			{
				"Caneloso" => caneloso,
				"Narración" => new Hablante(""),
				_ => dialogo.HablantePorNombre(nombre),
			},
		};
		Estado.Predeterminados["credibilidad"] = CredibilidadInicial;
		PrepararInterrogatorios();

		foreach (string id in new[] { ArbolIntroduccion, ArbolSala, ArbolAcusacion }.Concat(Sospechosos.Select(s => "caso1_" + s)))
			_arboles[id] = ArbolDialogo.Cargar(RutaDialogos + id + ".json");
		CargarPistas();
		CargarReconstruccion();
		Guia = new GuiaCaso1(TienePista, Estado.TieneConclusion, () => TieneLlavero());
	}

	// Las pistas del diario valen en cualquier escenario (las del Lago sirven en la sala)
	private bool TienePista(string id)
	{
		string enEscena = IdsEscena.TryGetValue(id, out string otro) ? otro : id;
		foreach (Variant pista in _hud.Get("pistas").AsGodotArray())
			if (pista.As<Resource>().Get("id").AsStringName().ToString() == enEscena) return true;
		return false;
	}

	private void CargarPistas()
	{
		using var archivo = FileAccess.Open(RutaPistas, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (archivo == null || json.Parse(archivo.GetAsText()) != Error.Ok)
		{
			GD.PushError($"GuionCaso1: no se pudo leer {RutaPistas}.");
			return;
		}
		var datos = json.Data.AsGodotDictionary();
		foreach (var (id, pista) in datos["pistas"].AsGodotDictionary())
		{
			var p = pista.AsGodotDictionary();
			string clave = id.AsString();
			_pistas[IdsEscena.TryGetValue(clave, out string enEscena) ? enEscena : clave] = (p["comentario"].AsString(), p["nota"].AsString());
		}
		foreach (var (id, nota) in datos["detalles"].AsGodotDictionary())
			_notasDetalles[id.AsString()] = nota.AsString();
	}

	// Lo que piensa Caneloso al recoger una pista y lo que anota (false si el guion no la tiene)
	public bool ComentarPista(Jugador jugador, string id)
	{
		if (!_pistas.TryGetValue(id, out var guion)) return false;
		// El tiempo en pantalla crece con el largo del comentario
		jugador.MostrarPensamiento(guion.Comentario, Math.Max(6.0, guion.Comentario.Length / 15.0));
		_hud.Call("agregar_nota", guion.Nota);
		return true;
	}

	// Con cinemática, el comentario se lee en ella: aquí solo queda la nota
	public string AnotarConCinematica(string id)
	{
		if (!_pistas.TryGetValue(id, out var guion)) return "";
		_hud.Call("agregar_nota", guion.Nota);
		return guion.Comentario;
	}

	// Lo que dice Caneloso del cuaderno de Eriz: el texto de la pista, con o sin ella en el diario
	public string ComentarioDe(string id) => _pistas.TryGetValue(id, out var guion) ? guion.Comentario : "";

	public string NotaDeDetalle(string id) => _notasDetalles.TryGetValue(id, out string nota) ? nota : null;

	// ---------- Conversaciones ----------

	// 2.x: Hecdog recibe a Caneloso y le da el expediente
	public async Task Introduccion()
	{
		await Motor.Recorrer(_arboles[ArbolIntroduccion]);
		AlTerminar?.Invoke("Introducción");
	}

	// Un interrogatorio: se sale con «Seguir investigando» o cuando ya no queda nada que decir
	public async Task Interrogar(string quien, Func<bool> sigue)
	{
		await AntesDeInterrogar(quien);
		if (!sigue()) return;
		await Motor.Recorrer(_arboles["caso1_" + quien]);
		Motor.Ventana.MostrarCandados(0, 0);
		_frasesDichas.Clear();
		// Con la noche ya reconstruida, el último candado de Tony lleva a su confesión
		if (quien == Culpable && TonySinCandados && ReconstruccionCompleta && !Resuelto && sigue())
		{
			await ConfesionDeTony(sigue);
			return;
		}
		if (sigue()) await DespuesDeInterrogar(quien);
		AlTerminar?.Invoke("Diálogo: " + quien);
	}

	// 3.1 y 4.x: Hecdog y los seis en la sala. Se sale con «Seguir investigando» o llegando a un final.
	public async Task Sala(Func<bool> sigue, string desde = null)
	{
		while (sigue())
		{
			string salida = await Motor.Recorrer(_arboles[ArbolSala], desde);
			AlTerminar?.Invoke("Sala");
			desde = "sala";
			if (salida.StartsWith("interrogar:"))
			{
				await Interrogar(salida.Substring("interrogar:".Length), sigue);
				if (Resuelto) return;
				continue;
			}
			if (salida == "armar")
			{
				if (await Reconstruir(sigue)) return;
				continue;
			}
			if (salida == "acusar_ya")
			{
				if (await AcusarYa(sigue)) return;
				continue;
			}
			if (salida == "pista")
			{
				var (_, texto) = PedirAyuda(GuiaCaso1.Escenario.Sala);
				await Motor.Hablar(_arboles[ArbolSala], "Hecdog", "serio", texto);
				continue;
			}
			return;
		}
	}

	// 11.x y 12.x: desde la acusación (o la confesión) hasta uno de los dos finales
	private async Task TerminarAcusacion(string desde, Func<bool> sigue)
	{
		var acusacion = _arboles[ArbolAcusacion];
		var finalA = acusacion.Nodo("final_a");
		// Lo que se cuenta en imagen: los tres párrafos del final B y el último del final A
		Motor.OmitirLinea = (arbol, nodo, linea) => arbol == acusacion && linea.Habla == "Narración" && (nodo.Id == "final_b" || linea == finalA.Lineas[^1]);
		// Antes de que Tony se derrumbe, se ve lo que acaba de demostrarse
		var antes = Motor.AntesDeNodo;
		Motor.AntesDeNodo = async (arbol, nodo) =>
		{
			if (arbol == acusacion && nodo.Id == "final_a" && NodoEscena != null)
				await CinematicaCaso1.Reproducir(NodoEscena, LibretosCaso1.Id2320, LibretosCaso1.Reconstruccion());
		};
		// Al final de la confesión, antes de que Hecdog lo detenga, la silueta de las 23:20 se ve como quien es
		Motor.AntesDeLinea = async (arbol, nodo, linea) =>
		{
			if (arbol == acusacion && nodo.Id == "final_a" && linea.Habla == "Hecdog" && NodoEscena != null)
				await CinematicaCaso1.Reproducir(NodoEscena, LibretosCaso1.IdRevelacion, LibretosCaso1.Revelacion());
		};
		string final = await Motor.Recorrer(acusacion, desde);
		Motor.OmitirLinea = _omitirNormal;
		Motor.AntesDeNodo = antes;
		Motor.AntesDeLinea = null;
		if (!sigue()) return;
		if (NodoEscena != null)
		{
			if (final == "A")
				await CinematicaCaso1.Reproducir(NodoEscena, LibretosCaso1.IdFinalA, LibretosCaso1.FinalA(finalA.Lineas[^1].Texto));
			else if (final == "B" && !CinematicaCaso1.Vista(LibretosCaso1.IdFinalB))
			{
				var parrafos = acusacion.Nodo("final_b").Lineas;
				await CinematicaCaso1.Reproducir(NodoEscena, LibretosCaso1.IdFinalB, LibretosCaso1.FinalBPorche(parrafos[0].Texto));
				if (sigue()) await CinematicaCaso1.Reproducir(NodoEscena, LibretosCaso1.IdFinalB, LibretosCaso1.FinalBLago(parrafos[1].Texto, parrafos[2].Texto), registrar: false);
			}
			if (!sigue()) return;
		}
		_hud.Call("avisar", final == "A" ? "FINAL A" : "FINAL B", final == "A" ? "LA CONFESIÓN" : "EL ERROR TRÁGICO");
		AlTerminar?.Invoke("Fin del caso");
	}

	// La credibilidad no se ve en pantalla y tiene piso: solo cuenta para la calificación
	private void AlCambiarContador(string id, int valor)
	{
		if (id == "credibilidad" && valor < CredibilidadMinima) Estado.Sumar(id, CredibilidadMinima - valor);
	}

	// Ids de fase de capitulos.json, según hasta dónde llegó el guion
	public string FaseActual() =>
		Resuelto ? "epilogo"
		: Estado.NodoCompletado(ArbolSala, "armar") ? "resolucion"
		: SalaVista ? "interrogatorios"
		: IntroduccionVista ? "investigacion"
		: "introduccion";

	// El objetivo principal del HUD en cada escenario
	public string ObjetivoEn(GuiaCaso1.Escenario donde)
	{
		if (Resuelto) return Guia.Objetivo("cerrado");
		if (!IntroduccionVista) return Guia.Objetivo("escuchar_hecdog");
		if (SalaVista) return Guia.ObjetivoInterrogatorios(donde);
		return donde == GuiaCaso1.Escenario.Sala ? Guia.Objetivo("sala_inicio") : Guia.ObjetivoExploracion(donde);
	}

	public string Objetivo() => ObjetivoEn(GuiaCaso1.Escenario.Sala);

	// Pone el objetivo en el HUD y los secundarios en el diario
	public void PublicarObjetivo(GuiaCaso1.Escenario donde)
	{
		_hud.Set("objetivo", ObjetivoEn(donde));
		_hud.Call("poner_objetivos", Guia.TituloSecundarios, new Godot.Collections.Array(Guia.Secundarios().Select(s => Variant.From(s))));
		foreach (string quien in Sospechosos.Where(DioDeclaracion))
		{
			var propios = Guia.Candados.Where(c => c.Quien == quien).ToList();
			var frases = Guia.FrasesDe(quien).Where((_, i) => !propios.Any(c => c.Frase == i && c.Oculta && !Guia.Roto(Guia.Buscar(c.Previo))));
			_hud.Call("poner_persona", NombreDe(quien), string.Join(" ", frases), Motor.Ventana.Retrato(NombreDe(quien)), propios.Count(Guia.Roto), propios.Count, Guia.Brilla(quien));
		}
	}

	// ---------- Ayudas ----------

	// La siguiente pista para donde está el jugador. Texto: lo que dice Hecdog (o por qué no puede).
	public (bool Nueva, string Texto) PedirAyuda(GuiaCaso1.Escenario donde)
	{
		if (!(Configuracion.Instancia?.AyudasInvestigacion ?? true)) return (false, Guia.Aviso("desactivadas"));
		if (Ayudas == null) return (false, Guia.Aviso("repetida"));
		var paso = SalaVista || donde == GuiaCaso1.Escenario.Sala ? Guia.Siguiente() : Guia.SiguienteExploracion(donde);
		var resultado = Guia.PedirAyuda(paso, Ayudas, Dificultad == "experto" ? 1 : 3);
		if (resultado.Nueva) Motor.Cambio?.Invoke();
		return resultado;
	}

	// Mensaje corto para Pausa → Pistas
	public string PedirPista(GuiaCaso1.Escenario donde)
	{
		if (!(Configuracion.Instancia?.AyudasInvestigacion ?? true)) return Guia.Aviso("desactivadas");
		var (nueva, _) = PedirAyuda(donde);
		return nueva ? Guia.Aviso("nueva") : Dificultad == "experto" ? Guia.Aviso("experto") : Guia.Aviso("repetida");
	}

	// Al empezar el capítulo: cómo se quiere investigar
	public async Task ElegirDificultad(Hablante caneloso)
	{
		// Las herramientas de prueba (--script) juegan sin elegir: se quedan con la dificultad que haya
		if (OS.GetCmdlineArgs().Contains("--script")) return;
		var opciones = Configuracion.Dificultades.Select(d => new OpcionVista(Guia.TextoDificultad(d.Id))).ToList();
		int actual = Math.Max(Array.FindIndex(Configuracion.Dificultades, d => d.Id == Dificultad), 0);
		int elegida = await Motor.Ventana.Elegir(caneloso, Guia.TextoDificultad("pregunta"), opciones, actual);
		if (Configuracion.Instancia != null) Configuracion.Instancia.Dificultad = Configuracion.Dificultades[elegida].Id;
		Motor.Cambio?.Invoke();
	}

	// ---------- Guardado ----------

	public Diccionario Capturar()
	{
		var datos = Estado.Capturar();
		datos["reconstruccion"] = CapturarReconstruccion();
		datos["dificultad"] = Dificultad;
		return datos;
	}

	// Opciones vistas y nodos recorridos (las partidas de antes no lo traen)
	public void Restaurar(Diccionario estado)
	{
		var datos = estado.TryGetValue("dialogos", out Variant dialogos) ? dialogos.AsGodotDictionary() : null;
		Estado.Restaurar(datos);
		RestaurarReconstruccion(datos);
		if (datos != null && datos.TryGetValue("dificultad", out Variant dificultad) && Configuracion.Instancia != null) Configuracion.Instancia.Dificultad = dificultad.AsString();
		// Toda partida guardada ya pasó la presentación de Hecdog (las de antes del guion no la traen)
		Estado.MarcarNodo(ArbolIntroduccion, "ir");
	}
}
