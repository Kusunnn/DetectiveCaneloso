# Cinemática del Caso 0 «La cinta de las 3:12», versión película: entrega

- **Rama:** `HU-9.5-Animaciones-Tutorial`
- **Fecha:** 28 de octubre de 2026
- **Herramientas:** Blender 5.2.3 LTS (Steam), Godot 4.7.2 .NET

## 1. Qué es

La cinemática sigue la estructura de *La conversación* y *Blow Out*: el detective escucha la cinta, la rebobina y la vuelve a oír. Dura 69.8 s: 7 s de presente, 49 s de cinta, 11 s de presente y 2.8 s de título.

| # | t (s) | Época | Cuadro | Lente | Entrada |
|---|---|---|---|---|---|
| 1 | 0.0 | Presente | Caneloso, pegado a la mesa, pone PLAY (la mano en el botón, sonido real) | 28 mm | Fundido desde negro · «SALA 1 · 06:58» |
| 2 | 3.6 | Presente | Inserto: los carretes empiezan a girar y la cámara se acerca hasta que uno llena el cuadro | 50 mm | Corte |
| 3 | 7.0 | Cinta | El reloj de pared a las 3:09, del mismo tamaño y en el mismo lugar que el carrete | 50 mm | **Corte por coincidencia** + tracking · «03:09» |
| 4 | 9.6 | Cinta | Plano de situación desde la esquina alta: mesa, Porky dormido, grabadora, reloj y lámpara | 16 mm | Corte |
| 5 | 13.2 | Cinta | Inserto de la grabadora con REC | 50 mm | Corte |
| 6 | 16.2 | Cinta | Porky dormido, plano medio de lado a la altura de la mesa | 35 mm | Corte (el ronquido entra antes) |
| 7 | 19.2 | Cinta | Contrapicado amplio desde el piso: techo, rejilla abierta con luz fría, polvo y la mano | 22 mm | Corte (la rejilla suena antes) · «03:12» |
| 8 | 22.8 | Cinta | General: la figura baja por la cuerda, a contraluz del ducto, con Porky dormido en primer plano | 28 mm | Corte |
| 9 | 26.4 | Cinta | Desde el lado de Porky: la figura agachada junto al micrófono, su cabeza dormida en primer plano. Susurro | 35 mm | Corte |
| 10 | 30.8 | Cinta | Plano medio lateral: la figura con el reloj en las manos, voltea hacia Porky | 32 mm | Corte |
| 11 | 33.8 | Cinta | **La caída en cámara lenta:** el reloj cae y gira 3 s; la cámara lo sigue. En el golpe tiembla y se congela con las astillas en el aire. Se corta el tic-tac | 40 mm | Corte |
| 12 | 38.2 | Cinta | Inserto de la carátula en 3:12 con luz dura; paneo hacia arriba y zoom hasta la pluma en la rejilla. Silencio absoluto | 42 → 110 mm | Corte |
| 13 | 42.6 | Cinta | Porky despierta de golpe, orejas arriba, voltea al reloj (su cara a cámara) | 28 mm | Corte (el sobresalto entra antes) · «03:15» |
| 14 | 45.6 | Cinta | General: Porky sobre la silla cuelga el reloj | 28 mm | Corte |
| 15 | 48.6 | Cinta | Plano medio a 3/4: Porky frente a la grabadora, la cara y el pecho en rojo por REC. STOP, rebobinado, expulsar | 26 mm | Corte |
| 16 | 53.0 | Cinta | Desde el piso: Porky en la puerta, recortado contra la luz del pasillo, con la cinta; las botas en primer plano | 24 mm | Corte · «03:16» |
| 17 | 56.6 | Presente | Corte en seco: Caneloso con la mano congelada en el botón. «La Garra no olvida, Caneloso.» | 32 mm | Tracking de salida + **corte en seco** · «07:01» |
| 18 | 60.0 | Presente | Inserto: rebobina (el contador regresa) y la vuelve a poner; la voz suena limpia, sin el filtro de cinta | 40 mm | Corte |
| 19 | 63.8 | Presente | Caneloso voltea hacia la rejilla del techo, desde el punto de vista de la rejilla | 28 mm | Corte |
| — | 67.0 | — | Negro con el título «¿Quién es La Garra?» a máquina | — | Fundido |

Caso0.cs no cambia: la cinemática entra cuando Caneloso ya resolvió el caso. Antes de este cambio ya sonaba la cinta en ese momento, así que mostrar que él la pone y la escucha no contradice el juego. Después siguen el pensamiento «Alguien más estuvo aquí a las 3:12... y entró por la rejilla de la pluma negra.» y el objetivo «¿Quién es La Garra?», que repiten el cierre. En el presente solo sale Caneloso; Porky, Eriz y el comisario quedan fuera de cuadro.

## 2. Tabla de los problemas que señalaste

