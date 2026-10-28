using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// La cinemática de la cinta como MOMENTOS CONGELADOS (estilo Return of the Obra Dinn): 12 cuadros quietos con
// poses hechas a mano en Blender (Assets/Cinematicas/Blender/scripts/momentos.py → cinta_momentos.glb). En cada
// cuadro los personajes no se mueven: la cámara va despacio de su toma de inicio a la de fin, un acento de luz
// dirige la mirada y el audio de la cinta corre por debajo y une todo. Antes de cada cuadro, un destello blanco
// y el clic de la cinta, como si la grabación saltara. Todo depende de t, igual que la versión animada.
public partial class CinematicaCinta
{
	// Interruptor para comparar: true = momentos congelados, false = la versión animada (cinta_caso0.glb)
	public static bool UsarMomentosCongelados { get; set; } = true;

	public const string AnimacionMomentos = "res://Assets/Cinematicas/Caso0/cinta_momentos.glb";

	private bool _momentos;

	// ---------- Tiempos: el negro con la confesión y los 12 cuadros ----------

	private const double MNegro = 4.2;
	private static readonly double[] MDuracion = { 4.0, 3.6, 4.0, 4.0, 5.0, 3.6, 4.0, 4.0, 3.6, 3.6, 5.2, 4.6 };
	private static readonly double[] MInicio = Inicios();
	private static readonly double MFin = MInicio[^1] + MDuracion[^1];
	private const double MDestello = 0.09;

	private static double[] Inicios()
	{
		var inicios = new double[MDuracion.Length + 1];
		inicios[0] = 0;
		inicios[1] = MNegro;
		for (int k = 2; k <= MDuracion.Length; k++) inicios[k] = inicios[k - 1] + MDuracion[k - 2];
		return inicios;
	}

	// El cuadro en t (0 = el negro del principio)
	private static int CuadroEn(double t)
	{
		for (int k = MDuracion.Length; k >= 1; k--) if (t >= MInicio[k]) return k;
		return 0;
	}

	private static double M(int k, double desde = 0) => MInicio[k] + desde;

	private static readonly (double T, string Texto)[] MHoras =
	{
		(MNegro - 0.6, "SALA 1 · 03:09"), (M(3), "SALA 1 · 03:12"), (M(9), "SALA 1 · 03:15"), (M(12), "SALA 1 · 03:16"),
	};
	private static readonly (double Inicio, double Fin, int Linea)[] MSubtitulos =
	{
		(0.7, 4.1, 0),
		(M(3, 0.15), M(3, 2.95), 1),
		(M(5, 0.6), M(5, 3.3), 2),
	};
	private static readonly double MGolpe = M(7, 1.0);       // el reloj llega al piso: se para a las 3:12

	// Acento de luz de cada cuadro: color y energía de un foco puesto en Luz_k (apunta a su -Z)
	private static readonly Dictionary<int, (Color Color, float Energia, float Angulo)> MAcentos = new()
	{
		{ 3, (new Color(0.62f, 0.72f, 1f), 7f, 30f) },      // luz fría del ducto sobre la mano y el polvo
		{ 4, (new Color(0.62f, 0.72f, 1f), 9f, 28f) },      // contraluz: la figura recortada
		{ 5, (new Color(0.62f, 0.72f, 1f), 5f, 26f) },
		{ 6, (new Color(0.62f, 0.72f, 1f), 5f, 26f) },
		{ 7, (new Color(0.62f, 0.72f, 1f), 16f, 34f) },
		{ 8, (new Color(1f, 0.86f, 0.62f), 6f, 12f) },      // foco sobre la carátula
		{ 9, (new Color(1f, 0.86f, 0.66f), 2.5f, 30f) },
		{ 10, (new Color(1f, 0.86f, 0.66f), 2.5f, 34f) },
		{ 12, (new Color(1f, 0.82f, 0.6f), 14f, 34f) },      // las botas en primer plano
	};

	private readonly Camera3D[] _camIni = new Camera3D[13], _camFin = new Camera3D[13];
	private readonly Node3D[] _foco = new Node3D[13], _luz = new Node3D[13];
	private readonly List<(int Cuadro, Node3D Nodo)> _soloEn = new();
	private SpotLight3D _acento;
	private OmniLight3D _recCara;
	private ColorRect _destello;
	private Node3D _animPluma;

