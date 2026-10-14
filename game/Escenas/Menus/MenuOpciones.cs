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
	private CheckButton _interruptorAyudas;
	private HSlider _deslizadorSensibilidad;
	private OptionButton _tamanoTexto;
	private Button _botonTeclas;
	private PanelTeclas _panelTeclas;
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
		_interruptorAyudas = GetNode<CheckButton>("%InterruptorAyudas");
		_deslizadorSensibilidad = GetNode<HSlider>("%DeslizadorSensibilidad");
		CrearAccesibilidad();
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
		_interruptorAyudas.Toggled += activado => Configuracion.Instancia.AyudasVisuales = activado;
		_deslizadorSensibilidad.ValueChanged += valor => Configuracion.Instancia.SensibilidadRaton = (float)valor;
	}

	// Tamaño del texto y cambio de teclas: dos filas más en la rejilla de la escena
	private void CrearAccesibilidad()
	{
		var rejilla = _deslizadorSensibilidad.GetParent();
		rejilla.AddChild(new Label { Text = "Tamaño del texto" });
		_tamanoTexto = new OptionButton { CustomMinimumSize = new Vector2(240, 0) };
		foreach (var (_, nombre) in Configuracion.EscalasTexto) _tamanoTexto.AddItem(nombre);
		_tamanoTexto.ItemSelected += indice => Configuracion.Instancia.EscalaTexto = Configuracion.EscalasTexto[indice].Escala;
		rejilla.AddChild(_tamanoTexto);
		rejilla.AddChild(new Label { Text = "Teclas" });
		_botonTeclas = new Button { Text = "Cambiar teclas…", CustomMinimumSize = new Vector2(240, 0) };
		rejilla.AddChild(_botonTeclas);
		_panelTeclas = new PanelTeclas();
		AddChild(_panelTeclas);
		_botonTeclas.Pressed += _panelTeclas.Abrir;
		_panelTeclas.Cerrado += () => _botonTeclas.GrabFocus();
	}

	public override void Abrir()
	{
		foreach (var (bus, deslizador) in _volumenes)
			deslizador.SetValueNoSignal(Configuracion.Instancia.Volumen(bus));
		_interruptorPantallaCompleta.SetPressedNoSignal(Configuracion.Instancia.PantallaCompleta);
		_interruptorAgacharse.SetPressedNoSignal(Configuracion.Instancia.AgacharseAlternar);
		_interruptorAyudas.SetPressedNoSignal(Configuracion.Instancia.AyudasVisuales);
		_deslizadorSensibilidad.SetValueNoSignal(Configuracion.Instancia.SensibilidadRaton);
		int escala = Array.FindIndex(Configuracion.EscalasTexto, e => Mathf.IsEqualApprox(e.Escala, Configuracion.Instancia.EscalaTexto));
		_tamanoTexto.Select(Math.Max(escala, 0));
		base.Abrir();
	}

	public override void Cerrar()
	{
		Configuracion.Instancia.Guardar();
		base.Cerrar();
	}
}
