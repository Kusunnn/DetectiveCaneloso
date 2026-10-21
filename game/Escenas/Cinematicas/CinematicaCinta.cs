using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Cinemática final del Caso 0: se reproduce la cinta y se ve lo que grabó en la Sala 1 de madrugada.
// Primero, sobre negro, la confesión de Tito (es una grabación, no algo que pase en la sala).
// 03:09 Porky duerme en la silla y la grabadora graba (la trampa del comisario). 03:12 una figura sin rostro
// baja por la rejilla del techo, le susurra al micrófono, descuelga el reloj a propósito y lo deja caer
// (por eso se paró a las 3:12); se va por la rejilla y deja la pluma negra enganchada. 03:15 Porky despierta,
// recoge el reloj y lo vuelve a colgar, ve la luz de REC, para la grabadora, rebobina la cinta hasta el 03:12,
// la saca y se va al vestidor; sus botas son las huellas del pasillo. No se ve el rostro de la figura.
//
// Se rueda en la sala real con una cámara propia y al terminar deja todo como estaba.
// Todo depende del segundo t (posiciones, animaciones, cámara, luces y estado de los sonidos continuos),
// así que IrA, adelantar con clic o Enter y Movie Maker dan siempre lo mismo. Mantener [Espacio], [Esc]
// o B/Start la salta. Todo lo que se oye viene de la cinta: va por un bus con filtro de cinta.
public partial class CinematicaCinta : CanvasLayer
{
	[Signal] public delegate void TerminadaEventHandler();

	public class Actores
	{
		public Npc Porky;
		public Npc Eriz;
		public Jugador Jugador;
		public Node3D Reloj;
		public Node3D Grabadora;
		public MusicaFondo Musica;
		// Las tres líneas de la cinta: la confesión de Tito, el ruido de la rejilla y la voz desconocida
		public string[] Lineas;
	}

	// Mientras dura no debe aparecer ninguna ayuda, pensamiento ni aviso encima (ver Jugador y Caso0)
	public static bool EnCurso { get; private set; }

	// Para las capturas: el tiempo no avanza solo, se fija con IrA
	public bool Manual { get; set; }

	public void IrA(double t)
	{
		_t = t;
		Actualizar(true);
	}

	public static async Task Reproducir(Node3D sala, Actores actores)
	{
		// No se puede disparar dos veces a la vez
		if (EnCurso) return;
		EnCurso = true;
		var cinematica = new CinematicaCinta { _a = actores, _sala = sala };
		sala.AddChild(cinematica);
		await cinematica.ToSignal(cinematica, SignalName.Terminada);
	}

	// ---------- Tiempos (segundos) ----------

	private const double TPlay = 0.25;           // clic de PLAY sobre negro
	private const double TImagen = 4.2;          // termina el negro: SALA 1 · 03:09
	private const double TRejilla = 12.0;        // ruido metálico: se abre la rejilla
	private const double TSusurro = 20.1;
	private const double TSuelta = 25.8;         // la figura suelta el reloj
	private const double TGolpe = 26.45;         // el reloj golpea el piso: se para a las 3:12
	private const double TRebote = 26.75;
	private const double TSeVa = 31.2;           // la figura desaparece por la rejilla
	private const double TCierraRejilla = 31.4;
	private const double TDespierta = 32.6;      // 03:15
	private const double TAgarra = 38.25;        // Porky levanta el reloj del piso
	private const double TCuelga = 40.35;        // el reloj queda colgado otra vez
	private const double TSobresalto = 42.6;
	private const double TStop = 44.7;
	private const double TRebobina = 45.1;
	private const double TFinRebobinado = 47.6;
	private const double TExpulsa = 48.3;
	private const double TTomaCinta = 48.75;
	private const double TAbrePuerta = 51.9;
	private const double TCierraPuerta = 54.3;
	private const double TFin = 55.6;
	private const double SegundosParaSaltar = 1.2;

	// Tomas: inicio, cámara (posición y punto al que mira, al empezar y al terminar), campo de visión y
	// distancia de enfoque (0 = sin desenfoque)
	private record struct Toma(double Inicio, Vector3 Desde, Vector3 Hasta, Vector3 MiraDesde, Vector3 MiraHasta, float Fov, float Enfoque = 0);

	private static readonly Toma[] Tomas =
	{
		// 0 · Negro: solo se oye la cinta (la cámara ya espera en la toma 1)
		new(0.0, new(0.62f, 1.36f, -1.42f), new(0.62f, 1.36f, -1.42f), new(0.4f, 1.31f, -1.88f), new(0.4f, 1.31f, -1.88f), 34, 0.48f),
		// 1 · Inserto de la grabadora a ras de mesa; Porky dormido, desenfocado al fondo
		new(TImagen, new(0.66f, 1.37f, -1.38f), new(0.6f, 1.35f, -1.46f), new(0.4f, 1.31f, -1.88f), new(0.38f, 1.31f, -1.9f), 34, 0.48f),
		// 2 · General picado desde la esquina de la puerta: Porky duerme, la rejilla queda fuera de cuadro
		new(8.4, new(3.3f, 3.05f, 2.3f), new(3.1f, 2.95f, 2.0f), new(-0.4f, 1.1f, -2.7f), new(-0.6f, 1.1f, -2.5f), 55),
		// 3 · Contrapicado bajo la rejilla: la figura baja recortada contra la luz que entra por ella
		new(13.2, new(-1.35f, 0.8f, -0.05f), new(-1.4f, 0.78f, 0.0f), new(-2.2f, 3.2f, 1.0f), new(-2.2f, 1.7f, 1.0f), 60),
		// 4 · Medio del susurro, del mismo lado que la toma 2 (frente de la sala): figura, micrófono y Porky atrás
		new(17.6, new(1.1f, 1.7f, -0.3f), new(0.95f, 1.55f, -0.55f), new(0.75f, 1.4f, -1.7f), new(0.7f, 1.38f, -1.8f), 46, 1.3f),
		// 5 · Medio lateral desde la izquierda: la figura descuelga el reloj y lo suelta
		new(23.2, new(-2.3f, 1.5f, -3.1f), new(-2.15f, 1.5f, -3.35f), new(0.3f, 2.05f, -4.5f), new(0.0f, 1.55f, -4.6f), 42),
		// 6 · Inserto cenital de la carátula en el piso: 3:12
		new(26.9, new(-0.12f, 1.05f, -4.4f), new(-0.13f, 0.98f, -4.42f), new(-0.2f, 0.52f, -4.55f), new(-0.2f, 0.52f, -4.55f), 40, 0.5f),
		// 7 · General desde el fondo derecho: la figura vuelve a la rejilla y sube
		new(29.4, new(3.3f, 2.4f, -4.3f), new(3.2f, 2.35f, -4.1f), new(-1.6f, 1.9f, 0.6f), new(-2.1f, 2.6f, 0.95f), 40),
		// 8 · Medio frontal de Porky (desde el lado del detective): despierta y mira alrededor
		new(32.2, new(0.2f, 1.5f, -0.75f), new(0.15f, 1.52f, -0.9f), new(0.0f, 1.7f, -2.7f), new(0.0f, 1.8f, -2.6f), 46),
		// 9 · Medio lateral derecho: recoge el reloj del piso y lo cuelga
		new(36.0, new(-2.3f, 1.2f, -4.45f), new(-2.2f, 1.4f, -4.5f), new(0.1f, 1.0f, -4.2f), new(0.0f, 1.9f, -4.7f), 50),
		// 10 · Inserto POV de Porky: la luz roja de REC
		new(41.2, new(0.52f, 1.48f, -2.0f), new(0.5f, 1.46f, -1.98f), new(0.44f, 1.33f, -1.86f), new(0.44f, 1.33f, -1.86f), 30, 0.55f),
		// 11 · Contraplano: Porky se sobresalta
		new(42.4, new(0.05f, 1.55f, -1.35f), new(0.1f, 1.55f, -1.45f), new(0.55f, 1.8f, -2.95f), new(0.55f, 1.82f, -2.95f), 44),
		// 12 · Inserto de la grabadora desde el frente: STOP, rebobinado, el contador a 03:12, sale la cinta
		new(44.4, new(0.47f, 1.62f, -1.35f), new(0.46f, 1.6f, -1.38f), new(0.41f, 1.3f, -1.88f), new(0.41f, 1.3f, -1.88f), 24, 0.65f),
		// 13 · General con la puerta de frente: Porky se va con la cinta
		new(49.2, new(-1.6f, 2.2f, -1.4f), new(-1.45f, 2.15f, -1.15f), new(1.6f, 1.3f, 1.4f), new(2.3f, 1.4f, 2.6f), 42),
		// 14 · Inserto de las botas cruzando hacia el pasillo
		new(52.8, new(1.75f, 0.6f, 1.35f), new(1.8f, 0.6f, 1.45f), new(2.5f, 0.5f, 2.6f), new(2.5f, 0.5f, 3.3f), 46),
		// 15 · General: la puerta se cierra
		new(54.0, new(-1.45f, 2.15f, -1.15f), new(-1.4f, 2.12f, -1.05f), new(2.3f, 1.4f, 2.6f), new(2.4f, 1.4f, 2.8f), 42),
	};
	private static readonly double[] Paradas = Tomas.Select(t => t.Inicio).Where(t => t > 0).Append(TFin).ToArray();

