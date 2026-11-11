using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

// El capítulo en actos (Campamento, Lago, Sala, Reconstrucción) y la calificación del final.
// Cada acto se anuncia una vez: Caneloso resume lo que sabe del anterior, sale la tarjeta y se autoguarda.
// Los actos vistos van en lo recorrido del guion (árbol «caso1_actos»), así viajan en la partida sin campos nuevos.
public partial class GuionCaso1
{
	private const string ArbolActos = "caso1_actos";

	// Lo ponen los escenarios
	public Func<double> TiempoCaso = () => 0;
	public Func<ResultadoCaso, Task<PantallaCasoCerrado.Eleccion>> MostrarCierre;

	public bool ActoVisto(int acto) => Estado.NodoCompletado(ArbolActos, "acto_" + acto);

	public async Task LlegarAlActo(int acto)
	{
		if (ActoVisto(acto) || Resuelto) return;
		// Partidas de antes: los actos que ya pasaron no se anuncian
		for (int i = 1; i < acto; i++)
			if (!ActoVisto(i)) Estado.MarcarNodo(ArbolActos, "acto_" + i);
		Estado.MarcarNodo(ArbolActos, "acto_" + acto);
		if (acto > 1)
		{
			string resumen = acto == 4 && !TonySinCandados ? "resumen_3_sin_tony" : "resumen_" + (acto - 1);
			var lineas = Guia.TextoSistema("actos", resumen).Split('\n');
			await Motor.Ventana.Decir(Motor.HablanteDe(_arboles[ArbolSala], "Caneloso"), lineas);
		}
		var tarjeta = Guia.TextoSistema("actos", "acto_" + acto).Split('|');
		_hud.Call("avisar", tarjeta[0], tarjeta[1]);
		Motor.Cambio?.Invoke();
		AlTerminar?.Invoke("Acto " + acto);
	}

	// ---------- Calificación ----------

	private (int Puntos, string Rango, int Lupas, string Frase, int PrimerIntento, int Pistas) Calificar(bool finalA)
	{
		int pistas = Guia.PistasEncontradas("campamento") + Guia.PistasEncontradas("lago");
		int totalPistas = Guia.PistasTotales("campamento") + Guia.PistasTotales("lago");
		int primerIntento = Guia.Candados.Count(c => Guia.Roto(c) && Estado.Contador(ClaveErroresDe(c)) == 0);
		int ayudas = Ayudas?.Usos ?? 0;
		double minutos = TiempoCaso() / 60.0;
		// Candados al primer intento 15, tiempo 5, pistas 40, ayudas 30, credibilidad 10
		double puntos = 15.0 * primerIntento / Guia.CandadosTotales
			+ 5.0 * (minutos <= 45 ? 1 : Math.Max(0, 1 - (minutos - 45) / 45))
			+ 40.0 * pistas / totalPistas
			+ Math.Max(0, 30 - 3 * ayudas)
			+ 10.0 * Math.Min(Estado.Contador("credibilidad"), CredibilidadInicial) / CredibilidadInicial;
		int total = (int)Math.Round(puntos);
		string rango = !finalA ? "C" : total >= 80 ? "A" : total >= 55 ? "B" : "C";
		int lupas = !finalA ? 1 : rango switch { "A" => 4, "B" => 3, _ => 2 };
		string frase = Guia.TextoSistema("calificacion", finalA ? "frase_" + rango : "frase_final_b");
		return (total, rango, lupas, frase, primerIntento, pistas);
	}

	private static string ClaveErroresDe(GuiaCaso1.Candado c) => "errores:" + c.Id;

	// Pantalla de «Caso cerrado»: lo registra en el progreso y ofrece repetir o volver al menú
	private async Task CerrarCaso(bool finalA)
	{
		var (puntos, rango, lupas, frase, primerIntento, pistas) = Calificar(finalA);
		var tiempo = TimeSpan.FromSeconds(TiempoCaso());
		Capitulos.Registrar(Caso1.IdCapitulo, new ResultadoCapitulo
		{
			AyudasUtilizadas = Ayudas?.Usos ?? 0, Rango = rango, Lupas = lupas, Puntos = puntos, Tiempo = TiempoCaso(), PrimerIntento = finalA,
			Porcentaje = (int)Math.Round(100.0 * pistas / (Guia.PistasTotales("campamento") + Guia.PistasTotales("lago"))),
		});
		if (MostrarCierre == null) return;
		var capitulo = Capitulos.Obtener(Caso1.IdCapitulo);
		var siguiente = Capitulos.Siguiente(Caso1.IdCapitulo);
		var dificultad = Configuracion.Dificultades.FirstOrDefault(d => d.Id == Dificultad).Nombre ?? "Detective";
		var resultado = new ResultadoCaso
		{
			Encabezado = $"{capitulo?.Etiqueta} · {capitulo?.Titulo}",
			Felicitacion = Guia.TextoSistema("calificacion", finalA ? "felicitacion_a" : "felicitacion_b"),
			Lineas =
			{
				("Pistas encontradas", $"{pistas} de {Guia.PistasTotales("campamento") + Guia.PistasTotales("lago")}"),
				("Candados rotos al primer intento", $"{primerIntento} de {Guia.CandadosTotales}"),
				("Ayudas usadas", (Ayudas?.Usos ?? 0) == 0 ? "Sin ayudas" : Ayudas.Usos.ToString()),
				("Credibilidad", $"{Math.Min(Estado.Contador("credibilidad"), CredibilidadInicial)} / {CredibilidadInicial}"),
				("Dificultad", dificultad),
				("Tiempo total", $"{(int)tiempo.TotalMinutes} min {tiempo.Seconds:00} s"),
			},
			Rango = rango,
			Lupas = lupas,
			Frase = frase,
			TextoContinuar = siguiente != null && siguiente.TieneEscena ? $"Continuar al {siguiente.Etiqueta}" : "",
			TextoRepetir = "Repetir caso",
		};
		Motor.Ventana.Visible = false;
		var eleccion = await MostrarCierre(resultado);
		var gestor = GestorPartida.Instancia;
		if (gestor == null) return;
		switch (eleccion)
		{
			case PantallaCasoCerrado.Eleccion.Continuar:
				gestor.IniciarCapitulo(siguiente.Id, rejugar: gestor.Rejugando || Capitulos.Resuelto(siguiente.Id));
				break;
			case PantallaCasoCerrado.Eleccion.Repetir:
				gestor.IniciarCapitulo(Caso1.IdCapitulo, rejugar: true);
				break;
			default:
				gestor.IrAlMenuInicio();
				break;
		}
	}
}
