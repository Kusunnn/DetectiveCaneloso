using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Quién habla en el diálogo: nombre, retrato y ánimo (cambia según cómo lo trate el detective).
public class Hablante
{
	public string Nombre;
	public Texture2D Retrato;
	public string Animo = "";
	public Color ColorAnimo = new Color(0.4f, 0.4f, 0.4f);
	public bool Tiembla = false; // Nervioso: el retrato tiembla un poco
	public float TonoVoz = 1f;   // Tono de los "blips" de máquina de escribir (personalidad de quien habla)

	public Hablante(string nombre, Texture2D retrato = null)
	{
		Nombre = nombre;
		Retrato = retrato;
	}
}

// Una opción del menú de conversación tal como se ve: etiqueta del tipo y si ya se leyó.
public class OpcionVista
{
	public string Texto;
	public string Etiqueta;   // PREGUNTAR, PRESIONAR, EVIDENCIA...
	public bool Leida;

	public OpcionVista(string texto, string etiqueta = "", bool leida = false)
	{
		Texto = texto;
		Etiqueta = etiqueta;
		Leida = leida;
	}
}

// Caja de diálogo con el estilo del HUD (papel, máquina de escribir, acento amarillo):
// retrato y nombre de quien habla, texto que se escribe solo, opciones y selector de evidencias.
//   await dialogo.Decir(hablante, "línea 1", "línea 2");
//   int i = await dialogo.Elegir(hablante, "¿Qué le dices?", opciones);
//   string id = await dialogo.ElegirEvidencia(pistas);   // null = cancelar
public partial class VentanaDialogo : CanvasLayer
{
	private static readonly Color Ambar = new Color("efca08");
	private static readonly Color Tinta = new Color("20272b");
	private static readonly Color Papel = new Color(0.84f, 0.81f, 0.72f);
	private const float LetrasPorSegundo = 55f;

	private Font _mono;
	private Font _titulos;
	private Control _caja;
	private TextureRect _retrato;
	private Control _marcoRetrato;
	private Label _nombre;
	private Label _animo;
	private Label _texto;
	private Label _ayuda;
	private VBoxContainer _opciones;
	private GridContainer _evidencias;

	private TaskCompletionSource<int> _espera;
	private int _indiceSalir = -1;
	private ulong _abiertoEn;
	private Tween _escritura;
	private Tween _temblor;
	private float _tonoVoz = 1f;
	private int _letrasSonadas;
	private Input.MouseModeEnum _ratonAnterior;
	private readonly Dictionary<string, Texture2D> _retratos = new Dictionary<string, Texture2D>();

	public bool Abierto => _caja.Visible;

