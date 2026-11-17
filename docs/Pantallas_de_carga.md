# Pantallas de carga

Las fotos son capturas del juego. El fondo cambia cada 20 segundos con un fundido y el nivel se abre en cuanto termina la carga (con un mínimo de dos segundos para que se alcance a ver la pantalla). La pantalla elige un dato al azar al abrirse y cada 10 segundos, acompaña cada dato con su modelo y evita repetir el último. Todos los iconos giran de frente, como la manecilla de un reloj. Los rumores y anécdotas compartidas llevan el modelo de Caneloso.

- Tutorial, «La cinta»: sala de interrogatorio, recepción y vestuario.
- Caso 1, «La noche del Ciervo de Luz»: fogata, cabañas y sala de interrogatorio.
- Lago y bosque, segundo escenario del Caso 1: lago, embarcadero y bosque.

Cuando el Caso 2 tenga una escena en `game/Datos/capitulos.json`, agrega sus capturas a `game/Assets/Carga/capitulo2/` con los nombres `1.jpg`, `2.jpg` y `3.jpg`. La pantalla usa el identificador del capítulo para encontrar su galería. Si todavía no hay fotos, usa la ilustración del capítulo cuando esté disponible.

La lista de datos y modelos está en `game/Escenas/UI/PantallaCarga.cs`. Cada entrada asocia un texto al archivo del personaje.