	// Textos: la hora arriba, la cinta abajo
	private static readonly (double T, string Texto)[] Horas = { (3.6, "SALA 1 · 03:09"), (TRejilla, "SALA 1 · 03:12"), (TDespierta - 0.4, "SALA 1 · 03:15") };
	// Cada subtítulo empieza con su sonido y dura al menos lo que pide leerlo a 15 caracteres por segundo
	private static readonly (double Inicio, double Fin, int Linea)[] Subtitulos =
	{
		(0.7, 4.3, 0),
		(TRejilla, TRejilla + 2.8, 1),
		(TSusurro, TSusurro + 2.7, 2),
	};

	// ---------- Movimiento de los personajes ----------

	// Dónde está y hacia dónde mira (grados; 0 = hacia +Z) en el segundo T; Caer acelera el tramo (caída libre)
	private record struct Punto(double T, Vector3 Pos, float Giro, bool Caer = false);
	// Qué animación hace desde el segundo T; Congelar >= 0 la deja fija en esa fracción
	private record struct Paso(double T, string Animacion, bool Bucle = true, double Congelar = -1, double Velocidad = 1);

	private static readonly Vector3 Silla = new(0f, 0.5f, -2.8f);
	private static readonly Vector3 BajoRejilla = new(-2.2f, 0.5f, 1.0f);
	private static readonly Vector3 RelojPiso = new(-0.2f, 0.52f, -4.55f);

	private static readonly Punto[] RutaPorky =
	{
		new(0, Silla, 0), new(TDespierta, Silla, 0), new(TDespierta + 1.3, Silla + new Vector3(0, 0, 0.18f), 0),
		// Mira alrededor y ve el reloj en el piso, detrás de él
		new(34.3, Silla + new Vector3(0, 0, 0.18f), 35), new(34.9, Silla + new Vector3(0, 0, 0.18f), -30), new(35.7, Silla + new Vector3(0.05f, 0, 0.18f), -160),
		// Rodea la silla y se para frente al reloj
		new(36.4, new(0.6f, 0.5f, -3.05f), 175), new(37.2, new(-0.12f, 0.5f, -4.05f), 180),
		// Lo recoge, da un paso a la pared y lo cuelga
		new(39.6, new(-0.12f, 0.5f, -4.05f), 180), new(39.95, new(-0.05f, 0.5f, -4.48f), 180),
		new(40.5, new(-0.05f, 0.5f, -4.48f), 180),
		// Vuelve a la mesa, junto a la grabadora
		new(40.9, new(0.3f, 0.5f, -4.1f), 60), new(41.4, new(0.7f, 0.5f, -3.3f), 20), new(41.9, new(0.55f, 0.5f, -2.95f), 0),
		// Para, rebobina y saca la cinta (del lado de la silla, frente a la grabadora)
		new(48.9, new(0.55f, 0.5f, -2.95f), 0),
		// Se va al vestidor con la cinta
		new(49.4, new(1.25f, 0.5f, -2.75f), 70), new(49.9, new(1.45f, 0.5f, -1.4f), 25), new(50.6, new(2.05f, 0.5f, 0.6f), 20), new(51.7, new(2.5f, 0.5f, 2.15f), 0),
		new(52.2, new(2.5f, 0.5f, 2.2f), 0), new(53.9, new(2.5f, 0.5f, 4.1f), 0), new(55.6, new(2.5f, 0.5f, 6.0f), 0),
	};

	private static readonly Paso[] AnimPorky =
	{
		new(0, "Sitting_Idle"), new(TDespierta, "Sitting_Exit", false, Velocidad: 0.8), new(TDespierta + 1.3, "Idle"),
		new(35.7, "Walk"), new(37.2, "Farm_Harvest", false), new(39.6, "Walk"), new(39.95, "Interact", false), new(40.5, "Walk"),
		new(41.9, "Idle"), new(TSobresalto, "Hit_Chest", false, Velocidad: 0.4), new(43.6, "Idle"),
		new(44.35, "Interact", false), new(46.4, "Idle"), new(47.9, "Interact", false), new(48.9, "Walk"), new(51.7, "Idle"),
		new(52.2, "Walk"),
	};

	private static readonly Punto[] RutaFigura =
	{
		// Arriba del techo, fuera de la vista: baja por la rejilla colgada de las manos y se suelta
		new(0, BajoRejilla + new Vector3(0, 3.5f, 0), 180), new(13.5, BajoRejilla + new Vector3(0, 3.15f, 0), 180),
		new(15.4, BajoRejilla + new Vector3(0, 0.82f, 0), 180), new(15.8, BajoRejilla, 180, true),
		// Camina hasta la mesa y se agacha junto al micrófono
		new(17.0, BajoRejilla, 150), new(19.6, new(1.12f, 0.5f, -1.62f), -90),
		new(23.0, new(1.12f, 0.5f, -1.62f), -90),
		// Va a la pared del reloj
		new(24.0, new(1.05f, 0.5f, -3.3f), 200), new(24.9, new(0.32f, 0.5f, -4.32f), 180),
		new(26.9, new(0.32f, 0.5f, -4.32f), 180), new(27.3, new(0.32f, 0.5f, -4.32f), 20),
		// De vuelta a la rejilla, salta, se agarra y sube
		new(29.9, BajoRejilla, -20), new(30.0, BajoRejilla, 0), new(30.4, BajoRejilla + new Vector3(0, 1.0f, 0), 0),
		new(TSeVa, BajoRejilla + new Vector3(0, 3.4f, 0), 0), new(TFin, BajoRejilla + new Vector3(0, 3.5f, 0), 0),
	};

	private static readonly Paso[] AnimFigura =
	{
		new(0, "Jump_Loop"), new(15.8, "NinjaJump_Land", false), new(17.0, "Walk"), new(19.6, "Crouch_Idle"),
		new(23.0, "Walk"), new(24.9, "Interact", false), new(26.9, "Idle"), new(27.3, "Jog_Fwd"),
		new(29.9, "NinjaJump_Start", false), new(30.4, "ClimbUp_1m_RM", false, Velocidad: 0.8),
	};

	// ---------- Estado ----------

	private Actores _a;
	private Node3D _sala;
	private double _t;
	private bool _terminada, _saltando;
	private double _relojSalto, _mantener, _relojFinal, _relojPista;
	private readonly List<(double T, Action Accion)> _eventos = new();
	private int _siguienteEvento;

