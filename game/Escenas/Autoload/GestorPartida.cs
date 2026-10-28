using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Lo implementa la escena de juego (el caso): sabe qué guardar de sí misma y cómo restaurarlo.
public interface IEstadoPartida
{
	bool PuedeGuardar { get; }                      // false durante diálogos o animaciones
	Godot.Collections.Dictionary CapturarEstado();
	void RestaurarEstado(Godot.Collections.Dictionary estado);
	Godot.Collections.Dictionary Resumen();         // caso, zona, pistas, total_pistas, tiempo
}

// Resumen de una ranura para mostrarla en el menú
public class InfoRanura
{
	public int Numero;
	public bool Ocupada;
	public bool Danada;
	public bool HayRespaldo;
	public string Caso = "", Zona = "", Fecha = "";
	public string Capitulo = "", Fase = "";
	public int Pistas, TotalPistas;
	public double Tiempo;
	public long GuardadoEn;
	public Texture2D Miniatura;
}

// Autoload: estado global de la partida, guardado en ranuras y navegación entre escenas.
//  - Ranura 0 = autoguardado; 1 a 3 = manuales.
//  - Cada guardado: versión + suma de comprobación; se escribe a un .tmp, el anterior pasa a .bak
//    y el .tmp lo reemplaza, así un cierre a mitad no deja la ranura corrupta.
//  - Avisa al salir (menú, salir del juego o la X de la ventana) si hay cambios sin guardar.
public partial class GestorPartida : Node
{
	public const int Version = 1;
	public const int RanuraAuto = 0;
	public const int RanurasManuales = 3;
	public const string Carpeta = "user://partidas/";
	public const string EscenaMenuInicio = "res://Escenas/Menus/menu_inicio.tscn";
	public const string EscenaJuego = "res://Escenas/Interrogatorio/sala_interrogatorio.tscn";
	private const double PausaEntreAutoguardados = 20.0;

	public static GestorPartida Instancia { get; private set; }

	// HU-04: si es true, la sala de interrogatorio no muestra el tutorial. Se guarda con la partida.
	public bool SaltarTutorial { get; set; } = false;

	// true si hay progreso que se perdería al salir sin guardar
	private bool _cambios;
	public bool HayCambiosSinGuardar
	{
		get => _cambios;
		set => _cambios = value;
	}

	// Estado que la escena de juego debe restaurar al cargarse (null = partida nueva)
	public Godot.Collections.Dictionary EstadoPendiente { get; private set; }
	// Al pasar a otro escenario del mismo capítulo: nombre del Marker3D donde aparece el jugador (null = donde se guardó)
	public string PuntoEntrada { get; private set; }
	// Ranura en la que se guarda con "Guardar y salir" (la última usada)
	public int RanuraActiva { get; private set; } = RanuraAuto;
	public double SegundosDesdeUltimoGuardado => _ultimoGuardado < 0 ? -1 : (Time.GetTicksMsec() / 1000.0) - _ultimoGuardado;

	// Capítulo que se está jugando (id de Datos/capitulos.json)
	public string CapituloActual { get; private set; } = "caso0";
	// Rejugando un capítulo ya resuelto: no se guarda en las ranuras, así la partida principal no cambia
	public bool Rejugando { get; private set; }
	// Capítulo sin escena todavía: el menú de inicio muestra su tarjeta y avisa que está en preparación
	public string CapituloEnPreparacion { get; set; }

	// HU-9.5 CA-03: cinemáticas que ya salieron en esta partida (por id). Viaja en cada guardado, así una
	// cinemática no se repite al cargar ni al volver a un escenario; las repeticiones a propósito no cuentan.
	private readonly HashSet<string> _cinematicasVistas = new();
	// Cada vez que una cinemática sale sola (no al volver a verla): para las pruebas de CA-03
	[Signal] public delegate void CinematicaReproducidaEventHandler(string id);
	// true si la presentación del capítulo fue la cinemática IntroCaso (la escena no repite el expediente)
	public bool PresentoConCinematica { get; private set; }

