using Godot;
using System;
using System.Collections.Generic;

// Anima a un personaje con las animaciones de Quaternius (Universal Animation Library 1 y 2, CC0).
// Las animaciones vienen hechas para su maniquí; aquí se pasan (retarget) al esqueleto del personaje
// en cada cuadro: Animal Crossing (Porky, Eriz), Mixamo (el comisario) o cualquiera con huesos parecidos.
// Cómo: un maniquí invisible reproduce la animación y, hueso por hueso, se copia cuánto giró cada uno
// respecto a su pose T. Los dos esqueletos parten de la pose T, así que el giro vale para los dos.
//
// Encima de la animación: voltea la cabeza hacia el detective, cabecea de sueño (Somnoliento), mueve
// orejas y cola, y lleva un objeto en la mano (la dona de Porky).
//
// Estados: reposo según el ánimo (-2 cerrado, -1 nervioso, 0 tranquilo, 1+ cooperativo), hablar mientras
// dice su línea, gestos de una vez (negar, asentir) y un gesto ocasional (Porky se come la dona).
// Una cinemática puede tomar el control con Controlar y fijar la animación y su tiempo en cada cuadro.
// Va como hijo del personaje (el nodo que mira hacia +Z), junto a su Modelo.
public partial class AnimadorPersonaje : Node
{
	[Export] public Node3D Modelo;
	// Animaciones de reposo según el ánimo (vacío = la de Reposo)
	[Export] public string Reposo = "Idle";
	[Export] public string ReposoNervioso = "";
	[Export] public string ReposoCerrado = "Idle_FoldArms";
	[Export] public string ReposoCooperativo = "";
	[Export] public string AlHablar = "Idle_Talking";
	// Los nervios aceleran el reposo
	[Export] public float VelocidadNervioso = 1.35f;
	// Un gesto que hace solo de vez en cuando, si no está hablando (vacío = ninguno)
	[Export] public string GestoOcasional = "";
	[Export] public Vector2 IntervaloGesto = new(14f, 26f);
	// Cabecea con la cabeza abajo mientras el detective está lejos
	[Export] public bool Somnoliento;
	// Voltea a ver al detective cuando lo tiene a menos de esta distancia
	[Export] public float DistanciaMirar = 5f;
	[Export] public float GiroMaximo = 45f;
	// Un objeto que lleva en la mano (se pega a la mano más cercana en la pose T)
	[Export] public Node3D ObjetoEnMano;
	// Segundos de mezcla al cambiar de animación
	[Export] public float Mezcla = 0.3f;

	[Signal] public delegate void GestoTerminadoEventHandler(string animacion);

	public const string RutaUal1 = "res://Assets/Animaciones/Quaternius/UAL1_Standard.glb";
	public const string RutaUal2 = "res://Assets/Animaciones/Quaternius/UAL2_Standard.glb";
	private static readonly string[] Librerias = { RutaUal1, RutaUal2 };
	private static readonly Dictionary<string, PackedScene> Escenas = new();

	// ---------- Huesos ----------

	// Nombres comunes de los huesos que se copian (el resto se queda en su pose de reposo)
	private enum H { Cadera, Columna, Pecho, PechoAlto, Cuello, Cabeza, HombroI, BrazoI, AntebrazoI, ManoI, HombroD, BrazoD, AntebrazoD, ManoD, MusloI, PiernaI, PieI, DedosI, MusloD, PiernaD, PieD, DedosD }
	private static readonly int TotalH = Enum.GetValues<H>().Length;

