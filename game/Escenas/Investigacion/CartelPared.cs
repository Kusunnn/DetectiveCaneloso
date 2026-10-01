using Godot;
using System;

// Cartel, letrero, nota o placa con texto que SIEMPRE cabe en su marco.
// Orden de ajuste: 1) el marco crece al alto del texto, 2) salto de línea automático al ancho
// del marco, 3) la fuente baja hasta un mínimo legible. El texto queda centrado con margen.
[Tool]
public partial class CartelPared : Node3D
{
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

	public override void _Ready()
	{
		Construir();
	}

	public void Construir()
	{
		foreach (Node hijo in GetChildren())
		{
			if (hijo.HasMeta("generado_cartel")) { RemoveChild(hijo); hijo.QueueFree(); }
		}
		if (string.IsNullOrEmpty(Texto)) return;

		Font fuente = ThemeDB.FallbackFont;
		float anchoUtil = Mathf.Max(0.02f, Ancho - 2f * Margen);
		int anchoPx = Mathf.Max(8, (int)(anchoUtil / TamPixel));
		float altoDisponible = (AltoFijo > 0f ? AltoFijo : AltoMaximo) - 2f * Margen;

		// Busca la fuente más grande que cabe a lo ancho (sin cortar palabras) y a lo alto
		int tam = TamFuente;
		Vector2 medida = Vector2.Zero;
		for (; tam >= TamMinimo; tam -= 2)
		{
			medida = fuente.GetMultilineStringSize(Texto, HorizontalAlignment.Center, anchoPx, tam,
				-1, TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound);
			if (PalabraMasLarga(fuente, tam) <= anchoPx && medida.Y * TamPixel <= altoDisponible) break;
		}
		tam = Mathf.Max(tam, TamMinimo);
		float altoTexto = medida.Y * TamPixel;
		float alto = AltoFijo > 0f ? AltoFijo : altoTexto + 2f * Margen;

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

		var etiqueta = new Label3D
		{
			Text = Texto,
			FontSize = tam,
			PixelSize = TamPixel,
			Modulate = ColorTexto,
			OutlineSize = 0,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Width = anchoPx,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Position = new Vector3(0, 0, 0.007f),
		};
		etiqueta.SetMeta("generado_cartel", true);
		AddChild(etiqueta);
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
}
