using Godot;
using System;

// Bucle de ambiente de una zona. Al ser 3D, se mezcla solo al cruzar de una zona a otra.
// El volumen de la escena se suma a la ganancia que iguala el archivo con el resto de sonidos.
// Un sonido que no es bucle (un búho lejano) se repite solo, con una pausa al azar entre una vez y otra.
public partial class SonidoAmbiente : AudioStreamPlayer3D
{
	[Export] public Sonidos.Tipo Tipo = Sonidos.Tipo.TonoSala;
	// Segundos de silencio entre repeticiones (solo para sonidos que no son bucle)
	[Export] public float PausaMinima = 12f;
	[Export] public float PausaMaxima = 30f;

	private double _espera;

	public override void _Ready()
	{
		Stream = Sonidos.Obtener(Tipo);
		VolumeDb += Sonidos.VolumenDe(Tipo);
		Bus = Sonidos.BusAmbiente;
		if (!Sonidos.EsBucle(Tipo))
		{
			_espera = GD.RandRange(PausaMinima * 0.3, PausaMaxima);
			return;
		}
		// Que todos los bucles no arranquen sincronizados en el mismo punto
		Play((float)GD.RandRange(0.0, Math.Max(0.0, Stream.GetLength() - 0.5)));
	}

	public override void _Process(double delta)
	{
		if (Sonidos.EsBucle(Tipo) || Playing) return;
		_espera -= delta;
		if (_espera > 0) return;
		_espera = GD.RandRange(PausaMinima, PausaMaxima);
		Play();
	}

	// Suelta el bucle al salir para que el servidor de audio no lo retenga al cerrar el juego
	public override void _ExitTree()
	{
		Stop();
		Stream = null;
	}
}
