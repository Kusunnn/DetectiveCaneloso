using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Guion del Caso 0 "La cinta de las 3:12" (HU-20). El resto de sistemas no saben nada del caso.
//
// Gancho: a las 6:02 la grabadora de la Sala 1 está en REC sin cinta y el reloj se paró a las 3:12.
// Bucle: explorar → pista → interrogar (preguntar / presionar / enseñar evidencia) → deducir en el
//        tablero [R] → se abre la siguiente zona o información.
// Giro: Porky escondió la cinta por vergüenza, pero la grabadora grababa porque el comisario
//       la dejó en REC a propósito. Cabo suelto: una voz desconocida en la cinta a las 3:12.
public partial class Caso0 : Node
{
	// Pistas (expediente)
	private const string Grabadora = "grabadora";
	private const string Reloj = "reloj";
	private const string Huellas = "huellas";
	private const string Envoltorio = "envoltorio";
	private const string Turnos = "libro_turnos";
	private const string NotaComisario = "nota_comisario";
	private const string Cinta = "cinta";
	private const string Pluma = "pluma";          // Secreta y opcional
	private const int PistasTotales = 8;

	// Testimonios que se pueden usar en el tablero
	private const string TEriz = "t_eriz";
	private const string TPorky = "t_porky";
	private const string TComisario = "t_comisario";

	// Deducciones
	private const string D1 = "d_312";
	private const string D2 = "d_botas";
	private const string D3 = "d_eriz";
	private const string D4 = "d_trampa";

	private const string Combinacion = "combinacion_porky";
	private const uint CapaInteractuable = 1u << 3;

	[Export] public Jugador Jugador;
	[Export] public CanvasLayer Hud;
	[Export] public VentanaDialogo Dialogo;
	[Export] public TableroDeduccion Tablero;
	[Export] public Puerta PuertaVestuario;
	[Export] public Npc Eriz;
	[Export] public Npc Porky;
	[Export] public Npc Comisario;
	[Export] public PuntoInteraccion SillaAcusacion;
	[Export] public Area3D ZonaPasillo;
	[Export] public Area3D ZonaRecepcion;
	[Export] public Area3D ZonaVestuario;
	[Export] public Node3D PuntoErizFinal;
	[Export] public Node3D PuntoPorkyFinal;
	[Export] public Contenedor CasilleroPorky;
	[Export] public Area3D ZonaAndamio;

	// ---------- Estado ----------

	private class Personaje
	{
		public string Nombre;
		public Npc Npc;
		public int Animo;          // -2 cerrado · -1 nervioso · 0 tranquilo · 1+ cooperativo
		public Hablante Hablante;
		public readonly HashSet<string> Leidas = new HashSet<string>();
		public bool Confeso;
	}

	private class Opcion
	{
		public string Id;
		public string Tipo;            // PREGUNTAR, PRESIONAR, EVIDENCIA o vacío (despedirse)
		public string Texto;
		public Func<bool> Visible = () => true;
		public Func<Task> Accion;
	}

	private readonly HashSet<string> _pistas = new HashSet<string>();
	private readonly HashSet<string> _deducciones = new HashSet<string>();
	private readonly List<string> _conclusiones = new List<string>();
	private readonly Dictionary<string, Carta> _testimonios = new Dictionary<string, Carta>();
	private readonly HashSet<string> _indicacionesDadas = new HashSet<string>();
	private readonly HashSet<string> _confrontaciones = new HashSet<string>();
	private Personaje _eriz, _porky, _comisario;
	private Hablante _caneloso;
	private bool _tutorial;
	private bool _autorizado, _resuelto, _enDialogo, _vioCandado;
	private int _intentosAcusacion;
	private double _tiempoCaso, _sinProgreso;
	private int _nivelAyuda;

