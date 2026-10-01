using Godot;
using System;

// Opciones del juego. Se usa tanto en el menú de inicio como en el de pausa.
public partial class MenuOpciones : PanelMenu
{
	private HSlider _deslizadorVolumen;
	private CheckButton _interruptorPantallaCompleta;
	private CheckButton _interruptorAgacharse;

	public override void _Ready()
	{
		_deslizadorVolumen = GetNode<HSlider>("%DeslizadorVolumen");
		_interruptorPantallaCompleta = GetNode<CheckButton>("%InterruptorPantallaCompleta");
		_interruptorAgacharse = GetNode<CheckButton>("%InterruptorAgacharse");
		base._Ready();

		_deslizadorVolumen.ValueChanged += valor => Configuracion.Instancia.VolumenGeneral = (float)valor;
		_interruptorPantallaCompleta.Toggled += activado => Configuracion.Instancia.PantallaCompleta = activado;
		_interruptorAgacharse.Toggled += activado => Configuracion.Instancia.AgacharseAlternar = activado;
	}

	public override void Abrir()
	{
		_deslizadorVolumen.SetValueNoSignal(Configuracion.Instancia.VolumenGeneral);
		_interruptorPantallaCompleta.SetPressedNoSignal(Configuracion.Instancia.PantallaCompleta);
		_interruptorAgacharse.SetPressedNoSignal(Configuracion.Instancia.AgacharseAlternar);
		base.Abrir();
	}

	public override void Cerrar()
	{
		Configuracion.Instancia.Guardar();
		base.Cerrar();
	}
}