	private static readonly string[] MapaUal1 = {
		"DEF-hips", "DEF-spine.001", "DEF-spine.002", "DEF-spine.003", "DEF-neck", "DEF-head",
		"DEF-shoulder.L", "DEF-upper_arm.L", "DEF-forearm.L", "DEF-hand.L", "DEF-shoulder.R", "DEF-upper_arm.R", "DEF-forearm.R", "DEF-hand.R",
		"DEF-thigh.L", "DEF-shin.L", "DEF-foot.L", "DEF-toe.L", "DEF-thigh.R", "DEF-shin.R", "DEF-foot.R", "DEF-toe.R" };
	private static readonly string[] MapaUal2 = {
		"pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "Head",
		"clavicle_l", "upperarm_l", "lowerarm_l", "hand_l", "clavicle_r", "upperarm_r", "lowerarm_r", "hand_r",
		"thigh_l", "calf_l", "foot_l", "ball_l", "thigh_r", "calf_r", "foot_r", "ball_r" };
	// Animal Crossing: Skl_Root lleva la columna (Spine_1) y la cintura con las piernas (Waist)
	private static readonly string[] MapaAnimal = {
		"Skl_Root", "Spine_1", "Spine_2", "Spine_3", "Neck", "Head",
		"Clavicle_L", "Arm_1_L", "Arm_2_L", "Hand_L", "Clavicle_R", "Arm_1_R", "Arm_2_R", "Hand_R",
		"Leg_1_L", "Leg_2_L", "Ankle_L", "Toe_L", "Leg_1_R", "Leg_2_R", "Ankle_R", "Toe_R" };
	// Mixamo, sin el prefijo (mixamorig_, mixamorig:, mixamorig1_...)
	private static readonly string[] MapaMixamo = {
		"Hips", "Spine", "Spine1", "Spine2", "Neck", "Head",
		"LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand",
		"LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase" };

	// Índice de cada hueso común en el esqueleto (-1 si no lo tiene)
	private static int[] Mapear(Skeleton3D esqueleto)
	{
		string[] nombres;
		string prefijo = "";
		if (esqueleto.FindBone("DEF-hips") >= 0) nombres = MapaUal1;
		else if (esqueleto.FindBone("pelvis") >= 0 && esqueleto.FindBone("spine_01") >= 0) nombres = MapaUal2;
		else if (esqueleto.FindBone("Skl_Root") >= 0) nombres = MapaAnimal;
		else
		{
			nombres = MapaMixamo;
			for (int i = 0; i < esqueleto.GetBoneCount(); i++)
			{
				string n = esqueleto.GetBoneName(i);
				if (n.EndsWith("Hips")) { prefijo = n[..^4]; break; }
			}
		}
		var mapa = new int[TotalH];
		for (int h = 0; h < TotalH; h++) mapa[h] = esqueleto.FindBone(prefijo + nombres[h]);
		return mapa;
	}

	// Padres antes que hijos
	private static int[] Orden(Skeleton3D esqueleto)
	{
		int total = esqueleto.GetBoneCount();
		var orden = new List<int>(total);
		var puesto = new bool[total];
		void Poner(int i)
		{
			if (puesto[i]) return;
			int padre = esqueleto.GetBoneParent(i);
			if (padre >= 0) Poner(padre);
			puesto[i] = true;
			orden.Add(i);
		}
		for (int i = 0; i < total; i++) Poner(i);
		return orden.ToArray();
	}

	private static Transform3D[] GlobalesReposo(Skeleton3D esqueleto, int[] orden)
	{
		var g = new Transform3D[esqueleto.GetBoneCount()];
		foreach (int i in orden)
		{
			int padre = esqueleto.GetBoneParent(i);
			g[i] = (padre >= 0 ? g[padre] : Transform3D.Identity) * esqueleto.GetBoneRest(i);
		}
		return g;
	}

	private static Quaternion Rot(in Transform3D t) => t.Basis.Orthonormalized().GetRotationQuaternion();

	// Ejes del personaje en el espacio del esqueleto (X a su izquierda, Y arriba, Z al frente), sacados de la pose T
	private static Basis Ejes(Transform3D[] g, int[] mapa)
	{
		Vector3 izquierda = g[mapa[(int)H.MusloI]].Origin - g[mapa[(int)H.MusloD]].Origin;
		Vector3 arriba = (g[mapa[(int)H.Cabeza]].Origin - g[mapa[(int)H.Cadera]].Origin).Normalized();
		izquierda = (izquierda - arriba * izquierda.Dot(arriba)).Normalized();
		return new Basis(izquierda, arriba, izquierda.Cross(arriba));
	}

