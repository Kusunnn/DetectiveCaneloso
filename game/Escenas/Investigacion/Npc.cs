using Godot;
using System;

// Personaje con el que se habla: respira en reposo y se gira hacia el detective al hablar.
// La conversación la decide el caso al recibir Usado.
public partial class Npc : PuntoInteraccion
{
	// Nodo visual que respira y gira (el modelo); el cuerpo de colisión no se mueve.
	[Export] public Node3D Cuerpo;
	[Export] public float Respiracion = 0.012f;

	private Vector3 _escalaBase;
	private float _fase;

	public override void _Ready()
	{
		if (Cuerpo != null) _escalaBase = Cuerpo.Scale;
		_fase = (float)GD.RandRange(0.0, Mathf.Tau); // Que no respiren todos a la vez
	}

	// Ánimo: -2 cerrado, -1 nervioso, 0 tranquilo, 1+ cooperativo. Los nervios aceleran la respiración.
	private float _velocidadRespiracion = 2.2f;

	public void FijarAnimo(int animo)
	{
		_velocidadRespiracion = animo <= -1 ? 4.5f : 2.2f;
		Respiracion = animo <= -1 ? 0.022f : 0.012f;
	}

	public override void _Process(double delta)
	{
		if (Cuerpo == null) return;
		_fase += (float)delta * _velocidadRespiracion;
		float s = Mathf.Sin(_fase);
		// Respira: sube un poco en alto y se estrecha lo mismo, sin mover los pies
		Cuerpo.Scale = new Vector3(_escalaBase.X * (1f - s * Respiracion * 0.5f), _escalaBase.Y * (1f + s * Respiracion), _escalaBase.Z);
	}

	// Gira el cuerpo hacia el jugador antes de hablar
	public override void Interactuar(Jugador jugador)
	{
		MirarA(jugador.GlobalPosition);
		base.Interactuar(jugador);
	}

	public void MirarA(Vector3 punto)
	{
		Vector3 direccion = punto - GlobalPosition;
		direccion.Y = 0;
		if (direccion.LengthSquared() < 0.01f) return;
		// El modelo mira hacia su +Z local
		float destino = Mathf.Atan2(direccion.X, direccion.Z);
		CreateTween().TweenProperty(this, "global_rotation:y", destino, 0.35)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}
}