	private double FinActual => _momentos ? MFin : TFin;
	private double[] ParadasActuales => _momentos ? MInicio.Skip(1).Append(MFin).ToArray() : Paradas;
	private (double T, string Texto)[] HorasActuales => _momentos ? MHoras : Horas;
	private (double Inicio, double Fin, int Linea)[] SubtitulosActuales => _momentos ? MSubtitulos : Subtitulos;

	// ---------- Montaje ----------

	private void PrepararMomentos()
	{
		for (int k = 1; k <= 12; k++)
		{
			_camIni[k] = _escena.GetNode<Camera3D>($"Cam_{k:00}_Ini");
			_camFin[k] = _escena.GetNode<Camera3D>($"Cam_{k:00}_Fin");
			_foco[k] = _escena.GetNode<Node3D>($"Foco_{k:00}");
			_luz[k] = _escena.GetNodeOrNull<Node3D>($"Luz_{k:00}");
		}
		// El polvo y la cuerda solo existen en su cuadro (Polvo_03, Cuerda_04...)
		foreach (Node n in _escena.GetChildren())
			if (n is Node3D nodo && (nodo.Name.ToString().StartsWith("Polvo_") || nodo.Name.ToString().StartsWith("Cuerda_")))
				_soloEn.Add((int.Parse(nodo.Name.ToString()[^2..]), nodo));
		foreach (var (_, nodo) in _soloEn)
			foreach (Node m in nodo.FindChildren("*", "MeshInstance3D", true, false).Append(nodo))
				if (m is MeshInstance3D malla) malla.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		_animPluma = _escena.GetNodeOrNull<Node3D>("Anim_Pluma");

		// Una sola cámara: va de Cam_k_Ini a Cam_k_Fin
		_camara = new Camera3D { Name = "Camara" };
		_escena.AddChild(_camara);
		_acento = new SpotLight3D { LightEnergy = 0f, SpotRange = 8f, SpotAttenuation = 0.9f, ShadowEnabled = true };
		_escena.AddChild(_acento);
		// El rojo de REC en la cara de Porky (cuadro 11)
		_recCara = new OmniLight3D { LightColor = new Color(1f, 0.08f, 0.05f), LightEnergy = 0f, OmniRange = 1.1f, OmniAttenuation = 1.4f };
		_escena.AddChild(_recCara);
		_recCara.GlobalPosition = (_luzRecMalla ?? _a.Grabadora).GlobalPosition + new Vector3(-0.02f, 0.06f, 0.08f);
	}

