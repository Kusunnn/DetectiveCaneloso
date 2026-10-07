using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Tarjeta de título al iniciar un capítulo ("Capítulo X — nombre" con ilustración) y resumen
// "Anteriormente..." al cargar o empezar un capítulo nuevo. Cualquier tecla, clic o botón la salta.
// Pausa el juego mientras se muestra.
public partial class PresentacionCapitulo : Control
{
	private ColorRect _fondo;
	private Control _titulo, _anteriormente;
	private TextureRect _ilustracion;
	private Label _etiqueta, _nombre, _ayuda;
	private VBoxContainer _lineas;
	private bool _saltar;
	private bool _activa;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Hide();

		_fondo = new ColorRect { Color = new Color(0.02f, 0.025f, 0.03f), MouseFilter = MouseFilterEnum.Ignore };
		_fondo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_fondo);

		// Tarjeta de título
		var centro = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(centro);
		var columna = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
		columna.AddThemeConstantOverride("separation", 14);
		centro.AddChild(columna);
		_titulo = centro;
		var marco = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
		var estiloMarco = new StyleBoxFlat { BgColor = EstiloPapel.Papel, ShadowColor = new Color(0, 0, 0, 0.6f), ShadowSize = 14 };
		estiloMarco.SetContentMarginAll(12);
		estiloMarco.ContentMarginBottom = 34;
		marco.AddThemeStyleboxOverride("panel", estiloMarco);
		marco.Rotation = Mathf.DegToRad(-1.5f);
		columna.AddChild(marco);
		_ilustracion = new TextureRect
		{
			CustomMinimumSize = new Vector2(560, 315),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			// Tono sepia de foto vieja
			SelfModulate = new Color(1f, 0.9f, 0.75f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		marco.AddChild(_ilustracion);
		_etiqueta = EstiloPapel.Texto("", 26, EstiloPapel.Ambar);
		_etiqueta.HorizontalAlignment = HorizontalAlignment.Center;
		columna.AddChild(_etiqueta);
		_nombre = EstiloPapel.Texto("", 76, new Color(0.93f, 0.92f, 0.88f), titulo: true);
		_nombre.HorizontalAlignment = HorizontalAlignment.Center;
		columna.AddChild(_nombre);
		var linea = EstiloPapel.Subrayado(220);
		linea.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		columna.AddChild(linea);

		// Anteriormente...
		var centro2 = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centro2.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(centro2);
		_anteriormente = centro2;
		var hoja = new PanelContainer { CustomMinimumSize = new Vector2(860, 0), MouseFilter = MouseFilterEnum.Ignore };
		hoja.AddThemeStyleboxOverride("panel", EstiloPapel.Hoja(34));
		hoja.Rotation = Mathf.DegToRad(0.6f);
		centro2.AddChild(hoja);
		var columna2 = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		columna2.AddThemeConstantOverride("separation", 14);
		hoja.AddChild(columna2);
		columna2.AddChild(EstiloPapel.Texto("ANTERIORMENTE...", 44, titulo: true));
		columna2.AddChild(EstiloPapel.Subrayado());
		_lineas = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_lineas.AddThemeConstantOverride("separation", 10);
		columna2.AddChild(_lineas);

		_ayuda = EstiloPapel.Texto("", 16, new Color(0.8f, 0.8f, 0.78f, 0.75f));
		_ayuda.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
		_ayuda.GrowHorizontal = GrowDirection.Begin;
		_ayuda.GrowVertical = GrowDirection.Begin;
		_ayuda.Position -= new Vector2(32, 26);
		AddChild(_ayuda);
	}

	public override void _Input(InputEvent evento)
	{
		if (!_activa) return;
		bool pulsado = evento switch
		{
			InputEventKey tecla => tecla.Pressed && !tecla.Echo,
			InputEventMouseButton boton => boton.Pressed,
			InputEventJoypadButton mando => mando.Pressed,
			InputEventAction accion => accion.Pressed,
			_ => false,
		};
		if (!pulsado) return;
		GetViewport().SetInputAsHandled();
		_saltar = true;
	}

	// Espera unos segundos o hasta que el jugador pulse algo
	private async Task Esperar(double segundos)
	{
		ulong fin = Time.GetTicksMsec() + (ulong)(segundos * 1000);
		while (!_saltar && Time.GetTicksMsec() < fin)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task Abrir(Control parte, string ayuda)
	{
		_titulo.Visible = parte == _titulo;
		_anteriormente.Visible = parte == _anteriormente;
		_ayuda.Text = ayuda;
		_ayuda.ResetSize();
		_ayuda.Position = Size - _ayuda.Size - new Vector2(32, 26);
		Modulate = new Color(1, 1, 1, 0);
		Show();
		_saltar = false;
		_activa = true;
		await ToSignal(CreateTween().TweenProperty(this, "modulate:a", 1f, 0.35), Tween.SignalName.Finished);
	}

	private async Task Cerrar()
	{
		_activa = false;
		await ToSignal(CreateTween().TweenProperty(this, "modulate:a", 0f, 0.35), Tween.SignalName.Finished);
		Hide();
	}

	public async Task MostrarTitulo(InfoCapitulo capitulo)
	{
		var arbol = GetTree();
		bool pausado = arbol.Paused;
		arbol.Paused = true;
		_etiqueta.Text = capitulo.Etiqueta.ToUpper();
		_nombre.Text = capitulo.Titulo;
		var imagen = capitulo.CargarIlustracion();
		_ilustracion.Texture = imagen;
		_ilustracion.GetParent<Control>().Visible = imagen != null;
		_nombre.Modulate = new Color(1, 1, 1, 0);
		await Abrir(_titulo, "Cualquier tecla para saltar");
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Pagina);
		var aparecer = CreateTween();
		aparecer.TweenInterval(0.35);
		aparecer.TweenCallback(Callable.From(() => Sonidos.ReproducirUI(this, Sonidos.Tipo.Sello, 3f)));
		aparecer.TweenProperty(_nombre, "modulate:a", 1f, 0.5);
		await Esperar(3.4);
		aparecer.Kill();
		await Cerrar();
		arbol.Paused = pausado;
	}

	public async Task MostrarAnteriormente(IList<string> lineas)
	{
		var arbol = GetTree();
		bool pausado = arbol.Paused;
		arbol.Paused = true;
		foreach (Node n in _lineas.GetChildren()) n.QueueFree();
		var etiquetas = new List<Label>();
		foreach (string texto in lineas)
		{
			var l = EstiloPapel.Texto("· " + texto, 21);
			l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			l.CustomMinimumSize = new Vector2(790, 0);
			l.VisibleRatio = 0;
			_lineas.AddChild(l);
			etiquetas.Add(l);
		}
		await Abrir(_anteriormente, "Cualquier tecla para continuar");
		// Máquina de escribir; una pulsación termina de escribir todo y la siguiente cierra
		foreach (var l in etiquetas)
		{
			int total = l.Text.Length;
			for (int i = 1; i <= total && !_saltar; i++)
			{
				l.VisibleCharacters = i;
				if (i % 3 == 0) Sonidos.ReproducirUI(this, Sonidos.Tipo.Texto);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			l.VisibleRatio = 1;
		}
		foreach (var l in etiquetas) l.VisibleRatio = 1;
		_saltar = false;
		await Esperar(2.5 + lineas.Count * 2.0);
		await Cerrar();
		arbol.Paused = pausado;
	}
}
