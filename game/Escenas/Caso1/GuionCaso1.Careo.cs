using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// El careo con Tony: es su árbol de diálogo, puesto en escena al final. Se juega una sola vez; `tony_5` se exige para el Final A.
public partial class GuionCaso1
{
	// ---------- El careo y el Final A ----------

	private async Task<bool> AcusarATony(Func<bool> sigue)
	{
		await AntesDeInterrogar(Culpable);
		if (!sigue()) return true;
		var acusacion = _arboles[ArbolAcusacion];
		// Oportunidad, medio y motivo, en la voz de Caneloso
		Motor.DetenerEn = "final_a";
		await Motor.Recorrer(acusacion, "a_tony");
		Motor.DetenerEn = null;
		if (!await Careo(sigue))
		{
			if (sigue()) await DespuesDeInterrogar(Culpable);
			return false;
		}
		if (!sigue()) return true;
		await TerminarAcusacion("final_a", sigue);
		return true;
	}

	// Tony suelta sus excusas una por una. true = cayó la última. Si no, se conserva lo tumbado y se puede volver
	private async Task<bool> Careo(Func<bool> sigue)
	{
		if (Estado.TieneConclusion("tony_5")) return true; // Partidas de antes, donde ya cayeron en el interrogatorio
		var arbol = _arboles["caso1_" + Culpable];
		_enCareo = true;
		int tumbadas = ExcusasTumbadas();
		var antes = Motor.AntesDeNodo;
		Motor.AntesDeNodo = async (a, nodo) =>
		{
			await antes(a, nodo);
			if (nodo.ConclusionesAlCompletar.Any(c => c.StartsWith("tony_")))
			{
				tumbadas++;
				if (AlTumbarExcusa != null) await AlTumbarExcusa(tumbadas);
			}
		};
		string salida = await Motor.Recorrer(arbol);
		Motor.AntesDeNodo = antes;
		_enCareo = false;
		if (salida == "paciencia" && sigue()) await CortarInterrogatorio(arbol);
		AlTerminar?.Invoke("Careo");
		return Estado.TieneConclusion("tony_5");
	}

	private int ExcusasTumbadas() => Enumerable.Range(1, 5).Count(i => Estado.TieneConclusion("tony_" + i));
}
