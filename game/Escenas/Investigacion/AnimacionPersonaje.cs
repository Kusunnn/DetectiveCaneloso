using Godot;
using System.Collections.Generic;

// Animación de reposo hecha por código para los personajes que vienen en pose T y sin animaciones.
// Con esqueleto: baja los brazos, respira con la espalda, mueve orejas y cola, y voltea la cabeza hacia el detective.
// Sin esqueleto: el modelo entero se mece y respira.
// Si el modelo ya trae animaciones hechas (Mixamo), basta con poner su nombre en Animacion: se repite en bucle
// y no se hace nada por código.
// Va como hijo del personaje (el nodo que mira hacia +Z), junto a su Modelo. No toca la escala del modelo
// cuando el padre es un Npc, porque el Npc ya lo hace respirar.
public partial class AnimacionPersonaje : Node
{
	[Export] public Node3D Modelo;
	// Ángulo de los brazos bajo la horizontal (90 = pegados al cuerpo)
	[Export] public float AnguloBrazos = 68f;
	// Para un brazo que sostiene algo (un trapeador, un letrero): -1 usa AnguloBrazos
	[Export] public float AnguloBrazoIzquierdo = -1f;
	[Export] public float AnguloBrazoDerecho = -1f;
	[Export] public float VelocidadRespiracion = 2.0f;
	// Voltea a ver al detective cuando lo tiene a menos de esta distancia
	[Export] public float DistanciaMirar = 5f;
	// Cabecea con la cabeza abajo mientras el detective está lejos
	[Export] public bool Somnoliento;
	// Altura de los ojos sobre los pies, para calcular hacia dónde inclina la cabeza
	[Export] public float AlturaOjos = 1.3f;
	// Cuánto voltea la cabeza como máximo
	[Export] public float GiroMaximo = 45f;
	// Un objeto que lleva en la mano (se busca la mano más cercana al empezar)
	[Export] public Node3D ObjetoEnMano;
	// Animación del modelo que se repite (p. ej. "global/Inactivo"); vacío = animación por código
	[Export] public string Animacion = "";
	// HU-9.5 CA-03: otras animaciones del modelo que se intercalan de vez en cuando con la de reposo (una vuelta
	// cada una), para que no repita el mismo ciclo para siempre. Cada cuánto, en segundos.
	[Export] public string[] Variantes = System.Array.Empty<string>();
	[Export] public Vector2 IntervaloVariante = new(12f, 26f);

	// Mientras camina (lo pone quien lo mueve): con animaciones del modelo usa la de caminar; por código mueve
	// piernas y brazos y sube y baja el cuerpo, así los pies no se deslizan quietos por el piso.
	public bool Caminando { get; private set; }

	private Node3D _personaje;
	private AnimationPlayer _reproductor;
	private float _velocidad = 1f;
	private double _siguienteVariante;
	private double _finVariante = -1;
	// Actividad de reposo por código: 0 quieto, 1 cambia el peso de pie, 2 mira hacia un lado, 3 se queda quieto
	private int _actividad, _ultimaActividad = 1;
	private float _finActividad, _pesoActividad, _ladoActividad = 1f;
	private float _paso, _pesoPaso;
	private bool _modeloMovido;
	private int _piernaI = -1, _piernaD = -1;
	private Skeleton3D _esqueleto;
	private Node3D _jugador;
	private float _tiempo;
	private float _fase;
	private bool _respiraModelo;

	// Esqueleto
	private Quaternion[] _localReposo;
	private Quaternion[] _global;
	private readonly Dictionary<int, Quaternion> _giros = new();
	private int _brazoI = -1, _brazoD = -1, _codoI = -1, _codoD = -1;
	private int _espalda = -1, _pecho = -1, _cabeza = -1, _cuello = -1;
	private int _orejaI = -1, _orejaD = -1, _cola = -1, _mano = -1;
	private Quaternion _bajarBrazoI = Quaternion.Identity, _bajarBrazoD = Quaternion.Identity;
	private Transform3D _manoAObjeto;
	private float _yawCabeza, _pitchCabeza;

	// Sin esqueleto
	private Transform3D _baseModelo;

