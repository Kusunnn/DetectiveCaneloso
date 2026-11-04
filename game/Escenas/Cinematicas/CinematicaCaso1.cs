using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// HU-9.5 CA-01: reproduce las cinemáticas propias del Caso 1. Cada una es un Libreto (LibretosCaso1.cs): una lista de
// planos con su cámara, sus textos y lo que pasa en cuadro. Se filma en la escena actual (pistas, llegada al lago) o en
// un escenario cargado aparte, en su propio mundo y con su caso sin arrancar (la reconstrucción y los finales).
// Todos los textos vienen del guion, ninguno vive aquí. Clic o Enter pasa al siguiente plano; mantener [Espacio], [Esc]
// o B/Start la salta. Todo depende de t, así que IrA y las capturas dan siempre lo mismo. Cada id sale una vez por
// partida (registro de GestorPartida).
public partial class CinematicaCaso1 : CanvasLayer
{
	[Signal] public delegate void TerminadaEventHandler();

	public class Plano
	{
		public double Duracion = 5;
		public Vector3 CamaraDesde, CamaraHasta, MiraDesde, MiraHasta;
		public float Fov = 50f;
		public string Hora, Texto;
		public float Recuerdo;                 // 0 = color normal; más = frío y deslavado
		public float Exposicion = 1f;          // Para los planos de noche cerrada
		public bool Negro;                     // Cuadro en negro: solo se oye
		public Action<float> Cada;             // Avance del plano (0 a 1)
		public (double T, string Audio, float Volumen)[] Sonidos = Array.Empty<(double, string, float)>();
	}

	public class Libreto
	{
		public string Mundo;                   // Escena que se carga aparte; vacío = se filma en la escena actual
		public string Luz = "";                // "" = sin ambiente de sonido; ruta de un sonido que se repite
		public Action<Node3D> Preparar;
		public List<Plano> Planos = new();
	}

	private const double SegundosParaSaltar = 1.2;
	private const double Fundido = 0.6;

	public static bool EnCurso { get; private set; }

	public static bool Vista(string id) => GestorPartida.Instancia?.CinematicaVista(id) ?? false;

	// Para las capturas: el tiempo no avanza solo, se fija con IrA
	public bool Manual { get; set; }
	public double Total => _inicios[^1];
	public double[] Inicios => _inicios;

	public void IrA(double t)
	{
		_t = t;
		Actualizar();
	}

	// Sale una sola vez por partida. Si ya se vio, no hace nada. registrar: false en el segundo acto de una misma cinemática.
	public static async Task Reproducir(Node nodo, string id, Libreto libreto, bool registrar = true)
	{
		if (EnCurso || (registrar && Vista(id))) return;
		EnCurso = true;
		if (registrar) GestorPartida.Instancia?.RegistrarCinematica(id);
		var cinematica = Crear(nodo, libreto);
		await cinematica.ToSignal(cinematica, SignalName.Terminada);
	}

	// Para las capturas y las pruebas: sin registro y sin esperar
	public static CinematicaCaso1 Crear(Node nodo, Libreto libreto)
	{
		EnCurso = true;
		var cinematica = new CinematicaCaso1 { _libreto = libreto, Name = "CinematicaCaso1" };
		nodo.GetTree().Root.AddChild(cinematica);
		return cinematica;
	}

	private Libreto _libreto;
	private double _t, _mantener, _relojSalto, _relojFinal;
	private double[] _inicios;
	private bool _saltando, _terminada;
	private int _planoActual = -1;
	private readonly HashSet<(int, int)> _sonados = new();

	private Node3D _escena;
	private SubViewport _vista;
	private Camera3D _camara, _camaraAntes;
	private CameraAttributesPractical _atributos;
	private Jugador _jugador;
	private bool _jugadorVisible, _ayudasAntes = true, _bloqueoHecho;
	private readonly List<CanvasLayer> _capasOcultas = new();
	private readonly Dictionary<string, float> _volumenBuses = new();
	private Input.MouseModeEnum _raton;
	private AudioStreamPlayer _ambiente;

	private Control _interfaz;
	private ColorRect _negro, _capaRecuerdo;
	private Label _hora, _subtitulo, _pista;
	private double _relojPista;

