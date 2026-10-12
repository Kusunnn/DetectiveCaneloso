using Godot;
using System;

// Música de fondo en bucle (bus Musica). Intensidad 0–1: más volumen y más brillo (abre el filtro
// paso bajo del bus) a medida que el caso avanza. Quien la usa decide la intensidad.
public partial class MusicaFondo : AudioStreamPlayer
{
	[Export] public float VolumenMinimoDb = -9f;
	[Export] public float VolumenMaximoDb = -3f;
	[Export] public float CorteMinimoHz = 900f;
	[Export] public float CorteMaximoHz = 9000f;

	private AudioEffectLowPassFilter _filtro;
	private Tween _transicion;
	private float _intensidad;

	public float Intensidad
	{
		get => _intensidad;
		set
		{
			_intensidad = Mathf.Clamp(value, 0f, 1f);
			_transicion?.Kill();
			_transicion = CreateTween().SetParallel();
			_transicion.TweenProperty(this, "volume_db", Mathf.Lerp(VolumenMinimoDb, VolumenMaximoDb, _intensidad), 3.0);
			if (_filtro != null)
				_transicion.TweenProperty(_filtro, "cutoff_hz", Mathf.Lerp(CorteMinimoHz, CorteMaximoHz, _intensidad), 3.0);
		}
	}

	public override void _Ready()
	{
		Bus = Sonidos.BusMusica;
		int bus = AudioServer.GetBusIndex(Sonidos.BusMusica);
		if (bus >= 0)
		{
			for (int i = 0; i < AudioServer.GetBusEffectCount(bus); i++)
				if (AudioServer.GetBusEffect(bus, i) is AudioEffectLowPassFilter filtro) _filtro = filtro;
		}
		if (Stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
		VolumeDb = -40f;
		if (_filtro != null) _filtro.CutoffHz = CorteMinimoHz;
		Play();
		// Entra con un fundido suave para que nunca sorprenda al empezar
		Intensidad = 0f;
	}

	public override void _ExitTree()
	{
		_transicion?.Kill();
		Stop();
		Stream = null;
	}
}
