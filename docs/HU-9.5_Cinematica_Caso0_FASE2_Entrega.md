# FASE 2 — Cinemática del Caso 0 «La cinta de las 3:12»: entrega

- **Rama:** `HU-9.5-Animaciones-Tutorial` (commits locales, sin push)
- **Fecha:** 21 de octubre de 2026
- **Referencias:** `Revision_CinematicaCinta_Caso0.md` (revisión de la FASE 1), `Caso0_Historia_Expediente.pdf` (historia completa), `docs/HU-20_Caso0_Historia_Dialogos.md`

## 00. Antes de empezar

| Punto | Resultado |
|---|---|
| `git status` | Los 17 `.import` modificados y los archivos sin seguimiento siguen igual: no se tocaron ni entraron en los commits |
| `git fetch --all --prune` / `git pull` | La rama ya estaba al día con `origin/HU-9.5-Animaciones-Tutorial`; `main` no tiene nada nuevo |
| Ramas de compañeros | `HU-10---Sistema-de-ayudas` (Mario, 20 oct) cambia `Caso0.cs`, `Caso0.Guia.cs`, `Caso0.Partida.cs` y `Jugador.cs`. `HU-19-HU-27` (19 oct) cambia `VentanaDialogo.cs`. Ninguna está en esta rama |
| Ayudas encima de la cinemática | `CinematicaCinta.EnCurso` bloquea `Jugador.MostrarIndicacion`, `MostrarPensamiento` y `MostrarAviso`, y `Caso0._Process` no corre la guía ni las ayudas mientras dura. Al mezclar HU-10, su `SistemaAyudas` solo muestra texto por `Jugador.MostrarPensamiento`, así que queda bloqueado igual |
| `dotnet build` | 0 errores antes y después |

**Archivo de la revisión.** `docs/REVISION_CINEMATICA_CASO0.md` no existe en el repositorio; la revisión de la FASE 1 estaba en `Descargas/Revision_CinematicaCinta_Caso0.md` y es la que se usó.

## 1. Archivos creados o modificados

| Archivo | Qué cambió |
|---|---|
| `game/Escenas/Cinematicas/CinematicaCinta.cs` | Reescrita: 16 tomas en 55.6 s, figura con capucha y gabardina que entra y sale por la rejilla, el reloj descolgado a propósito con caída y rebote, Porky lo cuelga, REC, STOP, rebobinado, expulsión, puerta y botas; bus de audio de cinta; estados de audio que dependen de `t`; `EnCurso` |
| `game/Escenas/Cinematicas/recuerdo.gdshader` | El rojo intenso y brillante (REC, contador) conserva su color |
| `game/Escenas/Interrogatorio/Jugador.cs` | No muestra pensamientos ni avisos mientras corre la cinemática |
| `game/Escenas/Caso0/Caso0.cs` | `_Process` se detiene durante la cinemática; la segunda línea de la cinta dice «[Ronquidos. Ruido metálico en el techo]» (antes decía «junto al micrófono») |
| `game/Assets/Audio/Cinematica/Cinta/*.wav` (13, con sus `.import`) | Sonidos CC0 de BigSoundBank |
| `game/Assets/Audio/CREDITOS_AUDIO.md`, `game/Assets/Caso1/CREDITOS_ASSETS.md` | Créditos |
| `game/tests/CinematicaCintaPrueba.cs`, `game/tests/cinematica_cinta_smoke.gd` | Línea nueva y 10 revisiones más |
| `game/tests/FlujoRealCaso0Prueba.cs`, `game/Herramientas/flujo_real_cinta.gd` | Prueba y grabación desde el flujo real del caso |
| `game/Herramientas/capturas_cinta.gd`, `game/Herramientas/medir_audio_cinta.gd` | Capturas con `Manual` + `IrA` y medición de audio por bus |
| `docs/HU-9.5_Cinematica_Caso0_FASE2_Entrega.md` y `docs/HU-9.5_capturas_cinta/` | Esta entrega y las hojas de capturas ANTES/DESPUÉS |