	private static float AlturaCadera(Transform3D[] g, int[] mapa, Basis ejes)
	{
		Vector3 pies = (g[mapa[(int)H.PieI]].Origin + g[mapa[(int)H.PieD]].Origin) * 0.5f;
		return Mathf.Max(1e-5f, (g[mapa[(int)H.Cadera]].Origin - pies).Dot(ejes.Y));
	}

	// ---------- Maniquí que reproduce las animaciones ----------

	private class Fuente
	{
		public Node Raiz;
		public Skeleton3D Esqueleto;
		public AnimationPlayer Reproductor;
		public int[] Orden;
		public int[] Mapa;
		public Quaternion[] ReposoInverso;  // giro global de reposo de cada hueso, invertido
		public Vector3 CaderaReposo;
		public Quaternion A;                // del espacio del maniquí al del personaje
		public float Escala;                // altura de cadera del personaje / la del maniquí
		public Transform3D[] Global;
		public HashSet<string> Animaciones = new();
	}

	private readonly List<Fuente> _fuentes = new();

	// ---------- Personaje ----------

	private Node3D _personaje;
	private Skeleton3D _esqueleto;
	private int[] _orden;
	private int[] _mapa;
	private int[] _huesoAH;            // hueso del personaje -> hueso común (-1 si no se copia)
	private Transform3D[] _reposoLocal;
	private Quaternion[] _reposoGlobal;
	private Vector3 _caderaReposo;
	private Basis _ejes;
	private Transform3D[] _global;     // pose calculada (sin mezcla)
	private Quaternion[] _local;
	private Vector3 _caderaLocal;
	private Quaternion[] _instantanea; // pose de donde viene la mezcla
	private Vector3 _caderaInstantanea;
	private float _mezcla = 1f, _duracionMezcla = 0.3f;
	private int _cuello = -1, _cabeza = -1, _orejaI = -1, _orejaD = -1, _cola = -1, _mano = -1;
	private Transform3D _manoAObjeto;
	private Node3D _jugador;

	// ---------- Estado ----------

	private Fuente _fuente;
	private string _actual = "";
	private double _tiempo;
	private float _velocidad = 1f;
	private bool _bucle = true;
	private int _animo;
	private bool _hablando;
	private string _gesto = "";
	private double _relojGesto;
	private float _fase;
	private float _yaw, _pitch;
	private bool _listo;

	// Una cinemática fija la animación y su tiempo; mientras tanto no mira al jugador ni hace gestos solo
	public bool Controlado { get; private set; }
	public bool MirarJugador { get; set; } = true;
	// Con el control de una cinemática: cuánto baja la cabeza (grados; dormido, mirando algo en el piso)
	public float InclinarCabeza { get; set; }

