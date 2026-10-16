# HU-07 — Examen detallado de la evidencia: entrega

Rama `HU-07`, creada desde `HU-20.5` (`1020752`). El análisis previo está en `HU-07_Examen_Analisis.md`.

## Cómo se juega

- Al recoger una evidencia aparece «[E] Examinar» junto a la retícula unos segundos.
- En el expediente [TAB] o en la libreta [Q], [E] o el botón [EXAMINAR] abre la evidencia elegida. Una entrada de detalle abre su evidencia enfocando el detalle.
- En la vista, el juego se pausa y se oscurece detrás. Los controles son:
  - Teclado: arrastrar para girar (con inercia), rueda para acercar hacia el cursor (1x a 3x), clic derecho para mover, [R] reiniciar, [F] linterna, [E] examinar el detalle y [Esc]/[TAB] para volver.
  - Control: stick izquierdo, LT/RT, stick derecho, X, Y, A y B.
  - La barra de abajo cambia sola entre teclado y control.
- Cuando un detalle queda a la vista (ángulo, acercamiento y, si hace falta, linterna), sale la lupa en la esquina y suena un «tic». Con [E] la cámara se acerca sola, Caneloso lo interpreta y se crea la entrada nueva.
- La primera vez sale la ayuda «Arrastra para girar el objeto».

## Archivos

**Nuevos**
- `game/Escenas/Investigacion/VistaExamen.tscn`, `VistaExamen.cs`, `examen_fondo.gdshader`: la vista de examen.
- `game/Escenas/Investigacion/DetalleOculto.cs`: recurso de un detalle oculto.
- `game/Datos/Detalles/*.tres`: los 6 detalles.
- `game/Escenas/Caso1/Caso1.Partida.cs`: guardado del campamento.
- `game/Assets/Caso1/Texturas/foto_grupo_hd.svg`: la foto del grupo con más detalle.
- Herramientas:
  - `game/Herramientas/probar_examen.gd`: prueba automática de CA-01 a CA-04.
  - `game/Herramientas/capturas_examen.gd`: capturas de la vista.
  - `game/Herramientas/medir_pistas.gd`: medidas para colocar los marcadores.
- `HU-07_Examen_Analisis.md` y este documento.

**Modificados**
- `game/hud/pista.gd`: campos `modelo`, `descripcion_ampliada`, `lugar`, `examinada`, `origen_id` y `detalles`.
- `game/hud/hud.gd`: estados en el expediente, la libreta y el post-it, contador, botón [EXAMINAR], aviso «[E] Examinar» y detalles colocados bajo su evidencia. `detalle_solicitado` ya no se emite al abrir con TAB: se emite con [E] o [EXAMINAR].
- `game/Escenas/Interrogatorio/ObjetoPista.cs`: exports `ModeloExamen`, `DescripcionAmpliada`, `Lugar`, `GiroExamen` y `Detalles`.
- `game/Escenas/Interrogatorio/Jugador.cs`: abre la vista, registra los detalles, señales `EvidenciaExaminada` y `DetalleDescubierto`, y restauración al cargar.
- `game/Escenas/Investigacion/Sonidos.cs`: 6 sonidos del examen (archivos nuevos en `game/Assets/Audio/Examen/`).
- Caso 0:
  - `Caso0.cs`: detalle de la grabadora, deducción y respuestas de Porky y el comisario.
  - `Caso0.Partida.cs`: guarda el estado de examen.
  - Escenas: textos ampliados, lugar y la etiqueta bajo la grabadora.
- Caso 1:
  - `Caso1.cs`: textos sin adelantar los detalles, notas de los detalles, guardado y «Miaurio».
  - Escenas: textos, detalles y marcadores. Se quitaron los juncos visibles de las botas.
  - `fotografo_pistas.gd`: puede rehacer solo algunas fotos.
  - Las 4 fotos que cambiaron.
- `HU-20_Caso0_Historia_Dialogos.md` y los créditos de assets y audio.

## Detalles ocultos

