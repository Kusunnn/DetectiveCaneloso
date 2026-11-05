using Godot;

// HU-9.5 CA-03 (C-05): volver a ver la cinta del Caso 0 sin rejugar el caso. La cinemática mueve los objetos reales de
// la Sala 1, así que aquí se carga una Sala 1 aparte, en su propio mundo y encima de la pantalla que la abrió, con el
// Caso 0 sin arrancar. No da pistas, notas ni objetivos, no guarda y no cuenta en el registro de la partida.
// La pantalla de abajo se queda quieta y al salir sigue exactamente como estaba.
public partial class VerCinta : CanvasLayer
{
	private const string Escena = "res://Escenas/Cinematicas/ver_cinta.tscn";
	private const string Sala = "res://Escenas/Interrogatorio/sala_interrogatorio.tscn";

	[Signal] public delegate void TerminadaEventHandler();

	// Solo una a la vez, incluso durante su fundido final
	public static bool EnCurso => IsInstanceValid(_actual);
	private static VerCinta _actual;

	private SubViewport _mundo;
	private Node _origen;
	private ProcessModeEnum _modoOrigen;
	private Control _focoOrigen;
	private Input.MouseModeEnum _raton;

	// origen: la pantalla que la abre (se queda en pausa mientras dura)
	public static VerCinta Abrir(Node origen)
	{
		if (EnCurso || CinematicaCinta.EnCurso) return null;
		var ver = GD.Load<PackedScene>(Escena).Instantiate<VerCinta>();
		ver._origen = origen;
		_actual = ver;
		origen.GetTree().Root.AddChild(ver);
		return ver;
	}

	public override void _Ready()
	{
		_mundo = GetNode<SubViewport>("Mundo/Vista");
		// La entrada se la pasa _Input: toda va a la cinemática y nada a la pantalla de abajo
		var contenedor = GetNode<SubViewportContainer>("Mundo");
		contenedor.SetProcessInput(false);
		contenedor.SetProcessUnhandledInput(false);
		contenedor.MouseFilter = Control.MouseFilterEnum.Ignore;
		_raton = Input.MouseMode;
		_focoOrigen = GetViewport().GuiGetFocusOwner();
		GetViewport().GuiReleaseFocus();
		if (_origen != null)
		{
			_modoOrigen = _origen.ProcessMode;
			_origen.ProcessMode = ProcessModeEnum.Disabled;
		}
		Reproducir();
	}

	private async void Reproducir()
	{
		var sala = GD.Load<PackedScene>(Sala).Instantiate<Node3D>();
		var caso = sala.GetNode<Caso0>("Caso0");
		caso.SoloVer = true;
		sala.GetNode("MenuPausa").ProcessMode = ProcessModeEnum.Disabled;
		_mundo.AddChild(sala);
		caso.Jugador.ProcessMode = ProcessModeEnum.Disabled;
		// Como al tener la cinta: Eriz y Porky esperan en la Sala 1
		caso.Eriz.GlobalTransform = caso.PuntoErizFinal.GlobalTransform;
		caso.Porky.GlobalTransform = caso.PuntoPorkyFinal.GlobalTransform;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		GetNode<CanvasItem>("Mundo").Show();
		await CinematicaCinta.Reproducir(sala, new CinematicaCinta.Actores
		{
			Porky = caso.Porky, Eriz = caso.Eriz, Jugador = caso.Jugador, Reloj = caso.RelojSala, Grabadora = caso.GrabadoraSala, Musica = caso.Musica,
			Lineas = Caso0.LineasCinta,
		}, registrar: false);
		Salir();
	}

	public override void _Input(InputEvent evento)
	{
		_mundo.PushInput(evento, true);
		GetViewport().SetInputAsHandled();
	}

	private void Salir()
	{
		if (IsInstanceValid(_origen)) _origen.ProcessMode = _modoOrigen;
		Input.MouseMode = _raton;
		if (IsInstanceValid(_focoOrigen)) _focoOrigen.GrabFocus();
		EmitSignal(SignalName.Terminada);
		QueueFree();
	}

	public override void _ExitTree()
	{
		if (_actual == this) _actual = null;
	}
}
