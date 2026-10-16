using Godot;
using System;

// Objeto de la escena que se puede examinar. Si es evidencia, se añade al panel de pistas.
public partial class ObjetoPista : StaticBody3D, IInteractuable
{
	// Identificador que usa el caso para saber qué pista se encontró (si está vacío, el nombre del nodo).
	[Export] public string Id = "";
	[Export] public string Titulo = "Pista";
	[Export(PropertyHint.MultilineText)] public string Descripcion = "";
	// false: solo muestra la descripción (decorado), no cuenta como evidencia.
	[Export] public bool EsEvidencia = true;
	// true: el detective se la lleva y desaparece de la escena.
	[Export] public bool SeRecoge = false;
	// true: en la oscuridad solo se puede examinar con la linterna encendida.
	[Export] public bool RequiereLinterna = false;
	// true: solo se alcanza agachado (algo en el suelo, entre cenizas o bajo un mueble)
	[Export] public bool SoloAgachado = false;
	// Foto tipo polaroid que muestra el HUD en la tarjeta de la pista y en el expediente.
	[Export] public Texture2D Imagen;
	// Mallas que se resaltan al apuntar cuando no son hijas de esta pista (un reloj de pared, un cartel)
	[Export] public Node3D[] Visuales = Array.Empty<Node3D>();
	// Lo que piensa el detective la primera vez que lo mira (sin pulsar nada). Vacío = nada.
	[Export(PropertyHint.MultilineText)] public string ComentarioAlMirar = "";

	[ExportGroup("Vista de examen (HU-07)")]
	// Modelo para la vista de examen. Vacío = se copian las mallas de esta pista (y sus Visuales).
	[Export] public PackedScene ModeloExamen;
	// Texto largo de la caja inferior del examen. Vacío = la descripción.
	[Export(PropertyHint.MultilineText)] public string DescripcionAmpliada = "";
	// Dónde se encontró, para la caja del examen ("Sala 1", "Cabaña de Porky"...)
	[Export] public string Lugar = "";
	// Giro inicial del modelo en el examen (grados), para que se presente por su cara buena
	[Export] public Vector3 GiroExamen = Vector3.Zero;
	// Detalles que solo se descubren examinándola (nunca al recogerla)
	[Export] public Godot.Collections.Array<DetalleOculto> Detalles = new();

	public bool Encontrada { get; private set; } = false;

	public string IdPista => string.IsNullOrEmpty(Id) ? Name.ToString() : Id;

	public override void _Ready()
	{
		// La vista de examen busca aquí el modelo de cada evidencia
		AddToGroup("objetos_pista");
	}

	public string TextoAccion => SeRecoge ? "Recoger" : "Inspeccionar";

	public void Interactuar(Jugador jugador)
	{
		if (RequiereLinterna && !jugador.LinternaEncendida)
		{
			jugador.MostrarPensamiento("Está demasiado oscuro para distinguir nada... Si encendiera la linterna [F]...", 3.0);
			return;
		}
		if (!EsEvidencia)
		{
			jugador.MostrarPensamiento(Descripcion, 4.0);
			return;
		}
		if (Encontrada)
		{
			jugador.MostrarPensamiento("Ya lo examiné: " + Titulo + ".", 2.0);
			return;
		}

		MarcarEncontrada();
		jugador.RegistrarEvidencia(this);
	}

	// También al cargar una partida: queda examinada y, si se recoge, desaparece de la escena
	public void MarcarEncontrada()
	{
		Encontrada = true;
		if (SeRecoge)
		{
			Visible = false;
			CollisionLayer = 0; // Ya no se puede apuntar ni chocar con ella
		}
	}
}