	public override void _Ready()
	{
		_personaje = GetParent<Node3D>();
		Modelo ??= _personaje.GetNodeOrNull<Node3D>("Modelo");
		_esqueleto = Modelo != null ? BuscarEsqueleto(Modelo) : null;
		if (_esqueleto == null)
		{
			GD.PushWarning($"AnimadorPersonaje: {_personaje.Name} no tiene esqueleto.");
			SetProcess(false);
			return;
		}
		_orden = Orden(_esqueleto);
		_mapa = Mapear(_esqueleto);
		// Mixamo (FBX): la cadera de reposo queda hundida en el piso; la altura buena es la de su propia
		// animación. Se toma de ahí y luego se apaga ese reproductor para que no pelee con el retarget.
		bool mixamo = _esqueleto.FindBone("Skl_Root") < 0 && _esqueleto.FindBone("DEF-hips") < 0 && _esqueleto.FindBone("pelvis") < 0;
		Vector3? caderaPropia = null;
		foreach (Node n in Modelo.FindChildren("*", "AnimationPlayer", true, false))
		{
			if (n is not AnimationPlayer propio) continue;
			string[] lista = propio.GetAnimationList();
			if (mixamo && lista.Length > 0 && _mapa[(int)H.Cadera] >= 0)
			{
				propio.Play(lista[0]);
				propio.Seek(0, true);
				caderaPropia = _esqueleto.GetBonePosePosition(_mapa[(int)H.Cadera]);
			}
			propio.Stop();
			propio.Active = false;
		}
		_esqueleto.ResetBonePoses();
		foreach (H h in new[] { H.Cadera, H.Cabeza, H.MusloI, H.MusloD, H.PieI, H.PieD })
			if (_mapa[(int)h] < 0)
			{
				GD.PushWarning($"AnimadorPersonaje: {_personaje.Name} no tiene el hueso {h}.");
				SetProcess(false);
				return;
			}
		int total = _esqueleto.GetBoneCount();
		_reposoLocal = new Transform3D[total];
		for (int i = 0; i < total; i++) _reposoLocal[i] = _esqueleto.GetBoneRest(i);
		Transform3D[] g = GlobalesReposo(_esqueleto, _orden);
		_reposoGlobal = new Quaternion[total];
		for (int i = 0; i < total; i++) _reposoGlobal[i] = Rot(g[i]);
		_ejes = Ejes(g, _mapa);
		_caderaReposo = g[_mapa[(int)H.Cadera]].Origin;
		if (caderaPropia is Vector3 propia && _esqueleto.GetBoneParent(_mapa[(int)H.Cadera]) < 0)
			_caderaReposo = propia;
		_huesoAH = new int[total];
		Array.Fill(_huesoAH, -1);
		for (int h = 0; h < TotalH; h++) if (_mapa[h] >= 0) _huesoAH[_mapa[h]] = h;
		_global = new Transform3D[total];
		_local = new Quaternion[total];
		_instantanea = new Quaternion[total];

		_cuello = _mapa[(int)H.Cuello] >= 0 ? _mapa[(int)H.Cuello] : _mapa[(int)H.Cabeza];
		_cabeza = _mapa[(int)H.Cabeza];
		_orejaI = _esqueleto.FindBone("Ear_1_L");
		_orejaD = _esqueleto.FindBone("Ear_1_R");
		_cola = _esqueleto.FindBone("Tail_1");

		if (ObjetoEnMano != null)
		{
			// Se pega a la mano que tenía más cerca en la pose T
			int manoI = _mapa[(int)H.ManoI], manoD = _mapa[(int)H.ManoD];
			Transform3D esq = _esqueleto.GlobalTransform;
			float dI = manoI < 0 ? float.MaxValue : (esq * g[manoI].Origin).DistanceTo(ObjetoEnMano.GlobalPosition);
			float dD = manoD < 0 ? float.MaxValue : (esq * g[manoD].Origin).DistanceTo(ObjetoEnMano.GlobalPosition);
			_mano = dI <= dD ? manoI : manoD;
			if (_mano >= 0) _manoAObjeto = (esq * g[_mano]).AffineInverse() * ObjetoEnMano.GlobalTransform;
		}

		foreach (string ruta in Librerias) CrearFuente(ruta, g);
		_fase = (float)GD.RandRange(0.0, Mathf.Tau);
		_relojGesto = GD.RandRange(IntervaloGesto.X, IntervaloGesto.Y);
		_listo = _fuentes.Count > 0;
		if (!_listo) { SetProcess(false); return; }
		Cambiar(Base(), true, VelocidadBase(), true);
		// Empieza en un punto al azar para que dos personajes no se muevan igual
		_tiempo = GD.RandRange(0.0, Duracion(_actual));
		_mezcla = 1f;
	}

	private static Skeleton3D BuscarEsqueleto(Node raiz)
	{
		Skeleton3D mejor = null;
		foreach (Node n in raiz.FindChildren("*", "Skeleton3D", true, false))
			if (n is Skeleton3D s && s.GetBoneCount() > (mejor?.GetBoneCount() ?? 1)) mejor = s;
		return mejor;
	}

