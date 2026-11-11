using Godot;
using System.Linq;
using Arreglo = Godot.Collections.Array;
using Diccionario = Godot.Collections.Dictionary;

// HU-29: ficha de cada sospechoso en el diario (pestaña SOSPECHOSOS). Se arma con lo que el jugador ya sabe:
// los datos del expediente desde el principio, el vínculo con el hecho al encontrar sus pruebas o interrogarlo,
// y las contradicciones a medida que las dice (pendientes) y las tumba con la prueba correcta (detectadas).
public partial class GuionCaso1
{
	// Algo que dijo el sospechoso y la prueba que lo pone a prueba.
	// Se oye al completar Nodo en su interrogatorio; queda resuelta con Conclusion. Comprobada = lo que dijo era cierto.
	private sealed record Dicho(string Texto, string Nodo, string Conclusion, string Prueba, string Pide, string Resultado, bool Comprobada = false);

	private sealed record Coartada(string Zona, string Declaracion, string Conclusion = null, bool Confirmada = false);

	private sealed record Ficha(string Id, string Nombre, string Datos, string Relacion, string Presencia,
		string Vinculo, string[] PruebasVinculo, string NodoInicio, string Confesion, Coartada Coartada, Dicho[] Dichos);

	// Textos del expediente (caso_01.tres) y del guion de los interrogatorios (Datos/Dialogos/caso1_*.json)
	private static readonly Ficha[] Fichas =
	{
		new("juan_carlos", "Juan Carlos", "24 años  ·  Monitor y cuentacuentos",
			"Amigo de Porky desde niños. Era el otro mejor amigo de Paco.",
			"Trabaja de monitor en el campamento; contó la leyenda del Ciervo de Luz en la fogata.",
			"Se fue «por leña» justo antes del apagón y nadie lo volvió a ver hasta que regresó la luz.",
			new[] { "lena_intacta", "baul_disfraces" }, "mentira", "juan_carlos_3",
			new("Campamento · Fogata", "Estaba sacando leña del montón.", "juan_carlos_1"), new Dicho[]
			{
				new("«Estaba sacando leña del montón de la fogata.»", "mentira", "juan_carlos_1", "Leña intacta", "lena_intacta", "nadie sacó leña del montón"),
				new("«Yo también vi al ciervo en la colina.»", "mentira", "juan_carlos_2", "Cornamenta y zancos", "cornamenta_zancos", "el ciervo era él"),
				new("«No bajé al lago. Ni toqué a Porky.»", "p2_disfraz", "juan_carlos_3", "Las huellas se detienen", "huellas_detenidas", "los zancos nunca llegaron al embarcadero", true),
			}),
		new("aya", "Aya", "26 años  ·  Fotógrafa de naturaleza",
			"Fue campista con Porky aquel verano, hace 10 años.",
			"Invitada para la temporada; esa noche fotografiaba el bosque.",
			"Estaba en el bosque con la cámara lista justo cuando sonaron los bramidos.",
			new[] { "altavoz" }, "mentira", "aya_2",
			new("Lago · Bosque", "Estaba tomando fotos en el bosque."), new Dicho[]
			{
				new("«Solo fotografiaba el bosque. No vi nada raro.»", "mentira", "aya_1", "Altavoz escondido", "altavoz", "tenía la cámara lista para los bramidos"),
				new("«Mucha gente toma fotos de noche.»", "p1_bocina", "aya_2", "Testimonio de Juan Carlos", "juan_carlos_3", "sabía del ciervo: lo estaba grabando"),
			}),
		new("miawrio", "Miawrio", "23 años  ·  Inversor",
			"Fue campista con Porky; era el mejor amigo de Paco.",
			"Negocia la compra del terreno para construir un hotel.",
			"Quería el campamento y Porky se negó a dejar su cabaña.",
			new[] { "oferta_compra" }, "mentira", null,
			new("Campamento · Estacionamiento", "Estaba en su carro haciendo llamadas.", "miawrio_2", true), new Dicho[]
			{
				new("«Casi no conocía a Porky.»", "mentira", "miawrio_1", "Oferta de compra rota", "oferta_compra", "le ofreció dinero por su cabaña"),
				new("«Estuve en mi carro haciendo llamadas.»", "mentira", "miawrio_2", "Registro de llamadas", "registro_llamadas", "habló de 23:00 a 23:40 sin cortes", true),
			}),
		new("eriz", "Eriz", "23 años  ·  De visita por nostalgia",
			"Fue campista con Porky aquel verano, hace 10 años.",
			"Volvió al antiguo campamento por nostalgia; anoche estaba en la orilla.",
			"Encontró el cuerpo. Estaba en el lago, sin permiso, a la hora del crimen.",
			new[] { "cuaderno_eriz" }, "mentira", "eriz_1",
			new("Lago · Orilla", "Apenas estaba llegando a la orilla.", "eriz_1"), new Dicho[]
			{
				new("«Apenas estaba llegando a la orilla.»", "mentira", "eriz_1", "Cuaderno de Eriz", "cuaderno_eriz", "estaba en el lago desde las 23:00"),
			}),
		new("mimita", "Mimita", "24 años  ·  Enfermera del campamento",
			"Fue campista con Porky aquel verano, hace 10 años.",
			"Estaba de guardia en la enfermería, que mira directo al lago.",
			"En su registro de guardia alguien tachó la hora del crimen.",
			new[] { "registro_enfermeria" }, "mentira", "mimita_2",
			new("Campamento · Enfermería", "Estaba de guardia y no salió en toda la noche.", "mimita_2"), new Dicho[]
			{
				new("«Estuve toda la noche en la enfermería.»", "mentira", "mimita_1", "Registro de la enfermería", "registro_enfermeria", "tachó la línea de las 23:20"),
				new("«Me equivoqué al anotar. No salí.»", "p1_registro", "mimita_2", "Foto de Aya de las 23:19", "foto_aya_2319", "salió a la orilla y oyó un silbido"),
			}),
		new("tony", "Tony", "31 años  ·  Director del campamento",
			"La conoce desde niña: era el monitor auxiliar del grupo de Paco, su hermano.",
			"Director del campamento; organizó el reencuentro antes del cierre.",
			"Tiene llave de la caseta del generador y del cobertizo de los remos.",
			new[] { "candado_caseta", "botas_tony", "silbato_ts", "hueco_remo" }, "excusa_1", null,
			new("Campamento · Caseta del generador", "Estaba revisando el generador.", "tony_1"), new Dicho[]
			{
				new("«Estaba en la caseta revisando el generador.»", "excusa_1", "tony_1", "Foto de Aya de las 23:12", "foto_aya_2312", "tomó el sendero del lago"),
				new("«No me acerqué al lago.»", "excusa_2", "tony_2", "Botas con barro", "botas_tony", "lodo y juncos de la orilla"),
				// [AJUSTE] Silbato «T. S.» (aprobado el 5 nov): ya no «era de su abuelo»
				new("«Ese silbato no es mío.»", "excusa_3", "tony_3", "Silbato del embarcadero", "silbato_ts", "es suyo, «T. S.»; se le cayó"),
				new("«¡No tenía nada contra Porky!»", "excusa_4", "tony_4", "Libro de guardia quemado", "libro_guardia_quemado", "Porky lo citó por el libro de guardia"),
				new("«Cualquiera pudo agarrar un remo.»", "excusa_5", "tony_5", "Hueco en el soporte de remos", "hueco_remo", "solo dos llaves abren el cobertizo"),
			}),
	};

