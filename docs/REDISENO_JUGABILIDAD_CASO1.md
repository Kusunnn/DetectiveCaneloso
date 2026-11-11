# Rediseño de la jugabilidad del Caso 1

**Estado: aprobado por Takeshi el 5 nov con cambios.** Esta sección manda sobre el resto del documento cuando se contradicen.

## Estado al 10 de noviembre

Los 7 pasos están hechos, en commits locales de `claude/funny-mestorf-5a148a`, sin subir. También está integrado `main` con HU-29 (fichas de sospechoso).

| Paso | Commit |
|---|---|
| 1 · GuiaCaso1 y objetivos | 644bc33 |
| Ajustes de guion aprobados (silbato «T. S.», pelo café, notas) | ab140e1 |
| 2 · Candados, confrontar, Relacionadas, corazonada | 12db9cd |
| 3 · Ayudas de 3 niveles y dificultad | 9d725bb |
| 4 · Reconstrucción de la noche | 4dd941f |
| 5 · Instinto y pistas por zona | 1f14659 |
| Merge de `main` (HU-29) | 5c3d62b |
| 6 · Actos y «Caso cerrado» | ae13a23 |
| 7 · Personajes en su lugar con su objeto; Eriz y Aya en el Lago | 6f320d9 |

**Fuera de alcance:** las rutinas de caminar quedaron fuera (no hubo tiempo) y tampoco hay shader de desaturación.

**Líneas para revisar:** todas las líneas nuevas para el equipo están en `game/Datos/Dialogos/caso1_sistema.json` (`"nueva": true`) y en `game/Datos/Caso1/caso1_reconstruccion.json`.

## 0. Lo que se aprobó el 5 de noviembre

| Tema | Decisión |
|---|---|
| Final A (D1) | Se dispara al sellar las viñetas con los candados de Tony rotos. Si faltan candados, Caneloso y Hecdog lo encaran. |
| Reconstrucción | Recortada a 15 huecos (0.1). |
| Juan Carlos (D2) | 3 candados; total 15. |
| Candados de Tony (D3) | En cualquier orden, con los [AJUSTE] de la tabla 0.2. |
| Teclas (D4) | Instinto en [V], cámara en [T], ambas configurables. |
| Ayudas (D5) | Sin tecla H y sin ayuda automática por tiempo. Se piden a Hecdog y en Pausa → Pistas. Niveles 1 y 2 con plantillas de GuiaCaso1; solo el 3 a mano. Todas en `caso1_sistema.json` con `"nueva": true`. |
| Acción (D6) | [E] Confrontar. Si al sospechoso le queda una sola frase falsa, se salta la elección y va directo a la prueba. |
| Lago (D7) | Eriz en la orilla y Aya en el bosque. |
| «Acusar ya» (D8) | Contra Tony sin el caso completo, Hecdog lo rechaza sin castigo. |
| Silbato y pelo (D9) | «T. S.» y el color real del pelaje (0.3). |
| Charlas (D10) | Una declaración por NPC. |
| Huellas que se detienen | Es la llave de JC → Aya → Mimita: con el instinto brilla desde más lejos (16 m en lugar de 8). |
| Credibilidad | No se muestra en pantalla; solo cuenta para la calificación. |
| Calificación | Candados al primer intento 15, tiempo 5, pistas 40, ayudas 30, credibilidad 10. |
| Instinto | Se amplía `ContornoPistas` con radio y colores. Sin shader de desaturación por ahora. |
| Rutinas | Primero cada NPC en su lugar, con su actividad y objeto. Caminar va al final, si hay tiempo. |
| FPS | Medir con la ventana al frente; si da 30 exactos, revisar VSync y el límite del proyecto. |

**Orden de trabajo:** 1) GuiaCaso1 y objetivos, 2) candados con Relacionadas y corazonada, 3) ayudas, 4) Reconstrucción, 5) instinto, 6) tarjetas y calificación, 7) rutinas. Commit y pruebas por paso; aviso al terminar cada uno. Sin push ni PR hasta que Takeshi lo diga.

### 0.1 Reconstrucción recortada: 15 huecos [NUEVA]

| Viñeta | Frases | Huecos |
|---|---|---|
| **1 · La fogata** (motivo) | «Hace 10 años murió [Paco] en el lago, y el [libro de guardia] decía quién estaba de guardia.»<br>«Porky escribió en [su nota] que esa noche lo diría todo, y citó a [Tony] en el embarcadero.» | 4 |
| **2 · El apagón** (oportunidad) | «A las [23:00], la luz se fue porque [Juan Carlos] bajó el interruptor del generador.»<br>«[Tony] caminó por el [sendero del lago] en lugar de ir a la caseta.» | 4 |
| **3 · El embarcadero** (medio) | «A las [23:20], en el embarcadero, [Tony] golpeó a Porky con el [remo] del [cobertizo].» | 4 |
| **4 · Después** | «Después, [Tony] quemó el [libro de guardia] en la fogata y escondió sus [botas].» | 3 |

Las 8 pruebas mínimas de la historia siguen obligadas por sus fichas: Paco, libro de guardia, su nota (motivo); 23:20, sendero del lago, botas (oportunidad); remo, cobertizo (medio). Se quitan las frases 1.3, 2.2, 3.2, 4.1 y 4.2. Los demás señuelos se conservan.

### 0.2 [AJUSTE] propuestos: Tony en cualquier orden (D3)

**Se puede sin reescribir.** Son 4 cambios y ninguno se ha aplicado todavía.

| Dónde | Hoy | Propuesta |
|---|---|---|
| `excusa_1` (Caneloso) | «Tony. Ya nada más falta usted.» | Se dice solo si los otros cinco ya confesaron. El texto no cambia. |
| `excusa_2` | «Pero no me acerqué al lago. Después de...» | «No me acerqué al lago. Después de...» |
| `e5_cae` | «...» y «Mmh... no voy a decir nada más. Si cree que fui yo, demuéstrelo.» | Al caer T5 queda solo «...». La segunda línea se dice al caer **el último candado**, sea cual sea. |
| Nota de `tony_5` | «...Tony ya no contesta.» | «El remo salió del cobertizo, que solo abren dos llaves: la de Porky y la de Tony.» |

### 0.3 [AJUSTE] propuestos: silbato «T. S.» (D9)

