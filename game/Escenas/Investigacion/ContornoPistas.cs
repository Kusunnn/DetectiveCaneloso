using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Ayuda visual para encontrar las pistas: un contorno ámbar (contorno_pista.gdshader) en cada evidencia
// que todavía no se ha examinado. Solo se ve de cerca y desaparece al examinarla.
// Uso: añadir este nodo a la escena de un caso; marca todas las evidencias (ObjetoPista) de esa escena.
// Respeta Opciones > Pantalla > "Ayudas visuales del tutorial".
public partial class ContornoPistas : Node
{
	// Distancia (m) a la que empieza a verse el contorno; se desvanece en el último cuarto
	[Export] public float DistanciaMaxima = 4.5f;
	// Id de las pistas que no se marcan: secretas u opcionales
	[Export] public string[] Excluir = { "pluma" };
	// Solo se contornean las piezas de al menos esta fracción de la pieza más grande (no cada botón o carrete)
	[Export(PropertyHint.Range, "0,1,0.05")] public float FraccionPiezaMinima = 0.15f;

	// Dos pasadas: la máscara marca la pieza en el esténcil y el contorno se dibuja solo fuera de ella
	private const string RutaMascara = "res://Escenas/Investigacion/contorno_pista_mascara.gdshader";
	private const string RutaShader = "res://Escenas/Investigacion/contorno_pista.gdshader";
	// Clave que usa Jugador.Resaltar para devolver el material al dejar de apuntar
	private const string MetaOriginal = "overlay_original";

	private ShaderMaterial _material;
	private readonly Dictionary<ObjetoPista, List<GeometryInstance3D>> _piezas = new();
	private bool _activas;
	private double _espera;

	// Para que otros resaltados (la guía del tutorial) sepan que pueden ocupar su lugar
	public static bool EsContorno(Material material) =>
		material is ShaderMaterial s && s.Shader?.ResourcePath == RutaMascara;

	public override void _Ready()
	{
		var mascara = GD.Load<Shader>(RutaMascara);
		var contorno = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(RutaShader),
			// Todas las máscaras se dibujan antes que cualquier contorno, sin depender del orden por distancia
			RenderPriority = 1,
		};
		contorno.SetShaderParameter("distancia_max", DistanciaMaxima);
		_material = new ShaderMaterial { Shader = mascara, RenderPriority = -1, NextPass = contorno };
		// Al cuadro siguiente: la escena ya cargó sus pistas y, si se continúa, las ya examinadas
		Callable.From(Registrar).CallDeferred();
	}

	private void Registrar()
	{
		foreach (Node nodo in GetParent().FindChildren("*", "StaticBody3D", true, false))
		{
			if (nodo is not ObjetoPista pista || !pista.EsEvidencia || Excluir.Contains(pista.IdPista)) continue;
			_piezas[pista] = PiezasGrandes(pista);
			pista.MarcadaEncontrada += () => Quitar(pista);
		}
		_activas = AyudasActivas();
		Aplicar();
	}

	// Se repasa cada poco: si se cambia la opción de ayudas con el juego en marcha, o si la guía
	// del tutorial soltó una pista que había resaltado, el contorno vuelve a su sitio
	public override void _Process(double delta)
	{
		_espera -= delta;
		if (_espera > 0) return;
		_espera = 0.3;
		_activas = AyudasActivas();
		Aplicar();
	}

	private static bool AyudasActivas() => Configuracion.Instancia?.AyudasVisuales ?? true;

	private void Aplicar()
	{
		foreach (var (pista, piezas) in _piezas)
			foreach (var pieza in piezas)
				Poner(pieza, _activas && !pista.Encontrada);
	}

	private void Quitar(ObjetoPista pista)
	{
		if (!_piezas.Remove(pista, out var piezas)) return;
		foreach (var pieza in piezas) Poner(pieza, false);
	}

	// El contorno ocupa el material de superposición. Si otro resaltado lo tiene (el brillo al apuntar,
	// la guía del tutorial), se le deja; al apuntar, el contorno queda guardado para cuando deje de apuntar.
	private void Poner(GeometryInstance3D pieza, bool visible)
	{
		if (!IsInstanceValid(pieza)) return;
		bool apuntada = pieza.HasMeta(MetaOriginal) && pieza.MaterialOverlay != null && pieza.MaterialOverlay != _material;
		if (apuntada)
		{
			var guardado = pieza.GetMeta(MetaOriginal).As<Material>();
			if (visible && guardado == null) pieza.SetMeta(MetaOriginal, _material);
			else if (!visible && guardado == _material) pieza.SetMeta(MetaOriginal, default(Variant));
			return;
		}
		if (visible && pieza.MaterialOverlay == null) pieza.MaterialOverlay = _material;
		else if (!visible)
		{
			if (pieza.MaterialOverlay == _material) pieza.MaterialOverlay = null;
			// Que al volver a apuntarla y soltarla no se devuelva un contorno ya retirado
			if (pieza.HasMeta(MetaOriginal) && pieza.GetMeta(MetaOriginal).As<Material>() == _material) pieza.RemoveMeta(MetaOriginal);
		}
	}

	// Mallas de la pista (y las de "Visuales", como el reloj de la pared), solo las piezas grandes
	private List<GeometryInstance3D> PiezasGrandes(ObjetoPista pista)
	{
		var todas = new List<GeometryInstance3D>();
		void Agregar(Node nodo)
		{
			if (nodo is GeometryInstance3D g) todas.Add(g);
			foreach (Node hijo in nodo.FindChildren("*", "GeometryInstance3D", true, false))
				todas.Add((GeometryInstance3D)hijo);
		}
		foreach (Node hijo in pista.GetChildren()) Agregar(hijo);
		foreach (Node3D visual in pista.Visuales)
			if (visual != null) Agregar(visual);

		var mallas = todas.Where(g => g is not Label3D && g is not GpuParticles3D && g is not CpuParticles3D && !g.HasMeta("sin_resaltado"))
			.Distinct().ToList();
		if (mallas.Count == 0) return mallas;
		float mayor = mallas.Max(Volumen);
		var grandes = mallas.Where(g => Volumen(g) >= mayor * FraccionPiezaMinima).ToList();
		// El contorno crece desde el centro de cada pieza (ver contorno_pista.gdshader)
		foreach (var g in grandes) g.SetInstanceShaderParameter("centro", g.GetAabb().GetCenter());
		return grandes;
	}

	private static float Volumen(GeometryInstance3D g)
	{
		Aabb caja = g.GetAabb();
		Vector3 escala = g.GlobalTransform.Basis.Scale;
		return Mathf.Abs(caja.Size.X * escala.X * caja.Size.Y * escala.Y * Mathf.Max(caja.Size.Z * escala.Z, 0.005f));
	}
}
