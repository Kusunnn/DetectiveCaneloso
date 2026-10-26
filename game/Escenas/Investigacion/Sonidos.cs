using Godot;
using System;
using System.Collections.Generic;

// Sonidos del juego: archivos CC0 de Assets/Audio (créditos en CREDITOS_AUDIO.md), ya recortados y
// normalizados al mismo nivel percibido. Cada tipo sabe su bus, su volumen relativo y si varía.
//   Sonidos.Reproducir(...)   → efecto en el mundo (3D, bus Efectos): se oye más fuerte cerca.
//   Sonidos.ReproducirUI(...) → interfaz y avisos (sin posición, bus Interfaz).
public static class Sonidos
{
	// Los valores se guardan como enteros en las escenas: los nuevos van SIEMPRE al final.
	public enum Tipo
	{
		AbrirMetal,   // Puerta de casillero
		Cajon,        // Cajón que se desliza
		Cremallera,   // Mochila
		Bloqueado,    // Candado o puerta cerrada
		Pista,        // Evidencia nueva (siempre el mismo sonido)
		Paso,         // Pisada del detective
		// Bucles de ambiente
		ZumbidoFluorescente,
		LluviaCalle,
		GoteoVestuario,
		TonoSala,
		// Agregados con los archivos de audio
		Deduccion,
		Error,
		ErrorAcusacion,
		Objeto,
		Nota,
		LibretaAbrir,
		LibretaCerrar,
		Expediente,
		Pagina,
		Sello,
		Texto,
		Hover,
		Clic,
		CerrarMetal,
		PuertaAbrir,
		PuertaTrabada,
		RelojPared,
		// Exteriores (Caso 1, campamento)
		Grillos,
		VientoPinos,
		Brasas,
		Buho,
		AguaLago,
		ZumbidoGenerador,
		// Vista de examen de evidencias (HU-07)
		ExamenAbrir,
		ExamenCerrar,
		ExamenGirar,
		ExamenAcercar,
		ExamenAlejar,
		ExamenTic,
		ExamenDescubrir,
	}

	public const string BusMusica = "Musica";
	public const string BusAmbiente = "Ambiente";
	public const string BusEfectos = "Efectos";
	public const string BusInterfaz = "Interfaz";

	private const string Carpeta = "res://Assets/Audio/";

	// Archivos (varias versiones = alternas al azar), volumen relativo en dB y si lleva ±5 % de tono y volumen
	private record Definicion(string[] Archivos, float VolumenDb, bool Variar = true, bool Bucle = false);