## 2. Shot list final

Duración: **55.6 s** (objetivo 48–55). Todo depende de `t`, así que estos tiempos son exactos en cualquier corrida.

| # | t | Plano | Qué se ve y se oye | Cambio respecto a la propuesta y por qué |
|---|---|---|---|---|
| 0 | 0–4.2 | Negro | Clic de PLAY (0.25), siseo de cinta, subtítulo de Tito (0.7–4.3); «SALA 1 · 03:09» se escribe a las 3.6 | Igual. No hay voz de Tito (ver pendientes) |
| 1 | 4.2–8.4 | Inserto de la grabadora a ras de mesa, DOF | REC parpadeando en rojo, contador avanzando; Porky dormido, desenfocado al fondo | Igual. La cámara a 1.36 m deja a Porky como masa oscura borrosa |
| 2 | 8.4–13.2 | General picado desde la esquina de la puerta | Porky ronca. 12.0: ruido metálico + subtítulo + «03:12»; la rejilla se abre fuera de cuadro | La cámara pasó de la esquina de la rejilla a la de la puerta: la rejilla queda fuera de cuadro y el sonido llega antes que la imagen |
| 3 | 13.2–17.6 | Contrapicado bajo la rejilla | La figura baja colgada de las manos, recortada contra el ducto iluminado; se suelta (15.4) y cae (15.8) | +0.4 s para la caída |
| 4 | 17.6–23.2 | Medio desde el frente de la sala (mismo lado que la toma 2) | La figura camina a la mesa, se agacha junto a la grabadora y acerca la cabeza al micrófono; susurro + subtítulo (20.1) | Respeta el eje de la toma 2 (las dos desde +Z) |
| 5 | 23.2–26.9 | Medio lateral desde la izquierda | La figura va a la pared, levanta el reloj del clavo (25.35), lo separa y lo suelta (25.8); cae 0.65 s girando, golpe (26.45) y rebote (26.75); el tic-tac se corta en el golpe | Caída total de 0.95 s con giro y rebote; el reloj cae lejos del cuerpo |
| 6 | 26.9–29.4 | Inserto cenital de la carátula en el piso | 3:12, cristal rajado. Solo siseo | +0.4 s para leer las manecillas |
| 7 | 29.4–32.2 | General desde el fondo derecho | La figura corre a la rejilla, salta (29.9) y sube (30.4–31.2); la rejilla se cierra (31.4) y queda la pluma negra | Se agregó el sonido de la rejilla al cerrarse |
| 8 | 32.2–36.0 | Medio frontal | «03:15». Porky despierta (32.6), se levanta, mira alrededor y voltea al reloj | Igual |
| 9 | 36.0–41.2 | Medio lateral bajo, junto a la pared | Porky rodea la silla, se agacha (`Farm_Harvest`) y levanta el reloj (38.25), da un paso y lo cuelga con `Interact` (40.35) | +1.2 s: colgarlo tiene que verse completo |
| 10 | 41.2–42.4 | Inserto a ras de mesa (lo que ve Porky) | La luz roja de REC | Más corto (1.2 s): basta un parpadeo |
| 11 | 42.4–44.4 | Contraplano frontal | Porky se sobresalta (`Hit_Chest` lento + respiración agitada, 42.6) | Igual |
| 12 | 44.4–49.2 | Inserto de la grabadora desde el frente | STOP (44.7), rebobinado audible (45.1–47.6) con el contador regresando de 03:15 a 03:12, la cinta sube (48.3) y Porky se la lleva (48.75) | +0.8 s para el rebobinado completo. Porky queda fuera de cuadro y no tapa nada |
| 13 | 49.2–52.8 | General con la puerta de frente | Porky camina con la cinta y abre la puerta (51.9); entra la luz del pasillo | La cámara está en el lado contrario a la puerta (z = −1.4), ya no en su pared |
| 14 | 52.8–54.0 | Inserto bajo de las botas | Las piernas de Porky cruzan el umbral hacia el pasillo, contra la luz | Plano aparte en lugar de un inserto dentro del general |
| 15 | 54.0–55.6 | General | La puerta se cierra (54.3) y fundido a negro | Plano agregado para ver la puerta cerrarse |

