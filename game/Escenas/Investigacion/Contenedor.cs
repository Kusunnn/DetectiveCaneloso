using Godot;
using System;

// Casillero, cajón, mochila o caja que se abre y se vuelve a cerrar con [E].
// Estados: cerrado ⇄ abierto; "revisado" se conserva aunque se cierre (marca de tiza).
// Puede pedir un objeto (llave, combinación) que se consigue en otra zona y puede dar uno la primera vez.
public partial class Contenedor : StaticBody3D, IInteractuable
{
	public enum Movimiento { Girar, Deslizar }

	[Signal] public delegate void AbiertoEventHandler();
	[Signal] public delegate void IntentoBloqueadoEventHandler();

	[Export] public string Nombre = "Casillero";
	// Lo que piensa el detective al abrirlo la primera vez. Cada contenedor debe tener uno distinto.
	[Export(PropertyHint.MultilineText)] public string MensajeAlAbrir = "Vacío.";
	// Id del objeto que hace falta para abrirlo (p. ej. "combinacion_porky"); vacío = se abre libre.
	[Export] public string RequiereObjeto = "";
	[Export(PropertyHint.MultilineText)] public string MensajeBloqueado = "Está cerrado con candado.";
	// Id del objeto que se obtiene la primera vez que se abre; vacío = nada.
	[Export] public string ObjetoQueDa = "";
	// Qué se mueve: una puerta o tapa que gira, o un cajón que sale.
	[Export] public Node3D Parte;
	[Export] public Movimiento TipoMovimiento = Movimiento.Girar;
	[Export] public Vector3 EjeGiro = Vector3.Up;
	[Export] public float AnguloApertura = 100f;
	[Export] public Vector3 Desplazamiento = new Vector3(0.35f, 0, 0);
	[Export] public Sonidos.Tipo SonidoAbrir = Sonidos.Tipo.AbrirMetal;
	// Marca de "ya revisado" (aparece la primera vez que se abre y se queda).
	[Export] public Node3D MarcaRevisado;
	// true: solo se puede usar agachado (p. ej. algo escondido debajo de un banco)
	[Export] public bool SoloAgachado = false;

	public bool EstaAbierto { get; private set; } = false;
	public bool Revisado { get; private set; } = false;
	// Mientras dura la animación no se acepta otra pulsación
	public bool Animando { get; private set; } = false;

	private Vector3 _rotacionCerrada;
	private Vector3 _posicionCerrada;

	public string TextoAccion
	{
		get
		{
			if (Animando) return "";
			string nombre = char.ToLower(Nombre[0]) + Nombre.Substring(1);
			string marca = Revisado ? " (revisado)" : "";
			return (EstaAbierto ? "Cerrar " : "Abrir ") + nombre + marca;
		}
	}

	public override void _Ready()
	{
		if (MarcaRevisado != null) MarcaRevisado.Visible = false;
		if (Parte != null)
		{
			_rotacionCerrada = Parte.Rotation;
			_posicionCerrada = Parte.Position;
		}
	}

	public void Interactuar(Jugador jugador)
	{
		if (Animando) return;
		if (EstaAbierto)
		{
			Mover(false);
			return;
		}
		if (!string.IsNullOrEmpty(RequiereObjeto) && !jugador.TieneObjeto(RequiereObjeto))
		{
			Sonidos.Reproducir(this, Sonidos.Tipo.Bloqueado, GlobalPosition);
			jugador.MostrarPensamiento(MensajeBloqueado, 5.0);
			EmitSignal(SignalName.IntentoBloqueado);
			return;
		}

		bool primeraVez = !Revisado;
		Mover(true);
		if (!primeraVez) return; // Reabrir no repite el mensaje ni entrega nada otra vez

		if (string.IsNullOrEmpty(ObjetoQueDa))
			jugador.MostrarPensamiento(MensajeAlAbrir, 4.5);
		else
			jugador.DarObjeto(ObjetoQueDa, MensajeAlAbrir);
		EmitSignal(SignalName.Abierto);
	}

	private void Mover(bool abrir)
	{
		EstaAbierto = abrir;
		Animando = true;
		// Mismo sonido al cerrar, algo más grave
		Sonidos.Reproducir(this, SonidoAbrir, GlobalPosition, 0f, abrir ? 1f : 0.8f);

		var animacion = CreateTween().SetTrans(abrir ? Tween.TransitionType.Back : Tween.TransitionType.Sine)
			.SetEase(abrir ? Tween.EaseType.Out : Tween.EaseType.InOut);
		if (Parte != null)
		{
			if (TipoMovimiento == Movimiento.Girar)
			{
				Vector3 destino = abrir ? _rotacionCerrada + EjeGiro * Mathf.DegToRad(AnguloApertura) : _rotacionCerrada;
				animacion.TweenProperty(Parte, "rotation", destino, abrir ? 0.55 : 0.4);
			}
			else
			{
				Vector3 destino = abrir ? _posicionCerrada + Desplazamiento : _posicionCerrada;
				animacion.TweenProperty(Parte, "position", destino, abrir ? 0.4 : 0.3);
			}
		}
		else
		{
			animacion.TweenInterval(0.3);
		}
		animacion.TweenCallback(Callable.From(() =>
		{
			Animando = false;
			if (abrir && !Revisado)
			{
				Revisado = true;
				if (MarcaRevisado != null) MarcaRevisado.Visible = true;
			}
		}));
	}
}
