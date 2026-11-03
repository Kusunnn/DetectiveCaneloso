# Auditoría CA-03 · HU-9.5 · Caso 1

**CA-03:** No deben haber animaciones ni cinemáticas repetidas sin contexto alguno.

Rama: `HU-9.5-Animaciones-Tutorial` (d25b2aa: main y dialogos-caso1 integrados). Fecha: 2026-10-27.
Esta primera parte es la revisión del 27 de octubre. **El estado final está en «Cierre de CA-03», al final.**

## Alcance

CA-01 y CA-02 están marcados como hechos, pero en el repositorio no existen cinemáticas del Caso 1 ni NPC con rutinas.
Takeshi aprobó auditar CA-03 con lo que ya existe y dejar CA-01 y CA-02 como una tarea aparte:

- **Cinemáticas:** `IntroCaso` (presentación del caso con el expediente; en el Caso 1 sale al empezarlo) y `CinematicaCinta` (final del Caso 0, hecha en Blender).
- **Escenas guionizadas que funcionan como cinemática:** la llegada al campamento (Hecdog, `caso1_introduccion.json`) y la llegada a la sala de interrogatorio (Hecdog presenta a los seis).
- **NPC:** `personajes_vista_previa.tscn` (Campamento) y `sala_interrogatorio_caso1.tscn` (Hecdog y los seis implicados). En el Lago no hay ningún NPC.

Cómo revisé: leí el código de `IntroCaso`, `CinematicaCinta`, `GestorPartida`, `Caso0`, `Caso1`, `EscenarioCaso1`, `Caso1Lago`, `SalaInterrogatorioCaso1`, `GuionCaso1`, `Npc`, `AnimacionPersonaje` y `expediente_caso.gd`. También corrí en headless `probar_escenarios.gd`: el recorrido Campamento ↔ Lago ↔ sala, guardar y cargar en el Lago, terminó con 0 fallos.
Lo que depende de verlo en pantalla (poses, choques con muebles) está marcado como **por verificar**.

## Tabla

Gravedad: **Crítica** (contradice la historia), **Alta** (se nota en una partida normal), **Media** (se nota si el jugador busca), **Baja** (detalle).

