using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Caso 1 «La Noche del Ciervo de Luz» — Escenario 1: el Campamento Pino Lunar (HU-20.5).
// Solo el escenario: pistas físicas, la cabaña de Porky cerrada hasta encontrar su llavero y la salida
// al sendero del lago (Escenario 2, HU-20.6) o a la estación. Los interrogatorios y deducciones vendrán después.
// Igual que Caso0.cs, es el único script que conoce la historia; los sistemas siguen siendo genéricos.
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
	// Confirmado en la historia v2: el registro de llamadas es la coartada de Miawrio en su interrogatorio
	private const bool RegistroLlamadasConfirmado = true;
	private static int PistasTotales => RegistroLlamadasConfirmado ? 13 : 12;
	// Cuántas pistas hacen falta para que el sendero del lago "se abra" (además del llavero de Porky)
	private const int PistasParaSalir = 8;

	// Pasos de la investigación en el orden en que se entiende la historia: el apagón, el «ciervo», lo que hizo
	// Porky esa noche y los testigos. Se puede recoger todo en cualquier orden; el objetivo propone el primer
	// paso que falte.
	private static readonly (string Texto, string[] Pistas)[] Pasos =
	{
		("La fogata del centro: ahí estaban todos cuando se fue la luz.", new[] { LibroQuemado, Lena }),
		("¿Quién apagó la luz? La caseta del generador, al oeste.", new[] { Interruptor, Candado }),
		("El «ciervo»: los disfraces se guardan en el almacén de los cuentos, al oeste de la fogata.", new[] { Baul }),
		("Porky se fue de la fogata antes del apagón. Su cocina, al sur.", new[] { TazaDelantal }),
		("La cabaña de Porky, al este: ¿qué sabía ella?", new[] { NotaPorky, FotoGrupo, Recorte, Oferta }),
		("La enfermería mira directo al lago. ¿Alguien vio algo?", new[] { RegistroEnfermeria }),
		("Tony dice que no bajó al lago. Detrás de la cabaña del director, al noroeste.", new[] { BotasTony }),
		("Un solo coche en el estacionamiento, junto a la entrada.", new[] { RegistroLlamadas }),
	};

	private const string Llavero = "llavero_porky";

	// HU-07: detalles que solo aparecen examinando la evidencia (entradas nuevas del diario)
	private const string LibroAntonio = "libro_guardia_quemado.antonio";
	private const string FotoDedicatoria = "foto_grupo.dedicatoria";
	private const string FotoSilbato = "foto_grupo.silbato";
	private const string RegistroTachon = "registro_enfermeria.tachon";
	private const string BotasJuncos = "botas_tony.juncos";

	[Export] public Jugador Jugador;
	[Export] public CanvasLayer Hud;
	[Export] public VentanaDialogo Dialogo;
	[Export] public TableroDeduccion Tablero;
	[Export] public Puerta PuertaPorky;
	[Export] public PuntoInteraccion Salida;
	[Export] public ObjetoPista PistaLlamadas;
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
		PuertaPorky.MensajeBloqueada = "Cabaña 4: la de Porky. Cerrada con llave... y él nunca deja la llave lejos de la cocina.";

		for (int i = 0; i < ContenedoresConPista.Length && i < PistasDentro.Length; i++)
		{
			var pista = PistasDentro[i];
			pista.CollisionLayer = 0;
			ContenedoresConPista[i].Abierto += () => pista.CollisionLayer = CapaInteractuable;
		}

		if (!RegistroLlamadasConfirmado && PistaLlamadas != null)
		{
			PistaLlamadas.CollisionLayer = 0;
			PistaLlamadas.Visible = false;
		}

		Salida.Usado += () => Conversar(AlUsarSalida);
		foreach (var zona in Zonas)
		{
			var area = zona;
			area.BodyEntered += cuerpo => { if (cuerpo == Jugador) AlEntrarZona(area.Name); };
		}

		if (VersionLigera) AplicarVersionLigera();
		ActualizarObjetivo();
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
		_enDialogo = true;
		await Dialogo.Decir(_caneloso,
			"Campamento Pino Lunar. Las cuatro de la mañana.",
			"La luz ya volvió, pero la fogata todavía humea.",
			"Alguien quemó algo aquí. Y nadie quiere decirme qué.");
		_enDialogo = false;
	}

	// ---------- Pistas ----------

	private void AlRegistrarEvidencia(string id)
	{
		_pistas.Add(id);
		Cambio();
		Autoguardar("Pista: " + id);
		string pensamiento = id switch
		{
			Interruptor => "Bajado a mano. El generador está bien: el apagón fue provocado.",
			Candado => "Sin una marca. Quien lo abrió tenía la llave.",
			Baul => "Falta la cornamenta del disfraz de ciervo. Alguien se vistió de «ciervo de luz».",
			Lena => "La leña sigue apilada. Miaurio dijo que fue por leña... no trajo ni una rama.",
			LibroQuemado => "«Guardia nocturna, 14 de agosto... olor a alcohol...» Alguien quería borrar esa noche.",
			NotaPorky => "«Esta noche se lo digo. Ya tengo el libro. Por Daniel.» ¿A quién iba a decírselo?",
			FotoGrupo => "La cabaña 4, hace veinte años. Porky la tenía a la vista... ¿por qué esta foto?",
			Recorte => "Daniel. Se ahogó en el lago aquel verano. Por eso nadie habla del 14 de agosto.",
			Oferta => "Juan Carlos quería comprar el campamento. Porky rompió la oferta.",
			TazaDelantal => "El chocolate a medias y el delantal colgado. Porky salió con prisa.",
			RegistroEnfermeria => "Una línea de las 23:20, tachada a conciencia. Con buena luz quizá se lea algo.",
			BotasTony => "Barro fresco. Las escondieron detrás de la cabaña del director.",
			RegistroLlamadas => "Llamadas de Juan Carlos a medianoche. Habrá que preguntarle.",
			_ => null,
		};
		if (pensamiento != null) Jugador.MostrarPensamiento(pensamiento, 6.0);
		Hud.Call("agregar_nota", NotaDe(id));
		AvisarSiPuedeSalir();
		ActualizarObjetivo();
	}

	private bool _avisoSendero;

	private void AvisarSiPuedeSalir()
	{
		if (_avisoSendero || !PuedeSalir) return;
		_avisoSendero = true;
		Jugador.MostrarAviso("El sendero del lago ya me espera", 4.0);
	}

	private static string NotaDe(string id) => id switch
	{
		Interruptor => "Caseta del generador: el interruptor se bajó a mano. El apagón fue provocado.",
		Candado => "El candado de la caseta no está forzado: lo abrió alguien con llave.",
		Baul => "Del baúl de disfraces falta la cornamenta del ciervo.",
		Lena => "La leña junto a la fogata está intacta: nadie trajo leña anoche.",
		LibroQuemado => "Restos del libro de guardia en la fogata: «14 de agosto», «olor a alcohol».",
		NotaPorky => "Nota de Porky: «Esta noche se lo digo. Ya tengo el libro. Por Daniel.»",
		FotoGrupo => "Foto de hace 20 años en la cabaña de Porky: los campistas de la cabaña 4 y su monitor.",
		Recorte => "Recorte: Daniel murió en el Lago Espejo hace 20 años.",
		Oferta => "Oferta de compra del campamento firmada por Juan Carlos, rota en la papelera de Porky.",
		TazaDelantal => "Cocina: chocolate a medias y el delantal de Porky colgado. Salió con prisa.",
		RegistroEnfermeria => "Enfermería: alguien tachó la línea de las 23:20 del registro.",
		BotasTony => "Botas con barro fresco escondidas detrás de la cabaña del director.",
		RegistroLlamadas => "Registro de llamadas de Juan Carlos.",
		_ => "Pista: " + id,
	};

	// HU-07: lo que se descubre examinando una evidencia queda anotado en la libreta
	private void AlDescubrirDetalle(string origen, string id)
	{
		string nota = id switch
		{
			LibroAntonio => "Reverso del libro de guardia: «De guardia: monitor auxiliar Antonio R.»",
			FotoDedicatoria => "Detrás de la foto: «Cabaña 4, 14 de agosto. Para Daniel... — A. R.»",
			FotoSilbato => "En la foto, el silbato del monitor lleva grabado «A. R.»: es Tony.",
			RegistroTachon => "Bajo el tachón del registro de la enfermería: «23:20, ruido en el lago».",
			BotasJuncos => "En la suela de las botas hay juncos del lago: quien las usó bajó a la orilla.",
			_ => null,
		};
		if (nota != null) Hud.Call("agregar_nota", nota);
		Cambio();
		Autoguardar("Detalle: " + id);
	}

	private void AlObtenerObjeto(string id)
	{
		Cambio();
		if (id != Llavero) return;
		PuertaPorky.Desbloquear();
		Jugador.MostrarAviso("Nueva zona disponible: Cabaña de Porky", 4.0);
		AvisarSiPuedeSalir();
		ActualizarObjetivo();
	}

	private void ActualizarObjetivo()
	{
		string cuenta = $" ({_pistas.Count}/{PistasTotales} pistas)";
		if (_pistas.Count >= PistasTotales) { Objetivo("Ya revisé todo el campamento. El sendero del lago, al norte."); return; }
		if (!Jugador.TieneObjeto(Llavero) && _pistas.Contains(TazaDelantal)) { Objetivo("Revisa el delantal de Porky en la cocina." + cuenta); return; }
		if (Jugador.TieneObjeto(Llavero) && !_pistas.Contains(NotaPorky)) { Objetivo("Abre la cabaña de Porky, al este." + cuenta); return; }
		if (PuedeSalir) { Objetivo("Sigue el sendero al Lago Espejo, al norte... o busca lo que falta." + cuenta); return; }
		foreach (var (texto, ids) in Pasos)
		{
			if (ids.All(id => _pistas.Contains(id) || (id == RegistroLlamadas && !RegistroLlamadasConfirmado))) continue;
			Objetivo(texto + cuenta);
			return;
		}
		Objetivo("Revisa el campamento: fogata, cocina, cabañas, enfermería y caseta." + cuenta);
	}

	// El llavero de Porky también abre el cobertizo del lago: sin él, allá abajo no se podría seguir
	private bool PuedeSalir => _pistas.Count >= PistasParaSalir && Jugador.TieneObjeto(Llavero);

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
			"Estacionamiento" => "Un solo coche. El de Juan Carlos.",
			"Sendero" => "Allá abajo brilla el lago. Azul... como dicen las historias.",
			_ => null,
		};
		if (texto != null && !_enDialogo) Jugador.MostrarPensamiento(texto, 4.5);
	}

	private async System.Threading.Tasks.Task AlUsarSalida()
	{
		if (_pistas.Count < PistasParaSalir)
		{
			await Dialogo.Decir(_caneloso, "Todavía no. Algo pasó aquí primero.", $"Me faltan cosas por revisar ({_pistas.Count}/{PistasTotales}).");
			return;
		}
		if (!Jugador.TieneObjeto(Llavero))
		{
			await Dialogo.Decir(_caneloso, "Porky tenía las llaves de todo el campamento.", "Antes de bajar, quiero ese llavero. Ella nunca lo dejaba lejos de la cocina.");
			return;
		}
		await Dialogo.Decir(_caneloso,
			"El sendero baja al Lago Espejo. Ahí vieron al «ciervo de luz».",
			"Lo que pasó anoche terminó allá abajo.");
		// HU-20.6: el sendero lleva al Lago y el Bosque; desde ahí, la patrulla de Hecdog va a la estación
		int opcion = await Dialogo.Elegir("Caneloso", "¿A dónde voy?", "Bajar al lago", "Ir a la estación con Hecdog", "Quedarme en el campamento");
		if (opcion == 0) GestorPartida.Instancia?.CambiarEscenaDelCapitulo(PartidaEscenas.Lago, "EntradaDesdeCampamento");
		else if (opcion == 1) GestorPartida.Instancia?.CambiarEscenaDelCapitulo(PartidaEscenas.Sala, "EntradaDesdeCampamento");
	}

	private async void Conversar(Func<System.Threading.Tasks.Task> conversacion)
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
