using Godot;
using System;

// Detective Caneloso: movimiento, cámaras, linterna e interacción con lo que apunta la retícula.
// No conoce el guion del caso: avisa con EvidenciaRegistrada y el caso decide qué pasa.
public partial class Jugador : CharacterBody3D
{
	[Signal] public delegate void EvidenciaRegistradaEventHandler(string id);

	[Export] public float Velocidad = 3.0f;
	[Export] public float SensibilidadRaton = 0.003f;
	[Export] public float DistanciaInteraccion = 3.0f;

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

	public bool SeHaMovido { get; private set; } = false;
	public bool LinternaEncendida { get; private set; } = false;

	public override void _Ready()
	{
		AddToGroup("jugador");

		_camara = GetNode<Camera3D>("CamaraInterrogatorio");
		_brazoCamara = GetNode<SpringArm3D>("BrazoCamara");
		_camaraTercera = GetNode<Camera3D>("BrazoCamara/CamaraTerceraPersona");
		_cuerpo = GetNodeOrNull<CuerpoDetective>("Modelo");
		_linterna = GetNodeOrNull<SpotLight3D>("CamaraInterrogatorio/Linterna");
		_brazoCamara.AddExcludedObject(GetRid()); // Que la cámara no choque con el propio jugador
		AplicarPerspectiva();

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
			RotateY(-eventoRaton.Relative.X * SensibilidadRaton);

			float giroVertical = -eventoRaton.Relative.Y * SensibilidadRaton;
			Vector3 rotacionCamara = _camara.Rotation;
			rotacionCamara.X = Mathf.Clamp(rotacionCamara.X + giroVertical, -Mathf.Pi / 2.5f, Mathf.Pi / 2.5f);
			_camara.Rotation = rotacionCamara;

			// En tercera persona se inclina el brazo de la cámara, con un rango más corto para no atravesar el suelo
			Vector3 rotacionBrazo = _brazoCamara.Rotation;
			rotacionBrazo.X = Mathf.Clamp(rotacionBrazo.X + giroVertical, -1.1f, 0.5f);
			_brazoCamara.Rotation = rotacionBrazo;
		}

		// Teclas del protagonista: V cambia de cámara; C agacharse, B bailar y K caerse.
		// La animación de agarrar no tiene tecla: se reproduce al tomar una pista.
		if (@event.IsActionPressed("cambiar_camara"))
		{
			_terceraPersona = !_terceraPersona;
			AplicarPerspectiva();
		}
		else if (@event.IsActionPressed("agacharse")) _cuerpo?.HacerAccion(CuerpoDetective.Agacharse);
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
		return resultado["collider"].As<Node>() as IInteractuable;
	}

	// Añade una evidencia al panel de pistas y avisa al caso
	public void RegistrarEvidencia(string id, string titulo, string descripcion, bool animar = true)
	{
		bool nueva = _hud.Call("registrar_pista", id, titulo, descripcion).AsBool();
		if (!nueva) return;
		if (animar) _cuerpo?.HacerAccion(CuerpoDetective.Agarrar);
		if (GestorPartida.Instancia != null) GestorPartida.Instancia.HayCambiosSinGuardar = true;
		EmitSignal(SignalName.EvidenciaRegistrada, id);
	}

	// Indicación del tutorial: se queda en pantalla hasta la siguiente
	public void MostrarIndicacion(string texto)
	{
		if (_textoTutorial == null) return;
		_idAviso++;
		_textoTutorial.Text = texto;
		_textoTutorial.Visible = true;
	}

	// Mensaje temporal que se oculta solo después de unos segundos
	public void MostrarAviso(string texto, double segundos)
	{
		if (_textoTutorial == null) return;
		int id = ++_idAviso;
		_textoTutorial.Text = texto;
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
			_apuntado = apuntado;
			_hud.Set("interaccion", apuntado?.TextoAccion ?? "");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Esc lo gestiona el menú de pausa (MenuPausa), que libera y restaura el cursor
		if (_panelAbierto || _bloqueado)
		{
			_cuerpo?.ActualizarMovimiento(false);
			return;
		}

		Vector3 velocidadActual = Velocity;

		if (!IsOnFloor())
			velocidadActual.Y -= _gravedad * (float)delta;

		Vector2 entrada = Input.GetVector("mover_izquierda", "mover_derecha", "mover_adelante", "mover_atras");
		Vector3 direccion = (Transform.Basis * new Vector3(entrada.X, 0, entrada.Y)).Normalized();

		if (direccion != Vector3.Zero)
		{
			velocidadActual.X = direccion.X * Velocidad;
			velocidadActual.Z = direccion.Z * Velocidad;
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
