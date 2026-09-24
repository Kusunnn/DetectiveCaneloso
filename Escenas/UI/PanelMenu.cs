using Godot;
using System;
using System.Linq;

// Base para pantallas secundarias de menú (opciones, créditos, códice...).
// Se abre con Abrir(), se cierra con el botón %BotonVolver o con Esc (ui_cancel).
public partial class PanelMenu : Control
{
	[Signal]
	public delegate void CerradoEventHandler();

	// Control que recibe el foco al abrir, para poder navegar con teclado.
	[Export]
	public Control FocoInicial { get; set; }

	public override void _Ready()
	{
		Hide();
		var botonVolver = GetNodeOrNull<BaseButton>("%BotonVolver");
		if (botonVolver != null)
		{
			botonVolver.Pressed += Cerrar;
		}
		UtilidadesUI.ConectarFocoConRaton(this);
		UtilidadesUI.EncadenarFoco(FindChildren("*", "Control", true, false)
			.Where(nodo => nodo is BaseButton or Godot.Range or RichTextLabel)
			.Cast<Control>());
	}

	public virtual void Abrir()
	{
		Show();
		FocoInicial?.GrabFocus();
	}

	public virtual void Cerrar()
	{
		Hide();
		EmitSignal(SignalName.Cerrado);
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (Visible && evento.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			Cerrar();
		}
	}
}
