# Cinemática del Caso 0 «La cinta de las 3:12» animada en Blender: entrega

- **Rama:** `HU-9.5-Animaciones-Tutorial`
- **Fecha:** 25 de octubre de 2026
- **Herramientas:** Blender 5.2.3 LTS (Steam), Godot 4.7.2 .NET

## 1. Qué opción usé y por qué

**Opción A:** la animación se hace en Blender y Godot la reproduce en tiempo real.

- Usa los modelos y la luz del juego, así que no parece otro juego. Con un video habría que igualarlos a mano.
- `IrA`, adelantar con clic, saltar, los subtítulos y los sonidos siguen funcionando igual: un `AnimationPlayer` se pone en el segundo `t` en cada cuadro.
- No hace falta ffmpeg (no está instalado) y no hay que mantener un video de hasta 60 MB.
- A 1920×1080, la GPU tarda en promedio 5.3 ms por cuadro (máximo 7.2 ms), lejos del límite de 16.7 ms para 60 FPS.

## 2. Cómo está hecha

Todo se arma con scripts, así que se puede rehacer cuando cambie algo (`game/Assets/Cinematicas/Blender/LEEME.md`):

1. `Herramientas/exportar_sala_blender.gd` exporta la Sala 1 desde Godot a glTF, con todo en su posición real.
2. `scripts/construir_cinta.py` importa la sala, a Porky (su `porky.glb`) y el maniquí de Quaternius, y arma la escena en `caso0_cinta.blend`:
   - **Ropa de la figura:** gabardina abierta al frente (con pesos automáticos, sigue las piernas) y capucha con el hueco de la cara en negro.
   - **Animaciones base:** de Quaternius (CC0). A Porky se le pasan con `retarget.py` y se mezclan al cambiar, sin poses congeladas: colgada y trepando, las piernas se mecen; al susurrar, la cabeza y los hombros se mueven con las palabras.
   - **Pies:** apoyados en el piso cuadro a cuadro (o en el asiento cuando Porky se sube a la silla).
   - **Manos y cabeza:** IK de dos huesos propio, horneado cuadro a cuadro. Las manos van a la rejilla, la mesa, el reloj, la silla, los botones, la cinta y la manija; la cabeza mira al micrófono, a Porky, al reloj y a la grabadora. El IK de Blender no sirve con el esqueleto de Porky (Animal Crossing) porque sus huesos tienen colas de varios metros.
   - **El reloj:** cae con Rigid Body (cilindro de 14.5 cm, piso y pared pasivos), horneado cuadro a cuadro.
   - **Cámara:** 18 tomas con cortes secos, lente por toma y un leve movimiento de cámara en mano que tiembla con el golpe. El desenfoque lo pone Godot.
   - **Revisión automática:** cada 2 cuadros revisa choques contra la mesa, las paredes, el piso y el techo, y cuánto le falta a cada mano para llegar (`reporte_choques.json`).
3. Exporta `Assets/Cinematicas/Caso0/cinta_caso0.glb` con una sola animación «Cinta»: Porky, la figura, la cámara, la cuerda y vacíos `Anim_*` con el recorrido del reloj, la tapa de la rejilla, la puerta, la silla y la cinta.
4. `CinematicaCinta.cs` instancia el .glb, esconde al Porky del juego, pone el `AnimationPlayer` en `t` en cada cuadro y hace que los objetos reales copien a sus vacíos. Ya no queda movimiento hecho por código. Al terminar o al saltar, todo vuelve como estaba.

## 3. Cómo se ve cada momento

