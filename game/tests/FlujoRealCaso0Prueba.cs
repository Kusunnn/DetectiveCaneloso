using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

// Lleva el Caso 0 directo a la resolución (con la pluma encontrada) y la juega como en una partida:
// Resolver hace las tres preguntas, las frases de Porky y del comisario, la cinemática de la cinta y el cierre.
// Lo usa Herramientas/flujo_real_cinta.gd para probar y grabar la cinemática desde el flujo real.
public partial class FlujoRealCaso0Prueba : Node
{
	public bool CinematicaEnCurso => CinematicaCinta.EnCurso;
	public bool Terminado { get; private set; }

	public async void Resolver(Caso0 caso)
	{
		const BindingFlags privado = BindingFlags.NonPublic | BindingFlags.Instance;
		var pistas = (HashSet<string>)typeof(Caso0).GetField("_pistas", privado).GetValue(caso);
		pistas.Add("pluma");
		typeof(Caso0).GetMethod("EmpezarResolucion", privado).Invoke(caso, null);
		await (Task)typeof(Caso0).GetMethod("Resolver", privado).Invoke(caso, null);
		Terminado = true;
	}
}