	// [rotos, total, ¿tengo con qué romper otro?] para dibujarlos en la ficha
	private Arreglo Candados(string quien)
	{
		var propios = Guia.Candados.Where(c => c.Quien == quien).ToList();
		return new Arreglo { propios.Count(Guia.Roto), propios.Count, Guia.Brilla(quien) };
	}

	// Lo pide el HUD al abrir el diario: una entrada por sospechoso (ver hud.gd, actualizar_sospechosos)
	public Arreglo FichasSospechosos()
	{
		var lista = new Arreglo();
		var pistas = new System.Collections.Generic.Dictionary<string, string>();
		foreach (Variant v in _hud.Get("pistas").AsGodotArray())
		{
			var p = v.As<Resource>();
			pistas.TryAdd(p.Get("id").AsStringName().ToString(), p.Get("titulo").AsString());
		}
		bool Tiene(string id) => TienePista(id) || Estado.TieneConclusion(id);

		foreach (var f in Fichas)
		{
			string arbol = "caso1_" + f.Id;
			bool interrogado = Estado.NodoCompletado(arbol, f.NodoInicio);

			// Vínculo con el hecho: aparece con la primera prueba que lo ata, o al interrogarlo
			var pruebas = new Arreglo();
			int encontradas = 0;
			foreach (string id in f.PruebasVinculo)
			{
				string enEscena = IdsEscena.TryGetValue(id, out string otro) ? otro : id;
				if (!pistas.TryGetValue(enEscena, out string titulo)) continue;
				pruebas.Add(new Diccionario { { "titulo", titulo } });
				encontradas++;
			}
			bool conVinculo = encontradas > 0 || interrogado;

			// Contradicciones: solo las que ya dijo; detectadas cuando la prueba correcta lo hizo hablar
			var dichos = new Arreglo();
			Dicho siguiente = null;
			foreach (var d in f.Dichos)
			{
				// Tony dice sus cinco excusas de una vez, como su declaración: se oyen al hablar con él
				bool oida = Estado.NodoCompletado(arbol, d.Nodo) || (f.Id == Culpable && interrogado);
				if (!oida) continue;
				bool resuelto = Estado.TieneConclusion(d.Conclusion);
				if (!resuelto && siguiente == null) siguiente = d;
				dichos.Add(new Diccionario
				{
					{ "dicho", d.Texto },
					{ "prueba", resuelto ? d.Prueba : "" },
					{ "resultado", resuelto ? d.Resultado : "" },
					{ "estado", !resuelto ? "pendiente" : d.Comprobada ? "comprobada" : "detectada" },
				});
			}

			bool coartadaResuelta = interrogado && f.Coartada.Conclusion != null
				&& Estado.TieneConclusion(f.Coartada.Conclusion);
			string estadoCoartada = !coartadaResuelta ? "pendiente"
				: f.Coartada.Confirmada ? "confirmada" : "refutada";

			string estado = !interrogado ? "sin"
				: estadoCoartada == "confirmada" ? "coartada"
				: f.Confesion != null && Estado.TieneConclusion(f.Confesion) ? "confeso"
				: estadoCoartada == "refutada" ? "refutada"
				: f.Id == Culpable ? "principal"
				: "sospecha";
			if (f.Id == Culpable && TonyDisponible && estado != "refutada") estado = "principal";

			// Con qué presionarlo: no se dice qué prueba, solo si ya la tiene (como «Presionar» en el interrogatorio)
			// [AJUSTE] Tony se puede interrogar desde el inicio: ya no «PRIMERO ESCUCHA A LOS DEMÁS»
			string consejo = !interrogado ? "AÚN NO LO INTERROGAS"
				: Guia.Brilla(f.Id) ? "TIENES CON QUÉ ROMPER UN CANDADO"
				: siguiente == null ? (estado == "coartada" ? "SU HISTORIA SE SOSTIENE" : "YA DIJO LO QUE SABÍA")
				: Tiene(siguiente.Pide) ? "TIENES CON QUÉ PRESIONARLO"
				: "TE FALTA UNA PRUEBA PARA PRESIONARLO";

			lista.Add(new Diccionario
			{
				{ "id", f.Id },
				{ "nombre", f.Nombre },
				{ "retrato", Motor.Ventana.Retrato(f.Nombre) },
				{ "datos", f.Datos },
				{ "relacion", f.Relacion },
				{ "presencia", f.Presencia },
				{ "vinculo", conVinculo ? f.Vinculo : "" },
				{ "pruebas", pruebas },
				{ "pruebas_faltan", encontradas < f.PruebasVinculo.Length },
				{ "contradicciones", dichos },
				{ "coartada", interrogado ? new Diccionario
					{
						{ "zona", f.Coartada.Zona },
						{ "declaracion", f.Coartada.Declaracion },
						{ "estado", estadoCoartada },
					} : new Diccionario() },
				{ "estado", estado },
				{ "consejo", consejo },
				{ "candados", Candados(f.Id) },
			});
		}
		return lista;
	}
}
