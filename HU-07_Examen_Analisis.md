# HU-07 — Examen detallado de la evidencia: análisis previo

Rama base propuesta: `HU-07` creada desde `HU-20.5` (commit `1020752`). Todavía no hay código cambiado.

## 0. Cómo funciona hoy

### Registro de una evidencia
- `Escenas/Interrogatorio/ObjetoPista.cs`: `StaticBody3D` con `Id`, `Titulo`, `Descripcion`, `Imagen`, `EsEvidencia`, `SeRecoge`, `RequiereLinterna`, `SoloAgachado`, `Visuales`, `ComentarioAlMirar`. Al usarlo llama `Jugador.RegistrarEvidencia(IdPista, Titulo, Descripcion, Imagen)` y, si `SeRecoge`, se oculta y pierde la colisión.
- `Jugador.RegistrarEvidencia` (`Jugador.cs:243`) arma un `Pista` con `GD.Load<GDScript>("res://hud/pista.gd").New()`, lo pasa a `hud.agregar_pista`, suena Pista + Sello, marca cambios sin guardar y emite `EvidenciaRegistrada(id)`. Si no hay imagen usa `res://Assets/Pistas/<id>.png`.
- `Jugador.RestaurarEvidencia` (`Jugador.cs:271`) hace lo mismo al cargar, sin sonido ni aviso.
- **No existe hoy un "modelo" de la evidencia**: el `ObjetoPista` solo pasa textos e imagen. La vista de examen necesita saber qué malla mostrar, así que la pista tiene que llevar su modelo (ver §3).

### Resource `Pista` y HUD
- `hud/pista.gd` solo tiene `id`, `titulo`, `descripcion`, `imagen`.
- `hud/hud.gd` guarda `pistas: Array[Pista]` y las dibuja en tres sitios con `_draw` a mano (no hay nodos Control):
  - el post-it de la esquina (`_dibujar_postit_pista`),
  - el expediente [TAB] (`_dibujar_expediente`: lista de 7 filas a la izquierda, detalle a la derecha),
  - la galería de la libreta [Q] (`_dibujar_galeria_pistas`, 9 fotos por hoja, clic para ver el detalle).
- **Matiz sobre `detalle_solicitado`**: hoy se emite **al abrir el expediente con TAB** (con la pista seleccionada), no al elegir una pista; al cambiar de pista se emite `pista_seleccionada`. Nadie escucha ninguna de las dos salvo el sonido de página en `Jugador.cs:84`. Propuesta: que `detalle_solicitado(pista)` se emita al pulsar **[E] o el botón [Examinar]** dentro del expediente (y en la libreta), y quitarla del TAB. Así sigue siendo la única entrada a la vista de examen, como pides.
- El aviso superior (`avisar(titulo, detalle)`) ya sirve para "EXPEDIENTE ACTUALIZADO / NUEVO DETALLE".

### Dónde se usan las pistas
- **Tablero** (`TableroDeduccion.cs`): no lee el HUD; el caso le da `ObtenerCartas`. `Caso0.Cartas()` y `Caso1.Cartas()` recorren `Hud.pistas`, así que **una entrada nueva aparece sola como carta**. Las parejas válidas viven en `Caso0.ProbarDeduccion`.
- **Diálogos** (`VentanaDialogo.ElegirEvidencia`): recibe `(Id, Titulo, Imagen)`; en el Caso 0 la lista sale de `Cartas()`, así que **la entrada nueva también se puede presentar** sin tocar `VentanaDialogo`. Si el personaje no la reconoce, responde "¿Y eso qué tiene que ver conmigo?" como con cualquier pista.
- **Caso 1 no tiene interrogatorios ni deducciones todavía** (`Probar = (_, _) => null`). Ahí los detalles nuevos solo podrán verse en el expediente y como cartas del tablero.

### Guardado (`GestorPartida.cs`)
- `GestorPartida` es genérico: guarda lo que devuelva `IEstadoPartida.CapturarEstado()` de la escena. **No hay que tocarlo**, salvo subir `Version` si cambia el formato.
- Las pistas se guardan en `Caso0.Partida.cs:72` como `{id, titulo, descripcion}` y se restauran en orden. Hay que agregar `examinada`, `origen_id` y los detalles descubiertos (la imagen capturada del detalle se guarda como PNG en `user://` o se vuelve a generar desde el modelo al cargar; propongo lo segundo, ver §5).
- **El Caso 1 no implementa `IEstadoPartida`: hoy no se puede guardar el campamento.** El guardado de HU-07 aplicará al Caso 0. Hacer guardable el Caso 1 es otra historia; si lo quieres dentro de HU-07, dímelo.

