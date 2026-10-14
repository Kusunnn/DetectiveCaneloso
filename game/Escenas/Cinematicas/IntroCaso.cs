using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Cinemática de inicio de capítulo: el expediente del caso sobre un escritorio noir.
// Lee el InformeCaso de res://hud/expediente/casos/caso_XX.tres y dibuja sus hojas con esos datos,
// así sirve para cualquier caso sin tocar el código. Si el caso no tiene expediente (Caso 0), termina al instante.
// La cámara, la carpeta y la lámpara van en un AnimationPlayer; los textos y sonidos dependen del tiempo,
// por eso adelantar con clic o Enter salta de toma en toma sin desordenar nada.
// Mantener [Espacio], [Esc] o B/Start la salta. El juego queda en pausa mientras dura.
public partial class IntroCaso : CanvasLayer
{
	[Signal] public delegate void TerminadaEventHandler();

	public const string Escena = "res://Escenas/Cinematicas/IntroCaso.tscn";
	private const string ArchivoVistas = "user://cinematicas.cfg";

	// Caso a presentar ("01", "02"...)
	[Export] public string Caso = "01";
	// Al terminar muestra «[TAB] Revisar el expediente cuando quieras» (no al volver a verla desde el expediente)
	[Export] public bool MostrarAviso = true;
	// Para las capturas: el tiempo no avanza solo, se fija con IrA
	public bool Manual { get; set; }

	public static string RutaInforme(string caso) => $"res://hud/expediente/casos/caso_{caso}.tres";
	// El tutorial (Caso 0) no tiene presentación por ahora, aunque algún día tenga expediente
	public const string CasoSinPresentacion = "00";
	public static bool Disponible(string caso) => caso != CasoSinPresentacion && ResourceLoader.Exists(RutaInforme(caso));

	public static bool Vista(string caso)
	{
		var cfg = new ConfigFile();
		return cfg.Load(ArchivoVistas) == Error.Ok && cfg.GetValue("vistas", caso, false).AsBool();
	}

	public static void MarcarVista(string caso)
	{
		var cfg = new ConfigFile();
		cfg.Load(ArchivoVistas);
		cfg.SetValue("vistas", caso, true);
		cfg.Save(ArchivoVistas);
	}

	// La pone encima de todo (en la raíz) y empieza sola
	public static IntroCaso Reproducir(Node cualquiera, string caso, bool aviso)
	{
		var intro = GD.Load<PackedScene>(Escena).Instantiate<IntroCaso>();
		intro.Caso = caso;
		intro.MostrarAviso = aviso;
		cualquiera.GetTree().Root.AddChild(intro);
		return intro;
	}

	// ---------- Tiempos (segundos) ----------

	private const double TLuz = 6.0;           // se enciende la lámpara
	private const double TCarpeta = 7.5;       // la carpeta entra deslizándose
	private const double TSello = 9.6;         // cae el sello
	private const double TAbrir = 12.0;        // se abre la carpeta
	private const double TObjetivo = 44.3;
	private const double TNegro = 49.5;        // fundido a la tarjeta del capítulo
	private const double TTarjeta = 50.3;
	private const double TFin = 55.4;
	private const double SegundosParaSaltar = 1.2;

	private static readonly double[] Paradas = { TLuz, 9.0, 13.5, 17.7, 21.9, 26.1, 30.3, 35.3, 39.5, 43.7, TTarjeta, TFin };

	// Frases cortas de Caneloso: no dicen nada del caso, sirven para cualquiera
	private static readonly (double Inicio, double Fin, string Texto)[] Frases =
	{
		(10.0, 11.8, "Otra noche. Otro expediente."),
		(14.5, 17.5, "Todo caso empieza con un nombre."),
		(18.7, 21.7, "Un lugar y una hora. Ahí empieza todo."),
		(22.9, 25.9, "Las fotos no mienten. La gente, a veces, sí."),
		(27.1, 30.1, "Alguien perdió la vida. Alguien sabe por qué."),
		(31.4, 35.0, "La verdad se esconde entre las horas."),
		(36.3, 39.3, "Todos estaban ahí. Cada uno calla algo."),
		(40.5, 43.5, "El pasado nunca se queda enterrado."),
	};

	// ---------- Hojas del expediente ----------

	private const float AnchoHoja = 0.216f, AltoHoja = 0.279f;
	private static readonly Vector2I TamHoja = new(1100, 1420);
	private static readonly Color Papel = new("#EFE9DA");
	private static readonly Color Tinta = new("#211D17");
	private static readonly Color Tinta2 = new("#3D372E");
	private static readonly Color Rojo = new("#8C231C");
	private static readonly Color Ambar = new("#EFCA08");

	private Resource _informe;
	private double _t;
	private bool _terminada;
	private bool _saltando;
	private double _relojSalto;
	private double _mantener;
	private double _relojFinal;
	private bool _estabaPausado;
	private Input.MouseModeEnum _ratonAntes;
	private readonly HashSet<string> _completos = new();
	private readonly List<(double T, Action Accion)> _eventos = new();
	private int _siguienteEvento;

	private Node3D _escritorio;
	private AnimationPlayer _anim;
	private Node3D _rig;
	private Camera3D _camara;
	private SpotLight3D _luzLampara;
	private StandardMaterial3D _materialFoco;
	private Vector3 _sacudida;
	private double _relojSacudida = 10;
	private SubViewportContainer _mundo;

	private Control _interfaz;
	private ColorRect _negro;
	private Control _franjaArriba, _franjaAbajo;
	private Label _archivo, _fecha, _subtitulo, _objetivoTitulo, _objetivoTexto, _tarjetaEtiqueta, _tarjetaTitulo, _pista, _aviso;
	private Control _objetivoFondo, _tarjetaLinea, _avisoFondo;
	private CirculoSalto _circulo;
	private AudioStreamPlayer _musica, _lluvia, _reloj;
	private float _escala = 1f;

