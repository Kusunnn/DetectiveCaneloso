using Godot;
using System;

// Caso 1 «La Noche del Ciervo de Luz» — Escenario 2: el Lago y el Bosque (HU-20.6).
// Las pistas del sendero, el bosque, la orilla, el embarcadero y el cobertizo de botes (cerrado hasta tener el
// llavero de Porky). Desde aquí se vuelve al Campamento o se va a la estación con Hecdog.
// Es el único script que conoce la historia de este escenario; los sistemas siguen siendo genéricos.
public partial class Caso1Lago : EscenarioCaso1
{
	private const string HuellasPezuna = "huellas_pezuna";
	private const string HuellasDetenidas = "huellas_detenidas";
	private const string Altavoz = "altavoz";
	private const string Cornamenta = "cornamenta_zancos";
	private const string HuecoRemo = "hueco_remo";
	private const string RemoSangre = "remo_sangre";
	private const string Silbato = "silbato_ts";
	private const string Linterna = "linterna_porky";
	private const string Cuaderno = "cuaderno_eriz";
	private const int PistasTotales = 9;

	private const string Llavero = "llavero_porky";

	[Export] public Puerta PuertaCobertizo;
	[Export] public PuntoInteraccion VolverCampamento;
	[Export] public PuntoInteraccion IrEstacion;
	// La estática del altavoz escondido: se apaga cuando Caneloso lo encuentra
	[Export] public AudioStreamPlayer3D Estatica;

	// El tablado cruje bajo los pasos: trocitos al azar de dos grabaciones largas
	private Area3D[] _muelle = Array.Empty<Area3D>();
	private double _crujido = 1.5;
	private double _finCrujido;
	private AudioStreamPlayer3D _tablado;

	protected override string Escena => PartidaEscenas.Lago;
	protected override string Lugar => "el lago";
	protected override string ZonaPorDefecto => "Lago y bosque";

	public override void _Ready()
	{
		base._Ready();
		PuertaCobertizo.Bloqueada = true;
		PuertaCobertizo.MensajeBloqueada = "Cobertizo de botes. Cerrado con llave: solo Tony y Porky tienen copia.";
		VolverCampamento.Usado += () => Conversar(AlVolverCampamento);
		IrEstacion.Usado += () => Conversar(AlIrEstacion);
		_muelle = Array.FindAll(Zonas, z => z.Name == "Embarcadero" || z.Name == "Lago");
		_tablado = new AudioStreamPlayer3D { Bus = Sonidos.BusEfectos, UnitSize = 3f, MaxDistance = 14f };
		AddChild(_tablado);
		ActualizarObjetivo();
	}

