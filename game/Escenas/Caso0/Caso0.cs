using Godot;
using System;
using System.Collections.Generic;

// Guion del Caso 0 "La cinta perdida" (HU-20): fases, objetivos, indicaciones del tutorial,
// diálogos y desbloqueo del vestuario. El resto de sistemas no saben nada del caso.
//
// Fase 1: la sala (P1 grabadora).
// Fase 2: el rastro (P2 huellas, P4 testimonio de Eriz, P3 libro de turnos) → autorización.
// Fase 3: el vestuario, bloqueado hasta la autorización (P5 cinta).
// Fase 4: acusación en la sala.
public partial class Caso0 : Node
{
	private const string Grabadora = "grabadora";
	private const string Huellas = "huellas";
	private const string Turnos = "libro_turnos";
	private const string Testimonio = "testimonio_eriz";
	private const string Cinta = "cinta";

	private const uint CapaInteractuable = 1u << 3; // Capa 4
	private const uint CapaNpc = 1u << 4;           // Capa 5

	[Export] public Jugador Jugador;
	[Export] public CanvasLayer Hud;
	[Export] public VentanaDialogo Dialogo;
	[Export] public Puerta PuertaVestuario;
	[Export] public PuntoInteraccion Eriz;
	[Export] public PuntoInteraccion Porky;
	[Export] public PuntoInteraccion Comisario;
	[Export] public PuntoInteraccion SillaAcusacion;
	[Export] public Area3D ZonaPasillo;
	[Export] public Area3D ZonaRecepcion;
	[Export] public Area3D ZonaVestuario;
	// Dónde esperan Eriz y Porky durante la acusación
	[Export] public Node3D PuntoErizFinal;
	[Export] public Node3D PuntoPorkyFinal;

	private readonly HashSet<string> _pistas = new HashSet<string>();
	private readonly HashSet<string> _indicacionesDadas = new HashSet<string>();
	private bool _tutorial;
	private bool _autorizado = false;
	private bool _resuelto = false;
	private bool _enDialogo = false;

	private bool FaseDosCompleta => _pistas.Contains(Huellas) && _pistas.Contains(Turnos) && _pistas.Contains(Testimonio);

	public override void _Ready()
	{
		_tutorial = !(GestorPartida.Instancia?.SaltarTutorial ?? false);

		Hud.Set("caso", "00");
		Hud.Set("archivo", "Tutorial");
		Jugador.EvidenciaRegistrada += AlRegistrarEvidencia;
		Hud.Connect("linterna_cambiada", Callable.From<bool>(AlCambiarLinterna));
		Hud.Connect("panel_cambiado", Callable.From<bool, bool>(AlCambiarPanel));

		Eriz.Usado += () => Conversar(HablarConEriz);
		Porky.Usado += () => Conversar(HablarConPorky);
		Comisario.Usado += () => Conversar(HablarConComisario);
		SillaAcusacion.Usado += () => Conversar(Acusar);
		SillaAcusacion.Habilitar(false, CapaInteractuable);

		PuertaVestuario.Bloqueada = true;
		PuertaVestuario.MensajeBloqueada = "Vestuario: solo personal autorizado. Necesitas el permiso del comisario.";

		ZonaPasillo.BodyEntered += cuerpo => { if (cuerpo == Jugador) AlEntrarPasillo(); };
		ZonaRecepcion.BodyEntered += cuerpo => { if (cuerpo == Jugador) AlEntrarRecepcion(); };
		ZonaVestuario.BodyEntered += cuerpo => { if (cuerpo == Jugador) Indicar("vestuario", "Busca el casillero de Porky y ábrelo."); };

		Objetivo("Examina la grabadora de la sala.");
		Indicar("inicio", "Usa [W, A, S, D] para moverte y el ratón para mirar. [V] cambia la cámara.");
	}

	public override void _Process(double delta)
	{
		if (Jugador.SeHaMovido && !_pistas.Contains(Grabadora))
		{
			Indicar("apuntar", "Apunta a la grabadora con el punto central y pulsa [E] (o clic) para examinarla.");
		}
	}

	// ---------- Pistas y fases ----------