	public override void _Ready()
	{
		Layer = 120;
		ProcessMode = ProcessModeEnum.Always;
		if (!Disponible(Caso))
		{
			// Sin expediente no hay cinemática
			CallDeferred(MethodName.Terminar);
			return;
		}
		_informe = GD.Load<Resource>(RutaInforme(Caso));
		_estabaPausado = GetTree().Paused;
		GetTree().Paused = true;
		_ratonAntes = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		_escala = GetViewport().GetVisibleRect().Size.Y / 1080f;

		_mundo = GetNode<SubViewportContainer>("Mundo");
		_escritorio = GetNode<Node3D>("Mundo/Vista/Escritorio");
		_rig = _escritorio.GetNode<Node3D>("Rig");
		_camara = _rig.GetNode<Camera3D>("Camara");
		_camara.Attributes = (CameraAttributes)_camara.Attributes.Duplicate();
		_luzLampara = _escritorio.GetNode<SpotLight3D>("LuzLampara");
		_escritorio.GetNode<SpotLight3D>("LuzPersiana").LightProjector = TexturaPersiana();
		_materialFoco = (StandardMaterial3D)((StandardMaterial3D)((SphereMesh)_escritorio.GetNode<MeshInstance3D>("Lampara/Foco").Mesh).Material).Duplicate();
		_escritorio.GetNode<MeshInstance3D>("Lampara/Foco").MaterialOverride = _materialFoco;
		_escritorio.GetNode<Label3D>("Carpeta/Tapa/Archivo").Text = "ARCHIVO " + Caso.PadLeft(3, '0');
		var sello = _escritorio.GetNode<Label3D>("Carpeta/Tapa/Sello");
		sello.Font = Fuente("BigShouldersDisplay-Variable.ttf", 900, 4);
		sello.Text = Texto("sello") == "" ? "CONFIDENCIAL" : Texto("sello");
		DejarSoloLibreta();
		VidrioTransparente(_escritorio.GetNode("Lupa"));

		CrearInterfaz();
		CrearAudio();
		CrearAnimacion();
		CrearEventos();
		_ = PrepararHojas();
		IrA(0);
	}

	// ---------- Tiempo ----------

	public override void _Process(double delta)
	{
		if (_informe == null) return;
		if (_terminada)
		{
			ProcesarFinal(delta);
			return;
		}

		ProcesarSalto(delta);
		if (_saltando)
		{
			double a = Mathf.Clamp((_relojSalto += delta) / 0.35, 0, 1);
			_negro.Color = new Color(0, 0, 0, Mathf.Max(_negro.Color.A, (float)a));
			_musica.VolumeDb = Mathf.Lerp(_musica.VolumeDb, -60f, (float)a * 0.2f);
			if (a >= 1) Terminar();
			return;
		}

		if (!Manual) _t += delta;
		Actualizar(false);
		if (_t >= TFin && !Manual) Terminar();
	}

	// Lleva todo al segundo t (las capturas lo usan para ir a una toma)
	public void IrA(double t)
	{
		_t = t;
		Actualizar(true);
	}

	private void Actualizar(bool silencio)
	{
		_anim.Seek(_t, true);
		while (_siguienteEvento < _eventos.Count && _eventos[_siguienteEvento].T <= _t)
		{
			if (!silencio) _eventos[_siguienteEvento].Accion();
			_siguienteEvento++;
		}
		if (silencio) SincronizarAudio();
		ActualizarCamara();
		ActualizarInterfaz();
		_materialFoco.EmissionEnergyMultiplier = _luzLampara.LightEnergy * 2.5f;
	}

	private void ActualizarCamara()
	{
		// Balanceo de cámara en mano: pequeño, y más fino en los acercamientos
		float tiempo = (float)(Time.GetTicksMsec() / 1000.0);
		float foco = _camara.Attributes is CameraAttributesPractical p ? p.DofBlurFarDistance / 1.25f : 1f;
		float amplitud = Mathf.Clamp(foco * 0.004f, 0.0008f, 0.005f);
		var balanceo = new Vector3(
			Mathf.Sin(tiempo * 0.73f) + 0.5f * Mathf.Sin(tiempo * 1.91f + 1.3f),
			Mathf.Sin(tiempo * 0.61f + 2.1f) + 0.4f * Mathf.Sin(tiempo * 1.37f),
			0) * amplitud;
		_relojSacudida += GetProcessDeltaTime();
		float golpe = (float)Mathf.Max(0, 1 - _relojSacudida / 0.35) * 0.006f;
		var sacudida = new Vector3(Mathf.Sin(tiempo * 61f), Mathf.Sin(tiempo * 47f + 1f), 0) * golpe;
		_camara.Position = balanceo + sacudida;
		_camara.Rotation = new Vector3(
			Mathf.Sin(tiempo * 0.53f + 0.7f) * 0.0018f,
			Mathf.Sin(tiempo * 0.47f) * 0.0022f,
			Mathf.Sin(tiempo * 0.31f + 1.1f) * 0.0012f);
	}

	// ---------- Entrada ----------

	public override void _Input(InputEvent evento)
	{
		if (_informe == null || _terminada) return;
		bool avanzar =
			(evento is InputEventMouseButton raton && raton.Pressed && raton.ButtonIndex == MouseButton.Left) ||
			(evento is InputEventKey tecla && tecla.Pressed && !tecla.Echo && (tecla.Keycode == Key.Enter || tecla.Keycode == Key.KpEnter)) ||
			(evento is InputEventJoypadButton boton && boton.Pressed && boton.ButtonIndex == JoyButton.A);
		if (avanzar && !_saltando) Avanzar();
		// La cinemática se queda con toda la entrada: el juego de abajo no debe enterarse
		if (evento is InputEventKey || evento is InputEventMouseButton || evento is InputEventJoypadButton)
			GetViewport().SetInputAsHandled();
	}

	private bool TeclaSaltar() =>
		Input.IsKeyPressed(Key.Space) || Input.IsKeyPressed(Key.Escape) ||
		Input.IsJoyButtonPressed(0, JoyButton.B) || Input.IsJoyButtonPressed(0, JoyButton.Start);

	private void ProcesarSalto(double delta)
	{
		if (Manual || _saltando) return;
		_mantener = TeclaSaltar() ? _mantener + delta : Mathf.Max(0, _mantener - delta * 3);
		_circulo.Progreso = (float)(_mantener / SegundosParaSaltar);
		_circulo.QueueRedraw();
		if (_mantener >= SegundosParaSaltar)
		{
			_saltando = true;
			_relojSalto = 0;
		}
	}

	// Clic o Enter: termina de escribir el texto en curso; si ya está completo, pasa a la siguiente toma
	private void Avanzar()
	{
		bool escribiendo = false;
		foreach (var (clave, etiqueta) in TextosActivos())
		{
			if (etiqueta.VisibleCharacters >= 0 && etiqueta.VisibleCharacters < etiqueta.GetTotalCharacterCount())
			{
				_completos.Add(clave);
				escribiendo = true;
			}
		}
		if (escribiendo) return;
		double siguiente = Paradas.FirstOrDefault(p => p > _t + 0.05, TFin);
		if (siguiente >= TFin) siguiente = TFin - 0.01;
		_t = siguiente;
		Actualizar(true);
	}

	private IEnumerable<(string, Label)> TextosActivos()
	{
		if (_archivo.Visible) { yield return ("archivo", _archivo); yield return ("fecha", _fecha); }
		if (_subtitulo.Visible) yield return ("frase" + FraseActual(), _subtitulo);
		if (_objetivoTexto.Visible) yield return ("objetivo", _objetivoTexto);
		if (_tarjetaEtiqueta.Visible) yield return ("tarjeta", _tarjetaEtiqueta);
	}

	// ---------- Final ----------

