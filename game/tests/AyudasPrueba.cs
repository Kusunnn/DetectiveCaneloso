using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

// Pruebas de reglas y selección contextual, sin tocar partidas ni preferencias del usuario.
public partial class AyudasPrueba : Node
{
	private int _comprobaciones;
	private void Verificar(bool condicion, string mensaje)
	{
		if (!condicion) throw new Exception(mensaje);
		_comprobaciones++;
	}
	private static (string, string) Orientacion(object caso) => ((string, string))caso.GetType()
		.GetMethod("OrientacionInvestigacion", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(caso, null);
	private static HashSet<string> Pistas(object caso) => (HashSet<string>)caso.GetType()
		.GetField("_pistas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(caso);

	public bool Ejecutar()
	{
		try { Probar(); return true; }
		catch (Exception error) { GD.PushError(error.ToString()); return false; }
	}

	private void Probar()
	{
		var ayudas = new SistemaAyudas();
        Verificar(ayudas.Agregar("zona", "Revisa la zona"), "Se puede pedir sin espera");
        Verificar(!ayudas.Agregar("zona", "Revisa la zona"), "No repetir entradas ni consumo");
        Verificar(ayudas.Usos == 1 && ayudas.Entradas.Count == 1, "Solo cuenta una ayuda nueva");
        Verificar(!ayudas.Agregar(null, null), "No registrar ayudas vacías");
        var cargadas = new SistemaAyudas();
        cargadas.Restaurar(GD.BytesToVar(GD.VarToBytes(ayudas.Capturar())).AsGodotDictionary());
        Verificar(cargadas.Entradas[0]["texto"].AsString() == "Revisa la zona", "El texto sobrevive al guardado");
        Verificar(cargadas.Usos == 1, "El consumo sobrevive al guardado");
        Verificar(cargadas.Agregar("otra", "Otra orientación"), "Se acumula una segunda entrada");
        Verificar(cargadas.Entradas[0]["texto"].AsString() == "Revisa la zona", "Se conserva la anterior");
        var copia = cargadas.Entradas;
        copia[0]["texto"] = "Modificado";
        Verificar(cargadas.Entradas[0]["texto"].AsString() == "Revisa la zona", "Leer no permite alterar el historial");
        cargadas.Restaurar(new Godot.Collections.Dictionary());
        Verificar(cargadas.Entradas.Count == 0 && cargadas.Usos == 0, "Partidas sin ayudas");
        cargadas.Restaurar(new Godot.Collections.Dictionary {
            { "usos", 2 }, { "historial", new Godot.Collections.Array { "sala", "sala" } }
        });
        Verificar(cargadas.Usos == 2 && cargadas.Entradas.Count == 1, "Migrar partidas antiguas sin duplicar");

		var caso = new Caso1 { Jugador = new Jugador() };
		Verificar(Orientacion(caso).Item1 == "fogata", "Inicio: zona accesible");
		Pistas(caso).UnionWith(new[] { "lena_intacta", "libro_guardia_quemado", "taza_delantal" });
		Verificar(Orientacion(caso).Item1 == "cocina", "No orientar hacia cabaña cerrada sin llavero");
		var objetos = (HashSet<string>)typeof(Jugador).GetField("_objetos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(caso.Jugador);
		objetos.Add("llavero_porky");
		Verificar(Orientacion(caso).Item1 == "porky", "Cabaña accesible con llavero");
		Pistas(caso).UnionWith(new[] { "nota_porky", "foto_grupo", "recorte_periodico", "oferta_compra", "baul_disfraces", "registro_enfermeria", "botas_tony", "interruptor_generador", "candado_caseta" });
		Verificar(Orientacion(caso).Item1 == "coche", "El registro de llamadas está activo");
		Pistas(caso).Add("registro_llamadas");
		Verificar(Orientacion(caso).Item1 == "lago", "Se puede continuar en el lago");
		caso.Jugador.Free();
		caso.Free();
		var tutorial = new Caso0();
		Verificar(Orientacion(tutorial).Item1 == "sala", "Tutorial: orientación de zona");
		Pistas(tutorial).UnionWith(new[] { "grabadora", "reloj" });
		Verificar(Orientacion(tutorial).Item1 == "tablero_sala", "Orientar al tablero sin revelar la combinación");
		tutorial.Free();
        var lago = new Caso1Lago { Jugador = new Jugador() };
        var pistasLago = (HashSet<string>)typeof(EscenarioCaso1).GetField("Pistas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lago);
        Verificar(Orientacion(lago).Item1 == "lago_sendero", "Lago: empezar por huellas");
        pistasLago.UnionWith(new[] { "huellas_pezuna", "huellas_detenidas" });
        Verificar(Orientacion(lago).Item1 == "lago_bosque", "Lago: seguir al bosque");
        pistasLago.UnionWith(new[] { "altavoz", "cornamenta_zancos" });
        Verificar(Orientacion(lago).Item1 == "lago_orilla", "Lago: seguir a la mochila");
        pistasLago.Add("cuaderno_eriz");
        Verificar(Orientacion(lago).Item1 == "lago_embarcadero", "Lago: seguir al muelle");
        pistasLago.UnionWith(new[] { "silbato_ts", "remo_sangre" });
        Verificar(Orientacion(lago).Item1 == "lago_embarcadero", "La linterna pendiente mantiene la orientación al muelle");
        pistasLago.Add("linterna_porky");
        Verificar(Orientacion(lago).Item1 == "lago_llavero", "No orientar al cobertizo cerrado sin llave");
        var objetosLago = (HashSet<string>)typeof(Jugador).GetField("_objetos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lago.Jugador);
        objetosLago.Add("llavero_porky");
        Verificar(Orientacion(lago).Item1 == "lago_cobertizo", "Con llavero orientar al cobertizo");
        pistasLago.Add("hueco_remo");
        Verificar(Orientacion(lago).Item1 == "lago_estacion", "Lago completo: estación");
        lago.Cuaderno.Restaurar(ayudas.Capturar());
        Verificar(lago.Cuaderno.Entradas[0]["texto"].AsString() == "Revisa la zona", "El cuaderno viaja al lago");
        lago.Jugador.Free();
        lago.Free();
		GD.Print($"HU-10: {_comprobaciones} comprobaciones correctas.");
	}
}