	public override void _Ready()
	{
		_personaje = GetParent<Node3D>();
		Modelo ??= _personaje.GetNodeOrNull<Node3D>("Modelo");
		if (Modelo == null) { SetProcess(false); return; }
		_fase = (float)GD.RandRange(0.0, Mathf.Tau);
		// Cada copia a su ritmo: nadie se mueve al mismo cuadro ni a la misma velocidad
		_velocidad = (float)GD.RandRange(0.9, 1.1);
		_finActividad = (float)GD.RandRange(4.0, 12.0);
		if (Animacion != "" && Reproducir(Animacion))
		{
			_siguienteVariante = GD.RandRange(IntervaloVariante.X, IntervaloVariante.Y);
			// Solo hace falta seguir procesando si tiene variantes que intercalar
			SetProcess(Variantes.Length > 0);
			return;
		}
		_respiraModelo = _personaje is not Npc;
		_baseModelo = Modelo.Transform;
		_esqueleto = BuscarEsqueleto(Modelo);
		if (_esqueleto != null) PrepararEsqueleto();
	}

	// Repite una animación del modelo desde un punto al azar, para que dos copias no se muevan igual
	public bool Reproducir(string nombre)
	{
		foreach (Node n in Modelo.FindChildren("*", "AnimationPlayer", true, false))
		{
			if (n is not AnimationPlayer reproductor || !reproductor.HasAnimation(nombre)) continue;
			Animation animacion = reproductor.GetAnimation(nombre);
			animacion.LoopMode = Animation.LoopModeEnum.Linear;
			reproductor.Play(nombre);
			reproductor.Seek(GD.RandRange(0.0, animacion.Length), true);
			reproductor.SpeedScale = _velocidad;
			_reproductor = reproductor;
			_finVariante = -1;
			return true;
		}
		GD.PushWarning($"AnimacionPersonaje: {_personaje.Name} no tiene la animación «{nombre}».");
		return false;
	}

	// Algunos modelos traen un esqueleto vacío además del bueno: se queda el de más huesos
	private static Skeleton3D BuscarEsqueleto(Node raiz)
	{
		Skeleton3D mejor = null;
		foreach (Node n in raiz.FindChildren("*", "Skeleton3D", true, false))
			if (n is Skeleton3D s && s.GetBoneCount() > (mejor?.GetBoneCount() ?? 1)) mejor = s;
		return mejor;
	}

	private void PrepararEsqueleto()
	{
		int total = _esqueleto.GetBoneCount();
		_localReposo = new Quaternion[total];
		_global = new Quaternion[total];
		for (int i = 0; i < total; i++)
			_localReposo[i] = _esqueleto.GetBonePoseRotation(i); // la pose con la que viene el modelo

		_brazoI = _esqueleto.FindBone("Arm_1_L"); _brazoD = _esqueleto.FindBone("Arm_1_R");
		_codoI = _esqueleto.FindBone("Arm_2_L"); _codoD = _esqueleto.FindBone("Arm_2_R");
		_espalda = _esqueleto.FindBone("Spine_2"); _pecho = _esqueleto.FindBone("Spine_3");
		_cuello = _esqueleto.FindBone("Neck"); _cabeza = _esqueleto.FindBone("Head");
		_orejaI = _esqueleto.FindBone("Ear_1_L"); _orejaD = _esqueleto.FindBone("Ear_1_R");
		_piernaI = _esqueleto.FindBone("Leg_1_L"); _piernaD = _esqueleto.FindBone("Leg_1_R");
		_cola = _esqueleto.FindBone("Tail_1");
		if (_cola < 0) _cola = _esqueleto.FindBone("S_Tail_1");

		// Giro (en el espacio del esqueleto) que lleva cada brazo de la horizontal a su ángulo hacia abajo
		Vector3 abajo = EjeEsqueleto(Vector3.Down);
		_bajarBrazoI = GiroBrazo(_brazoI, _codoI, abajo, AnguloBrazoIzquierdo >= 0 ? AnguloBrazoIzquierdo : AnguloBrazos);
		_bajarBrazoD = GiroBrazo(_brazoD, _codoD, abajo, AnguloBrazoDerecho >= 0 ? AnguloBrazoDerecho : AnguloBrazos);

		if (ObjetoEnMano != null)
		{
			// Se pega a la mano que tenía más cerca en la pose T
			int manoI = _esqueleto.FindBone("Hand_L"), manoD = _esqueleto.FindBone("Hand_R");
			float dI = manoI < 0 ? float.MaxValue : PosicionHueso(manoI).DistanceTo(ObjetoEnMano.GlobalPosition);
			float dD = manoD < 0 ? float.MaxValue : PosicionHueso(manoD).DistanceTo(ObjetoEnMano.GlobalPosition);
			_mano = dI <= dD ? manoI : manoD;
			if (_mano >= 0) _manoAObjeto = GlobalHueso(_mano).AffineInverse() * ObjetoEnMano.GlobalTransform;
		}
	}

