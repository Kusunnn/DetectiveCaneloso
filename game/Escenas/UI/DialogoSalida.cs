using Godot;
using System;
using System.Threading.Tasks;

// Advertencia al salir con progreso sin guardar (diseño "02 · Confirmar salida"):
// [Cancelar] [Salir sin guardar] [Guardar y salir]. El foco empieza en "Guardar y salir"
// y Esc (o B en el control) cancela, para que perder progreso nunca sea lo fácil.
public partial class DialogoSalida : Control
{
	public enum Respuesta { GuardarYSalir, SalirSinGuardar, Cancelar }

	private Label _detalle;
	private Button _guardar, _salir, _cancelar;
	private TaskCompletionSource<Respuesta> _respuesta;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Hide();

		var oscurecer = new ColorRect { Color = new Color(0.012f, 0.016f, 0.02f, 0.85f), MouseFilter = MouseFilterEnum.Stop };
		oscurecer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(oscurecer);
		var centro = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(centro);

		var estilo = new StyleBoxFlat
		{
			BgColor = EstiloMenus.FondoPanel,
			BorderColor = new Color(1, 1, 1, 0.1f),
			ShadowColor = new Color(0, 0, 0, 0.6f), ShadowSize = 80, ShadowOffset = new Vector2(0, 60),
			ContentMarginLeft = 88, ContentMarginRight = 88, ContentMarginTop = 80, ContentMarginBottom = 64,
		};
		estilo.SetBorderWidthAll(2);
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(1200, 0) };
		panel.AddThemeStyleboxOverride("panel", estilo);
		centro.AddChild(panel);

		var columna = EstiloMenus.Columna(56);
		panel.AddChild(columna);
		columna.AddChild(EstiloMenus.Marca("ATENCIÓN", EstiloMenus.Rojo));

		var textos = EstiloMenus.Columna(20);
		var titulo = EstiloMenus.Etiqueta("Tienes progreso sin guardar.", EstiloMenus.Display(800, 68, 0.02f), 68, EstiloMenus.TextoClaro);
		textos.AddChild(titulo);
		var mensaje = EstiloMenus.Etiqueta("Si sales ahora, se perderá lo que hiciste desde el último guardado.", EstiloMenus.Mono(28), 28, EstiloMenus.GrisClaro);
		mensaje.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		mensaje.CustomMinimumSize = new Vector2(1024, 0);
		textos.AddChild(mensaje);
		_detalle = EstiloMenus.Etiqueta("", EstiloMenus.Mono(24), 24, EstiloMenus.GrisOscuro);
		textos.AddChild(_detalle);
		columna.AddChild(textos);
		columna.AddChild(EstiloMenus.Raya(0, 2, EstiloMenus.Linea));

		var botones = EstiloMenus.Fila(48);
		botones.Alignment = BoxContainer.AlignmentMode.End;
		columna.AddChild(botones);
		_cancelar = EstiloMenus.BotonTexto("CANCELAR", 40);
		_salir = EstiloMenus.BotonPeligro("SALIR SIN GUARDAR", 40);
		_guardar = EstiloMenus.BotonPrincipal("GUARDAR Y SALIR", 40);
		botones.AddChild(_cancelar);
		botones.AddChild(_salir);
		botones.AddChild(_guardar);
		_guardar.Pressed += () => Responder(Respuesta.GuardarYSalir);
		_salir.Pressed += () => Responder(Respuesta.SalirSinGuardar);
		_cancelar.Pressed += () => Responder(Respuesta.Cancelar);
		UtilidadesUI.ConectarFocoConRaton(this);
		UtilidadesUI.EncadenarFoco(new Control[] { _guardar, _cancelar, _salir });
		// Izquierda/derecha también recorren los botones (control y teclado)
		_guardar.FocusNeighborLeft = _guardar.GetPathTo(_salir);
		_salir.FocusNeighborLeft = _salir.GetPathTo(_cancelar);
		_salir.FocusNeighborRight = _salir.GetPathTo(_guardar);
		_cancelar.FocusNeighborRight = _cancelar.GetPathTo(_salir);
	}

	public Task<Respuesta> Preguntar(string ultimoGuardado)
	{
		_detalle.Text = ultimoGuardado;
		_detalle.Visible = !string.IsNullOrEmpty(ultimoGuardado);
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
