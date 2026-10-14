using Godot;
using System.Collections.Generic;

// Comprueba el puente GDScript → C# y el uso exclusivo de testimonios ya reunidos.
public partial class PizarronCasoPrueba : Node
{
    public int Comprobaciones { get; private set; }
    public void Configurar(TableroDeduccion tablero, Node hud)
    {
        tablero.ObtenerCartas = () => new List<Carta> { new Carta("testimonio_eriz", "Eriz: volvió a las 3:00", "TESTIMONIO") };
        tablero.Probar = (a, b) =>
        {
            Comprobaciones++;
            string resultado = (a == "prueba_a" && b == "prueba_b") || (b == "prueba_a" && a == "prueba_b")
                ? "La grabadora y el reloj coinciden en la hora."
                : (a == "testimonio_eriz" && b == "prueba_b") || (b == "testimonio_eriz" && a == "prueba_b")
                    ? "El testimonio de Eriz coincide con el reloj." : "";
            if (resultado != "") hud.Call("agregar_nota", "Deducción: " + resultado);
            return resultado;
        };
    }
}
