using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Galería del menú de inicio: visor para ver de lejos y girar los modelos de personajes, pistas y escenarios.
//   Ratón: arrastrar con clic izquierdo gira, la rueda acerca o aleja, clic derecho (o central) desplaza.
//   Teclado: WASD gira, +/- acerca o aleja, R vuelve a la vista inicial, Esc regresa al menú.
//   Mando: palanca derecha gira, gatillos acercan o alejan.
// Solo hay un modelo cargado a la vez: al cambiar se libera el anterior, y al cerrar se libera todo.
public partial class Galeria : Control
{
	[Signal] public delegate void CerradoEventHandler();

	private enum Categoria { Personajes, Pistas, Escenarios }

	// Personaje: escena del modelo, escala y animación de reposo tal como aparecen en el juego.
	private record Entrada(string Nombre, string Ruta, string Grupo = null, float Escala = 1f, Vector3 Desfase = default,
		string Animacion = null, string[] Variantes = null, bool Somnoliento = false, bool SinAnimador = false, bool Ual = false, string[] Encuadre = null, bool SinTecho = false);

	private static readonly Entrada[] Personajes =
	{
		new("Detective Caneloso", "res://Assets/Models/Caneloso/Archivo/player.tscn", SinAnimador: true),
		new("Porky", "res://Assets/Models/Personajes/porky.glb", Escala: 6.67f, Somnoliento: true, Ual: true),
		new("Hecdog", "res://Assets/Models/poli/Poli/Neutral Idle.fbx", Escala: 380f, Desfase: new Vector3(0, 0.004f, 0), Ual: true),
		new("Eriz", "res://Assets/Models/Personajes/eriz.glb", Escala: 4.77f, Desfase: new Vector3(0, 0, 0.0076f), Ual: true),
		new("Juan Carlos", "res://Assets/Models/Personajes/lala.glb", Escala: 6.3707f, Desfase: new Vector3(0, 0, 0.5447f), Ual: true),
		new("Mimita", "res://Assets/Models/Personajes/mimita_rig.glb", Escala: 9.7403f, Desfase: new Vector3(0, 0, -0.0029f), Animacion: "Reposo", Variantes: new[] { "ReposoMirar", "ReposoPeso" }),
		new("Tony", "res://Assets/Models/Tony/Tony.tscn", Escala: 0.6503f, Desfase: new Vector3(-0.0052f, 0.001f, 0.0228f), Animacion: "global/InacTony"),
		new("Aya", "res://Assets/Models/Aya/Ayas.tscn", Escala: 0.0359f, Desfase: new Vector3(-0.0045f, 0.0051f, -0.9514f), Animacion: "Inaya"),
		new("Miawrio", "res://Assets/Models/Miaurio/miaurio.tscn", Escala: 0.0046f, Desfase: new Vector3(-1.5082f, 0.0025f, -0.4715f), Animacion: "global/Inactivo"),
	};

	// Personajes sin animaciones propias: se animan con la biblioteca de Quaternius, como Porky y el comisario en el juego.
	private static readonly (string Etiqueta, string Nombre)[] AnimacionesUal =
	{
		("Reposo", "Idle"), ("Brazos cruzados", "Idle_FoldArms"), ("Hablando", "Idle_Talking"),
		("Al teléfono", "Idle_TalkingPhone"), ("Negar", "Idle_No"), ("Asentir", "Yes"),
		("Caminar", "Walk"), ("Caminar formal", "Walk_Formal"), ("Trotar", "Jog_Fwd"), ("Correr", "Sprint"),
		("Agachado", "Crouch_Idle"), ("Sentado", "Sitting_Idle"), ("Sentado hablando", "Sitting_Talking"),
		("Comer", "Consume"), ("Con linterna", "Idle_Torch"), ("Con farol", "Idle_Lantern"),
		("Recoger", "PickUp_Table"), ("Interactuar", "Interact"), ("Abrir cofre", "Chest_Open"),
		("Lanzar", "OverhandThrow"), ("Empujar", "Push"), ("Golpe recibido", "Hit_Chest"),
		("Caer", "Death01"), ("Bailar", "Dance"),
	};

	// Nombres legibles de las animaciones propias de cada modelo
	private static readonly Dictionary<string, string> NombresAnimacion = new()
	{
		["anim_reposo"] = "Reposo", ["anim_caminar"] = "Caminar", ["anim_agacharse"] = "Agacharse",
		["anim_agarrar"] = "Agarrar", ["anim_macarena"] = "Macarena", ["anim_muerte"] = "Muerte",
		["InacTony"] = "Reposo", ["CaminaTony"] = "Caminar", ["TensoTony"] = "Tenso", ["AltTony"] = "Alterado",
		["Inaya"] = "Reposo", ["Caminaya"] = "Caminar", ["Tensaya"] = "Tensa", ["Alteraya"] = "Alterada",
		["Inactivo"] = "Reposo", ["Caminar"] = "Caminar", ["Tenso"] = "Tenso", ["Alterado"] = "Alterado",
		["Reposo"] = "Reposo", ["ReposoMirar"] = "Mirar alrededor", ["ReposoPeso"] = "Cambiar de peso",
	};