| ID | Tipo | Dónde | Problema | Cómo reproducirlo | Corrección propuesta | Gravedad |
|---|---|---|---|---|---|---|
| C-01 | Cinemática | `IntroCaso.cs` (`Vista`/`MarcarVista` en `user://cinematicas.cfg`), `Caso0.cs` (`_resuelto`) | No hay un registro central de cinemáticas vistas dentro de la partida. IntroCaso guarda «vista» en un archivo global de la PC, compartido por todas las ranuras, y la cinta depende solo de `_resuelto`. Una cinemática nueva del Caso 1 no tendría dónde consultarlo. | Revisar el código. Jugar el Caso 1 en la ranura 1 y repetirlo desde el tablero: decide con el archivo global y no con la partida. | Agregar `CinematicasVistas` (lista de ids) a la partida en GestorPartida, que se guarde y se cargue con la ranura. `IntroCaso` y `CinematicaCinta` lo consultan y lo marcan. `cinematicas.cfg` se queda solo para la regla «al rejugar, si ya la viste». | Media |
| C-02 | Cinemática | Inicio del Caso 1: `GestorPartida.PresentarCapitulo` → `Caso1.Empezar` → `caso1_introduccion.json` | Se presenta el mismo expediente tres veces seguidas: IntroCaso lo muestra hoja por hoja con su objetivo, Hecdog dice «Aquí está el expediente» y, al terminar el diálogo, el expediente se abre solo. | Nueva partida → Caso 1. | Si IntroCaso se acaba de ver en esta partida, no abrir el expediente solo al terminar el diálogo: basta con el aviso «[TAB] Revisar el expediente». No cambio la línea de Hecdog porque es del guion. | Media |
| C-03 | Cinemática | `IntroCaso.ProcesarFinal` (aviso de 0.8 a 5.8 s) y `Caso1.Empezar` (diálogo a los 0.8 s) | El aviso del expediente aparece encima de la primera línea de Hecdog: dos cosas a la vez y sin relación entre sí. | Nueva partida → Caso 1 y dejar que IntroCaso termine. | En el Caso 1, quitar el aviso del final de IntroCaso (la escena da su propio contexto) o empezar el diálogo cuando el aviso se va. | Baja |
| C-04 | Cinemática | `expediente_caso.gd` (`ver_presentacion`), `TableroCapitulos.cs` | Durante el fundido final de IntroCaso, `_Input` ya no se queda con la entrada (`_terminada`). Un clic en «VER PRESENTACIÓN» en ese segundo crea una segunda IntroCaso encima de la primera. | Expediente → VER → saltar → hacer clic de inmediato en VER. | Que solo pueda haber una a la vez: `IntroCaso.EnCurso` hasta el `QueueFree`, y `Reproducir` no hace nada si ya hay una. | Baja |
| C-05 | Cinemática | Solo existe la presentación (expediente y tablero) | «Volver a ver» solo existe para IntroCaso, y la repetición no cambia el progreso: correcto. La cinta del Caso 0 no se puede volver a ver si no se rejuega todo el caso. | Terminar el Caso 0 y buscar cómo volver a ver la cinta. | Agregar «Volver a ver» en el expediente o el diario con las cinemáticas que tiene el registro de C-01, en modo de solo ver: sin notas, objetivo, guardado ni sonidos de pista. | Baja |
| C-06 | Escena guionizada | `Caso1.AlUsarSalida` (opción «Ir a la estación con Hecdog»), `Caso1Lago.AlIrEstacion` | Fuera de orden: con 8 de 22 pistas del campamento se puede ir directo a la estación sin pasar por el Lago, y Hecdog presenta a los seis sin las pruebas del Lago (remo, embarcadero). La salida del Lago a la estación tampoco pide nada. | Nueva partida → juntar 8 pistas → sendero → «Ir a la estación». | **Decisión de historia (Takeshi):** pedir haber visitado el Lago, o un mínimo de sus pistas, antes de ofrecer la estación, y que Caneloso diga por qué todavía no. | Media |
| C-07 | Cinemática | `GestorPartida.PresentarCapitulo` (modo cargar) | Correcto: al cargar no vuelve a salir IntroCaso, sale el «Anteriormente…». | Guardar en el campamento y cargar. | No hace falta corregir. | OK |
| C-08 | Escena guionizada | `Caso1.Empezar`, `SalaInterrogatorioCaso1.AlLlegar` | Correcto: al ir y volver Campamento ↔ Lago ↔ sala no se repiten la llegada de Hecdog ni la presentación de la sala (`EstadoPendiente`, `SalaVista`). La prueba headless lo confirma. | `probar_escenarios.gd`. | No hace falta corregir. | OK |
| C-09 | Cinemática | `CinematicaCinta.Reproducir`, `Caso0.Resolver` | Correcto: `EnCurso` y `_resuelto` evitan que se dispare dos veces o que vuelva a salir después de saltarla. Al cargar después del cierre no vuelve a salir. | Resolver el Caso 0 y saltar la cinta. | Pasarla al registro de C-01 para que sea igual que las demás. | OK |
| C-10 | Escena guionizada | `SalaInterrogatorioCaso1.AlLlegar` | Cada vez que se entra a la sala sale el mismo pensamiento: «La sala de interrogatorio de la estación. Aquí van a pasar uno por uno.» | Ir a la sala, salir y volver. | Mostrarlo solo la primera vez después de la presentación, o cambiarlo por el objetivo actual. | Baja |
| N-01 | NPC | `personajes_vista_previa.tscn`, nodo `Porky` (1.8, 0, 11) | **Porky, la víctima, está de pie y respirando en el campamento.** Contradice la historia: solo puede aparecer en flashbacks y fotos. | Nueva partida → Caso 1 → ir al centro del campamento. | Quitar a Porky de `personajes_vista_previa.tscn`. | Crítica |
| N-02 | NPC | Campamento: Eriz, Juan Carlos y Mimita (`AnimacionPersonaje` por código) | Solo tienen reposo por código: brazos fijos, respiración y voltear la cabeza. Repiten el mismo ciclo para siempre, sin variantes, pausas ni cambio de actividad. La fase aleatoria evita que respiren a la vez. | Quedarse mirando a cualquiera de los tres un minuto. | Variantes de reposo por código: cambiar el peso de pie, mirar alrededor, frotarse los brazos (frío de madrugada), con pausas aleatorias. Si el modelo lo aguanta, pasarlos a `AnimadorPersonaje` con UAL, como en el Caso 0. | Media |
| N-03 | NPC | Campamento: Tony (`InacTony`), Aya (`Inaya`), Miawrio (`Inactivo`) | Una sola animación Mixamo en bucle para siempre. El punto de inicio sí es aleatorio, así que no se ven como clones. | Quedarse mirando un minuto. | Alternar el reposo con 1 o 2 variantes cada 10 a 25 s con mezcla, y velocidad aleatoria entre 0.9 y 1.1. Usar solo las animaciones que traen sus modelos. | Media |
| N-04 | NPC | Sala: Eriz, Juan Carlos y Mimita (`Datos.Caminar` vacío) | Entran y salen de la sala deslizándose, con el reposo puesto: **los pies patinan**. | Interrogar a Eriz en la sala. | Paso por código mientras se mueven (balanceo y ligero rebote), o una animación de caminar si su esqueleto la acepta. | Alta |
| N-05 | NPC | Sala: los seis en `PuntoSilla` | Llegan a la silla y se quedan de pie, porque no hay pose de sentado. Pueden atravesar la silla o la mesa. **Por verificar con captura.** | Interrogar a cada uno y mirar la silla. | Ajustar `PuntoSilla` para que queden de pie junto a la silla o, si la hay, usar una pose de sentado. | Media |
| N-06 | NPC | `Npc.MirarA` (Campamento y sala) | Al hablar, el que gira es el nodo `Colision`, no el personaje: no se voltea hacia Caneloso. Solo la cabeza lo sigue. | Hablar con Tony desde un lado. | `MirarA` gira el padre del `Npc` (el personaje) cuando el `Npc` es su `Colision`. | Media |
| N-07 | NPC | Caso 1 en general | No hay reacciones al avance del caso: `FijarAnimo` no se llama en el Caso 1. Tampoco hay saludos ni sobresaltos que se repitan, así que no hay nada que se reinicie al cargar. | Revisar el código. | Cuando existan (CA-02), guardar el ánimo de cada NPC en la partida, junto al registro de C-01. | Baja |
| N-08 | NPC | Campamento y Lago | Hecdog habla en la llegada al campamento y en el Lago («Hecdog espera en la patrulla»), pero no aparece en esos escenarios: habla sin cuerpo. | Nueva partida → Caso 1. | Ponerlo en el estacionamiento, junto a la patrulla, con un reposo propio. Es más de CA-02 que de CA-03. | Baja |
| N-09 | NPC | Campamento | Nadie tiene actividad ni objeto en la mano, así que no hay actividades sin sentido. Tampoco hay rutas, así que no hay bucles visibles. | — | Cuando se hagan las rutinas de CA-02, cada actividad lleva su objeto (BoneAttachment3D) y las rutas tienen pausas. | OK |
| N-10 | NPC | Eriz, Juan Carlos, Mimita (bajan los brazos por código) | Si `AnimacionPersonaje` no encuentra los huesos `Arm_1_L/R`, el personaje se queda en pose T. **Por verificar con captura** de los tres en el Campamento y en la sala. | Captura con `capturas_escenario.gd`. | Si alguno sale en pose T, corregir los nombres de los huesos o su `AnguloBrazos`. | Por verificar |

