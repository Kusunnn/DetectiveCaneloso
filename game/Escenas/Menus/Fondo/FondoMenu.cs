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
	private const string RutaShaderDestello = "res://Escenas/Menus/Fondo/destello_lupa.gdshader";
	private const string RutaCaneloso = "res://Assets/Menu/caneloso.png";
	private const string RutaCapas = "res://Assets/Menu/Caneloso/";

	// Alto de Caneloso en pantalla (a 1080p) y duración del ciclo de reposo
	private const float AltoFigura = 540f;
	private const double Ciclo = 3.0;

	private TextureRect _cono;
	private TextureRect _brillo;
	private Panel _foco;
	private ColorRect _oscuridad;
	private Control _puerta;
	private Control _figura;
	private TextureRect _imagenFigura;
	private TextureRect _sombraFigura;
	private double _tiempo;

	// Caneloso por capas (render de Assets/Menu/Blender/caneloso_menu.py): un nodo por capa con su pivote
	private CanvasGroup _grupo;
	private float _escala = 1f;
	private readonly Dictionary<string, Node2D> _partes = new();
	private readonly Dictionary<string, Vector2> _base = new();
	private Vector2 _cuello;
	private Sprite2D _brilloAro;
	private ShaderMaterial _destello;
	private bool _aContraluz;

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
		float balanceo = Mathf.Sin(t * 0.7f) * 1.6f;
		if (_grupo != null)
		{
			_grupo.RotationDegrees = balanceo;
			AnimarPartes((float)(_tiempo % Ciclo));
		}
		else
		{
			_figura.RotationDegrees = balanceo;
		}

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
		_aContraluz = valor < 0.35f;
		if (_grupo != null)
		{
			_grupo.Modulate = new Color(brillo, brillo, brillo);
		}
		else
		{
			_imagenFigura.Modulate = new Color(brillo, brillo, brillo);
		}
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
	// los pies al 15 % del borde inferior y centrado con la puerta. Si hay capas (Assets/Menu/Caneloso,
	// render de Assets/Menu/Blender/caneloso_menu.py), se arma por partes y respira; si no, es la imagen fija.
	private void ConstruirFigura()
	{
		var textura = TexturaSilueta ?? GD.Load<Texture2D>(RutaCaneloso);
		float alto = AltoFigura;
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
		if (TexturaSilueta == null && ConstruirCapas(centroPuerta))
		{
			return;
		}
		_imagenFigura = Imagen(_figura, textura, 0, 0, 1, 1);
	}

	// Un CanvasGroup con un Sprite2D por capa: así los bordes que se traslapan no se oscurecen y la silueta
	// de los apagones se aplica al grupo entero. Cada capa cuelga de un Node2D puesto en su pivote.
	private bool ConstruirCapas(float centroPuerta)
	{
		using var archivo = FileAccess.Open(RutaCapas + "capas.json", FileAccess.ModeFlags.Read);
		if (archivo == null)
		{
			return false;
		}
		var datos = Json.ParseString(archivo.GetAsText()).AsGodotDictionary();
		var lienzo = Vec(datos["lienzo"]);
		var pies = Vec(datos["pies"]);
		_escala = AltoFigura / lienzo.Y;

		// El control de la figura queda con el tamaño del lienzo escalado y los pies en el ancla
		_figura.OffsetLeft = -pies.X * _escala;
		_figura.OffsetRight = (lienzo.X - pies.X) * _escala;
		_figura.OffsetTop = -pies.Y * _escala;
		_figura.OffsetBottom = (lienzo.Y - pies.Y) * _escala;

		_grupo = new CanvasGroup { Name = "Figura", FitMargin = 8f, ClearMargin = 8f };
		_grupo.Position = pies * _escala;
		_grupo.Scale = new Vector2(_escala, _escala);
		_figura.AddChild(_grupo);

		// Coordenadas dentro del grupo: píxeles del lienzo con el origen en los pies
		var pivotes = new Dictionary<string, Vector2>();
		foreach (var capa in datos["capas"].AsGodotArray())
		{
			var c = capa.AsGodotDictionary();
			string nombre = c["nombre"].AsString();
			var pivote = Vec(c["pivote"]);
			pivotes[nombre] = pivote;
			string padre = c["padre"].VariantType == Variant.Type.Nil ? null : c["padre"].AsString();
			Node padreNodo = padre != null && _partes.TryGetValue(padre, out var p) ? p : _grupo;
			var origen = padre != null ? pivotes[padre] : pies;

			var nodo = new Node2D { Name = nombre, Position = pivote - origen };
			padreNodo.AddChild(nodo);
			var sprite = new Sprite2D
			{
				Texture = GD.Load<Texture2D>(RutaCapas + c["archivo"].AsString()),
				Centered = false,
				Position = Vec(c["offset"]) - pivote,
			};
			nodo.AddChild(sprite);
			_partes[nombre] = nodo;
			_base[nombre] = nodo.Position;

			if (nombre == "mascara_vidrio")
			{
				_destello = new ShaderMaterial { Shader = GD.Load<Shader>(RutaShaderDestello) };
				sprite.Material = _destello;
				// Brillo pequeño en el borde del aro, arriba a la derecha del vidrio
				var tam = Vec(c["tam"]);
				var brillo = new Gradient { Offsets = new[] { 0f, 0.25f, 1f }, Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.5f), new Color(1, 1, 1, 0) } };
				_brilloAro = new Sprite2D
				{
					Texture = Gradiente(brillo.Colors, brillo.Offsets, true, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f)),
					Position = sprite.Position + new Vector2(tam.X * 0.88f, tam.Y * 0.14f),
					Scale = Vector2.One * (tam.X * 0.35f / 256f),
					Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
					Modulate = new Color(1, 1, 1, 0),
				};
				nodo.AddChild(_brilloAro);
			}
		}
		if (_partes.TryGetValue("ojos_cerrados", out var cerrados))
		{
			cerrados.Visible = false;
		}
		_cuello = pivotes.GetValueOrDefault("cabeza", pies) - pies;
		return true;
	}

	// Ciclo de reposo de 3 s (valores a 1080p), calculado a partir del tiempo: sin AnimationPlayer
	private void AnimarPartes(float s)
	{
		float t = s / (float)Ciclo;
		float ang = Mathf.Tau * t;
		float px = 1f / _escala; // un píxel de pantalla en píxeles del lienzo
		float respira = (Mathf.Sin(ang) + 1f) / 2f;

		if (_partes.TryGetValue("cuerpo", out var cuerpo))
		{
			// Escala en Y con pivote en la cadera (el nodo del cuerpo está en ella)
			float k = 1f + 0.012f * respira;
			cuerpo.Scale = new Vector2(1f, k);
		}
		var sube = new Vector2(0, -3f * respira * px);

		float giroCabeza = Mathf.DegToRad(2.4f) * Mathf.Sin(ang + 0.9f);
		var cuello = _cuello + sube + new Vector2(0, 1f * respira * px);
		if (_partes.TryGetValue("cabeza", out var cabeza))
		{
			cabeza.Position = _base["cabeza"] + sube + new Vector2(0, 1f * respira * px);
			cabeza.Rotation = giroCabeza;
		}
		// Las orejas siguen a la cabeza (giran con ella alrededor del cuello) y cuelgan con su propio vaivén
		float oreja = Mathf.DegToRad(4f) * Mathf.Sin(ang + 0.2f);
		foreach (var (nombre, signo) in new[] { ("oreja_lejana", 1f), ("oreja_cercana", -1f) })
		{
			if (!_partes.TryGetValue(nombre, out var o)) continue;
			o.Position = cuello + (_base[nombre] - _cuello).Rotated(giroCabeza);
			o.Rotation = giroCabeza + signo * oreja;
		}
		if (_partes.TryGetValue("ceja_arriba", out var ceja))
		{
			ceja.Position = _base["ceja_arriba"] + new Vector2(0, -4f * Campana(s, 0.36f, 1.05f) * px);
		}
		if (_partes.TryGetValue("ojos_cerrados", out var cerrados) && _partes.TryGetValue("ojos_abiertos", out var abiertos))
		{
			bool parpadea = Mathf.Abs(s - 2.1f) < 0.07f;
			cerrados.Visible = parpadea;
			abiertos.Visible = !parpadea;
		}
		if (_partes.TryGetValue("brazo_lupa", out var brazo))
		{
			brazo.Position = _base["brazo_lupa"] + sube;
			brazo.Rotation = Mathf.DegToRad(1.5f) * Mathf.Sin(ang + 0.4f);
		}
		if (_destello != null)
		{
			// Entre 1.2 s y 1.86 s la franja cruza el vidrio y el borde del aro brilla; a contraluz no hay destello
			float avance = (s - 1.2f) / 0.66f;
			float luz = _aContraluz ? 0f : 1f;
			_destello.SetShaderParameter("avance", Mathf.Lerp(-0.15f, 1.15f, avance));
			_destello.SetShaderParameter("intensidad", avance is >= 0f and <= 1f ? luz : 0f);
			_brilloAro.Modulate = new Color(1, 1, 1, Campana(s, 1.35f, 1.86f) * luz);
		}
	}

	// 0 fuera de [desde, hasta]; sube y baja suave dentro
	private static float Campana(float s, float desde, float hasta)
	{
		if (s < desde || s > hasta) return 0f;
		return Mathf.Sin(Mathf.Pi * (s - desde) / (hasta - desde));
	}

	private static Vector2 Vec(Variant v)
	{
		var a = v.AsGodotArray();
		return new Vector2((float)a[0].AsDouble(), (float)a[1].AsDouble());
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
