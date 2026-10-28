using Godot;
using System;
using System.Collections.Generic;

// Gestiona las filas existentes del menú de main; no crea otro menú ni cambia su diseño.
public partial class PanelTeclas : Node
{
	private Control _menu;
	private Action<string, Color?> _mostrarAyuda;
	private readonly List<FilaTecla> _filas = new();
	private FilaTecla _esperando;

	public void Vincular(Control menu, Action<string, Color?> mostrarAyuda)
	{
		_menu = menu;
		_mostrarAyuda = mostrarAyuda;
	}

	public void Registrar(FilaTecla fila)
	{
		_filas.Add(fila);
		fila.SetMeta("accion", fila.Accion);
		fila.Pressed += () => EmpezarCaptura(fila);
	}

	private void EmpezarCaptura(FilaTecla fila)
	{
		CancelarCaptura();
		_esperando = fila;
		fila.Esperando = true;
		_mostrarAyuda($"Pulsa una tecla para {Configuracion.NombreAccion(fila.Accion).ToLower()} · ESC cancela", EstiloMenus.Ambar);
	}

	public void CancelarCaptura()
	{
		if (_esperando == null) return;
		_esperando.Esperando = false;
		_esperando.MostrarTecla(Teclas.EtiquetaTeclado(_esperando.Accion));
		_esperando = null;
	}

	public void Refrescar()
	{
		foreach (var fila in _filas) fila.MostrarTecla(Teclas.EtiquetaTeclado(fila.Accion));
	}

	public override void _Input(InputEvent evento)
	{
		if (_esperando == null || !_menu.IsVisibleInTree()) return;
		var fila = _esperando;
		// Solo se aceptan teclas del teclado: el ratón y el control no se pueden asignar (HU-19)
		if (evento is InputEventMouseButton { Pressed: true } clic)
		{
			// El clic izquierdo sigue sirviendo para navegar el menú: cancela la espera y hace su clic normal
			if (clic.ButtonIndex == MouseButton.Left)
			{
				CancelarCaptura();
				return;
			}
			GetViewport().SetInputAsHandled();
			_mostrarAyuda("Los botones y la rueda del mouse no se pueden asignar. Pulsa una tecla del teclado · ESC cancela", EstiloMenus.Coral);
			return;
		}
		if (evento is InputEventJoypadButton { Pressed: true } boton)
		{
			GetViewport().SetInputAsHandled();
			if (boton.IsAction("ui_cancel"))
			{
				CancelarCaptura();
				_mostrarAyuda("Selecciona un control para cambiar su tecla.", null);
				fila.GrabFocus();
				return;
			}
			_mostrarAyuda("Los botones del control no se pueden asignar. Pulsa una tecla del teclado · B cancela", EstiloMenus.Coral);
			return;
		}
		if (evento is not InputEventKey tecla || !tecla.Pressed || tecla.Echo) return;
		GetViewport().SetInputAsHandled();
		if (tecla.Keycode == Key.Escape || tecla.PhysicalKeycode == Key.Escape)
		{
			CancelarCaptura();
			_mostrarAyuda("Selecciona un control para cambiar su tecla.", null);
			fila.GrabFocus();
			return;
		}
		Key fisica = tecla.PhysicalKeycode != Key.None ? tecla.PhysicalKeycode : tecla.Keycode;
		string sistema = Configuracion.ReservadaPorSistema(fisica);
		if (sistema != null)
		{
			_mostrarAyuda($"La tecla {Teclas.NombreTecla(fisica)} está reservada para {sistema}. Elige otra · ESC cancela", EstiloMenus.Coral);
			return;
		}
		string ocupada = Configuracion.Instancia.UsoDeTecla(fisica, fila.Accion);
		if (ocupada != null)
		{
			_mostrarAyuda($"La tecla {Teclas.NombreTecla(fisica)} ya se usa para {ocupada}. Elige otra · ESC cancela", EstiloMenus.Coral);
			return;
		}
		if (!Configuracion.Instancia.AsignarTecla(fila.Accion, fisica)) return;
		CancelarCaptura();
		Refrescar();
		_mostrarAyuda("Selecciona un control para cambiar su tecla.", null);
		fila.GrabFocus();
	}
}
