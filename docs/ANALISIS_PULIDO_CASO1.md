# Análisis del pulido del Caso 1

Fecha: 3 de noviembre de 2026. Base revisada: `HU-9.5-Animaciones-Tutorial` en `c08b780`.
No se cambió ningún archivo del juego. Este documento espera tu aprobación antes de programar.

## 1. Lo que necesito que decidas

Son seis decisiones. Las dos primeras cambian más trabajo. Cada una trae mi recomendación, y si me dices «como recomiendas», sigo con todas.

| # | Decisión | Recomiendo | Por qué |
|---|---|---|---|
| D1 | El árbol de Tony ya es un careo de cinco excusas (`tony_1` a `tony_5`). Si se exige `tony_5` para armar el caso **y** después hay careo, el jugador tumba las mismas cinco excusas dos veces. | **Jugarlas una sola vez, en el careo.** Antes del caso, Tony solo da su primera versión. `tony_5` sigue siendo obligatorio, pero para llegar al Final A. | Conserva el careo que pediste, sin repetir texto y sin reescribir diálogos. |
| D2 | PREGUNTAR y PRESIONAR no tienen líneas. En los interrogatorios hay 41 opciones MOSTRAR, 1 CONTAR, 0 PREGUNTAR y 0 PRESIONAR. | **Líneas cortas de sistema ahora** (sección 6.2), y tus compañeros las cambian cuando quieran. | No frena el trabajo y no toca sus diálogos. |
| D3 | El silbato dice «T. S.» en la historia v2, en tu tabla y en el modelo, pero «G. S.» (Guillermo Spínola) en el guion: `caso1_pistas.json` y `caso1_tony.json`. | **Que lo decida quien escribió el guion.** Mientras tanto no toco textos. | Tú pediste que no cambien la historia ni los textos, y aquí se contradicen. |
| D4 | La acusación solo tiene líneas para Juan Carlos, Eriz, Miawrio y Tony. La historia tampoco contempla acusar a Aya o a Mimita. | **Mostrar a los seis y añadir dos líneas** para Aya y Mimita, con el borrador de la sección 7.4 revisado por tus compañeros. | Pediste los seis visibles, y sin esas líneas el Final B se queda sin entrada. |
| D5 | ¿Qué pasa con «Acusar de todos modos» si el acusado es Tony y el caso está incompleto? El Final B cuenta que Tony sigue libre, así que no sirve. | **No ofrecerlo contra Tony.** Solo sale «Todavía no me convences» y «Seguir investigando». | Evita un final que contradice lo que acaba de pasar. |
| D6 | Nadie tiene animación de sentarse. Juan Carlos, Eriz y Mimita tampoco tienen Tenso ni Alterado. | **Hacerlas en Blender:** pose sentada para los seis, y Tenso y Alterado para esos tres. | Sin esto no hay «se acomoda en la silla» ni lectura de la cara en tres implicados. Es la parte más pesada del trabajo. |

También necesito tu visto bueno para **reemplazar la reconstrucción de las 23:20 que hice hoy en Godot** (sección 3). Es lo que pides, pero descarta trabajo de `e996d67`.

Dos errores del guion para tus compañeros:

- El Final B dice que «Miawrio y Tony se dan la mano» aunque el acusado sea Miawrio.
- Hecdog dice «Me **lo** llevo a la patrulla», que no sirve para Aya ni para Mimita.

## 2. Estado del proyecto (sección 00 de tu mensaje)

| Paso | Resultado |
|---|---|
| `git status` | No hay trabajo sin guardar. Hay 34 `.import` modificados y 10 archivos sin seguimiento; no los toqué y no entran en commits. |
| Fetch y rama | Hice `git fetch --all --prune`. La rama está 21 commits por delante de `origin` y ninguno por detrás, así que el pull no trae nada. |
| Merge de `origin/main` | No hace falta: la rama va 37 commits por delante y 0 por detrás de `origin/main`. El commit (1) que pediste no existirá. |
| `dotnet build` | 0 errores y 0 advertencias. |
| `probar_escenarios.gd` | 0 fallos, pero con el error de la sección 8.1. |
| Push | No se subió nada. Espero que me lo confirmes en este hilo. |

