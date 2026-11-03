# Cinemática de la cinta (Caso 0) en Blender

Animación de la cinemática final del Caso 0 «La cinta de las 3:12». Se arma con scripts (Blender 5.2) y se exporta a `Assets/Cinematicas/Caso0/cinta_caso0.glb`, que reproduce `Escenas/Cinematicas/CinematicaCinta.cs`.

Godot no importa esta carpeta (`.gdignore`).

## Archivos

| Archivo | Qué es |
|---|---|
| `caso0_cinta.blend` | La escena armada: la Sala 1, Porky, la figura, el reloj con su Rigid Body, la silla, la cuerda, la puerta, la cinta y la cámara. Se genera con `construir_cinta.py`; abrirlo sirve para ver y revisar, pero los cambios van en los scripts |
| `sala1.glb` | La Sala 1 exportada desde Godot con `Herramientas/exportar_sala_blender.gd`, con todo en su posición real |
| `reporte_choques.json` | Revisión automática: golpes del reloj, choques de los personajes con la mesa, las paredes, el piso y el techo, y cuánto le falta a cada mano para llegar a lo que toca |
| `scripts/coreografia.py` | Tiempos, recorridos, animaciones base y tomas de cámara (en coordenadas de Godot) |
| `scripts/construir_cinta.py` | Arma la escena, anima, hornea y exporta |
| `scripts/retarget.py` | Pasa las animaciones de Quaternius (UAL1/UAL2, CC0) al esqueleto de Porky |
| `scripts/vista_previa.py` | Renders rápidos (Workbench) para revisar poses y encuadres |
| `scripts/comun.py` | Utilidades comunes |

## Cómo regenerar

```
# 1. Si cambió la Sala 1 en Godot
Godot --headless --path game --script res://Herramientas/exportar_sala_blender.gd -- <ruta absoluta>/game/Assets/Cinematicas/Blender/sala1.glb

# 2. Armar, revisar choques y exportar el .glb
blender -b --python game/Assets/Cinematicas/Blender/scripts/construir_cinta.py -- --choques

# 3. Revisar cuadros con la cámara de cada toma (o una cámara libre)
blender -b game/Assets/Cinematicas/Blender/caso0_cinta.blend --python game/Assets/Cinematicas/Blender/scripts/vista_previa.py -- <carpeta> 21 40.1 48
```

Después de exportar, abrir Godot (o `--import`) para que reimporte `cinta_caso0.glb`.

## Cómo está hecho

- **Personajes.** Porky es su propio `porky.glb` (esqueleto de Animal Crossing) y la figura es el maniquí de Quaternius con gabardina (pesos automáticos) y capucha sin rostro. Las animaciones base son de Quaternius (CC0); a Porky se le pasan con `retarget.py`.
- **Contactos.** IK propio de dos huesos, horneado cuadro a cuadro: manos en la rejilla, la mesa, el reloj, la silla, los botones, la cinta y la manija. Los pies se apoyan en el piso (o en el asiento de la silla) cuadro a cuadro. El IK de Blender no sirve con el esqueleto de Porky porque sus huesos tienen colas de varios metros.
- **El reloj.** Cae con Rigid Body (cilindro de 14.5 cm de radio, piso y pared pasivos) y se hornea cuadro a cuadro.
- **Cámara.** Una toma por tramo con cortes secos, lente por toma y un leve movimiento de cámara en mano. El desenfoque lo pone Godot.
- **Exportación.** Una sola animación «Cinta» (pistas NLA) con Porky, la figura, la cámara, la cuerda y vacíos `Anim_*` que llevan el recorrido del reloj, la tapa de la rejilla, la puerta, la silla y la cinta. En Godot, los objetos reales de la sala copian esos vacíos.
