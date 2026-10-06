using Godot;
using System;
using System.Collections.Generic;

// Un paso de la guía: qué tecla enseñar, junto a qué objeto o elemento del HUD, y cómo destacarlo.
public class PasoGuia
{
	public string Accion;                       // "interactuar", "libreta", "mover"... (null = sin ícono)
	public string Texto = "";                   // "Inspeccionar", "Abre la libreta"...
	public Node3D Objetivo;                     // Ancla en el mundo (null = abajo al centro)
	public float Altura = 0.35f;                // Metros sobre el objetivo donde va el ícono
	public Func<Vector2, Rect2> ZonaHud;        // Marco sobre un elemento del HUD (coordenadas 1600x900 del HUD)
	public bool Foco;                           // Oscurece el resto de la pantalla
	public bool Contorno = true;                // Brillo amarillo en el objetivo
}

// Guía visual del tutorial con el estilo del HUD (papel, máquina de escribir, ámbar):
// ícono de tecla que pulsa junto al objeto, foco tipo spotlight, contorno amarillo, marco sobre el HUD,
// flecha en el borde hacia el objetivo y palomita al completar. No sabe nada del caso:
// quien la usa decide qué paso mostrar y cuándo se completa.
public partial class GuiaTutorial : Node
{
	private static readonly Color Ambar = new Color("efca08");
	private static readonly Color Tinta = new Color("20272b");
	private static readonly Color Papel = new Color(0.84f, 0.81f, 0.72f);

	// Mientras devuelva true (diálogo, tablero, menú abiertos) no se dibuja nada encima
	public Func<bool> Pausada = () => false;
	// 0 normal · 2 fuerza el foco · 3 además señala el destino aunque esté a la vista
	public int Insistencia { get; set; }

	public PasoGuia PasoActual { get; private set; }
	// Texto del paso que se muestra ("" si no hay): lo pueden leer las pruebas y scripts GDScript
	public string TextoActual => PasoActual?.Texto ?? "";
	public bool Activa => Configuracion.Instancia?.AyudasVisuales ?? true;

	private CanvasLayer _capaFoco;
	private CanvasLayer _capaGuia;
	private ColorRect _foco;
	private ShaderMaterial _materialFoco;
	private Control _lienzo;
	private Font _mono;
	private Font _titulos;
	private ShaderMaterial _contorno;
	private readonly List<GeometryInstance3D> _contornados = new();
	private Node3D _destino;
	private string _nombreDestino = "";
	private float _tiempo;
	private float _fuerzaFoco;
	private float _palomita = -1f;
	private Vector2 _posPalomita;
	private Vector2 _ultimaPosicion;

