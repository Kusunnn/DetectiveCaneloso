using Godot;
using System;
using System.Collections.Generic;

// Autoload: preferencias del jugador, guardadas en user://configuracion.cfg.
public partial class Configuracion : Node
{
	private const string RutaConfiguracion = "user://configuracion.cfg";

	public static Configuracion Instancia { get; private set; }

	// Volumen de cada bus (0–1), encima del nivel de mezcla de default_bus_layout.tres.
	// El general arranca al 70 % para que el juego nunca sorprenda al abrirlo.
	public static readonly string[] Buses = { "Master", "Musica", "Ambiente", "Efectos", "Interfaz" };
	private readonly Dictionary<string, float> _volumenes = new()
	{
		["Master"] = 0.7f, ["Musica"] = 1f, ["Ambiente"] = 1f, ["Efectos"] = 1f, ["Interfaz"] = 1f,
	};
	private readonly Dictionary<string, float> _mezclaBase = new();

	public float Volumen(string bus) => _volumenes.TryGetValue(bus, out float v) ? v : 1f;

	public void FijarVolumen(string bus, float valor)
	{
		_volumenes[bus] = Mathf.Clamp(valor, 0f, 1f);
		int indice = AudioServer.GetBusIndex(bus);
		if (indice < 0) return;
		if (!_mezclaBase.ContainsKey(bus)) _mezclaBase[bus] = AudioServer.GetBusVolumeDb(indice);
		AudioServer.SetBusMute(indice, _volumenes[bus] <= 0.001f);
		AudioServer.SetBusVolumeDb(indice, _mezclaBase[bus] + Mathf.LinearToDb(Mathf.Max(_volumenes[bus], 0.001f)));
	}

	public float VolumenGeneral
	{
		get => Volumen("Master");
		set => FijarVolumen("Master", value);
	}

	private static string ClaveVolumen(string bus) => bus == "Master" ? "volumen_general" : "volumen_" + bus.ToLower();

	// El juego arranca en pantalla completa; [F11] o [Alt]+[Enter] la alternan en cualquier momento.
	private bool _pantallaCompleta = true;
	public bool PantallaCompleta
	{
		get => _pantallaCompleta;
		set
		{
			_pantallaCompleta = value;
			DisplayServer.WindowSetMode(value
				? DisplayServer.WindowMode.Fullscreen
				: DisplayServer.WindowMode.Windowed);
		}
	}

	// false: hay que mantener la tecla para seguir agachado; true: cada pulsación alterna.
	public bool AgacharseAlternar { get; set; } = false;

	// Multiplicador de la sensibilidad del ratón (0.3 a 2.0; 1 = normal).
	private float _sensibilidad = 1f;
	public float SensibilidadRaton { get => _sensibilidad; set => _sensibilidad = Mathf.Clamp(value, 0.3f, 2f); }

	// Ayudas visuales del tutorial (íconos de tecla, foco, flecha al objetivo). Se pueden apagar en Opciones.
	public bool AyudasVisuales { get; set; } = true;

	// Tamaño del texto del HUD, el diario, el expediente y los diálogos (1 = normal)
	public static readonly (float Escala, string Nombre)[] EscalasTexto = { (1f, "Normal"), (1.15f, "Grande"), (1.3f, "Muy grande") };
	private float _escalaTexto = 1f;
	public float EscalaTexto { get => _escalaTexto; set => _escalaTexto = Mathf.Clamp(value, 1f, 1.3f); }

	// Acciones que el jugador puede cambiar de tecla en Opciones → Controles (Esc y las del control no se cambian)
	public static readonly (string Accion, string Nombre)[] AccionesTeclas =
	{
		("mover_adelante", "Avanzar"), ("mover_atras", "Retroceder"), ("mover_izquierda", "Izquierda"), ("mover_derecha", "Derecha"),
		("correr", "Correr"), ("agacharse", "Agacharse"), ("interactuar", "Interactuar"), ("diario", "Diario"),
		("expediente", "Expediente"), ("deducir", "Tablero de deducción"), ("linterna", "Linterna"),
		("cambiar_camara", "Cambiar cámara"), ("ayuda", "Ayuda"),
	};

	// Tecla de teclado de una acción (la primera, si tiene varias)
	public static Key TeclaDe(string accion)
	{
		if (!InputMap.HasAction(accion)) return Key.None;
		foreach (InputEvent e in InputMap.ActionGetEvents(accion))
			if (e is InputEventKey k) return k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode;
		return Key.None;
	}

	// Asigna una tecla a la acción. Si otra acción la usaba, se queda con la tecla anterior (se intercambian).
	public void CambiarTecla(string accion, Key tecla)
	{
		Key anterior = TeclaDe(accion);
		foreach (var (otra, _) in AccionesTeclas)
			if (otra != accion && TeclaDe(otra) == tecla) PonerTecla(otra, anterior);
		PonerTecla(accion, tecla);
	}

	private static void PonerTecla(string accion, Key tecla)
	{
		foreach (InputEvent e in InputMap.ActionGetEvents(accion))
			if (e is InputEventKey) InputMap.ActionEraseEvent(accion, e);
		if (tecla != Key.None) InputMap.ActionAddEvent(accion, new InputEventKey { PhysicalKeycode = tecla });
	}

	// Vuelve a las teclas de project.godot
	public void RestablecerTeclas()
	{
		InputMap.LoadFromProjectSettings();
	}

