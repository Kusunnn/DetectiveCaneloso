using Godot;
using System;

// Fluorescente viejo: apagones cortos y aleatorios.
public partial class LuzParpadeante : OmniLight3D
{
	[Export] public float EnergiaNormal = 1.2f;

	private readonly RandomNumberGenerator _azar = new RandomNumberGenerator();
	private double _siguienteCambio = 0.0;

	public override void _Process(double delta)
	{
		_siguienteCambio -= delta;
		if (_siguienteCambio > 0.0) return;

		bool apagon = _azar.Randf() < 0.25f;
		LightEnergy = apagon ? EnergiaNormal * 0.1f : EnergiaNormal;
		_siguienteCambio = apagon ? _azar.RandfRange(0.04f, 0.15f) : _azar.RandfRange(0.3f, 2.5f);
	}
}
