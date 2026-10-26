using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Caso 1 «La Noche del Ciervo de Luz» — Escenario 1: el Campamento Pino Lunar (HU-20.5).
// Hecdog presenta el caso, Caneloso reúne pistas en el campamento (la cabaña de Porky se abre con su llavero)
// y por el sendero del lago llega a la sala, donde interroga a los seis y acusa. El Escenario 2 (lago y bosque)
// todavía no existe: sus pistas ya tienen texto y diálogos, y se activan en cuanto haya objetos con su id.
// El guion vive en Datos/Dialogos/caso1_*.json; este script solo lo conecta con el escenario.
public partial class Caso1 : Node
{
	// Pistas (★ señalan a Tony)
	private const string Interruptor = "interruptor_generador";
	private const string Candado = "candado_caseta";        // ★
	private const string Baul = "baul_disfraces";
	private const string Lena = "lena_intacta";
	private const string LibroQuemado = "libro_guardia_quemado"; // ★
	private const string NotaPorky = "nota_porky";            // ★
	private const string FotoGrupo = "foto_grupo";            // ★
	private const string Recorte = "recorte_periodico";
	private const string Oferta = "oferta_compra";
	private const string TazaDelantal = "taza_delantal";
	private const string RegistroEnfermeria = "registro_enfermeria";
	private const string BotasTony = "botas_tony";            // ★
	private const string RegistroLlamadas = "registro_llamadas";
	// Pistas del campamento (las del lago llegan con el Escenario 2)
	private const int PistasTotales = 13;
	// Cuántas pistas hacen falta para que el sendero del lago "se abra" (por ahora, el cartel de próximamente)
	private const int PistasParaSalir = 8;

	private const string Llavero = "llavero_porky";

	[Export] public Jugador Jugador;
	[Export] public CanvasLayer Hud;
	[Export] public VentanaDialogo Dialogo;
	[Export] public TableroDeduccion Tablero;
	[Export] public Puerta PuertaPorky;
	[Export] public PuntoInteraccion Salida;
	[Export] public MusicaFondo Musica;
	[Export] public WorldEnvironment Entorno;
	// Contenedores con una pista adentro: la pista solo se puede tomar con el contenedor abierto
	[Export] public Contenedor[] ContenedoresConPista = Array.Empty<Contenedor>();
	[Export] public ObjetoPista[] PistasDentro = Array.Empty<ObjetoPista>();
	// Áreas de cada zona (el nombre del nodo es el de la zona): al entrar, Caneloso piensa algo una vez
	[Export] public Area3D[] Zonas = Array.Empty<Area3D>();
	// Versión ligera para equipos modestos: sin niebla volumétrica ni SSAO
	[Export] public bool VersionLigera = false;

	private const uint CapaInteractuable = 1u << 3;

	private readonly HashSet<string> _pistas = new HashSet<string>();
	private readonly HashSet<string> _zonasVistas = new HashSet<string>();
	private Hablante _caneloso;
	private bool _enDialogo;

	public override void _Ready()
	{
		_caneloso = Dialogo.HablantePorNombre("Caneloso");
		_caneloso.TonoVoz = 1f;

		Hud.Set("caso", "01");
		Hud.Set("archivo", "La Noche del Ciervo de Luz");
		Hud.Set("lugar", "el campamento");
		Jugador.EvidenciaRegistrada += AlRegistrarEvidencia;
		Jugador.ObjetoObtenido += AlObtenerObjeto;
		Jugador.EvidenciaExaminada += _ => Cambio();
		Jugador.DetalleDescubierto += AlDescubrirDetalle;

		Tablero.ObtenerCartas = Cartas;
		Tablero.ObtenerConclusiones = () => Array.Empty<string>();
		Tablero.Probar = (_, _) => null; // Las deducciones llegan con los interrogatorios

		PuertaPorky.Bloqueada = true;
		PuertaPorky.MensajeBloqueada = "Cabaña 4: la de Porky. Cerrada con llave... y ella nunca dejaba la llave lejos de la cocina.";

		for (int i = 0; i < ContenedoresConPista.Length && i < PistasDentro.Length; i++)
		{
			var pista = PistasDentro[i];
			pista.CollisionLayer = 0;
			ContenedoresConPista[i].Abierto += () => pista.CollisionLayer = CapaInteractuable;
		}

		Salida.Usado += () => _ = Conversar(AlUsarSalida);
		PrepararDialogos();
		foreach (var zona in Zonas)
		{
			var area = zona;
			area.BodyEntered += cuerpo => { if (cuerpo == Jugador) AlEntrarZona(area.Name); };
		}

		if (VersionLigera) AplicarVersionLigera();
		Objetivo("Escucha a Hecdog: él tiene el expediente.");
		PrepararPartida();
		CallDeferred(MethodName.Empezar);
	}

