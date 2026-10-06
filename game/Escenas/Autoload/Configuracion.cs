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

	private bool _pantallaCompleta = false;
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
		if (!hayArchivo) return; // Primera vez: valores por defecto

		PantallaCompleta = archivo.GetValue("video", "pantalla_completa", PantallaCompleta).AsBool();
		AgacharseAlternar = archivo.GetValue("controles", "agacharse_alternar", AgacharseAlternar).AsBool();
		SensibilidadRaton = archivo.GetValue("controles", "sensibilidad_raton", SensibilidadRaton).AsSingle();
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
		if (evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && tecla.Keycode == Key.F3)
		{
			_contadorFps.Visible = !_contadorFps.Visible;
			SetProcess(_contadorFps.Visible);
		}
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
		archivo.SetValue("controles", "agacharse_alternar", AgacharseAlternar);
		archivo.SetValue("controles", "sensibilidad_raton", SensibilidadRaton);

		Error error = archivo.Save(RutaConfiguracion);
		if (error != Error.Ok)
		{
			GD.PushWarning("Configuracion: no se pudo guardar (" + error + ").");
		}
	}
}
