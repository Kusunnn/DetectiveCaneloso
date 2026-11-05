using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Caso 1 «La Noche del Ciervo de Luz» — Escenario 3: la sala de interrogatorio de la estación (HU-20.6).
// Solo el escenario y lo que el guion de los interrogatorios va a necesitar: los seis implicados entran uno por uno
// (TraerImplicado / RetirarImplicado), Hecdog espera junto a la puerta, el corcho con las fotos y el estambre rojo,
// y la mesa con las pruebas que el detective ya tiene. Los diálogos son los del guion (GuionCaso1): Hecdog lleva
// la sala y la acusación, y cada implicado pasa a la silla cuando le toca su interrogatorio.
public partial class SalaInterrogatorioCaso1 : EscenarioCaso1
{
	// Avisos para quien quiera reaccionar a la sala (los diálogos ya van por GuionCaso1)
	[Signal] public delegate void HablarConHecdogEventHandler();
	[Signal] public delegate void HablarConImplicadoEventHandler(string implicado);
	[Signal] public delegate void ImplicadoSentadoEventHandler(string implicado);
	[Signal] public delegate void ImplicadoRetiradoEventHandler(string implicado);
	[Signal] public delegate void PruebaMostradaEventHandler(string idPista);

	[Export] public PuntoInteraccion Salida;
	[Export] public PuntoInteraccion Hecdog;
	// Un hijo por implicado (JuanCarlos, Aya, Miawrio, Eriz, Mimita, Tony), cada uno con Modelo, Animacion y Colision
	[Export] public Node3D Implicados;
	[Export] public Marker3D PuntoSilla;
	[Export] public Marker3D PuntoEntradaImplicado;
	[Export] public Node3D Corcho;
	[Export] public Node3D MesaEvidencias;
	[Export] public float VelocidadCaminar = AnimacionPersonaje.VelocidadPorCodigo;

	private const uint CapaImplicado = (1u << 3) | (1u << 4);

	// Nombre para mostrar, animación de caminar (los que la traen) y archivo de retrato
	private static readonly Dictionary<string, (string Nombre, string Caminar, string Retrato)> Datos = new()
	{
		["JuanCarlos"] = ("Juan Carlos", "", "juan carlos"),
		["Aya"] = ("Aya", "Caminaya", "aya"),
		["Miawrio"] = ("Miawrio", "global/Caminar", "miawrio"),
		["Eriz"] = ("Eriz", "", "eriz"),
		["Mimita"] = ("Mimita", "Caminar", "mimita"),
		["Tony"] = ("Tony", "global/CaminaTony", "tony"),
	};

	// Sus animaciones de nervios. Juan Carlos y Eriz no tienen: se les ve por código. Mimita usa otros reposos, acelerados.
	private static readonly Dictionary<string, (string Tenso, string Alterado, float Acelera)> Nervios = new()
	{
		["Aya"] = ("Tensaya", "Alteraya", 1f),
		["Miawrio"] = ("global/Tenso", "global/Alterado", 1f),
		["Mimita"] = ("ReposoMirar", "ReposoPeso", 1.7f),
		["Tony"] = ("global/TensoTony", "global/AltTony", 1f),
	};

	// El recorrido de la cadera, alrededor de la mesa: la silla queda del otro lado, frente al detective.
	// La mesa va de x = -0.8 a 0.8 y de z = -2.45 a -1.55; la silla está en z = -2.8 y el asiento a 0.47 m del piso.
	private const float AlturaAsiento = 0.47f, GrosorCadera = 0.08f, SegundosGiro = 0.28f, SegundosSentarse = 0.5f;
	private static readonly Vector3 ViaPasillo = new(1.35f, 0, -0.9f), ViaEsquina = new(1.25f, 0, -2.85f), JuntoSilla = new(0.62f, 0, -2.85f);
	private readonly Dictionary<string, PoseInterrogatorio> _poses = new();

	// Id del implicado en el guion → su nodo en Implicados
	private static readonly Dictionary<string, string> ClaveDeGuion = new()
	{
		["juan_carlos"] = "JuanCarlos", ["aya"] = "Aya", ["miawrio"] = "Miawrio",
		["eriz"] = "Eriz", ["mimita"] = "Mimita", ["tony"] = "Tony",
	};

