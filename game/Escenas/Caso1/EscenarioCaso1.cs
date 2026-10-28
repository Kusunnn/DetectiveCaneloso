using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Diccionario = Godot.Collections.Dictionary;
using Arreglo = Godot.Collections.Array;

// Base de los escenarios del Caso 1 que vienen después del Campamento (HU-20.6): el Lago y el Bosque, y la sala de
// interrogatorio de la estación. Hace lo que todos comparten: HUD del caso, tablero, zonas con su pensamiento,
// conversaciones, y el guardado repartido por escenarios (PartidaEscenas). Cada escenario pone su guion encima.
public abstract partial class EscenarioCaso1 : Node, IEstadoPartida, ICuadernoPistas
{
	[Export] public Jugador Jugador;
	[Export] public CanvasLayer Hud;
	[Export] public VentanaDialogo Dialogo;
	[Export] public TableroDeduccion Tablero;
	[Export] public MusicaFondo Musica;
	[Export] public WorldEnvironment Entorno;
	// Contenedores con una pista adentro: la pista no se ve ni se puede tomar hasta abrirlo
	[Export] public Contenedor[] ContenedoresConPista = Array.Empty<Contenedor>();
	[Export] public ObjetoPista[] PistasDentro = Array.Empty<ObjetoPista>();
	// Áreas de cada zona (el nombre del nodo es el de la zona): al entrar, Caneloso piensa algo una vez
	[Export] public Area3D[] Zonas = Array.Empty<Area3D>();
	// Versión ligera para equipos modestos: sin niebla volumétrica ni SSAO
	[Export] public bool VersionLigera = false;

	protected const uint CapaInteractuable = 1u << 3;

	// Pistas de este escenario que ya están en el diario
	protected readonly HashSet<string> Pistas = new HashSet<string>();
	private readonly HashSet<string> _zonasVistas = new HashSet<string>();
	protected Hablante Caneloso;
	// El guion del caso (HU-27): su estado es el mismo en los tres escenarios
	protected GuionCaso1 Guion;
	protected bool EnDialogo;
	private bool _restaurando;
	private double _tiempoCaso;
	private Diccionario _escenas = new();

	// Ruta del .tscn de este escenario (la clave de su estado en la partida)
	protected abstract string Escena { get; }
	protected abstract string Lugar { get; }
	protected abstract string NombreZona(string zona);
	protected abstract string ZonaPorDefecto { get; }
	protected abstract string PensamientoDeZona(string zona);
	// Al llegar sin partida guardada de por medio (desde otro escenario o probando la escena sola)
	protected abstract void AlLlegar(bool desdeOtroEscenario);
	protected virtual void AlRegistrarEvidencia(string id) { }
	protected virtual void AlDescubrirDetalle(string origen, string id) { }
	protected virtual void AlObtenerObjeto(string id) { }
	protected virtual void ActualizarObjetivo() { }

	public override void _Ready()
	{
		Caneloso = Dialogo.HablantePorNombre("Caneloso");
		Caneloso.TonoVoz = 1f;
		Guion = new GuionCaso1(Dialogo, Hud, Caneloso, Cambio)
		{
			AlTerminar = motivo => { if (!IsInsideTree()) return; ActualizarObjetivo(); Autoguardar(motivo); },
		};

		Hud.Set("caso", "01");
		Hud.Set("archivo", "La Noche del Ciervo de Luz");
		Hud.Set("lugar", Lugar);
		Jugador.EvidenciaRegistrada += id => { Pistas.Add(id); Cambio(); Autoguardar("Pista: " + id); AlRegistrarEvidencia(id); };
		Jugador.ObjetoObtenido += id => { Cambio(); AlObtenerObjeto(id); };
		Jugador.EvidenciaExaminada += _ => Cambio();
		Jugador.DetalleDescubierto += (origen, id) => { AlDescubrirDetalle(origen, id); Cambio(); Autoguardar("Detalle: " + id); };

		Tablero.ObtenerCartas = Cartas;
		Tablero.ObtenerConclusiones = () => Array.Empty<string>();
		Tablero.Probar = (_, _) => null; // Las deducciones llegan con los interrogatorios

		for (int i = 0; i < ContenedoresConPista.Length && i < PistasDentro.Length; i++)
		{
			var pista = PistasDentro[i];
			Esconder(pista, true);
			ContenedoresConPista[i].Abierto += () => Esconder(pista, false);
		}
		foreach (var zona in Zonas)
		{
			var area = zona;
			area.BodyEntered += cuerpo => { if (cuerpo == Jugador) AlEntrarZona(area.Name); };
		}

		AddToGroup("estado_partida");
		foreach (var contenedor in Todos<Contenedor>()) contenedor.Abierto += Cambio;
		foreach (var puerta in Todos<Puerta>()) puerta.Abierta += Cambio;
		if (VersionLigera) AplicarVersionLigera();
		CallDeferred(MethodName.Empezar);
	}