## Resultado de las correcciones (2026-10-27, aprobadas por Takeshi)

| ID | Estado | Commit | Qué se hizo / por qué no |
|---|---|---|---|
| C-01 | Corregido | 608c6d4 | `GestorPartida` guarda las cinemáticas vistas (`cinematicas`) en cada ranura. IntroCaso (`intro_casoXX`) y la cinta (`caso0_cinta`) se registran ahí, y la señal `CinematicaReproducida` sirve para las pruebas. Una repetición desde el tablero empieza con su propio registro. |
| C-02 | Corregido | 608c6d4 | Si IntroCaso ya mostró el expediente, al terminar con Hecdog ya no se abre otra vez: sale el aviso «[TAB] Revisar el expediente cuando quieras». Las líneas del guion no cambiaron. |
| C-03 | Corregido | 608c6d4 | En el Caso 1, IntroCaso termina sin su aviso (`PresentarCapitulo(avisoExpediente: false)`). El aviso sale después de Hecdog. |
| C-04 | Corregido | 608c6d4 | Solo puede haber una IntroCaso en la raíz (nodo `IntroCaso`). El expediente y el tablero no ponen otra encima. |
| C-05 | No corregido | — | Volver a ver la cinta del Caso 0 necesita la Sala 1 con sus actores (la cinemática mueve los objetos reales de la sala), así que no se puede abrir desde el expediente o el diario de otro caso. Queda la presentación, que ya se puede volver a ver sin efectos. |
| C-06 | Corregido | 608c6d4 | La estación pide 5 de las 9 pistas del Lago (`PartidaEscenas.PistasLagoParaEstacion`), tanto desde el campamento como desde la patrulla del Lago. Si la sala ya se visitó, no se bloquea. |
| C-10 | Corregido | 608c6d4 | Al volver a la sala ya no se repite el pensamiento: queda el objetivo. |
| C-11 | Corregido (nuevo) | 57015f8 | Encontrado en las pruebas: lo que Caneloso piensa al llegar al Lago se repetía cada vez que volvía sin pistas de ahí. Ahora sale una vez y se guarda con las zonas vistas. |
| N-01 | Corregido | 8f2c5dd | Porky ya no está en `personajes_vista_previa.tscn`. Captura: `HU-9.5_CA03_capturas/campamento/donde_estaba_porky.png`. |
| N-02 | Corregido | 8f2c5dd | El reposo por código cambia de actividad al azar (cargar el peso, mirar hacia un lado, quedarse quieto) con mezcla suave, y cada personaje tiene su propio ritmo (0.9 a 1.1). |
| N-03 | Corregido | 8f2c5dd | Tony, Aya y Miawrio intercalan su animación «tenso» (`TensoTony`, `Tensaya`, `Tenso`) cada 12 a 26 s, a velocidad distinta cada uno. |
| N-04 | Corregido | 8f2c5dd | Eriz y Juan Carlos mueven piernas y brazos y suben y bajan al caminar en la sala. Mimita, que no tiene esqueleto, se balancea con cada paso. La prueba confirma que las piernas se mueven. |
| N-05 | Verificado | — | Las capturas `sala_*/lado.png` muestran que se quedan de pie junto a la silla, sin atravesarla. No hay pose de sentado. |
| N-06 | Corregido | 8f2c5dd | `Npc.MirarA` gira al personaje entero. Además, `Cuerpo` llegaba vacío en el Caso 1 (falta `node_paths` en las escenas), y ahora tiene un respaldo. |
| N-07 | Pendiente de CA-02 | — | Todavía no hay reacciones al avance. |
| N-08 | Pendiente de CA-02 | — | Hecdog sigue sin cuerpo en el Campamento y el Lago. |
| N-10 | **No corregido: Mimita en pose T** | — | Las capturas confirman que Eriz y Juan Carlos bajan los brazos. **Mimita (`miami.glb`) no tiene esqueleto** y su malla viene en pose T, con los brazos abiertos (`campamento/grupo_eriz_mimita_miawrio.png`). Por código no se le pueden bajar. Hace falta un modelo de Mimita con esqueleto o ya en reposo. |

