using Godot;

// Proveedor aislado: la prueba de interfaz no modifica ranuras ni preferencias.
public partial class CuadernoMenuPrueba : Node, ICuadernoPistas
{
    public SistemaAyudas Cuaderno { get; } = new();
    public Godot.Collections.Dictionary EstadoCuaderno() => Cuaderno.Capturar();
    public string PedirPista()
    {
        return Cuaderno.Agregar("prueba", "La zona de la fogata todavía merece otra mirada.")
            ? "Nueva pista añadida al cuaderno."
            : "La pista para este paso ya está en tu cuaderno.";
    }
}