	private void CrearFuente(string ruta, Transform3D[] globalPersonaje)
	{
		if (!Escenas.TryGetValue(ruta, out PackedScene escena))
		{
			escena = ResourceLoader.Exists(ruta) ? GD.Load<PackedScene>(ruta) : null;
			Escenas[ruta] = escena;
		}
		if (escena == null) return;
		Node raiz = escena.Instantiate();
		// El maniquí no se ve: solo se usan sus huesos
		foreach (Node n in raiz.FindChildren("*", "MeshInstance3D", true, false)) n.QueueFree();
		if (raiz is Node3D r3) r3.Visible = false;
		AddChild(raiz);
		var f = new Fuente { Raiz = raiz, Esqueleto = BuscarEsqueleto(raiz) };
		foreach (Node n in raiz.FindChildren("*", "AnimationPlayer", true, false))
			if (n is AnimationPlayer a) f.Reproductor = a;
		if (f.Esqueleto == null || f.Reproductor == null) { raiz.QueueFree(); return; }
		f.Reproductor.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
		foreach (string a in f.Reproductor.GetAnimationList()) f.Animaciones.Add(a);
		f.Orden = Orden(f.Esqueleto);
		f.Mapa = Mapear(f.Esqueleto);
		Transform3D[] g = GlobalesReposo(f.Esqueleto, f.Orden);
		f.ReposoInverso = new Quaternion[g.Length];
		for (int i = 0; i < g.Length; i++) f.ReposoInverso[i] = Rot(g[i]).Inverse();
		f.CaderaReposo = g[f.Mapa[(int)H.Cadera]].Origin;
		Basis ejesManiqui = Ejes(g, f.Mapa);
		f.A = (_ejes * ejesManiqui.Inverse()).Orthonormalized().GetRotationQuaternion();
		f.Escala = AlturaCadera(globalPersonaje, _mapa, _ejes) / AlturaCadera(g, f.Mapa, ejesManiqui);
		f.Global = new Transform3D[g.Length];
		_fuentes.Add(f);
	}

	// ---------- API ----------

	public bool Tiene(string animacion) => FuenteDe(animacion) != null;

	public double Duracion(string animacion)
	{
		Fuente f = FuenteDe(animacion);
		return f != null ? f.Reproductor.GetAnimation(animacion).Length : 0.0;
	}

	public void FijarAnimo(int animo) => _animo = animo;

	public void Hablar(bool hablando) => _hablando = hablando;

	// Gesto de una vez (negar, asentir...); al terminar vuelve al reposo
	public void Gesto(string animacion, float velocidad = 1f)
	{
		if (!_listo || Controlado || !Tiene(animacion)) return;
		_gesto = animacion;
		Cambiar(animacion, false, velocidad, true);
	}

	// La cinemática toma el control: desde aquí cada cuadro dice qué animación y en qué segundo
	public void Controlar(bool controlar)
	{
		Controlado = controlar;
		_gesto = "";
		if (!controlar) Cambiar(Base(), true, VelocidadBase(), true);
	}

	// Pone la animación en el segundo dado (en bucle si lo pide). Si cambia de animación, mezcla.
	public void Fijar(string animacion, double segundo, bool bucle = true, float mezcla = -1f)
	{
		if (!_listo) return;
		if (animacion != _actual) Cambiar(animacion, bucle, 1f, true, mezcla);
		_bucle = bucle;
		_tiempo = segundo;
	}

	// Transformación global (en el mundo) de una mano, para que la cinemática le pegue objetos
	public Transform3D Mano(bool izquierda)
	{
		int hueso = _mapa?[(int)(izquierda ? H.ManoI : H.ManoD)] ?? -1;
		if (hueso < 0) return _personaje.GlobalTransform;
		return _esqueleto.GlobalTransform * _esqueleto.GetBoneGlobalPose(hueso);
	}

	// ---------- Cuadro a cuadro ----------

	private string Base()
	{
		if (_hablando && AlHablar != "" && Tiene(AlHablar)) return AlHablar;
		string nombre = _animo switch
		{
			<= -2 => ReposoCerrado,
			-1 => ReposoNervioso,
			>= 1 => ReposoCooperativo,
			_ => Reposo,
		};
		return nombre != "" && Tiene(nombre) ? nombre : Reposo;
	}

	private float VelocidadBase() => _animo == -1 && !_hablando ? VelocidadNervioso : 1f;

	private Fuente FuenteDe(string animacion)
	{
		foreach (Fuente f in _fuentes) if (f.Animaciones.Contains(animacion)) return f;
		return null;
	}

