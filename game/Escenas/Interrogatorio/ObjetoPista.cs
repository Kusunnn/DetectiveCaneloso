using Godot;
using System;

// Objeto de la sala que contiene una pista. El Jugador lo detecta con el raycast.
public partial class ObjetoPista : StaticBody3D
{
	[Export] public string Titulo = "Pista";
	[Export(PropertyHint.MultilineText)] public string Descripcion = "";

	public bool Encontrada { get; private set; } = false;

	// Devuelve true solo la primera vez que se inspecciona
	public bool Inspeccionar()
	{
		if (Encontrada) return false;
		Encontrada = true;
		return true;
	}
}
