using Godot;
using System;
using System.Collections.Generic;

// Siembra muchas copias de unos modelos (pinos, pasto, rocas) con MultiMesh: un solo dibujo por modelo,
// así cientos de árboles no bajan los FPS. Cada copia varía en tamaño y giro. La semilla fija el
// resultado, así que el bosque sale igual cada vez y también se ve en el editor.
// Opcional: un cilindro de colisión por copia (solo el tronco) dentro de una zona.
[Tool]
public partial class SembradoMultiMesh : Node3D
{
	[Export] public PackedScene[] Modelos = Array.Empty<PackedScene>();
	[Export] public int Cantidad = 200;
	[Export] public int Semilla = 1;
	// Rectángulo del plano XZ (local) donde se siembra; x, z, ancho, fondo
	[Export] public Rect2 Area = new Rect2(-50, -50, 100, 100);
	// Rectángulos donde no se siembra (el área jugable, caminos, cabañas)
	[Export] public Godot.Collections.Array<Rect2> Huecos = new();
	[Export] public Vector2 Escala = new Vector2(6f, 9f);
	// Además de la escala uniforme, cuánto puede estirarse en alto (1 = nada)
	[Export] public float EstiramientoAlto = 1.15f;
	[Export] public bool ProyectarSombras = true;
	// Colisión del tronco: radio y alto en metros (0 = sin colisión) y solo dentro de este rectángulo
	[Export] public float RadioTronco = 0f;
	[Export] public float AltoTronco = 3f;
	[Export] public Rect2 ZonaColision = new Rect2(0, 0, 0, 0);
	[Export(PropertyHint.Layers3DPhysics)] public uint CapaColision = 1;

	public override void _Ready() => Sembrar();

	public void Sembrar()
	{
		foreach (Node hijo in GetChildren())
		{
			if (hijo.HasMeta("sembrado")) { RemoveChild(hijo); hijo.QueueFree(); }
		}
		if (Modelos.Length == 0 || Cantidad <= 0) return;

		var azar = new RandomNumberGenerator { Seed = (ulong)Semilla };
		var posiciones = new List<Transform3D>[Modelos.Length];
		for (int i = 0; i < Modelos.Length; i++) posiciones[i] = new List<Transform3D>();
		var troncos = new List<(Vector3 pos, float escala)>();

		int colocados = 0, intentos = 0;
		while (colocados < Cantidad && intentos < Cantidad * 20)
		{
			intentos++;
			var p = new Vector2(azar.RandfRange(Area.Position.X, Area.End.X), azar.RandfRange(Area.Position.Y, Area.End.Y));
			if (EnHueco(p)) continue;
			float s = azar.RandfRange(Escala.X, Escala.Y);
			var basis = new Basis(Vector3.Up, azar.RandfRange(0, Mathf.Tau)).Scaled(new Vector3(s, s * azar.RandfRange(1f, EstiramientoAlto), s));
			int modelo = azar.RandiRange(0, Modelos.Length - 1);
			var pos = new Vector3(p.X, 0, p.Y);
			posiciones[modelo].Add(new Transform3D(basis, pos));
			if (RadioTronco > 0 && ZonaColision.HasPoint(p)) troncos.Add((pos, s));
			colocados++;
		}

		for (int i = 0; i < Modelos.Length; i++)
		{
			if (Modelos[i] == null || posiciones[i].Count == 0) continue;
			var (malla, local) = BuscarMalla(Modelos[i]);
			if (malla == null) continue;
			var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = malla, InstanceCount = posiciones[i].Count };
			for (int j = 0; j < posiciones[i].Count; j++) mm.SetInstanceTransform(j, posiciones[i][j] * local);
			var instancia = new MultiMeshInstance3D
			{
				Multimesh = mm,
				CastShadow = ProyectarSombras ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
			};
			instancia.SetMeta("sembrado", true);
			AddChild(instancia);
		}

		if (troncos.Count > 0)
		{
			var cuerpo = new StaticBody3D { CollisionLayer = CapaColision, CollisionMask = 0 };
			cuerpo.SetMeta("sembrado", true);
			AddChild(cuerpo);
			foreach (var (pos, _) in troncos)
			{
				var forma = new CollisionShape3D
				{
					Shape = new CylinderShape3D { Radius = RadioTronco, Height = AltoTronco },
					Position = pos + Vector3.Up * AltoTronco / 2f,
				};
				cuerpo.AddChild(forma);
			}
		}
	}

	private bool EnHueco(Vector2 p)
	{
		foreach (var hueco in Huecos)
			if (hueco.HasPoint(p)) return true;
		return false;
	}

	// La primera malla del modelo importado y su posición dentro de él
	private static (Mesh, Transform3D) BuscarMalla(PackedScene escena)
	{
		Node raiz = escena.Instantiate();
		var resultado = Buscar(raiz, Transform3D.Identity);
		raiz.Free();
		return resultado;
	}

	private static (Mesh, Transform3D) Buscar(Node nodo, Transform3D acumulado)
	{
		if (nodo is Node3D n3) acumulado *= n3.Transform;
		if (nodo is MeshInstance3D mi && mi.Mesh != null) return (mi.Mesh, acumulado);
		foreach (Node hijo in nodo.GetChildren())
		{
			var r = Buscar(hijo, acumulado);
			if (r.Item1 != null) return r;
		}
		return (null, Transform3D.Identity);
	}
}