	private void Terminar()
	{
		if (_terminada) return;
		_terminada = true;
		_relojFinal = 0;
		if (_informe == null)
		{
			EmitSignal(SignalName.Terminada);
			QueueFree();
			return;
		}
		MarcarVista(Caso);
		GetTree().Paused = _estabaPausado;
		Input.MouseMode = _ratonAntes;
		_negro.Color = Colors.Black;
		_tarjetaEtiqueta.Visible = _tarjetaTitulo.Visible = _tarjetaLinea.Visible = false;
		_subtitulo.Visible = _objetivoFondo.Visible = false;
		_circulo.Visible = _pista.Visible = false;
		_franjaArriba.Visible = _franjaAbajo.Visible = false;
		// El escritorio ya no se ve: deja de dibujarse
		_mundo.Visible = false;
		_mundo.GetNode<SubViewport>("Vista").RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
		EmitSignal(SignalName.Terminada);
	}

	// Fundido de vuelta al juego y aviso del expediente
	private void ProcesarFinal(double delta)
	{
		_relojFinal += delta;
		_negro.Color = new Color(0, 0, 0, 1 - (float)Mathf.Clamp(_relojFinal / 1.0, 0, 1));
		foreach (var reproductor in new[] { _musica, _lluvia, _reloj })
			if (reproductor.Playing) reproductor.VolumeDb = Mathf.MoveToward(reproductor.VolumeDb, -60f, (float)delta * 40f);
		if (MostrarAviso)
		{
			float a = Desvanecer(_relojFinal, 0.8, 1.2, 5.0, 5.8);
			_avisoFondo.Visible = a > 0;
			_avisoFondo.Modulate = new Color(1, 1, 1, a);
		}
		if (_relojFinal > (MostrarAviso ? 6.0 : 1.2)) QueueFree();
	}

	// ---------- Interfaz ----------