## 1. Inventario de evidencias

Ninguna evidencia tiene hoy un modelo propio: todas son mallas primitivas dentro de la escena (BoxMesh, CylinderMesh, carteles de texto `CartelPared`) y dos usan prefabs Kenney. Propuesta: cada `ObjetoPista` exporta un `PackedScene` de examen. Para no duplicar trabajo, el valor por defecto será **una copia de las mallas hijas del propio ObjetoPista** (reutilizar lo del escenario); solo hago escena propia cuando la del escenario no aguanta un acercamiento 3x o cuando hace falta poner algo que no existe (reverso de una foto, suela de la bota).

### Caso 0 (8 evidencias)
| id | Evidencia | En la escena hoy | Modelo de examen |
|---|---|---|---|
| `grabadora` | Grabadora en REC | BoxMesh + 2 bobinas + Label3D "03:12" | Escena propia `examen_grabadora.tscn`: carcasa con biselado simple, tapa abierta, ranura vacía, etiqueta trasera (detalle) |
| `reloj` | Reloj parado a las 3:12 | Mallas de pared enlazadas por `Visuales` | Reutilizar `Reloj`, `RelojAro` y agujas, con cristal rajado como textura |
| `huellas` | Huellas de bota | 9 BoxMesh de barro en el piso | Plano de yeso/barro con la huella (molde) |
| `envoltorio` | Envoltura de sándwich | BoxMesh + migas | Papel plano 2 mm arrugado (malla con pliegues) |
| `libro_turnos` | Libro de turnos | BoxMesh | Libro abierto: dos planos con la página de firmas |
| `nota_comisario` | Nota del comisario | BoxMesh papel | Papel 2 mm, frente con el texto, reverso de papel |
| `cinta` | La cinta desaparecida | CylinderMesh | Casete simple (caja + dos carretes + etiqueta) |
| `pluma` | Pluma negra (secreta) | BoxMesh | Reutilizar, con textura de pluma en un plano doble cara |

### Caso 1 (12 evidencias activas + 1 apagada)
| id | Evidencia | En la escena hoy | Modelo de examen |
|---|---|---|---|
| `interruptor_generador` | Interruptor bajado | Tablero + palanca + cartel | Reutilizar |
| `candado_caseta` | Candado sin forzar | Torus + Box | Reutilizar (añadir ojo de la llave) |
| `baul_disfraces` | Baúl sin cornamenta | Capa, corona, inventario | Reutilizar la hoja de inventario como papel 2 mm |
| `lena_intacta` | Leña intacta | Prefabs Kenney `lena_grande`, `lena`, `hacha_k` | Reutilizar los prefabs |
| `libro_guardia_quemado` | Libro de guardia quemado | 3 hojas finas + cartel | Escena propia: 3 fragmentos de papel con borde quemado |
| `nota_porky` | Nota de Porky | Cartel | Papel 2 mm |
| `foto_grupo` | Foto del grupo de hace 20 años | Marco + Quad con `foto_grupo.png` | Escena propia: foto 2 mm con frente y reverso escrito |
| `recorte_periodico` | Recorte de periódico | Corcho + cartel | Papel 2 mm (sin el corcho) |
| `oferta_compra` | Oferta de compra rota | 3 pedazos | Reutilizar los pedazos como papel 2 mm |
| `taza_delantal` | Taza a medias y delantal | Prefab Kenney `taza` + chocolate | Reutilizar |
| `registro_enfermeria` | Registro de la enfermería | Libreta + tachón | Escena propia: libreta abierta, tachón y texto debajo (linterna) |
| `botas_tony` | Botas con barro | 2 botas de primitivas + juncos | Reutilizar y añadir suela con juncos (detalle) |
| `registro_llamadas` | (apagada) | Hoja | Papel 2 mm, por si se activa |

