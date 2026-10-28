# HU-10 · Cuaderno de pistas

En la pausa se añade la opción **06 · Pistas**. Las otras opciones conservan su orden y comportamiento.

- **Pedir una pista** añade la orientación del paso actual al cuaderno, sin temporizador.
- Las orientaciones solicitadas se conservan en orden y se pueden releer. Solicitar otra vez la misma no duplica la entrada ni aumenta el contador.
- Una orientación diferente aparece cuando avanza la investigación. No se revelan automáticamente pasos futuros.
- El cuaderno se guarda con la partida y viaja entre Campamento, Lago y estación.
- Las ayudas existentes del Caso 0 y Campamento conservan sus textos; se incluyen el registro de llamadas y las zonas del Lago.
- Desactivar Ayudas de investigación impide pedir nuevas orientaciones, pero permite leer las anteriores.
- Volver o Esc regresa a la pausa. La tecla H deja de ser un control de ayuda.
- Partidas antiguas: se recuperan los textos de los IDs de ayuda conocidos, conservando el contador anterior.

## Verificación

Compilar el proyecto .NET antes de ejecutar:

```sh
godot --headless --path game --script res://tests/ayudas_smoke.gd
godot --headless --path game --script res://tests/cuaderno_menu_smoke.gd
godot --headless --path game --script res://tests/ayudas_campamento_smoke.gd
```

Prueba manual: abrir Pausa → Pistas, pedir una, volver y reabrir; avanzar a otra zona, pedir la siguiente; guardar/cargar y cambiar entre Campamento y Lago. Ambas entradas deben seguir disponibles y en orden. Verificar navegación con teclado/control y desplazamiento del historial.