	public override void _Ready()
	{
		_tutorial = !(GestorPartida.Instancia?.SaltarTutorial ?? false);
		_caneloso = Dialogo.HablantePorNombre("Caneloso");
		_eriz = CrearPersonaje("Eriz", Eriz, -1);
		_porky = CrearPersonaje("Porky", Porky, 0);
		_comisario = CrearPersonaje("Comisario", Comisario, 0);

		Hud.Set("caso", "00");
		Hud.Set("archivo", "La cinta");
		Jugador.EvidenciaRegistrada += AlRegistrarEvidencia;
		Jugador.ObjetoObtenido += _ => { Progreso(); ActualizarObjetivo(); };
		Hud.Connect("linterna_cambiada", Callable.From<bool>(AlCambiarLinterna));

		Eriz.Usado += () => Conversar(() => Interrogar(_eriz, OpcionesEriz, EvidenciaEriz));
		Porky.Usado += () => Conversar(() => Interrogar(_porky, OpcionesPorky, EvidenciaPorky));
		Comisario.Usado += () => Conversar(() => Interrogar(_comisario, OpcionesComisario, EvidenciaComisario));
		SillaAcusacion.Usado += () => Conversar(Resolver);
		SillaAcusacion.Habilitar(false, CapaInteractuable);

		Tablero.ObtenerCartas = Cartas;
		Tablero.ObtenerConclusiones = () => _conclusiones;
		Tablero.Probar = ProbarDeduccion;

		PuertaVestuario.Bloqueada = true;
		PuertaVestuario.MensajeBloqueada = "Vestidores: solo personal autorizado. Necesito permiso del comisario.";
		CasilleroPorky.IntentoBloqueado += AlEncontrarCandado;

		ZonaPasillo.BodyEntered += c => { if (c == Jugador) AlEntrarPasillo(); };
		ZonaRecepcion.BodyEntered += c => { if (c == Jugador) AlEntrarRecepcion(); };
		ZonaVestuario.BodyEntered += c => { if (c == Jugador) Indicar("vestuario", "Doce casilleros. El que me importa es el de Porky."); };
		ZonaAndamio.BodyEntered += c => { if (c == Jugador) Indicar("andamio", "Un andamio en medio del pasillo. Paso agachado [C]."); };

		Objetivo("Revisa la grabadora y el reloj de la Sala 1.");
		CallDeferred(MethodName.Presentacion);
	}

	private Personaje CrearPersonaje(string nombre, Npc npc, int animo)
	{
		var p = new Personaje { Nombre = nombre, Npc = npc, Hablante = Dialogo.HablantePorNombre(nombre) };
		CambiarAnimo(p, animo - p.Animo);
		return p;
	}

	// ---------- Gancho inicial ----------

	private async void Presentacion()
	{
		await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
		_enDialogo = true;
		// El gancho en tres líneas; los controles se aprenden usándolos (indicación en pantalla)
		await Dialogo.Decir(_caneloso,
			"6:02 a. m. La confesión de Tito Garras desapareció de esta grabadora.",
			"Sigue grabando, pero sin cinta. Y el reloj se paró a las 3:12.",
			"¿Qué pasó aquí a las 3:12?");
		_enDialogo = false;
		Indicar("inicio", "[W A S D] para caminar. Me acerco a la grabadora y presiono [E].");
	}

	// ---------- Pistas y progreso ----------

	private void AlRegistrarEvidencia(string id)
	{
		_pistas.Add(id);
		Progreso();
		switch (id)
		{
			case Grabadora:
				Pensar("Sin cinta y en REC. Alguien la dejó grabando.");
				break;
			case Reloj:
				Pensar("Las 3:12 en punto. Y el vidrio está estrellado.");
				break;
			case Huellas:
				Indicar("hablar", "Botas de agente. El conserje está al fondo: le pregunto [E].");
				break;
			case Envoltorio:
				Pensar("Migas frescas junto a la C-2. ¿Quién le trajo de cenar a Tito?");
				break;
			case NotaComisario:
				Pensar("«Dejar la grabadora en REC. Veremos quién se duerme.» Es la letra del comisario.");
				break;
			case Pluma:
				Pensar("Una pluma negra en la rejilla del techo. Aquí no hay pájaros...");
				break;
			case Cinta:
				EmpezarResolucion();
				return;
		}
		if (_pistas.Contains(Grabadora) && _pistas.Contains(Reloj))
			Indicar("tablero", "La grabadora y el reloj. Los uno en mi tablero [R].");
		ActualizarObjetivo();
	}

