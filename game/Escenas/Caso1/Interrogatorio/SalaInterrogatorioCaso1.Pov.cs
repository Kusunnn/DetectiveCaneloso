using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// La puesta en escena del interrogatorio: toma general en tercera persona (como Detroit: Become Human), transición a la silla
// del detective y plano medio de frente (como L.A. Noire y Ace Attorney), lectura de la cara y salida en tercera persona.
// Los diálogos siguen siendo los de GuionCaso1: aquí solo se mueve la cámara y se reacciona a lo que pasa.
public partial class SalaInterrogatorioCaso1
{
	private const double SegundosTransicion = 1.2;
	private const string IdEntrada = "caso1_entrada_";
	private const string GolpeSonido = "res://Assets/Audio/Cinematica/Caso1/golpe_sordo.wav";
	private static readonly Vector3 CamaraGeneralPos = new(-2.9f, 2.65f, 1.5f), CamaraGeneralMira = new(0.35f, 1.0f, -2.2f);
	private static readonly Vector3 HecdogPov = new(1.5f, 0.5f, -2.7f);
	private const float CamaraPovZ = -0.15f, FovPov = 46f;

	private Camera3D _camaraSala;
	private CameraAttributesPractical _atributosPov;
	private SpotLight3D _luzPov;
	private Node3D _pruebasMesa;
	private ColorRect _negroPov;
	private bool _enPov, _saltable;
	private double _tPov;
	private float _empuje, _temblor, _desvioObjetivo, _desvioActual;
	private double _proximoDesvio, _finDesvio;
	private Vector3 _ojoPov, _miraPov, _hecdogPuerta;
	private float _hecdogGiroPuerta;
	private Tween _tweenEmpuje;

	private void PrepararPuestaEnEscena()
	{
		_atributosPov = new CameraAttributesPractical
		{
			DofBlurFarEnabled = true, DofBlurFarDistance = 3.0f, DofBlurFarTransition = 1.4f,
			DofBlurNearEnabled = true, DofBlurNearDistance = 0.6f, DofBlurNearTransition = 0.4f, DofBlurAmount = 0.06f, ExposureMultiplier = 0.72f,
		};
		_camaraSala = new Camera3D { Name = "CamaraInterrogatorioSala", Fov = 52f };
		AddChild(_camaraSala);
		_luzPov = new SpotLight3D { LightColor = new Color(1f, 0.87f, 0.68f), LightEnergy = 0.7f, SpotRange = 4.5f, SpotAngle = 34f, Visible = false };
		AddChild(_luzPov);
		var capa = new CanvasLayer { Layer = 19 };
		AddChild(capa);
		_negroPov = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
		_negroPov.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		capa.AddChild(_negroPov);
		var hecdog = Hecdog.GetParent<Node3D>();
		_hecdogPuerta = hecdog.GlobalPosition;
		_hecdogGiroPuerta = hecdog.GlobalRotation.Y;
		SetProcess(true);
		Guion.AntesDeInterrogar = EntrarAlInterrogatorio;
		Guion.DespuesDeInterrogar = SalirDelInterrogatorio;
		Guion.AnimoDe = VerAnimo;
		Guion.AlAcertar = Acierto;
		Guion.AlTumbarExcusa = ExcusaTumbada;
		Guion.Autoguardado = motivo => Autoguardar(motivo);
	}

	// ---------- Entrada y salida ----------

	private async Task EntrarAlInterrogatorio(string quien)
	{
		if (!ClaveDeGuion.TryGetValue(quien, out string clave)) return;
		if (_enPov && ImplicadoActual == clave) return;
		string id = IdEntrada + quien;
		bool corta = CinematicaCaso1.Vista(id);
		if (!corta) GestorPartida.Instancia?.RegistrarCinematica(id);
		Dialogo.Visible = false;
		Jugador.Visible = false;
		Jugador.Bloquear(true);
		_camaraSala.Fov = 52f;
		_camaraSala.Attributes = null;
		_camaraSala.LookAtFromPosition(CamaraGeneralPos, CamaraGeneralMira);
		_camaraSala.MakeCurrent();
		await Fundir(1f, 0f, 0.3);
		_saltable = true;
		await TraerImplicado(clave, corta);
		_saltable = false;
		Engine.TimeScale = 1;
		if (!IsInsideTree()) return;
		await TransicionAPov(clave);
		Dialogo.Compacto(true);
		Dialogo.Visible = true;
	}