	protected override async void AlLlegar(bool desdeOtroEscenario)
	{
		AbrirSiTieneLlavero(false);
		ActualizarObjetivo();
		if (Pistas.Count > 0 || !IsInsideTree()) return;
		await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree() || EnDialogo) return;
		EnDialogo = true;
		await Dialogo.Decir(Caneloso,
			"El sendero del Lago Espejo.",
			"Aquí abajo vieron al «ciervo de luz». Y aquí encontraron a Porky.",
			"El agua todavía brilla.");
		EnDialogo = false;
	}

	// ---------- Pistas ----------

	protected override void AlRegistrarEvidencia(string id)
	{
		string pensamiento = id switch
		{
			HuellasPezuna => "Muy hondas y con un paso larguísimo. Ningún venado camina así.",
			HuellasDetenidas => "Se acaban de golpe, a unos treinta metros del muelle. El «ciervo» se bajó de algo.",
			Altavoz => "De aquí salían los «bramidos». Alguien preparó el espectáculo.",
			Cornamenta => "El disfraz del ciervo... y unos zancos con pezuñas. Ahí están las huellas.",
			HuecoRemo => "Falta un remo. Y este cobertizo solo lo abren dos llaves.",
			RemoSangre => "Sangre en la pala. Lo tiraron aquí con prisa.",
			Silbato => "«T. S.». El cordón se rompió en el clavo... alguien retrocedió de golpe.",
			Linterna => "La linterna de Porky, encendida en el fondo. Ella estuvo en este muelle.",
			Cuaderno => "«23:20: perturbación luminosa fuerte junto al embarcadero.» Eso fija la hora.",
			_ => null,
		};
		if (id == Altavoz && Estatica != null) Estatica.Stop();
		if (pensamiento != null) Jugador.MostrarPensamiento(pensamiento, 6.0);
		Hud.Call("agregar_nota", NotaDe(id));
		ActualizarObjetivo();
	}

	private static string NotaDe(string id) => id switch
	{
		HuellasPezuna => "Sendero del lago: huellas de pezuña muy hondas y con un paso demasiado largo.",
		HuellasDetenidas => "Las huellas de pezuña se detienen a unos 30 m del embarcadero.",
		Altavoz => "Bosque: un altavoz escondido en un pino. De ahí salían los bramidos.",
		Cornamenta => "Tronco hueco: cornamenta plateada y zancos con pezuñas. Tienen pelos de gato a rayas.",
		HuecoRemo => "Cobertizo de botes: falta un remo en el soporte. Solo Tony y Porky tienen llave.",
		RemoSangre => "Entre los juncos, junto al embarcadero: un remo con sangre.",
		Silbato => "Embarcadero: silbato con las iniciales «T. S.», enganchado en un clavo, con el cordón roto.",
		Linterna => "La linterna de Porky, encendida, en el fondo del agua junto al embarcadero.",
		Cuaderno => "Cuaderno de Eriz: «23:20: perturbación luminosa fuerte junto al embarcadero».",
		_ => "Pista: " + id,
	};

	protected override void AlObtenerObjeto(string id)
	{
		if (id == Llavero) AbrirSiTieneLlavero(true);
	}

	protected override void AlRestaurar() => AbrirSiTieneLlavero(false);

	// El llavero de Porky (de la cocina del Campamento) también abre el cobertizo de botes
	private void AbrirSiTieneLlavero(bool avisar)
	{
		if (!Jugador.TieneObjeto(Llavero) || !PuertaCobertizo.Bloqueada) return;
		PuertaCobertizo.Desbloquear();
		if (avisar) Jugador.MostrarAviso("Nueva zona disponible: Cobertizo de botes", 4.0);
	}

	protected override void ActualizarObjetivo()
	{
		string cuenta = $" ({Pistas.Count}/{PistasTotales} pistas del lago)";
		if (Pistas.Count >= PistasTotales) { Objetivo("Ya revisé el lago. Hecdog espera en la patrulla para ir a la estación."); return; }
		if (!Jugador.TieneObjeto(Llavero) && !Pistas.Contains(HuecoRemo) && Pistas.Count >= 5)
		{ Objetivo("El cobertizo está cerrado. Porky tenía las llaves: búscalas en el Campamento." + cuenta); return; }
		if (Jugador.TieneObjeto(Llavero) && !Pistas.Contains(HuecoRemo)) { Objetivo("Abre el cobertizo de botes con el llavero de Porky." + cuenta); return; }
		Objetivo("Revisa el sendero, el bosque, la colina, la orilla y el embarcadero." + cuenta);
	}

	// ---------- Zonas ----------

	protected override string PensamientoDeZona(string zona) => zona switch
	{
		"Sendero" => "Lodo fresco. Por aquí bajó medio campamento anoche... y algo más.",
		"Bosque" => "Troncos caídos y niebla. Buen lugar para esconder algo.",
		"Colina" => "Desde aquí se ve todo: el muelle, la orilla y la enfermería.",
		"Orilla" => "Arena, juncos y latas. Alguien estuvo aquí sentado un buen rato.",
		"Embarcadero" => "El tablado cruje. Aquí terminó todo.",
		"Cobertizo" => "Huele a humedad y a barniz. Remos, redes, una canoa...",
		"Lago" => "El agua brilla azul alrededor del muelle. Como dicen las historias.",
		_ => null,
	};

	protected override string NombreZona(string zona) => zona switch
	{
		"Sendero" => "Sendero del lago",
		"Bosque" => "Bosque",
		"Colina" => "Colina",
		"Orilla" => "Orilla",
		"Embarcadero" => "Embarcadero",
		"Cobertizo" => "Cobertizo de botes",
		"Lago" => "Punta del embarcadero",
		_ => zona,
	};

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_tablado.Playing)
		{
			_tablado.GlobalPosition = Jugador.GlobalPosition;
			_finCrujido -= delta;
			if (_finCrujido <= 0) _tablado.Stop();
		}
		// Cruje el tablado de vez en cuando mientras se camina por el muelle
		if (EnDialogo || Jugador.Velocity.LengthSquared() < 0.5f) return;
		bool enMuelle = false;
		foreach (var area in _muelle) if (area.OverlapsBody(Jugador)) { enMuelle = true; break; }
		if (!enMuelle) return;
		_crujido -= delta;
		if (_crujido > 0) return;
		_crujido = GD.RandRange(2.0, 5.0);
		var tipo = GD.Randf() < 0.65f ? Sonidos.Tipo.MuelleCrujido : Sonidos.Tipo.MuellePasos;
		_tablado.Stream = Sonidos.Obtener(tipo);
		_tablado.VolumeDb = Sonidos.VolumenDe(tipo) - 2f;
		_tablado.GlobalPosition = Jugador.GlobalPosition;
		_tablado.Play((float)GD.RandRange(0.0, Math.Max(0.0, _tablado.Stream.GetLength() - 2.0)));
		_finCrujido = GD.RandRange(0.9, 1.6);
	}

	// ---------- Salidas ----------

	private async System.Threading.Tasks.Task AlVolverCampamento()
	{
		int opcion = await Dialogo.Elegir("Caneloso", "¿Regreso al Campamento?", "Sí, subo", "Todavía no");
		if (opcion != 0) return;
		IrA(PartidaEscenas.Campamento, "LlegadaDesdeLago");
	}

	private async System.Threading.Tasks.Task AlIrEstacion()
	{
		var hecdog = Dialogo.HablantePorNombre("Hecdog");
		int opcion = await Dialogo.Elegir(hecdog, "¿Nos vamos a la estación, detective? Nya.",
			new[] { new OpcionVista("Sí, vámonos"), new OpcionVista("Todavía no") }, 1);
		if (opcion != 0) return;
		IrA(PartidaEscenas.Sala, "EntradaDesdeLago");
	}
}