	private static readonly Dictionary<Tipo, Definicion> Definiciones = new()
	{
		[Tipo.AbrirMetal] = new(new[] { "Efectos/metal_abrir_1.wav", "Efectos/metal_abrir_2.wav", "Efectos/metal_abrir_3.wav" }, -3f),
		[Tipo.CerrarMetal] = new(new[] { "Efectos/metal_cerrar_1.wav", "Efectos/metal_cerrar_2.wav", "Efectos/metal_cerrar_3.wav" }, -5f),
		[Tipo.Cajon] = new(new[] { "Efectos/cajon_1.wav", "Efectos/cajon_2.wav", "Efectos/cajon_3.wav" }, -4f),
		[Tipo.Cremallera] = new(new[] { "Efectos/cremallera_1.wav", "Efectos/cremallera_2.wav", "Efectos/cremallera_3.wav" }, -3f),
		[Tipo.Bloqueado] = new(new[] { "Efectos/bloqueado_1.wav", "Efectos/bloqueado_2.wav", "Efectos/bloqueado_3.wav" }, -5f),
		[Tipo.PuertaAbrir] = new(new[] { "Efectos/puerta_abrir_1.wav", "Efectos/puerta_abrir_2.wav" }, -3f),
		[Tipo.PuertaTrabada] = new(new[] { "Efectos/puerta_trabada.wav" }, -4f),
		[Tipo.Paso] = new(new[] { "Efectos/paso_1.wav", "Efectos/paso_2.wav", "Efectos/paso_3.wav", "Efectos/paso_4.wav" }, -14f),

		[Tipo.Pista] = new(new[] { "Interfaz/pista.wav" }, 0f, Variar: false),
		[Tipo.Deduccion] = new(new[] { "Interfaz/deduccion.wav" }, 1f, Variar: false),
		[Tipo.Error] = new(new[] { "Interfaz/error_suave.wav" }, -8f),
		[Tipo.ErrorAcusacion] = new(new[] { "Interfaz/error_acusacion.wav" }, -6f, Variar: false),
		[Tipo.Objeto] = new(new[] { "Interfaz/objeto.wav" }, -4f, Variar: false),
		[Tipo.Nota] = new(new[] { "Interfaz/nota.wav" }, -9f),
		[Tipo.LibretaAbrir] = new(new[] { "Interfaz/libreta_abrir.wav" }, -6f),
		[Tipo.LibretaCerrar] = new(new[] { "Interfaz/libreta_cerrar.wav" }, -8f),
		[Tipo.Expediente] = new(new[] { "Interfaz/expediente.wav" }, -6f),
		[Tipo.Pagina] = new(new[] { "Interfaz/pagina.wav" }, -9f),
		[Tipo.Sello] = new(new[] { "Interfaz/sello.wav" }, -10f),
		[Tipo.Texto] = new(new[] { "Interfaz/texto_1.wav", "Interfaz/texto_2.wav", "Interfaz/texto_3.wav" }, -19f),
		[Tipo.Hover] = new(new[] { "Interfaz/hover_1.wav", "Interfaz/hover_2.wav", "Interfaz/hover_3.wav" }, -17f),
		[Tipo.Clic] = new(new[] { "Interfaz/clic_1.wav", "Interfaz/clic_2.wav", "Interfaz/clic_3.wav" }, -9f),
		// Vista de examen (HU-07): OGG de Kenney copiados tal cual; el volumen los iguala a los demás
		[Tipo.ExamenAbrir] = new(new[] { "Examen/abrir.ogg" }, -6f),
		[Tipo.ExamenCerrar] = new(new[] { "Examen/cerrar.ogg" }, -8f),
		[Tipo.ExamenGirar] = new(new[] { "Examen/girar_1.ogg", "Examen/girar_2.ogg", "Examen/girar_3.ogg" }, -12f),
		// Zoom: "maximize" y "minimize" de Kenney, graves y suaves (los "scroll" de antes eran clics agudos que saturaban)
		[Tipo.ExamenAcercar] = new(new[] { "Examen/acercar_1.ogg", "Examen/acercar_2.ogg" }, -20f),
		[Tipo.ExamenAlejar] = new(new[] { "Examen/alejar_1.ogg", "Examen/alejar_2.ogg" }, -20f),
		[Tipo.ExamenTic] = new(new[] { "Examen/tic.ogg" }, -6f, Variar: false),
		[Tipo.ExamenDescubrir] = new(new[] { "Examen/descubrir.ogg" }, -2f, Variar: false),

		// Ambientes: el volumen es la ganancia que los iguala a los efectos (se suma al de la escena)
		[Tipo.ZumbidoFluorescente] = new(new[] { "Ambiente/zumbido_fluorescente.ogg" }, 9.4f, false, true),
		[Tipo.LluviaCalle] = new(new[] { "Ambiente/lluvia_ventana.wav" }, 0f, false, true),
		[Tipo.GoteoVestuario] = new(new[] { "Ambiente/agua_vestidores.ogg" }, 19.1f, false, true),
		[Tipo.TonoSala] = new(new[] { "Ambiente/zumbido_sala.ogg" }, 7.3f, false, true),
		[Tipo.RelojPared] = new(new[] { "Ambiente/reloj_pared.wav" }, 0f, false, true),
		// Exteriores: archivos de Assets/Caso1/Sonidos (créditos en el mismo CREDITOS_AUDIO.md)
		[Tipo.Grillos] = new(new[] { "res://Assets/Caso1/Sonidos/grillos.ogg" }, -10f, false, true),
		[Tipo.VientoPinos] = new(new[] { "res://Assets/Caso1/Sonidos/viento_pinos.ogg" }, -12f, false, true),
		[Tipo.Brasas] = new(new[] { "res://Assets/Caso1/Sonidos/brasas.ogg" }, -9f, false, true),
		[Tipo.Buho] = new(new[] { "res://Assets/Caso1/Sonidos/buho.ogg" }, -8f, Variar: true),
		[Tipo.AguaLago] = new(new[] { "res://Assets/Caso1/Sonidos/agua_lago.ogg" }, -10f, false, true),
		[Tipo.ZumbidoGenerador] = new(new[] { "res://Assets/Caso1/Sonidos/generador.ogg" }, -9f, false, true),
	};

