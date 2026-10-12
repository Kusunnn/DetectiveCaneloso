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
	// Foto tipo polaroid que muestra el HUD en la tarjeta de la pista y en el expediente.
	[Export] public Texture2D Imagen;
	// Mallas que se resaltan al apuntar cuando no son hijas de esta pista (un reloj de pared, un cartel)
	[Export] public Node3D[] Visuales = Array.Empty<Node3D>();
	// Lo que piensa el detective la primera vez que lo mira (sin pulsar nada). Vacío = nada.
	[Export(PropertyHint.MultilineText)] public string ComentarioAlMirar = "";

	public bool Encontrada { get; private set; } = false;

	public string IdPista => string.IsNullOrEmpty(Id) ? Name.ToString() : Id;

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

		Encontrada = true;
		jugador.RegistrarEvidencia(IdPista, Titulo, Descripcion, Imagen);
		if (SeRecoge)
		{
			Visible = false;
			CollisionLayer = 0; // Ya no se puede apuntar ni chocar con ella
		}
	}
}
