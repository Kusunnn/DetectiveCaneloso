using Godot;
using System;

// Botón "invisible" de los menús del diseño: su contenido son etiquetas y se resalta (coral y un
// pequeño desplazamiento) al recibir el foco o el ratón. Teclado, ratón y control comparten el mismo
// resaltado porque UtilidadesUI pasa el foco al control que está bajo el ratón.
public partial class FilaResaltable : Button
{
	// Desplazamiento del contenido al resaltarse (el diseño usa 10 px a 960×540 en las opciones)
	public float Desplazamiento { get; set; } = 20f;

	protected Control Contenido { get; private set; }
	private Tween _tween;
	private bool _resaltada;

	public FilaResaltable()
	{
		Flat = true;
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		var vacio = new StyleBoxEmpty();
		foreach (string estado in new[] { "normal", "hover", "pressed", "focus", "disabled", "hover_pressed" })
			AddThemeStyleboxOverride(estado, vacio);
	}

	// El contenido se coloca a mano dentro del botón para poder desplazarlo sin mover la fila
	protected void PonerContenido(Control contenido)
	{
		Contenido = contenido;
		contenido.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(contenido);
		contenido.MinimumSizeChanged += Recolocar;
		Resized += Recolocar;
		Recolocar();
	}

	private void Recolocar()
	{
		if (Contenido == null) return;
		Vector2 minimo = Contenido.GetCombinedMinimumSize();
		CustomMinimumSize = new Vector2(minimo.X + Desplazamiento, minimo.Y);
		Contenido.Size = new Vector2(Mathf.Max(minimo.X, Size.X - Desplazamiento), Size.Y);
		Contenido.Position = new Vector2(_resaltada ? Desplazamiento : 0f, 0f);
	}

	public override void _Ready()
	{
		FocusEntered += Actualizar;
		FocusExited += Actualizar;
		MouseEntered += Actualizar;
		MouseExited += Actualizar;
		VisibilityChanged += Actualizar;
		Actualizar();
	}

	protected bool Resaltada => !Disabled && (HasFocus() || IsHovered());

	public void Actualizar()
	{
		bool resaltada = Resaltada;
		AplicarColores(resaltada);
		if (resaltada == _resaltada || Contenido == null) return;
		_resaltada = resaltada;
		if (!IsInsideTree()) return;
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(Contenido, "position:x", resaltada ? Desplazamiento : 0f, 0.18)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}

	// Cada fila decide qué partes cambian de color
	protected virtual void AplicarColores(bool resaltada) { }
}
