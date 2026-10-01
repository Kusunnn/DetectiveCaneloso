using Godot;
using System;

// Detective Caneloso: movimiento, cámaras, linterna e interacción con lo que apunta la retícula.
// No conoce el guion del caso: avisa con EvidenciaRegistrada y el caso decide qué pasa.
public partial class Jugador : CharacterBody3D
{
	[Signal] public delegate void EvidenciaRegistradaEventHandler(string id);
	[Signal] public delegate void ObjetoObtenidoEventHandler(string id);

	[Export] public float Velocidad = 3.0f;
	[Export] public float SensibilidadRaton = 0.003f;
	[Export] public float DistanciaInteraccion = 3.0f;
	// Agacharse: velocidad relativa y alturas de la cápsula (de pie / agachado)
	[Export] public float FactorVelocidadAgachado = 0.45f;
	[Export] public float FactorVelocidadCorrer = 1.6f;
	private const float AlturaDePie = 2.0f;
	private const float AlturaAgachado = 0.9f;
	private const float OjosDePie = 0.6f;
	private const float OjosAgachado = -0.25f;
	private const float BrazoDePie = 0.8f;
	private const float BrazoAgachado = 0.1f;

	private float _gravedad = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private Camera3D _camara;
	private Camera3D _camaraTercera;
	private SpringArm3D _brazoCamara;
	private CuerpoDetective _cuerpo;
	private SpotLight3D _linterna;
	private bool _terceraPersona = false;
	private Label _textoTutorial;
	private Node _hud;

	private bool _panelAbierto = false;   // Diario o expediente del HUD
	private bool _bloqueado = false;      // Diálogo en curso
	private int _idAviso = 0;             // Evita que un temporizador viejo oculte un texto nuevo
	private IInteractuable _apuntado;
	private CollisionShape3D _colision;
	private CapsuleShape3D _capsula;
	private bool _quiereAgacharse = false;   // Solo se usa en modo alternar
	private float _mezclaAgachado = 0f;      // 0 = de pie, 1 = agachado (transición suave)
	private readonly System.Collections.Generic.HashSet<ulong> _comentados = new System.Collections.Generic.HashSet<ulong>();
	private Material _materialResaltado;
	// Llaves, combinaciones... que abren contenedores en otras zonas
	private readonly System.Collections.Generic.HashSet<string> _objetos = new System.Collections.Generic.HashSet<string>();

	private static readonly Color ColorPensamiento = new Color(0.96f, 0.9f, 0.72f);
	private static readonly Color ColorAviso = new Color(1f, 1f, 1f);

	public bool SeHaMovido { get; private set; } = false;
	public bool LinternaEncendida { get; private set; } = false;
	public bool Agachado { get; private set; } = false;