	public string ImplicadoActual { get; private set; }
	public PanelArmarCaso Panel { get; private set; }
	public IEnumerable<string> NombresImplicados => Datos.Keys;
	private bool _moviendo;

	protected override string Escena => PartidaEscenas.Sala;
	protected override string Lugar => "la estación";
	protected override string ZonaPorDefecto => "Sala de interrogatorio";

	public override void _Ready()
	{
		base._Ready();
		SetProcess(false);
		Salida.Usado += () => Conversar(AlSalir);
		Hecdog.Usado += () => { EmitSignal(SignalName.HablarConHecdog); Conversar(() => Guion.Sala(Sigue)); };
		if (Guion != null) // Sin guion cuando solo se filma el escenario (SoloVer)
		{
			PrepararPuestaEnEscena();
			var panel = PanelArmarCaso.Crear(this, Guion, Dialogo);
			Panel = panel;
			Guion.AbrirArmarCaso = panel.Abrir;
			Guion.SelloDeCasilla = panel.Sello;
			Guion.CerrarPanelCaso = panel.Cerrar;
		}
		foreach (Node hijo in Implicados.GetChildren())
		{
			if (hijo is not Node3D implicado) continue;
			string clave = implicado.Name;
			implicado.Visible = false;
			if (implicado.GetNodeOrNull<PuntoInteraccion>("Colision") is { } punto)
			{
				punto.Habilitar(false, CapaImplicado);
				string quien = ClaveDeGuion.First(p => p.Value == clave).Key;
				punto.Usado += () => { EmitSignal(SignalName.HablarConImplicado, clave); Conversar(() => Guion.Interrogar(quien, Sigue)); };
			}
			if (implicado.GetNodeOrNull<AnimacionPersonaje>("Animacion") is { } anim && Nervios.TryGetValue(clave, out var nervios))
			{
				anim.AnimacionTenso = nervios.Tenso;
				anim.AnimacionAlterado = nervios.Alterado;
				anim.AceleraNervios = nervios.Acelera;
			}
			if (implicado.GetNodeOrNull<Node3D>("Modelo") is { } modelo && PoseInterrogatorio.Poner(implicado, modelo) is { } pose) _poses[clave] = pose;
		}
		ArmarCorcho();
	}

	protected override async void AlLlegar(bool desdeOtroEscenario)
	{
		AcomodarPruebas();
		ActualizarObjetivo();
		// Al volver ya no se repite la presentación ni el mismo pensamiento: el objetivo dice qué sigue (HU-9.5 CA-03)
		if (Guion.SalaVista || Guion.Resuelto) return;
		// La primera vez, Hecdog recibe al detective y presenta a los seis
		await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
		if (Sigue()) Conversar(() => Guion.Sala(Sigue));
	}

	protected override void AlRestaurar() => AcomodarPruebas();

	protected override void ActualizarObjetivo() =>
		Objetivo(Guion.Objetivo() ?? "Sala de interrogatorio de la estación. Habla con Hecdog, o sal por la puerta para volver al lago o al campamento.");

	protected override string PensamientoDeZona(string zona) => null;
	protected override string NombreZona(string zona) => zona;

	// ---------- Implicados ----------

	// Hace pasar a un implicado: entra por la puerta, rodea la mesa, se detiene junto a la silla, gira y se sienta frente al
	// detective. corta: ya entró antes, aparece a unos pasos de la silla. Si ya había otro, primero sale. Devuelve false si el
	// nombre no existe.
	public async Task<bool> TraerImplicado(string clave, bool corta = false)
	{
		var implicado = Implicados.GetNodeOrNull<Node3D>(clave);
		if (implicado == null || _moviendo) return false;
		if (ImplicadoActual == clave) return true;
		if (ImplicadoActual != null) await RetirarImplicado();
		_moviendo = true;
		ImplicadoActual = clave;
		if (_poses.TryGetValue(clave, out var poseInicial)) poseInicial.Sentado = 0f;
		implicado.GlobalPosition = PuntoEntradaImplicado.GlobalPosition;
		implicado.GlobalRotation = Vector3.Zero;
		implicado.Visible = true;
		float piso = PuntoEntradaImplicado.GlobalPosition.Y;
		// Dónde queda su cadera respecto a su nodo, mirando a +Z (algunos modelos no están centrados)
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Vector3 cadera = CaderaLocal(implicado, clave);
		var recorrido = corta
			? new[] { ViaEsquina, JuntoSilla }
			: new[] { ViaPasillo, ViaEsquina, JuntoSilla };
		if (corta) implicado.GlobalPosition = ViaEsquina + new Vector3(0, piso, 0) - new Basis(Vector3.Up, Mathf.Pi) * cadera;
		await Caminar(implicado, recorrido.Select(p => p + new Vector3(0, piso, 0)).ToArray(), clave, cadera);
		// Gira hacia la mesa y se acomoda en el asiento
		await Girar(implicado, PuntoSilla.GlobalRotation.Y);
		await Sentarse(implicado, clave, true);
		implicado.GetNodeOrNull<PuntoInteraccion>("Colision")?.Habilitar(true, CapaImplicado);
		_moviendo = false;
		EmitSignal(SignalName.ImplicadoSentado, clave);
		return true;
	}

