using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Armar el caso: oportunidad, medio y motivo. Hecdog revisa todo al final y nunca obliga a acusar.
public partial class GuionCaso1
{
	// ---------- Armar el caso ----------

	public bool CasillaEncaja(int casilla) =>
		CasoSospechoso == Culpable && Casillas[casilla].Pide.All(grupo => grupo.Any(CasoPruebas[casilla].Contains));

	public bool CasoCompleto => TonyDisponible && Enumerable.Range(0, Casillas.Length).All(CasillaEncaja);

	// true si el caso terminó en uno de los finales
	public async Task<bool> ArmarCaso(Func<bool> sigue)
	{
		var sala = _arboles[ArbolSala];
		if (!Estado.NodoCompletado(ArbolSala, "explico_caso"))
		{
			Estado.MarcarNodo(ArbolSala, "explico_caso");
			await Motor.Hablar(sala, "Hecdog", "serio", LineasSistema.Explicacion);
		}
		while (sigue())
		{
			EnEspera = true;
			Autoguardado?.Invoke("Armar el caso");
			bool presento = AbrirArmarCaso != null && await AbrirArmarCaso();
			EnEspera = false;
			Motor.Cambio?.Invoke();
			if (!presento || !sigue()) return false;
			Revisar?.Invoke();
			bool todoBien = true;
			for (int i = 0; i < Casillas.Length; i++)
			{
				bool bien = CasillaEncaja(i);
				todoBien &= bien;
				SelloDeCasilla?.Invoke(i, bien);
				await Motor.Hablar(sala, "Hecdog", bien ? "serio" : "molesto", bien ? LineasSistema.Casillas[i].Bien : LineasSistema.Casillas[i].Mal);
			}
			if (todoBien && CasoCompleto)
			{
				CerrarPanelCaso?.Invoke();
				return await AcusarATony(sigue);
			}
			Estado.Sumar("credibilidad", Estado.Contador("credibilidad") > 1 ? -1 : 0);
			AlCambiarContador("credibilidad", Estado.Contador("credibilidad"));
			await Motor.Hablar(sala, "Hecdog", "serio", LineasSistema.NoConvence);
			var opciones = new List<OpcionVista> { new("Seguir investigando") };
			bool sePuede = !string.IsNullOrEmpty(CasoSospechoso) && CasoSospechoso != Culpable;
			if (sePuede) opciones.Add(new OpcionVista("Acusar de todos modos"));
			Motor.Ventana.Sugerir("Seguir investigando", null);
			int decision = await Motor.Ventana.Elegir(Motor.HablanteDe(sala, "Hecdog"), "¿Qué hacemos?", opciones, 0);
			if (decision == 0) { CerrarPanelCaso?.Invoke(); return false; }
			int seguro = await Motor.Ventana.Elegir(Motor.HablanteDe(sala, "Caneloso"), LineasSistema.Confirmar, new List<OpcionVista> { new("Mejor no"), new($"Acusar a {NombreDe(CasoSospechoso)}") }, 0);
			if (seguro == 0) continue;
			CerrarPanelCaso?.Invoke();
			await TerminarAcusacion("a_" + CasoSospechoso, sigue);
			return true;
		}
		return false;
	}

	public Action CerrarPanelCaso;
	public Action<string> Autoguardado;
}