	public bool CinematicaVista(string id) => _cinematicasVistas.Contains(id);
	public IReadOnlyCollection<string> CinematicasVistas => _cinematicasVistas;

	public void RegistrarCinematica(string id)
	{
		_cinematicasVistas.Add(id);
		EmitSignal(SignalName.CinematicaReproducida, id);
	}

	// Qué presentar al entrar a la escena del capítulo: tarjeta de título (nuevo) o "Anteriormente..." (cargado)
	private enum Presentacion { Nada, Inicio, Carga }
	private Presentacion _presentacion = Presentacion.Nada;
	private PresentacionCapitulo _tarjeta;

	private double _ultimoGuardado = -1;
	private double _ultimoAutoguardado = -1000;
	private string _autoguardadoPendiente;
	private Image _miniaturaPendiente;
	private CanvasLayer _capa;
	private DialogoSalida _dialogoSalida;
	private Control _aviso;
	private Label _textoAviso;
	private Control _sello;
	private bool _saliendo;

	public override void _EnterTree()
	{
		Instancia = this;
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		DirAccess.MakeDirRecursiveAbsolute(Carpeta);
		// La X de la ventana se intercepta para poder avisar si hay progreso sin guardar
		GetTree().AutoAcceptQuit = false;
		CrearInterfaz();
	}

	// ---------- Navegación ----------

	// saltarTutorial ("Omitir tutorial"): se empieza directo en el capítulo que sigue al tutorial, que queda marcado
	// como omitido para que ese capítulo se desbloquee. Se puede jugar después desde el tablero de capítulos.
	public void NuevaPartida(bool saltarTutorial = false)
	{
		RanuraActiva = RanuraAuto;
		_cinematicasVistas.Clear();
		var tutorial = Capitulos.Lista.FirstOrDefault();
		var siguiente = tutorial != null ? Capitulos.Siguiente(tutorial.Id) : null;
		if (saltarTutorial && siguiente != null && siguiente.TieneEscena)
		{
			Capitulos.MarcarOmitido(tutorial.Id);
			IniciarCapitulo(siguiente.Id, rejugar: false, saltarTutorial: true);
			return;
		}
		IniciarCapitulo(tutorial?.Id ?? "caso0", rejugar: false, saltarTutorial);
	}

	// Empieza un capítulo desde el principio. rejugar = capítulo ya resuelto: no toca las ranuras.
	public void IniciarCapitulo(string id, bool rejugar, bool saltarTutorial = false)
	{
		var capitulo = Capitulos.Obtener(id);
		if (capitulo == null) return;
		SaltarTutorial = saltarTutorial;
		// Una repetición no se guarda: empieza con sus propias cinemáticas
		if (rejugar) _cinematicasVistas.Clear();
		Rejugando = rejugar;
		EstadoPendiente = null;
		PuntoEntrada = null;
		HayCambiosSinGuardar = false;
		_ultimoGuardado = -1;
		if (!capitulo.TieneEscena)
		{
			CapituloEnPreparacion = id;
			_presentacion = Presentacion.Nada;
			CambiarEscena(EscenaMenuInicio);
			return;
		}
		CapituloActual = id;
		_presentacion = Presentacion.Inicio;
		CambiarEscena(capitulo.Escena);
	}

