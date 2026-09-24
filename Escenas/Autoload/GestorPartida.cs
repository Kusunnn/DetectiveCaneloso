using Godot;
using System;

// Autoload: estado global de la partida y navegación entre escenas.
// El guardado es provisional: solo escribe un marcador para habilitar "Continuar".
public partial class GestorPartida : Node
{
	public const string RutaGuardado = "user://partida_guardada.json";
	public const string EscenaMenuInicio = "res://Escenas/Menus/menu_inicio.tscn";
	public const string EscenaJuego = "res://Escenas/Juego/juego_prueba.tscn";

	public static GestorPartida Instancia { get; private set; }

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

	public void NuevaPartida()
	{
		HayCambiosSinGuardar = true;
		CambiarEscena(EscenaJuego);
	}

	public void ContinuarPartida()
	{
		// TODO: cargar los datos de RutaGuardado cuando exista el sistema de guardado.
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
		var datos = new Godot.Collections.Dictionary { { "guardado_en", Time.GetDatetimeStringFromSystem() } };
		archivo.StoreString(Json.Stringify(datos));
		HayCambiosSinGuardar = false;
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
