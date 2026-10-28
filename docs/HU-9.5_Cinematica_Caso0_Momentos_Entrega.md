# Cinemática del Caso 0 «La cinta de las 3:12» en momentos congelados: entrega

- **Rama:** `HU-9.5-Animaciones-Tutorial`
- **Fecha:** 28 de octubre de 2026
- **Herramientas:** Blender 5.2.3 LTS (Steam), Godot 4.7.2 .NET

## 1. Qué cambió

La cinemática ya no anima a los personajes: muestra 12 instantes congelados, como Return of the Obra Dinn. Los personajes quedan quietos en una pose hecha a mano, la cámara se mueve despacio de su toma de inicio a la de fin, un acento de luz dirige la mirada y el audio de la cinta corre por debajo. Antes de cada cuadro hay un destello blanco de 3 fotogramas y el clic de la cinta.

La versión animada no se borró. `CinematicaCinta.UsarMomentosCongelados` (por defecto `true`) elige cuál se ve; en `false` vuelve la versión animada. Tú decides cuál se queda.

## 2. Los 12 cuadros (53.4 s en total)

| # | Hora | t (s) | Cuadro | Cámara | Audio |
|---|---|---|---|---|---|
| 0 | — | 0–4.2 | Negro: la confesión de Tito | — | PLAY, siseo, voz de Tito |
| 1 | 03:09 | 4.2 | La grabadora con REC; Porky dormido, desenfocado, al fondo | 50 mm, acercamiento | Siseo, ronquido, tic-tac |
| 2 | 03:09 | 8.2 | Porky dormido, el hocico sobre la mesa, la cabeza de lado y la gorra ladeada | 35 mm, paneo lateral | Ronquido |
| 3 | 03:12 | 11.8 | La tapa de la rejilla colgando, polvo en el aire, una mano enguantada asomando | 45 mm, contrapicado desde el piso | Rejilla metálica |
| 4 | 03:12 | 15.8 | La figura colgada del borde del hueco, con la cuerda | 24 mm, contrapicado que gira | Ronquido lejano |
| 5 | 03:12 | 19.8 | La figura agachada en la orilla de la mesa, la capucha junto al micrófono | 35 mm, acercamiento | Susurro «La Garra no olvida, Caneloso.» |
| 6 | 03:12 | 24.8 | De puntas, con el reloj en las manos, voltea hacia Porky | 30 mm, plano medio lateral | Tic-tac |
| 7 | 03:12 | 28.4 | El reloj a mitad de la caída, girado; la figura de espaldas se va | 28 mm, casi quieta | Golpe (el pico más fuerte) y se corta el tic-tac |
| 8 | 03:12 | 32.4 | La carátula en el piso en 3:12; paneo a la rejilla mal cerrada con la pluma | 85 mm, paneo | Silencio (solo el siseo) |
| 9 | 03:15 | 36.4 | Porky despierta de golpe, voltea al reloj: su cara a cámara | 35 mm | Sobresalto |
| 10 | 03:15 | 40.0 | De pie sobre su silla, cuelga el reloj con las dos manos | 35 mm, plano general | Roce del reloj |
| 11 | 03:15 | 43.6 | Frente a la grabadora, la cara roja por REC, la mano en la cinta | 28 mm, acercamiento | STOP, rebobinado, expulsar |
| 12 | 03:16 | 48.8 | En la puerta, de espaldas, con la cinta; las botas en primer plano | 24 mm, desde el piso | Puerta, pasos; fundido a negro |

No se ve el rostro de la figura: la capucha tiene el hueco negro y su tela es negra con borde de luz.

## 3. Cómo están hechas las poses

- **A mano, hueso por hueso.** En `scripts/momentos.py` cada cuadro dice el giro de cada hueso (columna, cuello, cabeza, muslos, piernas, pies, orejas, gorra, cola, faldones de la camisa) en los ejes del personaje. No hay animaciones de Quaternius ni keyframes calculados.
- **IK solo para el contacto.** La muñeca va a un punto exacto: el borde del hueco de la rejilla, el reloj, la mesa, la grabadora. El codo apunta adonde se le indica.
- **Contactos sin atravesar.** Después de posar, un parámetro se corrige paso a paso hasta que deja de cruzarse con el objeto, o hasta tocarlo (la rodilla en el asiento, los pies en el piso). Así lo que se apoya queda apoyado y sin meterse.
- **Porky duerme hincado en su silla.** Su cabeza mide casi la mitad de la mesa y sus hombros quedan a la altura del tablero. Sentado, ni la cabeza ni los brazos llegan arriba de la mesa sin atravesarla. Hincado sobre el asiento, como un niño en una mesa de adultos, apoya el hocico en la mesa con la cabeza de lado.
- **Expresiones.** Porky no tiene blendshapes ni huesos de párpados o boca; sus ojos son una sola textura. Las emociones se resuelven con la postura, la cabeza y las orejas: caídas al dormir, levantadas al despertar y en la grabadora. Por eso no hay «boca abierta» ni «ojos muy abiertos» de verdad: sus ojos siempre se ven igual.
- **Objetos en el instante.** La tapa de la rejilla cuelga a medio abrir, el reloj está a mitad de la caída, hay polvo suspendido y la cinta queda a medio salir.

## 4. En Godot

