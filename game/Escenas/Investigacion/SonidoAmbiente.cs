using Godot;
using System;

// Bucle de ambiente de una zona. Al ser 3D, se mezcla solo al cruzar de una zona a otra.
public partial class SonidoAmbiente : AudioStreamPlayer3D
{
	[Export] public Sonidos.Tipo Tipo = Sonidos.Tipo.TonoSala;

	public override void _Ready()
	{
		Stream = Sonidos.Obtener(Tipo);
		Play();
	}
}
