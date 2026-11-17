using Godot;
using System.Collections.Generic;
using System.Linq;

// Capítulo 2 · Embarcadero sellado: variante del Lago del Caso 1 (copia con las mismas zonas y terreno; la escena original no se toca).
// Al empezar apaga lo que es del Caso 1 (pistas, contenedores, puertas y puntos de plática): sin colisión y, las pistas,
// ocultas; así el lugar queda como decorado del cierre. No los borra porque otros sistemas (el contorno de las
// pistas) ya los tienen en su lista. Lo del Cap. 2 va en el grupo "caso2" y no se toca.
public partial class VarianteEmbarcadero : Node
{
	public override void _Ready()
	{
		var raiz = GetParent();
		var quitar = new List<Node>();
		var pendientes = new Stack<Node>();
		pendientes.Push(raiz);
		while (pendientes.Count > 0)
		{
			var n = pendientes.Pop();
			if (n.IsInGroup("caso2")) continue;
			if (n is ObjetoPista || n is Contenedor || n is Puerta || n is PuntoInteraccion)
			{
				quitar.Add(n);
				continue;
			}
			foreach (Node hijo in n.GetChildren()) pendientes.Push(hijo);
		}
		foreach (var n in quitar)
		{
			if (n is CollisionObject3D c) { c.CollisionLayer = 0; c.CollisionMask = 0; }
			if (n is ObjetoPista p) p.Visible = false;
		}
	}
}
