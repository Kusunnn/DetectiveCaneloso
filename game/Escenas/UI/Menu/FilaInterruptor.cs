using Godot;
using System;

// Interruptor del diseño: nombre (y subtítulo) a la izquierda; a la derecha "Activada" y un
// cuadrado que se rellena de ámbar. Se pulsa con clic, Enter o el botón A del control.
public partial class FilaInterruptor : FilaResaltable
{
	private readonly Label _nombre;
	private readonly Label _estado;
	private readonly Panel _cuadro;
	private readonly StyleBoxFlat _estiloCuadro;

	[Signal] public delegate void CambiadoEventHandler(bool activado);

	public bool Activado { get; private set; }

	public FilaInterruptor() : this("") { }

	public FilaInterruptor(string nombre, string subtitulo = null)
	{
		Desplazamiento = 0f;
		var margen = new MarginContainer();
		margen.AddThemeConstantOverride("margin_top", 12);
		margen.AddThemeConstantOverride("margin_bottom", 12);
		var fila = EstiloMenus.Fila(40);
		margen.AddChild(fila);

		var textos = EstiloMenus.Columna(6);
		textos.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_nombre = EstiloMenus.Etiqueta(nombre, EstiloMenus.Display(800, 42, 0.02f), 42, EstiloMenus.Texto);
		textos.AddChild(_nombre);
		if (!string.IsNullOrEmpty(subtitulo))
			textos.AddChild(EstiloMenus.Etiqueta(subtitulo, EstiloMenus.Mono(22), 22, EstiloMenus.GrisOscuro));
		fila.AddChild(textos);

		var derecha = EstiloMenus.Fila(20);
		derecha.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_estado = EstiloMenus.Etiqueta("", EstiloMenus.Mono(24, 0.04f), 24, EstiloMenus.Gris);
		_estado.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		derecha.AddChild(_estado);
		_estiloCuadro = new StyleBoxFlat();
		_estiloCuadro.SetBorderWidthAll(3);
		_cuadro = new Panel { CustomMinimumSize = new Vector2(28, 28), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
		_cuadro.AddThemeStyleboxOverride("panel", _estiloCuadro);
		derecha.AddChild(_cuadro);
		fila.AddChild(derecha);

		PonerContenido(margen);
		Pressed += () => Fijar(!Activado, avisar: true);
		Fijar(false, avisar: false);
	}

	public void Fijar(bool activado, bool avisar = false)
	{
		Activado = activado;
		_estado.Text = activado ? "Activada" : "Desactivada";
		_estado.AddThemeColorOverride("font_color", activado ? EstiloMenus.Texto : EstiloMenus.Gris);
		_estiloCuadro.BgColor = activado ? EstiloMenus.Ambar : new Color(0, 0, 0, 0);
		_estiloCuadro.BorderColor = activado ? EstiloMenus.Ambar : EstiloMenus.Numero;
		if (avisar) EmitSignal(SignalName.Cambiado, activado);
	}

	protected override void AplicarColores(bool resaltada)
	{
		_nombre.AddThemeColorOverride("font_color", resaltada ? EstiloMenus.Coral : EstiloMenus.Texto);
	}
}
