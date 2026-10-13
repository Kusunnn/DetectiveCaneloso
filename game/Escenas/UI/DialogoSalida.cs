using Godot;
using System;
using System.Threading.Tasks;

// Advertencia al salir con progreso sin guardar, con el estilo del HUD (papel, máquina de escribir, ámbar):
// [Guardar y salir] [Salir sin guardar] [Cancelar]. El foco empieza en "Guardar y salir"
// y Esc (o B en el control) cancela, para que perder progreso nunca sea lo fácil.
public partial class DialogoSalida : Control
{
	public enum Respuesta { GuardarYSalir, SalirSinGuardar, Cancelar }

	private static readonly Color Ambar = new Color("efca08");
	private static readonly Color Tinta = new Color("20272b");
	private static readonly Color Papel = new Color(0.84f, 0.81f, 0.72f);

	private Label _detalle;
	private Button _guardar, _salir, _cancelar;
	private TaskCompletionSource<Respuesta> _respuesta;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Hide();

		var mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var titulos = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf") };
		titulos.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };

		var oscurecer = new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = MouseFilterEnum.Stop };
		oscurecer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(oscurecer);
		var centro = new CenterContainer();
		centro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(centro);

		var fondo = new StyleBoxFlat { BgColor = Papel, ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 10, ShadowOffset = new Vector2(4, 6) };
		fondo.SetContentMarginAll(30);
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(640, 0) };
		panel.AddThemeStyleboxOverride("panel", fondo);
		centro.AddChild(panel);
		var columna = new VBoxContainer();
		columna.AddThemeConstantOverride("separation", 14);
		panel.AddChild(columna);

		var titulo = new Label { Text = "¿SALIR SIN GUARDAR?" };
		titulo.AddThemeFontOverride("font", titulos);
		titulo.AddThemeFontSizeOverride("font_size", 40);
		titulo.AddThemeColorOverride("font_color", Tinta);
		columna.AddChild(titulo);
		columna.AddChild(new ColorRect { Color = Ambar, CustomMinimumSize = new Vector2(150, 5), SizeFlagsHorizontal = SizeFlags.ShrinkBegin });

		var mensaje = new Label
		{
			Text = "Tienes progreso sin guardar. Si sales ahora, se perderá lo que hiciste desde el último guardado.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		mensaje.AddThemeFontOverride("font", mono);
		mensaje.AddThemeFontSizeOverride("font_size", 20);
		mensaje.AddThemeColorOverride("font_color", Tinta);
		columna.AddChild(mensaje);
		_detalle = new Label();
		_detalle.AddThemeFontOverride("font", mono);
		_detalle.AddThemeFontSizeOverride("font_size", 17);
		_detalle.AddThemeColorOverride("font_color", new Color(Tinta, 0.65f));
		columna.AddChild(_detalle);

		var botones = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		botones.AddThemeConstantOverride("separation", 12);
		columna.AddChild(botones);
		_guardar = Boton("Guardar y salir", mono, principal: true);
		_salir = Boton("Salir sin guardar", mono, principal: false);
		_cancelar = Boton("Cancelar", mono, principal: false);
		botones.AddChild(_salir);
		botones.AddChild(_cancelar);
		botones.AddChild(_guardar);
		_guardar.Pressed += () => Responder(Respuesta.GuardarYSalir);
		_salir.Pressed += () => Responder(Respuesta.SalirSinGuardar);
		_cancelar.Pressed += () => Responder(Respuesta.Cancelar);
		UtilidadesUI.ConectarFocoConRaton(this);
		UtilidadesUI.EncadenarFoco(new Control[] { _guardar, _cancelar, _salir });
		// Izquierda/derecha también recorren los botones (control y teclado)
		_guardar.FocusNeighborLeft = _guardar.GetPathTo(_cancelar);
		_cancelar.FocusNeighborLeft = _cancelar.GetPathTo(_salir);
		_cancelar.FocusNeighborRight = _cancelar.GetPathTo(_guardar);
		_salir.FocusNeighborRight = _salir.GetPathTo(_cancelar);
	}

	private Button Boton(string texto, Font fuente, bool principal)
	{
		var boton = new Button { Text = texto, CustomMinimumSize = new Vector2(0, 46) };
		boton.AddThemeFontOverride("font", fuente);
		boton.AddThemeFontSizeOverride("font_size", 18);
		var normal = new StyleBoxFlat { BgColor = principal ? Tinta : new Color(0, 0, 0, 0), BorderColor = Tinta };
		normal.SetBorderWidthAll(2);
		normal.SetContentMarginAll(10);
		var foco = new StyleBoxFlat { BgColor = Ambar, BorderColor = Tinta };
		foco.SetBorderWidthAll(3);
		foco.SetContentMarginAll(10);
		boton.AddThemeStyleboxOverride("normal", normal);
		boton.AddThemeStyleboxOverride("hover", foco);
		boton.AddThemeStyleboxOverride("focus", foco);
		boton.AddThemeStyleboxOverride("pressed", foco);
		boton.AddThemeColorOverride("font_color", principal ? Papel : Tinta);
		boton.AddThemeColorOverride("font_hover_color", Tinta);
		boton.AddThemeColorOverride("font_focus_color", Tinta);
		boton.AddThemeColorOverride("font_pressed_color", Tinta);
		return boton;
	}

	public Task<Respuesta> Preguntar(string ultimoGuardado)
	{
		_detalle.Text = ultimoGuardado;
		_respuesta = new TaskCompletionSource<Respuesta>();
		Show();
		_guardar.GrabFocus();
		// Otra vez al siguiente cuadro: que un botón de abajo no se quede con el foco por el cursor
		_guardar.CallDeferred(Control.MethodName.GrabFocus);
		return _respuesta.Task;
	}

	public bool Abierto => Visible;

	private void Responder(Respuesta r)
	{
		Hide();
		var t = _respuesta;
		_respuesta = null;
		t?.TrySetResult(r);
	}

	public override void _Input(InputEvent evento)
	{
		if (!Visible) return;
		if (evento.IsActionPressed("ui_cancel") || evento.IsActionPressed("pausa"))
		{
			GetViewport().SetInputAsHandled();
			Responder(Respuesta.Cancelar);
		}
	}
}
