using Godot;
using System;
using System.Collections.Generic;

public partial class Jugador : CharacterBody3D
{
	[Export] public float Velocidad = 3.0f;
	[Export] public float SensibilidadRaton = 0.003f;
	[Export] public float DistanciaInteraccion = 3.0f;

	private float _gravedad = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private Camera3D _camara;
	private Label _textoTutorial;
	private Node _hud;

	// Control de estados del tutorial y expediente
	private bool _movimientoRealizado = false;
	private bool _inspeccionRealizada = false;
	private bool _expedienteAbierto = false;
	private bool _tutorialActivo = true;
	private int _idAviso = 0; // Evita que un temporizador viejo oculte un texto nuevo

	// Pistas encontradas, en el orden en que se inspeccionaron
	private readonly List<ObjetoPista> _pistasEncontradas = new List<ObjetoPista>();

	public override void _Ready()
	{
		// Vinculamos la cámara de la escena
		_camara = GetNode<Camera3D>("CamaraInterrogatorio");

		// Rutas relativas al padre del Jugador, así funciona tanto si se ejecuta
		// detective.tscn directamente como si está instanciada dentro de mundo.tscn
		Node sala = GetParent();
		_textoTutorial = sala.GetNodeOrNull<Label>("CanvasLayer/TextoTutorial");
		_hud = sala.GetNode("HUD");
		_hud.Connect("panel_cambiado", Callable.From<bool, bool>(AlCambiarPanel));

		// Si al empezar la partida se eligió "Omitir tutorial" (o ya se completó), no mostramos indicaciones
		_tutorialActivo = !(GestorPartida.Instancia?.SaltarTutorial ?? false);
		if (_tutorialActivo)
		{
			MostrarTutorial("Usa [W, A, S, D] para moverte y el ratón para mirar alrededor.");
		}
		else if (_textoTutorial != null)
		{
			_textoTutorial.Visible = false;
		}

		// Capturar el ratón al iniciar el juego
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// Si el expediente está abierto, bloqueamos el resto de acciones del juego
		if (_expedienteAbierto) return;

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

			Vector3 rotacionCamara = _camara.Rotation;
			rotacionCamara.X -= eventoRaton.Relative.Y * SensibilidadRaton;
			rotacionCamara.X = Mathf.Clamp(rotacionCamara.X, -Mathf.Pi / 2.5f, Mathf.Pi / 2.5f);
			_camara.Rotation = rotacionCamara;
		}

		// Clic izquierdo para inspeccionar (solo con el cursor capturado, apuntando con la retícula)
		if (@event is InputEventMouseButton clicIzq && clicIzq.ButtonIndex == MouseButton.Left && clicIzq.Pressed
			&& Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			IntentarInspeccionarObjeto();
		}
	}

	private void AlCambiarPanel(bool abierto, bool expediente)
	{
		_expedienteAbierto = abierto;
		Input.MouseMode = abierto ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
		if (abierto)
		{
			Velocity = Vector3.Zero;
			_hud.Call("actualizar_movimiento", false);
		}
		if (expediente && _tutorialActivo && _inspeccionRealizada)
			FinalizarTutorial();
	}

	private void IntentarInspeccionarObjeto()
	{
		var espacioFisico = GetWorld3D().DirectSpaceState;
		// Centro del viewport (no de la ventana), que es lo que esperan los métodos Project* de la cámara
		Vector2 centroPantalla = GetViewport().GetVisibleRect().Size / 2;
		Vector3 origen = _camara.ProjectRayOrigin(centroPantalla);
		Vector3 destino = origen + _camara.ProjectRayNormal(centroPantalla) * DistanciaInteraccion;

		var query = PhysicsRayQueryParameters3D.Create(origen, destino);
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		var resultado = espacioFisico.IntersectRay(query);

		if (resultado.Count == 0)
		{
			GD.Print("Clic al aire...");
			return;
		}

		// Solo cuentan los objetos que tienen el script ObjetoPista (mesa, grabadora...)
		if (resultado["collider"].As<Node>() is not ObjetoPista pista)
		{
			GD.Print("Aquí no hay nada relevante.");
			return;
		}

		if (!pista.Inspeccionar())
		{
			GD.Print("Ya examinaste: " + pista.Titulo);
			return;
		}

		GD.Print("¡Pista encontrada: " + pista.Titulo + "!");
		_pistasEncontradas.Add(pista);
		_inspeccionRealizada = true;
		if (GestorPartida.Instancia != null) GestorPartida.Instancia.HayCambiosSinGuardar = true;
		_hud.Call("registrar_pista", pista.GetPath().ToString(), pista.Titulo, pista.Descripcion);

		if (_tutorialActivo)
		{
			MostrarTutorial(_pistasEncontradas.Count == 1
				? "¡Pista encontrada! Presiona CTRL para abrir el expediente."
				: "Nueva pista: " + pista.Titulo + ". Presiona CTRL para abrir el expediente.");
		}
		else
		{
			MostrarAviso("Nueva pista: " + pista.Titulo + ". Presiona CTRL para revisarla.", 4.0);
		}
	}

	// Indicación fija del tutorial (solo si el tutorial está activo)
	private void MostrarTutorial(string texto)
	{
		if (!_tutorialActivo || _textoTutorial == null) return;
		_idAviso++;
		_textoTutorial.Text = texto;
		_textoTutorial.Visible = true;
	}

	// Mensaje temporal que se oculta solo después de unos segundos
	private void MostrarAviso(string texto, double segundos)
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

	private void FinalizarTutorial()
	{
		_tutorialActivo = false;
		MostrarAviso("Tutorial completado. Sigue investigando la sala.", 4.0);

		// Guardamos en la partida que el tutorial ya no hace falta
		if (GestorPartida.Instancia != null)
		{
			GestorPartida.Instancia.SaltarTutorial = true;
			GestorPartida.Instancia.GuardarPartida();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Esc lo gestiona el menú de pausa (MenuPausa), que libera y restaura el cursor

		// Si el expediente está abierto, congelamos el movimiento del jugador
		if (_expedienteAbierto) return;

		Vector3 velocidadActual = Velocity;

		if (!IsOnFloor())
			velocidadActual.Y -= _gravedad * (float)delta;

		Vector2 entrada = Input.GetVector("mover_izquierda", "mover_derecha", "mover_adelante", "mover_atras");
		Vector3 direccion = (Transform.Basis * new Vector3(entrada.X, 0, entrada.Y)).Normalized();

		if (direccion != Vector3.Zero)
		{
			velocidadActual.X = direccion.X * Velocidad;
			velocidadActual.Z = direccion.Z * Velocidad;

			// Actualizar el tutorial al dar el primer paso
			if (!_movimientoRealizado && (Mathf.Abs(entrada.X) > 0 || Mathf.Abs(entrada.Y) > 0))
			{
				_movimientoRealizado = true;
				if (!_inspeccionRealizada)
				{
					MostrarTutorial("Acércate a la mesa, apunta con el punto central y haz Clic Izquierdo para examinarla.");
				}
			}
		}
		else
		{
			velocidadActual.X = Mathf.MoveToward(Velocity.X, 0, Velocidad);
			velocidadActual.Z = Mathf.MoveToward(Velocity.Z, 0, Velocidad);
		}

		Velocity = velocidadActual;
		MoveAndSlide();
		_hud.Call("actualizar_movimiento", new Vector2(Velocity.X, Velocity.Z).LengthSquared() > 0.001f);
	}
}
