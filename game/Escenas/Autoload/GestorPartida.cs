using Godot;
using System;

// Autoload: estado global de la partida y navegación entre escenas.
// El guardado es provisional: solo escribe un marcador para habilitar "Continuar".
public partial class GestorPartida : Node
{
	public const string RutaGuardado = "user://partida_guardada.json";
	public const string EscenaMenuInicio = "res://Escenas/Menus/menu_inicio.tscn";
	public const string EscenaJuego = "res://Escenas/Interrogatorio/sala_interrogatorio.tscn";

	public static GestorPartida Instancia { get; private set; }

	// HU-04: si es true, la sala de interrogatorio no muestra el tutorial.
	// Se elige al empezar una nueva partida y se guarda con ella.
	public bool SaltarTutorial { get; set; } = false;

	// true si hay progreso que se perdería al salir sin guardar.
	// El juego debe ponerlo a true cuando cambie algo que haya que guardar.
	public bool HayCambiosSinGuardar { get; set; } = false;

	public override void _EnterTree()
	{
		Instancia = this;
	}

	public bool HayPartidaGuardada()
	{
		return FileAccess.FileExists(RutaGuardado);
	}

	public void NuevaPartida(bool saltarTutorial = false)
	{
		// La elección del tutorial queda guardada en la partida desde el inicio (HU-04 CA-03)
		SaltarTutorial = saltarTutorial;
		GuardarPartida();
		CambiarEscena(EscenaJuego);
	}

	public void ContinuarPartida()
	{
		// TODO: cargar el resto de datos de RutaGuardado cuando exista el sistema de guardado.
		SaltarTutorial = CargarDatos().TryGetValue("saltar_tutorial", out Variant saltar) && saltar.AsBool();
		HayCambiosSinGuardar = false;
		CambiarEscena(EscenaJuego);
	}

	public void GuardarPartida()
	{
		// TODO: guardar el estado real de la partida.
		using var archivo = FileAccess.Open(RutaGuardado, FileAccess.ModeFlags.Write);
		if (archivo == null)
		{
			GD.PushWarning("GestorPartida: no se pudo guardar la partida (" + FileAccess.GetOpenError() + ").");
			return;
		}
		var datos = new Godot.Collections.Dictionary
		{
			{ "guardado_en", Time.GetDatetimeStringFromSystem() },
			{ "saltar_tutorial", SaltarTutorial },
		};
		archivo.StoreString(Json.Stringify(datos));
		HayCambiosSinGuardar = false;
	}

	// Lee el archivo de guardado; si no existe o está dañado devuelve un diccionario vacío.
	private Godot.Collections.Dictionary CargarDatos()
	{
		if (!HayPartidaGuardada()) return new Godot.Collections.Dictionary();

		using var archivo = FileAccess.Open(RutaGuardado, FileAccess.ModeFlags.Read);
		Variant datos = archivo == null ? default : Json.ParseString(archivo.GetAsText());
		return datos.VariantType == Variant.Type.Dictionary
			? datos.AsGodotDictionary()
			: new Godot.Collections.Dictionary();
	}

	public void IrAlMenuInicio()
	{
		HayCambiosSinGuardar = false;
		CambiarEscena(EscenaMenuInicio);
	}

	private void CambiarEscena(string ruta)
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(ruta);
	}
}
