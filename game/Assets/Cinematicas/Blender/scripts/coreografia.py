# Coreografía de la cinemática de la cinta (Caso 0). Tiempos en segundos de la cinemática (t), posiciones en
# coordenadas de Godot (x, y, z; y arriba) y giros en grados alrededor de Y (0 = mira hacia +Z), igual que en
# CinematicaCinta.cs, para poder comparar con el juego. construir_cinta.py lo convierte a Blender.
# "RELOJ" en una ruta se reemplaza por el punto frente al reloj caído (sale de la simulación).

FIN = 59.7

# ---------- Momentos ----------
T_PLAY = 0.25
T_IMAGEN = 4.2           # termina el negro: SALA 1 · 03:09
T_REJILLA = 12.0         # la rejilla vibra y suena
T_TAPA = 12.6            # la tapa sube y se desliza hacia adentro del ducto
T_CUERDA = 13.2          # cae la cuerda negra (la figura la amarró arriba para volver a subir)
T_PIERNAS = 13.6         # asoman las piernas
T_CUELGA = 14.9          # cuelga de las manos, con los brazos estirados
T_SUELTA = 15.3
T_ATERRIZA = 15.75       # cae 1 m: 0.45 s
T_SUSURRO = 20.1
T_DESCUELGA = 24.95      # toma el reloj con las dos manos y lo saca del clavo
T_SUELTA_RELOJ = 26.0    # lo deja caer a propósito, mirando a Porky
T_SUBE = 29.9            # empieza a subir por la cuerda
T_SE_VA = 32.0           # desaparece por el ducto
T_RECOGE_CUERDA = 32.05
T_CIERRA_TAPA = 32.5     # la tapa vuelve mal puesta; la pluma queda enganchada
T_DESPIERTA = 33.6       # 03:15: el golpe lo despertó; se endereza de un salto
T_PARA = 35.8            # se levanta de la silla
T_EMPUJA = 37.8          # empuja su silla hasta la pared, bajo el clavo
T_SILLA_PARED = 39.3
T_AGARRA = 40.0          # toma el reloj del piso con las dos manos
T_SUBE_SILLA = 41.0      # se sube a la silla con el reloj
T_ARRIBA = 41.45
T_CUELGA_RELOJ = 42.3
T_BAJA_SILLA = 42.6
T_ABAJO = 43.0
T_JALA = 43.4            # regresa la silla a su lugar
T_SILLA_MESA = 44.9
T_VE_REC = 46.0
T_SOBRESALTO = 46.45
T_STOP = 48.0
T_REBOBINA = 48.4
T_FIN_REBOBINA = 50.9
T_EXPULSA = 51.1
T_TOMA_CINTA = 51.5
T_ABRE_PUERTA = 55.0
T_CIERRA_PUERTA = 57.7

# ---------- Lugares ----------
SILLA = (0.0, 0.5, -2.8)                 # silla de Porky (la del sospechoso)
SILLA_ATRAS = (0.0, 0.5, -3.05)          # al levantarse la empuja hacia atrás
SILLA_PARED = (-0.3, 0.5, -4.62)          # bajo el clavo, con el respaldo contra la pared
ASIENTO = 0.47                           # altura del asiento sobre el piso
ANTE_SILLA = 0.52                        # Porky empuja la silla desde su frente
BAJO_REJILLA = (-2.2, 0.5, 1.0)
REJILLA = (-2.2, 3.49, 1.0)
TECHO = 3.5
JUNTO_GRABADORA = (1.08, 0.5, -1.65)     # la figura, inclinada sobre la orilla derecha de la mesa
FRENTE_GRABADORA = (0.45, 0.5, -1.2)    # Porky, del lado del detective, frente a los botones
PARED_RELOJ = (0.0, 0.5, -4.5)           # la figura, frente al reloj
SUELTA_RELOJ = (0.62, 0.5, -4.15)        # la figura da un paso a un lado con el reloj y lo suelta
PUERTA_FUERA = (1.95, 0.5, 2.6)         # Porky, junto a la manija y fuera del barrido de la puerta

