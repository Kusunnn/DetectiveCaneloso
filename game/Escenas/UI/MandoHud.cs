using Godot;
using System;
using System.Collections.Generic;

// El HUD (hud.gd) escucha teclas: diario, expediente y linterna (acciones que se pueden cambiar en Opciones) y flechas.
// Este nodo traduce los botones del control a la tecla que tenga cada acción, sin tocar el HUD.
public partial class MandoHud : Node
{
	// Las teclas generadas aquí llevan este dispositivo para no confundirlas con un teclado real
	public const int DispositivoVirtual = 77;

	private static readonly Dictionary<JoyButton, string> Acciones = new()
	{
		[JoyButton.X] = "diario",
		[JoyButton.LeftShoulder] = "expediente",
		[JoyButton.RightShoulder] = "linterna",
	};

	private static readonly Dictionary<JoyButton, Key> Flechas = new()
	{
		[JoyButton.DpadLeft] = Key.Left,
		[JoyButton.DpadRight] = Key.Right,
	};

	public override void _Input(InputEvent evento)
	{
		if (evento is not InputEventJoypadButton boton) return;
		Key tecla;
		if (Acciones.TryGetValue(boton.ButtonIndex, out string accion)) tecla = TeclaDe(accion);
		else if (!Flechas.TryGetValue(boton.ButtonIndex, out tecla)) return;
		if (tecla == Key.None) return;
		var equivalente = new InputEventKey { Keycode = tecla, PhysicalKeycode = tecla, Pressed = boton.Pressed, Device = DispositivoVirtual };
		Input.ParseInputEvent(equivalente);
	}

	// Primera tecla de teclado asignada a la acción
	private static Key TeclaDe(string accion)
	{
		if (!InputMap.HasAction(accion)) return Key.None;
		foreach (InputEvent e in InputMap.ActionGetEvents(accion))
			if (e is InputEventKey k) return k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode;
		return Key.None;
	}
}