	// Lo llama la escena del capítulo al empezar: tarjeta de título si es nuevo, "Anteriormente..." si se cargó.
	// extra: líneas que la escena añade al resumen (lo último que anotó el jugador...).
	// avisoExpediente: false si la escena da su propio contexto al terminar (no pone el aviso del expediente encima).
	public async Task PresentarCapitulo(IEnumerable<string> extra = null, bool avisoExpediente = true)
	{
		var modo = _presentacion;
		_presentacion = Presentacion.Nada;
		PresentoConCinematica = false;
		var capitulo = Capitulos.Obtener(CapituloActual);
		if (capitulo == null || modo == Presentacion.Nada) return;
		var lineas = capitulo.Anteriormente.ToList();
		if (modo == Presentacion.Inicio)
		{
			// Si el caso tiene expediente, al empezarlo se presenta con la cinemática (ya trae la tarjeta del título).
			// Sale una vez por partida al empezar el capítulo (nunca al cargar); al repetir uno ya resuelto, solo si no se ha visto.
			string caso = capitulo.Numero.ToString("00");
			string id = IntroCaso.IdCinematica(caso);
			if (IntroCaso.Disponible(caso) && !CinematicaVista(id) && (!Rejugando || !IntroCaso.Vista(caso)))
			{
				RegistrarCinematica(id);
				PresentoConCinematica = true;
				var intro = IntroCaso.Reproducir(this, caso, aviso: avisoExpediente);
				await ToSignal(intro, IntroCaso.SignalName.Terminada);
				return;
			}
			await _tarjeta.MostrarTitulo(capitulo);
			// El primer caso no tiene "anteriormente"; los demás lo muestran al empezar
			if (capitulo.Numero > 0 && lineas.Count > 0) await _tarjeta.MostrarAnteriormente(lineas);
		}
		else
		{
			if (extra != null) lineas.AddRange(extra);
			if (lineas.Count > 0) await _tarjeta.MostrarAnteriormente(lineas.Take(4).ToList());
		}
	}

	// Tarjeta y "Anteriormente..." de un capítulo en preparación (desde el menú de inicio)
	public async Task PresentarCapituloEnPreparacion(InfoCapitulo capitulo)
	{
		await _tarjeta.MostrarTitulo(capitulo);
		if (capitulo.Anteriormente.Count > 0) await _tarjeta.MostrarAnteriormente(capitulo.Anteriormente);
	}

	public bool HayPartidaGuardada() => Ranuras().Any(r => r.Ocupada);

	// "Continuar": la partida más reciente de todas las ranuras
	public InfoRanura MasReciente() => Ranuras().Where(r => r.Ocupada).OrderByDescending(r => r.GuardadoEn).FirstOrDefault();

	public void ContinuarPartida()
	{
		var reciente = MasReciente();
		if (reciente != null) CargarRanura(reciente.Numero);
	}

	// Devuelve false si la ranura está dañada (el menú ofrece cargar el respaldo)
	public bool CargarRanura(int ranura, bool desdeRespaldo = false)
	{
		var datos = LeerArchivo(RutaJson(ranura) + (desdeRespaldo ? ".bak" : ""));
		if (datos == null) return false;
		// Partida guardada al cerrar un caso (fase Epílogo): ya no queda nada por hacer ahí, así que sigue el próximo capítulo
		string guardado = datos.TryGetValue("capitulo", out Variant cap) ? cap.AsString() : "caso0";
		_cinematicasVistas.Clear();
		if (datos.TryGetValue("cinematicas", out Variant vistas))
			foreach (Variant id in vistas.AsGodotArray()) _cinematicasVistas.Add(id.AsString());
		var siguiente = CapituloTerminado(datos) ? Capitulos.Siguiente(guardado) : null;
		if (siguiente != null && siguiente.TieneEscena)
		{
			RanuraActiva = ranura;
			IniciarCapitulo(siguiente.Id, rejugar: false, saltarTutorial: true);
			return true;
		}
		SaltarTutorial = datos.TryGetValue("saltar_tutorial", out Variant saltar) && saltar.AsBool();
		EstadoPendiente = datos.TryGetValue("estado", out Variant estado) ? estado.AsGodotDictionary() : null;
		PuntoEntrada = null;
		RanuraActiva = ranura;
		HayCambiosSinGuardar = false;
		Rejugando = false;
		CapituloActual = datos.TryGetValue("capitulo", out Variant capitulo) ? capitulo.AsString() : "caso0";
		_presentacion = Presentacion.Carga;
		_ultimoGuardado = Time.GetTicksMsec() / 1000.0;
		string escena = datos.TryGetValue("escena", out Variant e) && ResourceLoader.Exists(e.AsString()) ? e.AsString() : EscenaJuego;
		CambiarEscena(escena);
		return true;
	}