Pruebas: `Herramientas/Caso1/probar_ca03.gd` (0 fallos), `probar_escenarios.gd` (0 fallos) y `tests/cinematica_cinta_smoke.gd` (0 fallos).
Rendimiento con los 6 NPC del Campamento en pantalla: CPU de 2.0 a 2.7 ms y GPU de 4.8 a 5.6 ms por cuadro (`campamento/rendimiento.txt`), muy por debajo de los 16.6 ms que pide 60 FPS. La herramienta de capturas marca unos 30 FPS porque la ventana estaba en segundo plano.

## Prioridad que propongo

1. **N-01** (Porky vivo), **N-04** (pies que patinan) y **C-02** (el expediente tres veces).
2. **C-01** (registro central de cinemáticas en la partida) y **C-04**. Después, **C-05** (Volver a ver), sobre ese registro.
3. **N-02**, **N-03** y **N-06** (variantes de reposo y voltearse al hablar).
4. **C-03**, **C-10**, **N-05** y **N-10** (detalles y verificaciones con captura).
5. **C-06** necesita una decisión de historia de Takeshi. **N-07** y **N-08** dependen de CA-02.

## Cierre de CA-03 (3 de noviembre de 2026)

Rama: `HU-9.5-Animaciones-Tutorial`, al día con `main` (7db7c33, con Pistas-y-flujo-Caso-1). El merge no hizo falta: la rama ya contenía `main`, sin conflictos.