	public override void _Ready()
	{
		_mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var display = new FontVariation { BaseFont = GD.Load<Font>("res://hud/fonts/BigShouldersDisplay.ttf") };
		display.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), 800 } };
		_titulos = display;

		// Foco: entre el mundo y el HUD (el HUD es la capa 10)
		_capaFoco = new CanvasLayer { Layer = 5 };
		AddChild(_capaFoco);
		_materialFoco = new ShaderMaterial { Shader = new Shader { Code = CodigoFoco } };
		_foco = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = _materialFoco, Color = Colors.White };
		_foco.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_capaFoco.AddChild(_foco);

		// Íconos, marcos y flecha: encima del HUD para poder señalarlo
		_capaGuia = new CanvasLayer { Layer = 11 };
		AddChild(_capaGuia);
		_lienzo = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_lienzo.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_lienzo.Draw += Dibujar;
		_capaGuia.AddChild(_lienzo);

		_contorno = new ShaderMaterial { Shader = GD.Load<Shader>("res://Escenas/Investigacion/resaltado.gdshader") };
		_contorno.SetShaderParameter("intensidad", 0.7f);
		_contorno.SetShaderParameter("relleno", 0.1f);
		_contorno.SetShaderParameter("velocidad_pulso", 3.0f);
	}

	// ---------- API ----------

	public void Mostrar(PasoGuia paso)
	{
		QuitarContorno();
		PasoActual = paso;
		if (paso?.Objetivo != null && paso.Contorno && Activa) PonerContorno(paso.Objetivo);
	}

	// Paso cumplido: palomita y sonido de éxito
	public void Completar()
	{
		if (PasoActual != null && Activa)
		{
			_palomita = 0f;
			_posPalomita = _ultimaPosicion;
			Sonidos.ReproducirUI(this, Sonidos.Tipo.Objeto, -6f);
		}
		QuitarContorno();
		PasoActual = null;
		Insistencia = 0;
	}

	public void Limpiar()
	{
		QuitarContorno();
		PasoActual = null;
		_destino = null;
		Insistencia = 0;
	}

	// Flecha en el borde de la pantalla mientras el destino esté fuera de vista (null = sin destino)
	public void FijarDestino(Node3D destino, string nombre = "")
	{
		_destino = destino;
		_nombreDestino = nombre;
	}

	// Zonas del HUD (hud.gd dibuja en 1600x900 escalado; "w" y "h" son el ancho y alto virtuales)
	public static Rect2 ZonaPistas(Vector2 tam) => new Rect2(tam.X - 405, tam.Y - 220, 355, 180);
	public static Rect2 ZonaObjetivo(Vector2 tam) => new Rect2(tam.X - 420, 42, 378, 137);

	// ---------- Interior ----------

	public override void _Process(double delta)
	{
		_tiempo += (float)delta;
		if (_palomita >= 0f) _palomita += (float)delta;
		if (_palomita > 1.1f) _palomita = -1f;

		bool visible = Activa && !Pausada();
		_capaGuia.Visible = visible;
		bool conFoco = visible && PasoActual != null && (PasoActual.Foco || Insistencia >= 2) && PasoActual.Objetivo != null;
		_fuerzaFoco = Mathf.MoveToward(_fuerzaFoco, conFoco ? 0.5f : 0f, (float)delta * 1.5f);
		_capaFoco.Visible = _fuerzaFoco > 0.01f;
		if (_capaFoco.Visible) ActualizarFoco();
		_lienzo.QueueRedraw();
	}

	private Camera3D Camara => GetViewport().GetCamera3D();

	// Posición en pantalla del ancla; null si queda detrás de la cámara
	private Vector2? EnPantalla(Node3D nodo, float altura)
	{
		var camara = Camara;
		if (camara == null || nodo == null || !IsInstanceValid(nodo)) return null;
		Vector3 punto = nodo.GlobalPosition + Vector3.Up * altura;
		if (camara.IsPositionBehind(punto)) return null;
		return camara.UnprojectPosition(punto);
	}

	private void ActualizarFoco()
	{
		Vector2 tam = GetViewport().GetVisibleRect().Size;
		Vector2? centro = PasoActual?.Objetivo != null ? EnPantalla(PasoActual.Objetivo, 0.1f) : null;
		float distancia = Camara != null && PasoActual?.Objetivo != null ? Camara.GlobalPosition.DistanceTo(PasoActual.Objetivo.GlobalPosition) : 4f;
		float radio = Mathf.Clamp(380f / Mathf.Max(distancia, 0.5f), 90f, 320f) * (tam.Y / 1080f);
		_materialFoco.SetShaderParameter("centro", centro ?? tam / 2f);
		_materialFoco.SetShaderParameter("tam", tam);
		_materialFoco.SetShaderParameter("radio", centro.HasValue ? radio : 0f);
		_materialFoco.SetShaderParameter("suavidad", radio * 0.6f);
		// Si el objetivo no está a la vista no se oscurece nada (manda la flecha)
		_materialFoco.SetShaderParameter("fuerza", centro.HasValue ? _fuerzaFoco : 0f);
	}

	private void Dibujar()
	{
		Vector2 tam = _lienzo.Size;
		float escalaHud = Mathf.Min(tam.X / 1600f, tam.Y / 900f);
		float pulso = 1f + 0.05f * Mathf.Sin(_tiempo * 5f);

		if (PasoActual != null)
		{
			// Sin objeto: abajo al centro, cerca del borde, para no tapar NPC ni pistas
			Vector2 posicion = new Vector2(tam.X / 2f, tam.Y - 110f * escalaHud);
			if (PasoActual.ZonaHud != null)
			{
				// Marco animado sobre un elemento del HUD, con la indicación al lado
				Rect2 zona = PasoActual.ZonaHud(tam / escalaHud);
				zona = new Rect2(zona.Position * escalaHud, zona.Size * escalaHud).Grow(8f + 4f * Mathf.Sin(_tiempo * 4f));
				_lienzo.DrawRect(zona, new Color(Ambar, 0.95f), false, 3f);
				Vector2 punta = new Vector2(zona.Position.X - 14f, zona.GetCenter().Y);
				_lienzo.DrawColoredPolygon(new[] { punta, punta + new Vector2(-22, -12), punta + new Vector2(-22, 12) }, Ambar);
				posicion = new Vector2(zona.Position.X - 30f - AnchoTarjeta(PasoActual) / 2f, zona.GetCenter().Y);
			}
			else if (PasoActual.Objetivo != null)
			{
				Vector2? ancla = EnPantalla(PasoActual.Objetivo, PasoActual.Altura);
				if (ancla.HasValue && new Rect2(Vector2.Zero, tam).Grow(-40f).HasPoint(ancla.Value))
					posicion = ancla.Value + new Vector2(0, -40f);
			}
			posicion.X = Mathf.Clamp(posicion.X, AnchoTarjeta(PasoActual) / 2f + 20f, tam.X - AnchoTarjeta(PasoActual) / 2f - 20f);
			posicion.Y = Mathf.Clamp(posicion.Y, 120f, tam.Y - 90f * escalaHud);
			_ultimaPosicion = posicion;
			DibujarTarjeta(PasoActual, posicion, pulso);
		}

		DibujarFlecha(tam);

		if (_palomita >= 0f)
		{
			float t = _palomita / 1.1f;
			float escala = Mathf.Min(1f, _palomita * 5f);
			float alfa = 1f - Mathf.Max(0f, (t - 0.6f) / 0.4f);
			Vector2 c = _posPalomita + new Vector2(0, -30f * t);
			_lienzo.DrawCircle(c, 26f * escala, new Color(Ambar, alfa));
			_lienzo.DrawPolyline(new[] { c + new Vector2(-12, 0) * escala, c + new Vector2(-3, 10) * escala, c + new Vector2(13, -10) * escala },
				new Color(Tinta, alfa), 5f, true);
		}
	}

	private float AnchoTarjeta(PasoGuia paso)
	{
		float texto = _mono.GetStringSize(paso.Texto, HorizontalAlignment.Left, -1, 20).X;
		float tecla = paso.Accion != null ? AnchoTecla(Teclas.Etiqueta(paso.Accion)) + 14f : 0f;
		return texto + tecla + 36f;
	}

	private float AnchoTecla(string etiqueta) =>
		Teclas.EsBotonRedondo(etiqueta) ? 38f : Mathf.Max(38f, _titulos.GetStringSize(etiqueta, HorizontalAlignment.Left, -1, 24).X + 20f);

	// Tarjeta de papel: [tecla] + texto, centrada en "centro"
	private void DibujarTarjeta(PasoGuia paso, Vector2 centro, float pulso)
	{
		float ancho = AnchoTarjeta(paso);
		var tarjeta = new Rect2(centro - new Vector2(ancho / 2f, 26f), new Vector2(ancho, 52f));
		_lienzo.DrawRect(new Rect2(tarjeta.Position + new Vector2(4, 6), tarjeta.Size), new Color(0, 0, 0, 0.35f));
		_lienzo.DrawRect(tarjeta, Papel);
		_lienzo.DrawRect(new Rect2(tarjeta.Position + new Vector2(0, tarjeta.Size.Y - 4), new Vector2(ancho, 4)), Ambar);
		float x = tarjeta.Position.X + 16f;
		if (paso.Accion != null)
		{
			string etiqueta = Teclas.Etiqueta(paso.Accion);
			float w = AnchoTecla(etiqueta);
			var tecla = new Rect2(new Vector2(x, centro.Y - 19f), new Vector2(w, 38f));
			Vector2 c = tecla.GetCenter();
			// La tecla pulsa hasta que el jugador hace la acción
			Rect2 animada = new Rect2(c - tecla.Size * pulso / 2f, tecla.Size * pulso);
			_lienzo.DrawRect(animada.Grow(5f), new Color(Ambar, 0.35f + 0.25f * Mathf.Sin(_tiempo * 5f)));
			if (Teclas.EsBotonRedondo(etiqueta))
			{
				_lienzo.DrawCircle(c, 19f * pulso, Tinta);
				_lienzo.DrawArc(c, 19f * pulso, 0, Mathf.Tau, 32, Ambar, 2f, true);
			}
			else
			{
				_lienzo.DrawRect(animada, Tinta);
				_lienzo.DrawRect(animada, Ambar, false, 2f);
			}
			float anchoEtiqueta = _titulos.GetStringSize(etiqueta, HorizontalAlignment.Left, -1, 24).X;
			_lienzo.DrawString(_titulos, new Vector2(c.X - anchoEtiqueta / 2f, c.Y + 9f), etiqueta, HorizontalAlignment.Left, -1, 24, Papel);
			x += w + 14f;
		}
		_lienzo.DrawString(_mono, new Vector2(x, centro.Y + 7f), paso.Texto, HorizontalAlignment.Left, -1, 20, Tinta);
	}

	// Flecha ámbar en el borde cuando el destino no se ve; desaparece al llegar
	private void DibujarFlecha(Vector2 tam)
	{
		var camara = Camara;
		if (_destino == null || !IsInstanceValid(_destino) || camara == null) return;
		float distancia = camara.GlobalPosition.DistanceTo(_destino.GlobalPosition);
		if (distancia < 2.5f) return;
		Vector3 punto = _destino.GlobalPosition + Vector3.Up * 0.8f;
		bool detras = camara.IsPositionBehind(punto);
		Vector2 p = camara.UnprojectPosition(punto);
		// Zona de la flecha: fuera de las bandas del HUD (arriba: objetivo; abajo: pistas)
		float e = Mathf.Min(tam.X / 1600f, tam.Y / 900f);
		var interior = new Rect2(70f, 200f * e, tam.X - 140f, tam.Y - 200f * e - 250f * e);
		if (!detras && interior.HasPoint(p) && Insistencia < 3) return;

		Vector2 centro = interior.GetCenter();
		Vector2 dir = (p - centro).Normalized();
		if (detras)
		{
			// Detrás del jugador: la flecha va al lado (izquierda o derecha) por donde conviene girar
			Vector3 local = camara.GlobalTransform.Basis.Inverse() * (punto - camara.GlobalPosition);
			dir = new Vector2(local.X >= 0f ? 1f : -1f, 0f);
		}
		if (dir == Vector2.Zero) dir = Vector2.Down;
		// Punto del borde (rectángulo interior) en esa dirección
		float t = Mathf.Min(Mathf.Abs((interior.Size.X / 2f) / Mathf.Max(Mathf.Abs(dir.X), 0.001f)),
			Mathf.Abs((interior.Size.Y / 2f) / Mathf.Max(Mathf.Abs(dir.Y), 0.001f)));
		Vector2 borde = !detras && interior.HasPoint(p) ? p + new Vector2(0, -50f) : centro + dir * t;
		if (!detras && interior.HasPoint(p)) dir = Vector2.Down;
		float vaiven = 6f * Mathf.Sin(_tiempo * 6f);
		Vector2 punta = borde + dir * vaiven;
		Vector2 normal = new Vector2(-dir.Y, dir.X);
		_lienzo.DrawColoredPolygon(new[] { punta + dir * 18f, punta - dir * 12f + normal * 14f, punta - dir * 12f - normal * 14f }, Ambar);
		string etiqueta = string.IsNullOrEmpty(_nombreDestino) ? $"{distancia:0} m" : $"{_nombreDestino} · {distancia:0} m";
		float ancho = _mono.GetStringSize(etiqueta, HorizontalAlignment.Left, -1, 16).X + 16f;
		Vector2 pos = punta - dir * 46f - new Vector2(ancho / 2f, 14f);
		pos.X = Mathf.Clamp(pos.X, 10f, tam.X - ancho - 10f);
		pos.Y = Mathf.Clamp(pos.Y, 10f, tam.Y - 38f);
		_lienzo.DrawRect(new Rect2(pos, new Vector2(ancho, 28f)), new Color(Papel, 0.92f));
		_lienzo.DrawString(_mono, pos + new Vector2(8f, 20f), etiqueta, HorizontalAlignment.Left, -1, 16, Tinta);
	}

	private void PonerContorno(Node3D objetivo)
	{
		var geometrias = new List<Node>(objetivo.FindChildren("*", "GeometryInstance3D", true, false));
		if (objetivo is GeometryInstance3D propio) geometrias.Add(propio);
		if (objetivo is ObjetoPista pista)
			foreach (Node3D visual in pista.Visuales)
				if (visual != null) { geometrias.Add(visual); geometrias.AddRange(visual.FindChildren("*", "GeometryInstance3D", true, false)); }
		foreach (Node n in geometrias)
		{
			if (n is not GeometryInstance3D g || n is Label3D || n is GpuParticles3D) continue;
			if (g.MaterialOverlay != null) continue; // No pisa el brillo de "apuntando" ni el arte
			g.MaterialOverlay = _contorno;
			_contornados.Add(g);
		}
	}

	private void QuitarContorno()
	{
		foreach (var g in _contornados)
		{
			if (!IsInstanceValid(g)) continue;
			if (g.MaterialOverlay == _contorno) g.MaterialOverlay = null;
			// Si el jugador lo estaba apuntando, que al soltarlo no vuelva el contorno de la guía
			if (g.HasMeta("overlay_original") && g.GetMeta("overlay_original").As<Material>() == _contorno) g.RemoveMeta("overlay_original");
		}
		_contornados.Clear();
	}

	public override void _ExitTree() => QuitarContorno();

	private const string CodigoFoco = @"
shader_type canvas_item;
uniform vec2 centro;
uniform vec2 tam = vec2(1920.0, 1080.0);
uniform float radio = 150.0;
uniform float suavidad = 90.0;
uniform float fuerza = 0.5;
void fragment() {
	float d = distance(SCREEN_UV * tam, centro);
	COLOR = vec4(0.0, 0.0, 0.0, smoothstep(radio, radio + suavidad, d) * fuerza);
}";
}