	private Camera3D _camara, _camaraAntes;
	private CameraAttributesPractical _enfoque;
	private Node3D _figura;
	private AnimadorPersonaje _animFigura, _animPorky;
	private Node3D _cintaEnGrabadora, _cintaEnMano;
	private bool _cintaVisibleAntes;
	private Vector3 _cintaPosAntes;
	private Label3D _contador;
	private string _contadorAntes;
	private Node3D _luzRecMalla;
	private OmniLight3D _luzRec;
	private SpotLight3D _luzRejilla;
	private OmniLight3D _rebote, _luzPasillo;
	private Node3D _rejilla, _pluma, _puerta, _grietas, _minutero, _horario;
	private MeshInstance3D _hueco;
	private Transform3D _rejillaAntes, _puertaAntes, _minuteroAntes, _horarioAntes;
	private bool _plumaVisible, _grietasVisible;
	private Light3D _focoReloj;
	private bool _focoRelojVisible;
	private bool _ayudasAntes = true;
	private Node3D _pizarron;
	private bool _pizarronVisible;
	private Label3D _suenoPorky;
	private bool _suenoVisible;
	private Transform3D _porkyAntes, _relojAntes;
	private bool _erizVisible, _jugadorVisible;
	private float _intensidadMusica;
	private readonly List<CanvasLayer> _capasOcultas = new();
	private readonly Dictionary<string, float> _volumenBuses = new();

	private Control _interfaz;
	private ColorRect _negro;
	private Label _hora, _subtitulo, _pista;
	private CirculoSalto _circulo;
	private AudioStreamPlayer _tictac, _zumbido, _ronquido, _siseo, _voz;
	private double _relojPaso;
	private Vector3 _pasoPorky, _pasoFigura;
	private float _escala = 1f;

	private const string BusCinta = "Cinta";
	private const string Cinta = "res://Assets/Audio/Cinematica/Cinta/";

	public override void _Ready()
	{
		Layer = 110;
		ProcessMode = ProcessModeEnum.Always;
		_escala = GetViewport().GetVisibleRect().Size.Y / 1080f;
		Preparar();
		CrearInterfaz();
		CrearAudio();
		CrearEventos();
		Actualizar(true);
	}

	// ---------- Montaje y desmontaje ----------

