using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diccionario = Godot.Collections.Dictionary;

// HU-27: el guion del Caso 1 (Datos/Dialogos/caso1_*.json) compartido por los tres escenarios.
// El Campamento (introducción e interrogatorios en el sitio), el Lago (comentarios de sus pistas) y la sala de
// la estación (Hecdog, los seis implicados y la acusación) usan el mismo estado, que viaja en la partida.
public class GuionCaso1
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
	public bool AbrirExpediente;

	public EstadoDialogos Estado => Motor.Estado;
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

		foreach (string id in new[] { ArbolIntroduccion, ArbolSala, ArbolAcusacion }.Concat(Sospechosos.Select(s => "caso1_" + s)))
			_arboles[id] = ArbolDialogo.Cargar(RutaDialogos + id + ".json");
		CargarPistas();
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

	public string NotaDeDetalle(string id) => _notasDetalles.TryGetValue(id, out string nota) ? nota : null;

	// ---------- Conversaciones ----------

	// 2.x: Hecdog recibe a Caneloso y le da el expediente
	public async Task Introduccion()
	{
		await Motor.Recorrer(_arboles[ArbolIntroduccion]);
		AlTerminar?.Invoke("Introducción");
	}

	// Un interrogatorio. Si Hecdog pierde la paciencia, se acusa ya (devuelve true).
	public async Task<bool> Interrogar(string quien, Func<bool> sigue)
	{
		await AntesDeInterrogar(quien);
		if (!sigue()) return true;
		string salida = await Motor.Recorrer(_arboles["caso1_" + quien]);
		AlTerminar?.Invoke("Diálogo: " + quien);
		if (salida != "paciencia" || !sigue()) return false;
		await Sala(sigue, "paciencia");
		return true;
	}

	// 3.1 y 4.x: Hecdog y los seis en la sala. Se sale con «Seguir investigando» o acusando.
	public async Task Sala(Func<bool> sigue, string desde = null)
	{
		while (sigue())
		{
			string salida = await Motor.Recorrer(_arboles[ArbolSala], desde);
			AlTerminar?.Invoke("Sala");
			desde = "sala";
			if (salida.StartsWith("interrogar:"))
			{
				if (await Interrogar(salida.Substring("interrogar:".Length), sigue)) return;
				continue;
			}
			if (salida == "acusar") await Acusar(sigue);
			return;
		}
	}

	// 11.x y 12.x: la acusación y uno de los dos finales
	private async Task Acusar(Func<bool> sigue)
	{
		string final = await Motor.Recorrer(_arboles[ArbolAcusacion]);
		if (!sigue()) return;
		_hud.Call("avisar", final == "A" ? "FINAL A" : "FINAL B", final == "A" ? "LA CONFESIÓN" : "EL ERROR TRÁGICO");
		AlTerminar?.Invoke("Fin del caso");
	}

	private void AlCambiarContador(string id, int valor)
	{
		if (id == "credibilidad")
			_hud.Call("avisar", "CREDIBILIDAD", $"{Math.Max(valor, 0)} / {CredibilidadInicial}");
	}

	// Ids de fase de capitulos.json, según hasta dónde llegó el guion
	public string FaseActual() =>
		Resuelto ? "epilogo"
		: Estado.NodoCompletado(ArbolAcusacion, "pregunta") ? "resolucion"
		: SalaVista ? "interrogatorios"
		: IntroduccionVista ? "investigacion"
		: "introduccion";

	// Objetivo que manda el guion (o null para que el escenario ponga el suyo)
	public string Objetivo()
	{
		if (Resuelto) return "Caso cerrado.";
		if (!IntroduccionVista) return "Escucha a Hecdog: él tiene el expediente.";
		if (!SalaVista) return null;
		return TonyDisponible
			? "Todos confesaron su secreto... menos uno. Habla con Tony y luego acusa."
			: "Interroga a los seis y enséñales la prueba que les tumbe la historia.";
	}

	// ---------- Guardado ----------

	public Diccionario Capturar() => Estado.Capturar();

	// Opciones vistas y nodos recorridos (las partidas de antes no lo traen)
	public void Restaurar(Diccionario estado)
	{
		Estado.Restaurar(estado.TryGetValue("dialogos", out Variant dialogos) ? dialogos.AsGodotDictionary() : null);
		// Toda partida guardada ya pasó la presentación de Hecdog (las de antes del guion no la traen)
		Estado.MarcarNodo(ArbolIntroduccion, "ir");
	}
}
