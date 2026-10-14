using Godot;
using System;
using System.Collections.Generic;

// Texto de la tecla o botón de una acción, según si el jugador usa teclado o control.
// Acciones del InputMap ("interactuar", "agacharse", "linterna"...) más las del HUD, que lee teclas fijas:
// "libreta" (Q) y "expediente" (TAB); y dos de movimiento: "mover" y "mirar".
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
		["libreta"] = ("Q", "X"),
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
		// Las acciones configurables (como la linterna) muestran la tecla que eligió el jugador
		if (Especiales.TryGetValue(accion, out var especial) && (UsandoMando || !InputMap.HasAction(accion)))
			return UsandoMando ? especial.Mando : especial.Teclado;
		if (!InputMap.HasAction(accion)) return accion.ToUpper();
		if (!UsandoMando) return EtiquetaTeclado(accion);

		var nombres = new List<string>();
		foreach (InputEvent evento in InputMap.ActionGetEvents(accion))
		{
			if (evento is InputEventJoypadButton boton && NombresBoton.TryGetValue((int)boton.ButtonIndex, out string nombre))
				nombres.Add(nombre);
		}
		return nombres.Count == 0 ? accion.ToUpper() : string.Join(" / ", nombres);
	}

	// Teclas del teclado de una acción, aunque se esté usando el control (Opciones > Controles)
	public static string EtiquetaTeclado(string accion)
	{
		if (!InputMap.HasAction(accion)) return accion.ToUpper();
		var nombres = new List<string>();
		foreach (InputEvent evento in InputMap.ActionGetEvents(accion))
		{
			if (evento is InputEventKey tecla)
				nombres.Add(NombreTecla(tecla.PhysicalKeycode != Key.None ? tecla.PhysicalKeycode : tecla.Keycode, tecla.PhysicalKeycode != Key.None));
		}
		return nombres.Count == 0 ? "—" : string.Join(" / ", nombres);
	}

	// Nombre corto de una tecla, según la distribución del teclado del jugador
	public static string NombreTecla(Key tecla, bool fisica = true)
	{
		// Sin ventana (pruebas automáticas) no hay distribución de teclado: se usa la tecla tal cual
		bool hayTeclado = DisplayServer.GetName() != "headless";
		Key codigo = fisica && hayTeclado ? DisplayServer.KeyboardGetKeycodeFromPhysical(tecla) : tecla;
		return codigo switch
		{
			Key.Ctrl => "Ctrl",
			Key.Shift => "Shift",
			Key.Alt => "Alt",
			Key.Space => "Espacio",
			Key.Enter => "Enter",
			Key.Backspace => "Retroceso",
			Key.Up => "↑",
			Key.Down => "↓",
			Key.Left => "←",
			Key.Right => "→",
			_ => OS.GetKeycodeString(codigo),
		};
	}
}