# ---------- Porky ----------
# Recorrido: (t, posición, giro). caer=True acelera el tramo.
RUTA_PORKY = [
    (0, SILLA, 0), (T_PARA, SILLA, 0), (T_PARA + 1.2, (0.0, 0.5, -2.62), 0),
    # Se da la vuelta hacia su silla y la empuja hasta la pared, bajo el clavo
    (37.1, (0.0, 0.5, -2.62), 0), (37.6, (0.0, 0.5, -2.62), 180),
    (T_EMPUJA, (0.0, 0.5, -2.62), 180), (T_SILLA_PARED, (SILLA_PARED[0], 0.5, SILLA_PARED[2] + ANTE_SILLA), 180),
    # Se acerca al reloj caído y lo recoge
    (39.65, "RELOJ", 175), (40.6, "RELOJ", 175),
    # Se sube a la silla y lo cuelga; se baja
    (T_SUBE_SILLA, (SILLA_PARED[0], 0.5, SILLA_PARED[2] + ANTE_SILLA), 180), (T_ARRIBA, SILLA_PARED, 145),
    (T_BAJA_SILLA, SILLA_PARED, 145), (T_ABAJO, (SILLA_PARED[0], 0.5, SILLA_PARED[2] + ANTE_SILLA), 180),
    # Regresa la silla a la mesa caminando hacia atrás
    (T_JALA, (SILLA_PARED[0], 0.5, SILLA_PARED[2] + ANTE_SILLA), 180), (T_SILLA_MESA, (0.0, 0.5, SILLA_ATRAS[2] + ANTE_SILLA), 180),
    # Va junto a la grabadora, rodeando la mesa por la derecha
    (45.2, (0.5, 0.5, -2.85), 60), (45.6, (1.15, 0.5, -2.4), 10), (45.95, (1.1, 0.5, -1.25), -40),
    (T_VE_REC + 0.25, FRENTE_GRABADORA, 180), (51.9, FRENTE_GRABADORA, 180),
    # Sale con la cinta hacia la puerta
    (52.4, (1.15, 0.5, -0.9), 50), (53.7, (1.75, 0.5, 1.0), 5), (54.8, PUERTA_FUERA, 25),
    (55.7, PUERTA_FUERA, 25), (56.25, (2.35, 0.5, 3.05), 10), (56.85, (2.1, 0.5, 4.15), -70), (58.6, (0.2, 0.5, 4.3), -90),
    (FIN, (-1.2, 0.5, 4.3), -90),
]

# Animaciones base (Quaternius): (t, acción, segundo de inicio, velocidad, bucle, mezcla en s)
ANIM_PORKY = [
    (0, "Sitting_Idle_Loop", 0, 0.8, True, 0.0),
    (T_PARA, "Sitting_Exit", 0, 0.9, False, 0.25),
    (T_PARA + 1.15, "Idle_Loop", 0, 1, True, 0.3),
    (T_EMPUJA, "Walk_Loop", 0, 0.9, True, 0.3),
    (T_SILLA_PARED, "Idle_Loop", 0, 1, True, 0.3),
    (39.65, "Crouch_Idle_Loop", 0.4, 0.6, True, 0.35),
    (40.35, "Idle_Loop", 0, 1, True, 0.4),
    (T_SUBE_SILLA, "Walk_Loop", 0.2, 0.7, True, 0.2),
    (T_ARRIBA, "Idle_Loop", 0, 1, True, 0.3),
    (T_BAJA_SILLA, "Walk_Loop", 0.6, -0.7, True, 0.2),
    (T_ABAJO, "Idle_Loop", 0, 1, True, 0.25),
    (T_JALA, "Walk_Loop", 0, -0.85, True, 0.3),        # hacia atrás, jalando la silla
    (T_SILLA_MESA, "Walk_Loop", 0, 1.1, True, 0.3),
    (T_VE_REC, "Idle_Loop", 0, 1, True, 0.3),
    (T_SOBRESALTO, "Hit_Chest", 0, 0.45, False, 0.12),
    (47.2, "Idle_Loop", 0, 1, True, 0.4),
    (51.9, "Walk_Loop", 0, 1.1, True, 0.3),
    (54.8, "Idle_Loop", 0, 1, True, 0.25),
    (55.7, "Walk_Loop", 0, 1, True, 0.25),
]

# ---------- La figura ----------
RUTA_FIGURA = [
    # Arriba, en el ducto (fuera de vista); baja con las piernas primero y queda colgada de las manos
    (0, (-2.2, 3.75, 0.95), 180), (T_PIERNAS, (-2.2, 3.6, 0.95), 180),
    (T_CUELGA, (-2.2, 1.5, 0.95), 180), (T_SUELTA, (-2.2, 1.5, 0.95), 180),
    (T_ATERRIZA, (-2.2, 0.5, 0.95), 180, True),
    # Agachada hasta la grabadora
    (16.6, (-2.2, 0.5, 0.95), 160), (19.1, JUNTO_GRABADORA, -100),
    (22.9, JUNTO_GRABADORA, -100),
    # A la pared del reloj; lo saca del clavo, da un paso a un lado y lo suelta
    (23.6, (1.0, 0.5, -2.9), 190), (24.6, PARED_RELOJ, 180),
    (25.35, PARED_RELOJ, 180), (25.9, SUELTA_RELOJ, 165),
    (26.9, SUELTA_RELOJ, 165), (27.2, SUELTA_RELOJ, 120),
    # De vuelta a la cuerda, rodeando la mesa por la izquierda
    (28.1, (-1.3, 0.5, -3.1), 330), (29.5, (-2.2, 0.5, 0.88), 180), (T_SUBE, (-2.2, 0.5, 0.88), 180),
    # Sube por la cuerda, por el centro del hueco
    (T_SE_VA, (-2.2, 3.75, 0.95), 180), (FIN, (-2.2, 3.8, 0.95), 180),
]

