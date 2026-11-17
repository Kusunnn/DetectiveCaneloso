using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// HU-30: preguntas específicas en el interrogatorio. El árbol de cada sospechoso (Datos/Dialogos/caso1_*.json)
// tiene un nodo «preguntas» con opciones PREGUNTAR:
//   CA-01  el conjunto crece con las pistas y conclusiones nuevas ("requiere"),
//   CA-02  cada una se hace una sola vez ("una_vez") y su respuesta queda en el diario como declaración
//          ("al_completar": { "notas": ["Declaración de ...: ..."] }),
//   CA-03  sin lo que pide, no aparece ("si_no_cumple": "ocultar").
// Se pregunta desde su declaración («Preguntarle algo») y, cuando ya no tiene candados, al terminar el interrogatorio.
public partial class GuionCaso1
{
	public const string NodoPreguntas = "preguntas";

	// Las que se le pueden hacer ahora: tiene lo que piden y todavía no se hicieron
	public List<OpcionDialogo> PreguntasDisponibles(string quien)
	{
		if (!_arboles.TryGetValue("caso1_" + quien, out var arbol) || arbol?.Nodo(NodoPreguntas) is not { } nodo)
			return new List<OpcionDialogo>();
		return Motor.OpcionesVisibles(arbol, nodo)
			.Where(v => !v.Vista.Bloqueada && !v.Opcion.Fin)
			.Select(v => v.Opcion)
			.ToList();
	}

	public bool TienePreguntas(string quien) => PreguntasDisponibles(quien).Count > 0;

	// El botón de la declaración: «Preguntarle algo (2)». null si no hay nada nuevo que preguntar.
	private string TextoPreguntar(string quien)
	{
		int cuantas = PreguntasDisponibles(quien).Count;
		return cuantas == 0 ? null : Guia.Interrogatorio("preguntar").Replace("{n}", cuantas.ToString());
	}

	// La lista de preguntas: se elige una, la contesta y se vuelve a la lista, hasta «Volver» o hasta que no quede ninguna
	private async Task HacerPreguntas(ArbolDialogo arbol, Func<bool> sigue = null)
	{
		string quien = IdDe(arbol);
		var nodo = arbol.Nodo(NodoPreguntas);
		string pregunta = Guia.Interrogatorio("que_pregunto").Replace("{quien}", NombreDe(quien));
		while (sigue == null || sigue())
		{
			var preguntas = PreguntasDisponibles(quien);
			if (preguntas.Count == 0) return;
			var vistas = preguntas.Select(p => new OpcionVista(p.Texto, p.Tipo)).ToList();
			vistas.Add(new OpcionVista(Guia.Interrogatorio("volver")));
			var personaje = Motor.HablanteDe(arbol, arbol.Personaje);
			personaje.Tiembla = false;
			int elegida = await Motor.Ventana.Elegir(personaje, pregunta, vistas, vistas.Count - 1);
			if (elegida < 0 || elegida >= preguntas.Count) return;

			var opcion = preguntas[elegida];
			// Hecha: ya no vuelve a aparecer (también si se sale a media respuesta)
			if (Estado.MarcarOpcion(arbol.Id, nodo.Id, opcion.Id)) Motor.Cambio?.Invoke();
			await Motor.Recorrer(arbol, opcion.Destino);
		}
	}
}
