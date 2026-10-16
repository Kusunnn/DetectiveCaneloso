using Godot;
using System;
using System.Collections.Generic;

// Vista de examen de una evidencia (HU-07), al estilo del menú de inspección de Resident Evil:
// el objeto flota solo en un mundo 3D aparte (SubViewport) con luz de estudio, sobre el juego oscurecido.
// Se gira, se acerca hacia el cursor, se desplaza y se ilumina con la linterna. Si una cara esconde un
// DetalleOculto y el jugador la pone a la vista (ángulo, acercamiento y luz), aparece la lupa y lo confirma con [E].
// No conoce ningún caso: avisa con DetalleConfirmado y Cerrada, y el Jugador registra lo descubierto.
public partial class VistaExamen : CanvasLayer
{
	[Signal] public delegate void CerradaEventHandler();

	// Se llama al confirmar un detalle: (detalle, foto del acercamiento)
	public Action<DetalleOculto, Texture2D> DetalleConfirmado;

	private const float DistanciaCamara = 3f;
	private const float RadioEnPantalla = 0.5f;   // Radio de la esfera que envuelve al modelo (~60 % del alto)
	private const float ZoomMaximo = 3f;
	private const float VelocidadGiroMaxima = 9f; // rad/s: inercia sin que salga girando como trompo
	private const float DuracionEntrada = 0.45f;
	private const float DuracionSalida = 0.25f;

	private static readonly Color Papel = new Color("c6c1b1");
	private static readonly Color Tinta = new Color("20272b");
	private static readonly Color Ambar = new Color("efca08");
	private static readonly Color Blanco = new Color("e8ebef");
	private static readonly Font Display = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf");
	private static readonly Font Mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");

	// La ayuda "gira el objeto" solo sale la primera vez en cada sesión de juego
	private static bool _yaGiroAlgunaVez;

	private ColorRect _fondo;
	private ShaderMaterial _materialFondo;
	private SubViewportContainer _visor;
	private SubViewport _mundo;
	private Camera3D _camara;
	private SpotLight3D _linterna;
	private Node3D _pivote;
	private Node3D _escala;
	private Node3D _modelo;

	private Label _titulo, _estado, _descripcion, _lugar, _ayuda;
	private Control _caja;
	// Aviso pequeño pegado al detalle que está a la vista: «Revisar detalle» (se revisa con clic o con A)
	private Control _revisar;
	private float _alfaRevisar;
	private Vector2 _clicInicio;
	private bool _barraConMover;
	private HBoxContainer _barra;
	private bool _barraMando;
	private ColorRect _destello;

	// Estado del examen
	private Resource _pista;
	private readonly List<DetalleOculto> _pendientes = new();
	// Punto y normal de cada detalle en el modelo: del marcador "Detalle_<id>" si el modelo lo trae, si no del recurso
	private readonly Dictionary<DetalleOculto, (Vector3 Punto, Vector3 Normal, Vector3 Arriba)> _ubicaciones = new();
	private bool _tieneDescubiertos;
	private bool _abierta, _cerrando;
	private float _tiempoAnimacion;
	private Quaternion _giro = Quaternion.Identity;
	private Quaternion _giroInicial = Quaternion.Identity;
	private Vector2 _velocidadGiro;     // rad/s (x = alrededor del eje vertical, y = del horizontal)
	private float _zoom = 1f, _zoomObjetivo = 1f;
	private Vector2 _despl, _desplObjetivo; // Desplazamiento de la cámara en el plano del objeto
	private Vector3 _centro;
	private float _factorEscala = 1f;
	private bool _arrastrando, _moviendo;
	private float _giroAcumulado;
	private bool _linternaEncendida;
	private DetalleOculto _detalleVisible;
	private DetalleOculto _descubriendo;   // Animación automática hacia el detalle en curso
	private Quaternion _giroDescubrir;
	private string _enfocar;               // Al abrir desde una entrada de detalle: se enfoca ese detalle
	private Input.MouseModeEnum _modoRatonAnterior;
	private bool _arbolPausadoAntes;

