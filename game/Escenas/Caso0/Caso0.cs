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
		PuertaVestuario.MensajeBloqueada = "Vestuario: solo personal autorizado. Necesito el permiso del comisario.";
		CasilleroPorky.IntentoBloqueado += AlEncontrarCandado;

		ZonaPasillo.BodyEntered += c => { if (c == Jugador) AlEntrarPasillo(); };
		ZonaRecepcion.BodyEntered += c => { if (c == Jugador) AlEntrarRecepcion(); };
		ZonaVestuario.BodyEntered += c => { if (c == Jugador) Indicar("vestuario", "Doce casilleros. Puedo revisarlos todos, pero el que me interesa es el de Porky."); };
		ZonaAndamio.BodyEntered += c => { if (c == Jugador) Indicar("andamio", "Un andamio cruzado en mitad del pasillo... Tendré que pasar agachado [C]."); };

		Objetivo("Examina la grabadora y el reloj de la Sala 1.");
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
		var lineas = new List<string>
		{
			"6:02 de la mañana. Sala 1. La confesión de Tito Garras estaba en esa grabadora... y ha desaparecido.",
			"Pero la grabadora sigue grabando. Sin cinta. Y el reloj de la pared se paró a las 3:12.",
			"¿Qué pasó en esta sala a las 3:12?",
		};
		if (_tutorial) lineas.Add("Bien: [W, A, S, D] para moverme, el ratón para mirar, [Mayús] para correr y [V] para verme desde fuera.");
		await Dialogo.Decir(_caneloso, lineas.ToArray());
		_enDialogo = false;
		Indicar("inicio", "Primero, la grabadora de la mesa. Me acerco y pulso [E].");
	}

	// ---------- Pistas y progreso ----------

	private void AlRegistrarEvidencia(string id)
	{
		_pistas.Add(id);
		Progreso();
		switch (id)
		{
			case Grabadora:
				Pensar("La tapa abierta, sin cinta... y la luz de REC encendida. Alguien la dejó grabando a propósito, o se le olvidó.");
				break;
			case Reloj:
				Pensar("Las 3:12 en punto. El cristal está rajado: no se paró solo, alguien lo golpeó.");
				break;
			case Huellas:
				Indicar("hablar", "Botas de agente camino del vestuario. El conserje friega al fondo: hablaré con él [E].");
				break;
			case Envoltorio:
				Pensar("Un envoltorio de sándwich con migas frescas junto a la C-2. ¿Quién le lleva la cena a un detenido de madrugada?");
				break;
			case NotaComisario:
				Pensar("«Dejar la grabadora de la Sala 1 en REC esta noche. Veremos quién duerme. — B.» La letra del comisario.");
				break;
			case Pluma:
				Pensar("Una pluma negra, larga, enganchada en la rejilla del techo. En esta comisaría no hay pájaros...");
				break;
			case Cinta:
				EmpezarResolucion();
				return;
		}
		if (_pistas.Contains(Grabadora) && _pistas.Contains(Reloj))
			Indicar("tablero", "La grabadora... el reloj... Si los uno en mi tablero [R], quizá vea la relación.");
		ActualizarObjetivo();
	}

	private void ActualizarObjetivo()
	{
		if (_resuelto) return;
		if (_pistas.Contains(Cinta)) { Objetivo("Vuelve a la Sala 1 y explica lo ocurrido desde la silla del sospechoso."); return; }
		if (!_pistas.Contains(Grabadora) || !_pistas.Contains(Reloj)) { Objetivo("Examina la grabadora y el reloj de la Sala 1."); return; }
		if (!_deducciones.Contains(D1)) { Objetivo("Une la grabadora y el reloj en el tablero [R]."); return; }
		if (!_deducciones.Contains(D2))
		{
			var pendientes = new List<string>();
			if (!_pistas.Contains(Huellas)) pendientes.Add("rastros en el pasillo");
			if (!_pistas.Contains(Turnos)) pendientes.Add("quién estaba de guardia (recepción)");
			Objetivo(pendientes.Count > 0
				? "¿Quién estaba despierto a las 3:12? Busca: " + string.Join(", ", pendientes) + "."
				: "Une las huellas con el libro de turnos en el tablero [R].");
			return;
		}
		if (!_autorizado) { Objetivo("Pide al comisario (recepción) permiso para entrar al vestuario."); return; }
		if (!Jugador.TieneObjeto(Combinacion) && _vioCandado) { Objetivo("Consigue la combinación del candado de Porky."); return; }
		Objetivo(Jugador.TieneObjeto(Combinacion)
			? "Abre el casillero de Porky con la combinación 3-1-2."
			: "Revisa los casilleros del vestuario, al oeste del pasillo.");
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
		if (Par(Huellas, Turnos)) return Deducir(D2, "Las huellas de bota son de Porky: era el único agente de guardia.");
		if (Par(Envoltorio, TEriz)) return Deducir(D3, "Eriz estuvo en las celdas con Tito, no en la Sala 1. No es nuestro hombre.");
		if (Par(NotaComisario, Grabadora) || Par(TComisario, Grabadora))
			return Deducir(D4, "El comisario dejó la grabadora en REC a propósito: una trampa para el que se duerme en la guardia.");
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
			lista.Add(new Opcion { Id = "evidencia", Tipo = "EVIDENCIA", Texto = "Enseñarle una prueba del expediente" });
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
					await Piensa("(No ha funcionado. Mejor elegir la prueba que contradiga lo que me ha dicho.)");
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
			"Eriz" => new[] { "No pienso decir nada más, detective.", "¿D-detective? Estoy fregando, ¿eh? Solo fregando.", "Dígame, detective.", "Lo que necesite, detective. Se lo debo." },
			"Porky" => new[] { "Sin mi representante sindical no hablo.", "¿Q-qué pasa ahora?", "Buenos días, detective. *bostezo*", "Lo que quiera. Ya no tengo nada que esconder." },
			_ => new[] { "Tengo trabajo, Caneloso. Abrevie.", "¿Y ahora qué, Caneloso?", "¿Novedades, Caneloso? El juicio es a las doce.", "Buen trabajo hasta ahora, Caneloso." },
		};
		return Habla(p, saludos[Mathf.Clamp(p.Animo + 2, 0, 3)]);
	}

	// ----- Eriz: conserje, ex carterista. Secreto: le llevó un sándwich a Tito a las 3:00. -----

	private List<Opcion> OpcionesEriz(Personaje p) => new List<Opcion>
	{
		new Opcion { Id = "e_noche", Tipo = "PREGUNTAR", Texto = "¿Qué hiciste anoche?", Accion = async () =>
		{
			if (p.Confeso) { await Habla(p, "Ya se lo dije: volví a las tres para llevarle un sándwich a Tito. Nada más."); return; }
			await Habla(p, "Fregué la Sala 1 y me fui a la una en punto. Está firmado en el libro de turnos.");
			Nota("Eriz dice que se fue a la 1:00 y que lo firmó en el libro de turnos.");
		} },
		new Opcion { Id = "e_tito", Tipo = "PREGUNTAR", Texto = "¿Conoces a Tito Garras, el detenido de la C-2?", Accion = async () =>
		{
			if (p.Confeso) { await Habla(p, "Compartimos celda hace veinte años. Tito no es mala gente... para ser carterista."); return; }
			CambiarAnimo(p, 0);
			await Habla(p, "¿Tito? Eh... de vista. Todo el mundo conoce a Tito.");
			await Piensa("(Se le erizan las púas al oír el nombre. Miente... o tiene miedo.)");
			Nota("Eriz se pone nervioso al hablar de Tito Garras.");
		} },
		new Opcion { Id = "e_312", Tipo = "PREGUNTAR", Texto = "¿Oíste algo a las 3:12?", Visible = () => _pistas.Contains(Reloj), Accion = async () =>
		{
			if (!p.Confeso) { await Habla(p, "¿A las tres? Yo ya estaba en casa, durmiendo."); await Piensa("(Eso no cuadra con las migas frescas de la C-2... si las encuentro.)"); return; }
			await Habla(p, "Sí... Un golpe seco en la Sala 1, como si se cayera algo. Y al rato, la puerta del vestuario.");
			Nota("Eriz oyó un golpe en la Sala 1 hacia las 3:12 y después la puerta del vestuario.");
		} },
		new Opcion { Id = "e_presionar", Tipo = "PRESIONAR", Texto = "Tienes llave de todo... y un pasado de carterista, «Dedos».", Visible = () => !p.Confeso, Accion = async () =>
		{
			CambiarAnimo(p, -1);
			await Habla(p, "¡Eso fue hace veinte años! Ahora friego suelos y pago mis impuestos.");
			await Piensa("(Sin pruebas solo consigo que se cierre. Necesito algo que le contradiga.)");
		} },
	};

	private async Task<bool> EvidenciaEriz(Personaje p, string id)
	{
		switch (id)
		{
			case Envoltorio:
				if (p.Confeso) { await Habla(p, "Sí, sí, el sándwich. Ya está confesado."); return true; }
				p.Confeso = true;
				CambiarAnimo(p, 3);
				_confrontaciones.Add("eriz_envoltorio");
				await Habla(p, "...Vale. Vale. Volví a las tres. Le llevé un sándwich de mortadela a Tito, a la C-2. Aquí no le dan de cenar.",
					"Fuimos compañeros de celda hace veinte años. Si el comisario se entera, me despide.",
					"Pero no toqué ninguna grabadora, se lo juro. Y con estas zapatillas de goma no dejo ni huella.");
				AñadirTestimonio(TEriz, "Eriz: volvió a las 3:00", "Eriz volvió a las 3:00 para llevar un sándwich a Tito Garras (C-2). Fueron compañeros de celda.");
				Indicar("testimonio", "Lo que me cuentan también va a mi tablero [R]. Y lo apunto en la libreta [Q].");
				return true;
			case Huellas:
				await Habla(p, "¿Huellas de bota? Mire mis pies, detective: zapatillas de goma. Las botas son cosa de agentes.");
				Nota("Eriz lleva zapatillas de goma: las huellas de bota no son suyas.");
				return true;
			case Turnos:
				await Habla(p, p.Confeso ? "Me fui a la una, sí... y volví. Eso no lo firmé." : "Ahí lo pone: salida, una en punto. ¿Lo ve?");
				return true;
			case Reloj:
				await Habla(p, "¿El reloj de la Sala 1? Lo limpié a las doce y funcionaba. Se lo juro.");
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
			await Habla(p, "Aquí, en esta silla. Toda la guardia, despierto como un búho.");
			Nota("Porky dice que pasó toda la guardia en recepción, despierto.");
		} },
		new Opcion { Id = "p_raro", Tipo = "PREGUNTAR", Texto = "¿Viste algo raro anoche?", Accion = async () =>
		{
			await Habla(p, "Nada. Bueno... el conserje rondaba por las celdas. Ese tiene llave de todo, ¿eh?");
			Nota("Porky señala al conserje: «tiene llave de todo».");
		} },
		new Opcion { Id = "p_312", Tipo = "PREGUNTAR", Texto = "¿Qué hacías a las 3:12?", Visible = () => _pistas.Contains(Reloj), Accion = async () =>
		{
			if (!p.Confeso)
			{
				CambiarAnimo(p, -1);
				await Habla(p, "Vigilar. Muy... intensamente.");
				await Piensa("(Se le cae una miga de donut del hocico. Está nervioso.)");
				return;
			}
			await Habla(p, "Me desperté a las tres y cuarto: el reloj de la sala estaba en el suelo. Lo colgué sin mirar la hora.");
			Nota("Porky despertó hacia las 3:15 con el reloj de la Sala 1 tirado en el suelo.");
		} },
		new Opcion { Id = "p_presionar", Tipo = "PRESIONAR", Texto = "Tienes cara de haber dormido... y no en tu cama.", Visible = () => !p.Confeso && !_deducciones.Contains(D2), Accion = async () =>
		{
			CambiarAnimo(p, -1);
			await Habla(p, "¡Oiga! Llevo once años en el cuerpo. ¡Hablaré con el sindicato!");
			await Piensa("(Se ha cerrado. Sin pruebas, presionar solo me complica las cosas.)");
		} },
		new Opcion { Id = "p_deduccion", Tipo = "PRESIONAR", Texto = "Eras el único agente de guardia... y tus botas fueron al vestuario.", Visible = () => !p.Confeso && _deducciones.Contains(D2), Accion = async () =>
		{
			p.Confeso = true;
			_confrontaciones.Add("porky_deduccion");
			bool coopera = p.Animo >= -1;
			CambiarAnimo(p, 2);
			await Habla(p, "...Está bien. Me quedé dormido en la Sala 1. La silla del sospechoso es comodísima.",
				"Cuando desperté, la luz de REC estaba encendida. ¡Pensé que me había grabado roncando!",
				"Saqué la cinta y la escondí en mi casillero. Iba a borrar lo mío y devolverla, lo juro.");
			AñadirTestimonio(TPorky, "Porky: se durmió", "Porky se durmió en la Sala 1. Al despertar vio la grabadora en REC, sacó la cinta y la escondió en su casillero.");
			if (coopera)
			{
				await Habla(p, "La combinación es 3-1-2. Pero no se lo diga al comisario.");
				Jugador.DarObjeto(Combinacion, "3-1-2. La combinación del casillero de Porky.");
			}
			else
			{
				await Habla(p, "Y la combinación no se la pienso dar. Que la busque usted.");
				await Piensa("(Por presionarle antes de tiempo. Tendré que encontrarla yo... ¿en sus cosas?)");
			}
		} },
	};

	private async Task<bool> EvidenciaPorky(Personaje p, string id)
	{
		switch (id)
		{
			case Huellas:
				if (_confrontaciones.Add("porky_huellas")) CambiarAnimo(p, -1);
				await Habla(p, "¿Botas? Todos los agentes llevamos botas. Bueno... anoche solo estaba yo.");
				Nota("Porky admite que anoche era el único agente en la comisaría.");
				return true;
			case Turnos:
				await Habla(p, "Sí, guardia de dos a cuatro. Firmé y todo. ¿Y qué?");
				return true;
			case Envoltorio:
				await Habla(p, "¿Un envoltorio de sándwich? Yo soy más de donuts, detective.");
				return true;
			case Reloj:
				await Habla(p, p.Confeso ? "Ese reloj estaba en el suelo cuando me desperté." : "Las 3:12... Ni idea. Yo estaba... aquí.");
				return true;
			case NotaComisario:
				await Habla(p, "¡¿El comisario la dejó grabando a propósito?! ¡Era una trampa! ...Y caí como un donut en el café.");
				return true;
		}
		return false;
	}

	// ----- Comisario: secreto, dejó la grabadora en REC para pillar al que se duerme. -----

	private List<Opcion> OpcionesComisario(Personaje p) => new List<Opcion>
	{
		new Opcion { Id = "c_grabadora", Tipo = "PREGUNTAR", Texto = "¿Qué sabe de la grabadora?", Accion = async () =>
		{
			if (p.Confeso) { await Habla(p, "Ya lo sabe: la dejé en REC a propósito. No hace falta que lo repita."); return; }
			await Habla(p, "Que tenía la confesión de Tito Garras y que ha volado. Yo no la toco desde ayer a las seis.");
			Nota("El comisario dice que no tocó la grabadora desde las 18:00.");
		} },
		new Opcion { Id = "c_guardia", Tipo = "PREGUNTAR", Texto = "¿Quién vigilaba anoche?", Accion = async () =>
		{
			await Habla(p, "Porky, de dos a cuatro. Un buen chico... cuando está despierto.");
			Nota("Guardia nocturna: Porky, de 2:00 a 4:00.");
		} },
		new Opcion { Id = "c_tito", Tipo = "PREGUNTAR", Texto = "¿Quién es Tito Garras?", Accion = async () =>
		{
			await Habla(p, "Un carterista con dedos de seda. Ayer confesó el robo de la cartera del alcalde. Sin cinta, mañana sale libre.",
				"Juraba que trabaja para alguien al que llama «La Garra». Tonterías de ladrón.");
			Nota("Tito Garras confesó el robo de la cartera del alcalde. Habló de alguien llamado «La Garra».");
		} },
		new Opcion { Id = "c_permiso", Tipo = "PREGUNTAR", Texto = "Necesito entrar en el vestuario.", Visible = () => !_autorizado, Accion = async () =>
		{
			if (!_deducciones.Contains(D2))
			{
				await Habla(p, "¿El vestuario de mis agentes? Tráeme algo sólido primero.");
				await Piensa("(Algo sólido... Debería unir mis pistas en el tablero [R].)");
				return;
			}
			await Habla(p, "Botas de agente y un solo agente de guardia... Entendido. Toma la llave.",
				"Y ojo: Porky le pone candado de combinación a todo.");
			_autorizado = true;
			PuertaVestuario.Desbloquear();
			Jugador.MostrarAviso("Nueva zona disponible: Vestuario", 4.0);
			Progreso();
		} },
		new Opcion { Id = "c_presionar", Tipo = "PRESIONAR", Texto = "¿Seguro que no tocó la grabadora anoche?", Visible = () => !p.Confeso, Accion = async () =>
		{
			CambiarAnimo(p, -1);
			await Habla(p, "¿Me está acusando a mí, Caneloso? Cuidado con lo que insinúa.");
			await Piensa("(Ha mirado el cajón de su escritorio antes de contestar...)");
		} },
	};

	private async Task<bool> EvidenciaComisario(Personaje p, string id)
	{
		switch (id)
		{
			case NotaComisario:
				if (p.Confeso) { await Habla(p, "Sí, Caneloso, esa nota es mía. Ya lo hemos hablado."); return true; }
				p.Confeso = true;
				_confrontaciones.Add("comisario_nota");
				CambiarAnimo(p, 1);
				await Habla(p, "...Ejem. Sí. La dejé grabando. Llevo semanas sospechando que alguien duerme en las guardias.",
					"Pensé que la grabadora lo pillaría. No imaginé que el dormilón se llevaría la cinta... ni que nos jugaríamos el juicio.",
					"Que esto no salga de aquí, Caneloso.");
				AñadirTestimonio(TComisario, "Comisario: la trampa", "El comisario dejó la grabadora en REC a propósito para pillar al que se duerme en la guardia.");
				return true;
			case Turnos:
				await Habla(p, "La nota del margen es mía: «Grabadora Sala 1: NO TOCAR». ¿Algún problema?");
				await Piensa("(¿Por qué no se podía tocar una grabadora... de noche?)");
				return true;
			case Cinta:
				await Habla(p, "¡La cinta! Ahora explícame quién y por qué. En la Sala 1, Caneloso.");
				return true;
			case Huellas:
				await Habla(p, "Botas reglamentarias. Bien visto. ¿Y quién estaba de guardia?");
				return true;
		}
		return false;
	}

	// ---------- Resolución (estilo Obra Dinn: solo se confirma si todo encaja) ----------

	private void EmpezarResolucion()
	{
		Progreso();
		Indicar("resolver", "Todo empieza a encajar. De vuelta a la Sala 1: desde la silla del sospechoso explicaré lo ocurrido.");
		Eriz.GlobalTransform = PuntoErizFinal.GlobalTransform;
		Porky.GlobalTransform = PuntoPorkyFinal.GlobalTransform;
		SillaAcusacion.Habilitar(true, CapaInteractuable);
		ActualizarObjetivo();
	}

	private async Task Resolver()
	{
		if (_resuelto) return;
		await Piensa("Repasemos. Tres preguntas, tres respuestas. Si una falla, nada encaja.");
		int quien = await Dialogo.Elegir(_caneloso, "¿Quién sacó la cinta de la grabadora?",
			new List<OpcionVista> { new("Eriz, el conserje"), new("Porky, el agente de guardia"), new("El comisario") });
		int porque = await Dialogo.Elegir(_caneloso, "¿Por qué la sacó?",
			new List<OpcionVista> { new("Para vendérsela a Tito Garras"), new("Creyó que la grabadora lo había grabado dormido"), new("Para sabotear el juicio") });
		int rec = await Dialogo.Elegir(_caneloso, "¿Por qué estaba la grabadora grabando de madrugada?",
			new List<OpcionVista> { new("Porky la dejó encendida"), new("Eriz la puso en marcha al limpiar"), new("El comisario la dejó en REC como trampa") });
		_intentosAcusacion++;

		if (quien != 1 || porque != 1 || rec != 2)
		{
			await Piensa("Algo no encaja todavía.", "Repasaré el tablero [R] y la libreta [Q]. Alguien me ha mentido... y no solo una persona.");
			Progreso();
			return;
		}

		_resuelto = true;
		SillaAcusacion.Habilitar(false, CapaInteractuable);
		await Habla(_porky, "Lo siento mucho, detective. La próxima guardia la hago de pie.");
		await Habla(_comisario, "Y yo me guardaré mis trampas. La confesión está a salvo y el juicio seguirá. Buen trabajo, Caneloso.");
		await Piensa("Antes de entregarla... escucharé la cinta entera.");
		await Dialogo.Decir(Dialogo.HablantePorNombre("Cinta"),
			"[Voz de Tito Garras] «...y sí, me llevé la cartera del alcalde. Lo confieso.»",
			"[Ronquidos. Alguien murmura: «...cinco minutitos más...»]",
			"[03:12 · Un roce metálico. Una voz que nadie reconoce, muy cerca del micrófono:]",
			"«La Garra no olvida, Caneloso.»",
			"[Fin de la cinta]");
		await Piensa(_pistas.Contains(Pluma)
			? new[] { "Esa voz no es de nadie de esta comisaría. Alguien más estuvo aquí a las 3:12.", "La pluma negra de la rejilla... Entró por la ventilación. Y sabía que yo escucharía esto." }
			: new[] { "Esa voz no es de nadie de esta comisaría. Alguien más estuvo aquí a las 3:12.", "¿Por dónde entró? La sala solo tiene una puerta... y una rejilla en el techo." });
		Nota("En la cinta, a las 3:12, una voz desconocida: «La Garra no olvida, Caneloso». Caso abierto.");
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
			$"Pistas: {pistas}/{PistasTotales}{(_pistas.Contains(Pluma) ? " (¡incluida la secreta!)" : "")} · Deducciones: {deducciones}/4",
			$"Confrontaciones clave: {confrontaciones}/4 · Resuelto al primer intento: {(primera ? "sí" : "no (" + _intentosAcusacion + " intentos)")}",
			$"Tiempo: {(int)tiempo.TotalMinutes} min {tiempo.Seconds:00} s · CALIFICACIÓN: {nota}",
			nota == "S" ? "Impecable, detective." : "¿Te faltó algo? Hay más secretos en la comisaría de los que parece.");
	}

	// ---------- Zonas y tutorial ----------

	private void AlEntrarPasillo()
	{
		if (!Jugador.LinternaEncendida && !_pistas.Contains(Huellas))
			Indicar("linterna", "No veo ni mis propias patas. Mi linterna [F]...");
	}

	private void AlEntrarRecepcion()
	{
		if (_vioCandado && !Jugador.TieneObjeto(Combinacion))
			Indicar("agacharse_banco", "Ese bulto bajo el banco de espera... Si me agacho [C], podré verlo.");
		else if (!_pistas.Contains(Turnos))
			Indicar("recoger", "El libro de turnos, en el mostrador. Me lo llevo [E].");
	}

	private void AlCambiarLinterna(bool encendida)
	{
		if (encendida && !_pistas.Contains(Huellas))
			Indicar("buscar_huellas", "Con luz directa se ven cosas que antes no... ¿Qué hay en el suelo?");
	}

	private void AlEncontrarCandado()
	{
		if (Jugador.TieneObjeto(Combinacion)) return;
		_vioCandado = true;
		Indicar("mochila", "Candado de combinación. Porky no se separa de sus cosas... ¿Las dejó en recepción?");
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
		if (!_pistas.Contains(Grabadora)) return claro ? "La grabadora de la mesa. Debería examinarla [E]." : "Esa luz roja de la mesa no deja de parpadear...";
		if (!_pistas.Contains(Reloj)) return claro ? "El reloj de la pared... ¿está parado?" : "Hay algo raro en esta sala... ¿qué hora es?";
		if (!_deducciones.Contains(D1)) return claro ? "Si uno la grabadora y el reloj en el tablero [R]..." : "La grabadora y el reloj... ¿estarán relacionados?";
		if (!_pistas.Contains(Huellas)) return claro ? "El suelo del pasillo está muy oscuro. Con la linterna [F]..." : "Si alguien salió de la sala de noche, habrá dejado rastro.";
		if (!_pistas.Contains(Turnos)) return claro ? "En recepción tiene que haber un registro de turnos." : "¿Quién estaba de guardia anoche?";
		if (!_deducciones.Contains(D2)) return claro ? "Huellas de bota y un solo agente de guardia. Al tablero [R]." : "Esas huellas... ¿de quién pueden ser?";
		if (!_autorizado) return claro ? "Hablaré con el comisario en recepción." : "El comisario tiene la llave del vestuario.";
		if (_vioCandado && !Jugador.TieneObjeto(Combinacion)) return claro ? "¿Y si la mochila de Porky está bajo el banco de espera? Agachado [C]." : "Porky no se separa de sus cosas...";
		if (!_pistas.Contains(Cinta)) return claro ? "El casillero de Porky, en el vestuario." : "Si yo escondiera algo aquí, ¿dónde sería?";
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
