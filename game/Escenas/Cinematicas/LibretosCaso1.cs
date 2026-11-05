using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// HU-9.5 CA-01: los planos de las cinemáticas del Caso 1. Lo que se lee viene del guion (Datos/Dialogos): aquí solo se
// decide qué se ve mientras se lee. Lo que aparece sale de la historia (docs/historias, «Lo que ocurrió realmente»).
// Las cinemáticas de las pistas no enseñan a nadie: Tony solo aparece después de que el jugador lo acusa.
public static class LibretosCaso1
{
	public const string IdLibro = "caso1_libro";
	public const string IdFoto = "caso1_foto";
	public const string IdLlegada = "caso1_llegada";
	public const string IdSilbato = "caso1_silbato";
	public const string Id2320 = "caso1_2320";
	public const string IdRevelacion = "caso1_2320_revelacion";
	public const string IdFinalA = "caso1_final_a";
	public const string IdFinalB = "caso1_final_b";

	private const string Lago = "res://Escenas/Caso1/Lago/escenario_lago.tscn";
	private const string Campamento = "res://Escenas/Caso1/escenario_campamento.tscn";
	private const string Audio = "res://Assets/Audio/Cinematica/Caso1/";
	private const string Tony = "res://Assets/Models/Tony/Tony.tscn";
	private const string Porky = "res://Assets/Models/Personajes/porky.glb";
	private const string Hecdog = "res://Assets/Models/poli/Poli/Neutral Idle.fbx";
	private const float EscalaTony = 0.6503f, EscalaPorky = 5.4f, EscalaHecdog = 380f;
	private static readonly Color AzulAlgas = new(0.1f, 0.55f, 1f);

	private static Vector3 V(float x, float y, float z) => new(x, y, z);

	// ---------- Actores y utilería ----------

	// Pone un modelo en el mundo con la misma escala que tiene en el juego
	private static Node3D Actor(Node3D mundo, string ruta, float escala, Vector3 posicion, float giroGrados)
	{
		var raiz = new Node3D { Name = "Actor" };
		mundo.AddChild(raiz);
		var modelo = GD.Load<PackedScene>(ruta).Instantiate<Node3D>();
		modelo.Name = "Modelo";
		modelo.Scale *= escala;
		raiz.AddChild(modelo);
		raiz.GlobalPosition = posicion;
		raiz.RotationDegrees = new Vector3(0, giroGrados, 0);
		return raiz;
	}

	private static void Animar(Node3D actor, string animacion, float velocidad = 1f)
	{
		foreach (Node n in actor.FindChildren("*", "AnimationPlayer", true, false))
		{
			if (n is not AnimationPlayer reproductor || !reproductor.HasAnimation(animacion)) continue;
			reproductor.GetAnimation(animacion).LoopMode = Animation.LoopModeEnum.Linear;
			reproductor.SpeedScale = velocidad;
			reproductor.Play(animacion);
			return;
		}
	}

	// Porky no trae animaciones: baja los brazos como en el juego
	private static void BrazosAbajo(Node3D actor)
	{
		var animacion = new AnimacionPersonaje { DistanciaMirar = 0f };
		actor.AddChild(animacion);
	}

	private static OmniLight3D Luz(Node3D mundo, Vector3 posicion, Color color, float energia, float rango)
	{
		var luz = new OmniLight3D { LightColor = color, LightEnergy = energia, OmniRange = rango, LightSpecular = 0f };
		mundo.AddChild(luz);
		luz.GlobalPosition = posicion;
		return luz;
	}