	private static bool CapituloTerminado(Godot.Collections.Dictionary datos)
	{
		if (!datos.TryGetValue("resumen", out Variant resumen)) return false;
		var r = resumen.AsGodotDictionary();
		return r.TryGetValue("fase", out Variant fase) && fase.AsString() == Capitulos.Fase("epilogo")?.Nombre;
	}

	// La escena de juego lo llama tras restaurar
	public void EstadoRestaurado()
	{
		EstadoPendiente = null;
		PuntoEntrada = null;
	}

	// Otro escenario del mismo capítulo (Campamento ↔ Lago ↔ sala): se lleva todo el progreso y el jugador
	// aparece en el Marker3D «punto» de la escena nueva. No cuenta como cargar: no hay «Anteriormente…».
	public void CambiarEscenaDelCapitulo(string escena, string punto)
	{
		var juego = EscenaDeJuego();
		if (juego == null || !ResourceLoader.Exists(escena)) return;
		EstadoPendiente = juego.CapturarEstado();
		PuntoEntrada = punto;
		_presentacion = Presentacion.Nada;
		CambiarEscena(escena);
	}

	public void IrAlMenuInicio()
	{
		HayCambiosSinGuardar = false;
		EstadoPendiente = null;
		Rejugando = false;
		_presentacion = Presentacion.Nada;
		CambiarEscena(EscenaMenuInicio);
	}

	private string EscenaActual()
	{
		string ruta = GetTree().CurrentScene?.SceneFilePath;
		return string.IsNullOrEmpty(ruta) ? null : ruta;
	}

	private void CambiarEscena(string ruta)
	{
		_autoguardadoPendiente = null; // Un autoguardado pendiente no pasa a otra partida
		_dialogoSalida.Cancelar();
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(ruta);
	}

	// ---------- Guardar ----------

	private IEstadoPartida EscenaDeJuego() =>
		GetTree().GetNodesInGroup("estado_partida").OfType<IEstadoPartida>().FirstOrDefault();

	public bool PuedeGuardar => EscenaDeJuego() is { PuedeGuardar: true };

	// La captura se toma antes de abrir el menú de pausa, para que la miniatura sea de la escena
	public void PrepararMiniatura()
	{
		var imagen = GetViewport().GetTexture().GetImage();
		imagen.Resize(256, 144, Image.Interpolation.Bilinear);
		_miniaturaPendiente = imagen;
	}

	// Compatibilidad: el caso lo llamaba al terminar
	public void GuardarPartida() => Autoguardar("Fin del caso", forzar: true);

	// Autoguardado en momentos clave; si ahora no se puede (diálogo), se guarda en cuanto se pueda
	public void Autoguardar(string motivo, bool forzar = false)
	{
		if (Rejugando) return; // Repetir un capítulo no toca la partida principal
		if (!forzar && Time.GetTicksMsec() / 1000.0 - _ultimoAutoguardado < PausaEntreAutoguardados)
		{
			_autoguardadoPendiente ??= motivo;
			return;
		}
		if (!PuedeGuardar) { _autoguardadoPendiente = motivo; return; }
		_autoguardadoPendiente = null;
		_ultimoAutoguardado = Time.GetTicksMsec() / 1000.0;
		PrepararMiniatura();
		Guardar(RanuraAuto, mostrarAviso: true, texto: "Autoguardado");
	}

	public override void _Process(double delta)
	{
		if (_autoguardadoPendiente != null && PuedeGuardar
			&& Time.GetTicksMsec() / 1000.0 - _ultimoAutoguardado >= PausaEntreAutoguardados)
			Autoguardar(_autoguardadoPendiente);
	}

