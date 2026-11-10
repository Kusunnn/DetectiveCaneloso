using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// La Reconstrucción de la noche, con el estilo del expediente: 4 viñetas con huecos. Se elige un hueco y luego la ficha,
// con ratón, teclado o control. Al llenar una viñeta se comprueba sola: «ENCAJA» la sella y la colorea; con 1 o 2
// errores, «Casi... algo no cuadra», sin decir cuál.
public partial class PanelReconstruccion : CanvasLayer
{
	private static readonly Color Papel = new(0.84f, 0.81f, 0.72f), Tinta = new("20272b"), Ambar = new("efca08");
	private static readonly Color Gris = new(0.62f, 0.62f, 0.6f), Noche = new(0.33f, 0.45f, 0.62f), Verde = new(0.1f, 0.42f, 0.16f), Rojo = new(0.68f, 0.1f, 0.08f);
	private static readonly Dictionary<string, string> NombreTipo = new() { ["persona"] = "QUIÉN", ["hora"] = "A QUÉ HORA", ["objeto"] = "QUÉ", ["lugar"] = "DÓNDE" };

	private GuionCaso1 _guion;
	private Font _mono, _titulos;
	private TaskCompletionSource<string> _espera;
	private string _huecoActivo;
	private readonly List<PanelContainer> _vinetas = new();
	private readonly List<VBoxContainer> _textos = new();
	private VentanaDialogo _dialogo;
	private readonly List<Label> _sellos = new(), _mensajes = new();
	private readonly Dictionary<string, Button> _botonesHueco = new();
	private VBoxContainer _fichas;
	private Label _tituloFichas;
	private Button _salir, _acusar;