	private static GpuParticles3D Particulas(Node3D mundo, Vector3 posicion, Color color, int cantidad, float vida, float velocidad, float tam, Vector3 gravedad, float dispersion = 40f)
	{
		var material = new ParticleProcessMaterial
		{
			Direction = Vector3.Up,
			Spread = dispersion,
			InitialVelocityMin = velocidad * 0.5f,
			InitialVelocityMax = velocidad,
			Gravity = gravedad,
			Color = color,
			ScaleMin = 0.5f,
			ScaleMax = 1f,
		};
		var particulas = new GpuParticles3D
		{
			Amount = cantidad,
			Lifetime = vida,
			ProcessMaterial = material,
			DrawPass1 = new SphereMesh
			{
				Radius = tam,
				Height = tam * 2,
				RadialSegments = 6,
				Rings = 3,
				Material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, VertexColorUseAsAlbedo = true, AlbedoColor = color, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 2f },
			},
		};
		mundo.AddChild(particulas);
		particulas.GlobalPosition = posicion;
		return particulas;
	}

	// El brillo de las algas: el agua y sus luces
	private static Action<float> Brillo(Node3D mundo)
	{
		var agua = mundo.GetNodeOrNull<MeshInstance3D>("Lago/Agua");
		var material = agua?.GetActiveMaterial(0) as ShaderMaterial;
		var luces = new[] { mundo.GetNodeOrNull<OmniLight3D>("Lago/BrilloAlgas"), mundo.GetNodeOrNull<OmniLight3D>("Lago/BrilloAlgasLejos") };
		float[] base_ = luces.Select(l => l?.LightEnergy ?? 0f).ToArray();
		float intensidad = material?.GetShaderParameter("intensidad").AsSingle() ?? 3f;
		return nivel =>
		{
			material?.SetShaderParameter("intensidad", intensidad * nivel);
			for (int i = 0; i < luces.Length; i++) if (luces[i] != null) luces[i].LightEnergy = base_[i] * nivel;
		};
	}

	// ---------- Pistas del campamento ----------

	// Las cenizas de la fogata: el libro arde otra vez mientras se lee lo que queda de él
	public static CinematicaCaso1.Libreto Libro(string comentario)
	{
		GpuParticles3D brasas = null;
		OmniLight3D fuego = null;
		var libro = V(0.17f, 0.12f, -3.88f);
		return new CinematicaCaso1.Libreto
		{
			Preparar = escena =>
			{
				brasas = Particulas(escena, libro, new Color(1f, 0.45f, 0.1f), 60, 1.6f, 0.9f, 0.012f, new Vector3(0, 0.4f, 0));
				fuego = Luz(escena, libro + V(0, 0.35f, 0), new Color(1f, 0.5f, 0.15f), 0.9f, 4f);
			},
			Planos =
			{
				new()
				{
					Duracion = 4.5, CamaraDesde = libro + V(1.7f, 1.0f, 1.7f), CamaraHasta = libro + V(0.9f, 0.55f, 1.0f), MiraDesde = libro, MiraHasta = libro, Fov = 42f,
					Cada = u => { if (fuego != null) fuego.LightEnergy = 0.9f + 0.25f * Mathf.Sin(u * 40f); },
				},
				new()
				{
					Duracion = 9.5, CamaraDesde = libro + V(0.3f, 0.95f, 0.35f), CamaraHasta = libro + V(0.14f, 0.55f, 0.16f), MiraDesde = libro + V(0, -0.08f, 0), MiraHasta = libro + V(0, -0.08f, 0), Fov = 38f, Texto = comentario,
					Cada = u => { if (fuego != null) fuego.LightEnergy = (0.9f + 0.25f * Mathf.Sin(u * 90f)) * (1f - 0.6f * u); if (brasas != null) brasas.AmountRatio = 1f - 0.8f * u; },
				},
			},
		};
	}

	// La foto de la pared: de la cabaña al grupo, de Paco en el centro a Tony atrás, con el silbato al cuello
	public static CinematicaCaso1.Libreto Foto(string comentario, Vector3 foto, Vector3 frente)
	{
		return new CinematicaCaso1.Libreto
		{
			Preparar = escena => Luz(escena, foto + frente * 0.9f, new Color(1f, 0.86f, 0.66f), 1.1f, 3f),
			Planos =
			{
				new() { Duracion = 3.5, CamaraDesde = foto + frente * 2.3f + V(0, 0.05f, 0.5f), CamaraHasta = foto + frente * 1.2f, MiraDesde = foto, MiraHasta = foto, Fov = 42f },
				new() { Duracion = 5.5, CamaraDesde = foto + frente * 0.45f, CamaraHasta = foto + frente * 0.3f, MiraDesde = foto, MiraHasta = foto, Fov = 38f, Texto = comentario },
				new() { Duracion = 4.5, CamaraDesde = foto + frente * 0.3f, CamaraHasta = foto + frente * 0.2f + V(0, 0.018f, -0.09f), MiraDesde = foto, MiraHasta = foto + V(0, 0.018f, -0.09f), Fov = 34f, Texto = comentario },
			},
		};
	}

	// ---------- El lago ----------

	// La primera vez que se baja al lago: del sendero oscuro al agua que brilla. Las tres líneas de Caneloso, sobre los planos.
	public static CinematicaCaso1.Libreto Llegada(string[] lineas)
	{
		return new CinematicaCaso1.Libreto
		{
			Planos =
			{
				new() { Duracion = 4, CamaraDesde = V(-2f, 1.5f, 48f), CamaraHasta = V(0f, 1.6f, 36f), MiraDesde = V(0f, 1.2f, 25f), MiraHasta = V(3f, 1.2f, 16f), Fov = 55f, Exposicion = 2.4f, Texto = lineas.ElementAtOrDefault(0) },
				new() { Duracion = 5, CamaraDesde = V(3f, 3f, 20f), CamaraHasta = V(4f, 5.5f, 4f), MiraDesde = V(7f, 0.5f, -10f), MiraHasta = V(8f, 0f, -24f), Fov = 55f, Exposicion = 1.5f, Texto = lineas.ElementAtOrDefault(1) },
				new() { Duracion = 5, CamaraDesde = V(-5f, 2.2f, -6f), CamaraHasta = V(-2f, 1.4f, -12f), MiraDesde = V(8f, 0.1f, -24f), MiraHasta = V(8.5f, 0f, -30f), Fov = 42f, Texto = lineas.ElementAtOrDefault(2) },
			},
		};
	}

	// El silbato atorado en el clavo, con el cordón roto: se mueve con el viento y se oye, muy lejos, el silbido
	public static CinematicaCaso1.Libreto Silbato(string comentario, Node3D silbato)
	{
		var punto = silbato.GlobalPosition;
		var giro = silbato.Rotation;
		return new CinematicaCaso1.Libreto
		{
			Luz = Audio + "viento_lago.wav",
			Preparar = escena => Luz(escena, punto + V(-0.6f, 0.2f, 0.2f), AzulAlgas, 0.6f, 2f),
			Planos =
			{
				new()
				{
					Duracion = 3.5, CamaraDesde = punto + V(-2.6f, 0.5f, 0.4f), CamaraHasta = punto + V(-1f, 0.2f, 0.15f), MiraDesde = punto, MiraHasta = punto, Fov = 40f,
					Cada = u => silbato.Rotation = giro + V(0, 0, 0.12f * Mathf.Sin(u * 14f)),
				},
				new()
				{
					Duracion = 8, CamaraDesde = punto + V(-0.5f, 0.12f, 0.1f), CamaraHasta = punto + V(-0.28f, 0.05f, 0.03f), MiraDesde = punto, MiraHasta = punto, Fov = 34f, Texto = comentario,
					Sonidos = new[] { (1.0, Audio + "silbido_corto.wav", -24f) },
					Cada = u => silbato.Rotation = giro + V(0, 0, 0.1f * Mathf.Sin(u * 30f)),
				},
			},
		};
	}

	// ---------- 23:20: lo que el jugador acaba de demostrar al acusar a Tony ----------

	// La escena de las 23:20 en cuadros congelados, como la cinta del Caso 0: las poses vienen de Blender
	// (Blender/scripts/construir_2320.py → Caso1/reconstruccion_2320.glb, una pose por segundo de la animación Momentos); el
	// lago, el muelle y la luz son los del juego. Solo se mueve la cámara, y la acción la cuenta el sonido: el golpe queda
	// fuera de cuadro. Hasta la confesión la silueta no se identifica; Revelacion la muestra como quien es.
	private const string Poses = "res://Assets/Cinematicas/Caso1/reconstruccion_2320.glb";
	private const string ModeloRemo = "res://Assets/Pistas/Modelos/remo_sangre/remo_sangre.tscn";
	private const string ModeloLinterna = "res://Assets/Pistas/Modelos/linterna_porky/linterna_porky.tscn";
	private static readonly Color Linterna = new(1f, 0.85f, 0.6f);
	private static readonly Color Oscuro = new(0.012f, 0.016f, 0.03f);

	private class Montaje
	{
		public Node3D Poses, Porky, LinternaMano, LinternaAire, CuerdaTensa, Silbato;
		public AnimationPlayer Reproductor;
		public Action<float> Brillo;
		public readonly List<MeshInstance3D> Silueta = new();
		public SpotLight3D HazMano, HazAire;
		public OmniLight3D LuzAgua, Relleno;
		public readonly List<(MeshInstance3D Malla, float Fase)> Ondas = new();

		public void Pose(int k)
		{
			Reproductor.Seek(k, true);
			Porky.Visible = k != 4;
			LinternaMano.Visible = k != 4;
		}
	}

	// Un tramo recto de cordón entre dos puntos
	private static void Tramo(Node3D padre, Vector3 a, Vector3 b, float radio = 0.0035f)
	{
		var malla = new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = radio, BottomRadius = radio, Height = a.DistanceTo(b), RadialSegments = 6 },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.62f, 0.06f, 0.05f), Roughness = 0.9f },
		};
		padre.AddChild(malla);
		var eje = (b - a).Normalized();
		var lado = Mathf.Abs(eje.Dot(Vector3.Forward)) > 0.95f ? Vector3.Right : Vector3.Forward;
		var x = eje.Cross(lado).Normalized();
		malla.GlobalTransform = new Transform3D(new Basis(x, eje, x.Cross(eje)), (a + b) / 2);
	}

	private static Montaje Montar(Node3D mundo)
	{
		var m = new Montaje { Brillo = Brillo(mundo) };
		m.Poses = GD.Load<PackedScene>(Poses).Instantiate<Node3D>();
		m.Poses.Name = "Poses2320";
		mundo.AddChild(m.Poses);
		m.Reproductor = m.Poses.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().First();
		m.Reproductor.Play(m.Reproductor.GetAnimationList()[0]);
		m.Reproductor.Pause();
		m.Porky = (Node3D)m.Poses.FindChild("Porky", true, false);

		// El remo, sin sangre: lo que importa queda fuera de cuadro
		var remo = GD.Load<PackedScene>(ModeloRemo).Instantiate<Node3D>();
		m.Poses.FindChild("Anim_Remo", true, false).AddChild(remo);
		foreach (Node n in remo.FindChildren("Sangre*", "MeshInstance3D", true, false)) ((Node3D)n).Visible = false;
		if (mundo.GetNodeOrNull<Node3D>("Orilla/Remo") is { } remoOrilla) remoOrilla.Visible = false;

		// Tony y el remo: mallas que se pueden volver silueta
		m.Silueta.AddRange(m.Poses.FindChild("Tony", true, false).FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>());
		m.Silueta.AddRange(remo.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Where(x => x.Visible));

		// La linterna en la mano de Porky: alumbra el agua
		var anclaLinterna = m.Poses.FindChild("Anim_Linterna", true, false);
		m.LinternaMano = GD.Load<PackedScene>(ModeloLinterna).Instantiate<Node3D>();
		anclaLinterna.AddChild(m.LinternaMano);
		m.HazMano = new SpotLight3D { LightColor = Linterna, LightEnergy = 9f, SpotAngle = 26f, SpotRange = 9f, ShadowEnabled = true };
		anclaLinterna.AddChild(m.HazMano);

		// La misma linterna, congelada en el aire al caer
		m.LinternaAire = GD.Load<PackedScene>(ModeloLinterna).Instantiate<Node3D>();
		mundo.AddChild(m.LinternaAire);
		m.LinternaAire.GlobalPosition = V(7.82f, 0.62f, -34.28f);
		m.LinternaAire.RotationDegrees = V(-62f, 38f, 14f);
		m.HazAire = new SpotLight3D { LightColor = Linterna, LightEnergy = 7f, SpotAngle = 30f, SpotRange = 4f };
		m.LinternaAire.AddChild(m.HazAire);
		m.LinternaAire.Visible = false;

		// El azul de las algas donde cayó, y las ondas que abre
		m.LuzAgua = Luz(mundo, V(8f, 0.15f, -35.2f), AzulAlgas, 0f, 7f);
		m.Relleno = Luz(mundo, V(8.6f, 1.6f, -32f), new Color(0.62f, 0.72f, 1f), 0f, 7f);
		for (int i = 0; i < 4; i++)
		{
			var onda = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.97f, OuterRadius = 1f, Rings = 48, RingSegments = 4 },
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = new Color(0.45f, 0.85f, 1f, 0.7f),
					Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
					ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				},
				Visible = false,
			};
			mundo.AddChild(onda);
			onda.GlobalPosition = V(8f, -0.03f, -35.2f);
			m.Ondas.Add((onda, i * 0.25f));
		}

		// El silbato del juego ya trae su cordón roto; tenso, le falta el tramo que sale del cuadro hacia quien lo jala
		m.Silbato = mundo.GetNodeOrNull<Node3D>("Embarcadero/Silbato");
		if (m.Silbato != null) m.Silbato.Visible = false;
		var clavo = V(7.0f, 0.77f, -33.9f);
		m.CuerdaTensa = new Node3D { Name = "CuerdaTensa", Visible = false };
		mundo.AddChild(m.CuerdaTensa);
		Tramo(m.CuerdaTensa, clavo + V(0.02f, 0.012f, 0), clavo + V(1.1f, 1.1f, -0.25f), 0.0018f);
		return m;
	}

	private static void Silueta(Montaje m, bool activa)
	{
		var oscuro = new StandardMaterial3D { AlbedoColor = Oscuro, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
		foreach (var malla in m.Silueta) malla.MaterialOverride = activa ? oscuro : null;
	}

	public static CinematicaCaso1.Libreto Reconstruccion()
	{
		Montaje m = null;
		var punta = V(8f, 1.05f, -33f);
		return new CinematicaCaso1.Libreto
		{
			Mundo = Lago,
			Luz = Audio + "agua_muelle.wav",
			Preparar = mundo =>
			{
				m = Montar(mundo);
				Silueta(m, true);
				m.Brillo(0.6f);
				m.Pose(2);
			},
			Planos =
			{
				// 1. Negro: pasos sobre madera, agua, el clic de una linterna
				new()
				{
					Duracion = 3.4, Negro = true, Hora = "23:20 · EMBARCADERO", CamaraDesde = V(15f, 1f, -23f), CamaraHasta = V(15f, 1f, -23f), MiraDesde = punta, MiraHasta = punta,
					Sonidos = new[] { (0.1, Audio + "pasos_madera.wav", -5f), (2.85, Audio + "clic_linterna.wav", -3f) },
				},
				// 2. General desde el lago: Porky de espaldas, atrás una silueta con el remo levantado. Push-in lento
				new()
				{
					Duracion = 5.2, Exposicion = 2.2f, CamaraDesde = V(14.2f, 1.1f, -24.4f), CamaraHasta = V(12.7f, 1.05f, -26.6f), MiraDesde = V(8f, 1.35f, -32.3f), MiraHasta = V(8f, 1.3f, -32.5f), Fov = 30f,
					Cada = _ => { m.Poses.Visible = true; m.Pose(2); m.Brillo(0.6f); m.LuzAgua.LightEnergy = 0.7f; m.HazMano.Visible = true; m.LinternaAire.Visible = false; },
				},
				// 3. Fuera de cuadro: la linterna congelada en el aire. Golpe sordo y silbido corto
				new()
				{
					Duracion = 3.2, Exposicion = 1.7f, CamaraDesde = V(8.15f, 0.5f, -35.2f), CamaraHasta = V(8.08f, 0.52f, -35.0f), MiraDesde = V(7.82f, 0.6f, -34.28f), MiraHasta = V(7.82f, 0.61f, -34.28f), Fov = 30f,
					Sonidos = new[] { (-0.12, Audio + "golpe_sordo.wav", -1f), (0.08, Audio + "silbido_corto.wav", -7f) },
					Cada = _ => { m.Poses.Visible = false; m.HazMano.Visible = false; m.LinternaAire.Visible = true; m.Brillo(0.9f); m.LuzAgua.LightEnergy = 0.9f; },
				},
				// 4. Desde el agua: la estela azul y las ondas; la silueta retrocede en el muelle. Chapoteo
				new()
				{
					Duracion = 4.8, Exposicion = 2f, CamaraDesde = V(10.6f, 0.1f, -40.6f), CamaraHasta = V(10.2f, 0.12f, -39.8f), MiraDesde = V(8.2f, 0.8f, -32f), MiraHasta = V(8.2f, 0.75f, -32.2f), Fov = 30f,
					Sonidos = new[] { (-0.25, Audio + "chapoteo.wav", -2f) },
					Cada = u =>
					{
						m.Poses.Visible = true;
						m.Pose(4);
						m.LinternaAire.Visible = false;
						m.HazMano.Visible = false;
						m.Brillo(2.4f - 0.5f * u);
						m.LuzAgua.LightEnergy = 3.4f - u;
						foreach (var (onda, fase) in m.Ondas)
						{
							float r = 0.5f + 3.2f * Mathf.Clamp(u * 0.55f + fase, 0f, 1f);
							onda.Visible = true;
							onda.Scale = V(r, 0.25f, r);
						}
					},
				},
				// 5. Inserto: el cordón tenso, atorado en el clavo, a punto de romperse
				new()
				{
					Duracion = 3.0, Exposicion = 1.4f, CamaraDesde = V(7.42f, 0.86f, -33.42f), CamaraHasta = V(7.35f, 0.84f, -33.5f), MiraDesde = V(7.02f, 0.78f, -33.88f), MiraHasta = V(7.02f, 0.78f, -33.88f), Fov = 34f,
					Cada = _ =>
					{
						m.Poses.Visible = false;
						foreach (var (onda, _) in m.Ondas) onda.Visible = false;
						m.Relleno.GlobalPosition = V(7.6f, 1.2f, -33.2f);
						m.Relleno.LightEnergy = 1.2f;
						m.CuerdaTensa.Visible = true;
						if (m.Silbato != null) m.Silbato.Visible = true;
						m.Brillo(1.6f);
						m.LuzAgua.LightEnergy = 1.2f;
					},
				},
				// 6. Inserto final: el silbato colgando solo del clavo. Silencio y agua
				new()
				{
					Duracion = 4.0, Exposicion = 1.4f, CamaraDesde = V(7.42f, 0.8f, -33.5f), CamaraHasta = V(7.36f, 0.8f, -33.56f), MiraDesde = V(7.03f, 0.74f, -33.9f), MiraHasta = V(7.03f, 0.74f, -33.9f), Fov = 34f,
					Cada = _ =>
					{
						m.Poses.Visible = false;
						m.CuerdaTensa.Visible = false;
						if (m.Silbato != null) m.Silbato.Visible = true;
						m.Relleno.GlobalPosition = V(7.6f, 1.2f, -33.2f);
						m.Relleno.LightEnergy = 1.2f;
						m.Brillo(1.2f);
						m.LuzAgua.LightEnergy = 0.8f;
					},
				},
			},
		};
	}

	// Al final de la confesión: el mismo instante, y ahora se ve quién era
	public static CinematicaCaso1.Libreto Revelacion()
	{
		Montaje m = null;
		return new CinematicaCaso1.Libreto
		{
			Mundo = Lago,
			Luz = Audio + "agua_muelle.wav",
			Preparar = mundo =>
			{
				m = Montar(mundo);
				m.Pose(2);
				m.Brillo(0.9f);
				m.Relleno.GlobalPosition = V(9.8f, 1.6f, -33.4f);
				m.Relleno.LightEnergy = 2.4f;
				m.Relleno.LightColor = new Color(0.82f, 0.86f, 1f);
				m.LuzAgua.LightEnergy = 1f;
			},
			Planos =
			{
				new()
				{
					Duracion = 4.6, Hora = "23:20 · EMBARCADERO", Exposicion = 1.3f, CamaraDesde = V(12.6f, 1.1f, -35.6f), CamaraHasta = V(12f, 1.15f, -35f), MiraDesde = V(8f, 1.35f, -32.4f), MiraHasta = V(8f, 1.45f, -32.2f), Fov = 28f,
					Cada = _ => m.Pose(2),
				},
			},
		};
	}

	// ---------- Los finales ----------

	// Final A: Tony esposado hacia la patrulla, bajo la lluvia; travelling hacia atrás hasta el muelle vacío, con el agua brillando
	public static CinematicaCaso1.Libreto FinalA(string narracion)
	{
		Node3D tony = null, hecdog = null;
		Action<float> brillo = null;
		return new CinematicaCaso1.Libreto
		{
			Mundo = Lago,
			Luz = "res://Assets/Audio/Ambiente/lluvia_ventana.wav",
			Preparar = mundo =>
			{
				tony = Actor(mundo, Tony, EscalaTony, V(-1f, 0f, 44f), 0f);
				hecdog = Actor(mundo, Hecdog, EscalaHecdog, V(-1.8f, 0f, 43f), 0f);
				Animar(hecdog, "mixamo_com");
				Animar(tony, "global/CaminaTony", 0.7f);
				var lluvia = Particulas(mundo, V(-2f, 12f, 48f), new Color(0.7f, 0.8f, 1f, 0.6f), 700, 1.1f, 2f, 0.012f, new Vector3(0, -22f, 0), 6f);
				((ParticleProcessMaterial)lluvia.ProcessMaterial).EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
				((ParticleProcessMaterial)lluvia.ProcessMaterial).EmissionBoxExtents = V(14f, 1f, 14f);
				brillo = Brillo(mundo);
				brillo(0.6f);
			},
			Planos =
			{
				new()
				{
					Duracion = 6, Exposicion = 2.2f, CamaraDesde = V(0.5f, 1.7f, 38f), CamaraHasta = V(-0.5f, 1.7f, 43f), MiraDesde = V(-1.5f, 1f, 46f), MiraHasta = V(-3.5f, 1f, 53f), Fov = 40f,
					Cada = u =>
					{
						if (tony != null) tony.GlobalPosition = V(-1f, 0f, 44f).Lerp(V(-3.5f, 0f, 53f), u);
						if (hecdog != null) { hecdog.GlobalPosition = V(-1.8f, 0f, 43f).Lerp(V(-4.3f, 0f, 52f), u); hecdog.Rotation = tony?.Rotation ?? Vector3.Zero; }
					},
				},
				new()
				{
					Duracion = 14, CamaraDesde = V(8f, 1.6f, -22f), CamaraHasta = V(8f, 2.4f, -9f), MiraDesde = V(8f, 0.6f, -33f), MiraHasta = V(8f, 0.3f, -32f), Fov = 42f, Texto = narracion,
					Cada = u => brillo?.Invoke(0.6f + 1.5f * Mathf.Clamp(u * 2f, 0f, 1f)),
				},
			},
		};
	}

	// Final B, primera parte: Tony mira en silencio desde su porche, con su silbato nuevo
	public static CinematicaCaso1.Libreto FinalBPorche(string narracion)
	{
		var tony = V(-25.6f, 1.45f, -13.4f);
		return new CinematicaCaso1.Libreto
		{
			Mundo = Campamento,
			Preparar = mundo => Luz(mundo, tony + V(1.2f, 0.8f, 2f), new Color(0.75f, 0.82f, 1f), 1.2f, 7f),
			Planos =
			{
				new() { Duracion = 13, CamaraDesde = tony + V(0.6f, 0.1f, 9f), CamaraHasta = tony + V(0.15f, 0.05f, 2.4f), MiraDesde = tony, MiraHasta = tony, Fov = 38f, Texto = narracion },
			},
		};
	}

	// Final B, segunda parte: el lago se vacía. El periódico flota, se empapa y se hunde, y se apaga el brillo de las algas.
	public static CinematicaCaso1.Libreto FinalBLago(string obras, string periodico)
	{
		Action<float> brillo = null;
		MeshInstance3D agua = null;
		Vector3 nivel = Vector3.Zero;
		Node3D hoja = null;
		var punto = V(14f, -0.2f, -24f);
		string titular = periodico.Contains('«') ? periodico.Substring(periodico.IndexOf('«') + 1, periodico.IndexOf('»') - periodico.IndexOf('«') - 1) : "";
		return new CinematicaCaso1.Libreto
		{
			Mundo = Lago,
			Luz = Audio + "bombas.wav",
			Preparar = mundo =>
			{
				agua = mundo.GetNodeOrNull<MeshInstance3D>("Lago/Agua");
				if (agua != null) nivel = agua.GlobalPosition;
				brillo = Brillo(mundo);
				brillo(1.2f);
				hoja = Periodico(mundo, punto, titular);
			},
			Planos =
			{
				new()
				{
					Duracion = 10, CamaraDesde = V(-16f, 6f, 6f), CamaraHasta = V(-12f, 5f, 2f), MiraDesde = V(8f, 0f, -26f), MiraHasta = V(8f, -0.2f, -28f), Fov = 40f, Texto = obras,
					Cada = u => { if (agua != null) agua.GlobalPosition = nivel + V(0, -0.5f * u, 0); },
				},
				new()
				{
					Duracion = 16, CamaraDesde = punto + V(0, 3.2f, 0.02f), CamaraHasta = punto + V(0, 1.6f, 0.02f), MiraDesde = punto, MiraHasta = punto, Fov = 40f, Texto = periodico,
					Cada = u =>
					{
						if (agua != null) agua.GlobalPosition = nivel + V(0, -0.5f, 0);
						if (hoja != null)
						{
							float hundida = Mathf.Clamp((u - 0.6f) / 0.3f, 0f, 1f);
							hoja.GlobalPosition = punto + V(0, -0.5f - 0.5f * hundida, 0);
							((StandardMaterial3D)((MeshInstance3D)hoja).MaterialOverride).AlbedoColor = new Color(0.82f, 0.8f, 0.72f, 1f - hundida);
							foreach (Node n in hoja.GetChildren())
								if (n is SpriteBase3D s) s.Modulate = new Color(s.Modulate, 1f - hundida);
								else if (n is Label3D l) l.Modulate = new Color(l.Modulate, 1f - hundida);
						}
						brillo?.Invoke(1.2f * (1f - Mathf.Clamp((u - 0.72f) / 0.25f, 0f, 1f)));
					},
				},
			},
		};
	}

	// La hoja de periódico: el titular del guion y los retratos de Miawrio y Tony
	private static Node3D Periodico(Node3D mundo, Vector3 punto, string titular)
	{
		var hoja = new MeshInstance3D
		{
			Mesh = new PlaneMesh { Size = new Vector2(0.9f, 1.15f) },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.82f, 0.8f, 0.72f), Roughness = 1f, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha },
		};
		mundo.AddChild(hoja);
		hoja.GlobalPosition = punto;
		var texto = new Label3D
		{
			Text = titular,
			Font = GD.Load<FontFile>("res://Assets/Fuentes/SpecialElite-Regular.ttf"),
			FontSize = 30,
			PixelSize = 0.0022f,
			Width = 360f,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.1f, 0.1f, 0.1f),
			OutlineSize = 0,
			Shaded = false,
			RotationDegrees = V(-90f, 0, 0),
			Position = V(0, 0.01f, -0.3f),
		};
		hoja.AddChild(texto);
		foreach (var (ruta, x) in new[] { ("res://Assets/Retratos/miawrio.png", -0.2f), ("res://Assets/Retratos/tony.png", 0.2f) })
		{
			var retrato = new Sprite3D
			{
				Texture = GD.Load<Texture2D>(ruta),
				PixelSize = 0.34f / Mathf.Max(1, GD.Load<Texture2D>(ruta).GetWidth()),
				Shaded = false,
				RotationDegrees = V(-90f, 0, 0),
				Position = V(x, 0.012f, 0.22f),
			};
			hoja.AddChild(retrato);
		}
		return hoja;
	}
}