| Problema | Estado | Cómo |
|---|---|---|
| Subexpuesta y lavada (cuadros 3, 4, 7, 8 y 12) | **Corregido** | Cada cuadro tiene luz principal, relleno y contraluz motivados (lámpara, ducto, REC, pasillo). El pasado se expone ×1.15 y `recuerdo.gdshader` baja de 0.85 a 0.62 de fuerza. La carátula se lee (3:12 con luz dura) y la caída del reloj se ve completa |
| recuerdo.gdshader deja todo gris | **Corregido** | La fuerza va por época: 0.62 en la cinta (frío y legible; REC sigue siendo el único rojo vivo) y 0 en el presente (color normal y cálido) |
| Encuadres pegados a cabezas grandes (2, 5, 10, 11) | **Corregido** | Planos medios a 26–35 mm con cabeza y torso, cámaras a 1.8–2.5 m. Los primeros planos son solo de objetos: carretes, reloj, REC, carátula y grabadora |
| No hay plano de situación | **Corregido** | Cuadro 4, desde la esquina alta a 16 mm. Se ven la mesa, Porky, la grabadora, el reloj de la pared y la lámpara. La rejilla queda en el techo, fuera de ese cuadro, y se sitúa en el 7 |
| La tapa con polvo parece flotar en el vacío | **Corregido** | Cuadro 7: contrapicado de 22 mm desde el piso, con la esquina de las paredes y el techo y la luz del ducto |
| Ojos de Porky rotos o manchados (9 y 11) | **Corregido en parte** | La textura (`mEye_Alb`, 256×128), su UV y el material son iguales a los del juego. El problema era el recorte de alfa ampliado de cerca, que deja bordes dentados. En la cinemática los ojos se mezclan con alfa suave y filtro anisotrópico, y ya no hay primeros planos de su cara. La textura sigue siendo de baja resolución |
| Todos los cortes son el mismo destello blanco | **Corregido** | Fundido desde negro, cortes directos, corte por coincidencia (carrete → reloj), tracking solo al entrar y salir de la cinta, corte en seco al presente y fundido al título |
| No se entiende que escuchamos una cinta | **Corregido** | Presente → cinta → presente: Caneloso pone PLAY, los carretes giran y al final rebobina y vuelve a oír la voz |

## 3. Técnica

- **Blender.** `scripts/pelicula.py` describe los 19 cuadros con el mismo formato que `momentos.py`, y de ahí toma las poses del pasado. `construir_momentos.py -- --datos pelicula` agrega a Caneloso (su modelo del juego, exportado con `Herramientas/exportar_modelo_blender.gd`, porque el .fbx de Mixamo entra a Blender con las escalas rotas), los carretes, las astillas, los vacíos de inicio y fin de la caída, y exporta `cinta_pelicula.glb` y `cinta_pelicula.json`.
- **Godot.** `CinematicaCintaPelicula.cs` lee el .json (duración, época, transición, hora, luces, sonidos, subtítulos, música). Pone la pose de cada cuadro, mueve la cámara con curva suave y prende solo las luces del cuadro. Hace girar los carretes, anima la caída del reloj con aceleración y giro, y la cámara lo sigue. También maneja el tracking (`tracking.gdshader`), el color por época y el título. Todo depende de t: `IrA`, adelantar por cuadro, saltar y Movie Maker siguen funcionando.
- **Sonido.** En el pasado todo va por el bus con filtro de cinta; en el presente suena limpio (PLAY, rebobinado y la segunda vez de la voz). El ronquido, la rejilla y el sobresalto entran antes del corte (J-cut).
- **Música.** *Moil* (Ruskerdax, CC0), que ya estaba en el proyecto. Crece hasta la caída del reloj, se calla en la carátula y vuelve en el cierre. No se descargó música nueva.
- **Interruptor.** `CinematicaCinta.Version` = `Animada`, `Momentos` o `Pelicula` (por defecto). `UsarMomentosCongelados` sigue funcionando.

## 4. Revisión

| Punto | Resultado |
|---|---|
| Choques (`reporte_choques_pelicula.json`) | **0 intersecciones en los 19 cuadros** |
| `tests/cinematica_cinta_smoke.gd` | 0 fallos con la versión película (el paso de adelantar ahora empieza en t = 32, antes del golpe) |
| Consola | Sin errores |
| Rendimiento a 1920×1080 (`medir_audio_cinta.gd`) | GPU: 6.9 ms promedio y 8.1 ms máximo por cuadro (el límite de 60 FPS es 16.7 ms). El contador marcó 30 FPS estables, con una baja a 21 FPS solo en el primer segundo, mientras se compilan las luces. La ventana de la prueba corría en segundo plano, así que no pude confirmar los 60 FPS en primer plano |
| La sala al terminar | Igual que antes: reloj colgado y rajado en 3:12, rejilla con la pluma, grabadora sin cinta |

## 5. Entrega

- **Video:** `Escritorio/Cinematica_Caso0_Pelicula.avi` (Movie Maker, 1920×1080, 30 fps).
- **Comparación:** `Escritorio/Cinematica_Caso0_Comparacion_Pelicula.avi`, con los momentos congelados a la izquierda y la película a la derecha.
- **Hoja de contacto:** `docs/HU-9.5_capturas_cinta/pelicula_hoja.jpg`, y cada cuadro a 1920×1080 en `Escritorio/Cinematica_Caso0_Pelicula_capturas/`.
- Para verla: `tests/ver_cinematica_cinta.tscn` (F6). Con `-- momentos` o `-- animada` se ven las otras versiones.

## 6. Pendiente

- Los ojos de Porky dependen de una textura de 256×128. Para que se vean nítidos de cerca habría que redibujarla a mayor resolución.
- La pluma en el cuadro 12 se lee como una silueta pequeña en la rejilla. Si la quieres más grande, se puede cerrar más el zoom.
- Porky, la figura y Caneloso no tienen huesos de cara: la expresión depende de la postura, la cabeza y las orejas.
