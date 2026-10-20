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
		int mostradas = 0;
		Action<string> mostrar = _ => mostradas++;
		Verificar(!ayudas.Solicitar(true, 120, "zona", "Revisa la zona", mostrar), "No ofrecer antes del umbral");
		ayudas.Avanzar(119, true, true);
		Verificar(!ayudas.Disponible(true, 120), "Umbral de 120 s");
		ayudas.Avanzar(300, false, true);
		ayudas.Avanzar(300, true, false);
		Verificar(ayudas.SinPista == 119, "Pausa, lectura y desactivación no suman tiempo");
		ayudas.Avanzar(1, true, true);
		Verificar(mostradas == 0 && ayudas.Usos == 0, "El tiempo no ofrece ni registra ayudas automáticamente");
		Verificar(!ayudas.Solicitar(false, 120, "zona", "Texto", mostrar), "Desactivar bloquea ayuda disponible");
		Verificar(!ayudas.Solicitar(true, 120, null, null, mostrar), "Sin orientación no hay consumo");
		Verificar(ayudas.Solicitar(true, 120, "zona", "Texto", mostrar), "Solicitud válida");
		Verificar(mostradas == 1 && ayudas.Usos == 1, "Contar solo orientaciones mostradas");
		var cargadas = new SistemaAyudas();
		cargadas.Restaurar(GD.BytesToVar(GD.VarToBytes(ayudas.Capturar())).AsGodotDictionary());
		Verificar(cargadas.Usos == 1 && cargadas.SinPista == 120, "Persistencia tras serializar y cargar");
		Verificar(cargadas.Capturar()["historial"].AsGodotArray()[0].AsString() == "zona", "Persistencia de identificadores");
		cargadas.NuevaPista();
		Verificar(!cargadas.Disponible(true, 120) && cargadas.Usos == 1, "Pista nueva reinicia tiempo, conserva registro");
		cargadas.Avanzar(30, true, true);
		Verificar(cargadas.Disponible(true, 30) && !cargadas.Disponible(true, 60), "Umbral configurable");
		cargadas.Restaurar(new Godot.Collections.Dictionary());
		Verificar(cargadas.Usos == 0 && cargadas.SinPista == 0, "Compatibilidad con partidas antiguas");

		var caso = new Caso1 { Jugador = new Jugador() };
		Verificar(Orientacion(caso).Item1 == "fogata", "Inicio: zona accesible");
		Pistas(caso).UnionWith(new[] { "lena_intacta", "libro_guardia_quemado", "taza_delantal" });
		Verificar(Orientacion(caso).Item1 == "cocina", "No orientar hacia cabaña cerrada sin llavero");
		var objetos = (HashSet<string>)typeof(Jugador).GetField("_objetos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(caso.Jugador);
		objetos.Add("llavero_porky");
		Verificar(Orientacion(caso).Item1 == "porky", "Cabaña accesible con llavero");
		Pistas(caso).UnionWith(new[] { "nota_porky", "foto_grupo", "recorte_periodico", "oferta_compra", "baul_disfraces", "registro_enfermeria", "botas_tony", "interruptor_generador", "candado_caseta" });
		Verificar(Orientacion(caso).Item1 == null, "No recomendar pista deshabilitada ni escenario pendiente");
		caso.Jugador.Free();
		caso.Free();
		var tutorial = new Caso0();
		Verificar(Orientacion(tutorial).Item1 == "sala", "Tutorial: orientación de zona");
		Pistas(tutorial).UnionWith(new[] { "grabadora", "reloj" });
		Verificar(Orientacion(tutorial).Item1 == "tablero_sala", "Orientar al tablero sin revelar la combinación");
		tutorial.Free();
		GD.Print($"HU-10: {_comprobaciones} comprobaciones correctas.");
	}
}