Todo lo nuevo lo modelo simple en la escena con primitivas y texturas propias; no veo necesario descargar modelos. Si algo se descarga (Kenney / Poly Pizza CC0), queda en `CREDITOS_ASSETS.md`.

## 2. Problema importante con los detalles del Caso 1

Los cuatro detalles propuestos **ya se le dan al jugador al recoger la pista**, lo que rompe CA-02:
- `libro_guardia_quemado`: descripción, pensamiento (`Caso1.cs:124`) y nota de libreta dicen "monitor auxiliar Antonio R.", y el cartel del fragmento lo muestra en el escenario.
- `foto_grupo`: descripción, pensamiento y nota dicen "el monitor con silbato... es Tony".
- `registro_enfermeria`: el título y el pensamiento ya dicen "«23:20, ruido en el lago», tachado".
- `botas_tony`: el título es "Botas con barro y juncos" y la nota menciona los juncos.

Para cumplir CA-02 hay que **reescribir esos textos** (descripción, pensamiento, nota de libreta) para que digan solo lo visible a simple vista, **tapar el detalle en el escenario** (tachón opaco, fragmento ilegible, suela que no se ve) y **rehacer sus fotos** con `fotografo_pistas.gd`. La información no se pierde: pasa a la entrada nueva que se descubre al examinar. Esto cambia el guion del Caso 1, por eso lo pongo a tu aprobación.

## 3. Propuesta de detalles ocultos

| Evidencia | Detalle | Cómo se descubre | Entrada nueva | Dónde se usa |
|---|---|---|---|---|
| Grabadora (Caso 0, tutorial) | Etiqueta trasera escrita a mano: «Sala 1 — no detener — B.» | Girar (verla por detrás) + zoom 1.5x. Con ayuda visual "Gira el objeto" la primera vez | `grabadora.etiqueta` «La letra de "B."» | Carta del tablero: `Par(grabadora.etiqueta, NotaComisario)` confirma que es la misma letra. Se puede mostrar al comisario igual que la nota |
| Libro de guardia (Caso 1) | En un fragmento: «monitor auxiliar Antonio R.» | Girar el fragmento quemado + zoom 2x | `libro_guardia_quemado.antonio` | Expediente y tablero |
| Foto del grupo (Caso 1) | Reverso: dedicatoria «Cabaña 4, verano del 2006. Para Daniel. — A.R.» | Girar 180° (reverso) | `foto_grupo.dedicatoria` | Expediente y tablero |
| Foto del grupo (Caso 1) | Frente: el monitor del fondo lleva el mismo silbato que hoy usa Tony | Zoom 2.5x sobre el fondo | `foto_grupo.silbato` | Expediente y tablero |
| Registro de enfermería (Caso 1) | Debajo del tachón: «23:20 – ruido en el lago» | Linterna [F] + zoom 1.5x | `registro_enfermeria.tachon` | Expediente y tablero |
| Botas (Caso 1) | Suela con juncos del lago | Girar hasta ver la suela | `botas_tony.juncos` | Expediente y tablero |

Son 6 detalles en 5 evidencias (CA-02 pide 3), mezclando giro, reverso, zoom y linterna. Las otras 15 evidencias no tienen detalle. El año de la dedicatoria lo ajusto a lo que diga el guion del Caso 1 (hoy solo dice "hace 20 años").

Alternativa para el Caso 0, si prefieres otra: el reloj con una barba de pluma negra atorada en el cristal rajado (adelanta a "La Garra"). La descarto como principal porque destapa el secreto de la pluma demasiado pronto.

## 4. Diseño técnico propuesto (resumen)

