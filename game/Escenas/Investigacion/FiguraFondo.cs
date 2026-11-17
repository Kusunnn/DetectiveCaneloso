using Godot;
using System.Linq;

// Personaje de fondo sin modelo propio (recepcionista, guardia, técnicos): el maniquí de Quaternius pintado de un
// color, con una animación de reposo en bucle. Es un marcador temporal hasta que exista su modelo; no habla.
public partial class FiguraFondo : Node3D
{
	// Color del cuerpo (uniforme) y de lo que se ve como piel o cabeza
	[Export] public Color ColorCuerpo = new Color(0.16f, 0.2f, 0.3f);
	// Animación del maniquí que se repite (las de la Universal Animation Library: "Idle", "Idle_Talking", "Fixing_Kneeling"…)
	[Export] public string Animacion = "Idle";
	// Desfase para que dos figuras iguales no se muevan al mismo tiempo
	[Export] public float Desfase = 0f;

	public override void _Ready()
	{
		var material = new StandardMaterial3D { AlbedoColor = ColorCuerpo, Roughness = 0.75f };
		foreach (var malla in FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
			malla.MaterialOverride = material;
		var reproductor = FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().FirstOrDefault();
		if (reproductor == null) return;
		var lista = reproductor.GetAnimationList();
		string nombre = lista.FirstOrDefault(a => a == Animacion || a.EndsWith("/" + Animacion)) ?? "";
		if (nombre == "") return;
		reproductor.GetAnimation(nombre).LoopMode = Animation.LoopModeEnum.Linear;
		reproductor.Play(nombre);
		reproductor.Seek(Desfase, true);
	}
}
