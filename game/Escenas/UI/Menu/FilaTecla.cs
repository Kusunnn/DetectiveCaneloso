using Godot;
using System;

// Fila de la lista de teclas: "Avanzar ........ [W]". Al pulsarla espera una tecla nueva ("...").
public partial class FilaTecla : FilaResaltable
{
	public string Accion { get; }

	private readonly Label _nombre;
	private readonly Label _tecla;
	private readonly StyleBoxFlat _caja;
	private bool _esperando;

	public FilaTecla() : this("", "") { }

	public FilaTecla(string nombre, string accion)
	{
		Accion = accion;
		Desplazamiento = 12f;
		var margen = new MarginContainer();
		margen.AddThemeConstantOverride("margin_top", 6);
		margen.AddThemeConstantOverride("margin_bottom", 6);
		var fila = EstiloMenus.Fila(32);
		margen.AddChild(fila);
		_nombre = EstiloMenus.Etiqueta(nombre, EstiloMenus.Display(800, 40, 0.02f), 40, EstiloMenus.Texto);
		_nombre.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_nombre.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		fila.AddChild(_nombre);

		_caja = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
		_caja.SetBorderWidthAll(3);
		_caja.ContentMarginLeft = _caja.ContentMarginRight = 18;
		var marco = new PanelContainer { CustomMinimumSize = new Vector2(80, 48), MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
		marco.AddThemeStyleboxOverride("panel", _caja);
		_tecla = EstiloMenus.Etiqueta("", EstiloMenus.Mono(24, 0.08f), 24, EstiloMenus.Texto);
		_tecla.HorizontalAlignment = HorizontalAlignment.Center;
		_tecla.VerticalAlignment = VerticalAlignment.Center;
		marco.AddChild(_tecla);
		fila.AddChild(marco);

		// Línea muy tenue debajo de cada fila
		var columna = EstiloMenus.Columna(0);
		columna.AddChild(margen);
		columna.AddChild(EstiloMenus.Raya(0, 2, EstiloMenus.LineaSuave));
		PonerContenido(columna);
	}

	public void MostrarTecla(string texto) => _tecla.Text = texto;

	public bool Esperando
	{
		get => _esperando;
		set { _esperando = value; if (value) _tecla.Text = "..."; Actualizar(); }
	}

	protected override void AplicarColores(bool resaltada)
	{
		bool activa = resaltada || _esperando;
		_nombre.AddThemeColorOverride("font_color", activa ? EstiloMenus.Coral : EstiloMenus.Texto);
		_caja.BorderColor = _esperando ? EstiloMenus.Ambar : EstiloMenus.Numero;
		_tecla.AddThemeColorOverride("font_color", _esperando ? EstiloMenus.Ambar : EstiloMenus.Texto);
	}
}
