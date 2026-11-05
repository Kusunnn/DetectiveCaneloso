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

	// Lo que le quede por romper a Tony, cuando ya se le presentó el caso. true = no le queda ninguno.
	private async Task<bool> Careo(Func<bool> sigue)
	{
		if (TonySinCandados) return true;
		await Motor.Recorrer(_arboles["caso1_" + Culpable]);
		Motor.Ventana.MostrarCandados(0, 0);
		AlTerminar?.Invoke("Careo");
		return TonySinCandados;
	}

	public bool TonySinCandados => ExcusasTumbadas() == 5;

	private int ExcusasTumbadas() => Enumerable.Range(1, 5).Count(i => Estado.TieneConclusion("tony_" + i));
}
