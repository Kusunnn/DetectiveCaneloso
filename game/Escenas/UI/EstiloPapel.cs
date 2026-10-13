using Godot;
using System;

// Estilo del HUD para las pantallas hechas en código: papel, máquina de escribir y acento ámbar.
public static class EstiloPapel
{
	public static readonly Color Ambar = new Color("efca08");
	public static readonly Color Tinta = new Color("20272b");
	public static readonly Color Papel = new Color(0.84f, 0.81f, 0.72f);
	public static readonly Color Rojo = new Color(0.62f, 0.12f, 0.1f);

	// Sin caché estática: los recursos guardados en campos estáticos siguen vivos al cerrar el juego
	public static Font Mono => GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");

	public static Font Titulos
	{
		get
		{
			var titulos = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf") };
			titulos.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };
			return titulos;
		}
	}

	public static Label Texto(string texto, int tam, Color? color = null, bool titulo = false)
	{
		var l = new Label { Text = texto, MouseFilter = Control.MouseFilterEnum.Ignore };
		l.AddThemeFontOverride("font", titulo ? Titulos : Mono);
		l.AddThemeFontSizeOverride("font_size", tam);
		l.AddThemeColorOverride("font_color", color ?? Tinta);
		return l;
	}

	public static StyleBoxFlat Hoja(int margen = 26)
	{
		var fondo = new StyleBoxFlat { BgColor = Papel, ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 10, ShadowOffset = new Vector2(4, 6) };
		fondo.SetContentMarginAll(margen);
		return fondo;
	}

	public static ColorRect Subrayado(float ancho = 150) =>
		new() { Color = Ambar, CustomMinimumSize = new Vector2(ancho, 5), SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin, MouseFilter = Control.MouseFilterEnum.Ignore };

	// Botón de papel: contorno de tinta, ámbar al tener el foco. principal = relleno de tinta.
	public static Button Boton(string texto, bool principal = false, int tam = 18)
	{
		var boton = new Button { Text = texto, CustomMinimumSize = new Vector2(0, 46) };
		boton.AddThemeFontOverride("font", Mono);
		boton.AddThemeFontSizeOverride("font_size", tam);
		var normal = new StyleBoxFlat { BgColor = principal ? Tinta : new Color(0, 0, 0, 0), BorderColor = Tinta };
		normal.SetBorderWidthAll(2);
		normal.SetContentMarginAll(10);
		var foco = new StyleBoxFlat { BgColor = Ambar, BorderColor = Tinta };
		foco.SetBorderWidthAll(3);
		foco.SetContentMarginAll(10);
		var desactivado = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = new Color(Tinta, 0.3f) };
		desactivado.SetBorderWidthAll(2);
		desactivado.SetContentMarginAll(10);
		boton.AddThemeStyleboxOverride("normal", normal);
		boton.AddThemeStyleboxOverride("hover", foco);
		boton.AddThemeStyleboxOverride("focus", foco);
		boton.AddThemeStyleboxOverride("pressed", foco);
		boton.AddThemeStyleboxOverride("disabled", desactivado);
		boton.AddThemeColorOverride("font_color", principal ? Papel : Tinta);
		boton.AddThemeColorOverride("font_hover_color", Tinta);
		boton.AddThemeColorOverride("font_focus_color", Tinta);
		boton.AddThemeColorOverride("font_pressed_color", Tinta);
		boton.AddThemeColorOverride("font_disabled_color", new Color(Tinta, 0.4f));
		return boton;
	}

	// Rango de lupas: "◉◉◉○○" no se ve bien en la fuente; se dibujan lupas con un Control
	public static Control Lupas(int llenas, int total = 5, float tam = 34)
	{
		var c = new Control { CustomMinimumSize = new Vector2(total * (tam + 8), tam), MouseFilter = Control.MouseFilterEnum.Ignore };
		c.Draw += () =>
		{
			for (int i = 0; i < total; i++)
			{
				bool llena = i < llenas;
				var centro = new Vector2(i * (tam + 8) + tam * 0.42f, tam * 0.42f);
				float r = tam * 0.3f;
				var color = llena ? Tinta : new Color(Tinta, 0.25f);
				if (llena) c.DrawCircle(centro, r, new Color(Ambar, 0.85f));
				c.DrawArc(centro, r, 0, Mathf.Tau, 32, color, 3f, true);
				var mango = centro + new Vector2(r * 0.72f, r * 0.72f);
				c.DrawLine(mango, mango + new Vector2(tam * 0.28f, tam * 0.28f), color, 5f, true);
			}
		};
		return c;
	}
}
