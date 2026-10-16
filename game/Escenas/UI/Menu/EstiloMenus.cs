using Godot;
using System;
using System.Collections.Generic;

// Estilo común de los menús (diseño "Detective Caneloso – Menus" de Claude Design).
// Los diseños están a 960×540 y el juego a 1920×1080: todas las medidas del diseño van ×2.
public static class EstiloMenus
{
	public static readonly Color Ambar = new("#EFCA08");
	public static readonly Color Texto = new("#E8EBEF");
	public static readonly Color TextoClaro = new("#EEF1F4");
	public static readonly Color Coral = new("#F0645C");      // oklch(0.7 0.19 25): selección y hover
	public static readonly Color Rojo = new("#E0463C");       // oklch(0.64 0.2 25): rayas y avisos
	public static readonly Color RojoSello = new("#D6443F");  // oklch(0.62 0.19 25)
	public static readonly Color BordeSello = new("#C93B36"); // oklch(0.58 0.19 25)
	public static readonly Color Gris = new("#8A939E");
	public static readonly Color GrisClaro = new("#AEB6BF");
	public static readonly Color GrisOscuro = new("#6B747F");
	public static readonly Color Numero = new("#5F6873");
	public static readonly Color Pista = new("#2A2F36");
	public static readonly Color FondoPanel = new("#0B0D10");
	public static readonly Color Linea = new(1, 1, 1, 0.08f);
	public static readonly Color LineaSuave = new(1, 1, 1, 0.04f);

	private const string Fuentes = "res://Assets/Fuentes/";
	private static readonly Dictionary<string, Font> _cache = new();