	private void ActualizarObjetivo()
	{
		if (_resuelto) return;
		if (_pistas.Contains(Cinta)) { Objetivo("Vuelve a la Sala 1 y siéntate en la silla del sospechoso."); return; }
		if (!_pistas.Contains(Grabadora) || !_pistas.Contains(Reloj)) { Objetivo("Revisa la grabadora y el reloj de la Sala 1."); return; }
		if (!_deducciones.Contains(D1)) { Objetivo("Une la grabadora y el reloj en el tablero [R]."); return; }
		if (!_deducciones.Contains(D2))
		{
			var pendientes = new List<string>();
			if (!_pistas.Contains(Huellas)) pendientes.Add("rastros en el pasillo");
			if (!_pistas.Contains(Turnos)) pendientes.Add("quién estaba de guardia (recepción)");
			Objetivo(pendientes.Count > 0
				? "¿Quién estaba despierto a las 3:12? Busca: " + string.Join(", ", pendientes) + "."
				: "Une las huellas y el libro de turnos en el tablero [R].");
			return;
		}
		if (!_autorizado) { Objetivo("Pídele al comisario (recepción) permiso para entrar a los vestidores."); return; }
		if (!Jugador.TieneObjeto(Combinacion) && _vioCandado) { Objetivo("Consigue la combinación del candado de Porky."); return; }
		Objetivo(Jugador.TieneObjeto(Combinacion)
			? "Abre el casillero de Porky con la combinación 3-1-2."
			: "Revisa los casilleros de los vestidores, al oeste del pasillo.");
	}

	private void Progreso()
	{
		_sinProgreso = 0;
		_nivelAyuda = 0;
	}

	// ---------- Tablero de deducción ----------

	private IList<Carta> Cartas()
	{
		var cartas = new List<Carta>();
		foreach (Variant pista in Hud.Get("pistas").AsGodotArray())
		{
			var recurso = pista.As<Resource>();
			cartas.Add(new Carta(recurso.Get("id").AsStringName().ToString(), recurso.Get("titulo").AsString(), "PISTA", recurso.Get("imagen").As<Texture2D>()));
		}
		cartas.AddRange(_testimonios.Values);
		return cartas;
	}

	private string ProbarDeduccion(string a, string b)
	{
		bool Par(string x, string y) => (a == x && b == y) || (a == y && b == x);
		if (Par(Grabadora, Reloj)) return Deducir(D1, "A las 3:12 pasó algo en la Sala 1 mientras la grabadora grababa.");
		if (Par(Huellas, Turnos)) return Deducir(D2, "Las huellas son de Porky: era el único agente de guardia.");
		if (Par(Envoltorio, TEriz)) return Deducir(D3, "Eriz estaba con Tito en las celdas, no en la Sala 1. No fue él.");
		if (Par(NotaComisario, Grabadora) || Par(TComisario, Grabadora))
			return Deducir(D4, "El comisario dejó la grabadora en REC: una trampa para quien se durmiera en la guardia.");
		return null;
	}

	private string Deducir(string id, string conclusion)
	{
		if (_deducciones.Add(id))
		{
			_conclusiones.Add(conclusion);
			Hud.Call("avisar", "DEDUCCIÓN", "+1 CONCLUSIÓN");
			Hud.Call("agregar_nota", "Deducción: " + conclusion);
			Sonidos.Reproducir(Jugador, Sonidos.Tipo.Pista, Jugador.GlobalPosition);
			Progreso();
			ActualizarObjetivo();
		}
		return conclusion;
	}

	private void AñadirTestimonio(string id, string titulo, string nota)
	{
		_testimonios[id] = new Carta(id, titulo, "TESTIMONIO", Dialogo.Retrato(titulo.Split(' ')[0]));
		Nota(nota);
	}

	// ---------- Conversaciones ----------

	private async void Conversar(Func<Task> conversacion)
	{
		if (_enDialogo || Tablero.Abierto) return;
		_enDialogo = true;
		Dialogo.Retener();
		await conversacion();
		Dialogo.Soltar();
		_enDialogo = false;
		ActualizarObjetivo();
	}

	private async Task Interrogar(Personaje p, Func<Personaje, List<Opcion>> opciones, Func<Personaje, string, Task<bool>> evidencia)
	{
		await Saludo(p);
		while (true)
		{
			var lista = opciones(p).Where(o => o.Visible()).ToList();
			lista.Add(new Opcion { Id = "evidencia", Tipo = "EVIDENCIA", Texto = "Mostrarle una prueba del expediente" });
			lista.Add(new Opcion { Id = "adios", Texto = "Seguir investigando" });
			var vistas = lista.Select(o => new OpcionVista(o.Texto, o.Tipo, p.Leidas.Contains(o.Id) && o.Id != "evidencia")).ToList();
			int elegida = await Dialogo.Elegir(p.Hablante, "¿Qué le digo?", vistas, salir: vistas.Count - 1);
			var opcion = lista[elegida];
			if (opcion.Id == "adios") break;
			p.Leidas.Add(opcion.Id);
			if (opcion.Id == "evidencia")
			{
				var pistas = Cartas().Where(c => c.Tipo == "PISTA").Select(c => (c.Id, c.Titulo, c.Imagen)).ToList();
				string id = await Dialogo.ElegirEvidencia(p.Hablante, pistas);
				if (id == null) continue;
				if (!await evidencia(p, id))
				{
					CambiarAnimo(p, -1);
					await Habla(p, "¿Y eso qué tiene que ver conmigo?");
					await Piensa("(No. Necesito una prueba que contradiga lo que me dijo.)");
				}
				continue;
			}
			await opcion.Accion();
		}
	}

