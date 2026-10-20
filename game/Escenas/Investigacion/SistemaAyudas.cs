using Godot;
using System;
using Diccionario = Godot.Collections.Dictionary;

// Estado por partida. El caso decide qué orientación es accesible; nunca se muestra automáticamente.
public sealed class SistemaAyudas
{
	public double SinPista { get; private set; }
	public int Usos { get; private set; }
	private Godot.Collections.Array<string> _historial = new();

	public void Avanzar(double delta, bool activo, bool habilitado)
	{
		if (activo && habilitado && double.IsFinite(delta) && delta > 0) SinPista += delta;
	}
	public void NuevaPista() => SinPista = 0;
	public bool Disponible(bool habilitado, double umbral) => habilitado && SinPista >= Math.Max(1, umbral);

	public bool Solicitar(bool habilitado, double umbral, string id, string texto, Action<string> mostrar)
	{
		if (!Disponible(habilitado, umbral) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(texto)) return false;
		mostrar(texto);
		Usos++;
		_historial.Add(id);
		return true;
	}
	public Diccionario Capturar() => new()
	{
		{ "sin_pista", SinPista }, { "usos", Usos }, { "historial", _historial.Duplicate() },
	};
	public void Restaurar(Diccionario estado)
	{
		SinPista = estado.TryGetValue("sin_pista", out var tiempo) && double.IsFinite(tiempo.AsDouble()) ? Math.Max(0, tiempo.AsDouble()) : 0;
		Usos = estado.TryGetValue("usos", out var usos) ? Math.Max(0, usos.AsInt32()) : 0;
		_historial.Clear();
		if (estado.TryGetValue("historial", out var historial))
			foreach (var id in historial.AsGodotArray()) _historial.Add(id.AsString());
	}
}
