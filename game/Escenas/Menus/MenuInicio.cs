using Godot;
using System;

// Menú de inicio (CA-01): nueva partida, continuar, opciones, créditos y salir.
// Diseño: "Detective Caneloso – Inicio" (Claude Design). Colores, tamaños y estilos
// están en la escena; aquí solo se cargan las fuentes y la animación de foco.
public partial class MenuInicio : Control
{
	private const string CarpetaFuentes = "res://Assets/Fuentes/";
	// Desplazamiento (px) de la opción seleccionada.
	private const int DesplazamientoFoco = 10;
	private const float TransparenciaDesactivado = 0.35f;

	private Control _pantallaPrincipal;
	private Control _listaOpciones;
	private Button _botonNuevaPartida;
	private Button _botonContinuar;
	private Button _botonCapitulos;
	private Button _botonOpciones;
	private Button _botonCreditos;
	private Button _botonSalir;
	private PanelMenu _menuOpciones;
	private PanelMenu _pantallaCreditos;
	private TableroCapitulos _tableroCapitulos;
	private DialogoConfirmacion _dialogoConfirmacion;

	// Botón que abrió el panel actual, para devolverle el foco al cerrarlo.
	private Control _focoAlVolver;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;

		_pantallaPrincipal = GetNode<Control>("%PantallaPrincipal");
		_listaOpciones = GetNode<Control>("%ListaOpciones");
		_botonNuevaPartida = GetNode<Button>("%BotonNuevaPartida");
		_botonContinuar = GetNode<Button>("%BotonContinuar");
		_botonCapitulos = GetNode<Button>("%BotonCapitulos");
		_botonOpciones = GetNode<Button>("%BotonOpciones");
		_botonCreditos = GetNode<Button>("%BotonCreditos");
		_botonSalir = GetNode<Button>("%BotonSalir");
		_menuOpciones = GetNode<PanelMenu>("%MenuOpciones");
		_pantallaCreditos = GetNode<PanelMenu>("%PantallaCreditos");
		_dialogoConfirmacion = GetNode<DialogoConfirmacion>("%DialogoConfirmacion");

		AplicarFuentes();
		var botones = new[] { _botonNuevaPartida, _botonContinuar, _botonCapitulos, _botonOpciones, _botonCreditos, _botonSalir };
		foreach (var boton in botones)
		{
			var fila = FilaDe(boton);
			boton.FocusEntered += () => Deslizar(fila, DesplazamientoFoco);
			boton.FocusExited += () => Deslizar(fila, 0);
		}

		_botonNuevaPartida.Pressed += AlPresionarNuevaPartida;
		_botonContinuar.Pressed += () => _ = AlPresionarContinuar();
		_botonCapitulos.Pressed += () => AbrirTablero();
		_botonOpciones.Pressed += () => AbrirPanel(_menuOpciones, _botonOpciones);
		_botonCreditos.Pressed += () => AbrirPanel(_pantallaCreditos, _botonCreditos);
		_botonSalir.Pressed += AlPresionarSalir;
		_menuOpciones.Cerrado += AlCerrarPanel;
		_pantallaCreditos.Cerrado += AlCerrarPanel;

		bool puedeContinuar = GestorPartida.Instancia.HayPartidaGuardada();
		_botonContinuar.Disabled = !puedeContinuar;
		_botonContinuar.FocusMode = puedeContinuar ? FocusModeEnum.All : FocusModeEnum.None;
		_botonContinuar.MouseDefaultCursorShape = puedeContinuar ? CursorShape.PointingHand : CursorShape.Arrow;
		var filaContinuar = FilaDe(_botonContinuar);
		filaContinuar.Modulate = new Color(filaContinuar.Modulate, puedeContinuar ? 1.0f : TransparenciaDesactivado);

		// Tablero de capítulos: debajo del diálogo de confirmación
		_tableroCapitulos = new TableroCapitulos();
		AddChild(_tableroCapitulos);
		MoveChild(_tableroCapitulos, _dialogoConfirmacion.GetIndex());
		_tableroCapitulos.Cerrado += AlCerrarPanel;


		UtilidadesUI.ConectarFocoConRaton(this);
		UtilidadesUI.EncadenarFoco(botones);
		(puedeContinuar ? _botonContinuar : _botonNuevaPartida).GrabFocus();

