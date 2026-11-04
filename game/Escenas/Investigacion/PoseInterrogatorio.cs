using Godot;

// Lo que un implicado hace con el cuerpo en la sala, encima de cualquier animación (la del modelo o la hecha por código):
// se sienta (dobla muslos y rodillas) y desvía la mirada cuando está nervioso. Va como hijo del esqueleto, así funciona
// igual con los tres tipos de esqueleto del juego y el torso sigue con su reposo, Tenso o Alterado.
public partial class PoseInterrogatorio : SkeletonModifier3D
{
	private const float AnguloMuslo = 82f, AnguloRodilla = 82f;

	// 0 = de pie, 1 = sentado
	public float Sentado { get; set; }
	// Cuánto se aparta de la mirada del detective, en grados (+ = a su izquierda)
	public float Desvio { get; set; }

	private Node3D _personaje;
	private int _musloI = -1, _musloD = -1, _rodillaI = -1, _rodillaD = -1, _cuello = -1;

	public static PoseInterrogatorio Poner(Node3D personaje, Node3D modelo)
	{
		Skeleton3D esqueleto = null;
		foreach (Node n in modelo.FindChildren("*", "Skeleton3D", true, false))
			if (n is Skeleton3D s && s.GetBoneCount() > (esqueleto?.GetBoneCount() ?? 1)) esqueleto = s;
		if (esqueleto == null) return null;
		var pose = new PoseInterrogatorio { Name = "PoseInterrogatorio", _personaje = personaje };
		esqueleto.AddChild(pose);
		pose._musloI = Hueso(esqueleto, "mixamorig_LeftUpLeg", "Leg_1_L");
		pose._musloD = Hueso(esqueleto, "mixamorig_RightUpLeg", "Leg_1_R");
		pose._rodillaI = Hueso(esqueleto, "mixamorig_LeftLeg", "Leg_2_L");
		pose._rodillaD = Hueso(esqueleto, "mixamorig_RightLeg", "Leg_2_R");
		pose._cuello = Hueso(esqueleto, "mixamorig_Neck", "Neck");
		return pose;
	}

	private static int Hueso(Skeleton3D esqueleto, params string[] nombres)
	{
		foreach (string nombre in nombres)
		{
			int i = esqueleto.FindBone(nombre);
			if (i >= 0) return i;
		}
		return -1;
	}

	// El hueso de la cadera (para colocar al personaje sobre el asiento)
	public static Vector3 PosicionCadera(Node3D modelo)
	{
		foreach (Node n in modelo.FindChildren("*", "Skeleton3D", true, false))
		{
			if (n is not Skeleton3D s || s.GetBoneCount() < 2) continue;
			int i = Hueso(s, "mixamorig_Hips", "Skl_Root", "Hips");
			if (i >= 0) return s.GlobalTransform * s.GetBoneGlobalPose(i).Origin;
		}
		return modelo.GlobalPosition;
	}

	public override void _ProcessModificationWithDelta(double delta)
	{
		var esqueleto = GetSkeleton();
		if (esqueleto == null || !IsInstanceValid(_personaje)) return;
		// Los ejes del personaje (+Z al frente) en el espacio del esqueleto
		Basis b = esqueleto.GlobalBasis.Orthonormalized().Inverse() * _personaje.GlobalBasis.Orthonormalized();
		Vector3 lado = (b * Vector3.Right).Normalized(), arriba = (b * Vector3.Up).Normalized();
		if (Sentado > 0.001f)
		{
			var muslo = new Quaternion(lado, Mathf.DegToRad(-AnguloMuslo * Sentado));
			var rodilla = new Quaternion(lado, Mathf.DegToRad(AnguloRodilla * Sentado));
			Girar(esqueleto, _musloI, muslo);
			Girar(esqueleto, _musloD, muslo);
			Girar(esqueleto, _rodillaI, rodilla);
			Girar(esqueleto, _rodillaD, rodilla);
		}
		if (Mathf.Abs(Desvio) > 0.01f) Girar(esqueleto, _cuello, new Quaternion(arriba, Mathf.DegToRad(Desvio)));
	}

	// Gira el hueso en el espacio del esqueleto, sin cambiar a su padre
	private static void Girar(Skeleton3D esqueleto, int hueso, Quaternion giro)
	{
		if (hueso < 0) return;
		int padre = esqueleto.GetBoneParent(hueso);
		Quaternion arriba = padre >= 0 ? esqueleto.GetBoneGlobalPose(padre).Basis.GetRotationQuaternion().Normalized() : Quaternion.Identity;
		Quaternion actual = arriba * esqueleto.GetBonePoseRotation(hueso);
		esqueleto.SetBonePoseRotation(hueso, (arriba.Inverse() * (giro * actual)).Normalized());
	}
}
