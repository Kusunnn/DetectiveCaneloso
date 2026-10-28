using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Cinemática final del Caso 0: se reproduce la cinta y se ve lo que grabó en la Sala 1 de madrugada.
// Primero, sobre negro, la confesión de Tito (es una grabación, no algo que pase en la sala).
// 03:09 Porky duerme sobre la mesa y la grabadora graba (la trampa del comisario). 03:12 la rejilla del techo
// vibra y se abre; una figura sin rostro se descuelga, le susurra al micrófono, descuelga el reloj a propósito
// y lo deja caer (por eso se paró a las 3:12); se va trepando por su cuerda y la pluma negra queda enganchada en
// la tapa mal puesta. 03:15 Porky despierta, arrastra su silla hasta la pared, se sube y cuelga el reloj; regresa
// la silla, ve la luz de REC, para la grabadora, rebobina hasta 03:12, saca la cinta y se va al vestidor
// (sus botas son las huellas del pasillo). No se ve el rostro de la figura.
//
// La animación está hecha en Blender (Assets/Cinematicas/Blender, ver construir_cinta.py) y llega en
// cinta_caso0.glb: Porky, la figura, la cámara y vacíos con el recorrido del reloj, la tapa de la rejilla, la
// puerta, la silla y la cinta. Aquí un AnimationPlayer se pone en el segundo t cada cuadro y los objetos reales
// de la sala copian a sus vacíos. Todo depende de t (también luces y sonidos continuos), así que IrA, adelantar
// con clic o Enter y Movie Maker dan siempre lo mismo. Mantener [Espacio], [Esc] o B/Start la salta.
// Todo lo que se oye viene de la cinta: va por un bus con filtro de cinta. Al terminar deja la sala como estaba.
// Con UsarMomentosCongelados (por defecto) se ve la versión de momentos congelados (CinematicaCintaMomentos.cs).
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

	public const string Animacion = "res://Assets/Cinematicas/Caso0/cinta_caso0.glb";

	// Mientras dura no debe aparecer ninguna ayuda, pensamiento ni aviso encima (ver Jugador y Caso0)
	public static bool EnCurso { get; private set; }

	// Para las capturas: el tiempo no avanza solo, se fija con IrA
	public bool Manual { get; set; }

	public void IrA(double t)
	{
		_t = t;
		Actualizar(true);
	}

	// Id en el registro de cinemáticas de la partida (GestorPartida)
	public const string Id = "caso0_cinta";

	public static async Task Reproducir(Node3D sala, Actores actores)
	{
		// No se puede disparar dos veces a la vez
		if (EnCurso) return;
		EnCurso = true;
		GestorPartida.Instancia?.RegistrarCinematica(Id);
		var cinematica = new CinematicaCinta { _a = actores, _sala = sala };
		sala.AddChild(cinematica);
		await cinematica.ToSignal(cinematica, SignalName.Terminada);
	}

	// ---------- Tiempos (segundos; los mismos de coreografia.py) ----------

	private const double TPlay = 0.25;           // clic de PLAY sobre negro
	private const double TImagen = 4.2;          // termina el negro: SALA 1 · 03:09
	private const double TRejilla = 12.0;        // la rejilla vibra y suena
	private const double TTapa = 12.6;           // la tapa se desliza hacia adentro
	private const double TAterriza = 15.75;
	private const double TSusurro = 20.1;
	private const double TDescuelga = 24.95;
	private const double TGolpe = 26.4;          // el reloj golpea el piso (Rigid Body): se para a las 3:12
	private const double TRebote = 26.53;
	private const double TSeVa = 32.0;
	private const double TCierraTapa = 32.5;
	private const double TDespierta = 33.6;      // 03:15
	private const double TEmpuja = 37.8;
	private const double TSillaPared = 39.3;
	private const double TSubeSilla = 41.0;
	private const double TCuelga = 42.3;         // el reloj queda colgado otra vez
	private const double TBajaSilla = 42.6;
	private const double TJala = 43.4;
	private const double TSillaMesa = 44.9;
	private const double TSobresalto = 46.45;
	private const double TStop = 48.0;
	private const double TRebobina = 48.4;
	private const double TFinRebobinado = 50.9;
	private const double TExpulsa = 51.1;
	private const double TAbrePuerta = 55.0;
	private const double TCierraPuerta = 57.7;
	private const double TBotas = 56.3;
	private const double TFin = 59.7;
	private const double SegundosParaSaltar = 1.2;

	// Inicio de cada toma (la cámara viene del .glb) y distancia de enfoque para el desenfoque (0 = sin él)
	private static readonly (double Inicio, float Enfoque)[] Tomas =
	{
		(0.0, 0.48f), (TImagen, 0.48f), (8.4, 0), (11.6, 0), (17.2, 0), (23.0, 0), (27.45, 0.5f), (29.6, 0),
		(33.3, 0), (37.1, 0), (39.45, 0), (40.7, 0), (42.6, 0), (45.75, 0.6f), (46.3, 0), (47.6, 0.6f), (51.9, 0),
		(TBotas, 0), (57.5, 0),
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

	// ---------- Estado ----------

	private Actores _a;
	private Node3D _sala;
	private double _t;
	private bool _terminada, _saltando;
	private double _relojSalto, _mantener, _relojFinal, _relojPista;
	private readonly List<(double T, Action Accion)> _eventos = new();
	private int _siguienteEvento;

	private Node3D _escena;
	private AnimationPlayer _anim;
	private Camera3D _camara, _camaraAntes;
	private CameraAttributesPractical _enfoque;
	private Node3D _porky, _figura;
	private Node3D _animReloj, _animRejilla, _animPuerta, _animSilla, _animCinta;
	private Node3D _cinta, _silla, _rejilla, _pluma, _puerta, _grietas, _minutero, _horario, _huellas;
	private bool _cintaVisibleAntes, _plumaVisible, _grietasVisible, _huellasVisibles, _porkyVisible;
	private Transform3D _cintaAntes, _sillaAntes, _rejillaAntes, _puertaAntes, _relojAntes, _minuteroAntes, _horarioAntes;
	private Label3D _contador;
	private string _contadorAntes;
	private Node3D _luzRecMalla;
	private OmniLight3D _luzRec, _rebote, _luzPasillo;
	private SpotLight3D _luzRejilla;
	private MeshInstance3D _hueco;
	private Light3D _focoReloj;
	private bool _focoRelojVisible;
	private bool _ayudasAntes = true;
	private Node3D _pizarron;
	private bool _pizarronVisible;
	private bool _erizVisible, _jugadorVisible;
	private float _intensidadMusica;
	private readonly List<CanvasLayer> _capasOcultas = new();
	private readonly Dictionary<string, float> _volumenBuses = new();

	private Control _interfaz;
	private ColorRect _negro;
	private Label _hora, _subtitulo, _pista;
	private CirculoSalto _circulo;
	private AudioStreamPlayer _tictac, _zumbido, _ronquido, _siseo, _voz;
	private double _relojPaso, _relojPasoFigura;
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

	private T Guardar<T>(string ruta) where T : Node => _sala.GetNodeOrNull<T>(ruta);

	private void Preparar()
	{
		_camaraAntes = GetViewport().GetCamera3D();

		// La animación de Blender: personajes, cámara y vacíos de los objetos
		_momentos = Version == VersionCinta.Momentos;
		_pelicula = Version == VersionCinta.Pelicula;
		_escena = GD.Load<PackedScene>(_pelicula ? AnimacionPelicula : _momentos ? AnimacionMomentos : Animacion).Instantiate<Node3D>();
		_escena.Name = "CinematicaCintaBlender";
		_sala.AddChild(_escena);
		_anim = _escena.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().First();
		_anim.Play(_momentos || _pelicula ? "Momentos" : "Cinta");
		_anim.Pause();
		_porky = _escena.GetNode<Node3D>("Porky");
		_figura = _escena.GetNode<Node3D>("Figura");
		_animReloj = _escena.GetNode<Node3D>("Anim_Reloj");
		_animRejilla = _escena.GetNode<Node3D>("Anim_Rejilla");
		_animPuerta = _escena.GetNode<Node3D>("Anim_Puerta");
		_animSilla = _escena.GetNode<Node3D>("Anim_Silla");
		_animCinta = _escena.GetNode<Node3D>("Anim_Cinta");
		if (_pelicula) { _camara = new Camera3D { Name = "Camara" }; _escena.AddChild(_camara); }
		else if (_momentos) PrepararMomentos();
		else _camara = _escena.GetNode<Camera3D>("Camara");
		_enfoque = new CameraAttributesPractical { DofBlurAmount = 0.06f, DofBlurFarTransition = 0.5f };
		_camara.Near = 0.02f;
		_camara.Attributes = _enfoque;
		_camara.MakeCurrent();
		VestirFigura();

		// Todo lo que se dibuja encima del juego (HUD, diálogo, viñeta, guía) se esconde mientras dura
		foreach (Node n in _sala.FindChildren("*", "CanvasLayer", true, false))
			if (n is CanvasLayer capa && capa != this && capa.Visible) { capa.Visible = false; _capasOcultas.Add(capa); }

		_jugadorVisible = _a.Jugador.Visible;
		_a.Jugador.Visible = false;
		if (_a.Eriz != null) { _erizVisible = _a.Eriz.Visible; _a.Eriz.Visible = false; }
		// El Porky del juego se esconde: el de la cinemática es el del .glb
		_porkyVisible = _a.Porky.Visible;
		_a.Porky.Visible = false;

		// El reloj: entero (sin grietas) y con las manecillas avanzando hasta que cae
		_relojAntes = _a.Reloj.GlobalTransform;
		_grietas = _a.Reloj.GetNodeOrNull<Node3D>("Modelo/Grietas");
		if (_grietas != null) _grietasVisible = _grietas.Visible;
		_minutero = _a.Reloj.FindChild("wall_clock_minute_hand", true, false) as Node3D;
		_horario = _a.Reloj.FindChild("wall_clock_hours_hand", true, false) as Node3D;
		if (_minutero != null) _minuteroAntes = _minutero.Transform;
		if (_horario != null) _horarioAntes = _horario.Transform;

		// La grabadora: la cinta adentro, la luz de REC y el contador
		_cinta = _a.Grabadora.FindChild("cassette_player_tape", true, false) as Node3D;
		if (_cinta != null) { _cintaVisibleAntes = _cinta.Visible; _cintaAntes = _cinta.GlobalTransform; }
		_contador = _a.Grabadora.FindChild("Contador", true, false) as Label3D;
		if (_contador != null) _contadorAntes = _contador.Text;
		_luzRecMalla = _a.Grabadora.FindChild("LuzRec", true, false) as Node3D;
		_luzRec = new OmniLight3D { LightColor = new Color(1f, 0.1f, 0.06f), OmniRange = 0.07f, OmniAttenuation = 2f, LightEnergy = 0f };
		_sala.AddChild(_luzRec);
		_luzRec.GlobalPosition = (_luzRecMalla ?? _a.Grabadora).GlobalPosition + new Vector3(0, 0.03f, 0);

		// La silla de Porky, la tapa de la rejilla, la puerta y la pluma
		_silla = Guardar<Node3D>("SillaSospechoso");
		if (_silla != null) _sillaAntes = _silla.GlobalTransform;
		_rejilla = Guardar<Node3D>("Detalles/RejillaVentilacion");
		if (_rejilla != null) _rejillaAntes = _rejilla.GlobalTransform;
		_puerta = Guardar<Node3D>("Detalles/Puerta");
		if (_puerta != null) _puertaAntes = _puerta.GlobalTransform;
		_pluma = Guardar<Node3D>("PlumaSecreta/Marcas");
		if (_pluma != null) _plumaVisible = _pluma.Visible;
		_huellas = Guardar<Node3D>("ZonaPasillo/Huellas/Marcas");
		if (_huellas != null) _huellasVisibles = _huellas.Visible;

		// La luz de la escena es la lámpara del techo (encendida, como en el juego), con un rebote cálido muy bajo.
		// El foco del reloj no: en el recuerdo parecía una luna detrás de la figura.
		_focoReloj = Guardar<Light3D>("FocoReloj");
		if (_focoReloj != null) { _focoRelojVisible = _focoReloj.Visible; _focoReloj.Visible = false; }
		_rebote = new OmniLight3D { LightColor = new Color(1f, 0.86f, 0.66f), LightEnergy = 1.2f, OmniRange = 6f, OmniAttenuation = 1.2f, ShadowEnabled = false, LightSpecular = 0.1f };
		_sala.AddChild(_rebote);
		_rebote.GlobalPosition = new Vector3(0.3f, 2.6f, -3.8f);
		// Al abrirse la rejilla entra luz fría del ducto y la figura se recorta contra ella
		var rejilla = new Vector3(-2.2f, 3.5f, 1.0f);
		_hueco = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(0.48f, 0.48f), Orientation = PlaneMesh.OrientationEnum.Y },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.42f, 0.5f, 0.66f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, CullMode = BaseMaterial3D.CullModeEnum.Disabled },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Visible = false,
		};
		_sala.AddChild(_hueco);
		_hueco.GlobalPosition = rejilla - new Vector3(0, 0.003f, 0);
		_luzRejilla = new SpotLight3D { LightColor = new Color(0.62f, 0.72f, 1f), LightEnergy = 0f, SpotRange = 4.2f, SpotAngle = 32f, SpotAttenuation = 0.8f, ShadowEnabled = true };
		_sala.AddChild(_luzRejilla);
		_luzRejilla.GlobalTransform = new Transform3D(new Basis(Vector3.Right, -Mathf.Pi / 2), rejilla - new Vector3(0, 0.07f, 0));
		// Al abrir la puerta entra la luz del pasillo
		_luzPasillo = new OmniLight3D { LightColor = new Color(0.95f, 0.85f, 0.7f), LightEnergy = 0f, OmniRange = 5f, ShadowEnabled = true };
		_sala.AddChild(_luzPasillo);
		_luzPasillo.GlobalPosition = new Vector3(2.5f, 2.3f, 4.3f);

		// El pizarrón es de la investigación de Caneloso: a las 3:12 todavía no estaba
		_pizarron = Guardar<Node3D>("PizarronInvestigacion");
		if (_pizarron != null) { _pizarronVisible = _pizarron.Visible; _pizarron.Visible = false; }

		if (_a.Musica != null) _intensidadMusica = _a.Musica.Intensidad;

		if (_pelicula) PrepararPelicula();

		// Los contornos de ayuda de las pistas no van en un recuerdo
		if (Configuracion.Instancia != null)
		{
			_ayudasAntes = Configuracion.Instancia.AyudasVisuales;
			Configuracion.Instancia.AyudasVisuales = false;
		}
	}

	// La figura: tela negra con borde de luz (se separa del fondo) y el hueco de la cara en negro puro
	private void VestirFigura()
	{
		var tela = new StandardMaterial3D { AlbedoColor = new Color(0.018f, 0.018f, 0.022f), Roughness = 0.95f, RimEnabled = true, Rim = 1f, RimTint = 0.6f };
		var vacio = new StandardMaterial3D { AlbedoColor = Colors.Black, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
		foreach (Node n in _figura.FindChildren("*", "MeshInstance3D", true, false))
			if (n is MeshInstance3D malla) malla.MaterialOverride = malla.Name.ToString().Contains("Rostro") ? vacio : tela;
	}

	private void Restaurar()
	{
		_a.Porky.Visible = _porkyVisible;
		// El reloj, colgado y parado en las 3:12, con el cristal rajado
		_a.Reloj.GlobalTransform = _relojAntes;
		if (_grietas != null) _grietas.Visible = _grietasVisible;
		if (_minutero != null) _minutero.Transform = _minuteroAntes;
		if (_horario != null) _horario.Transform = _horarioAntes;
		// La grabadora: sin cinta, en REC y con el contador en 03:12
		if (_cinta != null) { _cinta.GlobalTransform = _cintaAntes; _cinta.Visible = _cintaVisibleAntes; }
		if (_contador != null) _contador.Text = _contadorAntes;
		if (_luzRecMalla != null) _luzRecMalla.Visible = true;
		if (_silla != null) _silla.GlobalTransform = _sillaAntes;
		if (_rejilla != null) _rejilla.GlobalTransform = _rejillaAntes;
		if (_puerta != null) _puerta.GlobalTransform = _puertaAntes;
		if (_pluma != null) _pluma.Visible = _plumaVisible;
		if (_huellas != null) _huellas.Visible = _huellasVisibles;
		if (_focoReloj != null) _focoReloj.Visible = _focoRelojVisible;
		if (_pizarron != null) _pizarron.Visible = _pizarronVisible;
		_a.Jugador.Visible = _jugadorVisible;
		if (_a.Eriz != null) _a.Eriz.Visible = _erizVisible;
		foreach (CanvasLayer capa in _capasOcultas) if (IsInstanceValid(capa)) capa.Visible = true;
		if (IsInstanceValid(_camaraAntes)) _camaraAntes.MakeCurrent();
		RestaurarAudio();
		foreach (Node n in new Node[] { _escena, _luzRec, _luzRejilla, _rebote, _luzPasillo, _hueco })
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
		if (!_momentos && !_pelicula) Pasos(delta);
		if (_t >= FinActual) Terminar();
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
		if (_pelicula) ActualizarPelicula();
		else if (_momentos) ActualizarMomentos();
		else
		{
			_anim.Seek(Math.Min(_t, _anim.CurrentAnimationLength), true);
			Objetos();
			Camara();
		}
		Interfaz();
		if (_pelicula) SonidosPelicula();
		else if (_momentos) SonidosMomentos();
		else SonidosContinuos();
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

	// ---------- Objetos ----------

	private static float Rampa(double t, double desde, double hasta) => (float)Mathf.Clamp((t - desde) / (hasta - desde), 0, 1);

	private void Objetos()
	{
		// Los objetos reales siguen a sus vacíos animados en Blender
		_a.Reloj.GlobalTransform = _animReloj.GlobalTransform;
		if (_silla != null) _silla.GlobalTransform = _animSilla.GlobalTransform;
		if (_rejilla != null) _rejilla.GlobalTransform = _animRejilla.GlobalTransform;
		if (_puerta != null) _puerta.GlobalTransform = _animPuerta.GlobalTransform;
		if (_cinta != null) { _cinta.Visible = true; _cinta.GlobalTransform = _animCinta.GlobalTransform; }
		_figura.Visible = _t > 13.3 && _t < TSeVa + 0.2;

		// Cristal entero hasta el golpe; las manecillas avanzan de las 3:09 a las 3:12 y se paran con el golpe
		if (_grietas != null) _grietas.Visible = _t >= TGolpe;
		double minuto = Mathf.Lerp(9.0, 12.0, Mathf.Clamp((_t - TImagen) / (TGolpe - TImagen), 0, 1));
		float atraso = (float)(12.0 - minuto);
		if (_minutero != null) _minutero.Transform = _minuteroAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(6f * atraso)) * _minuteroAntes.Basis };
		if (_horario != null) _horario.Transform = _horarioAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(0.5f * atraso)) * _horarioAntes.Basis };

		// REC parpadea todo el tiempo: es la trampa del comisario y nadie la apagó (a las 6:02 sigue en REC).
		// El STOP solo detiene la cinta.
		bool encendida = Mathf.PosMod((float)_t, 1f) < 0.6f;
		_luzRec.LightEnergy = encendida ? 0.12f : 0.02f;
		if (_luzRecMalla != null) _luzRecMalla.Visible = encendida;

		// Contador de la cinta: avanza mientras graba; el STOP lo detiene y el rebobinado lo regresa a 03:12
		if (_contador != null)
		{
			double m = _t < TStop ? Mathf.Lerp(9.0, 15.0, Mathf.Clamp((_t - 4.0) / (TStop - 4.0), 0, 1)) : Mathf.Lerp(15.0, 12.0, Rampa(_t, TRebobina, TFinRebobinado));
			_contador.Text = $"03:{(int)m:00}";
		}

		// El ducto abierto (con su luz) mientras la tapa está corrida
		float abierta = Rampa(_t, TTapa + 0.15, TTapa + 0.45) * (1 - Rampa(_t, TCierraTapa - 0.25, TCierraTapa));
		_hueco.Visible = abierta > 0.01f;
		_luzRejilla.LightEnergy = 9f * abierta;
		if (_pluma != null) _pluma.Visible = _t >= TCierraTapa;
		// Las huellas de las botas de Porky, hacia el vestidor
		if (_huellas != null) _huellas.Visible = _t >= TBotas - 0.3 || _huellasVisibles;
		// La luz del pasillo entra con la puerta abierta
		float puerta = Rampa(_t, TAbrePuerta + 0.15, TAbrePuerta + 0.75) * (1 - Rampa(_t, TCierraPuerta, TCierraPuerta + 0.5));
		_luzPasillo.LightEnergy = 2.2f * puerta;
	}

	// ---------- Cámara ----------

	private void Camara()
	{
		// La cámara (posición, giro, lente y el temblor del golpe) viene de Blender; aquí solo el desenfoque
		int i = Array.FindLastIndex(Tomas, toma => toma.Inicio <= _t);
		float enfoque = Tomas[Math.Max(0, i)].Enfoque;
		_enfoque.DofBlurFarEnabled = enfoque > 0;
		if (enfoque > 0) _enfoque.DofBlurFarDistance = enfoque;
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
		_t = ParadasActuales.FirstOrDefault(p => p > _t + 0.05, FinActual);
		if (_t >= FinActual) { Terminar(); return; }
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
		_recuerdo = (ShaderMaterial)Capa(GD.Load<Shader>("res://Escenas/Cinematicas/recuerdo.gdshader")).Material;
		_vineta = (ShaderMaterial)Capa(GD.Load<Shader>("res://Escenas/Cinematicas/vineta.gdshader")).Material;
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
		if (_momentos) CrearDestello();
		if (_pelicula) CrearInterfazPelicula();

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
		var subtitulos = SubtitulosActuales;
		for (int i = 0; i < subtitulos.Length; i++)
			if (_t >= subtitulos[i].Inicio && _t < subtitulos[i].Fin) return i;
		return -1;
	}

	private void Interfaz()
	{
		// Negro con la cinta sonando; fundido a la sala; fundido a negro al final
		if (_pelicula) InterfazPelicula();
		else if (_momentos) InterfazMomentos();
		else _negro.Color = new Color(0, 0, 0, Mathf.Max(1 - Rampa(_t, TImagen, TImagen + 1.0), Rampa(_t, TFin - 1.4, TFin)));

		// La hora, a máquina
		var hora = HorasActuales.LastOrDefault(h => h.T <= _t);
		_hora.Visible = hora.Texto != null;
		if (hora.Texto != null)
		{
			_hora.Text = hora.Texto;
			_hora.VisibleCharacters = Mathf.Min(hora.Texto.Length, (int)((_t - hora.T) * 18));
		}

		// Subtítulo de la cinta: aparece entero con su sonido y se desvanece
		int s = SubtituloActual();
		_subtitulo.Visible = s >= 0 && _a.Lineas != null && SubtitulosActuales[s].Linea < _a.Lineas.Length;
		if (_subtitulo.Visible)
		{
			var (inicio, fin, linea) = SubtitulosActuales[s];
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
		if (_pelicula)
		{
			CrearEventosPelicula();
			_eventos.Sort((a, b) => a.T.CompareTo(b.T));
			return;
		}
		if (_momentos)
		{
			CrearEventosMomentos();
			_eventos.Sort((a, b) => a.T.CompareTo(b.T));
			return;
		}
		const string maquina = "res://Assets/Audio/Cinematica/";
		const string paso = "res://Assets/Audio/Efectos/paso_";
		_eventos.AddRange(new (double, Action)[]
		{
			(TPlay, () => Sonar(Cinta + "cinta_play.wav", -6f)),
			(0.7, () => { if (_voz.Stream != null) _voz.Play(); }),
			// Las horas se escriben a máquina (no vienen de la cinta: van por la interfaz)
			(Horas[0].T, () => Sonar(maquina + "maquina_2.wav", -14f, Sonidos.BusInterfaz)),
			(Horas[1].T, () => Sonar(maquina + "maquina_4.wav", -14f, Sonidos.BusInterfaz)),
			(Horas[2].T, () => Sonar(maquina + "maquina_5.wav", -14f, Sonidos.BusInterfaz)),
			// La rejilla vibra y la tapa se desliza
			(TRejilla, () => Sonar(Cinta + "rejilla_metal.wav", -3f)),
			(TTapa + 0.2, () => Sonar(Cinta + "reloj_rebote.wav", -14f)),
			// Aterriza agachada, casi sin ruido
			(TAterriza, () => Sonar(Cinta + "reloj_rebote.wav", -18f)),
			(TSusurro, () => Sonar(Cinta + "susurro.wav", -2.5f)),
			(TDescuelga + 0.1, () => Sonar(Cinta + "reloj_rebote.wav", -18f)),
			// El golpe del reloj es el pico más fuerte de la cinemática
			(TGolpe, () => Sonar(Cinta + "reloj_golpe.wav", 5f)),
			(TRebote, () => Sonar(Cinta + "reloj_rebote.wav", -10f)),
			(TCierraTapa, () => Sonar(Cinta + "rejilla_metal.wav", -10f)),
			// Porky mueve la silla, se sube, cuelga el reloj, se baja y la regresa
			(TEmpuja, () => Sonar("res://Assets/Audio/Efectos/cajon_1.wav", -16f)),
			(TSillaPared, () => Sonar("res://Assets/Audio/Efectos/cajon_3.wav", -14f)),
			(TSubeSilla + 0.3, () => Sonar(paso + "2.wav", -14f)),
			(TCuelga, () => Sonar(Cinta + "reloj_rebote.wav", -10f)),
			(TBajaSilla + 0.35, () => Sonar(paso + "3.wav", -12f)),
			(TJala, () => Sonar("res://Assets/Audio/Efectos/cajon_1.wav", -16f)),
			(TSillaMesa, () => Sonar("res://Assets/Audio/Efectos/cajon_3.wav", -14f)),
			(TSobresalto, () => Sonar(Cinta + "sobresalto.wav", -2f)),
			(TStop, () => Sonar(Cinta + "cinta_stop.wav", -3f)),
			(TRebobina, () => Sonar(Cinta + "cinta_rebobinar.wav", -3f)),
			(TExpulsa, () => Sonar(Cinta + "cinta_expulsar.wav", -2f)),
			(TAbrePuerta + 0.1, () => Sonar(Cinta + "puerta_abrir.wav", -4f)),
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

	// Pasos: las botas de Porky (bajas, son de la cinta) y los de la figura, casi mudos, cuando caminan
	private void Pasos(double delta)
	{
		Vector3 porky = _porky.GlobalPosition with { Y = 0 };
		Vector3 figura = _figura.GlobalPosition with { Y = 0 };
		float vPorky = porky.DistanceTo(_pasoPorky) / (float)Math.Max(delta, 1e-4);
		float vFigura = figura.DistanceTo(_pasoFigura) / (float)Math.Max(delta, 1e-4);
		_pasoPorky = porky;
		_pasoFigura = figura;
		if (vPorky > 0.4f && (_relojPaso -= delta) <= 0)
		{
			_relojPaso = 0.42;
			Sonar($"res://Assets/Audio/Efectos/paso_{GD.RandRange(1, 4)}.wav", -15f);
		}
		if (_figura.Visible && _figura.GlobalPosition.Y < 0.7f && vFigura > 0.4f && (_relojPasoFigura -= delta) <= 0)
		{
			_relojPasoFigura = 0.36;
			Sonar($"res://Assets/Audio/Efectos/paso_{GD.RandRange(1, 4)}.wav", -27f);
		}
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
