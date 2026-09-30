using Godot;
using System;
using System.Collections.Generic;

// Sonidos sintetizados en código mientras el proyecto no tenga archivos de audio.
// Cuando el equipo tenga sonidos reales, basta con cambiar Crear() por GD.Load<AudioStream>(ruta).
public static class Sonidos
{
	public enum Tipo
	{
		AbrirMetal,   // Puerta de casillero
		Cajon,        // Cajón que se desliza
		Cremallera,   // Mochila
		Bloqueado,    // Candado o puerta cerrada
		Pista,        // Evidencia nueva
		Paso,         // Clic suave de interfaz
		// Bucles de ambiente
		ZumbidoFluorescente,
		LluviaCalle,
		GoteoVestuario,
		TonoSala,
	}

	private const int Frecuencia = 22050;
	private static readonly Dictionary<Tipo, AudioStreamWav> _cache = new Dictionary<Tipo, AudioStreamWav>();

	public static AudioStreamWav Obtener(Tipo tipo)
	{
		if (!_cache.TryGetValue(tipo, out var sonido))
		{
			sonido = Crear(tipo);
			_cache[tipo] = sonido;
		}
		return sonido;
	}

	// Reproduce un efecto corto en una posición del mundo y se borra solo.
	public static void Reproducir(Node padre, Tipo tipo, Vector3 posicion, float volumenDb = 0f)
	{
		var reproductor = new AudioStreamPlayer3D { Stream = Obtener(tipo), VolumeDb = volumenDb, UnitSize = 4f };
		padre.GetTree().CurrentScene.AddChild(reproductor);
		reproductor.GlobalPosition = posicion;
		reproductor.Finished += reproductor.QueueFree;
		reproductor.Play();
	}

	private static AudioStreamWav Crear(Tipo tipo)
	{
		var azar = new RandomNumberGenerator { Seed = (ulong)tipo + 7 };
		switch (tipo)
		{
			case Tipo.AbrirMetal:
				// Golpe metálico: tonos inarmónicos que decaen + chasquido inicial
				return Generar(0.45f, false, t =>
					Mathf.Exp(-t * 9f) * (0.45f * Mathf.Sin(t * 2 * Mathf.Pi * 310f) + 0.3f * Mathf.Sin(t * 2 * Mathf.Pi * 587f)
					+ 0.2f * Mathf.Sin(t * 2 * Mathf.Pi * 1130f)) + (t < 0.02f ? azar.Randfn() * 0.5f : 0f));
			case Tipo.Cajon:
				// Roce de madera: ruido filtrado que sube y baja
				float previo = 0f;
				return Generar(0.35f, false, t =>
				{
					previo = previo * 0.92f + azar.Randfn() * 0.08f;
					return previo * 3f * Mathf.Sin(Mathf.Pi * t / 0.35f);
				});
			case Tipo.Cremallera:
				return Generar(0.4f, false, t =>
					(Mathf.Sin(t * 2 * Mathf.Pi * 90f) > 0.6f ? azar.Randfn() * 0.5f : 0f) * Mathf.Sin(Mathf.Pi * t / 0.4f));
			case Tipo.Bloqueado:
				return Generar(0.25f, false, t => Mathf.Exp(-t * 18f) * 0.7f * Mathf.Sin(t * 2 * Mathf.Pi * 140f)
					+ (t < 0.01f ? azar.Randfn() * 0.4f : 0f));
			case Tipo.Pista:
				// Dos notas ascendentes, estilo "¡aha!"
				return Generar(0.7f, false, t =>
				{
					float nota = t < 0.18f ? 659f : 988f;
					float local = t < 0.18f ? t : t - 0.18f;
					return 0.35f * Mathf.Exp(-local * 5f) * (Mathf.Sin(t * 2 * Mathf.Pi * nota) + 0.3f * Mathf.Sin(t * 4 * Mathf.Pi * nota));
				});
			case Tipo.Paso:
				return Generar(0.06f, false, t => Mathf.Exp(-t * 60f) * 0.3f * Mathf.Sin(t * 2 * Mathf.Pi * 1800f));
			case Tipo.ZumbidoFluorescente:
				return Generar(2f, true, t => 0.12f * Mathf.Sin(t * 2 * Mathf.Pi * 120f) + 0.05f * Mathf.Sin(t * 2 * Mathf.Pi * 240f)
					+ azar.Randfn() * 0.01f);
			case Tipo.LluviaCalle:
				float lluvia = 0f;
				return Generar(3f, true, t =>
				{
					lluvia = lluvia * 0.7f + azar.Randfn() * 0.3f;
					return lluvia * 0.35f + (azar.Randf() < 0.0008f ? 0.6f : 0f);
				});
			case Tipo.GoteoVestuario:
				// Gota cada ~1.3 s sobre un zumbido muy bajo
				return Generar(2.6f, true, t =>
				{
					float gota = t % 1.3f;
					return 0.4f * Mathf.Exp(-gota * 40f) * Mathf.Sin(gota * 2 * Mathf.Pi * (900f - gota * 3000f))
						+ 0.03f * Mathf.Sin(t * 2 * Mathf.Pi * 100f);
				});
			default: // TonoSala: reloj y aire acondicionado lejano
				float aire = 0f;
				return Generar(2f, true, t =>
				{
					aire = aire * 0.97f + azar.Randfn() * 0.03f;
					float tic = t % 1f;
					return aire * 0.5f + 0.25f * Mathf.Exp(-tic * 200f) * Mathf.Sin(tic * 2 * Mathf.Pi * 2500f);
				});
		}
	}

	private static AudioStreamWav Generar(float segundos, bool bucle, Func<float, float> onda)
	{
		int muestras = (int)(segundos * Frecuencia);
		var datos = new byte[muestras * 2];
		for (int i = 0; i < muestras; i++)
		{
			float valor = Mathf.Clamp(onda((float)i / Frecuencia), -1f, 1f);
			short muestra = (short)(valor * short.MaxValue * 0.8f);
			datos[i * 2] = (byte)(muestra & 0xFF);
			datos[i * 2 + 1] = (byte)((muestra >> 8) & 0xFF);
		}
		var sonido = new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = Frecuencia,
			Stereo = false,
			Data = datos,
		};
		if (bucle)
		{
			sonido.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
			sonido.LoopEnd = muestras;
		}
		return sonido;
	}
}
