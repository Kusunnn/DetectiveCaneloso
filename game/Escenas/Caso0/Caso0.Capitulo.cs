using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// El Caso 0 como capítulo: fases, indicador del expediente, tarjeta de inicio y pantalla de "Caso cerrado".
public partial class Caso0
{
	public const string IdCapitulo = "caso0";
	private const int ConfrontacionesClave = 4;
	private const int DeduccionesTotales = 4;

	private bool _cerrandoCaso;
	private PantallaCasoCerrado _pantallaCierre;

	private void PrepararCapitulo()
	{
		var indicador = new IndicadorCapitulo(Hud) { Texto = TextoIndicador };
		AddChild(indicador);
		var capa = new CanvasLayer { Layer = 30, ProcessMode = ProcessModeEnum.Always };
		AddChild(capa);
		_pantallaCierre = new PantallaCasoCerrado();
		capa.AddChild(_pantallaCierre);
	}

	// Inicio de la escena: partida cargada ("Anteriormente...") o nueva (tarjeta de título y presentación)
	private async void Empezar()
	{
		var gestor = GestorPartida.Instancia;
		if (gestor?.EstadoPendiente != null)
		{
			RestaurarPendiente();
			var notas = Hud.Get("notas").AsGodotArray();
			var extra = notas.Count > 0 ? new[] { "Lo último que anoté: «" + notas[notas.Count - 1].AsString() + "»" } : null;
			await gestor.PresentarCapitulo(extra);
			return;
		}
		if (gestor != null) await gestor.PresentarCapitulo();
		Presentacion();
	}

	// Fase del capítulo según cómo va la investigación
	private string FaseActual()
	{
		if (_resuelto) return "epilogo";
		if (_pistas.Contains(Cinta)) return "resolucion";
		if (_pistas.Count == 0) return "introduccion";
		if (!_deducciones.Contains(D1)) return "investigacion";
		if (!_deducciones.Contains(D2) || !_autorizado) return _hablados.Count > 0 ? "interrogatorios" : "investigacion";
		return "deduccion";
	}

	private string TextoIndicador()
	{
		var capitulo = Capitulos.Obtener(IdCapitulo);
		return $"{capitulo?.Etiqueta ?? "Caso 0"} · Fase: {Capitulos.Fase(FaseActual())?.Nombre} · Pistas: {_pistas.Count}/{PistasTotales}";
	}

	// Completado del capítulo: pistas, diálogos clave (confrontaciones) y secretos
	private int Porcentaje()
	{
		int secretos = _pistas.Contains(Pluma) ? 1 : 0;
		int logrado = _pistas.Count + _confrontaciones.Count + secretos;
		int total = PistasTotales + ConfrontacionesClave + 1;
		return Mathf.RoundToInt(100f * logrado / total);
	}

	private (int Puntos, string Rango, int Lupas, string Frase) Calificar()
	{
		bool primera = _intentosAcusacion == 1;
		int puntos = _pistas.Count * 10 + _deducciones.Count * 10 + _confrontaciones.Count * 5 + (primera ? 20 : 0);
		string rango = puntos >= 145 ? "S" : puntos >= 120 ? "A" : puntos >= 90 ? "B" : "C";
		int lupas = rango switch { "S" => 5, "A" => 4, "B" => 3, _ => puntos >= 60 ? 2 : 1 };
		string frase = lupas switch
		{
			5 => "Detective nato",
			4 => "Olfato de sabueso",
			3 => "Instinto prometedor",
			2 => "Buen comienzo, novato",
			_ => "Hay que afinar el olfato",
		};
		return (puntos, rango, lupas, frase);
	}

	// Al resolver: guarda, marca el caso como completado y muestra la pantalla de cierre
	private async Task CerrarCaso()
	{
		var gestor = GestorPartida.Instancia;
		var (puntos, rango, lupas, frase) = Calificar();
		var tiempo = TimeSpan.FromSeconds(_tiempoCaso);
		Capitulos.Registrar(IdCapitulo, new ResultadoCapitulo
		{
			Rango = rango, Lupas = lupas, Puntos = puntos, Porcentaje = Porcentaje(), Tiempo = _tiempoCaso, PrimerIntento = _intentosAcusacion == 1,
		});
		if (gestor != null)
		{
			gestor.SaltarTutorial = true;
			// Guardado antes de la pantalla: así "Menú principal" no pregunta por progreso sin guardar
			_cerrandoCaso = true;
			if (!gestor.Rejugando) gestor.Guardar(GestorPartida.RanuraAuto, mostrarAviso: false);
			_cerrandoCaso = false;
			gestor.HayCambiosSinGuardar = false;
		}

		int contenedores = Todos<Contenedor>().Count();
		int revisados = Todos<Contenedor>().Count(c => c.Revisado);
		var capitulo = Capitulos.Obtener(IdCapitulo);
		var siguiente = Capitulos.Siguiente(IdCapitulo);
		var resultado = new ResultadoCaso
		{
			Encabezado = $"{capitulo?.Etiqueta} · {capitulo?.Titulo}",
			Felicitacion = "¡Felicidades, detective! Resolviste tu primer caso.",
			Lineas =
			{
				("Pistas encontradas", $"{_pistas.Count} de {PistasTotales}"),
				("Acusación correcta al primer intento", _intentosAcusacion == 1 ? "Sí" : $"No ({_intentosAcusacion} intentos)"),
				("Confrontaciones clave con pruebas", $"{_confrontaciones.Count} de {ConfrontacionesClave}"),
				("Deducciones en el tablero", $"{_deducciones.Count} de {DeduccionesTotales}"),
				("Contenedores revisados", $"{revisados} de {contenedores}"),
				("Secretos encontrados", _pistas.Contains(Pluma) ? "1 de 1 (la pluma negra)" : "0 de 1"),
				("Tiempo total", $"{(int)tiempo.TotalMinutes} min {tiempo.Seconds:00} s"),
			},
			Rango = rango,
			Lupas = lupas,
			Frase = frase,
			TextoContinuar = siguiente != null ? $"Continuar al {siguiente.Etiqueta}" : "",
			TextoRepetir = "Repetir tutorial",
		};
		Dialogo.Visible = false;
		var eleccion = await _pantallaCierre.Mostrar(resultado);
		if (gestor == null) return;
		switch (eleccion)
		{
			case PantallaCasoCerrado.Eleccion.Continuar:
				gestor.IniciarCapitulo(siguiente.Id, rejugar: Capitulos.Resuelto(siguiente.Id));
				break;
			case PantallaCasoCerrado.Eleccion.Repetir:
				gestor.IniciarCapitulo(IdCapitulo, rejugar: true);
				break;
			default:
				gestor.IrAlMenuInicio();
				break;
		}
	}
}
