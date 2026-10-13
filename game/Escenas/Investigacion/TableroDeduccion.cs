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

// Pizarrón físico con cámara dedicada; el HUD superpone el diario para arrastrar pistas.
public partial class TableroDeduccion : CanvasLayer
{
	// Se conserva el contrato del caso para la futura formulación de ideas.
	public Func<string, string, string> Probar;
	public Func<IList<Carta>> ObtenerCartas;
	public Func<IList<string>> ObtenerConclusiones;
	public override void _Ready()
	{
		var pizarron = (Node3D)GD.Load<GDScript>("res://Escenas/Investigacion/pizarron_espacial.gd").New();
		pizarron.Name = "PizarronInvestigacion";
		pizarron.Position = new Vector3(3.88f, 2.5f, -1.2f);
		pizarron.Rotation = new Vector3(0, -Mathf.Pi / 2, 0);
		pizarron.Set("hud", Hud);
		Hud.Set("pizarron_espacial", pizarron);
		GetParent().CallDeferred(Node.MethodName.AddChild, pizarron);
	}
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
