# Créditos de todos los assets

Índice único de lo que el juego usa y no se hizo desde cero en el proyecto. El detalle de cada archivo está en los créditos de cada carpeta; aquí va todo lo demás. Lo marcado **por confirmar** no tiene fuente anotada en el repositorio: quien lo subió debe completar la fila antes de publicar el juego.

## Créditos detallados por carpeta

| Qué | Archivo de créditos |
|---|---|
| Campamento del Caso 1 (Kenney, KayKit, Quaternius), modelos de las pistas (Poly Haven, ambientCG), cinemática de inicio y embarcadero | [Caso1/CREDITOS_ASSETS.md](Caso1/CREDITOS_ASSETS.md) |
| Todos los sonidos y la música (Kenney, OpenGameArt, BigSoundBank) | [Audio/CREDITOS_AUDIO.md](Audio/CREDITOS_AUDIO.md) |
| Animaciones de los personajes y la figura de la cinemática de la cinta (Quaternius, CC0) | [Animaciones/CREDITOS_ANIMACIONES.md](Animaciones/CREDITOS_ANIMACIONES.md) |

## Fuentes tipográficas

Las licencias completas están junto a cada fuente.

| Fuente | Dónde se usa | Licencia | Archivo de licencia |
|---|---|---|---|
| Big Shoulders Display | Títulos del HUD, menús y expediente | SIL OFL 1.1 | `Fuentes/OFL-BigShouldersDisplay.txt`, `../hud/fonts/BigShoulders-OFL.txt` |
| IBM Plex Mono | Textos del HUD, diálogos y menús | SIL OFL 1.1 | `Fuentes/OFL-IBMPlexMono.txt`, `../hud/fonts/IBM-OFL.txt` |
| Courier Prime | Expediente | SIL OFL 1.1 | `Fuentes/OFL-CourierPrime.txt` |
| Special Elite | Papeles a máquina, cinemática | Apache 2.0 | `Fuentes/LICENSE-SpecialElite.txt` |
| Caveat | Notas a mano | SIL OFL 1.1 | `Fuentes/licencias/Caveat-OFL.txt` |
| Homemade Apple | Firmas | Apache 2.0 | `Fuentes/licencias/HomemadeApple-LICENSE.txt` |
| Old Standard TT | Recorte de periódico, membretes | SIL OFL 1.1 | `Fuentes/licencias/OldStandard-OFL.txt` |

## Personajes y animaciones (por confirmar)

| Archivos | Lo subió | Fuente y licencia |
|---|---|---|
| `Models/Personajes/*.glb` (Eriz, Porky, Lala/Juan Carlos, Miami, Aya, Tony) y sus texturas | Takeshi (HU-20.5) | **Por confirmar.** Anotar de dónde salieron los modelos y su licencia |
| `Models/Caneloso/` (modelo del jugador y animaciones `anim_*.res`) | FatimaContrerasOlague (HU-14.5) | **Por confirmar.** El esqueleto usa huesos `mixamorig_*`, así que las animaciones parecen de Mixamo (Adobe, uso libre en juegos); confirmar el origen del modelo |
| `Models/Aya/`, `Models/Miaurio/`, `Models/Tony/` (animaciones de personajes secundarios) | FatimaContrerasOlague | **Por confirmar.** Los nombres («Breathing Idle») son de Mixamo |
| `Models/eriz.fbx`, `Models/porky.fbx`, `*.blend` (modelos anteriores) | Taco0fJava | **Por confirmar** si se hicieron para el proyecto |

## Hecho para el proyecto

| Qué | Cómo se hizo |
|---|---|
| Retratos de los diálogos (`Retratos/*.png`) | Capturas de los modelos del juego con `Herramientas/retratos.gd` |
| Fotos de las pistas (`Pistas/*.png`, `Pistas/caso1/*.png`) | Capturas del juego con `fotografo_pistas.gd` |
| Foto del expediente del Caso 1 | Captura del campamento con `Herramientas/foto_expediente.gd` |
| Ilustraciones de los capítulos (`Capitulos/*.png`) | **Por confirmar** quién las hizo |
| Texturas de papeles, huellas, pluma y manchas de las pistas | `Herramientas/Pistas/generar_texturas.gd` |
