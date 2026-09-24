using Godot;
using System;
using System.Collections.Generic;

// Fondo animado del menú de inicio: callejón con una farola que parpadea y una puerta
// en la que, durante los apagones, aparece la silueta de alguien. Se genera por código.
// Diseño: "Detective Caneloso – Inicio" (Claude Design).
public partial class FondoMenu : Control
{
	// Frecuencia de los parpadeos y apagones de la farola.
	[Export(PropertyHint.Enum, "baja,media,alta")]
	public string IntensidadParpadeo { get; set; } = "media";

	// PNG opcional para sustituir la silueta dibujada por código.
	[Export]
	public Texture2D TexturaSilueta { get; set; }

	private static readonly Color Sombra = new("#050608");
	private const string RutaShaderGrano = "res://Escenas/Menus/Fondo/grano_pelicula.gdshader";

	private TextureRect _cono;
	private TextureRect _brillo;
	private Panel _foco;
	private ColorRect _oscuridad;
	private Control _puerta;
	private Control _silueta;

	// Pasos pendientes de la animación de luz: (nivel de luz 0..1, duración en ms).
	private readonly Queue<(float Luz, double Ms)> _pasos = new();
	private double _tiempoRestante = 0.0;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Construir();
	}

	public override void _Process(double delta)
	{
		_tiempoRestante -= delta;
		if (_tiempoRestante > 0.0) return;

		if (_pasos.Count == 0)
		{
			GenerarSecuencia();
		}
		var paso = _pasos.Dequeue();
		PonerLuz(paso.Luz);
		_tiempoRestante = paso.Ms / 1000.0;
	}

	// ---------- Animación de la luz ----------

	private void GenerarSecuencia()
	{
		// Por intensidad: probabilidad de parpadeo y de apagón con silueta.
		(float parpadeo, float apagon) = IntensidadParpadeo switch
		{
			"baja" => (0.1f, 0.03f),
			"alta" => (0.38f, 0.16f),
			_ => (0.22f, 0.08f),
		};

		float r = GD.Randf();
		if (r < apagon)
		{
			// Apagón largo: la silueta aparece en un punto aleatorio de la puerta.
			float x = (float)GD.RandRange(0.3, 0.7);
			_silueta.AnchorLeft = x - 0.24f;
			_silueta.AnchorRight = x + 0.24f;
			Encolar((0.2f, 60), (1.0f, 70), (0.05f, 90), (0.8f, 40), (0.0f, GD.RandRange(1400.0, 2800.0)),
				(0.6f, 50), (0.0f, 120), (1.0f, 80), (0.3f, 50), (1.0f, 400));
		}
		else if (r < parpadeo)
		{
			Encolar((0.3f, 50), (1.0f, 90), (0.15f, 70), (0.95f, 60), (0.4f, 40), (1.0f, 300));
		}
		else
		{
			Encolar(((float)GD.RandRange(0.88, 1.0), GD.RandRange(700.0, 2400.0)));
		}
	}

	private void Encolar(params (float, double)[] pasos)
	{
		foreach (var paso in pasos)
		{
			_pasos.Enqueue(paso);
		}
	}

	private void PonerLuz(float valor)
	{
		_foco.Modulate = new Color(_foco.Modulate, 0.15f + 0.85f * valor);
		_brillo.Modulate = new Color(_brillo.Modulate, valor);
		_cono.Modulate = new Color(_cono.Modulate, valor);
		_oscuridad.Modulate = new Color(_oscuridad.Modulate, (1.0f - valor) * 0.82f);
		_silueta.Modulate = new Color(_silueta.Modulate, valor < 0.35f ? 0.95f : 0.0f);
		_puerta.Modulate = new Color(_puerta.Modulate, 0.5f + (1.0f - valor) * 0.2f + GD.Randf() * 0.05f);
	}

	// ---------- Construcción de la escena ----------

	private void Construir()
	{
		var arriba = new Vector2(0, 0);
		var abajo = new Vector2(0, 1);

		// Cielo y suelo
		Imagen(this, Gradiente(new[] { new Color("#101318"), new Color("#0c0e12"), new Color("#08090b") }, new[] { 0.0f, 0.7f, 1.0f }, false, arriba, abajo), 0, 0, 1, 1);
		Imagen(this, Gradiente(new[] { new Color("#07080a"), new Color("#0d0f13") }, new[] { 0.0f, 1.0f }, false, arriba, abajo), 0, 0.8f, 1, 1);
		var horizonte = new ColorRect { Color = new Color(1, 1, 1, 0.05f) };
		Poner(this, horizonte, 0, 0.8f, 1, 0.8f);
		horizonte.OffsetBottom = 1;

		// Farola: cono de luz, cable, brillo y lámpara
		var luz = new Color(0.84f, 0.87f, 0.91f);
		_cono = Imagen(this, Gradiente(new[] { new Color(luz, 0.16f), new Color(luz, 0.05f), new Color(luz, 0.0f) }, new[] { 0.0f, 0.45f, 1.0f }, true, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.64f)), 0.17f, -0.1f, 1.07f, 1.0f);

		var cable = new ColorRect { Color = new Color("#2a2f36") };
		Poner(this, cable, 0.62f, 0, 0.62f, 0.22f);
		cable.OffsetRight = 1;

		_brillo = Imagen(this, Gradiente(new[] { new Color(0.9f, 0.93f, 0.96f, 0.45f), new Color(0.9f, 0.93f, 0.96f, 0.0f) }, new[] { 0.0f, 1.0f }, true, new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.5f)), 0.62f, 0.22f, 0.62f, 0.22f);
		_brillo.OffsetLeft = -90;
		_brillo.OffsetRight = 90;
		_brillo.OffsetTop = -80;
		_brillo.OffsetBottom = 100;

		_foco = new Panel();
		var estiloFoco = new StyleBoxFlat
		{
			BgColor = new Color("#f1f4f7"),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
		};
		_foco.AddThemeStyleboxOverride("panel", estiloFoco);
		Poner(this, _foco, 0.62f, 0.22f, 0.62f, 0.22f);
		_foco.OffsetLeft = -8;
		_foco.OffsetRight = 8;
		_foco.OffsetBottom = 22;

		_oscuridad = new ColorRect { Color = new Color("#030405"), Modulate = new Color(1, 1, 1, 0) };
		Poner(this, _oscuridad, 0, 0, 1, 1);

		// Puerta iluminada con la silueta
		var grisPuerta = new Color(0.51f, 0.59f, 0.67f);
		Imagen(this, Gradiente(new[] { new Color(grisPuerta, 0.18f), new Color(grisPuerta, 0.0f) }, new[] { 0.0f, 1.0f }, true, new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.5f)), 0.68f, 0.16f, 0.93f, 0.88f);

		_puerta = new Control { ClipContents = true, Modulate = new Color(1, 1, 1, 0.55f) };
		Poner(this, _puerta, 0.74f, 0.24f, 0.87f, 0.8f);
		Imagen(_puerta, Gradiente(new[] { new Color("#9fb0c2"), new Color("#6f8193"), new Color("#4b5968") }, new[] { 0.0f, 0.6f, 1.0f }, false, arriba, abajo), 0, 0, 1, 1);

		_silueta = new Control { Modulate = new Color(1, 1, 1, 0) };
		Poner(_puerta, _silueta, 0.26f, 0.2f, 0.74f, 1.0f);
		if (TexturaSilueta != null)
		{
			var textura = Imagen(_silueta, TexturaSilueta, 0, 0, 1, 1);
			textura.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		}
		else
		{
			_silueta.Draw += DibujarSilueta;
			_silueta.Resized += _silueta.QueueRedraw;
		}

		// Reflejo de la puerta en el suelo, viñeta y grano
		Imagen(this, Gradiente(new[] { new Color(grisPuerta, 0.16f), new Color(grisPuerta, 0.0f) }, new[] { 0.0f, 1.0f }, false, arriba, abajo), 0.72f, 0.8f, 0.89f, 1.0f);
		Imagen(this, Gradiente(new[] { new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.78f) }, new[] { 0.0f, 0.4f, 1.0f }, true, new Vector2(0.55f, 0.45f), new Vector2(0.55f, 1.15f)), 0, 0, 1, 1);

		var grano = new ColorRect { Material = new ShaderMaterial { Shader = GD.Load<Shader>(RutaShaderGrano) } };
		Poner(this, grano, 0, 0, 1, 1);
	}

	private void DibujarSilueta()
	{
		var s = _silueta.Size;
		_silueta.DrawRect(new Rect2(s.X * 0.30f, s.Y * 0.01f, s.X * 0.40f, s.Y * 0.09f), Sombra); // sombrero
		DibujarElipse(new Rect2(s.X * 0.12f, s.Y * 0.085f, s.X * 0.76f, s.Y * 0.03f)); // ala
		DibujarElipse(new Rect2(s.X * 0.33f, s.Y * 0.10f, s.X * 0.34f, s.Y * 0.15f)); // cabeza

		float y0 = s.Y * 0.23f;
		float alto = s.Y * 0.77f;
		var cuerpo = new[]
		{
			new Vector2(0.32f, 0), new Vector2(0.68f, 0), new Vector2(0.96f, 0.18f),
			new Vector2(0.92f, 1), new Vector2(0.08f, 1), new Vector2(0.04f, 0.18f),
		};
		for (int i = 0; i < cuerpo.Length; i++)
		{
			cuerpo[i] = new Vector2(cuerpo[i].X * s.X, y0 + cuerpo[i].Y * alto);
		}
		_silueta.DrawColoredPolygon(cuerpo, Sombra);
	}

	private void DibujarElipse(Rect2 rect)
	{
		var puntos = new Vector2[24];
		var centro = rect.GetCenter();
		for (int i = 0; i < puntos.Length; i++)
		{
			float angulo = Mathf.Tau * i / puntos.Length;
			puntos[i] = centro + new Vector2(Mathf.Cos(angulo) * rect.Size.X / 2, Mathf.Sin(angulo) * rect.Size.Y / 2);
		}
		_silueta.DrawColoredPolygon(puntos, Sombra);
	}

	// ---------- Utilidades ----------

	// Añade un control anclado por proporciones (0..1) que no bloquea el ratón.
	private static T Poner<T>(Node padre, T control, float izquierda, float arriba, float derecha, float abajo) where T : Control
	{
		control.AnchorLeft = izquierda;
		control.AnchorTop = arriba;
		control.AnchorRight = derecha;
		control.AnchorBottom = abajo;
		control.OffsetLeft = 0;
		control.OffsetTop = 0;
		control.OffsetRight = 0;
		control.OffsetBottom = 0;
		control.MouseFilter = MouseFilterEnum.Ignore;
		padre.AddChild(control);
		return control;
	}

	private static TextureRect Imagen(Node padre, Texture2D textura, float izquierda, float arriba, float derecha, float abajo)
	{
		var imagen = new TextureRect
		{
			Texture = textura,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
		};
		return Poner(padre, imagen, izquierda, arriba, derecha, abajo);
	}

	private static GradientTexture2D Gradiente(Color[] colores, float[] posiciones, bool radial, Vector2 desde, Vector2 hasta)
	{
		var gradiente = new Gradient { Offsets = posiciones, Colors = colores };
		var textura = new GradientTexture2D
		{
			Gradient = gradiente,
			Width = 256,
			Height = 256,
			FillFrom = desde,
			FillTo = hasta,
		};
		if (radial)
		{
			textura.Fill = GradientTexture2D.FillEnum.Radial;
		}
		return textura;
	}
}
