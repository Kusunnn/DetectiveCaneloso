using Godot;
using System;

// Bucle de ambiente de una zona. Al ser 3D, se mezcla solo al cruzar de una zona a otra.
// El volumen de la escena se suma a la ganancia que iguala el archivo con el resto de sonidos.
public partial class SonidoAmbiente : AudioStreamPlayer3D
{
	[Export] public Sonidos.Tipo Tipo = Sonidos.Tipo.TonoSala;

	public override void _Ready()
	{
		Stream = Sonidos.Obtener(Tipo);
		VolumeDb += Sonidos.VolumenDe(Tipo);
		Bus = Sonidos.BusAmbiente;
		// Que todos los bucles no arranquen sincronizados en el mismo punto
		Play((float)GD.RandRange(0.0, Math.Max(0.0, Stream.GetLength() - 0.5)));
	}

	// Suelta el bucle al salir para que el servidor de audio no lo retenga al cerrar el juego
	public override void _ExitTree()
	{
		Stop();
		Stream = null;
	}
}
