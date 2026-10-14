using Godot;
using System;

// Códice de reglas (menú de pausa). No tiene diseño propio: usa el lenguaje de los demás menús
// (título ámbar, raya roja, texto en IBM Plex Mono y VOLVER abajo). Se cierra con VOLVER o con Esc.
public partial class CodiceReglas : PanelMenu
{
	// Texto del códice en BBCode: [b]...[/b] para los títulos de cada regla
	[Export(PropertyHint.MultilineText)]
	public string Texto { get; set; } =
		"[b]Regla 1[/b]\nTexto de ejemplo. Aquí irán las reglas del juego.\n\n" +
		"[b]Regla 2[/b]\nTexto de ejemplo.\n\n" +
		"[b]Regla 3[/b]\nTexto de ejemplo.";

	private Button _volver;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Construir();
		FocoInicial = _volver;
		base._Ready();
		_volver.Pressed += Cerrar;
	}

	private void Construir()
	{
		EstiloMenus.Fondo(this);
		var margen = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		margen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		margen.AddThemeConstantOverride("margin_left", 112);
		margen.AddThemeConstantOverride("margin_right", 112);
		margen.AddThemeConstantOverride("margin_top", 80);
		margen.AddThemeConstantOverride("margin_bottom", 60);
		AddChild(margen);
		var columna = EstiloMenus.Columna(52);
		margen.AddChild(columna);

		var titulo = EstiloMenus.Columna(20);
		titulo.AddChild(EstiloMenus.Marca("EXPEDIENTE DEL DETECTIVE", EstiloMenus.Gris, 0.28f));
		titulo.AddChild(EstiloMenus.Titulo("CÓDICE DE REGLAS", 112));
		columna.AddChild(titulo);

		var texto = new RichTextLabel
		{
			BbcodeEnabled = true,
			Text = Texto,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(1240, 0),
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
			FocusMode = FocusModeEnum.All,
			ScrollFollowing = false,
		};
		texto.AddThemeFontOverride("normal_font", EstiloMenus.Mono());
		texto.AddThemeFontOverride("bold_font", EstiloMenus.Display(800, 40, 0.04f));
		texto.AddThemeFontSizeOverride("normal_font_size", 28);
		texto.AddThemeFontSizeOverride("bold_font_size", 40);
		texto.AddThemeColorOverride("default_color", EstiloMenus.GrisClaro);
		texto.AddThemeConstantOverride("line_separation", 10);
		texto.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		columna.AddChild(texto);

		var pie = EstiloMenus.Columna(28);
		pie.AddChild(EstiloMenus.Raya(0, 2, EstiloMenus.Linea));
		var filaPie = EstiloMenus.Fila(28);
		var nota = EstiloMenus.Etiqueta("La verdad siempre deja rastro.", EstiloMenus.Mono(22, 0.04f), 22, EstiloMenus.Gris);
		nota.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		filaPie.AddChild(nota);
		_volver = new OpcionMenu("←", "VOLVER", 44) { Desplazamiento = 12f };
		filaPie.AddChild(_volver);
		pie.AddChild(filaPie);
		columna.AddChild(pie);
	}
}
