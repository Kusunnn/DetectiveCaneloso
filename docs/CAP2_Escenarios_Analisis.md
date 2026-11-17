# Capítulo 2 «La Trampa de la Garrapata» · Análisis de escenarios

Rama `Escenarios-Capitulo-2`, creada desde `main` (2f3f9f1) el 17 de noviembre de 2026.
Fuente de verdad: `docs/historias/Capitulo_2_La_Trampa_de_la_Garrapata.docx` (versión del 12 de noviembre, de @Yaya).

Este documento es solo el análisis (pasos 00 y 0). **No se ha construido nada.** Espera tu aprobación.

---

## 0. Resumen para decidir rápido

| # | Decisión que necesito | Mi recomendación |
|---|---|---|
| D1 | `caso_02.tres` ya existe en `main` y cuenta **otra historia** («El último testigo», hospital San Judas, muere Tony). | No lo toco en esta rama. Que quien lleve el expediente lo reescriba para «La Trampa de la Garrapata». |
| D2 | La comisaría del Caso 0 está hecha casi toda con **cajas y cilindros** (39 `BoxMesh` y 13 `CylinderMesh` solo en la recepción). Reutilizar «sus modelos» choca con la regla 3 (cero primitivas visibles). | Reutilizo la **distribución, medidas, paleta y componentes**, pero armo los escritorios con modelos de librería (Kenney, KayKit, Poly Haven). |
| D3 | El documento **no trae un «Título»** por pista y la P01 se describe de tres formas distintas. Además tiene erratas («tomo», «versione», «alguen»). | Uso los títulos de la tabla de la sección 4.1 y copio las erratas **corregidas solo en ortografía** (tilde y letras), sin cambiar palabras. Si prefieres copia literal con erratas, me dices. |
| D4 | `ObjetoDecorativo` (HU-21) **no está en `main`**: vive en la rama local `HU-21-Detalles-ambientales` (30c720f), sin subir. | Construyo con marcadores `Decorativo_*` listos para convertirse. Si subes HU-21 antes de que llegue a esa parte, uso el componente real. |
| D5 | Pulgas no tiene modelo. | Marcador temporal sobre el maniquí de Quaternius (CC0, ya está en el repo), con gorra, cubrebocas y uniforme. Nodo marcado `MARCADOR_TEMPORAL`. |
| D6 | Animación de «sentado». | Hay animaciones de sentarse en la Universal Animation Library de Quaternius (ya en el repo). Probaría sentar a Miawrio; si se ve mal, queda de pie junto a la silla. |

---

## 1. Paso 00 · Estado del proyecto