**Ninguno se ha aplicado. Esperan el visto bueno de Takeshi.**

| Dónde | Hoy | Propuesta |
|---|---|---|
| `caso1_pistas`: título | Silbato «G. S.» | Silbato «T. S.» |
| `caso1_pistas`: comentario | ...dos iniciales: «G. S.». ¿De quién será? | ...dos iniciales: «T. S.». ¿De quién será? |
| `caso1_pistas`: nota | Silbato con las iniciales «G. S.»... | Silbato con las iniciales «T. S.»... |
| `caso1_pistas`: `foto_grupo.silbato` | ...un silbato con las iniciales «G. S.». | ...un silbato con las iniciales «T. S.». |
| `foto_grupo_silbato.tres`: descripción | ...con las iniciales «G. S.» grabadas. No son sus iniciales. | ...con las iniciales «T. S.» grabadas. Son sus iniciales. |
| `foto_grupo_silbato.tres`: pensamiento | «G. S.» en el silbato de Tony. ¿De quién eran esas iniciales? | «T. S.» en el silbato de Tony. Tony Spínola. |
| `caso1_juan_carlos` y `caso1_tony`: opción | Silbato «G. S.» | Silbato «T. S.» |
| `excusa_3` | ¿Ese silbato? No es mío. «G. S.» puede ser cualquiera... | ¿Ese silbato? No es mío. «T. S.» puede ser cualquiera... |
| `e3_silbato` | «G. S.»: Guillermo Spínola. Su abuelo. Usted heredó el campamento... y también su silbato. Estaba atorado... | «T. S.»: Tony Spínola. Estaba atorado en un clavo del embarcadero, con el cordón roto. Y hoy trae uno nuevo. |
| `e3_escapa` | ...Mi abuelo les regalaba silbatos a todos los monitores. Hay docenas así... | ...Mi abuelo les regalaba silbatos a todos los monitores. Cualquiera pudo grabarle esas letras... |
| `e3_cae` | ...ya lo traigo colgado. Sí. Era de mi abuelo. Se me cayó. | ...ya lo traigo colgado. Sí. Es mío. Se me cayó. |
| Nota de `tony_3` | El silbato «G. S.» era de Guillermo Spínola, el abuelo de Tony. Tony lo traía anoche... | El silbato «T. S.» es de Tony Spínola. Lo traía anoche... |
| Textura de la foto (`generar_texturas.gd`) | El silbato dibujado dice G.S. | T.S., regenerando la imagen |

**Duda:** la dedicatoria de la foto («Para Paco, que nunca le tuvo miedo al lago. — G. S.») no habla del silbato: la firma Don Guillermo, y el antecedente del expediente lo nombra. Propongo **dejarla como está**. Si se cambia a «T. S.», la firmaría Tony el mismo día que murió Paco, y eso cambia la historia.

El id interno `silbato_gs` no se ve en el juego y no se toca.

### 0.4 [AJUSTE] propuesto: pelo del disfraz (D9)

La textura de Juan Carlos es **café lisa, sin rayas**, así que queda «pelo café». El guion y `caso1_pistas` ya lo dicen, y el modelo no cambia. Solo falta corregir la descripción de la pista en `zona_bosque.tscn`:

| Hoy | Propuesta |
|---|---|
| ...Tiene pelos de gato a rayas atorados. | ...Tiene pelo café atorado en las correas. |

### 0.5 Encontrado al integrar: notas con nombres viejos

El commit `2608f23` (Mario, «revision final y hud») regresó en `Caso1.cs` las notas de detalles a una versión anterior: «monitor auxiliar Antonio R.», «Para Daniel... — A. R.», «lleva grabado "A. R.": es Tony» y «23:20, ruido en el lago». Esos nombres no existen en la historia v2, y hoy el juego los muestra al examinar esas pistas.

**Propuesta:** volver a `_guion.NotaDeDetalle`, que lee `caso1_pistas.json` igual que el Lago. No lo he tocado: conviene confirmarlo con Mario.

---

Fuente de verdad: `docs/historias/Caso_1_La_Noche_del_Ciervo_de_Luz_v2.docx`. Base revisada: `HU-9.5-Animaciones-Tutorial` en `454e77d`.

La idea central es que **casi nada del guion se toca**. Los árboles `caso1_*.json` ya tienen cada paso de cada sospechoso con su prueba y su confesión. El rediseño cambia cómo se llega a esos nodos, no lo que dicen.

## Índice

1. Decisiones que necesito de ti
2. Reporte del paso 00
3. Flujo del capítulo por actos
4. Pistas: instinto y contadores
5. Charlas y rutinas en el campamento
6. Tabla de candados
7. Reconstrucción de la noche
8. Ayudas y dificultad
9. Calificación
10. Código: qué se reutiliza, qué se reemplaza, qué JSON cambia
11. Cambios a los diálogos
12. Pruebas
13. Riesgos y límites

---

## 1. Decisiones que necesito de ti

Si respondes «va con tus recomendaciones», avanzo con la columna **Recomiendo**.