**Decisión sobre REC.** La historia (PDF, sección 2) dice que a las 6:02 la grabadora «sigue en REC pero sin cinta», y en el juego su descripción dice que nadie la apagó. Por eso el STOP de Porky solo detiene la cinta: la luz de REC sigue parpadeando antes y después, y el contador se queda en 03:12. Así el estado final coincide con lo que encuentra Caneloso.

**Decisión sobre la luz.** La fuente es la lámpara del techo, encendida como en el juego, con un rebote cálido muy bajo para que la pared del reloj no quede en negro. Al abrirse la rejilla entra luz fría del ducto, que recorta a la figura; al abrirse la puerta entra la luz del pasillo. El foco del reloj se apaga durante el recuerdo (era lo que parecía una luna).

## 3. Capturas ANTES / DESPUÉS

- Hojas: `docs/HU-9.5_capturas_cinta/hoja_antes.jpg` (26 cuadros de la versión anterior) y `hoja_despues.jpg` (65 cuadros: inicio, mitad y final de cada toma y cada evento).
- Las capturas a 1920×1080 están en `Escritorio/Cinematica_Caso0_FASE2_capturas/` (`antes/` y `despues/`), hechas con `Manual` + `IrA` (`Herramientas/capturas_cinta.gd`).

## 4. Video

`Escritorio/Cinematica_Caso0_FASE2.avi`: Movie Maker, 1920×1080, 30 fps, 75.7 s, jugado desde el flujo real (`Herramientas/flujo_real_cinta.gd`): las tres preguntas, las frases de Porky y del comisario, la cinemática (empieza en 0:11.7), el pensamiento de la pluma y la pantalla de caso cerrado. Se grabó rejugando el capítulo y con respaldo en disco; las partidas y el progreso quedaron iguales (comparado con md5).

## 5. Audio antes / después

Medido en los dos videos (RMS cada 0.5 s y picos; tablas completas en `Escritorio/Cinematica_Caso0_FASE2_capturas/audio_rms_*.txt`). Gráfica: `docs/HU-9.5_capturas_cinta/audio_antes_despues.png`.

| Medida | Antes | Después |
|---|---|---|
| Base en t = 0–22 (RMS mínimo / mediana) | −45.4 / −35.7 dB | −37.1 / −32.2 dB |
| Pico más fuerte | −3.9 dB en t = 36.9 (pasos de Porky) | −3.5 dB en t = 26.45 (golpe del reloj) |
| Siguientes picos | −4.2, −5.6, −6.0, −6.2 (todos pasos) | −6.0 (rebobinado), −6.5 (rejilla), −6.8 (susurro), −6.9 (cinta) |
| Capa de +15 dB desde t ≈ 23 | Sí | No |

**La capa de +15 dB.** Medida por bus, estaba en `Efectos`, no en `Musica`: era `golpe_titulo.ogg`, que dura 20.6 s («Bad thing happen», un golpe musical con cola), y por eso parecía la música regresando. Pasaba igual en una partida normal porque es el mismo código. Ya no se usa: el golpe del reloj es madera con vidrio.

**Cómo suena ahora.** Todo lo que se oye va por el bus `Cinta` (paso alto, paso bajo, saturación y wow) sobre el siseo real de un casete. La música y el ambiente del juego bajan a −60 dB al entrar y regresan con un fundido de 2.5 s al terminar o al saltar. El tic-tac, el siseo, el zumbido y el ronquido dependen de `t`, así que adelantar con clic deja todo bien (la prueba lo revisa).

## 6. Hallazgos de la revisión de la FASE 1

