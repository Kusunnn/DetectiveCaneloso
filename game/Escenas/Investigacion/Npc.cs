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

	// Si tiene un AnimadorPersonaje, sus animaciones ya respiran y reaccionan: el Npc solo le pasa el ánimo
	public AnimadorPersonaje Animador { get; private set; }

	public override void _Ready()
	{
		if (Cuerpo != null) _escalaBase = Cuerpo.Scale;
		_fase = (float)GD.RandRange(0.0, Mathf.Tau); // Que no respiren todos a la vez
		foreach (Node hijo in GetChildren())
			if (hijo is AnimadorPersonaje animador) Animador = animador;
	}

	// Ánimo: -2 cerrado, -1 nervioso, 0 tranquilo, 1+ cooperativo. Los nervios aceleran la respiración.
	private float _velocidadRespiracion = 2.2f;

	public void FijarAnimo(int animo)
	{
		_velocidadRespiracion = animo <= -1 ? 4.5f : 2.2f;
		Respiracion = animo <= -1 ? 0.022f : 0.012f;
		Animador?.FijarAnimo(animo);
	}

	// Mientras dice su línea gesticula (si tiene animaciones)
	public void Hablar(bool hablando) => Animador?.Hablar(hablando);

	// Negar, asentir...: una animación de una vez (si tiene animaciones)
	public void Gesto(string animacion) => Animador?.Gesto(animacion);

	public override void _Process(double delta)
	{
		if (Cuerpo == null || Animador != null) return;
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
		// Gira el personaje entero: si el Npc es solo su colisión (hermano del modelo, como en el Caso 1),
		// el que se voltea es el padre de los dos; si no, el propio Npc (HU-9.5 CA-03)
		Node3D quien = this;
		if (Cuerpo != null && !IsAncestorOf(Cuerpo) && Cuerpo.GetParent() is Node3D padre) quien = padre;
		else if (Cuerpo == null && GetParent() is Node3D personaje && personaje.HasNode("Modelo")) quien = personaje;
		Vector3 direccion = punto - quien.GlobalPosition;
		direccion.Y = 0;
		if (direccion.LengthSquared() < 0.01f) return;
		// El modelo mira hacia su +Z local
		float destino = Mathf.Atan2(direccion.X, direccion.Z);
		quien.CreateTween().TweenProperty(quien, "global_rotation:y", destino, 0.35)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}
}