| # | Qué pasa | Opciones | Recomiendo |
|---|---|---|---|
| D1 | **Tu prompt se contradice.** La sección 4.3 dice que el 5.º candado de Tony lleva a la confesión y al Final A. Las secciones 1 y 6 ponen la Reconstrucción antes del final. Si el 5.º candado termina el juego, la Reconstrucción nunca se juega. | A) El Final A pide **las dos cosas**: los 5 candados y las 4 viñetas, en cualquier orden. Lo último que se complete dispara el final. B) Como dice 4.3, y la Reconstrucción queda opcional. | **A.** Además la línea del guion ya lo pide: al caer el 5.º, Tony dice «Si cree que fui yo, demuéstrelo». |
| D2 | **Juan Carlos no cabe en «1 o 2 candados».** La historia pide tres pruebas para él y su árbol tiene tres pasos con líneas propias. | A) 3 candados. B) 2, juntando leña y disfraz, lo que obliga a quitar una línea del guion. | **A.** El total queda en **15 candados** (10 + 5 de Tony), no 11. |
| D3 | **Orden de los candados de Tony.** Sus excusas se escribieron en escalera: cada una es un retroceso de la anterior («Pero no me acerqué al lago...»). | A) En orden: los 5 visibles, pero se rompen del 1 al 5. B) Libres, aunque «Pero...» y «no voy a decir nada más» quedan fuera de lugar. | **A.** No hace falta cambiar texto. |
| D4 | **La tecla [V] ya está ocupada** por «Cambiar cámara» (`cambiar_camara`). | A) Instinto en [V] y cámara a [T]. B) Instinto en otra tecla. | **A.** Ambas siguen siendo configurables. |
| D5 | **Mario quitó [H] y el temporizador a propósito** en HU-10 («sin temporizador», «La tecla H deja de ser un control de ayuda»). Tu prompt los pide de vuelta. | A) Tu prompt manda. B) Preguntarle a Mario antes. | **A**, avisándole a Mario. Sus pruebas no revisan la tecla H ni el temporizador, así que no se rompen. |
| D6 | **Tres candados no desmienten una mentira:** el registro de llamadas confirma a Miawrio, las huellas confirman a Juan Carlos y la bocina solo incomoda a Aya. «Objetar» suena raro ahí. | A) Llamar a la acción **[E] Confrontar**. B) Mantener «Objetar». | **A.** |
| D7 | **Acto 2 sin nadie con quien platicar:** no hay ningún NPC en la escena del Lago. | A) Eriz (orilla) y Aya (bosque) también aparecen allá. B) El Acto 2 es solo explorar. | **A.** Son sus lugares en la historia. |
| D8 | **«Acusar ya» eligiendo a Tony sin el caso completo.** La historia no prevé ese final. | A) Hecdog lo rechaza sin castigo y te regresa. B) Ocultar a Tony de la lista, como hoy, lo que delata que es el culpable. | **A.** |
| D9 | **Silbato:** la historia y el modelo dicen «T. S.»; el guion dice «G. S.» y lo explica con Guillermo Spínola. El pelo del disfraz también difiere: «de gato a rayas» en la historia, «café» en el guion. | Lo decide quien escribe el guion. | No bloquea: la Reconstrucción dice «silbato» y «un silbido», sin iniciales. |
| D10 | **La charla del campamento** solo puede usar lo que ya está escrito, y cada NPC tiene una sola declaración. | A) Se queda así. B) Tu equipo escribe chismes extra para el campamento. | **A** para empezar; B se puede agregar después sin programar nada. |

---

## 2. Reporte del paso 00

| Punto | Resultado |
|---|---|
| Copia de trabajo | Sin cambios. La sesión trabaja en un worktree (`claude/funny-mestorf-5a148a`), que puse en `454e77d` porque `HU-9.5-Animaciones-Tutorial` ya está abierta en tu carpeta principal y Git no deja abrirla dos veces. |
| Tu carpeta principal | En `454e77d`, igual que GitHub. Tiene **63 archivos sin commit** que no toqué: 52 `.import` modificados y 11 sin seguimiento (`.codex/`, `AGENTS.md`, `_generador_caso1/`, `docs/HU-9.5_Analisis.md`, `docs/REVISION_JUEGO.md`, dos `.uid` y otros). |
| `git merge origin/main` | «Already up to date»: `main` (`7db7c33`) ya está dentro. **El commit 1 de la sección 10 de tu prompt no hace falta.** |
| `HU-10---Sistema-de-ayudas` | **Ya integrada completa** (`dedb8a3`, 3 nov). No hay que integrar nada antes. Lo que aporta: `SistemaAyudas.cs`, `PanelPistas.cs` (Pausa → Pistas), `AyudasPrueba.cs` y 3 pruebas de humo. Hoy solo da **un nivel** de orientación y sin temporizador; la sección 8 lo amplía sin duplicarlo. |
| `voces-personajes` | **Ya integrada** (`1bec891`). Su punta (`aadf873`) solo agrega un merge de `main`, sin contenido nuevo: las voces son iguales y lo único distinto en `VentanaDialogo.cs` es lo que esta rama le agregó después. **No choca:** cada frase de una declaración pasa por el mismo `VentanaDialogo.Decir`, así que se oye con su voz. |
| `dotnet build` | Correcto, 0 advertencias y 0 errores. |

---

## 3. Flujo del capítulo por actos

```
 ACTO 1 · EL CAMPAMENTO          Objetivo: «Averigua qué pasó en el campamento»
 Explorar · 13 pistas · llavero de Porky · charlas (declaraciones)
    │  Para bajar: 8 pistas + llavero (regla actual; el objetivo dice cuánto falta)
    ▼  Resumen de Caneloso + autoguardado
 ACTO 2 · EL LAGO Y EL BOSQUE    Objetivo: «Averigua qué pasó en el lago»
 Explorar · 9 pistas · charlas con Eriz y Aya (D7)
    │  Para ir a la estación: 5 pistas del lago (regla CA-03 que ya aprobaste)
    ▼  Resumen + autoguardado
 ACTO 3 · LA SALA                Objetivo: «Rompe los candados» o «Tengo algo para Aya»
 Candados de los 5 en el orden que quieras · Tony disponible desde el inicio
 Se puede volver al lago o al campamento en cualquier momento
    │  Se abre sola al romper el 5.º candado de Tony; también con Hecdog desde el inicio
    ▼  Resumen + autoguardado
 ACTO 4 · LA RECONSTRUCCIÓN      Objetivo: «Cuenta la noche completa»
 4 viñetas con huecos
    │
    ├─ 4 viñetas + 5 candados ──► «Oportunidad, medio, motivo» (guion) ► 23:20 ► Confesión ► FINAL A
    ├─ 4 viñetas, faltan candados ► Caneloso y Hecdog encaran a Tony ► 5.º candado ► lo mismo
    └─ «Acusar ya» (con confirmación) a uno de los 5 inocentes ──────► FINAL B
    ▼
 CASO CERRADO: calificación
```

- **Actos y fases:** los actos se calculan con lo que ya guarda la partida (`GuionCaso1.FaseActual`), así que las partidas viejas caen en su acto correcto.
- **Siempre visibles:** tarjeta de título corta al entrar a cada acto y objetivo principal en el HUD. En el diario, los secundarios: «Pistas del campamento 9/13», «Pistas del lago 4/9», «Candados rotos 6/15».
- **Dependencias del Acto 3.** No hay ciclos: siempre hay algo que se puede hacer. Lo verifica la prueba de la sección 12.

