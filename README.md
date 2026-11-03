# DetectiveCaneloso
Detective Caneloso es un videojuego 2.5D de misterio y deducción dirigido a jóvenes y adultos, que ejercite el pensamiento crítico y la atención al detalle mediante una experiencia narrativa de investigación, diferenciándose de los juegos de acción sin trama y juegos de del mismo género que son monótonos o que cuentan con el mismo final.

## HU-10 · Ayudas de investigación

- La acción **Ayuda** (H por defecto, reasignable) ofrece orientación después de 120 segundos de investigación activa sin encontrar una pista nueva. `UmbralAyudaSegundos` se configura en el inspector del controlador de cada caso.
- Solo el tutorial muestra una vez el aviso que enseña a pedir ayuda. En los demás casos no hay avisos ni orientaciones automáticas del sistema.
- Pausas, diálogos, tablero, diario, expediente y examen de evidencias no avanzan el contador. Una pista o detalle nuevo lo reinicia.
- **Opciones → Pantalla → Ayudas de investigación** permite desactivar el sistema; es independiente de las ayudas visuales del tutorial.
- Las partidas guardan el contador, las ayudas utilizadas y sus identificadores. Las partidas anteriores empiezan con registro vacío. La pantalla de cierre del Caso 0 incluye el total, sin penalizar la calificación.
- El Caso 1 conserva el registro, pero aún no tiene cierre: su salida sigue anunciando el escenario siguiente en preparación.

Verificación: compilar el proyecto .NET y ejecutar Godot .NET con `--headless --path game --script res://tests/ayudas_smoke.gd`. La prueba no modifica partidas ni preferencias del usuario.