- **`hud/pista.gd`**: se agregan `modelo: PackedScene`, `descripcion_ampliada`, `lugar`, `examinada: bool`, `origen_id: StringName` y `detalles: Array[DetalleOculto]`.
- **`hud/detalle_oculto.gd`** (Resource nuevo): `id`, `posicion`, `angulo_vista` (normal y tolerancia), `zoom_minimo`, `requiere_linterna`, `titulo`, `descripcion`, `pensamiento`. Se crean como `.tres` y se asignan en el inspector del `ObjetoPista` (nuevos exports `ModeloExamen`, `DescripcionAmpliada`, `Lugar`, `Detalles`), así se configuran sin tocar código.
- **`Escenas/Investigacion/VistaExamen.tscn` + `VistaExamen.cs`**: CanvasLayer alto con fondo oscurecido (85–90 %) y desenfoque por shader sobre la captura de pantalla, `SubViewportContainer` con un `SubViewport` de mundo propio (`own_world_3d`): cámara, luz principal cálida, luz de contorno, relleno y sombra suave. El modelo se centra por AABB y se escala al ~60 % de la pantalla. `ProcessMode = Always` y `GetTree().Paused = true` mientras está abierta.
- **Controles**: arrastre izquierdo / stick izquierdo con inercia y amortiguación; rueda / gatillos 1x–3x hacia el punto bajo el cursor; arrastre derecho / stick derecho para desplazar al estar acercado; [R] reiniciar; [F] linterna (un SpotLight en la cámara que además hace visibles los nodos marcados "solo_linterna" del modelo); Esc / TAB / [Volver] cerrar. Barra de controles abajo que cambia entre teclado y control según el último dispositivo usado.
- **Detección del detalle**: cada frame se comprueba, para cada detalle no descubierto, que su normal apunte a la cámara dentro de la tolerancia, que el zoom sea ≥ al mínimo, que esté dentro del encuadre y, si hace falta, que la linterna esté encendida. Si se cumple: lupa en la esquina + "tic" + aviso "[E] Examinar detalle". Al confirmar: la cámara se acerca sola, se captura el SubViewport como imagen de la entrada, pensamiento de Caneloso y sonido de descubrimiento.
- **Señales genéricas**: `VistaExamen` emite `Examinada(id)` y `DetalleDescubierto(origenId, detalleId)`. El `Jugador` crea la entrada nueva con `RegistrarEvidencia` (con `origen_id`) y el caso decide si da pensamiento o nota extra. Ningún sistema conoce el guion.
- **Aviso tras recoger**: "[E] Examinar" unos 4 segundos en el HUD; si se pulsa, abre la vista.
- **Expediente (CA-04)**: en cada fila, ícono de lupa gris + "SIN REVISAR" / sello "EXAMINADA" + lupa ámbar / entrada de detalle con lupa con destello, sangrada bajo su evidencia de origen con un hilo y la etiqueta "Descubierto en: …". Contador "EXAMINADAS 3/7" (cuenta solo evidencias, no detalles). Botón [Examinar] en el panel derecho. Lo mismo, más compacto, en la galería de la libreta.
- **Sonidos**: girar, acercar, tic de detalle y descubrimiento, de Kenney (CC0) o Freesound CC0, registrados en `Assets/Audio/CREDITOS_AUDIO.md` y añadidos al enum de `Sonidos.cs`.
- **Rendimiento**: SubViewport a la resolución de pantalla, sombras de una sola luz, el mundo del juego pausado detrás. Mido FPS con la vista abierta.

## 5. Guardado
- `Caso0.CapturarEstado` añade a cada pista `examinada` y `origen_id`; las entradas de detalle se guardan como pistas normales con su `origen_id` y el id del detalle.
- La imagen del detalle se vuelve a generar al cargar (render del modelo en la pose del detalle, fuera de pantalla), así la partida no depende de PNG sueltos. Si da problemas, la alternativa es guardar el PNG junto a la ranura.
- Partidas viejas siguen cargando: si falta `examinada`, vale `false`.

## 6. Decisiones que necesito de ti
1. ¿Apruebas los 6 detalles de la tabla, incluido reescribir los textos del Caso 1 que hoy ya revelan el detalle al recogerla?
2. Caso 0: ¿grabadora con etiqueta «— B.» (recomendada) o reloj con la barba de pluma?
3. Guardado: ¿solo Caso 0 (el Caso 1 hoy no se puede guardar) o también hago guardable el campamento dentro de HU-07?
4. `detalle_solicitado`: ¿de acuerdo en que se emita con [E]/[Examinar] en vez de al abrir el expediente con TAB?

Notas sueltas que vi al revisar: `Caso1.cs:123` escribe "Miawrio" en vez de "Miaurio", y `HU-20.5_Escenario1_Analisis.md` dice que el registro de llamadas está en la guantera cuando la escena lo pone en el asiento del copiloto.