	private void CrearDestello()
	{
		_destello = new ColorRect { Color = new Color(1, 1, 1, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_destello);
		_interfaz.MoveChild(_destello, _negro.GetIndex() + 1);
		Anclar(_destello, 0, 0, 1, 1);
	}

	// ---------- Cada cuadro de imagen ----------

	private void ActualizarMomentos()
	{
		int k = CuadroEn(_t);
		int cuadro = Math.Max(1, k);
		// La pose del cuadro: un solo fotograma por cuadro (en el segundo k del .glb), sin interpolar
		_anim.Seek(cuadro + 0.05, true);
		ObjetosMomentos(k);
		CamaraMomentos(cuadro);
	}

	private static float Suave(double u)
	{
		float x = (float)Mathf.Clamp(u, 0, 1);
		return x * x * (3 - 2 * x);
	}

	private void CamaraMomentos(int k)
	{
		double u = (_t - MInicio[k]) / MDuracion[k - 1];
		float s = Suave(u);
		Transform3D a = _camIni[k].GlobalTransform, b = _camFin[k].GlobalTransform;
		_camara.GlobalTransform = new Transform3D(new Basis(a.Basis.GetRotationQuaternion().Slerp(b.Basis.GetRotationQuaternion(), s)), a.Origin.Lerp(b.Origin, s));
		_camara.Fov = Mathf.Lerp(_camIni[k].Fov, _camFin[k].Fov, s);
		// El desenfoque enfoca el punto del cuadro: lo que queda detrás se pierde
		float enfoque = _camara.GlobalPosition.DistanceTo(_foco[k].GlobalPosition);
		_enfoque.DofBlurFarEnabled = true;
		_enfoque.DofBlurFarDistance = enfoque + 0.35f;
		_enfoque.DofBlurFarTransition = Mathf.Max(0.4f, enfoque * 0.5f);
	}

	private void ObjetosMomentos(int k)
	{
		_a.Reloj.GlobalTransform = _animReloj.GlobalTransform;
		if (_silla != null) _silla.GlobalTransform = _animSilla.GlobalTransform;
		if (_rejilla != null) _rejilla.GlobalTransform = _animRejilla.GlobalTransform;
		if (_puerta != null) _puerta.GlobalTransform = _animPuerta.GlobalTransform;
		if (_cinta != null) { _cinta.Visible = true; _cinta.GlobalTransform = _animCinta.GlobalTransform; }
		_figura.Visible = k >= 3 && k <= 7;
		foreach (var (cuadro, nodo) in _soloEn) nodo.Visible = cuadro == k;
		if (_animPluma != null) _animPluma.Visible = false;

		// El reloj: 3:09 al principio, 3:12 desde que se abre la rejilla; con el golpe se raja y se para
		if (_grietas != null) _grietas.Visible = _t >= MGolpe;
		float atraso = k <= 2 ? 3f : 0f;
		if (_minutero != null) _minutero.Transform = _minuteroAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(6f * atraso)) * _minuteroAntes.Basis };
		if (_horario != null) _horario.Transform = _horarioAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(0.5f * atraso)) * _horarioAntes.Basis };

		// REC parpadea todo el tiempo (la trampa del comisario sigue en REC a las 6:02)
		bool encendida = Mathf.PosMod((float)_t, 1f) < 0.6f;
		_luzRec.LightEnergy = encendida ? 0.12f : 0.02f;
		if (_luzRecMalla != null) _luzRecMalla.Visible = encendida;
		_recCara.LightEnergy = k == 11 ? (encendida ? 1.6f : 0.5f) : 0f;

		// El contador: 03:09, 03:12 cuando entra la figura, 03:15 al despertar; el rebobinado lo regresa a 03:12
		if (_contador != null)
		{
			double m = k <= 2 ? 9 : k <= 8 ? 12 : k <= 10 ? 15 : k == 11 ? Mathf.Lerp(15.0, 12.0, Rampa(_t, M(11, 1.0), M(11, 3.4))) : 12;
			_contador.Text = $"03:{(int)m:00}";
		}

		// El ducto abierto (con su luz) mientras la tapa está corrida; la pluma queda en la tapa mal puesta
		bool abierto = k >= 3 && k <= 7;
		_hueco.Visible = abierto;
		// En el inserto de la pluma se cuela un poco de la luz del ducto por la tapa mal puesta
		_luzRejilla.LightEnergy = abierto ? 9f : k == 8 ? 2.5f : 0f;
		if (_pluma != null) _pluma.Visible = k >= 8;
		if (_huellas != null) _huellas.Visible = k >= 12 || _huellasVisibles;
		_luzPasillo.LightEnergy = k == 12 ? 3.5f : 0f;

		// El acento de luz del cuadro
		if (k >= 1 && _luz[k] != null && MAcentos.TryGetValue(k, out var acento))
		{
			_acento.GlobalTransform = _luz[k].GlobalTransform;
			_acento.LightColor = acento.Color;
			_acento.LightEnergy = acento.Energia;
			_acento.SpotAngle = acento.Angulo;
		}
		else _acento.LightEnergy = 0f;
		// En el inserto, el foco sigue a la cámara: de la carátula pasa a la pluma en la rejilla
		if (k == 8 && (_t - MInicio[8]) / MDuracion[7] > 0.55)
		{
			var rejilla = new Vector3(-2.17f, 3.47f, 1.0f);
			var desde = new Vector3(-1.5f, 1.9f, 0.1f);
			_acento.GlobalTransform = Transform3D.Identity.Translated(desde).LookingAt(rejilla, Vector3.Up);
			_acento.LightColor = new Color(0.7f, 0.78f, 1f);
			_acento.LightEnergy = 9f;
			_acento.SpotAngle = 14f;
		}
	}

	private void InterfazMomentos()
	{
		// Negro con la confesión; un destello blanco al saltar de cuadro; fundido a negro al final
		_negro.Color = new Color(0, 0, 0, Mathf.Max(_t < MNegro ? 1f : 0f, Rampa(_t, MFin - 1.2, MFin)));
		int k = CuadroEn(_t);
		float destello = k >= 1 ? 1f - Rampa(_t, MInicio[k], MInicio[k] + MDestello) : 0f;
		_destello.Color = new Color(1, 1, 1, 0.9f * destello);
	}

	// ---------- Sonido ----------

	private void CrearEventosMomentos()
	{
		const string maquina = "res://Assets/Audio/Cinematica/";
		const string paso = "res://Assets/Audio/Efectos/paso_";
		_eventos.Add((TPlay, () => Sonar(Cinta + "cinta_play.wav", -6f)));
		_eventos.Add((0.7, () => { if (_voz.Stream != null) _voz.Play(); }));
		// El clic de la cinta en cada salto
		for (int k = 1; k <= 12; k++) _eventos.Add((MInicio[k], () => Sonar(Cinta + "cinta_stop.wav", -13f)));
		string[] teclas = { "maquina_2.wav", "maquina_4.wav", "maquina_5.wav", "maquina_2.wav" };
		for (int i = 0; i < MHoras.Length; i++)
		{
			string tecla = teclas[i];
			_eventos.Add((MHoras[i].T, () => Sonar(maquina + tecla, -14f, Sonidos.BusInterfaz)));
		}
		_eventos.AddRange(new (double, Action)[]
		{
			(M(3, 0.15), () => Sonar(Cinta + "rejilla_metal.wav", -3f)),
			(M(5, 0.6), () => Sonar(Cinta + "susurro.wav", -2.5f)),
			// El golpe del reloj es el pico más fuerte; el tic-tac se corta con él
			(MGolpe, () => Sonar(Cinta + "reloj_golpe.wav", 5f)),
			(MGolpe + 0.13, () => Sonar(Cinta + "reloj_rebote.wav", -10f)),
			(M(9, 0.1), () => Sonar(Cinta + "sobresalto.wav", -2f)),
			(M(10, 0.8), () => Sonar(Cinta + "reloj_rebote.wav", -12f)),
			(M(10, 1.0), () => Sonar("res://Assets/Audio/Efectos/cajon_3.wav", -20f)),
			(M(11, 0.5), () => Sonar(Cinta + "cinta_stop.wav", -3f)),
			(M(11, 1.0), () => Sonar(Cinta + "cinta_rebobinar.wav", -3f)),
			(M(11, 3.6), () => Sonar(Cinta + "cinta_expulsar.wav", -2f)),
			(M(12, 0.2), () => Sonar(Cinta + "puerta_abrir.wav", -4f)),
			(M(12, 0.7), () => Sonar(paso + "1.wav", -13f)),
			(M(12, 1.2), () => Sonar(paso + "3.wav", -13f)),
			(M(12, 1.7), () => Sonar(paso + "2.wav", -14f)),
			(M(12, 2.2), () => Sonar(paso + "4.wav", -16f)),
			(M(12, 3.0), () => Sonar(Cinta + "puerta_cerrar.wav", -6f)),
		});
	}

	private void SonidosMomentos()
	{
		bool jugando = !Manual && !_terminada;
		int k = CuadroEn(_t);
		Mantener(_siseo, jugando && _t >= TPlay + 0.1);
		// El cuadro 8 es silencio (solo el siseo de la cinta)
		Mantener(_zumbido, jugando && _t >= MNegro - 0.5 && k != 8);
		Mantener(_tictac, jugando && _t >= MNegro - 0.5 && _t < MGolpe);
		// Ronca mientras duerme (más lejos mientras la figura cuelga); el golpe lo calla
		Mantener(_ronquido, jugando && _t >= MNegro - 0.5 && _t < MGolpe - 0.05);
		if (_ronquido.Stream != null) _ronquido.VolumeDb = k == 4 ? -14f : -7f;
		if (_voz.Playing && _t >= MNegro) _voz.Stop();
	}
}
