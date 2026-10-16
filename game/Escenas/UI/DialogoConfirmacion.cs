using Godot;
using System;
using System.Threading.Tasks;

// Diálogo de confirmación reutilizable para acciones destructivas (CA-03).
// Diseño "02 · Confirmar salida": panel oscuro con "● ATENCIÓN", título, texto y dos botones.
// Uso:  if (await _dialogo.Preguntar("¿Salir sin guardar?", "Salir")) { ... }
// La primera línea del mensaje es el título; el resto, el texto pequeño de debajo.
// El foco empieza en "Cancelar" y Esc también cancela, para que lo seguro sea lo fácil.
public partial class DialogoConfirmacion : Control
{
	private HBoxContainer _marca;
	private Label _marcaTexto;
	private Label _titulo;
	private Label _detalle;
	private HBoxContainer _botones;
	private Button _botonCancelar;
	private Button _botonConfirmar;
	private Button _botonConfirmarPeligro;
	private Button _botonConfirmarNormal;

	private Control _focoAnterior;
	private TaskCompletionSource<bool> _respuesta;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Construir();
		Hide();
		UtilidadesUI.ConectarFocoConRaton(this);
	}

	private void Construir()
	{
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
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(1040, 0) };
		panel.AddThemeStyleboxOverride("panel", estilo);
		centro.AddChild(panel);

		var columna = EstiloMenus.Columna(56);
		panel.AddChild(columna);
		_marca = EstiloMenus.Marca("ATENCIÓN", EstiloMenus.Rojo);
		_marcaTexto = _marca.GetChild<Label>(1);
		columna.AddChild(_marca);

		var textos = EstiloMenus.Columna(20);
		_titulo = EstiloMenus.Etiqueta("", EstiloMenus.Display(800, 68, 0.02f), 68, EstiloMenus.TextoClaro);
		_titulo.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_titulo.CustomMinimumSize = new Vector2(864, 0);
		textos.AddChild(_titulo);
		_detalle = EstiloMenus.Etiqueta("", EstiloMenus.Mono(28), 28, EstiloMenus.GrisClaro);
		_detalle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_detalle.AddThemeConstantOverride("line_spacing", 14);
		textos.AddChild(_detalle);
		columna.AddChild(textos);
		columna.AddChild(EstiloMenus.Raya(0, 2, EstiloMenus.Linea));

		_botones = EstiloMenus.Fila(72);
		_botones.Alignment = BoxContainer.AlignmentMode.End;
		columna.AddChild(_botones);
		_botonCancelar = EstiloMenus.BotonTexto("CANCELAR", 48);
		_botonConfirmarPeligro = EstiloMenus.BotonPeligro("SÍ", 48);
		_botonConfirmarNormal = EstiloMenus.BotonPrincipal("SÍ", 48);
		_botones.AddChild(_botonCancelar);
		_botones.AddChild(_botonConfirmarPeligro);
		_botones.AddChild(_botonConfirmarNormal);
		_botonCancelar.Pressed += () => Responder(false);
		_botonConfirmarPeligro.Pressed += () => Responder(true);
		_botonConfirmarNormal.Pressed += () => Responder(true);
	}

	// peligro = false para preguntas que no destruyen nada (p. ej. omitir el tutorial):
	// la etiqueta dice "PREGUNTA" y el botón de confirmar es ámbar en vez de rojo.
	public Task<bool> Preguntar(string mensaje, string textoConfirmar = "Sí", string textoCancelar = "Cancelar", bool peligro = true)
	{
		int salto = mensaje.IndexOf('\n');
		_titulo.Text = salto < 0 ? mensaje : mensaje[..salto];
		_detalle.Text = salto < 0 ? "" : mensaje[(salto + 1)..];
		_detalle.Visible = _detalle.Text != "";

		_marcaTexto.Text = peligro ? "ATENCIÓN" : "PREGUNTA";
		_marcaTexto.AddThemeColorOverride("font_color", peligro ? EstiloMenus.Rojo : EstiloMenus.Gris);
		_botonConfirmar = peligro ? _botonConfirmarPeligro : _botonConfirmarNormal;
		_botonConfirmarPeligro.Visible = peligro;
		_botonConfirmarNormal.Visible = !peligro;
		_botonConfirmar.Text = textoConfirmar.ToUpper();
		_botonCancelar.Text = textoCancelar.ToUpper();
		AtraparFoco();

		_focoAnterior = GetViewport().GuiGetFocusOwner();
		_respuesta = new TaskCompletionSource<bool>();
		Show();
		_botonCancelar.GrabFocus();
		return _respuesta.Task;
	}

	private void Responder(bool aceptado)
	{
		Hide();
		if (IsInstanceValid(_focoAnterior) && _focoAnterior.IsVisibleInTree())
		{
			_focoAnterior.GrabFocus();
		}
		var respuesta = _respuesta;
		_respuesta = null;
		respuesta?.TrySetResult(aceptado);
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (Visible && evento.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			Responder(false);
		}
	}

	// Evita que el teclado salte a los botones del menú que queda detrás.
	private void AtraparFoco()
	{
		var rutaCancelar = _botonCancelar.GetPath();
		var rutaConfirmar = _botonConfirmar.GetPath();
		foreach (var boton in new[] { _botonCancelar, _botonConfirmar })
		{
			boton.FocusNeighborTop = boton.GetPath();
			boton.FocusNeighborBottom = boton.GetPath();
		}
		_botonCancelar.FocusNeighborLeft = rutaCancelar;
		_botonCancelar.FocusNeighborRight = rutaConfirmar;
		_botonConfirmar.FocusNeighborLeft = rutaCancelar;
		_botonConfirmar.FocusNeighborRight = rutaConfirmar;
		_botonCancelar.FocusNext = rutaConfirmar;
		_botonCancelar.FocusPrevious = rutaConfirmar;
		_botonConfirmar.FocusNext = rutaCancelar;
		_botonConfirmar.FocusPrevious = rutaCancelar;
	}
}
