using Godot;
using System;
using System.Collections.Generic;

// Opciones del juego. Se usa tanto en el menú de inicio como en el de pausa.
public partial class MenuOpciones : PanelMenu
{
	// Deslizador de volumen de cada bus de audio (Master = "Volumen general")
	private readonly Dictionary<string, HSlider> _volumenes = new();
	private CheckButton _interruptorPantallaCompleta;
	private CheckButton _interruptorAgacharse;
	private HSlider _deslizadorSensibilidad;
	private ulong _ultimaMuestra;

	public override void _Ready()
	{
		_volumenes["Master"] = GetNode<HSlider>("%DeslizadorVolumen");
		_volumenes["Musica"] = GetNode<HSlider>("%DeslizadorMusica");
		_volumenes["Ambiente"] = GetNode<HSlider>("%DeslizadorAmbiente");
		_volumenes["Efectos"] = GetNode<HSlider>("%DeslizadorEfectos");
		_volumenes["Interfaz"] = GetNode<HSlider>("%DeslizadorInterfaz");
		_interruptorPantallaCompleta = GetNode<CheckButton>("%InterruptorPantallaCompleta");
		_interruptorAgacharse = GetNode<CheckButton>("%InterruptorAgacharse");
		_deslizadorSensibilidad = GetNode<HSlider>("%DeslizadorSensibilidad");
		base._Ready();

		foreach (var (bus, deslizador) in _volumenes)
		{
			deslizador.ValueChanged += valor =>
			{
				Configuracion.Instancia.FijarVolumen(bus, (float)valor);
				// Muestra de cómo suena (como mucho una cada 0.15 s mientras se arrastra)
				ulong ahora = Time.GetTicksMsec();
				if (ahora - _ultimaMuestra > 150 && (bus == "Efectos" || bus == "Interfaz" || bus == "Master"))
				{
					_ultimaMuestra = ahora;
					Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic);
				}
			};
		}
		_interruptorPantallaCompleta.Toggled += activado => Configuracion.Instancia.PantallaCompleta = activado;
		_interruptorAgacharse.Toggled += activado => Configuracion.Instancia.AgacharseAlternar = activado;
		_deslizadorSensibilidad.ValueChanged += valor => Configuracion.Instancia.SensibilidadRaton = (float)valor;
	}

	public override void Abrir()
	{
		foreach (var (bus, deslizador) in _volumenes)
			deslizador.SetValueNoSignal(Configuracion.Instancia.Volumen(bus));
		_interruptorPantallaCompleta.SetPressedNoSignal(Configuracion.Instancia.PantallaCompleta);
		_interruptorAgacharse.SetPressedNoSignal(Configuracion.Instancia.AgacharseAlternar);
		_deslizadorSensibilidad.SetValueNoSignal(Configuracion.Instancia.SensibilidadRaton);
		base.Abrir();
	}

	public override void Cerrar()
	{
		Configuracion.Instancia.Guardar();
		base.Cerrar();
	}
}
