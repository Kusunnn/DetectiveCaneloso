using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Caso 1 «La Noche del Ciervo de Luz» — Escenario 1: el Campamento Pino Lunar (HU-20.5).
// Hecdog presenta el caso y Caneloso reúne pistas en el campamento (la cabaña de Porky se abre con su llavero).
// El sendero lleva al Lago y el Bosque (Escenario 2, HU-20.6) o a la sala de la estación, donde se interroga a los seis
// y se acusa. El guion vive en Datos/Dialogos/caso1_*.json (GuionCaso1); este script solo lo conecta con el escenario.
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
	// Pistas del campamento (las 9 del lago están en Caso1Lago)
	private const int PistasTotales = 13;
	// Cuántas pistas hacen falta para que el sendero del lago "se abra"
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
		// Si se llegó por el flujo de capítulos, muestra la tarjeta de título (o la cinemática IntroCaso).
		// Sin el aviso del expediente al final: lo que sigue es Hecdog, que da su propio contexto.
		if (gestor != null) await gestor.PresentarCapitulo(avisoExpediente: false);
		// Si el jugador salió al menú durante la presentación, la escena ya no existe
		if (!IsInstanceValid(this) || !IsInsideTree()) return;
		bool vioExpediente = gestor?.PresentoConCinematica ?? false;
		await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
		await Conversar(_guion.Introduccion);
		// Hecdog le da el expediente. Si la cinemática ya lo mostró hoja por hoja, no se abre otra vez:
		// basta con recordar la tecla. Si no, se abre en cuanto se cierra la conversación.
		if (_guion.AbrirExpediente && IsInsideTree())
		{
			if (vioExpediente) Jugador.MostrarAviso($"[{Teclas.Etiqueta("expediente")}] Revisar el expediente cuando quieras", 5.0);
			else Hud.Call("abrir_expediente");
		}
		_guion.AbrirExpediente = false;
	}


	// ---------- Pistas ----------

	private void AlRegistrarEvidencia(string id)
	{
		_pistas.Add(id);
		Cambio();
		Autoguardar("Pista: " + id);
		if (!_guion.ComentarPista(Jugador, id)) Hud.Call("agregar_nota", "Pista: " + id);
		if (_pistas.Count == PistasParaSalir)
			Jugador.MostrarAviso("El sendero del lago ya me espera", 4.0);
		ActualizarObjetivo();
	}

	// HU-07: lo que se descubre examinando una evidencia queda anotado en la libreta
	private void AlDescubrirDetalle(string origen, string id)
	{
		if (_guion.NotaDeDetalle(id) is string nota) Hud.Call("agregar_nota", nota);
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
		if (_guion.Objetivo() is string delGuion) { Objetivo(delGuion); return; }
		string cuenta = $" ({_pistas.Count}/{PistasTotales} pistas)";
		if (_pistas.Count >= PistasTotales) { Objetivo("Ya revisé todo el campamento. El sendero del lago, al norte."); return; }
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
		if (_guion.Resuelto)
		{
			await Dialogo.Decir(_caneloso, "Ya no hay nada que buscar allá abajo. El caso está cerrado.");
			return;
		}
		if (_pistas.Count < PistasParaSalir)
		{
			await Dialogo.Decir(_caneloso, "Todavía no. Algo pasó aquí primero.", $"Me faltan cosas por revisar ({_pistas.Count}/{PistasTotales}).");
			return;
		}
		await Dialogo.Decir(_caneloso,
			"El sendero baja al Lago Espejo. Ahí vieron al «ciervo de luz».",
			"Lo que pasó anoche terminó allá abajo.");
		// HU-20.6: el sendero lleva al Lago y el Bosque; desde ahí, la patrulla de Hecdog va a la estación.
		// HU-9.5 CA-03: la estación (Hecdog presenta a los seis) solo después de reunir pruebas en el Lago.
		int delLago = PartidaEscenas.PistasEnDiario(Hud) - _pistas.Count;
		if (!_guion.SalaVista && delLago < PartidaEscenas.PistasLagoParaEstacion)
		{
			int opcionLago = await Dialogo.Elegir("Caneloso", "¿A dónde voy?", "Bajar al lago", "Quedarme en el campamento");
			if (opcionLago == 0) GestorPartida.Instancia?.CambiarEscenaDelCapitulo(PartidaEscenas.Lago, "EntradaDesdeCampamento");
			return;
		}
		int opcion = await Dialogo.Elegir("Caneloso", "¿A dónde voy?", "Bajar al lago", "Ir a la estación con Hecdog", "Quedarme en el campamento");
		if (opcion == 0) GestorPartida.Instancia?.CambiarEscenaDelCapitulo(PartidaEscenas.Lago, "EntradaDesdeCampamento");
		else if (opcion == 1) GestorPartida.Instancia?.CambiarEscenaDelCapitulo(PartidaEscenas.Sala, "EntradaDesdeCampamento");
	}

	// ---------- Diálogos ramificados (HU-27) ----------

	// Nodo de cada implicado en el campamento (Personajes/<nodo>/Colision) → su interrogatorio
	private static readonly (string Nodo, string Id)[] Sospechosos =
	{
		("JuanCarlos", "juan_carlos"), ("Aya", "aya"), ("Miaurio", "miawrio"),
		("Eriz", "eriz"), ("Miami", "mimita"), ("Tony", "tony"),
	};

	private GuionCaso1 _guion;

	private void PrepararDialogos()
	{
		_guion = new GuionCaso1(Dialogo, Hud, _caneloso, Cambio)
		{
			AlTerminar = motivo => { if (!IsInsideTree()) return; ActualizarObjetivo(); Autoguardar(motivo); },
		};
		foreach (var (nodo, id) in Sospechosos)
		{
			var npc = GetParent().GetNodeOrNull<Npc>($"Personajes/{nodo}/Colision");
			if (npc == null) continue;
			string quien = id;
			npc.Usado += () => _ = Conversar(() => _guion.Interrogar(quien, IsInsideTree));
		}
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