	private Task Saludo(Personaje p)
	{
		string[] saludos = p.Nombre switch
		{
			"Eriz" => new[] { "Ya no voy a decir nada, detective.", "¿D-detective? Nomás estoy trapeando, ¿eh?", "Dígame, detective.", "Lo que necesite, detective. Se lo debo." },
			"Porky" => new[] { "Sin mi representante sindical no hablo.", "¿Q-qué pasó ahora?", "Buenos días, detective. *bostezo*", "Lo que quiera. Ya no tengo nada que esconder." },
			_ => new[] { "Tengo trabajo, Caneloso. Sé breve.", "¿Y ahora qué, Caneloso?", "¿Qué hay de nuevo? El juicio es a las doce.", "Buen trabajo, Caneloso." },
		};
		return Habla(p, saludos[Mathf.Clamp(p.Animo + 2, 0, 3)]);
	}

	// ----- Eriz: conserje, ex carterista. Secreto: le llevó un sándwich a Tito a las 3:00. -----

	private List<Opcion> OpcionesEriz(Personaje p) => new List<Opcion>
	{
		new Opcion { Id = "e_noche", Tipo = "PREGUNTAR", Texto = "¿Qué hiciste anoche?", Accion = async () =>
		{
			if (p.Confeso) { await Habla(p, "Ya le dije: volví a las tres a llevarle un sándwich a Tito."); return; }
			await Habla(p, "Trapeé la Sala 1 y me fui a la una. Lo firmé en el libro de turnos.");
			Nota("Eriz dice que se fue a la 1:00.");
		} },
		// Historia de fondo: opcional, no hace falta para avanzar
		new Opcion { Id = "e_tito", Tipo = "PREGUNTAR", Texto = "¿Conoces a Tito Garras, el de la C-2?", Accion = async () =>
		{
			if (p.Confeso) { await Habla(p, "Compartimos celda hace veinte años. No es mala persona."); return; }
			await Habla(p, "¿Tito? Eh... de vista nada más.");
			await Piensa("(Se le erizaron las púas. Está nervioso.)");
		} },
		new Opcion { Id = "e_312", Tipo = "PREGUNTAR", Texto = "¿Oíste algo a las 3:12?", Visible = () => _pistas.Contains(Reloj), Accion = async () =>
		{
			if (!p.Confeso) { await Habla(p, "¿A esa hora? Yo ya estaba dormido en mi casa."); return; }
			await Habla(p, "Un golpe en la Sala 1. Luego, la puerta de los vestidores.");
			Nota("Eriz oyó un golpe en la Sala 1 a las 3:12 y luego la puerta de los vestidores.");
		} },
		new Opcion { Id = "e_presionar", Tipo = "PRESIONAR", Texto = "Tienes llave de todo... y un pasado de carterista, «Dedos».", Visible = () => !p.Confeso, Accion = async () =>
		{
			CambiarAnimo(p, -1);
			await Habla(p, "¡Eso fue hace veinte años! Ahora trapeo y pago mis impuestos.");
			await Piensa("(Sin pruebas solo se cierra más.)");
		} },
	};

	private async Task<bool> EvidenciaEriz(Personaje p, string id)
	{
		switch (id)
		{
			case Envoltorio:
				if (p.Confeso) { await Habla(p, "Sí, lo del sándwich. Ya se lo conté."); return true; }
				p.Confeso = true;
				CambiarAnimo(p, 3);
				_confrontaciones.Add("eriz_envoltorio");
				await Habla(p, "...Está bien. Volví a las tres a llevarle un sándwich a Tito.",
					"Si el comisario se entera, me corre. Pero no toqué la grabadora.");
				AñadirTestimonio(TEriz, "Eriz: volvió a las 3:00", "Eriz volvió a las 3:00 a llevarle un sándwich a Tito Garras (C-2).");
				Indicar("testimonio", "Lo que me cuentan también va a mi tablero [R].");
				return true;
			case Huellas:
				await Habla(p, "¿Botas? Yo uso tenis, detective. Las botas son de los agentes.");
				Nota("Eriz usa tenis: las huellas de bota no son suyas.");
				return true;
			case Turnos:
				await Habla(p, p.Confeso ? "Me fui a la una... y volví. Eso no lo firmé." : "Ahí dice: salida a la una. ¿Ya ve?");
				return true;
			case Reloj:
				await Habla(p, "Lo limpié a las doce y funcionaba. Se lo juro.");
				return true;
		}
		return false;
	}