| Hallazgo | Estado |
|---|---|
| La luz de REC es invisible | Corregido: el shader conserva el rojo intenso; inserto de REC en la toma 10 |
| Nunca se ve el reloj en 3:12 | Corregido: inserto cenital (toma 6); las manecillas avanzan de 3:09 a 3:12 y se paran con el golpe |
| Porky nunca cuelga el reloj | Corregido: lo recoge del piso y lo cuelga (toma 9); se quitó el corte de t = 31.5 |
| La caída no tiene causa visible | Corregido: la figura lo levanta del clavo y lo suelta |
| La ruta de la figura contradice la rejilla | Corregido: entra y sale por la rejilla y deja la pluma |
| La cinta no se escucha | Corregido en parte: siseo, PLAY, STOP, rebobinado, expulsión y susurro reales, con filtro de cinta. Falta la voz de Tito (pendiente) |
| Pocos planos; el susurro no se lee | Corregido: contrapicado, la figura agachada junto al micrófono e insertos de REC, carátula y grabadora |
| Salto de eje 2→3 y jump cut 5→6 | Corregido: cada corte cambia de lado o de tamaño de plano; las tomas 2 y 4 están del mismo lado |
| Cabeza de Porky en la toma 1 | Corregido: cámara a ras de mesa con desenfoque |
| Oreja (toma 4) y hocico (toma 6) tapando | Corregido |
| Puerta ilegible en la toma final | Corregido: cámara del lado contrario; la puerta se abre, entra luz y se cierra |
| Ritmo: despertar largo, REC corto | Corregido: despertar 3.8 s; REC, sobresalto y cinta 8 s |
| El reloj parece luna; mancha en la pared | Corregido: el foco del reloj se apaga en el recuerdo |
| «Mantén [Espacio]» siempre visible | Corregido: se desvanece a los 3 s y vuelve 3 s al tocar una tecla |
| Porky usa `PickUp_Table` en el piso y no toca el reloj | Corregido: `Farm_Harvest` y el reloj pegado a su mano |
| La figura parece maniquí; mano congelada | Corregido: capucha y gabardina, borde de luz; ya no hay `PickUp_Table` congelado |
| La caída dura 0.45 s y atraviesa la figura | Corregido: 0.95 s con giro y rebote, lejos del cuerpo |
| La cinta es un `BoxMesh` | Corregido: casete del modelo de Poly Haven |
| `Visible = false` a media toma | Corregido: la figura entra y sale por el techo |
| La figura negro sobre negro | Corregido en parte: se recorta contra el ducto y la pared; en la toma 4 sigue siendo una silueta oscura (a propósito, sin rostro) |
| Base baja, pasos lo más fuerte, golpe débil | Corregido (sección 5) |
| `error_suave.wav` y `cajon_2.wav` | Corregido: respiración agitada y expulsión de casete reales |
| Sin sonido de puerta | Corregido |
| Subtítulo 2 desfasado | Corregido: cada subtítulo empieza con su sonido |
| Bug: tic-tac al adelantar | Corregido, con prueba |
| Bug: balanceo con `Time.GetTicksMsec()` | Corregido: depende de `t` |
| Bug: el reloj vuelve a la pared por corte | Corregido |
| Bug: `Visible = false` en t = 23.6 | Corregido |
| `IntroCaso.cs` sin revisar | No corregido: queda fuera de esta tarea |

## 7. Checklist contra la historia del Caso 0

