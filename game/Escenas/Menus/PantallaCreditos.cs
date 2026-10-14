using Godot;
using System;

// Créditos (diseño "04 · Créditos"): el equipo como en una rueda de reconocimiento, con la regla de
// alturas de fondo y el sello "CASO RESUELTO". Se cierra con VOLVER o con Esc.
public partial class PantallaCreditos : PanelMenu
{
	private static readonly (string Apellidos, string Nombre)[] Equipo =
	{
		("ARANDA GONZÁLEZ", "Yara Guadalupe"),
		("AVILA CASTRO", "Jose Eduardo"),
		("CONTRERAS OLAGUE", "Fátima"),
		("DÍAZ NAVA", "Mario Enrique"),
		("ESPARZA JÁQUEZ", "Erik Eduardo"),
	};

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
		ReglaDeAlturas();

		var margen = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		margen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		margen.AddThemeConstantOverride("margin_left", 112);
		margen.AddThemeConstantOverride("margin_right", 112);
		margen.AddThemeConstantOverride("margin_top", 80);
		margen.AddThemeConstantOverride("margin_bottom", 60);
		AddChild(margen);
		var columna = EstiloMenus.Columna(52);
		margen.AddChild(columna);

		// Encabezado: título a la izquierda, sello a la derecha
		var cabecera = EstiloMenus.Fila(48);
		var titulo = EstiloMenus.Columna(20);
		titulo.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		titulo.AddChild(EstiloMenus.Marca("CRÉDITOS", EstiloMenus.Gris, 0.28f));
		titulo.AddChild(EstiloMenus.Titulo("LOS RESPONSABLES", 112));
		cabecera.AddChild(titulo);
		cabecera.AddChild(Sello());
		columna.AddChild(cabecera);

		// Una columna por integrante, alineadas abajo
		var equipo = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
		equipo.AddThemeConstantOverride("separation", 0);
		for (int i = 0; i < Equipo.Length; i++)
			equipo.AddChild(Integrante(i + 1, Equipo[i].Apellidos, Equipo[i].Nombre));
		columna.AddChild(equipo);

		// Pie
		var pie = EstiloMenus.Columna(28);
		pie.AddChild(EstiloMenus.Raya(0, 2, EstiloMenus.Linea));
		var filaPie = EstiloMenus.Fila(28);
		var firma = EstiloMenus.Fila(28);
		firma.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		firma.AddChild(EstiloMenus.Etiqueta("DETECTIVE CANELOSO", EstiloMenus.Mono(22, 0.16f), 22, EstiloMenus.Gris));
		var cuadrito = new ColorRect { Color = EstiloMenus.Numero, CustomMinimumSize = new Vector2(8, 8), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
		firma.AddChild(cuadrito);
		firma.AddChild(EstiloMenus.Etiqueta("HECHO CON GODOT ENGINE", EstiloMenus.Mono(22, 0.16f), 22, EstiloMenus.Gris));
		filaPie.AddChild(firma);
		_volver = new OpcionMenu("←", "VOLVER", 44) { Desplazamiento = 12f };
		filaPie.AddChild(_volver);
		pie.AddChild(filaPie);
		columna.AddChild(pie);
	}

	// Líneas horizontales con las alturas (2.2 m … 1.2 m), como en una sala de reconocimiento
	private void ReglaDeAlturas()
	{
		var capa = new Control { MouseFilter = MouseFilterEnum.Ignore };
		capa.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(capa);
		MoveChild(capa, 1); // encima del degradado y debajo de la viñeta
		for (int i = 0; i < 11; i++)
		{
			float y = 80 + i * 84;
			bool principal = i % 2 == 0;
			var linea = new ColorRect { Color = principal ? EstiloMenus.Linea : EstiloMenus.LineaSuave, MouseFilter = MouseFilterEnum.Ignore };
			linea.AnchorRight = 1f;
			linea.OffsetTop = y;
			linea.OffsetBottom = y + 2;
			capa.AddChild(linea);
			if (!principal) continue;
			var altura = EstiloMenus.Etiqueta($"{2.2f - i * 0.1f:0.0} m", EstiloMenus.Mono(20, 0.1f), 20, new Color("#3a4048"));
			altura.AnchorLeft = altura.AnchorRight = 1f;
			altura.OffsetLeft = -160;
			altura.OffsetRight = -56;
			altura.HorizontalAlignment = HorizontalAlignment.Right;
			altura.OffsetTop = y - 30;
			capa.AddChild(altura);
		}
	}

