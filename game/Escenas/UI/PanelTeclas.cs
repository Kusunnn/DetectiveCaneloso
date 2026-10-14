using Godot;
using System;
using System.Collections.Generic;

// Opciones → Controles: cambiar la tecla de cada acción (Configuracion.AccionesTeclas).
// Clic en una acción y pulsar la tecla nueva; Esc cancela. Si la tecla ya era de otra acción, se intercambian.
// El control no se cambia aquí: sus botones son fijos.
public partial class PanelTeclas : Control
{
	[Signal] public delegate void CerradoEventHandler();

	private static readonly Color Ambar = new Color("efca08");
	private static readonly Color Tinta = new Color("20272b");
	private static readonly Color Papel = new Color(0.84f, 0.81f, 0.72f);

	private Font _mono;
	private GridContainer _lista;
	private Label _aviso;
	private Button _restablecer, _volver;
	private readonly List<Button> _botones = new();
	private string _esperando;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Hide();
		_mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var titulos = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf") };
		titulos.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };

		var oscurecer = new ColorRect { Color = new Color(0, 0, 0, 0.6f), MouseFilter = MouseFilterEnum.Stop };
		oscurecer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(oscurecer);
		var centro = new CenterContainer();
		centro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(centro);
		var fondo = new StyleBoxFlat { BgColor = Papel, ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 10 };
		fondo.SetContentMarginAll(26);
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(760, 0) };
		panel.AddThemeStyleboxOverride("panel", fondo);
		centro.AddChild(panel);
		var columna = new VBoxContainer();
		columna.AddThemeConstantOverride("separation", 12);
		panel.AddChild(columna);

		var titulo = new Label { Text = "CONTROLES" };
		titulo.AddThemeFontOverride("font", titulos);
		titulo.AddThemeFontSizeOverride("font_size", 40);
		titulo.AddThemeColorOverride("font_color", Tinta);
		columna.AddChild(titulo);
		columna.AddChild(new ColorRect { Color = Ambar, CustomMinimumSize = new Vector2(150, 5), SizeFlagsHorizontal = SizeFlags.ShrinkBegin });
		_aviso = Texto("Elige una acción y pulsa la tecla nueva. [Esc] cancela.", 16, 0.7f);
		columna.AddChild(_aviso);

		_lista = new GridContainer { Columns = 2 };
		_lista.AddThemeConstantOverride("h_separation", 30);
		_lista.AddThemeConstantOverride("v_separation", 6);
		columna.AddChild(_lista);
		foreach (var (accion, nombre) in Configuracion.AccionesTeclas)
		{
			var etiqueta = Texto(nombre, 18);
			etiqueta.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			_lista.AddChild(etiqueta);
			var boton = Boton("");
			boton.CustomMinimumSize = new Vector2(200, 0);
			string a = accion;
			boton.Pressed += () => Esperar(a);
			boton.SetMeta("accion", accion);
			_lista.AddChild(boton);
			_botones.Add(boton);
		}

		var pie = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		pie.AddThemeConstantOverride("separation", 12);
		columna.AddChild(pie);
		_restablecer = Boton("Restablecer");
		_restablecer.Pressed += () =>
		{
			Configuracion.Instancia.RestablecerTeclas();
			_esperando = null;
			Refrescar();
		};
		_volver = Boton("Volver");
		_volver.Pressed += Cerrar;
		pie.AddChild(_restablecer);
		pie.AddChild(_volver);

		UtilidadesUI.ConectarFocoConRaton(this);
		UtilidadesUI.EncadenarFoco(new List<Control>(_botones) { _restablecer, _volver });
	}

	private Label Texto(string texto, int tam, float alfa = 1f)
	{
		var l = new Label { Text = texto, MouseFilter = MouseFilterEnum.Ignore };
		l.AddThemeFontOverride("font", _mono);
		l.AddThemeFontSizeOverride("font_size", tam);
		l.AddThemeColorOverride("font_color", new Color(Tinta, alfa));
		return l;
	}

	private Button Boton(string texto)
	{
		var b = new Button { Text = texto };
		b.AddThemeFontOverride("font", _mono);
		b.AddThemeFontSizeOverride("font_size", 18);
		var normal = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.25f), BorderColor = new Color(Tinta, 0.35f) };
		normal.SetBorderWidthAll(1);
		normal.SetContentMarginAll(6);
		var foco = new StyleBoxFlat { BgColor = new Color(Ambar, 0.45f), BorderColor = Ambar };
		foco.SetBorderWidthAll(3);
		foco.SetContentMarginAll(6);
		b.AddThemeStyleboxOverride("normal", normal);
		b.AddThemeStyleboxOverride("hover", foco);
		b.AddThemeStyleboxOverride("focus", foco);
		b.AddThemeStyleboxOverride("pressed", foco);
		foreach (string c in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color" })
			b.AddThemeColorOverride(c, Tinta);
		return b;
	}

	public void Abrir()
	{
		_esperando = null;
		Refrescar();
		Show();
		_botones[0].GrabFocus();
	}

	public void Cerrar()
	{
		_esperando = null;
		Hide();
		Configuracion.Instancia.Guardar();
		EmitSignal(SignalName.Cerrado);
	}

	private void Refrescar()
	{
		foreach (var boton in _botones)
		{
			string accion = (string)boton.GetMeta("accion");
			Key tecla = Configuracion.TeclaDe(accion);
			boton.Text = accion == _esperando ? "Pulsa una tecla…" : tecla == Key.None ? "—" : NombreTecla(tecla);
		}
		_aviso.Text = _esperando != null
			? "Pulsa la tecla nueva. [Esc] cancela."
			: "Elige una acción y pulsa la tecla nueva. [Esc] cancela.";
	}

	private static string NombreTecla(Key tecla)
	{
		Key codigo = DisplayServer.KeyboardGetKeycodeFromPhysical(tecla);
		if (codigo == Key.None) codigo = tecla;
		return codigo switch
		{
			Key.Ctrl => "Ctrl",
			Key.Shift => "Shift",
			Key.Tab => "TAB",
			Key.Space => "Espacio",
			_ => OS.GetKeycodeString(codigo),
		};
	}

	private void Esperar(string accion)
	{
		_esperando = accion;
		Refrescar();
	}

	public override void _Input(InputEvent evento)
	{
		if (!Visible) return;
		if (evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo)
		{
			GetViewport().SetInputAsHandled();
			if (_esperando == null)
			{
				if (tecla.Keycode == Key.Escape) Cerrar();
				return;
			}
			// Esc no se puede asignar: es pausa y cancelar
			if (tecla.Keycode != Key.Escape)
			{
				Key nueva = tecla.PhysicalKeycode != Key.None ? tecla.PhysicalKeycode : tecla.Keycode;
				Configuracion.Instancia.CambiarTecla(_esperando, nueva);
				Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic);
			}
			string accion = _esperando;
			_esperando = null;
			Refrescar();
			_botones.Find(b => (string)b.GetMeta("accion") == accion)?.GrabFocus();
		}
		else if (evento.IsActionPressed("ui_cancel") && _esperando == null)
		{
			GetViewport().SetInputAsHandled();
			Cerrar();
		}
	}
}
