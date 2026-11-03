using Godot;
using System;
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

	public static CinematicaCaso1.Libreto Reconstruccion(string cuaderno)
	{
		Node3D tony = null, porky = null, remo = null;
		OmniLight3D linterna = null;
		GpuParticles3D salpicadura = null;
		Action<float> brillo = null;
		var finPorky = V(8.1f, 0.85f, -31.5f);
		return new CinematicaCaso1.Libreto
		{
			Mundo = Lago,
			Luz = Audio + "viento_lago.wav",
			Preparar = mundo =>
			{
				tony = Actor(mundo, Tony, EscalaTony, V(1f, 0f, 30f), 200f);
				porky = Actor(mundo, Porky, EscalaPorky, finPorky, 180f);
				BrazosAbajo(porky);
				Animar(tony, "global/CaminaTony");
				linterna = Luz(mundo, finPorky + V(0.3f, 1f, 0.2f), new Color(1f, 0.85f, 0.6f), 0f, 5f);
				// El remo del juego, ahora en sus manos
				remo = mundo.GetNodeOrNull<Node3D>("Orilla/Remo");
				if (remo != null) remo.Visible = false;
				salpicadura = Particulas(mundo, finPorky + V(0.9f, -1f, 0f), AzulAlgas, 90, 1.2f, 3.2f, 0.025f, new Vector3(0, -7f, 0), 30f);
				salpicadura.Emitting = false;
				salpicadura.OneShot = true;
				brillo = Brillo(mundo);
				brillo(0.45f);
			},
			Planos =
			{
				new()
				{
					Duracion = 4.5, Hora = "23:12 · SENDERO DEL LAGO", Recuerdo = 0.35f, Exposicion = 3f, CamaraDesde = V(-1f, 1.4f, 36f), CamaraHasta = V(0f, 1.4f, 31f), MiraDesde = V(1f, 1f, 28f), MiraHasta = V(3f, 1f, 22f), Fov = 40f,
					Cada = u => { if (tony != null) tony.GlobalPosition = V(1f, 0f, 30f).Lerp(V(4f, 0f, 22f), u); },
				},
				new()
				{
					Duracion = 3.5, Hora = "23:15 · EMBARCADERO", Recuerdo = 0.35f, Exposicion = 1.8f, CamaraDesde = V(-3f, 1.8f, -18f), CamaraHasta = V(-1f, 1.6f, -21f), MiraDesde = finPorky + V(0, 0.8f, 0), MiraHasta = finPorky + V(0, 0.8f, 0), Fov = 16f,
					Cada = u => { if (linterna != null) linterna.LightEnergy = 4f; if (tony != null) tony.Visible = false; },
				},
				new()
				{
					Duracion = 3.5, Hora = "23:19 · DESDE LA COLINA", Recuerdo = 0.35f, Exposicion = 1.8f, CamaraDesde = V(21f, 7.5f, -7f), CamaraHasta = V(20.5f, 7.2f, -8f), MiraDesde = V(8.2f, 1f, -30f), MiraHasta = V(8.2f, 1f, -30.2f), Fov = 16f,
					Sonidos = new[] { (2.6, "res://Assets/Audio/Cinematica/Cinta/cinta_stop.wav", -10f) },
					Cada = u =>
					{
						if (tony == null) return;
						tony.Visible = true;
						Animar(tony, "global/InacTony");
						tony.GlobalPosition = V(8.2f, 0.85f, -29f);
						tony.RotationDegrees = V(0, 180f, 0);
					},
				},
				new()
				{
					Duracion = 3.2, Hora = "23:20", Recuerdo = 0.35f, Exposicion = 1.8f, CamaraDesde = V(3.4f, 1.5f, -29f), CamaraHasta = V(3.8f, 1.4f, -29.4f), MiraDesde = V(8.1f, 1.5f, -30.3f), MiraHasta = V(8.1f, 1.5f, -30.5f), Fov = 40f,
					Sonidos = new[] { (1.75, Audio + "silbido_corto.wav", -8f), (1.7, "res://Assets/Audio/Cinematica/Cinta/reloj_rebote.wav", -6f) },
					Cada = u =>
					{
						if (remo != null)
						{
							remo.Visible = true;
							float golpe = Mathf.Clamp((u - 0.42f) / 0.14f, 0f, 1f);
							remo.GlobalPosition = V(8.2f, 2.0f - 0.3f * golpe, -29.6f - 0.8f * golpe);
							remo.GlobalRotation = V(Mathf.DegToRad(100f * golpe - 40f), 0, 0);
						}
						if (porky != null && u > 0.56f) porky.GlobalRotation = V(Mathf.DegToRad(-30f * Mathf.Clamp((u - 0.56f) / 0.44f, 0f, 1f)), Mathf.Pi, 0);
					},
				},
				new()
				{
					Duracion = 3.5, Hora = "23:20", Recuerdo = 0.2f, Exposicion = 1.5f, CamaraDesde = V(13f, 0.5f, -24f), CamaraHasta = V(12.5f, 0.4f, -25.5f), MiraDesde = V(9f, 0f, -31f), MiraHasta = V(9f, -0.1f, -31.2f), Fov = 40f,
					Sonidos = new[] { (0.35, Audio + "chapoteo.wav", -3f) },
					Cada = u =>
					{
						if (porky != null)
						{
							float caida = Mathf.Clamp(u / 0.13f, 0f, 1f);
							porky.GlobalPosition = (finPorky + V(0.9f, 0, 0)).Lerp(finPorky + V(0.9f, -1.5f, 0), caida * caida);
							porky.Visible = u < 0.2f;
							porky.GlobalRotation = V(Mathf.DegToRad(-30f - 60f * caida), Mathf.Pi, 0);
						}
						if (salpicadura != null && u > 0.1f && !salpicadura.Emitting) salpicadura.Emitting = true;
						brillo?.Invoke(0.45f + 1.8f * Mathf.Clamp((u - 0.1f) * 3f, 0f, 1f));
					},
				},
				new()
				{
					Duracion = 7.5, Hora = "23:20", Exposicion = 1.6f, CamaraDesde = V(-21f, 2.6f, -7f), CamaraHasta = V(-20f, 2.5f, -7.6f), MiraDesde = V(8f, 0.2f, -30f), MiraHasta = V(8f, 0.2f, -30f), Fov = 30f, Texto = cuaderno,
					Cada = u => brillo?.Invoke(2.2f - 0.6f * u),
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