	private void Cambiar(string animacion, bool bucle, float velocidad, bool desdeCero, float mezcla = -1f)
	{
		Fuente f = FuenteDe(animacion);
		if (f == null) return;
		if (animacion != _actual && _actual != "")
		{
			// Mezcla desde la pose que se ve ahora
			for (int i = 0; i < _local.Length; i++) _instantanea[i] = _esqueleto.GetBonePoseRotation(i);
			_caderaInstantanea = _esqueleto.GetBonePosePosition(_mapa[(int)H.Cadera]);
			_mezcla = 0f;
			_duracionMezcla = mezcla >= 0 ? mezcla : Mezcla;
		}
		_fuente = f;
		_actual = animacion;
		_bucle = bucle;
		_velocidad = velocidad;
		if (desdeCero) _tiempo = 0;
		f.Reproductor.Play(animacion);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (!Controlado)
		{
			string base_ = Base();
			if (_gesto != "")
			{
				if (_tiempo >= Duracion(_gesto) - _duracionMezcla)
				{
					string hecho = _gesto;
					_gesto = "";
					Cambiar(base_, true, VelocidadBase(), true);
					EmitSignal(SignalName.GestoTerminado, hecho);
				}
			}
			else
			{
				if (base_ != _actual) Cambiar(base_, true, VelocidadBase(), true);
				_velocidad = VelocidadBase();
				if (GestoOcasional != "" && !_hablando && (_relojGesto -= delta) <= 0)
				{
					_relojGesto = GD.RandRange(IntervaloGesto.X, IntervaloGesto.Y);
					Gesto(GestoOcasional);
				}
			}
			_tiempo += delta * _velocidad;
		}
		_mezcla = Mathf.Min(1f, _mezcla + dt / Mathf.Max(0.01f, _duracionMezcla));
		_fase += dt * (Somnoliento && !Cerca(out _) ? 1.2f : 2f);
		Aplicar(dt);
	}

	private bool Cerca(out Vector3 punto)
	{
		_jugador ??= GetTree().GetFirstNodeInGroup("jugador") as Node3D;
		punto = _jugador != null ? _jugador.GlobalPosition + Vector3.Up * 1.6f : Vector3.Zero;
		return _jugador != null && _jugador.GlobalPosition.DistanceTo(_personaje.GlobalPosition) < DistanciaMirar;
	}

	private void Aplicar(float dt)
	{
		Fuente f = _fuente;
		double largo = f.Reproductor.GetAnimation(_actual).Length;
		double t = _bucle && largo > 0 ? _tiempo % largo : Math.Min(_tiempo, largo);
		f.Reproductor.Seek(t, true);

		// Pose global del maniquí
		foreach (int i in f.Orden)
		{
			int padre = f.Esqueleto.GetBoneParent(i);
			f.Global[i] = (padre >= 0 ? f.Global[padre] : Transform3D.Identity) * f.Esqueleto.GetBonePose(i);
		}

		// Pose del personaje: cada hueso común gira lo mismo que en el maniquí respecto a la pose T
		Quaternion aInv = f.A.Inverse();
		int cadera = _mapa[(int)H.Cadera];
		foreach (int i in _orden)
		{
			int padre = _esqueleto.GetBoneParent(i);
			Transform3D globalPadre = padre >= 0 ? _global[padre] : Transform3D.Identity;
			int h = _huesoAH[i];
			int s = h >= 0 ? f.Mapa[h] : -1;
			Quaternion local = _reposoLocal[i].Basis.GetRotationQuaternion();
			Vector3 posicion = _reposoLocal[i].Origin;
			if (s >= 0)
			{
				Quaternion giro = f.A * (Rot(f.Global[s]) * f.ReposoInverso[s]) * aInv;
				Quaternion objetivo = (giro * _reposoGlobal[i]).Normalized();
				local = (Rot(globalPadre).Inverse() * objetivo).Normalized();
				if (i == cadera)
				{
					Vector3 desplazamiento = f.A * (f.Global[s].Origin - f.CaderaReposo) * f.Escala;
					posicion = globalPadre.AffineInverse() * (_caderaReposo + desplazamiento);
				}
			}
			_local[i] = local;
			if (i == cadera) _caderaLocal = posicion;
			_global[i] = globalPadre * new Transform3D(new Basis(local).Scaled(_reposoLocal[i].Basis.Scale), posicion);
		}

		// Mezcla con la pose anterior
		float w = Mathf.SmoothStep(0f, 1f, _mezcla);
		if (w < 1f)
		{
			for (int i = 0; i < _local.Length; i++) _local[i] = _instantanea[i].Slerp(_local[i], w).Normalized();
			_caderaLocal = _caderaInstantanea.Lerp(_caderaLocal, w);
		}

		Adornos(dt);

		for (int i = 0; i < _local.Length; i++) _esqueleto.SetBonePoseRotation(i, _local[i]);
		_esqueleto.SetBonePosePosition(cadera, _caderaLocal);

		if (ObjetoEnMano != null && _mano >= 0)
			ObjetoEnMano.GlobalTransform = _esqueleto.GlobalTransform * _esqueleto.GetBoneGlobalPose(_mano) * _manoAObjeto;
	}