	private Quaternion GiroBrazo(int brazo, int codo, Vector3 abajo, float angulo)
	{
		if (brazo < 0 || codo < 0) return Quaternion.Identity;
		Vector3 dir = (_esqueleto.GetBoneGlobalRest(codo).Origin - _esqueleto.GetBoneGlobalRest(brazo).Origin).Normalized();
		Vector3 eje = dir.Cross(abajo);
		if (eje.LengthSquared() < 1e-6f) return Quaternion.Identity;
		float actual = Mathf.RadToDeg(dir.AngleTo(abajo)); // 90 si está horizontal
		float girar = Mathf.Max(0f, actual - (90f - angulo));
		return new Quaternion(eje.Normalized(), Mathf.DegToRad(girar));
	}

	// Un eje del personaje (+Z al frente, +Y arriba) en el espacio del esqueleto
	private Vector3 EjeEsqueleto(Vector3 ejePersonaje)
	{
		Basis b = _esqueleto.GlobalBasis.Orthonormalized().Inverse() * _personaje.GlobalBasis.Orthonormalized();
		return (b * ejePersonaje).Normalized();
	}

	private Transform3D GlobalHueso(int hueso) => _esqueleto.GlobalTransform * _esqueleto.GetBoneGlobalPose(hueso);
	private Vector3 PosicionHueso(int hueso) => GlobalHueso(hueso).Origin;

	// Empieza o deja de caminar. animacionCaminar: la del modelo, si la tiene (vacío = paso por código).
	public void Caminar(bool caminando, string animacionCaminar = "")
	{
		Caminando = caminando;
		if (_reproductor != null && Animacion != "")
		{
			if (caminando && animacionCaminar != "") Reproducir(animacionCaminar);
			else if (!caminando) Reproducir(Animacion);
			return;
		}
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_tiempo += dt;
		if (_reproductor != null && Animacion != "") { IntercalarVariantes(); return; }
		_fase += dt * VelocidadRespiracion * _velocidad * (Somnoliento && !JugadorCerca(out _) ? 0.6f : 1f);
		_pesoPaso = Mathf.MoveToward(_pesoPaso, Caminando ? 1f : 0f, dt * 5f);
		if (_pesoPaso > 0f) _paso += dt * 7.5f;
		ElegirActividad(dt);
		if (_esqueleto != null) AnimarEsqueleto(dt);
		else AnimarModelo();
	}

	// Animaciones del modelo: de vez en cuando una vuelta de una variante y de regreso al reposo, con mezcla
	private void IntercalarVariantes()
	{
		if (Caminando || Variantes.Length == 0) return;
		if (_finVariante >= 0)
		{
			if (_tiempo < _finVariante) return;
			_finVariante = -1;
			_reproductor.Play(Animacion, 0.5);
			_siguienteVariante = _tiempo + GD.RandRange(IntervaloVariante.X, IntervaloVariante.Y);
			return;
		}
		if (_tiempo < _siguienteVariante) return;
		string variante = Variantes[GD.RandRange(0, Variantes.Length - 1)];
		if (!_reproductor.HasAnimation(variante)) { _siguienteVariante = _tiempo + IntervaloVariante.Y; return; }
		_reproductor.Play(variante, 0.5);
		_finVariante = _tiempo + _reproductor.GetAnimation(variante).Length / Mathf.Max(0.1f, _velocidad);
	}

	// Reposo por código: cada tantos segundos cambia de actividad (cambiar el peso, mirar a un lado, quedarse
	// quieto) con una mezcla suave, así nunca repite el mismo ciclo. Con el detective cerca, solo lo mira.
	private void ElegirActividad(float dt)
	{
		_finActividad -= dt;
		if (_finActividad <= 0f)
		{
			_actividad = _actividad != 0 ? 0 : GD.RandRange(1, 3);
			if (_actividad != 0)
			{
				_ultimaActividad = _actividad;
				_ladoActividad = GD.Randf() < 0.5f ? -1f : 1f;
			}
			_finActividad = _actividad == 0 ? (float)GD.RandRange(5.0, 12.0) : (float)GD.RandRange(2.5, 5.0);
		}
		_pesoActividad = Mathf.MoveToward(_pesoActividad, _actividad != 0 && !Caminando ? 1f : 0f, dt * 0.8f);
	}