### Tabla final

| ID | Estado | Qué quedó |
|---|---|---|
| C-01 | Corregido | Registro de cinemáticas en cada ranura. La prueba confirma que viaja al guardar y cargar. |
| C-02 | Corregido | Después de IntroCaso, Hecdog no vuelve a abrir el expediente. |
| C-03 | Corregido | El aviso del expediente ya no sale encima de Hecdog. |
| C-04 | Corregido | Una sola IntroCaso, también en su fundido final. |
| C-05 | Corregido | Botón «Volver a ver la cinta» en el Caso 0 del tablero de capítulos. Solo sale si la partida guardada más reciente (la que abre «Continuar») ya la vio. `VerCinta` carga una Sala 1 aparte, en su propio mundo y encima del tablero, con el Caso 0 sin arrancar. No cambia la partida y regresa al mismo detalle. |
| C-06 | Verificado | La estación pide 5 de las 9 pistas del Lago, también con el código de Pistas-y-flujo-Caso-1. Caneloso dice por qué todavía no. |
| C-07 | Verificado | Al cargar sale el «Anteriormente…» y no IntroCaso. |
| C-08 | Verificado | Campamento ↔ Lago ↔ sala no repite la llegada de Hecdog ni la presentación. |
| C-09 | Corregido | Al usar el registro, la cinta sale una vez. Tampoco vuelve al cargar después del cierre. |
| C-10 | Verificado | Al volver a la sala queda el objetivo; no se repite el pensamiento. |
| C-11 | Verificado | El pensamiento al llegar al Lago sale una vez. |
| C-12 | Corregido (nuevo) | Tres versiones de la cinta con un interruptor. Queda solo la película, elegida por Takeshi. El código, los `.glb` y el interruptor de las otras dos ya no están, así que ningún camino del juego, guardado ni prueba las puede reproducir. |
| N-01 | Corregido | Porky no está en el Campamento. |
| N-02 | Corregido | Variantes de reposo por código. |
| N-03 | Corregido | Tony, Aya y Miawrio intercalan su reposo «tenso», sin ir al mismo cuadro ni a la misma velocidad. |
| N-04 | Corregido | Nadie se desliza: Eriz y Juan Carlos caminan por código y Mimita tiene su propia animación de caminar. |
| N-05 | Verificado | De pie junto a la silla, sin atravesarla (`revision_final/sala_*_lado.jpg`). |
| N-06 | Corregido | Al hablar se voltean completos. |
| N-07 | No aplica a CA-03 | Sin reacciones al avance del caso, así que nada se repite. Es de CA-02 (ver el reporte de abajo). |
| N-08 | No aplica a CA-03 | Hecdog sin cuerpo en el Campamento y el Lago. Es de CA-02 (ver el reporte de abajo). |
| N-09 | Verificado | Sin actividades ni rutas, no hay bucles visibles. |
| N-10 | Corregido | Mimita tiene esqueleto y los brazos abajo en la malla: `mimita_rig.glb` (`Assets/Models/Personajes/Blender/riggear_mimita.py`), con `Reposo`, `ReposoMirar`, `ReposoPeso` y `Caminar`. Opción A, hecha en Blender sin Mixamo. La malla y las texturas son las mismas. Antes y después en `revision_final/mimita/`. |
| N-11 | Verificado (nuevo) | Ninguna línea de un implicado se repite entre las dos primeras conversaciones. **A partir de la tercera sí**: «Ahorita regreso…» y su réplica salen iguales, porque el guion solo tiene esa variante. Pasa si el jugador vuelve sin pruebas nuevas, así que tiene contexto. No cambié líneas del guion. |

