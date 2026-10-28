using Godot;
using System;
using Diccionario = Godot.Collections.Dictionary;

public interface ICuadernoPistas
{
    SistemaAyudas Cuaderno { get; }
    string PedirPista();
}

// Orientaciones solicitadas, compartidas por los escenarios y guardadas con la partida.
public sealed class SistemaAyudas
{
    public int Usos { get; private set; }
    private static readonly System.Collections.Generic.Dictionary<string, (string Id, string Texto)> Anteriores = new()
    {
        { "sala", ("caso0:sala", "Todavía hay algo que revisar en la Sala 1.") },
        { "tablero_sala", ("caso0:tablero_sala", "Conviene repasar en el tablero lo que encontré en la Sala 1.") },
        { "pasillo", ("caso0:pasillo", "Quizá el pasillo conserve algo que pasé por alto.") },
        { "recepcion", ("caso0:recepcion", "Debería revisar con calma la recepción.") },
        { "tablero_recepcion", ("caso0:tablero_recepcion", "Lo que encontré en recepción merece otra mirada en el tablero.") },
        { "comisario", ("caso0:comisario", "El comisario en recepción podría orientarme sobre cómo continuar.") },
        { "espera", ("caso0:espera", "Tal vez dejé algo sin revisar en la zona de espera de recepción.") },
        { "vestidores", ("caso0:vestidores", "Aún queda algo por investigar en los vestidores.") },
        { "resolver", ("caso0:resolver", "Es momento de volver a la Sala 1 y hablar de lo ocurrido.") },
        { "fogata", ("caso1:fogata", "La zona de la fogata todavía merece otra mirada.") },
        { "cocina", ("caso1:cocina", "Quizá haya algo pendiente en la cocina.") },
        { "porky", ("caso1:porky", "Ahora puedo investigar con más calma la cabaña de Porky.") },
        { "almacen", ("caso1:almacen", "Debería revisar el almacén.") },
        { "enfermeria", ("caso1:enfermeria", "La enfermería es un buen lugar para seguir investigando.") },
        { "director", ("caso1:director", "Conviene recorrer los alrededores de la cabaña del director.") },
        { "caseta", ("caso1:caseta", "Falta revisar algo en la caseta del generador.") },
        { "coche", ("caso1:coche", "Podría volver al estacionamiento.") },
        { "lago", ("caso1:lago", "Ya revisé el campamento. Puedo seguir el sendero al Lago Espejo, al norte.") },
    };
    private Godot.Collections.Array<Diccionario> _entradas = new();
    public Godot.Collections.Array<Diccionario> Entradas => _entradas.Duplicate(true);

    public bool Agregar(string id, string texto)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(texto)) return false;
        foreach (var entrada in _entradas)
            if (entrada["id"].AsString() == id) return false;
        _entradas.Add(new Diccionario { { "id", id }, { "texto", texto } });
        Usos++;
        return true;
    }

    public Diccionario Capturar() => new()
    {
        { "usos", Usos }, { "entradas", _entradas.Duplicate(true) },
    };

    public void Restaurar(Diccionario estado)
    {
        Usos = estado.TryGetValue("usos", out var usos) ? Math.Max(0, usos.AsInt32()) : 0;
        _entradas.Clear();
        if (estado.TryGetValue("entradas", out var entradas))
            foreach (var valor in entradas.AsGodotArray())
            {
                var entrada = valor.AsGodotDictionary();
                if (entrada.ContainsKey("id") && entrada.ContainsKey("texto"))
                    _entradas.Add(entrada.Duplicate(true));
            }
        // Recupera los textos conocidos de versiones que solo guardaban IDs.
        else if (estado.TryGetValue("historial", out var historial))
            foreach (var valor in historial.AsGodotArray())
            {
                string id = valor.AsString();
                string texto = "Orientación de una partida anterior: " + id;
                if (Anteriores.TryGetValue(id, out var anterior)) { id = anterior.Id; texto = anterior.Texto; }
                bool existe = false;
                foreach (var entrada in _entradas) if (entrada["id"].AsString() == id) existe = true;
                if (!existe) _entradas.Add(new Diccionario { { "id", id }, { "texto", texto } });
            }
    }
}
