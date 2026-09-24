using Godot;
using System;
using System.Threading.Tasks;

// Diálogo de confirmación reutilizable para acciones destructivas (CA-03).
// Uso:  if (await _dialogo.Preguntar("¿Salir sin guardar?", "Salir")) { ... }
// El foco empieza en "Cancelar" y Esc también cancela, para que lo seguro sea lo fácil.
public partial class DialogoConfirmacion : Control
{
	private Label _mensaje;
	private Button _botonConfirmar;
	private Button _botonCancelar;

	private Control _focoAnterior;
	private TaskCompletionSource<bool> _respuesta;

	public override void _Ready()
	{
		_mensaje = GetNode<Label>("%Mensaje");
		_botonConfirmar = GetNode<Button>("%BotonConfirmar");
		_botonCancelar = GetNode<Button>("%BotonCancelar");

		Hide();
		_botonConfirmar.Pressed += () => Responder(true);
		_botonCancelar.Pressed += () => Responder(false);
		AtraparFoco();
		UtilidadesUI.ConectarFocoConRaton(this);
	}

	public Task<bool> Preguntar(string mensaje, string textoConfirmar = "Sí", string textoCancelar = "Cancelar")
	{
		_mensaje.Text = mensaje;
		_botonConfirmar.Text = textoConfirmar;
		_botonCancelar.Text = textoCancelar;
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
