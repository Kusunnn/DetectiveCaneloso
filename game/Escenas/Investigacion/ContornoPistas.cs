using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Ayuda visual para encontrar las pistas: un contorno ámbar (contorno_pista.gdshader) en cada evidencia
// que todavía no se ha examinado. Solo se ve de cerca y desaparece al examinarla.
// Uso: añadir este nodo a la escena de un caso; marca todas las evidencias (ObjetoPista) de esa escena.
// Respeta Opciones > Pantalla > "Ayudas visuales del tutorial".
//
// Caso 1 (Instinto = true): con [instinto] brilla todo lo interactivo cercano por unos segundos: amarillo lo que
// falta tomar, gris lo ya revisado y azul lo que pide linterna, agacharse o una llave (con su ícono). También brillan
// los contenedores con una pista adentro. Cerca de una pista sin tomar suena un pulso suave.
public partial class ContornoPistas : Node
{
	// Distancia (m) a la que empieza a verse el contorno; se desvanece en el último cuarto
	[Export] public float DistanciaMaxima = 4.5f;
	// Id de las pistas que no se marcan: secretas u opcionales
	[Export] public string[] Excluir = { "pluma" };
	// Solo se contornean las piezas de al menos esta fracción de la pieza más grande (no cada botón o carrete)
	[Export(PropertyHint.Range, "0,1,0.05")] public float FraccionPiezaMinima = 0.15f;

	[ExportGroup("Instinto de detective (Caso 1)")]
	[Export] public bool Instinto = false;
	[Export] public float RadioInstinto = 8f;
	// Pistas que destraban mucho del caso: con el instinto se ven desde más lejos
	[Export] public string[] Lejanas = { "huellas_detenidas" };
	[Export] public float RadioLejano = 16f;
	[Export] public float DuracionInstinto = 4f;
	[Export] public float RecargaInstinto = 10f;
	[Export] public float RadioPulso = 3f;

	// Dos pasadas: la máscara marca la pieza en el esténcil y el contorno se dibuja solo fuera de ella
	private const string RutaMascara = "res://Escenas/Investigacion/contorno_pista_mascara.gdshader";
	private const string RutaShader = "res://Escenas/Investigacion/contorno_pista.gdshader";
	// Clave que usa Jugador.Resaltar para devolver el material al dejar de apuntar
	private const string MetaOriginal = "overlay_original";
	private static readonly Color Amarillo = new(0.937f, 0.792f, 0.031f), GrisRevisado = new(0.62f, 0.62f, 0.62f), Azul = new(0.3f, 0.62f, 1f);

	private enum Estado { Nada, Falta, Revisado, Requiere }

	private ShaderMaterial _material, _amarillo, _gris, _azul, _amarilloLejos, _azulLejos;
	private readonly HashSet<Material> _propios = new();
	private readonly Dictionary<ObjetoPista, List<GeometryInstance3D>> _piezas = new();
	// Todo lo que brilla con el instinto (las pistas ya examinadas también, en gris)
	private readonly Dictionary<Node3D, List<GeometryInstance3D>> _interactivos = new();
	private readonly Dictionary<Node3D, Label3D> _iconos = new();
	private bool _activas;
	private double _espera, _instinto, _recarga, _pulso;
	private Jugador _jugador;

	public bool InstintoActivo => _instinto > 0;
	// Para las pruebas: cuántas piezas brillan de cada color y si hay ícono de requisito
	public Godot.Collections.Dictionary Brillando()
	{
		int Cuenta(Material m) => _interactivos.Values.Concat(_piezas.Values).SelectMany(p => p).Distinct().Count(g => IsInstanceValid(g) && g.MaterialOverlay == m);
		return new Godot.Collections.Dictionary
		{
			{ "amarillo", Cuenta(_amarillo) + Cuenta(_amarilloLejos) }, { "gris", Cuenta(_gris) }, { "azul", Cuenta(_azul) + Cuenta(_azulLejos) },
			{ "ambar", Cuenta(_material) }, { "iconos", _iconos.Count }, { "contenedores", _interactivos.Keys.Count(n => n is Contenedor && EstadoDe(n) != Estado.Nada) },
		};
	}
	public double RecargaRestante => _recarga;

	// Para que otros resaltados (la guía del tutorial) sepan que pueden ocupar su lugar
	public static bool EsContorno(Material material) =>
		material is ShaderMaterial s && s.Shader?.ResourcePath == RutaMascara;

	private static string Dificultad => Configuracion.Instancia?.Dificultad ?? "detective";

	public override void _Ready()
	{
		_material = Crear(Amarillo, DistanciaMaxima);
		_amarillo = Crear(Amarillo, RadioInstinto);
		_gris = Crear(GrisRevisado, RadioInstinto);
		_azul = Crear(Azul, RadioInstinto);
		_amarilloLejos = Crear(Amarillo, RadioLejano);
		_azulLejos = Crear(Azul, RadioLejano);
		// Al cuadro siguiente: la escena ya cargó sus pistas y, si se continúa, las ya examinadas
		Callable.From(Registrar).CallDeferred();
	}