| # | t | Qué pasa |
|---|---|---|
| 1 | 0–4.2 | Negro: PLAY, siseo y la confesión de Tito subtitulada; «SALA 1 · 03:09» |
| 2 | 4.2–12 | Porky duerme inclinado sobre la mesa, con la cabeza de lado sobre los brazos cruzados; respira, cabecea y ronca |
| 3 | 12–17 | 03:12. La rejilla vibra y suena; la tapa sube y se desliza hacia el ducto; cae una cuerda negra. La figura asoma las piernas, queda colgada de las manos del borde (el hueco es de 0.5 m), se suelta (1 m, 0.45 s) y cae agachada amortiguando con las rodillas |
| 4 | 16.6–23 | Camina agachada hasta la grabadora, se inclina sobre la mesa apoyando una mano y susurra al micrófono |
| 5 | 23–27.5 | Va a la pared, toma el reloj con las dos manos, lo saca del clavo, da un paso a un lado mirando a Porky y lo suelta. Cae 0.4 s, golpe en t = 26.40, un rebote en 26.53 y queda quieto, boca arriba, en 27.27. El tic-tac se corta con el golpe |
| 6 | 27.5–32.5 | Inserto de la carátula en 3:12. La figura vuelve a la cuerda, trepa y sube por el hueco; recoge la cuerda. La tapa regresa ladeada y corrida, con la pluma negra enganchada |
| 7 | 33.6–45 | 03:15. Porky despierta de un salto, mira alrededor y ve el reloj. Se levanta, empuja su silla hasta la pared, recoge el reloj agachándose con las dos manos, se sube a la silla y lo cuelga; se baja y regresa la silla a la mesa |
| 8 | 45.7–51.9 | Ve la luz de REC, se encoge y se lleva las manos a la cabeza. STOP, rebobina (el contador regresa a 03:12), la cinta sale y la toma |
| 9 | 51.9–59.7 | Va a la puerta, jala la manija, sale con la cinta y la puerta se cierra. Inserto de sus botas en el pasillo, rumbo al vestidor, donde están las huellas |

No se ve el rostro de la figura ni nada que diga quién es «La Garra». La luz de REC sigue encendida al final, porque la historia dice que a las 6:02 la grabadora «sigue en REC pero sin cinta».

## 4. Dos cambios en la Sala 1 (para que fuera posible físicamente)

| Qué | Antes | Ahora | Por qué |
|---|---|---|---|
| Altura del reloj (`RelojPista`; `FocoReloj` reapuntado) | 2.55 m (2.05 m sobre el piso) | 1.9 m (1.4 m sobre el piso) | Porky mide 1.74 m con la cabeza, pero sus hombros están a 0.66 m del piso: aun subido a su silla alcanza 1.98 m. A 2.55 m ni de pie sobre la mesa podría colgarlo |
| Grabadora (`GrabadoraPista`) | z = −1.85 | z = −1.70 (15 cm hacia el lado del detective) | Con sus brazos de 0.36 m y una cabeza de casi 0.8 m, desde el borde de la mesa no alcanzaba los botones |

El juego no cambia: las dos pistas siguen donde el jugador las busca, con la misma descripción. Si prefieres otra altura para el reloj, solo cambia la coreografía de Porky.

## 5. Lista de chequeo

| Punto | Resultado |
|---|---|
| Porky no atraviesa la mesa | **Casi.** Dormido, la cara descansa sobre la mesa y la toca (hasta 13 vértices de la muestra dentro del tablero). Al levantarse y frente a la grabadora roza la orilla (hasta 21). Antes metía la cara en la mesa |
| La figura no atraviesa la mesa | Sí: 0 vértices al susurrar (antes, 350) |
| Nadie atraviesa paredes | **Casi.** Porky roza la pared con la cabeza mientras cuelga el reloj subido a la silla (t = 41.3–42.9); en la toma de lado no se nota. En un solo cuadro (t = 25.0), cuando la figura toma el reloj, sus manos pasan la pared |
| Pies en el piso | Sí, salvo 1 cm al agacharse a recoger el reloj y 1–2 cm de la figura al trotar |
| Las manos tocan los objetos | Sí (a menos de 7 cm) en la rejilla, la mesa (la figura y Porky dormido), la silla, el reloj al descolgarlo y al recogerlo, la cinta y la manija. **No del todo** en cuatro momentos: la mano derecha de Porky queda a 20 cm de los botones, a 12–26 cm del reloj al colgarlo y a 13 cm de la cabeza en el pánico (no le alcanzan los brazos); y las manos de la figura se separan 25 cm del reloj medio segundo antes de soltarlo. En cámara se ve la mano de Porky sobre la grabadora y sobre el reloj |
| El reloj cae con física | Sí: Rigid Body con giro, un rebote y se para boca arriba (0.4 s de caída y quieto 1.27 s después de soltarlo) |
| La figura entra y sale por la rejilla de forma creíble | Sí: cuelga del borde y se suelta 1 m; sale trepando por una cuerda que amarró arriba, por el centro del hueco |
| Sin poses congeladas | Sí: todas las animaciones corren y se mezclan; colgada y trepando se mueven las piernas |
| Rojo de REC como único color vivo | Sí (el shader de la FASE 2) |
| Sin saltos de eje ni jump cuts | Sí: cada corte cambia de lado o de tamaño de plano |