	public static PanelReconstruccion Crear(Node padre, GuionCaso1 guion, VentanaDialogo dialogo)
	{
		var panel = new PanelReconstruccion { Name = "PanelReconstruccion", _guion = guion, _dialogo = dialogo, Layer = 18, Visible = false };
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

	public async Task<string> Abrir()
	{
		Visible = true;
		// La caja de diálogo de la sala sigue abierta debajo: que no tape la hoja
		bool dialogoVisible = _dialogo?.Visible ?? false;
		if (_dialogo != null) _dialogo.Visible = false;
		var raton = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		foreach (var m in _mensajes) m.Text = "";
		_huecoActivo = PrimerHuecoVacio();
		Refrescar();
		_espera = new TaskCompletionSource<string>();
		string resultado = await _espera.Task;
		_espera = null;
		if (_dialogo != null) _dialogo.Visible = dialogoVisible;
		Input.MouseMode = raton;
		return resultado;
	}

	public void Cerrar() => Visible = false;

	// Lo usan las pruebas: llenar como lo haría el jugador
	public void Pruebas_Poner(string hueco, string ficha) { _huecoActivo = hueco; Poner(ficha); }
	public void Pruebas_Salir() => _espera?.TrySetResult("salir");
	public void Pruebas_Acusar() => _espera?.TrySetResult("acusar");
	public string Pruebas_Mensaje(int vineta) => _mensajes[vineta].Text;
	public bool EsperandoJugador => _espera != null;

	public override void _Input(InputEvent evento)
	{
		if (!Visible || _espera == null) return;
		if (evento.IsActionPressed("ui_cancel") || evento.IsActionPressed("pausa"))
		{
			GetViewport().SetInputAsHandled();
			_espera.TrySetResult("salir");
		}
	}

	private Label Texto(string texto, Font fuente, int tam, Color color)
	{
		var l = new Label { Text = texto };
		l.AddThemeFontOverride("font", fuente);
		l.AddThemeFontSizeOverride("font_size", tam);
		l.AddThemeColorOverride("font_color", color);
		return l;
	}

	private Button Boton(string texto, int tam = 20, bool hueco = false)
	{
		var b = new Button { Text = texto, ClipText = false };
		b.AddThemeFontOverride("font", _mono);
		b.AddThemeFontSizeOverride("font_size", tam);
		var normal = new StyleBoxFlat { BgColor = hueco ? new Color(1, 1, 1, 0.35f) : new Color(0.95f, 0.93f, 0.86f) };
		normal.SetContentMarginAll(4);
		normal.BorderWidthBottom = 2;
		normal.BorderColor = new Color(Tinta, 0.6f);
		var foco = (StyleBoxFlat)normal.Duplicate();
		foco.BgColor = Ambar;
		foreach (string estado in new[] { "hover", "focus", "pressed" }) b.AddThemeStyleboxOverride(estado, foco);
		b.AddThemeStyleboxOverride("normal", normal);
		b.AddThemeStyleboxOverride("disabled", normal);
		foreach (string color in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color", "font_disabled_color" }) b.AddThemeColorOverride(color, Tinta);
		b.Pressed += () => Sonidos.ReproducirUI(this, Sonidos.Tipo.Clic);
		return b;
	}

	private void Construir()
	{
		var fondo = new ColorRect { Color = new Color(0, 0, 0, 0.72f), MouseFilter = Control.MouseFilterEnum.Stop };
		fondo.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(fondo);
		var estilo = new StyleBoxFlat { BgColor = Papel, ShadowColor = new Color(0, 0, 0, 0.5f), ShadowSize = 14 };
		estilo.SetContentMarginAll(20);
		var hoja = new PanelContainer();
		hoja.AddThemeStyleboxOverride("panel", estilo);
		hoja.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		hoja.OffsetLeft = 40; hoja.OffsetRight = -40; hoja.OffsetTop = 22; hoja.OffsetBottom = -22;
		AddChild(hoja);

		var columnas = new VBoxContainer();
		columnas.AddThemeConstantOverride("separation", 10);
		hoja.AddChild(columnas);
		columnas.AddChild(Texto("RECONSTRUCCIÓN DE LA NOCHE", _titulos, 42, Tinta));

		var cuerpo = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		cuerpo.AddThemeConstantOverride("separation", 18);
		columnas.AddChild(cuerpo);

		var rejilla = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		rejilla.AddThemeConstantOverride("h_separation", 14);
		rejilla.AddThemeConstantOverride("v_separation", 14);
		cuerpo.AddChild(rejilla);
		for (int v = 0; v < _guion.Vinetas.Count; v++)
		{
			var caja = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(420, 0) };
			var interior = new VBoxContainer();
			interior.AddThemeConstantOverride("separation", 8);
			caja.AddChild(interior);
			var fila = new HBoxContainer();
			interior.AddChild(fila);
			var titulo = Texto(_guion.Vinetas[v].Titulo, _titulos, 28, Tinta);
			titulo.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			fila.AddChild(titulo);
			var sello = Texto("ENCAJA", _titulos, 32, Verde);
			sello.Rotation = -0.08f;
			sello.Visible = false;
			fila.AddChild(sello);
			_sellos.Add(sello);
			var texto = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
			texto.AddThemeConstantOverride("separation", 14);
			interior.AddChild(texto);
			_textos.Add(texto);
			var mensaje = Texto("", _mono, 20, Rojo);
			mensaje.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			interior.AddChild(mensaje);
			_mensajes.Add(mensaje);
			_vinetas.Add(caja);
			rejilla.AddChild(caja);
		}

		var derecha = new VBoxContainer { CustomMinimumSize = new Vector2(330, 0) };
		derecha.AddThemeConstantOverride("separation", 6);
		cuerpo.AddChild(derecha);
		_tituloFichas = Texto("FICHAS", _titulos, 26, Tinta);
		derecha.AddChild(_tituloFichas);
		var marco = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		derecha.AddChild(marco);
		_fichas = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_fichas.AddThemeConstantOverride("separation", 4);
		marco.AddChild(_fichas);

		var pie = new HBoxContainer();
		pie.AddThemeConstantOverride("separation", 14);
		columnas.AddChild(pie);
		_salir = Boton("Seguir investigando", 22);
		_salir.Pressed += () => _espera?.TrySetResult("salir");
		pie.AddChild(_salir);
		var ayuda = Texto("Elige un hueco y luego la ficha que va ahí. Cada viñeta se revisa sola al llenarla.", _mono, 18, new Color(Tinta, 0.75f));
		ayuda.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		ayuda.HorizontalAlignment = HorizontalAlignment.Center;
		pie.AddChild(ayuda);
		_acusar = Boton("Acusar ya", 22);
		_acusar.Pressed += () => _espera?.TrySetResult("acusar");
		pie.AddChild(_acusar);
	}

	private string PrimerHuecoVacio()
	{
		for (int v = 0; v < _guion.Vinetas.Count; v++)
		{
			if (_guion.Selladas.Contains(v)) continue;
			foreach (var (id, _) in _guion.HuecosDe(v))
				if (!_guion.Huecos.ContainsKey(id)) return id;
		}
		return null;
	}

	private string TipoDe(string hueco)
	{
		var p = hueco.Split('.').Select(int.Parse).ToArray();
		return _guion.Vinetas[p[0]].Frases[p[1]].Huecos[p[2]];
	}

	private void Refrescar()
	{
		_botonesHueco.Clear();
		for (int v = 0; v < _guion.Vinetas.Count; v++)
		{
			bool sellada = _guion.Selladas.Contains(v);
			_sellos[v].Visible = sellada;
			// La viñeta en gris hasta que encaja; sellada, en color
			var estilo = new StyleBoxFlat { BgColor = sellada ? new Color(0.98f, 0.86f, 0.55f) : new Color(Gris, 0.3f), BorderColor = sellada ? Verde : new Color(Tinta, 0.35f) };
			estilo.SetBorderWidthAll(sellada ? 3 : 1);
			estilo.SetContentMarginAll(12);
			_vinetas[v].AddThemeStyleboxOverride("panel", estilo);
			foreach (Node n in _textos[v].GetChildren()) { _textos[v].RemoveChild(n); n.QueueFree(); }
			var frases = _guion.Vinetas[v].Frases;
			for (int f = 0; f < frases.Count; f++)
			{
				var renglon = new HFlowContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
				renglon.AddThemeConstantOverride("h_separation", 6);
				renglon.AddThemeConstantOverride("v_separation", 8);
				_textos[v].AddChild(renglon);
				var partes = frases[f].Texto.Split('{', '}');
				for (int i = 0; i < partes.Length; i++)
				{
					if (i % 2 == 0)
					{
						foreach (string palabra in partes[i].Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
							renglon.AddChild(Texto(palabra, _mono, 21, Tinta));
						continue;
					}
					string id = GuionCaso1.IdHueco(v, f, int.Parse(partes[i]));
					string puesta = _guion.Huecos.TryGetValue(id, out string ficha) ? _guion.FichasReconstruccion[ficha].Texto : "________";
					var boton = Boton(puesta, 21, hueco: true);
					boton.Disabled = sellada;
					if (id == _huecoActivo) boton.Modulate = new Color(1f, 0.9f, 0.45f);
					string hueco = id;
					boton.Pressed += () => { _huecoActivo = hueco; Refrescar(); };
					renglon.AddChild(boton);
					_botonesHueco[id] = boton;
				}
			}
		}
		foreach (Node n in _fichas.GetChildren()) { _fichas.RemoveChild(n); n.QueueFree(); }
		if (_huecoActivo == null)
		{
			_tituloFichas.Text = "FICHAS";
			_salir.GrabFocus();
			return;
		}
		string tipo = TipoDe(_huecoActivo);
		_tituloFichas.Text = "FICHAS · " + NombreTipo.GetValueOrDefault(tipo, tipo.ToUpper());
		Button primera = null;
		foreach (var ficha in _guion.FichasDe(tipo).OrderBy(f => f.Texto))
		{
			string id = ficha.Id;
			var b = Boton(ficha.Texto, 21);
			b.Alignment = HorizontalAlignment.Left;
			b.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			b.Pressed += () => Poner(id);
			_fichas.AddChild(b);
			primera ??= b;
		}
		primera?.CallDeferred(Control.MethodName.GrabFocus);
	}

	private void Poner(string ficha)
	{
		if (_huecoActivo == null || !_guion.FichasReconstruccion.ContainsKey(ficha)) return;
		int vineta = int.Parse(_huecoActivo.Split('.')[0]);
		if (_guion.Selladas.Contains(vineta)) return;
		_guion.Huecos[_huecoActivo] = ficha;
		_mensajes[vineta].Text = "";
		if (_guion.VinetaLlena(vineta))
		{
			int errores = _guion.Comprobar(vineta);
			if (errores == 0)
			{
				Sonidos.ReproducirUI(this, Sonidos.Tipo.Sello);
				_mensajes[vineta].Text = "";
			}
			else
			{
				Sonidos.ReproducirUI(this, Sonidos.Tipo.Error);
				_mensajes[vineta].Text = _guion.LineaReconstruccion(errores <= 2 ? "casi" : "no_cuadra");
			}
		}
		// Sigue con el próximo hueco vacío de esa misma viñeta, o de la siguiente
		_huecoActivo = _guion.HuecosDe(vineta).Select(h => h.Id).FirstOrDefault(h => !_guion.Huecos.ContainsKey(h)) ?? PrimerHuecoVacio();
		Refrescar();
		if (_guion.ReconstruccionCompleta) CompletarDespues();
	}

	private async void CompletarDespues()
	{
		await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
		_espera?.TrySetResult("completa");
	}
}