**`voces-personajes` ya está integrada en local** (`1bec891`), desde hoy y a tu pedido, igual que HU-10. Tu paso 4 pedía no integrarla sin aprobación, así que te digo qué cambia:

- Cada personaje suena con su clip de `Assets/Audio/Voces/<nombre>.wav` mientras se escribe el texto. Quien no tiene clip conserva los blips: Caneloso y la narración.
- `VentanaDialogo.cs` gana un reproductor, un bus de audio «Voces» y la búsqueda del clip. La interfaz, `Elegir` y `ElegirEvidencia` no cambian.

**Recomiendo dejarla.** No choca con el interrogatorio, porque el POV no necesita tocar ese audio. La versión de `origin` solo difiere en una línea de ese archivo, sin efecto en esto.

## 3. La escena de Tony y Porky hoy

- **Dónde está:** `LibretosCaso1.Reconstruccion` (`Escenas/Cinematicas/LibretosCaso1.cs:209`), con id `caso1_2320`. Se filma en Godot con el motor de `CinematicaCaso1`. No usa Blender.
- **Cómo se dispara:** solo en el Final A, desde `Motor.AntesDeNodo` al llegar a `final_a` (`GuionCaso1.cs:174`), antes de la confesión. No sale en ningún otro momento.
- **Qué tiene de distinto a lo que pides:** los personajes se mueven, Tony se ve desde el plano de las 23:12 y no hay silueta, inserto de la linterna ni inserto del cordón.
- **Qué se aprovecha:** el id `caso1_2320` y su registro en CA-03, la luz de algas (`Brillo`) y dos sonidos (`silbido_corto.wav`, `chapoteo.wav`). Faltan el golpe sordo, los pasos sobre madera y el clic de la linterna.

## 4. Problemas de movimiento encontrados

Las velocidades son una primera medición de los pies apoyados en la animación. Las confirmo con una prueba antes de ajustar.

| Quién | Caminata | Pies | Velocidad usada hoy |
|---|---|---|---|
| Aya | `Caminaya` | 0.34 m/s | 1.1 m/s |
| Miawrio | `global/Caminar` | 0.48 m/s | 1.1 m/s |
| Tony | `global/CaminaTony` | 0.12 m/s (me parece baja; se revisa) | 1.1 m/s |
| Mimita | `Caminar` | sin hueso de pie, se mide de otra forma | 1.1 m/s |
| Juan Carlos y Eriz | por código | depende del paso por código | 1.1 m/s |
| Hecdog | no tiene | no camina | no aplica |

| # | Problema | Dónde |
|---|---|---|
| M1 | Todos avanzan a 1.1 m/s, mucho más rápido que sus pies: patinan. | `SalaInterrogatorioCaso1.cs:29` |
| M2 | Giros instantáneos al salir y al llegar. | `SalaInterrogatorioCaso1.cs:117` y `:150` |
| M3 | Sin mezcla entre animaciones, y cada cambio salta a un punto al azar. | `AnimacionPersonaje.cs:102-103` |
| M4 | La ruta recta de la puerta a la silla **atraviesa la esquina de la mesa y la silla** (cálculo con las medidas de la escena). | `TraerImplicado` |
| M5 | Nadie se sienta: se quedan de pie detrás de la silla. Juan Carlos, por su desplazamiento de modelo, queda metido en ella. | `PuntoSilla` en z = -3.45, silla en z = -2.8 |
| M6 | El implicado no sale al terminar: se queda de pie hasta que entra el siguiente. | `GuionCaso1.Interrogar` |
| M7 | Caneloso arranca a toda velocidad en un cuadro y frena en otro: no se desliza, pero es brusco. | `Jugador.cs:673` y `:679` |

**Correcciones:** velocidad propia por personaje, medida de su animación; giros interpolados; mezcla de 0.25 s; ruta con puntos alrededor de la mesa; sentarse y levantarse; salida al terminar; aceleración de Caneloso en unos 0.15 s y frenado en 0.2 s, sin deslizar.

## 5. Pistas

**Todas se pueden tomar cuando el jugador está bien colocado.** La prueba actual apunta con la retícula y pulsa E:

