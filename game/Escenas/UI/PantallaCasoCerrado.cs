using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Lo que muestra la pantalla de "Caso cerrado". Lo arma el caso; la pantalla no conoce la historia.
public class ResultadoCaso
{
	public string Encabezado = "";          // "CASO 0 · LA CINTA"
	public string Felicitacion = "";        // "¡Felicidades, detective! Resolviste tu primer caso."
	public List<(string Concepto, string Valor)> Lineas = new();
	public string Rango = "";               // S, A, B, C
	public int Lupas;                       // 1 a 5
	public string Frase = "";               // "Detective nato"
	public string TextoContinuar = "";      // "Continuar al Capítulo 1"
	public string TextoRepetir = "Repetir caso";
}

// Pantalla de cierre de un caso (estilo expediente): sello "CASO CERRADO", felicitación, resumen línea por
// línea con máquina de escribir, calificación con lupas y [Continuar] [Repetir] [Menú principal].
// Un clic o una tecla termina las animaciones. Teclado, mouse y control.
public partial class PantallaCasoCerrado : Control
{
	public enum Eleccion { Continuar, Repetir, Menu }

	private ColorRect _oscuro;
	private Control _hoja;
	private Label _encabezado, _felicitacion, _frase, _rango;
	private PanelContainer _sello;
	private Control _anclaSello;
	private VBoxContainer _lineas;
	private HBoxContainer _calificacion, _botones;
	private Control _lupas;
	private Button _continuar, _repetir, _menu;
	private bool _saltar, _animando;
	private TaskCompletionSource<Eleccion> _eleccion;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Hide();

