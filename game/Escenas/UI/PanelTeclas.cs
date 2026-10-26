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
		if (evento is not InputEventKey tecla || !tecla.Pressed || tecla.Echo) return;
		GetViewport().SetInputAsHandled();
		var fila = _esperando;
		if (tecla.Keycode == Key.Escape)
		{
			CancelarCaptura();
			_mostrarAyuda("Selecciona un control para cambiar su tecla.", null);
			fila.GrabFocus();
			return;
		}
		Key fisica = tecla.PhysicalKeycode != Key.None ? tecla.PhysicalKeycode : tecla.Keycode;
		string ocupada = Configuracion.Instancia.UsoDeTecla(fisica, fila.Accion);
		if (ocupada != null)
		{
			_mostrarAyuda($"La tecla {Teclas.NombreTecla(fisica)} ya se usa para {ocupada}. Elige otra · ESC cancela", EstiloMenus.Coral);
			return;
		}
		Configuracion.Instancia.AsignarTecla(fila.Accion, fisica);
		CancelarCaptura();
		Refrescar();
		_mostrarAyuda("Selecciona un control para cambiar su tecla.", null);
		fila.GrabFocus();
	}
}
