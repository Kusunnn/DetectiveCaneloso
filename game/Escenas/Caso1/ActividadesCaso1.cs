using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Rediseño del Caso 1 (paso 7): cada implicado en su lugar del campamento o del lago, con su actividad y un objeto
// en la mano (o a su lado). Todo se arma por código sobre los nodos que ya existen; las escenas no cambian.
public static class ActividadesCaso1
{
	private const string Prefabs = "res://Assets/Caso1/Modelos/_prefabs/";

	public sealed record Lugar(string Nodo, Vector3 Posicion, Vector3 MiraA, Func<Node3D> Objeto, float Tamano, bool EnLaMano, Vector3 Ajuste);

	// Campamento: zonas de su rol (ver docs/REDISENO_JUGABILIDAD_CASO1.md, 5.2)
	public static readonly Lugar[] Campamento =
	{
		new("JuanCarlos", new(-6.2f, 0, -7.4f), new(-2f, 0, -4f), Guitarra, 0.95f, true, new(0, 0, 0.05f)),
		new("Aya", new(9.5f, 0, -13.5f), new(15f, 0, -21f), Camara, 0.16f, true, Vector3.Zero),
		new("Miaurio", new(21.6f, 0, 24.2f), new(24.3f, 0, 27f), () => Prefab("celular"), 0.14f, true, Vector3.Zero),
		new("Miami", new(22.2f, 0, -23.2f), new(24f, 0, -27f), () => Prefab("botiquin_bolsa"), 0.3f, true, Vector3.Zero),
		new("Tony", new(-25.6f, 0, -13.4f), new(-2f, 0, -4f), () => Prefab("taza_cafe"), 0.11f, true, Vector3.Zero),
		new("Eriz", new(-2.6f, 0, 1.6f), new(-2f, 0, -4f), () => Prefab("saco_extendido"), 1.7f, false, new(0, 0.01f, 0.2f)),
	};


	// Espera un cuadro de física: así se puede buscar el piso y un hueco libre (nada de quedar hundido o dentro de un mueble)
	public static async void Colocar(Node3D personajes, IEnumerable<Lugar> lugares)
	{
		await personajes.ToSignal(personajes.GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!GodotObject.IsInstanceValid(personajes) || !personajes.IsInsideTree()) return;
		foreach (var lugar in lugares)
		{
			if (personajes.GetNodeOrNull<Node3D>(lugar.Nodo) is not { } npc) continue;
			var sitio = personajes.ToLocal(Libre(npc, personajes.ToGlobal(lugar.Posicion)));
			npc.Position = sitio;
			var mira = new Vector3(lugar.MiraA.X, sitio.Y, lugar.MiraA.Z);
			if (mira.DistanceTo(sitio) > 0.1f) npc.LookAt(personajes.ToGlobal(mira), Vector3.Up, useModelFront: true);
			var objeto = lugar.Objeto();
			if (objeto == null) continue;
			objeto.Name = "Objeto";
			if (lugar.EnLaMano && Mano(npc) is { } mano)
			{
				mano.AddChild(objeto);
				objeto.Position = lugar.Ajuste;
			}
			else
			{
				npc.AddChild(objeto);
				objeto.Position = lugar.Ajuste;
			}
			Escalar(objeto, lugar.Tamano);
		}
	}

	// El punto libre más cercano (un hueco del tamaño de una persona) y la altura del piso ahí
	private static Vector3 Libre(Node3D npc, Vector3 deseado)
	{
		var espacio = npc.GetWorld3D().DirectSpaceState;
		var excluir = new Godot.Collections.Array<Rid>(npc.FindChildren("*", "CollisionObject3D", true, false).OfType<CollisionObject3D>().Select(c => c.GetRid()));
		var cuerpo = new PhysicsShapeQueryParameters3D { Shape = new CapsuleShape3D { Radius = 0.35f, Height = 1.5f }, Exclude = excluir, CollisionMask = 1 };
		Vector3 Piso(Vector3 p)
		{
			var rayo = PhysicsRayQueryParameters3D.Create(p + Vector3.Up * 1.6f, p + Vector3.Down * 3f, 1, excluir);
			var choque = espacio.IntersectRay(rayo);
			return choque.Count > 0 ? choque["position"].AsVector3() : p;
		}
		for (float radio = 0; radio <= 4f; radio += 0.5f)
			for (int i = 0; i < (radio == 0 ? 1 : 12); i++)
			{
				var p = Piso(deseado + new Vector3(Mathf.Cos(i * Mathf.Tau / 12), 0, Mathf.Sin(i * Mathf.Tau / 12)) * radio);
				cuerpo.Transform = new Transform3D(Basis.Identity, p + Vector3.Up * 0.95f);
				if (espacio.IntersectShape(cuerpo, 1).Count == 0) return p;
			}
		return Piso(deseado);
	}

