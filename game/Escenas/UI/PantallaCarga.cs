using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PantallaCarga : CanvasLayer
{
	private const double IntervaloFotos = 20.0;
	private const double IntervaloCuriosidades = 10.0;
	private const string Carpeta = "res://Assets/Carga/";
	private const string ModeloMiawrio = "res://Assets/Models/Miaurio/miaurio.tscn";
	private const string ModeloAya = "res://Assets/Models/Aya/Ayas.tscn";
	private const string ModeloCaneloso = "res://Assets/Models/Caneloso/Archivo/player.tscn";
	private static readonly (string Texto, string Modelo)[] Curiosidades =
	{
		("A Miaurio le apasionan los videojuegos. Dicen que llegó a Challenger, el rango más alto de League of Legends.",
			ModeloMiawrio),
		("A Juan Carlos le apasiona la repostería. Sus chocoflanes son tan buenos que pueden volverse adictivos.",
			"res://Assets/Models/Personajes/lala.glb"),
		("Mimita está tan viciada a ACNH que ya lleva más de 15 000 horas de juego.",
			"res://Assets/Models/Personajes/mimita_rig.glb"),
		("Aya es una dibujante experta en hombres embarazados.",
			ModeloAya),
		("Eriz es un rapero reconocido por participar en la famosa canción «Slenderman vs. Jeff the Killer».",
			"res://Assets/Models/Personajes/eriz.glb"),
		("Se rumorea que Aya y Eriz estuvieron saliendo. ¿Seguirán juntos?", ModeloCaneloso),
		("Según dicen, de niños Juan Carlos y Eriz vivían en un lugar sin pavimento y con una fuente.", ModeloCaneloso),
		("Se dice que Mimita ft. Miaurio compusieron la canción que dice: «Quítale lo aburrido, ponle lo divertido».", ModeloCaneloso),
		("Corre el rumor de que hay una epidemia llamada Genshin. Quienes la tienen se ven obligados a hacer algo llamado «las diarias».", ModeloCaneloso),
	};

	private readonly List<string> _fotos = new();
	private TextureRect _foto, _siguiente;
	private static int _ultimaCuriosidad = -1;
	private HBoxContainer _fila;
	private Label _mensaje;
	private Control _icono;
	private Node3D _giro;
	private double _segundosCuriosidad;
	private int _indice;
	private double _segundos;

	public PantallaCarga() { }

	public PantallaCarga(InfoCapitulo capitulo, string escena, int turno)
	{
		Name = "PantallaCarga";
		Layer = 130;
		ProcessMode = ProcessModeEnum.Always;

		string galeria = escena == PartidaEscenas.Lago ? "lago" : capitulo.Id;
		for (int i = 1; i <= 3; i++)
		{
			string ruta = $"{Carpeta}{galeria}/{i}.jpg";
			if (ResourceLoader.Exists(ruta)) _fotos.Add(ruta);
		}
		if (_fotos.Count == 0 && !string.IsNullOrEmpty(capitulo.Ilustracion) && ResourceLoader.Exists(capitulo.Ilustracion))
			_fotos.Add(capitulo.Ilustracion);

		var fondo = new ColorRect { Color = new Color("151915"), MouseFilter = Control.MouseFilterEnum.Stop };
		AddChild(fondo);
		fondo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_foto = CrearFoto(fondo);
		_siguiente = CrearFoto(fondo);
		if (_fotos.Count > 0)
		{
			_indice = turno % _fotos.Count;
			_foto.Texture = GD.Load<Texture2D>(_fotos[_indice]);
		}

		var velo = new ColorRect
		{
			Color = new Color(0.27f, 0.27f, 0.27f, 0.24f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		fondo.AddChild(velo);
		velo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		var degradado = new TextureRect
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = new GradientTexture2D
			{
				Width = 2, Height = 256,
				FillFrom = new Vector2(0, 1), FillTo = Vector2.Zero,
				Gradient = new Gradient
				{
					Offsets = new[] { 0f, 0.27f, 0.59f, 1f },
					Colors = new[] { new Color(0.026f, 0.035f, 0.026f, 0.98f),
						new Color(0.026f, 0.035f, 0.026f, 0.82f), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.2f) }
				}
			}
		};
		fondo.AddChild(degradado);
		degradado.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_fila = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		_fila.AddThemeConstantOverride("separation", 28);
		fondo.AddChild(_fila);
		_fila.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		_fila.OffsetLeft = 56;
		_fila.OffsetRight = -56;
		_fila.OffsetTop = -252;
		_fila.OffsetBottom = -52;

		var textos = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
		textos.AddThemeConstantOverride("separation", 7);
		_fila.AddChild(textos);
		string nivel = capitulo.Numero == 0 ? "TUTORIAL" : $"CASO {capitulo.Numero:00}";
		textos.AddChild(Etiqueta($"CARGANDO  /  {nivel}", 22, "f4ce63"));
		textos.AddChild(Etiqueta(capitulo.Titulo, 57, "fff7e5", titulo: true));
		var nota = new HBoxContainer();
		nota.AddThemeConstantOverride("separation", 18);
		textos.AddChild(nota);
		nota.AddChild(Etiqueta("DATO CURIOSO", 19, "f4ce63"));
		_mensaje = Etiqueta("", 22, "fff7e5");
		_mensaje.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_mensaje.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		nota.AddChild(_mensaje);
		MostrarCuriosidad();
	}

	private void MostrarCuriosidad()
	{
		int indice = GD.RandRange(0, Curiosidades.Length - (_ultimaCuriosidad < 0 ? 1 : 2));
		if (_ultimaCuriosidad >= 0 && indice >= _ultimaCuriosidad) indice++;
		_ultimaCuriosidad = indice;
		var dato = Curiosidades[indice];
		_mensaje.Text = dato.Texto;

		if (_icono != null)
		{
			_fila.RemoveChild(_icono);
			_icono.QueueFree();
		}
		_icono = CrearIcono(dato.Modelo);
		_fila.AddChild(_icono);
		_fila.MoveChild(_icono, 0);
	}

	private static TextureRect CrearFoto(Control fondo)
	{
		var foto = new TextureRect
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered
		};
		fondo.AddChild(foto);
		foto.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		return foto;
	}

	private static Label Etiqueta(string texto, int tamano, string color, bool titulo = false)
	{
		var etiqueta = new Label { Text = texto };
		Font fuente = titulo
			? new FontVariation
			{
				BaseFont = GD.Load<Font>("res://Assets/Fuentes/BigShouldersDisplay-Variable.ttf"),
				VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 650 } }
			}
			: GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		etiqueta.AddThemeFontOverride("font", fuente);
		etiqueta.AddThemeFontSizeOverride("font_size", tamano);
		etiqueta.AddThemeColorOverride("font_color", new Color(color));
		return etiqueta;
	}

	private Control CrearIcono(string ruta)
	{
		var marco = new SubViewportContainer
		{
			CustomMinimumSize = new Vector2(200, 200), Stretch = true, MouseFilter = Control.MouseFilterEnum.Ignore
		};
		var viewport = new SubViewport
		{
			OwnWorld3D = true, TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			GuiDisableInput = true, Msaa3D = Viewport.Msaa.Msaa4X
		};
		marco.AddChild(viewport);
		_giro = new Node3D();
		viewport.AddChild(_giro);

		var camara = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal, Size = 2.16f, Current = true,
			Position = new Vector3(0, 0, 4)
		};
		viewport.AddChild(camara);
		viewport.AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color("fff1da"), AmbientLightEnergy = 0.75f
			}
		});
		viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-28, -30, 0), LightEnergy = 1.1f });
		viewport.Ready += () => PrepararModelo(ruta);
		return marco;
	}

	private void PrepararModelo(string ruta)
	{
		var origen = GD.Load<PackedScene>(ruta).Instantiate<Node3D>();
		var escala = new Node3D();
		var modelo = new Node3D();
		_giro.AddChild(escala);
		escala.AddChild(modelo);
		Aabb? volumen = null;
		bool usarEsqueleto = ruta is ModeloCaneloso or ModeloMiawrio or ModeloAya;
		if (usarEsqueleto)
		{
			// La malla sin esqueleto puede perder la colocación de las piezas del personaje.
			foreach (var animacion in origen.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>())
				animacion.Autoplay = "";
			foreach (var esqueleto in origen.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>())
				esqueleto.ResetBonePoses();
			modelo.AddChild(origen);
		}

		// Las demás mallas conservan la pose del archivo importado.
		foreach (var original in origen.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
		{
			if (original.Mesh == null || !original.Visible) continue;
			var malla = new MeshInstance3D
			{
				Mesh = usarEsqueleto ? original.BakeMeshFromCurrentSkeletonPose() ?? original.Mesh : original.Mesh,
				Transform = usarEsqueleto ? modelo.GlobalTransform.AffineInverse() * original.GlobalTransform : original.Transform,
				MaterialOverride = original.MaterialOverride
			};
			for (int i = 0; i < malla.GetSurfaceOverrideMaterialCount(); i++)
				malla.SetSurfaceOverrideMaterial(i, original.GetActiveMaterial(i));
			modelo.AddChild(malla);
			var caja = malla.Transform * malla.GetAabb();
			volumen = volumen?.Merge(caja) ?? caja;
		}
		origen.Free();
		if (volumen is not Aabb limites) return;
		// La diagonal debe caber en la vista durante toda la vuelta.
		float dimension = new Vector2(limites.Size.X, limites.Size.Y).Length();
		if (dimension > 0)
		{
			modelo.Position = -limites.GetCenter();
			escala.Scale = Vector3.One * (1.85f / dimension);
		}
	}

	public override void _Input(InputEvent evento) => GetViewport().SetInputAsHandled();

	public override void _Process(double delta)
	{
		_giro?.RotateZ(-(float)delta * 1.4f);

		if (_mensaje != null)
		{
			_segundosCuriosidad += delta;
			if (_segundosCuriosidad >= IntervaloCuriosidades)
			{
				_segundosCuriosidad %= IntervaloCuriosidades;
				MostrarCuriosidad();
			}
		}

		if (_fotos.Count < 2) return;
		_segundos += delta;
		if (_segundos < IntervaloFotos) return;

		_segundos -= IntervaloFotos;
		_indice = (_indice + 1) % _fotos.Count;
		_siguiente.Texture = GD.Load<Texture2D>(_fotos[_indice]);
		_siguiente.Modulate = new Color(1, 1, 1, 0);
		var transicion = CreateTween();
		transicion.TweenProperty(_siguiente, "modulate:a", 1f, 0.7);
		transicion.TweenCallback(Callable.From(() =>
		{
			_foto.Texture = _siguiente.Texture;
			_siguiente.Texture = null;
		}));
	}
}
