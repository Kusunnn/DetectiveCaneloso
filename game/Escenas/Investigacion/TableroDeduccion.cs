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
		Hud.Set("gestor_tablero", this);
		foreach (string herramienta in new[] { "notas", "marcador" })
			if (HerramientaTablero.Desbloqueada(herramienta)) Hud.Call("desbloquear_herramienta_tablero", herramienta);
		var pizarron = (Node3D)GD.Load<GDScript>("res://Escenas/Investigacion/pizarron_espacial.gd").New();
		pizarron.Name = "PizarronInvestigacion";
		pizarron.AddToGroup("auditoria_fuera_ok"); // No es parte del escenario: la auditoría no lo revisa
		// Lejos de cualquier escenario y oculto: solo aparece mientras el tablero está abierto
		pizarron.Position = PosicionPizarron;
		pizarron.Set("hud", Hud);
		Hud.Set("pizarron_espacial", pizarron);
		GetParent().CallDeferred(Node.MethodName.AddChild, pizarron);
	}
	private static readonly Vector3 PosicionPizarron = new Vector3(0, 400f, 0);
	private Node Hud => GetParent().GetNode("HUD");
	public bool Abierto => Hud.Get("tablero_abierto").AsBool();
	public void Alternar() { if (Abierto) Cerrar(); else Abrir(); }
	public void Sugerir(string nota, params string[] ids)
	{
		Hud.Call("sugerir_tablero", nota, new Godot.Collections.Array<string>(ids));
	}
	// Una ficha de persona puede usar solo los testimonios ya obtenidos en el caso.
	private IList<string> Identidades(string id)
	{
		if (!id.StartsWith("persona:")) return new List<string> { id };
		string nombre = id.Substring("persona:".Length);
		var lista = new List<string>();
		foreach (var carta in ObtenerCartas?.Invoke() ?? new List<Carta>())
			if (carta.Tipo == "TESTIMONIO" && carta.Titulo.StartsWith(nombre + ":", StringComparison.OrdinalIgnoreCase))
				lista.Add(carta.Id);
		return lista;
	}
	public string ProbarRelacion(string a, string b)
	{
		foreach (string x in Identidades(a))
			foreach (string y in Identidades(b))
			{
				string conclusion = Probar?.Invoke(x, y);
				if (!string.IsNullOrEmpty(conclusion)) return conclusion;
			}
		return "";
	}
	public void Abrir() => Hud.Call("abrir_tablero");
	public void Cerrar() => Hud.Call("cerrar_tablero");
}
