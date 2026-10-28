# Créditos de assets — Caso 1, Escenario 1 (Campamento Pino Lunar)

Todo es de licencia libre. CC0 no exige atribución, pero la dejamos por cortesía.

Los colores de los packs de Kenney se apagaron un poco (saturación 70 %, brillo 90 %) para que el campamento se vea gastado. Los modelos de la enfermería usan una copia de los módulos de cabaña con la textura en blanco (`Modelos/cabana_blanca`). Las texturas de suelo se redujeron a 512 px. Las texturas de KayKit también se apagaron (saturación 70 %).

| Archivo | Fuente | Autor | Licencia | Enlace |
|---|---|---|---|---|
| Modelos/cabana/*.glb (módulos de cabaña, faroles, guirnaldas) | Kenney · Holiday Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/holiday-kit |
| Modelos/cabana_blanca/*.glb (los mismos módulos, textura en blanco) | Kenney · Holiday Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/holiday-kit |
| Modelos/naturaleza/*.glb (pinos, troncos, rocas, pasto, canoa, fogata, letrero, cercas, casas de campaña) | Kenney · Nature Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/nature-kit |
| Modelos/supervivencia/*.glb (baúl, cajas, barril, cubeta, sacos de dormir, hacha, pala, láminas y estructura de la caseta) | Kenney · Survival Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/survival-kit |
| Modelos/muebles/*.glb (camas, literas, mesas, bancas, cocina, escritorio, buró, librero, lámparas, papelera, cajas de mudanza) | Kenney · Furniture Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/furniture-kit |
| Modelos/comida/*.glb (tazas, ollas, sartén, platos, tazón, tabla de picar) | Kenney · Food Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/food-kit |
| Modelos/coches/sedan.glb (coche de Juan Carlos) | Kenney · Car Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/car-kit |
| Modelos/pueblo/*.glb (farolas, postes de madera, tablones) | Kenney · Fantasy Town Kit | Kenney | CC0 1.0 | https://kenney.nl/assets/fantasy-town-kit |
| Modelos/kaykit_muebles/* (camas, sillas, mesas, vitrina, archivero, repisas, lámparas, cuadros, tapetes) | KayKit · Furniture Bits 1.0 | Kay Lousberg | CC0 1.0 | https://github.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0 |
| Modelos/kaykit_restaurante/* (ollas, sartenes, platos, frascos, huacales, campana, escurridor) | KayKit · Restaurant Bits 1.0 | Kay Lousberg | CC0 1.0 | https://github.com/KayKit-Game-Assets/KayKit-Restaurant-Bits-1.0 |
| Modelos/quaternius/*.glb (mochila, botiquines, bidón, tanque de gas, radio, celular, linterna, bote de basura, herramientas) | Quaternius · Survival Pack (convertido de FBX a GLB) | Quaternius | CC0 1.0 | https://opengameart.org/content/lowpoly-survival-pack |
| Modelos/_prefabs/*.tscn | Hechos para el proyecto: cada modelo con el pivote en la base, a la escala de Caneloso y con colisión | Equipo Detective Caneloso | Del proyecto | — |
| Texturas/madera_piso_*.jpg | ambientCG · Wood049 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Wood049 |
| Texturas/mapa_campamento.png | Hecha para el proyecto | Equipo Detective Caneloso | Del proyecto | — |
| Particulas/*.png (humo, chispas, luciérnagas, polvo) | Kenney · Particle Pack | Kenney | CC0 1.0 | https://kenney.nl/assets/particle-pack |
| Texturas/suelo_bosque_*.jpg | ambientCG · Ground037 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Ground037 |
| Texturas/suelo_tierra_*.jpg | ambientCG · Ground048 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Ground048 |
| Texturas/suelo_pasto_*.jpg | ambientCG · Grass004 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Grass004 |
| Texturas/foto_grupo.png, foto_lago.png, foto_fogata.png | Hechas para el proyecto (siluetas en sepia) | Equipo Detective Caneloso | Del proyecto | — |
| Texturas/mascara_suelo.png (caminos y pasto del suelo) | Hecha para el proyecto | Equipo Detective Caneloso | Del proyecto | — |
| ../Pistas/caso1/*.png (fotos de las pistas) | Capturas del escenario con `Escenas/Caso1/fotografo_pistas.gd` | Equipo Detective Caneloso | Del proyecto | — |
| Sonidos/*.ogg | Ver `Assets/Audio/CREDITOS_AUDIO.md` | — | CC0 1.0 | — |

## Modelos de las pistas (HU-07, Caso 0 y Caso 1)

Están en `Assets/Pistas/Modelos/`. Los de Poly Haven se usan en glTF con texturas de 1K; en cada escena de pista (`Modelos/<pista>/<pista>.tscn`) se ajustan (manecillas en las 3:12, cinta quitada, etiquetas, grietas...). Las texturas de papel con texto, la foto, las huellas, la pluma y las manchas se generan con `Herramientas/Pistas/generar_texturas.gd` y las mallas propias con `Herramientas/Pistas/generar_modelos.gd`.

| Pista | Archivo | Fuente | Autor | Licencia | Enlace |
|---|---|---|---|---|---|
| grabadora | Modelos/polyhaven/cassette_player/ | Poly Haven · Cassette Player | Oday Abuzaeed | CC0 1.0 | https://polyhaven.com/a/cassette_player |
| cinta | Modelos/polyhaven/portable_cassette_player/ (solo el casete) | Poly Haven · Portable Cassette Player | Mateusz Sadek | CC0 1.0 | https://polyhaven.com/a/portable_cassette_player |
| reloj | Modelos/polyhaven/wall_clock/ | Poly Haven · Wall Clock | PierreB3D | CC0 1.0 | https://polyhaven.com/a/wall_clock |
| interruptor_generador | Modelos/polyhaven/power_box_01/ | Poly Haven · Power Box 01 | Rico Cilliers, Yann Kervran | CC0 1.0 | https://polyhaven.com/a/power_box_01 |
| botas_tony | Modelos/polyhaven/rubber_boots/ (el par sucio) | Poly Haven · Rubber Boots | L | CC0 1.0 | https://polyhaven.com/a/rubber_boots |
| baul_disfraces (y el baúl del almacén) | Modelos/polyhaven/treasure_chest/ | Poly Haven · Treasure Chest | Rico Cilliers | CC0 1.0 | https://polyhaven.com/a/treasure_chest |
| lena_intacta (el hacha) | Modelos/polyhaven/hatchet/ | Poly Haven · Hatchet | James Ray Cock, Ulan Cabanilla | CC0 1.0 | https://polyhaven.com/a/hatchet |
| foto_grupo (el marco) | Modelos/polyhaven/hanging_picture_frame_02/ | Poly Haven · Hanging Picture Frame 02 | James Ray Cock | CC0 1.0 | https://polyhaven.com/a/hanging_picture_frame_02 |
| papeles, notas, libros y recortes | Modelos/texturas/Paper005, Paper006 (reducidas a 1K) | ambientCG · Paper 005 y Paper 006 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Paper005 · https://ambientcg.com/view?id=Paper006 |
| libro_turnos, registro_enfermeria (tapas) | Modelos/texturas/Leather037 | ambientCG · Leather 037 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Leather037 |
| candado_caseta, corona del baúl | Modelos/texturas/Metal048B, Metal038, Metal021 | ambientCG · Metal 048 B, Metal 038, Metal 021 (óxido) | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Metal048B · https://ambientcg.com/view?id=Metal038 · https://ambientcg.com/view?id=Metal021 |
| lena_intacta (troncos) | Modelos/texturas/Bark012, TreeEnd003 | ambientCG · Bark 012 y Tree End 003 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Bark012 · https://ambientcg.com/view?id=TreeEnd003 |
| delantal, capa del baúl | Modelos/texturas/Fabric061 | ambientCG · Fabric 061 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Fabric061 |
| recorte_periodico (corcho), reverso de la foto | Modelos/texturas/Cork002 | ambientCG · Cork 002 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Cork002 |
| huellas (molde de la vista de examen) | Modelos/texturas/Concrete034 | ambientCG · Concrete 034 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Concrete034 |
| nota_comisario, oferta_compra, registro_llamadas, libro_turnos (a máquina) | Fuentes/SpecialElite-Regular.ttf | Google Fonts · Special Elite | Astigmatic | Apache 2.0 | https://fonts.google.com/specimen/Special+Elite |
| notas y registros escritos a mano | Fuentes/Caveat-Variable.ttf | Google Fonts · Caveat | Impallari Type | SIL OFL 1.1 | https://fonts.google.com/specimen/Caveat |
| firmas | Fuentes/HomemadeApple-Regular.ttf | Google Fonts · Homemade Apple | Font Diner | Apache 2.0 | https://fonts.google.com/specimen/Homemade+Apple |
| recorte_periodico, oferta_compra (membrete) | Fuentes/OldStandard-Regular.ttf, OldStandard-Bold.ttf | Google Fonts · Old Standard TT | Alexey Kryukov | SIL OFL 1.1 | https://fonts.google.com/specimen/Old+Standard+TT |
| el resto de las pistas (papeles, candado, taza, delantal, leña, pluma, envoltorio, huellas, corona, palanca) | Modelos/<pista>/ y Modelos/generadas/ | Hechos para el proyecto con Herramientas/Pistas/generar_modelos.gd y generar_texturas.gd | Equipo Detective Caneloso | Del proyecto | — |

## Cinemática de inicio de capítulo y expediente

El escritorio está en `Escenas/Cinematicas/escritorio_noir.tscn` y los modelos en `Assets/Cinematicas/Modelos/polyhaven/` (glTF con texturas de 1K). La carpeta, las hojas, el lápiz y el sello se arman en la escena; el contenido de las hojas se dibuja con los datos del `InformeCaso`.

| Objeto | Archivo | Fuente | Autor | Licencia | Enlace |
|---|---|---|---|---|---|
| escritorio | Cinematicas/Modelos/polyhaven/WoodenTable_01/ | Poly Haven · Wooden Table 01 | Ethan Place | CC0 1.0 | https://polyhaven.com/a/WoodenTable_01 |
| lámpara | Cinematicas/Modelos/polyhaven/desk_lamp_arm_01/ | Poly Haven · Desk Lamp Arm 01 | Yann Kervran, Kuutti Siitonen | CC0 1.0 | https://polyhaven.com/a/desk_lamp_arm_01 |
| reloj despertador | Cinematicas/Modelos/polyhaven/alarm_clock_01/ | Poly Haven · Alarm Clock 01 | Yann Kervran, James Ray Cock | CC0 1.0 | https://polyhaven.com/a/alarm_clock_01 |
| libreta y notas adhesivas | Cinematicas/Modelos/polyhaven/office_notepads/ | Poly Haven · Office Notepads | Ulan Cabanilla | CC0 1.0 | https://polyhaven.com/a/office_notepads |
| lupa | Cinematicas/Modelos/polyhaven/magnifying_glass_01/ | Poly Haven · Magnifying Glass 01 | Nazar Borodavka | CC0 1.0 | https://polyhaven.com/a/magnifying_glass_01 |
| taza | Pistas/Modelos/taza_delantal/taza.tscn | Hecha para el proyecto | Equipo Detective Caneloso | Del proyecto | — |
| papel de la carpeta y las hojas | Pistas/Modelos/texturas/Paper005 | ambientCG · Paper 005 | ambientCG (Lennart Demes) | CC0 1.0 | https://ambientcg.com/view?id=Paper005 |
| foto del expediente del Caso 1 | hud/expediente/fotos/caso_01_embarcadero.png | Captura del campamento con `Herramientas/foto_expediente.gd` | Equipo Detective Caneloso | Del proyecto | — |
| embarcadero del lago | Escenas/Caso1/embarcadero.tscn | Hecho para el proyecto | Equipo Detective Caneloso | Del proyecto | — |

## Caso 1 · Escenario 2 (Lago y Bosque, HU-20.6)

El escenario reusa los modelos del Campamento (Kenney, KayKit, Quaternius, `_prefabs/`) y las texturas de suelo. Lo nuevo está en `Assets/Caso1/Lago/`. Los modelos de Poly Haven se usan en glTF con texturas de 1K.

| Archivo | Uso | Fuente | Autor | Licencia | Enlace |
|---|---|---|---|---|---|
| Lago/Modelos/polyhaven/boombox/ | pista altavoz | Poly Haven · Boombox | Thomas Paul Mouilleron | CC0 1.0 | https://polyhaven.com/a/boombox |
| Lago/Modelos/polyhaven/vintage_flashlight/ | pista linterna_porky | Poly Haven · Vintage Flashlight | Omar M. El-Safy | CC0 1.0 | https://polyhaven.com/a/vintage_flashlight |
| Lago/Modelos/polyhaven/tree_stump_01/ | tocones del bosque y la colina | Poly Haven · Tree Stump 01 | Rob Tuytel | CC0 1.0 | https://polyhaven.com/a/tree_stump_01 |
| Lago/Modelos/polyhaven/tree_stump_02/ | tocones del bosque y la colina | Poly Haven · Tree Stump 02 | Rob Tuytel | CC0 1.0 | https://polyhaven.com/a/tree_stump_02 |
| Lago/Modelos/polyhaven/lifebuoy/ | salvavidas del muelle y del cobertizo | Poly Haven · Lifebuoy | Hank Kaamura | CC0 1.0 | https://polyhaven.com/a/lifebuoy |
| Lago/Modelos/polyhaven/wooden_crate_01/ | cajas del cobertizo | Poly Haven · Wooden Crate 01 | James Ray Cock | CC0 1.0 | https://polyhaven.com/a/wooden_crate_01 |
| Lago/Modelos/polyhaven/rock_moss_set_01/ | rocas con musgo del bosque | Poly Haven · Rock Moss Set 01 | Kless Gyzen | CC0 1.0 | https://polyhaven.com/a/rock_moss_set_01 |
| Lago/Texturas/huella_pezuna.png, sangre.png, red_pesca.png | huellas, sangre del remo, redes rotas | Hechas para el proyecto con `Herramientas/Caso1/texturas_lago.gd` | Equipo Detective Caneloso | Del proyecto | — |
| Lago/Texturas/colormap_patrulla.png | patrulla de Hecdog (paleta del sedán de Kenney con la carrocería en azul) | Kenney · Car Kit, recoloreada con `texturas_lago.gd` | Kenney / Equipo Detective Caneloso | CC0 1.0 | https://kenney.nl/assets/car-kit |
| Escenas/Caso1/Lago/terreno_lago.res, terreno_lago_forma.res | terreno y su colisión | Hechos para el proyecto con `_generador_caso1/lago_mapa.py` y `Herramientas/Caso1/terreno_lago.gd` | Equipo Detective Caneloso | Del proyecto | — |
| Escenas/Caso1/Lago/*.gdshader | suelo, agua con algas y juncos con viento | Hechos para el proyecto | Equipo Detective Caneloso | Del proyecto | — |
| ../Pistas/Modelos/<pista>/ de las 9 pistas del lago | silbato, cornamenta y zancos, soporte de remos, remo, cuaderno, huellas (mallas propias); altavoz y linterna (Poly Haven ajustados) | Hechos con `Herramientas/Caso1/modelos_pistas_lago.gd` | Equipo Detective Caneloso | Del proyecto | — |
| ../Retratos/tony.png, aya.png, miawrio.png, juan carlos.png, mimita.png, hecdog.png | retratos de diálogo | Capturas de los modelos del equipo con `Herramientas/Caso1/retratos_caso1.gd` | Equipo Detective Caneloso | Del proyecto | — |
| Fuentes/Caveat-Variable.ttf | letra de Eriz en su cuaderno | Google Fonts · Caveat | Impallari Type | SIL OFL 1.1 | https://fonts.google.com/specimen/Caveat |

## Caso 1 · Escenario 3 (sala de interrogatorio del Capítulo 1, HU-20.6)

`Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn` se arma con `Herramientas/Caso1/sala_caso1.gd` a partir de la sala del Caso 0 (mismos materiales y muebles). El corcho, la mesa de pruebas y la puerta cerrada son mallas simples hechas para el proyecto. Los personajes son los modelos del equipo (`Assets/Models/`).

## Cinemática de la cinta (Caso 0)

No trae modelos nuevos: rueda en la Sala 1 con lo que ya está en el juego. La cinta que se ve en la grabadora y en la mano de Porky es el casete que trae el modelo **Poly Haven · Cassette Player** (`cassette_player_tape`, el mismo de la grabadora de arriba). La animación está hecha en Blender 5.2 (`Assets/Cinematicas/Blender/`, ver su `LEEME.md`) y se exporta a `Assets/Cinematicas/Caso0/cinta_caso0.glb`. Ese .glb solo reúne modelos que ya estaban en el juego:

| Pieza | De dónde sale | Licencia |
|---|---|---|
| Porky | `Assets/Models/Personajes/porky.glb`, el mismo del juego | el del modelo original (ver `Assets/CREDITOS.md`) |
| La figura | El maniquí de **Quaternius · Universal Animation Library** (`UAL1_Standard.glb`) con gabardina y capucha modeladas en Blender (un cono con pesos automáticos y dos esferas) | CC0 1.0 |
| Animaciones base de Porky y la figura | **Quaternius · Universal Animation Library 1 y 2** (Sitting_Idle, Sitting_Exit, Idle, Walk, Crouch_Idle, Crouch_Fwd, Jog_Fwd, Hit_Chest, NinjaJump_Land, Jump_Loop), ajustadas con IK y la coreografía | CC0 1.0 |
| La cuerda | Un cilindro de Blender | propio |
| La sala | La Sala 1 del juego exportada a glTF (`sala1.glb`), solo como referencia en Blender | la de sus modelos |

No se usaron animaciones de Mixamo ni capturas de Rokoko: Mixamo pide iniciar sesión con una cuenta de Adobe y Rokoko Vision necesita grabar a alguien con la webcam.