	// ----- Porky: agente de guardia. Secreto: se durmió en la Sala 1 y escondió la cinta. -----

	private List<Opcion> OpcionesPorky(Personaje p) => new List<Opcion>
	{
		new Opcion { Id = "p_noche", Tipo = "PREGUNTAR", Texto = "¿Dónde estuviste durante la guardia?", Accion = async () =>
		{
			if (p.Confeso) { await Habla(p, "En la Sala 1... dormido. Ya lo sabe."); return; }
			await Habla(p, "Aquí en recepción. Toda la guardia, bien despierto.");
			Nota("Porky dice que pasó la guardia despierto en recepción.");
		} },
		new Opcion { Id = "p_raro", Tipo = "PREGUNTAR", Texto = "¿Viste algo raro anoche?", Accion = async () =>
		{
			await Habla(p, "Nada. Bueno... el conserje andaba por las celdas. Ese tiene llave de todo.");
			Nota("Porky culpa al conserje: «tiene llave de todo».");
		} },
		new Opcion { Id = "p_312", Tipo = "PREGUNTAR", Texto = "¿Qué hacías a las 3:12?", Visible = () => _pistas.Contains(Reloj), Accion = async () =>
		{
			if (!p.Confeso)
			{
				CambiarAnimo(p, -1);
				await Habla(p, "Vigilando. Muy... atento.");
				await Piensa("(Se le cayó una migaja de dona. Está nervioso.)");
				return;
			}
			await Habla(p, "Desperté como a las 3:15. El reloj estaba en el piso y lo colgué.");
			Nota("Porky despertó a las 3:15 con el reloj de la Sala 1 en el piso.");
		} },
		new Opcion { Id = "p_presionar", Tipo = "PRESIONAR", Texto = "Tienes cara de haber dormido... y no en tu cama.", Visible = () => !p.Confeso && !_deducciones.Contains(D2), Accion = async () =>
		{
			CambiarAnimo(p, -1);
			await Habla(p, "¡Oiga! Llevo once años en la corporación. ¡Voy a ir con el sindicato!");
			await Piensa("(Sin pruebas solo se cierra más.)");
		} },
		new Opcion { Id = "p_deduccion", Tipo = "PRESIONAR", Texto = "Eras el único agente de guardia... y tus botas fueron a los vestidores.", Visible = () => !p.Confeso && _deducciones.Contains(D2), Accion = async () =>
		{
			p.Confeso = true;
			_confrontaciones.Add("porky_deduccion");
			bool coopera = p.Animo >= -1;
			CambiarAnimo(p, 2);
			await Habla(p, "...Está bien. Me quedé dormido en la Sala 1.",
				"Desperté con la luz de REC prendida y escondí la cinta en mi casillero.");
			AñadirTestimonio(TPorky, "Porky: se durmió", "Porky se durmió en la Sala 1, vio la grabadora en REC y escondió la cinta en su casillero.");
			if (coopera)
			{
				await Habla(p, "La combinación es 3-1-2. No le diga al comisario.");
				Jugador.DarObjeto(Combinacion, "3-1-2. La combinación del casillero de Porky.");
			}
			else
			{
				await Habla(p, "Y la combinación no se la doy. Búsquela usted.");
				await Piensa("(Lo presioné de más. Tendré que buscar entre sus cosas.)");
			}
		} },
	};

	private async Task<bool> EvidenciaPorky(Personaje p, string id)
	{
		switch (id)
		{
			case Huellas:
				if (_confrontaciones.Add("porky_huellas")) CambiarAnimo(p, -1);
				await Habla(p, "Todos los agentes usamos botas. Bueno... anoche solo estaba yo.");
				Nota("Porky admite que anoche era el único agente de guardia.");
				return true;
			case Turnos:
				await Habla(p, "Sí, guardia de dos a cuatro. Hasta firmé. ¿Y?");
				return true;
			case Envoltorio:
				await Habla(p, "¿Un sándwich? Yo soy más de donas, detective.");
				return true;
			case Reloj:
				await Habla(p, p.Confeso ? "Ese reloj estaba en el piso cuando desperté." : "Las 3:12... Ni idea. Yo estaba aquí.");
				return true;
			case NotaComisario:
				await Habla(p, "¡¿Era una trampa?! ...Y caí redondito.");
				return true;
		}
		return false;
	}