	private const string Pistas_ = "res://Assets/Pistas/Modelos/";
	private static readonly Entrada[] Pistas =
	{
		new("Cinta de la grabadora", Pistas_ + "cinta/cinta.tscn", "Caso 0"),
		new("Grabadora", Pistas_ + "grabadora/grabadora.tscn", "Caso 0"),
		new("Envoltorio", Pistas_ + "envoltorio/envoltorio.tscn", "Caso 0"),
		new("Huellas", Pistas_ + "huellas/huellas_molde.tscn", "Caso 0"),
		new("Libro de turnos", Pistas_ + "libro_turnos/libro_turnos.tscn", "Caso 0"),
		new("Nota del comisario", Pistas_ + "nota_comisario/nota_comisario.tscn", "Caso 0"),
		new("Pluma", Pistas_ + "pluma/pluma.tscn", "Caso 0"),
		new("Reloj", Pistas_ + "reloj/reloj.tscn", "Caso 0"),
		new("Altavoz", Pistas_ + "altavoz/altavoz.tscn", "Caso 1"),
		new("Baúl de disfraces", Pistas_ + "baul_disfraces/baul.tscn", "Caso 1"),
		new("Botas de Tony", Pistas_ + "botas_tony/botas_tony.tscn", "Caso 1"),
		new("Candado de la caseta", Pistas_ + "candado_caseta/candado_caseta.tscn", "Caso 1"),
		new("Cornamenta y zancos", Pistas_ + "cornamenta_zancos/cornamenta_zancos.tscn", "Caso 1"),
		new("Cuaderno de Eriz", Pistas_ + "cuaderno_eriz/cuaderno_eriz.tscn", "Caso 1"),
		new("Delantal", Pistas_ + "delantal/delantal.tscn", "Caso 1"),
		new("Taza y delantal", Pistas_ + "taza_delantal/taza_delantal.tscn", "Caso 1"),
		new("Foto de grupo", Pistas_ + "foto_grupo/foto_grupo.tscn", "Caso 1"),
		new("Hueco del remo", Pistas_ + "hueco_remo/hueco_remo.tscn", "Caso 1"),
		new("Remo con sangre", Pistas_ + "remo_sangre/remo_sangre.tscn", "Caso 1"),
		new("Huellas detenidas", Pistas_ + "huellas_detenidas/huellas_detenidas.tscn", "Caso 1"),
		new("Huellas de pezuña", Pistas_ + "huellas_pezuna/huellas_pezuna.tscn", "Caso 1"),
		new("Interruptor del generador", Pistas_ + "interruptor_generador/interruptor_generador.tscn", "Caso 1"),
		new("Leña intacta", Pistas_ + "lena_intacta/lena_intacta.tscn", "Caso 1"),
		new("Libro de guardia quemado", Pistas_ + "libro_guardia_quemado/libro_guardia_quemado.tscn", "Caso 1"),
		new("Linterna de Porky", Pistas_ + "linterna_porky/linterna_porky.tscn", "Caso 1"),
		new("Nota de Porky", Pistas_ + "nota_porky/nota_porky.tscn", "Caso 1"),
		new("Oferta de compra", Pistas_ + "oferta_compra/oferta_compra.tscn", "Caso 1"),
		new("Recorte de periódico", Pistas_ + "recorte_periodico/recorte_periodico.tscn", "Caso 1"),
		new("Registro de enfermería", Pistas_ + "registro_enfermeria/registro_enfermeria.tscn", "Caso 1"),
		new("Registro de llamadas", Pistas_ + "registro_llamadas/registro_llamadas.tscn", "Caso 1"),
		new("Silbato «G. S.»", Pistas_ + "silbato_ts/silbato_ts.tscn", "Caso 1"),
	};

	// Escenarios completos, tal como se juegan. Del nivel se quitan el jugador, la interfaz, la lógica del caso, la música,
	// la niebla y su iluminación (la galería pone la suya). Encuadre: las partes que definen el encuadre inicial
	// (sin el bosque lejano ni el terreno, que harían ver todo diminuto).
	private static readonly Entrada[] Escenarios =
	{
		new("Comisaría", "res://Escenas/Interrogatorio/sala_interrogatorio.tscn", "Caso 0",
			Encuadre: new[] { "Sala", "ZonaPasillo", "ZonaRecepcion", "ZonaVestuario" }, SinTecho: true),
		new("Campamento", "res://Escenas/Caso1/escenario_campamento.tscn", "Caso 1",
			Encuadre: new[] { "ZonaFogata", "Almacen", "Cocina", "CabanaPorky", "CabanaDirector", "Campistas", "Enfermeria", "Caseta", "Estacionamiento", "SalidaSendero", "Embarcadero" }),
		new("Lago", "res://Escenas/Caso1/Lago/escenario_lago.tscn", "Caso 1",
			Encuadre: new[] { "Sendero", "Bosque", "Colina", "Orilla", "Embarcadero", "Cobertizo", "Lago" }),
		new("Sala de interrogatorio", "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn", "Caso 1",
			Encuadre: new[] { "Sala", "CSGBox3D", "Mesa", "Corcho", "MesaEvidencias", "Implicados" }, SinTecho: true),
	};

