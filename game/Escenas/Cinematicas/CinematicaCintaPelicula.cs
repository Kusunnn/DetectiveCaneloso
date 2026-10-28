using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// La cinemática de la cinta como PELÍCULA: presente → cinta → presente (como en La conversación o Blow Out).
// Caneloso pone la cinta y presiona PLAY; los carretes giran y, por corte de coincidencia, el carrete se vuelve el
// reloj de pared a las 3:09. Lo que grabó la cinta son momentos congelados con luz motivada, color frío pero
// legible y líneas de tracking al entrar y salir; solo la caída del reloj se mueve, en cámara lenta, y se congela
// en el golpe. De vuelta en el presente, Caneloso rebobina, la voz se oye más clara y voltea a la rejilla.
//
// Las poses, las cámaras y los objetos vienen de Blender (scripts/pelicula.py → cinta_pelicula.glb); lo demás
// (duración, época, transición, hora, luces, sonidos, subtítulos y música de cada cuadro) de cinta_pelicula.json.
// Todo depende de t: IrA, adelantar (por cuadro) y Movie Maker dan siempre lo mismo.
public partial class CinematicaCinta
{
	public enum VersionCinta { Animada, Momentos, Pelicula }

	// Qué versión se reproduce. Las tres se conservan para compararlas.
	public static VersionCinta Version { get; set; } = VersionCinta.Pelicula;

	public const string AnimacionPelicula = "res://Assets/Cinematicas/Caso0/cinta_pelicula.glb";
	public const string DatosPelicula = "res://Assets/Cinematicas/Caso0/cinta_pelicula.json";
	private const string Musica = "res://Assets/Audio/Cinematica/musica_moil.mp3";
	private const string Titulo = "¿Quién es La Garra?";
	private const double DuracionTitulo = 2.8;
	private const double DuracionTracking = 0.45;

	private bool _pelicula;

	private class Cuadro
	{
		public int K;
		public double Inicio, Dur;
		public bool Presente;
		public string Entrada, Hora, Mueve;
		public double Golpe;
		public float Musica, Exposicion;
		public bool Silencio, Ducto, Pluma, Huellas;
		public readonly List<Light3D> Luces = new();
		public (int Linea, double Desde, double Hasta)? Subtitulo;
	}

	private readonly List<Cuadro> _cuadros = new();
	private double _finPelicula;
	private Node3D _relojIni, _relojFin, _astillas;
	private readonly List<(int Cuadro, Node3D Nodo, Transform3D Base)> _carretes = new();
	private ColorRect _tracking;
	private ShaderMaterial _recuerdo, _vineta;
	private Label _titulo;
	private AudioStreamPlayer _musica;

	// ---------- Montaje ----------

	private static Vector3 V(Godot.Collections.Array a) => new((float)(double)a[0], (float)(double)a[1], (float)(double)a[2]);

	private void PrepararPelicula()
	{
		// Cámaras, enfoque y lo que existe solo en un cuadro (Polvo_07, Cuerda_08, Carrete_02_0, Astillas_11...)
		for (int k = 1; k < _camIni.Length; k++)
		{
			_camIni[k] = _escena.GetNodeOrNull<Camera3D>($"Cam_{k:00}_Ini");
			_camFin[k] = _escena.GetNodeOrNull<Camera3D>($"Cam_{k:00}_Fin");
			_foco[k] = _escena.GetNodeOrNull<Node3D>($"Foco_{k:00}");
		}
		var patron = new Regex(@"^(Polvo|Cuerda|Carrete|Astillas)_(\d\d)");
		foreach (Node n in _escena.GetChildren())
		{
			if (n is not Node3D nodo) continue;
			var m = patron.Match(nodo.Name);
			if (!m.Success) continue;
			int k = int.Parse(m.Groups[2].Value);
			_soloEn.Add((k, nodo));
			if (m.Groups[1].Value == "Carrete") _carretes.Add((k, nodo, nodo.Transform));
			if (m.Groups[1].Value == "Astillas") _astillas = nodo;
			foreach (Node malla in nodo.FindChildren("*", "MeshInstance3D", true, false).Append(nodo))
				if (malla is MeshInstance3D mi) mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}
		_relojIni = _escena.GetNodeOrNull<Node3D>("Reloj_Caida_Ini");
		_relojFin = _escena.GetNodeOrNull<Node3D>("Reloj_Caida_Fin");
		_animPluma = _escena.GetNodeOrNull<Node3D>("Anim_Pluma");

		_recCara = new OmniLight3D { LightColor = new Color(1f, 0.08f, 0.05f), LightEnergy = 0f, OmniRange = 1.1f, OmniAttenuation = 1.4f };
		_escena.AddChild(_recCara);
		ArreglarOjos();
		LeerCuadros();
	}