	private async void Empezar()
	{
		var gestor = GestorPartida.Instancia;
		if (gestor?.EstadoPendiente != null)
		{
			// Viene de otro escenario del capítulo (con su punto de llegada) o de cargar una partida guardada aquí
			bool cambioDeEscena = !string.IsNullOrEmpty(gestor.PuntoEntrada);
			RestaurarEstado(gestor.EstadoPendiente);
			if (cambioDeEscena) { AlLlegar(true); return; }
			var notas = Hud.Get("notas").AsGodotArray();
			var extra = notas.Count > 0 ? new[] { "Lo último que anoté: «" + notas[notas.Count - 1].AsString() + "»" } : null;
			await gestor.PresentarCapitulo(extra);
			return;
		}
		AlLlegar(false);
	}

	public SistemaAyudas Cuaderno { get; } = new();
    protected virtual (string Id, string Texto) OrientacionInvestigacion() =>
        ("estacion_" + Guion.FaseActual(), Guion.Objetivo() ?? "Habla con los implicados y contrasta sus relatos con las pruebas del diario.");
    public string PedirPista()
    {
        if (!(Configuracion.Instancia?.AyudasInvestigacion ?? true))
            return "Activa las ayudas de investigación en Opciones para pedir una pista.";
        if (_restaurando || EnDialogo || Dialogo.Abierto || Tablero.Abierto || Jugador.Examinando || Guion.Resuelto)
            return "Continúa la investigación antes de pedir otra pista.";
        var (id, texto) = OrientacionInvestigacion();
        if (!Cuaderno.Agregar("caso1:" + id, texto))
            return "La pista para este paso ya está en tu cuaderno. Puedes volver a leerla abajo.";
        Cambio();
        return "Nueva pista añadida al cuaderno.";
    }

	// ---------- Pistas y zonas ----------

