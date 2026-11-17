using Godot;
using System;
using System.Collections.Generic;

// Cartel, letrero, nota o placa con texto que SIEMPRE cabe en su marco.
// Orden de ajuste: 1) el marco crece al alto del texto, 2) salto de línea automático al ancho
// del marco, 3) la fuente baja hasta un mínimo legible. El texto queda centrado con margen.
//
// «Estilo» decide de qué está hecho. Clasico es la hoja lisa de siempre; los demás ocupan exactamente el mismo
// ancho y alto (así no se salen del poste o la pared donde ya estaban) y se arman como una sola malla:
//   Madera   → tabla con vetas, tablones, marco y clavos; letras pintadas con contorno
//   Placa    → lámina de metal con filete y tornillos (avisos, «Se vende», peligro)
//   Papel    → hoja un poco chueca, pegada con cinta; letra manuscrita
//   Pizarron → pizarrón con marco de madera, repisa y gis; letra manuscrita
[Tool]
public partial class CartelPared : Node3D
{
	public enum EstiloCartel { Clasico, Madera, Placa, Papel, Pizarron }

	[Export(PropertyHint.MultilineText)] public string Texto = "";
	[Export] public float Ancho = 0.5f;           // Ancho del marco (m)
	[Export] public float AltoFijo = 0f;          // 0 = el marco se adapta al texto; > 0 = marco fijo (placas)
	[Export] public float AltoMaximo = 1.2f;      // Límite si el marco se adapta
	[Export] public float Margen = 0.03f;         // Margen interior en los 4 lados (m)
	[Export] public int TamFuente = 40;
	[Export] public int TamMinimo = 14;
	[Export] public float TamPixel = 0.0025f;
	[Export] public Color ColorPapel = new Color(0.86f, 0.83f, 0.72f);
	[Export] public Color ColorTexto = new Color(0.12f, 0.1f, 0.08f);
	[Export] public bool ConFondo = true;
	[Export] public EstiloCartel Estilo = EstiloCartel.Clasico;

	private const string FuenteRotulo = "res://Assets/Fuentes/BigShouldersDisplay-Variable.ttf";
	private const string FuenteMano = "res://Assets/Fuentes/Caveat-Variable.ttf";
	private static readonly Dictionary<string, Font> Fuentes = new();
	private static NoiseTexture2D _vetas;

	public override void _Ready()
	{
		Construir();
		// En el juego, un cartel colgado en una pared de troncos puede quedar medio metido en ella: se despega solo
		if (!Engine.IsEditorHint() && ConFondo && Mathf.Abs(GlobalBasis.Z.Normalized().Y) < 0.7f)
			Callable.From(Despegar).CallDeferred();
	}

