using Godot;

// Coleccionable opcional: sus herramientas se conservan entre capítulos y repeticiones.
public partial class HerramientaTablero : PuntoInteraccion
{
    public const string RutaColeccion = "user://coleccion_tablero.cfg";
    [Export] public string Herramienta = "notas";

    public static bool Desbloqueada(string id)
    {
        var datos = new ConfigFile();
        return datos.Load(RutaColeccion) == Error.Ok && datos.GetValue("herramientas", id, false).AsBool();
    }

    public override void _Ready()
    {
        AddToGroup("herramientas_tablero");
        if (Desbloqueada(Herramienta)) Ocultar();
    }

    public override void Interactuar(Jugador jugador)
    {
        if (!Visible || Desbloqueada(Herramienta)) return;
        var datos = new ConfigFile();
        datos.Load(RutaColeccion);
        datos.SetValue("herramientas", Herramienta, true);
        if (datos.Save(RutaColeccion) != Error.Ok) return;
        var hud = GetTree().GetFirstNodeInGroup("hud_investigacion");
        hud?.Call("desbloquear_herramienta_tablero", Herramienta);
        jugador.DarObjeto("herramienta_tablero:" + Herramienta, Herramienta == "notas"
            ? "Notas adhesivas. Me servirán para escribir mis ideas en el tablero."
            : "Un marcador negro. Puedo dibujar sobre mis notas del tablero.");
        Ocultar();
    }

    private void Ocultar()
    {
        Visible = false;
        CollisionLayer = 0;
    }
}