	private void AlRegistrarEvidencia(string id)
	{
		_pistas.Add(id);
		switch (id)
		{
			case Grabadora:
				Indicar("salir", "¡Primera pista! Date la vuelta y sal por la puerta al pasillo.");
				break;
			case Huellas:
				Indicar("hablar", "Al otro lado del pasillo alguien está limpiando. Apúntale y pulsa [E] para hablar.");
				break;
			case Turnos:
				Indicar("diario", "Pulsa [Q] para abrir tu diario. Con la rueda del ratón repasas las pistas del panel.");
				break;
			case Cinta:
				EmpezarAcusacion();
				return;
		}
		ActualizarObjetivo();
	}

	private void ActualizarObjetivo()
	{
		if (_resuelto) return;
		if (!_pistas.Contains(Grabadora))
		{
			Objetivo("Examina la grabadora de la sala.");
		}
		else if (!FaseDosCompleta)
		{
			var pendientes = new List<string>();
			if (!_pistas.Contains(Huellas)) pendientes.Add("rastros en el pasillo");
			if (!_pistas.Contains(Testimonio)) pendientes.Add("hablar con el conserje");
			if (!_pistas.Contains(Turnos)) pendientes.Add("libro de turnos (recepción)");
			Objetivo("Reconstruye la noche: " + string.Join(", ", pendientes) + ".");
		}
		else if (!_autorizado)
		{
			Objetivo("Pide al comisario (recepción) permiso para entrar al vestuario.");
			Indicar("autorizacion", "Ya tienes lo necesario. Habla con el comisario en la recepción.");
		}
		else if (!_pistas.Contains(Cinta))
		{
			Objetivo("Abre el casillero de Porky en el vestuario, al oeste del pasillo.");
		}
	}

	private void EmpezarAcusacion()
	{
		Objetivo("Vuelve a la sala de interrogatorio y señala al culpable.");
		Indicar("acusar", "Tienes todas las pistas. Vuelve a la sala y usa la silla del sospechoso para acusar.");

		// Los dos sospechosos esperan en la sala
		Eriz.GlobalTransform = PuntoErizFinal.GlobalTransform;
		Porky.GlobalTransform = PuntoPorkyFinal.GlobalTransform;
		SillaAcusacion.Habilitar(true, CapaInteractuable);
	}

	// ---------- Zonas ----------

	private void AlEntrarPasillo()
	{
		if (!Jugador.LinternaEncendida && !_pistas.Contains(Huellas))
		{
			Indicar("linterna", "Está muy oscuro. Pulsa [F] para encender la linterna.");
		}
	}

	private void AlEntrarRecepcion()
	{
		if (!_pistas.Contains(Turnos))
		{
			Indicar("recoger", "Recoge el libro de turnos del mostrador: apúntalo y pulsa [E].");
		}
	}

	private void AlCambiarLinterna(bool encendida)
	{
		if (encendida && _pistas.Contains(Grabadora) && !_pistas.Contains(Huellas))
		{
			Indicar("buscar_huellas", "Con luz directa se ven cosas nuevas. Revisa el suelo del pasillo.");
		}
	}

	private void AlCambiarPanel(bool abierto, bool expediente)
	{
		if (!abierto && _indicacionesDadas.Contains("diario") && !_autorizado)
		{
			Indicar("tras_diario", FaseDosCompleta
				? "Ya tienes lo necesario. Habla con el comisario en la recepción."
				: "Sigue el objetivo de la nota de arriba a la derecha.");
		}
	}

	// ---------- Diálogos ----------

	// Evita abrir dos conversaciones a la vez
	private async void Conversar(Func<System.Threading.Tasks.Task> conversacion)
	{
		if (_enDialogo) return;
		_enDialogo = true;
		await conversacion();
		_enDialogo = false;
	}