	// El implicado actual se levanta y sale por la puerta
	public async Task RetirarImplicado()
	{
		if (ImplicadoActual == null || _moviendo) return;
		string clave = ImplicadoActual;
		var implicado = Implicados.GetNode<Node3D>(clave);
		_moviendo = true;
		implicado.GetNodeOrNull<PuntoInteraccion>("Colision")?.Habilitar(false, CapaImplicado);
		implicado.GetNodeOrNull<AnimacionPersonaje>("Animacion")?.FijarAnimo(AnimacionPersonaje.Animo.Sereno);
		await Sentarse(implicado, clave, false);
		float piso = PuntoEntradaImplicado.GlobalPosition.Y;
		Vector3 cadera = CaderaLocal(implicado, clave);
		var puerta = PuntoEntradaImplicado.GlobalPosition;
		await Caminar(implicado, new[] { ViaEsquina, ViaPasillo, new Vector3(puerta.X, 0, puerta.Z) }.Select(p => p + new Vector3(0, piso, 0)).ToArray(), clave, cadera);
		implicado.Visible = false;
		ImplicadoActual = null;
		_moviendo = false;
		EmitSignal(SignalName.ImplicadoRetirado, clave);
	}

	// Lo mismo, sin esperar (para GDScript o un botón): avisa con ImplicadoSentado / ImplicadoRetirado
	public void Traer(string clave) => _ = TraerImplicado(clave);
	public void Retirar() => _ = RetirarImplicado();

	private Vector3 CaderaLocal(Node3D implicado, string clave)
	{
		var modelo = implicado.GetNodeOrNull<Node3D>("Modelo");
		if (modelo == null) return Vector3.Zero;
		var local = implicado.GlobalBasis.Orthonormalized().Inverse() * (PoseInterrogatorio.PosicionCadera(modelo) - implicado.GlobalPosition);
		local.Y = 0;
		return local;
	}

	private static float Giro(Vector3 direccion) => Mathf.Atan2(direccion.X, direccion.Z); // El frente de los modelos es +Z

