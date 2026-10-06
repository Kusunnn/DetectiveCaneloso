using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Tutorial guiado del Caso 0: una mecánica a la vez, con la guía visual (GuiaTutorial),
// sugerencias en diálogos y tablero, ayuda gradual si el jugador se atora y [H] para repetir.
public partial class Caso0
{
	[Export] public GuiaTutorial Guia;

	private class PasoTutorial
	{
		public string Id;
		public string Mecanica;                 // Para la tabla del tutorial
		public Func<bool> Hecho;
		public Func<bool> Disponible = () => true;
		public Func<PasoGuia> Ayuda;
		public Func<Node3D> Destino = () => null;
		public string NombreDestino = "";
	}

	private List<PasoTutorial> _pasos;
	private PasoTutorial _pasoMostrado;
	private double _esperaSiguiente;
	private bool _vioExpediente, _vioLibreta, _entroPasillo, _entroVestidores, _avisoAyudaDado;
	private readonly HashSet<string> _hablados = new();
	private string _ultimaIndicacion = "";

	private bool GuiaActiva => _tutorial && Guia != null && Guia.Activa && !_resuelto;

	private Node3D Nodo(string ruta) => GetNodeOrNull<Node3D>("../" + ruta);

	private void PrepararGuia()
	{
		if (Guia == null) return;
		Guia.Pausada = () => _enDialogo || Tablero.Abierto || Hud.Get("diario_abierto").AsBool();
		Hud.Connect("panel_cambiado", Callable.From<bool, bool>((abierto, expediente) =>
		{
			if (!abierto) return;
			if (expediente) _vioExpediente = true; else _vioLibreta = true;
		}));
		ZonaVestuario.BodyEntered += c => { if (c == Jugador) _entroVestidores = true; };

		Node3D grabadora = Nodo("GrabadoraPista"), reloj = Nodo("RelojPista"), puertaSala = Nodo("Detalles/Puerta");
		Node3D huellas = Nodo("ZonaPasillo/Huellas"), envoltorio = Nodo("ZonaPasillo/Envoltorio"), andamio = Nodo("ZonaPasillo/Andamio");
		Node3D libro = Nodo("ZonaRecepcion/LibroTurnos"), mochila = Nodo("ZonaRecepcion/MochilaPorky"), cinta = Nodo("ZonaVestuario/Cinta");

		PasoGuia En(Node3D objetivo, string accion, string texto, bool foco = false, float altura = 0.35f) =>
			new() { Objetivo = objetivo, Accion = accion, Texto = texto, Foco = foco, Altura = altura };

		_pasos = new List<PasoTutorial>
		{
			new() { Id = "mover", Mecanica = "Moverse", Hecho = () => Jugador.DistanciaRecorrida > 1.5f,
				Ayuda = () => new PasoGuia { Accion = "mover", Texto = "Camina" } },
			new() { Id = "mirar", Mecanica = "Mirar alrededor", Hecho = () => Jugador.GiroAcumulado > 1.2f,
				Ayuda = () => new PasoGuia { Accion = "mirar", Texto = "Mira alrededor" } },
			new() { Id = "grabadora", Mecanica = "Inspeccionar [E]", Hecho = () => _pistas.Contains(Grabadora),
				Ayuda = () => En(grabadora, "interactuar", "Inspeccionar la grabadora", true), Destino = () => grabadora, NombreDestino = "Grabadora" },
			new() { Id = "reloj", Mecanica = "Inspeccionar [E] (repaso)", Hecho = () => _pistas.Contains(Reloj),
				Ayuda = () => En(reloj, "interactuar", "Inspeccionar el reloj", true, 0.45f), Destino = () => reloj, NombreDestino = "Reloj" },
			new() { Id = "expediente", Mecanica = "Pistas del HUD y expediente [TAB]", Hecho = () => _vioExpediente,
				Ayuda = () => new PasoGuia { Accion = "expediente", Texto = "Tus pistas se guardan aquí", ZonaHud = GuiaTutorial.ZonaPistas } },
			new() { Id = "tablero", Mecanica = "Tablero de deducción [R]", Hecho = () => _deducciones.Contains(D1),
				Ayuda = () => new PasoGuia { Accion = "deducir", Texto = "Une la grabadora y el reloj" } },
			new() { Id = "pasillo", Mecanica = "Seguir el objetivo", Hecho = () => _entroPasillo,
				Ayuda = () => En(puertaSala, null, "Sal al pasillo", false, 1.4f), Destino = () => puertaSala, NombreDestino = "Pasillo" },
			new() { Id = "linterna", Mecanica = "Linterna [F]", Hecho = () => Jugador.LinternaEncendida || _pistas.Contains(Huellas),
				Ayuda = () => new PasoGuia { Accion = "linterna", Texto = "Enciende la linterna" } },
			new() { Id = "huellas", Mecanica = "Pistas que solo se ven con luz", Hecho = () => _pistas.Contains(Huellas),
				Ayuda = () => En(huellas, "interactuar", "Examinar las huellas", true), Destino = () => huellas, NombreDestino = "Huellas" },
			new() { Id = "envoltura", Mecanica = "Explorar con la linterna", Hecho = () => _pistas.Contains(Envoltorio),
				Ayuda = () => En(envoltorio, "interactuar", "Revisar junto a la C-2", true), Destino = () => envoltorio, NombreDestino = "C-2" },
			new() { Id = "eriz", Mecanica = "Hablar con un NPC", Hecho = () => _hablados.Contains("Eriz"),
				Ayuda = () => En(Eriz, "interactuar", "Hablar con Eriz", false, 2.0f), Destino = () => Eriz, NombreDestino = "Eriz" },
			new() { Id = "evidencia", Mecanica = "Presentar evidencia (2 pasos)", Hecho = () => _eriz.Confeso,
				Ayuda = () => En(Eriz, "interactuar", "Muéstrale la envoltura a Eriz", false, 2.0f), Destino = () => Eriz, NombreDestino = "Eriz" },
			new() { Id = "libreta", Mecanica = "Libreta [Q]", Hecho = () => _vioLibreta,
				Disponible = () => Hud.Get("notas").AsGodotArray().Count > 0,
				Ayuda = () => new PasoGuia { Accion = "libreta", Texto = "Repasa tus notas" } },
			new() { Id = "libro", Mecanica = "Recoger una pista", Hecho = () => _pistas.Contains(Turnos),
				Ayuda = () => En(libro, "interactuar", "Tomar el libro de turnos", true), Destino = () => libro, NombreDestino = "Recepción" },
			new() { Id = "d2", Mecanica = "Tablero: unir pistas de zonas distintas", Hecho = () => _deducciones.Contains(D2),
				Ayuda = () => new PasoGuia { Accion = "deducir", Texto = "Une las huellas con el libro" } },
			new() { Id = "porky", Mecanica = "Presionar con una deducción", Hecho = () => _porky.Confeso,
				Ayuda = () => En(Porky, "interactuar", "Habla con Porky", false, 2.0f), Destino = () => Porky, NombreDestino = "Porky" },
			new() { Id = "permiso", Mecanica = "Pedir algo a un NPC", Hecho = () => _autorizado,
				Ayuda = () => En(Comisario, "interactuar", "Pide permiso al comisario", false, 2.1f), Destino = () => Comisario, NombreDestino = "Comisario" },
			new() { Id = "agacharse", Mecanica = "Agacharse", Hecho = () => _entroVestidores || Jugador.GlobalPosition.X < -5.3f,
				Ayuda = () => En(andamio, "agacharse", "Agáchate para pasar", false, 1.7f), Destino = () => andamio, NombreDestino = "Andamio" },
			new() { Id = "vestidores", Mecanica = "Zona desbloqueada", Hecho = () => _entroVestidores,
				Ayuda = () => En(PuertaVestuario, null, "Entra a los vestidores", false, 2.3f), Destino = () => PuertaVestuario, NombreDestino = "Vestidores" },
			new() { Id = "mochila", Mecanica = "Agacharse para alcanzar algo", Hecho = () => Jugador.TieneObjeto(Combinacion),
				Disponible = () => _vioCandado,
				Ayuda = () => En(mochila, Jugador.Agachado ? "interactuar" : "agacharse", Jugador.Agachado ? "Revisar la mochila" : "Agáchate junto al banco", true, 0.6f),
				Destino = () => mochila, NombreDestino = "Mochila" },
			new() { Id = "casillero", Mecanica = "Abrir un contenedor con candado", Hecho = () => _pistas.Contains(Cinta),
				Ayuda = () => En(CasilleroPorky, "interactuar", "Abrir el casillero de Porky", true, 2.1f), Destino = () => CasilleroPorky, NombreDestino = "Casillero" },
			new() { Id = "resolver", Mecanica = "Explicar el caso", Hecho = () => _resuelto,
				Ayuda = () => En(SillaAcusacion, "interactuar", "Explica lo que pasó", true, 1.3f), Destino = () => SillaAcusacion, NombreDestino = "Sala 1" },
		};
	}

