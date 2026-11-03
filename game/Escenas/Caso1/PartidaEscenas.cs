using Godot;
using System;
using Diccionario = Godot.Collections.Dictionary;

// Guardado del Caso 1 repartido en varios escenarios (Campamento, Lago y sala de interrogatorio).
// Lo compartido va en la raíz del estado: jugador (con sus objetos), pistas del diario, notas y tiempo.
// Lo de cada escenario (contenedores, puertas, zonas vistas) va en "escenas", bajo la ruta de su .tscn,
// así al ir y volver cada lugar queda como se dejó.
public static class PartidaEscenas
{
	public const string Campamento = "res://Escenas/Caso1/escenario_campamento.tscn";
	public const string Lago = "res://Escenas/Caso1/Lago/escenario_lago.tscn";
	public const string Sala = "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn";

// Pistas del capítulo: 13 del Campamento y 9 del Lago y el Bosque.
public const int TotalPistas = 22;

// Pistas del Lago necesarias antes de poder ir a la estación.
public const int PistasLagoParaEstacion = 5;

	// Pistas del diario sin contar los detalles descubiertos en la vista de examen
	public static int PistasEnDiario(CanvasLayer hud)
	{
		int total = 0;
		foreach (Variant pista in hud.Get("pistas").AsGodotArray())
			if (pista.As<Resource>().Get("origen_id").AsStringName().ToString() == "") total++;
		return total;
	}

	// Estado de todos los escenarios. Las partidas de antes (solo Campamento) lo tenían en la raíz.
	public static Diccionario Escenas(Diccionario estado)
	{
		if (estado == null) return new Diccionario();
		if (estado.TryGetValue("escenas", out Variant escenas)) return escenas.AsGodotDictionary().Duplicate(true);
		var anterior = new Diccionario();
		foreach (string clave in new[] { "contenedores", "puertas", "zonas_vistas" })
			if (estado.TryGetValue(clave, out Variant v)) anterior[clave] = v;
		return new Diccionario { { Campamento, anterior } };
	}

	// Lo guardado de un escenario, o null si nunca se visitó
	public static Diccionario DeEscena(Diccionario estado, string escena)
	{
		var escenas = Escenas(estado);
		return escenas.TryGetValue(escena, out Variant d) ? d.AsGodotDictionary() : null;
	}

	// Una pista que se encontró en otro escenario vuelve al diario con su foto y su modelo para la vista de examen
	public static void RestaurarPistaDeOtroEscenario(Jugador jugador, CanvasLayer hud, string id, string titulo, string descripcion)
	{
		string foto = $"res://Assets/Pistas/caso1/{id}.png";
		jugador.RestaurarEvidencia(id, titulo, descripcion, ResourceLoader.Exists(foto) ? GD.Load<Texture2D>(foto) : null);
		string modelo = $"res://Assets/Pistas/Modelos/{id}/{id}.tscn";
		var pistas = hud.Get("pistas").AsGodotArray();
		if (pistas.Count > 0 && ResourceLoader.Exists(modelo))
			pistas[pistas.Count - 1].As<Resource>().Set("modelo", GD.Load<PackedScene>(modelo));
	}

	// Pone al jugador en el Marker3D de entrada (al llegar desde otro escenario)
	public static void ColocarEnEntrada(Node escena, Node3D jugador)
	{
		string punto = GestorPartida.Instancia?.PuntoEntrada;
		if (string.IsNullOrEmpty(punto)) return;
		if (escena.FindChild(punto, true, false) is not Node3D marcador) return;
		jugador.GlobalPosition = marcador.GlobalPosition;
		jugador.Rotation = new Vector3(0, marcador.GlobalRotation.Y, 0);
	}
}