	// Busca qué tanto sobresale la pared por delante del cartel y adelanta el dibujo eso más medio centímetro.
	// Solo cuenta superficies paralelas al cartel (la pared), no lo que esté enfrente (un mueble, un personaje).
	private async void Despegar()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!IsInsideTree()) return;
		var espacio = GetWorld3D().DirectSpaceState;
		Vector3 frente = GlobalBasis.Z.Normalized();
		float ancho = Ancho * GlobalBasis.X.Length(), alto = 0f;
		foreach (Node hijo in GetChildren())
			if (hijo is MeshInstance3D m && m.HasMeta("generado_cartel")) alto = m.GetAabb().Size.Y * GlobalBasis.Y.Length();
		if (alto <= 0f) return;
		float sobresale = -1f;
		for (int i = 0; i < 5; i++)
			for (int j = 0; j < 3; j++)
			{
				Vector3 punto = GlobalPosition + GlobalBasis.X.Normalized() * ancho * (i / 4f - 0.5f) * 0.9f
					+ GlobalBasis.Y.Normalized() * alto * (j / 2f - 0.5f) * 0.9f;
				var rayo = PhysicsRayQueryParameters3D.Create(punto + frente * 0.14f, punto - frente * 0.1f, 1u | 2u);
				var golpe = espacio.IntersectRay(rayo);
				if (golpe.Count == 0 || golpe["normal"].AsVector3().Dot(frente) < 0.8f) continue;
				sobresale = Mathf.Max(sobresale, (golpe["position"].AsVector3() - GlobalPosition).Dot(frente));
			}
		if (sobresale <= -0.004f) return;
		float empuje = Mathf.Min(sobresale + 0.006f, 0.13f) / Mathf.Max(0.01f, GlobalBasis.Z.Length());
		foreach (Node hijo in GetChildren())
			if (hijo is Node3D pieza && hijo.HasMeta("generado_cartel")) pieza.Position += new Vector3(0, 0, empuje);
	}

	public void Construir()
	{
		foreach (Node hijo in GetChildren())
		{
			if (hijo.HasMeta("generado_cartel")) { RemoveChild(hijo); hijo.QueueFree(); }
		}
		if (string.IsNullOrEmpty(Texto)) return;

		// El tamaño del cartel se calcula siempre como el clásico: cambiar de estilo no lo agranda ni lo achica
		var (tam, altoTexto) = Ajustar(ThemeDB.FallbackFont, Ancho - 2f * Margen, (AltoFijo > 0f ? AltoFijo : AltoMaximo) - 2f * Margen);
		float alto = AltoFijo > 0f ? AltoFijo : altoTexto + 2f * Margen;
		EstiloCartel estilo = ConFondo ? Estilo : EstiloCartel.Clasico;

		if (estilo == EstiloCartel.Clasico)
		{
			if (ConFondo)
			{
				var papel = new MeshInstance3D
				{
					Mesh = new BoxMesh
					{
						Size = new Vector3(Ancho, alto, 0.01f),
						Material = new StandardMaterial3D { AlbedoColor = ColorPapel, Roughness = 0.9f },
					},
				};
				papel.SetMeta("generado_cartel", true);
				AddChild(papel);
			}
			Rotular(this, ThemeDB.FallbackFont, tam, Ancho - 2f * Margen, 0.007f, ColorTexto, 0, Colors.Black);
			return;
		}

		// Los demás estilos: una sola malla y su propia fuente, hijos directos del cartel (así los reconoce la auditoría)
		Node3D cuerpo = this;
		float inclinacion = 0f;
		float borde = Mathf.Clamp(Mathf.Min(Ancho, alto) * 0.07f, 0.006f, 0.06f);
		Font fuente = estilo is EstiloCartel.Papel or EstiloCartel.Pizarron ? Fuente(FuenteMano, 700) : Fuente(FuenteRotulo, 800);
		float margen = estilo == EstiloCartel.Papel ? Margen * 0.6f : borde + Margen * 0.25f;
		float anchoUtil = Mathf.Max(0.02f, Ancho - 2f * margen);
		// La fuente del estilo puede ser más angosta: se permite crecer hasta llenar el mismo marco
		int tamEstilo = AjustarAlMarco(fuente, anchoUtil, alto - 2f * margen, Mathf.RoundToInt(TamFuente * 2.2f));

		var piezas = new Piezas();
		float frente;
		switch (estilo)
		{
			case EstiloCartel.Madera:
				frente = ArmarMadera(piezas, alto, borde);
				Rotular(cuerpo, fuente, tamEstilo, anchoUtil, frente, ColorTexto.Lightened(0.25f), Mathf.Max(4, tamEstilo / 4), ColorPapel.Darkened(0.72f));
				break;
			case EstiloCartel.Placa:
				frente = ArmarPlaca(piezas, alto);
				Rotular(cuerpo, fuente, tamEstilo, anchoUtil, frente, ColorTexto, 0, Colors.Black);
				break;
			case EstiloCartel.Pizarron:
				frente = ArmarPizarron(piezas, alto, borde);
				Rotular(cuerpo, fuente, tamEstilo, anchoUtil, frente, new Color(ColorTexto, 0.92f), 0, Colors.Black);
				break;
			default:
				frente = ArmarPapel(piezas, alto);
				// Chueco siempre igual para el mismo cartel (depende de su texto, no del azar)
				int suma = 0;
				foreach (char letra in Texto) suma = (suma * 31 + letra) % 500;
				inclinacion = (suma / 100f - 2.5f) * 1.2f;
				Rotular(cuerpo, fuente, tamEstilo, anchoUtil, frente, ColorTexto, 0, Colors.Black);
				break;
		}
		var malla = new MeshInstance3D { Name = "Malla", Mesh = piezas.Unir() };
		malla.SetMeta("generado_cartel", true);
		AddChild(malla);
		foreach (Node hijo in GetChildren())
			if (hijo is Node3D pieza && hijo.HasMeta("generado_cartel")) pieza.RotationDegrees = new Vector3(0, 0, inclinacion);
	}

	// ---------- Estilos ----------

	// Tabla de madera: tablones con ranuras, marco más oscuro y clavos en las esquinas
	private float ArmarMadera(Piezas p, float alto, float borde)
	{
		int tabla = p.Material(MaterialMadera(ColorPapel));
		int ranura = p.Material(Liso(ColorPapel.Darkened(0.45f), 0.95f));
		int marco = p.Material(MaterialMadera(ColorPapel.Darkened(0.28f)));
		int clavo = p.Material(Liso(new Color(0.16f, 0.16f, 0.17f), 0.45f, 0.7f));
		p.Caja(new Vector3(Ancho, alto, 0.018f), Vector3.Zero, tabla);
		int tablones = Mathf.Clamp(Mathf.RoundToInt((alto - 2f * borde) / 0.17f), 1, 4);
		for (int i = 1; i < tablones; i++)
			p.Caja(new Vector3(Ancho - 2f * borde, Mathf.Max(0.003f, borde * 0.12f), 0.004f),
				new Vector3(0, -alto / 2f + borde + (alto - 2f * borde) * i / tablones, 0.0085f), ranura);
		float z = 0.004f;
		p.Caja(new Vector3(Ancho, borde, 0.026f), new Vector3(0, alto / 2f - borde / 2f, z), marco);
		p.Caja(new Vector3(Ancho, borde, 0.026f), new Vector3(0, -alto / 2f + borde / 2f, z), marco);
		p.Caja(new Vector3(borde, alto - 2f * borde, 0.026f), new Vector3(-Ancho / 2f + borde / 2f, 0, z), marco);
		p.Caja(new Vector3(borde, alto - 2f * borde, 0.026f), new Vector3(Ancho / 2f - borde / 2f, 0, z), marco);
		float c = Mathf.Clamp(borde * 0.38f, 0.005f, 0.028f);
		foreach (int sx in new[] { -1, 1 })
			foreach (int sy in new[] { -1, 1 })
				p.Caja(new Vector3(c, c, c * 0.5f), new Vector3(sx * (Ancho / 2f - borde / 2f), sy * (alto / 2f - borde / 2f), z + 0.013f + c * 0.2f), clavo, 45f);
		return 0.0115f;
	}

	// Lámina de metal: filete del color del texto y un tornillo en cada esquina
	private float ArmarPlaca(Piezas p, float alto)
	{
		int lamina = p.Material(Liso(ColorPapel, 0.42f, 0.35f));
		int filete = p.Material(Liso(ColorTexto, 0.5f, 0.2f));
		int tornillo = p.Material(Liso(new Color(0.72f, 0.73f, 0.75f), 0.35f, 0.8f));
		p.Caja(new Vector3(Ancho, alto, 0.008f), Vector3.Zero, lamina);
		float corto = Mathf.Min(Ancho, alto);
		float adentro = Mathf.Clamp(corto * 0.06f, 0.006f, 0.05f);
		float grosor = Mathf.Clamp(corto * 0.018f, 0.002f, 0.016f);
		float w = Ancho - 2f * adentro, h = alto - 2f * adentro, z = 0.0045f;
		p.Caja(new Vector3(w, grosor, 0.0015f), new Vector3(0, h / 2f, z), filete);
		p.Caja(new Vector3(w, grosor, 0.0015f), new Vector3(0, -h / 2f, z), filete);
		p.Caja(new Vector3(grosor, h, 0.0015f), new Vector3(-w / 2f, 0, z), filete);
		p.Caja(new Vector3(grosor, h, 0.0015f), new Vector3(w / 2f, 0, z), filete);
		float t = Mathf.Clamp(corto * 0.035f, 0.004f, 0.022f);
		foreach (int sx in new[] { -1, 1 })
			foreach (int sy in new[] { -1, 1 })
				p.Caja(new Vector3(t, t, t * 0.4f), new Vector3(sx * (w / 2f - adentro * 1.3f), sy * (h / 2f - adentro * 1.3f), z + t * 0.15f), tornillo, 45f);
		return 0.0062f;
	}

	// Hoja de papel pegada con dos tiras de cinta
	private float ArmarPapel(Piezas p, float alto)
	{
		int hoja = p.Material(Liso(ColorPapel, 1f));
		var mCinta = Liso(new Color(0.93f, 0.88f, 0.62f, 0.72f), 0.6f);
		mCinta.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		int cinta = p.Material(mCinta);
		p.Caja(new Vector3(Ancho, alto, 0.003f), Vector3.Zero, hoja);
		float largo = Mathf.Clamp(Ancho * 0.24f, 0.018f, 0.13f), ancho = largo * 0.36f;
		p.Caja(new Vector3(largo, ancho, 0.001f), new Vector3(-Ancho / 2f + largo * 0.25f, alto / 2f - ancho * 0.25f, 0.0022f), cinta, 38f);
		p.Caja(new Vector3(largo, ancho, 0.001f), new Vector3(Ancho / 2f - largo * 0.25f, alto / 2f - ancho * 0.25f, 0.0022f), cinta, -38f);
		return 0.0026f;
	}

	// Pizarrón: marco de madera, repisa y un gis olvidado
	private float ArmarPizarron(Piezas p, float alto, float borde)
	{
		int tablero = p.Material(Liso(ColorPapel, 0.95f));
		int marco = p.Material(MaterialMadera(new Color(0.45f, 0.31f, 0.18f)));
		int gis = p.Material(Liso(new Color(0.95f, 0.95f, 0.92f), 0.9f));
		p.Caja(new Vector3(Ancho - borde, alto - borde, 0.008f), Vector3.Zero, tablero);
		p.Caja(new Vector3(Ancho, borde, 0.022f), new Vector3(0, alto / 2f - borde / 2f, 0.004f), marco);
		p.Caja(new Vector3(Ancho, borde, 0.022f), new Vector3(0, -alto / 2f + borde / 2f, 0.004f), marco);
		p.Caja(new Vector3(borde, alto - 2f * borde, 0.022f), new Vector3(-Ancho / 2f + borde / 2f, 0, 0.004f), marco);
		p.Caja(new Vector3(borde, alto - 2f * borde, 0.022f), new Vector3(Ancho / 2f - borde / 2f, 0, 0.004f), marco);
		p.Caja(new Vector3(Ancho * 0.7f, borde * 0.35f, 0.04f), new Vector3(0, -alto / 2f + borde * 0.2f, 0.026f), marco);
		p.Caja(new Vector3(borde * 0.9f, borde * 0.22f, borde * 0.22f), new Vector3(Ancho * 0.18f, -alto / 2f + borde * 0.5f, 0.034f), gis);
		return 0.0062f;
	}

	// ---------- Texto ----------

	// Busca la fuente más grande (desde TamFuente) que cabe a lo ancho, sin cortar palabras, y a lo alto
	private (int Tam, float AltoTexto) Ajustar(Font fuente, float anchoUtil, float altoDisponible)
	{
		anchoUtil = Mathf.Max(0.02f, anchoUtil);
		int anchoPx = Mathf.Max(8, (int)(anchoUtil / TamPixel));
		int tam = TamFuente;
		Vector2 medida = Vector2.Zero;
		for (; tam >= TamMinimo; tam -= 2)
		{
			medida = Medir(fuente, anchoPx, tam);
			if (PalabraMasLarga(fuente, tam) <= anchoPx && medida.Y * TamPixel <= altoDisponible) break;
		}
		return (Mathf.Max(tam, TamMinimo), medida.Y * TamPixel);
	}

	// Para un marco de tamaño ya decidido: la fuente más grande que lo llena sin salirse
	private int AjustarAlMarco(Font fuente, float anchoUtil, float altoUtil, int desde)
	{
		int anchoPx = Mathf.Max(8, (int)(anchoUtil / TamPixel));
		for (int tam = desde; tam > 8; tam -= 2)
			if (PalabraMasLarga(fuente, tam) <= anchoPx && Medir(fuente, anchoPx, tam).Y * TamPixel <= altoUtil) return tam;
		return 8;
	}

	private Vector2 Medir(Font fuente, int anchoPx, int tam) =>
		fuente.GetMultilineStringSize(Texto, HorizontalAlignment.Center, anchoPx, tam,
			-1, TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound);

	private void Rotular(Node3D padre, Font fuente, int tam, float anchoUtil, float z, Color color, int contorno, Color colorContorno)
	{
		var etiqueta = new Label3D
		{
			Text = Texto,
			Font = fuente == ThemeDB.FallbackFont ? null : fuente,
			FontSize = tam,
			PixelSize = TamPixel,
			Modulate = color,
			OutlineSize = contorno,
			OutlineModulate = colorContorno,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Width = Mathf.Max(8, (int)(Mathf.Max(0.02f, anchoUtil) / TamPixel)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Position = new Vector3(0, 0, z + 0.0008f),
		};
		etiqueta.SetMeta("generado_cartel", true);
		padre.AddChild(etiqueta);
	}

	private float PalabraMasLarga(Font fuente, int tam)
	{
		float maximo = 0f;
		foreach (string palabra in Texto.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			maximo = Mathf.Max(maximo, fuente.GetStringSize(palabra, HorizontalAlignment.Left, -1, tam).X);
		}
		return maximo;
	}

	// ---------- Materiales y fuentes (compartidos entre carteles) ----------

	private static Font Fuente(string ruta, int peso)
	{
		string clave = ruta + peso;
		if (Fuentes.TryGetValue(clave, out var lista) && IsInstanceValid(lista)) return lista;
		if (!ResourceLoader.Exists(ruta)) return ThemeDB.FallbackFont;
		var fuente = new FontVariation
		{
			BaseFont = GD.Load<Font>(ruta),
			// El eje de peso de una fuente variable se pide por su etiqueta OpenType, no por su nombre
			VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), peso } },
		};
		Fuentes[clave] = fuente;
		return fuente;
	}

	private static StandardMaterial3D Liso(Color color, float rugosidad, float metal = 0f) =>
		new() { AlbedoColor = color, Roughness = rugosidad, Metallic = metal };

	// Madera con vetas a lo largo: un ruido estirado que aclara y oscurece el color
	private static StandardMaterial3D MaterialMadera(Color color)
	{
		if (_vetas == null || !IsInstanceValid(_vetas))
		{
			var rampa = new Gradient();
			rampa.SetColor(0, new Color(0.55f, 0.5f, 0.45f));
			rampa.SetColor(1, new Color(1f, 1f, 1f));
			_vetas = new NoiseTexture2D
			{
				Width = 256, Height = 256, Seamless = true, ColorRamp = rampa,
				Noise = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Frequency = 0.012f, FractalOctaves = 4, Seed = 21 },
			};
		}
		return new StandardMaterial3D
		{
			// Más cálida que el color plano: el ruido la oscurece y la luz de la linterna la lava
			AlbedoColor = new Color(Mathf.Min(1f, color.R * 1.45f), color.G * 1.2f, color.B * 0.95f), AlbedoTexture = _vetas, Roughness = 0.92f,
			Uv1Triplanar = true, Uv1Scale = new Vector3(0.9f, 9f, 9f),
		};
	}

	// Junta cajas en una sola malla, con una superficie por material: un cartel cuesta pocos dibujados
	private sealed class Piezas
	{
		private readonly List<Material> _materiales = new();
		private readonly List<(Vector3 Tam, Vector3 Pos, int Mat, float GiroZ)> _cajas = new();

		public int Material(Material m)
		{
			_materiales.Add(m);
			return _materiales.Count - 1;
		}

		public void Caja(Vector3 tam, Vector3 pos, int material, float giroZ = 0f) => _cajas.Add((tam, pos, material, giroZ));

		public ArrayMesh Unir()
		{
			var malla = new ArrayMesh();
			for (int m = 0; m < _materiales.Count; m++)
			{
				var st = new SurfaceTool();
				st.Begin(Mesh.PrimitiveType.Triangles);
				bool hay = false;
				foreach (var c in _cajas)
				{
					if (c.Mat != m) continue;
					var giro = new Basis(Vector3.Back, Mathf.DegToRad(c.GiroZ));
					st.AppendFrom(new BoxMesh { Size = c.Tam }, 0, new Transform3D(giro, c.Pos));
					hay = true;
				}
				if (!hay) continue;
				st.Commit(malla);
				malla.SurfaceSetMaterial(malla.GetSurfaceCount() - 1, _materiales[m]);
			}
			return malla;
		}
	}
}
