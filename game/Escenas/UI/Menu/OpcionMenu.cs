using Godot;
using System;

// Opción numerada de un menú: "01  REANUDAR". Se pone coral y se desliza al seleccionarla.
public partial class OpcionMenu : FilaResaltable
{
	private readonly Label _texto;

	public OpcionMenu() : this("", "") { }

	public OpcionMenu(string numero, string texto, int tam = 60, float espaciadoEm = 0.08f)
	{
		var fila = EstiloMenus.Fila(36);
		var num = EstiloMenus.Etiqueta(numero, EstiloMenus.Mono(22, 0.1f), 22, EstiloMenus.Numero);
		num.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		fila.AddChild(num);
		_texto = EstiloMenus.Etiqueta(texto, EstiloMenus.Display(800, tam, espaciadoEm), tam, EstiloMenus.Texto);
		fila.AddChild(_texto);
		// 6 px de aire arriba y abajo en el diseño
		var margen = new MarginContainer();
		margen.AddThemeConstantOverride("margin_top", 12);
		margen.AddThemeConstantOverride("margin_bottom", 12);
		margen.AddChild(fila);
		PonerContenido(margen);
	}

	protected override void AplicarColores(bool resaltada)
	{
		_texto.AddThemeColorOverride("font_color", resaltada ? EstiloMenus.Coral : EstiloMenus.Texto);
		Modulate = new Color(1, 1, 1, Disabled ? 0.35f : 1f);
	}
}