	private async Task Girar(Node3D implicado, float giro)
	{
		float desde = implicado.GlobalRotation.Y;
		float hasta = desde + Mathf.Wrap(giro - desde, -Mathf.Pi, Mathf.Pi);
		if (Mathf.Abs(hasta - desde) < 0.01f) return;
		var tween = CreateTween();
		tween.TweenProperty(implicado, "global_rotation:y", hasta, SegundosGiro).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	// Lleva la cadera por los puntos: gira mientras avanza, sin voltear de golpe, y los pies van a la velocidad del cuerpo
	private async Task Caminar(Node3D implicado, Vector3[] recorrido, string clave, Vector3 cadera)
	{
		var anim = implicado.GetNodeOrNull<AnimacionPersonaje>("Animacion");
		string caminar = Datos.TryGetValue(clave, out var d) ? d.Caminar : "";
		anim?.Caminar(true, caminar, VelocidadCaminar);
		Vector3 anterior = implicado.GlobalPosition + implicado.GlobalBasis.Orthonormalized() * cadera;
		foreach (Vector3 punto in recorrido)
		{
			var dir = punto - anterior;
			dir.Y = 0;
			float largo = dir.Length();
			if (largo < 0.05f) continue;
			float giro = Giro(dir);
			var destino = punto - new Basis(Vector3.Up, giro) * cadera;
			float desde = implicado.GlobalRotation.Y;
			float hasta = desde + Mathf.Wrap(giro - desde, -Mathf.Pi, Mathf.Pi);
			var tween = CreateTween().SetParallel();
			tween.TweenProperty(implicado, "global_position", destino, largo / VelocidadCaminar);
			tween.TweenProperty(implicado, "global_rotation:y", hasta, Mathf.Min(SegundosGiro, largo / VelocidadCaminar)).SetTrans(Tween.TransitionType.Sine);
			await ToSignal(tween, Tween.SignalName.Finished);
			anterior = punto;
		}
		anim?.Caminar(false);
		// Que se note el alto antes de seguir
		await ToSignal(GetTree().CreateTimer(0.12), SceneTreeTimer.SignalName.Timeout);
	}

	// Baja la cadera al asiento mientras dobla las piernas, o al revés
	private async Task Sentarse(Node3D implicado, string clave, bool sentarse)
	{
		var pose = _poses.GetValueOrDefault(clave);
		var modelo = implicado.GetNodeOrNull<Node3D>("Modelo");
		if (modelo == null) return;
		Vector3 hasta;
		if (sentarse)
		{
			_paradoEn = implicado.GlobalPosition;
			Vector3 asiento = new(0, PuntoEntradaImplicado.GlobalPosition.Y + AlturaAsiento + GrosorCadera, -2.85f);
			hasta = implicado.GlobalPosition + (asiento - PoseInterrogatorio.PosicionCadera(modelo));
		}
		else hasta = _paradoEn;
		var tween = CreateTween().SetParallel();
		tween.TweenProperty(implicado, "global_position", hasta, SegundosSentarse).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		if (pose != null)
			tween.TweenMethod(Callable.From<float>(v => { if (IsInstanceValid(pose)) pose.Sentado = v; }), sentarse ? 0f : 1f, sentarse ? 1f : 0f, SegundosSentarse);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private Vector3 _paradoEn;

	public string NombreDe(string clave) => Datos.TryGetValue(clave, out var d) ? d.Nombre : clave;

	// ---------- Pruebas ----------

	// Pone en la mesa auxiliar el modelo de cada pista del diario (las de los tres escenarios)
	private void AcomodarPruebas()
	{
		foreach (Node hijo in MesaEvidencias.GetChildren()) hijo.QueueFree();
		var ids = new List<string>();
		foreach (Variant pista in Hud.Get("pistas").AsGodotArray())
		{
			var recurso = pista.As<Resource>();
			if (recurso.Get("origen_id").AsStringName().ToString() != "") continue;
			ids.Add(recurso.Get("id").AsStringName().ToString());
		}
		const int Columnas = 7;
		const float Celda = 0.25f;
		for (int i = 0; i < ids.Count; i++)
		{
			string ruta = $"res://Assets/Pistas/Modelos/{ids[i]}/{ids[i]}.tscn";
			if (!ResourceLoader.Exists(ruta)) continue;
			var modelo = GD.Load<PackedScene>(ruta).Instantiate<Node3D>();
			modelo.Name = ids[i];
			MesaEvidencias.AddChild(modelo);
			int fila = i / Columnas, col = i % Columnas;
			var caja = CajaDe(modelo);
			float mayor = Mathf.Max(caja.Size.X, Mathf.Max(caja.Size.Y, caja.Size.Z));
			float escala = mayor > 0.001f ? Mathf.Min(1f, (Celda * 0.85f) / mayor) : 1f;
			modelo.Scale = Vector3.One * escala;
			modelo.Position = new Vector3((col - (Columnas - 1) / 2f) * Celda, -caja.Position.Y * escala,
				(fila - 1) * Celda) - new Vector3(caja.GetCenter().X, 0, caja.GetCenter().Z) * escala;
		}
	}

	// Ilumina una prueba de la mesa (la levanta un poco) para señalarla durante un interrogatorio
	public void MostrarPrueba(string idPista)
	{
		var modelo = MesaEvidencias.GetNodeOrNull<Node3D>(idPista);
		if (modelo == null) return;
		var tween = CreateTween();
		tween.TweenProperty(modelo, "position:y", modelo.Position.Y + 0.08f, 0.25);
		tween.TweenProperty(modelo, "position:y", modelo.Position.Y, 0.4).SetDelay(1.2);
		EmitSignal(SignalName.PruebaMostrada, idPista);
	}

	private static Aabb CajaDe(Node3D raiz)
	{
		Aabb? caja = null;
		foreach (Node n in raiz.FindChildren("*", "VisualInstance3D", true, false))
		{
			var v = (VisualInstance3D)n;
			var local = v.GetAabb();
			if (local.Size == Vector3.Zero) continue;
			Transform3D t = v.Transform;
			for (Node p = v.GetParent(); p != null && p != raiz; p = p.GetParent())
				if (p is Node3D n3) t = n3.Transform * t;
			var a = t * local;
			caja = caja?.Merge(a) ?? a;
		}
		return caja ?? new Aabb(Vector3.Zero, Vector3.One * 0.1f);
	}

	// ---------- Corcho ----------

	// Fotos de los implicados alrededor de la de Porky, cada una unida a ella con estambre rojo
	private void ArmarCorcho()
	{
		var centro = new Vector3(0, 0.1f, 0);
		Foto("Porky", "porky", centro, 0.34f, true);
		var claves = new List<string>(Datos.Keys);
		for (int i = 0; i < claves.Count; i++)
		{
			float ang = Mathf.Pi + i * Mathf.Tau / claves.Count + 0.25f;
			var pos = new Vector3(Mathf.Cos(ang) * 0.95f, Mathf.Sin(ang) * 0.42f, 0);
			var d = Datos[claves[i]];
			Foto(d.Nombre, d.Retrato, pos, 0.26f, false);
			Estambre(centro, pos);
		}
	}

	private void Foto(string nombre, string retrato, Vector3 pos, float tam, bool victima)
	{
		var foto = new Node3D { Name = "Foto" + nombre.Replace(" ", ""), Position = pos };
		Corcho.AddChild(foto);
		foto.RotationDegrees = new Vector3(0, 0, (float)GD.RandRange(-6.0, 6.0));
		var papel = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(tam, tam * 1.2f) }, Position = new Vector3(0, 0, 0.001f) };
		papel.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.93f, 0.91f, 0.86f), Roughness = 0.9f };
		foto.AddChild(papel);
		string ruta = $"res://Assets/Retratos/{retrato}.png";
		var imagen = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(tam * 0.86f, tam * 0.86f) }, Position = new Vector3(0, tam * 0.1f, 0.002f) };
		var material = new StandardMaterial3D { Roughness = 0.6f };
		if (ResourceLoader.Exists(ruta)) material.AlbedoTexture = GD.Load<Texture2D>(ruta);
		else material.AlbedoColor = new Color(0.16f, 0.15f, 0.14f); // Todavía sin retrato: solo el nombre
		imagen.MaterialOverride = material;
		foto.AddChild(imagen);
		var etiqueta = new Label3D
		{
			Text = victima ? nombre.ToUpper() + " †" : nombre,
			PixelSize = 0.0011f,
			FontSize = 40,
			OutlineSize = 0,
			Modulate = new Color(0.1f, 0.08f, 0.07f),
			Position = new Vector3(0, -tam * 0.49f, 0.003f),
		};
		foto.AddChild(etiqueta);
		var chincheta = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.012f, Height = 0.024f }, Position = new Vector3(0, tam * 0.55f, 0.006f) };
		chincheta.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.75f, 0.08f, 0.06f), Roughness = 0.4f };
		foto.AddChild(chincheta);
	}

	private void Estambre(Vector3 a, Vector3 b)
	{
		var largo = a.DistanceTo(b);
		var hilo = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(largo, 0.006f, 0.003f) },
			Position = (a + b) / 2 + new Vector3(0, 0, 0.008f),
			RotationDegrees = new Vector3(0, 0, Mathf.RadToDeg(Mathf.Atan2(b.Y - a.Y, b.X - a.X))),
		};
		hilo.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.7f, 0.05f, 0.05f), Roughness = 0.8f };
		Corcho.AddChild(hilo);
	}

	// ---------- Salida ----------

	private async Task AlSalir()
	{
		int opcion = await Dialogo.Elegir("Caneloso", "¿A dónde voy?", "Volver al lago", "Volver al campamento", "Quedarme aquí");
		if (opcion == 0) IrA(PartidaEscenas.Lago, "EntradaDesdeSala");
		else if (opcion == 1) IrA(PartidaEscenas.Campamento, "LlegadaDesdeSala");
	}
}
