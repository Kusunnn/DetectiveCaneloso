using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Ranuras de guardado (menú de pausa): miniatura, caso y zona, pistas, tiempo jugado y fecha.
// Pide confirmación antes de sobrescribir una ranura ocupada. Teclado, mouse y control.
public partial class PanelRanuras : Control
{
	[Signal] public delegate void CerradoEventHandler();

	private static readonly Color Ambar = new Color("efca08");
	private static readonly Color Tinta = new Color("20272b");
	private static readonly Color Papel = new Color(0.84f, 0.81f, 0.72f);

	// Lo asigna el menú: confirmación reutilizable (foco en "Cancelar")
	public Func<string, string, Task<bool>> Confirmar;

	private VBoxContainer _lista;
	private Label _autoguardado;
	private Button _volver;
	private Font _mono, _titulos;
	private readonly List<Button> _botones = new();

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		Hide();
		_mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var titulos = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf") };
		titulos.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };
		_titulos = titulos;

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

		var titulo = new Label { Text = "GUARDAR PARTIDA" };
		titulo.AddThemeFontOverride("font", _titulos);
		titulo.AddThemeFontSizeOverride("font_size", 40);
		titulo.AddThemeColorOverride("font_color", Tinta);
		columna.AddChild(titulo);
		columna.AddChild(new ColorRect { Color = Ambar, CustomMinimumSize = new Vector2(150, 5), SizeFlagsHorizontal = SizeFlags.ShrinkBegin });
		_autoguardado = Texto("", 16, 0.65f);
		columna.AddChild(_autoguardado);
		_lista = new VBoxContainer();
		_lista.AddThemeConstantOverride("separation", 10);
		columna.AddChild(_lista);
		_volver = Boton("Volver");
		_volver.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		_volver.Pressed += Cerrar;
		columna.AddChild(_volver);
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
		var b = new Button { Text = texto, Alignment = HorizontalAlignment.Left };
		b.AddThemeFontOverride("font", _mono);
		b.AddThemeFontSizeOverride("font_size", 18);
		var normal = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.25f), BorderColor = new Color(Tinta, 0.35f) };
		normal.SetBorderWidthAll(1);
		normal.SetContentMarginAll(10);
		var foco = new StyleBoxFlat { BgColor = new Color(Ambar, 0.45f), BorderColor = Ambar };
		foco.SetBorderWidthAll(3);
		foco.SetContentMarginAll(10);
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
		Refrescar();
		Show();
		if (_botones.Count > 0) _botones[0].GrabFocus();
	}

	public void Cerrar()
	{
		Hide();
		EmitSignal(SignalName.Cerrado);
	}

	private void Refrescar()
	{
		foreach (Node n in _lista.GetChildren()) n.QueueFree();
		_botones.Clear();
		var ranuras = GestorPartida.Instancia.Ranuras();
		var auto = ranuras[GestorPartida.RanuraAuto];
		_autoguardado.Text = auto.Ocupada && !auto.Danada
			? $"Autoguardado: {Capitulos.Obtener(auto.Capitulo)?.Etiqueta} · {auto.Fase} · {auto.Zona} · {auto.Fecha}"
			: "Autoguardado: todavía no hay.";
		foreach (var r in ranuras)
		{
			if (r.Numero == GestorPartida.RanuraAuto) continue;
			var boton = Boton(Describir(r));
			boton.CustomMinimumSize = new Vector2(0, 96);
			boton.Icon = r.Miniatura;
			boton.ExpandIcon = true;
			boton.IconAlignment = HorizontalAlignment.Left;
			boton.AddThemeConstantOverride("icon_max_width", 150);
			int numero = r.Numero;
			bool ocupada = r.Ocupada;
			boton.Pressed += () => _ = Elegir(numero, ocupada);
			_lista.AddChild(boton);
			_botones.Add(boton);
		}
		var cadena = new List<Control>(_botones) { _volver };
		UtilidadesUI.EncadenarFoco(cadena);
		UtilidadesUI.ConectarFocoConRaton(this);
	}

	private static string Describir(InfoRanura r)
	{
		if (!r.Ocupada) return $"RANURA {r.Numero}  ·  vacía";
		if (r.Danada) return $"RANURA {r.Numero}  ·  archivo dañado (se puede sobrescribir)";
		var t = TimeSpan.FromSeconds(r.Tiempo);
		string capitulo = Capitulos.Obtener(r.Capitulo)?.Etiqueta ?? "Caso " + r.Caso;
		string fase = string.IsNullOrEmpty(r.Fase) ? "" : " · " + r.Fase;
		return $"RANURA {r.Numero}  ·  {capitulo}{fase} · {r.Zona}\n" +
			$"Pistas {r.Pistas}/{r.TotalPistas}  ·  Tiempo {(int)t.TotalMinutes} min {t.Seconds:00} s\n{r.Fecha}";
	}

	private async Task Elegir(int ranura, bool ocupada)
	{
		if (!GestorPartida.Instancia.PuedeGuardar)
		{
			GestorPartida.Instancia.MostrarAviso("No puedes guardar ahora", sello: false);
			return;
		}
		if (ocupada && Confirmar != null && !await Confirmar($"La ranura {ranura} ya tiene una partida.\n¿Sobrescribirla?", "Sobrescribir"))
		{
			_botones[ranura - 1].GrabFocus();
			return;
		}
		GestorPartida.Instancia.Guardar(ranura);
		Refrescar();
		_botones[Math.Clamp(ranura - 1, 0, _botones.Count - 1)].GrabFocus();
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (Visible && evento.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			Cerrar();
		}
	}
}