	private async void Empezar()
	{
		// Partida cargada: se restaura y se muestra el "Anteriormente..." en lugar de la presentación
		var gestor = GestorPartida.Instancia;
		if (gestor?.EstadoPendiente != null)
		{
			RestaurarEstado(gestor.EstadoPendiente);
			var notas = Hud.Get("notas").AsGodotArray();
			var extra = notas.Count > 0 ? new[] { "Lo último que anoté: «" + notas[notas.Count - 1].AsString() + "»" } : null;
			await gestor.PresentarCapitulo(extra);
			return;
		}
		// Si se llegó por el flujo de capítulos, muestra la tarjeta de título
		if (gestor != null) await gestor.PresentarCapitulo();
		// Si el jugador salió al menú durante la presentación, la escena ya no existe
		if (!IsInstanceValid(this) || !IsInsideTree()) return;
		await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
		await Conversar(Introduccion);
		// Hecdog le da el expediente: se abre en cuanto se cierra la conversación
		if (_abrirExpediente && IsInsideTree()) Hud.Call("abrir_expediente");
		_abrirExpediente = false;
	}

	// 2.x: Hecdog recibe a Caneloso y le da el expediente
	private async Task Introduccion()
	{
		await _dialogos.Recorrer(_arboles[ArbolIntroduccion]);
		if (!IsInsideTree()) return;
		ActualizarObjetivo();
		Autoguardar("Introducción");
	}

	// ---------- Pistas ----------

	private void AlRegistrarEvidencia(string id)
	{
		_pistas.Add(id);
		Cambio();
		Autoguardar("Pista: " + id);
		if (_guionPistas.TryGetValue(id, out var guion))
		{
			// Lo que piensa Caneloso: el tiempo en pantalla crece con el largo del comentario
			Jugador.MostrarPensamiento(guion.Comentario, Math.Max(6.0, guion.Comentario.Length / 15.0));
			Hud.Call("agregar_nota", guion.Nota);
		}
		else Hud.Call("agregar_nota", "Pista: " + id);
		if (_pistas.Count == PistasParaSalir)
			Jugador.MostrarAviso("El sendero del lago ya me espera", 4.0);
		ActualizarObjetivo();
	}

	// HU-07: lo que se descubre examinando una evidencia queda anotado en la libreta
	private void AlDescubrirDetalle(string origen, string id)
	{
		if (_notasDetalles.TryGetValue(id, out string nota)) Hud.Call("agregar_nota", nota);
		Cambio();
		Autoguardar("Detalle: " + id);
	}

	private void AlObtenerObjeto(string id)
	{
		Cambio();
		if (id != Llavero) return;
		PuertaPorky.Desbloquear();
		Jugador.MostrarAviso("Nueva zona disponible: Cabaña de Porky", 4.0);
		ActualizarObjetivo();
	}

	private void ActualizarObjetivo()
	{
		if (Resuelto) { Objetivo("Caso cerrado."); return; }
		if (!_dialogos.Estado.NodoCompletado(ArbolIntroduccion, "ir")) { Objetivo("Escucha a Hecdog: él tiene el expediente."); return; }
		if (_dialogos.Estado.NodoCompletado(ArbolSala, "llegada"))
		{
			Objetivo(TonyDisponible
				? "Todos confesaron su secreto... menos uno. Habla con Tony y luego acusa."
				: "Interroga a los seis y enséñales la prueba que les tumbe la historia.");
			return;
		}
		string cuenta = $" ({_pistas.Count}/{PistasTotales} pistas)";
		if (_pistas.Count >= PistasTotales) { Objetivo("Ya revisé todo el campamento. El sendero del lago, al norte: Hecdog me espera."); return; }
		if (!Jugador.TieneObjeto(Llavero) && _pistas.Contains(TazaDelantal)) { Objetivo("Revisa el delantal de Porky en la cocina." + cuenta); return; }
		if (Jugador.TieneObjeto(Llavero) && !_pistas.Contains(NotaPorky)) { Objetivo("Abre la cabaña de Porky, al este." + cuenta); return; }
		if (_pistas.Count >= PistasParaSalir) { Objetivo("Sigue el sendero al Lago Espejo, al norte... o busca lo que falta." + cuenta); return; }
		Objetivo("Revisa el campamento: fogata, cocina, cabañas, enfermería y caseta." + cuenta);
	}