	// Cada frame: el primer paso pendiente es el que se enseña; al cumplirse, palomita y el siguiente
	private void ActualizarGuia(double delta)
	{
		if (_pasos == null || Guia == null) return;
		if (!GuiaActiva)
		{
			if (Guia.PasoActual != null) Guia.Limpiar();
			return;
		}
		if (_enDialogo && _pasoMostrado == null) return;

		if (_pasoMostrado != null && _pasoMostrado.Hecho())
		{
			Guia.Completar();
			Guia.FijarDestino(null);
			_pasoMostrado = null;
			_esperaSiguiente = 0.9;
			Progreso();
		}
		if (_esperaSiguiente > 0) { _esperaSiguiente -= delta; return; }

		var siguiente = _pasos.FirstOrDefault(p => !p.Hecho() && p.Disponible());
		if (siguiente != _pasoMostrado)
		{
			_pasoMostrado = siguiente;
			if (siguiente == null) { Guia.Limpiar(); return; }
		}
		if (_pasoMostrado == null) return;
		// Se reconstruye cada frame porque algunos pasos cambian de tecla (mochila: agacharse → E)
		var ayuda = _pasoMostrado.Ayuda();
		if (Guia.PasoActual == null || Guia.PasoActual.Texto != ayuda.Texto || Guia.PasoActual.Accion != ayuda.Accion)
			Guia.Mostrar(ayuda);
		Guia.FijarDestino(_pasoMostrado.Destino(), _pasoMostrado.NombreDestino);
	}

