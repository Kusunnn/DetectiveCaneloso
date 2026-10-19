using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Opciones del juego (diseño "03B · Opciones", con pestañas). Se usa en el menú de inicio y en el de pausa.
// Controles: sensibilidad del ratón, agacharse alternando y teclas configurables.
// Pantalla: pantalla completa y ayudas visuales del tutorial. Audio: volumen de cada bus.
// Los cambios se aplican al momento y se guardan al salir (Guardar y volver o Esc).
public partial class MenuOpciones : PanelMenu
{
	private static readonly (string Bus, string Nombre)[] Volumenes =
	{
		("Master", "Volumen general"), ("Musica", "Música"), ("Ambiente", "Ambiente"),
		("Efectos", "Efectos"), ("Interfaz", "Interfaz y diálogos"),
	};

	private static readonly Dictionary<string, string> Ayudas = new()
	{
		["controles"] = "Selecciona un control para cambiar su tecla.",
		["pantalla"] = "Opciones de visualización y ayudas del juego.",
		["audio"] = "Ajusta el volumen de cada canal de sonido.",
	};

	private readonly Dictionary<string, PestanaMenu> _pestanas = new();
	private readonly Dictionary<string, Control> _paneles = new();
	private readonly Dictionary<string, List<Control>> _enfocables = new();
	private string _pestanaActual = "controles";

	private readonly Dictionary<string, HSlider> _volumenes = new();
	private readonly Dictionary<string, Label> _valoresVolumen = new();
	private HSlider _sensibilidad;
	private Label _valorSensibilidad;
	private FilaInterruptor _agacharse;
	private FilaInterruptor _pantallaCompleta;
	private FilaInterruptor _ayudas;
	private readonly List<FilaTecla> _filasTecla = new();
	private FilaTecla _esperando;