```
 Pistas ─► Juan Carlos ─► Aya ─► Mimita ─► T3 (silbato)
                 │         │
                 └────┬────┘
                      └─► T1 (generador)
 Pistas ─► Miawrio ─► T4 (motivo)       Pistas ─► Eriz (solo da su propia línea)
 Pistas ─► T2 (botas), T5 (remo)
 Tony va en orden: T1 → T2 → T3 → T4 → T5
```

### El objetivo nunca se queda sin decir qué sigue

Una sola función recorre los candados y responde en este orden. Con esto se resuelven los problemas 1, 3 y 4.

1. Si tengo la prueba de un candado: «Tengo algo para Aya», y su retrato brilla con la lupa.
2. Si falta una pista: «Me falta algo del lago», con la zona.
3. Si falta una confesión: «Juan Carlos tiene que hablar primero», con quien corresponda.
4. Si ya no queda ningún candado: «Cuenta la noche completa».

---

## 4. Pistas: instinto y contadores

### 4.1 Instinto de detective

- **Tecla [V]** y un botón de control, configurables (D4). Dura 4 s, recarga en 10 s y alcanza 8 m. La imagen se desatura con un solo shader de pantalla completa.
- **Colores:** amarillo = sin tomar; gris = ya revisada; azul = requiere linterna o agacharse, con su ícono.
- **Reutiliza `ContornoPistas.cs`,** que ya dibuja el contorno ámbar a 4.5 m. Se le agrega color por estado y el radio del instinto.
- **Lo que un conteo simple se saltaría,** porque hoy esas pistas no se pueden tomar hasta abrir su contenedor: si la pista está dentro de algo cerrado (baúl, almohada, papelera, escritorio), **brilla el contenedor**. También brillan las puertas cerradas con llave y el delantal que da el llavero.
- **Pulso sonoro suave** si hay una pista sin tomar a menos de 3 m. Hace falta un sonido nuevo.

### 4.2 Contadores por zona

Aviso al entrar a la zona («Fogata · 2 pistas por descubrir») y lista en el diario, sin decir cuáles son.

| Zona | Pistas | Condición |
|---|---|---|
| Fogata | 2 | `libro_guardia_quemado` pide linterna y agacharse |
| Almacén | 1 | Abrir el baúl |
| Cocina | 1 + llavero | Revisar el delantal |
| Cabaña de Porky | 4 | Con llavero; la almohada y la papelera se abren; `oferta_compra` pide agacharse |
| Cabaña del director | 1 | `botas_tony` pide linterna |
| Enfermería | 1 | Abrir el escritorio |
| Caseta del generador | 2 | |
| Estacionamiento | 1 | |
| **Campamento** | **13** | |
| Sendero | 2 | Agacharse |
| Bosque | 2 | |
| Orilla | 2 | `remo_sangre` pide agacharse |
| Embarcadero | 2 | |
| Cobertizo | 1 | Con llavero |
| **Lago** | **9** | |

### 4.3 Pistas que se explican solas

Los comentarios de `caso1_pistas.json` ya dicen para qué sirve cada pista, así que **no se cambian**. Lo nuevo es un dato `apunta_a` que pone el retrato del sospechoso junto a la pista en el diario.

**Cuidado con el spoiler:** si `libro_guardia_quemado` mostrara a Tony desde que se recoge, le diríamos el culpable al jugador. La regla es que el retrato aparece **cuando ya se escuchó la frase que esa pista contradice**. Excepción: las botas, porque el comentario del guion ya nombra al director.

---

## 5. Charlas y rutinas en el campamento

### 5.1 Charla (sin pruebas)

- Hablar con un NPC en el campamento o el lago ya **no abre el interrogatorio**. Dice su declaración y se guarda en el diario, sección Personas.
- La declaración es la línea `mentira` del guion, dividida en frases sin cambiar palabras (sección 6). Se omite la línea de Caneloso que abre la escena, porque «Miawrio, siéntate, por favor» no tiene sentido caminando.
- Al marcar `mentira` como vista, en la sala arranca con «¿Otra vez tú?» (`regreso_1`), que ya existe y queda natural.
- Ícono **«!»** sobre la cabeza mientras no haya dado su declaración; después, nada. Si el jugador vuelve a hablarle, usa las variantes de `confeso` o `regreso` que ya existen.
- Si alguien llega a la sala sin haber platicado, su declaración se escucha ahí y se guarda igual.

### 5.2 Rutinas

| NPC | Recorrido | Actividad |
|---|---|---|
| Aya | Fogata ↔ bordes del bosque | Tomar fotos con la cámara |
| Miawrio | Estacionamiento ↔ entrada | Hablar por celular |
| Juan Carlos | Fogata ↔ escenario de los cuentos | Tocar la guitarra |
| Mimita | Dentro y frente a la enfermería | Acomodar el botiquín |
| Tony | Cabaña del director ↔ fogata | Mirar al suelo, abatido |
| Eriz | Orilla de la fogata, poco movimiento | Sentado con su manta |

- Nuevo `RutinaNpc.cs`: puntos con espera y actividad, sin repetir la misma dos veces seguidas (como pide CA-03). Usa los `Marker3D` de cada zona y los caminados que ya tiene `AnimacionPersonaje`, también el de código para Juan Carlos y Eriz.
- **Sin navegación automática:** las escenas no tienen `NavigationRegion3D`, así que son rutas fijas revisadas para no atravesar muebles. Es más barato para los 60 FPS.
- Al hablarle, se detiene y te mira (`Npc.MirarA`); después sigue. En cinemáticas se pausa. La posición no se guarda: al cargar, cada quien vuelve a su punto de inicio.
- **Objetos en la mano:** todavía no tengo confirmado qué modelos hay de cámara, celular, guitarra y manta. Lo reviso al empezar; si faltan, te aviso antes de hacerlos.

---

## 6. Tabla de candados

### 6.1 Cómo se juega