	private bool JugadorCerca(out Vector3 punto)
	{
		_jugador ??= GetTree().GetFirstNodeInGroup("jugador") as Node3D;
		punto = _jugador != null ? _jugador.GlobalPosition + Vector3.Up * 1.6f : Vector3.Zero;
		return _jugador != null && _jugador.GlobalPosition.DistanceTo(_personaje.GlobalPosition) < DistanciaMirar;
	}

	private void AnimarEsqueleto(float dt)
	{
		Vector3 arriba = EjeEsqueleto(Vector3.Up);
		Vector3 frente = EjeEsqueleto(Vector3.Back); // +Z
		Vector3 lado = EjeEsqueleto(Vector3.Right);
		float respira = Mathf.Sin(_fase);
		_giros.Clear();
		// Actividad de reposo (ver ElegirActividad): 1 = carga el peso en una pierna, 2 = mira hacia un lado
		float peso = _ultimaActividad == 1 ? _pesoActividad * _ladoActividad : 0f;
		float mirarLado = _ultimaActividad == 2 ? _pesoActividad * _ladoActividad : 0f;
		float quieto = _ultimaActividad == 3 ? _pesoActividad : 0f;

		// Espalda: respira inclinándose apenas y se mece de lado muy lento (al cargar el peso, se ladea más)
		float meceLado = 1.5f * Mathf.Sin(_tiempo * 0.45f + _fase) * (1f - quieto * 0.7f) + 4f * peso;
		Agregar(_espalda, new Quaternion(lado, Mathf.DegToRad(1.2f * respira)) * new Quaternion(frente, Mathf.DegToRad(meceLado)));
		Agregar(_pecho, new Quaternion(lado, Mathf.DegToRad(0.8f * respira)));

		// Caminar por código: las piernas van y vienen y los brazos al revés (no hay animación de caminar)
		float zancada = Mathf.Sin(_paso) * _pesoPaso;
		Agregar(_piernaI, new Quaternion(lado, Mathf.DegToRad(22f * zancada)));
		Agregar(_piernaD, new Quaternion(lado, Mathf.DegToRad(-22f * zancada)));
		// Sube y baja con cada paso (solo mientras camina; al parar, el modelo vuelve a su lugar)
		if (_pesoPaso > 0f || _modeloMovido)
		{
			Modelo.Transform = new Transform3D(_baseModelo.Basis, _baseModelo.Origin + Vector3.Up * 0.035f * Mathf.Abs(Mathf.Cos(_paso)) * _pesoPaso);
			_modeloMovido = _pesoPaso > 0f;
		}

		// Brazos abajo, con un vaivén que sigue la respiración (o los pasos)
		float vaiven = Mathf.DegToRad(3f * Mathf.Sin(_fase * 0.5f) * (1f - quieto) - 16f * zancada);
		Agregar(_brazoI, new Quaternion(lado, vaiven) * _bajarBrazoI);
		Agregar(_brazoD, new Quaternion(lado, -vaiven * (_pesoPaso > 0f ? 1f : 0.7f)) * _bajarBrazoD);
		Agregar(_codoI, new Quaternion(lado, Mathf.DegToRad(-8f)));
		Agregar(_codoD, new Quaternion(lado, Mathf.DegToRad(-8f)));

		// Cabeza: voltea a ver al detective, o mira alrededor (o cabecea si tiene sueño)
		float yawObjetivo, pitchObjetivo;
		if (JugadorCerca(out Vector3 punto))
		{
			Vector3 local = _personaje.GlobalTransform.AffineInverse() * punto;
			Vector3 d = local - new Vector3(0, AlturaOjos, 0);
			yawObjetivo = Mathf.Clamp(Mathf.Atan2(d.X, d.Z), Mathf.DegToRad(-GiroMaximo), Mathf.DegToRad(GiroMaximo));
			pitchObjetivo = Mathf.Clamp(-Mathf.Atan2(d.Y, new Vector2(d.X, d.Z).Length()), Mathf.DegToRad(-15f), Mathf.DegToRad(15f));
		}
		else if (Somnoliento)
		{
			yawObjetivo = 0f;
			pitchObjetivo = Mathf.DegToRad(24f + 6f * Mathf.Sin(_tiempo * 0.8f));
		}
		else
		{
			// Mira alrededor despacio; a veces se queda mirando hacia un lado (actividad 2)
			yawObjetivo = Mathf.DegToRad(18f * Mathf.Sin(_tiempo * 0.23f + _fase) * (1f - Mathf.Abs(mirarLado)) + 6f * Mathf.Sin(_tiempo * 0.61f) + 38f * mirarLado);
			pitchObjetivo = Mathf.DegToRad(3f * Mathf.Sin(_tiempo * 0.37f) + 4f * quieto);
		}
		float suave = 1f - Mathf.Exp(-dt * 4f);
		_yawCabeza = Mathf.Lerp(_yawCabeza, yawObjetivo, suave);
		_pitchCabeza = Mathf.Lerp(_pitchCabeza, pitchObjetivo, suave);
		// Todo el giro va en el cuello: la cara, los ojos y los lentes cuelgan de huesos distintos y así no se separan
		Quaternion giroCabeza(float parte) => new Quaternion(arriba, _yawCabeza * parte) * new Quaternion(lado, _pitchCabeza * parte);
		if (_cuello >= 0) Agregar(_cuello, giroCabeza(1f));
		else Agregar(_cabeza, giroCabeza(1f));

		// Orejas: un tic de vez en cuando; cola: se mece
		float tic = Mathf.Max(0f, Mathf.Sin(_tiempo * 1.7f + _fase * 3f));
		tic = tic > 0.97f ? (tic - 0.97f) / 0.03f : 0f;
		Agregar(_orejaI, new Quaternion(frente, Mathf.DegToRad(10f * tic)));
		Agregar(_orejaD, new Quaternion(frente, Mathf.DegToRad(-10f * tic)));
		Agregar(_cola, new Quaternion(arriba, Mathf.DegToRad(14f * Mathf.Sin(_tiempo * 1.3f + _fase))));

		AplicarGiros();

		if (ObjetoEnMano != null && _mano >= 0)
			ObjetoEnMano.GlobalTransform = GlobalHueso(_mano) * _manoAObjeto;
	}

