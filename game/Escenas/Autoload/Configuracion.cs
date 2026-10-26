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

	// Teclas que el jugador puede cambiar en Opciones > Controles (nombre visible, acción del InputMap)
	public static readonly (string Nombre, string Accion)[] TeclasConfigurables =
	{
		("Avanzar", "mover_adelante"), ("Retroceder", "mover_atras"),
		("Izquierda", "mover_izquierda"), ("Derecha", "mover_derecha"),
		("Correr", "correr"), ("Agacharse", "agacharse"),
		("Interactuar", "interactuar"), ("Linterna", "linterna"),
	};

	// Teclas que el HUD lee directamente o que el juego reserva: no se pueden asignar a otra acción
	private static readonly Dictionary<Key, string> TeclasReservadas = new()
	{
		[Key.Escape] = "Pausa", [Key.Tab] = "Expediente", [Key.Q] = "Libreta", [Key.F11] = "Pantalla completa", [Key.F3] = "Contador de FPS",
	};

	// Nombre en español de las demás acciones, para avisar de conflictos
	private static readonly Dictionary<string, string> NombresAcciones = new()
	{
		["deducir"] = "Tablero", ["cambiar_camara"] = "Cambiar cámara", ["ayuda"] = "Ayuda",
		["bailar"] = "Bailar", ["morir"] = "Morir", ["pausa"] = "Pausa",
	};

	// Teclas que Windows, macOS o el propio teclado se reservan (HU-19): el juego no las recibe bien
	// o, al pulsarlas, el sistema hace otra cosa (cambiar de ventana, cerrar el juego, captura de pantalla...)
	private static readonly Dictionary<Key, string> TeclasSistema = new()
	{
		[Key.Alt] = "combinaciones del sistema como Alt + Tab, Alt + F4 y Ctrl + Alt + Supr",
		[Key.Meta] = "el sistema (tecla Windows / Cmd)",
		[Key.Print] = "la captura de pantalla del sistema",
		[Key.Sysreq] = "el sistema (PetSis)",
		[Key.Pause] = "el sistema (Pausa / Inter)",
		[Key.Scrolllock] = "el sistema (Bloq Despl)",
	};

	// Motivo por el que el sistema se reserva la tecla, o null si el juego puede usarla
	public static string ReservadaPorSistema(Key fisica)
	{
		if (TeclasSistema.TryGetValue(fisica, out string motivo)) return motivo;
		// Menú contextual, ayuda, multimedia (volumen, reproducir...) y teclas de abrir aplicaciones
		if ((fisica >= Key.Menu && fisica <= Key.JisKana) || fisica == Key.Unknown || fisica == Key.None)
			return "funciones especiales del teclado";
		return null;
	}

	// Teclas cambiadas por el jugador: acción → tecla física. Las demás siguen como en project.godot.
	private readonly Dictionary<string, Key> _teclas = new();

	// Asigna la tecla si es válida: no reservada y sin repetir. Se guarda al momento para que el
	// cambio no se pierda aunque el jugador cierre el juego sin pasar por «Guardar y volver».
	public bool AsignarTecla(string accion, Key fisica)
	{
		if (ReservadaPorSistema(fisica) != null || UsoDeTecla(fisica, accion) != null) return false;
		_teclas[accion] = fisica;
		AplicarTecla(accion, fisica);
		Guardar();
		return true;
	}

	// Sustituye las teclas de la acción (los botones del control se conservan)
	private static void AplicarTecla(string accion, Key fisica)
	{
		if (!InputMap.HasAction(accion)) return;
		foreach (InputEvent evento in InputMap.ActionGetEvents(accion))
			if (evento is InputEventKey) InputMap.ActionEraseEvent(accion, evento);
		InputMap.ActionAddEvent(accion, new InputEventKey { PhysicalKeycode = fisica });
	}

	public void RestaurarTeclas()
	{
		_teclas.Clear();
		foreach (var (_, accion) in TeclasConfigurables) RestaurarAccion(accion);
	}

	// Vuelve una acción a las teclas y botones de project.godot
	private static void RestaurarAccion(string accion)
	{
		if (!InputMap.HasAction(accion) || !ProjectSettings.HasSetting("input/" + accion)) return;
		var definicion = ProjectSettings.GetSetting("input/" + accion).AsGodotDictionary();
		InputMap.ActionEraseEvents(accion);
		foreach (InputEvent evento in definicion["events"].AsGodotArray<InputEvent>())
			InputMap.ActionAddEvent(accion, evento);
	}

	// Acciones configurables que se quedaron sin ninguna tecla del teclado (nombres visibles)
	public static List<string> AccionesSinTecla()
	{
		var faltan = new List<string>();
		foreach (var (nombre, accion) in TeclasConfigurables)
		{
			if (!InputMap.HasAction(accion)) continue;
			bool tieneTecla = false;
			foreach (InputEvent evento in InputMap.ActionGetEvents(accion))
				if (evento is InputEventKey) tieneTecla = true;
			if (!tieneTecla) faltan.Add(nombre);
		}
		return faltan;
	}

	// Teclas guardadas en el archivo: se aplican todas y después se revisan, para que un archivo editado
	// a mano o de otra versión no deje teclas reservadas ni repetidas (esas vuelven a la predeterminada)
	private void CargarTeclas(ConfigFile archivo)
	{
		if (!archivo.HasSection("teclas")) return;
		var configurables = new HashSet<string>();
		foreach (var (_, accion) in TeclasConfigurables) configurables.Add(accion);
		foreach (string accion in archivo.GetSectionKeys("teclas"))
		{
			var tecla = (Key)archivo.GetValue("teclas", accion, 0).AsInt64();
			if (!configurables.Contains(accion) || !InputMap.HasAction(accion) || ReservadaPorSistema(tecla) != null) continue;
			_teclas[accion] = tecla;
			AplicarTecla(accion, tecla);
		}
		foreach (string accion in new List<string>(_teclas.Keys))
		{
			if (UsoDeTecla(_teclas[accion], accion) == null) continue;
			GD.PushWarning($"Configuracion: la tecla guardada para {accion} se repetía; vuelve a la predeterminada.");
			_teclas.Remove(accion);
			RestaurarAccion(accion);
		}
	}

	// Nombre de la acción que ya usa esa tecla (o null si está libre)
	public string UsoDeTecla(Key fisica, string exceptoAccion)
	{
		if (TeclasReservadas.TryGetValue(fisica, out string reservada)) return reservada;
		foreach (StringName accion in InputMap.GetActions())
		{
			string nombre = accion.ToString();
			if (nombre == exceptoAccion || nombre.StartsWith("ui_")) continue;
			foreach (InputEvent evento in InputMap.ActionGetEvents(accion))
			{
				if (evento is InputEventKey tecla && (tecla.PhysicalKeycode == fisica || tecla.Keycode == fisica))
					return NombreAccion(nombre);
			}
		}
		return null;
	}

	public static string NombreAccion(string accion)
	{
		foreach (var (nombre, a) in TeclasConfigurables)
			if (a == accion) return nombre;
		return NombresAcciones.TryGetValue(accion, out string otro) ? otro : accion;
	}

	// Vuelve todo a los valores de fábrica (Opciones > Restaurar predeterminados)
	public void RestaurarPredeterminados()
	{
		foreach (string bus in Buses) FijarVolumen(bus, bus == "Master" ? 0.7f : 1f);
		PantallaCompleta = true;
		AgacharseAlternar = false;
		SensibilidadRaton = 1f;
		AyudasVisuales = true;
		RestaurarTeclas();
		Guardar();
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
		CargarTeclas(archivo);
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

	// Si el jugador cierra la ventana con un menú abierto, lo que cambió se guarda igual
	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest) Guardar();
	}

	public override void _Process(double delta)
	{
		_contadorFps.Text = Engine.GetFramesPerSecond() + " FPS";
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
		foreach (var (accion, tecla) in _teclas)
			archivo.SetValue("teclas", accion, (long)tecla);

		Error error = archivo.Save(RutaConfiguracion);
		if (error != Error.Ok)
		{
			GD.PushWarning("Configuracion: no se pudo guardar (" + error + ").");
		}
	}
}
