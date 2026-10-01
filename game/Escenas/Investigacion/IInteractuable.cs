using Godot;
using System;

// Todo lo que el detective puede usar apuntando con la retícula y pulsando [E] o clic:
// pistas, personajes, puertas... El Jugador solo conoce esta interfaz.
public interface IInteractuable
{
	// Texto que el HUD muestra al apuntar, p. ej. "Inspeccionar" o "Hablar con Eriz".
	string TextoAccion { get; }

	void Interactuar(Jugador jugador);
}
