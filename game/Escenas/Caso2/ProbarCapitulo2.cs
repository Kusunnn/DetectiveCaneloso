using Godot;
using System;

// Acceso de prueba del Capítulo 2: abre cualquier escenario sin jugar el Caso 0 ni el Caso 1.
// Desde el editor: abre probar_capitulo2.tscn y pulsa F6. Desde la consola:
//   <Godot>.exe --path game res://Escenas/Caso2/probar_capitulo2.tscn [-- despacho] [-- --estado=hackeado]
// No toca los menús del juego ni capitulos.json.
public partial class ProbarCapitulo2 : Control
{
	private static readonly (string Clave, string Nombre, string Ruta)[] Escenas =
	{
		("comisaria", "Comisaría por la mañana", "res://Escenas/Caso2/comisaria_manana.tscn"),
		("lobby", "Torre Miawrio · Lobby", "res://Escenas/Caso2/torre_lobby.tscn"),
		("despacho", "Torre Miawrio · Despacho del piso 30", "res://Escenas/Caso2/torre_despacho.tscn"),
		("escaleras", "Escaleras de emergencia", "res://Escenas/Caso2/torre_escaleras.tscn"),
		("piso23", "Piso 23 · Cuarto del servidor", "res://Escenas/Caso2/torre_piso23.tscn"),
		("cocina", "Cocina del edificio", "res://Escenas/Caso2/torre_cocina.tscn"),
		("basura", "Cuarto de basura", "res://Escenas/Caso2/torre_basura.tscn"),
		("estacionamiento", "Estacionamiento", "res://Escenas/Caso2/torre_estacionamiento.tscn"),
		("fachada", "Afuera de la torre (noche)", "res://Escenas/Caso2/torre_fachada_noche.tscn"),
		("celda", "Penal · Celda de Tony", "res://Escenas/Caso2/penal_celda.tscn"),
		("embarcadero", "Embarcadero sellado", "res://Escenas/Caso2/embarcadero_sellado.tscn"),
	};

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		foreach (string arg in OS.GetCmdlineUserArgs())
			foreach (var e in Escenas)
				if (arg == e.Clave)
				{
					CallDeferred(MethodName.Abrir, e.Ruta);
					return;
				}

		SetAnchorsPreset(LayoutPreset.FullRect);
		var fondo = new ColorRect { Color = new Color(0.1f, 0.08f, 0.07f) };
		fondo.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(fondo);
		var lista = new VBoxContainer();
		lista.SetAnchorsPreset(LayoutPreset.Center);
		lista.Position = new Vector2(-260, -330);
		lista.CustomMinimumSize = new Vector2(520, 0);
		lista.AddThemeConstantOverride("separation", 8);
		AddChild(lista);
		var titulo = new Label { Text = "Capítulo 2 · Probar escenarios", HorizontalAlignment = HorizontalAlignment.Center };
		titulo.AddThemeFontSizeOverride("font_size", 30);
		lista.AddChild(titulo);
		foreach (var e in Escenas)
		{
			var ruta = e.Ruta;
			var boton = new Button { Text = e.Nombre, CustomMinimumSize = new Vector2(0, 44) };
			boton.Pressed += () => Abrir(ruta);
			lista.AddChild(boton);
		}
	}

	private void Abrir(string ruta) => GetTree().ChangeSceneToFile(ruta);
}
