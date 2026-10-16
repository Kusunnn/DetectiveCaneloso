using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Menú de capítulos: tablero de corcho con un expediente clavado por capítulo, unidos con hilo rojo en orden.
//   Resuelto: sello "CERRADO", calificación y % completado (se puede rejugar sin tocar la partida guardada).
//   Actual: iluminado y con un balanceo sutil.
//   Bloqueado: carpeta sellada, sin revelar el título.
// Lee los capítulos de Datos/capitulos.json y el progreso de user://progreso.json.
public partial class TableroCapitulos : Control
{
	[Signal] public delegate void CerradoEventHandler();

	private static readonly Color Manila = new Color(0.86f, 0.76f, 0.55f);
	private static readonly Color Corcho = new Color(0.55f, 0.38f, 0.22f);
	private static readonly Color Hilo = new Color(0.72f, 0.08f, 0.07f);

	private Control _hilo;
	private HBoxContainer _fila;
	private Label _mensaje;
	private Button _volver;
	private readonly List<(Button Carpeta, InfoCapitulo Capitulo)> _carpetas = new();
	private Control _detalle;
	private VBoxContainer _detalleColumna;
	private Control _focoAlCerrarDetalle;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Hide();

		// Corcho: color base, grano con ruido y marco de madera
		var marco = new Panel { MouseFilter = MouseFilterEnum.Ignore };
		var madera = new StyleBoxFlat { BgColor = Corcho, BorderColor = new Color(0.25f, 0.15f, 0.08f) };
		madera.SetBorderWidthAll(26);
		marco.AddThemeStyleboxOverride("panel", madera);
		marco.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(marco);
		var ruido = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular, Frequency = 0.09f };
		var rampa = new Gradient();
		rampa.SetColor(0, new Color(0.38f, 0.24f, 0.12f, 0.55f));
		rampa.SetColor(1, new Color(0.78f, 0.6f, 0.4f, 0.35f));
		var grano = new TextureRect
		{
			Texture = new NoiseTexture2D { Width = 512, Height = 512, Seamless = true, Noise = ruido, ColorRamp = rampa },
			StretchMode = TextureRect.StretchModeEnum.Tile,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		grano.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 26);
		AddChild(grano);

		// Título clavado arriba
		var cabecera = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Rotation = Mathf.DegToRad(-1.2f) };
		cabecera.AddThemeStyleboxOverride("panel", EstiloPapel.Hoja(16));
		var columnaTitulo = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		columnaTitulo.AddChild(EstiloPapel.Texto("EXPEDIENTES DE CANELOSO", 46, titulo: true));
		columnaTitulo.AddChild(EstiloPapel.Subrayado(200));
		cabecera.AddChild(columnaTitulo);
		cabecera.Position = new Vector2(90, 70);
		AddChild(cabecera);

		// Hilo rojo (detrás de las carpetas)
		_hilo = new Control { MouseFilter = MouseFilterEnum.Ignore };
		_hilo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_hilo.Draw += DibujarHilo;
		AddChild(_hilo);

		var centro = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(centro);
		_fila = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_fila.AddThemeConstantOverride("separation", 70);
		centro.AddChild(_fila);

		// Pie: mensaje y volver
		var pie = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		pie.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
		pie.OffsetTop = -130;
		pie.OffsetBottom = -60;
		pie.OffsetLeft = 90;
		pie.OffsetRight = -90;
		AddChild(pie);
		var nota = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		nota.AddThemeStyleboxOverride("panel", EstiloPapel.Hoja(14));
		_mensaje = EstiloPapel.Texto("", 20);
		nota.AddChild(_mensaje);
		pie.AddChild(nota);
		pie.AddChild(new Control { CustomMinimumSize = new Vector2(30, 0) });
		_volver = EstiloPapel.Boton("Volver", tam: 22);
		_volver.CustomMinimumSize = new Vector2(180, 0);
		_volver.Pressed += Cerrar;
		pie.AddChild(_volver);

		CrearDetalle();
	}

	// ---------- Abrir y armar el tablero ----------

	public void Abrir(string mensaje = null)
	{
		Capitulos.Recargar();
		Construir();
		_mensaje.Text = mensaje ?? "Elige un expediente. Los resueltos se pueden repetir sin tocar tu partida guardada.";
		Show();
		var actual = _carpetas.FirstOrDefault(c => c.Capitulo == Capitulos.Actual()).Carpeta ?? _carpetas.FirstOrDefault().Carpeta;
		actual?.GrabFocus();
	}

	public void Cerrar()
	{
		Hide();
		EmitSignal(SignalName.Cerrado);
	}

	private void Construir()
	{
		foreach (Node n in _fila.GetChildren()) { _fila.RemoveChild(n); n.QueueFree(); }
		_carpetas.Clear();
		var actual = Capitulos.Actual();
		int i = 0;
		foreach (var capitulo in Capitulos.Lista)
		{
			var estado = Capitulos.Resuelto(capitulo.Id) ? Estado.Resuelto
				: capitulo == actual ? Estado.Actual
				: Capitulos.Desbloqueado(capitulo) ? Estado.Actual
				: Estado.Bloqueado;
			var carpeta = CrearCarpeta(capitulo, estado, i++);
			_fila.AddChild(carpeta);
			_carpetas.Add((carpeta, capitulo));
		}
		var controles = _carpetas.Select(c => (Control)c.Carpeta).ToList();
		for (int k = 0; k < controles.Count; k++)
		{
			var c = controles[k];
			c.FocusNeighborLeft = c.GetPathTo(controles[(k - 1 + controles.Count) % controles.Count]);
			c.FocusNeighborRight = c.GetPathTo(controles[(k + 1) % controles.Count]);
			c.FocusNeighborBottom = c.GetPathTo(_volver);
			c.FocusNeighborTop = c.GetPathTo(_volver);
		}
		_volver.FocusNeighborTop = _volver.GetPathTo(controles.FirstOrDefault() ?? _volver);
		_volver.FocusNeighborLeft = _volver.FocusNeighborTop;
		UtilidadesUI.ConectarFocoConRaton(this);
	}

	private enum Estado { Resuelto, Actual, Bloqueado }

	private Button CrearCarpeta(InfoCapitulo capitulo, Estado estado, int indice)
	{
		var carpeta = new Button { CustomMinimumSize = new Vector2(300, 400), FocusMode = FocusModeEnum.All, ClipContents = false };
		carpeta.SetMeta("capitulo", capitulo.Id);
		carpeta.SetMeta("estado", estado.ToString());
		bool bloqueado = estado == Estado.Bloqueado;
		var color = bloqueado ? new Color(0.45f, 0.4f, 0.33f) : Manila;
		var normal = new StyleBoxFlat { BgColor = color, ShadowColor = new Color(0, 0, 0, 0.5f), ShadowSize = 8, ShadowOffset = new Vector2(4, 6) };
		normal.SetCornerRadiusAll(3);
		var foco = (StyleBoxFlat)normal.Duplicate();
		foco.BorderColor = EstiloPapel.Ambar;
		foco.SetBorderWidthAll(6);
		foco.ShadowSize = 18;
		foreach (string s in new[] { "normal", "pressed", "disabled" }) carpeta.AddThemeStyleboxOverride(s, normal);
		carpeta.AddThemeStyleboxOverride("hover", foco);
		carpeta.AddThemeStyleboxOverride("focus", foco);
		// Cada carpeta clavada un poco chueca y a distinta altura
		carpeta.PivotOffset = new Vector2(150, 0);
		carpeta.Rotation = Mathf.DegToRad(indice % 2 == 0 ? -2.5f : 2f);
		carpeta.SizeFlagsVertical = SizeFlags.ShrinkBegin;

		var margen = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		margen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		foreach (string lado in new[] { "left", "right" }) margen.AddThemeConstantOverride("margin_" + lado, 18);
		margen.AddThemeConstantOverride("margin_top", 34);
		margen.AddThemeConstantOverride("margin_bottom", 16);
		carpeta.AddChild(margen);
		var columna = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		columna.AddThemeConstantOverride("separation", 6);
		margen.AddChild(columna);

		columna.AddChild(EstiloPapel.Texto(capitulo.Etiqueta.ToUpper(), 16, new Color(EstiloPapel.Tinta, 0.7f)));
		var titulo = EstiloPapel.Texto(bloqueado ? "? ? ?" : capitulo.Titulo, 32, titulo: true);
		titulo.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		titulo.CustomMinimumSize = new Vector2(264, 0);
		columna.AddChild(titulo);

		// Foto clavada (o un recuadro vacío si el capítulo aún no tiene ilustración)
		var foto = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(264, 150) };
		var estiloFoto = new StyleBoxFlat { BgColor = bloqueado ? new Color(0.2f, 0.18f, 0.15f) : new Color(0.12f, 0.12f, 0.13f) };
		estiloFoto.SetContentMarginAll(6);
		foto.AddThemeStyleboxOverride("panel", estiloFoto);
		var imagen = bloqueado ? null : capitulo.CargarIlustracion();
		if (imagen != null)
			foto.AddChild(new TextureRect { Texture = imagen, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, SelfModulate = new Color(1f, 0.9f, 0.75f), MouseFilter = MouseFilterEnum.Ignore });
		else
		{
			var vacio = EstiloPapel.Texto(bloqueado ? "" : "SIN FOTO", 20, new Color(1, 1, 1, 0.35f), titulo: true);
			vacio.HorizontalAlignment = HorizontalAlignment.Center;
			vacio.VerticalAlignment = VerticalAlignment.Center;
			foto.AddChild(vacio);
		}
		columna.AddChild(foto);

		var resultado = Capitulos.Resultado(capitulo.Id);
		switch (estado)
		{
			case Estado.Resuelto:
				var fila = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
				fila.AddThemeConstantOverride("separation", 10);
				fila.AddChild(EstiloPapel.Texto(resultado.Rango, 34, titulo: true));
				var lupas = EstiloPapel.Lupas(resultado.Lupas, 5, 22);
				lupas.SizeFlagsVertical = SizeFlags.ShrinkCenter;
				fila.AddChild(lupas);
				columna.AddChild(fila);
				columna.AddChild(EstiloPapel.Texto($"{resultado.Porcentaje} % completado", 16));
				break;
			case Estado.Actual:
				columna.AddChild(EstiloPapel.Texto(capitulo.TieneEscena ? "▶ CASO ACTUAL" : "▶ EN PREPARACIÓN", 18, EstiloPapel.Rojo));
				break;
			default:
				columna.AddChild(EstiloPapel.Texto("Resuelve el caso anterior", 16, new Color(EstiloPapel.Tinta, 0.8f)));
				break;
		}

		// Chinche roja
		var chinche = new Control { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(150, 12) };
		chinche.Draw += () =>
		{
			chinche.DrawCircle(new Vector2(2, 3), 11, new Color(0, 0, 0, 0.35f));
			chinche.DrawCircle(Vector2.Zero, 11, new Color(0.78f, 0.1f, 0.08f));
			chinche.DrawCircle(new Vector2(-3, -3), 4, new Color(1, 1, 1, 0.5f));
		};
		carpeta.AddChild(chinche);

		if (estado == Estado.Resuelto)
		{
			// Sello CERRADO en tinta roja, cruzado sobre la carpeta
			var sello = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Rotation = Mathf.DegToRad(-14), Position = new Vector2(70, 205) };
			var estilo = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = new Color(EstiloPapel.Rojo, 0.85f) };
			estilo.SetBorderWidthAll(5);
			estilo.SetCornerRadiusAll(5);
			estilo.SetContentMarginAll(6);
			estilo.ContentMarginLeft = estilo.ContentMarginRight = 14;
			sello.AddThemeStyleboxOverride("panel", estilo);
			sello.AddChild(EstiloPapel.Texto("CERRADO", 40, new Color(EstiloPapel.Rojo, 0.85f), titulo: true));
			carpeta.AddChild(sello);
		}
		else if (estado == Estado.Bloqueado)
		{
			// Cinta de "sellado" cruzada y la carpeta apagada
			var cinta = new ColorRect { Color = new Color(0.12f, 0.1f, 0.08f, 0.9f), Size = new Vector2(380, 34), Position = new Vector2(-40, 250), Rotation = Mathf.DegToRad(-24), MouseFilter = MouseFilterEnum.Ignore };
			var texto = EstiloPapel.Texto("CONFIDENCIAL · CONFIDENCIAL", 18, EstiloPapel.Ambar);
			texto.Position = new Vector2(48, 4);
			cinta.AddChild(texto);
			carpeta.AddChild(cinta);
			carpeta.Modulate = new Color(0.8f, 0.8f, 0.8f);
		}
		else
		{
			// Iluminado: halo cálido detrás y un balanceo sutil
			var halo = new TextureRect
			{
				Texture = new GradientTexture2D
				{
					Width = 256, Height = 256, Fill = GradientTexture2D.FillEnum.Radial,
					FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(0.5f, 0f),
					Gradient = new Gradient { Colors = new[] { new Color(1f, 0.85f, 0.4f, 0.55f), new Color(1f, 0.85f, 0.4f, 0f) } },
				},
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				Size = new Vector2(560, 640),
				Position = new Vector2(-130, -120),
				MouseFilter = MouseFilterEnum.Ignore,
				ShowBehindParent = true,
			};
			carpeta.AddChild(halo);
			carpeta.MoveChild(halo, 0);
			float base_ = carpeta.Rotation;
			var balanceo = carpeta.CreateTween().SetLoops();
			balanceo.TweenProperty(carpeta, "rotation", base_ + Mathf.DegToRad(1.6f), 1.6).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			balanceo.TweenProperty(carpeta, "rotation", base_ - Mathf.DegToRad(0.6f), 1.6).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		}

		carpeta.Pressed += () => Elegir(capitulo, estado, carpeta);
		return carpeta;
	}

	// Hilo rojo de chinche a chinche, con una ligera curva
	private void DibujarHilo()
	{
		var puntos = _carpetas.Select(c => _hilo.GetGlobalTransform().AffineInverse() * c.Carpeta.GetGlobalTransform() * new Vector2(150, 12)).ToList();
		for (int i = 0; i + 1 < puntos.Count; i++)
		{
			Vector2 a = puntos[i], b = puntos[i + 1];
			Vector2 medio = (a + b) / 2 + new Vector2(0, 40);
			var curva = new List<Vector2>();
			for (int k = 0; k <= 16; k++)
			{
				float t = k / 16f;
				curva.Add(a.Lerp(medio, t).Lerp(medio.Lerp(b, t), t));
			}
			_hilo.DrawPolyline(curva.Select(p => p + new Vector2(2, 4)).ToArray(), new Color(0, 0, 0, 0.3f), 4f, true);
			_hilo.DrawPolyline(curva.ToArray(), Hilo, 3f, true);
		}
	}

	public override void _Process(double delta)
	{
		if (Visible) _hilo.QueueRedraw();
	}

	// ---------- Detalle de un expediente ----------

	private void CrearDetalle()
	{
		var fondo = new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = MouseFilterEnum.Stop };
		fondo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(fondo);
		var centro = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		fondo.AddChild(centro);
		var hoja = new PanelContainer { CustomMinimumSize = new Vector2(820, 0), MouseFilter = MouseFilterEnum.Ignore };
		hoja.AddThemeStyleboxOverride("panel", EstiloPapel.Hoja(32));
		centro.AddChild(hoja);
		_detalleColumna = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_detalleColumna.AddThemeConstantOverride("separation", 12);
		hoja.AddChild(_detalleColumna);
		_detalle = fondo;
		_detalle.Hide();
	}

	private void Elegir(InfoCapitulo capitulo, Estado estado, Button carpeta)
	{
		if (estado == Estado.Bloqueado)
		{
			Sonidos.ReproducirUI(this, Sonidos.Tipo.Error);
			_mensaje.Text = $"{capitulo.Etiqueta}: expediente sellado. Resuelve el caso anterior para abrirlo.";
			return;
		}
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Expediente);
		foreach (Node n in _detalleColumna.GetChildren()) { _detalleColumna.RemoveChild(n); n.QueueFree(); }
		_detalleColumna.AddChild(EstiloPapel.Texto(capitulo.Etiqueta.ToUpper(), 18, new Color(EstiloPapel.Tinta, 0.65f)));
		_detalleColumna.AddChild(EstiloPapel.Texto(capitulo.Titulo, 48, titulo: true));
		_detalleColumna.AddChild(EstiloPapel.Subrayado());

		Button principal;
		if (estado == Estado.Resuelto)
		{
			var r = Capitulos.Resultado(capitulo.Id);
			var resumen = EstiloPapel.Texto(capitulo.Resumen, 19);
			resumen.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			resumen.CustomMinimumSize = new Vector2(750, 0);
			_detalleColumna.AddChild(resumen);
			var fila = new HBoxContainer();
			fila.AddThemeConstantOverride("separation", 14);
			fila.AddChild(EstiloPapel.Texto("Mejor calificación:", 20));
			fila.AddChild(EstiloPapel.Texto(r.Rango, 30, titulo: true));
			var lupas = EstiloPapel.Lupas(r.Lupas, 5, 24);
			lupas.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			fila.AddChild(lupas);
			_detalleColumna.AddChild(fila);
			var tiempo = TimeSpan.FromSeconds(r.Tiempo);
			_detalleColumna.AddChild(EstiloPapel.Texto($"Completado: {r.Porcentaje} %  ·  Mejor tiempo: {(int)tiempo.TotalMinutes} min {tiempo.Seconds:00} s  ·  Al primer intento: {(r.PrimerIntento ? "sí" : "no")}", 17));
			_detalleColumna.AddChild(EstiloPapel.Texto("Repetirlo no cambia tu partida guardada.", 16, new Color(EstiloPapel.Tinta, 0.65f)));
			principal = EstiloPapel.Boton("Repetir caso", principal: true);
			principal.Pressed += () => GestorPartida.Instancia.IniciarCapitulo(capitulo.Id, rejugar: true);
		}
		else
		{
			bool omitido = Capitulos.Omitido(capitulo.Id);
			var texto = omitido
				? "Omitiste el tutorial. Puedes jugarlo cuando quieras; no cambia tu partida guardada."
				: capitulo.TieneEscena
					? "Tu caso actual. Si ya lo empezaste, usa «Continuar» en el menú principal."
					: "Este capítulo todavía está en preparación.";
			var l = EstiloPapel.Texto(texto, 19);
			l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			l.CustomMinimumSize = new Vector2(750, 0);
			_detalleColumna.AddChild(l);
			principal = EstiloPapel.Boton(omitido ? "Jugar tutorial" : capitulo.TieneEscena ? "Jugar desde el inicio" : "Ver presentación", principal: true);
			// Un tutorial omitido se juega como repetición: no pisa la partida del capítulo en curso
			principal.Pressed += () => GestorPartida.Instancia.IniciarCapitulo(capitulo.Id, rejugar: omitido);
		}
		var botones = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		botones.AddThemeConstantOverride("separation", 12);
		var cerrar = EstiloPapel.Boton("Volver");
		cerrar.Pressed += CerrarDetalle;
		botones.AddChild(cerrar);
		// Cinemática de inicio del caso, si tiene expediente
		string caso = capitulo.Numero.ToString("00");
		Button presentacion = null;
		if (IntroCaso.Disponible(caso))
		{
			presentacion = EstiloPapel.Boton("Ver presentación del caso");
			presentacion.Pressed += () => IntroCaso.Reproducir(this, caso, aviso: false);
			botones.AddChild(presentacion);
		}
		botones.AddChild(principal);
		_detalleColumna.AddChild(botones);
		UtilidadesUI.ConectarFocoConRaton(_detalleColumna);
		var orden = presentacion != null ? new Control[] { principal, presentacion, cerrar } : new Control[] { principal, cerrar };
		UtilidadesUI.EncadenarFoco(orden);
		for (int i = 0; i < orden.Length; i++)
		{
			var izquierda = orden[(i + 1) % orden.Length];
			var derecha = orden[(i + orden.Length - 1) % orden.Length];
			orden[i].FocusNeighborLeft = orden[i].GetPathTo(izquierda);
			orden[i].FocusNeighborRight = orden[i].GetPathTo(derecha);
		}
		_focoAlCerrarDetalle = carpeta;
		_detalle.Show();
		principal.GrabFocus();
	}

	private void CerrarDetalle()
	{
		_detalle.Hide();
		(_focoAlCerrarDetalle as Control)?.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (!Visible || !evento.IsActionPressed("ui_cancel")) return;
		GetViewport().SetInputAsHandled();
		if (_detalle.Visible) CerrarDetalle();
		else Cerrar();
	}
}