	private void Preparar()
	{
		_camaraAntes = GetViewport().GetCamera3D();
		_enfoque = new CameraAttributesPractical { DofBlurAmount = 0.06f, DofBlurFarTransition = 0.5f };
		_camara = new Camera3D { Near = 0.02f, Attributes = _enfoque };
		_sala.AddChild(_camara);
		_camara.MakeCurrent();

		// Todo lo que se dibuja encima del juego (HUD, diálogo, viñeta, guía) se esconde mientras dura
		foreach (Node n in _sala.FindChildren("*", "CanvasLayer", true, false))
			if (n is CanvasLayer capa && capa != this && capa.Visible) { capa.Visible = false; _capasOcultas.Add(capa); }

		_jugadorVisible = _a.Jugador.Visible;
		_a.Jugador.Visible = false;
		if (_a.Eriz != null) { _erizVisible = _a.Eriz.Visible; _a.Eriz.Visible = false; }

		_porkyAntes = _a.Porky.GlobalTransform;
		_animPorky = _a.Porky.Animador;
		_animPorky?.Controlar(true);
		_suenoPorky = _a.Porky.GetNodeOrNull<Label3D>("Sueno");
		if (_suenoPorky != null) _suenoVisible = _suenoPorky.Visible;

		// El reloj: entero (sin grietas) y con las manecillas avanzando hasta que cae
		_relojAntes = _a.Reloj.GlobalTransform;
		_grietas = _a.Reloj.GetNodeOrNull<Node3D>("Modelo/Grietas");
		if (_grietas != null) _grietasVisible = _grietas.Visible;
		_minutero = _a.Reloj.FindChild("wall_clock_minute_hand", true, false) as Node3D;
		_horario = _a.Reloj.FindChild("wall_clock_hours_hand", true, false) as Node3D;
		if (_minutero != null) _minuteroAntes = _minutero.Transform;
		if (_horario != null) _horarioAntes = _horario.Transform;

		// La grabadora: la cinta adentro, la luz de REC y el contador
		_cintaEnGrabadora = _a.Grabadora.FindChild("cassette_player_tape", true, false) as Node3D;
		if (_cintaEnGrabadora != null)
		{
			_cintaVisibleAntes = _cintaEnGrabadora.Visible;
			_cintaPosAntes = _cintaEnGrabadora.Position;
			_cintaEnMano = (Node3D)_cintaEnGrabadora.Duplicate();
			_cintaEnMano.Visible = false;
			_sala.AddChild(_cintaEnMano);
		}
		_contador = _a.Grabadora.FindChild("Contador", true, false) as Label3D;
		if (_contador != null) _contadorAntes = _contador.Text;
		_luzRecMalla = _a.Grabadora.FindChild("LuzRec", true, false) as Node3D;
		_luzRec = new OmniLight3D { LightColor = new Color(1f, 0.1f, 0.06f), OmniRange = 0.14f, OmniAttenuation = 2f, LightEnergy = 0f };
		_sala.AddChild(_luzRec);
		_luzRec.GlobalPosition = (_luzRecMalla ?? _a.Grabadora).GlobalPosition + new Vector3(0, 0.03f, 0);

		// La luz de la escena es la lámpara del techo (encendida, como en el juego). El foco del reloj no:
		// en el recuerdo parecía una luna detrás de la figura y dejaba una mancha en la pared al caer.
		_focoReloj = _sala.GetNodeOrNull<Light3D>("FocoReloj");
		if (_focoReloj != null) { _focoRelojVisible = _focoReloj.Visible; _focoReloj.Visible = false; }

		// La rejilla del techo: se abre como trampilla; por el hueco entra luz fría del ducto
		_rejilla = _sala.GetNodeOrNull<Node3D>("Detalles/RejillaVentilacion");
		if (_rejilla != null) _rejillaAntes = _rejilla.GlobalTransform;
		_hueco = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.48f, 0.48f), Orientation = PlaneMesh.OrientationEnum.Y },
			// El ducto, iluminado desde arriba: la figura se recorta contra él
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.42f, 0.5f, 0.66f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, CullMode = BaseMaterial3D.CullModeEnum.Disabled },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Visible = false,
		};
		_sala.AddChild(_hueco);
		_hueco.GlobalPosition = BajoRejilla + new Vector3(0, 2.997f, 0);
		_luzRejilla = new SpotLight3D
		{
			LightColor = new Color(0.62f, 0.72f, 1f), LightEnergy = 0f, SpotRange = 4.2f, SpotAngle = 32f, SpotAttenuation = 0.8f,
			ShadowEnabled = true, LightVolumetricFogEnergy = 0f,
		};
		_sala.AddChild(_luzRejilla);
		_luzRejilla.GlobalTransform = new Transform3D(new Basis(Vector3.Right, -Mathf.Pi / 2), BajoRejilla + new Vector3(0, 2.93f, 0));
		// Rebote de la lámpara: tenue y cálido, para que la pared del reloj y la puerta no queden en negro
		_rebote = new OmniLight3D { LightColor = new Color(1f, 0.86f, 0.66f), LightEnergy = 0.6f, OmniRange = 6f, OmniAttenuation = 1.2f, ShadowEnabled = false, LightSpecular = 0.1f };
		_sala.AddChild(_rebote);
		_rebote.GlobalPosition = new Vector3(0.4f, 2.9f, -3.3f);
		// Luz del pasillo: entra cuando Porky abre la puerta
		_luzPasillo = new OmniLight3D { LightColor = new Color(0.95f, 0.85f, 0.7f), LightEnergy = 0f, OmniRange = 5f, ShadowEnabled = true };
		_sala.AddChild(_luzPasillo);
		_luzPasillo.GlobalPosition = new Vector3(2.5f, 2.3f, 4.3f);

		// La pluma negra (solo se ve con linterna en el juego): queda enganchada cuando la figura se va
		_pluma = _sala.GetNodeOrNull<Node3D>("PlumaSecreta/Marcas");
		if (_pluma != null) _plumaVisible = _pluma.Visible;

		// La puerta de la sala: cerrada de madrugada; Porky la abre al irse
		_puerta = _sala.GetNodeOrNull<Node3D>("Detalles/Puerta");
		if (_puerta != null) _puertaAntes = _puerta.GlobalTransform;

		// El pizarrón es de la investigación de Caneloso: a las 3:12 todavía no estaba
		_pizarron = _sala.GetNodeOrNull<Node3D>("PizarronInvestigacion");
		if (_pizarron != null) { _pizarronVisible = _pizarron.Visible; _pizarron.Visible = false; }

		if (_a.Musica != null) _intensidadMusica = _a.Musica.Intensidad;

		_figura = CrearFigura();

		// Los contornos de ayuda de las pistas no van en un recuerdo
		if (Configuracion.Instancia != null)
		{
			_ayudasAntes = Configuracion.Instancia.AyudasVisuales;
			Configuracion.Instancia.AyudasVisuales = false;
		}
	}

	// La figura: el maniquí de Quaternius con gabardina y capucha, negro, sin rostro y con borde de luz
	private Node3D CrearFigura()
	{
		var figura = new Node3D { Name = "FiguraCinta", Visible = false };
		_sala.AddChild(figura);
		var maniqui = GD.Load<PackedScene>(AnimadorPersonaje.RutaUal1).Instantiate<Node3D>();
		maniqui.Name = "Modelo";
		maniqui.Scale = Vector3.One * 1.15f;
		figura.AddChild(maniqui);
		var tela = new StandardMaterial3D { AlbedoColor = new Color(0.018f, 0.018f, 0.022f), Roughness = 0.95f, RimEnabled = true, Rim = 1f, RimTint = 0.6f };
		foreach (Node n in maniqui.FindChildren("*", "MeshInstance3D", true, false))
			if (n is MeshInstance3D malla) malla.MaterialOverride = tela;

		var esqueleto = maniqui.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().FirstOrDefault();
		if (esqueleto != null)
		{
			var sombra = new StandardMaterial3D { AlbedoColor = new Color(0.003f, 0.003f, 0.004f), Roughness = 1f, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
			// Capucha: cubre la cabeza; el hueco de la cara es negro puro
			Ropa(esqueleto, "DEF-head", new SphereMesh { Radius = 0.15f, Height = 0.34f }, tela, new Vector3(0, 0.09f, -0.01f), new Vector3(1.05f, 1.05f, 1.1f));
			Ropa(esqueleto, "DEF-head", new SphereMesh { Radius = 0.1f, Height = 0.2f }, sombra, new Vector3(0, 0.07f, 0.075f), new Vector3(1f, 1.1f, 0.6f));
			// Gabardina: de los hombros a las rodillas
			Ropa(esqueleto, "DEF-spine.003", new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.27f, Height = 0.55f, RadialSegments = 16 }, tela, new Vector3(0, -0.12f, 0), new Vector3(1.05f, 1f, 0.8f));
			Ropa(esqueleto, "DEF-hips", new CylinderMesh { TopRadius = 0.25f, BottomRadius = 0.33f, Height = 0.62f, RadialSegments = 16 }, tela, new Vector3(0, -0.22f, 0), new Vector3(1f, 1f, 0.85f));
		}

		_animFigura = new AnimadorPersonaje { Modelo = maniqui, MirarJugador = false };
		figura.AddChild(_animFigura);
		_animFigura.Controlar(true);
		return figura;
	}

	private static void Ropa(Skeleton3D esqueleto, string hueso, Mesh malla, Material material, Vector3 pos, Vector3 escala)
	{
		if (esqueleto.FindBone(hueso) < 0) return;
		var union = new BoneAttachment3D { BoneName = hueso };
		esqueleto.AddChild(union);
		union.AddChild(new MeshInstance3D { Mesh = malla, MaterialOverride = material, Position = pos, Scale = escala });
	}

	private void Restaurar()
	{
		_a.Porky.GlobalTransform = _porkyAntes;
		if (_animPorky != null) { _animPorky.InclinarCabeza = 0; _animPorky.Controlar(false); }
		if (_suenoPorky != null) _suenoPorky.Visible = _suenoVisible;
		// El reloj, colgado y parado en las 3:12, con el cristal rajado
		_a.Reloj.GlobalTransform = _relojAntes;
		if (_grietas != null) _grietas.Visible = _grietasVisible;
		if (_minutero != null) _minutero.Transform = _minuteroAntes;
		if (_horario != null) _horario.Transform = _horarioAntes;
		// La grabadora: sin cinta, en REC y con el contador en 03:12
		if (_cintaEnGrabadora != null) { _cintaEnGrabadora.Visible = _cintaVisibleAntes; _cintaEnGrabadora.Position = _cintaPosAntes; }
		if (_contador != null) _contador.Text = _contadorAntes;
		if (_luzRecMalla != null) _luzRecMalla.Visible = true;
		if (_rejilla != null) _rejilla.GlobalTransform = _rejillaAntes;
		if (_pluma != null) _pluma.Visible = _plumaVisible;
		if (_puerta != null) _puerta.GlobalTransform = _puertaAntes;
		if (_focoReloj != null) _focoReloj.Visible = _focoRelojVisible;
		if (_pizarron != null) _pizarron.Visible = _pizarronVisible;
		_a.Jugador.Visible = _jugadorVisible;
		if (_a.Eriz != null) _a.Eriz.Visible = _erizVisible;
		foreach (CanvasLayer capa in _capasOcultas) if (IsInstanceValid(capa)) capa.Visible = true;
		if (IsInstanceValid(_camaraAntes)) _camaraAntes.MakeCurrent();
		RestaurarAudio();
		foreach (Node n in new Node[] { _figura, _cintaEnMano, _luzRec, _luzRejilla, _rebote, _luzPasillo, _hueco, _camara })
			if (n != null && IsInstanceValid(n)) n.QueueFree();
		if (Configuracion.Instancia != null) Configuracion.Instancia.AyudasVisuales = _ayudasAntes;
		EnCurso = false;
	}

	public override void _ExitTree()
	{
		// Si la sala se cierra a media cinemática, que no se quede bloqueado el juego
		if (!_terminada) EnCurso = false;
	}

	// ---------- Tiempo ----------

	public override void _Process(double delta)
	{
		if (_terminada)
		{
			_relojFinal += delta;
			_negro.Color = new Color(0, 0, 0, 1 - (float)Mathf.Clamp(_relojFinal / 0.8, 0, 1));
			if (_relojFinal > 0.8)
			{
				EmitSignal(SignalName.Terminada);
				QueueFree();
			}
			return;
		}

		ProcesarSalto(delta);
		_relojPista += delta;
		if (_saltando)
		{
			double a = Mathf.Clamp((_relojSalto += delta) / 0.35, 0, 1);
			_negro.Color = new Color(0, 0, 0, Mathf.Max(_negro.Color.A, (float)a));
			if (a >= 1) Terminar();
			return;
		}

		if (Manual) { Actualizar(true); return; }
		_t += delta;
		Actualizar(false);
		Pasos(delta);
		if (_t >= TFin) Terminar();
	}

	// Pone todo como va en el segundo _t. Los sonidos de un solo golpe solo suenan al pasar por ellos
	// jugando; los continuos (siseo, tic-tac, ronquido, zumbido) se ajustan siempre a _t.
	private void Actualizar(bool silencio)
	{
		while (_siguienteEvento < _eventos.Count && _eventos[_siguienteEvento].T <= _t)
		{
			if (!silencio) _eventos[_siguienteEvento].Accion();
			_siguienteEvento++;
		}
		Personajes();
		Objetos();
		Camara();
		Interfaz();
		SonidosContinuos();
	}

	private void Terminar()
	{
		if (_terminada) return;
		_terminada = true;
		_relojFinal = 0;
		_negro.Color = Colors.Black;
		_subtitulo.Visible = _hora.Visible = _circulo.Visible = _pista.Visible = false;
		foreach (var reproductor in new[] { _tictac, _zumbido, _ronquido, _siseo, _voz }) reproductor?.Stop();
		Restaurar();
	}

	// ---------- Personajes y objetos ----------

	private static (Vector3 Pos, float Giro) EnRuta(Punto[] ruta, double t)
	{
		if (t <= ruta[0].T) return (ruta[0].Pos, ruta[0].Giro);
		for (int i = 1; i < ruta.Length; i++)
		{
			if (t > ruta[i].T) continue;
			var a = ruta[i - 1];
			var b = ruta[i];
			float u = (float)((t - a.T) / Math.Max(1e-6, b.T - a.T));
			float avance = b.Caer ? u * u : u;
			return (a.Pos.Lerp(b.Pos, avance), Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(a.Giro), Mathf.DegToRad(b.Giro), Mathf.SmoothStep(0, 1, u))));
		}
		return (ruta[^1].Pos, ruta[^1].Giro);
	}

	private static Paso? PasoEn(Paso[] pasos, double t)
	{
		Paso? actual = null;
		foreach (Paso p in pasos) if (p.T <= t) actual = p;
		return actual;
	}

	private static void Animar(AnimadorPersonaje animador, Paso[] pasos, double t)
	{
		if (animador == null || PasoEn(pasos, t) is not Paso p) return;
		double local = p.Congelar >= 0 ? p.Congelar * animador.Duracion(p.Animacion) : (t - p.T) * p.Velocidad;
		animador.Fijar(p.Animacion, local, p.Bucle);
	}

	private static float Rampa(double t, double desde, double hasta) => (float)Mathf.Clamp((t - desde) / (hasta - desde), 0, 1);

	private void Personajes()
	{
		var (pos, giro) = EnRuta(RutaPorky, _t);
		_a.Porky.GlobalTransform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(giro)), pos);
		Animar(_animPorky, AnimPorky, _t);
		if (_animPorky != null)
		{
			// Dormido cabecea; con el golpe del reloj se medio despierta y vuelve a dormirse;
			// despierto mira el reloj en el piso y luego la grabadora
			float dormido = 30f + 4f * Mathf.Sin((float)_t * 1.9f);
			float golpe = (float)Mathf.Clamp(1 - Math.Abs(_t - (TGolpe + 0.5)) / 1.0, 0, 1);
			float cabeza;
			if (_t < TDespierta) cabeza = Mathf.Lerp(dormido, 12f, golpe);
			else if (_t < 35.2) cabeza = Mathf.Lerp(20f, -12f, Rampa(_t, TDespierta, TDespierta + 1.2));
			else if (_t < 39.6) cabeza = 22f;
			else if (_t < 41.0) cabeza = -8f;
			else if (_t < 44.3) cabeza = -12f;
			else if (_t < 48.9) cabeza = 16f;
			else cabeza = 0f;
			_animPorky.InclinarCabeza = cabeza;
		}
		if (_suenoPorky != null) _suenoPorky.Visible = _t < TDespierta;

		// La figura existe solo mientras está bajo el techo (antes y después está arriba, en el ducto)
		var (fp, fg) = EnRuta(RutaFigura, _t);
		_figura.GlobalTransform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(fg)), fp);
		_figura.Visible = _t > 13.3 && _t < TSeVa + 0.2;
		Animar(_animFigura, AnimFigura, _t);
		// Agachada junto a la grabadora, acerca la cabeza al micrófono
		_animFigura.InclinarCabeza = 24f * Rampa(_t, 19.7, 20.1) * (1 - Rampa(_t, 22.7, 23.0));
	}

	private void Objetos()
	{
		Reloj();

		// La cinta: en la grabadora hasta que Porky la saca; luego en su mano
		bool enMano = _t >= TTomaCinta && _animPorky != null;
		if (_cintaEnGrabadora != null)
		{
			// Al expulsarla sube un poco antes de que Porky la tome
			_cintaEnGrabadora.Visible = !enMano;
			_cintaEnGrabadora.Position = _cintaPosAntes + new Vector3(0, 0, 0.012f * Rampa(_t, TExpulsa, TExpulsa + 0.25f));
		}
		if (_cintaEnMano != null)
		{
			_cintaEnMano.Visible = enMano;
			if (enMano)
			{
				Transform3D mano = _animPorky.Mano(false);
				_cintaEnMano.GlobalTransform = new Transform3D(_a.Porky.GlobalBasis.Orthonormalized() * new Basis(Vector3.Right, 0.4f), mano.Origin + _a.Porky.GlobalBasis.Z.Normalized() * 0.03f);
			}
		}

		// REC parpadea todo el tiempo: es la trampa del comisario y nadie la apagó (a las 6:02 sigue en REC).
		// El STOP solo detiene la cinta.
		bool encendida = Mathf.PosMod((float)_t, 1f) < 0.6f;
		_luzRec.LightEnergy = encendida ? 0.12f : 0.02f;
		if (_luzRecMalla != null) _luzRecMalla.Visible = encendida;

		// Contador de la cinta: avanza mientras graba; el STOP lo detiene y el rebobinado lo regresa a 03:12
		if (_contador != null)
		{
			double minuto;
			if (_t < TStop) minuto = Mathf.Lerp(9.0, 15.0, Mathf.Clamp((_t - 4.0) / (TStop - 4.0), 0, 1));
			else minuto = Mathf.Lerp(15.0, 12.0, Rampa(_t, TRebobina, TFinRebobinado));
			int m = (int)minuto;
			_contador.Text = $"03:{m:00}";
		}

		// La rejilla: se abre con el ruido metálico y se cierra cuando la figura sube
		float abierta = Rampa(_t, TRejilla, TRejilla + 0.35) * (1 - Rampa(_t, TCierraRejilla - 0.25, TCierraRejilla));
		if (_rejilla != null)
		{
			// Bisagra en el borde -X de la rejilla
			Vector3 bisagra = _rejillaAntes.Origin + new Vector3(-0.25f, 0, 0);
			var giro = new Basis(Vector3.Back, -1.75f * abierta);
			_rejilla.GlobalTransform = new Transform3D(giro * _rejillaAntes.Basis, bisagra + giro * (_rejillaAntes.Origin - bisagra));
		}
		_hueco.Visible = abierta > 0.01f;
		_luzRejilla.LightEnergy = 9f * abierta;
		if (_pluma != null) _pluma.Visible = _t >= TCierraRejilla;

		// La puerta: cerrada; Porky la abre, sale y se cierra detrás de él
		if (_puerta != null)
		{
			float a = Rampa(_t, TAbrePuerta, TAbrePuerta + 0.6) * (1 - Rampa(_t, TCierraPuerta, TCierraPuerta + 0.55));
			var bisagra = new Vector3(2.97f, _puertaAntes.Origin.Y, 3.0f);
			var cerrada = new Vector3(2.47f, _puertaAntes.Origin.Y, 2.975f);
			var giro = new Basis(Vector3.Up, Mathf.DegToRad(-88f * a));
			_puerta.GlobalTransform = new Transform3D(giro, bisagra + giro * (cerrada - bisagra));
			_luzPasillo.LightEnergy = 2.2f * a;
		}
	}

	// El reloj: en la pared; la figura lo descuelga y lo suelta (cae, gira y rebota); en el piso marca las 3:12;
	// Porky lo recoge y lo vuelve a colgar (por eso a las 6:02 está colgado y parado)
	private void Reloj()
	{
		Transform3D pared = _relojAntes;
		var piso = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(-25f)) * new Basis(Vector3.Right, Mathf.DegToRad(-90f)), RelojPiso);
		const double tDescuelga = 25.35;
		Vector3 descolgado = pared.Origin + new Vector3(0.02f, 0.04f, 0.2f);

		if (_t < tDescuelga || _t >= TCuelga) _a.Reloj.GlobalTransform = pared;
		else if (_t < TSuelta)
		{
			// La figura lo levanta del clavo y lo separa de la pared
			float u = Mathf.SmoothStep(0, 1, Rampa(_t, tDescuelga, TSuelta));
			_a.Reloj.GlobalTransform = new Transform3D(pared.Basis * new Basis(Vector3.Right, -0.15f * u), pared.Origin.Lerp(descolgado, u));
		}
		else if (_t < TGolpe)
		{
			// Caída libre (0.65 s) inclinándose y girando
			float u = Rampa(_t, TSuelta, TGolpe);
			Vector3 p = descolgado.Lerp(RelojPiso, u);
			p.Y = Mathf.Lerp(descolgado.Y, RelojPiso.Y, u * u);
			Quaternion q = pared.Basis.GetRotationQuaternion().Slerp(piso.Basis.GetRotationQuaternion(), Mathf.Pow(u, 1.4f));
			_a.Reloj.GlobalTransform = new Transform3D(new Basis(q) * new Basis(Vector3.Forward, 1.4f * u), p);
		}
		else if (_t < TRebote + 0.15)
		{
			// Rebote corto y se asienta
			float u = Rampa(_t, TGolpe, TRebote);
			float alto = 0.07f * 4 * u * (1 - u);
			float vaiven = 0.25f * (1 - Rampa(_t, TGolpe, TRebote + 0.15)) * Mathf.Sin((float)(_t - TGolpe) * 30f);
			_a.Reloj.GlobalTransform = new Transform3D(piso.Basis * new Basis(Vector3.Right, vaiven), RelojPiso + Vector3.Up * alto);
		}
		else if (_t < TAgarra || _animPorky == null) _a.Reloj.GlobalTransform = piso;
		else
		{
			// En las manos de Porky, con la carátula hacia la pared; al final lo acerca al clavo
			Transform3D mano = _animPorky.Mano(false);
			Basis porky = _a.Porky.GlobalBasis.Orthonormalized();
			var enMano = new Transform3D(porky * new Basis(Vector3.Up, Mathf.Pi), mano.Origin + porky.Z * 0.1f);
			float u = Mathf.SmoothStep(0, 1, Rampa(_t, TCuelga - 0.5, TCuelga));
			_a.Reloj.GlobalTransform = enMano.InterpolateWith(pared, u);
		}

		// Cristal entero hasta el golpe
		if (_grietas != null) _grietas.Visible = _t >= TGolpe;
		// Las manecillas avanzan de las 3:09 a las 3:12 y se paran con el golpe
		double minuto = Mathf.Lerp(9.0, 12.0, Mathf.Clamp((_t - TImagen) / (TGolpe - TImagen), 0, 1));
		float atraso = (float)(12.0 - minuto);
		if (_minutero != null) _minutero.Transform = _minuteroAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(6f * atraso)) * _minuteroAntes.Basis };
		if (_horario != null) _horario.Transform = _horarioAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(0.5f * atraso)) * _horarioAntes.Basis };
	}

	// ---------- Cámara ----------

	private void Camara()
	{
		int i = Array.FindLastIndex(Tomas, toma => toma.Inicio <= _t);
		Toma toma = Tomas[Math.Max(0, i)];
		double fin = i + 1 < Tomas.Length ? Tomas[i + 1].Inicio : TFin;
		float u = Mathf.SmoothStep(0, 1, (float)((_t - toma.Inicio) / (fin - toma.Inicio)));
		Vector3 mira = toma.MiraDesde.Lerp(toma.MiraHasta, u);
		// La toma 13 sigue un poco a Porky hacia la puerta
		if (i == 13) mira = mira.Lerp(_a.Porky.GlobalPosition + Vector3.Up * 0.9f, 0.35f);
		// Cámara en mano, muy suave; con el golpe del reloj tiembla un instante. Depende de _t (repetible).
		float t = (float)_t;
		Vector3 balanceo = new Vector3(Mathf.Sin(t * 0.7f), Mathf.Sin(t * 0.53f + 1.7f), 0) * 0.005f;
		float golpe = _t >= TGolpe ? (float)Mathf.Clamp(1 - (_t - TGolpe) / 0.45, 0, 1) : 0f;
		Vector3 sacudida = new Vector3(Mathf.Sin(t * 63f), Mathf.Sin(t * 51f + 1f), 0) * 0.03f * golpe;
		_camara.GlobalPosition = toma.Desde.Lerp(toma.Hasta, u) + balanceo + sacudida;
		_camara.LookAt(mira, Vector3.Up);
		_camara.Fov = toma.Fov;
		_enfoque.DofBlurFarEnabled = toma.Enfoque > 0;
		if (toma.Enfoque > 0) _enfoque.DofBlurFarDistance = toma.Enfoque;
	}

	// ---------- Entrada ----------

	public override void _Input(InputEvent evento)
	{
		if (_terminada) return;
		bool tecla = evento is InputEventKey k && k.Pressed && !k.Echo;
		bool boton = evento is InputEventMouseButton r && r.Pressed || evento is InputEventJoypadButton j && j.Pressed;
		// Cualquier tecla vuelve a mostrar cómo saltar
		if (tecla || boton) _relojPista = 0;
		bool avanzar =
			(evento is InputEventMouseButton raton && raton.Pressed && raton.ButtonIndex == MouseButton.Left) ||
			(evento is InputEventKey enter && enter.Pressed && !enter.Echo && (enter.Keycode == Key.Enter || enter.Keycode == Key.KpEnter)) ||
			(evento is InputEventJoypadButton a && a.Pressed && a.ButtonIndex == JoyButton.A);
		if (avanzar && !_saltando) Avanzar();
		// Se queda con toda la entrada: el juego de abajo no debe enterarse
		if (evento is InputEventKey || evento is InputEventMouseButton || evento is InputEventJoypadButton)
			GetViewport().SetInputAsHandled();
	}

	private static bool TeclaSaltar() =>
		Input.IsKeyPressed(Key.Space) || Input.IsKeyPressed(Key.Escape) ||
		Input.IsJoyButtonPressed(0, JoyButton.B) || Input.IsJoyButtonPressed(0, JoyButton.Start);

	private void ProcesarSalto(double delta)
	{
		if (_saltando) return;
		bool mantiene = TeclaSaltar();
		if (mantiene) _relojPista = 0;
		_mantener = mantiene ? _mantener + delta : Mathf.Max(0, _mantener - delta * 3);
		_circulo.Progreso = (float)(_mantener / SegundosParaSaltar);
		_circulo.QueueRedraw();
		if (_mantener >= SegundosParaSaltar)
		{
			_saltando = true;
			_relojSalto = 0;
		}
	}

	// Clic o Enter: pasa a la siguiente toma
	private void Avanzar()
	{
		_t = Paradas.FirstOrDefault(p => p > _t + 0.05, TFin);
		if (_t >= TFin) { Terminar(); return; }
		Actualizar(true);
	}

	// ---------- Interfaz ----------

	private Font Fuente(string archivo, int espaciado = 0)
	{
		Font fuente = GD.Load<FontFile>("res://Assets/Fuentes/" + archivo);
		return espaciado == 0 ? fuente : new FontVariation { BaseFont = fuente, SpacingGlyph = espaciado };
	}

	private Label NuevaEtiqueta(Font fuente, float tam, Color color, HorizontalAlignment alineacion)
	{
		var etiqueta = new Label
		{
			HorizontalAlignment = alineacion,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		etiqueta.AddThemeFontOverride("font", fuente);
		etiqueta.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(tam));
		etiqueta.AddThemeColorOverride("font_color", color);
		etiqueta.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_interfaz.AddChild(etiqueta);
		return etiqueta;
	}

	private static void Anclar(Control control, float izq, float arriba, float der, float abajo)
	{
		control.AnchorLeft = izq;
		control.AnchorTop = arriba;
		control.AnchorRight = der;
		control.AnchorBottom = abajo;
		control.OffsetLeft = control.OffsetTop = control.OffsetRight = control.OffsetBottom = 0;
	}

	private ColorRect Capa(Shader shader)
	{
		var capa = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = new ShaderMaterial { Shader = shader } };
		_interfaz.AddChild(capa);
		Anclar(capa, 0, 0, 1, 1);
		return capa;
	}

	private void CrearInterfaz()
	{
		float e = _escala;
		_interfaz = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(_interfaz);
		Anclar(_interfaz, 0, 0, 1, 1);

		// Recuerdo: frío y deslavado (salvo el rojo de REC), con viñeta y grano de película
		Capa(GD.Load<Shader>("res://Escenas/Cinematicas/recuerdo.gdshader"));
		Capa(GD.Load<Shader>("res://Escenas/Cinematicas/vineta.gdshader"));
		var grano = Capa(GD.Load<Shader>("res://Escenas/Menus/Fondo/grano_pelicula.gdshader"));
		((ShaderMaterial)grano.Material).SetShaderParameter("intensidad", 0.1f);

		// Franjas de cine
		var arriba = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		var abajo = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(arriba);
		_interfaz.AddChild(abajo);
		Anclar(arriba, 0, 0, 1, 0.11f);
		Anclar(abajo, 0, 0.89f, 1, 1);

		_negro = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_negro);
		Anclar(_negro, 0, 0, 1, 1);

		// Las horas: a máquina, arriba a la izquierda, en el papel del HUD
		_hora = NuevaEtiqueta(Fuente("SpecialElite-Regular.ttf", 4), 30 * e, new Color(0.86f, 0.84f, 0.78f), HorizontalAlignment.Left);
		Anclar(_hora, 0.05f, 0, 0.6f, 0.11f);
		// Lo que viene de la cinta: abajo, en ámbar como el HUD y con la marca ▶ de reproducción
		_subtitulo = NuevaEtiqueta(Fuente("CourierPrime-Regular.ttf"), 30 * e, new Color("#EFCA08"), HorizontalAlignment.Center);
		Anclar(_subtitulo, 0.1f, 0.89f, 0.9f, 1);

		// Mantener para saltar: arriba a la derecha, en la franja negra; se desvanece a los 3 s
		_circulo = new CirculoSalto { MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_circulo);
		Anclar(_circulo, 1, 0, 1, 0);
		_circulo.OffsetLeft = -86 * e; _circulo.OffsetTop = 33 * e; _circulo.OffsetRight = -34 * e; _circulo.OffsetBottom = 85 * e;
		bool mando = Configuracion.Instancia?.UsandoMando ?? false;
		_pista = NuevaEtiqueta(Fuente("CourierPrime-Regular.ttf", 2), 22 * e, new Color(0.72f, 0.7f, 0.66f), HorizontalAlignment.Right);
		_pista.Text = mando ? "Mantén [B] para saltar" : "Mantén [Espacio] para saltar";
		Anclar(_pista, 1, 0, 1, 0);
		_pista.OffsetLeft = -700 * e; _pista.OffsetTop = 41 * e; _pista.OffsetRight = -100 * e; _pista.OffsetBottom = 77 * e;
	}

	private int SubtituloActual()
	{
		for (int i = 0; i < Subtitulos.Length; i++)
			if (_t >= Subtitulos[i].Inicio && _t < Subtitulos[i].Fin) return i;
		return -1;
	}

	private void Interfaz()
	{
		// Negro con la cinta sonando; fundido a la sala; fundido a negro al final
		_negro.Color = new Color(0, 0, 0, Mathf.Max(1 - Rampa(_t, TImagen, TImagen + 1.0), Rampa(_t, TFin - 1.4, TFin)));

		// La hora, a máquina
		var hora = Horas.LastOrDefault(h => h.T <= _t);
		_hora.Visible = hora.Texto != null;
		if (hora.Texto != null)
		{
			_hora.Text = hora.Texto;
			_hora.VisibleCharacters = Mathf.Min(hora.Texto.Length, (int)((_t - hora.T) * 18));
		}

		// Subtítulo de la cinta: aparece entero con su sonido y se desvanece
		int s = SubtituloActual();
		_subtitulo.Visible = s >= 0 && _a.Lineas != null && Subtitulos[s].Linea < _a.Lineas.Length;
		if (_subtitulo.Visible)
		{
			var (inicio, fin, linea) = Subtitulos[s];
			_subtitulo.Text = "▶ " + _a.Lineas[linea];
			_subtitulo.Modulate = new Color(1, 1, 1, Rampa(_t, inicio, inicio + 0.15) * (1 - Rampa(_t, fin - 0.35, fin)));
		}

		// La pista de saltar: 3 s al empezar y 3 s después de tocar una tecla (o mientras se mantiene)
		float pista = _mantener > 0 ? 1f : 1f - Rampa(_relojPista, 2.4, 3.0);
		_pista.Modulate = _circulo.Modulate = new Color(1, 1, 1, pista);
	}

	// ---------- Audio ----------

	// Todo lo de la cinta pasa por el bus Cinta: banda estrecha, un poco de saturación y wow/flutter
	private void CrearBusCinta()
	{
		if (AudioServer.GetBusIndex(BusCinta) >= 0) return;
		AudioServer.AddBus();
		int bus = AudioServer.BusCount - 1;
		AudioServer.SetBusName(bus, BusCinta);
		AudioServer.SetBusSend(bus, "Master");
		AudioServer.SetBusVolumeDb(bus, 3f);
		AudioServer.AddBusEffect(bus, new AudioEffectHighPassFilter { CutoffHz = 160f });
		AudioServer.AddBusEffect(bus, new AudioEffectLowPassFilter { CutoffHz = 4800f });
		AudioServer.AddBusEffect(bus, new AudioEffectDistortion { Mode = AudioEffectDistortion.ModeEnum.Overdrive, Drive = 0.18f, PreGain = 0f, PostGain = -1.5f, KeepHfHz = 4000f });
		// Wow: una sola voz del coro que oscila lento y reemplaza a la señal seca
		var wow = new AudioEffectChorus { VoiceCount = 1, Dry = 0f, Wet = 1f };
		wow.SetVoiceRateHz(0, 0.55f);
		wow.SetVoiceDepthMs(0, 1.6f);
		wow.SetVoiceDelayMs(0, 6f);
		wow.SetVoiceLevelDb(0, 0f);
		wow.SetVoiceCutoffHz(0, 8000f);
		AudioServer.AddBusEffect(bus, wow);
	}

	private void Atenuar(string bus, float db, double segundos)
	{
		int i = AudioServer.GetBusIndex(bus);
		if (i < 0) return;
		_volumenBuses.TryAdd(bus, AudioServer.GetBusVolumeDb(i));
		float desde = AudioServer.GetBusVolumeDb(i);
		CreateTween().SetPauseMode(Tween.TweenPauseMode.Process)
			.TweenMethod(Callable.From<float>(v => AudioServer.SetBusVolumeDb(i, v)), desde, db, segundos);
	}

	private void RestaurarAudio()
	{
		// La música y el ambiente del juego regresan con un fundido; la cinemática ya no existe, así que el
		// tween va en la sala
		foreach (var (bus, db) in _volumenBuses)
		{
			int i = AudioServer.GetBusIndex(bus);
			if (i < 0) continue;
			float desde = AudioServer.GetBusVolumeDb(i);
			_sala.CreateTween().TweenMethod(Callable.From<float>(v => AudioServer.SetBusVolumeDb(i, v)), desde, db, 2.5);
		}
		if (_a.Musica != null) _a.Musica.Intensidad = _intensidadMusica;
		int cinta = AudioServer.GetBusIndex(BusCinta);
		if (cinta >= 0) AudioServer.RemoveBus(cinta);
	}

	private AudioStreamPlayer NuevoAudio(string ruta, float volumen, bool bucle = false)
	{
		AudioStream stream = ruta != null && ResourceLoader.Exists(ruta) ? GD.Load<AudioStream>(ruta) : null;
		if (stream is AudioStreamWav wav && bucle) { wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward; wav.LoopEnd = (int)(wav.GetLength() * wav.MixRate); }
		if (stream is AudioStreamOggVorbis ogg && bucle) ogg.Loop = true;
		var reproductor = new AudioStreamPlayer { Stream = stream, Bus = BusCinta, VolumeDb = volumen };
		AddChild(reproductor);
		return reproductor;
	}

	private void Sonar(string ruta, float volumen = 0f, string bus = BusCinta)
	{
		if (!ResourceLoader.Exists(ruta)) return;
		var reproductor = new AudioStreamPlayer { Stream = GD.Load<AudioStream>(ruta), Bus = bus, VolumeDb = volumen };
		AddChild(reproductor);
		reproductor.Finished += reproductor.QueueFree;
		reproductor.Play();
	}

	private void CrearAudio()
	{
		CrearBusCinta();
		// Lo que sonaba en el juego se aparta: aquí todo viene de la cinta
		Atenuar(Sonidos.BusMusica, -60f, 1.2);
		Atenuar(Sonidos.BusAmbiente, -60f, 0.8);
		_siseo = NuevoAudio(Cinta + "siseo_cinta.wav", -3f, true);
		_zumbido = NuevoAudio("res://Assets/Audio/Ambiente/zumbido_sala.ogg", -11f, true);
		_tictac = NuevoAudio("res://Assets/Audio/Ambiente/reloj_pared.wav", -5f, true);
		_ronquido = NuevoAudio(Cinta + "ronquido.wav", -7f, true);
		// La voz de Tito: si alguien la graba, basta con dejarla en Cinta/voz_tito.ogg
		_voz = NuevoAudio(Cinta + "voz_tito.ogg", -4f);
	}

	private void CrearEventos()
	{
		const string maquina = "res://Assets/Audio/Cinematica/";
		_eventos.AddRange(new (double, Action)[]
		{
			(TPlay, () => Sonar(Cinta + "cinta_play.wav", -6f)),
			(0.7, () => { if (_voz.Stream != null) _voz.Play(); }),
			// Las horas se escriben a máquina (no vienen de la cinta: van por la interfaz)
			(Horas[0].T, () => Sonar(maquina + "maquina_2.wav", -14f, Sonidos.BusInterfaz)),
			(Horas[1].T, () => Sonar(maquina + "maquina_4.wav", -14f, Sonidos.BusInterfaz)),
			(Horas[2].T, () => Sonar(maquina + "maquina_5.wav", -14f, Sonidos.BusInterfaz)),
			(TRejilla, () => Sonar(Cinta + "rejilla_metal.wav", -3f)),
			(15.8, () => Sonar(Cinta + "reloj_rebote.wav", -6f)),
			(TSusurro, () => Sonar(Cinta + "susurro.wav", -2.5f)),
			(TSuelta - 0.4, () => Sonar(Cinta + "reloj_rebote.wav", -16f)),
			// El golpe del reloj es el pico más fuerte de la cinemática
			(TGolpe, () => Sonar(Cinta + "reloj_golpe.wav", 5f)),
			(TRebote, () => Sonar(Cinta + "reloj_rebote.wav", -8f)),
			(29.95, () => Sonar(Cinta + "reloj_rebote.wav", -12f)),
			(TCierraRejilla, () => Sonar(Cinta + "rejilla_metal.wav", -10f)),
			(TCuelga, () => Sonar(Cinta + "reloj_rebote.wav", -10f)),
			(TSobresalto, () => Sonar(Cinta + "sobresalto.wav", -2f)),
			(TStop, () => Sonar(Cinta + "cinta_stop.wav", -3f)),
			(TRebobina, () => Sonar(Cinta + "cinta_rebobinar.wav", -3f)),
			(TExpulsa, () => Sonar(Cinta + "cinta_expulsar.wav", -2f)),
			(TAbrePuerta, () => Sonar(Cinta + "puerta_abrir.wav", -4f)),
			(TCierraPuerta, () => Sonar(Cinta + "puerta_cerrar.wav", -4f)),
		});
		_eventos.Sort((a, b) => a.T.CompareTo(b.T));
	}

	// Los sonidos continuos dependen solo de _t: así quedan bien aunque se adelante o se salte
	private void SonidosContinuos()
	{
		bool jugando = !Manual && !_terminada;
		Mantener(_siseo, jugando && _t >= TPlay + 0.1);
		Mantener(_zumbido, jugando && _t >= TImagen - 0.5);
		// El tic-tac se corta en seco con el golpe
		Mantener(_tictac, jugando && _t >= TImagen - 0.5 && _t < TGolpe);
		// Ronca mientras duerme; el golpe lo calla un momento y vuelve a dormirse
		Mantener(_ronquido, jugando && _t >= TImagen - 0.5 && _t < TDespierta && (_t < TGolpe - 0.05 || _t > TGolpe + 2.4));
		// La voz de Tito solo suena sobre negro
		if (_voz.Playing && _t >= TImagen) _voz.Stop();
	}

	private static void Mantener(AudioStreamPlayer reproductor, bool sonando)
	{
		if (reproductor?.Stream == null) return;
		if (sonando && !reproductor.Playing) reproductor.Play();
		else if (!sonando && reproductor.Playing) reproductor.Stop();
	}

	// Pasos: las botas de Porky (bajas, son de la cinta) y los de la figura, casi mudos
	private void Pasos(double delta)
	{
		if ((_relojPaso -= delta) > 0) return;
		_relojPaso = 0.4;
		Vector3 porky = _a.Porky.GlobalPosition;
		if (porky.DistanceTo(_pasoPorky) > 0.2f && PasoEn(AnimPorky, _t) is Paso p && p.Animacion == "Walk")
			Sonar($"res://Assets/Audio/Efectos/paso_{GD.RandRange(1, 4)}.wav", -15f);
		_pasoPorky = porky;
		Vector3 figura = _figura.GlobalPosition;
		if (_figura.Visible && figura.DistanceTo(_pasoFigura) > 0.2f && figura.Y < 0.6f)
			Sonar($"res://Assets/Audio/Efectos/paso_{GD.RandRange(1, 4)}.wav", -27f);
		_pasoFigura = figura;
	}

	// Círculo que se llena mientras se mantiene la tecla de saltar
	private partial class CirculoSalto : Control
	{
		public float Progreso;

		public override void _Draw()
		{
			var centro = Size / 2;
			float radio = Mathf.Min(Size.X, Size.Y) / 2 - 3;
			DrawArc(centro, radio, 0, Mathf.Tau, 48, new Color(1, 1, 1, 0.22f), 3, true);
			if (Progreso > 0)
				DrawArc(centro, radio, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * Mathf.Clamp(Progreso, 0, 1), 48, new Color("#EFCA08"), 4, true);
			DrawString(ThemeDB.FallbackFont, centro + new Vector2(-5, 6), "»", HorizontalAlignment.Left, -1, 18, new Color(1, 1, 1, 0.75f));
		}
	}
}
