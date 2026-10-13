using Godot;
using System;

// Menú de pausa (CA-02): reanudar, guardar partida, códice de reglas, opciones, volver al menú de inicio
// y salir del juego. Basta con instanciarlo en cualquier escena de juego. Se abre y cierra con la acción
// "pausa" (Esc / Start) y sigue funcionando con el árbol pausado (process_mode = Always).
public partial class MenuPausa : CanvasLayer
{
	private Control _raiz;
	private Control _pantallaPrincipal;
	private Button _botonReanudar;
	private Button _botonGuardar;
	private Button _botonCodice;
	private Button _botonOpciones;
	private Button _botonMenuInicio;
	private Button _botonSalirJuego;
	private PanelMenu _codiceReglas;
	private PanelMenu _menuOpciones;
	private PanelRanuras _panelRanuras;
	private DialogoConfirmacion _dialogoConfirmacion;

	// Botón que abrió el panel actual, para devolverle el foco al cerrarlo.
	private Control _focoAlVolver;
	// El juego puede tener el ratón capturado; se muestra durante la pausa y se restaura al salir.
	private Input.MouseModeEnum _modoRatonAnterior;

	public bool Abierto => _raiz.Visible;

	public override void _Ready()
	{
		_raiz = GetNode<Control>("%Raiz");
		_pantallaPrincipal = GetNode<Control>("%PantallaPrincipal");
		_botonReanudar = GetNode<Button>("%BotonReanudar");
		_botonGuardar = GetNode<Button>("%BotonGuardar");
		_botonCodice = GetNode<Button>("%BotonCodice");
		_botonOpciones = GetNode<Button>("%BotonOpciones");
		_botonMenuInicio = GetNode<Button>("%BotonMenuInicio");
		_botonSalirJuego = GetNode<Button>("%BotonSalirJuego");
		_codiceReglas = GetNode<PanelMenu>("%CodiceReglas");
		_menuOpciones = GetNode<PanelMenu>("%MenuOpciones");
		_dialogoConfirmacion = GetNode<DialogoConfirmacion>("%DialogoConfirmacion");

		// Ranuras de guardado: debajo del diálogo de confirmación para que este quede encima
		_panelRanuras = new PanelRanuras { Confirmar = (mensaje, boton) => _dialogoConfirmacion.Preguntar(mensaje, boton) };
		_raiz.AddChild(_panelRanuras);
		_raiz.MoveChild(_panelRanuras, _dialogoConfirmacion.GetIndex());

		_raiz.Hide();
		_botonReanudar.Pressed += Reanudar;
		_botonGuardar.Pressed += () => AbrirRanuras();
		_botonCodice.Pressed += () => AbrirPanel(_codiceReglas, _botonCodice);
		_botonOpciones.Pressed += () => AbrirPanel(_menuOpciones, _botonOpciones);
		_botonMenuInicio.Pressed += () => _ = GestorPartida.Instancia.VolverAlMenuInicio();
		_botonSalirJuego.Pressed += () => _ = GestorPartida.Instancia.SalirDelJuego();
		_codiceReglas.Cerrado += AlCerrarPanel;
		_menuOpciones.Cerrado += AlCerrarPanel;
		_panelRanuras.Cerrado += AlCerrarPanel;
		UtilidadesUI.ConectarFocoConRaton(_raiz);
		UtilidadesUI.EncadenarFoco(new Control[] { _botonReanudar, _botonGuardar, _botonCodice, _botonOpciones, _botonMenuInicio, _botonSalirJuego });
	}

	// Los paneles y el diálogo de confirmación son hijos, así que reciben Esc antes
	// que este nodo; aquí solo llega cuando está visible la lista principal.
	public override void _UnhandledInput(InputEvent evento)
	{
		if (_panelRanuras.Visible) return;
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
		// La miniatura de la ranura se toma antes de que aparezca el menú
		GestorPartida.Instancia?.PrepararMiniatura();
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

	private void AbrirRanuras()
	{
		// Al repetir un capítulo resuelto la partida principal no se toca
		if (GestorPartida.Instancia.Rejugando)
		{
			GestorPartida.Instancia.MostrarAviso("Repetición: no se guarda en tus ranuras", sello: false);
			return;
		}
		// No se guarda a mitad de un diálogo o una animación
		if (!GestorPartida.Instancia.PuedeGuardar)
		{
			GestorPartida.Instancia.MostrarAviso("No puedes guardar ahora", sello: false);
			return;
		}
		_focoAlVolver = _botonGuardar;
		_pantallaPrincipal.Hide();
		_panelRanuras.Abrir();
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
}