| Parte | Pistas | Se toman | Fallos |
|---|---|---|---|
| Caso 0 | 8 | 8 | 0 |
| Campamento | 13 | 13 | 0 |
| Lago | 9 | 9 | 0 |

Revisé también los archivos de escena: no hay ids duplicados y todas las pistas del guion existen.

**Pero esa prueba no cubre todo lo que pides.** Coloca al jugador a mano, así que no demuestra que el punto sea alcanzable.

| # | Problema | Corrección |
|---|---|---|
| P1 | Cinco pistas exigen agacharse y, de pie, **no responden nada**: `oferta_compra`, `libro_guardia_quemado`, `remo_sangre`, `huellas_pezuna` y `huellas_detenidas`. El rayo las ignora (`Jugador.cs:274`). | Al apuntarlas de pie, «Tendría que agacharme [Ctrl]». |
| P2 | Ninguna escena tiene `NavigationRegion3D`, que era tu forma de saber si un punto es alcanzable. | `probar_pistas.gd` hornea la navegación por zona solo durante la prueba. Si consume demasiada memoria, recorre el piso en cuadrícula desde la entrada. Te avisaría del cambio. |
| P3 | Las pruebas de la mesa de la sala se ven y no hacen nada, aunque no se resaltan. | En POV se seleccionan desde el expediente. |

La linterna («Está demasiado oscuro…») y el candado («Está cerrado con…») ya explican por qué no se puede.

## 6. Interrogatorio

### 6.1 Cómo funciona hoy

- El implicado camina hasta la silla y la conversación empieza. La cámara es la del jugador, que se bloquea donde estaba.
- No hay plano de entrada, transición ni cámara sentada. Hecdog está en la puerta, a espaldas de la silla del detective.
- Cada paso muestra tres pruebas fijas: la correcta y dos falsas. Si falta una, sale «???». Eso delata la respuesta y no abre el expediente.
- `ElegirEvidencia` existe, pero solo la usa el Caso 0, y sus tarjetas no caben con unas 30 pistas.
- El ánimo de cada línea ya está en el guion, pero solo cambia el color del texto y el temblor del retrato.
- Los testimonios (`foto_aya_2312`, `jc_vio_tony_sendero`, `mimita_oyo_silbido`, `miawrio_oyo_cita`) son conclusiones, no pistas. El expediente no los puede ofrecer todavía.
- Los mismos árboles también corren en el campamento, al hablar con cada personaje (`Caso1.cs:322`).

### 6.2 Plan

**Entrada y salida.** Plano general de 4 a 6 s que se puede saltar, y de 1 a 2 s las siguientes veces, registrado para CA-03. Al terminar se levanta y sale en tercera persona.

**POV.** Transición de 1 a 1.5 s a la silla del detective, con respiración de cámara y DOF. Hecdog pasa a un punto nuevo al lado de la mesa durante el corte, porque en la puerta quedaría detrás de la cámara. Las pruebas pasan de la mesa auxiliar del fondo a una versión reducida en la mesa principal. Encuadro al implicado arriba, porque la caja de diálogo cubre el 28 % inferior de la pantalla, más si hay muchas opciones.

**Tres acciones, sin cambiar los textos ni los JSON:**

- **PRESENTAR PRUEBA:** abre un selector con todas las pistas y testimonios. Si la prueba elegida tiene opción en ese paso, sigue su ruta actual. Si no, va al nodo `error`, que ya existe. El acierto suma golpe de sonido, push-in y Alterado.
- **PREGUNTAR:** repite su versión actual, con líneas que ya existen.
- **PRESIONAR:** si el jugador tiene la prueba correcta, el implicado reacciona y Hecdog le da pie a presentarla. Si no la tiene, baja la credibilidad.

Líneas de sistema propuestas, todas cambiables:

- Caneloso: «Eso no me cuadra.»
- Hecdog, con prueba: «Pues enséñasela, Nya.»
- Hecdog, sin prueba: «Sin pruebas no, Caneloso, Nya.»
- Hecdog, al cortar: «Ya basta por ahora, Nya.» (la tuya)

