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
		Dirigido();
		for (ulong semilla = 1; semilla <= 300; semilla++) AlAzar(semilla);
		Declaraciones();
		Ayudas();
		Textos();
		GD.Print($"GuiaCaso1Prueba: {_fallos} fallos");
		return _fallos == 0;
	}

	private sealed class Partida
	{
		public readonly HashSet<string> Pistas = new(), Conclusiones = new();
		public bool Llavero;
		public readonly GuiaCaso1 Guia;
		public Partida() { Guia = new GuiaCaso1(Pistas.Contains, Conclusiones.Contains, () => Llavero); }
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

	// Sin candados pendientes
	private void RevisarFin(Partida p, string quien)
	{
		foreach (var c in p.Guia.Candados.Where(c => !p.Guia.Roto(c))) Revisar(false, $"{quien}: callejón en {c.Id}");
	}

	// Haciendo solo lo que dice la guía se termina el caso
	private void Dirigido()
	{
		var p = new Partida();
		for (int vuelta = 0; vuelta < 80; vuelta++)
		{
			var paso = p.Guia.Siguiente();
			if (paso.Clase == GuiaCaso1.Clase.TodoRoto) { RevisarFin(p, "dirigido"); return; }
			Aplicar(p, paso);
		}
		Revisar(false, "dirigido: la guía da vueltas sin terminar");
	}

	// En cualquier estado alcanzable, la guía propone algo que se puede hacer y que hace avanzar
	private void AlAzar(ulong semilla)
	{
		var azar = new RandomNumberGenerator { Seed = semilla };
		var p = new Partida();
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
			if (paso.Clase == GuiaCaso1.Clase.TodoRoto) { RevisarFin(p, $"semilla {semilla}"); return; }
			Aplicar(p, paso);
			Revisar(p.Avance > antes, $"semilla {semilla}: el paso no hace avanzar ({paso.Clase} {paso.Pista} {paso.Candado?.Id})");
		}
		Revisar(false, $"semilla {semilla}: no terminó");
	}

	// Cada candado existe en el árbol de su sospechoso, con esa prueba
	private void TablaContraGuion()
	{
		var guia = new Partida().Guia;
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

	// Las frases son el texto del guion, sin cambiar palabras, y cada candado tiene su nodo y su frase
	private void Declaraciones()
	{
		var guia = new Partida().Guia;
		foreach (string quien in GuionCaso1.Sospechosos)
		{
			var arbol = (Godot.Collections.Dictionary)Json.ParseString(FileAccess.GetFileAsString($"res://Datos/Dialogos/caso1_{quien}.json"));
			var nodos = arbol["nodos"].AsGodotDictionary();
			string TextoDe(string nodo)
			{
				var lineas = nodos[nodo].AsGodotDictionary()["lineas"].AsGodotArray().Select(l => l.AsGodotDictionary()).ToList();
				return string.Join(" ", lineas.Where(l => l["habla"].AsString() != "Caneloso").Select(l => l["texto"].AsString()));
			}
			var frases = guia.FrasesDe(quien);
			if (quien == GuionCaso1.Culpable)
			{
				for (int i = 0; i < 5; i++) Revisar(TextoDe("excusa_" + (i + 1)) == frases[i], $"Tony: la excusa {i + 1} no coincide con la frase");
			}
			else
			{
				var propias = guia.Candados.Where(c => c.Quien == quien && !c.Oculta).Select(c => c.Frase).Max();
				string unidas = string.Join(" ", frases.Where((_, i) => !guia.Candados.Any(c => c.Quien == quien && c.Oculta && c.Frase == i)));
				Revisar(TextoDe("mentira") == unidas, $"{quien}: las frases no coinciden con el guion: {unidas}");
				Revisar(propias < frases.Count, $"{quien}: índice de frase");
			}
			foreach (var c in guia.Candados.Where(c => c.Quien == quien))
			{
				Revisar(nodos.ContainsKey(c.Nodo), $"{c.Id}: no existe el nodo {c.Nodo}");
				Revisar(c.Frase < frases.Count, $"{c.Id}: no existe la frase {c.Frase}");
				if (c.EsDeTony) Revisar(guia.FaltaParaTony(c) != "", $"{c.Id}: sin texto de lo que falta");
			}
		}
		Revisar(FileAccess.GetFileAsString("res://Datos/Dialogos/caso1_juan_carlos.json").Contains(guia.FrasesDe("juan_carlos")[2]), "la frase oculta de Juan Carlos está en el guion");
	}

	// Cada paso posible tiene sus tres niveles, sin repetir, y Experto se queda en el primero
	private void Ayudas()
	{
		var p = new Partida();
		foreach (var c in p.Guia.Candados)
		{
			Revisar(p.Guia.TieneNivel3(c.Id), $"{c.Id}: falta su ayuda de nivel 3");
			foreach (string pista in c.Pistas) Revisar(p.Guia.TieneNivel3(pista), $"{pista}: falta su ayuda de nivel 3");
		}
		var cuaderno = new SistemaAyudas();
		var paso = p.Guia.Siguiente();
		var textos = new List<string>();
		for (int i = 0; i < 4; i++) textos.Add(p.Guia.PedirAyuda(paso, cuaderno, 3).Texto);
		Revisar(textos.Take(3).Distinct().Count() == 3 && cuaderno.Usos == 3, "tres niveles distintos: " + string.Join(" | ", textos));
		Revisar(textos[0] == "¿Ya revisaste bien la fogata, Nya?", "nivel 1 con la zona: " + textos[0]);
		Revisar(!p.Guia.PedirAyuda(paso, cuaderno, 3).Nueva && cuaderno.Usos == 3, "el cuarto pedido no cuenta");
		var experto = new SistemaAyudas();
		p.Guia.PedirAyuda(paso, experto, 1);
		Revisar(!p.Guia.PedirAyuda(paso, experto, 1).Nueva && experto.Usos == 1, "Experto: solo el nivel 1");
		p.Pistas.Add("lena_intacta");
		var candado = p.Guia.Siguiente();
		Revisar(p.Guia.Ayuda(candado, 2).Contains("Fíjate que fui por leña"), "nivel 2 de un candado cita la frase: " + p.Guia.Ayuda(candado, 2));
		// Sin llavero, la cabaña lleva a las llaves
		var q = new Partida();
		foreach (string id in new[] { "libro_guardia_quemado", "lena_intacta", "interruptor_generador", "candado_caseta", "baul_disfraces", "taza_delantal" }) q.Pistas.Add(id);
		var exploracion = q.Guia.SiguienteExploracion(GuiaCaso1.Escenario.Campamento);
		Revisar(exploracion.Clase == GuiaCaso1.Clase.FaltaLlavero && q.Guia.Ayuda(exploracion, 3).Contains("delantal"), "sin llavero: " + q.Guia.Ayuda(exploracion, 3));
		// Todos los pasos de un recorrido completo tienen texto en sus tres niveles
		var r = new Partida();
		for (int vuelta = 0; vuelta < 80; vuelta++)
		{
			var s = r.Guia.Siguiente();
			for (int nivel = 1; nivel <= 3; nivel++) Revisar(!string.IsNullOrEmpty(r.Guia.Ayuda(s, nivel)), $"{GuiaCaso1.ClaveAyuda(s)} nivel {nivel} vacío");
			if (s.Clase == GuiaCaso1.Clase.TodoRoto) break;
			Aplicar(r, s);
		}
	}

	private void Textos()
	{
		const GuiaCaso1.Escenario Sala = GuiaCaso1.Escenario.Sala;
		var p = new Partida();
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
		var r = new Partida();
		o = r.Guia.ObjetivoExploracion(GuiaCaso1.Escenario.Campamento);
		Revisar(o == "La fogata del centro: ahí estaban todos cuando se fue la luz. (0/13 pistas)", "exploración: " + o);
		Revisar(r.Guia.PistasTotales("lago") == 9 && r.Guia.PistasTotales("campamento") == 13, "13 y 9 pistas");
		Revisar(r.Guia.Secundarios()[2] == "Candados rotos 0/15", "secundarios");
	}
}