	public bool Abierta => _abierta;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 40;
		Visible = false;
		_fondo = GetNode<ColorRect>("Fondo");
		_materialFondo = (ShaderMaterial)_fondo.Material;
		_visor = GetNode<SubViewportContainer>("Visor");
		_mundo = GetNode<SubViewport>("Visor/Mundo");
		_camara = GetNode<Camera3D>("Visor/Mundo/Camara");
		_linterna = GetNode<SpotLight3D>("Visor/Mundo/Camara/Linterna");
		_pivote = GetNode<Node3D>("Visor/Mundo/Pivote");
		_escala = GetNode<Node3D>("Visor/Mundo/Pivote/Escala");
		// Luz de estudio: principal cálida arriba a la izquierda, contorno fría desde atrás y un relleno suave
		GetNode<Node3D>("Visor/Mundo/LuzPrincipal").LookAtFromPosition(new Vector3(-2f, 2.5f, 3f), Vector3.Zero);
		GetNode<Node3D>("Visor/Mundo/LuzContorno").LookAtFromPosition(new Vector3(2.2f, 2f, -3f), Vector3.Zero);
		GetNode<Node3D>("Visor/Mundo/LuzRelleno").LookAtFromPosition(new Vector3(2.5f, -0.6f, 2f), Vector3.Zero);
		CrearInterfaz();
	}

	// ---------- Abrir y cerrar ----------

	// pista: recurso Pista de la evidencia (nunca la entrada de un detalle). objeto: su ObjetoPista en la escena,
	// para copiar sus mallas si no trae modelo. descubiertos: ids completos de los detalles ya encontrados.
	public async void Abrir(Resource pista, ObjetoPista objeto, ICollection<string> descubiertos, string enfocar = null)
	{
		if (_abierta) return;
		_abierta = true;
		_cerrando = false;
		_pista = pista;
		_enfocar = enfocar;
		_arbolPausadoAntes = GetTree().Paused;
		CongelarFondo();
		GetTree().Paused = true;
		_modoRatonAnterior = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		string id = pista.Get("id").AsStringName().ToString();
		_pendientes.Clear();
		_tieneDescubiertos = false;
		foreach (Variant v in pista.Get("detalles").AsGodotArray())
		{
			if (v.As<Resource>() is not DetalleOculto d) continue;
			if (descubiertos.Contains(id + "." + d.Id)) _tieneDescubiertos = true;
			else _pendientes.Add(d);
		}

		// Modelo: se centra según su AABB y se escala para ocupar lo mismo sin importar su tamaño real
		_modelo = ConstruirModelo(pista, objeto);
		_escala.AddChild(_modelo);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); // Los Label3D calculan su caja al entrar
		if (!_abierta) return;
		Aabb caja = CajaDe(_modelo);
		_centro = caja.GetCenter();
		float radio = Mathf.Max(0.01f, caja.Size.Length() / 2f);
		_factorEscala = RadioEnPantalla / radio;
		_escala.Scale = Vector3.One * _factorEscala;
		_escala.Position = -_centro * _factorEscala;
		UbicarDetalles();

		Vector3 giro = objeto?.GiroExamen ?? Vector3.Zero;
		_giroInicial = giro != Vector3.Zero ? Quaternion.FromEuler(giro * Mathf.Pi / 180f) : PoseAutomatica(caja);
		Reiniciar(inmediato: true);
		_linternaEncendida = false;
		AplicarLinterna();
		_detalleVisible = null;
		_descubriendo = null;
		_giroAcumulado = 0f;

		_titulo.Text = pista.Get("titulo").AsString().ToUpper();
		string ampliada = pista.Get("descripcion_ampliada").AsString();
		_descripcion.Text = string.IsNullOrEmpty(ampliada) ? pista.Get("descripcion").AsString() : ampliada;
		string lugar = pista.Get("lugar").AsString();
		_lugar.Text = string.IsNullOrEmpty(lugar) ? "" : "ENCONTRADA EN: " + lugar.ToUpper();
		ActualizarEstado();
		_alfaRevisar = 0f;
		_revisar.Visible = false;
		_ayuda.Visible = !_yaGiroAlgunaVez;
		ActualizarBarra(Input.GetConnectedJoypads().Count > 0 && _barraMando);

		_tiempoAnimacion = 0f;
		Visible = true;
		Sonidos.ReproducirUI(this, Sonidos.Tipo.ExamenAbrir);
		if (_enfocar != null) EnfocarDescubierto(_enfocar);
	}

	public void Cerrar()
	{
		if (!_abierta || _cerrando) return;
		_cerrando = true;
		_tiempoAnimacion = 0f;
		Sonidos.ReproducirUI(this, Sonidos.Tipo.ExamenCerrar);
	}

	private void TerminarCierre()
	{
		_abierta = false;
		_cerrando = false;
		Visible = false;
		_modelo?.QueueFree();
		_modelo = null;
		GetTree().Paused = _arbolPausadoAntes;
		GetViewport().Disable3D = false;
		Input.MouseMode = _modoRatonAnterior;
		EmitSignal(SignalName.Cerrada);
	}

	// El juego de fondo se fotografía una vez y se deja de renderizar el 3D mientras la vista está abierta:
	// así el examen no paga el costo del escenario (niebla, sombras...) y se mantiene fluido.
	private void CongelarFondo()
	{
		var captura = GetViewport().GetTexture().GetImage();
		if (captura == null || captura.IsEmpty()) return; // Sin pantalla (pruebas en headless)
		captura.GenerateMipmaps();
		_materialFondo.SetShaderParameter("fondo", ImageTexture.CreateFromImage(captura));
		GetViewport().Disable3D = true;
	}

	private void Reiniciar(bool inmediato = false)
	{
		_giro = _giroInicial;
		_velocidadGiro = Vector2.Zero;
		_zoomObjetivo = 1f;
		_desplObjetivo = Vector2.Zero;
		if (!inmediato) return;
		_zoom = 1f;
		_despl = Vector2.Zero;
	}

	// ---------- Entrada ----------

	public override void _Input(InputEvent evento)
	{
		if (!_abierta) return;
		// Mientras está abierta, nada del juego recibe teclas (ni el menú de pausa ni el HUD)
		GetViewport().SetInputAsHandled();
		if (_cerrando) return;

		bool esMando = evento is InputEventJoypadButton || (evento is InputEventJoypadMotion m && Mathf.Abs(m.AxisValue) > 0.3f);
		bool esTeclado = evento is InputEventKey || evento is InputEventMouseButton || (evento is InputEventMouseMotion raton && raton.Relative.LengthSquared() > 4f);
		if (esMando && !_barraMando) ActualizarBarra(true);
		else if (esTeclado && _barraMando) ActualizarBarra(false);

		if (_descubriendo != null) return; // La cámara se está acercando sola al detalle

		switch (evento)
		{
			case InputEventMouseButton boton:
				if (boton.ButtonIndex == MouseButton.Left)
				{
					_arrastrando = boton.Pressed;
					// Un clic sin arrastrar sobre el detalle a la vista lo revisa
					if (boton.Pressed) _clicInicio = boton.Position;
					else if (boton.Position.DistanceTo(_clicInicio) < 8f) ClicEn(boton.Position);
				}
				else if (boton.ButtonIndex == MouseButton.Right) _moviendo = boton.Pressed;
				else if (boton.Pressed && boton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
					Acercar(boton.ButtonIndex == MouseButton.WheelUp ? 0.2f : -0.2f, boton.Position);
				break;
			case InputEventMouseMotion movimiento:
				if (_arrastrando)
				{
					var giro = movimiento.Relative * 0.008f;
					Girar(giro.X, giro.Y);
					// La velocidad del arrastre queda como inercia al soltar
					_velocidadGiro = _velocidadGiro.Lerp((giro / (float)Mathf.Max(GetProcessDeltaTime(), 0.001)).LimitLength(VelocidadGiroMaxima), 0.5f);
				}
				else if (_moviendo) Desplazar(movimiento.Relative);
				break;
			case InputEventKey tecla when tecla.Pressed && !tecla.Echo:
				switch (tecla.Keycode)
				{
					case Key.Escape: case Key.Q: case Key.Backspace: Cerrar(); break;
					case Key.R: Reiniciar(); Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic, -4f); break;
					case Key.F: AlternarLinterna(); break;
					case Key.E: case Key.Enter: case Key.Space: Confirmar(); break;
				}
				break;
			case InputEventJoypadButton boton when boton.Pressed:
				switch (boton.ButtonIndex)
				{
					case JoyButton.B: case JoyButton.LeftShoulder: case JoyButton.Back: Cerrar(); break;
					case JoyButton.X: Reiniciar(); break;
					case JoyButton.Y: case JoyButton.RightShoulder: AlternarLinterna(); break;
					case JoyButton.A: Confirmar(); break;
				}
				break;
		}
	}

	private void Girar(float horizontal, float vertical)
	{
		// Alrededor de los ejes de la cámara: arrastrar a la derecha gira el objeto a la derecha
		_giro = (new Quaternion(Vector3.Up, horizontal) * new Quaternion(Vector3.Right, vertical) * _giro).Normalized();
		_giroAcumulado += Mathf.Abs(horizontal) + Mathf.Abs(vertical);
		if (_ayuda.Visible && _giroAcumulado > 0.8f)
		{
			_ayuda.Visible = false;
			_yaGiroAlgunaVez = true;
		}
		_sonidoGiro += Mathf.Abs(horizontal) + Mathf.Abs(vertical);
		if (_sonidoGiro > 1.4f)
		{
			_sonidoGiro = 0f;
			Sonidos.ReproducirUI(this, Sonidos.Tipo.ExamenGirar);
		}
	}
	private float _sonidoGiro;

	// Rueda o gatillos: acerca hacia el punto que está bajo el cursor (que se queda en su sitio)
	private void Acercar(float paso, Vector2 cursor)
	{
		float anterior = _zoomObjetivo;
		_zoomObjetivo = Mathf.Clamp(_zoomObjetivo + paso, 1f, ZoomMaximo);
		if (Mathf.IsEqualApprox(anterior, _zoomObjetivo)) return;
		Vector2 tam = _visor.Size;
		var n = new Vector2(cursor.X / tam.X * 2f - 1f, 1f - cursor.Y / tam.Y * 2f);
		float t = Mathf.Tan(Mathf.DegToRad(_camara.Fov) / 2f);
		var media = new Vector2(t * tam.X / tam.Y, t);
		float d1 = DistanciaCamara / anterior, d2 = DistanciaCamara / _zoomObjetivo;
		_desplObjetivo = LimitarDespl(_desplObjetivo + n * media * (d1 - d2), _zoomObjetivo);
		if (Mathf.Abs(_zoomObjetivo - _ultimoZoomSonado) >= 0.4f)
		{
			_ultimoZoomSonado = _zoomObjetivo;
			Sonidos.ReproducirUI(this, Sonidos.Tipo.ExamenAcercar, 0f, 0.9f + _zoomObjetivo * 0.08f);
		}
	}
	private float _ultimoZoomSonado = 1f;

	private void Desplazar(Vector2 relativo)
	{
		if (_zoomObjetivo <= 1.01f) return;
		float porPixel = 2f * Mathf.Tan(Mathf.DegToRad(_camara.Fov) / 2f) * (DistanciaCamara / _zoom) / _visor.Size.Y;
		_desplObjetivo = LimitarDespl(_desplObjetivo + new Vector2(-relativo.X, relativo.Y) * porPixel, _zoomObjetivo);
	}

	// Acercado se puede recorrer el objeto, pero sin perderlo de vista
	private static Vector2 LimitarDespl(Vector2 despl, float zoom)
	{
		float limite = RadioEnPantalla * 1.1f * (zoom - 1f) / zoom;
		return despl.LimitLength(limite);
	}

	private void AlternarLinterna()
	{
		_linternaEncendida = !_linternaEncendida;
		AplicarLinterna();
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic, _linternaEncendida ? 0f : -3f, _linternaEncendida ? 1f : 0.85f);
	}

	// Lo del grupo "examen_linterna" (texto bajo un tachón, marcas tenues) solo se ve con la linterna
	private void AplicarLinterna()
	{
		_linterna.Visible = _linternaEncendida;
		if (_modelo == null) return;
		foreach (Node nodo in _modelo.FindChildren("*", "", true, false))
		{
			if (nodo is not Node3D n) continue;
			if (n.IsInGroup("examen_linterna")) n.Visible = _linternaEncendida;
			else if (n.IsInGroup("examen_sin_linterna")) n.Visible = !_linternaEncendida;
		}
	}

	// ---------- Cada cuadro ----------

	public override void _Process(double delta)
	{
		if (!_abierta) return;
		float dt = (float)delta;
		_tiempoAnimacion += dt;

		// Entrada: el objeto llega con un pequeño giro y acercamiento; salida: se desvanece
		float mezcla;
		float entrada = 1f;
		if (_cerrando)
		{
			mezcla = 1f - Mathf.Clamp(_tiempoAnimacion / DuracionSalida, 0f, 1f);
			if (mezcla <= 0f) { TerminarCierre(); return; }
		}
		else
		{
			entrada = Mathf.Clamp(_tiempoAnimacion / DuracionEntrada, 0f, 1f);
			mezcla = Mathf.Clamp(_tiempoAnimacion / 0.2f, 0f, 1f);
		}
		float suave = 1f - Mathf.Pow(1f - entrada, 3f);
		_materialFondo.SetShaderParameter("mezcla", mezcla);
		_visor.Modulate = new Color(1, 1, 1, _cerrando ? mezcla : suave);
		GetNode<Control>("Interfaz").Modulate = new Color(1, 1, 1, mezcla);

		if (_descubriendo != null)
		{
			// La cámara se acerca sola al detalle
			_giro = _giro.Slerp(_giroDescubrir, 1f - Mathf.Exp(-7f * dt));
		}
		else if (!_arrastrando)
		{
			// Inercia al soltar, que se apaga sola; el stick izquierdo gira de forma continua
			var stick = new Vector2(Input.GetJoyAxis(0, JoyAxis.LeftX), Input.GetJoyAxis(0, JoyAxis.LeftY));
			if (stick.Length() > 0.2f) _velocidadGiro = stick * 2.8f;
			else _velocidadGiro *= Mathf.Exp(-5.5f * dt);
			if (_velocidadGiro.LengthSquared() > 0.0004f) Girar(_velocidadGiro.X * dt, _velocidadGiro.Y * dt);
			var derecho = new Vector2(Input.GetJoyAxis(0, JoyAxis.RightX), Input.GetJoyAxis(0, JoyAxis.RightY));
			if (derecho.Length() > 0.2f) Desplazar(derecho * 600f * dt);
			float gatillos = Input.GetJoyAxis(0, JoyAxis.TriggerRight) - Input.GetJoyAxis(0, JoyAxis.TriggerLeft);
			if (Mathf.Abs(gatillos) > 0.15f) Acercar(gatillos * 1.6f * dt, _visor.Size / 2f);
		}

		_zoom = Mathf.Lerp(_zoom, _zoomObjetivo, 1f - Mathf.Exp(-10f * dt));
		_despl = _despl.Lerp(_desplObjetivo, 1f - Mathf.Exp(-10f * dt));
		_camara.Position = new Vector3(_despl.X, _despl.Y, DistanciaCamara / _zoom);
		_pivote.Quaternion = new Quaternion(Vector3.Up, (1f - suave) * -0.7f) * _giro;
		_pivote.Scale = Vector3.One * Mathf.Lerp(0.8f, 1f, suave);

		BuscarDetalle();
		MoverAvisoRevisar(dt);
		// «Mover» solo tiene sentido con zoom: la barra lo muestra u oculta
		if ((_zoomObjetivo > 1.01f) != _barraConMover) ActualizarBarra(_barraMando);
	}

	// ---------- Detalles ocultos ----------

	private Vector3 PuntoEnMundo(DetalleOculto d) => _escala.GlobalTransform * Ubicacion(d).Punto;
	private Vector3 NormalEnMundo(DetalleOculto d) => (_pivote.GlobalTransform.Basis * Ubicacion(d).Normal).Normalized();

	private (Vector3 Punto, Vector3 Normal, Vector3 Arriba) Ubicacion(DetalleOculto d)
	{
		if (_ubicaciones.TryGetValue(d, out var u)) return u;
		Vector3 normal = d.Normal.Normalized();
		return (d.Posicion, normal, Mathf.Abs(normal.Dot(Vector3.Up)) > 0.9f ? Vector3.Back : Vector3.Up);
	}

	// Un Marker3D llamado "Detalle_<id>" dentro del modelo marca el detalle: su posición, su eje +Z (hacia afuera)
	// y su eje +Y (hacia arriba del texto, para que al acercarse se lea derecho)
	private void UbicarDetalles()
	{
		_ubicaciones.Clear();
		foreach (Variant v in _pista.Get("detalles").AsGodotArray())
		{
			if (v.As<Resource>() is not DetalleOculto d) continue;
			if (_modelo.FindChild("Detalle_" + d.Id, true, false) is not Node3D marcador) continue;
			Transform3D t = marcador.Transform;
			for (Node p = marcador.GetParent(); p != null && p != _modelo; p = p.GetParent())
				if (p is Node3D n3) t = n3.Transform * t;
			_ubicaciones[d] = (t.Origin, t.Basis.Z.Normalized(), t.Basis.Y.Normalized());
		}
	}

	private bool SeVe(DetalleOculto d)
	{
		if (d.RequiereLinterna && !_linternaEncendida) return false;
		if (_zoom < d.ZoomMinimo - 0.05f) return false;
		Vector3 punto = PuntoEnMundo(d);
		Vector3 haciaCamara = (_camara.GlobalPosition - punto).Normalized();
		if (NormalEnMundo(d).Dot(haciaCamara) < Mathf.Cos(Mathf.DegToRad(d.Tolerancia))) return false;
		if (_camara.IsPositionBehind(punto)) return false;
		// Que esté dentro del encuadre (no en la orilla de la pantalla)
		Vector2 enPantalla = _camara.UnprojectPosition(punto);
		Vector2 tam = _mundo.Size;
		return new Rect2(tam * 0.1f, tam * 0.8f).HasPoint(enPantalla);
	}

	private void BuscarDetalle()
	{
		if (_descubriendo != null || _entradaAnimando) return;
		DetalleOculto visible = null;
		foreach (var d in _pendientes)
			if (SeVe(d)) { visible = d; break; }
		if (visible == _detalleVisible) return;
		_detalleVisible = visible;
		// Pista sutil: un aviso pequeño junto al detalle y un "tic". Sin flechas ni brillos sobre el objeto.
		if (visible != null)
		{
			Sonidos.ReproducirUI(this, Sonidos.Tipo.ExamenTic);
			Input.StartJoyVibration(0, 0.15f, 0.05f, 0.12f);
		}
	}
	private bool _entradaAnimando => !_cerrando && _tiempoAnimacion < DuracionEntrada;

	// Dónde se ve un detalle en la pantalla (coordenadas de la interfaz)
	private Vector2 EnPantalla(DetalleOculto d) => _camara.UnprojectPosition(PuntoEnMundo(d)) * (_visor.Size / (Vector2)_mundo.Size);

	private void ClicEn(Vector2 posicion)
	{
		if (_detalleVisible == null) return;
		bool cerca = posicion.DistanceTo(EnPantalla(_detalleVisible)) < 120f || _revisar.GetGlobalRect().HasPoint(posicion);
		if (cerca) Descubrir(_detalleVisible);
	}

	// El aviso sigue al detalle, aparece suave y se apaga al alejarse
	private void MoverAvisoRevisar(float dt)
	{
		bool mostrar = _detalleVisible != null && _descubriendo == null;
		_alfaRevisar = Mathf.MoveToward(_alfaRevisar, mostrar ? 1f : 0f, dt / 0.2f);
		_revisar.Visible = _alfaRevisar > 0.01f;
		if (!_revisar.Visible) return;
		if (_detalleVisible != null) _revisar.Position = EnPantalla(_detalleVisible) + new Vector2(18f, -_revisar.Size.Y - 10f);
		_revisar.Modulate = new Color(1, 1, 1, _alfaRevisar);
	}

	// Público para las pruebas: dónde habría que hacer clic para revisar el detalle a la vista
	public Vector2 PosicionDetalleALaVista => _detalleVisible != null ? EnPantalla(_detalleVisible) : new Vector2(-1, -1);

	private void Confirmar()
	{
		if (_detalleVisible == null) return;
		Descubrir(_detalleVisible);
	}

	private async void Descubrir(DetalleOculto d)
	{
		_descubriendo = d;
		_pendientes.Remove(d);
		_detalleVisible = null;
		_arrastrando = _moviendo = false;
		_velocidadGiro = Vector2.Zero;
		ApuntarA(d);

		await ToSignal(GetTree().CreateTimer(0.7, processAlways: true), SceneTreeTimer.SignalName.Timeout);
		if (!_abierta) return;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var foto = Fotografiar();

		_destello.Color = new Color(1, 1, 1, 0.35f);
		CreateTween().TweenProperty(_destello, "color:a", 0f, 0.5f);
		Sonidos.ReproducirUI(this, Sonidos.Tipo.ExamenDescubrir);
		Input.StartJoyVibration(0, 0.4f, 0.3f, 0.25f);
		_tieneDescubiertos = true;
		ActualizarEstado();
		_descripcion.Text = d.Descripcion + (string.IsNullOrEmpty(d.Pensamiento) ? "" : "\n\n“" + d.Pensamiento + "”");
		DetalleConfirmado?.Invoke(d, foto);

		await ToSignal(GetTree().CreateTimer(0.9, processAlways: true), SceneTreeTimer.SignalName.Timeout);
		_descubriendo = null;
	}

	// Gira el objeto para que la cara del detalle mire a la cámara, derecha, y se acerca a él
	private void ApuntarA(DetalleOculto d)
	{
		var ubicacion = Ubicacion(d);
		Vector3 normal = (_giro * ubicacion.Normal).Normalized();
		Quaternion alinear = normal.Dot(Vector3.Back) < -0.999f ? new Quaternion(Vector3.Up, Mathf.Pi) : new Quaternion(normal, Vector3.Back);
		Quaternion alineado = (alinear * _giro).Normalized();
		// Que lo de arriba del detalle quede arriba en la pantalla (un texto no se lee de cabeza)
		Vector3 arriba = alineado * ubicacion.Arriba;
		float giroPlano = Mathf.Atan2(arriba.X, arriba.Y);
		_giroDescubrir = (new Quaternion(Vector3.Back, giroPlano) * alineado).Normalized();
		_zoomObjetivo = Mathf.Clamp(Mathf.Max(d.ZoomMinimo + 0.3f, 1.5f), 1f, ZoomMaximo);
		Vector3 punto = _giroDescubrir * ((Ubicacion(d).Punto - _centro) * _factorEscala);
		_desplObjetivo = LimitarDespl(new Vector2(punto.X, punto.Y), _zoomObjetivo);
	}

	// Id del detalle que está a la vista ahora mismo (null si ninguno). Lo usan las pruebas.
	public string DetalleALaVista => _detalleVisible?.Id;

	// Pone a la vista un detalle pendiente sin animación, como si el jugador lo hubiera girado y acercado.
	// Solo para pruebas automáticas (Herramientas/probar_examen.gd): el jugador nunca lo usa.
	public void MirarDetalle(string id)
	{
		foreach (var d in _pendientes)
		{
			if (d.Id != id) continue;
			if (d.RequiereLinterna && !_linternaEncendida) { _linternaEncendida = true; AplicarLinterna(); }
			ApuntarA(d);
			_giro = _giroDescubrir;
			_zoom = _zoomObjetivo;
			_despl = _desplObjetivo;
			return;
		}
	}

	// Al abrir desde la entrada de un detalle ya descubierto: el objeto se presenta mostrándolo
	private void EnfocarDescubierto(string idCompleto)
	{
		string id = _pista.Get("id").AsStringName().ToString();
		foreach (Variant v in _pista.Get("detalles").AsGodotArray())
		{
			if (v.As<Resource>() is not DetalleOculto d || id + "." + d.Id != idCompleto) continue;
			if (d.RequiereLinterna) { _linternaEncendida = true; AplicarLinterna(); }
			ApuntarA(d);
			_giro = _giroDescubrir;
			return;
		}
	}

	// Foto del acercamiento para la entrada nueva del diario (sobre papel oscuro, 4:3)
	private Texture2D Fotografiar()
	{
		var captura = _mundo.GetTexture().GetImage();
		if (captura == null || captura.IsEmpty()) return _pista.Get("imagen").As<Texture2D>(); // Sin pantalla (pruebas en headless)
		captura.Convert(Image.Format.Rgba8);
		int alto = Mathf.Min(captura.GetHeight(), (int)(captura.GetWidth() * 0.75f));
		int ancho = (int)(alto / 0.75f);
		var recorte = captura.GetRegion(new Rect2I((captura.GetWidth() - ancho) / 2, (captura.GetHeight() - alto) / 2, ancho, alto));
		var foto = Image.CreateEmpty(ancho, alto, false, Image.Format.Rgba8);
		foto.Fill(new Color(0.09f, 0.08f, 0.07f));
		foto.BlendRect(recorte, new Rect2I(0, 0, ancho, alto), Vector2I.Zero);
		foto.Resize(480, 360, Image.Interpolation.Lanczos);
		return ImageTexture.CreateFromImage(foto);
	}

	private void ActualizarEstado()
	{
		bool examinada = _pista.Get("examinada").AsBool();
		_estado.Text = _tieneDescubiertos ? "◆ DETALLE DESCUBIERTO" : examinada ? "● EXAMINADA" : "○ RECOGIDA · SIN REVISAR";
	}

	// ---------- Modelo ----------

	// Modelo propio (Pista.modelo o ObjetoPista.ModeloExamen) o, si no hay, copia de las mallas del objeto
	public static Node3D ConstruirModelo(Resource pista, ObjetoPista objeto)
	{
		var escena = pista.Get("modelo").As<PackedScene>() ?? objeto?.ModeloExamen;
		if (escena != null) return escena.Instantiate<Node3D>();

		var raiz = new Node3D { Name = "Copia" };
		if (objeto == null || !IsInstanceValid(objeto))
		{
			raiz.AddChild(Tarjeta(pista.Get("imagen").As<Texture2D>()));
			return raiz;
		}
		foreach (Node hijo in objeto.GetChildren())
			if (hijo is Node3D n && Copiable(n)) Agregar(raiz, n, n.Transform);
		Transform3D inversa = objeto.GlobalTransform.AffineInverse();
		foreach (Node3D visual in objeto.Visuales)
			if (visual != null && IsInstanceValid(visual)) Agregar(raiz, visual, inversa * visual.GlobalTransform);
		if (raiz.GetChildCount() == 0) raiz.AddChild(Tarjeta(pista.Get("imagen").As<Texture2D>()));
		return raiz;
	}

	private static bool Copiable(Node n) =>
		n is not (CollisionObject3D or CollisionShape3D or Light3D or AudioStreamPlayer3D or GpuParticles3D or Camera3D);

	private static void Agregar(Node3D raiz, Node3D original, Transform3D transformacion)
	{
		var copia = (Node3D)original.Duplicate((int)(Node.DuplicateFlags.Scripts | Node.DuplicateFlags.Groups | Node.DuplicateFlags.UseInstantiation));
		copia.Transform = transformacion;
		copia.Visible = true;
		raiz.AddChild(copia);
		Limpiar(copia);
	}

	// Sin colisiones, sonidos ni resaltado; lo que en el escenario solo se ve con linterna aquí se ve siempre
	private static void Limpiar(Node nodo)
	{
		foreach (Node hijo in nodo.GetChildren())
		{
			if (!Copiable(hijo)) { nodo.RemoveChild(hijo); hijo.QueueFree(); continue; }
			Limpiar(hijo);
		}
		if (nodo is GeometryInstance3D g) g.MaterialOverlay = null;
		if (nodo.IsInGroup("solo_con_linterna"))
		{
			nodo.RemoveFromGroup("solo_con_linterna");
			if (nodo is Node3D n) n.Visible = true;
		}
	}

	// Sin modelo ni objeto: la foto de la pista como una tarjeta de 2 mm
	private static Node3D Tarjeta(Texture2D imagen)
	{
		var tam = imagen != null ? (Vector2)imagen.GetSize() / Mathf.Max(imagen.GetWidth(), imagen.GetHeight()) * 0.3f : new Vector2(0.3f, 0.22f);
		var raiz = new Node3D();
		raiz.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(tam.X, tam.Y, 0.002f), Material = new StandardMaterial3D { AlbedoColor = new Color(0.86f, 0.83f, 0.74f), Roughness = 0.9f } } });
		if (imagen != null)
			raiz.AddChild(new MeshInstance3D
			{
				Mesh = new QuadMesh { Size = tam * 0.94f, Material = new StandardMaterial3D { AlbedoTexture = imagen, Roughness = 0.8f } },
				Position = new Vector3(0, 0, 0.0011f),
			});
		return raiz;
	}

	// Sin giro configurado: un papel se presenta de frente (su cara plana hacia la cámara) y un objeto, a tres cuartos
	private static Quaternion PoseAutomatica(Aabb caja)
	{
		Vector3 t = caja.Size;
		float menor = Mathf.Min(t.X, Mathf.Min(t.Y, t.Z));
		float medio = t.X + t.Y + t.Z - menor - Mathf.Max(t.X, Mathf.Max(t.Y, t.Z));
		if (menor < medio * 0.35f)
		{
			if (Mathf.IsEqualApprox(menor, t.Y)) return new Quaternion(Vector3.Right, Mathf.Pi / 2f);  // Acostado: la cara de arriba al frente
			if (Mathf.IsEqualApprox(menor, t.X)) return new Quaternion(Vector3.Up, -Mathf.Pi / 2f);   // De canto
			return Quaternion.Identity;                                                              // Colgado en la pared
		}
		return (new Quaternion(Vector3.Right, Mathf.DegToRad(22f)) * new Quaternion(Vector3.Up, Mathf.DegToRad(-28f))).Normalized();
	}

	// Caja que envuelve todas las mallas del modelo, en sus coordenadas
	private static Aabb CajaDe(Node3D raiz)
	{
		Aabb? caja = null;
		foreach (Node nodo in raiz.FindChildren("*", "VisualInstance3D", true, false))
		{
			var visual = (VisualInstance3D)nodo;
			if (!visual.IsVisibleInTree() && !visual.IsInGroup("examen_linterna")) continue;
			Aabb local = visual.GetAabb();
			if (local.Size == Vector3.Zero) continue;
			Transform3D t = visual.Transform;
			for (Node p = visual.GetParent(); p != null && p != raiz; p = p.GetParent())
				if (p is Node3D n3) t = n3.Transform * t;
			Aabb enRaiz = t * local;
			caja = caja?.Merge(enRaiz) ?? enRaiz;
		}
		return caja ?? new Aabb(new Vector3(-0.1f, -0.1f, -0.1f), new Vector3(0.2f, 0.2f, 0.2f));
	}

	// ---------- Interfaz ----------

	private void CrearInterfaz()
	{
		var interfaz = GetNode<Control>("Interfaz");

		_titulo = Etiqueta(Display, 46, Blanco);
		_titulo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
		_titulo.HorizontalAlignment = HorizontalAlignment.Center;
		_titulo.GrowHorizontal = Control.GrowDirection.Both;
		_titulo.Position = new Vector2(_titulo.Position.X, 34);
		interfaz.AddChild(_titulo);
		var raya = new ColorRect { Color = Ambar, CustomMinimumSize = new Vector2(140, 4) };
		raya.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
		raya.Position = new Vector2(-70, 96);
		interfaz.AddChild(raya);

		_estado = Etiqueta(Mono, 15, Ambar);
		_estado.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
		_estado.HorizontalAlignment = HorizontalAlignment.Center;
		_estado.GrowHorizontal = Control.GrowDirection.Both;
		_estado.Position = new Vector2(_estado.Position.X, 108);
		interfaz.AddChild(_estado);

		// Caja de texto de papel, como las tarjetas del HUD
		var caja = new PanelContainer();
		caja.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = Papel, ContentMarginLeft = 26, ContentMarginRight = 26, ContentMarginTop = 16, ContentMarginBottom = 16,
			ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 10, ShadowOffset = new Vector2(0, 4),
		});
		caja.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
		caja.CustomMinimumSize = new Vector2(880, 0);
		caja.GrowHorizontal = Control.GrowDirection.Both;
		caja.GrowVertical = Control.GrowDirection.Begin;
		caja.OffsetLeft = -440; caja.OffsetRight = 440; caja.OffsetTop = -64; caja.OffsetBottom = -64;
		interfaz.AddChild(caja);
		_caja = caja;
		var columna = new VBoxContainer();
		columna.AddThemeConstantOverride("separation", 8);
		caja.AddChild(columna);
		_descripcion = Etiqueta(Mono, 17, Tinta);
		_descripcion.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_descripcion.CustomMinimumSize = new Vector2(828, 0);
		columna.AddChild(_descripcion);
		_lugar = Etiqueta(Mono, 12, new Color("75654e"));
		columna.AddChild(_lugar);

		var revisar = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		revisar.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.04f, 0.04f, 0.05f, 0.85f), ContentMarginLeft = 8, ContentMarginRight = 12,
			ContentMarginTop = 3, ContentMarginBottom = 4, BorderWidthLeft = 3, BorderColor = Ambar,
			CornerRadiusTopRight = 3, CornerRadiusBottomRight = 3,
		});
		var filaRevisar = new HBoxContainer();
		filaRevisar.AddThemeConstantOverride("separation", 6);
		var lupaChica = new LupaExamen { CustomMinimumSize = new Vector2(26, 26), Escala = 0.42f };
		filaRevisar.AddChild(lupaChica);
		var textoRevisar = Etiqueta(Mono, 14, Blanco);
		textoRevisar.Text = "Revisar detalle";
		textoRevisar.VerticalAlignment = VerticalAlignment.Center;
		filaRevisar.AddChild(textoRevisar);
		revisar.AddChild(filaRevisar);
		interfaz.AddChild(revisar);
		_revisar = revisar;

		_ayuda = Etiqueta(Mono, 16, Blanco);
		_ayuda.Text = "↻  Arrastra para girar el objeto y revisarlo por todos lados";
		_ayuda.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		_ayuda.HorizontalAlignment = HorizontalAlignment.Center;
		_ayuda.GrowHorizontal = Control.GrowDirection.Both;
		_ayuda.Position = new Vector2(_ayuda.Position.X, 190);
		_ayuda.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
		_ayuda.AddThemeConstantOverride("outline_size", 5);
		interfaz.AddChild(_ayuda);


		_barra = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_barra.AddThemeConstantOverride("separation", 18);
		_barra.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		_barra.OffsetTop = -44; _barra.OffsetBottom = -14;
		interfaz.AddChild(_barra);

		_destello = new ColorRect { Color = new Color(1, 1, 1, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
		_destello.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		interfaz.AddChild(_destello);
	}

	private static Label Etiqueta(Font fuente, int tam, Color color)
	{
		var etiqueta = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
		etiqueta.AddThemeFontOverride("font", fuente);
		etiqueta.AddThemeFontSizeOverride("font_size", tam);
		etiqueta.AddThemeColorOverride("font_color", color);
		return etiqueta;
	}

	// Barra de controles con teclas o botones según lo último que se usó. Solo lo que sirve en esta pantalla:
	// revisar un detalle no tiene botón fijo (es un clic sobre él) y «Mover» aparece solo con zoom.
	private void ActualizarBarra(bool mando)
	{
		_barraMando = mando;
		_barraConMover = _zoomObjetivo > 1.01f;
		foreach (Node hijo in _barra.GetChildren()) hijo.QueueFree();
		var controles = new List<(string Tecla, string Accion)>
		{
			mando ? ("L", "Girar") : ("Arrastrar", "Girar"),
			mando ? ("LT RT", "Acercar") : ("Rueda", "Acercar"),
		};
		if (_barraConMover) controles.Add(mando ? ("R", "Mover") : ("Clic der.", "Mover"));
		controles.Add(mando ? ("Y", "Linterna") : ("F", "Linterna"));
		controles.Add(mando ? ("X", "Restablecer") : ("R", "Restablecer"));
		controles.Add(mando ? ("B", "Volver") : ("Esc", "Volver"));
		foreach (var (tecla, accion) in controles)
		{
			var grupo = new HBoxContainer();
			grupo.AddThemeConstantOverride("separation", 6);
			var tapa = new PanelContainer();
			tapa.AddThemeStyleboxOverride("panel", new StyleBoxFlat
			{
				BgColor = new Color(Blanco, 0.92f), CornerRadiusTopLeft = mando ? 11 : 3, CornerRadiusTopRight = mando ? 11 : 3,
				CornerRadiusBottomLeft = mando ? 11 : 3, CornerRadiusBottomRight = mando ? 11 : 3,
				ContentMarginLeft = 7, ContentMarginRight = 7, ContentMarginTop = 1, ContentMarginBottom = 1,
				BorderWidthBottom = 3, BorderColor = new Color("8a8f96"),
			});
			var texto = Etiqueta(Mono, 13, Tinta);
			texto.Text = tecla;
			tapa.AddChild(texto);
			grupo.AddChild(tapa);
			var nombre = Etiqueta(Mono, 13, new Color(Blanco, 0.8f));
			nombre.Text = accion;
			grupo.AddChild(nombre);
			_barra.AddChild(grupo);
		}
	}
}

// Lupa del aviso «Revisar detalle», con un leve latido
public partial class LupaExamen : Control
{
	public float Escala = 1f;
	private float _t;
	public override void _Process(double delta) { _t += (float)delta; QueueRedraw(); }
	public override void _Draw()
	{
		float latido = (1f + 0.06f * Mathf.Sin(_t * 6f)) * Escala;
		var centro = new Vector2(30, 30) * Escala;
		var ambar = new Color("efca08");
		DrawArc(centro, 17 * latido, 0, Mathf.Tau, 32, ambar, Mathf.Max(2f, 4 * Escala), true);
		DrawLine(centro + new Vector2(12, 12) * latido, centro + new Vector2(30, 30) * latido, ambar, Mathf.Max(3f, 7 * Escala), true);
	}
}
