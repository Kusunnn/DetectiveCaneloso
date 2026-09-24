using Godot;
using System;

// Menú de pausa (CA-02): reanudar, códice de reglas, opciones y volver al menú de inicio.
// Basta con instanciarlo en cualquier escena de juego. Se abre y cierra con la acción
// "pausa" (Esc / Start) y sigue funcionando con el árbol pausado (process_mode = Always).
public partial class MenuPausa : CanvasLayer
{
	private Control _raiz;
	private Control _pantallaPrincipal;
	private Button _botonReanudar;
	private Button _botonCodice;
	private Button _botonOpciones;
	private Button _botonMenuInicio;
	private PanelMenu _codiceReglas;
	private PanelMenu _menuOpciones;
	private DialogoConfirmacion _dialogoConfirmacion;

	// Botón que abrió el panel actual, para devolverle el foco al cerrarlo.
	private Control _focoAlVolver;
	// El juego puede tener el ratón capturado; se muestra durante la pausa y se restaura al salir.
	private Input.MouseModeEnum _modoRatonAnterior;

	public override void _Ready()
	{
		_raiz = GetNode<Control>("%Raiz");
		_pantallaPrincipal = GetNode<Control>("%PantallaPrincipal");
		_botonReanudar = GetNode<Button>("%BotonReanudar");
		_botonCodice = GetNode<Button>("%BotonCodice");
		_botonOpciones = GetNode<Button>("%BotonOpciones");
		_botonMenuInicio = GetNode<Button>("%BotonMenuInicio");
		_codiceReglas = GetNode<PanelMenu>("%CodiceReglas");
		_menuOpciones = GetNode<PanelMenu>("%MenuOpciones");
		_dialogoConfirmacion = GetNode<DialogoConfirmacion>("%DialogoConfirmacion");

		_raiz.Hide();
		_botonReanudar.Pressed += Reanudar;
		_botonCodice.Pressed += () => AbrirPanel(_codiceReglas, _botonCodice);
		_botonOpciones.Pressed += () => AbrirPanel(_menuOpciones, _botonOpciones);
		_botonMenuInicio.Pressed += AlPresionarMenuInicio;
		_codiceReglas.Cerrado += AlCerrarPanel;
		_menuOpciones.Cerrado += AlCerrarPanel;
		UtilidadesUI.ConectarFocoConRaton(_raiz);
		UtilidadesUI.EncadenarFoco(new Control[] { _botonReanudar, _botonCodice, _botonOpciones, _botonMenuInicio });
	}

	// Los paneles y el diálogo de confirmación son hijos, así que reciben Esc antes
	// que este nodo; aquí solo llega cuando está visible la lista principal.
	public override void _UnhandledInput(InputEvent evento)
	{
		if (evento.IsActionPressed("pausa") || (_raiz.Visible && evento.IsActionPressed("ui_cancel")))
		{
			GetViewport().SetInputAsHandled();
			if (_raiz.Visible)
			{
				Reanudar();
			}
			else
			{
				Pausar();
			}
		}
	}

	public void Pausar()
	{
		GetTree().Paused = true;
		_modoRatonAnterior = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_raiz.Show();
		_pantallaPrincipal.Show();
		_botonReanudar.GrabFocus();
	}

	public void Reanudar()
	{
		_raiz.Hide();
		Input.MouseMode = _modoRatonAnterior;
		GetTree().Paused = false;
	}

	private void AbrirPanel(PanelMenu panel, Control abiertoDesde)
	{
		_focoAlVolver = abiertoDesde;
		_pantallaPrincipal.Hide();
		panel.Abrir();
	}

	private void AlCerrarPanel()
	{
		_pantallaPrincipal.Show();
		_focoAlVolver.GrabFocus();
	}

	private async void AlPresionarMenuInicio()
	{
		if (GestorPartida.Instancia.HayCambiosSinGuardar)
		{
			bool confirmado = await _dialogoConfirmacion.Preguntar(
				"Tienes progreso sin guardar.\n¿Volver al menú de inicio sin guardar?",
				"Salir sin guardar");
			if (!confirmado) return;
		}
		GestorPartida.Instancia.IrAlMenuInicio();
	}
}