**Lectura de la cara.** El motor avisa del ánimo de cada línea y se traduce con las mismas familias de palabras que ya usa `ColorDeAnimo`: sereno a reposo, nervioso a Tenso y alterado a Alterado. En POV solo se ven cabeza y torso, así que el cuerpo de arriba anima sobre la pose sentada, con una máscara de huesos.

**Credibilidad.** Hoy nunca se recupera, así que al llegar a 0 cada error volvería a cortar el interrogatorio de inmediato. Propongo que Hecdog corte, que el implicado salga y que la credibilidad vuelva a 2 al corte, con +1 por cada prueba correcta. El máximo sigue en 5.

**En el campamento** queda la conversación como hoy, pero con el selector y sin acusación forzada. La cámara POV solo existe en la sala.

## 7. Acusación

### 7.1 Cómo funciona hoy

Los dos problemas que describes existen:

- `GuionCaso1.Interrogar` lleva de «paciencia» a `Sala(..., "paciencia")`, cuyo nodo termina en acusar (`GuionCaso1.cs:143-145`).
- Tony tiene `si_no_cumple: ocultar` en `caso1_acusacion.json`, así que sin requisitos solo se puede acusar a inocentes.

La advertencia de `caso1_sala.json` («ya no hay vuelta atrás») es cierta hoy: no se puede salir una vez dentro.

### 7.2 Las tres casillas contra lo que hay en el juego

| Casilla | Pruebas | Tipo | Id |
|---|---|---|---|
| Oportunidad | Cuaderno de Eriz | pista | `cuaderno_eriz` |
| | Tony sin coartada | testimonio | `jc_vio_tony_sendero` o `foto_aya_2312` |
| | Botas embarradas | pista | `botas_tony` |
| Medio | Remo con sangre | pista | `remo_sangre` |
| | Hueco en el cobertizo | pista | `hueco_remo` (`soporte_remos` en el guion) |
| Motivo | Fragmentos del libro | pista | `libro_guardia_quemado` |
| | Nota de Porky | pista | `nota_porky` |
| | Recorte | pista | `recorte_periodico` |

Todas son alcanzables hoy.

### 7.3 Plan

- **Armar el caso:** pantalla nueva con el estilo del expediente. La revisión es completa, como en Obra Dinn: nada se valida hasta presentar. Cada casilla da sello verde o rojo con una línea de Hecdog.
- **Baja de credibilidad:** -1 por revisión, no por casilla. Si no, un caso vacío costaría 3 de golpe.
- **Salida:** «Seguir investigando» siempre disponible, sin penalización si no se presentó.
- **Careo:** es el árbol actual de Tony. Tu tabla ya coincide con su lógica, incluidas las pruebas combinadas. Lo que falta es la puesta en escena. Las pruebas equivocadas de hoy solo tienen una línea de Caneloso y el regaño de Hecdog, así que para que **Tony se burle** hace falta una línea corta de sistema, salvo donde ya existe.
- **Golpe en la mesa:** clip nuevo en Blender.
- **Si la credibilidad llega a 0 en el careo:** Hecdog lo detiene y se conserva lo tumbado. Al volver sigue en la excusa pendiente (`regreso_N` ya existe).

**Orden del final.** Tus secciones 1 y 5.4 lo dicen un poco distinto. Entiendo este:

1. Caneloso dice oportunidad, medio y motivo (`a_tony`).
2. Careo.
3. Escena de las 23:20 con la silueta.
4. Confesión.
5. Plano final de la silueta revelada como Tony.
6. Final A.

### 7.4 Cambios de estructura que tus compañeros deben ver

Ningún texto existente cambia.

| Archivo | Cambio |
|---|---|
| `caso1_sala.json` | La opción `acusar` pasa a decir «Armar el caso». Sale del diálogo con `armar` en lugar de preguntar «¿Acuso ya?». El nodo `paciencia` deja de usarse. |
| `caso1_acusacion.json` | `pregunta` y `elegir` dejan de usarse, porque los reemplaza la pantalla. Se entra directo a `a_<sospechoso>`. Se añaden `a_aya` y `a_mimita` si apruebas D4. |
| `caso1_tony.json` | Sin cambios en el archivo. Con D1, antes del caso el motor se detiene en `hub_1`. |
| `GuionCaso1.cs` | `Interrogar` ya no fuerza acusar. Aparecen `ArmarCaso` y `Careo`. El careo corre desde `AntesDeNodo`, antes de `final_a`. El objetivo «Habla con Tony y luego acusa» cambia, porque ya no es el orden. |

