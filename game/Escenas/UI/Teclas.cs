using Godot;
using System;
using System.Collections.Generic;

// Texto de la tecla o botón de una acción, según si el jugador usa teclado o control.
// Acciones del InputMap ("interactuar", "agacharse"...) más las del HUD, que lee teclas fijas:
// "libreta" (Q), "expediente" (TAB) y "linterna" (F); y dos de movimiento: "mover" y "mirar".
public static class Teclas
{
	// Botones de control (estilo Xbox) por índice de JoyButton
	private static readonly Dictionary<int, string> NombresBoton = new()
	{
		[0] = "A", [1] = "B", [2] = "X", [3] = "Y", [4] = "Back", [6] = "Start",
		[7] = "L3", [8] = "R3", [9] = "LB", [10] = "RB", [11] = "↑", [12] = "↓", [13] = "←", [14] = "→",
	};

	// Acciones del HUD (hud.gd lee estas teclas directamente; MandoHud traduce los botones)
	private static readonly Dictionary<string, (string Teclado, string Mando)> Especiales = new()
	{
		["diario"] = ("Q", "X"),
		["expediente"] = ("TAB", "LB"),
		["linterna"] = ("F", "RB"),
		["mover"] = ("W A S D", "Stick izq."),
		["mirar"] = ("Mouse", "Stick der."),
	};

	public static bool UsandoMando => Configuracion.Instancia?.UsandoMando ?? false;

	// Botón redondo (A, B, X, Y del control) o tecla rectangular
	public static bool EsBotonRedondo(string etiqueta) => UsandoMando && etiqueta.Length == 1;

	public static string Etiqueta(string accion)
	{
		// "libreta" es el nombre viejo del diario; diario, expediente y linterna ya son acciones que se pueden cambiar
		if (accion == "libreta") accion = "diario";
		if (Especiales.TryGetValue(accion, out var especial) && (UsandoMando || !InputMap.HasAction(accion)))
			return UsandoMando ? especial.Mando : especial.Teclado;
		if (!InputMap.HasAction(accion)) return accion.ToUpper();

		var nombres = new List<string>();
		foreach (InputEvent evento in InputMap.ActionGetEvents(accion))
		{
			if (UsandoMando && evento is InputEventJoypadButton boton && NombresBoton.TryGetValue((int)boton.ButtonIndex, out string nombre))
				nombres.Add(nombre);
			else if (!UsandoMando && evento is InputEventKey tecla)
			{
				Key codigo = tecla.PhysicalKeycode != Key.None
					? DisplayServer.KeyboardGetKeycodeFromPhysical(tecla.PhysicalKeycode)
					: tecla.Keycode;
				nombres.Add(codigo == Key.Ctrl ? "Ctrl" : codigo == Key.Shift ? "Shift" : OS.GetKeycodeString(codigo));
			}
		}
		return nombres.Count == 0 ? accion.ToUpper() : string.Join(" / ", nombres);
	}
}
