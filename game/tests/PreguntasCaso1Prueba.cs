using Godot;
using System.Collections.Generic;
using System.Linq;

// HU-30 y HU-33 sin escenas: las preguntas de cada implicado (aparecen con lo que piden, una sola vez, con su
// declaración en el diario) y la respuesta escrita de cada prueba equivocada del guion.
public partial class PreguntasCaso1Prueba : RefCounted
{
	private int _fallos;
	private void Revisar(bool bien, string que) { if (!bien) { _fallos++; GD.PrintErr("FALLA: " + que); } }

	public bool Ejecutar()
	{
		foreach (string quien in GuionCaso1.Sospechosos)
		{
			var arbol = ArbolDialogo.Cargar($"res://Datos/Dialogos/caso1_{quien}.json");
			Revisar(arbol != null && arbol.Validar().Count == 0, $"{quien}: el árbol se valida");
			if (arbol == null) continue;
			Preguntas(quien, arbol);
			PruebasEquivocadas(quien, arbol);
		}
		GD.Print($"PreguntasCaso1Prueba: {_fallos} fallos");
		return _fallos == 0;
	}

	private void Preguntas(string quien, ArbolDialogo arbol)
	{
		var nodo = arbol.Nodo(GuionCaso1.NodoPreguntas);
		Revisar(nodo != null && nodo.Opciones.Count >= 2, $"{quien}: tiene preguntas");
		if (nodo == null) return;
		foreach (var o in nodo.Opciones)
		{
			Revisar(o.Tipo == "PREGUNTAR" && o.UnaVez && o.OcultarSiNoCumple, $"{quien}/{o.Id}: PREGUNTAR, una_vez y oculta sin requisito");
			var respuesta = arbol.Nodo(o.Destino);
			Revisar(respuesta != null && respuesta.Lineas.Any(l => l.Habla == arbol.Personaje), $"{quien}/{o.Id}: {arbol.Personaje} contesta");
			Revisar(respuesta != null && respuesta.NotasAlCompletar.Count == 1 && respuesta.NotasAlCompletar[0].StartsWith("Declaración de "),
				$"{quien}/{o.Id}: la respuesta queda como declaración");
		}
		// CA-01: al menos una se abre con una pista o una conclusión
		Revisar(nodo.Opciones.Any(o => o.Requiere.Pistas.Count + o.Requiere.Conclusiones.Count > 0), $"{quien}: el conjunto crece con pistas o conclusiones");

		// CA-02 y CA-03 con el motor: sin nada, solo las que no piden nada; con todo, todas; ya hecha, ya no se ve
		var motor = new MotorDialogos(null) { TienePista = _ => false, TieneConclusion = _ => false };
		var sinNada = motor.OpcionesVisibles(arbol, nodo);
		Revisar(sinNada.All(v => !v.Vista.Bloqueada), $"{quien}: una pregunta sin su pista no aparece ni como «???»");
		Revisar(sinNada.All(v => v.Opcion.Requiere.Pistas.Count + v.Opcion.Requiere.Conclusiones.Count + v.Opcion.Requiere.NodosCompletados.Count == 0),
			$"{quien}: sin pistas solo se ven las preguntas que no piden nada");
		var primera = sinNada.FirstOrDefault().Opcion;
		Revisar(primera != null, $"{quien}: hay una pregunta desde el inicio");
		if (primera == null) return;
		motor.Estado.MarcarOpcion(arbol.Id, nodo.Id, primera.Id);
		Revisar(motor.OpcionesVisibles(arbol, nodo).All(v => v.Opcion != primera), $"{quien}/{primera.Id}: ya hecha, no se vuelve a ver");
	}

	private void PruebasEquivocadas(string quien, ArbolDialogo arbol)
	{
		foreach (var (id, nodo) in arbol.Nodos.Where(n => n.Key.StartsWith("e_")))
		{
			if (nodo.Siguiente != "error") continue;
			Revisar(nodo.Lineas.Any(l => l.Habla == arbol.Personaje), $"{quien}/{id}: {arbol.Personaje} desestima la prueba con su respuesta");
		}
		if (quien == GuionCaso1.Culpable) return;
		var desestima = arbol.Nodo(GuionCaso1.NodoDesestima);
		Revisar(desestima != null && desestima.Siguiente == "error" && desestima.Variantes.All(t => t.All(l => l.Habla == arbol.Personaje)),
			$"{quien}: desestima con sus palabras lo que el guion no contempla");
	}
}
