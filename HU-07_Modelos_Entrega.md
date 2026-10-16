# HU-07 (corrección) — Modelos 3D de las pistas y vista de examen

Rama `HU-07`. Sigue lo aprobado en el análisis previo ([Modelos de las pistas](https://claude.ai/artifact/Be51DaK8nxyi4sxqcETSWn)) con las recomendaciones de las cuatro decisiones:

1. **Pluma, leña, taza y delantal:** como no hubo descargas de Sketchfab, los modelé en el proyecto. Si después se descargan esos modelos, se cambian en sus escenas de `Assets/Pistas/Modelos/<pista>/`.
2. **Texturas:** Poly Haven con texturas de 1K. Godot genera LOD automático de cada malla para el escenario.
3. **Registro de llamadas:** sigue siendo una hoja impresa.
4. **Textos:** sin cambios. El testimonio de Eriz no tiene modelo.

## Vista de examen

- La barra de abajo ya no tiene «[E] Examinar». Muestra Girar, Acercar, Mover (solo con zoom), Linterna [F], Restablecer [R] y Volver [Esc]. Con control muestra L, LT RT, R, Y, X y B.
- Un detalle se revisa con clic sobre él (o con A en el control). Cuando un detalle queda a la vista aparece junto a él un aviso pequeño, «Revisar detalle», con una lupa. El aviso sigue al detalle y se desvanece al alejarse. No hay botón fijo.
- La prueba automática revisa los detalles con un clic en la pantalla, como el jugador.

## Modelos por pista

| Pista | Antes | Ahora |
|---|---|---|
| grabadora | Caja y cilindros | Grabadora de casetes (Poly Haven), sin cinta, con contador 03:12, luz REC y la etiqueta «— B.» por debajo |
| cinta | Cilindro | Casete (Poly Haven) con etiqueta escrita a mano |
| reloj | Aro y manecillas planas | Reloj de pared (Poly Haven) a las 3:12, con cristal estrellado y polvo |
| pluma | Caja plana | Pluma negra con barbas y cañón, medio colgando de la rejilla |
| envoltorio | Caja plana | Papel encerado arrugado con sello y migas |
| huellas | Cajas de barro | Calcomanías con la huella de la bota (y un molde de concreto en la vista de examen) |
| libro_turnos | Caja | Libro abierto con renglones, firmas y la nota del margen |
| nota_comisario | Caja plana | Memorándum a máquina doblado en tres |
| interruptor_generador | Cajas | Caja de interruptores (Poly Haven) con la palanca principal bajada |
| candado_caseta | Toro y caja | Candado de latón abierto, con óxido en las orillas |
| baul_disfraces | Primitivas | Capa, corona e inventario dentro de un baúl de madera (Poly Haven). También cambia el baúl del almacén |
| lena_intacta | Prefabs Kenney | Troncos partidos con corteza y anillos, y un hacha (Poly Haven) |
| libro_guardia_quemado | Cajas y cartel | Tres hojas con borde quemado; el nombre está en el reverso de una |
| nota_porky | Cartel | Nota doblada a la mitad, a mano, con la tinta corrida |
| foto_grupo | Caja y plano | Marco (Poly Haven) con la foto sepia y la dedicatoria atrás |
| recorte_periodico | Corcho y cartel | Recorte amarillento con titular, foto y columnas, clavado a un corcho enmarcado |
| oferta_compra | Tres carteles | Documento roto en cuatro pedazos con orillas rasgadas y firma, tirado junto a la papelera |
| taza_delantal | Prefab y cilindro | Taza de cerámica con chocolate y escurrimientos; delantal de tela colgado de un gancho |
| registro_enfermeria | Cartel y tachón | Libreta abierta con la línea tachada; con la linterna se leen los surcos |
| botas_tony | Cilindros y cajas | Botas de hule con barro (Poly Haven) y juncos en la suela |
| registro_llamadas | Cartel | Hoja impresa (sigue apagada) |

Los papeles tienen 1 mm de grosor, curvatura o dobleces y el reverso con su propia textura. El texto está escrito con fuentes de máquina de escribir, letra manuscrita y tipografía de periódico. Todos los materiales son PBR (albedo, normal y aspereza, y metal en lo metálico).

## Archivos

**Nuevos**
- `game/Assets/Pistas/Modelos/`:
  - una escena por pista (`<pista>/<pista>.tscn`) y sus mallas `.res`;
  - `materiales/`;
  - `generadas/`: texturas hechas en el proyecto;
  - `texturas/`: ambientCG;
  - `polyhaven/`: modelos glTF.
- `game/Assets/Fuentes/`: Special Elite, Caveat, Homemade Apple y Old Standard TT, con sus licencias en `licencias/`.
- `game/Herramientas/Pistas/`:
  - `generar_texturas.gd`: papeles con texto, foto, huellas, grietas, pluma y manchas;
  - `generar_modelos.gd`: mallas propias y materiales;
  - `mallas.gd`: funciones para armar hojas, libros, tubos, tornos, troncos y telas.
- `game/Herramientas/vista_previa_modelo.gd`: renderiza un modelo desde cuatro lados.
- `game/Herramientas/capturas_pistas.gd`: captura cada pista en el escenario y en la vista de examen.
- `game/Herramientas/medir_rendimiento.gd`: tiempo de dibujado de una escena.

**Modificados**
- `game/Escenas/Investigacion/VistaExamen.cs`: barra y revisión con clic.
- `game/Herramientas/probar_examen.gd`: revisa los detalles con un clic.
- Las escenas de las pistas: `sala_interrogatorio.tscn`, `zona_pasillo.tscn`, `zona_recepcion.tscn`, `zona_vestuario.tscn`, `caseta_generador.tscn`, `almacen.tscn`, `zona_fogata.tscn`, `cabana_porky.tscn`, `cocina_comedor.tscn`, `enfermeria.tscn`, `cabana_director.tscn` y `estacionamiento.tscn`.
  - Se quitaron las mallas viejas, se puso el modelo nuevo y se ajustó la colisión.
  - Los marcadores de los detalles ocultos ahora viven dentro de cada modelo.
- `game/Escenas/Caso1/fotografo_pistas.gd`: también toma las fotos del Caso 0 (`-- caso0`) y acepta la fuerza del foco por toma.
- `game/Assets/Pistas/*.png` y `game/Assets/Pistas/caso1/*.png`: las 21 fotos del expediente, rehechas.
- `game/Assets/Caso1/CREDITOS_ASSETS.md`: tabla con pista, archivo, fuente, autor, licencia y enlace.

## Checklist

- [x] **CA-01 a CA-04:** pasan en el Caso 0 y en el Caso 1 con `Herramientas/probar_examen.gd -- caso0|caso1`.
  - Todas las evidencias abren la vista.
  - Ningún detalle aparece al recoger.
  - Los 6 detalles se encuentran en su nuevo lugar y crean su entrada sin duplicarse.
  - Los estados se ven distintos y vuelven igual al guardar y cargar.
- [x] **Caso 0 y Caso 1 cargan sin errores nuevos:** el juego, el campamento y el menú, en headless. `dotnet build` compila sin errores.
- [ ] **60 FPS: falta comprobarlo en otro equipo.** En esta computadora todo corre a 30 FPS, incluso sin vsync. Los tiempos de dibujado son bajos:

  | Escena | GPU por cuadro | CPU por cuadro |
  |---|---|---|
  | Juego (Caso 0) | 3.6 ms | 0.7 ms |
  | Campamento | 5.1 ms | 1.9 ms |
  | Vista de examen | 0.8 ms | — |

  Para 60 FPS hacen falta menos de 16.6 ms por cuadro, así que el límite de 30 viene del equipo.