	private async System.Threading.Tasks.Task HablarConEriz()
	{
		if (_pistas.Contains(Testimonio))
		{
			await Dialogo.Decir("Eriz", "Ya le conté todo, detective. Yo solo quiero terminar de fregar.");
			return;
		}
		await Dialogo.Decir("Eriz",
			"¿Detective? Yo solo limpio, ¿eh? Anoche fregué la sala de interrogatorio y me fui a la una en punto. Lo firmé en el libro de turnos.",
			"Pero olvidé mi termo y volví sobre las tres. La recepción estaba vacía... y se oía la puerta del vestuario.",
			"Solo los agentes tienen llave del vestuario. Y yo, con estas zapatillas de goma, no hago ni ruido.");
		Jugador.RegistrarEvidencia(Testimonio, "Testimonio de Eriz",
			"Eriz se fue a la 1:00 y usa zapatillas de goma. Volvió a las 3:00 por su termo: la recepción estaba vacía y se oía la puerta del vestuario.",
			animar: false);
	}

	private async System.Threading.Tasks.Task HablarConPorky()
	{
		if (_resuelto)
		{
			await Dialogo.Decir("Porky", "Lo siento mucho, detective...");
			return;
		}
		await Dialogo.Decir("Porky",
			"¿Yo? Estuve toda la noche en la recepción, sin moverme de la silla.",
			"(Porky evita mirarte a los ojos.)");
	}

	private async System.Threading.Tasks.Task HablarConComisario()
	{
		if (_resuelto)
		{
			await Dialogo.Decir("Comisario", "Caso cerrado. Buen trabajo, Caneloso.");
		}
		else if (_pistas.Contains(Cinta))
		{
			await Dialogo.Decir("Comisario", "Los dos sospechosos te esperan en la sala de interrogatorio. Señala al culpable.");
		}
		else if (_autorizado)
		{
			await Dialogo.Decir("Comisario", "El vestuario está al otro extremo del pasillo, pasando la sala. Revisa los casilleros.");
		}
		else if (FaseDosCompleta)
		{
			await Dialogo.Decir("Comisario",
				"Botas de agente, un solo agente de guardia... y alguien en el vestuario a las tres. Bien visto.",
				"Toma, la llave del vestuario. Revisa los casilleros.");
			_autorizado = true;
			PuertaVestuario.Desbloquear();
			Jugador.MostrarAviso("Nueva zona disponible: Vestuario", 4.0);
			ActualizarObjetivo();
		}
		else
		{
			await Dialogo.Decir("Comisario",
				"Caneloso, esa cinta tiene una confesión y el juicio es mañana.",
				"Reconstruye la noche: la sala, el pasillo y el libro de turnos. Cuando tengas algo sólido, ven a verme.");
		}
	}

	private async System.Threading.Tasks.Task Acusar()
	{
		if (_resuelto) return;
		int elegido = await Dialogo.Elegir("Acusación", "¿Quién se llevó la cinta de la grabadora?",
			"Eriz, el conserje", "Porky, el agente de guardia");

		if (elegido == 0)
		{
			await Dialogo.Decir("Comisario",
				"¿Eriz? Usa zapatillas de goma, y las huellas del pasillo son de bota reglamentaria.",
				"Repasa tus pistas con la rueda del ratón o en el diario [Q] y vuelve a intentarlo.");
			return;
		}

		await Dialogo.Decir("Porky",
			"Está bien... ¡fui yo! Me quedé dormido en la sala durante la guardia.",
			"La grabadora me grabó roncando y hablando en sueños. Me dio tanta vergüenza que saqué la cinta y la escondí en mi casillero.");
		await Dialogo.Decir("Comisario", "La confesión está a salvo y el juicio seguirá mañana. Caso cerrado, Caneloso.");

		_resuelto = true;
		SillaAcusacion.Habilitar(false, CapaInteractuable);
		Objetivo("Caso cerrado. ¡Buen trabajo, detective!");
		Jugador.MostrarAviso("¡CASO RESUELTO!", 5.0);
		if (GestorPartida.Instancia != null)
		{
			GestorPartida.Instancia.SaltarTutorial = true;
			GestorPartida.Instancia.GuardarPartida();
		}
	}

	// ---------- Utilidades ----------

	private void Objetivo(string texto)
	{
		Hud.Set("objetivo", texto);
	}

	// Muestra una indicación del tutorial una sola vez (nada si se omitió el tutorial)
	private void Indicar(string clave, string texto)
	{
		if (!_tutorial || !_indicacionesDadas.Add(clave)) return;
		Jugador.MostrarIndicacion(texto);
	}
}
