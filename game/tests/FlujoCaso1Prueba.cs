using Godot;
using System;
using System.Reflection;
using System.Threading.Tasks;

// Para probar_ca03.gd: lo que no se puede pedir desde GDScript sin jugar todo el capítulo a mano.
public partial class FlujoCaso1Prueba : Node
{
	private const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;

	public bool AcusacionTerminada { get; private set; }

	// Hecdog ya tiene las confesiones de los cinco y Caneloso acusa: corre la acusación y el final como en la partida
	public async void Acusar(Node escenario)
	{
		var guion = (GuionCaso1)typeof(EscenarioCaso1).GetField("Guion", Privado).GetValue(escenario);
		foreach (string c in new[] { "juan_carlos_3", "aya_2", "miawrio_2", "eriz_1", "mimita_2" })
			guion.Estado.DesbloquearConclusion(c);
		Func<bool> sigue = () => IsInstanceValid(escenario) && escenario.IsInsideTree();
		await (Task)typeof(GuionCaso1).GetMethod("Acusar", Privado).Invoke(guion, new object[] { sigue });
		AcusacionTerminada = true;
	}

	public bool Resuelto(Node escenario)
	{
		var guion = (GuionCaso1)typeof(EscenarioCaso1).GetField("Guion", Privado).GetValue(escenario);
		return guion.Resuelto;
	}

	// Abre el detalle de un capítulo resuelto en el tablero, como al hacer clic en su carpeta
	public void AbrirDetalle(Node tablero, string capitulo)
	{
		var elegir = typeof(TableroCapitulos).GetMethod("Elegir", Privado);
		var estado = Enum.Parse(elegir.GetParameters()[1].ParameterType, "Resuelto");
		elegir.Invoke(tablero, new object[] { Capitulos.Obtener(capitulo), estado, null });
	}
}
