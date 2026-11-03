using Godot;
using System.Linq;

// Monta una cinemática del Caso 1 sin jugar el capítulo (para Herramientas/Caso1/capturas_cinematicas.gd).
public partial class CinematicasCaso1Prueba : Node
{
	public CinematicaCaso1 Cinematica { get; private set; }

	// Escena donde se filma: vacío si el libreto carga su propio mundo
	public static string EscenaDe(string cual) => cual switch
	{
		"libro" or "foto" => "res://Escenas/Caso1/escenario_campamento.tscn",
		"llegada" or "silbato" => "res://Escenas/Caso1/Lago/escenario_lago.tscn",
		_ => "",
	};

	public string Escena(string cual) => EscenaDe(cual);

	public void Empezar(string cual, string comentario, string segundo)
	{
		ObjetoPista Pista(string id) => GetTree().GetNodesInGroup("objetos_pista").OfType<ObjetoPista>().First(p => p.IdPista == id);
		var libreto = cual switch
		{
			"libro" => LibretosCaso1.Libro(comentario),
			"foto" => LibretosCaso1.Foto(comentario, Pista("foto_grupo").GlobalPosition, Pista("foto_grupo").GlobalBasis.Z),
			"llegada" => LibretosCaso1.Llegada(new[] { comentario, segundo, comentario }),
			"silbato" => LibretosCaso1.Silbato(comentario, Pista("silbato_ts")),
			"reconstruccion" => LibretosCaso1.Reconstruccion(comentario),
			"final_a" => LibretosCaso1.FinalA(comentario),
			"final_b_porche" => LibretosCaso1.FinalBPorche(comentario),
			_ => LibretosCaso1.FinalBLago(comentario, segundo),
		};
		Cinematica = CinematicaCaso1.Crear(this, libreto);
		Cinematica.Manual = true;
	}

	public double Total => Cinematica.Total;
	public double[] Inicios => Cinematica.Inicios;
	public void IrA(double t) => Cinematica.IrA(t);
}