1. El sospechoso repite su declaración frase por frase (flechas para avanzar y retroceder).
2. **[E] Confrontar** sobre la frase (D6).
3. Se elige la prueba. La lista abre en **Relacionadas** (las de esa frase o ese sospechoso) y a un lado está **Todas**.
4. **Acierto:** cadena que cae, ánimo «alterado» y el nodo de confesión del guion, sin cambios.
5. **Error:** el nodo `e_*` si el guion lo tiene, o si no el regaño de Hecdog (`error_*`). Se puede volver a intentar.
6. **Tras 2 errores en el candado,** la corazonada marca la frase correcta. **Tras 3,** quedan solo 3 pruebas: la correcta y dos señuelos.

El estado de cada candado **no se guarda aparte: se calcula con las conclusiones que ya existen** (`juan_carlos_1`, `tony_3`, etc.). Las partidas viejas no pierden nada.

### 6.2 Los cinco sospechosos (10 candados)

Las frases son el texto actual del guion, solo divididas. **La prueba** sale del escenario o de una confesión.

| Candado | Frase | Prueba que lo rompe | De dónde sale | Conclusión | Qué revela |
|---|---|---|---|---|---|
| **JC-1** | «Fíjate que fui por leña cuando se fue la luz, bro.» | Montón de leña intacto | Campamento · Fogata | `juan_carlos_1` | Dice que se entretuvo en el bosque |
| **JC-2** | «Y sí, vi al ciervo, ¿eh? ¿Yo? Ahí estaba en la colina, bien brillante.» | Cornamenta y zancos | Lago · Bosque | `juan_carlos_2` | Admite que él era el ciervo |
| **JC-3** | «Pero eso no quiere decir que bajé al lago.» (de `p2_disfraz`) | Huellas que se detienen *(confirma)* | Lago · Sendero | `juan_carlos_3`, `jc_vio_tony_sendero`, `jc_vio_linterna` | El montaje; linterna a las 23:15; **vio a Tony en el sendero** |
| **AY-1** | «Oh... yo estaba tomando fotos del bosque, detective.» | Bocina escondida *(incomoda)* | Lago · Bosque | `aya_1` | Tenía la cámara lista justo a esa hora |
| **AY-2** | «La neta del planeta, no vi nada raro.» | Lo que confesó Juan Carlos | **Confesión de JC-3** | `aya_2`, `foto_aya_2319`, `foto_aya_2312` | **Fotos de las 23:19 y 23:12** |
| **MI-1** | «A la cocinera casi no la conocía.» | Oferta de compra rota | Campamento · Cabaña de Porky | `miawrio_1` | La oferta y la amenaza legal |
| **MI-2** | «Eh... estuve en mi carro, haciendo llamadas.» | Registro de llamadas *(confirma)* | Campamento · Estacionamiento | `miawrio_2`, `miawrio_coartada`, `miawrio_oyo_cita` | Coartada; **oyó la cita de las 18:00** |
| **ER-1** | «Llegué al lago justo antes.» | Cuaderno de Eriz | Lago · Orilla | `eriz_1`, `eriz_hora_crimen` | **La hora: 23:20**, el brillo y el chapuzón |
| **MM-1** | «No vi nada. ¿Sí?» | Registro de la enfermería | Campamento · Enfermería | `mimita_1` | «Me equivoqué al anotar» |
| **MM-2** | «Mmm... estuve toda la noche en la enfermería.» | Foto de Aya de las 23:19 | **Confesión de AY-2** | `mimita_2`, `mimita_oyo_silbido` | **Oyó el silbido de Tony** |

Dentro de cada sospechoso van en orden (es el orden de su árbol). Las demás frases de cada declaración quedan como distractores: «La luna, los pinos... bien bonito todo», «Cerrando pendientes», «Mira... yo encontré el cuerpo y ya». Eriz, por ejemplo, tiene 3 frases y una sola es la buena.

### 6.3 Tony (5 candados)

Tony se puede interrogar desde el inicio de la sala. Se quita el nodo `bloqueado`, que es el «Platique primero con los demás» del problema 4. En su lugar, cada candado dice lo que falta, en una línea nueva para que la revise tu equipo.

| Candado | Excusa (guion) | Se rompe con todo esto | De dónde sale | Lo que dice el candado **[NUEVA]** | Conclusión |
|---|---|---|---|---|---|
| **T1** | «No me moví de la fogata. Solo salí un momento a revisar el generador.» | Foto de las 23:12 + candado sin forzar + lo que vio Juan Carlos | **AY-2** + Caseta + **JC-3** | «Necesito saber quién lo vio camino al lago. Juan Carlos y Aya saben algo.» | `tony_1` |
| **T2** | «Pero no me acerqué al lago.» | Botas con lodo | Cabaña del director, con linterna | «Necesito algo que lo ponga en la orilla. Sus cosas, tal vez.» | `tony_2` |
| **T3** | «¿Ese silbato? No es mío.» | Silbato + foto del grupo + el silbido | Embarcadero + Cabaña de Porky + **MM-2** | «Necesito saber de quién es ese silbato. Mimita oyó algo.» | `tony_3` |
| **T4** | «¡No tenía nada contra Porky!» | Libro quemado + nota de Porky + la cita de las 18:00 | Fogata + Cabaña de Porky + **MI-2** | «Necesito saber para qué lo citó Porky. Miawrio estaba ahí.» | `tony_4` |
| **T5** | «Cualquiera pudo agarrar un remo de ese cobertizo.» | Soporte de remos vacío + remo con sangre | Cobertizo, con llavero + Orilla | «Necesito el arma, y de dónde salió.» | `tony_5` |

- Los árboles ya distinguen `e1_cae` de `e1_escapa`. Si le muestras la prueba principal sin las demás, Tony se zafa con su línea del guion («Una foto borrosa...»). Eso **no cuenta como error**: el candado te dice qué te falta.
- La línea de Caneloso «Tony. Ya nada más falta usted.» se omite si todavía faltan otros, con el `OmitirLinea` que ya existe, sin editar el texto.

### 6.4 Credibilidad sin castigo

- Empieza en 5; baja 1 por error y sube 1 por acierto, como hoy.
- **Tiene un piso de 2 por código.** Así nunca se llega a `paciencia` ni a `error_1` («te mando a acusar a quien sea»), que serían mentira en este diseño. No se edita ninguna línea: simplemente dejan de alcanzarse.
- **Se quitan** Preguntar, Presionar y el corte de Hecdog. Sigue contando para la calificación, con los errores en un contador aparte.

---

## 7. Reconstrucción de la noche

