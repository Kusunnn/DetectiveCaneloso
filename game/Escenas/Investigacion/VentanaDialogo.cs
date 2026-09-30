using Godot;
using System;
using System.Threading.Tasks;

// Caja de diálogo inferior. Uso:
//   await dialogo.Decir("Eriz", "Primera línea", "Segunda línea");
//   int elegida = await dialogo.Elegir("Acusación", "¿Quién fue?", "Eriz", "Porky");
// Mientras está abierta, el jugador no se mueve. Clic, [E] o Espacio avanzan.
public partial class VentanaDialogo : CanvasLayer
{
	private static readonly Color Ambar = new Color("efca08");

	private Control _caja;
	private Label _nombre;
	private Label _texto;
	private Label _ayuda;
	private VBoxContainer _opciones;

	private TaskCompletionSource<int> _espera;
	private ulong _abiertoEn;
	private Input.MouseModeEnum _ratonAnterior;

	public bool Abierto => _caja.Visible;

	public override void _Ready()
	{
		Layer = 10; // Por encima del HUD
		ConstruirInterfaz();
		_caja.Hide();
	}

	private void ConstruirInterfaz()
	{
		var fondo = new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.06f, 0.07f, 0.94f),
			BorderColor = new Color(Ambar, 0.8f),
			BorderWidthTop = 3,
			ContentMarginLeft = 40,
			ContentMarginRight = 40,
			ContentMarginTop = 22,
			ContentMarginBottom = 22,
		};
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", fondo);
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		panel.OffsetTop = -260;
		_caja = panel;
		AddChild(panel);

		var columna = new VBoxContainer();
		columna.AddThemeConstantOverride("separation", 10);
		panel.AddChild(columna);

		_nombre = new Label();
		_nombre.AddThemeFontSizeOverride("font_size", 30);
		_nombre.AddThemeColorOverride("font_color", Ambar);
		columna.AddChild(_nombre);

		_texto = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_texto.AddThemeFontSizeOverride("font_size", 26);
		_texto.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		columna.AddChild(_texto);

		_opciones = new VBoxContainer();
		_opciones.AddThemeConstantOverride("separation", 8);
		columna.AddChild(_opciones);

		_ayuda = new Label { Text = "Clic o [E] para continuar", HorizontalAlignment = HorizontalAlignment.Right };
		_ayuda.AddThemeFontSizeOverride("font_size", 18);
		_ayuda.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		columna.AddChild(_ayuda);
	}

	public async Task Decir(string nombre, params string[] lineas)
	{
		Abrir();
		foreach (string linea in lineas)
		{
			Mostrar(nombre, linea);
			_espera = new TaskCompletionSource<int>();
			await _espera.Task;
		}
		Cerrar();
	}

	// Devuelve el índice de la opción elegida.
	public async Task<int> Elegir(string nombre, string pregunta, params string[] opciones)
	{
		Abrir();
		Mostrar(nombre, pregunta);
		_ayuda.Hide();
		_ratonAnterior = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		_espera = new TaskCompletionSource<int>();
		for (int i = 0; i < opciones.Length; i++)
		{
			int indice = i;
			var boton = new Button { Text = (i + 1) + ". " + opciones[i], Alignment = HorizontalAlignment.Left };
			boton.AddThemeFontSizeOverride("font_size", 24);
			boton.Pressed += () => _espera?.TrySetResult(indice);
			_opciones.AddChild(boton);
		}
		((Button)_opciones.GetChild(0)).GrabFocus();

		int elegida = await _espera.Task;
		foreach (Node boton in _opciones.GetChildren()) boton.QueueFree();
		Input.MouseMode = _ratonAnterior;
		Cerrar();
		return elegida;
	}

	private void Abrir()
	{
		_abiertoEn = Time.GetTicksMsec();
		_caja.Show();
		_ayuda.Show();
		JugadorActual()?.Bloquear(true);
	}

	private void Cerrar()
	{
		_caja.Hide();
		_espera = null;
		JugadorActual()?.Bloquear(false);
	}

	private void Mostrar(string nombre, string texto)
	{
		_nombre.Text = nombre;
		_texto.Text = texto;
	}

	private Jugador JugadorActual() => GetTree().GetFirstNodeInGroup("jugador") as Jugador;

	public override void _Input(InputEvent evento)
	{
		if (!_caja.Visible || _opciones.GetChildCount() > 0) return;
		// Evita que el mismo clic que abrió el diálogo lo haga avanzar
		if (Time.GetTicksMsec() - _abiertoEn < 200) return;

		bool avanzar = evento.IsActionPressed("interactuar")
			|| (evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && tecla.Keycode == Key.Space)
			|| (evento is InputEventMouseButton boton && boton.Pressed && boton.ButtonIndex == MouseButton.Left);
		if (!avanzar) return;

		GetViewport().SetInputAsHandled();
		_abiertoEn = Time.GetTicksMsec();
		_espera?.TrySetResult(0);
	}
}