	// Guardado manual (menú de pausa). Devuelve false si ahora no se puede.
	public bool Guardar(int ranura, bool mostrarAviso = true, string texto = "Partida guardada")
	{
		var escena = EscenaDeJuego();
		if (Rejugando)
		{
			if (mostrarAviso) MostrarAviso("Repetición: no se guarda en tus ranuras", sello: false);
			return false;
		}
		if (escena == null || !escena.PuedeGuardar)
		{
			if (mostrarAviso) MostrarAviso("No puedes guardar ahora", sello: false);
			return false;
		}
		var resumen = escena.Resumen();
		var datos = new Godot.Collections.Dictionary
		{
			{ "guardado_en", (long)Time.GetUnixTimeFromSystem() },
			{ "fecha", Time.GetDatetimeStringFromSystem(false, true) },
			{ "ranura", ranura },
			{ "saltar_tutorial", SaltarTutorial },
			// La escena donde está el jugador (un capítulo puede tener varios escenarios)
			{ "escena", EscenaActual() ?? Capitulos.Obtener(CapituloActual)?.Escena ?? EscenaJuego },
			{ "capitulo", CapituloActual },
			{ "resumen", resumen },
			{ "estado", escena.CapturarEstado() },
			{ "cinematicas", new Godot.Collections.Array<string>(_cinematicasVistas) },
		};
		if (!EscribirArchivo(RutaJson(ranura), datos))
		{
			MostrarAviso("No se pudo guardar", sello: false);
			return false;
		}
		_miniaturaPendiente?.SavePng(Carpeta + $"ranura_{ranura}.png");
		// El autoguardado no cambia la ranura elegida por el jugador ("Guardar y salir" guarda ahí)
		if (ranura != RanuraAuto) RanuraActiva = ranura;
		HayCambiosSinGuardar = false;
		_ultimoGuardado = Time.GetTicksMsec() / 1000.0;
		if (mostrarAviso) MostrarAviso(texto, sello: true);
		return true;
	}

	// ---------- Archivos ----------

	private static string RutaJson(int ranura) => Carpeta + $"ranura_{ranura}.json";

