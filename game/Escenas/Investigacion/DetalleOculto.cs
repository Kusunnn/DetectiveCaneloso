using Godot;

// Detalle que una evidencia esconde y que solo se descubre en la vista de examen (HU-07).
// Se crea como recurso (.tres) y se asigna a ObjetoPista.Detalles desde el inspector, sin tocar código.
// Las coordenadas son las del modelo de examen, antes de centrarlo y escalarlo.
[GlobalClass]
public partial class DetalleOculto : Resource
{
	// El id de la entrada nueva es "<id de la evidencia>.<Id>", por ejemplo "foto_grupo.silbato"
	[Export] public string Id = "";
	[Export] public string Titulo = "Detalle";
	[Export(PropertyHint.MultilineText)] public string Descripcion = "";
	// Lo que piensa Caneloso al descubrirlo
	[Export(PropertyHint.MultilineText)] public string Pensamiento = "";
	// Punto del modelo donde está el detalle
	[Export] public Vector3 Posicion = Vector3.Zero;
	// Hacia dónde mira el detalle: hay que girar el objeto hasta que esta cara apunte a la cámara
	[Export] public Vector3 Normal = Vector3.Back;
	// Cuánto puede desviarse la vista de la normal y seguir viéndose (grados)
	[Export(PropertyHint.Range, "5,90,1")] public float Tolerancia = 35f;
	// Acercamiento mínimo para leerlo (1 = sin acercar, 3 = máximo)
	[Export(PropertyHint.Range, "1,3,0.1")] public float ZoomMinimo = 1f;
	[Export] public bool RequiereLinterna = false;
}