- `CinematicaCintaMomentos.cs` (parte de `CinematicaCinta`): en cada cuadro pone la pose (un fotograma del .glb, sin interpolar), lleva la cámara de `Cam_k_Ini` a `Cam_k_Fin` con curva suave (lente incluida), enfoca `Foco_k` con el desenfoque y pone el acento de luz en `Luz_k`. Además: luz fría del ducto, contraluz de la figura, foco en la carátula que pasa a la pluma, rojo de REC en la cara de Porky y luz del pasillo en la puerta.
- Todo depende de `t`, así que `IrA`, adelantar con clic o Enter (salta al siguiente cuadro) y Movie Maker dan el mismo resultado.
- Se conserva lo demás: saltar manteniendo Espacio/Esc/B/Start, subtítulos, horas a máquina, barras, `recuerdo.gdshader` (REC como único color vivo), viñeta, grano y el bus con filtro de cinta.
- Al terminar la sala queda igual que con la versión animada: el reloj colgado y rajado en 3:12, la tapa con la pluma, la grabadora sin cinta en REC y el contador en 03:12. El registro de cinemáticas vistas (CA-03) no cambia.

## 5. Revisión

| Punto | Resultado |
|---|---|
| Choques (`reporte_choques_momentos.json`) | **0 intersecciones en los 12 cuadros.** Son triángulos de las mallas deformadas contra la mesa, las sillas, las paredes, el piso, el techo (salvo el hueco), el reloj, la grabadora, la puerta, la cuerda y entre personajes |
| Manos en su punto | La figura sí llega: al borde del hueco, al reloj y a la mesa (a menos de 1.1 cm). **Porky no del todo:** dormido, sus manos quedan a 12–14 cm de la orilla de la mesa; al colgar el reloj, a 12–13 cm de su borde; en la grabadora, a 5–8 cm. Le faltan brazo (0.36 m) y no puede acercarse más sin meter la cabeza en la pared o en la mesa |
| La cabeza de Porky «apoyada en su brazo» | **No se pudo:** con esa cabeza y esos brazos, el hocico se apoya directo en la mesa, de lado, sin atravesarla |
| `tests/cinematica_cinta_smoke.gd` | 0 fallos (el paso de adelantar ahora da 3 clics, para pasar el golpe en las dos versiones) |
| Consola | Sin errores de la cinemática |
| Rendimiento a 1920×1080 (`medir_audio_cinta.gd`) | GPU: 6.2 ms promedio y 7.0 ms máximo por cuadro, lejos de los 16.7 ms de 60 FPS. En esa corrida el contador marcó 30 FPS estables; la ventana de la prueba corría en segundo plano, así que no pude confirmar los 60 FPS en primer plano |
| Duración | 53.4 s (pedido: 45–55 s) |

## 6. Video y capturas

- **Video:** `Escritorio/Cinematica_Caso0_Momentos.avi`, grabado con Movie Maker a 1920×1080 y 30 fps desde `tests/ver_cinematica_cinta.tscn`.
- **Comparación lado a lado:** `Escritorio/Cinematica_Caso0_Comparacion.avi` (1920×1080, 30 fps). A la izquierda la versión animada y a la derecha los momentos congelados, con el audio de la nueva. También como imagen en `docs/HU-9.5_capturas_cinta/antes_despues_momentos.jpg`.
- **Hoja de contacto:** los 12 cuadros a 1920×1080 desde su cámara final en `Escritorio/Cinematica_Caso0_Momentos_capturas/` y juntos en `docs/HU-9.5_capturas_cinta/momentos_hoja.jpg`.

Para ver una u otra versión: abre `tests/ver_cinematica_cinta.tscn` y dale F6 (momentos congelados), o desde la consola con `-- animada`. Para dejar una en el juego, cambia `UsarMomentosCongelados` en `CinematicaCintaMomentos.cs`.

## 7. Archivos

| Archivo | Qué es |
|---|---|
| `game/Assets/Cinematicas/Blender/scripts/momentos.py` | Los 12 cuadros, pose por pose |
| `game/Assets/Cinematicas/Blender/scripts/construir_momentos.py` | Armado, contactos, choques y exportación |
| `game/Assets/Cinematicas/Blender/caso0_momentos.blend` | La escena |
| `game/Assets/Cinematicas/Blender/reporte_choques_momentos.json` | Choques e IK |
| `game/Assets/Cinematicas/Blender/LEEME.md` | Cómo regenerarla |
| `game/Assets/Cinematicas/Caso0/cinta_momentos.glb` (+ texturas de Porky extraídas por Godot) | La exportación |
| `game/Escenas/Cinematicas/CinematicaCintaMomentos.cs` | La reproducción de los cuadros |
| `game/Escenas/Cinematicas/CinematicaCinta.cs` | El interruptor entre las dos versiones |
| `game/tests/CinematicaCintaPrueba.cs`, `ver_cinematica_cinta.gd`, `cinematica_cinta_smoke.gd` | Elegir la versión, grabarla y la prueba |
| `game/Herramientas/reproducir_secuencia.gd` | Graba la comparación lado a lado con Movie Maker |
| `game/Assets/Caso1/CREDITOS_ASSETS.md`, `game/Assets/Audio/CREDITOS_AUDIO.md` | Créditos (sin assets externos nuevos) |

## 8. Pendiente

- Las manos de Porky no llegan a la mesa ni al reloj por la proporción del modelo (sección 5).
- El cuadro 7 (la figura de espaldas) y el 12 (las botas) son muy oscuros: se leen como silueta. Si los quieres más claros, solo hay que subir su acento en `MAcentos`.
- Las expresiones de Porky dependen de su postura: el modelo no tiene párpados ni boca que se muevan.