**Borrador para D4, para que lo corrijan:**

- Aya: «Aya. Tenía la cámara, estaba en el bosque y no dijo nada de lo que vio.»
- Mimita: «Mimita. Tachó la hora en su registro y mintió sobre dónde estuvo.»

## 8. Plan de trabajo

### 8.1 Error nuevo que corrijo de paso

`probar_escenarios.gd` pasa, pero el registro trae 310 `ObjectDisposedException`, en `CinematicaCaso1.Actualizar` (`CinematicaCaso1.cs:271`). La cinemática sigue corriendo después de que su cámara se libera, y ocurre cuando cambia la escena durante una cinemática. Es de mi trabajo de hoy. Sospecho que también puede pasar al cargar partida a media cinemática; lo compruebo al corregirlo.

### 8.2 Escena de las 23:20

- **Cadena:** la misma de la cinta: `pelicula.py` y `construir_momentos.py` dan un `.glb` y un `.json`. El juego los muestra sobre el lago real, así que la luz y el agua coinciden con lo que el jugador ya vio.
- **Código:** `CinematicaCintaPelicula` hoy está atada al Caso 0: reloj, pluma, carretes y Caneloso. Saco a una clase común solo los cuadros, la cámara, las luces y los sonidos. Riesgo: tocar la cinta del Caso 0. Lo cubro con `probar_ca03.gd -- solo-ver` antes de seguir.
- **Poses:** solo Tony y Porky, con IK y reporte de choques. Porky sale de espaldas, lo que evita sus límites (cabeza grande, brazos de 0.36 m, sin cara).
- **Entrega:** hoja de contacto antes de integrarla, para que la veas pronto.

### 8.3 Orden de los commits

| # | Commit | Contenido |
|---|---|---|
| 1 | Merge de main | **No hace falta**, ya está integrado. |
| 2 | Escena de Tony y Porky | Sección 8.2. |
| 3 | Movimientos | Sección 4 y el error de 8.1. |
| 4 | Pistas | `probar_pistas.gd`, reporte antes y después, y aviso de agacharse. |
| 5 | Interrogatorio en POV | Sección 6.2. Las poses de D6 van aquí. |
| 6 | Armar el caso | Sección 7.3, con su guardado. |
| 7 | Careo final | Sección 7.3 y el orden del final. |
| 8 | Pruebas y entrega | Pruebas y videos. |

### 8.4 Guardado

Hoy no se puede guardar durante un diálogo (`PuedeGuardar`), y tu prueba pide guardar y cargar en medio. Propongo permitirlo solo cuando el juego espera al jugador: menú de opciones, pantalla de armar el caso y turno del careo. Al cargar, el jugador vuelve a la sala con su progreso: casillas llenas, excusas tumbadas y credibilidad. La conversación se retoma en ese punto, no a mitad de línea. Las partidas guardadas de antes siguen cargando.

### 8.5 Pruebas y límites de tu PC

- Corro una prueba a la vez, por partes (`solo-sala`, `solo-final-a`). `probar_ca03.gd` completa tarda unos 20 minutos y corre al final. Te mando avances cortos mientras tanto.
- Las pruebas nuevas cubren tu lista de la sección 6: paciencia agotada, caso vacío, caso incompleto con cada salida, caso completo con un error en el careo, y guardar y cargar en los tres momentos.
- Los 60 FPS los mido en la sala con DOF activo, con ventana. Si el DOF no los alcanza, lo bajo y te lo digo.

### 8.6 Riesgos

| Riesgo | Qué hago |
|---|---|
| Las poses sentadas no se ven bien en modelos chibi. | Te muestro capturas de una antes de hacer las seis. |
| Animar el torso sobre la pose sentada puede fallar en Juan Carlos, Eriz y Mimita, cuyos esqueletos son distintos. | Para ellos, Tenso y Alterado se pueden hacer por código, igual que su reposo actual. |
| Tocar `CinematicaCinta` puede afectar al Caso 0. | Separo poco código y repito la prueba de la cinta. |