	public override void _Ready()
	{
		AddToGroup("jugador");

		_camara = GetNode<Camera3D>("CamaraInterrogatorio");
		_brazoCamara = GetNode<SpringArm3D>("BrazoCamara");
		_camaraTercera = GetNode<Camera3D>("BrazoCamara/CamaraTerceraPersona");
		_cuerpo = GetNodeOrNull<CuerpoDetective>("Modelo");
		_linterna = GetNodeOrNull<SpotLight3D>("CamaraInterrogatorio/Linterna");
		_brazoCamara.AddExcludedObject(GetRid()); // Que la cámara no choque con el propio jugador
		_colision = GetNode<CollisionShape3D>("CollisionShape3D");
		_capsula = (CapsuleShape3D)_colision.Shape.Duplicate(); // Propia, porque su altura cambia al agacharse
		_colision.Shape = _capsula;
		AplicarPerspectiva();
		_materialResaltado = new ShaderMaterial { Shader = GD.Load<Shader>("res://Escenas/Investigacion/resaltado.gdshader") };

		// Rutas relativas al padre del Jugador (la escena del caso)
		Node sala = GetParent();
		_textoTutorial = sala.GetNodeOrNull<Label>("CanvasLayer/TextoTutorial");
		if (_textoTutorial != null) _textoTutorial.Visible = false;
		_hud = sala.GetNode("HUD");
		_hud.Connect("panel_cambiado", Callable.From<bool, bool>(AlCambiarPanel));
		_hud.Connect("linterna_cambiada", Callable.From<bool>(AlCambiarLinterna));
		AlCambiarLinterna(false);

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// Con el diario abierto o en un diálogo, el detective no hace nada más
		if (_panelAbierto || _bloqueado) return;

		// Alternar el cursor con Clic Derecho
		if (@event is InputEventMouseButton boton && boton.ButtonIndex == MouseButton.Right && boton.Pressed)
		{
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
		}

		// Rotación de la vista con el ratón
		if (@event is InputEventMouseMotion eventoRaton && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			float sensibilidad = SensibilidadRaton * (Configuracion.Instancia?.SensibilidadRaton ?? 1f);
			RotateY(-eventoRaton.Relative.X * sensibilidad);

			float giroVertical = -eventoRaton.Relative.Y * sensibilidad;
			Vector3 rotacionCamara = _camara.Rotation;
			rotacionCamara.X = Mathf.Clamp(rotacionCamara.X + giroVertical, -Mathf.Pi / 2.5f, Mathf.Pi / 2.5f);
			_camara.Rotation = rotacionCamara;

			// En tercera persona se inclina el brazo de la cámara, con un rango más corto para no atravesar el suelo
			Vector3 rotacionBrazo = _brazoCamara.Rotation;
			rotacionBrazo.X = Mathf.Clamp(rotacionBrazo.X + giroVertical, -1.1f, 0.5f);
			_brazoCamara.Rotation = rotacionBrazo;
		}

		// Teclas del protagonista: V cambia de cámara; C o Ctrl agacharse; B bailar y K caerse.
		// La animación de agarrar no tiene tecla: se reproduce al tomar una pista.
		if (@event.IsActionPressed("cambiar_camara"))
		{
			_terceraPersona = !_terceraPersona;
			AplicarPerspectiva();
		}
		else if (@event.IsActionPressed("agacharse") && ModoAlternar) _quiereAgacharse = !_quiereAgacharse;
		else if (@event.IsActionPressed("bailar")) _cuerpo?.HacerAccion(CuerpoDetective.Bailar);
		else if (@event.IsActionPressed("morir")) _cuerpo?.HacerAccion(CuerpoDetective.Morir);

		// [E] o clic izquierdo: usar lo que apunta la retícula
		bool clic = @event is InputEventMouseButton clicIzq && clicIzq.ButtonIndex == MouseButton.Left && clicIzq.Pressed;
		if ((clic || @event.IsActionPressed("interactuar")) && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			IInteractuable objetivo = BuscarInteractuable();
			if (objetivo != null)
			{
				GetViewport().SetInputAsHandled();
				objetivo.Interactuar(this);
			}
		}
	}

	// Activa la cámara de primera o tercera persona y muestra u oculta el cuerpo de Caneloso
	private void AplicarPerspectiva()
	{
		_camara.Current = !_terceraPersona;
		_camaraTercera.Current = _terceraPersona;
		_cuerpo?.MostrarEnPrimeraPersona(!_terceraPersona);
	}

	private Camera3D CamaraActiva => _terceraPersona ? _camaraTercera : _camara;

	private void AlCambiarPanel(bool abierto, bool expediente)
	{
		_panelAbierto = abierto;
		Input.MouseMode = abierto ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
		if (abierto) Detenerse();
	}

	private void AlCambiarLinterna(bool encendida)
	{
		LinternaEncendida = encendida;
		if (_linterna != null) _linterna.Visible = encendida;
		// Lo que solo se ve con luz directa (huellas...) aparece con la linterna
		foreach (Node nodo in GetTree().GetNodesInGroup("solo_con_linterna"))
		{
			if (nodo is Node3D objeto) objeto.Visible = encendida;
		}
	}

	// Lo usa la ventana de diálogo: congela al detective mientras se habla
	public void Bloquear(bool bloqueado)
	{
		_bloqueado = bloqueado;
		if (bloqueado) Detenerse();
		// Durante el diálogo se oculta el HUD para que no tape el texto
		if (_hud is CanvasLayer capa) capa.Visible = !bloqueado;
		if (bloqueado && _textoTutorial != null) _textoTutorial.Visible = false;
	}

	public bool TieneObjeto(string id) => _objetos.Contains(id);

	public void DarObjeto(string id, string pensamiento)
	{
		if (!_objetos.Add(id)) return;
		Sonidos.Reproducir(this, Sonidos.Tipo.Pista, GlobalPosition, -6f);
		MostrarPensamiento(pensamiento, 5.0);
		EmitSignal(SignalName.ObjetoObtenido, id);
	}

	private void Detenerse()
	{
		Velocity = Vector3.Zero;
		_hud.Call("actualizar_movimiento", false);
		_cuerpo?.ActualizarMovimiento(false);
	}