	// ----- Comisario: secreto, dejó la grabadora en REC para pillar al que se duerme. -----

	private List<Opcion> OpcionesComisario(Personaje p) => new List<Opcion>
	{
		new Opcion { Id = "c_grabadora", Tipo = "PREGUNTAR", Texto = "¿Qué sabe de la grabadora?", Accion = async () =>
		{
			if (p.Confeso) { await Habla(p, "Ya lo sabe: la dejé en REC a propósito."); return; }
			await Habla(p, "Tenía la confesión de Tito Garras. No la toco desde ayer a las seis.");
			Nota("El comisario dice que no tocó la grabadora desde las 18:00.");
		} },
		new Opcion { Id = "c_guardia", Tipo = "PREGUNTAR", Texto = "¿Quién vigilaba anoche?", Accion = async () =>
		{
			await Habla(p, "Porky, de dos a cuatro. Buen muchacho... cuando está despierto.");
			Nota("Guardia nocturna: Porky, de 2:00 a 4:00.");
		} },
		// Historia de fondo: opcional, no hace falta para avanzar
		new Opcion { Id = "c_tito", Tipo = "PREGUNTAR", Texto = "¿Quién es Tito Garras?", Accion = async () =>
		{
			await Habla(p, "Un carterista. Confesó que robó la cartera del alcalde.",
				"Dice que trabaja para un tal «La Garra». Puros cuentos.");
			Nota("Tito Garras habló de alguien llamado «La Garra».");
		} },
		new Opcion { Id = "c_permiso", Tipo = "PREGUNTAR", Texto = "Necesito entrar a los vestidores.", Visible = () => !_autorizado, Accion = async () =>
		{
			if (!_deducciones.Contains(D2))
			{
				await Habla(p, "¿A los vestidores? Primero tráeme algo concreto.");
				await Piensa("(Algo concreto... Uno mis pistas en el tablero [R].)");
				return;
			}
			await Habla(p, "Botas de agente y un solo agente de guardia. Ten la llave.");
			_autorizado = true;
			PuertaVestuario.Desbloquear();
			Jugador.MostrarAviso("Nueva zona disponible: Vestidores", 4.0);
			Progreso();
		} },
		new Opcion { Id = "c_presionar", Tipo = "PRESIONAR", Texto = "¿Seguro que no tocó la grabadora anoche?", Visible = () => !p.Confeso, Accion = async () =>
		{
			CambiarAnimo(p, -1);
			await Habla(p, "¿Me estás acusando, Caneloso? Cuidado.");
			await Piensa("(Miró el cajón de su escritorio antes de contestar...)");
		} },
	};

	private async Task<bool> EvidenciaComisario(Personaje p, string id)
	{
		switch (id)
		{
			case NotaComisario:
				if (p.Confeso) { await Habla(p, "Sí, esa nota es mía. Ya lo hablamos."); return true; }
				p.Confeso = true;
				_confrontaciones.Add("comisario_nota");
				CambiarAnimo(p, 1);
				await Habla(p, "...Sí. La dejé grabando para cachar al que se duerme en la guardia.",
					"No pensé que se llevaría la cinta. Que esto no salga de aquí.");
				AñadirTestimonio(TComisario, "Comisario: la trampa", "El comisario dejó la grabadora en REC a propósito para cachar al que se duerme en la guardia.");
				return true;
			case Turnos:
				await Habla(p, "La nota del margen es mía: «NO TOCAR». ¿Algún problema?");
				return true;
			case Cinta:
				await Habla(p, "¡La cinta! Explícame quién y por qué. En la Sala 1.");
				return true;
			case Huellas:
				await Habla(p, "Botas de agente. Bien visto. ¿Y quién estaba de guardia?");
				return true;
		}
		return false;
	}

	// ---------- Resolución (estilo Obra Dinn: solo se confirma si todo encaja) ----------

	private void EmpezarResolucion()
	{
		Progreso();
		Indicar("resolver", "Ya tengo la cinta. Vuelvo a la Sala 1, a la silla del sospechoso.");
		Eriz.GlobalTransform = PuntoErizFinal.GlobalTransform;
		Porky.GlobalTransform = PuntoPorkyFinal.GlobalTransform;
		SillaAcusacion.Habilitar(true, CapaInteractuable);
		ActualizarObjetivo();
	}