	public override void _Ready()
	{
		Layer = 115;
		ProcessMode = ProcessModeEnum.Always;
		_raton = Input.MouseMode;
		var inicios = new List<double> { 0 };
		foreach (var plano in _libreto.Planos) inicios.Add(inicios[^1] + plano.Duracion);
		_inicios = inicios.ToArray();

		bool enMundoAparte = !string.IsNullOrEmpty(_libreto.Mundo);
		if (enMundoAparte) MontarMundo();
		else MontarEnEscena();
		_atributos = new CameraAttributesPractical();
		_camara = new Camera3D { Name = "CamaraCinematica", Near = 0.03f, Attributes = _atributos };
		_escena.AddChild(_camara);
		_camaraAntes = enMundoAparte ? null : GetViewport().GetCamera3D();
		_camara.MakeCurrent();
		_libreto.Preparar?.Invoke(_escena);

		CrearInterfaz();
		Atenuar(Sonidos.BusMusica);
		Atenuar(Sonidos.BusAmbiente);
		if (!string.IsNullOrEmpty(_libreto.Luz) && ResourceLoader.Exists(_libreto.Luz))
		{
			var stream = GD.Load<AudioStream>(_libreto.Luz);
			if (stream is AudioStreamWav wav) { wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward; wav.LoopEnd = (int)(wav.GetLength() * wav.MixRate); }
			_ambiente = new AudioStreamPlayer { Stream = stream, VolumeDb = -8f, Bus = "Master" };
			AddChild(_ambiente);
			_ambiente.Play();
		}
		if (Configuracion.Instancia != null)
		{
			_ayudasAntes = Configuracion.Instancia.AyudasVisuales;
			Configuracion.Instancia.AyudasVisuales = false;
		}
		Actualizar();
	}