	// Nodos del nivel que no son parte del escenario
	private static readonly HashSet<string> FueraDelEscenario = new() { "Jugador", "Limites", "HUD", "MenuPausa" };

	private static readonly Color Fondo3D = new(0.045f, 0.05f, 0.06f);
	private static readonly Color Coral = new(0.941f, 0.392f, 0.361f);
	private static readonly Color Gris = new(0.682f, 0.714f, 0.749f);

	private const float SensibilidadRaton = 0.006f;
	private const float VelocidadTeclas = 1.8f; // rad/s
	private const float GiroAutomatico = 0.25f; // rad/s mientras nadie toca la vista
	private const float EsperaGiroAutomatico = 4f; // s sin tocar antes de volver a girar solo
	private const float AnchoPanel = 420f;
	private const float AlturaCorte = 2.3f; // m sobre el piso: lo que quede arriba (techos) se oculta con T
	private const float PitchMin = -1.45f, PitchMax = 1.45f;

	private SubViewport _visor;
	private Node3D _escenario; // raíz de lo que se muestra
	private Node3D _modelo;
	private MeshInstance3D _pedestal;
	private Camera3D _camara;
	private ItemList _lista;
	private ItemList _animaciones;
	private Label _tituloAnimaciones;
	private readonly List<(string Etiqueta, string Nombre)> _opcionesAnimacion = new();
	private AnimadorPersonaje _animador; // retarget de Quaternius
	private AnimacionPersonaje _animacionPropia;
	private Label _nombre;
	private Label _detalle;
	private Button _volver;
	private readonly Dictionary<Categoria, Button> _pestanas = new();
	private Categoria _categoria = Categoria.Personajes;
	private readonly List<Entrada> _visibles = new();

	// Cámara orbital
	private Vector3 _centro, _centroInicial;
	private float _yaw, _pitch, _distancia, _distanciaInicial, _radio = 1f;
	private const float YawInicial = 0.6f, PitchInicial = -0.25f;
	private bool _girando, _desplazando;
	private bool _sinTecho;
	private float _piso;
	private float _quieto = EsperaGiroAutomatico;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Hide();

		// Vista 3D en su propio mundo (no se mezcla con el fondo animado del menú)
		var contenedor = new SubViewportContainer { Stretch = true, MouseFilter = MouseFilterEnum.Ignore };
		contenedor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		contenedor.OffsetLeft = AnchoPanel; // el modelo queda centrado en lo que se ve, no detrás de la lista
		AddChild(contenedor);
		_visor = new SubViewport
		{
			OwnWorld3D = true,
			Msaa3D = Viewport.Msaa.Msaa4X,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
			PhysicsObjectPicking = false,
			AudioListenerEnable3D = false, // los sonidos de ambiente de las zonas no suenan en la galería
		};
		contenedor.AddChild(_visor);
		CrearMundo();

		// Superficie que recibe el ratón sobre la vista
		var superficie = new Control { MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.Move };
		superficie.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		superficie.OffsetLeft = AnchoPanel;
		superficie.GuiInput += AlEntradaVista;
		AddChild(superficie);