## 6. Video y capturas

- **Video:** `Escritorio/Cinematica_Caso0_Blender.avi`. Movie Maker, 1920×1080, 30 fps, 80.6 s, jugado desde el flujo real: las tres preguntas, la frase del comisario, la cinemática (de 0:12.5 a 1:12) y la pantalla de caso cerrado.
- **Capturas:** 27 a 1920×1080 en `Escritorio/Cinematica_Caso0_Blender_capturas/`.
- **ANTES/DESPUÉS por momento:** `docs/HU-9.5_capturas_cinta/antes_despues_blender.jpg`. ANTES es la versión animada por código de la FASE 2.

## 7. Pruebas

| Prueba | Resultado |
|---|---|
| `tests/cinematica_cinta_smoke.gd` | 0 fallos |
| `Herramientas/flujo_real_cinta.gd` | 4 OK: sale después de la frase del comisario, nada encima, objetivo final correcto |
| Audio del video | El golpe del reloj sigue siendo el pico más fuerte (−3.5 dB); base de t = 0–22 entre −36.8 y −32.3 dB RMS |
| GPU a 1920×1080 (`medir_audio_cinta.gd`) | 5.3 ms promedio, 7.2 ms máximo |
| Partidas | Se respaldan en disco y se devuelven al final de la prueba del flujo real |

## 8. Archivos, fuentes y licencias

| Archivo | Qué es |
|---|---|
| `game/Assets/Cinematicas/Blender/caso0_cinta.blend` | La escena de Blender (29 MB) |
| `game/Assets/Cinematicas/Blender/scripts/*.py` | Coreografía, armado, retarget y vista previa |
| `game/Assets/Cinematicas/Blender/sala1.glb` | La Sala 1 exportada desde Godot (solo referencia; Godot no la importa) |
| `game/Assets/Cinematicas/Blender/reporte_choques.json` | Revisión automática |
| `game/Assets/Cinematicas/Blender/LEEME.md` | Cómo regenerar |
| `game/Assets/Cinematicas/Caso0/cinta_caso0.glb` (+ texturas de Porky extraídas por Godot) | La animación exportada |
| `game/Escenas/Cinematicas/CinematicaCinta.cs` | Reproduce el .glb sincronizado con `t` |
| `game/Escenas/Interrogatorio/sala_interrogatorio.tscn` | Reloj a 1.9 m (y su foco) y grabadora 15 cm más cerca |
| `game/Herramientas/exportar_sala_blender.gd`, `inspeccionar_glb.gd` | Exportar la sala e inspeccionar un .glb |
| `game/tests/cinematica_cinta_smoke.gd` | Revisa que se borre la animación de Blender al terminar |
| `game/Assets/Audio/CREDITOS_AUDIO.md`, `game/Assets/Caso1/CREDITOS_ASSETS.md` | Créditos |

**Animaciones:** Quaternius · Universal Animation Library 1 y 2, CC0 1.0. No se usaron Mixamo ni Rokoko: Mixamo pide iniciar sesión con una cuenta de Adobe y Rokoko Vision necesita grabar a alguien con la webcam.

**Modelos:** solo los del juego (Porky, el maniquí de Quaternius, la sala). La gabardina, la capucha y la cuerda están modeladas en Blender.

**Sonidos:** no hay nuevos. Se reusan los de la FASE 2 (BigSoundBank, CC0) y los Kenney del proyecto (CC0), ahora también para la silla.

## 9. Lo que queda pendiente

- Los contactos de mano que no llegan del todo y el roce de Porky con la pared (sección 5). Se pueden mejorar acercando más los objetos o con una animación grabada para Porky.
- La voz de Tito y el susurro con palabras siguen sin grabar (`Cinta/voz_tito.ogg`).