		_oscuro = new ColorRect { Color = new Color(0, 0, 0, 0.8f), MouseFilter = MouseFilterEnum.Ignore };
		_oscuro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_oscuro);
		var centro = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(centro);

		var hoja = new PanelContainer { CustomMinimumSize = new Vector2(920, 0), MouseFilter = MouseFilterEnum.Ignore };
		hoja.AddThemeStyleboxOverride("panel", EstiloPapel.Hoja(36));
		centro.AddChild(hoja);
		_hoja = hoja;
		var columna = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		columna.AddThemeConstantOverride("separation", 10);
		hoja.AddChild(columna);

		var cabecera = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		columna.AddChild(cabecera);
		_encabezado = EstiloPapel.Texto("", 16, new Color(EstiloPapel.Tinta, 0.65f));
		_encabezado.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		cabecera.AddChild(_encabezado);
		_anclaSello = new Control { MouseFilter = MouseFilterEnum.Ignore };
		cabecera.AddChild(_anclaSello);
		_felicitacion = EstiloPapel.Texto("", 42, titulo: true);
		_felicitacion.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_felicitacion.CustomMinimumSize = new Vector2(600, 0);
		_felicitacion.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		columna.AddChild(_felicitacion);
		columna.AddChild(EstiloPapel.Subrayado());

		_lineas = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_lineas.AddThemeConstantOverride("separation", 6);
		columna.AddChild(_lineas);

		columna.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
		_calificacion = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_calificacion.AddThemeConstantOverride("separation", 18);
		columna.AddChild(_calificacion);
		_calificacion.AddChild(EstiloPapel.Texto("CALIFICACIÓN", 22, titulo: true));
		_rango = EstiloPapel.Texto("", 54, titulo: true);
		_calificacion.AddChild(_rango);
		_frase = EstiloPapel.Texto("", 20);
		_frase.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_calificacion.AddChild(_frase);

		_botones = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		_botones.AddThemeConstantOverride("separation", 12);
		columna.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
		columna.AddChild(_botones);
		_menu = EstiloPapel.Boton("Menú principal");
		_repetir = EstiloPapel.Boton("Repetir");
		_continuar = EstiloPapel.Boton("Continuar", principal: true);
		_botones.AddChild(_menu);
		_botones.AddChild(_repetir);
		_botones.AddChild(_continuar);
		_continuar.Pressed += () => Elegir(Eleccion.Continuar);
		_repetir.Pressed += () => Elegir(Eleccion.Repetir);
		_menu.Pressed += () => Elegir(Eleccion.Menu);
		UtilidadesUI.ConectarFocoConRaton(this);
		UtilidadesUI.EncadenarFoco(new Control[] { _continuar, _repetir, _menu });
		_continuar.FocusNeighborLeft = _continuar.GetPathTo(_repetir);
		_repetir.FocusNeighborLeft = _repetir.GetPathTo(_menu);
		_repetir.FocusNeighborRight = _repetir.GetPathTo(_continuar);
		_menu.FocusNeighborRight = _menu.GetPathTo(_repetir);

		// Sello "CASO CERRADO": tinta roja sobre la hoja, ladeado
		_sello = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
		var estilo = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = EstiloPapel.Rojo };
		estilo.SetBorderWidthAll(6);
		estilo.SetCornerRadiusAll(6);
		estilo.SetContentMarginAll(10);
		estilo.ContentMarginLeft = estilo.ContentMarginRight = 22;
		_sello.AddThemeStyleboxOverride("panel", estilo);
		_sello.AddChild(EstiloPapel.Texto("CASO CERRADO", 50, EstiloPapel.Rojo, titulo: true));
		// Fuera de los contenedores para que conserve el giro: cuelga de un ancla al final de la cabecera
		_anclaSello.AddChild(_sello);
		_sello.Position = new Vector2(-285, -50);
		_sello.Rotation = Mathf.DegToRad(-9);
	}

	public override void _Input(InputEvent evento)
	{
		if (!_animando) return;
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

	private async Task Esperar(double segundos)
	{
		ulong fin = Time.GetTicksMsec() + (ulong)(segundos * 1000);
		while (!_saltar && Time.GetTicksMsec() < fin)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	public async Task<Eleccion> Mostrar(ResultadoCaso r)
	{
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_encabezado.Text = "EXPEDIENTE · " + r.Encabezado.ToUpper();
		_felicitacion.Text = r.Felicitacion;
		_continuar.Text = r.TextoContinuar;
		_continuar.Visible = !string.IsNullOrEmpty(r.TextoContinuar);
		_repetir.Text = r.TextoRepetir;
		_rango.Text = r.Rango;
		foreach (Node n in _lineas.GetChildren()) n.QueueFree();
		_lupas?.QueueFree();
		_lupas = EstiloPapel.Lupas(r.Lupas);
		_lupas.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_calificacion.AddChild(_lupas);
		_calificacion.MoveChild(_lupas, _rango.GetIndex() + 1);
		_frase.Text = "«" + r.Frase + "»";

		var filas = new List<(Label, Label)>();
		foreach (var (concepto, valor) in r.Lineas)
		{
			var fila = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
			var c = EstiloPapel.Texto(concepto, 20);
			c.CustomMinimumSize = new Vector2(520, 0);
			var v = EstiloPapel.Texto(valor, 20);
			c.VisibleRatio = 0;
			v.VisibleRatio = 0;
			fila.AddChild(c);
			fila.AddChild(v);
			_lineas.AddChild(fila);
			filas.Add((c, v));
		}

		// Todo empieza oculto y aparece en orden
		_saltar = false;
		_animando = true;
		_oscuro.Modulate = new Color(1, 1, 1, 0);
		_hoja.Modulate = new Color(1, 1, 1, 0);
		_sello.Modulate = new Color(1, 1, 1, 0);
		_felicitacion.Modulate = new Color(1, 1, 1, 0);
		_calificacion.Modulate = new Color(1, 1, 1, 0);
		_botones.Visible = false;
		Show();

		var entrada = CreateTween();
		entrada.TweenProperty(_oscuro, "modulate:a", 1f, 0.5);
		entrada.TweenProperty(_hoja, "modulate:a", 1f, 0.35);
		await Esperar(0.95);

		// 1. Sello con golpe y música de victoria
		_sello.PivotOffset = _sello.Size / 2;
		_sello.Scale = new Vector2(2.4f, 2.4f);
		var golpe = CreateTween().SetParallel();
		golpe.TweenProperty(_sello, "modulate:a", 1f, 0.12);
		golpe.TweenProperty(_sello, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Sello, 6f);
		TocarFanfarria();
		await Esperar(0.9);

		// 2. Felicitación
		CreateTween().TweenProperty(_felicitacion, "modulate:a", 1f, 0.4);
		await Esperar(0.9);

		// 3. Resumen línea por línea
		foreach (var (c, v) in filas)
		{
			foreach (var etiqueta in new[] { c, v })
			{
				int total = etiqueta.Text.Length;
				for (int i = 1; i <= total && !_saltar; i++)
				{
					etiqueta.VisibleCharacters = i;
					if (i % 3 == 0) Sonidos.ReproducirUI(this, Sonidos.Tipo.Texto);
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				etiqueta.VisibleRatio = 1;
			}
			await Esperar(0.15);
		}

		// 4. Calificación
		await Esperar(0.3);
		CreateTween().TweenProperty(_calificacion, "modulate:a", 1f, 0.35);
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Deduccion);
		await Esperar(0.6);

		// Si el jugador saltó, todo queda en su sitio de golpe
		golpe.Kill();
		entrada.Kill();
		foreach (var (c, v) in filas) { c.VisibleRatio = 1; v.VisibleRatio = 1; }
		foreach (var nodo in new Control[] { _oscuro, _hoja, _sello, _felicitacion, _calificacion }) nodo.Modulate = Colors.White;
		_sello.Scale = Vector2.One;
		_animando = false;

		// 5. Botones
		_botones.Visible = true;
		(_continuar.Visible ? _continuar : _menu).GrabFocus();
		_eleccion = new TaskCompletionSource<Eleccion>();
		return await _eleccion.Task;
	}

	private void Elegir(Eleccion e)
	{
		if (_animando) return;
		var t = _eleccion;
		_eleccion = null;
		t?.TrySetResult(e);
	}

	// ---------- Música de victoria (corta, bus Música, generada aquí: sin archivos extra) ----------

	private void TocarFanfarria()
	{
		var reproductor = new AudioStreamPlayer
		{
			Stream = Fanfarria(),
			Bus = Sonidos.BusMusica,
			VolumeDb = -9f,
			ProcessMode = ProcessModeEnum.Always,
		};
		AddChild(reproductor);
		reproductor.Finished += reproductor.QueueFree;
		reproductor.Play();
	}

	// Piano de cine negro: arpegio menor que se resuelve en mayor (≈3.6 s)
	private static AudioStreamWav Fanfarria()
	{
		const int frecuencia = 22050;
		// (inicio en s, nota MIDI, duración en s, volumen)
		var notas = new (double, int, double, float)[]
		{
			(0.00, 48, 1.2, 0.45f), (0.00, 60, 0.5, 0.35f), (0.16, 63, 0.5, 0.33f), (0.32, 67, 0.6, 0.33f),
			(0.62, 44, 1.0, 0.4f), (0.62, 68, 0.5, 0.33f), (0.80, 72, 0.6, 0.33f),
			(1.12, 46, 1.0, 0.4f), (1.12, 70, 0.5, 0.33f), (1.30, 74, 0.7, 0.33f),
			(1.70, 36, 2.0, 0.45f), (1.70, 48, 2.0, 0.35f), (1.70, 64, 1.9, 0.3f), (1.70, 67, 1.9, 0.28f), (1.84, 72, 1.8, 0.3f), (1.98, 76, 1.7, 0.26f),
		};
		int muestras = (int)(frecuencia * 3.7);
		var mezcla = new float[muestras];
		foreach (var (inicio, nota, duracion, volumen) in notas)
		{
			double hz = 440.0 * Math.Pow(2, (nota - 69) / 12.0);
			int desde = (int)(inicio * frecuencia);
			int largo = Math.Min((int)(duracion * frecuencia) + frecuencia / 2, muestras - desde);
			for (int i = 0; i < largo; i++)
			{
				double t = (double)i / frecuencia;
				double ataque = Math.Min(1.0, t / 0.008);
				double caida = Math.Exp(-t * 2.6) * (t > duracion ? Math.Exp(-(t - duracion) * 9) : 1);
				double onda = Math.Sin(Math.Tau * hz * t) + 0.35 * Math.Sin(Math.Tau * hz * 2 * t) + 0.12 * Math.Sin(Math.Tau * hz * 3 * t);
				mezcla[desde + i] += (float)(onda * ataque * caida * volumen * 0.5);
			}
		}
		var datos = new byte[muestras * 2];
		for (int i = 0; i < muestras; i++)
		{
			short valor = (short)(Math.Clamp(mezcla[i], -1f, 1f) * 30000);
			datos[i * 2] = (byte)(valor & 0xff);
			datos[i * 2 + 1] = (byte)((valor >> 8) & 0xff);
		}
		return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = frecuencia, Stereo = false, Data = datos };
	}
}