Reemplaza «Armar el caso». Pantalla estilo expediente con 4 viñetas ilustradas. **Todas las frases son nuevas [NUEVA]**, escritas a partir de la cronología de la historia, para que tu equipo las revise. `[ ]` es un hueco.

| Viñeta | Frases completas |
|---|---|
| **1 · La fogata** (motivo) | **1.1** «Hace 10 años murió [Paco] en el lago, y el [libro de guardia] decía quién estaba de guardia.»<br>**1.2** «Porky escribió en [su nota] que esa noche lo diría todo, y a las [18:00] citó a [Tony] en el embarcadero.»<br>**1.3** «En la fogata, [Juan Carlos] dijo que iba por [leña] y se alejó.» |
| **2 · El apagón** (oportunidad) | **2.1** «A las [23:00], la luz se fue porque [Juan Carlos] bajó el interruptor del generador.»<br>**2.2** «El ciervo era un [disfraz]: se quedó en la [colina] y los bramidos salían de una [bocina].»<br>**2.3** «[Tony] caminó por el [sendero del lago] en lugar de ir a la [caseta].» |
| **3 · El embarcadero** (medio) | **3.1** «A las [23:20], en el embarcadero, [Tony] golpeó a [Porky] con el [remo] del [cobertizo].»<br>**3.2** «[Mimita] oyó [un silbido] y luego el agua; [Eriz] vio el brillo y pensó que era un pez.» |
| **4 · Después** | **4.1** «[Tony] tiró el remo entre los [juncos] y volvió a la [fogata].»<br>**4.2** «A las [23:47], [Eriz] encontró el cuerpo.»<br>**4.3** «Después, [Tony] quemó el [libro de guardia] en la fogata y escondió sus [botas].» |

Son 31 huecos en total: 7, 8, 8 y 8. La frase 2.3 cambia un poco tu ejemplo para que «sendero del lago» y «caseta» también sean fichas.

### 7.1 Fichas y de dónde salen

Se juntan solas. Se pueden arrastrar o elegir, también con control.

| Tipo | Ficha | La da |
|---|---|---|
| Implicados *(siempre)* | Tony, Juan Carlos, Aya, Miawrio, Eriz, Mimita | Expediente |
| Víctimas | Porky *(siempre)*; **Paco** | Expediente; `recorte_periodico` |
| Horas | 23:00, 23:40*, 23:47 | Expediente |
| | **18:00** | `miawrio_oyo_cita` |
| | 23:12* · 23:15* · 23:19* | Foto 23:12 · linterna de JC · foto 23:19 |
| | **23:20** | `cuaderno_eriz` |
| Objetos | **libro de guardia** · **su nota** · leña | `libro_guardia_quemado` · `nota_porky` · `lena_intacta` |
| | **remo** · **botas** · disfraz · bocina | `remo_sangre` · `botas_tony` · `cornamenta_zancos` · `altavoz` |
| | **un silbido** | `mimita_oyo_silbido` |
| | silbato* · linterna* · oferta de compra* · cuaderno* · un bramido* | Sus pistas |
| Lugares | fogata, colina, caseta | Expediente, declaración de JC, `candado_caseta` |
| | **sendero del lago** | `jc_vio_tony_sendero` o `foto_aya_2312` |
| | **cobertizo** · juncos | `hueco_remo` · `remo_sangre` |
| | enfermería* · bosque* | Sus pistas |

`*` = señuelo, no va en ningún hueco. **En negritas** = las que obligan a tener la prueba mínima.

### 7.2 Se conserva la regla de oportunidad, medio y motivo

Como las pruebas mínimas de la historia son las fichas, **no se puede sellar una viñeta sin tenerlas**. No hay una segunda validación que pueda contradecir a la primera.

| Elemento (`GuionCaso1.Casillas`) | Prueba mínima | Ficha | Viñeta |
|---|---|---|---|
| Oportunidad | Cuaderno de Eriz | 23:20 | 3 |
| | Juan Carlos o foto de Aya | sendero del lago | 2 |
| | Botas | botas | 4 |
| Medio | Remo con sangre | remo | 3 |
| | Cobertizo | cobertizo | 3 |
| Motivo | Libro quemado | libro de guardia | 1 y 4 |
| | Nota de Porky | su nota | 1 |
| | Recorte | Paco | 1 |

### 7.3 Cómo se confirma

- Viñeta completa y correcta: sello **«ENCAJA»** y la ilustración se colorea.
- Completa con 1 o 2 errores: «Casi... algo no cuadra» **[NUEVA]**, sin decir cuál.
- Completa con 3 o más: «Todavía no me convences, Caneloso» (línea de sistema que ya existe).
- Sin castigo. Los intentos se cuentan solo para la calificación.
- Los nombres se pueden poner donde sea: la deducción es del jugador.

### 7.4 «Acusar ya» (Final B)

Disponible con Hecdog desde el inicio de la sala. Pide elegir a quién y confirmar con la línea que ya existe: «Si me equivoco, un inocente irá a prisión».

- A un inocente: usa `a_<sospechoso>` → `equivocado` → `final_b`, sin cambios.
- A Tony sin el caso completo (D8): Hecdog se niega y te regresa, sin castigo: «Con eso se nos sale, Nya. Cuenta la noche completa.» **[NUEVA]**

---

## 8. Ayudas y dificultad

### 8.1 Sobre `SistemaAyudas.cs`

- **Se amplía,** no se duplica: `Agregar`, `Capturar`, `Restaurar` y `Usos` siguen igual, y las entradas se guardan con id `caso1:<paso>:<nivel>`. Una petición repetida no cuenta doble, como hoy. `PanelPistas` sigue mostrando el historial.
- **Se unifica la orientación:** hoy `Caso1` y `Caso1Lago` tienen cada uno su `OrientacionInvestigacion`, y la sala no tiene. Pasa a una sola guía que usa la misma función del objetivo (sección 3), así que objetivo y ayuda nunca se contradicen.
- **Los textos de Mario se conservan** como nivel 1 o 2 donde encajan.
- **Entrada:** tecla [H] y Hecdog («Pedirle una pista a Hecdog»), además de Pausa → Pistas (D5).

### 8.2 Tres niveles

Son líneas **[NUEVA]**, una por paso. Estos son ejemplos; el resto siguen el mismo molde y van marcados en `caso1_sistema.json`.