	private async Task Resolver()
	{
		if (_resuelto) return;
		await Piensa("Tres preguntas. Si una falla, nada encaja.");
		int quien = await Dialogo.Elegir(_caneloso, "¿Quién sacó la cinta de la grabadora?",
			new List<OpcionVista> { new("Eriz, el conserje"), new("Porky, el agente de guardia"), new("El comisario") });
		int porque = await Dialogo.Elegir(_caneloso, "¿Por qué la sacó?",
			new List<OpcionVista> { new("Para vendérsela a Tito Garras"), new("Creyó que la grabadora lo había grabado dormido"), new("Para sabotear el juicio") });
		int rec = await Dialogo.Elegir(_caneloso, "¿Por qué estaba la grabadora grabando de madrugada?",
			new List<OpcionVista> { new("Porky la dejó encendida"), new("Eriz la puso en marcha al limpiar"), new("El comisario la dejó en REC como trampa") });
		_intentosAcusacion++;

		if (quien != 1 || porque != 1 || rec != 2)
		{
			await Piensa("Algo no encaja. Repaso el tablero [R] y la libreta [Q].");
			Progreso();
			return;
		}

		_resuelto = true;
		SillaAcusacion.Habilitar(false, CapaInteractuable);
		await Habla(_porky, "Perdón, detective. La próxima guardia la hago de pie.");
		await Habla(_comisario, "Se acabaron mis trampas. El juicio sigue. Buen trabajo, Caneloso.");
		await Dialogo.Decir(Dialogo.HablantePorNombre("Cinta"),
			"[Tito Garras] «...sí, yo me robé la cartera del alcalde.»",
			"[03:12 · Ronquidos. Un ruido metálico junto al micrófono]",
			"«La Garra no olvida, Caneloso.»");
		await Piensa(_pistas.Contains(Pluma)
			? "Alguien más estuvo aquí a las 3:12... y entró por la rejilla de la pluma negra."
			: "Alguien más estuvo aquí a las 3:12. ¿Por dónde entró?");
		Nota("En la cinta, a las 3:12, una voz desconocida: «La Garra no olvida, Caneloso».");
		Objetivo("Caso cerrado... por ahora. ¿Quién es «La Garra»?");
		await MostrarCalificacion();
		if (GestorPartida.Instancia != null)
		{
			GestorPartida.Instancia.SaltarTutorial = true;
			GestorPartida.Instancia.GuardarPartida();
		}
	}

	// Calificación final para motivar a rejugar
	private Task MostrarCalificacion()
	{
		int pistas = _pistas.Count;
		int deducciones = _deducciones.Count;
		int confrontaciones = _confrontaciones.Count;
		bool primera = _intentosAcusacion == 1;
		int puntos = pistas * 10 + deducciones * 10 + confrontaciones * 5 + (primera ? 20 : 0);
		string nota = puntos >= 145 ? "S" : puntos >= 120 ? "A" : puntos >= 90 ? "B" : "C";
		var tiempo = TimeSpan.FromSeconds(_tiempoCaso);
		return Dialogo.Decir(Dialogo.HablantePorNombre("Informe del caso"),
			$"Pistas: {pistas}/{PistasTotales}{(_pistas.Contains(Pluma) ? " (¡con la secreta!)" : "")} · Deducciones: {deducciones}/4 · Confrontaciones: {confrontaciones}/4",
			$"Al primer intento: {(primera ? "sí" : "no (" + _intentosAcusacion + " intentos)")} · Tiempo: {(int)tiempo.TotalMinutes} min {tiempo.Seconds:00} s",
			$"CALIFICACIÓN: {nota}. " + (nota == "S" ? "Impecable, detective." : "Todavía quedan secretos en la estación."));
	}

	// ---------- Zonas y tutorial ----------

	private void AlEntrarPasillo()
	{
		if (!Jugador.LinternaEncendida && !_pistas.Contains(Huellas))
			Indicar("linterna", "No veo nada. Prendo la linterna [F].");
	}

	private void AlEntrarRecepcion()
	{
		if (_vioCandado && !Jugador.TieneObjeto(Combinacion))
			Indicar("agacharse_banco", "Algo hay bajo el banco. Me agacho [C] para verlo.");
		else if (!_pistas.Contains(Turnos))
			Indicar("recoger", "El libro de turnos está en el mostrador. Lo tomo [E].");
	}

	private void AlCambiarLinterna(bool encendida)
	{
		if (encendida && !_pistas.Contains(Huellas))
			Indicar("buscar_huellas", "Con luz se ve mejor... ¿qué hay en el piso?");
	}

