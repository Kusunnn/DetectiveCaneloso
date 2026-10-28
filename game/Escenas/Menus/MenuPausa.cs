using Godot;
using System;

// Menú de pausa (CA-02): reanudar, guardar partida, opciones, volver al menú de inicio
// y salir del juego. Diseño "01 · Pausa": título PAUSA en ámbar y opciones numeradas a la izquierda.
// Basta con instanciarlo en cualquier escena de juego. Se abre y cierra con la acción
// "pausa" (Esc / Start) y sigue funcionando con el árbol pausado (process_mode = Always).
public partial class MenuPausa : CanvasLayer
{
	private Control _raiz;
	private Control _pantallaPrincipal;
	private Button _botonReanudar;
	private Button _botonGuardar;
	private Button _botonOpciones;
	private Button _botonPistas;
	private PanelPistas _panelPistas;
	private Button _botonMenuInicio;
	private Button _botonSalirJuego;
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
		_menuOpciones = GetNode<PanelMenu>("%MenuOpciones");
		_dialogoConfirmacion = GetNode<DialogoConfirmacion>("%DialogoConfirmacion");
		ConstruirPantallaPrincipal();

		// Ranuras de guardado: debajo del diálogo de confirmación para que este quede encima
		_panelRanuras = new PanelRanuras { Confirmar = (mensaje, boton) => _dialogoConfirmacion.Preguntar(mensaje, boton) };
		_raiz.AddChild(_panelRanuras);
		_raiz.MoveChild(_panelRanuras, _dialogoConfirmacion.GetIndex());

		_panelPistas = new PanelPistas { Name = "PanelPistas" };
		_raiz.AddChild(_panelPistas);
		_panelPistas.Cerrado += AlCerrarPanel;
		_botonPistas.Pressed += () => AbrirPanel(_panelPistas, _botonPistas);

		_raiz.Hide();
		_botonReanudar.Pressed += Reanudar;
		_botonGuardar.Pressed += () => AbrirRanuras();
		_botonOpciones.Pressed += () => AbrirPanel(_menuOpciones, _botonOpciones);
		_botonMenuInicio.Pressed += () => _ = GestorPartida.Instancia.VolverAlMenuInicio();
		_botonSalirJuego.Pressed += () => _ = GestorPartida.Instancia.SalirDelJuego();
		_menuOpciones.Cerrado += AlCerrarPanel;
		_panelRanuras.Cerrado += AlCerrarPanel;
		UtilidadesUI.ConectarFocoConRaton(_raiz);
		UtilidadesUI.EncadenarFoco(new Control[] { _botonReanudar, _botonGuardar, _botonOpciones, _botonMenuInicio, _botonSalirJuego, _botonPistas });
	}

	private void ConstruirPantallaPrincipal()
	{
		_pantallaPrincipal = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
		_pantallaPrincipal.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_raiz.AddChild(_pantallaPrincipal);
		_raiz.MoveChild(_pantallaPrincipal, 0);
		// El juego queda detrás, oscurecido y con viñeta
		EstiloMenus.Fondo(_pantallaPrincipal, opaco: false, oscurecer: 0.72f);

		var columna = EstiloMenus.Columna(64);
		columna.AnchorTop = 0f;
		columna.AnchorBottom = 1f;
		columna.OffsetLeft = 128;
		columna.OffsetRight = 1100;
		columna.Alignment = BoxContainer.AlignmentMode.Center;
		_pantallaPrincipal.AddChild(columna);
		columna.AddChild(EstiloMenus.Titulo("PAUSA", 168, 28));

		var lista = EstiloMenus.Columna(8);
		columna.AddChild(lista);
		_botonReanudar = new OpcionMenu("01", "REANUDAR");
		_botonGuardar = new OpcionMenu("02", "GUARDAR PARTIDA");
		_botonOpciones = new OpcionMenu("03", "OPCIONES");
		_botonMenuInicio = new OpcionMenu("04", "VOLVER AL MENÚ PRINCIPAL");
		_botonSalirJuego = new OpcionMenu("05", "SALIR DEL JUEGO");
		_botonPistas = new OpcionMenu("06", "PISTAS") { Name = "BotonPistas" };
		foreach (var opcion in new[] { _botonReanudar, _botonGuardar, _botonOpciones, _botonMenuInicio, _botonSalirJuego, _botonPistas })
		{
			opcion.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
			lista.AddChild(opcion);
		}

		var pista = EstiloMenus.Etiqueta("ESC · REANUDAR", EstiloMenus.Mono(22, 0.2f), 22, EstiloMenus.GrisOscuro);
		pista.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
		pista.GrowHorizontal = Control.GrowDirection.Begin;
		pista.GrowVertical = Control.GrowDirection.Begin;
		pista.OffsetRight = -80;
		pista.OffsetBottom = -56;
		_pantallaPrincipal.AddChild(pista);
	}

	// Los paneles y el diálogo de confirmación son hijos, así que reciben Esc antes
	// que este nodo; aquí solo llega cuando está visible la lista principal.
	public override void _UnhandledInput(InputEvent evento)
	{
		if (evento.IsEcho()) return;
		if (_panelRanuras.Visible || _panelPistas.Visible) return;
		if (!_raiz.Visible && GetTree().Paused) return;
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
		if (Abierto || GetTree().Paused) return;
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
		(_focoAlVolver ?? _botonReanudar).GrabFocus();
	}
}