	private Label _ayuda;
	private Button _botonRestaurar;
	private Button _botonGuardar;
	private ulong _ultimaMuestra;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Construir();
		base._Ready();
		RehacerFoco();
	}

	// ---------- Construcción ----------

	private void Construir()
	{
		EstiloMenus.Fondo(this);
		var margen = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		margen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		margen.AddThemeConstantOverride("margin_left", 112);
		margen.AddThemeConstantOverride("margin_right", 112);
		margen.AddThemeConstantOverride("margin_top", 72);
		margen.AddThemeConstantOverride("margin_bottom", 56);
		AddChild(margen);
		var columna = EstiloMenus.Columna(36);
		margen.AddChild(columna);

		columna.AddChild(EstiloMenus.Titulo("OPCIONES", 112));

		var barra = EstiloMenus.Columna(0);
		var filaPestanas = EstiloMenus.Fila(80);
		foreach (var (clave, numero, nombre) in new[] { ("controles", "01", "CONTROLES"), ("pantalla", "02", "PANTALLA"), ("audio", "03", "AUDIO") })
		{
			var pestana = new PestanaMenu(numero, nombre);
			pestana.Pressed += () => MostrarPestana(clave, enfocarPestana: true);
			_pestanas[clave] = pestana;
			filaPestanas.AddChild(pestana);
		}
		barra.AddChild(filaPestanas);
		barra.AddChild(EstiloMenus.Raya(0, 2, new Color(1, 1, 1, 0.1f)));
		columna.AddChild(barra);

		var contenido = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
		columna.AddChild(contenido);
		_paneles["controles"] = PanelControles();
		_paneles["pantalla"] = PanelPantalla();
		_paneles["audio"] = PanelAudio();
		foreach (var panel in _paneles.Values)
		{
			panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			contenido.AddChild(panel);
		}

		var pie = EstiloMenus.Columna(28);
		pie.AddChild(EstiloMenus.Raya(0, 2, EstiloMenus.Linea));
		var filaPie = EstiloMenus.Fila(48);
		_ayuda = EstiloMenus.Etiqueta("", EstiloMenus.Mono(22, 0.04f), 22, EstiloMenus.Gris);
		_ayuda.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_ayuda.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_ayuda.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		filaPie.AddChild(_ayuda);
		_botonRestaurar = EstiloMenus.BotonTexto("Restaurar predeterminados", 36, 0.02f);
		_botonGuardar = EstiloMenus.BotonPrincipal("Guardar y volver", 36, 0.02f);
		_botonRestaurar.Pressed += Restaurar;
		_botonGuardar.Pressed += Cerrar;
		filaPie.AddChild(_botonRestaurar);
		filaPie.AddChild(_botonGuardar);
		pie.AddChild(filaPie);
		columna.AddChild(pie);
	}

	private Control PanelControles()
	{
		var columnas = EstiloMenus.Fila(96);
		var enfocables = new List<Control>();

		var izquierda = EstiloMenus.Columna(28);
		izquierda.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		izquierda.AddChild(EstiloMenus.Seccion("A", "MOUSE Y MOVIMIENTO"));
		var bloque = EstiloMenus.Columna(16);
		var cabecera = EstiloMenus.Fila(16);
		var nombre = EstiloMenus.Etiqueta("Sensibilidad del mouse", EstiloMenus.Display(800, 42, 0.02f), 42, EstiloMenus.Texto);
		nombre.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		cabecera.AddChild(nombre);
		_valorSensibilidad = EstiloMenus.Etiqueta("", EstiloMenus.Mono(26), 26, EstiloMenus.GrisClaro);
		_valorSensibilidad.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		cabecera.AddChild(_valorSensibilidad);
		bloque.AddChild(cabecera);
		_sensibilidad = EstiloMenus.Deslizador(0.3, 2.0, 0.05);
		_sensibilidad.ValueChanged += valor =>
		{
			Configuracion.Instancia.SensibilidadRaton = (float)valor;
			_valorSensibilidad.Text = TextoSensibilidad(valor);
		};
		bloque.AddChild(_sensibilidad);
		izquierda.AddChild(bloque);
		enfocables.Add(_sensibilidad);
		_agacharse = new FilaInterruptor("Agacharse: alternar", "Cada pulsación cambia entre agachado y de pie");
		_agacharse.Cambiado += activado => Configuracion.Instancia.AgacharseAlternar = activado;
		izquierda.AddChild(_agacharse);
		enfocables.Add(_agacharse);
		columnas.AddChild(izquierda);

		var derecha = EstiloMenus.Columna(8);
		derecha.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		derecha.AddChild(EstiloMenus.Seccion("B", "TECLAS"));
		var desplazable = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			FollowFocus = true,
		};
		var lista = EstiloMenus.Columna(0);
		lista.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		desplazable.AddChild(lista);
		foreach (var (texto, accion) in Configuracion.TeclasConfigurables)
		{
			var fila = new FilaTecla(texto, accion) { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			fila.Pressed += () => EmpezarCaptura(fila);
			_filasTecla.Add(fila);
			lista.AddChild(fila);
			enfocables.Add(fila);
		}
		derecha.AddChild(desplazable);
		columnas.AddChild(derecha);

		_enfocables["controles"] = enfocables;
		return columnas;
	}

	private Control PanelPantalla()
	{
		var lista = EstiloMenus.Columna(20);
		lista.CustomMinimumSize = new Vector2(1240, 0);
		lista.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		_pantallaCompleta = new FilaInterruptor("Pantalla completa", "Atajo: F11 o Alt + Enter");
		_pantallaCompleta.Cambiado += activado => Configuracion.Instancia.PantallaCompleta = activado;
		_ayudas = new FilaInterruptor("Ayudas visuales del tutorial", "Muestra indicaciones en pantalla durante el caso 00");
		_ayudas.Cambiado += activado => Configuracion.Instancia.AyudasVisuales = activado;
		lista.AddChild(_pantallaCompleta);
		lista.AddChild(_ayudas);
		_enfocables["pantalla"] = new List<Control> { _pantallaCompleta, _ayudas };
		var contenedor = new Control { MouseFilter = MouseFilterEnum.Ignore };
		contenedor.AddChild(lista);
		return contenedor;
	}

	private Control PanelAudio()
	{
		var lista = EstiloMenus.Columna(12);
		lista.CustomMinimumSize = new Vector2(1240, 0);
		var enfocables = new List<Control>();
		foreach (var (bus, nombre) in Volumenes)
		{
			var fila = EstiloMenus.Fila(40);
			var etiqueta = EstiloMenus.Etiqueta(nombre, EstiloMenus.Display(800, 42, 0.02f), 42, EstiloMenus.Texto);
			etiqueta.CustomMinimumSize = new Vector2(400, 0);
			fila.AddChild(etiqueta);
			var deslizador = EstiloMenus.Deslizador(0, 100, 1);
			fila.AddChild(deslizador);
			var valor = EstiloMenus.Etiqueta("", EstiloMenus.Mono(26), 26, EstiloMenus.GrisClaro);
			valor.CustomMinimumSize = new Vector2(80, 0);
			valor.HorizontalAlignment = HorizontalAlignment.Right;
			valor.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			fila.AddChild(valor);
			deslizador.ValueChanged += v =>
			{
				Configuracion.Instancia.FijarVolumen(bus, (float)v / 100f);
				valor.Text = ((int)v).ToString();
				// Muestra de cómo suena (como mucho una cada 0.15 s mientras se arrastra)
				ulong ahora = Time.GetTicksMsec();
				if (ahora - _ultimaMuestra > 150 && (bus == "Efectos" || bus == "Interfaz" || bus == "Master"))
				{
					_ultimaMuestra = ahora;
					Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic);
				}
			};
			_volumenes[bus] = deslizador;
			_valoresVolumen[bus] = valor;
			enfocables.Add(deslizador);
			lista.AddChild(fila);
		}
		_enfocables["audio"] = enfocables;
		var contenedor = new Control { MouseFilter = MouseFilterEnum.Ignore };
		contenedor.AddChild(lista);
		return contenedor;
	}

	private static string TextoSensibilidad(double valor) => Mathf.RoundToInt(valor * 100) + " %";

	// ---------- Pestañas y foco ----------

	private void MostrarPestana(string clave, bool enfocarPestana = false)
	{
		CancelarCaptura();
		_pestanaActual = clave;
		foreach (var (k, pestana) in _pestanas) pestana.Activa = k == clave;
		foreach (var (k, panel) in _paneles) panel.Visible = k == clave;
		MostrarAyuda(Ayudas[clave]);
		RehacerFoco();
		if (enfocarPestana) _pestanas[clave].GrabFocus();
	}

	// Pestañas: izquierda/derecha. Bajando: los controles de la pestaña y luego el pie. Con vuelta arriba.
	private void RehacerFoco()
	{
		var pestanas = _pestanas.Values.Cast<Control>().ToList();
		var controles = _enfocables[_pestanaActual];
		var pie = new List<Control> { _botonRestaurar, _botonGuardar };
		Control primero = controles.Count > 0 ? controles[0] : _botonRestaurar;
		Control ultimo = controles.Count > 0 ? controles[^1] : _pestanas[_pestanaActual];

		for (int i = 0; i < pestanas.Count; i++)
		{
			var p = pestanas[i];
			p.FocusNeighborLeft = p.GetPathTo(pestanas[(i - 1 + pestanas.Count) % pestanas.Count]);
			p.FocusNeighborRight = p.GetPathTo(pestanas[(i + 1) % pestanas.Count]);
			p.FocusNeighborTop = p.GetPathTo(_botonGuardar);
			p.FocusNeighborBottom = p.GetPathTo(primero);
			p.FocusNext = p.GetPathTo(primero);
			p.FocusPrevious = p.GetPathTo(_botonGuardar);
		}
		for (int i = 0; i < controles.Count; i++)
		{
			var c = controles[i];
			Control arriba = i == 0 ? _pestanas[_pestanaActual] : controles[i - 1];
			Control abajo = i == controles.Count - 1 ? _botonRestaurar : controles[i + 1];
			c.FocusNeighborTop = c.GetPathTo(arriba);
			c.FocusNeighborBottom = c.GetPathTo(abajo);
			c.FocusPrevious = c.GetPathTo(arriba);
			c.FocusNext = c.GetPathTo(abajo);
			if (c is not Godot.Range)
			{
				c.FocusNeighborLeft = ".";
				c.FocusNeighborRight = ".";
			}
		}
		foreach (var b in pie)
		{
			b.FocusNeighborTop = b.GetPathTo(ultimo);
			b.FocusNeighborBottom = b.GetPathTo(_pestanas[_pestanaActual]);
		}
		_botonRestaurar.FocusNeighborLeft = _botonRestaurar.GetPathTo(_botonGuardar);
		_botonRestaurar.FocusNeighborRight = _botonRestaurar.GetPathTo(_botonGuardar);
		_botonGuardar.FocusNeighborLeft = _botonGuardar.GetPathTo(_botonRestaurar);
		_botonGuardar.FocusNeighborRight = _botonGuardar.GetPathTo(_botonRestaurar);
		_botonRestaurar.FocusPrevious = _botonRestaurar.GetPathTo(ultimo);
		_botonRestaurar.FocusNext = _botonRestaurar.GetPathTo(_botonGuardar);
		_botonGuardar.FocusPrevious = _botonGuardar.GetPathTo(_botonRestaurar);
		_botonGuardar.FocusNext = _botonGuardar.GetPathTo(_pestanas[_pestanaActual]);
	}

	private void MostrarAyuda(string texto, Color? color = null)
	{
		_ayuda.Text = texto;
		_ayuda.AddThemeColorOverride("font_color", color ?? EstiloMenus.Gris);
	}

	// ---------- Valores ----------

	private void CargarValores()
	{
		var config = Configuracion.Instancia;
		foreach (var (bus, deslizador) in _volumenes)
		{
			int valor = Mathf.RoundToInt(config.Volumen(bus) * 100f);
			deslizador.SetValueNoSignal(valor);
			_valoresVolumen[bus].Text = valor.ToString();
		}
		_sensibilidad.SetValueNoSignal(config.SensibilidadRaton);
		_valorSensibilidad.Text = TextoSensibilidad(config.SensibilidadRaton);
		_agacharse.Fijar(config.AgacharseAlternar);
		_pantallaCompleta.Fijar(config.PantallaCompleta);
		_ayudas.Fijar(config.AyudasVisuales);
		foreach (var fila in _filasTecla) fila.MostrarTecla(Teclas.EtiquetaTeclado(fila.Accion));
	}

	private void Restaurar()
	{
		CancelarCaptura();
		Configuracion.Instancia.RestaurarPredeterminados();
		CargarValores();
		MostrarAyuda("Se restauraron los valores predeterminados: las teclas volvieron a los controles originales.", EstiloMenus.Ambar);
	}

	// ---------- Cambio de teclas ----------

	private void EmpezarCaptura(FilaTecla fila)
	{
		CancelarCaptura();
		_esperando = fila;
		fila.Esperando = true;
		MostrarAyuda($"Pulsa una tecla para {Configuracion.NombreAccion(fila.Accion).ToLower()} · ESC cancela", EstiloMenus.Ambar);
	}

	private void CancelarCaptura()
	{
		if (_esperando == null) return;
		_esperando.Esperando = false;
		_esperando.MostrarTecla(Teclas.EtiquetaTeclado(_esperando.Accion));
		_esperando = null;
		if (_ayuda != null) MostrarAyuda(Ayudas[_pestanaActual]);
	}

	// Mientras se espera una tecla, esa pulsación no llega al resto del juego ni cierra el menú.
	// Solo se aceptan teclas del teclado: el ratón y el control no se pueden asignar (HU-19).
	public override void _Input(InputEvent evento)
	{
		if (_esperando == null || !Visible) return;
		var fila = _esperando;
		if (evento is InputEventMouseButton { Pressed: true } clic)
		{
			// El clic izquierdo sigue sirviendo para navegar el menú: cancela la espera y hace su clic normal
			if (clic.ButtonIndex == MouseButton.Left)
			{
				CancelarCaptura();
				return;
			}
			GetViewport().SetInputAsHandled();
			MostrarAyuda("Los botones y la rueda del mouse no se pueden asignar. Pulsa una tecla del teclado · ESC cancela", EstiloMenus.Coral);
			return;
		}
		if (evento is InputEventJoypadButton { Pressed: true } boton)
		{
			GetViewport().SetInputAsHandled();
			if (boton.IsAction("ui_cancel"))
			{
				CancelarCaptura();
				fila.GrabFocus();
				return;
			}
			MostrarAyuda("Los botones del control no se pueden asignar. Pulsa una tecla del teclado · B cancela", EstiloMenus.Coral);
			return;
		}
		if (evento is not InputEventKey tecla || !tecla.Pressed || tecla.Echo) return;
		GetViewport().SetInputAsHandled();

		// ESC queda reservada para pausar y cancelar: nunca se asigna a una acción del juego
		if (tecla.Keycode == Key.Escape || tecla.PhysicalKeycode == Key.Escape)
		{
			CancelarCaptura();
			fila.GrabFocus();
			return;
		}
		Key fisica = tecla.PhysicalKeycode != Key.None ? tecla.PhysicalKeycode : tecla.Keycode;
		string sistema = Configuracion.ReservadaPorSistema(fisica);
		if (sistema != null)
		{
			MostrarAyuda($"La tecla {Teclas.NombreTecla(fisica)} está reservada para {sistema}. Elige otra · ESC cancela", EstiloMenus.Coral);
			return;
		}
		string ocupada = Configuracion.Instancia.UsoDeTecla(fisica, fila.Accion);
		if (ocupada != null)
		{
			MostrarAyuda($"La tecla {Teclas.NombreTecla(fisica)} ya está asignada a {ocupada}. Elige otra · ESC cancela", EstiloMenus.Coral);
			return;
		}
		if (!Configuracion.Instancia.AsignarTecla(fila.Accion, fisica)) return;
		_esperando = null;
		fila.Esperando = false;
		fila.MostrarTecla(Teclas.EtiquetaTeclado(fila.Accion));
		MostrarAyuda($"{Configuracion.NombreAccion(fila.Accion)} ahora usa {Teclas.NombreTecla(fisica)}. Cambio guardado.", EstiloMenus.Ambar);
		fila.GrabFocus();
	}

	// Avisa si alguna acción se quedó sin tecla; true si falta alguna
	private bool AvisarTeclasFaltantes()
	{
		var faltan = Configuracion.AccionesSinTecla();
		if (faltan.Count == 0) return false;
		MostrarPestana("controles");
		MostrarAyuda($"Falta asignar una tecla a: {string.Join(", ", faltan)}. Selecciónala y pulsa una tecla, o restaura los predeterminados.", EstiloMenus.Coral);
		return true;
	}

	// ---------- Abrir y cerrar ----------

	public override void Abrir()
	{
		CargarValores();
		MostrarPestana(_pestanaActual);
		AvisarTeclasFaltantes();
		base.Abrir();
		_pestanas[_pestanaActual].GrabFocus();
	}

	public override void Cerrar()
	{
		CancelarCaptura();
		// Sin tecla, esa acción no se podría usar en el juego: no se sale hasta asignarla
		if (AvisarTeclasFaltantes()) return;
		Configuracion.Instancia.Guardar();
		base.Cerrar();
	}
}