	// Cabeza hacia el detective (o cabeceo de sueño), orejas y cola, sumados encima de la animación
	private void Adornos(float dt)
	{
		float yawObjetivo = 0f, pitchObjetivo = 0f;
		if (!Controlado && MirarJugador && _cuello >= 0)
		{
			if (Cerca(out Vector3 punto))
			{
				Vector3 local = _esqueleto.GlobalTransform.AffineInverse() * punto - _global[_cuello].Origin;
				float x = local.Dot(_ejes.X), y = local.Dot(_ejes.Y), z = local.Dot(_ejes.Z);
				yawObjetivo = Mathf.Clamp(Mathf.Atan2(x, z), Mathf.DegToRad(-GiroMaximo), Mathf.DegToRad(GiroMaximo));
				pitchObjetivo = Mathf.Clamp(-Mathf.Atan2(y, new Vector2(x, z).Length()), Mathf.DegToRad(-15f), Mathf.DegToRad(15f));
				// De espaldas no se tuerce el cuello
				if (z < 0) yawObjetivo = pitchObjetivo = 0f;
			}
			else if (Somnoliento && _gesto == "")
				pitchObjetivo = Mathf.DegToRad(24f + 6f * Mathf.Sin(_fase * 0.4f));
		}
		else if (Controlado)
			pitchObjetivo = Mathf.DegToRad(InclinarCabeza);
		float suave = 1f - Mathf.Exp(-dt * 4f);
		_yaw = Mathf.Lerp(_yaw, yawObjetivo, suave);
		_pitch = Mathf.Lerp(_pitch, pitchObjetivo, suave);
		if (_cuello >= 0 && (Mathf.Abs(_yaw) > 1e-4f || Mathf.Abs(_pitch) > 1e-4f))
			GirarEnEsqueleto(_cuello, new Quaternion(_ejes.Y, _yaw) * new Quaternion(_ejes.X, _pitch));

		float tic = Mathf.Max(0f, Mathf.Sin(_fase * 0.85f + 1.3f));
		tic = tic > 0.97f ? (tic - 0.97f) / 0.03f : 0f;
		if (_orejaI >= 0) GirarEnEsqueleto(_orejaI, new Quaternion(_ejes.Z, Mathf.DegToRad(10f * tic)));
		if (_orejaD >= 0) GirarEnEsqueleto(_orejaD, new Quaternion(_ejes.Z, Mathf.DegToRad(-10f * tic)));
		if (_cola >= 0) GirarEnEsqueleto(_cola, new Quaternion(_ejes.Y, Mathf.DegToRad(12f * Mathf.Sin(_fase * 0.65f))));
	}

	// Suma un giro dado en el espacio del esqueleto al hueso (sus hijos lo siguen)
	private void GirarEnEsqueleto(int hueso, Quaternion giro)
	{
		int padre = _esqueleto.GetBoneParent(hueso);
		Quaternion globalPadre = padre >= 0 ? Rot(_global[padre]) : Quaternion.Identity;
		_local[hueso] = (globalPadre.Inverse() * giro * globalPadre * _local[hueso]).Normalized();
	}
}