	// Las rutas cortas son relativas a Assets/Audio; las que empiezan con res:// se usan tal cual
	private static string Ruta(string archivo) => archivo.StartsWith("res://") ? archivo : Carpeta + archivo;

	public static float VolumenDe(Tipo tipo) => Definiciones[tipo].VolumenDb;
	public static bool EsBucle(Tipo tipo) => Definiciones[tipo].Bucle;

	// Sin caché propia: GD.Load ya reutiliza los archivos y así no quedan recursos vivos al salir.
	public static AudioStream Obtener(Tipo tipo)
	{
		AudioStream sonido;
		var def = Definiciones[tipo];
		if (def.Bucle)
		{
			sonido = ComoBucle(GD.Load<AudioStream>(Ruta(def.Archivos[0])));
		}
		else if (!def.Variar && def.Archivos.Length == 1)
		{
			sonido = GD.Load<AudioStream>(Ruta(def.Archivos[0]));
		}
		else
		{
			// Versiones alternas, ±5 % de tono y ±0.4 dB (≈5 %) de volumen para que no canse al repetirse
			var azar = new AudioStreamRandomizer
			{
				RandomPitch = def.Variar ? 1.05f : 1f,
				RandomVolumeOffsetDb = def.Variar ? 0.4f : 0f,
				PlaybackMode = AudioStreamRandomizer.PlaybackModeEnum.RandomNoRepeats,
			};
			for (int i = 0; i < def.Archivos.Length; i++)
				azar.AddStream(i, GD.Load<AudioStream>(Ruta(def.Archivos[i])));
			sonido = azar;
		}
		return sonido;
	}

	// Efecto en una posición del mundo; se borra solo al terminar.
	public static void Reproducir(Node padre, Tipo tipo, Vector3 posicion, float volumenDb = 0f, float tono = 1f)
	{
		var reproductor = new AudioStreamPlayer3D
		{
			Stream = Obtener(tipo),
			VolumeDb = VolumenDe(tipo) + volumenDb,
			PitchScale = tono,
			UnitSize = 3f,
			MaxDistance = 18f,
			Bus = BusEfectos,
		};
		padre.GetTree().CurrentScene.AddChild(reproductor);
		reproductor.GlobalPosition = posicion;
		reproductor.Finished += reproductor.QueueFree;
		reproductor.Play();
	}

	// Sonido de interfaz o aviso: sin posición, suena igual con cámara en primera o tercera persona
	// y también con el juego en pausa.
	public static void ReproducirUI(Node padre, Tipo tipo, float volumenDb = 0f, float tono = 1f)
	{
		// Un temporizador puede llegar cuando el nodo ya salió de la escena (al cerrar o cambiar de escena)
		if (padre == null || !GodotObject.IsInstanceValid(padre) || !padre.IsInsideTree()) return;
		var arbol = padre.GetTree();
		var reproductor = new AudioStreamPlayer
		{
			Stream = Obtener(tipo),
			VolumeDb = VolumenDe(tipo) + volumenDb,
			PitchScale = tono,
			Bus = BusInterfaz,
			ProcessMode = Node.ProcessModeEnum.Always,
		};
		reproductor.Finished += reproductor.QueueFree;
		// El foco inicial puede sonar durante _Ready, mientras Root agrega la escena.
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(arbol.Root)) { reproductor.Free(); return; }
			arbol.Root.AddChild(reproductor);
			reproductor.Play();
		}).CallDeferred();
	}

	private static AudioStream ComoBucle(AudioStream sonido)
	{
		switch (sonido)
		{
			case AudioStreamOggVorbis ogg:
				ogg.Loop = true;
				break;
			case AudioStreamWav wav:
				wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
				wav.LoopBegin = 0;
				wav.LoopEnd = (int)(wav.GetLength() * wav.MixRate);
				break;
		}
		return sonido;
	}
}
