# Capítulo 2 «La Trampa de la Garrapata» · Entrega de escenarios

Rama `Escenarios-Capitulo-2` (subida a origin, sin PR). El análisis aprobado está en `docs/CAP2_Escenarios_Analisis.md`.
Capturas a 1920 × 1080 (fuera del repo, pesan demasiado): `Proyecto/CAP2_capturas/` (una carpeta por espacio y
`galeria/`). Reportes de pruebas: `docs/CAP2_reportes/`.

## Cómo probarlo

- **Galería** (menú de inicio → GALERÍA → ESCENARIOS → grupo «Caso 2»): los 11 escenarios para girarlos y verlos de lejos.
- **Caminarlos**: abre `res://Escenas/Caso2/probar_capitulo2.tscn` y pulsa F6 (o `--path game res://Escenas/Caso2/probar_capitulo2.tscn -- despacho`).
- **Estado hackeado**: `EstadoLuces.FijarEstado(true)` desde código, o abre la escena con `-- --estado=hackeado` (y `--estado=normal`).
- **Regenerar**: cada escena sale de un generador en `Herramientas/Caso2/generar_*.gd`; el kit y los objetos, de `Assets/Caso2/Blender/*.py`.

## Espacios

| Escena | Medidas | Qué tiene |
|---|---|---|
| `comisaria_manana.tscn` | 8 × 6 × 2.8 m | Escritorios de Caneloso y Hecdog, persianas con la calle de mañana, café humeante, radio, corcho, reloj; Hecdog con su taza |
| `torre_lobby.tscn` | 12 × 10 × 4 m | Recepción de mármol y nogal, sala de espera, elevadores, logo dorado, directorio, noticias; recepcionista (marcador temporal) |
| `torre_despacho.tscn` | 14 × 10 × 3.4 m | Escritorio, zona de estar, mesa de proyectos con maqueta, archivero, pecera, torre para gato, rincón de Aya; ciudad desde el piso 30; Miawrio, Aya, Mimita, Hecdog |
| `torre_escaleras.tscn` | 3 × 6 m, 3 tramos | Piso 30 → 24 → 23, panel de tarjeta, letreros de piso, luces de emergencia |
| `torre_piso23.tscn` | 8 × 6 m + pasillo | Racks con luces, charolas de cables, aire, ServidorDisco, mampara de vidrio |
| `torre_cocina.tscn` | 6 × 5 m | Cocina industrial, mesa de acero, ollas, la puerta que todavía se mueve |
| `torre_basura.tscn` | 4 × 4 m | Contenedores, bolsas, huellas mojadas |
| `torre_estacionamiento.tscn` | 20 × 12 m + rampa | Coches, columnas, rampa con luz de día, patrulla, Tecnico_A y Tecnico_B |
| `torre_fachada_noche.tscn` | banqueta 16 m | Torre de noche, farolas que parpadean, encuadres de Miawrio y Mimita y de Aya |
| `penal_celda.tscn` | 3 × 4 m | Catre, mesa, ventana con barrotes, reja, libro; Tony |
| `embarcadero_sellado.tscn` | Lago del Caso 1 | Variante (la escena original no se toca) con sellos rotos y cinta amarilla deshilachada |

## Tabla de pistas

| id | espacio | ubicación | cómo se encuentra | imagen |
|---|---|---|---|---|
| `dibujo_eriz` | Comisaría | Escritorio de Caneloso, junto al sobre y la invitación | A simple vista; nota al reverso | `Assets/Pistas/caso2/dibujo_eriz.png` |
| `registro_accesos` | Lobby | Pantalla del mostrador, del lado del visitante | A simple vista; las 3 líneas resaltadas | `…/registro_accesos.png` |
| `planos_clinica` | Despacho | Mesa de proyectos, junto a la maqueta | A simple vista | `…/planos_clinica.png` |
| `contrato_ixodes` | Despacho | Escritorio, bajo la lámpara | A simple vista | `…/contrato_ixodes.png` |
| `estado_cuenta` | Despacho | Cajón de en medio del archivero | Abrir el cajón (se abre libre) | `…/estado_cuenta.png` |
| `registro_llamadas2` | Despacho | Escritorio, junto al teléfono | A simple vista | `…/registro_llamadas2.png` |
| `abrecartas` | Despacho | Escritorio, con la tarjeta del socio | A simple vista | `…/abrecartas.png` |
| `expediente_filtrado` | Despacho | Cajón de abajo del escritorio (lado de Miawrio) | Abrir el cajón | `…/expediente_filtrado.png` |

Título, descripción y comentario salen del docx. La invitación de Miawrio se lee pero no es evidencia.

## Puntos preparados para las mecánicas (sin lógica)