	// Los ojos de Porky son una textura chica con recorte de alfa: de cerca se veían dentados y manchados. En la
	// cinemática se mezclan con suavidad y con filtro anisotrópico.
	private void ArreglarOjos()
	{
		foreach (Node n in _escena.FindChildren("*Eye*", "MeshInstance3D", true, false))
		{
			if (n is not MeshInstance3D ojo) continue;
			for (int i = 0; i < ojo.Mesh.GetSurfaceCount(); i++)
			{
				if (ojo.GetActiveMaterial(i) is not StandardMaterial3D original) continue;
				var mat = (StandardMaterial3D)original.Duplicate();
				mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				mat.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Always;
				mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
				mat.RenderPriority = 1;
				ojo.SetSurfaceOverrideMaterial(i, mat);
			}
		}
	}

	private void LeerCuadros()
	{
		var datos = GD.Load<Json>(DatosPelicula).Data.AsGodotArray();
		double t = 0;
		foreach (var item in datos)
		{
			var d = item.AsGodotDictionary();
			var c = new Cuadro
			{
				K = (int)d["k"], Inicio = t, Dur = (double)d["dur"],
				Presente = (string)d["epoca"] == "presente",
				Entrada = (string)d["entrada"],
				Hora = d.ContainsKey("hora") ? (string)d["hora"] : null,
				Mueve = d.ContainsKey("mueve") ? (string)d["mueve"] : null,
				Golpe = d.ContainsKey("golpe") ? (double)d["golpe"] : 0,
				Musica = d.ContainsKey("musica") ? (float)(double)d["musica"] : 0f,
				Exposicion = d.ContainsKey("exposicion") ? (float)(double)d["exposicion"] : (string)d["epoca"] == "presente" ? 0.78f : 1.15f,
				Silencio = d.ContainsKey("silencio") && (bool)d["silencio"],
				Ducto = d.ContainsKey("ducto") && (bool)d["ducto"],
				Pluma = d.ContainsKey("pluma") && (bool)d["pluma"],
				Huellas = d.ContainsKey("huellas") && (bool)d["huellas"],
			};
			if (d.ContainsKey("subtitulo"))
			{
				var s = d["subtitulo"].AsGodotDictionary();
				c.Subtitulo = ((int)s["linea"], (double)s["desde"], (double)s["hasta"]);
			}
			if (d.ContainsKey("luces"))
				foreach (var l in d["luces"].AsGodotArray())
					c.Luces.Add(CrearLuz(l.AsGodotDictionary()));
			if (d.ContainsKey("sonidos"))
				foreach (var s in d["sonidos"].AsGodotArray())
				{
					var son = s.AsGodotDictionary();
					string archivo = (string)son["archivo"];
					string ruta = archivo.StartsWith("Cinta/") ? Cinta + archivo["Cinta/".Length..] : "res://Assets/Audio/" + archivo;
					float vol = (float)(double)son["vol"];
					bool limpio = son.ContainsKey("limpio") && (bool)son["limpio"];
					_eventos.Add((t + (double)son["t"], () => Sonar(ruta, vol, limpio ? "Master" : BusCinta)));
				}
			_cuadros.Add(c);
			t += c.Dur;
		}
		_finPelicula = t + DuracionTitulo;
	}

	private Light3D CrearLuz(Godot.Collections.Dictionary l)
	{
		var color = V(l["color"].AsGodotArray());
		Light3D luz;
		if ((string)l["tipo"] == "spot")
			luz = new SpotLight3D { SpotAngle = (float)(double)l["angulo"], SpotRange = (float)(double)l["rango"], SpotAttenuation = 0.9f, ShadowEnabled = true };
		else
			luz = new OmniLight3D { OmniRange = (float)(double)l["rango"], OmniAttenuation = 1.2f };
		luz.LightColor = new Color(color.X, color.Y, color.Z);
		luz.LightEnergy = (float)(double)l["energia"];
		luz.Visible = false;
		_escena.AddChild(luz);
		var desde = V(l["desde"].AsGodotArray());
		luz.GlobalPosition = desde;
		if (l.ContainsKey("mira"))
		{
			var mira = V(l["mira"].AsGodotArray());
			var arriba = Mathf.Abs((mira - desde).Normalized().Dot(Vector3.Up)) > 0.98f ? Vector3.Forward : Vector3.Up;
			luz.LookAt(mira, arriba);
		}
		return luz;
	}

