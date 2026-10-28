using Godot;

// Cuaderno de orientaciones: se abre desde pausa y conserva las entradas de la partida.
public partial class PanelPistas : PanelMenu
{
    private RichTextLabel _paginas;
    private Label _estado;
    private Button _pedir;
    private ICuadernoPistas _caso;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        EstiloMenus.Fondo(this, opaco: false, oscurecer: 0.92f);
        var margen = new MarginContainer();
        margen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margen.AddThemeConstantOverride("margin_left", 128);
        margen.AddThemeConstantOverride("margin_right", 128);
        margen.AddThemeConstantOverride("margin_top", 55);
        margen.AddThemeConstantOverride("margin_bottom", 55);
        AddChild(margen);
        var columna = EstiloMenus.Columna(20);
        margen.AddChild(columna);
        columna.AddChild(EstiloMenus.Titulo("PISTAS", 88, 8));
        columna.AddChild(EstiloMenus.Etiqueta("Pide una orientación y vuelve a leerla cuando la necesites.", EstiloMenus.Mono(22, 0), 22, EstiloMenus.Gris));
        _paginas = new RichTextLabel
        {
            Name = "Historial", SizeFlagsVertical = SizeFlags.ExpandFill,
            FocusMode = FocusModeEnum.All, SelectionEnabled = true,
            BbcodeEnabled = false, ScrollActive = true,
        };
        _paginas.AddThemeFontOverride("normal_font", EstiloMenus.Mono(24, 0));
        _paginas.AddThemeFontSizeOverride("normal_font_size", 24);
        _paginas.AddThemeColorOverride("default_color", EstiloMenus.Gris);
        columna.AddChild(_paginas);
        _estado = EstiloMenus.Etiqueta("", EstiloMenus.Mono(20, 0), 20, EstiloMenus.Gris);
        _estado.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        columna.AddChild(_estado);
        var acciones = new HBoxContainer();
        acciones.AddThemeConstantOverride("separation", 40);
        columna.AddChild(acciones);
        _pedir = new OpcionMenu("+", "PEDIR UNA PISTA") { Name = "PedirPista" };
        acciones.AddChild(_pedir);
        _pedir.Pressed += () =>
        {
            _estado.Text = _caso?.PedirPista() ?? "No hay orientaciones disponibles en este escenario.";
            Actualizar();
            _paginas.CallDeferred(RichTextLabel.MethodName.ScrollToLine, _paginas.GetLineCount() - 1);
        };
        var volver = new OpcionMenu("←", "VOLVER") { Name = "BotonVolver", UniqueNameInOwner = false };
        acciones.AddChild(volver);
        volver.Pressed += Cerrar;
        FocoInicial = _pedir;
        base._Ready();
    }

    public override void Abrir()
    {
        _caso = Buscar(GetTree().CurrentScene);
        _pedir.Disabled = _caso == null;
        _estado.Text = _caso == null ? "No hay orientaciones disponibles en este escenario." : "";
        Actualizar();
        base.Abrir();
    }

    private void Actualizar()
    {
        var entradas = _caso?.Cuaderno.Entradas;
        _paginas.Text = "";
        if (entradas == null || entradas.Count == 0)
        {
            _paginas.Text = "Tu cuaderno está vacío.\n\nLas pistas que solicites aparecerán aquí.";
            return;
        }
        for (int i = 0; i < entradas.Count; i++)
            _paginas.AddText($"{i + 1:00} · {entradas[i]["texto"].AsString()}\n\n");
    }

    private static ICuadernoPistas Buscar(Node nodo)
    {
        if (nodo == null) return null;
        if (nodo is ICuadernoPistas caso) return caso;
        foreach (Node hijo in nodo.GetChildren())
            if (Buscar(hijo) is ICuadernoPistas encontrado) return encontrado;
        return null;
    }
}
