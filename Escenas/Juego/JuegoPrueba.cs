using Godot;
using System;

// Escena de juego PROVISIONAL, solo para probar el menú de pausa.
// Sustituir por el juego real (manteniendo una instancia de menu_pausa.tscn).
public partial class JuegoPrueba : Control
{
	public override void _Ready()
	{
		GetNode<Button>("%BotonGuardar").Pressed += GestorPartida.Instancia.GuardarPartida;
		GetNode<Button>("%BotonSimularProgreso").Pressed += () => GestorPartida.Instancia.HayCambiosSinGuardar = true;
	}
}