ANIM_FIGURA = [
    (0, "Jump_Loop", 0.6, 0.0, True, 0.0),                 # colgada: la pose de brazos la pone el IK
    (T_ATERRIZA - 0.06, "NinjaJump_Land", 0, 0.9, False, 0.08),
    (16.6, "Crouch_Fwd_Loop", 0, 1.0, True, 0.35),
    (19.1, "Idle_Loop", 0, 0.7, True, 0.4),
    (22.9, "Walk_Loop", 0, 1.1, True, 0.35),
    (24.6, "Idle_Loop", 0, 0.8, True, 0.3),
    (25.35, "Walk_Loop", 0, 0.8, True, 0.2),
    (25.9, "Idle_Loop", 0, 0.8, True, 0.2),
    (27.2, "Jog_Fwd_Loop", 0, 1.0, True, 0.3),
    (29.5, "Idle_Loop", 0, 1.0, True, 0.3),
    (T_SUBE, "Jump_Loop", 0.6, 0.0, True, 0.25),          # trepando: brazos con IK, piernas en la coreografía
]

# ---------- Cámara ----------
# (t de inicio, desde_inicio, desde_fin, mira_inicio, mira_fin, fov vertical en grados).
# None: la toma se calcula en construir_cinta.py (inserto de la carátula).
TOMAS = [
    (0.0, (0.68, 1.37, -1.24), (0.68, 1.37, -1.24), (0.4, 1.31, -1.73), (0.4, 1.31, -1.73), 34),
    (T_IMAGEN, (0.68, 1.37, -1.24), (0.62, 1.35, -1.31), (0.4, 1.31, -1.73), (0.38, 1.31, -1.75), 34),
    (8.4, (3.3, 3.05, 2.3), (3.1, 2.95, 2.0), (-0.4, 1.1, -2.7), (-0.6, 1.1, -2.5), 55),
    (11.6, (-1.2, 0.75, -0.25), (-1.25, 0.72, -0.2), (-2.2, 3.3, 1.0), (-2.2, 1.6, 1.0), 62),
    (17.2, (2.0, 1.75, 0.6), (1.75, 1.6, 0.2), (0.6, 1.3, -1.6), (0.6, 1.35, -1.75), 44),
    (23.0, (-2.3, 1.6, -3.1), (-2.15, 1.5, -3.35), (0.1, 1.8, -4.5), (0.3, 1.2, -4.6), 42),
    (27.45, None, None, None, None, 40),
    (29.6, (3.3, 2.4, -4.3), (3.2, 2.35, -4.1), (-1.6, 1.9, 0.7), (-2.1, 3.0, 1.0), 42),
    (33.3, (0.2, 1.5, -0.75), (0.15, 1.52, -0.9), (0.0, 1.6, -2.7), (0.0, 1.75, -2.6), 46),
    # Porky empuja la silla hasta la pared
    (37.1, (-2.3, 1.35, -2.9), (-2.25, 1.4, -3.6), (0.0, 1.0, -3.2), (0.0, 1.1, -4.4), 50),
    # Recoge el reloj, se sube a la silla y lo cuelga
    (39.45, (-2.6, 1.0, -3.1), (-2.5, 1.1, -3.3), (0.4, 0.75, -4.3), (0.3, 1.0, -4.4), 48),
    (40.7, (2.3, 1.7, -3.55), (2.15, 1.75, -3.75), (-0.05, 1.8, -4.8), (-0.05, 1.85, -4.85), 46),
    # Se baja y regresa la silla
    (42.6, (1.9, 2.1, -1.1), (1.85, 2.05, -1.3), (0.0, 1.0, -4.2), (0.0, 1.0, -3.3), 46),
    # La luz de REC (lo que ve Porky)
    (45.75, (0.02, 1.37, -1.33), (0.03, 1.37, -1.36), (0.44, 1.33, -1.68), (0.44, 1.33, -1.68), 30),
    # Contraplano: se sobresalta
    (46.3, (0.3, 1.6, -2.62), (0.32, 1.6, -2.58), (0.45, 1.65, -1.33), (0.45, 1.68, -1.33), 44),
    # Inserto de la grabadora: STOP, rebobinado, la cinta sale
    (47.6, (-0.06, 1.325, -1.42), (-0.05, 1.325, -1.43), (0.42, 1.34, -1.72), (0.42, 1.34, -1.72), 34),
    # Sale con la cinta
    (51.9, (-1.6, 2.2, -1.4), (-1.45, 2.15, -1.15), (1.6, 1.3, 0.4), (2.2, 1.4, 2.4), 42),
    (56.3, (0.2, 0.6, 4.55), (0.25, 0.6, 4.55), (1.9, 0.55, 4.0), (1.2, 0.55, 4.25), 46),
    (57.5, (-1.45, 2.15, -1.15), (-1.4, 2.12, -1.05), (2.3, 1.4, 2.6), (2.4, 1.4, 2.8), 42),
]
