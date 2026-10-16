using Godot;
using System;

// Pestaña de Opciones: "01  CONTROLES". La activa va en coral con raya roja debajo.
public partial class PestanaMenu : FilaResaltable
{
	private readonly Label _texto;
	private readonly ColorRect _raya;
	private bool _activa;

	public bool Activa
	{
		get => _activa;
		set { _activa = value; Actualizar(); }
	}

	public PestanaMenu() : this("", "") { }

	public PestanaMenu(string numero, string texto)
	{
		Desplazamiento = 0f;
		var columna = EstiloMenus.Columna(0);
		var margen = new MarginContainer();
		margen.AddThemeConstantOverride("margin_top", 20);
		margen.AddThemeConstantOverride("margin_bottom", 20);
		var fila = EstiloMenus.Fila(24);
		var num = EstiloMenus.Etiqueta(numero, EstiloMenus.Mono(22, 0.1f), 22, EstiloMenus.Numero);
		num.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		fila.AddChild(num);
		_texto = EstiloMenus.Etiqueta(texto, EstiloMenus.Display(800, 48, 0.08f), 48, EstiloMenus.Texto);
		fila.AddChild(_texto);
		margen.AddChild(fila);
		columna.AddChild(margen);
		_raya = EstiloMenus.Raya(0, 4, EstiloMenus.Rojo);
		columna.AddChild(_raya);
		PonerContenido(columna);
	}

	protected override void AplicarColores(bool resaltada)
	{
		_texto.AddThemeColorOverride("font_color", _activa || resaltada ? EstiloMenus.Coral : EstiloMenus.Texto);
		_raya.Color = _activa ? EstiloMenus.Rojo : new Color(0, 0, 0, 0);
	}
}
