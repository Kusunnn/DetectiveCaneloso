using Godot;
using System;
using System.Reflection;
using System.Threading.Tasks;

// Para probar_ca03.gd: lo que no se puede pedir desde GDScript sin jugar todo el capítulo a mano.
public partial class FlujoCaso1Prueba : Node
{
	private const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;

	public bool AcusacionTerminada { get; private set; }

	// Hecdog ya tiene las confesiones de los cinco y Caneloso acusa: corre la acusación y el final como en la partida
	public async void Acusar(Node escenario, bool aTony)
	{
		var guion = Guion(escenario);
		foreach (string c in new[] { "juan_carlos_3", "aya_2", "miawrio_2", "eriz_1", "mimita_2" })
			guion.Estado.DesbloquearConclusion(c);
		Func<bool> sigue = () => IsInstanceValid(escenario) && escenario.IsInsideTree();
		await (Task)typeof(GuionCaso1).GetMethod("TerminarAcusacion", Privado).Invoke(guion, new object[] { aTony ? "a_tony" : "a_juan_carlos", sigue });
		AcusacionTerminada = true;
	}

	private GuionCaso1 Guion(Node escenario) => (GuionCaso1)typeof(EscenarioCaso1).GetField("Guion", Privado).GetValue(escenario);

	public int UsosAyudas(Node escenario) => ((EscenarioCaso1)escenario).Cuaderno.Usos;
	public int Errores(Node escenario, string candado) => Guion(escenario).Estado.Contador("errores:" + candado);
	public int ValorCredibilidad(Node escenario) => Guion(escenario).Estado.Contador("credibilidad");
	public bool EnEspera(Node escenario) => Guion(escenario).EnEspera;
	public bool TieneConclusion(Node escenario, string id) => Guion(escenario).Estado.TieneConclusion(id);
	public bool EnDialogo(Node escenario) => (bool)typeof(EscenarioCaso1).GetField("EnDialogo", Privado).GetValue(escenario);
	public string Hueco(Node escenario, string id) => Guion(escenario).Huecos.TryGetValue(id, out string ficha) ? ficha : "";
	public bool Sellada(Node escenario, int vineta) => Guion(escenario).Selladas.Contains(vineta);

	// La noche ya reconstruida y la primera excusa de Tony tumbada
	public void ReconstruccionCompletaYPrimeraExcusa(Node escenario)
	{
		var guion = Guion(escenario);
		guion.Estado.DesbloquearConclusion("tony_1");
		guion.Estado.MarcarNodo("caso1_tony", "excusa_1");
		for (int v = 0; v < guion.Vinetas.Count; v++)
		{
			foreach (var (id, correcta) in guion.HuecosDe(v)) guion.Huecos[id] = correcta;
			guion.Selladas.Add(v);
		}
	}

	public void DarConclusion(Node escenario, string id) => Guion(escenario).Estado.DesbloquearConclusion(id);

	public void RomperTony(Node escenario)
	{
		for (int i = 1; i <= 5; i++) Guion(escenario).Estado.DesbloquearConclusion("tony_" + i);
	}

	// Todas las pistas en el diario (el título es su id) y todas las confesiones de los cinco
	public void DarTodo(Node escenario)
	{
		DarPistas(escenario);
		foreach (string c in new[] { "juan_carlos_3", "jc_vio_tony_sendero", "jc_vio_linterna", "aya_2", "foto_aya_2319", "foto_aya_2312", "miawrio_2", "miawrio_coartada", "miawrio_oyo_cita", "eriz_1", "eriz_hora_crimen", "mimita_2", "mimita_oyo_silbido" })
			Guion(escenario).Estado.DesbloquearConclusion(c);
	}

	public void DarPistas(Node escenario)
	{
		var jugador = escenario.GetParent().GetNode<Jugador>("Jugador");
		foreach (string id in new[] { "interruptor_generador", "candado_caseta", "baul_disfraces", "lena_intacta", "libro_guardia_quemado", "nota_porky", "foto_grupo", "recorte_periodico", "oferta_compra", "taza_delantal", "registro_enfermeria", "botas_tony", "registro_llamadas", "huellas_pezuna", "huellas_detenidas", "altavoz", "cornamenta_zancos", "hueco_remo", "remo_sangre", "silbato_ts", "linterna_porky", "cuaderno_eriz" })
			jugador.RegistrarEvidencia(id, id, "", null, false);
	}

	public void Credibilidad(Node escenario, int valor)
	{
		var estado = Guion(escenario).Estado;
		estado.Sumar("credibilidad", valor - estado.Contador("credibilidad"));
	}

	public bool PasoPorAcusacion(Node escenario) => Guion(escenario).Estado.NodoCompletado(GuionCaso1.ArbolAcusacion, "equivocado");

	public string Final(Node escenario)
	{
		var estado = Guion(escenario).Estado;
		return estado.NodoCompletado(GuionCaso1.ArbolAcusacion, "final_a") ? "A" : estado.NodoCompletado(GuionCaso1.ArbolAcusacion, "final_b") ? "B" : "";
	}

	public void SaltarCinematica(Node cinematica)
	{
		((Node)cinematica).Call("Saltar");
	}

	public bool Resuelto(Node escenario)
	{
		var guion = (GuionCaso1)typeof(EscenarioCaso1).GetField("Guion", Privado).GetValue(escenario);
		return guion.Resuelto;
	}

	// Abre el detalle de un capítulo resuelto en el tablero, como al hacer clic en su carpeta
	public void AbrirDetalle(Node tablero, string capitulo)
	{
		var elegir = typeof(TableroCapitulos).GetMethod("Elegir", Privado);
		var estado = Enum.Parse(elegir.GetParameters()[1].ParameterType, "Resuelto");
		elegir.Invoke(tablero, new object[] { Capitulos.Obtener(capitulo), estado, null });
	}
}