	// El sello va dentro de un Control normal: los contenedores anulan la rotación de sus hijos
	private static Control Sello()
	{
		var sello = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0.85f) };
		var estilo = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = EstiloMenus.BordeSello, ContentMarginLeft = 28, ContentMarginRight = 28, ContentMarginTop = 12, ContentMarginBottom = 12 };
		estilo.SetBorderWidthAll(6);
		sello.AddThemeStyleboxOverride("panel", estilo);
		sello.AddChild(EstiloMenus.Etiqueta("CASO RESUELTO", EstiloMenus.Display(900, 44, 0.14f), 44, EstiloMenus.RojoSello));

		var soporte = new Control { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin };
		soporte.AddChild(sello);
		sello.Resized += () =>
		{
			soporte.CustomMinimumSize = sello.Size + new Vector2(0, 16);
			sello.Position = new Vector2(0, 16);
			sello.PivotOffset = sello.Size / 2f;
		};
		sello.RotationDegrees = -7f;
		return soporte;
	}

	// Columna de un integrante: número grande y tenue, apellidos, nombre y raya roja.
	// Al pasar el ratón sube un poco y se pone ámbar (es decorativo: no recibe el foco).
	private static Control Integrante(int numero, string apellidos, string nombre)
	{
		var columna = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass };
		var borde = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = EstiloMenus.Linea, BorderWidthLeft = 2, ContentMarginLeft = 32, ContentMarginRight = 8, ContentMarginBottom = 12 };
		columna.AddThemeStyleboxOverride("panel", borde);

		var contenido = EstiloMenus.Columna(28);
		contenido.Alignment = BoxContainer.AlignmentMode.End;
		contenido.AddChild(EstiloMenus.Etiqueta($"{numero:00}", EstiloMenus.Display(900), 128, new Color(0.91f, 0.92f, 0.94f, 0.1f)));
		var nombres = EstiloMenus.Columna(16);
		var etiquetaApellidos = EstiloMenus.Etiqueta(apellidos, EstiloMenus.Display(800, 52, 0.05f), 52, EstiloMenus.Texto);
		etiquetaApellidos.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		etiquetaApellidos.AddThemeConstantOverride("line_spacing", -6);
		// Siempre el alto de dos líneas, pegado abajo: así los números grandes quedan alineados
		etiquetaApellidos.CustomMinimumSize = new Vector2(220, 128);
		etiquetaApellidos.VerticalAlignment = VerticalAlignment.Bottom;
		nombres.AddChild(etiquetaApellidos);
		nombres.AddChild(EstiloMenus.Etiqueta(nombre, EstiloMenus.Mono(24, 0.06f), 24, EstiloMenus.GrisClaro));
		contenido.AddChild(nombres);
		contenido.AddChild(EstiloMenus.Raya(56, 4, EstiloMenus.Rojo));
		columna.AddChild(contenido);

		Tween tween = null;
		void Resaltar(bool activo)
		{
			etiquetaApellidos.AddThemeColorOverride("font_color", activo ? EstiloMenus.Ambar : EstiloMenus.Texto);
			tween?.Kill();
			tween = contenido.CreateTween();
			tween.TweenProperty(contenido, "position:y", activo ? -12f : 0f, 0.2).SetTrans(Tween.TransitionType.Sine);
		}
		columna.MouseEntered += () => Resaltar(true);
		columna.MouseExited += () => Resaltar(false);
		return columna;
	}
}
