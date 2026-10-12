using Godot;
using System;
using System.Collections.Generic;

// Tarjeta del tablero: una pista del expediente o un testimonio de la libreta.
public class Carta
{
	public string Id;
	public string Titulo;
	public Texture2D Imagen;
	public string Tipo; // "PISTA" o "TESTIMONIO"

	public Carta(string id, string titulo, string tipo, Texture2D imagen = null)
	{
		Id = id;
		Titulo = titulo;
		Tipo = tipo;
		Imagen = imagen;
	}
}

// Tablero de deducción [R]: el jugador une dos tarjetas que crea relacionadas.
// El caso decide si forman una conclusión (estilo Obra Dinn: el juego no da la respuesta, solo confirma).
public partial class TableroDeduccion : CanvasLayer
{
	private static readonly Color Ambar = new Color("efca08");
	private static readonly Color Tinta = new Color("20272b");

	// Devuelve el texto de la conclusión si la pareja es correcta, o null si no se relacionan.
	public Func<string, string, string> Probar;
	// Tarjetas y conclusiones actuales (las da el caso al abrir)
	public Func<IList<Carta>> ObtenerCartas;
	public Func<IList<string>> ObtenerConclusiones;

	private Font _mono;
	private Font _titulos;
	private Control _raiz;
	private GridContainer _rejilla;
	private VBoxContainer _conclusiones;
	private Label _mensaje;
	private readonly List<Button> _seleccion = new List<Button>();

	public bool Abierto => _raiz.Visible;

