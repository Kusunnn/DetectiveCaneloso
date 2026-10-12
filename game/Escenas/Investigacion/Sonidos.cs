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

		// Ambientes: el volumen es la ganancia que los iguala a los efectos (se suma al de la escena)
		[Tipo.ZumbidoFluorescente] = new(new[] { "Ambiente/zumbido_fluorescente.ogg" }, 9.4f, false, true),
		[Tipo.LluviaCalle] = new(new[] { "Ambiente/lluvia_ventana.wav" }, 0f, false, true),
		[Tipo.GoteoVestuario] = new(new[] { "Ambiente/agua_vestidores.ogg" }, 19.1f, false, true),
		[Tipo.TonoSala] = new(new[] { "Ambiente/zumbido_sala.ogg" }, 7.3f, false, true),
		[Tipo.RelojPared] = new(new[] { "Ambiente/reloj_pared.wav" }, 0f, false, true),
	};

	public static float VolumenDe(Tipo tipo) => Definiciones[tipo].VolumenDb;
	public static bool EsBucle(Tipo tipo) => Definiciones[tipo].Bucle;

	// Sin caché propia: GD.Load ya reutiliza los archivos y así no quedan recursos vivos al salir.
	public static AudioStream Obtener(Tipo tipo)
	{
		AudioStream sonido;
		var def = Definiciones[tipo];
		if (def.Bucle)
		{
			sonido = ComoBucle(GD.Load<AudioStream>(Carpeta + def.Archivos[0]));
		}
		else if (!def.Variar && def.Archivos.Length == 1)
		{
			sonido = GD.Load<AudioStream>(Carpeta + def.Archivos[0]);
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
				azar.AddStream(i, GD.Load<AudioStream>(Carpeta + def.Archivos[i]));
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
		var arbol = padre.GetTree();
		if (arbol == null) return;
		var reproductor = new AudioStreamPlayer
		{
			Stream = Obtener(tipo),
			VolumeDb = VolumenDe(tipo) + volumenDb,
			PitchScale = tono,
			Bus = BusInterfaz,
			ProcessMode = Node.ProcessModeEnum.Always,
		};
		arbol.Root.AddChild(reproductor);
		reproductor.Finished += reproductor.QueueFree;
		reproductor.Play();
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