	private void CrearInterfazPelicula()
	{
		_tracking = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Escenas/Cinematicas/tracking.gdshader") } };
		_interfaz.AddChild(_tracking);
		_interfaz.MoveChild(_tracking, 2);
		Anclar(_tracking, 0, 0, 1, 1);
		_titulo = NuevaEtiqueta(Fuente("SpecialElite-Regular.ttf", 6), 64 * _escala, new Color(0.9f, 0.88f, 0.82f), HorizontalAlignment.Center);
		Anclar(_titulo, 0, 0.35f, 1, 0.65f);
		_titulo.Visible = false;
	}

	// ---------- Cada cuadro ----------

	private int CuadroPelicula(double t)
	{
		for (int i = _cuadros.Count - 1; i >= 0; i--) if (t >= _cuadros[i].Inicio) return i;
		return 0;
	}

	private void ActualizarPelicula()
	{
		int i = CuadroPelicula(_t);
		var c = _cuadros[i];
		_anim.Seek(c.K + 0.05, true);
		ObjetosPelicula(c);
		CamaraPelicula(c);
		for (int j = 0; j < _cuadros.Count; j++)
			foreach (var luz in _cuadros[j].Luces) luz.Visible = j == i && _t < _finPelicula - DuracionTitulo;
	}

	private void CamaraPelicula(Cuadro c)
	{
		int k = c.K;
		float s = Suave((_t - c.Inicio) / c.Dur);
		Transform3D a = _camIni[k].GlobalTransform, b = _camFin[k].GlobalTransform;
		var origen = a.Origin.Lerp(b.Origin, s);
		var giro = a.Basis.GetRotationQuaternion().Slerp(b.Basis.GetRotationQuaternion(), s);
		_camara.GlobalTransform = new Transform3D(new Basis(giro), origen);
		_camara.Fov = Mathf.Lerp(_camIni[k].Fov, _camFin[k].Fov, s);
		var foco = _foco[k].GlobalPosition;
		if (c.Mueve == "caida")
		{
			// La cámara sigue al reloj mientras cae; con el golpe tiembla un instante
			var reloj = _a.Reloj.GlobalPosition;
			_camara.LookAt(reloj, Vector3.Up);
			foco = reloj;
			double desde = _t - c.Inicio - c.Golpe;
			if (desde > 0 && desde < 0.35)
			{
				float f = (float)(1 - desde / 0.35) * 0.02f;
				_camara.GlobalPosition += new Vector3(Mathf.Sin((float)_t * 71) * f, Mathf.Sin((float)_t * 53) * f, 0);
			}
		}
		float enfoque = _camara.GlobalPosition.DistanceTo(foco);
		_enfoque.DofBlurFarEnabled = true;
		_enfoque.DofBlurFarDistance = enfoque + 0.4f;
		_enfoque.DofBlurFarTransition = Mathf.Max(0.5f, enfoque * 0.6f);
		// El pasado se expone un poco más: oscuro pero legible
		_enfoque.ExposureMultiplier = c.Exposicion;
	}

