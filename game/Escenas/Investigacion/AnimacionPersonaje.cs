using Godot;
using System.Collections.Generic;

// Animación de reposo hecha por código para los personajes que vienen en pose T y sin animaciones.
// Con esqueleto: baja los brazos, respira con la espalda, mueve orejas y cola, y voltea la cabeza hacia el detective.
// Sin esqueleto: el modelo entero se mece y respira.
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

	private Node3D _personaje;
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
		_respiraModelo = _personaje is not Npc;
		_baseModelo = Modelo.Transform;
		_esqueleto = BuscarEsqueleto(Modelo);
		if (_esqueleto != null) PrepararEsqueleto();
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

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_tiempo += dt;
		_fase += dt * VelocidadRespiracion * (Somnoliento && !JugadorCerca(out _) ? 0.6f : 1f);
		if (_esqueleto != null) AnimarEsqueleto(dt);
		else AnimarModelo();
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

		// Espalda: respira inclinándose apenas y se mece de lado muy lento
		Agregar(_espalda, new Quaternion(lado, Mathf.DegToRad(1.2f * respira)) * new Quaternion(frente, Mathf.DegToRad(1.5f * Mathf.Sin(_tiempo * 0.45f + _fase))));
		Agregar(_pecho, new Quaternion(lado, Mathf.DegToRad(0.8f * respira)));

		// Brazos abajo, con un vaivén que sigue la respiración
		float vaiven = Mathf.DegToRad(3f * Mathf.Sin(_fase * 0.5f));
		Agregar(_brazoI, new Quaternion(lado, vaiven) * _bajarBrazoI);
		Agregar(_brazoD, new Quaternion(lado, -vaiven * 0.7f) * _bajarBrazoD);
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
			yawObjetivo = Mathf.DegToRad(18f * Mathf.Sin(_tiempo * 0.23f + _fase) + 6f * Mathf.Sin(_tiempo * 0.61f));
			pitchObjetivo = Mathf.DegToRad(3f * Mathf.Sin(_tiempo * 0.37f));
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
		var giro = new Basis(Vector3.Up, Mathf.DegToRad(5f * Mathf.Sin(_tiempo * 0.3f + _fase)))
			* new Basis(Vector3.Back, Mathf.DegToRad(1.8f * Mathf.Sin(_tiempo * 0.7f + _fase)));
		Transform3D t = new Transform3D(giro, Vector3.Zero) * _baseModelo;
		if (_respiraModelo)
			t.Basis = t.Basis * Basis.FromScale(new Vector3(1f - respira * 0.006f, 1f + respira * 0.012f, 1f));
		Modelo.Transform = t;
	}
}
