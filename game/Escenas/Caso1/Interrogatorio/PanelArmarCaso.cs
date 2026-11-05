using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// «Armar el caso», como la deducción de Return of the Obra Dinn: se elige un sospechoso y se llenan tres casillas con pruebas del
// expediente. Nada se valida hasta presentarlo completo: entonces Hecdog revisa casilla por casilla y aparece cada sello.
public partial class PanelArmarCaso : CanvasLayer
{
	private static readonly Color Papel = new(0.84f, 0.81f, 0.72f), Tinta = new("20272b"), Estambre = new(0.7f, 0.05f, 0.05f);
	private static readonly Color Verde = new(0.1f, 0.42f, 0.16f), Rojo = new(0.68f, 0.1f, 0.08f);
	private static readonly (string Id, string Nombre, string Retrato)[] Sospechosos =
	{
		("juan_carlos", "Juan Carlos", "juan carlos"), ("aya", "Aya", "aya"), ("miawrio", "Miawrio", "miawrio"),
		("eriz", "Eriz", "eriz"), ("mimita", "Mimita", "mimita"), ("tony", "Tony", "tony"),
	};
	private const int PruebasPorCasilla = 4;

	private GuionCaso1 _guion;
	private VentanaDialogo _dialogo;
	private Font _mono, _titulos;
	private TaskCompletionSource<bool> _espera;
	private int _casillaActiva;
	private Control _raiz, _ayuda;
	private readonly Dictionary<string, Button> _botonesSospechoso = new();
	private readonly List<PanelContainer> _casillas = new();
	private readonly List<HFlowContainer> _fichas = new();
	private readonly List<Label> _sellos = new();
	private VBoxContainer _lista;
	private Button _presentar, _salir;
	private Control _hilos;

	public static PanelArmarCaso Crear(Node padre, GuionCaso1 guion, VentanaDialogo dialogo)
	{
		var panel = new PanelArmarCaso { Name = "PanelArmarCaso", _guion = guion, _dialogo = dialogo, Layer = 18, Visible = false };
		padre.AddChild(panel);
		return panel;
	}