		// Se eligió un capítulo que todavía no tiene escena: su tarjeta y luego el tablero
		string enPreparacion = GestorPartida.Instancia.CapituloEnPreparacion;
		if (!string.IsNullOrEmpty(enPreparacion))
		{
			GestorPartida.Instancia.CapituloEnPreparacion = null;
			_ = MostrarEnPreparacion(Capitulos.Obtener(enPreparacion));
		}
	}

	private void AbrirTablero(string mensaje = null)
	{
		_focoAlVolver = _botonCapitulos;
		_pantallaPrincipal.Hide();
		_tableroCapitulos.Abrir(mensaje);
	}

	private async System.Threading.Tasks.Task MostrarEnPreparacion(InfoCapitulo capitulo)
	{
		if (capitulo == null) return;
		await GestorPartida.Instancia.PresentarCapituloEnPreparacion(capitulo);
		AbrirTablero($"{capitulo.Etiqueta} todavía está en preparación. Aquí aparecerá cuando esté listo.");
	}

	// Carga las fuentes del diseño; si faltan en Assets/Fuentes/, usa la de Godot.
	// Big Shoulders Display es una fuente variable: el grosor se elige con "wght".
	private void AplicarFuentes()
	{
		const string display = "BigShouldersDisplay-Variable.ttf";
		GetNode<Label>("%Antetitulo").AddThemeFontOverride("font", Fuente(display, 16, 500)); // Medium
		GetNode<Label>("%Titulo").AddThemeFontOverride("font", Fuente(display, 2, 900)); // Black
		_listaOpciones.Theme.SetFont("font", "Button", Fuente(display, 3, 800)); // ExtraBold
		_listaOpciones.Theme.SetFont("font", "Label", Fuente("IBMPlexMono-Regular.ttf", 1));
	}

	private static Font Fuente(string archivo, int espaciado, int peso = 0)
	{
		string ruta = CarpetaFuentes + archivo;
		var fuente = new FontVariation
		{
			BaseFont = ResourceLoader.Exists(ruta) ? GD.Load<Font>(ruta) : ThemeDB.FallbackFont,
			SpacingGlyph = espaciado,
		};
		if (peso > 0)
		{
			long etiquetaPeso = TextServerManager.GetPrimaryInterface().NameToTag("wght");
			fuente.VariationOpentype = new Godot.Collections.Dictionary { { etiquetaPeso, peso } };
		}
		return fuente;
	}

	// Fila (MarginContainer) que contiene al botón junto a su número.
	private static MarginContainer FilaDe(Button boton)
	{
		return boton.GetParent().GetParent<MarginContainer>();
	}

	private static void Deslizar(MarginContainer fila, int x)
	{
		if (!fila.IsInsideTree()) return; // p. ej. al cambiar de escena
		fila.CreateTween()
			.TweenProperty(fila, "theme_override_constants/margin_left", x, 0.18)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
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

	// HU-04 CA-03: antes de empezar se pregunta si se quiere omitir el tutorial.
	// El foco empieza en "Jugar tutorial", así que lo recomendado es lo fácil.
	private async void AlPresionarNuevaPartida()
	{
		bool omitir = await _dialogoConfirmacion.Preguntar(
			"¿Deseas omitir el tutorial?", "Omitir tutorial", "Jugar tutorial", peligro: false);
		GestorPartida.Instancia.NuevaPartida(omitir);
	}

	private async void AlPresionarSalir()
	{
		if (await _dialogoConfirmacion.Preguntar("¿Seguro que quieres salir del juego?", "Salir"))
		{
			GetTree().Quit();
		}
	}

	// Carga la partida más reciente. Si el archivo está dañado no se cierra el juego: se ofrece el respaldo.
	private async System.Threading.Tasks.Task AlPresionarContinuar()
	{
		var reciente = GestorPartida.Instancia.MasReciente();
		if (reciente == null) return;
		if (!reciente.Danada && GestorPartida.Instancia.CargarRanura(reciente.Numero)) return;

		if (reciente.HayRespaldo && await _dialogoConfirmacion.Preguntar(
			"La partida guardada está dañada.\n¿Cargar la copia de respaldo?", "Cargar respaldo"))
		{
			if (GestorPartida.Instancia.CargarRanura(reciente.Numero, desdeRespaldo: true)) return;
			await _dialogoConfirmacion.Preguntar("El respaldo también está dañado. No se pudo cargar la partida.", "Aceptar", "Cerrar");
		}
		else if (!reciente.HayRespaldo)
		{
			await _dialogoConfirmacion.Preguntar("La partida guardada está dañada y no hay copia de respaldo.", "Aceptar", "Cerrar");
		}
	}
}