	private ShaderMaterial Crear(Color color, float distancia)
	{
		var contorno = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(RutaShader),
			// Todas las máscaras se dibujan antes que cualquier contorno, sin depender del orden por distancia
			RenderPriority = 1,
		};
		contorno.SetShaderParameter("distancia_max", distancia);
		contorno.SetShaderParameter("color", color);
		var material = new ShaderMaterial { Shader = GD.Load<Shader>(RutaMascara), RenderPriority = -1, NextPass = contorno };
		_propios.Add(material);
		return material;
	}

	private void Registrar()
	{
		_jugador = GetParent().GetNodeOrNull<Jugador>("Jugador");
		foreach (Node nodo in GetParent().FindChildren("*", "StaticBody3D", true, false))
		{
			if (nodo is ObjetoPista pista && pista.EsEvidencia && !Excluir.Contains(pista.IdPista))
			{
				var piezas = PiezasGrandes(pista);
				_interactivos[pista] = piezas;
				if (pista.Encontrada) continue;
				_piezas[pista] = piezas;
				pista.MarcadaEncontrada += () => Quitar(pista);
			}
			else if (Instinto && (nodo is Contenedor || nodo is Puerta))
				_interactivos[(Node3D)nodo] = PiezasGrandes((Node3D)nodo);
		}
		_activas = AyudasActivas();
		Aplicar();
	}

	// Se repasa cada poco: si se cambia la opción de ayudas con el juego en marcha, o si la guía
	// del tutorial soltó una pista que había resaltado, el contorno vuelve a su sitio
	public override void _Process(double delta)
	{
		if (_recarga > 0) _recarga -= delta;
		if (_instinto > 0)
		{
			_instinto -= delta;
			if (_instinto <= 0) { _recarga = Dificultad == "historia" ? 0 : RecargaInstinto; Aplicar(); }
		}
		if (Instinto) Pulso(delta);
		_espera -= delta;
		if (_espera > 0) return;
		_espera = 0.3;
		_activas = AyudasActivas();
		Aplicar();
	}

	// En Experto no hay contorno ámbar ni instinto
	private bool AyudasActivas() => (Configuracion.Instancia?.AyudasVisuales ?? true) && !(Instinto && Dificultad == "experto");

	public override void _UnhandledInput(InputEvent evento)
	{
		if (!Instinto || !evento.IsActionPressed("instinto") || GetTree().Paused) return;
		GetViewport().SetInputAsHandled();
		Activar();
	}

	// true si se activó. Si no, Caneloso dice por qué.
	public bool Activar()
	{
		if (Dificultad == "experto")
		{
			_jugador?.MostrarAviso("En Experto no hay instinto de detective.", 2.5);
			return false;
		}
		if (_instinto > 0) return false;
		if (_recarga > 0)
		{
			_jugador?.MostrarAviso($"El instinto se recupera en {Mathf.CeilToInt(_recarga)} s", 1.5);
			return false;
		}
		_instinto = DuracionInstinto;
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Pista);
		Aplicar();
		return true;
	}

	private void Aplicar()
	{
		if (_instinto > 0)
		{
			foreach (var (pista, piezas) in _piezas)
				if (!_interactivos.ContainsKey(pista)) foreach (var pieza in piezas) Poner(pieza, null);
			foreach (var (nodo, piezas) in _interactivos)
			{
				var estado = EstadoDe(nodo);
				bool lejos = nodo is ObjetoPista p && Lejanas.Contains(p.IdPista);
				Material material = estado switch
				{
					Estado.Falta => lejos ? _amarilloLejos : _amarillo,
					Estado.Requiere => lejos ? _azulLejos : _azul,
					Estado.Revisado => _gris,
					_ => null,
				};
				foreach (var pieza in piezas) Poner(pieza, material);
				Icono(nodo, estado == Estado.Requiere ? TextoRequisito(nodo) : null);
			}
			return;
		}
		foreach (var (nodo, piezas) in _interactivos)
		{
			if (nodo is ObjetoPista pista && _piezas.ContainsKey(pista)) continue;
			foreach (var pieza in piezas) Poner(pieza, null);
			Icono(nodo, null);
		}
		foreach (var (pista, piezas) in _piezas)
			foreach (var pieza in piezas)
				Poner(pieza, _activas && !pista.Encontrada ? _material : null);
	}

	private Estado EstadoDe(Node3D nodo)
	{
		switch (nodo)
		{
			case ObjetoPista pista:
				if (pista.Encontrada) return Estado.Revisado;
				if (pista.CollisionLayer == 0) return Estado.Nada; // Dentro de algo cerrado: brilla el contenedor
				if ((pista.RequiereLinterna && !(_jugador?.LinternaEncendida ?? false)) || (pista.SoloAgachado && !(_jugador?.Agachado ?? false))) return Estado.Requiere;
				return Estado.Falta;
			case Contenedor contenedor:
				bool daAlgo = !string.IsNullOrEmpty(contenedor.ObjetoQueDa) && !contenedor.Revisado;
				bool guarda = !contenedor.EstaAbierto && _interactivos.Keys.OfType<ObjetoPista>().Any(p => !p.Encontrada && p.CollisionLayer == 0 && p.GlobalPosition.DistanceTo(contenedor.GlobalPosition) < 2f);
				if (!daAlgo && !guarda) return contenedor.Revisado ? Estado.Revisado : Estado.Nada;
				return !string.IsNullOrEmpty(contenedor.RequiereObjeto) && !(_jugador?.TieneObjeto(contenedor.RequiereObjeto) ?? false) ? Estado.Requiere : Estado.Falta;
			case Puerta puerta:
				return puerta.Bloqueada ? Estado.Requiere : Estado.Nada;
		}
		return Estado.Nada;
	}

	private string TextoRequisito(Node3D nodo) => nodo switch
	{
		ObjetoPista p when p.RequiereLinterna && !(_jugador?.LinternaEncendida ?? false) => $"Linterna [{Teclas.Etiqueta("linterna")}]",
		ObjetoPista p when p.SoloAgachado => $"Agáchate [{Teclas.Etiqueta("agacharse")}]",
		_ => "Con llave",
	};

	private void Icono(Node3D nodo, string texto)
	{
		if (texto == null)
		{
			if (_iconos.Remove(nodo, out var viejo) && IsInstanceValid(viejo)) viejo.QueueFree();
			return;
		}
		if (_iconos.TryGetValue(nodo, out var icono)) { icono.Text = texto; return; }
		icono = new Label3D
		{
			Text = texto, FontSize = 40, PixelSize = 0.004f, Modulate = Azul, OutlineSize = 10,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true, Position = new Vector3(0, 0.5f, 0),
		};
		nodo.AddChild(icono);
		_iconos[nodo] = icono;
	}

	// Un pulso suave cada pocos segundos si hay una pista sin tomar muy cerca
	private void Pulso(double delta)
	{
		_pulso -= delta;
		if (_pulso > 0 || _jugador == null || Dificultad == "experto") return;
		_pulso = 0.5;
		var cerca = _piezas.Keys.Any(p => !p.Encontrada && p.CollisionLayer != 0 && p.GlobalPosition.DistanceTo(_jugador.GlobalPosition) < RadioPulso);
		if (!cerca) return;
		_pulso = 4.0;
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Hover);
	}

	private void Quitar(ObjetoPista pista)
	{
		if (!_piezas.Remove(pista, out var piezas)) return;
		foreach (var pieza in piezas) Poner(pieza, null);
		if (_instinto > 0) Aplicar();
	}

	// El contorno ocupa el material de superposición. Si otro resaltado lo tiene (el brillo al apuntar,
	// la guía del tutorial), se le deja; al apuntar, el contorno queda guardado para cuando deje de apuntar.
	// quiero: el contorno que debe tener (null = ninguno de los nuestros)
	private void Poner(GeometryInstance3D pieza, Material quiero)
	{
		if (!IsInstanceValid(pieza)) return;
		bool EsPropio(Material m) => m != null && _propios.Contains(m);
		bool apuntada = pieza.HasMeta(MetaOriginal) && pieza.MaterialOverlay != null && !EsPropio(pieza.MaterialOverlay);
		if (apuntada)
		{
			var guardado = pieza.GetMeta(MetaOriginal).As<Material>();
			if (quiero != null && (guardado == null || EsPropio(guardado))) pieza.SetMeta(MetaOriginal, quiero);
			else if (quiero == null && EsPropio(guardado)) pieza.SetMeta(MetaOriginal, default(Variant));
			return;
		}
		if (quiero != null && (pieza.MaterialOverlay == null || EsPropio(pieza.MaterialOverlay))) pieza.MaterialOverlay = quiero;
		else if (quiero == null)
		{
			if (EsPropio(pieza.MaterialOverlay)) pieza.MaterialOverlay = null;
			// Que al volver a apuntarla y soltarla no se devuelva un contorno ya retirado
			if (pieza.HasMeta(MetaOriginal) && EsPropio(pieza.GetMeta(MetaOriginal).As<Material>())) pieza.RemoveMeta(MetaOriginal);
		}
	}

	// Mallas de la pista (y las de "Visuales", como el reloj de la pared), solo las piezas grandes
	private List<GeometryInstance3D> PiezasGrandes(Node3D raiz)
	{
		var todas = new List<GeometryInstance3D>();
		void Agregar(Node nodo)
		{
			if (nodo is GeometryInstance3D g) todas.Add(g);
			foreach (Node hijo in nodo.FindChildren("*", "GeometryInstance3D", true, false))
				todas.Add((GeometryInstance3D)hijo);
		}
		foreach (Node hijo in raiz.GetChildren()) Agregar(hijo);
		if (raiz is ObjetoPista pista)
			foreach (Node3D visual in pista.Visuales)
				if (visual != null) Agregar(visual);
		if (raiz is Contenedor contenedor && contenedor.Parte != null) Agregar(contenedor.Parte);

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