| Paso | 1 · Insinuación | 2 · Dirección | 3 · Respuesta |
|---|---|---|---|
| Hueco del remo | «¿Ya revisaste bien el lago, Nya?» | «Algo en el cobertizo no me cuadra.» | «Falta un remo en el soporte del cobertizo.» |
| Botas | «Tony anda muy limpio para haber caminado anoche, Nya.» | «Date una vuelta atrás de la cabaña del director.» | «Prende la linterna atrás de la cabaña del director: ahí están sus botas.» |
| AY-2 | «A Aya alguien le pagaba, Nya.» | «Lo que te dijo Juan Carlos le interesa a Aya.» | «Cuéntale a Aya lo que confesó Juan Carlos.» |
| T1 | «Ese sendero no lleva a donde dice, Nya.» | «La caseta tiene algo que decirte sobre ese candado.» | «Necesitas el candado sin forzar, a Juan Carlos y la foto de las 23:12.» |
| Viñeta 3 | «¿A qué hora brilló el agua, Nya?» | «Eriz lo apuntó en su cuaderno.» | «Las 23:20, con el remo del cobertizo.» |

**Automática:** con 3 min sin avanzar (sin pista, candado ni viñeta nueva), Caneloso piensa el nivel 1 sin interrumpir y **no cuenta** como ayuda usada. El reloj se detiene en diálogos, menús y cinemáticas.

### 8.3 Dificultad

Se elige al empezar el capítulo y se puede cambiar en Opciones. Se guarda con la partida; las partidas viejas quedan en Detective.

| | Historia | Detective (por defecto) | Experto |
|---|---|---|---|
| Instinto | Sin recarga | 4 s, recarga 10 s | No hay |
| Corazonada | Desde el 1.er error | A los 2 errores | No hay |
| Reducir a 3 pruebas | Desde el 2.º error | A los 3 errores | No hay |
| Ayudas | Los 3 niveles sin límite | Los 3 niveles | Solo nivel 1 |
| Contorno ámbar actual | Sí | Sí | No |

---

## 9. Calificación

Pantalla «Caso cerrado» tras el Final A o B. Reutiliza `ResultadoCapitulo` y el patrón de `Caso0.Capitulo.cs`.

| Dato | Peso |
|---|---|
| Pistas encontradas (de 22) | 30 |
| Candados rotos al primer intento (de 15) | 30 |
| Ayudas usadas | 20 (−4 por ayuda, hasta 0) |
| Credibilidad | 10 |
| Tiempo | 10 (completo hasta 45 min) |

- **Rango:** A ≥ 80, B ≥ 55, C el resto. El Final B queda en C como máximo. Hay una frase de Hecdog por rango **[NUEVA]**.
- **Dificultad:** no cambia los pesos. Se muestra en la pantalla.
- `Capitulos` también admite «S». No la uso porque pediste A/B/C.

---

## 10. Código

### 10.1 Se reutiliza tal cual

| Pieza | Para qué |
|---|---|
| `MotorDialogos` y `EstadoDialogos` | Recorrer árboles, conclusiones, notas, guardado |
| Árboles `prueba_N`, `p*`, `e_*`, `e*_cae`, `e*_escapa` | Toda la lógica de pruebas y confesiones |
| `CinematicaCaso1`, `LibretosCaso1` | Libro, foto, silbato, llegada, 23:20, finales |
| `GuionCaso1.AcusarATony` y `TerminarAcusacion` | La secuencia hasta el final. Ya resuelve «encarar a Tony si aún tiene candados» |
| `SalaInterrogatorioCaso1` y `.Pov` | Puesta en escena, ánimos, push-in, entradas |
| `VentanaDialogo` | Voces, frases, selector (se amplía) |
| `ContornoPistas`, `ObjetoPista`, `Contenedor`, `Puerta` | Base del instinto y de las condiciones |
| `PartidaEscenas`, `Caso1.Partida`, `EscenarioCaso1` | Guardado entre escenas |
| `SistemaAyudas`, `PanelPistas` | Base de las ayudas |
| `AnimacionPersonaje`, `Npc` | Base de las rutinas |
| `PistasParaSalir` y `PistasLagoParaEstacion` | Reglas que ya aprobaste |

### 10.2 Se reemplaza

| Actual | Queda |
|---|---|
| `GuionCaso1.ElegirAccion`, `Presionar`, `Presentar`, `CortarInterrogatorio` | Contrainterrogatorio por frases, con el mismo gancho `Motor.ElegirAccion` |
| `GuionCaso1.ArmarCaso.cs` y `PanelArmarCaso.cs` | `GuionCaso1.Reconstruccion.cs` y `PanelReconstruccion.cs` |
| `GuionCaso1.Objetivo()`, `Pasos` y `OrientacionInvestigacion` | `GuiaCaso1.cs`, una sola función |
| `Careo` como juego aparte | Tony usa el mismo contrainterrogatorio; se conserva la compatibilidad con partidas viejas |
| `Npc.Usado → Interrogar` en el campamento | `Npc.Usado → Charla` |

### 10.3 Nuevo

| Archivo | Qué hace |
|---|---|
| `GuionCaso1.Candados.cs` | Estado calculado de cada candado y qué falta |
| `GuiaCaso1.cs` | Qué sigue: objetivo y ayudas |
| `PanelContrainterrogatorio.cs` | Frases, flechas, [E] y lista con pestañas |
| `InstintoDetective.cs` | Instinto, colores y pulso |
| `RutinaNpc.cs` | Recorridos |
| `Datos/Caso1/caso1_candados.json` | Tablas de la sección 6 |
| `Datos/Caso1/caso1_reconstruccion.json` | Tablas de la sección 7 |
| `Datos/Dialogos/caso1_sistema.json` | **Todas** las líneas nuevas, con `"nueva": true`, en un solo lugar para que tu equipo las revise |

### 10.4 JSON del guion que cambia (solo estructura)

| Archivo | Cambio | ¿Cambia texto? |
|---|---|---|
| `caso1_tony.json` | `entrada`: se quitan los 5 saltos a `bloqueado` | No |
| `caso1_sala.json` | Opción `tony`: se quita `requiere` y `motivo`. Opción `acusar`: se renombra y se agrega «Acusar ya» | Solo 2 etiquetas de menú |
| `caso1_pistas.json` | Se agrega `apunta_a` a cada pista | No |
| `caso1_juan_carlos`, `aya`, `miawrio`, `eriz`, `mimita`, `introduccion`, `acusacion` | Nada | No |

