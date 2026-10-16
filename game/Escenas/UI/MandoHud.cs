using Godot;
using System;
using System.Collections.Generic;

// El HUD (hud.gd) escucha teclas fijas: Q libreta, TAB expediente y flechas, y la acción "linterna".
// Este nodo traduce los botones del control a esas teclas y a esa acción.
public partial class MandoHud : Node
{
	// Las teclas generadas aquí llevan este dispositivo para no confundirlas con un teclado real
	public const int DispositivoVirtual = 77;

	private static readonly Dictionary<JoyButton, Key> Equivalencias = new()
	{
		[JoyButton.X] = Key.Q,
		[JoyButton.LeftShoulder] = Key.Tab,
		[JoyButton.DpadLeft] = Key.Left,
		[JoyButton.DpadRight] = Key.Right,
	};

	public override void _Input(InputEvent evento)
	{
		if (evento is not InputEventJoypadButton boton) return;
		// La linterna es una acción configurable: RB la activa sin depender de la tecla elegida
		if (boton.ButtonIndex == JoyButton.RightShoulder)
		{
			Input.ParseInputEvent(new InputEventAction { Action = "linterna", Pressed = boton.Pressed });
			return;
		}
		if (!Equivalencias.TryGetValue(boton.ButtonIndex, out Key tecla)) return;
		var equivalente = new InputEventKey { Keycode = tecla, PhysicalKeycode = tecla, Pressed = boton.Pressed, Device = DispositivoVirtual };
		Input.ParseInputEvent(equivalente);
	}
}