| Punto | Resultado |
|---|---|
| AGENTS.md | No está en el repo. Hay uno **sin seguimiento** en tu checkout principal y lo leí: las reglas son las mismas que ya sigo (código en español, LF, no editar `.godot` ni `.uid`, capas de física, ObjetoPista y HUD en GDScript). |
| `git status` | Esta copia (worktree) estaba limpia. Tu checkout principal (rama `Carteles-Capitulo-1`) tiene 17 `.import` modificados y 3 archivos sin seguimiento (`.codex/`, `AGENTS.md`, `Caso0_Historia_Expediente.pdf`). **No los toqué.** |
| `fetch` + `main` | `origin/main` en 2f3f9f1 («Merge pull request #36 from Kusunnn/HU-30-HU-33»). |
| Ramas de compañeros | Revisé las ramas con actividad de las últimas 4 semanas (HU-18, HUE-03, HU-31, HU-29, HU-30-HU-33, Carteles-Capitulo-1 y otras). **Ninguna** tiene escenas, diálogos ni `capitulos.json` del Capítulo 2. HU-18 («miawrio», Mario, 16-nov) solo cambia 2 líneas de `PantallaCarga.cs` y ya está en `main`. |
| Lo único del Cap. 2 que existe | `game/hud/expediente/casos/caso_02.tres` (LostPain25, 9-oct, retocado el 26-oct) con la historia vieja «El último testigo». En `capitulos.json`, `capitulo2` sigue «en preparación» y sin escena. Ver D1. |
| Rama nueva | `Escenarios-Capitulo-2` desde `origin/main`, sin upstream hasta el push final. |
| Historia en el repo | Copié el docx que adjuntaste a `docs/historias/Capitulo_2_La_Trampa_de_la_Garrapata.docx` (idéntico al de tu carpeta de Descargas, mismo SHA-256). Aún sin commit. |
| `dotnet build` | 0 errores, 0 advertencias (10 s). |
| Godot sin ventana | El worktree tenía la caché de importación incompleta. Tras `--import` (75 s), `menu_inicio`, `sala_interrogatorio`, `escenario_campamento` y `escenario_lago` cargan con **0 errores y 0 advertencias**. Ningún `.import` quedó modificado. |

---

## 2. Qué se reutiliza

### 2.1 Componentes (todos genéricos, sin lógica de caso)

| Componente | Dónde | Uso en el Cap. 2 |
|---|---|---|
| `ObjetoPista` | `Escenas/Interrogatorio/ObjetoPista.cs` | Las 8 pistas. Campos: `Id`, `Titulo`, `Descripcion`, `ComentarioAlMirar`, `Lugar`, `Imagen`, `ModeloExamen`, `GiroExamen`, `DescripcionAmpliada`, `Detalles` (DetalleOculto). La invitación también, con `EsEvidencia = false`. |
| `Contenedor` | `Investigacion/Contenedor.cs` | Cajón del expediente (P08), archivero, cajones de escritorio. `TipoMovimiento = Deslizar`, `MensajeBloqueado` y `Comentario` propios. La pista de dentro empieza sin colisión y el guion la activa al abrir (como en el Caso 1). |
| `Puerta` | `Investigacion/Puerta.cs` | Puertas de escaleras, cocina, basura y las `PuertaTito_N` (con `MensajeBloqueada` y el piloto que se apaga al desbloquear). |
| `PuntoInteraccion` | `Investigacion/PuntoInteraccion.cs` | Salida «Ir a la Torre Miawrio», elevador, panel de tarjeta, pantallas de Tito, servidor. Solo emite `Usado`. |
| `Npc` + `AnimacionPersonaje` / `AnimadorPersonaje` | `Investigacion/` | Miawrio, Aya, Mimita, Hecdog, recepcionista y técnicos. Reposo, mirar al jugador, objeto en la mano, ánimo «alterado» para el estado hackeado. |
| `ObjetoDecorativo` (HU-21) | **solo en la rama local `HU-21-Detalles-ambientales`** | Ver D4. Campos: `Id`, `Zona`, `Comentarios[]`, `TipoInteraccion`, `Reaccion`, `MovimientoContinuo`, sonido propio. Comentarios en `Datos/Dialogos/decorativos.json` con `"nueva": true`. |
| `LuzParpadeante` | `Investigacion/` | Luces de emergencia y la fachada de noche. |
| `SonidoAmbiente` | `Investigacion/` | Un nodo por espacio (bucle o con pausas). |
| `CartelPared` | `Investigacion/` | Letreros de piso, directorio del lobby, logo, avisos de escaleras. Ya ajusta la letra a la caja (`TamFuente` → `TamMinimo`) y tiene estilos Madera, Placa, Papel y Pizarrón (estos dos últimos están en la rama `Carteles-Capitulo-1`, no en `main`). |
| `SembradoMultiMesh` | `Investigacion/` | Repetidos: plantas, libros del librero, coches del estacionamiento, edificios de la vista de ciudad. |
| `VistaExamen` | `Investigacion/` | Examen de pistas con reverso (P01) y detalles ocultos. |

### 2.2 Herramientas

| Herramienta | Qué hay que adaptar |
|---|---|
| `Herramientas/AuditoriaEscenario.gd` y `auditar.gd` | Ya aceptan la escena por argumento. Sirven tal cual (flotantes, encimados, mallas sin colisión, pistas inalcanzables). |
| `Herramientas/Caso1/probar_pistas.gd` | Solo conoce Caso 0, Campamento y Lago. Le agrego un modo `solo-caso2` con la lista de escenas del capítulo. Respalda las partidas antes de empezar. |
| `Escenas/Caso1/fotografo_pistas.gd` | Tiene la escena del Campamento fija. Le agrego `-- caso2`, que escribe en `Assets/Pistas/caso2/<id>.png`. Las fotos se toman al final, con el modelo definitivo. |
| `Herramientas/Caso1/capturas_escenario.gd` | Ya recibe escena, carpeta y vistas `nombre=px,py,pz,tx,ty,tz`, y escribe `rendimiento.txt`. Le agrego `--estado=hackeado`, que llama a la función de luces antes de capturar. |
| `Herramientas/medir_rendimiento.gd` | Tal cual (CPU y GPU por cuadro, sin vsync). En tu PC sale 30 FPS fijos: reporto ms de CPU y GPU, no FPS, y con la ventana al frente. |
| `probar_escenarios.gd` y `probar_ca03.gd` | Solo para comprobar que el Caso 0 y el Caso 1 siguen igual. `probar_ca03` completo tarda ~20 min y pesa en memoria: lo corro por partes (`solo-caso0`, `solo-sala`…). |

### 2.3 Cómo se conectan las escenas en el Caso 1 (lo mismo para el Cap. 2)

- Cada espacio es una escena con su `Jugador`, HUD y un nodo de guion (`Caso1`, `Caso1Lago`). Se cambia con `GestorPartida.Instancia.CambiarEscenaDelCapitulo(escena, punto)`: guarda el estado (`CapturarEstado`), pone la pantalla de carga con fundido y deja al jugador en el `Marker3D` con ese nombre (`PartidaEscenas.ColocarEnEntrada`).
- El estado por escena se guarda bajo la ruta de su `.tscn` (`PartidaEscenas.Escenas`), así cada lugar queda como se dejó. Las pistas de otro escenario vuelven al diario con `RestaurarPistaDeOtroEscenario` (foto en `Assets/Pistas/caso1/<id>.png`).
- **Para el Cap. 2** propongo una clase `PartidaEscenasCaso2` igual (rutas de las escenas, `TotalPistas = 8`), una clase base de escenario con el cambio de escena y los puntos de entrada, y **ningún guion**: los nodos de mecánicas quedan con nombre y sin lógica. Las transiciones se dejan cableadas con fundido para poder recorrer el capítulo.
- **Acceso de prueba:** un botón «Probar Capítulo 2 · <espacio>» en el menú de depuración (como «Probar Escenario 1»), que abre cada escena sin pasar por el Caso 0 ni el Caso 1. No toca `capitulos.json`.

### 2.4 Modelos y packs que ya están en el repo

| Pack | Carpeta | Licencia | Útil para el Cap. 2 |
|---|---|---|---|
| Kenney Furniture Kit (parcial, 23) | `Assets/Caso1/Modelos/muebles` | CC0 | `desk`, `chairDesk`, `bookcaseOpen`, `bookcaseClosedDoors`, `radio`, `lampRoundFloor`, `lampSquareCeiling`, `sideTableDrawers`, `coatRack`, `trashcan`, `cardboardBox*`, `kitchen*` |
| KayKit Furniture Bits (parcial, 26) | `kaykit_muebles` | CC0 | `armchair`, `table_low`, `lamp_table`, `lamp_standing`, `pictureframe_*`, `book_set`, `shelf_*`, `rug_*`, `pillow_A`, `cabinet_*` |
| KayKit Restaurant Bits (31) | `kaykit_restaurante` | CC0 | Cocina completa: `kitchencounter_*`, `stove_multi`, `oven`, `pot_*`, `pan_*`, `fridge_A`, `extractorhood`, `crate*`, `dishrack_plates` |
| Kenney Food / Survival / Car | `comida`, `supervivencia`, `coches` | CC0 | `cup-coffee`, `mug`; `barrel`, `box*`, `bucket`, `metal-panel`, `structure-metal-*`; `sedan` (solo uno) |
| Quaternius Survival | `quaternius` | CC0 | `phone`, `radio`, `trashcan`, `can_*`, `battery_big` |
| Poly Haven | `Assets/Cinematicas/Modelos/polyhaven` | CC0 | `desk_lamp_arm_01`, `alarm_clock_01`, `office_notepads`, `WoodenTable_01`, `magnifying_glass_01` |
| Texturas de pistas | `Assets/Pistas/Modelos/texturas` | CC0 (ambientCG) | `Paper005` para papeles; generadores en `Herramientas/Pistas/` (`generar_modelos.gd`, `generar_texturas.gd`, `mallas.gd`) que ya hacen papel con grosor y reverso |
| Animaciones Quaternius UAL 1 y 2 | `Assets/Animaciones/Quaternius` | CC0 | 88 animaciones (reposo, sentarse, hablar, teléfono, brazos cruzados). `AnimadorPersonaje` las pasa a los esqueletos de los personajes |

### 2.5 Personajes: estado de los modelos

| Personaje | Modelo | Animaciones | Estado | Riesgo para el Cap. 2 |
|---|---|---|---|---|
| Miawrio | `Models/Miaurio/miaurio.tscn` (FBX, esqueleto Mixamo) | `Inactivo`, `Caminar`, `Tenso`, `Alterado` | Sin pose T; texturas y lentes bien en la sala del Caso 1 (captura `docs/HU-9.5_CA03_capturas/revision_final/sala_Miawrio_frente.jpg`). | Corbata y reloj no existen en el modelo: «se acomoda la corbata» queda como gesto. |
| Aya | `Models/Aya/Ayas.tscn` | `Inaya`, `Caminaya`, `Tensaya`, `Alteraya` | Sin pose T. **Su textura se ve pixelada** de cerca (`sala_Aya_frente.jpg`). | La cámara en la mano necesita modelo (no hay uno en el repo). Revisar el filtro de la textura antes de las capturas. |
| Mimita | `Models/Personajes/mimita_rig.glb` (re-riggeada en HU-9.5) | Por código (`AnimacionPersonaje`) + UAL | Sin pose T desde HU-9.5 (`mimita/despues_reposo.jpg`). Ojos y texturas bien. | Ninguno visible. Revisar pies al estar junto a un sillón. |
| Hecdog | `Models/poli/Poli/Policia.tscn` | `inapoli`, `caminarpoli`, `ten` | Funciona en el Caso 0 (recepción). | Sin modelo de radio de mano: uso `radio` de Quaternius. |
| Caneloso | `Models/Caneloso/Archivo/player.tscn` | Mixamo `anim_*.res` | Primera persona: solo se ve en cinemáticas. | La licencia del modelo sigue «por confirmar» en `CREDITOS.md`. |
| Recepcionista / guardia | **No hay modelo propio.** | | | Propongo reutilizar a Hecdog con otro material (uniforme de seguridad) o un personaje de fondo del Caso 1. Lo decides tú. |
| Pulgas y el otro técnico | **No hay modelo.** | | | Ver 2.6. |
| Tony | `Models/Tony/Tony.tscn` | sí | Solo para la celda (escena de cierre). | |

Los modelos de `Models/Personajes/*.glb` siguen con licencia «por confirmar» en `CREDITOS.md`. No es nuevo, pero lo dejo anotado.

### 2.6 Pulgas sin modelo final

- Marcador temporal hecho con el **maniquí de Quaternius** (CC0, ya en el repo y usado en la cinemática de la cinta): uniforme azul grisáceo con el texto «Servicios Pulgar» en la espalda (calcomanía), gorra y cubrebocas modelados en Blender como piezas aparte, y una calcomanía de tres rayas en el dorso de la mano derecha.
- `Tecnico_A` usa el mismo marcador **sin** las rayas, para que la foto F1 sirva para distinguirlos.
- Los dos nodos llevan el grupo `marcador_temporal` y un comentario `MARCADOR TEMPORAL: falta el modelo final de Pulgas`. No invento cara ni especie.

---

## 3. Lo que dice la historia (y dónde choca con el pedido)

1. **P01:** el pedido dice «foto borrosa de la camioneta negra». El documento lo dice de tres formas: «una foto borrosa de alguen entrando al campamento» (intro), «Foto que tomo Eriz. Camioneta negra y linternas en el campamento sellado» (tabla) y «Dibujo de Eriz» (interrogatorios). **Propuesta:** un sobre con dos piezas, el dibujo a carboncillo de Caneloso y una foto borrosa donde se ven la camioneta negra, linternas y una silueta entrando por el portón con sellos, con la fecha impresa. Así cumple las tres versiones.
2. **La invitación** no es pista (no está en la tabla de 8), pero el texto exacto está en el documento. Va como `ObjetoPista` con `EsEvidencia = false` (se lee, no cuenta).
3. **Fecha de la foto:** el documento dice «una semana antes de la subasta» y que la subasta fue «hace tres semanas», sin fechas. Para la foto propongo que la fecha impresa sea relativa a la del capítulo (los Casos 0 y 1 usan «2 al 3 de septiembre»). **Necesito la fecha** o la dejo como `--/--` borrosa.
4. **Expediente N.º 002:** el documento lo nombra así. Choca con el `caso_02.tres` viejo (D1).
5. **Horas del lobby:** P02 dice «Mimita entró ayer a las 21:00» y «un técnico subió hoy al piso 23». F1 dice 10:42. Uso esas horas en el registro: el técnico entre las 9:00 y las 10:42.

---

## 4. Pistas, puntos de mecánica y espacios

### 4.1 Pistas (texto copiado del documento)

| Id | Título propuesto (D3) | Espacio | Ubicación | Cómo se encuentra |
|---|---|---|---|---|
| `dibujo_eriz` ★ | Dibujo de Eriz | Comisaría | Escritorio de Caneloso, junto a la invitación, dentro del sobre abierto | A simple vista, sobre la mesa. Examen con reverso: «Oye pa, vi gente midiendo el lago.» |
| `registro_accesos` ★ | Registro de accesos | Lobby | Pantalla de registro del mostrador (y libro de visitas al lado como apoyo visual) | Se mira la pantalla: tres líneas resaltadas legibles en la vista de examen |
| `planos_clinica` | Planos de la clínica | Despacho | Mesa de proyectos, desplegados junto a la maqueta | A simple vista. «Para M.» en la esquina, a mano |
| `contrato_ixodes` ★ | Contrato de financiamiento con Ixodes | Despacho | Escritorio, bajo la lámpara, en una carpeta abierta | A simple vista. Firma «Lic. R. Garrido» y fecha |
| `estado_cuenta` ★ | Estado de cuenta | Despacho | Archivero (cajón de en medio, abierto libre) | Abrir el cajón. Ocho renglones con la misma hora |
| `registro_llamadas2` ★ | Registro de llamadas de Miawrio | Despacho | Junto al teléfono del escritorio (hoja impresa) | A simple vista. Las 23:00 subrayadas |
| `abrecartas` | Abrecartas en forma de garra | Despacho | Escritorio, en su base de cuero, con la tarjeta | A simple vista |
| `expediente_filtrado` (opcional) | Copia del expediente de Pino Lunar | Despacho | Cajón inferior del escritorio de Miawrio (`Contenedor`) | Abrir el cajón. Sello de la Fiscalía |

`Descripcion` = el texto de la columna «Pista» del documento, sin el número ni la estrella. `ComentarioAlMirar` = el comentario de Caneloso, completo y sin el paréntesis de la P05. `Lugar` = el nombre del espacio.

**Distancia:** en el despacho ninguna pista queda a más de 6 m de otra. A 3 m/s (velocidad del jugador) son 2 s de camino.

### 4.2 Puntos preparados para las mecánicas (sin lógica)

| Nodo | Escena | Qué es |
|---|---|---|
| `PuntoInterrogatorio` | despacho | `Node3D` con `PuntoSilla` (silla del implicado), `PuntoCaneloso` (silla de enfrente), `PuntoEntradaImplicado` y `CamaraPlanoMedio` (`Marker3D` a 1.1 m de altura y ~1.6 m del implicado, FOV 46–52°, igual que la sala del Caso 1). |
| `PuntoTablero` | despacho | `Marker3D` frente a la mesa de proyectos, donde Caneloso «arma» la deducción. |
| `PuntoEspera_Mimita`, `PuntoEspera_Hecdog`, `PuntoAya` | despacho | Dónde esperan. |
| `Elevador` | lobby, despacho, piso 23 | `PuntoInteraccion` + puertas que se abren y un indicador de piso (`CartelPared` tipo placa). |
| `PanelTarjeta` | escaleras | Lector de tarjeta con piloto rojo/verde (`PuntoInteraccion`). Lo abre K1. |
| `PuertaTito_1…4` | escaleras, piso 23, cocina, basura | `Puerta` bloqueada + `PantallaTito` (malla con las tres rayas rojas) + `PuntoInteraccion`. |
| `Cruce_1…4` | escaleras, cocina, basura, estacionamiento | `Node3D` con `Rastro` (aceite, puerta que se mueve, huellas mojadas) y `Camino_A`/`Camino_B` (`Marker3D`). |
| `ServidorDisco` | piso 23 | Bahía del servidor donde va el disco (malla del disco aparte, para quitarla). |
| `Tecnico_A`, `Tecnico_B` (Pulgas) | estacionamiento | Marcadores temporales (2.6). |
| `Patrulla` | estacionamiento | Coche de policía al pie de la rampa, con `PuntoEntradaPatrulla` arriba de la rampa. |
| `LucesNormal`, `LucesHackeado` | despacho, piso 23, escaleras | Dos grupos de luces. Un script pequeño `EstadoLuces` con `FijarEstado(Estado.Normal / Estado.Hackeado)` y la señal `EstadoCambiado`. |
| `Entrada_*`, `Salida_*` | todas | `Marker3D` de llegada y `PuntoInteraccion` de salida. |

---

## 5. Planos vistos desde arriba

Escala de todos los planos: **1 carácter = 0.5 m** en horizontal y 1 renglón = 0.5 m. Norte arriba. `J` = donde aparece el jugador, `→` = camino sugerido, `P0n` = pista, `◇` = decorativo de HU-21, `■` = mueble, `▒` = ventana, `╪` = puerta.
Todas las puertas miden 0.9 × 2.1 m y los pasillos al menos 1 m.

### 5.1 Comisaría por la mañana · `comisaria_manana.tscn` (8 × 6 m, techo 2.8 m)

Escena nueva. Solo la zona de los dos escritorios. Ningún nodo, script ni pista del Caso 0.

```
 0         2         4         6         8 m
 ┌▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒┬─────────────────────────────┐ 0
 │ persianas (luz de día entra en franjas)      │
 │  ■■■■■■■■ ◇radio      ■■■■■■■■  ◇cafetera   │
 │  ■ESCRIT.■ P01        ■ESCRIT.■             │ 2
 │  ■CANELOSO■ invitación ■HECDOG ■  Hecdog(de │
 │    silla                  silla    pie)     │
 │                                              │ 3
 │  ■archivero      ◇corcho con recortes        │
 │                  → → → → →                   │ 4
 │  perchero  J ← entrada                       │
 │                              ╪ «Ir a la     │ 5
 │  ◇planta   ◇bote basura        Torre Miawrio»│
 └──────────────────────────────╪──────────────┘ 6
```

- **Muebles:** 2 `desk` (Kenney) con `chairDesk`, `sideTableDrawers`, `bookcaseClosedDoors` como archivero, `coatRack`, `trashcan`, planta (Poly Haven), `radio` (Kenney) en el escritorio de Caneloso, `mug` + `cup-coffee` con vapor (partículas), pilas de papeles (`office_notepads`), `desk_lamp_arm_01`.
- **Pistas:** P01 sobre el escritorio de Caneloso. La invitación, a su lado (no evidencia).
- **Personaje:** Hecdog de pie junto a su escritorio, con taza en la mano.
- **Decorativos (≥ 2):** radio de policía (sonido de estática), cafetera con vapor, corcho con recortes.
- **Salida:** la puerta de la derecha, `PuntoInteraccion` «Ir a la Torre Miawrio».

### 5.2 Torre Miawrio · Lobby · `torre_lobby.tscn` (12 × 10 m, techo 4 m)

```
 0     2     4     6     8    10    12 m
 ┌▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒╪╪▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒┐ 0  ventanales a la calle (mañana)
 │ ◇planta       J (entra por        ◇planta│
 │               la puerta giratoria)      │
 │  sillones ■■   ↓                 ■■     │ 2
 │  ■mesa■  ■■   ↓      pantalla    ■■     │
 │               ↓      noticias ◇         │ 4
 │          ┌────────────┐                 │
 │          │ MOSTRADOR  │ ← P02 pantalla  │ 6
 │          │ mármol+mad.│   de registro   │
 │          └────────────┘ recepcionista   │
 │   LOGO DORADO EN LA PARED DEL FONDO     │ 8
 │  ┌──╪──┐  ┌──╪──┐  directorio de pisos  │
 │  │ELEV1│  │ELEV2│  (CartelPared)        │
 └──┴─────┴──┴─────┴──────────────────────┘ 10
```

- **Camino:** entrada → mostrador (5 m, 2 s) → elevadores (4 m).
- **Pista:** P02 en la pantalla del mostrador (lado del visitante, inclinada 15°). Libro de visitas abierto al lado como decorativo.
- **Elevador:** `ELEV2` es el interactivo (transición al piso 30). `ELEV1` con su número de piso cambiando (decorativo).
- **Decorativos:** pantalla de noticias, planta grande, directorio de pisos.

### 5.3 Torre Miawrio · Despacho del piso 30 · `torre_despacho.tscn` (14 × 10 m, techo 3.4 m)

```
 0     2     4     6     8    10    12    14 m
 ┌▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒┐ 0  ventanal: ciudad desde el piso 30
 │ ◇torre    ┌──────────────┐        ◇pecera│
 │  gato     │  ESCRITORIO  │ silla           │
 │           │ P04 P06 P07  │ Miawrio         │ 2
 │  Aya +    │ lámpara tel. │                 │
 │  tripié   └──────────────┘  cajón P08      │
 │  (rincón)        ↑                         │ 4
 │                  ↑     ┌───────────┐       │
 │ ■LIBRERO■        ↑     │ MESA DE   │PuntoTablero
 │ ■trofeos■  ┌─alfombra─┐│ PROYECTOS │       │ 6
 │ ■ARCHIV.■P05 sillón  ││ maqueta   │       │
 │           │ mesa baja ││ P03 planos│       │
 │           │ café+gall.│└───────────┘       │ 8
 │  Mimita   │ sillón    │  PuntoInterrog.    │
 │  Hecdog   └───────────┘  (2 sillas)  ◇planta
 │  (espera)       ← ← J ╪ elevador           │ 10
 └───────────────────────╪──────────────────┘
```

- **Camino:** elevador → zona de estar (3 m) → escritorio (6 m) → mesa de proyectos (4 m) → librero/archivero (8 m). Todo el cuarto se cruza en 5 s.
- **Pistas:** P04, P06 y P07 en el escritorio (separadas 40–60 cm para que no se tapen); P03 en la mesa de proyectos; P05 en el archivero; P08 en el cajón inferior del escritorio.
- **Interrogatorio:** dos sillas frente a frente al sur de la mesa baja, a 1.6 m entre respaldos. La cámara de plano medio queda a la altura de los ojos de Caneloso, sin pegarse a las cabezas.
- **Personajes:** Miawrio de pie junto a su silla (o sentado si funciona la animación, D6); Aya en el rincón noroeste con cámara y tripié; Mimita y Hecdog esperando junto al librero.
- **Decorativos (≥ 2):** torre para gato, pecera con peces que se mueven, trofeos, galletas.

### 5.4 Escape (escenas cortas)

**Escaleras de emergencia · `torre_escaleras.tscn` (3 × 6 m de planta, 3 tramos, ~9 m de alto)**
```
 piso 30 ╪ PanelTarjeta (K1)
   ┌────────┐
   │ tramo1 │↓  letrero «30»
   │descanso│ ← Cruce_1 (aceite de máquina en el escalón)
   │ tramo2 │↓  letrero «29…24» (salto con fundido)
   │descanso│ ← PuertaTito_1
   │ tramo3 │↓  letrero «23»
   └────────┘╪ piso 23
```
Barandal metálico, luz de emergencia roja por descanso, letreros de piso con `CartelPared`.

**Piso 23 · Cuarto del servidor · `torre_piso23.tscn` (8 × 6 m + pasillo de 8 × 1.5 m)**
```
 0       2       4       6       8 m
 ┌─────────────────────────────────┐ 0
 │ ■RACK■ ■RACK■ ■RACK■ ■RACK■      │   luces verdes y ámbar
 │                                  │ 2
 │ ■RACK■ ■RACK■  ServidorDisco ■■  │   rejillas de aire frío
 │   cables en charola               │ 4
 │                       PuertaTito_2│
 ├▒▒▒▒▒▒▒▒▒▒ vidrio ▒▒▒▒▒▒▒▒▒▒▒▒▒╪──┤ 6  (el reflejo de la foto de Aya)
 │ J ← escaleras   pasillo  → cocina│ 7.5
 └──────────────────────────────────┘
```

**Cocina del edificio · `torre_cocina.tscn` (6 × 5 m)**: mesa de acero al centro (`kitchentable_A_large` con material metálico), estufas, ollas y sartenes (KayKit), refrigerador, campana. `Cruce_2` = la puerta abatible que todavía se mueve. `PuertaTito_3`.

**Cuarto de basura · `torre_basura.tscn` (4 × 4 m)**: 2 contenedores grandes, bolsas, cajas aplastadas, piso de concreto mojado. `Cruce_3` = huellas mojadas hacia la puerta de servicio. `PuertaTito_4`.

**Estacionamiento · `torre_estacionamiento.tscn` (20 × 12 m, rampa de 4 × 10 m)**
```
 0    4    8    12   16   20 m
 ┌────────────────────────┬────┐ 0
 │ coche coche coche coche│RAMPA ← Patrulla entra por aquí
 │  ●col      ●col       ●│ ↑  │
 │ coche     Tecnico_A    │ ↑  │ 6
 │  ●col  Tecnico_B(Pulgas)│ ↑ │
 │ coche coche coche coche│    │
 │ J ╪ puerta de servicio  │   │ 12
 └────────────────────────┴────┘
```
`Cruce_4` en la puerta de servicio (huellas y aceite). Columnas cada 6 m con número de cajón pintado.

### 5.5 Cierre (lo mínimo)

- **Fachada de noche · `torre_fachada_noche.tscn`:** una banqueta de 16 × 4 m, la base de la torre (vidrio, logo dorado, puerta giratoria), banca, poste de luz, planta. Un `Marker3D` `Encuadre_MiawrioMimita` y otro `Encuadre_Aya`. Las luces de la fachada con `LuzParpadeante`.
- **Celda de Tony · `penal_celda.tscn` (3 × 4 m):** catre (`bedSingle` con material gris), mesa, banco, ventana alta con barrotes (vista de muro y cielo de tarde), libro (`book_set`), puerta de barrotes.
- **Embarcadero · `embarcadero_sellado.tscn`:** escena **nueva** que instancia `Lago/zona_embarcadero.tscn` y `zona_lago.tscn` (agua) sin el nodo `Caso1Lago` ni el resto del Lago. Encima: sellos rojos rotos en los postes y cinta amarilla deshilachada (modelo con malla de tiras, no plano). `escenario_lago.tscn` no se toca. Noche, 23:00.

---

## 6. Paleta y luces

Todos los espacios son del mismo edificio: madera de nogal, latón y verde botella en las zonas de Miawrio; mármol crema en el lobby; concreto y acero en los cuartos de servicio. Hackeado = frío azulado + rojo.

| Espacio | Paleta normal | Luz principal | Relleno | Contraluz | Hackeado |
|---|---|---|---|---|---|
| Comisaría (mañana) | Madera miel `#B07A45`, verde oliva `#6F7A4B`, papel `#E9DFC7`, piso `#5A4636` | Sol por las persianas (`DirectionalLight3D` con sombra, cálido `#FFE2B0`, rayos en franjas con un cookie) | Lámpara de escritorio ámbar (sin sombra) | Ventana del fondo, cielo de mañana | — |
| Lobby (mañana) | Mármol crema `#E8DCC4`, nogal `#6B4527`, dorado `#C9A04A`, verde planta `#4F7A4A` | Sol por los ventanales (con sombra) | Plafones cálidos `#FFD9A0` (sin sombra) | Pantalla de noticias (azul suave) | — |
| Despacho (mañana) | Nogal `#5C3A22`, verde botella `#2F5D4A`, latón `#B8913F`, alfombra vino `#7A2E35`, crema `#EADBC0` | Ventanal, luz de día lateral (con sombra) | Lámpara del escritorio y de pie, ámbar | Pecera y monitor (cian suave) | Persianas metálicas abajo, plafones apagados. **Principal:** luces de emergencia rojas `#FF2A2A` (2 con sombra suave). **Relleno:** pantallas con tres rayas, luz fría `#7FA8FF`. **Contraluz:** la pecera, que sigue prendida. Ambiente mínimo 0.15 para que nunca sea negro. |
| Escaleras | Concreto `#8A8A85`, amarillo seguridad `#E3B23C` | Plafón por descanso (blanco cálido) | Ambiente | Letrero de salida verde | Rojo de emergencia por descanso + piloto del panel |
| Piso 23 | Gris acero `#4A5058`, azul servidor `#2B6CB0`, LED verde `#3DDC84` | Plafones fríos | LEDs de los racks (emisivos) | Pasillo a través del vidrio | Racks con LED rojo, pantalla de Tito, rojo de emergencia |
| Cocina / basura | Acero `#A9AEB2`, azulejo blanco, piso de concreto | Plafón fluorescente | Ambiente | Luz de la puerta de servicio | (solo si hace falta) |
| Estacionamiento | Concreto `#7D7A73`, amarillo de líneas, columnas con franja | Lámparas tubulares cada 6 m (sin sombra, una principal con sombra) | Ambiente | Luz de día que baja por la rampa | Rojo intermitente en la salida |
| Fachada (noche) | Azul noche `#1C2440`, dorado del logo, ámbar de farolas | Farola (con sombra) | Vitrinas del lobby | Luna/cielo | — (parpadeo) |

---

## 7. Ventanas con vista

- **Comisaría:** persianas medio abiertas; detrás, calle de mañana con fachadas low-poly a 10–30 m (Kenney City Kit, ver 8).
- **Lobby:** calle con banqueta, coches estacionados, edificios de enfrente y cielo de mañana.
- **Despacho (piso 30):** ciudad desde arriba en tres capas: azoteas cercanas a 40–80 m, edificios medianos a 150–300 m y una silueta lejana (tarjeta pintada con niebla). Todo con `SembradoMultiMesh` y sin colisión.
- **Celda:** muro del penal y cielo.
- Cielo con `ProceduralSkyMaterial` ajustado a la hora (mañana o noche).

---

## 8. Assets nuevos y de dónde salen

Todo CC0. Se anotan en `game/Assets/Caso2/CREDITOS_ASSETS.md` y `game/Assets/Audio/CREDITOS_AUDIO.md`. Los nombres exactos de cada modelo se confirman al descargar.

| Qué | Fuente propuesta | Licencia |
|---|---|---|
| Kit modular de interiores (pared, piso, techo, zócalo, ventana, puerta) | Kenney **Building Kit** o, si no cuadra la escala, un kit propio en Blender con módulos de 1 m (una sola pieza por tipo, con UV para texturas ambientCG) | CC0 |
| Muebles de oficina que faltan (monitor, teclado, teléfono, sofá, mesa de centro, plantas, libros, televisión) | Kenney **Furniture Kit** completo (ya tenemos 23 de ~140) y KayKit **Furniture Bits** completo | CC0 |
| Plantas grandes, sillones finos, marcos, jarrones | Poly Haven (modelos) | CC0 |
| Coches variados y patrulla | Kenney **Car Kit** (sedan, SUV, van, policía, taxi) | CC0 |
| Edificios para las vistas y la fachada | Kenney **City Kit (Commercial)** | CC0 |
| Racks de servidor, cables, charolas | Kenney **Space Station Kit** o Quaternius **Sci-Fi Essentials**; si ninguno se ve como rack de oficina, modelo propio en Blender | CC0 |
| Contenedores de basura, bolsas, conos | Kenney **City Kit (Roads)** / Quaternius | CC0 |
| Texturas (mármol, madera, alfombra, concreto, acero) | ambientCG | CC0 |
| Hechos en Blender (no hay en librerías) | Maqueta del hotel, abrecartas de tres puntas, torre para gato, pecera, logo dorado, cámara con tripié de Aya, gorra y cubrebocas de Pulgas, cinta amarilla deshilachada, sellos rotos | propios |
| Papeles de las pistas | Generadores de `Herramientas/Pistas/` (papel con grosor de 1 mm, curvatura y reverso) + `Paper005` | CC0 |
| Fuentes | Las que ya están: Special Elite (formularios), Caveat (notas a mano, «Para M.»), Homemade Apple (firma de Garrido), Old Standard TT (membretes) | Apache 2.0 / OFL |
| Sonido: lobby (murmullo, campanita de elevador) | Kenney / Freesound CC0 | CC0 |
| Sonido: despacho (reloj, ciudad lejana) | `reloj_pared.wav` (ya está) + ciudad de Freesound CC0 | CC0 |
| Sonido: servidor (zumbido, ventiladores) | `zumbido_sala.ogg` (ya está) + ventiladores CC0 | CC0 |
| Sonido: estacionamiento (eco, goteo) | Freesound CC0 | CC0 |

Solo CC0 o CC-BY. Si algo sale CC-BY, lleva el autor en los créditos.

---

## 9. Riesgos y cómo los evito (secciones 1 y 7 de tu pedido)

| Lección | Cómo lo cumplo |
|---|---|
| 1 Flotantes | Prefabs con pivote en la base (como `_prefabs/` del Caso 1), grupos `apoyar_suelo`/`apoyar_pared` y `ajustar_apoyos.gd`; rejilla de 0.25 m y 15°. |
| 2 Paredes y techos | Un solo kit modular por edificio, zócalos y molduras en las uniones, techo en todos los interiores. Lo reviso con capturas desde abajo. |
| 3 Primitivas | Ninguna malla visible con `BoxMesh`/`CylinderMesh`. Le agrego a la auditoría un aviso «primitiva visible» si no lo tiene. |
| 4 Pistas fieles | Papeles con grosor, curvatura y reverso; texto revisado en la vista de examen. |
| 5 Pistas que no se toman | `probar_pistas.gd -- solo-caso2` y nada en la capa 4 que no responda. |
| 6 Oscuridad | Tres luces por espacio y ambiente mínimo en hackeado; capturas en los dos estados. |
| 7 Ventanas | Vista en capas (sección 7). |
| 8 Textos | `CartelPared` con ajuste automático; reviso cada uno en captura. |
| 9 Personajes | Reposo aplicado, de pie junto a la silla si no se sientan bien, sin intersecciones (la auditoría revisa encimados). |
| 10 Decoración vs. pistas | Decoración sin contorno; decorativos con el resaltado gris de HU-21. |
| 11 Vacío | Grupos que cuentan algo (el café a medio tomar de Hecdog, las fotos de Mimita en el despacho, la maqueta con post-its). |
| 12 Rendimiento | ms de CPU y GPU con la ventana al frente; luces nuevas sin sombra salvo la principal; `MultiMesh` para repetidos. |

**Memoria de tu PC:** las pruebas largas las corro por escena, una a la vez, y cierro Godot entre corridas. Te voy dando avances cortos.

---

## 10. Orden de trabajo y commits (cuando lo apruebes)

1. Comisaría de mañana → commit 1.
2. Lobby → commit 2.
3. Despacho piso 30 → commit 3.
4. Estado hackeado (despacho, piso 23, escaleras) → commit 4.
5. Escape (escaleras, piso 23, cocina, basura, estacionamiento) → commit 5.
6. Cierre (fachada, celda, embarcadero) → commit 6.
7. Pruebas, capturas y ajustes de herramientas → commit 7.

Después de cada espacio te mando 3 capturas para que lo veas antes de seguir. Push a `origin Escenarios-Capitulo-2` al final, sin PR.
