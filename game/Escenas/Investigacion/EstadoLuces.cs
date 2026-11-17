using Godot;
using System;

// Dos estados de iluminación de un espacio: «normal» (cálido) y «hackeado» (frío y rojo). Solo enciende y apaga
// grupos de nodos y cambia el entorno; quién decide cuándo cambiar es el guion del caso.
public partial class EstadoLuces : Node
{
	[Signal] public delegate void EstadoCambiadoEventHandler(bool hackeado);

	// Lo que solo existe en el estado normal (luces cálidas, sol, pantallas encendidas…)
	[Export] public Node3D[] SoloNormal = Array.Empty<Node3D>();
	// Lo que solo existe hackeado (persianas metálicas abajo, luces de emergencia, pantallas con las tres rayas…)
	[Export] public Node3D[] SoloHackeado = Array.Empty<Node3D>();
	[Export] public WorldEnvironment Entorno;
	[Export] public Color AmbienteHackeado = new Color(0.32f, 0.36f, 0.5f);
	[Export] public float EnergiaAmbienteHackeado = 0.35f;
	[Export] public bool EmpiezaHackeado = false;

	public bool Hackeado { get; private set; }

	private Color _ambienteNormal;
	private float _energiaNormal;

	public override void _Ready()
	{
		if (Entorno?.Environment != null)
		{
			_ambienteNormal = Entorno.Environment.AmbientLightColor;
			_energiaNormal = Entorno.Environment.AmbientLightEnergy;
		}
		// Para revisar el estado hackeado sin guion: el juego se abre con «-- --estado=hackeado»
		bool porArgumento = Array.IndexOf(OS.GetCmdlineUserArgs(), "--estado=hackeado") >= 0;
		FijarEstado(EmpiezaHackeado || porArgumento);
	}

	public void FijarEstado(bool hackeado)
	{
		Hackeado = hackeado;
		foreach (var n in SoloNormal) if (n != null) n.Visible = !hackeado;
		foreach (var n in SoloHackeado) if (n != null) n.Visible = hackeado;
		var entorno = Entorno?.Environment;
		if (entorno != null)
		{
			entorno.AmbientLightColor = hackeado ? AmbienteHackeado : _ambienteNormal;
			entorno.AmbientLightEnergy = hackeado ? EnergiaAmbienteHackeado : _energiaNormal;
		}
		EmitSignal(SignalName.EstadoCambiado, hackeado);
	}

	public void Alternar() => FijarEstado(!Hackeado);
}
