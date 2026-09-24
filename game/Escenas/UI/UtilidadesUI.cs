using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Utilidades compartidas por los menús.
public static class UtilidadesUI
{
	// Hace que pasar el ratón por encima de un botón o slider le dé el foco, para que
	// teclado y ratón compartan un único elemento resaltado (CA-04).
	// Solo reacciona al movimiento real del ratón: si una pantalla aparece bajo un cursor
	// quieto, no le roba el foco a quien está usando el teclado.
	public static void ConectarFocoConRaton(Node raiz)
	{
		foreach (Node nodo in raiz.FindChildren("*", "Control", true, false))
		{
			if (nodo is not (BaseButton or Godot.Range) || nodo.HasMeta("foco_raton")) continue;

			var control = (Control)nodo;
			control.SetMeta("foco_raton", true);
			control.GuiInput += evento =>
			{
				if (evento is InputEventMouseMotion && !control.HasFocus()
					&& control.FocusMode != Control.FocusModeEnum.None)
				{
					control.GrabFocus();
				}
			};
		}
	}

	// Encadena el foco de los controles en el orden dado (arriba/abajo y Tab), con vuelta
	// al principio, para que el teclado no se escape a controles de otras pantallas (CA-04).
	public static void EncadenarFoco(IEnumerable<Control> controles)
	{
		var enfocables = controles.Where(c => c.FocusMode != Control.FocusModeEnum.None).ToList();
		for (int i = 0; i < enfocables.Count; i++)
		{
			var control = enfocables[i];
			var anterior = control.GetPathTo(enfocables[(i - 1 + enfocables.Count) % enfocables.Count]);
			var siguiente = control.GetPathTo(enfocables[(i + 1) % enfocables.Count]);
			control.FocusNeighborTop = anterior;
			control.FocusNeighborBottom = siguiente;
			control.FocusPrevious = anterior;
			control.FocusNext = siguiente;
			control.FocusNeighborLeft = ".";
			control.FocusNeighborRight = ".";
		}
	}
}