	private IList<Carta> Cartas()
	{
		var cartas = new List<Carta>();
		foreach (Variant pista in Hud.Get("pistas").AsGodotArray())
		{
			var recurso = pista.As<Resource>();
			cartas.Add(new Carta(recurso.Get("id").AsStringName().ToString(), recurso.Get("titulo").AsString(), "PISTA", recurso.Get("imagen").As<Texture2D>()));
		}
		return cartas;
	}

	// ---------- Zonas ----------

	private void AlEntrarZona(string zona)
	{
		if (!_zonasVistas.Add(zona)) return;
		string texto = zona switch
		{
			"Fogata" => "La fogata del reencuentro. Tazas, mantas... todos se fueron de golpe.",
			"Almacen" => "El almacén de los cuentos. Aquí guardaban los disfraces.",
			"Cocina" => "La cocina de Porky. Huele todavía a chocolate.",
			"CabanaPorky" => "La cabaña de Porky. Fotos por todos lados.",
			"Director" => "La cabaña del director. Tony... ¿qué hay detrás?",
			"Enfermeria" => "La enfermería mira directo al lago.",
			"Caseta" => "La caseta del generador. De aquí salió el apagón.",
			"Estacionamiento" => "Un solo carro en el estacionamiento. ¿De quién será?",
			"Sendero" => "Allá abajo brilla el lago. Azul... como dicen las historias.",
			_ => null,
		};
		if (texto != null && !_enDialogo) Jugador.MostrarPensamiento(texto, 4.5);
	}

	private async Task AlUsarSalida()
	{
		if (Resuelto)
		{
			await Dialogo.Decir(_caneloso, "Ya no hay nada que buscar allá abajo. El caso está cerrado.");
			return;
		}
		if (_pistas.Count < PistasParaSalir)
		{
			await Dialogo.Decir(_caneloso, "Todavía no. Algo pasó aquí primero.", $"Me faltan cosas por revisar ({_pistas.Count}/{PistasTotales}).");
			return;
		}
		if (!_dialogos.Estado.NodoCompletado(ArbolSala, "llegada"))
		{
			await Dialogo.Decir(_caneloso,
				"El sendero baja al Lago Espejo. Ahí vieron al «ciervo de luz».",
				"Lo que pasó anoche terminó allá abajo.");
			// El Escenario 2 (lago y bosque) todavía no existe: de aquí se pasa directo a la sala
			Hud.Call("avisar", "ESCENARIO 2", "LAGO Y BOSQUE · PRÓXIMAMENTE");
		}
		await Sala();
	}

	// ---------- Diálogos ramificados (HU-27) ----------

	// El guion del Caso 1 (Excel de Yaya, capas 2 a 12): un archivo por conversación
	private const string ArbolIntroduccion = "caso1_introduccion";
	private const string ArbolSala = "caso1_sala";
	private const string ArbolAcusacion = "caso1_acusacion";
	private const string RutaDialogos = "res://Datos/Dialogos/";
	private const string RutaPistas = "res://Datos/Dialogos/caso1_pistas.json";
	private const int CredibilidadInicial = 5;
	// Nodo de cada sospechoso en la escena (Personajes/<nodo>/Colision) → su interrogatorio
	private static readonly (string Nodo, string Id, string Nombre)[] Sospechosos =
	{
		("JuanCarlos", "juan_carlos", "Juan Carlos"),
		("Aya", "aya", "Aya"),
		("Miaurio", "miawrio", "Miawrio"),
		("Eriz", "eriz", "Eriz"),
		("Miami", "mimita", "Mimita"),
		("Tony", "tony", "Tony"),
	};
	// Las cinco confesiones que abren el interrogatorio de Tony
	private static readonly string[] Confesiones = { "juan_carlos_3", "aya_2", "miawrio_2", "eriz_1", "mimita_2" };

	private MotorDialogos _dialogos;
	private readonly Dictionary<string, ArbolDialogo> _arboles = new();
	private readonly Dictionary<string, (string Comentario, string Nota)> _guionPistas = new();
	private readonly Dictionary<string, string> _notasDetalles = new();
	private bool _abrirExpediente;

	private bool TonyDisponible => Confesiones.All(_dialogos.Estado.TieneConclusion);
	private bool Resuelto => _dialogos.Estado.NodoCompletado(ArbolAcusacion, "final_a") || _dialogos.Estado.NodoCompletado(ArbolAcusacion, "final_b");

