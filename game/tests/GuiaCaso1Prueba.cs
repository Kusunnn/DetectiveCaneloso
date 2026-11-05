using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Lo que promete GuiaCaso1, sin escenas (corre en segundos): siempre hay un siguiente paso que sí se puede cumplir,
// jugando en cualquier orden, y la tabla de candados coincide con los árboles del guion.
public partial class GuiaCaso1Prueba : RefCounted
{
	private int _fallos;
	private void Revisar(bool bien, string que) { if (!bien) { _fallos++; GD.PrintErr("FALLA: " + que); } }

	public bool Ejecutar()
	{
		TablaContraGuion();
		foreach (bool careo in new[] { true, false })
		{
			Dirigido(careo);
			for (ulong semilla = 1; semilla <= 300; semilla++) AlAzar(semilla, careo);
		}
		Textos();
		GD.Print($"GuiaCaso1Prueba: {_fallos} fallos");
		return _fallos == 0;
	}

	private sealed class Partida
	{
		public readonly HashSet<string> Pistas = new(), Conclusiones = new();
		public bool Llavero;
		public readonly GuiaCaso1 Guia;
		public Partida(bool careo) { Guia = new GuiaCaso1(Pistas.Contains, Conclusiones.Contains, () => Llavero) { TonyEnCareo = careo }; }
		public void Romper(string id) { foreach (string d in Guia.Buscar(id).Da) Conclusiones.Add(d); }
		public int Avance => Pistas.Count + Conclusiones.Count + (Llavero ? 1 : 0);
	}

	private static void Aplicar(Partida p, GuiaCaso1.Paso paso)
	{
		switch (paso.Clase)
		{
			case GuiaCaso1.Clase.Listo: p.Romper(paso.Candado.Id); break;
			case GuiaCaso1.Clase.FaltaLlavero: p.Llavero = true; break;
			case GuiaCaso1.Clase.FaltaPista: p.Pistas.Add(paso.Pista); break;
		}
	}

	// Sin candados pendientes, o solo los de Tony cuando caen en el careo con todas sus pruebas
	private void RevisarFin(Partida p, bool careo, string quien)
	{
		foreach (var c in p.Guia.Candados.Where(c => !p.Guia.Roto(c)))
			Revisar(careo && c.EsDeTony && c.Pistas.All(p.Pistas.Contains) && c.Conclusiones.All(p.Conclusiones.Contains), $"{quien}: callejón en {c.Id}");
	}

	// Haciendo solo lo que dice la guía se termina el caso
	private void Dirigido(bool careo)
	{
		var p = new Partida(careo);
		for (int vuelta = 0; vuelta < 80; vuelta++)
		{
			var paso = p.Guia.Siguiente();
			if (paso.Clase == GuiaCaso1.Clase.TodoRoto) { RevisarFin(p, careo, "dirigido"); return; }
			Aplicar(p, paso);
		}
		Revisar(false, "dirigido: la guía da vueltas sin terminar");
	}

	// En cualquier estado alcanzable, la guía propone algo que se puede hacer y que hace avanzar
	private void AlAzar(ulong semilla, bool careo)
	{
		var azar = new RandomNumberGenerator { Seed = semilla };
		var p = new Partida(careo);
		var todas = new[] { "lena_intacta", "cornamenta_zancos", "huellas_detenidas", "altavoz", "oferta_compra", "registro_llamadas", "cuaderno_eriz", "registro_enfermeria", "botas_tony", "hueco_remo", "remo_sangre", "candado_caseta", "silbato_ts", "foto_grupo", "libro_guardia_quemado", "nota_porky" };
		var cerradas = new HashSet<string> { "hueco_remo", "foto_grupo", "nota_porky", "oferta_compra" };
		for (int vuelta = 0; vuelta < 200; vuelta++)
		{
			// El jugador hace algo por su cuenta: toma una pista alcanzable o rompe un candado que brilla
			if (azar.Randf() < 0.1f) p.Llavero = true;
			string pista = todas[azar.RandiRange(0, todas.Length - 1)];
			if (p.Llavero || !cerradas.Contains(pista)) p.Pistas.Add(pista);
			var listos = p.Guia.Candados.Where(p.Guia.Listo).ToList();
			if (listos.Count > 0 && azar.Randf() < 0.5f) p.Romper(listos[azar.RandiRange(0, listos.Count - 1)].Id);

			int antes = p.Avance;
			var paso = p.Guia.Siguiente();
			if (paso.Clase == GuiaCaso1.Clase.TodoRoto) { RevisarFin(p, careo, $"semilla {semilla}"); return; }
			Aplicar(p, paso);
			Revisar(p.Avance > antes, $"semilla {semilla}: el paso no hace avanzar ({paso.Clase} {paso.Pista} {paso.Candado?.Id})");
		}
		Revisar(false, $"semilla {semilla}: no terminó");
	}

