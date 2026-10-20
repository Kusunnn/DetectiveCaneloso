using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Cinemática final del Caso 0: mientras suena la cinta, se ve lo que pasó en la Sala 1 de madrugada.
// 03:09 Porky duerme en la silla y la grabadora graba. 03:12 una figura sin rostro se acerca al micrófono,
// susurra y al irse tumba el reloj (por eso se paró a las 3:12). 03:15 Porky despierta, recoge el reloj,
// ve la luz de REC, saca la cinta y se va a los vestidores. Todo cuadra con lo que confiesan en el juego.
// Los únicos textos son los de la cinta y la hora: la cinemática no le pone palabras a nadie.
//
// Se rueda en la sala real con una cámara propia y al terminar deja todo como estaba.
// Todo depende del segundo t (posiciones, animaciones, cámara), así que adelantar con clic o Enter
// salta de toma en toma sin desordenar nada. Mantener [Espacio], [Esc] o B/Start la salta.
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
		// Las tres líneas de la cinta: la confesión de Tito, los ronquidos y la voz desconocida
		public string[] Lineas;
	}

	// Para las capturas: el tiempo no avanza solo, se fija con IrA
	public bool Manual { get; set; }

	public void IrA(double t)
	{
		_t = t;
		Actualizar(true);
	}

	public static async Task Reproducir(Node3D sala, Actores actores)
	{
		var cinematica = new CinematicaCinta { _a = actores, _sala = sala };
		sala.AddChild(cinematica);
		await cinematica.ToSignal(cinematica, SignalName.Terminada);
	}

	// ---------- Tiempos (segundos) ----------

	private const double TFigura = 8.0;      // la figura sale de la oscuridad
	private const double TSusurro = 14.0;
	private const double TSeVa = 18.0;
	private const double TReloj = 21.6;      // tumba el reloj
	private const double TDespierta = 25.0;
	private const double TRecoge = 28.4;
	private const double TCinta = 35.0;      // Porky saca la cinta
	private const double TFin = 41.5;
	private const double SegundosParaSaltar = 1.2;

	private static readonly double[] Paradas = { 6.5, TSusurro, 19.5, 24.0, 31.5, 36.0, TFin };

	// Tomas: inicio, cámara (posición y punto al que mira, al empezar y al terminar) y campo de visión
	private record struct Toma(double Inicio, Vector3 Desde, Vector3 Hasta, Vector3 MiraDesde, Vector3 MiraHasta, float Fov);

	private static readonly Toma[] Tomas =
	{
		new(0.0, new(0.95f, 1.6f, -0.95f), new(0.8f, 1.55f, -1.1f), new(0.4f, 1.32f, -1.85f), new(0.4f, 1.32f, -1.85f), 36),
		new(6.5, new(-3.3f, 3.0f, 2.2f), new(-3.0f, 2.9f, 1.9f), new(-0.6f, 1.3f, -2.6f), new(-0.3f, 1.3f, -2.4f), 55),
		new(TSusurro, new(2.6f, 2.0f, -1.3f), new(2.4f, 1.95f, -1.4f), new(0.7f, 1.45f, -1.55f), new(0.7f, 1.45f, -1.6f), 40),
		new(19.5, new(2.2f, 2.4f, -2.4f), new(2.0f, 2.3f, -2.6f), new(0.2f, 1.9f, -4.7f), new(0.1f, 1.3f, -4.6f), 52),
		new(24.0, new(2.6f, 2.7f, -1.2f), new(2.4f, 2.6f, -1.8f), new(0f, 1.5f, -2.8f), new(0.3f, 1.2f, -4.2f), 50),
		new(31.5, new(2.4f, 2.5f, -0.4f), new(2.3f, 2.45f, -0.5f), new(0.5f, 1.4f, -2.3f), new(0.5f, 1.4f, -2.3f), 44),
		new(36.0, new(-1.5f, 2.8f, 2.5f), new(-1.2f, 2.7f, 2.5f), new(0.5f, 1.2f, -2.0f), new(2.4f, 1.2f, 1.5f), 55),
	};
	// Textos: la hora arriba y la cinta abajo
	private static readonly (double T, string Texto)[] Horas = { (0.6, "SALA 1 · 03:09"), (6.5, "SALA 1 · 03:12"), (24.0, "SALA 1 · 03:15") };
	private (double Inicio, double Fin, int Linea)[] Subtitulos =
	{
		(1.2, 6.3, 0),
		(7.0, 13.5, 1),
		(14.4, 18.6, 2),
	};

	// ---------- Movimiento de los personajes ----------

	// Dónde está y hacia dónde mira (grados; 0 = hacia +Z) en el segundo T
	private record struct Punto(double T, Vector3 Pos, float Giro);
	// Qué animación hace desde el segundo T; Congelar >= 0 la deja fija en ese segundo
	private record struct Paso(double T, string Animacion, bool Bucle = true, double Congelar = -1);

	private static readonly Vector3 Silla = new(0f, 0.5f, -2.8f);

	private static readonly Punto[] RutaPorky =
	{
		new(0, Silla, 0), new(TDespierta + 1.4, Silla + new Vector3(0, 0, 0.18f), 0),
		new(26.8, Silla + new Vector3(0, 0, 0.18f), 150), new(27.6, new(0.7f, 0.5f, -3.0f), 175), new(TRecoge, new(0.3f, 0.5f, -4.15f), 180),
		new(31.5, new(0.3f, 0.5f, -4.0f), 180), new(32.3, new(0.8f, 0.5f, -3.3f), 20), new(33.0, new(0.5f, 0.5f, -2.45f), 0),
		new(36.2, new(0.5f, 0.5f, -2.45f), 40), new(37.6, new(1.6f, 0.5f, -1.0f), 25), new(40.8, new(2.5f, 0.5f, 2.4f), 15),
	};

	private static readonly Paso[] AnimPorky =
	{
		new(0, "Sitting_Idle"), new(TDespierta, "Sitting_Exit", false), new(TDespierta + 1.4, "Idle"),
		new(26.8, "Walk"), new(TRecoge, "PickUp_Table", false), new(29.8, "Idle"),
		new(31.5, "Walk"), new(33.0, "Hit_Chest", false), new(34.0, "Idle"), new(34.2, "Interact", false), new(35.8, "Idle"),
		new(36.2, "Walk"),
	};

	private static readonly Punto[] RutaFigura =
	{
		new(TFigura, new(-3.4f, 0.5f, -0.3f), 95), new(11.6, new(0.95f, 0.5f, -1.3f), 95), new(11.9, new(0.95f, 0.5f, -1.3f), -135),
		new(TSeVa, new(0.95f, 0.5f, -1.3f), -135), new(18.4, new(0.95f, 0.5f, -1.3f), 170), new(19.8, new(1.25f, 0.5f, -3.5f), 200),
		new(TReloj, new(0.25f, 0.5f, -4.55f), 265), new(24.0, new(-3.0f, 0.5f, -4.5f), 270),
	};

	private static readonly Paso[] AnimFigura =
	{
		new(TFigura, "Walk"), new(11.6, "Idle"), new(11.9, "Interact", false), new(13.4, "PickUp_Table", false, 0.75), new(TSeVa, "Idle"),
		new(18.4, "Walk"),
	};

	// ---------- Estado ----------

	private Actores _a;
	private Node3D _sala;
	private double _t;
	private bool _terminada, _saltando;
	private double _relojSalto, _mantener, _relojFinal;
	private readonly List<(double T, Action Accion)> _eventos = new();
	private int _siguienteEvento;
	private readonly HashSet<int> _completos = new();

	private Camera3D _camara, _camaraAntes;
	private Node3D _figura;
	private AnimadorPersonaje _animFigura, _animPorky;
	private MeshInstance3D _cinta;
	private OmniLight3D _luzRec, _luna;
	private bool _ayudasAntes = true;
	private Node3D _pizarron;
	private bool _pizarronVisible;
	private Label3D _suenoPorky;
	private bool _suenoVisible;
	private Transform3D _porkyAntes, _relojAntes;
	private bool _erizVisible, _jugadorVisible;
	private float _intensidadMusica;
	private readonly List<CanvasLayer> _capasOcultas = new();

	private Control _interfaz;
	private ColorRect _negro;
	private Label _hora, _subtitulo, _pista;
	private CirculoSalto _circulo;
	private AudioStreamPlayer _tictac, _zumbido, _ronquido;
	private AudioStreamGeneratorPlayback _ronquidoSalida;
	private float _ruido, _faseRonquido;
	private double _relojPaso;
	private float _escala = 1f;

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
		_camara = new Camera3D { Near = 0.03f };
		_sala.AddChild(_camara);
		_camara.MakeCurrent();

		// Todo lo que se dibuja encima del juego (HUD, diálogo, viñeta) se esconde mientras dura
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

		_relojAntes = _a.Reloj.GlobalTransform;

		// El pizarrón es de la investigación de Caneloso: a las 3:12 todavía no estaba
		_pizarron = _sala.GetNodeOrNull<Node3D>("PizarronInvestigacion");
		if (_pizarron != null) { _pizarronVisible = _pizarron.Visible; _pizarron.Visible = false; }

		if (_a.Musica != null)
		{
			_intensidadMusica = _a.Musica.Intensidad;
			CreateTween().TweenProperty(_a.Musica, "volume_db", -40f, 1.5);
		}

		// La figura: el maniquí de Quaternius, negro y sin rostro
		_figura = new Node3D { Name = "FiguraCinta", Visible = false };
		_sala.AddChild(_figura);
		var maniqui = GD.Load<PackedScene>(AnimadorPersonaje.RutaUal1).Instantiate<Node3D>();
		maniqui.Name = "Modelo";
		maniqui.Scale = Vector3.One * 1.15f;
		_figura.AddChild(maniqui);
		var sombra = new StandardMaterial3D { AlbedoColor = new Color(0.015f, 0.016f, 0.02f), Roughness = 1f, RimEnabled = true, Rim = 0.35f, RimTint = 0.2f };
		foreach (Node n in maniqui.FindChildren("*", "MeshInstance3D", true, false))
			if (n is MeshInstance3D malla) malla.MaterialOverride = sombra;
		_animFigura = new AnimadorPersonaje { Modelo = maniqui };
		_figura.AddChild(_animFigura);
		_animFigura.Controlar(true);

		// La cinta en la grabadora, y la luz de REC
		_cinta = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(0.1f, 0.014f, 0.064f) },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.09f, 0.07f), Roughness = 0.55f, Metallic = 0.2f },
		};
		_sala.AddChild(_cinta);
		_luzRec = new OmniLight3D { LightColor = new Color(1f, 0.12f, 0.08f), OmniRange = 0.7f, LightEnergy = 0f };
		_sala.AddChild(_luzRec);
		_luzRec.GlobalPosition = _a.Grabadora.GlobalPosition + new Vector3(0, 0.14f, 0.05f);
		// Luz de luna, fría y tenue, para que se lea la sala de madrugada
		_luna = new OmniLight3D { LightColor = new Color(0.6f, 0.7f, 1f), LightEnergy = 0.55f, OmniRange = 7f };
		_sala.AddChild(_luna);
		_luna.GlobalPosition = new Vector3(0.5f, 3.1f, -2.8f);

		// Los contornos de ayuda de las pistas no van en un recuerdo
		if (Configuracion.Instancia != null)
		{
			_ayudasAntes = Configuracion.Instancia.AyudasVisuales;
			Configuracion.Instancia.AyudasVisuales = false;
		}
	}

	private void Restaurar()
	{
		_a.Porky.GlobalTransform = _porkyAntes;
		if (_animPorky != null) { _animPorky.InclinarCabeza = 0; _animPorky.Controlar(false); }
		if (_suenoPorky != null) _suenoPorky.Visible = _suenoVisible;
		_a.Reloj.GlobalTransform = _relojAntes;
		if (_pizarron != null) _pizarron.Visible = _pizarronVisible;
		_a.Jugador.Visible = _jugadorVisible;
		if (_a.Eriz != null) _a.Eriz.Visible = _erizVisible;
		foreach (CanvasLayer capa in _capasOcultas) if (IsInstanceValid(capa)) capa.Visible = true;
		if (IsInstanceValid(_camaraAntes)) _camaraAntes.MakeCurrent();
		if (_a.Musica != null) _a.Musica.Intensidad = _intensidadMusica;
		_figura.QueueFree();
		_cinta.QueueFree();
		_luzRec.QueueFree();
		_luna.QueueFree();
		if (Configuracion.Instancia != null) Configuracion.Instancia.AyudasVisuales = _ayudasAntes;
		_camara.QueueFree();
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
		AudioPorCuadro();
		if (_t >= TFin) Terminar();
	}

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
	}

	private void Terminar()
	{
		if (_terminada) return;
		_terminada = true;
		_relojFinal = 0;
		_negro.Color = Colors.Black;
		_subtitulo.Visible = _hora.Visible = _circulo.Visible = _pista.Visible = false;
		foreach (var reproductor in new[] { _tictac, _zumbido, _ronquido }) reproductor.Stop();
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
			return (a.Pos.Lerp(b.Pos, u), Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(a.Giro), Mathf.DegToRad(b.Giro), Mathf.SmoothStep(0, 1, u))));
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
		double local = p.Congelar >= 0 ? p.Congelar * animador.Duracion(p.Animacion) : t - p.T;
		animador.Fijar(p.Animacion, local, p.Bucle);
	}

	private void Personajes()
	{
		var (pos, giro) = EnRuta(RutaPorky, _t);
		_a.Porky.GlobalTransform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(giro)), pos);
		Animar(_animPorky, AnimPorky, _t);
		if (_animPorky != null)
		{
			// Dormido cabecea; al caer el reloj se medio despierta; ya despierto mira el reloj en el piso
			float dormido = 30f + 4f * Mathf.Sin((float)_t * 1.9f);
			float sobresalto = (float)Mathf.Clamp(1 - Math.Abs(_t - (TReloj + 0.6)) / 0.9, 0, 1);
			float cabeza = _t < TDespierta ? Mathf.Lerp(dormido, 8f, sobresalto) : (_t > 27.6 && _t < 31.5 ? 18f : 0f);
			_animPorky.InclinarCabeza = cabeza;
		}
		if (_suenoPorky != null) _suenoPorky.Visible = _t < TDespierta;

		bool figura = _t >= TFigura && _t < 23.6;
		_figura.Visible = figura;
		if (figura)
		{
			var (fp, fg) = EnRuta(RutaFigura, _t);
			_figura.GlobalTransform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(fg)), fp);
		}
		Animar(_animFigura, AnimFigura, _t);
	}

	private void Objetos()
	{
		// El reloj: en la pared, cae al piso, Porky lo recoge y (en el corte) ya está colgado otra vez
		Transform3D pared = _relojAntes;
		var piso = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(20f)) * new Basis(Vector3.Right, Mathf.DegToRad(-90f)), new Vector3(0.25f, 0.53f, -4.55f));
		if (_t < TReloj || _t >= 31.5) _a.Reloj.GlobalTransform = pared;
		else if (_t < TReloj + 0.45)
		{
			float u = (float)((_t - TReloj) / 0.45);
			Vector3 p = pared.Origin.Lerp(piso.Origin, u * u);
			Quaternion q = pared.Basis.GetRotationQuaternion().Slerp(piso.Basis.GetRotationQuaternion(), Mathf.Min(1f, u * 1.3f));
			_a.Reloj.GlobalTransform = new Transform3D(new Basis(q), p);
		}
		else if (_t < TRecoge + 0.7 || _animPorky == null) _a.Reloj.GlobalTransform = piso;
		else
		{
			// En las manos de Porky, con la carátula hacia él
			Transform3D mano = _animPorky.Mano(false);
			Basis porky = _a.Porky.GlobalBasis.Orthonormalized();
			Vector3 frente = porky.Z;
			_a.Reloj.GlobalTransform = new Transform3D(porky * new Basis(Vector3.Up, Mathf.Pi) * new Basis(Vector3.Right, Mathf.DegToRad(-25f)), mano.Origin + frente * 0.06f);
		}

		// La cinta: en la grabadora hasta que Porky la saca y se la lleva en la mano
		if (_t < TCinta || _animPorky == null)
		{
			_cinta.GlobalTransform = new Transform3D(_a.Grabadora.GlobalBasis.Orthonormalized(), _a.Grabadora.GlobalPosition + new Vector3(0, 0.06f, 0));
		}
		else
		{
			Transform3D mano = _animPorky.Mano(false);
			_cinta.GlobalTransform = new Transform3D(_a.Porky.GlobalBasis.Orthonormalized() * new Basis(Vector3.Right, Mathf.Pi / 2), mano.Origin);
		}

		// REC parpadea mientras la grabadora graba (todo el tiempo: nadie la apagó)
		_luzRec.LightEnergy = Mathf.PosMod((float)_t, 1f) < 0.55f ? 1.6f : 0.15f;
	}

	// ---------- Cámara ----------

	private void Camara()
	{
		int i = Array.FindLastIndex(Tomas, toma => toma.Inicio <= _t);
		Toma toma = Tomas[Math.Max(0, i)];
		double fin = i + 1 < Tomas.Length ? Tomas[i + 1].Inicio : TFin;
		float u = Mathf.SmoothStep(0, 1, (float)((_t - toma.Inicio) / (fin - toma.Inicio)));
		Vector3 mira = toma.MiraDesde.Lerp(toma.MiraHasta, u);
		// Última toma: sigue a Porky hacia la puerta
		if (i == Tomas.Length - 1) mira = mira.Lerp(_a.Porky.GlobalPosition + Vector3.Up * 0.8f, 0.6f);
		float tiempo = (float)(Time.GetTicksMsec() / 1000.0);
		Vector3 balanceo = new Vector3(Mathf.Sin(tiempo * 0.7f), Mathf.Sin(tiempo * 0.53f + 1.7f), 0) * 0.006f;
		// Golpe del reloj: la cámara tiembla un instante
		float golpe = (float)Mathf.Clamp(1 - (_t - (TReloj + 0.45)) / 0.4, 0, 1) * (_t >= TReloj + 0.45 ? 1 : 0);
		Vector3 sacudida = new Vector3(Mathf.Sin(tiempo * 63f), Mathf.Sin(tiempo * 51f + 1f), 0) * 0.02f * golpe;
		_camara.GlobalPosition = toma.Desde.Lerp(toma.Hasta, u) + balanceo + sacudida;
		_camara.LookAt(mira, Vector3.Up);
		_camara.Fov = toma.Fov;
	}

	// ---------- Entrada ----------

	public override void _Input(InputEvent evento)
	{
		if (_terminada) return;
		bool avanzar =
			(evento is InputEventMouseButton raton && raton.Pressed && raton.ButtonIndex == MouseButton.Left) ||
			(evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && (tecla.Keycode == Key.Enter || tecla.Keycode == Key.KpEnter)) ||
			(evento is InputEventJoypadButton boton && boton.Pressed && boton.ButtonIndex == JoyButton.A);
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
		_mantener = TeclaSaltar() ? _mantener + delta : Mathf.Max(0, _mantener - delta * 3);
		_circulo.Progreso = (float)(_mantener / SegundosParaSaltar);
		_circulo.QueueRedraw();
		if (_mantener >= SegundosParaSaltar)
		{
			_saltando = true;
			_relojSalto = 0;
		}
	}

	// Clic o Enter: termina de escribir el subtítulo; si ya está completo, pasa a la siguiente toma
	private void Avanzar()
	{
		int actual = SubtituloActual();
		if (actual >= 0 && !_completos.Contains(actual) && _subtitulo.VisibleCharacters >= 0 && _subtitulo.VisibleCharacters < _subtitulo.GetTotalCharacterCount())
		{
			_completos.Add(actual);
			return;
		}
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

		// Recuerdo: frío y deslavado, con viñeta y grano de película
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

		_hora = NuevaEtiqueta(Fuente("SpecialElite-Regular.ttf", 4), 30 * e, new Color(0.86f, 0.84f, 0.78f), HorizontalAlignment.Left);
		Anclar(_hora, 0.05f, 0, 0.6f, 0.11f);
		_subtitulo = NuevaEtiqueta(Fuente("CourierPrime-Regular.ttf"), 30 * e, new Color(0.92f, 0.9f, 0.84f), HorizontalAlignment.Center);
		Anclar(_subtitulo, 0.1f, 0.89f, 0.9f, 1);

		_negro = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_negro);
		Anclar(_negro, 0, 0, 1, 1);

		// Mantener para saltar: arriba a la derecha, para no tapar la cinta
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

	private static float Rampa(double t, double desde, double hasta) => (float)Mathf.Clamp((t - desde) / (hasta - desde), 0, 1);

	private void Interfaz()
	{
		// Entra desde negro y se va a negro al final
		_negro.Color = new Color(0, 0, 0, Mathf.Max(1 - Rampa(_t, 0, 1.2), Rampa(_t, TFin - 1.5, TFin)));

		// La hora, a máquina
		var hora = Horas.LastOrDefault(h => h.T <= _t);
		_hora.Visible = hora.Texto != null;
		if (hora.Texto != null)
		{
			_hora.Text = hora.Texto;
			_hora.VisibleCharacters = Mathf.Min(hora.Texto.Length, (int)((_t - hora.T) * 18));
		}

		int s = SubtituloActual();
		_subtitulo.Visible = s >= 0 && _a.Lineas != null && Subtitulos[s].Linea < _a.Lineas.Length;
		if (_subtitulo.Visible)
		{
			var (inicio, fin, linea) = Subtitulos[s];
			_subtitulo.Text = _a.Lineas[linea];
			int letras = _completos.Contains(s) ? -1 : (int)((_t - inicio) * 32);
			_subtitulo.VisibleCharacters = letras < 0 || letras >= _a.Lineas[linea].Length ? -1 : letras;
			_subtitulo.Modulate = new Color(1, 1, 1, 1 - Rampa(_t, fin - 0.35, fin));
		}
	}

	// ---------- Audio ----------

	private AudioStreamPlayer NuevoAudio(string ruta, string bus, float volumen)
	{
		var reproductor = new AudioStreamPlayer { Stream = ruta != null ? GD.Load<AudioStream>(ruta) : null, Bus = bus, VolumeDb = volumen };
		AddChild(reproductor);
		return reproductor;
	}

	private void Sonar(string ruta, float volumen = 0f)
	{
		var reproductor = NuevoAudio(ruta, Sonidos.BusEfectos, volumen);
		reproductor.Finished += reproductor.QueueFree;
		reproductor.Play();
	}

	private void CrearAudio()
	{
		_zumbido = NuevoAudio("res://Assets/Audio/Ambiente/zumbido_sala.ogg", Sonidos.BusAmbiente, -14f);
		_tictac = NuevoAudio("res://Assets/Audio/Ambiente/reloj_pared.wav", Sonidos.BusAmbiente, -12f);
		_zumbido.Play();
		_tictac.Play();
		// Ronquido: ruido grave hecho al vuelo, con el ritmo de la respiración
		_ronquido = NuevoAudio(null, Sonidos.BusEfectos, -10f);
		_ronquido.Stream = new AudioStreamGenerator { MixRate = 22050, BufferLength = 0.25f };
		_ronquido.Play();
		_ronquidoSalida = (AudioStreamGeneratorPlayback)_ronquido.GetStreamPlayback();
	}

	private void CrearEventos()
	{
		const string cine = "res://Assets/Audio/Cinematica/";
		const string efectos = "res://Assets/Audio/Efectos/";
		_eventos.AddRange(new (double, Action)[]
		{
			(0.6, () => Sonar(cine + "maquina_2.wav", -10f)),
			(6.5, () => Sonar(cine + "maquina_4.wav", -10f)),
			(12.3, () => Sonar(efectos + "metal_cerrar_2.wav", -8f)),
			(TReloj + 0.45, () => { Sonar(cine + "golpe_titulo.ogg", -4f); Sonar(efectos + "metal_cerrar_1.wav", -6f); _tictac.Stop(); }),
			(24.0, () => Sonar(cine + "maquina_5.wav", -10f)),
			(TRecoge + 0.6, () => Sonar(efectos + "metal_abrir_1.wav", -14f)),
			(33.0, () => Sonar("res://Assets/Audio/Interfaz/error_suave.wav", -12f)),
			(TCinta, () => Sonar("res://Assets/Audio/Efectos/cajon_2.wav", -10f)),
		});
		_eventos.Sort((a, b) => a.T.CompareTo(b.T));
	}

	// Pasos y ronquido, cuadro a cuadro
	private void AudioPorCuadro()
	{
		// Pasos de Porky mientras camina
		if (PasoEn(AnimPorky, _t) is Paso paso && paso.Animacion == "Walk" && (_relojPaso -= GetProcessDeltaTime()) <= 0)
		{
			_relojPaso = 0.42;
			Sonar($"res://Assets/Audio/Efectos/paso_{GD.RandRange(1, 4)}.wav", -14f);
		}
		if (_ronquidoSalida == null) return;
		// Ronca mientras duerme; se calla un momento cuando cae el reloj
		bool ronca = _t < TDespierta && Math.Abs(_t - (TReloj + 1.2)) > 1.4;
		int cuadros = _ronquidoSalida.GetFramesAvailable();
		for (int i = 0; i < cuadros; i++)
		{
			_faseRonquido += 1f / 22050f;
			float ciclo = Mathf.PosMod(_faseRonquido, 3.4f);
			float sobre = ciclo < 1.4f ? Mathf.Pow(Mathf.Sin(Mathf.Pi * ciclo / 1.4f), 2) * (0.55f + 0.45f * Mathf.Sin(Mathf.Tau * 31f * _faseRonquido))
				: ciclo > 1.7f && ciclo < 2.7f ? 0.2f * Mathf.Sin(Mathf.Pi * (ciclo - 1.7f)) : 0f;
			_ruido = (_ruido + 0.02f * (GD.Randf() * 2 - 1)) / 1.02f;
			float v = ronca ? Mathf.Clamp(_ruido * 7f * sobre, -1, 1) : 0f;
			_ronquidoSalida.PushFrame(new Vector2(v, v));
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