	// Big Shoulders Display (variable): 500 Medium, 800 ExtraBold, 900 Black.
	// espaciadoEm = letter-spacing del diseño en em; se convierte a píxeles según el tamaño.
	public static Font Display(int peso, int tam = 0, float espaciadoEm = 0f)
	{
		int espaciado = Mathf.RoundToInt(espaciadoEm * tam);
		string clave = $"display|{peso}|{espaciado}";
		if (_cache.TryGetValue(clave, out Font f)) return f;
		var fuente = new FontVariation
		{
			BaseFont = Cargar("BigShouldersDisplay-Variable.ttf"),
			SpacingGlyph = espaciado,
			VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), peso } },
		};
		_cache[clave] = fuente;
		return fuente;
	}

	public static Font Mono(int tam = 0, float espaciadoEm = 0f)
	{
		int espaciado = Mathf.RoundToInt(espaciadoEm * tam);
		string clave = $"mono|{espaciado}";
		if (_cache.TryGetValue(clave, out Font f)) return f;
		var fuente = new FontVariation { BaseFont = Cargar("IBMPlexMono-Regular.ttf"), SpacingGlyph = espaciado };
		_cache[clave] = fuente;
		return fuente;
	}

	private static Font Cargar(string archivo)
	{
		string ruta = Fuentes + archivo;
		return ResourceLoader.Exists(ruta) ? GD.Load<Font>(ruta) : ThemeDB.FallbackFont;
	}

	public static Label Etiqueta(string texto, Font fuente, int tam, Color color)
	{
		var l = new Label { Text = texto, MouseFilter = Control.MouseFilterEnum.Ignore };
		l.AddThemeFontOverride("font", fuente);
		l.AddThemeFontSizeOverride("font_size", tam);
		l.AddThemeColorOverride("font_color", color);
		return l;
	}

	// Título grande en ámbar con la raya roja debajo (PAUSA, OPCIONES, LOS RESPONSABLES...)
	public static VBoxContainer Titulo(string texto, int tam, int separacion = 20)
	{
		var bloque = Columna(separacion);
		var titulo = Etiqueta(texto, Display(900), tam, Ambar);
		titulo.AddThemeConstantOverride("line_spacing", -Mathf.RoundToInt(tam * 0.14f));
		bloque.AddChild(titulo);
		bloque.AddChild(Raya(80, 4, Rojo));
		return bloque;
	}

	public static ColorRect Raya(float ancho, float alto, Color color)
	{
		return new ColorRect
		{
			Color = color,
			CustomMinimumSize = new Vector2(ancho, alto),
			SizeFlagsHorizontal = ancho > 0 ? Control.SizeFlags.ShrinkBegin : Control.SizeFlags.Fill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
	}

	public static VBoxContainer Columna(int separacion)
	{
		var v = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		v.AddThemeConstantOverride("separation", separacion);
		return v;
	}

	public static HBoxContainer Fila(int separacion)
	{
		var h = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		h.AddThemeConstantOverride("separation", separacion);
		return h;
	}

	// Encabezado de bloque: "01  GENERAL" con línea inferior
	public static VBoxContainer Seccion(string numero, string texto)
	{
		var bloque = Columna(20);
		var fila = Fila(24);
		fila.AddChild(Etiqueta(numero, Mono(22, 0.24f), 22, Numero));
		fila.AddChild(Etiqueta(texto, Mono(22, 0.24f), 22, Gris));
		bloque.AddChild(fila);
		bloque.AddChild(Raya(0, 2, Linea));
		return bloque;
	}

	// Etiqueta con punto rojo: "● ATENCIÓN", "● CRÉDITOS"
	public static HBoxContainer Marca(string texto, Color colorTexto, float espaciadoEm = 0.24f)
	{
		var fila = Fila(24);
		var punto = new Panel { CustomMinimumSize = new Vector2(12, 12), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
		var estilo = new StyleBoxFlat { BgColor = Rojo };
		estilo.SetCornerRadiusAll(6);
		punto.AddThemeStyleboxOverride("panel", estilo);
		fila.AddChild(punto);
		fila.AddChild(Etiqueta(texto, Mono(22, espaciadoEm), 22, colorTexto));
		return fila;
	}

	// Fondo de las pantallas: degradado oscuro (opcional) y viñeta
	public static void Fondo(Control padre, bool opaco = true, float oscurecer = 0f)
	{
		if (opaco)
		{
			var gradiente = new Gradient
			{
				Offsets = new[] { 0f, 0.7f, 1f },
				Colors = new[] { new Color("#101318"), new Color("#0c0e12"), new Color("#08090b") },
			};
			padre.AddChild(Imagen(new GradientTexture2D { Gradient = gradiente, FillTo = new Vector2(0, 1), Width = 8, Height = 256 }));
		}
		if (oscurecer > 0f)
		{
			var capa = new ColorRect { Color = new Color(0.012f, 0.016f, 0.02f, oscurecer), MouseFilter = Control.MouseFilterEnum.Stop };
			capa.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			padre.AddChild(capa);
		}
		var vineta = new Gradient
		{
			Offsets = new[] { 0f, 0.4f, 1f },
			Colors = new[] { new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.78f) },
		};
		padre.AddChild(Imagen(new GradientTexture2D
		{
			Gradient = vineta, Fill = GradientTexture2D.FillEnum.Radial, Width = 256, Height = 256,
			FillFrom = new Vector2(0.55f, 0.45f), FillTo = new Vector2(0.55f, 1.15f),
		}));
	}

	private static TextureRect Imagen(Texture2D textura)
	{
		var imagen = new TextureRect
		{
			Texture = textura,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		imagen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		return imagen;
	}

	// ---------- Botones simples ----------

	private static void SinFondo(Button boton)
	{
		var vacio = new StyleBoxEmpty();
		foreach (string estado in new[] { "normal", "hover", "pressed", "focus", "disabled", "hover_pressed" })
			boton.AddThemeStyleboxOverride(estado, vacio);
	}

	private static void Colores(Button boton, Color normal, Color resaltado)
	{
		boton.AddThemeColorOverride("font_color", normal);
		foreach (string estado in new[] { "font_hover_color", "font_focus_color", "font_pressed_color", "font_hover_pressed_color" })
			boton.AddThemeColorOverride(estado, resaltado);
	}

	// Botón de texto: CANCELAR, Restaurar predeterminados. Se pone ámbar al seleccionarlo.
	public static Button BotonTexto(string texto, int tam, float espaciadoEm = 0.08f)
	{
		var boton = new Button { Text = texto, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
		boton.AddThemeFontOverride("font", Display(800, tam, espaciadoEm));
		boton.AddThemeFontSizeOverride("font_size", tam);
		SinFondo(boton);
		Colores(boton, Texto, Ambar);
		return boton;
	}

	// Botón principal ámbar: "Guardar y volver", "GUARDAR Y SALIR"
	public static Button BotonPrincipal(string texto, int tam, float espaciadoEm = 0.08f)
	{
		var boton = new Button { Text = texto, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
		boton.AddThemeFontOverride("font", Display(800, tam, espaciadoEm));
		boton.AddThemeFontSizeOverride("font_size", tam);
		var normal = Caja(Ambar, Ambar);
		var activo = Caja(Coral, Coral);
		boton.AddThemeStyleboxOverride("normal", normal);
		foreach (string estado in new[] { "hover", "pressed", "focus", "hover_pressed" })
			boton.AddThemeStyleboxOverride(estado, activo);
		Colores(boton, FondoPanel, FondoPanel);
		return boton;
	}

	// Botón de peligro con borde rojo: "SALIR SIN GUARDAR". Al seleccionarlo se rellena.
	public static Button BotonPeligro(string texto, int tam, float espaciadoEm = 0.08f)
	{
		var boton = new Button { Text = texto, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
		boton.AddThemeFontOverride("font", Display(800, tam, espaciadoEm));
		boton.AddThemeFontSizeOverride("font_size", tam);
		boton.AddThemeStyleboxOverride("normal", Caja(new Color(0, 0, 0, 0), Rojo));
		var activo = Caja(Rojo, Rojo);
		foreach (string estado in new[] { "hover", "pressed", "focus", "hover_pressed" })
			boton.AddThemeStyleboxOverride(estado, activo);
		Colores(boton, Coral, FondoPanel);
		return boton;
	}

	private static StyleBoxFlat Caja(Color fondo, Color borde)
	{
		var caja = new StyleBoxFlat { BgColor = fondo, BorderColor = borde };
		caja.SetBorderWidthAll(2);
		caja.ContentMarginLeft = caja.ContentMarginRight = 32;
		caja.ContentMarginTop = caja.ContentMarginBottom = 10;
		return caja;
	}

	// ---------- Deslizador del diseño: línea fina y rombo ámbar ----------

	private static ImageTexture _rombo;

	public static HSlider Deslizador(double minimo, double maximo, double paso)
	{
		var deslizador = new HSlider
		{
			MinValue = minimo, MaxValue = maximo, Step = paso,
			CustomMinimumSize = new Vector2(0, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		var pista = new StyleBoxFlat { BgColor = Pista, ContentMarginTop = 2, ContentMarginBottom = 2 };
		var lleno = new StyleBoxFlat { BgColor = Ambar, ContentMarginTop = 2, ContentMarginBottom = 2 };
		deslizador.AddThemeStyleboxOverride("slider", pista);
		deslizador.AddThemeStyleboxOverride("grabber_area", lleno);
		deslizador.AddThemeStyleboxOverride("grabber_area_highlight", lleno);
		_rombo ??= Rombo(26, Ambar);
		deslizador.AddThemeIconOverride("grabber", _rombo);
		deslizador.AddThemeIconOverride("grabber_highlight", _rombo);
		deslizador.AddThemeIconOverride("grabber_disabled", _rombo);
		deslizador.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		return deslizador;
	}

	private static ImageTexture Rombo(int tam, Color color)
	{
		var imagen = Image.CreateEmpty(tam, tam, false, Image.Format.Rgba8);
		float centro = (tam - 1) / 2f;
		for (int y = 0; y < tam; y++)
			for (int x = 0; x < tam; x++)
			{
				float d = Mathf.Abs(x - centro) + Mathf.Abs(y - centro);
				float alfa = Mathf.Clamp(centro + 0.5f - d, 0f, 1f);
				imagen.SetPixel(x, y, new Color(color, alfa));
			}
		return ImageTexture.CreateFromImage(imagen);
	}
}