	// true si lo último que tocó el jugador fue un control; las ayudas muestran sus botones
	public bool UsandoMando { get; private set; } = false;
	[Signal] public delegate void DispositivoCambiadoEventHandler(bool mando);

	// Contador de FPS en la esquina, se muestra y oculta con F3
	private Label _contadorFps;

	public override void _EnterTree()
	{
		Instancia = this;
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		CrearContadorFps();

		var archivo = new ConfigFile();
		bool hayArchivo = archivo.Load(RutaConfiguracion) == Error.Ok;
		foreach (string bus in Buses)
			FijarVolumen(bus, hayArchivo ? archivo.GetValue("audio", ClaveVolumen(bus), Volumen(bus)).AsSingle() : Volumen(bus));
		if (!hayArchivo)
		{
			PantallaCompleta = PantallaCompleta; // Primera vez: valores por defecto
			return;
		}

		// Los archivos de antes de la versión 2 guardaban «ventana» porque era lo predeterminado, no porque se eligiera
		bool eleccionGuardada = archivo.GetValue("video", "version", 1).AsInt32() >= 2;
		PantallaCompleta = eleccionGuardada ? archivo.GetValue("video", "pantalla_completa", PantallaCompleta).AsBool() : PantallaCompleta;
		AgacharseAlternar = archivo.GetValue("controles", "agacharse_alternar", AgacharseAlternar).AsBool();
		SensibilidadRaton = archivo.GetValue("controles", "sensibilidad_raton", SensibilidadRaton).AsSingle();
		AyudasVisuales = archivo.GetValue("controles", "ayudas_visuales", AyudasVisuales).AsBool();
		EscalaTexto = archivo.GetValue("accesibilidad", "escala_texto", EscalaTexto).AsSingle();
		// Teclas cambiadas: solo se guardan las que el jugador cambió
		if (archivo.HasSection("teclas"))
			foreach (string accion in archivo.GetSectionKeys("teclas"))
				if (InputMap.HasAction(accion)) PonerTecla(accion, (Key)archivo.GetValue("teclas", accion).AsInt64());
	}

	private void CrearContadorFps()
	{
		var capa = new CanvasLayer { Layer = 100 };
		AddChild(capa);
		_contadorFps = new Label { Position = new Vector2(12, 8), Visible = false };
		_contadorFps.AddThemeFontSizeOverride("font_size", 22);
		_contadorFps.AddThemeColorOverride("font_color", new Color("efca08"));
		_contadorFps.AddThemeColorOverride("font_outline_color", Colors.Black);
		_contadorFps.AddThemeConstantOverride("outline_size", 6);
		capa.AddChild(_contadorFps);
		SetProcess(false);
	}

	public override void _Input(InputEvent evento)
	{
		// Teclado/mouse o control: lo último que se usó decide qué íconos muestran las ayudas
		bool? mando = evento switch
		{
			InputEventJoypadButton => true,
			InputEventJoypadMotion movimiento when Mathf.Abs(movimiento.AxisValue) > 0.4f => true,
			InputEventKey virtual_ when virtual_.Device == MandoHud.DispositivoVirtual => null,
			InputEventKey or InputEventMouseButton => false,
			InputEventMouseMotion raton when raton.Relative.Length() > 3f => false,
			_ => null,
		};
		if (mando.HasValue && mando.Value != UsandoMando)
		{
			UsandoMando = mando.Value;
			EmitSignal(SignalName.DispositivoCambiado, UsandoMando);
		}

		if (evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && tecla.Keycode == Key.F3)
		{
			_contadorFps.Visible = !_contadorFps.Visible;
			SetProcess(_contadorFps.Visible);
		}

		if (evento is InputEventKey teclaVentana && teclaVentana.Pressed && !teclaVentana.Echo
			&& (teclaVentana.Keycode == Key.F11 || (teclaVentana.Keycode == Key.Enter && teclaVentana.AltPressed)))
		{
			PantallaCompleta = !PantallaCompleta;
			Guardar();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Process(double delta)
	{
		_contadorFps.Text = Engine.GetFramesPerSecond() + " FPS";
	}

	// Tecla de project.godot, para guardar solo las que cambian
	private static Key TeclaOriginal(string accion)
	{
		var datos = ProjectSettings.GetSetting("input/" + accion).AsGodotDictionary();
		if (datos == null || !datos.ContainsKey("events")) return Key.None;
		foreach (Variant v in datos["events"].AsGodotArray())
			if (v.As<InputEvent>() is InputEventKey k) return k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode;
		return Key.None;
	}

	public void Guardar()
	{
		var archivo = new ConfigFile();
		foreach (string bus in Buses)
			archivo.SetValue("audio", ClaveVolumen(bus), Volumen(bus));
		archivo.SetValue("video", "pantalla_completa", PantallaCompleta);
		archivo.SetValue("video", "version", 2);
		archivo.SetValue("controles", "agacharse_alternar", AgacharseAlternar);
		archivo.SetValue("controles", "sensibilidad_raton", SensibilidadRaton);
		archivo.SetValue("controles", "ayudas_visuales", AyudasVisuales);
		archivo.SetValue("accesibilidad", "escala_texto", EscalaTexto);
		foreach (var (accion, _) in AccionesTeclas)
		{
			Key actual = TeclaDe(accion), original = TeclaOriginal(accion);
			if (actual != original) archivo.SetValue("teclas", accion, (long)actual);
		}

		Error error = archivo.Save(RutaConfiguracion);
		if (error != Error.Ok)
		{
			GD.PushWarning("Configuracion: no se pudo guardar (" + error + ").");
		}
	}
}