		CrearPanel();
	}

	// ---------- Construcción ----------

	private void CrearMundo()
	{
		var cielo = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = Fondo3D,
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.55f, 0.58f, 0.66f),
			AmbientLightEnergy = 0.55f,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
			GlowEnabled = false,
			FogEnabled = false,
		};
		_visor.AddChild(new WorldEnvironment { Environment = cielo });

		// Luz principal cálida desde arriba (estilo interrogatorio) y un contraluz frío para separar del fondo
		var principal = new DirectionalLight3D { LightColor = new Color(1f, 0.92f, 0.78f), LightEnergy = 1.35f, ShadowEnabled = true };
		principal.RotationDegrees = new Vector3(-55, 35, 0);
		_visor.AddChild(principal);
		var contraluz = new DirectionalLight3D { LightColor = new Color(0.55f, 0.65f, 0.9f), LightEnergy = 0.6f };
		contraluz.RotationDegrees = new Vector3(-20, 210, 0);
		_visor.AddChild(contraluz);

		_escenario = new Node3D { Name = "Escenario" };
		_visor.AddChild(_escenario);

		var material = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.12f, 0.13f), Roughness = 0.85f };
		_pedestal = new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 1f, BottomRadius = 1.05f, Height = 0.06f, RadialSegments = 48, Material = material },
			Visible = false,
		};
		_visor.AddChild(_pedestal);

		_camara = new Camera3D { Fov = 40f, Current = true };
		_visor.AddChild(_camara);
	}

	private void CrearPanel()
	{
		var fuenteTitulo = EstiloPapel.Titulos;

		// Columna izquierda: título, pestañas y lista
		var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
		var fondoPanel = new StyleBoxFlat { BgColor = new Color(0.03f, 0.035f, 0.04f, 0.88f), BorderColor = new Color(EstiloPapel.Ambar, 0.6f), BorderWidthRight = 2 };
		fondoPanel.SetContentMarginAll(28);
		panel.AddThemeStyleboxOverride("panel", fondoPanel);
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
		panel.OffsetRight = AnchoPanel;
		AddChild(panel);

		var columna = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		columna.AddThemeConstantOverride("separation", 14);
		panel.AddChild(columna);

		var titulo = EstiloPapel.Texto("GALERÍA", 64, EstiloPapel.Ambar, titulo: true);
		columna.AddChild(titulo);
		columna.AddChild(EstiloPapel.Subrayado(120));

		var pestanas = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		pestanas.AddThemeConstantOverride("separation", 18);
		columna.AddChild(pestanas);
		foreach (Categoria c in Enum.GetValues<Categoria>())
		{
			var boton = new Button { Text = c.ToString().ToUpper(), Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand, ToggleMode = true };
			boton.AddThemeFontOverride("font", fuenteTitulo);
			boton.AddThemeFontSizeOverride("font_size", 24);
			boton.AddThemeColorOverride("font_color", Gris);
			boton.AddThemeColorOverride("font_pressed_color", EstiloPapel.Ambar);
			boton.AddThemeColorOverride("font_hover_pressed_color", EstiloPapel.Ambar);
			boton.AddThemeColorOverride("font_hover_color", Coral);
			boton.AddThemeColorOverride("font_focus_color", Coral);
			var categoria = c;
			boton.Pressed += () => ElegirCategoria(categoria);
			pestanas.AddChild(boton);
			_pestanas[c] = boton;
		}

		_lista = new ItemList { AllowSearch = false, SizeFlagsVertical = SizeFlags.ExpandFill, AllowReselect = true, MouseDefaultCursorShape = CursorShape.PointingHand };
		_lista.AddThemeFontOverride("font", EstiloPapel.Mono);
		_lista.AddThemeFontSizeOverride("font_size", 18);
		_lista.AddThemeColorOverride("font_color", new Color(0.91f, 0.92f, 0.94f));
		_lista.AddThemeColorOverride("font_selected_color", EstiloPapel.Tinta);
		_lista.AddThemeColorOverride("font_hovered_color", Coral);
		_lista.AddThemeColorOverride("font_disabled_color", EstiloPapel.Ambar);
		var vacio = new StyleBoxEmpty();
		_lista.AddThemeStyleboxOverride("panel", vacio);
		_lista.AddThemeStyleboxOverride("focus", vacio);
		var seleccion = new StyleBoxFlat { BgColor = EstiloPapel.Ambar };
		seleccion.SetContentMarginAll(4);
		_lista.AddThemeStyleboxOverride("selected", seleccion);
		_lista.AddThemeStyleboxOverride("selected_focus", seleccion);
		_lista.AddThemeStyleboxOverride("cursor", vacio);
		_lista.AddThemeStyleboxOverride("cursor_unfocused", vacio);
		_lista.AddThemeConstantOverride("v_separation", 8);
		_lista.ItemSelected += i => MostrarIndice((int)i);
		columna.AddChild(_lista);

		// Animaciones del personaje elegido (solo en Personajes)
		_tituloAnimaciones = EstiloPapel.Texto("ANIMACIONES", 24, EstiloPapel.Ambar, titulo: true);
		columna.AddChild(_tituloAnimaciones);
		_animaciones = (ItemList)_lista.Duplicate(0); // sin señales: no debe cambiar de modelo
		_animaciones.SizeFlagsStretchRatio = 1.3f;
		_animaciones.ItemSelected += i => ReproducirIndice((int)i);
		columna.AddChild(_animaciones);

		_volver = new Button { Text = "← VOLVER", Flat = true, Alignment = HorizontalAlignment.Left, MouseDefaultCursorShape = CursorShape.PointingHand };
		_volver.AddThemeFontOverride("font", fuenteTitulo);
		_volver.AddThemeFontSizeOverride("font_size", 30);
		_volver.AddThemeColorOverride("font_color", new Color(0.91f, 0.92f, 0.94f));
		_volver.AddThemeColorOverride("font_hover_color", Coral);
		_volver.AddThemeColorOverride("font_focus_color", Coral);
		_volver.Pressed += Cerrar;
		columna.AddChild(_volver);

		// Pie a la derecha: nombre del modelo y ayuda de controles
		var pie = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
		pie.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
		pie.OffsetLeft = 470;
		pie.OffsetRight = -50;
		pie.OffsetTop = -150;
		pie.OffsetBottom = -40;
		AddChild(pie);
		_nombre = EstiloPapel.Texto("", 54, EstiloPapel.Ambar, titulo: true);
		pie.AddChild(_nombre);
		_detalle = EstiloPapel.Texto("", 16, Gris);
		pie.AddChild(_detalle);
		pie.AddChild(EstiloPapel.Texto(
			"Arrastrar: girar   ·   Rueda o +/-: acercar   ·   Clic derecho: mover   ·   WASD: girar   ·   T: techo   ·   R: vista inicial   ·   Esc: volver",
			14, new Color(Gris, 0.7f)));
	}

	// ---------- Abrir, cerrar y elegir ----------

	public void Abrir()
	{
		Show();
		_visor.RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible;
		ElegirCategoria(_categoria);
		_lista.GrabFocus();
	}

	public void Cerrar()
	{
		Hide();
		LiberarModelo();
		_visor.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
		EmitSignal(SignalName.Cerrado);
	}

	private void ElegirCategoria(Categoria categoria)
	{
		_categoria = categoria;
		foreach (var (c, boton) in _pestanas) boton.SetPressedNoSignal(c == categoria);

		var fuente = categoria switch { Categoria.Personajes => Personajes, Categoria.Pistas => Pistas, _ => Escenarios };
		_lista.Clear();
		_visibles.Clear();
		string grupo = null;
		int primero = -1;
		foreach (var e in fuente)
		{
			if (e.Grupo != null && e.Grupo != grupo)
			{
				// Encabezado del grupo: no se puede elegir
				grupo = e.Grupo;
				int h = _lista.AddItem(grupo.ToUpper(), selectable: false);
				_lista.SetItemDisabled(h, true);
				_visibles.Add(null);
			}
			int i = _lista.AddItem("   " + e.Nombre);
			_visibles.Add(e);
			if (primero < 0) primero = i;
		}
		if (primero >= 0)
		{
			_lista.Select(primero);
			MostrarIndice(primero);
		}
	}

	private void MostrarIndice(int indice)
	{
		if (indice < 0 || indice >= _visibles.Count || _visibles[indice] == null) return;
		Mostrar(_visibles[indice]);
	}

	private void LiberarModelo()
	{
		if (_modelo == null) return;
		_escenario.RemoveChild(_modelo);
		_modelo.QueueFree();
		_modelo = null;
	}

	private int _turnoCarga;

	private async void Mostrar(Entrada entrada)
	{
		_ocultos.Clear();
		LiberarModelo();
		_animador = null;
		_animacionPropia = null;
		_nombre.Text = entrada.Nombre.ToUpper();
		_detalle.Text = entrada.Grupo ?? "";

		int turno = ++_turnoCarga;
		if (_categoria == Categoria.Escenarios)
		{
			// Un nivel completo tarda unos segundos: primero se muestra el aviso
			_detalle.Text = "Cargando…";
			_pedestal.Visible = false;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (turno != _turnoCarga || !Visible) return; // ya se eligió otra cosa
			_detalle.Text = entrada.Grupo ?? "";
		}

		var escena = ResourceLoader.Exists(entrada.Ruta) ? GD.Load<PackedScene>(entrada.Ruta) : null;
		if (escena == null)
		{
			_detalle.Text = "No se encontró el modelo.";
			_pedestal.Visible = false;
			return;
		}

		var instancia = escena.Instantiate<Node3D>();
		if (_categoria == Categoria.Personajes) _modelo = ArmarPersonaje(entrada, instancia);
		else
		{
			_modelo = new Node3D { Name = "Modelo" };
			instancia.Position = Vector3.Zero;
			if (_categoria == Categoria.Escenarios) LimpiarNivel(instancia);
			_modelo.AddChild(instancia);
		}
		_escenario.AddChild(_modelo);
		if (_categoria == Categoria.Escenarios)
		{
			// Las paredes CSG arman su malla un cuadro después: antes no tienen tamaño
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (turno != _turnoCarga || _modelo == null) return;
		}
		// Los interiores se abren sin techo para ver dentro; con T se vuelve a poner
		if (_categoria == Categoria.Escenarios) _sinTecho = entrada.SinTecho;
		Encuadrar(_categoria != Categoria.Escenarios, entrada.Encuadre);
		LlenarAnimaciones(entrada);
	}

	// Se quita lo que no es escenario antes de entrar al árbol, así sus scripts nunca arrancan.
	private static void LimpiarNivel(Node3D nivel)
	{
		foreach (Node hijo in nivel.GetChildren())
		{
			bool fuera = hijo is not Node3D || hijo is DirectionalLight3D || hijo is FogVolume || FueraDelEscenario.Contains(hijo.Name);
			if (!fuera) continue;
			nivel.RemoveChild(hijo);
			hijo.Free();
		}
	}

	// Igual que en el juego: el modelo escalado dentro de un nodo, con su animación de reposo.
	private Node3D ArmarPersonaje(Entrada e, Node3D instancia)
	{
		var personaje = new Node3D { Name = "Personaje" };
		instancia.Name = "Modelo";
		instancia.Scale = Vector3.One * e.Escala;
		instancia.Position = e.Desfase;
		personaje.AddChild(instancia);
		if (e.SinAnimador) return personaje;

		if (e.Ual)
		{
			_animador = new AnimadorPersonaje
			{
				Name = "Animador",
				Modelo = instancia,
				Reposo = "Idle",
				ReposoCerrado = "",
				AlHablar = "",
				Somnoliento = e.Somnoliento,
				DistanciaMirar = 0f,
				MirarJugador = false,
			};
			personaje.AddChild(_animador);
			return personaje;
		}

		var animacion = _animacionPropia = new AnimacionPersonaje
		{
			Name = "Animacion",
			Modelo = instancia,
			Animacion = e.Animacion ?? "",
			Variantes = e.Variantes ?? Array.Empty<string>(),
			Somnoliento = e.Somnoliento,
			DistanciaMirar = 0f, // no hay jugador al que mirar
		};
		personaje.AddChild(animacion);
		return personaje;
	}

	// ---------- Animaciones ----------

	private void LlenarAnimaciones(Entrada entrada)
	{
		_animaciones.Clear();
		_opcionesAnimacion.Clear();
		bool personaje = _categoria == Categoria.Personajes;
		_tituloAnimaciones.Visible = _animaciones.Visible = personaje;
		if (!personaje || _modelo == null) return;

		if (entrada.Ual) _opcionesAnimacion.AddRange(AnimacionesUal);
		else
		{
			// Las animaciones propias del modelo («mixamo_com» es la toma cruda de importación: se omite)
			foreach (Node n in _modelo.FindChildren("*", "AnimationPlayer", true, false))
				foreach (string nombre in ((AnimationPlayer)n).GetAnimationList())
				{
					if (nombre.Contains("mixamo_com") || nombre.StartsWith("galeria/")) continue;
					string corto = nombre.Contains('/') ? nombre[(nombre.LastIndexOf('/') + 1)..] : nombre;
					string etiqueta = NombresAnimacion.TryGetValue(corto, out var bonito) ? bonito : corto;
					_opcionesAnimacion.Add((etiqueta, nombre));
				}
			// Reposo primero, luego en orden alfabético
			_opcionesAnimacion.Sort((a, b) => a.Etiqueta == "Reposo" ? -1 : b.Etiqueta == "Reposo" ? 1
				: string.Compare(a.Etiqueta, b.Etiqueta, StringComparison.CurrentCulture));
		}

		foreach (var (etiqueta, _) in _opcionesAnimacion) _animaciones.AddItem("   " + etiqueta);
		if (_opcionesAnimacion.Count == 0)
		{
			int i = _animaciones.AddItem("   (solo respira)", selectable: false);
			_animaciones.SetItemDisabled(i, true);
		}
		else _animaciones.Select(0);
	}

	private void ReproducirIndice(int indice)
	{
		if (indice < 0 || indice >= _opcionesAnimacion.Count || _modelo == null) return;
		string nombre = _opcionesAnimacion[indice].Nombre;
		_quieto = 0f;

		if (IsInstanceValid(_animador) && _animador.IsInsideTree())
		{
			_animador.Reposo = nombre;
			_animador.Controlar(false); // vuelve al «reposo», que ahora es la animación elegida, en bucle
			return;
		}

		// Animación propia: se apaga el animador del juego (que alterna variantes) y se reproduce directo
		if (IsInstanceValid(_animacionPropia)) _animacionPropia.SetProcess(false);
		foreach (Node n in _modelo.FindChildren("*", "AnimationPlayer", true, false))
		{
			var reproductor = (AnimationPlayer)n;
			if (!reproductor.HasAnimation(nombre)) continue;
			bool caminata = nombre.Contains("camin", StringComparison.OrdinalIgnoreCase) || nombre.Contains("walk", StringComparison.OrdinalIgnoreCase);
			string aReproducir = caminata ? EnSitio(reproductor, nombre) : nombre;
			reproductor.GetAnimation(aReproducir).LoopMode = Animation.LoopModeEnum.Linear;
			reproductor.SpeedScale = 1f;
			reproductor.Play(aReproducir, 0.25);
			return;
		}
	}

	// Las caminatas de Mixamo avanzan la cadera; en la galería se camina en el sitio (la cadera no se mueve en X/Z).
	private static string EnSitio(AnimationPlayer reproductor, string nombre)
	{
		const string Biblioteca = "galeria";
		string copiaNombre = nombre.Replace('/', '_');
		if (!reproductor.HasAnimationLibrary(Biblioteca)) reproductor.AddAnimationLibrary(Biblioteca, new AnimationLibrary());
		var biblioteca = reproductor.GetAnimationLibrary(Biblioteca);
		if (!biblioteca.HasAnimation(copiaNombre))
		{
			var copia = (Animation)reproductor.GetAnimation(nombre).Duplicate();
			for (int t = 0; t < copia.GetTrackCount(); t++)
			{
				if (copia.TrackGetType(t) != Animation.TrackType.Position3D) continue;
				string hueso = copia.TrackGetPath(t).GetConcatenatedSubNames().ToLower();
				if (!(hueso.Contains("hip") || hueso.Contains("pelvis") || hueso.Contains("root"))) continue;
				int claves = copia.TrackGetKeyCount(t);
				if (claves == 0) continue;
				var inicio = (Vector3)copia.TrackGetKeyValue(t, 0);
				for (int k = 0; k < claves; k++)
				{
					var v = (Vector3)copia.TrackGetKeyValue(t, k);
					copia.TrackSetKeyValue(t, k, new Vector3(inicio.X, v.Y, inicio.Z));
				}
			}
			biblioteca.AddAnimation(copiaNombre, copia);
		}
		return Biblioteca + "/" + copiaNombre;
	}

	// ---------- Cámara ----------

	// Centra la cámara en la caja que ocupa el modelo y se aleja lo justo para verlo completo.
	private void Encuadrar(bool conPedestal, string[] partes = null)
	{
		Aabb? porPartes = null;
		if (partes != null && _modelo.GetChildCount() > 0)
			foreach (string parte in partes)
				if (_modelo.GetChild(0).GetNodeOrNull<Node3D>(parte) is Node3D nodo)
				{
					if (CajaDeONada(nodo) is not Aabb c) continue;
					porPartes = porPartes?.Merge(c) ?? c;
				}
		var caja = porPartes ?? CajaDe(_modelo);
		_radio = Mathf.Max(caja.Size.Length() * 0.5f, 0.05f);
		_centroInicial = caja.GetCenter();
		float mitadFov = Mathf.DegToRad(_camara.Fov * 0.5f);
		_distanciaInicial = _radio / Mathf.Sin(mitadFov) * 1.05f;
		_camara.Near = Mathf.Max(0.005f, _radio * 0.01f);
		_camara.Far = _radio * 40f + 50f;

		_piso = caja.Position.Y;
		AplicarCorte();
		_pedestal.Visible = conPedestal;
		if (conPedestal)
		{
			float r = Mathf.Max(caja.Size.X, caja.Size.Z) * 0.65f + _radio * 0.05f;
			_pedestal.Scale = new Vector3(r, Mathf.Max(r * 0.5f, 0.05f), r);
			_pedestal.Position = new Vector3(_centroInicial.X, caja.Position.Y - 0.03f * _pedestal.Scale.Y, _centroInicial.Z);
		}
		Reiniciar();
	}

	private void Reiniciar()
	{
		_centro = _centroInicial;
		_distancia = _distanciaInicial;
		_yaw = YawInicial;
		_pitch = _categoria == Categoria.Escenarios ? -0.55f : PitchInicial;
		_quieto = 0f;
		ColocarCamara();
	}

	private void ColocarCamara()
	{
		_pitch = Mathf.Clamp(_pitch, PitchMin, PitchMax);
		_distancia = Mathf.Clamp(_distancia, Mathf.Min(_radio * 0.15f, 1f), _distanciaInicial * 4f);
		var direccion = new Vector3(Mathf.Sin(_yaw) * Mathf.Cos(_pitch), -Mathf.Sin(_pitch), Mathf.Cos(_yaw) * Mathf.Cos(_pitch));
		_camara.Position = _centro + direccion * _distancia;
		_camara.LookAt(_centro, Vector3.Up);
	}

	// Caja que ocupa lo visible. Las mallas con esqueleto (FBX de Mixamo, sobre todo) traen una caja en
	// otra escala que no sirve; en su lugar se usan las posiciones de los huesos, con margen para la cabeza y los pies.
	private static Aabb CajaDe(Node3D raiz) => CajaDeONada(raiz) ?? new Aabb(new Vector3(-0.5f, 0, -0.5f), Vector3.One);

	private static Aabb? CajaDeONada(Node3D raiz)
	{
		Aabb? total = null;
		void Sumar(Aabb caja) => total = total?.Merge(caja) ?? caja;

		foreach (Node n in raiz.FindChildren("*", "Skeleton3D", true, false))
		{
			var esqueleto = (Skeleton3D)n;
			if (!esqueleto.IsVisibleInTree() || esqueleto.GetBoneCount() == 0) continue;
			Aabb? huesos = null;
			for (int i = 0; i < esqueleto.GetBoneCount(); i++)
			{
				var punto = esqueleto.GlobalTransform * esqueleto.GetBoneGlobalPose(i).Origin;
				huesos = huesos?.Expand(punto) ?? new Aabb(punto, Vector3.Zero);
			}
			var h = huesos.Value;
			float margen = Mathf.Max(h.Size.Y, Mathf.Max(h.Size.X, h.Size.Z)) * 0.08f;
			Sumar(h.Grow(margen));
		}

		foreach (Node n in raiz.FindChildren("*", "GeometryInstance3D", true, false))
		{
			if (n is not (MeshInstance3D or MultiMeshInstance3D or CsgShape3D)) continue;
			if (n is MeshInstance3D malla && malla.GetNodeOrNull<Skeleton3D>(malla.Skeleton) != null) continue;
			var g = (GeometryInstance3D)n;
			if (!g.IsVisibleInTree()) continue;
			var local = g.GetAabb();
			if (local.Size == Vector3.Zero) continue;
			Sumar(g.GlobalTransform * local);
		}
		return total;
	}

	// ---------- Entrada ----------

	private void AlEntradaVista(InputEvent evento)
	{
		switch (evento)
		{
			case InputEventMouseButton boton:
				if (boton.ButtonIndex == MouseButton.Left) _girando = boton.Pressed;
				else if (boton.ButtonIndex is MouseButton.Right or MouseButton.Middle) _desplazando = boton.Pressed;
				else if (boton.Pressed && boton.ButtonIndex == MouseButton.WheelUp) Acercar(0.88f);
				else if (boton.Pressed && boton.ButtonIndex == MouseButton.WheelDown) Acercar(1f / 0.88f);
				_quieto = 0f;
				AcceptEvent();
				break;
			case InputEventMouseMotion movimiento:
				if (_girando)
				{
					_yaw -= movimiento.Relative.X * SensibilidadRaton;
					_pitch -= movimiento.Relative.Y * SensibilidadRaton;
				}
				else if (_desplazando)
				{
					float escala = _distancia * 0.0015f;
					var b = _camara.GlobalTransform.Basis;
					_centro += (-b.X * movimiento.Relative.X + b.Y * movimiento.Relative.Y) * escala;
				}
				else break;
				_quieto = 0f;
				ColocarCamara();
				AcceptEvent();
				break;
			case InputEventMagnifyGesture gesto:
				Acercar(1f / gesto.Factor);
				AcceptEvent();
				break;
		}
	}

	private void Acercar(float factor)
	{
		_distancia *= factor;
		ColocarCamara();
	}

	// Quitar techo: oculta lo que empieza por encima de la altura de corte para ver el interior desde arriba.
	private readonly List<GeometryInstance3D> _ocultos = new();

	private void AplicarCorte()
	{
		foreach (var g in _ocultos) if (IsInstanceValid(g)) g.Visible = true;
		_ocultos.Clear();
		if (!_sinTecho || _modelo == null) return;
		foreach (Node n in _modelo.FindChildren("*", "GeometryInstance3D", true, false))
		{
			if (n is not GeometryInstance3D g || !g.Visible) continue;
			if (g is MeshInstance3D m && m.GetNodeOrNull<Skeleton3D>(m.Skeleton) != null) continue;
			var caja = g.GlobalTransform * g.GetAabb();
			if (caja.Position.Y <= _piso + AlturaCorte) continue;
			g.Visible = false;
			_ocultos.Add(g);
		}
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (!Visible) return;
		if (evento.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			Cerrar();
		}
		else if (evento is InputEventKey { Pressed: true, Echo: false, Keycode: Key.T })
		{
			GetViewport().SetInputAsHandled();
			_sinTecho = !_sinTecho;
			AplicarCorte();
		}
		else if (evento is InputEventKey { Pressed: true, Echo: false, Keycode: Key.R })
		{
			GetViewport().SetInputAsHandled();
			Reiniciar();
		}
	}

	public override void _Process(double delta)
	{
		if (!Visible || _modelo == null) return;
		float dt = (float)delta;

		// Teclado y mando
		var giro = Vector2.Zero;
		if (Input.IsKeyPressed(Key.A)) giro.X -= 1;
		if (Input.IsKeyPressed(Key.D)) giro.X += 1;
		if (Input.IsKeyPressed(Key.W)) giro.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) giro.Y += 1;
		var palanca = new Vector2(Input.GetJoyAxis(0, JoyAxis.RightX), Input.GetJoyAxis(0, JoyAxis.RightY));
		if (palanca.Length() > 0.2f) giro += palanca;
		float zoom = 0f;
		if (Input.IsKeyPressed(Key.Equal) || Input.IsKeyPressed(Key.Plus) || Input.IsKeyPressed(Key.KpAdd)) zoom -= 1;
		if (Input.IsKeyPressed(Key.Minus) || Input.IsKeyPressed(Key.KpSubtract)) zoom += 1;
		zoom += Input.GetJoyAxis(0, JoyAxis.TriggerLeft) - Input.GetJoyAxis(0, JoyAxis.TriggerRight);

		bool manual = giro != Vector2.Zero || Mathf.Abs(zoom) > 0.1f || _girando || _desplazando;
		if (giro != Vector2.Zero)
		{
			_yaw -= giro.X * VelocidadTeclas * dt;
			_pitch -= giro.Y * VelocidadTeclas * dt;
		}
		if (Mathf.Abs(zoom) > 0.1f) _distancia *= Mathf.Pow(2f, zoom * dt);

		// Si nadie toca la vista un rato, el modelo gira despacio como en una vitrina
		if (manual) _quieto = 0f;
		else
		{
			_quieto += dt;
			if (_quieto > EsperaGiroAutomatico) _yaw += GiroAutomatico * dt;
		}
		ColocarCamara();
	}
}