### 10.5 Guardado

Solo se **agrega**; las partidas viejas siguen abriendo:

```
"dificultad": "detective"
"declaraciones": ["juan_carlos", ...]
"reconstruccion": { "huecos": {"1.1.a": "Paco", ...}, "selladas": [1, 3], "intentos": 2 }
"errores_candado": { "AY-2": 1, ... }
```

- **Se calculan, no se guardan:** candados (de las conclusiones) y fichas (de pistas y conclusiones).
- **`caso_armado`:** se ignora al cargar una partida vieja.

---

## 11. Cambios a los diálogos

**Solo estructura y líneas de sistema. Ninguna línea de la historia ni del guion cambia sin tu permiso.**

| Tipo | Qué | Cuántas |
|---|---|---|
| Sin cambio de texto | Declaraciones divididas en frases. Una prueba compara cada una con el guion, para que no se desincronicen | 6 |
| Se dejan de usar, sin borrar | `bloqueado`, `paciencia`, `error_1`, nodos sin uso de `caso1_sala` y `caso1_acusacion` (`acusar`, `pregunta`, `elegir`), y `LineasSistema` de Presionar y Casillas | |
| **[NUEVA]** | Frases de la Reconstrucción | 11 |
| **[NUEVA]** | Candados de Tony (sección 6.3) | 5 |
| **[NUEVA]** | Ayudas de 3 niveles | ~3 por paso, unos 40 pasos |
| **[NUEVA]** | Corazonadas y acierto o error en Reconstrucción | ~6 |
| **[NUEVA]** | Hecdog: rechazo de «Acusar ya» y entrada a la Reconstrucción | 2 |
| **[NUEVA]** | Tarjetas de los 4 actos y frases de calificación | 7 |
| **[NUEVA]** | Resúmenes de fin de acto (abajo) | 9 |

### Resúmenes de fin de acto [NUEVA]

| Acto | Líneas de Caneloso |
|---|---|
| 1 | «El apagón lo provocaron a mano y el ciervo era un disfraz.» / «Porky se fue con prisa y tenía un libro que alguien quemó.» / «Lo que me falta está en el lago.» |
| 2 | «Las huellas no llegan al embarcadero: el ciervo no la mató.» / «Tengo un remo con sangre y un silbato sin dueño.» / «Ahora sí: a la estación, a ver quién miente.» |
| 3 | «Todos soltaron lo suyo; las excusas de Tony ya no dan para más.» / «Tengo la hora, el arma y el motivo.» / «Falta contar la noche completa.» |

Los resúmenes del Acto 3 suponen que ya están todos los candados. Si el jugador va por otro camino, Caneloso dice la versión del objetivo.

### Pendientes del guion

Siguen abiertos desde la entrega anterior y **tampoco los toco**: silbato (D9), borradores `a_aya` y `a_mimita`, y el Final B («Me lo llevo» cuando la acusada es una mujer; «Miawrio y Tony se dan la mano» aunque el acusado sea Miawrio).

---

## 12. Pruebas

Tu PC se queda sin memoria con pruebas largas, así que la prueba de jugabilidad va **en dos partes**:

1. **Lógica sin escenas** (`tests/JugabilidadCaso1Prueba.cs`), en segundos:
   - Cada candado se rompe con su prueba y la tabla coincide con los árboles.
   - No hay ciclos: ninguna prueba necesaria es imposible de conseguir.
   - Con un recorrido en **todos los órdenes posibles**, siempre hay un retrato que brilla o una zona indicada.
   - Las 4 viñetas se completan con las fichas juntadas, y sin una prueba mínima no se pueden completar.
   - Las frases divididas coinciden con el guion.
2. **`Herramientas/Caso1/probar_jugabilidad.gd`,** por partes (`-- solo-acto1` ... `-- solo-final-b`):
   - Juega como jugador, con guardar y cargar en cada acto.
   - Final A por las dos rutas de D1, y Final B.
   - **Respalda las partidas en disco primero** y las restaura al terminar, también si la corrida anterior se cortó.

**También:** `probar_pistas.gd`, `probar_ca03.gd`, `probar_escenarios.gd` y las 3 pruebas de ayudas de Mario, en 0 fallos. Hay que **reescribir** `probar_acusacion.gd` y `FlujoCaso1Prueba.cs`, porque prueban la pantalla que se va.

**Lo que no puedo medir yo:** la meta de 30 a 45 min y 0 atascos solo se puede **simular**. El simulador da tiempos aproximados por acción y no reemplaza a una persona; lo reportaré como estimación. Conviene que alguien que no conozca el juego lo pruebe.

---

## 13. Riesgos y límites

| Riesgo | Qué haré |
|---|---|
| **60 FPS sin confirmar.** La entrega anterior midió 30.0 exactos en la sala, y no comprobé la causa. | Reportaré el FPS con y sin instinto, comparado con el de hoy. No puedo prometer 60 hasta saber por qué da 30. |
| **El tamaño.** Es mucho trabajo: 8 sistemas. | Commits por sistema como pediste, en orden: guía y actos, luego candados (lo que más duele), y al final rutinas. Cada uno compila y se prueba solo. |
| **Declaraciones cortas.** Con 2 o 3 frases por sospechoso, elegir la frase es fácil. | Es aceptable para el tono de tu prompt (claro y guiado). La dificultad real queda en la prueba y en la Reconstrucción. |
| **Sonidos y modelos nuevos** (cadena, pulso, objetos en la mano). | Reviso qué existe y te aviso antes de agregar recursos. |
| **`hud.gd` es grande (903 líneas) y lo usa también el Caso 0.** | El diario del Caso 1 se agrega con funciones aparte; `probar_ca03.gd -- solo-caso0` lo verifica. |
| **Fuera de alcance:** no se tocan Caso 0, cinemáticas ni menús, salvo la opción de dificultad. | |

---

## Siguiente paso

Con tu visto bueno a la sección 1 empiezo por el commit de **actos y objetivos**. No subo nada ni abro PR hasta que me lo digas.