	private void PrepararDialogos()
	{
		_dialogos = new MotorDialogos(Dialogo)
		{
			TienePista = id => _pistas.Contains(id),
			Cambio = Cambio,
			NotaNueva = nota => Hud.Call("agregar_nota", nota),
			ContadorCambiado = AlCambiarContador,
			Evento = AlEventoDialogo,
			HablantePorNombre = nombre => nombre switch
			{
				"Caneloso" => _caneloso,
				"Narración" => new Hablante(""),
				_ => Dialogo.HablantePorNombre(nombre),
			},
		};
		_dialogos.Estado.Predeterminados["credibilidad"] = CredibilidadInicial;

		foreach (string id in new[] { ArbolIntroduccion, ArbolSala, ArbolAcusacion }.Concat(Sospechosos.Select(s => "caso1_" + s.Id)))
			_arboles[id] = ArbolDialogo.Cargar(RutaDialogos + id + ".json");
		CargarGuionPistas();

		foreach (var (nodo, id, nombre) in Sospechosos)
		{
			var npc = GetParent().GetNodeOrNull<Npc>($"Personajes/{nodo}/Colision");
			if (npc == null) continue;
			string quien = id;
			npc.Usado += () => _ = Conversar(() => Interrogar(quien));
		}
	}

	private void CargarGuionPistas()
	{
		using var archivo = FileAccess.Open(RutaPistas, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (archivo == null || json.Parse(archivo.GetAsText()) != Error.Ok)
		{
			GD.PushError($"Caso1: no se pudo leer {RutaPistas}.");
			return;
		}
		var datos = json.Data.AsGodotDictionary();
		foreach (var (id, pista) in datos["pistas"].AsGodotDictionary())
		{
			var p = pista.AsGodotDictionary();
			_guionPistas[id.AsString()] = (p["comentario"].AsString(), p["nota"].AsString());
		}
		foreach (var (id, nota) in datos["detalles"].AsGodotDictionary())
			_notasDetalles[id.AsString()] = nota.AsString();
	}

	// Un interrogatorio (en el campamento o desde la sala). Si Hecdog pierde la paciencia, se acusa ya.
	private async Task<bool> Interrogar(string quien)
	{
		string salida = await _dialogos.Recorrer(_arboles["caso1_" + quien]);
		Autoguardar("Diálogo: " + quien);
		ActualizarObjetivo();
		if (salida != "paciencia") return false;
		await Sala("paciencia");
		return true;
	}

	// 3.1 y 4.x: Hecdog y los seis en la sala. Se sale con «Seguir investigando» o acusando.
	private async Task Sala(string desde = null)
	{
		while (IsInsideTree())
		{
			string salida = await _dialogos.Recorrer(_arboles[ArbolSala], desde);
			ActualizarObjetivo();
			desde = "sala";
			if (salida.StartsWith("interrogar:"))
			{
				if (await Interrogar(salida.Substring("interrogar:".Length))) return;
				continue;
			}
			if (salida == "acusar") await Acusar();
			return;
		}
	}

	// 11.x y 12.x: la acusación y uno de los dos finales
	private async Task Acusar()
	{
		string final = await _dialogos.Recorrer(_arboles[ArbolAcusacion]);
		if (!IsInsideTree()) return;
		Hud.Call("avisar", final == "A" ? "FINAL A" : "FINAL B", final == "A" ? "LA CONFESIÓN" : "EL ERROR TRÁGICO");
		ActualizarObjetivo();
		Autoguardar("Fin del caso");
	}

	private void AlCambiarContador(string id, int valor)
	{
		if (id == "credibilidad")
			Hud.Call("avisar", "CREDIBILIDAD", $"{Math.Max(valor, 0)} / {CredibilidadInicial}");
	}

	private void AlEventoDialogo(string evento)
	{
		if (evento == "abrir_expediente") _abrirExpediente = true;
	}

	private async Task Conversar(Func<Task> conversacion)
	{
		if (_enDialogo || Tablero.Abierto) return;
		_enDialogo = true;
		Dialogo.Retener();
		await conversacion();
		if (!IsInsideTree()) return;
		Dialogo.Soltar();
		_enDialogo = false;
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (evento.IsActionPressed("deducir") && !_enDialogo && !Tablero.Abierto)
		{
			GetViewport().SetInputAsHandled();
			Tablero.Abrir();
		}
	}

	// ---------- Utilidades ----------

	private void Objetivo(string texto) => Hud.Set("objetivo", texto);

	private void AplicarVersionLigera()
	{
		var entorno = Entorno?.Environment;
		if (entorno == null) return;
		entorno.VolumetricFogEnabled = false;
		entorno.SsaoEnabled = false;
		entorno.FogEnabled = true;
	}
}