	private void Agregar(int hueso, Quaternion giro)
	{
		if (hueso >= 0) _giros[hueso] = giro;
	}

	// Recorre los huesos de padre a hijo: cada giro se da en el espacio del esqueleto y se pasa al local del hueso
	private void AplicarGiros()
	{
		for (int i = 0; i < _localReposo.Length; i++)
		{
			int padre = _esqueleto.GetBoneParent(i);
			Quaternion globalPadre = padre >= 0 ? _global[padre] : Quaternion.Identity;
			Quaternion reposo = globalPadre * _localReposo[i];
			if (_giros.TryGetValue(i, out Quaternion giro))
			{
				_global[i] = (giro * reposo).Normalized();
				_esqueleto.SetBonePoseRotation(i, (globalPadre.Inverse() * _global[i]).Normalized());
			}
			else _global[i] = reposo; // Los que no se animan no se tocan: heredan el giro de su padre
		}
	}

	private void AnimarModelo()
	{
		float respira = Mathf.Sin(_fase);
		float peso = _ultimaActividad == 1 ? _pesoActividad * _ladoActividad : 0f;
		float mirarLado = _ultimaActividad == 2 ? _pesoActividad * _ladoActividad : 0f;
		// Al caminar se balancea de lado a lado con cada paso y sube y baja
		float paso = Mathf.Sin(_paso) * _pesoPaso;
		var giro = new Basis(Vector3.Up, Mathf.DegToRad(5f * Mathf.Sin(_tiempo * 0.3f + _fase) + 20f * mirarLado))
			* new Basis(Vector3.Back, Mathf.DegToRad(1.8f * Mathf.Sin(_tiempo * 0.7f + _fase) + 3f * peso + 5f * paso));
		Transform3D t = new Transform3D(giro, Vector3.Up * 0.04f * Mathf.Abs(Mathf.Cos(_paso)) * _pesoPaso) * _baseModelo;
		if (_respiraModelo)
			t.Basis = t.Basis * Basis.FromScale(new Vector3(1f - respira * 0.006f, 1f + respira * 0.012f, 1f));
		Modelo.Transform = t;
	}
}
