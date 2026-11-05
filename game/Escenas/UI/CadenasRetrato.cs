using Godot;
using System;

// Cadenas con candados sobre el retrato de quien se interroga. Se dibuja por código: no necesita recursos.
public partial class CadenasRetrato : Control
{
	private static readonly Color Acero = new(0.2f, 0.2f, 0.22f);
	private static readonly Color Candado = new(0.72f, 0.56f, 0.12f);
	private const float Tam = 24f;

	private int _total, _rotos;
	private float _caida = -1f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public void Mostrar(int total, int rotos)
	{
		_total = total;
		_rotos = rotos;
		_caida = -1f;
		QueueRedraw();
	}

	// El siguiente candado cae y se aleja
	public void Romper()
	{
		if (_rotos >= _total) return;
		_caida = 0f;
		var tween = CreateTween();
		tween.TweenMethod(Callable.From<float>(t => { _caida = t; QueueRedraw(); }), 0f, 1f, 0.55f)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(() => { _rotos++; _caida = -1f; QueueRedraw(); }));
	}

	public override void _Draw()
	{
		if (_total <= 0) return;
		// La cadena se queda mientras quede algún candado
		if (_rotos < _total)
		{
			Eslabones(new Vector2(0, Size.Y * 0.15f), new Vector2(Size.X, Size.Y * 0.8f));
			Eslabones(new Vector2(Size.X, Size.Y * 0.15f), new Vector2(0, Size.Y * 0.8f));
		}
		float paso = Mathf.Min(Tam + 4f, Size.X / _total);
		float x = (Size.X - paso * _total) / 2f;
		for (int i = 0; i < _total; i++)
		{
			var pos = new Vector2(x + paso * i + paso / 2f, Size.Y - Tam * 0.6f);
			if (i < _rotos) DibujarCandado(pos, new Color(0.45f, 0.45f, 0.45f, 0.65f), true);
			else if (i == _rotos && _caida >= 0f) DibujarCandado(pos + new Vector2(0, _caida * 70f), new Color(Candado, 1f - _caida), true);
			else DibujarCandado(pos, Candado, false);
		}
	}

	private void Eslabones(Vector2 desde, Vector2 hasta)
	{
		float largo = desde.DistanceTo(hasta);
		int cuantos = Mathf.Max(2, (int)(largo / 13f));
		for (int i = 0; i < cuantos; i++)
		{
			var centro = desde.Lerp(hasta, (i + 0.5f) / cuantos);
			DrawArc(centro, 5f, 0f, Mathf.Tau, 10, Acero, 2f, true);
		}
	}

	private void DibujarCandado(Vector2 centro, Color color, bool abierto)
	{
		var cuerpo = new Rect2(centro - new Vector2(Tam / 2f, Tam * 0.1f), new Vector2(Tam, Tam * 0.65f));
		var arco = centro - new Vector2(abierto ? -Tam * 0.28f : 0f, Tam * 0.1f);
		DrawArc(arco, Tam * 0.28f, Mathf.Pi, Mathf.Tau, 12, color, 3f, true);
		DrawRect(cuerpo, color);
		DrawCircle(cuerpo.GetCenter(), 2.5f, new Color(0, 0, 0, color.A * 0.7f));
	}
}
