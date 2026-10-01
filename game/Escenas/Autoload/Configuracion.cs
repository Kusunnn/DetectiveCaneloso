using Godot;
using System;

// Autoload: preferencias del jugador, guardadas en user://configuracion.cfg.
public partial class Configuracion : Node
{
	private const string RutaConfiguracion = "user://configuracion.cfg";

	public static Configuracion Instancia { get; private set; }

	private float _volumenGeneral = 1.0f;
	public float VolumenGeneral
	{
		get => _volumenGeneral;
		set
		{
			_volumenGeneral = Mathf.Clamp(value, 0.0f, 1.0f);
			AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(_volumenGeneral));
		}
	}

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

	public override void _EnterTree()
	{
		Instancia = this;
	}

	public override void _Ready()
	{
		var archivo = new ConfigFile();
		if (archivo.Load(RutaConfiguracion) != Error.Ok) return; // Primera vez: valores por defecto

		VolumenGeneral = archivo.GetValue("audio", "volumen_general", VolumenGeneral).AsSingle();
		PantallaCompleta = archivo.GetValue("video", "pantalla_completa", PantallaCompleta).AsBool();
		AgacharseAlternar = archivo.GetValue("controles", "agacharse_alternar", AgacharseAlternar).AsBool();
		SensibilidadRaton = archivo.GetValue("controles", "sensibilidad_raton", SensibilidadRaton).AsSingle();
	}

	public void Guardar()
	{
		var archivo = new ConfigFile();
		archivo.SetValue("audio", "volumen_general", VolumenGeneral);
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
