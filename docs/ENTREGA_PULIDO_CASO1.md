# Entrega del pulido del Caso 1

Plan aprobado: `docs/ANALISIS_PULIDO_CASO1.md`. Las capturas y los reportes están en `docs/Pulido_Caso1_capturas/`.

## Pruebas

| Prueba | Resultado |
|---|---|
| `probar_pistas.gd` (reporte `pistas_despues.md`) | 30 de 30; 0 pistas visibles que no se puedan tomar |
| `probar_acusacion.gd` | 0 fallos: credibilidad agotada, caso vacío, caso incompleto con sus dos salidas, guardar y cargar en medio, careo con un error y Final A |
| `probar_escenarios.gd` | 0 fallos |
| `probar_ca03.gd` | 1 fallo en la corrida completa: la prueba buscaba el nombre viejo de la caminata de Mimita. Se corrigió la prueba y `-- solo-sala` ya no marca fallos. No se repitió la corrida completa (unos 20 minutos) |
| Reporte de choques de las 23:20 | 0 intersecciones |
| FPS | **No se pudo confirmar 60.** Da 30.0 exactos en la sala y en POV, con DOF o sin él y sin vsync. Como no cambia con nada, parece un límite externo (por ejemplo, ahorro de energía de la tarjeta), pero no lo comprobé |

## Lo que se hizo distinto a lo acordado

- **Poses sentadas por código, no en Blender.** `PoseInterrogatorio.cs` dobla las piernas encima de cualquier animación. Sirve igual para los tres tipos de esqueleto y el torso sigue con su reposo, Tenso o Alterado.
- **Tenso y Alterado de Juan Carlos, Eriz y Mimita, sin Blender.** Juan Carlos y Eriz los hacen por código. Mimita usa sus otros reposos, acelerados.
- **Motor de la escena de las 23:20.** No se tocó `CinematicaCintaPelicula`; se ampliaron `CinematicaCaso1` con J-cuts y cuadro en negro. Las poses sí vienen de Blender.
- **Lo alcanzable en `probar_pistas.gd`.** Recorre el piso en cuadrícula en lugar de usar `NavigationRegion3D`, que ninguna escena tiene.
- **Armar el caso se llena eligiendo**, no arrastrando.
- **Tony golpea la mesa con sonido y temblor de cámara**, sin animación de golpe.
- **El remo se levanta a un lado**, porque Tony no alcanza a pasarlo sobre la cabeza. La muñeca derecha queda a 7 cm del mango y la cubre la mano.
- **La entrada de cada implicado dura unos 6.5 s**, y la de Juan Carlos 7 s; pediste de 4 a 6 s. Se puede saltar.
- **Los commits 5, 6 y 7 compilan juntos**, no por separado.

## Límites conocidos

- La cola de Tony atraviesa el respaldo de la silla en la toma general: su esqueleto no tiene huesos de cola para doblarla.
- Los pies de los implicados sentados cuelgan, porque sus piernas son más cortas que la altura de la silla.
- Los videos arrancan con la introducción del caso, porque se graban desde una partida nueva.

## Para quien escribió el guion

1. **Silbato:** la historia v2 y el modelo dicen «T. S.»; `caso1_pistas.json` y `caso1_tony.json` dicen «G. S.». No se cambió ningún texto.
2. **Líneas nuevas de acusación, en borrador:** `a_aya` y `a_mimita` en `caso1_acusacion.json`.
3. **Líneas de sistema:** `GuionCaso1.LineasSistema`, en `Escenas/Caso1/GuionCaso1.Interrogatorio.cs`. Hay una burla genérica de Tony y una de presionar para todos.
4. **Final B:** dice «Miawrio y Tony se dan la mano» aunque el acusado sea Miawrio, y Hecdog dice «Me lo llevo» aunque sea Aya o Mimita.
5. **Testimonios:** sus títulos en el selector también son nuevos (`GuionCaso1.Testimonios`).
6. **Estructura:** `caso1_sala.json` tiene el nodo nuevo `armar` y su opción ahora dice «Armar el caso». Los nodos `acusar`, `paciencia`, `pregunta` y `elegir` quedaron sin uso, sin borrarse.