`PuntoInterrogatorio` (sillones, `PuntoSilla`, `PuntoCaneloso`, `PuntoEntradaImplicado`, `CamaraPlanoMedio`), `PuntoTablero`,
`PuntoEspera_Mimita`, `PuntoEspera_Hecdog`, `PuntoAya`, `Elevador` (lobby y despacho), `PuertaEscaleras` (despacho, cerrada),
`PanelTarjeta`, `PuertaTito_1` a `PuertaTito_4` (puerta cerrada, pantalla con las tres rayas, `PuntoPregunta` y `Salida` apagados),
`Cruce_1` a `Cruce_4` (`Rastro`, `Camino_A`, `Camino_B`), `ServidorDisco` (`Disco`, `PuntoDisco`), `Tecnico_A`, `Tecnico_B` (Pulgas),
`Patrulla` (`PuntoEntradaPatrulla`), `EstadoLuces` (despacho, escaleras y piso 23), encuadres y `PuntoSilueta` del cierre,
y los `Entrada_*` de cada escena.

## Definición de terminado

| Punto | | Evidencia |
|---|---|---|
| AuditoriaEscenario: 0 flotando, 0 intersecciones, pistas alcanzables | ✔ | `docs/CAP2_reportes/auditoria_*.md`: 0 hallazgos en las 10 escenas nuevas. El embarcadero solo tiene 1 aviso heredado del Lago (colisión del soporte de remos sin la pista del Caso 1, que se oculta) |
| probar_pistas: 0 pistas visibles que no se puedan tomar | ✔ | `probar_pistas_caso2.md`: 8 de 8 |
| 0 cajas o cilindros primitivos visibles | ✔ | Todo es Kenney, KayKit, Quaternius, Poly Haven o hecho en Blender. Excepciones que no son mallas primitivas: textos (Label3D), pantallas planas y el vapor (partículas) |
| Techo completo, sin huecos ni z-fighting | ✔ | Kit modular con zócalos y molduras; revisado en capturas |
| Ventanas con vista | ✔ | Calle de mañana (comisaría, lobby), ciudad desde el piso 30 (despacho), muro del penal (celda) |
| Textos visibles caben y se leen | ✔ | Revisado en capturas (directorio, noticias, letreros, papeles en el examen) |
| Legible normal y hackeado | ✔ | Despacho, escaleras y piso 23 en los dos estados |
| Personajes sin pose T, sin atravesar muebles, texturas y ojos | ✔ con nota | Reposo aplicado y de pie. La textura de Aya se ve pixelada de cerca (ya pasaba en el Caso 1). Los objetos en la mano de Aya y Miawrio no se pegaban a la mano y los quité |
| Rendimiento (16.6 ms) | ✔ | GPU 3.2–6.5 ms y CPU 0.4–1.1 ms en todas las vistas (tabla abajo), con ventana. En tu PC marca 30 FPS fijos, como siempre |
| 0 errores y advertencias nuevas en consola | ✔ con nota | Las 12 escenas cargan sin errores. El aviso «N resources still in use at exit» al cerrar también sale en el Campamento del Caso 1: no es nuevo |
| Caso 0 y Caso 1 siguen funcionando | ✔ parcial | `probar_escenarios.gd`: 0 fallos. `probar_ca03.gd`: partes `solo-caso0` y `solo-sala` sin fallos; la corrida completa (~20 min) no la hice para no saturar tu PC |

## Rendimiento (ms por cuadro, ventana al frente)

| Espacio | CPU | GPU |
|---|---|---|
| Comisaría | 0.7 | 4.7 |
| Lobby | 0.6 | 4.1–4.9 |
| Despacho (normal) | 0.6–0.8 | 4.3–5.4 |
| Despacho (hackeado) | 0.6–1.1 | 4.0–6.5 |
| Escaleras | 0.4–0.5 | 3.4–3.5 |
| Piso 23 | 0.6–0.7 | 3.9–4.5 |
| Cocina | 0.5 | 3.5–3.6 |
| Basura | 0.4–0.5 | 3.2–3.4 |
| Estacionamiento | 0.7–0.8 | 5.3–5.7 |
| Fachada de noche | 0.6–0.8 | 3.8–4.5 |
| Celda | 0.5–0.6 | 4.1–4.4 |
| Embarcadero | 0.6–0.7 | 5.1–5.5 |

## Pendientes y decisiones

- **Marcadores temporales**: recepcionista, Tecnico_A y Tecnico_B (Pulgas) son el maniquí de Quaternius pintado, con gorra, cubrebocas y las tres rayas en la mano de Pulgas. Faltan sus modelos.
- **Audio**: no tenía sonidos de murmullo, campanita de elevador, ciudad ni eco. Por ahora usan los ambientes que ya había (zumbido, reloj). Hay que conseguir CC0 y agregarlos a `CREDITOS_AUDIO.md`.
- **caso_02.tres** sigue con la historia vieja («El último testigo»).
- **Fechas** de los papeles (derivadas, no están en el docx): subasta 26/09, contrato 25/09, foto de Eriz 19/09, Caso 1 el 14/08.
- **Nombres de relleno** que no están en el docx: las 8 empresas del estado de cuenta, las filas normales del registro de accesos, el directorio y la cinta de noticias. Los puede cambiar quien escribe el guion.
- La rama trae mezclada `Galeria-Modelos` para poder agregar los escenarios a la galería.