	private Font Fuente(string archivo, int peso = 0, int espaciado = 0)
	{
		Font fuente = GD.Load<FontFile>("res://Assets/Fuentes/" + archivo);
		if (peso == 0 && espaciado == 0) return fuente;
		var variacion = new FontVariation { BaseFont = fuente, SpacingGlyph = espaciado };
		if (peso > 0)
			variacion.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("weight"), peso } };
		return variacion;
	}

	private Label NuevaEtiqueta(Control padre, string texto, Font fuente, float tam, Color color, HorizontalAlignment alineacion = HorizontalAlignment.Center)
	{
		var etiqueta = new Label
		{
			Text = texto,
			HorizontalAlignment = alineacion,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		etiqueta.AddThemeFontOverride("font", fuente);
		etiqueta.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(tam));
		etiqueta.AddThemeColorOverride("font_color", color);
		padre.AddChild(etiqueta);
		return etiqueta;
	}

	private static void Anclar(Control control, float izq, float arriba, float der, float abajo)
	{
		control.AnchorLeft = izq;
		control.AnchorTop = arriba;
		control.AnchorRight = der;
		control.AnchorBottom = abajo;
		control.OffsetLeft = control.OffsetTop = control.OffsetRight = control.OffsetBottom = 0;
	}

	private void CrearInterfaz()
	{
		float e = _escala;
		_interfaz = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(_interfaz);
		Anclar(_interfaz, 0, 0, 1, 1);

		// Grano y viñeta sobre la imagen
		var vineta = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Escenas/Cinematicas/vineta.gdshader") } };
		_interfaz.AddChild(vineta);
		Anclar(vineta, 0, 0, 1, 1);
		var grano = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Escenas/Menus/Fondo/grano_pelicula.gdshader") } };
		((ShaderMaterial)grano.Material).SetShaderParameter("intensidad", 0.07f);
		_interfaz.AddChild(grano);
		Anclar(grano, 0, 0, 1, 1);

		// Franjas de cine
		_franjaArriba = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_franjaAbajo = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_franjaArriba);
		_interfaz.AddChild(_franjaAbajo);
		Anclar(_franjaArriba, 0, 0, 1, 0.11f);
		Anclar(_franjaAbajo, 0, 0.89f, 1, 1);

		var maquina = Fuente("SpecialElite-Regular.ttf");
		var courier = Fuente("CourierPrime-Regular.ttf", 0, 2);
		var titulos = Fuente("BigShouldersDisplay-Variable.ttf", 900, 2);

		_subtitulo = NuevaEtiqueta(_interfaz, "", Fuente("CourierPrime-Regular.ttf"), 30 * e, new Color(0.92f, 0.9f, 0.84f));
		Anclar(_subtitulo, 0.1f, 0.89f, 0.9f, 1);

		// Objetivo, al final, sobre la carpeta
		_objetivoFondo = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_objetivoFondo);
		Anclar(_objetivoFondo, 0, 0.11f, 1, 0.89f);
		var sombra = new TextureRect
		{
			Texture = new GradientTexture2D
			{
				Gradient = new Gradient { Colors = new[] { new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.82f) }, Offsets = new[] { 0f, 0.55f } },
				FillFrom = new Vector2(0, 0), FillTo = new Vector2(0, 1),
			},
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_objetivoFondo.AddChild(sombra);
		Anclar(sombra, 0, 0.35f, 1, 1);
		_objetivoTitulo = NuevaEtiqueta(_objetivoFondo, "OBJETIVO", Fuente("BigShouldersDisplay-Variable.ttf", 800, 6), 34 * e, new Color(0.88f, 0.27f, 0.23f));
		Anclar(_objetivoTitulo, 0.15f, 0.56f, 0.85f, 0.66f);
		_objetivoTexto = NuevaEtiqueta(_objetivoFondo, Texto("objetivo"), maquina, 38 * e, new Color(0.95f, 0.93f, 0.87f));
		Anclar(_objetivoTexto, 0.15f, 0.66f, 0.85f, 0.95f);
		_objetivoTexto.VerticalAlignment = VerticalAlignment.Top;

		// Archivo y fecha a máquina, sobre negro
		_negro = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_negro);
		Anclar(_negro, 0, 0, 1, 1);
		_archivo = NuevaEtiqueta(_interfaz, "ARCHIVO " + Caso.PadLeft(3, '0'), Fuente("SpecialElite-Regular.ttf", 0, 6), 64 * e, new Color(0.9f, 0.88f, 0.82f));
		Anclar(_archivo, 0, 0.38f, 1, 0.5f);
		_fecha = NuevaEtiqueta(_interfaz, Texto("fecha"), maquina, 32 * e, new Color(0.62f, 0.6f, 0.56f));
		Anclar(_fecha, 0.1f, 0.5f, 0.9f, 0.58f);

		// Tarjeta del capítulo
		var (etiqueta, titulo) = TituloCapitulo();
		_tarjetaEtiqueta = NuevaEtiqueta(_interfaz, etiqueta.ToUpper(), Fuente("CourierPrime-Regular.ttf", 0, 10), 26 * e, new Color(0.62f, 0.6f, 0.56f));
		Anclar(_tarjetaEtiqueta, 0, 0.36f, 1, 0.43f);
		_tarjetaTitulo = NuevaEtiqueta(_interfaz, titulo, titulos, 96 * e, new Color(0.95f, 0.93f, 0.88f));
		Anclar(_tarjetaTitulo, 0.05f, 0.43f, 0.95f, 0.6f);
		_tarjetaLinea = new ColorRect { Color = new Color(0.82f, 0.22f, 0.18f), MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_tarjetaLinea);
		Anclar(_tarjetaLinea, 0.5f, 0.615f, 0.5f, 0.615f);
		_tarjetaLinea.OffsetBottom = 3 * e;

		// Mantener para saltar
		_circulo = new CirculoSalto { MouseFilter = Control.MouseFilterEnum.Ignore };
		_interfaz.AddChild(_circulo);
		Anclar(_circulo, 1, 1, 1, 1);
		_circulo.OffsetLeft = -86 * e; _circulo.OffsetTop = -86 * e; _circulo.OffsetRight = -34 * e; _circulo.OffsetBottom = -34 * e;
		bool mando = Configuracion.Instancia?.UsandoMando ?? false;
		_pista = NuevaEtiqueta(_interfaz, mando ? "Mantén [B] para saltar" : "Mantén [Espacio] para saltar", courier, 22 * e, new Color(0.72f, 0.7f, 0.66f), HorizontalAlignment.Right);
		Anclar(_pista, 1, 1, 1, 1);
		_pista.OffsetLeft = -700 * e; _pista.OffsetTop = -78 * e; _pista.OffsetRight = -100 * e; _pista.OffsetBottom = -42 * e;

		// Aviso al volver al juego
		_avisoFondo = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		var estilo = new StyleBoxFlat { BgColor = new Color(0.03f, 0.035f, 0.045f, 0.82f), ContentMarginLeft = 28 * e, ContentMarginRight = 28 * e, ContentMarginTop = 12 * e, ContentMarginBottom = 12 * e };
		estilo.SetCornerRadiusAll(Mathf.RoundToInt(6 * e));
		estilo.BorderWidthLeft = Mathf.RoundToInt(3 * e);
		estilo.BorderColor = Ambar;
		_avisoFondo.AddThemeStyleboxOverride("panel", estilo);
		_interfaz.AddChild(_avisoFondo);
		Anclar(_avisoFondo, 0.5f, 0.14f, 0.5f, 0.14f);
		_aviso = NuevaEtiqueta(_avisoFondo, (mando ? "[LB]" : "[TAB]") + "  Revisar el expediente cuando quieras", Fuente("CourierPrime-Regular.ttf", 0, 1), 24 * e, new Color(0.92f, 0.9f, 0.84f));
		_aviso.AutowrapMode = TextServer.AutowrapMode.Off;
		_avisoFondo.GrowHorizontal = Control.GrowDirection.Both;
		_avisoFondo.GrowVertical = Control.GrowDirection.Both;
	}

	private (string Etiqueta, string Titulo) TituloCapitulo()
	{
		var capitulo = Capitulos.Lista.FirstOrDefault(c => c.Numero.ToString("00") == Caso);
		if (capitulo != null) return (capitulo.Etiqueta, capitulo.Titulo);
		string titulo = Texto("titulo").Trim('"', '«', '»', ' ');
		return ("Caso " + Caso, titulo.Length > 0 ? char.ToUpper(titulo[0]) + titulo[1..].ToLower() : "");
	}

	private string Texto(string campo) => _informe?.Get(campo).AsString() ?? "";
	private string[] Lista(string campo) => _informe?.Get(campo).AsStringArray() ?? Array.Empty<string>();

	private static float Rampa(double t, double desde, double hasta) => (float)Mathf.Clamp((t - desde) / (hasta - desde), 0, 1);

	// 0 → 1 entre entra y llena, 1 → 0 entre vacia y sale
	private static float Desvanecer(double t, double entra, double llena, double vacia, double sale) =>
		Mathf.Min(Rampa(t, entra, llena), 1 - Rampa(t, vacia, sale));

	private void Escribir(Label etiqueta, string clave, double inicio, double letrasPorSegundo)
	{
		int total = etiqueta.GetTotalCharacterCount();
		int letras = _completos.Contains(clave) ? total : Mathf.Clamp((int)((_t - inicio) * letrasPorSegundo), 0, total);
		etiqueta.VisibleCharacters = letras >= total ? -1 : letras;
	}

	private int FraseActual()
	{
		for (int i = 0; i < Frases.Length; i++)
			if (_t >= Frases[i].Inicio - 0.2 && _t <= Frases[i].Fin + 0.3) return i;
		return -1;
	}

	private void ActualizarInterfaz()
	{
		double t = _t;
		// Negro: abre con la lámpara y vuelve para la tarjeta
		float negro = 1 - Rampa(t, TLuz, TLuz + 1.6);
		negro = Mathf.Max(negro, Rampa(t, TNegro, TTarjeta - 0.1));
		_negro.Color = new Color(0, 0, 0, negro);

		float archivo = Desvanecer(t, 0.5, 0.7, 5.2, 5.7);
		_archivo.Visible = _fecha.Visible = archivo > 0;
		_archivo.Modulate = _fecha.Modulate = new Color(1, 1, 1, archivo);
		Escribir(_archivo, "archivo", 0.8, 9);
		Escribir(_fecha, "fecha", 2.6, 20);

		int i = FraseActual();
		_subtitulo.Visible = i >= 0;
		if (i >= 0)
		{
			var (inicio, fin, texto) = Frases[i];
			_subtitulo.Text = "«" + texto + "»";
			_subtitulo.Modulate = new Color(1, 1, 1, Desvanecer(t, inicio - 0.2, inicio, fin, fin + 0.3));
			Escribir(_subtitulo, "frase" + i, inicio, 34);
		}

		float objetivo = Desvanecer(t, TObjetivo, TObjetivo + 0.6, TNegro - 0.4, TNegro + 0.2);
		_objetivoFondo.Visible = objetivo > 0 && _objetivoTexto.Text != "";
		_objetivoFondo.Modulate = new Color(1, 1, 1, objetivo);
		_objetivoTexto.Visible = _objetivoFondo.Visible;
		Escribir(_objetivoTexto, "objetivo", TObjetivo + 0.5, 42);

		float tarjeta = Desvanecer(t, TTarjeta, TTarjeta + 0.2, TFin - 0.7, TFin - 0.15);
		_tarjetaEtiqueta.Visible = _tarjetaTitulo.Visible = _tarjetaLinea.Visible = tarjeta > 0;
		_tarjetaEtiqueta.Modulate = new Color(1, 1, 1, tarjeta);
		Escribir(_tarjetaEtiqueta, "tarjeta", TTarjeta + 0.2, 12);
		float titulo = Mathf.Min(Rampa(t, TTarjeta + 1.0, TTarjeta + 1.8), tarjeta);
		_tarjetaTitulo.Modulate = new Color(1, 1, 1, titulo);
		_tarjetaTitulo.PivotOffset = _tarjetaTitulo.Size / 2;
		_tarjetaTitulo.Scale = Vector2.One * (1.04f - 0.04f * Mathf.Ease(titulo, 0.4f));
		float ancho = 260 * _escala * Mathf.Ease(Rampa(t, TTarjeta + 1.4, TTarjeta + 2.4), 0.3f);
		_tarjetaLinea.OffsetLeft = -ancho;
		_tarjetaLinea.OffsetRight = ancho;
		_tarjetaLinea.Modulate = new Color(1, 1, 1, tarjeta);

		float pista = Desvanecer(t, 1.0, 1.6, 7.0, 8.0);
		_pista.Modulate = new Color(1, 1, 1, Mathf.Max(pista, Mathf.Clamp((float)_mantener * 4, 0, 1)));
		_circulo.Modulate = _pista.Modulate;
	}

	// ---------- Escritorio ----------

	// De la colección de libretas de Poly Haven solo se queda el block amarillo y las notas adhesivas
	private void DejarSoloLibreta()
	{
		var libretas = _escritorio.GetNode<Node3D>("Libretas");
		foreach (var malla in libretas.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>())
		{
			string nombre = malla.Name.ToString();
			bool queda = nombre == "office_notepads_yellow_pad" || nombre == "office_notepads_sticky_stack";
			malla.Visible = queda;
			if (!queda) continue;
			var global = malla.GlobalTransform;
			malla.GlobalTransform = new Transform3D(global.Basis, libretas.GlobalPosition + (nombre.Contains("sticky") ? libretas.GlobalBasis * new Vector3(0.15f, 0, -0.09f) : Vector3.Zero));
		}
	}

	// El vidrio de la lupa de Poly Haven llega opaco: se cambia por uno transparente
	private static void VidrioTransparente(Node modelo)
	{
		foreach (var malla in modelo.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>())
		{
			for (int i = 0; i < malla.Mesh.GetSurfaceCount(); i++)
			{
				var material = malla.Mesh.SurfaceGetMaterial(i);
				if (material == null || !material.ResourceName.ToLower().Contains("lens")) continue;
				malla.SetSurfaceOverrideMaterial(i, new StandardMaterial3D
				{
					Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
					AlbedoColor = new Color(0.85f, 0.9f, 0.95f, 0.12f),
					Metallic = 0f,
					Roughness = 0.04f,
					MetallicSpecular = 0.9f,
				});
			}
		}
	}

	// Sombra de persiana: tiras claras y oscuras con borde suave
	private static Texture2D TexturaPersiana()
	{
		var imagen = Image.CreateEmpty(256, 256, false, Image.Format.Rgba8);
		for (int y = 0; y < 256; y++)
		{
			float fase = (y % 32) / 32f;
			float luz = Mathf.SmoothStep(0.08f, 0.16f, fase) * (1 - Mathf.SmoothStep(0.66f, 0.74f, fase));
			for (int x = 0; x < 256; x++)
			{
				float borde = Mathf.SmoothStep(0f, 0.12f, x / 256f) * Mathf.SmoothStep(1f, 0.88f, x / 256f);
				float v = luz * borde;
				imagen.SetPixel(x, y, new Color(v, v, v, 1));
			}
		}
		return ImageTexture.CreateFromImage(imagen);
	}

	private static Vector3 PuntoHoja(bool izquierda, float u, float v)
	{
		var centro = new Vector3(izquierda ? -0.1175f : 0.1175f, 0.5525f, 0.02f);
		return centro + new Vector3((u - 0.5f) * AnchoHoja, 0, (v - 0.5f) * AltoHoja);
	}

	private record struct Plano(Vector3 Pos, Vector3 Mira, float Fov);

	private static Plano Cerca(bool izquierda, float u, float v, Vector3 desplazamiento, float fov = 34)
	{
		var punto = PuntoHoja(izquierda, u, v);
		return new Plano(punto + desplazamiento, punto, fov);
	}

	private void CrearAnimacion()
	{
		_anim = new AnimationPlayer { Name = "Animacion" };
		_escritorio.AddChild(_anim);
		var animacion = new Animation { Length = (float)TFin + 0.5f };

		// Tomas: (inicio, segundos de movimiento, plano al llegar, plano al terminar la toma)
		var tomas = new List<(double Inicio, double Mover, Plano Llega, Plano Sale)>
		{
			(0.0, 0.0, new(new(0.55f, 1.05f, 1.05f), new(0f, 0.6f, 0f), 42), new(new(0.38f, 0.98f, 0.86f), new(0.05f, 0.58f, 0f), 42)),
			(9.0, 0.55, new(new(0.13f, 0.86f, 0.40f), new(0.115f, 0.55f, 0.03f), 38), new(new(0.12f, 0.84f, 0.37f), new(0.115f, 0.55f, 0.03f), 38)),
			(11.8, 0.9, new(new(0f, 0.98f, 0.36f), new(0f, 0.55f, 0.03f), 40), new(new(0f, 0.96f, 0.34f), new(0f, 0.55f, 0.03f), 40)),
			(13.5, 1.1, Cerca(true, 0.5f, 0.16f, new(-0.042f, 0.210f, 0.140f)), Cerca(true, 0.5f, 0.16f, new(0.000f, 0.196f, 0.133f))),
			(17.7, 1.0, Cerca(true, 0.5f, 0.32f, new(0.028f, 0.210f, 0.140f)), Cerca(true, 0.5f, 0.32f, new(-0.014f, 0.196f, 0.133f))),
			(21.9, 1.0, Cerca(true, 0.5f, 0.61f, new(-0.028f, 0.238f, 0.154f), 36), Cerca(true, 0.5f, 0.61f, new(0.014f, 0.210f, 0.133f), 36)),
			(26.1, 1.0, Cerca(true, 0.5f, 0.90f, new(0.028f, 0.210f, 0.140f)), Cerca(true, 0.5f, 0.90f, new(0.000f, 0.196f, 0.126f))),
			(30.3, 1.2, Cerca(false, 0.5f, 0.17f, new(0.000f, 0.224f, 0.140f)), Cerca(false, 0.5f, 0.36f, new(0.000f, 0.224f, 0.140f))),
			(35.3, 1.0, Cerca(false, 0.5f, 0.63f, new(-0.028f, 0.224f, 0.140f)), Cerca(false, 0.5f, 0.63f, new(0.028f, 0.210f, 0.133f))),
			(39.5, 1.0, Cerca(false, 0.5f, 0.88f, new(0.014f, 0.210f, 0.140f)), Cerca(false, 0.5f, 0.88f, new(-0.014f, 0.196f, 0.133f))),
			(43.7, 1.6, new(new(0.05f, 1.02f, 0.55f), new(0f, 0.55f, 0.02f), 40), new(new(0.1f, 1.12f, 0.75f), new(0f, 0.56f, 0.02f), 40)),
		};
		int pos = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(pos, "Rig:position");
		int rot = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(rot, "Rig:quaternion");
		int fov = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(fov, "Rig/Camara:fov");
		int lejos = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(lejos, "Rig/Camara:attributes:dof_blur_far_distance");
		int cerca = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(cerca, "Rig/Camara:attributes:dof_blur_near_distance");

		void Clave(double t, Plano p, float transicion)
		{
			float foco = p.Pos.DistanceTo(p.Mira);
			animacion.TrackInsertKey(pos, t, p.Pos, transicion);
			animacion.TrackInsertKey(rot, t, Basis.LookingAt(p.Mira - p.Pos, Vector3.Up).GetRotationQuaternion(), transicion);
			animacion.TrackInsertKey(fov, t, p.Fov, transicion);
			animacion.TrackInsertKey(lejos, t, foco * 1.25f, transicion);
			animacion.TrackInsertKey(cerca, t, foco * 0.72f, transicion);
		}
		for (int i = 0; i < tomas.Count; i++)
		{
			var toma = tomas[i];
			double fin = i + 1 < tomas.Count ? tomas[i + 1].Inicio : TNegro + 0.8;
			// La deriva dentro de la toma es pareja; el viaje a la siguiente acelera y frena suave
			Clave(toma.Inicio + toma.Mover, toma.Llega, 1f);
			Clave(fin, toma.Sale, -2f);
		}

		// Lámpara: parpadea al encenderse
		int luz = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(luz, "LuzLampara:light_energy");
		foreach (var (t, energia) in new[] { (0.0, 0f), (TLuz + 0.35, 0f), (TLuz + 0.4, 2.6f), (TLuz + 0.46, 0.5f), (TLuz + 0.55, 2.6f), (TLuz + 0.63, 1.2f), (TLuz + 0.72, 2.4f) })
			animacion.TrackInsertKey(luz, t, energia, 1f);

		// Carpeta: entra deslizándose y gira hasta quedar derecha
		int carpeta = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(carpeta, "Carpeta:position");
		int giro = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(giro, "Carpeta:rotation");
		var fuera = new Vector3(0.95f, 0.5495f, 0.2f);
		var dentro = new Vector3(0f, 0.5495f, 0.02f);
		animacion.TrackInsertKey(carpeta, 0, fuera, 1f);
		animacion.TrackInsertKey(carpeta, TCarpeta, fuera, 0.35f);
		animacion.TrackInsertKey(carpeta, TCarpeta + 1.2, dentro, 1f);
		animacion.TrackInsertKey(giro, 0, new Vector3(0, 0.5f, 0), 1f);
		animacion.TrackInsertKey(giro, TCarpeta, new Vector3(0, 0.5f, 0), 0.35f);
		animacion.TrackInsertKey(giro, TCarpeta + 1.2, new Vector3(0, 0.03f, 0), 1f);

		// Sello: cae con fuerza
		int sello = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(sello, "Carpeta/Tapa/Sello:scale");
		int tinta = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(tinta, "Carpeta/Tapa/Sello:modulate");
		var rojo = new Color(0.72f, 0.13f, 0.11f, 0.85f);
		animacion.TrackInsertKey(sello, 0, Vector3.One * 1.6f, 1f);
		animacion.TrackInsertKey(sello, TSello - 0.12, Vector3.One * 1.6f, 2.5f);
		animacion.TrackInsertKey(sello, TSello, Vector3.One, 1f);
		animacion.TrackInsertKey(tinta, 0, rojo with { A = 0 }, 1f);
		animacion.TrackInsertKey(tinta, TSello - 0.12, rojo with { A = 0 }, 1f);
		animacion.TrackInsertKey(tinta, TSello, rojo, 1f);

		// Tapa: se abre hacia la izquierda
		int tapa = animacion.AddTrack(Animation.TrackType.Value);
		animacion.TrackSetPath(tapa, "Carpeta/Tapa:rotation");
		animacion.TrackInsertKey(tapa, 0, Vector3.Zero, 1f);
		animacion.TrackInsertKey(tapa, TAbrir, Vector3.Zero, -1.8f);
		animacion.TrackInsertKey(tapa, TAbrir + 1.2, new Vector3(0, 0, Mathf.Pi), 1f);

		var biblioteca = new AnimationLibrary();
		biblioteca.AddAnimation("secuencia", animacion);
		_anim.AddAnimationLibrary("", biblioteca);
		_anim.Play("secuencia");
		_anim.Pause();
	}

	// ---------- Hojas (SubViewport → textura) ----------

	private async System.Threading.Tasks.Task PrepararHojas()
	{
		var izquierda = CrearHoja(true);
		var derecha = CrearHoja(false);
		for (int i = 0; i < 3; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		if (!IsInstanceValid(this)) return;
		AplicarHoja(izquierda, "Carpeta/Tapa/HojaIzquierda");
		AplicarHoja(derecha, "Carpeta/HojaDerecha");
	}

	private void AplicarHoja(SubViewport vista, string malla)
	{
		var imagen = vista.GetTexture().GetImage();
		imagen.GenerateMipmaps();
		_escritorio.GetNode<MeshInstance3D>(malla).MaterialOverride = new StandardMaterial3D
		{
			AlbedoTexture = ImageTexture.CreateFromImage(imagen),
			AlbedoColor = new Color(0.8f, 0.78f, 0.74f),
			Roughness = 0.88f,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
		};
		vista.QueueFree();
	}

	private SubViewport CrearHoja(bool izquierda)
	{
		var vista = new SubViewport
		{
			Size = TamHoja,
			Disable3D = true,
			TransparentBg = false,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
		};
		AddChild(vista);
		var hoja = new Control { Size = TamHoja };
		vista.AddChild(hoja);
		var fondo = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			Texture = GD.Load<Texture2D>("res://Assets/Pistas/Modelos/texturas/Paper005/Paper005_Color.jpg"),
			Size = TamHoja,
			SelfModulate = new Color(1.06f, 1.02f, 0.92f),
		};
		hoja.AddChild(fondo);
		var tinte = new ColorRect { Color = Papel with { A = 0.55f }, Size = TamHoja };
		hoja.AddChild(tinte);
		if (izquierda) HojaIzquierda(hoja); else HojaDerecha(hoja);
		return vista;
	}

	private Label Texto(Control padre, string texto, Font fuente, int tam, Color color, Rect2 caja, int lineas = 0, HorizontalAlignment alineacion = HorizontalAlignment.Left)
	{
		var etiqueta = new Label
		{
			Text = texto,
			// En un contenedor (caja vacía) no se recorta: si no, su alto mínimo sería cero
			ClipText = caja.Size != Vector2.Zero,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = alineacion,
			VerticalAlignment = VerticalAlignment.Top,
			MaxLinesVisible = lineas > 0 ? lineas : -1,
			TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
		};
		etiqueta.AddThemeFontOverride("font", fuente);
		etiqueta.AddThemeFontSizeOverride("font_size", tam);
		etiqueta.AddThemeColorOverride("font_color", color);
		etiqueta.AddThemeConstantOverride("line_spacing", 2);
		padre.AddChild(etiqueta);
		// El tamaño va al final: con el ajuste de línea ya puesto, no crece al ancho del texto
		etiqueta.Position = caja.Position;
		etiqueta.Size = caja.Size;
		return etiqueta;
	}

	private static void Raya(Control padre, Rect2 caja, Color color)
	{
		padre.AddChild(new ColorRect { Position = caja.Position, Size = caja.Size, Color = color });
	}

	private void HojaIzquierda(Control hoja)
	{
		var maquina = Fuente("SpecialElite-Regular.ttf");
		var courier = Fuente("CourierPrime-Bold.ttf", 0, 3);
		Texto(hoja, "EXPEDIENTE · CASO " + Caso, courier, 26, Tinta2, new Rect2(70, 46, 600, 40));
		Texto(hoja, "ARCHIVO " + Caso.PadLeft(3, '0'), courier, 26, Rojo, new Rect2(630, 46, 400, 40), 1, HorizontalAlignment.Right);
		Raya(hoja, new Rect2(70, 92, 960, 3), Tinta2);
		Texto(hoja, Texto("titulo").ToUpper(), Fuente("BigShouldersDisplay-Variable.ttf", 900, 1), 86, Tinta, new Rect2(70, 110, 960, 236), 2);

		Texto(hoja, "LUGAR", courier, 24, Rojo, new Rect2(70, 352, 300, 34));
		Texto(hoja, Texto("lugar"), maquina, 32, Tinta, new Rect2(70, 386, 960, 80), 2);
		Texto(hoja, "FECHA", courier, 24, Rojo, new Rect2(70, 470, 300, 34));
		Texto(hoja, Texto("fecha"), maquina, 32, Tinta, new Rect2(70, 504, 960, 44), 1);

		// Foto con clip
		var polaroid = new Panel { Position = new Vector2(310, 590), Size = new Vector2(480, 540), RotationDegrees = -3, PivotOffset = new Vector2(240, 270) };
		var estilo = new StyleBoxFlat { BgColor = new Color("#F6F3EC"), ShadowColor = new Color(0, 0, 0, 0.5f), ShadowSize = 18, ShadowOffset = new Vector2(6, 10), BorderWidthBottom = 2, BorderWidthRight = 2, BorderColor = new Color("#CFC8B8") };
		polaroid.AddThemeStyleboxOverride("panel", estilo);
		hoja.AddChild(polaroid);
		var foto = _informe.Get("foto").As<Texture2D>();
		if (foto != null)
			polaroid.AddChild(new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, Texture = foto, Position = new Vector2(22, 22), Size = new Vector2(436, 436) });
		else
		{
			polaroid.AddChild(new ColorRect { Color = new Color("#17191E"), Position = new Vector2(22, 22), Size = new Vector2(436, 436) });
			Texto(polaroid, "SIN FOTO", courier, 26, new Color("#6B747F"), new Rect2(22, 220, 436, 40), 1, HorizontalAlignment.Center);
		}
		Texto(polaroid, Texto("pie_foto"), maquina, 30, Tinta, new Rect2(22, 474, 436, 44), 1, HorizontalAlignment.Center);
		hoja.AddChild(new ClipPapel { Position = new Vector2(470, 552), RotationDegrees = 8 });

		string victima = Texto("etiqueta_victima");
		Texto(hoja, victima == "" ? "VÍCTIMA" : victima, courier, 24, Rojo, new Rect2(70, 1170, 400, 34));
		Texto(hoja, Texto("victima"), maquina, 32, Tinta, new Rect2(70, 1206, 960, 130), 3);
		Raya(hoja, new Rect2(70, 1342, 960, 2), Tinta2 with { A = 0.6f });
		Texto(hoja, "A CARGO: " + Texto("a_cargo").ToUpper() + "     ESTADO: " + Texto("estado").ToUpper(), courier, 22, Tinta2, new Rect2(70, 1356, 960, 34), 1);
	}

	private void HojaDerecha(Control hoja)
	{
		var maquina = Fuente("SpecialElite-Regular.ttf");
		var courier = Fuente("CourierPrime-Bold.ttf", 0, 2);
		var encabezado = Fuente("BigShouldersDisplay-Variable.ttf", 800, 3);

		Texto(hoja, "LÍNEA DE TIEMPO", encabezado, 52, Rojo, new Rect2(70, 50, 960, 64), 1);
		Raya(hoja, new Rect2(70, 118, 960, 3), Tinta2);
		// Cada entrada se mide para que la siguiente empiece justo debajo (máximo 3 renglones)
		float y = 140;
		foreach (var entrada in Lista("linea_tiempo"))
		{
			var partes = entrada.Split('|');
			bool resaltar = partes.Length > 2 && partes[2] == "!";
			string texto = partes.Length > 1 ? partes[1] : "";
			float alto = Mathf.Max(partes[0].Contains('–') ? 72 : 38, maquina.GetMultilineStringSize(texto, HorizontalAlignment.Left, 740, 27, 3).Y + 6);
			if (y + alto > 650) break;
			Texto(hoja, partes[0].Replace("–", "–\n"), courier, 28, resaltar ? Rojo : Tinta, new Rect2(70, y, 210, alto + 30), 2);
			Texto(hoja, texto, maquina, 27, Tinta, new Rect2(290, y + 2, 740, alto + 30), 3);
			y += alto + 12;
		}

		string titulo = Texto("titulo_personas");
		Texto(hoja, titulo == "" ? "PERSONAS" : titulo, encabezado, 40, Rojo, new Rect2(70, 680, 960, 52), 1);
		Raya(hoja, new Rect2(70, 736, 960, 3), Tinta2);
		var personas = new GridContainer { Columns = 2, Position = new Vector2(70, 756), Size = new Vector2(960, 340), ClipContents = true };
		personas.AddThemeConstantOverride("h_separation", 30);
		personas.AddThemeConstantOverride("v_separation", 12);
		hoja.AddChild(personas);
		foreach (var persona in Lista("personas").Take(6))
		{
			var partes = persona.Split('|');
			var ficha = new VBoxContainer { CustomMinimumSize = new Vector2(465, 0) };
			ficha.AddThemeConstantOverride("separation", 0);
			personas.AddChild(ficha);
			string edad = partes.Length > 1 && partes[1] != "" ? ", " + partes[1] : "";
			Texto(ficha, partes[0] + edad, Fuente("BigShouldersDisplay-Variable.ttf", 800, 1), 36, Tinta, new Rect2(), 1).CustomMinimumSize = new Vector2(465, 0);
			Texto(ficha, partes.Length > 2 ? partes[2] : "", Fuente("CourierPrime-Regular.ttf"), 21, Tinta2, new Rect2(), 2).CustomMinimumSize = new Vector2(465, 0);
		}
		var nota = Texto(hoja, "¿Quién miente?", Fuente("Caveat-Variable.ttf", 600), 46, Rojo, new Rect2(720, 664, 320, 60), 1);
		nota.RotationDegrees = -6;

		Texto(hoja, "ANTECEDENTE", encabezado, 40, Rojo, new Rect2(70, 1112, 960, 52), 1);
		Raya(hoja, new Rect2(70, 1166, 960, 3), Tinta2);
		Texto(hoja, Texto("antecedente"), maquina, 27, Tinta, new Rect2(70, 1184, 960, 210), 6);
	}

	// ---------- Audio y eventos ----------

	private AudioStreamPlayer NuevoAudio(string ruta, string bus, float volumen)
	{
		var reproductor = new AudioStreamPlayer { Stream = GD.Load<AudioStream>(ruta), Bus = bus, VolumeDb = volumen };
		AddChild(reproductor);
		return reproductor;
	}

	private void CrearAudio()
	{
		_musica = NuevoAudio("res://Assets/Audio/Cinematica/musica_moil.mp3", "Musica", -8f);
		_lluvia = NuevoAudio("res://Assets/Audio/Ambiente/lluvia_ventana.wav", "Ambiente", -16f);
		_reloj = NuevoAudio("res://Assets/Audio/Ambiente/reloj_pared.wav", "Ambiente", -24f);
	}

	private void Sonar(string ruta, float volumen = 0f, string bus = "Efectos")
	{
		var reproductor = NuevoAudio(ruta, bus, volumen);
		reproductor.Finished += reproductor.QueueFree;
		reproductor.Play();
	}

	private void CrearEventos()
	{
		const string interfaz = "res://Assets/Audio/Interfaz/";
		const string cine = "res://Assets/Audio/Cinematica/";
		_eventos.AddRange(new (double, Action)[]
		{
			(0.0, () => _lluvia.Play()),
			(0.8, () => Sonar(cine + "maquina_2.wav", -6f)),
			(2.6, () => Sonar(cine + "maquina_4.wav", -8f)),
			(TLuz, () => { _musica.Play(); _reloj.Play(); }),
			(TLuz + 0.38, () => Sonar(interfaz + "clic_2.wav", -2f)),
			(TCarpeta + 0.05, () => Sonar(interfaz + "pagina.wav", -4f)),
			(TCarpeta + 1.1, () => Sonar(interfaz + "expediente.wav", -2f)),
			(TSello, () => { Sonar(interfaz + "sello.wav", 2f); Sonar("res://Assets/Audio/Efectos/cajon_1.wav", -4f); _relojSacudida = 0; }),
			(TAbrir, () => Sonar(interfaz + "libreta_abrir.wav", -2f)),
			(30.3, () => Sonar(interfaz + "pagina.wav", -10f)),
			(TObjetivo + 0.5, () => Sonar(cine + "maquina_5.wav", -10f)),
			(TTarjeta, () => Sonar(cine + "golpe_titulo.ogg", -4f, "Musica")),
			(TTarjeta + 0.2, () => Sonar(cine + "maquina_1.wav", -8f)),
		});
		_eventos.Sort((a, b) => a.T.CompareTo(b.T));
	}

	// Al saltar de toma: los bucles y la música se ponen donde deberían ir, sin repetir efectos
	private void SincronizarAudio()
	{
		if (!_lluvia.Playing) _lluvia.Play();
		if (_t >= TLuz && !_musica.Playing) { _musica.Play((float)(_t - TLuz)); _reloj.Play(); }
		_relojSacudida = 10;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_informe == null || _terminada || _saltando) return;
		// Música: entra con la lámpara y se va antes de la tarjeta
		float volumen = Mathf.Lerp(-30f, -8f, Rampa(_t, TLuz, TLuz + 2.5));
		volumen = Mathf.Lerp(volumen, -50f, Rampa(_t, TNegro - 0.5, TTarjeta + 0.3));
		_musica.VolumeDb = volumen;
		_reloj.VolumeDb = Mathf.Lerp(-24f, -60f, Rampa(_t, TNegro, TTarjeta));
	}

	// ---------- Piezas dibujadas ----------

	// Círculo que se llena mientras se mantiene la tecla de saltar
	private partial class CirculoSalto : Control
	{
		public float Progreso;

		public override void _Draw()
		{
			var centro = Size / 2;
			float radio = Mathf.Min(Size.X, Size.Y) / 2 - 3;
			DrawArc(centro, radio, 0, Mathf.Tau, 48, new Color(1, 1, 1, 0.22f), 3, true);
			if (Progreso > 0)
				DrawArc(centro, radio, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * Mathf.Clamp(Progreso, 0, 1), 48, new Color("#EFCA08"), 4, true);
			DrawString(ThemeDB.FallbackFont, centro + new Vector2(-5, 6), "»", HorizontalAlignment.Left, -1, 18, new Color(1, 1, 1, 0.75f));
		}
	}

	// Clip metálico que sujeta la foto
	private partial class ClipPapel : Control
	{
		public override void _Draw()
		{
			// Alambre: sube por fuera, da la vuelta arriba, baja, vuelta abajo y sube por dentro
			var puntos = new List<Vector2> { new(0, 112) };
			Arco(puntos, new Vector2(17, 17), 17, Mathf.Pi, Mathf.Tau);
			Arco(puntos, new Vector2(23, 124), 11, 0, Mathf.Pi);
			Arco(puntos, new Vector2(17.5f, 32), 5.5f, Mathf.Pi, Mathf.Tau);
			puntos.Add(new Vector2(23, 96));
			DrawPolyline(Desplazar(puntos, new Vector2(3, 4)), new Color(0, 0, 0, 0.3f), 7, true);
			var linea = puntos.ToArray();
			DrawPolyline(linea, new Color("#7E858C"), 7, true);
			DrawPolyline(linea, new Color("#D5DADF"), 3, true);
		}

		private static void Arco(List<Vector2> puntos, Vector2 centro, float radio, float desde, float hasta)
		{
			for (int i = 0; i <= 10; i++)
			{
				float a = Mathf.Lerp(desde, hasta, i / 10f);
				puntos.Add(centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio);
			}
		}

		private static Vector2[] Desplazar(List<Vector2> puntos, Vector2 d) => puntos.Select(p => p + d).ToArray();
	}
}