	// Escenario aparte: solo es el decorado, su caso no arranca ni toca la partida
	private void MontarMundo()
	{
		var contenedor = new SubViewportContainer { Stretch = true, MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(contenedor);
		contenedor.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		contenedor.SetProcessInput(false);
		contenedor.SetProcessUnhandledInput(false);
		_vista = new SubViewport { OwnWorld3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, AudioListenerEnable3D = true };
		contenedor.AddChild(_vista);
		_escena = GD.Load<PackedScene>(_libreto.Mundo).Instantiate<Node3D>();
		foreach (Node hijo in _escena.GetChildren())
		{
			if (hijo is Caso1 campamento) campamento.SoloVer = true;
			if (hijo is EscenarioCaso1 escenario) escenario.SoloVer = true;
			if (hijo is Jugador or MenuPausa or MandoHud) hijo.ProcessMode = ProcessModeEnum.Disabled;
			if (hijo is MusicaFondo) hijo.QueueFree();
		}
		_vista.AddChild(_escena);
		foreach (Node n in _escena.FindChildren("*", "CanvasLayer", true, false))
			if (n is CanvasLayer capa && n.Name != "Vineta") capa.Visible = false;
		if (_escena.GetNodeOrNull<Node3D>("Jugador") is { } jugador) jugador.Visible = false;
	}

	// En la escena actual: se esconde la interfaz y el detective, y el juego queda quieto
	private void MontarEnEscena()
	{
		_escena = GetTree().CurrentScene as Node3D;
		_jugador = _escena.GetNodeOrNull<Jugador>("Jugador");
		if (_jugador != null)
		{
			_jugadorVisible = _jugador.Visible;
			_jugador.Visible = false;
			_jugador.Bloquear(true);
			_bloqueoHecho = true;
		}
		foreach (Node n in _escena.FindChildren("*", "CanvasLayer", true, false))
			if (n is CanvasLayer capa && capa.Visible && n.Name != "Vineta") { capa.Visible = false; _capasOcultas.Add(capa); }
	}

	private void Restaurar()
	{
		if (string.IsNullOrEmpty(_libreto.Mundo))
		{
			if (_jugador != null && IsInstanceValid(_jugador))
			{
				_jugador.Visible = _jugadorVisible;
				if (_bloqueoHecho) _jugador.Bloquear(false);
			}
			foreach (var capa in _capasOcultas) if (IsInstanceValid(capa)) capa.Visible = true;
			if (IsInstanceValid(_camaraAntes)) _camaraAntes.MakeCurrent();
			if (IsInstanceValid(_camara)) _camara.QueueFree();
			Input.MouseMode = _raton;
		}
		foreach (var (bus, db) in _volumenBuses)
		{
			int i = AudioServer.GetBusIndex(bus);
			if (i >= 0) AudioServer.SetBusVolumeDb(i, db);
		}
		if (Configuracion.Instancia != null) Configuracion.Instancia.AyudasVisuales = _ayudasAntes;
		EnCurso = false;
	}

	public override void _ExitTree()
	{
		if (!_terminada) EnCurso = false;
	}

	private void Atenuar(string bus)
	{
		int i = AudioServer.GetBusIndex(bus);
		if (i < 0) return;
		_volumenBuses[bus] = AudioServer.GetBusVolumeDb(i);
		AudioServer.SetBusVolumeDb(i, -60f);
	}

	// ---------- Tiempo ----------

	private int PlanoEn(double t)
	{
		for (int i = _libreto.Planos.Count - 1; i >= 0; i--) if (t >= _inicios[i]) return i;
		return 0;
	}

	public override void _Process(double delta)
	{
		if (_terminada)
		{
			_relojFinal += delta;
			_negro.Color = new Color(0, 0, 0, 1 - (float)Mathf.Clamp(_relojFinal / Fundido, 0, 1));
			if (_relojFinal >= Fundido)
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
		if (Manual) return;
		_t += delta;
		Actualizar();
		SonarLoQueToca();
		if (_t >= Total) Terminar();
	}

	public void Saltar() => Terminar();

	private void Terminar()
	{
		if (_terminada) return;
		_terminada = true;
		_relojFinal = 0;
		_negro.Color = Colors.Black;
		_subtitulo.Visible = _hora.Visible = _pista.Visible = false;
		_ambiente?.Stop();
		if (IsInstanceValid(_vista)) _vista.GetParent<Control>().Visible = false;
		Restaurar();
		if (!string.IsNullOrEmpty(_libreto.Mundo) && IsInstanceValid(_vista)) _vista.GetParent().QueueFree();
	}

	private void Actualizar()
	{
		if (_libreto.Planos.Count == 0) return;
		// Cambió la escena a media cinemática (cargar partida, volver al menú): ya no hay qué filmar
		if (!IsInstanceValid(_camara) || !_camara.IsInsideTree())
		{
			Terminar();
			return;
		}
		int i = Mathf.Min(PlanoEn(_t), _libreto.Planos.Count - 1);
		var plano = _libreto.Planos[i];
		_planoActual = i;
		float u = (float)Mathf.Clamp((_t - _inicios[i]) / plano.Duracion, 0, 1);
		float s = u * u * (3 - 2 * u);
		var posicion = plano.CamaraDesde.Lerp(plano.CamaraHasta, s);
		var mira = plano.MiraDesde.Lerp(plano.MiraHasta, s);
		_camara.Fov = plano.Fov;
		_atributos.ExposureMultiplier = plano.Exposicion;
		if (_camara.IsInsideTree()) _camara.LookAtFromPosition(posicion, mira, Mathf.Abs((mira - posicion).Normalized().Y) > 0.98f ? Vector3.Forward : Vector3.Up);
		plano.Cada?.Invoke(u);

		// Interfaz: fundido al entrar y al salir, hora a máquina y texto del guion
		float negro = Mathf.Max(1 - (float)Mathf.Clamp(_t / Fundido, 0, 1), (float)Mathf.Clamp((_t - (Total - Fundido)) / Fundido, 0, 1));
		if (plano.Negro) negro = 1f;
		_negro.Color = new Color(0, 0, 0, negro);
		((ShaderMaterial)_capaRecuerdo.Material).SetShaderParameter("fuerza", plano.Recuerdo);
		_capaRecuerdo.Visible = plano.Recuerdo > 0;
		string hora = plano.Hora ?? Enumerable.Range(0, i + 1).Select(k => _libreto.Planos[i - k].Hora).FirstOrDefault(h => h != null);
		_hora.Visible = hora != null;
		if (hora != null) _hora.Text = hora;
		_subtitulo.Visible = !string.IsNullOrEmpty(plano.Texto);
		if (_subtitulo.Visible)
		{
			_subtitulo.Text = plano.Texto;
			float visible = (float)Mathf.Clamp((_t - _inicios[i]) / 0.3, 0, 1) * (1 - (float)Mathf.Clamp((_t - (_inicios[i + 1] - 0.3)) / 0.3, 0, 1));
			_subtitulo.Modulate = new Color(1, 1, 1, visible);
		}
		float pista = _mantener > 0 ? 1f : 1f - (float)Mathf.Clamp((_relojPista - 2.4) / 0.6, 0, 1);
		_pista.Modulate = new Color(1, 1, 1, pista);
	}

	// Un sonido con T negativo empieza antes del corte de su plano (J-cut)
	private void SonarLoQueToca()
	{
		for (int i = 0; i < _libreto.Planos.Count; i++)
		{
			var sonidos = _libreto.Planos[i].Sonidos;
			for (int k = 0; k < sonidos.Length; k++)
			{
				if (_t < _inicios[i] + sonidos[k].T || !_sonados.Add((i, k))) continue;
				if (!ResourceLoader.Exists(sonidos[k].Audio)) continue;
				var reproductor = new AudioStreamPlayer { Stream = GD.Load<AudioStream>(sonidos[k].Audio), VolumeDb = sonidos[k].Volumen, Bus = "Master" };
				AddChild(reproductor);
				reproductor.Finished += reproductor.QueueFree;
				reproductor.Play();
			}
		}
	}

	// ---------- Entrada ----------

	public override void _Input(InputEvent evento)
	{
		if (_terminada) return;
		bool tecla = evento is InputEventKey k && k.Pressed && !k.Echo;
		bool boton = evento is InputEventMouseButton r && r.Pressed || evento is InputEventJoypadButton j && j.Pressed;
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

	private void Avanzar()
	{
		double siguiente = _inicios.FirstOrDefault(p => p > _t + 0.05, Total);
		// Lo que ya pasó no suena
		for (int i = 0; i < _libreto.Planos.Count; i++)
			if (_inicios[i] < siguiente)
				for (int k = 0; k < _libreto.Planos[i].Sonidos.Length; k++) _sonados.Add((i, k));
		_t = siguiente;
		if (_t >= Total) { Terminar(); return; }
		Actualizar();
	}

	private void ProcesarSalto(double delta)
	{
		if (_saltando) return;
		bool mantiene = Input.IsKeyPressed(Key.Space) || Input.IsKeyPressed(Key.Escape) ||
			Input.IsJoyButtonPressed(0, JoyButton.B) || Input.IsJoyButtonPressed(0, JoyButton.Start);
		if (mantiene) _relojPista = 0;
		_mantener = mantiene ? _mantener + delta : Mathf.Max(0, _mantener - delta * 3);
		if (_mantener >= SegundosParaSaltar)
		{
			_saltando = true;
			_relojSalto = 0;
		}
	}

	// ---------- Interfaz ----------

	private static void Anclar(Control control, float izq, float arriba, float der, float abajo)
	{
		control.AnchorLeft = izq;
		control.AnchorTop = arriba;
		control.AnchorRight = der;
		control.AnchorBottom = abajo;
		control.OffsetLeft = control.OffsetTop = control.OffsetRight = control.OffsetBottom = 0;
	}

	private Label NuevaEtiqueta(string fuente, int tam, Color color, HorizontalAlignment alineacion)
	{
		var etiqueta = new Label
		{
			HorizontalAlignment = alineacion,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		etiqueta.AddThemeFontOverride("font", GD.Load<FontFile>("res://Assets/Fuentes/" + fuente));
		etiqueta.AddThemeFontSizeOverride("font_size", tam);
		etiqueta.AddThemeColorOverride("font_color", color);
		etiqueta.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_interfaz.AddChild(etiqueta);
		return etiqueta;
	}

	private ColorRect Capa(string shader)
	{
		var capa = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = new ShaderMaterial { Shader = GD.Load<Shader>(shader) } };
		_interfaz.AddChild(capa);
		Anclar(capa, 0, 0, 1, 1);
		return capa;
	}

	private void CrearInterfaz()
	{
		float e = GetViewport().GetVisibleRect().Size.Y / 1080f;
		_interfaz = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(_interfaz);
		Anclar(_interfaz, 0, 0, 1, 1);
		_capaRecuerdo = Capa("res://Escenas/Cinematicas/recuerdo.gdshader");
		((ShaderMaterial)Capa("res://Escenas/Cinematicas/vineta.gdshader").Material).SetShaderParameter("fuerza", 0.6f);
		((ShaderMaterial)Capa("res://Escenas/Menus/Fondo/grano_pelicula.gdshader").Material).SetShaderParameter("intensidad", 0.07f);

		var arriba = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		var abajo = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(arriba);
		_interfaz.AddChild(abajo);
		Anclar(arriba, 0, 0, 1, 0.1f);
		Anclar(abajo, 0, 0.83f, 1, 1);

		_negro = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_negro);
		Anclar(_negro, 0, 0, 1, 1);

		_hora = NuevaEtiqueta("SpecialElite-Regular.ttf", Mathf.RoundToInt(30 * e), new Color(0.86f, 0.84f, 0.78f), HorizontalAlignment.Left);
		Anclar(_hora, 0.05f, 0, 0.6f, 0.1f);
		_subtitulo = NuevaEtiqueta("CourierPrime-Regular.ttf", Mathf.RoundToInt(30 * e), new Color(0.92f, 0.9f, 0.84f), HorizontalAlignment.Center);
		Anclar(_subtitulo, 0.08f, 0.835f, 0.92f, 0.995f);
		bool mando = Configuracion.Instancia?.UsandoMando ?? false;
		_pista = NuevaEtiqueta("CourierPrime-Regular.ttf", Mathf.RoundToInt(22 * e), new Color(0.72f, 0.7f, 0.66f), HorizontalAlignment.Right);
		_pista.Text = mando ? "Mantén [B] para saltar" : "Mantén [Espacio] para saltar";
		Anclar(_pista, 0.5f, 0, 0.97f, 0.1f);
	}
}