	private static void Esconder(ObjetoPista pista, bool escondida)
	{
		if (pista.Encontrada && pista.SeRecoge) return;
		pista.Visible = !escondida;
		pista.CollisionLayer = escondida ? 0 : CapaInteractuable;
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

	// true solo la primera vez en la partida (se guarda con las zonas vistas de este escenario): para lo que se
	// dice al llegar, que no debe repetirse al volver (HU-9.5 CA-03)
	protected bool PrimeraVez(string clave)
	{
		if (!_zonasVistas.Add(clave)) return false;
		Cambio();
		return true;
	}

	private void AlEntrarZona(string zona)
	{
		if (!_zonasVistas.Add(zona)) return;
		string texto = PensamientoDeZona(zona);
		if (texto != null && !EnDialogo) Jugador.MostrarPensamiento(texto, 4.5);
	}

	// ---------- Conversaciones y entrada ----------

	protected async void Conversar(Func<System.Threading.Tasks.Task> conversacion)
	{
		if (EnDialogo || Tablero.Abierto) return;
		EnDialogo = true;
		Dialogo.Retener();
		await conversacion();
		if (!IsInsideTree()) return;
		Dialogo.Soltar();
		EnDialogo = false;
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (evento.IsActionPressed("deducir") && !EnDialogo && !Tablero.Abierto)
		{
			GetViewport().SetInputAsHandled();
			Tablero.Abrir();
		}
	}

	// Pasa a otro escenario del capítulo llevándose el progreso (sin pasar por una ranura)
	protected void IrA(string escena, string puntoEntrada) => GestorPartida.Instancia?.CambiarEscenaDelCapitulo(escena, puntoEntrada);

	protected void Objetivo(string texto) => Hud.Set("objetivo", texto);

	private void AplicarVersionLigera()
	{
		var entorno = Entorno?.Environment;
		if (entorno == null) return;
		entorno.VolumetricFogEnabled = false;
		entorno.SsaoEnabled = false;
		entorno.FogEnabled = true;
	}

	// ---------- Guardado ----------

	public override void _Process(double delta)
	{
		if (!_restaurando) _tiempoCaso += delta;
	}

	protected void Cambio()
	{
		if (!_restaurando && GestorPartida.Instancia != null) GestorPartida.Instancia.HayCambiosSinGuardar = true;
	}

	protected void Autoguardar(string motivo)
	{
		if (!_restaurando) GestorPartida.Instancia?.Autoguardar(motivo);
	}

	protected IEnumerable<T> Todos<T>() where T : Node
	{
		var pendientes = new Stack<Node>();
		pendientes.Push(GetParent());
		while (pendientes.Count > 0)
		{
			var nodo = pendientes.Pop();
			if (nodo is T encontrado) yield return encontrado;
			foreach (Node hijo in nodo.GetChildren()) pendientes.Push(hijo);
		}
	}

	private string Ruta(Node nodo) => GetParent().GetPathTo(nodo).ToString();

	public virtual bool PuedeGuardar =>
		!_restaurando && !EnDialogo && !Tablero.Abierto && !Dialogo.Abierto && !Jugador.Examinando && !Todos<Contenedor>().Any(c => c.Animando);

	private string ZonaActual()
	{
		foreach (var zona in Zonas)
			if (zona.OverlapsBody(Jugador)) return NombreZona(zona.Name);
		return ZonaPorDefecto;
	}

	public Diccionario Resumen() => new()
	{
		{ "caso", "01" },
		{ "capitulo", Caso1.IdCapitulo },
		{ "fase", Capitulos.Fase(Guion.FaseActual())?.Nombre ?? "" },
		{ "zona", ZonaActual() },
		{ "pistas", PartidaEscenas.PistasEnDiario(Hud) },
		{ "total_pistas", PartidaEscenas.TotalPistas },
		{ "tiempo", _tiempoCaso },
	};

	public Diccionario CapturarEstado()
	{
		var pistas = new Arreglo();
		foreach (Variant pista in Hud.Get("pistas").AsGodotArray())
		{
			var recurso = pista.As<Resource>();
			pistas.Add(new Diccionario
			{
				{ "id", recurso.Get("id").AsStringName().ToString() },
				{ "titulo", recurso.Get("titulo").AsString() },
				{ "descripcion", recurso.Get("descripcion").AsString() },
				{ "examinada", recurso.Get("examinada").AsBool() },
				{ "origen_id", recurso.Get("origen_id").AsStringName().ToString() },
			});
		}
		var contenedores = new Diccionario();
		foreach (var c in Todos<Contenedor>())
			contenedores[Ruta(c)] = new Diccionario { { "abierto", c.EstaAbierto }, { "revisado", c.Revisado } };
		var puertas = new Diccionario();
		foreach (var p in Todos<Puerta>())
			puertas[Ruta(p)] = new Diccionario { { "bloqueada", p.Bloqueada }, { "abierta", p.EstaAbierta } };

		var escenas = _escenas.Duplicate(true);
		escenas[Escena] = new Diccionario
		{
			{ "contenedores", contenedores },
			{ "puertas", puertas },
			{ "zonas_vistas", new Arreglo(_zonasVistas.Select(z => Variant.From(z))) },
		};
		return new Diccionario
		{
			{ "jugador", Jugador.CapturarEstado() },
			{ "pistas", pistas },
			{ "notas", Hud.Get("notas").AsGodotArray().Duplicate() },
			{ "tablero", Hud.Call("capturar_tablero").AsGodotDictionary() },
			{ "dialogos", Guion.Capturar() },
			{ "ayudas", Cuaderno.Capturar() },
			{ "tiempo", _tiempoCaso },
			{ "escenas", escenas },
		};
	}

	public void RestaurarEstado(Diccionario estado)
	{
		_restaurando = true;
		Cuaderno.Restaurar(estado.TryGetValue("ayudas", out var ayudas) ? ayudas.AsGodotDictionary() : new Diccionario());
		Jugador.RestaurarEstado(estado["jugador"].AsGodotDictionary());
		PartidaEscenas.ColocarEnEntrada(GetParent(), Jugador);
		_escenas = PartidaEscenas.Escenas(estado);
		var propio = PartidaEscenas.DeEscena(estado, Escena) ?? new Diccionario();

		// Pistas: todas vuelven al diario; las de este escenario además dejan su objeto examinado (o recogido)
		var objetos = Todos<ObjetoPista>().GroupBy(o => o.IdPista).ToDictionary(g => g.Key, g => g.First());
		foreach (Variant v in estado["pistas"].AsGodotArray())
		{
			var pista = v.AsGodotDictionary();
			string id = pista["id"].AsString();
			string origen = pista.TryGetValue("origen_id", out Variant o) ? o.AsString() : "";
			if (!string.IsNullOrEmpty(origen))
			{
				Jugador.RestaurarDetalle(id, pista["titulo"].AsString(), pista["descripcion"].AsString(), origen);
				continue;
			}
			bool examinada = pista.TryGetValue("examinada", out Variant e) && e.AsBool();
			if (objetos.TryGetValue(id, out var objeto))
			{
				Pistas.Add(id);
				objeto.MarcarEncontrada();
				Jugador.RestaurarEvidencia(objeto, pista["titulo"].AsString(), pista["descripcion"].AsString(), examinada);
			}
			else PartidaEscenas.RestaurarPistaDeOtroEscenario(Jugador, Hud, id, pista["titulo"].AsString(), pista["descripcion"].AsString());
		}
		foreach (Variant nota in estado["notas"].AsGodotArray()) Hud.Call("agregar_nota", nota.AsString());
		Hud.Set("_aviso", 0.0);
		Hud.Call("restaurar_tablero", estado.TryGetValue("tablero", out Variant tablero)
			? tablero.AsGodotDictionary() : new Diccionario());
		Guion.Restaurar(estado);

		var contenedores = propio.TryGetValue("contenedores", out Variant cs) ? cs.AsGodotDictionary() : new Diccionario();
		foreach (var c in Todos<Contenedor>())
			if (contenedores.TryGetValue(Ruta(c), out Variant d))
				c.RestaurarEstado(d.AsGodotDictionary()["abierto"].AsBool(), d.AsGodotDictionary()["revisado"].AsBool());
		var puertas = propio.TryGetValue("puertas", out Variant ps) ? ps.AsGodotDictionary() : new Diccionario();
		foreach (var p in Todos<Puerta>())
			if (puertas.TryGetValue(Ruta(p), out Variant d))
				p.RestaurarEstado(d.AsGodotDictionary()["bloqueada"].AsBool(), d.AsGodotDictionary()["abierta"].AsBool());
		// Una pista dentro de un contenedor ya abierto se ve y se puede tomar
		for (int i = 0; i < ContenedoresConPista.Length && i < PistasDentro.Length; i++)
			if (ContenedoresConPista[i].EstaAbierto) Esconder(PistasDentro[i], false);
		if (propio.TryGetValue("zonas_vistas", out Variant zs))
			foreach (Variant zona in zs.AsGodotArray()) _zonasVistas.Add(zona.AsString());
		_tiempoCaso = estado["tiempo"].AsDouble();
		AlRestaurar();
		ActualizarObjetivo();

		_restaurando = false;
		GestorPartida.Instancia?.EstadoRestaurado();
	}

	// Después de restaurar (por ejemplo, abrir lo que ya se podía abrir con un objeto que se trae)
	protected virtual void AlRestaurar() { }
}
