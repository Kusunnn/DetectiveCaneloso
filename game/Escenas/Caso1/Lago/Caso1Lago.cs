using Godot;
using System;

// Caso 1 «La Noche del Ciervo de Luz» — Escenario 2: el Lago y el Bosque (HU-20.6).
// Las pistas del sendero, el bosque, la orilla, el embarcadero y el cobertizo de botes (cerrado hasta tener el
// llavero de Porky). Desde aquí se vuelve al Campamento o se va a la estación con Hecdog.
// Es el único script que conoce la historia de este escenario; los sistemas siguen siendo genéricos.
public partial class Caso1Lago : EscenarioCaso1
{
	private const string Altavoz = "altavoz";
	private const string HuecoRemo = "hueco_remo";
	private const int PistasTotales = 9;

	private const string Llavero = "llavero_porky";

	// Pasos en el orden de la historia: el «ciervo» que no era un ciervo, quién estaba en la orilla, el crimen en el
	// embarcadero y de dónde salió el remo. Se puede recoger todo en cualquier orden; el objetivo propone el primero que falte.
	private static readonly (string Texto, string[] Pistas)[] Pasos =
	{
		("El sendero baja al lago: ¿por dónde caminó el «ciervo»?", new[] { HuellasPezuna, HuellasDetenidas }),
		("Los bramidos salían del bosque, al oeste del sendero.", new[] { Altavoz, Cornamenta }),
		("En la orilla oeste alguien pasó un buen rato sentado.", new[] { Cuaderno }),
		("El embarcadero: ahí terminó todo. El agua aún brilla.", new[] { Silbato, Linterna, RemoSangre }),
		("¿De dónde salió el remo? El cobertizo de botes, al este del muelle.", new[] { HuecoRemo }),
	};
	// Sin estas, el embarcadero queda sin revisar: Hecdog lo recuerda antes de irse a la estación
	private static readonly string[] PruebasEmbarcadero = { RemoSangre, Silbato, Cuaderno };

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
		// Lo que piensa al llegar sale una sola vez (antes se repetía cada vez que volvía sin pistas del lago)
		if (Pistas.Count > 0 || !IsInsideTree() || !PrimeraVez("_llegada")) return;
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
		if (id == Altavoz && Estatica != null) Estatica.Stop();
		// Lo que piensa Caneloso y lo que anota viene del guion (Datos/Dialogos/caso1_pistas.json)
		if (!Guion.ComentarPista(Jugador, id)) Hud.Call("agregar_nota", "Pista: " + id);
		ActualizarObjetivo();
	}

	protected override void AlDescubrirDetalle(string origen, string id)
	{
		if (Guion.NotaDeDetalle(id) is string nota) Hud.Call("agregar_nota", nota);
	}

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
		if (Guion.Resuelto) { Objetivo("Caso cerrado."); return; }
		string cuenta = $" ({Pistas.Count}/{PistasTotales} pistas del lago)";
		if (Pistas.Count >= PistasTotales) { Objetivo("Ya revisé el lago. Hecdog espera en la patrulla para ir a la estación."); return; }
		if (!Jugador.TieneObjeto(Llavero) && !Pistas.Contains(HuecoRemo) && Pistas.Count >= 5)
		{ Objetivo("El cobertizo está cerrado. Porky tenía las llaves: búscalas en el Campamento." + cuenta); return; }
		foreach (var (texto, ids) in Pasos)
		{
			if (Array.TrueForAll(ids, Pistas.Contains)) continue;
			// El cobertizo está cerrado: con el llavero se abre; sin él, hay que volver por él
			if (ids[0] == HuecoRemo && Jugador.TieneObjeto(Llavero)) { Objetivo("Abre el cobertizo de botes con el llavero de Porky." + cuenta); return; }
			Objetivo(texto + cuenta);
			return;
		}
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
		// HU-9.5 CA-03: Hecdog presenta a los seis en la estación; antes hay que reunir pruebas aquí
		if (!Guion.SalaVista && Pistas.Count < PartidaEscenas.PistasLagoParaEstacion)
		{
			await Dialogo.Decir(Caneloso, "Todavía no. Antes de sentarlos frente a mí, quiero saber qué pasó en este lago.",
				$"Me faltan cosas por revisar ({Pistas.Count}/{PistasTotales} pistas del lago).");
			return;
		}
		var hecdog = Dialogo.HablantePorNombre("Hecdog");
		if (!Array.TrueForAll(PruebasEmbarcadero, Pistas.Contains))
			await Dialogo.Decir(hecdog, "Todavía no revisa bien la orilla y el embarcadero, detective. Ahí la encontraron. Nya.");
		int opcion = await Dialogo.Elegir(hecdog, "¿Nos vamos a la estación, detective? Nya.",
			new[] { new OpcionVista("Sí, vámonos"), new OpcionVista("Todavía no") }, 1);
		if (opcion != 0) return;
		IrA(PartidaEscenas.Sala, "EntradaDesdeLago");
	}
}
