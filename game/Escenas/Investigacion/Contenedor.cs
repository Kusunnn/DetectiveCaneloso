using Godot;
using System;

// Casillero, cajón, mochila o caja que se puede abrir y revisar.
// Estados: Cerrado → Abierto (animación, sonido y pensamiento) → Revisado (queda abierto con una marca de tiza).
// Puede pedir un objeto (llave, combinación) que se consigue en otra zona, y puede dar uno al abrirse.
public partial class Contenedor : StaticBody3D, IInteractuable
{
	public enum Estado { Cerrado, Abierto, Revisado }
	public enum Movimiento { Girar, Deslizar }

	[Signal] public delegate void AbiertoEventHandler();
	[Signal] public delegate void IntentoBloqueadoEventHandler();

	[Export] public string Nombre = "Casillero";
	// Lo que piensa el detective al abrirlo. Cada contenedor debe tener uno distinto.
	[Export(PropertyHint.MultilineText)] public string MensajeAlAbrir = "Vacío.";
	// Id del objeto que hace falta para abrirlo (p. ej. "combinacion_porky"); vacío = se abre libre.
	[Export] public string RequiereObjeto = "";
	[Export(PropertyHint.MultilineText)] public string MensajeBloqueado = "Está cerrado con candado.";
	// Id del objeto que se obtiene al abrirlo (p. ej. una nota con una combinación); vacío = nada.
	[Export] public string ObjetoQueDa = "";
	// Qué se mueve al abrir: una puerta o tapa que gira, o un cajón que sale.
	[Export] public Node3D Parte;
	[Export] public Movimiento TipoMovimiento = Movimiento.Girar;
	[Export] public Vector3 EjeGiro = Vector3.Up;
	[Export] public float AnguloApertura = 100f;
	[Export] public Vector3 Desplazamiento = new Vector3(0.35f, 0, 0);
	[Export] public Sonidos.Tipo SonidoAbrir = Sonidos.Tipo.AbrirMetal;
	// Marca de "ya revisado" (se muestra al terminar de abrir).
	[Export] public Node3D MarcaRevisado;
	// true si dentro hay algo que recoger: al abrirse deja de bloquear el rayo de interacción.
	[Export] public bool LiberarAlAbrir = false;

	public Estado EstadoActual { get; private set; } = Estado.Cerrado;

	public string TextoAccion => EstadoActual == Estado.Cerrado ? "Abrir " + char.ToLower(Nombre[0]) + Nombre.Substring(1) : Nombre + " (revisado)";

	public override void _Ready()
	{
		if (MarcaRevisado != null) MarcaRevisado.Visible = false;
	}

	public void Interactuar(Jugador jugador)
	{
		if (EstadoActual != Estado.Cerrado)
		{
			jugador.MostrarPensamiento("Ya lo revisé. " + MensajeAlAbrir, 3.0);
			return;
		}
		if (!string.IsNullOrEmpty(RequiereObjeto) && !jugador.TieneObjeto(RequiereObjeto))
		{
			Sonidos.Reproducir(this, Sonidos.Tipo.Bloqueado, GlobalPosition);
			jugador.MostrarPensamiento(MensajeBloqueado, 5.0);
			EmitSignal(SignalName.IntentoBloqueado);
			return;
		}
		Abrir(jugador);
	}

	private void Abrir(Jugador jugador)
	{
		EstadoActual = Estado.Abierto;
		Sonidos.Reproducir(this, SonidoAbrir, GlobalPosition);
		if (LiberarAlAbrir) CollisionLayer = 0;

		var animacion = CreateTween().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		if (Parte != null)
		{
			if (TipoMovimiento == Movimiento.Girar)
				animacion.TweenProperty(Parte, "rotation", Parte.Rotation + EjeGiro * Mathf.DegToRad(AnguloApertura), 0.55);
			else
				animacion.TweenProperty(Parte, "position", Parte.Position + Desplazamiento, 0.4);
		}
		animacion.TweenCallback(Callable.From(() =>
		{
			EstadoActual = Estado.Revisado;
			if (MarcaRevisado != null) MarcaRevisado.Visible = true;
		})).SetDelay(0.6);

		if (string.IsNullOrEmpty(ObjetoQueDa))
			jugador.MostrarPensamiento(MensajeAlAbrir, 4.5);
		else
			jugador.DarObjeto(ObjetoQueDa, MensajeAlAbrir);
		EmitSignal(SignalName.Abierto);
	}
}