	public override void _Ready()
	{
		_mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var display = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf") };
		display.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };
		display.SpacingGlyph = 2;
		_titulos = display;
		Construir();
	}

	public async Task<bool> Abrir()
	{
		Visible = true;
		_raiz.Modulate = Colors.White;
		foreach (var sello in _sellos) sello.Visible = false;
		_presentar.Disabled = _salir.Disabled = false;
		_dialogo.Visible = false;
		((Control)_raiz).OffsetBottom = -26;
		foreach (var b in _botonesSospechoso.Values) b.CustomMinimumSize = new Vector2(240, 96);
		var raton = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Refrescar();
		_espera = new TaskCompletionSource<bool>();
		bool presento = await _espera.Task;
		_espera = null;
		_dialogo.Visible = true;
		_presentar.Disabled = _salir.Disabled = true;
		// Hecdog revisa abajo, sin tapar las casillas
		((Control)_raiz).OffsetBottom = presento ? -270 : -26;
		foreach (var b in _botonesSospechoso.Values) b.CustomMinimumSize = new Vector2(240, presento ? 54 : 96);
		Input.MouseMode = raton;
		if (!presento) Cerrar();
		return presento;
	}

	public void Cerrar() => Visible = false;

	public void Sello(int casilla, bool bien)
	{
		var sello = _sellos[casilla];
		sello.Text = bien ? "ENCAJA" : "NO ENCAJA";
		sello.AddThemeColorOverride("font_color", bien ? Verde : Rojo);
		sello.Visible = true;
		Sonidos.ReproducirUI(this, bien ? Sonidos.Tipo.Sello : Sonidos.Tipo.Error);
	}

	// Lo usan las pruebas: llenar el caso como lo haría el jugador
	public void Pruebas_Sospechoso(string id) { _guion.CasoSospechoso = id; Refrescar(); }
	public void Pruebas_Agregar(int casilla, string id) { _casillaActiva = casilla; Agregar(id); }
	public void Pruebas_Presentar() => _espera?.TrySetResult(true);
	public void Pruebas_Salir() => _espera?.TrySetResult(false);
	public bool EsperandoJugador => _espera != null;

	public override void _Input(InputEvent evento)
	{
		if (!Visible || _espera == null) return;
		if (evento.IsActionPressed("ui_cancel") || evento.IsActionPressed("pausa"))
		{
			GetViewport().SetInputAsHandled();
			if (_ayuda.Visible) _ayuda.Visible = false;
			else _espera.TrySetResult(false);
		}
	}

	private Label Texto(string texto, Font fuente, int tam, Color color)
	{
		var l = new Label { Text = texto, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		l.AddThemeFontOverride("font", fuente);
		l.AddThemeFontSizeOverride("font_size", tam);
		l.AddThemeColorOverride("font_color", color);
		return l;
	}

	private Button Boton(string texto, int tam = 20)
	{
		var b = new Button { Text = texto, Alignment = HorizontalAlignment.Left, ClipText = false };
		b.AddThemeFontOverride("font", _mono);
		b.AddThemeFontSizeOverride("font_size", tam);
		var normal = new StyleBoxFlat { BgColor = new Color(0.95f, 0.93f, 0.86f) };
		normal.SetContentMarginAll(6);
		normal.SetBorderWidthAll(1);
		normal.BorderColor = new Color(Tinta, 0.4f);
		var foco = (StyleBoxFlat)normal.Duplicate();
		foco.BgColor = new Color("efca08");
		b.AddThemeStyleboxOverride("normal", normal);
		b.AddThemeStyleboxOverride("hover", foco);
		b.AddThemeStyleboxOverride("focus", foco);
		b.AddThemeStyleboxOverride("pressed", foco);
		b.AddThemeColorOverride("font_color", Tinta);
		b.AddThemeColorOverride("font_hover_color", Tinta);
		b.AddThemeColorOverride("font_focus_color", Tinta);
		b.AddThemeColorOverride("font_pressed_color", Tinta);
		b.Pressed += () => Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic);
		return b;
	}

	private void Construir()
	{
		var fondo = new ColorRect { Color = new Color(0, 0, 0, 0.72f), MouseFilter = Control.MouseFilterEnum.Stop };
		fondo.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(fondo);
		var estilo = new StyleBoxFlat { BgColor = Papel, ShadowColor = new Color(0, 0, 0, 0.5f), ShadowSize = 14 };
		estilo.SetContentMarginAll(22);
		var hoja = new PanelContainer();
		hoja.AddThemeStyleboxOverride("panel", estilo);
		hoja.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		hoja.OffsetLeft = 50; hoja.OffsetRight = -50; hoja.OffsetTop = 26; hoja.OffsetBottom = -26;
		AddChild(hoja);
		_raiz = hoja;

		var columnas = new VBoxContainer();
		columnas.AddThemeConstantOverride("separation", 10);
		hoja.AddChild(columnas);
		var cabecera = new HBoxContainer();
		columnas.AddChild(cabecera);
		var titulo = Texto("ARMAR EL CASO", _titulos, 42, Tinta);
		titulo.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		cabecera.AddChild(titulo);
		var boton_ayuda = Boton(" ? ", 24);
		boton_ayuda.Pressed += () => _ayuda.Visible = !_ayuda.Visible;
		cabecera.AddChild(boton_ayuda);

		var cuerpo = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		cuerpo.AddThemeConstantOverride("separation", 18);
		columnas.AddChild(cuerpo);

		// Los seis, Tony incluido
		var sospechosos = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0) };
		sospechosos.AddThemeConstantOverride("separation", 6);
		cuerpo.AddChild(sospechosos);
		sospechosos.AddChild(Texto("¿QUIÉN FUE?", _titulos, 24, Tinta));
		foreach (var (id, nombre, retrato) in Sospechosos)
		{
			var b = Boton(nombre, 22);
			b.Icon = _dialogo.Retrato(retrato);
			b.ExpandIcon = true;
			b.CustomMinimumSize = new Vector2(240, 96);
			b.AddThemeConstantOverride("icon_max_width", 84);
			string quien = id;
			b.Pressed += () => { _guion.CasoSospechoso = quien; Refrescar(); };
			sospechosos.AddChild(b);
			_botonesSospechoso[id] = b;
		}

		// Las tres casillas
		var centro = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		centro.AddThemeConstantOverride("separation", 10);
		cuerpo.AddChild(centro);
		for (int i = 0; i < GuionCaso1.Casillas.Length; i++)
		{
			int indice = i;
			var caja = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
			var interior = new VBoxContainer();
			interior.AddThemeConstantOverride("separation", 6);
			caja.AddChild(interior);
			var fila = new HBoxContainer();
			interior.AddChild(fila);
			var elegir = Boton(GuionCaso1.Casillas[i].Nombre, 24);
			elegir.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			elegir.Pressed += () => { _casillaActiva = indice; Refrescar(); };
			fila.AddChild(elegir);
			var sello = Texto("", _titulos, 30, Rojo);
			sello.AutowrapMode = TextServer.AutowrapMode.Off;
			sello.Visible = false;
			sello.Rotation = -0.06f;
			fila.AddChild(sello);
			_sellos.Add(sello);
			var fichas = new HFlowContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
			fichas.AddThemeConstantOverride("h_separation", 6);
			fichas.AddThemeConstantOverride("v_separation", 6);
			interior.AddChild(fichas);
			_fichas.Add(fichas);
			_casillas.Add(caja);
			centro.AddChild(caja);
		}

		// Lo que tiene en el expediente
		var derecha = new VBoxContainer { CustomMinimumSize = new Vector2(430, 0) };
		derecha.AddThemeConstantOverride("separation", 6);
		cuerpo.AddChild(derecha);
		derecha.AddChild(Texto("EXPEDIENTE", _titulos, 24, Tinta));
		var marco = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		derecha.AddChild(marco);
		_lista = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_lista.AddThemeConstantOverride("separation", 4);
		marco.AddChild(_lista);

		var pie = new HBoxContainer();
		pie.AddThemeConstantOverride("separation", 14);
		columnas.AddChild(pie);
		_salir = Boton("Seguir investigando", 22);
		_salir.Pressed += () => _espera?.TrySetResult(false);
		pie.AddChild(_salir);
		var hueco = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		pie.AddChild(hueco);
		_presentar = Boton("Presentar el caso", 22);
		_presentar.Pressed += () => _espera?.TrySetResult(true);
		pie.AddChild(_presentar);

		// El estambre: del sospechoso a cada casilla
		_hilos = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_hilos.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_hilos);

		// [?]
		var ayuda = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
		ayuda.AddThemeStyleboxOverride("panel", estilo);
		ayuda.SetAnchorsPreset(Control.LayoutPreset.Center);
		ayuda.GrowHorizontal = Control.GrowDirection.Both;
		ayuda.GrowVertical = Control.GrowDirection.Both;
		ayuda.CustomMinimumSize = new Vector2(760, 0);
		ayuda.Visible = false;
		ayuda.AddChild(Texto("Elige a quién acusas y llena las tres casillas con pruebas del expediente.\n\nOPORTUNIDAD: pudo estar ahí a la hora del crimen.\nMEDIO: tuvo con qué hacerlo.\nMOTIVO: tuvo por qué hacerlo.\n\nHecdog lo revisa todo al final. [?] cierra esta nota.", _mono, 24, Tinta));
		AddChild(ayuda);
		_ayuda = ayuda;
	}

	private void Refrescar()
	{
		foreach (var (id, boton) in _botonesSospechoso)
			boton.Modulate = id == _guion.CasoSospechoso ? new Color(1f, 0.92f, 0.5f) : Colors.White;
		for (int i = 0; i < _casillas.Count; i++)
		{
			var estilo = new StyleBoxFlat { BgColor = new Color(0.95f, 0.93f, 0.86f, 0.55f), BorderColor = i == _casillaActiva ? Estambre : new Color(Tinta, 0.35f) };
			estilo.SetBorderWidthAll(i == _casillaActiva ? 3 : 1);
			estilo.SetContentMarginAll(8);
			_casillas[i].AddThemeStyleboxOverride("panel", estilo);
			foreach (Node n in _fichas[i].GetChildren()) { _fichas[i].RemoveChild(n); n.QueueFree(); }
			foreach (string id in _guion.CasoPruebas[i])
			{
				string prueba = id;
				int casilla = i;
				var ficha = Boton(TituloDe(id) + "  ✕", 18);
				ficha.Pressed += () => { _guion.CasoPruebas[casilla].Remove(prueba); Refrescar(); };
				_fichas[i].AddChild(ficha);
			}
		}
		foreach (Node n in _lista.GetChildren()) { _lista.RemoveChild(n); n.QueueFree(); }
		foreach (var (id, titulo, _) in _guion.PruebasDisponibles())
		{
			if (_guion.CasoPruebas.Any(c => c.Contains(id))) continue;
			string prueba = id;
			var b = Boton(titulo, 19);
			b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			b.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			b.Pressed += () => Agregar(prueba);
			_lista.AddChild(b);
		}
		CallDeferred(nameof(DibujarHilos));
	}

	private string TituloDe(string id) => _guion.PruebasDisponibles().FirstOrDefault(p => p.Id == id).Titulo ?? id;

	private void Agregar(string id)
	{
		var casilla = _guion.CasoPruebas[_casillaActiva];
		if (casilla.Count >= PruebasPorCasilla || _guion.CasoPruebas.Any(c => c.Contains(id))) return;
		casilla.Add(id);
		Refrescar();
	}

	private void DibujarHilos()
	{
		foreach (Node n in _hilos.GetChildren()) { _hilos.RemoveChild(n); n.QueueFree(); }
		if (!_botonesSospechoso.TryGetValue(_guion.CasoSospechoso ?? "", out var boton)) return;
		var desde = boton.GetGlobalRect().GetCenter() + new Vector2(boton.Size.X / 2, 0);
		foreach (var caja in _casillas)
		{
			var hilo = new Line2D { Width = 3, DefaultColor = Estambre, Points = new[] { desde, caja.GetGlobalRect().Position + new Vector2(0, caja.Size.Y / 2) } };
			_hilos.AddChild(hilo);
		}
	}
}
