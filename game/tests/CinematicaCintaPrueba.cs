using Godot;
using System.Linq;

// Lanza la cinemática final del Caso 0 sin jugar el caso (para cinematica_cinta_smoke.gd y capturas).
public partial class CinematicaCintaPrueba : Node
{
	public CinematicaCinta Cinematica { get; private set; }
	public bool Terminada { get; private set; }

	// Para comparar: true = momentos congelados, false = la versión animada
	public bool Momentos { get => CinematicaCinta.UsarMomentosCongelados; set => CinematicaCinta.UsarMomentosCongelados = value; }

	// 0 = animada, 1 = momentos congelados, 2 = película
	public int Version { get => (int)CinematicaCinta.Version; set => CinematicaCinta.Version = (CinematicaCinta.VersionCinta)value; }

	public async void Empezar(Node3D sala, bool manual)
	{
		var caso = sala.GetNode<Caso0>("Caso0");
		// Como al tener la cinta: Eriz y Porky esperan en la Sala 1
		caso.Eriz.GlobalTransform = caso.PuntoErizFinal.GlobalTransform;
		caso.Porky.GlobalTransform = caso.PuntoPorkyFinal.GlobalTransform;
		var tarea = CinematicaCinta.Reproducir(sala, new CinematicaCinta.Actores
		{
			Porky = caso.Porky, Eriz = caso.Eriz, Jugador = caso.Jugador, Reloj = caso.RelojSala, Grabadora = caso.GrabadoraSala, Musica = caso.Musica,
			Lineas = new[] { "[Tito Garras] «...sí, yo me robé la cartera del alcalde.»", "[Ronquidos. Ruido metálico en el techo]", "«La Garra no olvida, Caneloso.»" },
		});
		Cinematica = sala.GetChildren().OfType<CinematicaCinta>().LastOrDefault();
		if (Cinematica != null) Cinematica.Manual = manual;
		await tarea;
		Terminada = true;
	}
}