	private void AlEncontrarCandado()
	{
		if (Jugador.TieneObjeto(Combinacion)) return;
		_vioCandado = true;
		Indicar("mochila", "Candado de combinación. ¿Porky dejó sus cosas en recepción?");
		ActualizarObjetivo();
	}

	// ---------- Ayuda progresiva ----------

	public override void _Process(double delta)
	{
		if (_resuelto) return;
		_tiempoCaso += delta;
		if (_enDialogo || Tablero.Abierto) return;
		_sinProgreso += delta;
		if ((_nivelAyuda == 0 && _sinProgreso > 100) || (_nivelAyuda == 1 && _sinProgreso > 200))
		{
			_nivelAyuda++;
			string ayuda = Ayuda(_nivelAyuda);
			if (ayuda != null) Jugador.MostrarPensamiento(ayuda, 6.0);
		}
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (evento.IsActionPressed("deducir") && !_enDialogo && !Tablero.Abierto)
		{
			GetViewport().SetInputAsHandled();
			Tablero.Abrir();
		}
	}

	// Pista sutil (nivel 1) y otra más clara (nivel 2), sin resolverle el caso
	private string Ayuda(int nivel)
	{
		bool claro = nivel >= 2;
		if (!_pistas.Contains(Grabadora)) return claro ? "La grabadora de la mesa. La reviso [E]." : "Esa luz roja de la mesa no deja de parpadear...";
		if (!_pistas.Contains(Reloj)) return claro ? "El reloj de la pared... ¿está parado?" : "Hay algo raro en esta sala... ¿qué hora es?";
		if (!_deducciones.Contains(D1)) return claro ? "Si uno la grabadora y el reloj en el tablero [R]..." : "La grabadora y el reloj... ¿estarán relacionados?";
		if (!_pistas.Contains(Huellas)) return claro ? "El piso del pasillo está muy oscuro. Con la linterna [F]..." : "Si alguien salió de la sala de noche, habrá dejado rastro.";
		if (!_pistas.Contains(Turnos)) return claro ? "En recepción tiene que haber un registro de turnos." : "¿Quién estaba de guardia anoche?";
		if (!_deducciones.Contains(D2)) return claro ? "Huellas de bota y un solo agente de guardia. Al tablero [R]." : "Esas huellas... ¿de quién pueden ser?";
		if (!_autorizado) return claro ? "Voy con el comisario a recepción." : "El comisario tiene la llave de los vestidores.";
		if (_vioCandado && !Jugador.TieneObjeto(Combinacion)) return claro ? "¿Y si la mochila de Porky está bajo el banco de espera? Agachado [C]." : "Porky no se separa de sus cosas...";
		if (!_pistas.Contains(Cinta)) return claro ? "El casillero de Porky, en los vestidores." : "Si yo escondiera algo aquí, ¿dónde sería?";
		return claro ? "La silla del sospechoso, en la Sala 1. Ya puedo explicarlo todo." : "Creo que ya tengo todas las piezas.";
	}

	// ---------- Utilidades ----------

	private void CambiarAnimo(Personaje p, int delta)
	{
		p.Animo = Mathf.Clamp(p.Animo + delta, -2, 2);
		(string texto, Color color) = p.Animo switch
		{
			<= -2 => ("cerrado", new Color(0.65f, 0.15f, 0.1f)),
			-1 => ("nervioso", new Color(0.75f, 0.5f, 0.05f)),
			0 => ("tranquilo", new Color(0.35f, 0.35f, 0.35f)),
			_ => ("cooperativo", new Color(0.15f, 0.45f, 0.15f)),
		};
		p.Hablante.Animo = texto;
		p.Hablante.ColorAnimo = color;
		p.Hablante.Tiembla = p.Animo == -1;
		p.Npc?.FijarAnimo(p.Animo);
	}

	private Task Habla(Personaje p, params string[] lineas) => Dialogo.Decir(p.Hablante, lineas);

	private Task Piensa(params string[] lineas) => Dialogo.Decir(_caneloso, lineas);

	private void Pensar(string texto) => Jugador.MostrarPensamiento(texto, 5.0);

	private void Nota(string texto)
	{
		Hud.Call("agregar_nota", texto);
		Indicar("libreta", "Lo apunto en mi libreta [Q].");
	}

	private void Objetivo(string texto) => Hud.Set("objetivo", texto);

	// Indicación del tutorial, una sola vez (nada si se omitió el tutorial)
	private void Indicar(string clave, string texto)
	{
		if (!_tutorial || !_indicacionesDadas.Add(clave)) return;
		Jugador.MostrarIndicacion(texto);
	}
}