	private void ObjetosPelicula(Cuadro c)
	{
		int k = c.K;
		double u = _t - c.Inicio;
		_a.Reloj.GlobalTransform = _animReloj.GlobalTransform;
		if (_silla != null) _silla.GlobalTransform = _animSilla.GlobalTransform;
		if (_rejilla != null) _rejilla.GlobalTransform = _animRejilla.GlobalTransform;
		if (_puerta != null) _puerta.GlobalTransform = _animPuerta.GlobalTransform;
		if (_cinta != null) { _cinta.Visible = true; _cinta.GlobalTransform = _animCinta.GlobalTransform; }
		_figura.Visible = true;
		foreach (var (cuadro, nodo) in _soloEn) nodo.Visible = cuadro == k;
		if (_animPluma != null) _animPluma.Visible = false;

		// Los carretes giran cada vez más rápido (PLAY)
		foreach (var (cuadro, nodo, base_) in _carretes)
			if (cuadro == k) nodo.Transform = base_ * new Transform3D(new Basis(Vector3.Back, -(float)(u * 4.0 + u * u * 1.2)), Vector3.Zero);

		// La caída del reloj en cámara lenta: cae (con aceleración) y gira; en el golpe se congela con las astillas
		bool caida = c.Mueve == "caida" && _relojIni != null && _relojFin != null;
		if (caida)
		{
			float p = (float)Mathf.Clamp(u / c.Golpe, 0, 1);
			var ini = _relojIni.GlobalTransform;
			var fin = _relojFin.GlobalTransform;
			var giro = ini.Basis.GetRotationQuaternion().Slerp(fin.Basis.GetRotationQuaternion(), Suave(p));
			var vuelta = new Quaternion(Vector3.Up, Mathf.DegToRad(160f * p));
			_a.Reloj.GlobalTransform = new Transform3D(new Basis(vuelta * giro), ini.Origin.Lerp(fin.Origin, p * p));
		}
		if (_astillas != null) _astillas.Visible = caida && u >= c.Golpe;

		// El reloj: 3:09 hasta que se abre la rejilla; el golpe lo raja y lo para a las 3:12. En el presente,
		// colgado, rajado y parado.
		double golpe = GolpePelicula();
		if (_grietas != null) _grietas.Visible = c.Presente || _t >= golpe;
		float atraso = !c.Presente && k < 7 ? 3f : 0f;
		if (_minutero != null) _minutero.Transform = _minuteroAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(6f * atraso)) * _minuteroAntes.Basis };
		if (_horario != null) _horario.Transform = _horarioAntes with { Basis = new Basis(Vector3.Back, Mathf.DegToRad(0.5f * atraso)) * _horarioAntes.Basis };

		// REC parpadea en el pasado (la trampa del comisario); en el presente sigue encendida
		bool encendida = c.Presente || Mathf.PosMod((float)_t, 1f) < 0.6f;
		_luzRec.LightEnergy = encendida ? 0.12f : 0.02f;
		if (_luzRecMalla != null) _luzRecMalla.Visible = encendida;
		_recCara.LightEnergy = 0f;

		if (_contador != null)
		{
			double m = c.Presente ? 12 : k < 7 ? 9 : k < 13 ? 12 : 15;
			if (c.Mueve == "rebobina")
				m = c.Presente ? Mathf.Lerp(16.0, 12.0, Rampa(u, 0.2, 1.9)) : Mathf.Lerp(15.0, 12.0, Rampa(u, 0.9, 3.3));
			_contador.Text = $"03:{(int)m:00}";
		}

		_hueco.Visible = c.Ducto;
		_luzRejilla.LightEnergy = c.Ducto ? 10f : 0f;
		if (_pluma != null) _pluma.Visible = c.Pluma;
		if (_huellas != null) _huellas.Visible = c.Huellas || _huellasVisibles;
		_luzPasillo.LightEnergy = 0f;
	}

	private double GolpePelicula()
	{
		var c = _cuadros.FirstOrDefault(x => x.Mueve == "caida");
		return c == null ? double.MaxValue : c.Inicio + c.Golpe;
	}

	// ---------- Interfaz: negro, tracking, color por época y el título ----------

	private void InterfazPelicula()
	{
		int i = CuadroPelicula(_t);
		var c = _cuadros[i];
		double u = _t - c.Inicio;
		bool titulo = _t >= _finPelicula - DuracionTitulo;
		float negro = c.Entrada == "negro" ? 1f - Rampa(u, 0.0, 0.9) : 0f;
		if (titulo) negro = 1f;
		_negro.Color = new Color(0, 0, 0, Mathf.Max(negro, Rampa(_t, _finPelicula - 0.6, _finPelicula)));

		// Tracking solo al entrar y al salir de la cinta
		float tracking = c.Entrada is "match" or "seco" ? 1f - Rampa(u, 0.0, DuracionTracking) : 0f;
		if (i + 1 < _cuadros.Count && _cuadros[i + 1].Entrada == "seco") tracking = Mathf.Max(tracking, Rampa(u, c.Dur - 0.3, c.Dur));
		var mat = (ShaderMaterial)_tracking.Material;
		mat.SetShaderParameter("intensidad", tracking);
		mat.SetShaderParameter("tiempo", (float)_t);
		_tracking.Visible = tracking > 0.001f;

		// El pasado frío y desaturado, pero no gris; el presente con su color
		_recuerdo.SetShaderParameter("fuerza", c.Presente ? 0f : 0.62f);
		_recuerdo.SetShaderParameter("contraste", c.Presente ? 1.04f : 1.1f);
		_vineta.SetShaderParameter("fuerza", c.Presente ? 0.4f : 0.7f);

		_titulo.Visible = titulo;
		if (titulo)
		{
			double desde = _t - (_finPelicula - DuracionTitulo);
			_titulo.Text = Titulo;
			_titulo.VisibleCharacters = Mathf.Min(Titulo.Length, (int)(desde * 16));
			_titulo.Modulate = new Color(1, 1, 1, 1 - Rampa(_t, _finPelicula - 0.5, _finPelicula));
		}
	}

	private (double T, string Texto)[] HorasPelicula() =>
		_cuadros.Where(c => c.Hora != null).Select(c => (c.Inicio + (c.Entrada == "negro" ? 0.5 : 0.1), c.Hora)).ToArray();

	private (double Inicio, double Fin, int Linea)[] SubtitulosPelicula() =>
		_cuadros.Where(c => c.Subtitulo != null).Select(c => (c.Inicio + c.Subtitulo.Value.Desde, c.Inicio + c.Subtitulo.Value.Hasta, c.Subtitulo.Value.Linea)).ToArray();

	// ---------- Sonido ----------

	private void CrearEventosPelicula()
	{
		const string maquina = "res://Assets/Audio/Cinematica/";
		foreach (var (t, _) in HorasPelicula())
			_eventos.Add((t, () => Sonar(maquina + "maquina_4.wav", -14f, Sonidos.BusInterfaz)));
		_musica = new AudioStreamPlayer { Stream = ResourceLoader.Exists(Musica) ? GD.Load<AudioStream>(Musica) : null, Bus = "Master", VolumeDb = -80f };
		if (_musica.Stream is AudioStreamMP3 mp3) mp3.Loop = true;
		AddChild(_musica);
	}

	private void SonidosPelicula()
	{
		bool jugando = !Manual && !_terminada;
		int i = CuadroPelicula(_t);
		var c = _cuadros[i];
		bool cinta = !c.Presente && _t < _finPelicula - DuracionTitulo;
		Mantener(_siseo, jugando && cinta && !c.Silencio);
		if (_siseo.Stream != null) _siseo.VolumeDb = c.Silencio ? -18f : -3f;
		Mantener(_zumbido, jugando && !c.Silencio && _t < _finPelicula - DuracionTitulo);
		// El tic-tac del pasado, hasta el golpe
		Mantener(_tictac, jugando && cinta && _t < GolpePelicula());
		// Ronca mientras duerme; más lejos cuando la figura baja por la cuerda
		bool duerme = cinta && c.K >= 3 && c.K <= 11 && _t < GolpePelicula() - 0.05;
		Mantener(_ronquido, jugando && duerme);
		if (_ronquido.Stream != null) _ronquido.VolumeDb = c.K == 8 ? -15f : -7f;

		// Música: crece hasta la caída del reloj, silencio absoluto en la carátula y regresa al cierre
		if (_musica?.Stream != null)
		{
			float antes = i > 0 ? _cuadros[i - 1].Musica : 0f;
			float nivel = c.Silencio ? 0f : Mathf.Lerp(antes, c.Musica, Suave((_t - c.Inicio) / Math.Min(c.Dur, 2.0)));
			if (_t >= _finPelicula - DuracionTitulo) nivel = Mathf.Lerp(c.Musica, 0f, Rampa(_t, _finPelicula - 1.5, _finPelicula));
			bool suena = jugando && nivel > 0.01f;
			if (suena && !_musica.Playing) _musica.Play((float)Math.Max(0, _t - _cuadros[0].Dur));
			else if (!suena && _musica.Playing) _musica.Stop();
			_musica.VolumeDb = Mathf.LinearToDb(Mathf.Max(nivel, 0.0001f)) - 6f;
		}
		if (_voz.Playing && _t >= _cuadros[0].Dur) _voz.Stop();
	}
}