	// Rayo desde el centro de la cámara activa; devuelve lo que se puede usar, o null
	private IInteractuable BuscarInteractuable()
	{
		var espacioFisico = GetWorld3D().DirectSpaceState;
		Vector2 centroPantalla = GetViewport().GetVisibleRect().Size / 2;
		// En tercera persona la cámara está detrás del detective, así que el rayo se alarga esa distancia
		Camera3D camara = CamaraActiva;
		float alcance = DistanciaInteraccion + (_terceraPersona ? camara.GlobalPosition.DistanceTo(_camara.GlobalPosition) : 0f);
		Vector3 origen = camara.ProjectRayOrigin(centroPantalla);
		Vector3 destino = origen + camara.ProjectRayNormal(centroPantalla) * alcance;

		var query = PhysicsRayQueryParameters3D.Create(origen, destino);
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		var resultado = espacioFisico.IntersectRay(query);
		if (resultado.Count == 0) return null;
		// La pieza golpeada puede ser parte de algo mayor (la puerta de un casillero): se sube hasta encontrarlo
		Node nodo = resultado["collider"].As<Node>();
		for (int nivel = 0; nivel < 4 && nodo != null; nivel++, nodo = nodo.GetParent())
		{
			if (nodo is Contenedor { SoloAgachado: true } && !Agachado) return null; // Hay que agacharse para alcanzarlo
			if (nodo is IInteractuable interactuable) return interactuable;
		}
		return null;
	}

	// Añade una evidencia al panel de pistas y avisa al caso
	// Usa el sistema de pistas del HUD del equipo (aviso "Expediente actualizado", tarjeta y expediente)
	public void RegistrarEvidencia(string id, string titulo, string descripcion, Texture2D imagen = null, bool animar = true)
	{
		// Si el objeto no trae foto, se usa la de Assets/Pistas/<id>.png
		string rutaFoto = "res://Assets/Pistas/" + id + ".png";
		if (imagen == null && ResourceLoader.Exists(rutaFoto)) imagen = GD.Load<Texture2D>(rutaFoto);
		var pista = (Resource)GD.Load<GDScript>("res://hud/pista.gd").New();
		pista.Set("id", new StringName(id));
		pista.Set("titulo", titulo);
		pista.Set("descripcion", descripcion);
		pista.Set("imagen", imagen);
		bool nueva = _hud.Call("agregar_pista", pista).AsBool();
		if (!nueva) return;
		if (animar) _cuerpo?.HacerAccion(CuerpoDetective.Agarrar);
		Sonidos.Reproducir(this, Sonidos.Tipo.Pista, GlobalPosition);
		if (GestorPartida.Instancia != null) GestorPartida.Instancia.HayCambiosSinGuardar = true;
		EmitSignal(SignalName.EvidenciaRegistrada, id);
	}

	// Pensamiento del detective que guía el tutorial: se queda hasta el siguiente
	public void MostrarIndicacion(string texto)
	{
		if (_textoTutorial == null) return;
		_idAviso++;
		_textoTutorial.Text = "“" + texto + "”";
		_textoTutorial.Modulate = ColorPensamiento;
		_textoTutorial.Visible = !_bloqueado;
	}

	// Pensamiento corto que se oculta solo (al abrir un casillero, al examinar algo...)
	public void MostrarPensamiento(string texto, double segundos)
	{
		MostrarTemporal("“" + texto + "”", ColorPensamiento, segundos);
	}

	// Aviso del juego (no es el detective quien habla): "Nueva zona disponible", "Caso resuelto"...
	public void MostrarAviso(string texto, double segundos)
	{
		MostrarTemporal(texto, ColorAviso, segundos);
	}

	private void MostrarTemporal(string texto, Color color, double segundos)
	{
		if (_textoTutorial == null) return;
		int id = ++_idAviso;
		_textoTutorial.Text = texto;
		_textoTutorial.Modulate = color;
		_textoTutorial.Visible = true;

		GetTree().CreateTimer(segundos).Timeout += () =>
		{
			if (id == _idAviso && IsInstanceValid(_textoTutorial))
			{
				_textoTutorial.Visible = false;
			}
		};
	}

	public override void _Process(double delta)
	{
		// El HUD muestra "[ E ] acción" cuando la retícula apunta a algo usable
		IInteractuable apuntado = (_panelAbierto || _bloqueado) ? null : BuscarInteractuable();
		if (apuntado != _apuntado)
		{
			Resaltar(_apuntado, false);
			_apuntado = apuntado;
			Resaltar(_apuntado, true);
			if (apuntado != null) Sonidos.Reproducir(this, Sonidos.Tipo.Paso, GlobalPosition, -14f);
			if (apuntado is ObjetoPista objeto && !string.IsNullOrEmpty(objeto.ComentarioAlMirar) && _comentados.Add(objeto.GetInstanceId()))
				MostrarPensamiento(objeto.ComentarioAlMirar, 4.0);
		}
		// Se actualiza siempre: el texto cambia al abrir un contenedor ("Abrir" → "Revisado")
		_hud.Set("interaccion", _apuntado?.TextoAccion ?? "");
	}