	public override void _Ready()
	{
		Layer = 9;
		_mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var display = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf"), SpacingGlyph = 2 };
		display.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };
		_titulos = display;
		Construir();
		_raiz.Hide();
	}

	private void Construir()
	{
		var raiz = new ColorRect { Color = new Color(0.02f, 0.04f, 0.06f, 0.88f) };
		raiz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_raiz = raiz;
		AddChild(raiz);

		var papel = new PanelContainer();
		var estilo = new StyleBoxFlat { BgColor = new Color(0.84f, 0.81f, 0.72f), ShadowColor = new Color(0, 0, 0, 0.5f), ShadowSize = 12 };
		estilo.SetContentMarginAll(34);
		papel.AddThemeStyleboxOverride("panel", estilo);
		papel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		papel.CustomMinimumSize = new Vector2(1560, 820);
		papel.OffsetLeft = -780; papel.OffsetRight = 780; papel.OffsetTop = -410; papel.OffsetBottom = 410;
		raiz.AddChild(papel);

		var columnas = new HBoxContainer();
		columnas.AddThemeConstantOverride("separation", 36);
		papel.AddChild(columnas);

		var izquierda = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.8f };
		izquierda.AddThemeConstantOverride("separation", 10);
		columnas.AddChild(izquierda);
		izquierda.AddChild(Titulo("TABLERO DE DEDUCCIÓN"));
		izquierda.AddChild(Texto("Une dos tarjetas que creas relacionadas. Si encajan, saldrá una conclusión.", 18, 0.7f));
		_rejilla = new GridContainer { Columns = 3 };
		_rejilla.AddThemeConstantOverride("h_separation", 12);
		_rejilla.AddThemeConstantOverride("v_separation", 12);
		izquierda.AddChild(_rejilla);
		_mensaje = Texto("", 22, 1f);
		_mensaje.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		izquierda.AddChild(_mensaje);

		var derecha = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		derecha.AddThemeConstantOverride("separation", 10);
		columnas.AddChild(derecha);
		derecha.AddChild(Titulo("CONCLUSIONES"));
		_conclusiones = new VBoxContainer();
		_conclusiones.AddThemeConstantOverride("separation", 12);
		derecha.AddChild(_conclusiones);
		var ayuda = Texto("[R] o [Esc] para cerrar", 16, 0.6f);
		ayuda.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		ayuda.VerticalAlignment = VerticalAlignment.Bottom;
		derecha.AddChild(ayuda);
	}

	private Label Titulo(string texto)
	{
		var l = new Label { Text = texto };
		l.AddThemeFontOverride("font", _titulos);
		l.AddThemeFontSizeOverride("font_size", 40);
		l.AddThemeColorOverride("font_color", Tinta);
		return l;
	}

	private Label Texto(string texto, int tam, float alfa)
	{
		var l = new Label { Text = texto, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		l.AddThemeFontOverride("font", _mono);
		l.AddThemeFontSizeOverride("font_size", tam);
		l.AddThemeColorOverride("font_color", new Color(Tinta, alfa));
		return l;
	}

	public void Alternar()
	{
		if (Abierto) Cerrar(); else Abrir();
	}

	public void Abrir()
	{
		_seleccion.Clear();
		foreach (Node n in _rejilla.GetChildren()) n.QueueFree();
		foreach (Carta carta in ObtenerCartas?.Invoke() ?? new List<Carta>())
		{
			var boton = new Button
			{
				Text = (carta.Tipo == "TESTIMONIO" ? "« " : "") + carta.Titulo,
				ToggleMode = true,
				Icon = carta.Imagen,
				ExpandIcon = true,
				IconAlignment = HorizontalAlignment.Left,
				Alignment = HorizontalAlignment.Left,
				CustomMinimumSize = new Vector2(300, 84),
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				ClipText = false,
			};
			boton.SetMeta("id", carta.Id);
			boton.AddThemeFontOverride("font", _mono);
			boton.AddThemeFontSizeOverride("font_size", 18);
			var normal = new StyleBoxFlat { BgColor = new Color(0.95f, 0.93f, 0.86f), BorderColor = new Color(Tinta, 0.3f) };
			normal.SetBorderWidthAll(1);
			normal.SetContentMarginAll(8);
			var marcada = new StyleBoxFlat { BgColor = new Color(Ambar, 0.45f), BorderColor = Ambar };
			marcada.SetBorderWidthAll(3);
			marcada.SetContentMarginAll(8);
			boton.AddThemeStyleboxOverride("normal", normal);
			boton.AddThemeStyleboxOverride("hover", normal);
			boton.AddThemeStyleboxOverride("focus", normal);
			boton.AddThemeStyleboxOverride("pressed", marcada);
			boton.AddThemeStyleboxOverride("hover_pressed", marcada);
			foreach (string c in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
				boton.AddThemeColorOverride(c, Tinta);
			boton.Toggled += activo => AlMarcar(boton, activo);
			_rejilla.AddChild(boton);
		}
		ActualizarConclusiones();
		_mensaje.Text = _rejilla.GetChildCount() < 2 ? "Necesito más pistas antes de sacar conclusiones." : "";
		_raiz.Show();
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Expediente);
		Input.MouseMode = Input.MouseModeEnum.Visible;
		JugadorActual()?.Bloquear(true);
	}

	public void Cerrar()
	{
		_raiz.Hide();
		Sonidos.ReproducirUI(this, Sonidos.Tipo.LibretaCerrar);
		Input.MouseMode = Input.MouseModeEnum.Captured;
		JugadorActual()?.Bloquear(false);
	}

	private void ActualizarConclusiones()
	{
		foreach (Node n in _conclusiones.GetChildren()) n.QueueFree();
		var lista = ObtenerConclusiones?.Invoke() ?? new List<string>();
		if (lista.Count == 0) _conclusiones.AddChild(Texto("Ninguna todavía.", 18, 0.6f));
		foreach (string c in lista) _conclusiones.AddChild(Texto("✓ " + c, 19, 1f));
	}

	private void AlMarcar(Button boton, bool activo)
	{
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic, activo ? 0f : -3f, activo ? 1f : 0.9f);
		if (!activo) { _seleccion.Remove(boton); return; }
		_seleccion.Add(boton);
		if (_seleccion.Count < 2) return;

		string a = (string)_seleccion[0].GetMeta("id");
		string b = (string)_seleccion[1].GetMeta("id");
		string conclusion = Probar?.Invoke(a, b);
		foreach (Button s in _seleccion.ToArray()) s.SetPressedNoSignal(false);
		_seleccion.Clear();
		if (conclusion == null)
		{
			_mensaje.Text = "No veo cómo se relacionan... todavía.";
			Sonidos.ReproducirUI(this, Sonidos.Tipo.Error);
			_mensaje.AddThemeColorOverride("font_color", new Color(0.55f, 0.15f, 0.1f));
		}
		else
		{
			_mensaje.Text = "¡Encaja! " + conclusion;
			_mensaje.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.15f));
			ActualizarConclusiones();
		}
	}

	private Jugador JugadorActual() => GetTree().GetFirstNodeInGroup("jugador") as Jugador;

	public override void _Input(InputEvent evento)
	{
		if (!Abierto) return;
		if (evento.IsActionPressed("deducir") || evento.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			Cerrar();
		}
	}
}
