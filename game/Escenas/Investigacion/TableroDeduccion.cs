using Godot;
using System;
using System.Collections.Generic;

// Tarjeta del tablero: una pista del expediente o un testimonio de la libreta.
public class Carta
{
	public string Id;
	public string Titulo;
	public Texture2D Imagen;
	public string Tipo; // "PISTA" o "TESTIMONIO"

	public Carta(string id, string titulo, string tipo, Texture2D imagen = null)
	{
		Id = id;
		Titulo = titulo;
		Tipo = tipo;
		Imagen = imagen;
	}
}

// El HUD comparte el diario y el pizarrón para arrastrar las pistas originales.
public partial class TableroDeduccion : CanvasLayer
{
	// Se conserva el contrato del caso para la futura formulación de ideas.
	public Func<string, string, string> Probar;
	public Func<IList<Carta>> ObtenerCartas;
	public Func<IList<string>> ObtenerConclusiones;
	private Node Hud => GetParent().GetNode("HUD");
	public bool Abierto => Hud.Get("tablero_abierto").AsBool();
	public void Alternar() { if (Abierto) Cerrar(); else Abrir(); }
	public void Sugerir(string nota, params string[] ids)
	{
		Hud.Call("sugerir_tablero", nota, new Godot.Collections.Array<string>(ids));
	}
	public void Abrir() => Hud.Call("abrir_tablero");
	public void Cerrar() => Hud.Call("cerrar_tablero");
}
