using Godot;
using System;

// Interacción genérica (hablar con un personaje, acusar...). No decide qué pasa:
// emite Usado y el guion del caso responde.
public partial class PuntoInteraccion : StaticBody3D, IInteractuable
{
	[Signal] public delegate void UsadoEventHandler();

	[Export] public string Accion = "Hablar";

	public string TextoAccion => Accion;

	public virtual void Interactuar(Jugador jugador)
	{
		EmitSignal(SignalName.Usado);
	}

	// Activa o desactiva la interacción sin ocultar el objeto.
	public void Habilitar(bool habilitado, uint capa)
	{
		CollisionLayer = habilitado ? capa : 0;
	}
}
