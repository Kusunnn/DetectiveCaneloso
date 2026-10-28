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
	// Huecos redondos: x, z (local) y radio en z del Vector3 → (x, z, radio)
	[Export] public Godot.Collections.Array<Vector3> HuecosRedondos = new();
	// Terreno con relieve (HU-20.6, Lago): cada copia se apoya en la altura del HeightMapShape3D. Origen = dónde está
	// el centro de la forma (en local). Las copias que caerían más abajo de AlturaMinima (agua, orilla) no se siembran.
	[Export] public HeightMapShape3D Alturas;
	[Export] public Vector3 OrigenAlturas = Vector3.Zero;
	[Export] public float AlturaMinima = -1000f;
	[Export] public float AlturaMaxima = 1000f;
	// Material para todas las copias (por ejemplo, juncos que se mecen con el viento)
	[Export] public Material MaterialCopias;

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
			float y = AlturaEn(p);
			if (y < AlturaMinima || y > AlturaMaxima) continue;
			float s = azar.RandfRange(Escala.X, Escala.Y);
			var basis = new Basis(Vector3.Up, azar.RandfRange(0, Mathf.Tau)).Scaled(new Vector3(s, s * azar.RandfRange(1f, EstiramientoAlto), s));
			int modelo = azar.RandiRange(0, Modelos.Length - 1);
			var pos = new Vector3(p.X, y, p.Y);
			posiciones[modelo].Add(new Transform3D(basis, pos));
			if (RadioTronco > 0 && ZonaColision.HasPoint(p)) troncos.Add((pos, s));
			colocados++;
		}

		for (int i = 0; i < Modelos.Length; i++)
		{
			if (Modelos[i] == null || posiciones[i].Count == 0) continue;
			var (malla, local) = BuscarMalla(Modelos[i]);
			if (malla == null) continue;
			// La base del modelo queda en Y = 0 aunque el pivote del archivo no esté ahí (Nature Kit lo trae 5 cm arriba)
			local = local.Translated(Vector3.Up * -(local * malla.GetAabb()).Position.Y);
			var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = malla, InstanceCount = posiciones[i].Count };
			for (int j = 0; j < posiciones[i].Count; j++) mm.SetInstanceTransform(j, posiciones[i][j] * local);
			var instancia = new MultiMeshInstance3D
			{
				Multimesh = mm,
				CastShadow = ProyectarSombras ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
				MaterialOverride = MaterialCopias,
			};
			instancia.SetMeta("sembrado", true);
			AddChild(instancia);
		}

		if (troncos.Count > 0)
		{
			var cuerpo = new StaticBody3D { CollisionLayer = CapaColision, CollisionMask = 0 };
			cuerpo.SetMeta("sembrado", true);
			// Los troncos se dibujan con el MultiMesh: la auditoría no debe tomarlos por colisiones invisibles
			cuerpo.AddToGroup("auditoria_invisible_ok");
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
		foreach (var h in HuecosRedondos)
			if (p.DistanceSquaredTo(new Vector2(h.X, h.Y)) < h.Z * h.Z) return true;
		return false;
	}

	// Altura del terreno en un punto (interpolada entre las cuatro esquinas de su celda de 1 m); 0 sin terreno
	private float AlturaEn(Vector2 p)
	{
		if (Alturas == null) return 0f;
		int ancho = Alturas.MapWidth, fondo = Alturas.MapDepth;
		var datos = Alturas.MapData;
		float fx = p.X - OrigenAlturas.X + (ancho - 1) / 2f;
		float fz = p.Y - OrigenAlturas.Z + (fondo - 1) / 2f;
		int i = Mathf.Clamp((int)Mathf.Floor(fx), 0, ancho - 2), j = Mathf.Clamp((int)Mathf.Floor(fz), 0, fondo - 2);
		float tx = Mathf.Clamp(fx - i, 0f, 1f), tz = Mathf.Clamp(fz - j, 0f, 1f);
		float a = Mathf.Lerp(datos[j * ancho + i], datos[j * ancho + i + 1], tx);
		float b = Mathf.Lerp(datos[(j + 1) * ancho + i], datos[(j + 1) * ancho + i + 1], tx);
		return Mathf.Lerp(a, b, tz) + OrigenAlturas.Y;
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
