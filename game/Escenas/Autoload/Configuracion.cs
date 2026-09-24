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
	}

	public void Guardar()
	{
		var archivo = new ConfigFile();
		archivo.SetValue("audio", "volumen_general", VolumenGeneral);
		archivo.SetValue("video", "pantalla_completa", PantallaCompleta);

		Error error = archivo.Save(RutaConfiguracion);
		if (error != Error.Ok)
		{
			GD.PushWarning("Configuracion: no se pudo guardar (" + error + ").");
		}
	}
}