### Conteo del recorrido (`Herramientas/Caso1/probar_ca03.gd`)

| Qué se cuenta | Resultado |
|---|---|
| Cinta del Caso 0 (`caso0_cinta`) | **1** en la partida, 0 al cargar después del cierre. Repetir el capítulo es una partida aparte y se puede saltar. |
| IntroCaso del Caso 1 (`intro_caso01`) | **1**; 0 al cargar y al volver a verla |
| Escenas automáticas | Campamento **1**, Lago **1**, sala **1** |
| Pensamientos | 45 distintos, 51 en total. Salen más de una vez solo «Ya lo examiné…», «Está demasiado oscuro…» y la descripción de la canoa, porque el jugador volvió a mirar |
| Reacciones | **0**: en el Caso 1 todavía no hay (N-07) |
| Líneas de diálogo | 59 distintas, 91 en total. Se repiten solo a partir de la tercera conversación (N-11) |
| Volver a ver la cinta | Una a la vez, también en el fundido final. La partida guardada queda igual, byte por byte, y no cuenta en el registro |
| Fallos | **0** |

Otras pruebas con esta rama: `probar_escenarios.gd` 0 fallos y `tests/cinematica_cinta_smoke.gd` 33 revisiones sin fallos. El Caso 0 de principio a fin se probó **desde su resolución** (`FlujoRealCaso0Prueba`) y la acusación del Caso 1 con las confesiones puestas por la prueba (`FlujoCaso1Prueba`), con Esc o la última opción como respuesta. Guardar y cargar se probó en el cierre del Caso 0, en el Campamento y en el Lago (`probar_escenarios.gd`), no en cada fase de los interrogatorios. Lo último del recorrido se volvió a correr aparte después de corregir la entrada de «Volver a ver»; el recorrido entero es anterior a esa corrección.

**Para el guion:** el título de la pista del silbato dice «T. S.» y el guion dice «G. S.» (`caso1_pistas.json`). No lo cambié porque es de la historia.

`«Ya lo examiné…»`, `«Está demasiado oscuro…»` y la descripción de los objetos que no son evidencia responden a que el jugador volvió a mirar algo, así que no cuentan como repetición. Tampoco cuenta «¿Qué le enseño?», la pregunta del menú de pruebas, que sale cada vez que se abre.

### Capítulo 1 en específico