| Evidencia | Detalle | Cómo se descubre | Entrada nueva | Dónde se usa |
|---|---|---|---|---|
| Grabadora en REC (Caso 0) | Etiqueta escrita a mano: «Sala 1. NO DETENER hasta el cambio de turno. — B.» | Girarla hasta verla por debajo y acercarse a 1.5x | `grabadora.etiqueta` | Tablero: con la nota del comisario da la deducción de la trampa. Interrogatorios: Porky y el comisario responden. |
| Libro de guardia quemado | «De guardia: monitor auxiliar Antonio R.» | Girar las hojas hasta el reverso y acercarse a 2x | `libro_guardia_quemado.antonio` | Expediente, libreta y tablero |
| Foto del grupo | Dedicatoria: «Cabaña 4, 14 de agosto. Para Daniel… — A. R.» | Voltear la foto hasta el reverso | `foto_grupo.dedicatoria` | Expediente, libreta y tablero |
| Foto del grupo | Iniciales «A. R.» en el silbato del monitor | Acercarse al frente a 2.4x | `foto_grupo.silbato` | Expediente, libreta y tablero |
| Registro de la enfermería | «23:20, ruido en el lago» bajo el tachón | Linterna [F] y acercarse a 1.4x | `registro_enfermeria.tachon` | Expediente, libreta y tablero |
| Botas de hule con barro | Juncos del lago en la suela | Girarlas hasta ver la suela y acercarse a 1.4x | `botas_tony.juncos` | Expediente, libreta y tablero |

Las otras 15 evidencias no tienen detalle. El Caso 1 todavía no tiene interrogatorios ni deducciones. Cuando lleguen, las entradas nuevas ya salen como cartas y en «Mostrar una prueba».

## Pruebas

`Herramientas/probar_examen.gd` recoge todas las evidencias de un caso y comprueba cada criterio. También guarda y carga sin tocar las ranuras del jugador: las respalda y las devuelve.

```
--headless --path game --script res://Herramientas/probar_examen.gd -- caso0   → RESULTADO caso0: OK
--headless --path game --script res://Herramientas/probar_examen.gd -- caso1   → RESULTADO caso1: OK
```

- **CA-01:** las 8 evidencias del Caso 0 y las 12 del Caso 1 abren la vista con su modelo. El juego se pausa y se reanuda al cerrar.
- **CA-02:**
  - Recoger nunca crea entradas de detalle.
  - Al abrir la vista ningún detalle está a la vista.
  - Cada detalle aparece al girarlo y acercarlo.
- **CA-03:**
  - Cada detalle crea su propia entrada, vinculada a su origen.
  - Volver a examinar no lo ofrece otra vez.
  - Las entradas no se duplican (9 en el Caso 0 y 17 en el Caso 1).
- **CA-04:**
  - Recién recogidas están «sin revisar». Después pasan a «examinada» o «con detalle».
  - El contador muestra EXAMINADAS 8/8 y 12/12.
  - Cada detalle queda bajo su evidencia.
  - Al cargar vuelven las mismas entradas, en el mismo orden y con los mismos estados.

Además:
- `dotnet build` compila sin errores.
- Las escenas del juego, del campamento y del menú cargan en headless con los mismos avisos que la rama base. No hay errores nuevos.

## Rendimiento

Con la vista abierta, el juego de fondo se fotografía una vez y deja de renderizarse el 3D. La vista mide unos 0.8 ms de GPU y la pantalla 0.3 ms.

En esta computadora el juego corre a 30 FPS con y sin la vista, sin vsync. El límite viene del equipo, no de la vista: hay que comprobar los 60 FPS en una computadora que ya llegue a 60 jugando.

## Sonidos

Abrir, cerrar, girar, acercar, el «tic» de un detalle a la vista y descubrir son de Kenney (RPG Audio, Interface Sounds y Music Jingles, CC0), en `Assets/Audio/Examen/` y con sus créditos en `CREDITOS_AUDIO.md`.
