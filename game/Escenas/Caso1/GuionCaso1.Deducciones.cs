using Godot;

public partial class GuionCaso1
{
	private sealed record Deduccion(string Primera, string Segunda, string Texto, string Sospechoso = null);

	private static readonly Deduccion[] Deducciones =
	{
		new("cuaderno_eriz", "registro_enfermeria",
			"El destello del embarcadero coincide con la hora tachada en la enfermería. Las 23:20 son la hora más probable del ataque a Porky."),
		new("hueco_remo", "remo_sangre",
			"El remo ensangrentado pudo salir del soporte vacío. Si se usó en el ataque, la llave del cobertizo explica cómo consiguió el arma quien atacó a Porky."),
		new("libro_guardia_quemado", "nota_porky",
			"Porky planeaba hablar esa noche sobre el libro de guardia y Paco. Alguien pudo quemarlo para ocultar lo ocurrido durante la guardia en que Paco murió."),
		new("interruptor_generador", "altavoz",
			"Alguien apagó el generador a mano y preparó bramidos grabados. El apagón y el ruido pudieron servir para que todos miraran hacia el bosque."),
		new("persona:Eriz", "cuaderno_eriz",
			"Eriz dice que apenas llegaba a la orilla. Su cuaderno registra un destello junto al embarcadero a las 23:20: ya observaba el lago cuando ocurrió el hecho.", "eriz"),
		new("persona:Tony", "botas_tony",
			"Tony asegura que se quedó en la caseta. El lodo y los juncos en sus botas contradicen que evitara la orilla, aunque por sí solos no dicen a qué hora bajó.", "tony"),
	};

	public void PrepararTablero(TableroDeduccion tablero)
	{
		tablero.Probar = ProbarDeduccion;
		foreach (string sospechoso in Sospechosos)
		{
			string nombre = NombreDe(sospechoso);
			_hud.Call("registrar_persona", nombre, "", Motor.Ventana.Retrato(nombre));
		}
	}

	private string ProbarDeduccion(string a, string b)
	{
		foreach (var deduccion in Deducciones)
		{
			if (!((a == deduccion.Primera && b == deduccion.Segunda) || (b == deduccion.Primera && a == deduccion.Segunda)))
				continue;
			if (!TienePista(deduccion.Segunda)) return "";
			if (deduccion.Sospechoso == null ? !TienePista(deduccion.Primera) : !DioDeclaracion(deduccion.Sospechoso))
				return "";

			if (_hud.Call("agregar_nota", "Deducción: " + deduccion.Texto).AsBool())
			{
				Motor.Cambio?.Invoke();
				_hud.Call("avisar", "DEDUCCIÓN", "+1 CONCLUSIÓN");
				Sonidos.ReproducirUI(_hud, Sonidos.Tipo.Deduccion);
				GestorPartida.Instancia?.Autoguardar("Deducción");
			}
			return deduccion.Texto;
		}
		return "";
	}
}