	// Mini-pasos dentro de las conversaciones (antes de cada menú)
	private void SugerirEnDialogo(Personaje p, bool evidencia)
	{
		if (!GuiaActiva) return;
		if (p == _eriz && !p.Confeso && _pistas.Contains(Envoltorio))
		{
			if (evidencia) Dialogo.Sugerir("Envoltura", "Paso 2 de 2: la envoltura contradice lo que te dijo.");
			else Dialogo.Sugerir("Mostrarle", "Paso 1 de 2: elige «Mostrarle una prueba».");
		}
		else if (p == _porky && !p.Confeso && _deducciones.Contains(D2) && !evidencia)
			Dialogo.Sugerir("único agente", "Presiónalo con lo que dedujiste en el tablero.");
		else if (p == _comisario && !_autorizado && _deducciones.Contains(D2) && !evidencia)
			Dialogo.Sugerir("vestidores", "Ya tienes algo concreto: pide el permiso.");
	}

	// Antes de abrir el tablero: qué cartas unir en este punto del tutorial
	private void SugerirEnTablero()
	{
		if (!GuiaActiva) return;
		if (!_deducciones.Contains(D1) && _pistas.Contains(Grabadora) && _pistas.Contains(Reloj))
			Tablero.Sugerir("Marca la grabadora y luego el reloj.", Grabadora, Reloj);
		else if (!_deducciones.Contains(D2) && _pistas.Contains(Huellas) && _pistas.Contains(Turnos))
			Tablero.Sugerir("Marca las huellas y el libro de turnos.", Huellas, Turnos);
	}

	// Ayuda gradual (solo en el tutorial): 35 s un pensamiento, 60 s foco sobre el objetivo, 90 s señalarlo
	private void AyudaGradual()
	{
		int nivel = _sinProgreso > 90 ? 3 : _sinProgreso > 60 ? 2 : _sinProgreso > 35 ? 1 : 0;
		if (nivel <= _nivelAyuda) return;
		_nivelAyuda = nivel;
		if (Guia != null) Guia.Insistencia = nivel;
		string ayuda = Ayuda(nivel >= 2 ? 2 : 1);
		if (ayuda == null) return;
		if (!_avisoAyudaDado)
		{
			_avisoAyudaDado = true;
			ayuda += $"  ([{Teclas.Etiqueta("ayuda")}] repite la pista)";
		}
		Jugador.MostrarPensamiento(ConTeclas(ayuda), 7.0);
	}

	// [H]: repite la última indicación y señala el objetivo
	private void RepetirAyuda()
	{
		string texto = _pasoMostrado != null ? Guia?.PasoActual?.Texto : null;
		if (string.IsNullOrEmpty(texto)) texto = Ayuda(2) ?? _ultimaIndicacion;
		if (!string.IsNullOrEmpty(texto)) Jugador.MostrarPensamiento(ConTeclas(texto), 6.0);
		if (Guia != null) Guia.Insistencia = Math.Max(Guia.Insistencia, 2);
		Sonidos.ReproducirUI(this, Sonidos.Tipo.Pagina);
	}

	// Los textos escriben las teclas de teclado ("[E]", "[R]"...); si el jugador usa control se cambian por sus botones
	private static readonly (string Marca, string Accion)[] MarcasDeTecla =
	{
		("[W A S D]", "mover"), ("[E]", "interactuar"), ("[R]", "deducir"), ("[Q]", "libreta"),
		("[F]", "linterna"), ("[C]", "agacharse"), ("[TAB]", "expediente"), ("[H]", "ayuda"),
	};

	private static string ConTeclas(string texto)
	{
		if (string.IsNullOrEmpty(texto)) return texto;
		foreach (var (marca, accion) in MarcasDeTecla)
			if (texto.Contains(marca)) texto = texto.Replace(marca, "[" + Teclas.Etiqueta(accion) + "]");
		return texto;
	}

	public List<(string Paso, string Mecanica)> PasosDelTutorial() => _pasos?.Select(p => (p.Id, p.Mecanica)).ToList() ?? new();
}