| Hecho | ¿Se ve? | Dónde |
|---|---|---|
| 1. La cinta: confesión de Tito como grabación | Sí: subtítulo «▶» sobre negro, con PLAY y siseo (sin voz) | Toma 0 |
| 2. 03:09, Porky duerme y ronca; la grabadora en REC | Sí | Tomas 1–2 |
| 3. 03:12, ruido en la rejilla; la figura baja y susurra al micrófono | Sí | Tomas 2–4 |
| 4. Descuelga el reloj a propósito, golpe, 3:12; se va por la rejilla; la pluma | Sí | Tomas 5–7 |
| 5. 03:15, Porky despierta, recoge el reloj y lo cuelga | Sí | Tomas 8–9 |
| 6. Ve REC, se asusta, STOP, rebobina a 03:12 y saca la cinta | Sí (REC sigue encendida, como la encuentra Caneloso) | Tomas 10–12 |
| 7. Sale con la cinta; sus botas hacia el pasillo | Sí | Tomas 13–15 |
| No se ve el rostro de la figura, ni a Eriz, ni quién es «La Garra» | Correcto | — |

## 8. Revisiones que faltaron en la FASE 1

**Subtítulos.** Los tres van en la franja negra de abajo, en ámbar y con «▶» (lo que viene de la cinta); las horas van arriba a la izquierda, a máquina y en el color papel del HUD. Ninguno pasa de 15 caracteres por segundo (Tito: 54 en 3.6 s; rejilla: 39 en 2.8 s; susurro: 31 en 2.7 s) y ninguno tapa la acción ni el indicador de saltar.

**Experiencia del jugador.** Llega justo después de explicar el caso: es la recompensa y responde la pregunta del inicio (qué pasó a las 3:12) sin cambiar la solución. Se puede adelantar de toma en toma con clic o Enter, o saltar manteniendo Espacio, Esc o B.

**Referentes.** Como en *Return of the Obra Dinn*, el sonido llega antes que la imagen (toma 2) y la escena se reconstruye sin explicar. Como en *Sherlock Holmes Chapter One*, el recuerdo se distingue por el color (desaturado salvo el rojo de REC). Como en *Detroit: Become Human*, los insertos de objetos marcan cada pista. Como en *Life is Strange*, el gancho queda en un objeto (la pluma) y no en un diálogo. Como en *Ace Attorney*, cada detalle que se ve es una prueba que el jugador ya tuvo en la mano (reloj, grabadora, huellas, pluma).

**Flujo real.** Probado y grabado: entra después de «Se acabaron mis trampas...» con el diálogo ya cerrado, no aparece nada encima, devuelve la cámara y el HUD, y el objetivo final es «Caso cerrado... por ahora. ¿Quién es «La Garra»?».

## 9. Pruebas

| Prueba | Resultado |
|---|---|
| `tests/cinematica_cinta_smoke.gd` (headless) | 30 OK, 0 fallos |
| `Herramientas/flujo_real_cinta.gd` (con ventana) | 4 OK |
| `dotnet build` | 0 errores |
| Partidas y progreso después de las pruebas | Iguales (md5) |
| Rendimiento a 1920×1080 (`medir_audio_cinta.gd`, GPU por cuadro cada 0.5 s) | Promedio 5.4 ms, máximo 6.5 ms (t = 28–29, la carátula): cabe de sobra en los 16.7 ms de 60 FPS |
| Consola | Sin errores ni advertencias durante la cinemática. Al cerrar Godot sale «7 RIDs of type Texture were leaked», que ya salía con la versión anterior |

En este equipo el juego entero corre a 30 FPS aunque se quite el vsync (la sala sola también: `medir_rendimiento.gd` da 30 FPS con 3.5 ms de GPU), así que el tope viene de la configuración del equipo y no de la cinemática. Por eso se reporta el tiempo de GPU.

## 10. Pendientes

- **Voz de Tito y susurro con palabras.** No hay grabaciones libres con esas frases. Si alguien las graba, `Cinta/voz_tito.ogg` ya está conectada y el susurro se reemplaza en `Cinta/susurro.wav`.
- **Manos de Porky en los botones.** En la toma 12 Porky queda fuera de cuadro para no tapar la grabadora: se ve la cinta salir, pero no su dedo en STOP.
- **Al mezclar HU-10.** Revisar que su `SistemaAyudas` siga mostrando texto solo con `Jugador.MostrarPensamiento` (que ya está bloqueado).