| Punto | Resultado |
|---|---|
| IntroCaso del Caso 1 | Sale una vez al empezar. Al cargar no sale. Se puede volver a ver sin cambiar el objetivo ni las notas. |
| Llegada al campamento con Hecdog | Una vez; el expediente no se presenta otra vez. |
| Presentación de los seis | Una vez; al volver a la sala no se repite. |
| Pensamiento al llegar al Lago | Una vez. |
| Acceso a la estación | Sin las pruebas, Caneloso explica por qué todavía no y no se va. |
| Interrogatorios | Ver N-11. Las preguntas ya hechas se marcan con ✓ (`OpcionVista.Leida`). Las voces no están integradas (ver las ramas de abajo). |
| NPC | Variantes de reposo, sin clones ni pies patinando, sin pose T (Mimita incluida), y se voltean al hablar. |
| Cinemáticas propias del capítulo | **No existe ninguna** además de IntroCaso: ni libro quemado, ni foto del grupo, ni llegada al lago, ni silbato, ni reconstrucción de las 23:20, ni finales A y B (los finales son diálogo con un aviso en pantalla). **Queda pendiente de CA-01.** No las creé. |

### Ramas sin integrar

| Rama | ¿Afecta CA-03? |
|---|---|
| `dialogos-caso1` | Ya está integrada: 0 commits por delante. |
| `voces-personajes` (45ae6bc) | No repite nada por acercarse: las sílabas solo suenan mientras se escribe una línea, y no hay comentarios por cercanía. Ninguna sílaba sale dos veces seguidas y el tono varía ±6 %. Al integrarla habrá un conflicto en `CREDITOS_AUDIO.md` y otros dos en `.uid`, de arreglo fácil. |
| `HU-10---Sistema-de-ayudas` (dfe1a39) | Mejora CA-03: quita las ayudas que salían solas y las deja a petición. `PedirPista` no da nada durante la cinta ni en diálogos, y la misma pista no se agrega dos veces. **Cuidado al integrarla:** en `Caso1.cs` vuelve a escribir a mano las notas de los detalles que hoy vienen del guion, y borra la tecla de ayuda `[H]`. Conflictos en tres `.uid`. |

### Revisión visual

Capturas a 1920×1080 en `HU-9.5_CA03_capturas/revision_final/`: cada NPC del Campamento y de la sala, de frente y de lado, y Mimita antes y después. La cinta está en `cinta_hoja_pelicula.jpg`, con las dos versiones retiradas para comparar. IntroCaso está en `intro_caso01_*.jpg`. Sin poses T ni personajes atravesando muebles.

Rendimiento con todos los NPC del Campamento: de 0.5 a 1.4 ms de CPU y de 4.5 a 5.7 ms de GPU por cuadro (`rendimiento_campamento.txt`), dentro de los 16.6 ms que pide 60 FPS. La herramienta marca 30 FPS porque la ventana estaba en segundo plano.

**Lo que no cubre la prueba:** corre sin ventana, así que no ve poses ni cortes; eso se revisó con las capturas y el video. El `NullReference` en `GestorPartida.PrepararMiniatura` solo sale sin ventana, porque no hay textura del viewport.

### Reporte de CA-02 (no hecho, espera tu aprobación)

| ID | Qué es | Trabajo estimado |
|---|---|---|
| N-07 | Conectar el `animo` que ya traen las líneas de los `.json` con `Npc.FijarAnimo`, guardarlo en la partida y probarlo. Tony, Aya y Miawrio solo tienen su reposo «tenso»; los demás solo cambiarían la respiración si no se les hace más animación. | Medio día a un día |
| N-08 | Poner a Hecdog junto a la patrulla en el Campamento y el Lago, con su reposo. No cambia líneas. | 2 a 4 horas |

Total: de 1 a 1.5 días, sin contar rutinas de NPC con movimiento, que tampoco existen y serían otra tarea de varios días. Si se valida con esa exigencia, CA-02 no está completo.
