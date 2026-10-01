using Godot;
using System;

// Puerta o cajón que se abre girando su bisagra. Puede empezar bloqueada (zona cerrada).
public partial class Puerta : StaticBody3D, IInteractuable
{
	[Signal] public delegate void AbiertaEventHandler();

	[Export] public bool Bloqueada = false;
	[Export(PropertyHint.MultilineText)] public string MensajeBloqueada = "Está cerrada.";
	// Nodo que gira al abrir (la hoja cuelga de él).
	[Export] public Node3D Bisagra;
	[Export] public float AnguloApertura = 95.0f;

	public bool EstaAbierta { get; private set; } = false;

	public string TextoAccion => "Abrir";

	public void Interactuar(Jugador jugador)
	{
		if (EstaAbierta) return;
		if (Bloqueada)
		{
			Sonidos.Reproducir(this, Sonidos.Tipo.Bloqueado, GlobalPosition);
			jugador.MostrarPensamiento(MensajeBloqueada, 4.0);
			return;
		}
		Abrir();
	}

	public void Desbloquear()
	{
		Bloqueada = false;
	}

	public void Abrir()
	{
		EstaAbierta = true;
		CollisionLayer = 0; // Deja pasar al jugador y al rayo de interacción
		Sonidos.Reproducir(this, Sonidos.Tipo.AbrirMetal, GlobalPosition);
		if (Bisagra != null)
		{
			float destino = Bisagra.Rotation.Y + Mathf.DegToRad(AnguloApertura);
			CreateTween().TweenProperty(Bisagra, "rotation:y", destino, 0.6)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		}
		EmitSignal(SignalName.Abierta);
	}
}