	// Un BoneAttachment3D en la mano derecha (los esqueletos se llaman distinto según el modelo)
	private static Node3D Mano(Node3D npc)
	{
		if (npc.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().FirstOrDefault() is not { } esqueleto) return null;
		int hueso = -1;
		foreach (string nombre in new[] { "hand_r", "righthand", "hand.r", "r_hand", "handr", "mano_d" })
		{
			for (int i = 0; i < esqueleto.GetBoneCount() && hueso < 0; i++)
				if (esqueleto.GetBoneName(i).ToLower().Replace("mixamorig:", "").Contains(nombre)) hueso = i;
			if (hueso >= 0) break;
		}
		if (hueso < 0) return null;
		var enlace = new BoneAttachment3D { Name = "ManoDerecha", BoneName = esqueleto.GetBoneName(hueso) };
		esqueleto.AddChild(enlace);
		return enlace;
	}

	// El modelo del personaje está escalado (de 0.03 a 9 veces): el objeto se mide ya puesto y se ajusta a su tamaño real
	private static void Escalar(Node3D objeto, float metros)
	{
		for (int vuelta = 0; vuelta < 2; vuelta++)
		{
			var mallas = objeto.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToList();
			if (objeto is MeshInstance3D propia) mallas.Add(propia);
			if (mallas.Count == 0) return;
			var caja = mallas[0].GlobalTransform * mallas[0].GetAabb();
			foreach (var m in mallas.Skip(1)) caja = caja.Merge(m.GlobalTransform * m.GetAabb());
			float mayor = Mathf.Max(caja.Size.X, Mathf.Max(caja.Size.Y, caja.Size.Z));
			if (mayor < 0.0001f) return;
			objeto.Scale *= metros / mayor;
		}
	}

	private static Node3D Prefab(string nombre)
	{
		string ruta = Prefabs + nombre + ".tscn";
		if (!ResourceLoader.Exists(ruta)) return null;
		var nodo = GD.Load<PackedScene>(ruta).Instantiate<Node3D>();
		// Que no estorbe al caminar ni al apuntar
		foreach (var cuerpo in nodo.FindChildren("*", "CollisionObject3D", true, false).OfType<CollisionObject3D>())
		{
			cuerpo.CollisionLayer = 0;
			cuerpo.CollisionMask = 0;
		}
		return nodo;
	}

	// No hay modelos de cámara ni de guitarra: se arman con piezas simples
	private static Node3D Camara() => Piezas(
		(new BoxMesh { Size = new Vector3(0.14f, 0.09f, 0.07f) }, new Color(0.08f, 0.08f, 0.09f), Vector3.Zero, Vector3.Zero),
		(new CylinderMesh { TopRadius = 0.03f, BottomRadius = 0.033f, Height = 0.06f }, new Color(0.15f, 0.15f, 0.17f), new Vector3(0, 0, 0.06f), new Vector3(90, 0, 0)));

	private static Node3D Guitarra() => Piezas(
		(new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.2f, Height = 0.1f }, new Color(0.55f, 0.32f, 0.12f), Vector3.Zero, new Vector3(90, 0, 0)),
		(new CylinderMesh { TopRadius = 0.16f, BottomRadius = 0.16f, Height = 0.1f }, new Color(0.55f, 0.32f, 0.12f), new Vector3(0, 0.25f, 0), new Vector3(90, 0, 0)),
		(new BoxMesh { Size = new Vector3(0.06f, 0.5f, 0.03f) }, new Color(0.25f, 0.14f, 0.06f), new Vector3(0, 0.6f, 0), Vector3.Zero),
		(new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.05f, Height = 0.105f }, new Color(0.05f, 0.04f, 0.03f), new Vector3(0, 0.08f, 0), new Vector3(90, 0, 0)));

	private static Node3D Piezas(params (PrimitiveMesh Malla, Color Color, Vector3 Posicion, Vector3 Giro)[] piezas)
	{
		var raiz = new Node3D();
		foreach (var (malla, color, posicion, giro) in piezas)
		{
			malla.Material = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.7f };
			raiz.AddChild(new MeshInstance3D { Mesh = malla, Position = posicion, RotationDegrees = giro });
		}
		return raiz;
	}
}