	public override void _Ready()
	{
		Layer = 10; // Por encima del HUD
		_mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var display = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf") };
		display.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };
		display.SpacingGlyph = 2;
		_titulos = display;
		ConstruirInterfaz();
		_caja.Hide();
	}

	private void ConstruirInterfaz()
	{
		var fondo = new StyleBoxFlat
		{
			BgColor = Papel,
			BorderColor = new Color(0.28f, 0.24f, 0.16f, 0.6f),
			ShadowColor = new Color(0, 0, 0, 0.45f),
			ShadowSize = 10,
			ShadowOffset = new Vector2(4, 6),
		};
		fondo.SetBorderWidthAll(2);
		fondo.SetCornerRadiusAll(3);
		fondo.SetContentMarginAll(26);
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", fondo);
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		panel.OffsetLeft = 70;
		panel.OffsetRight = -70;
		panel.OffsetTop = -330;
		panel.OffsetBottom = -30;
		panel.GrowVertical = Control.GrowDirection.Begin; // Con muchas opciones crece hacia arriba, nunca se sale de la pantalla
		_caja = panel;
		AddChild(panel);

		var fila = new HBoxContainer();
		fila.AddThemeConstantOverride("separation", 28);
		panel.AddChild(fila);

		// Retrato tipo polaroid
		var marco = new PanelContainer();
		var estiloMarco = new StyleBoxFlat { BgColor = new Color(0.95f, 0.94f, 0.9f) };
		estiloMarco.SetContentMarginAll(8);
		estiloMarco.ContentMarginBottom = 26;
		marco.AddThemeStyleboxOverride("panel", estiloMarco);
		marco.CustomMinimumSize = new Vector2(196, 0);
		marco.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		_marcoRetrato = marco;
		fila.AddChild(marco);
		_retrato = new TextureRect
		{
			CustomMinimumSize = new Vector2(180, 180),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
		};
		marco.AddChild(_retrato);

		var columna = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		columna.AddThemeConstantOverride("separation", 8);
		fila.AddChild(columna);

		var cabecera = new HBoxContainer();
		cabecera.AddThemeConstantOverride("separation", 18);
		columna.AddChild(cabecera);
		_nombre = new Label();
		_nombre.AddThemeFontOverride("font", _titulos);
		_nombre.AddThemeFontSizeOverride("font_size", 40);
		_nombre.AddThemeColorOverride("font_color", Tinta);
		cabecera.AddChild(_nombre);
		_animo = new Label { VerticalAlignment = VerticalAlignment.Center };
		_animo.AddThemeFontOverride("font", _mono);
		_animo.AddThemeFontSizeOverride("font_size", 18);
		cabecera.AddChild(_animo);

		var subrayado = new ColorRect { Color = Ambar, CustomMinimumSize = new Vector2(150, 5) };
		subrayado.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		columna.AddChild(subrayado);

		_texto = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		_texto.AddThemeFontOverride("font", _mono);
		_texto.AddThemeFontSizeOverride("font_size", 25);
		_texto.AddThemeColorOverride("font_color", Tinta);
		columna.AddChild(_texto);

		_opciones = new VBoxContainer();
		_opciones.AddThemeConstantOverride("separation", 4);
		columna.AddChild(_opciones);

		_evidencias = new GridContainer { Columns = 4 };
		_evidencias.AddThemeConstantOverride("h_separation", 12);
		_evidencias.AddThemeConstantOverride("v_separation", 8);
		columna.AddChild(_evidencias);

		_ayuda = new Label { Text = "Clic o [E] para continuar", HorizontalAlignment = HorizontalAlignment.Right };
		_ayuda.AddThemeFontOverride("font", _mono);
		_ayuda.AddThemeFontSizeOverride("font_size", 16);
		_ayuda.AddThemeColorOverride("font_color", new Color(Tinta, 0.6f));
		columna.AddChild(_ayuda);
	}

	// ---------- API ----------

	public Task Decir(string nombre, params string[] lineas) => Decir(HablantePorNombre(nombre), lineas);

	public async Task Decir(Hablante hablante, params string[] lineas)
	{
		Abrir();
		foreach (string linea in lineas)
		{
			MostrarHablante(hablante);
			Escribir(linea);
			_espera = new TaskCompletionSource<int>();
			await _espera.Task;
		}
		Cerrar();
	}

	public Task<int> Elegir(string nombre, string pregunta, params string[] opciones)
	{
		var vistas = new List<OpcionVista>();
		foreach (string o in opciones) vistas.Add(new OpcionVista(o));
		return Elegir(HablantePorNombre(nombre), pregunta, vistas);
	}

	// Devuelve el índice de la opción elegida. Si se pasa "salir", Esc o clic derecho eligen esa opción.
	public async Task<int> Elegir(Hablante hablante, string pregunta, IList<OpcionVista> opciones, int salir = -1)
	{
		Abrir();
		MostrarHablante(hablante);
		Escribir(pregunta, instantaneo: true);
		_indiceSalir = salir;
		_ayuda.Text = "Elige con el mouse o con las teclas 1-" + Math.Min(opciones.Count, 9)
			+ (salir >= 0 ? "   ·   [Esc] o clic derecho: terminar la conversación" : "");
		LiberarRaton();

		_espera = new TaskCompletionSource<int>();
		for (int i = 0; i < opciones.Count; i++)
		{
			int indice = i;
			var o = opciones[i];
			string etiqueta = string.IsNullOrEmpty(o.Etiqueta) ? "" : "[" + o.Etiqueta + "] ";
			var boton = CrearBoton((i + 1) + ". " + etiqueta + o.Texto + (o.Leida ? "  ✓" : ""), o.Leida);
			boton.Pressed += () => _espera?.TrySetResult(indice);
			_opciones.AddChild(boton);
		}
		((Button)_opciones.GetChild(0)).GrabFocus();

		int elegida = await _espera.Task;
		_indiceSalir = -1;
		LimpiarOpciones();
		RestaurarRaton();
		Cerrar();
		return elegida;
	}

	// Selector de evidencias del expediente. Devuelve el id elegido o null si se cancela.
	public async Task<string> ElegirEvidencia(Hablante hablante, IList<(string Id, string Titulo, Texture2D Imagen)> pistas)
	{
		Abrir();
		MostrarHablante(hablante);
		Escribir(pistas.Count == 0 ? "Todavía no tengo pruebas que mostrar." : "¿Qué prueba le muestro?", instantaneo: true);
		_ayuda.Text = "Elige una pista del expediente   ·   [Esc] o clic derecho: volver";
		LiberarRaton();

		_espera = new TaskCompletionSource<int>();
		for (int i = 0; i < pistas.Count; i++)
		{
			int indice = i;
			var tarjeta = CrearBoton(pistas[i].Titulo, false);
			tarjeta.Icon = pistas[i].Imagen;
			tarjeta.ExpandIcon = true;
			tarjeta.IconAlignment = HorizontalAlignment.Left;
			tarjeta.CustomMinimumSize = new Vector2(330, 74);
			tarjeta.Pressed += () => _espera?.TrySetResult(indice);
			_evidencias.AddChild(tarjeta);
		}
		var cancelar = CrearBoton("Mejor no mostrarle nada", true);
		cancelar.Pressed += () => _espera?.TrySetResult(-1);
		_evidencias.AddChild(cancelar);
		((Button)_evidencias.GetChild(0)).GrabFocus();

		int elegida = await _espera.Task;
		foreach (Node n in _evidencias.GetChildren()) n.QueueFree();
		RestaurarRaton();
		Cerrar();
		return elegida < 0 ? null : pistas[elegida].Id;
	}

	// Retratos en Assets/Retratos/<nombre>.png (Caneloso, Eriz, Porky, Comisario...)
	public Hablante HablantePorNombre(string nombre)
	{
		return new Hablante(nombre, Retrato(nombre));
	}

	public Texture2D Retrato(string nombre)
	{
		string clave = nombre.ToLower().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
		if (!_retratos.TryGetValue(clave, out var textura))
		{
			string ruta = "res://Assets/Retratos/" + clave + ".png";
			textura = ResourceLoader.Exists(ruta) ? GD.Load<Texture2D>(ruta) : null;
			_retratos[clave] = textura;
		}
		return textura;
	}

	// ---------- Interior ----------

	private Button CrearBoton(string texto, bool apagado)
	{
		var boton = new Button { Text = texto, Alignment = HorizontalAlignment.Left, Flat = false };
		boton.AddThemeFontOverride("font", _mono);
		boton.AddThemeFontSizeOverride("font_size", 21);
		var normal = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
		normal.SetContentMarginAll(4);
		var foco = new StyleBoxFlat { BgColor = new Color(Ambar, 0.35f), BorderColor = Ambar };
		foco.BorderWidthLeft = 5;
		foco.SetContentMarginAll(4);
		boton.AddThemeStyleboxOverride("normal", normal);
		boton.AddThemeStyleboxOverride("hover", foco);
		boton.AddThemeStyleboxOverride("focus", foco);
		boton.AddThemeStyleboxOverride("pressed", foco);
		Color color = apagado ? new Color(Tinta, 0.55f) : Tinta;
		boton.AddThemeColorOverride("font_color", color);
		boton.AddThemeColorOverride("font_hover_color", Tinta);
		boton.AddThemeColorOverride("font_focus_color", Tinta);
		boton.AddThemeColorOverride("font_pressed_color", Tinta);
		boton.MouseEntered += () => boton.GrabFocus();
		boton.FocusEntered += () => Sonidos.ReproducirUI(this, Sonidos.Tipo.Hover);
		boton.Pressed += () => Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic);
		return boton;
	}

	private void MostrarHablante(Hablante h)
	{
		_nombre.Text = h.Nombre.ToUpper();
		_animo.Text = string.IsNullOrEmpty(h.Animo) ? "" : "· " + h.Animo.ToUpper();
		_animo.AddThemeColorOverride("font_color", h.ColorAnimo);
		_retrato.Texture = h.Retrato;
		_marcoRetrato.Visible = h.Retrato != null;
		_tonoVoz = h.TonoVoz;
		_temblor?.Kill();
		_marcoRetrato.Rotation = 0;
		if (h.Tiembla)
		{
			_temblor = CreateTween().SetLoops(6);
			_temblor.TweenProperty(_marcoRetrato, "rotation", 0.02f, 0.05);
			_temblor.TweenProperty(_marcoRetrato, "rotation", -0.02f, 0.05);
			_temblor.Finished += () => _marcoRetrato.Rotation = 0;
		}
	}

	// Efecto de máquina de escribir; un clic mientras escribe completa la línea
	private void Escribir(string texto, bool instantaneo = false)
	{
		_texto.Text = texto;
		_escritura?.Kill();
		if (instantaneo)
		{
			_texto.VisibleRatio = 1f;
			return;
		}
		_texto.VisibleRatio = 0f;
		_letrasSonadas = 0;
		_escritura = CreateTween();
		_escritura.TweenProperty(_texto, "visible_ratio", 1f, Mathf.Max(0.2f, texto.Length / LetrasPorSegundo));
	}

	private bool Escribiendo => _escritura != null && _escritura.IsRunning();

	// Blips suaves de máquina de escribir: uno cada 3 letras, con el tono de quien habla
	public override void _Process(double delta)
	{
		if (!Escribiendo) return;
		int visibles = (int)(_texto.VisibleRatio * _texto.Text.Length);
		if (visibles - _letrasSonadas < 3) return;
		_letrasSonadas = visibles;
		char letra = _texto.Text[Math.Clamp(visibles - 1, 0, _texto.Text.Length - 1)];
		if (char.IsWhiteSpace(letra)) return;
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Texto, 0f, _tonoVoz);
	}

	// Una conversación completa (varias preguntas seguidas) mantiene la caja abierta sin parpadeos:
	// Retener() al empezar y Soltar() al terminar.
	private int _nivel = 0;
	public void Retener() => Abrir();
	public void Soltar() => Cerrar();

	private void Abrir()
	{
		_abiertoEn = Time.GetTicksMsec();
		_ayuda.Text = "Clic o [E] para continuar";
		if (_nivel++ > 0) return;
		_caja.Show();
		JugadorActual()?.Bloquear(true);
	}

	private void Cerrar()
	{
		_espera = null;
		if (--_nivel > 0) return;
		_nivel = 0;
		_caja.Hide();
		JugadorActual()?.Bloquear(false);
	}

	private void LiberarRaton()
	{
		_ratonAnterior = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void RestaurarRaton()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private void LimpiarOpciones()
	{
		foreach (Node boton in _opciones.GetChildren()) boton.QueueFree();
	}

	private Jugador JugadorActual() => GetTree().GetFirstNodeInGroup("jugador") as Jugador;

	public override void _Input(InputEvent evento)
	{
		if (!_caja.Visible) return;

		// Esc o clic derecho: salir de la conversación o volver del selector de evidencias
		bool eligiendo = _opciones.GetChildCount() > 0 || _evidencias.GetChildCount() > 0;
		bool cancelar = evento.IsActionPressed("ui_cancel") || evento.IsActionPressed("pausa")
			|| (evento is InputEventMouseButton derecho && derecho.Pressed && derecho.ButtonIndex == MouseButton.Right);
		if (eligiendo && cancelar)
		{
			if (_evidencias.GetChildCount() > 0) { GetViewport().SetInputAsHandled(); _espera?.TrySetResult(-1); }
			else if (_indiceSalir >= 0) { GetViewport().SetInputAsHandled(); _espera?.TrySetResult(_indiceSalir); }
			return;
		}

		// Teclas 1-9 para elegir opción
		if (_opciones.GetChildCount() > 0 && evento is InputEventKey numero && numero.Pressed && !numero.Echo)
		{
			int indice = (int)numero.Keycode - (int)Key.Key1;
			if (indice >= 0 && indice < _opciones.GetChildCount())
			{
				GetViewport().SetInputAsHandled();
				_espera?.TrySetResult(indice);
			}
			return;
		}
		if (_opciones.GetChildCount() > 0 || _evidencias.GetChildCount() > 0) return;
		// Evita que el mismo clic que abrió el diálogo lo haga avanzar
		if (Time.GetTicksMsec() - _abiertoEn < 200) return;

		bool avanzar = evento.IsActionPressed("interactuar")
			|| (evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && tecla.Keycode == Key.Space)
			|| (evento is InputEventMouseButton boton && boton.Pressed && boton.ButtonIndex == MouseButton.Left);
		if (!avanzar) return;

		GetViewport().SetInputAsHandled();
		_abiertoEn = Time.GetTicksMsec();
		if (Escribiendo)
		{
			_escritura.Kill();
			_texto.VisibleRatio = 1f;
			return;
		}
		_espera?.TrySetResult(0);
	}
}