	// Borde brillante en el objeto que apunta la retícula
	private void Resaltar(IInteractuable objetivo, bool activo)
	{
		if (objetivo is not Node nodo || !IsInstanceValid(nodo)) return;
		foreach (Node hijo in nodo.FindChildren("*", "GeometryInstance3D", true, false))
		{
			if (hijo is GeometryInstance3D geometria && hijo is not Label3D)
				geometria.MaterialOverlay = activo ? _materialResaltado : null;
		}
	}

	private bool ModoAlternar => Configuracion.Instancia?.AgacharseAlternar ?? false;

	// Agacharse: la cápsula, la cámara y el cuerpo pasan de una altura a otra de forma gradual.
	// No se puede levantar si hay algo encima (una mesa, el andamio del pasillo...).
	private void ActualizarAgachado(float delta)
	{
		bool quiere = ModoAlternar ? _quiereAgacharse : Input.IsActionPressed("agacharse");
		if (quiere) Agachado = true;
		else if (Agachado && HayEspacioParaLevantarse()) Agachado = false;
		if (!ModoAlternar) _quiereAgacharse = Agachado;

		_mezclaAgachado = Mathf.MoveToward(_mezclaAgachado, Agachado ? 1f : 0f, delta * 4f);
		float t = Mathf.SmoothStep(0f, 1f, _mezclaAgachado);
		_capsula.Height = Mathf.Lerp(AlturaDePie, AlturaAgachado, t);
		// Los pies quedan siempre en el mismo sitio (1 m por debajo del centro del jugador)
		_colision.Position = new Vector3(0, -1f + _capsula.Height / 2f, 0);
		_camara.Position = new Vector3(_camara.Position.X, Mathf.Lerp(OjosDePie, OjosAgachado, t), _camara.Position.Z);
		_brazoCamara.Position = new Vector3(_brazoCamara.Position.X, Mathf.Lerp(BrazoDePie, BrazoAgachado, t), _brazoCamara.Position.Z);
		_cuerpo?.FijarAgachado(Agachado);
	}

	private bool HayEspacioParaLevantarse()
	{
		var forma = new CapsuleShape3D { Radius = _capsula.Radius - 0.02f, Height = AlturaDePie - 0.05f };
		var consulta = new PhysicsShapeQueryParameters3D
		{
			Shape = forma,
			Transform = new Transform3D(Basis.Identity, GlobalPosition + new Vector3(0, 0.03f, 0)),
			CollisionMask = CollisionMask,
			Exclude = new Godot.Collections.Array<Rid> { GetRid() },
		};
		return GetWorld3D().DirectSpaceState.IntersectShape(consulta, 1).Count == 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Esc lo gestiona el menú de pausa (MenuPausa), que libera y restaura el cursor
		if (_panelAbierto || _bloqueado)
		{
			_cuerpo?.ActualizarMovimiento(false);
			return;
		}

		ActualizarAgachado((float)delta);
		Vector3 velocidadActual = Velocity;
		float velocidad = Velocidad * Mathf.Lerp(1f, FactorVelocidadAgachado, _mezclaAgachado);
		if (Input.IsActionPressed("correr") && !Agachado) velocidad *= FactorVelocidadCorrer;
		if (!IsOnFloor())
			velocidadActual.Y -= _gravedad * (float)delta;

		Vector2 entrada = Input.GetVector("mover_izquierda", "mover_derecha", "mover_adelante", "mover_atras");
		Vector3 direccion = (Transform.Basis * new Vector3(entrada.X, 0, entrada.Y)).Normalized();

		if (direccion != Vector3.Zero)
		{
			velocidadActual.X = direccion.X * velocidad;
			velocidadActual.Z = direccion.Z * velocidad;
			SeHaMovido = true;
		}
		else
		{
			velocidadActual.X = Mathf.MoveToward(Velocity.X, 0, Velocidad);
			velocidadActual.Z = Mathf.MoveToward(Velocity.Z, 0, Velocidad);
		}

		Velocity = velocidadActual;
		MoveAndSlide();
		bool moviendose = new Vector2(Velocity.X, Velocity.Z).LengthSquared() > 0.001f;
		_hud.Call("actualizar_movimiento", moviendose);
		_cuerpo?.ActualizarMovimiento(moviendose);
	}
}