	private async Task SalirDelInterrogatorio(string quien)
	{
		_enPov = false;
		Dialogo.Compacto(false);
		Dialogo.Visible = false;
		await Fundir(0f, 1f, 0.15);
		_luzPov.Visible = false;
		QuitarPruebasDeMesa();
		var hecdog = Hecdog.GetParent<Node3D>();
		hecdog.GlobalPosition = _hecdogPuerta;
		hecdog.GlobalRotation = new Vector3(0, _hecdogGiroPuerta, 0);
		_camaraSala.Fov = 52f;
		_camaraSala.Attributes = null;
		_camaraSala.LookAtFromPosition(CamaraGeneralPos, CamaraGeneralMira);
		await Fundir(1f, 0f, 0.15);
		_saltable = true;
		await RetirarImplicado();
		_saltable = false;
		Engine.TimeScale = 1;
		if (!IsInsideTree()) return;
		await Fundir(0f, 1f, 0.2);
		Jugador.Visible = true;
		Jugador.CamaraActiva?.MakeCurrent();
		await Fundir(1f, 0f, 0.2);
		Dialogo.Visible = true;
	}

	private async Task Fundir(float desde, float hasta, double segundos)
	{
		_negroPov.Color = new Color(0, 0, 0, desde);
		var tween = CreateTween();
		tween.TweenProperty(_negroPov, "color:a", hasta, segundos);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	// ---------- Transición y plano del interrogatorio ----------

	private async Task TransicionAPov(string clave)
	{
		var implicado = Implicados.GetNode<Node3D>(clave);
		float cabeza = AlturaCabeza(implicado);
		// A la altura de los ojos, cabeza y torso a cuadro, y un poco arriba del centro: abajo va la caja de diálogo
		_miraPov = new Vector3(0, cabeza - 0.32f, -2.85f);
		_ojoPov = new Vector3(0, cabeza - 0.05f, CamaraPovZ);
		// Hecdog pasa al lado de la mesa en un parpadeo
		var desdePos = _camaraSala.GlobalPosition;
		var desdeBase = _camaraSala.GlobalBasis.GetRotationQuaternion();
		await Fundir(0f, 1f, 0.12);
		var hecdog = Hecdog.GetParent<Node3D>();
		hecdog.GlobalPosition = HecdogPov;
		hecdog.GlobalRotation = new Vector3(0, Mathf.Atan2(-HecdogPov.X, -2.85f - HecdogPov.Z), 0);
		PonerPruebasEnMesa();
		_luzPov.Visible = true;
		_luzPov.LookAtFromPosition(new Vector3(0.25f, 2.4f, -1.4f), new Vector3(0, cabeza - 0.25f, -2.85f));
		var fundido = Fundir(1f, 0f, 0.12);
		var hastaBase = Basis.LookingAt(_miraPov - _ojoPov, Vector3.Up).GetRotationQuaternion();
		double t = 0;
		while (t < SegundosTransicion && IsInsideTree())
		{
			t += GetProcessDeltaTime();
			float u = (float)Mathf.Clamp(t / SegundosTransicion, 0, 1);
			u = u * u * (3 - 2 * u);
			_camaraSala.GlobalPosition = desdePos.Lerp(_ojoPov, u);
			_camaraSala.GlobalBasis = new Basis(desdeBase.Slerp(hastaBase, u));
			_camaraSala.Fov = Mathf.Lerp(52f, FovPov, u);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		await fundido;
		_camaraSala.Attributes = _atributosPov;
		_atributosPov.DofBlurFarDistance = _ojoPov.DistanceTo(_miraPov) + 0.7f;
		_enPov = true;
		_tPov = 0;
		_proximoDesvio = 2.5;
	}

	private float AlturaCabeza(Node3D implicado)
	{
		foreach (Node n in implicado.FindChildren("*", "Skeleton3D", true, false))
		{
			if (n is not Skeleton3D s || s.GetBoneCount() < 2) continue;
			int i = s.FindBone("mixamorig_Head");
			if (i < 0) i = s.FindBone("Head");
			if (i >= 0) return (s.GlobalTransform * s.GetBoneGlobalPose(i).Origin).Y + 0.3f;
		}
		return 1.7f;
	}

	public override void _Process(double delta)
	{
		if (_saltable && Input.IsKeyPressed(Key.Space) | Input.IsKeyPressed(Key.Escape) | Input.IsMouseButtonPressed(MouseButton.Left))
			Engine.TimeScale = 12;
		if (!_enPov) return;
		_tPov += delta;
		// Respiración sutil, y el golpe de cuando algo cae
		float t = (float)_tPov;
		var respira = new Vector3(Mathf.Sin(t * 0.7f) * 0.006f, Mathf.Sin(t * 1.1f) * 0.009f, 0);
		var golpe = _temblor > 0.001f ? new Vector3((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0), 0) * _temblor * 0.02f : Vector3.Zero;
		_temblor = Mathf.MoveToward(_temblor, 0, (float)delta * 2.6f);
		var ojo = _ojoPov.Lerp(_miraPov, 0.22f * _empuje) + respira + golpe;
		_camaraSala.LookAtFromPosition(ojo, _miraPov + respira * 0.3f);
		_camaraSala.Fov = FovPov - 4f * _empuje;
		ActualizarMirada(delta);
	}

	// Cuando miente, a ratos no le sostiene la mirada
	private void ActualizarMirada(double delta)
	{
		if (ImplicadoActual == null || !_poses.TryGetValue(ImplicadoActual, out var pose)) return;
		var anim = Implicados.GetNode<Node3D>(ImplicadoActual).GetNodeOrNull<AnimacionPersonaje>("Animacion");
		bool nervioso = anim != null && anim.Actual != AnimacionPersonaje.Animo.Sereno;
		if (nervioso && _tPov >= _proximoDesvio)
		{
			_desvioObjetivo = (GD.Randf() < 0.5f ? -1f : 1f) * (float)GD.RandRange(14.0, 24.0);
			_finDesvio = _tPov + GD.RandRange(0.7, 1.3);
			_proximoDesvio = _finDesvio + GD.RandRange(1.8, 4.0);
		}
		if (!nervioso || _tPov >= _finDesvio) _desvioObjetivo = 0;
		_desvioActual = Mathf.MoveToward(_desvioActual, _desvioObjetivo, (float)delta * 70f);
		pose.Desvio = _desvioActual;
	}

	// ---------- Lo que se le ve en la cara ----------

	private void VerAnimo(string quien, string animo)
	{
		if (ImplicadoActual == null || NombreDe(ImplicadoActual) != quien) return;
		var anim = Implicados.GetNode<Node3D>(ImplicadoActual).GetNodeOrNull<AnimacionPersonaje>("Animacion");
		anim?.FijarAnimo(LecturaDe(animo));
	}

	// Las mismas familias de palabras que colorean el ánimo en la caja de diálogo
	public static AnimacionPersonaje.Animo LecturaDe(string animo)
	{
		string a = (animo ?? "").ToLower();
		bool Es(params string[] raices) => raices.Any(r => a.StartsWith(r));
		if (Es("molest", "enojad", "alterad", "furios", "cortante", "impacient", "harto", "harta", "a la defensiva", "desafiante", "acorralad")) return AnimacionPersonaje.Animo.Alterado;
		if (Es("nervios", "asustad", "ansios", "preocupad", "incómod", "sarcástic", "frí", "trist", "quebrad", "apenad", "dolid", "arrepentid", "culpable", "derrotad", "sombrí")) return AnimacionPersonaje.Animo.Tenso;
		return AnimacionPersonaje.Animo.Sereno;
	}

	private void Golpe(float fuerza)
	{
		if (ResourceLoader.Exists(GolpeSonido))
		{
			var sonido = new AudioStreamPlayer { Stream = GD.Load<AudioStream>(GolpeSonido), VolumeDb = -3f, Bus = "Master" };
			AddChild(sonido);
			sonido.Finished += sonido.QueueFree;
			sonido.Play();
		}
		_temblor = fuerza;
		_tweenEmpuje?.Kill();
		_tweenEmpuje = CreateTween();
		_tweenEmpuje.TweenMethod(Callable.From<float>(v => _empuje = v), _empuje, 1f, 0.16).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		_tweenEmpuje.TweenMethod(Callable.From<float>(v => _empuje = v), 1f, 0.35f, 0.9).SetDelay(0.5);
	}

	// La prueba correcta: golpe, push-in rápido y se le altera la cara
	private async Task Acierto()
	{
		if (ImplicadoActual == null || !_enPov) return;
		Golpe(0.5f);
		Implicados.GetNode<Node3D>(ImplicadoActual).GetNodeOrNull<AnimacionPersonaje>("Animacion")?.FijarAnimo(AnimacionPersonaje.Animo.Alterado);
		await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
	}

	// Careo: Tony pierde la compostura con cada excusa: se tensa, se altera, se levanta y golpea la mesa
	private async Task ExcusaTumbada(int tumbadas)
	{
		if (ImplicadoActual == null) return;
		var tony = Implicados.GetNode<Node3D>(ImplicadoActual);
		var anim = tony.GetNodeOrNull<AnimacionPersonaje>("Animacion");
		anim?.FijarAnimo(tumbadas >= 2 ? AnimacionPersonaje.Animo.Alterado : AnimacionPersonaje.Animo.Tenso);
		if (tumbadas == 3) await Sentarse(tony, ImplicadoActual, false); // Se levanta
		if (tumbadas >= 4) Golpe(1f);                                     // Golpea la mesa
		if (tumbadas == 5) await Sentarse(tony, ImplicadoActual, true);   // Se deja caer
	}

	// ---------- Las pruebas sobre la mesa ----------

	private void PonerPruebasEnMesa()
	{
		QuitarPruebasDeMesa();
		_pruebasMesa = new Node3D { Name = "PruebasEnMesa", Position = new Vector3(0, 0.5f + 0.785f, -1.72f) };
		GetParent().AddChild(_pruebasMesa);
		var ids = new List<string>();
		foreach (Variant pista in Hud.Get("pistas").AsGodotArray())
		{
			var r = pista.As<Resource>();
			if (r.Get("origen_id").AsStringName().ToString() != "") continue;
			string id = r.Get("id").AsStringName().ToString();
			if (ResourceLoader.Exists($"res://Assets/Pistas/Modelos/{id}/{id}.tscn")) ids.Add(id);
		}
		// Las últimas que encontró, en fila del lado del detective
		var ultimas = ids.Skip(System.Math.Max(0, ids.Count - 6)).ToList();
		const float Celda = 0.24f;
		for (int i = 0; i < ultimas.Count; i++)
		{
			var modelo = GD.Load<PackedScene>($"res://Assets/Pistas/Modelos/{ultimas[i]}/{ultimas[i]}.tscn").Instantiate<Node3D>();
			_pruebasMesa.AddChild(modelo);
			var caja = CajaDe(modelo);
			float mayor = Mathf.Max(caja.Size.X, Mathf.Max(caja.Size.Y, caja.Size.Z));
			float escala = mayor > 0.001f ? Mathf.Min(1f, Celda * 0.8f / mayor) : 1f;
			modelo.Scale = Vector3.One * escala;
			modelo.Position = new Vector3((i - (ultimas.Count - 1) / 2f) * Celda, -caja.Position.Y * escala, 0) - new Vector3(caja.GetCenter().X, 0, caja.GetCenter().Z) * escala;
		}
	}

	private void QuitarPruebasDeMesa()
	{
		if (_pruebasMesa != null && IsInstanceValid(_pruebasMesa)) _pruebasMesa.QueueFree();
		_pruebasMesa = null;
	}

	// ---------- Guardado ----------

	public override bool PuedeGuardar => base.PuedeGuardar || (Guion != null && Guion.EnEspera && !Tablero.Abierto && !Jugador.Examinando);

	public override void _ExitTree()
	{
		Engine.TimeScale = 1;
		base._ExitTree();
	}
}
