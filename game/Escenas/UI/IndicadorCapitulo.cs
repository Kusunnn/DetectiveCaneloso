using Godot;
using System;

// Tira al pie de la hoja del expediente [TAB]: "Capítulo X · Fase: … · Pistas: 4/9". Sin decir qué pistas faltan.
// El HUD es del equipo (GDScript): solo se escucha su señal panel_cambiado, no se modifica.
public partial class IndicadorCapitulo : CanvasLayer
{
	// Lo asigna el caso
	public Func<string> Texto;

	private Node _hud;
	private PanelContainer _tira;
	private Label _etiqueta;

	public IndicadorCapitulo(Node hud)
	{
		_hud = hud;
		Layer = 11; // Encima del HUD (10)
	}

	public IndicadorCapitulo() { }

	public override void _Ready()
	{
		_tira = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		var estilo = new StyleBoxFlat { BgColor = EstiloPapel.Tinta, BorderColor = EstiloPapel.Ambar, ShadowColor = new Color(0, 0, 0, 0.4f), ShadowSize = 6 };
		estilo.BorderWidthLeft = 6;
		estilo.SetContentMarginAll(10);
		estilo.ContentMarginLeft = 18;
		estilo.ContentMarginRight = 18;
		_tira.AddThemeStyleboxOverride("panel", estilo);
		_etiqueta = EstiloPapel.Texto("", 18, EstiloPapel.Papel);
		_tira.AddChild(_etiqueta);
		AddChild(_tira);
		_hud?.Connect("panel_cambiado", Callable.From<bool, bool>(AlCambiarPanel));
	}

	private void AlCambiarPanel(bool abierto, bool expediente)
	{
		_tira.Visible = abierto && expediente && Texto != null;
		if (!_tira.Visible) return;
		_etiqueta.Text = Texto();
		_tira.ResetSize();
		// El expediente del HUD es una hoja de 1000×550 centrada en un lienzo de 1600×900 escalado; la tira va en su margen inferior
		Vector2 pantalla = _tira.GetViewportRect().Size;
		float escala = Mathf.Min(pantalla.X / 1600f, pantalla.Y / 900f);
		_tira.Scale = Vector2.One * Mathf.Max(escala, 0.6f);
		_tira.Position = new Vector2(pantalla.X / 2 - _tira.Size.X * _tira.Scale.X / 2, pantalla.Y / 2 + 213 * escala);
	}
}
