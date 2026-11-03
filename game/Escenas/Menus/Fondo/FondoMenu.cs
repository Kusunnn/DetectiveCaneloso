using Godot;
using System;
using System.Collections.Generic;

// Fondo animado del menú de inicio: callejón con una farola que parpadea y, delante de la puerta
// iluminada, Caneloso con su lupa: a contraluz (negro) en los apagones e iluminado cuando hay luz.
// Se genera por código. Diseño: "Detective Caneloso – Inicio" (Claude Design).
public partial class FondoMenu : Control
{
	// Frecuencia de los parpadeos y apagones de la farola.
	[Export(PropertyHint.Enum, "baja,media,alta")]
	public string IntensidadParpadeo { get; set; } = "media";

	// Imagen del personaje delante de la puerta (por defecto, Caneloso)
	[Export]
	public Texture2D TexturaSilueta { get; set; }

	private const string RutaShaderGrano = "res://Escenas/Menus/Fondo/grano_pelicula.gdshader";
	private const string RutaCaneloso = "res://Assets/Menu/caneloso.png";

	private TextureRect _cono;
	private TextureRect _brillo;
	private Panel _foco;
	private ColorRect _oscuridad;
	private Control _puerta;
	private Control _figura;
	private TextureRect _imagenFigura;
	private TextureRect _sombraFigura;
	private double _tiempo;

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
		// Balanceo suave del personaje (como en el diseño)
		_tiempo += delta;
		float t = (float)_tiempo;
		_figura.RotationDegrees = Mathf.Sin(t * 0.7f) * 1.6f;

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
			// Apagón largo: Caneloso queda a contraluz frente a la puerta
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
		// Con poca luz es solo una silueta negra; con luz, se le ve (algo más brillante que la imagen)
		float brillo = valor < 0.35f ? 0.03f : 0.55f + 0.5f * valor;
		_imagenFigura.Modulate = new Color(brillo, brillo, brillo);
		_sombraFigura.Modulate = new Color(1, 1, 1, 0.4f + 0.6f * valor);
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

		// Puerta iluminada
		var grisPuerta = new Color(0.51f, 0.59f, 0.67f);
		Imagen(this, Gradiente(new[] { new Color(grisPuerta, 0.18f), new Color(grisPuerta, 0.0f) }, new[] { 0.0f, 1.0f }, true, new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.5f)), 0.68f, 0.16f, 0.93f, 0.88f);

		_puerta = new Control { ClipContents = true, Modulate = new Color(1, 1, 1, 0.55f) };
		Poner(this, _puerta, 0.74f, 0.24f, 0.87f, 0.8f);
		Imagen(_puerta, Gradiente(new[] { new Color("#9fb0c2"), new Color("#6f8193"), new Color("#4b5968") }, new[] { 0.0f, 0.6f, 1.0f }, false, arriba, abajo), 0, 0, 1, 1);

		// Reflejo de la puerta en el suelo
		Imagen(this, Gradiente(new[] { new Color(grisPuerta, 0.16f), new Color(grisPuerta, 0.0f) }, new[] { 0.0f, 1.0f }, false, arriba, abajo), 0.72f, 0.8f, 0.89f, 1.0f);

		ConstruirFigura();

		// Viñeta y grano
		Imagen(this, Gradiente(new[] { new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.78f) }, new[] { 0.0f, 0.4f, 1.0f }, true, new Vector2(0.55f, 0.45f), new Vector2(0.55f, 1.15f)), 0, 0, 1, 1);

		var grano = new ColorRect { Material = new ShaderMaterial { Shader = GD.Load<Shader>(RutaShaderGrano) } };
		Poner(this, grano, 0, 0, 1, 1);
	}

	// Caneloso delante de la puerta (medidas del diseño a 1920×1080): 540 px de alto (50vh),
	// los pies al 15 % del borde inferior y centrado con la puerta. La lupa ya viene en la imagen,
	// en su mano (la hace Herramientas/retratos.gd con el nombre "menu").
	private void ConstruirFigura()
	{
		var textura = TexturaSilueta ?? GD.Load<Texture2D>(RutaCaneloso);
		float alto = 540f;
		float ancho = alto * textura.GetWidth() / textura.GetHeight();
		const float centroPuerta = 0.805f;

		// Sombra elíptica a sus pies
		var sombra = new Gradient { Offsets = new[] { 0f, 0.7f }, Colors = new[] { new Color(0, 0, 0, 0.75f), new Color(0, 0, 0, 0) } };
		_sombraFigura = Imagen(this, Gradiente(sombra.Colors, sombra.Offsets, true, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f)), centroPuerta, 0.85f, centroPuerta, 0.85f);
		_sombraFigura.OffsetLeft = -130;
		_sombraFigura.OffsetRight = 130;
		_sombraFigura.OffsetTop = -3;
		_sombraFigura.OffsetBottom = 29;

		_figura = new Control();
		Poner(this, _figura, centroPuerta, 0.85f, centroPuerta, 0.85f);
		_figura.OffsetLeft = -ancho / 2f;
		_figura.OffsetRight = ancho / 2f;
		_figura.OffsetTop = -alto;
		_figura.PivotOffset = new Vector2(ancho / 2f, alto); // se balancea desde los pies
		_imagenFigura = Imagen(_figura, textura, 0, 0, 1, 1);
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