	// Escribe a .tmp, comprueba que se puede leer, el actual pasa a .bak y el .tmp lo reemplaza
	public static bool EscribirArchivo(string ruta, Godot.Collections.Dictionary datos)
	{
		string cuerpo = Json.Stringify(datos);
		var envoltorio = new Godot.Collections.Dictionary
		{
			{ "version", Version },
			{ "suma", cuerpo.Md5Text() },
			{ "datos", cuerpo },
		};
		string temporal = ruta + ".tmp";
		using (var archivo = FileAccess.Open(temporal, FileAccess.ModeFlags.Write))
		{
			if (archivo == null) return false;
			archivo.StoreString(Json.Stringify(envoltorio));
		}
		if (LeerArchivo(temporal) == null) return false;
		string global = ProjectSettings.GlobalizePath(ruta);
		if (FileAccess.FileExists(ruta))
		{
			DirAccess.CopyAbsolute(global, global + ".bak");
			DirAccess.RemoveAbsolute(global);
		}
		return DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(temporal), global) == Error.Ok;
	}

	// Devuelve los datos o null si no existe, está dañado o la versión no es compatible
	public static Godot.Collections.Dictionary LeerArchivo(string ruta)
	{
		if (!FileAccess.FileExists(ruta)) return null;
		using var archivo = FileAccess.Open(ruta, FileAccess.ModeFlags.Read);
		if (archivo == null) return null;
		Variant leido = LeerJson(archivo.GetAsText());
		if (leido.VariantType != Variant.Type.Dictionary) return null;
		var envoltorio = leido.AsGodotDictionary();
		if (!envoltorio.TryGetValue("datos", out Variant cuerpo) || !envoltorio.TryGetValue("suma", out Variant suma)) return null;
		if (!envoltorio.TryGetValue("version", out Variant version) || version.AsInt32() > Version) return null;
		if (cuerpo.AsString().Md5Text() != suma.AsString()) return null;
		Variant datos = LeerJson(cuerpo.AsString());
		return datos.VariantType == Variant.Type.Dictionary ? datos.AsGodotDictionary() : null;
	}

	// Sin mensajes de error en la consola: un archivo dañado se reporta como null
	private static Variant LeerJson(string texto)
	{
		var json = new Json();
		return json.Parse(texto) == Error.Ok ? json.Data : default;
	}

	public List<InfoRanura> Ranuras()
	{
		var lista = new List<InfoRanura>();
		for (int i = 0; i <= RanurasManuales; i++)
		{
			string ruta = RutaJson(i);
			var info = new InfoRanura { Numero = i, HayRespaldo = FileAccess.FileExists(ruta + ".bak") };
			if (FileAccess.FileExists(ruta))
			{
				var datos = LeerArchivo(ruta);
				info.Ocupada = true;
				info.Danada = datos == null;
				// Una ranura dañada sigue contando para "Continuar" (por la fecha del archivo), así se ofrece su respaldo
				if (datos == null) info.GuardadoEn = (long)FileAccess.GetModifiedTime(ruta);
				if (datos != null)
				{
					var r = datos["resumen"].AsGodotDictionary();
					info.Caso = r.TryGetValue("caso", out Variant c) ? c.AsString() : "";
					info.Capitulo = r.TryGetValue("capitulo", out Variant cap) ? cap.AsString() : "caso0";
					info.Fase = r.TryGetValue("fase", out Variant fase) ? fase.AsString() : "";
					info.Zona = r.TryGetValue("zona", out Variant z) ? z.AsString() : "";
					info.Pistas = r.TryGetValue("pistas", out Variant p) ? p.AsInt32() : 0;
					info.TotalPistas = r.TryGetValue("total_pistas", out Variant tp) ? tp.AsInt32() : 0;
					info.Tiempo = r.TryGetValue("tiempo", out Variant t) ? t.AsDouble() : 0;
					info.Fecha = datos["fecha"].AsString();
					info.GuardadoEn = datos["guardado_en"].AsInt64();
				}
				string png = Carpeta + $"ranura_{i}.png";
				if (FileAccess.FileExists(png))
				{
					var imagen = Image.LoadFromFile(ProjectSettings.GlobalizePath(png));
					if (imagen != null) info.Miniatura = ImageTexture.CreateFromImage(imagen);
				}
			}
			lista.Add(info);
		}
		return lista;
	}

	// ---------- Salir ----------

	public override void _Notification(int what)
	{
		// Diferido: la notificación llega mientras el árbol la reparte y ahí no se pueden agregar nodos (sonidos)
		if (what == NotificationWMCloseRequest) Callable.From(() => { _ = SalirDelJuego(); }).CallDeferred();
	}

	// Menú de inicio o salir del juego desde una partida: siempre pregunta [Guardar y salir] [Salir sin guardar] [Cancelar]
	public async Task<bool> ConfirmarSalida()
	{
		if (EscenaDeJuego() == null) return true;
		// Sin ningún guardado en esta partida, todo lo hecho cuenta como progreso sin guardar
		bool hayCambios = HayCambiosSinGuardar || SegundosDesdeUltimoGuardado < 0;
		var respuesta = await _dialogoSalida.Preguntar(TextoUltimoGuardado(), hayCambios, puedeGuardar: !Rejugando);
		if (respuesta == DialogoSalida.Respuesta.Cancelar) return false;
		if (respuesta == DialogoSalida.Respuesta.GuardarYSalir)
		{
			if (!Guardar(RanuraActiva, mostrarAviso: true)) return false;
		}
		return true;
	}

	public async Task SalirDelJuego()
	{
		if (_saliendo) return;
		_saliendo = true;
		bool pausado = GetTree().Paused;
		GetTree().Paused = true;
		var raton = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		bool salir = await ConfirmarSalida();
		_saliendo = false;
		if (salir) { GetTree().Quit(); return; }
		GetTree().Paused = pausado;
		Input.MouseMode = raton;
	}

	public async Task VolverAlMenuInicio()
	{
		if (await ConfirmarSalida()) IrAlMenuInicio();
	}

	private string TextoUltimoGuardado()
	{
		double s = SegundosDesdeUltimoGuardado;
		if (s < 0) return "Todavía no has guardado esta partida.";
		int minutos = (int)(s / 60);
		return minutos < 1 ? "Último guardado: hace menos de un minuto." : minutos == 1 ? "Último guardado: hace 1 minuto." : $"Último guardado: hace {minutos} minutos.";
	}

	// ---------- Interfaz: aviso "Partida guardada" y diálogo de salida ----------

	private void CrearInterfaz()
	{
		_capa = new CanvasLayer { Layer = 120, ProcessMode = ProcessModeEnum.Always };
		AddChild(_capa);
		_dialogoSalida = new DialogoSalida();
		_capa.AddChild(_dialogoSalida);
		_tarjeta = new PresentacionCapitulo();
		_capa.AddChild(_tarjeta);

		var mono = GD.Load<Font>("res://hud/fonts/IBMPlexMono-Regular.ttf");
		var fondo = new StyleBoxFlat { BgColor = new Color(0.84f, 0.81f, 0.72f), BorderColor = new Color("efca08"), ShadowColor = new Color(0, 0, 0, 0.4f), ShadowSize = 6 };
		fondo.BorderWidthBottom = 4;
		fondo.SetContentMarginAll(12);
		var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		panel.AddThemeStyleboxOverride("panel", fondo);
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
		panel.Position = new Vector2(28, -96);
		panel.GrowVertical = Control.GrowDirection.Begin;
		var fila = new HBoxContainer();
		fila.AddThemeConstantOverride("separation", 12);
		panel.AddChild(fila);
		// Sello: libreta con una palomita que se estampa
		_sello = new Control { CustomMinimumSize = new Vector2(34, 34) };
		_sello.Draw += () =>
		{
			_sello.DrawRect(new Rect2(3, 2, 26, 30), new Color("20272b"));
			_sello.DrawRect(new Rect2(6, 5, 20, 24), new Color(0.84f, 0.81f, 0.72f));
			_sello.DrawPolyline(new[] { new Vector2(10, 17), new Vector2(15, 23), new Vector2(24, 10) }, new Color("c79a00"), 3f, true);
		};
		fila.AddChild(_sello);
		_textoAviso = new Label { VerticalAlignment = VerticalAlignment.Center };
		_textoAviso.AddThemeFontOverride("font", mono);
		_textoAviso.AddThemeFontSizeOverride("font_size", 20);
		_textoAviso.AddThemeColorOverride("font_color", new Color("20272b"));
		fila.AddChild(_textoAviso);
		_aviso = panel;
		_aviso.Modulate = new Color(1, 1, 1, 0);
		_capa.AddChild(_aviso);
	}

	public void MostrarAviso(string texto, bool sello)
	{
		_textoAviso.Text = texto;
		_sello.Visible = sello;
		_aviso.Position = new Vector2(28, GetViewport().GetVisibleRect().Size.Y - 96);
		Sonidos.ReproducirUI(this, sello ? Sonidos.Tipo.Sello : Sonidos.Tipo.Error, sello ? 2f : 0f);
		var animacion = _aviso.CreateTween();
		_sello.Scale = new Vector2(1.6f, 1.6f);
		_sello.PivotOffset = new Vector2(17, 17);
		animacion.TweenProperty(_aviso, "modulate:a", 1f, 0.2);
		animacion.Parallel().TweenProperty(_sello, "scale", Vector2.One, 0.25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		animacion.TweenInterval(1.8);
		animacion.TweenProperty(_aviso, "modulate:a", 0f, 0.4);
	}
}