	// Cada candado existe en el árbol de su sospechoso, con esa prueba
	private void TablaContraGuion()
	{
		var guia = new Partida(false).Guia;
		Revisar(guia.CandadosTotales == 15, "son 15 candados");
		var enGuion = new Dictionary<string, string> { ["cornamenta_zancos"] = "disfraz_ciervo", ["hueco_remo"] = "soporte_remos", ["silbato_ts"] = "silbato_gs" };
		foreach (var c in guia.Candados)
		{
			string arbol = FileAccess.GetFileAsString($"res://Datos/Dialogos/caso1_{c.Quien}.json");
			foreach (string d in c.Da) Revisar(arbol.Contains($"\"{d}\""), $"{c.Id}: {d} no está en el árbol");
			foreach (string pista in c.Pistas)
				Revisar(arbol.Contains($"\"{(enGuion.TryGetValue(pista, out string otro) ? otro : pista)}\""), $"{c.Id}: {pista} no está en el árbol");
		}
	}

	private void Textos()
	{
		const GuiaCaso1.Escenario Sala = GuiaCaso1.Escenario.Sala;
		var p = new Partida(false);
		string o = p.Guia.ObjetivoInterrogatorios(Sala);
		Revisar(o == "Me falta algo del campamento: la fogata.", "sin nada: " + o);
		p.Pistas.Add("lena_intacta");
		o = p.Guia.ObjetivoInterrogatorios(Sala);
		Revisar(o == "Tengo algo para Juan Carlos.", "listo en sala: " + o);
		Revisar(p.Guia.ObjetivoInterrogatorios(GuiaCaso1.Escenario.Lago).EndsWith("Vamos a la estación."), "listo fuera de la sala");
		Revisar(p.Guia.Brilla("juan_carlos") && !p.Guia.Brilla("aya"), "brilla solo quien tiene prueba");
		p.Romper("JC-1");
		o = p.Guia.ObjetivoInterrogatorios(Sala);
		Revisar(o == "Me falta algo del lago: el bosque.", "del lago: " + o);
		foreach (string id in new[] { "JC-2", "JC-3", "AY-1" }) p.Romper(id);
		o = p.Guia.ObjetivoInterrogatorios(Sala);
		Revisar(o == "Tengo algo para Aya.", "Aya con la confesión: " + o);
		p.Romper("AY-2");
		o = p.Guia.ObjetivoInterrogatorios(Sala);
		Revisar(o.Contains("Las llaves de Porky"), "sin llavero lo dice: " + o);
		// Antes de la sala se conserva el recorrido por zonas
		var r = new Partida(false);
		o = r.Guia.ObjetivoExploracion(GuiaCaso1.Escenario.Campamento);
		Revisar(o == "La fogata del centro: ahí estaban todos cuando se fue la luz. (0/13 pistas)", "exploración: " + o);
		Revisar(r.Guia.PistasTotales("lago") == 9 && r.Guia.PistasTotales("campamento") == 13, "13 y 9 pistas");
		Revisar(r.Guia.Secundarios()[2] == "Candados rotos 0/15", "secundarios");
	}
}
