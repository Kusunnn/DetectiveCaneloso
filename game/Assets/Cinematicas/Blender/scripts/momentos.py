# Los 12 momentos congelados de la cinta (Caso 0), pose por pose. Lo lee construir_momentos.py.
#
# Coordenadas de Godot (x, y, z; y arriba). Giro del personaje en grados alrededor de Y (0 = mira a +Z).
# "fk": giro de cada hueso, en orden, en los ejes del personaje: "izq" (doblar hacia el frente: + inclina
# adelante el tronco y adelanta el muslo), "arriba" (voltear) y "frente" (ladear; en los brazos, + sube el
# izquierdo). Los nombres son los de retarget.H (Columna, Cabeza, BrazoI...) o los del esqueleto (Ear_1_L...).
# "ik": la muñeca (o el tobillo) va a un punto exacto; el segundo punto es hacia dónde apunta el codo.
#   ("local", x, y, z) es relativo al personaje (x a su izquierda, z a su frente); ("obj", nombre, punto) es un
#   punto del objeto tal como quedó en ese cuadro.
# "contactos": se corrige un parámetro paso a paso hasta dejar de atravesar un objeto (o, con "tocar", hasta
#   tocarlo): así lo que se apoya queda apoyado, sin meterse.
# "objetos": dónde queda cada objeto de la sala en el cuadro (None = como en el juego).
# "camara": inicio y fin del movimiento lento, lente en mm (sensor de 36 mm) y punto de enfoque.
PISO = 0.5
REJILLA = (-2.2, 3.49, 1.0)
TECHO = 3.5

X_PORKY = -0.45                    # Porky duerme en el lado izquierdo de la mesa, lejos del vaso y el cenicero
SILLA_PORKY = {"mover": (X_PORKY, 0, 0)}
RELOJ_PISO = (0.32, 0.52, -4.30)   # donde queda el reloj después de caer, boca arriba
TAPA_ADENTRO = {"pos": (-2.2, 3.72, 1.55)}     # la tapa, corrida adentro del ducto mientras la figura está en la sala

# Su cabeza es casi la mitad de la mesa y sus hombros quedan a la altura del tablero: sentado no alcanza a
# poner los brazos encima. Duerme como un niño en una mesa de adultos: hincado sobre el asiento, vencido hacia
# adelante, con el hocico sobre la mesa, la cabeza de lado y la gorra ladeada.
DORMIDO = {"pos": (X_PORKY, 0.95, -2.70), "giro": 0,
           "fk": {"MusloI": [("izq", 8)], "MusloD": [("izq", 4)], "PiernaI": [("izq", -100)], "PiernaD": [("izq", -96)],
                  "PieI": [("izq", -40)], "PieD": [("izq", -40)],
                  "Columna": [("izq", 4)], "Pecho": [("izq", 4)], "Cuello": [("izq", 10)],
                  "Skirt_1_LF": [("izq", -20)], "Skirt_1_RF": [("izq", -20)], "TailRoot": [("izq", -50)],
                  "Ear_1_L": [("izq", 25)], "Ear_1_R": [("izq", 25)],
                  "Cabeza": [("izq", 40), ("frente", 22), ("arriba", -12)], "KagoRoot": [("frente", 14)]},
           "ik": {"ManoI": ((X_PORKY - 0.08, 1.315, -2.50), ("local", 0.6, -0.1, 0.0)),
                  "ManoD": ((X_PORKY + 0.12, 1.315, -2.50), ("local", -0.6, -0.1, 0.0))},
           "contactos": [("Asiento", ("pos", "y"), -0.005, 60, "tocar"),
                         ("Travesano|Tablero", ("pos", "z"), -0.005, 10),
                         ("Respaldo", ("TailRoot", "izq"), -5, 10),
                         ("Tablero|Cenicero|cassette", ("Cabeza", "izq"), -1.0, 50),
                         ("Asiento", ("pos", "y"), 0.005, 10)]}

# La figura: brazos caídos desde la pose T
BRAZOS_ABAJO = {"BrazoI": [("frente", -72)], "BrazoD": [("frente", 72)]}


def figura(pos, giro, fk, ik=None, **extra):
    d = {"pos": pos, "giro": giro, "fk": fk}
    if ik: d["ik"] = ik
    d.update(extra)
    return d


CUADROS = [
    # 1 · 03:09 · La grabadora en primer plano con REC; Porky dormido, desenfocado, al fondo
    {"k": 1, "nombre": "La grabadora graba", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY},
     "camara": {"desde": [(0.78, 1.40, -1.18), (0.66, 1.37, -1.36)], "mira": [(0.36, 1.32, -1.72), (0.38, 1.32, -1.72)],
                "lente": 50, "foco": (0.40, 1.32, -1.68)},
     "libres": [((1.6, 1.8, -0.6), (0.0, 1.3, -2.2))]},

    # 2 · 03:09 · Porky dormido; paneo lateral
    {"k": 2, "nombre": "Porky dormido", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY},
     "camara": {"desde": [(1.45, 1.85, -1.55), (1.50, 1.85, -1.95)], "mira": [(-0.45, 1.55, -2.35), (-0.45, 1.55, -2.55)],
                "lente": 35, "foco": (-0.40, 1.55, -2.45)},
     "libres": [((-0.45, 1.9, -0.6), (-0.45, 1.4, -2.6)), ((-2.2, 1.5, -2.6), (-0.4, 1.3, -2.6))]},

    # 3 · 03:12 · La tapa de la rejilla a medio abrir, polvo en el aire y una mano enguantada asomando
    {"k": 3, "nombre": "La rejilla se abre", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY,
        "Rejilla": {"pos": (-2.25, 3.335, 1.0), "giro": [("z", -38)]}},
     "figura": figura((-2.2, 3.85, 2.30), 0, {"Cabeza": [("izq", 30)], "BrazoD": [("frente", 40)]},
                      ik={"ManoI": ((-2.04, 3.43, 0.86), ("local", 0.5, 0.3, 0.0))}, inclinar=[("x", -88)],
                      contactos=[("Techo", ("pos", "y"), 0.01, 10)]),
     "polvo": {"centro": (-2.15, 3.42, 1.0), "radio": 0.42, "cuantos": 260},
     "camara": {"desde": [(-1.45, 0.95, 1.75), (-1.58, 0.95, 1.80)], "mira": [(-2.10, 3.36, 0.92), (-2.16, 3.40, 0.92)],
                "lente": 45, "foco": (-2.2, 3.38, 0.95)},
     "luz": {"desde": (-2.2, 3.9, 1.05), "mira": (-2.2, 2.0, 1.0)},
     "libres": [((-0.6, 4.6, 2.2), (-2.2, 3.75, 1.6)), ((-1.5, 2.7, 1.9), (-2.2, 3.45, 1.0))]},

    # 4 · 03:12 · La figura cuelga de la orilla del hueco, a medio descender; contrapicado que gira despacio
    {"k": 4, "nombre": "La figura cuelga", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY,
        "Rejilla": TAPA_ADENTRO},
     "figura": figura((-2.2, 1.56, 1.02), 0,
                      {"MusloI": [("izq", 24)], "PiernaI": [("izq", -40)], "MusloD": [("izq", 6)], "PiernaD": [("izq", -18)],
                       "PieI": [("izq", 25)], "PieD": [("izq", 30)], "Columna": [("izq", -4)], "Cabeza": [("izq", 30), ("arriba", 15)]},
                      ik={"ManoI": ((-2.00, 3.44, 0.96), ("local", 0.6, 2.6, -0.3)), "ManoD": ((-2.40, 3.44, 1.04), ("local", -0.6, 2.6, -0.3))}),
     "cuerda": ((-2.06, 3.47, 1.20), (-2.06, 0.52, 1.20)),
     "polvo": {"centro": (-2.2, 3.1, 1.0), "radio": 0.6, "cuantos": 90},
     "camara": {"desde": [(-1.15, 0.72, 2.35), (-0.80, 0.72, 1.75)], "mira": [(-2.2, 2.45, 1.0), (-2.2, 2.50, 1.0)],
                "lente": 24, "foco": (-2.2, 2.4, 1.0)},
     "luz": {"desde": (-2.2, 3.3, -0.6), "mira": (-2.2, 2.3, 1.0)},
     "libres": [((-0.5, 2.0, 1.0), (-2.2, 2.3, 1.0))]},

    # 5 · 03:12 · Inclinada sobre la grabadora, la capucha junto al micrófono; Porky dormido al fondo
    {"k": 5, "nombre": "El susurro", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY, "Rejilla": TAPA_ADENTRO},
     # Agachada junto a la orilla, asomada sobre el tablero con la capucha pegada al micrófono
     "figura": figura((0.50, 0.5, -1.18), 180,
                      {"MusloI": [("izq", 95)], "PiernaI": [("izq", -120)], "MusloD": [("izq", 70)], "PiernaD": [("izq", -125)],
                       "PieI": [("izq", 25)], "PieD": [("izq", 45)],
                       "Columna": [("izq", 22)], "Pecho": [("izq", 14)], "Cuello": [("izq", 6)],
                       "Cabeza": [("izq", 18), ("arriba", -20), ("frente", -14)]},
                      ik={"ManoI": ((0.22, 1.36, -1.60), ("local", 0.6, 0.0, -0.3)), "ManoD": ((0.74, 1.36, -1.62), ("local", -0.6, 0.0, -0.3))},
                      fk_final={"ManoI": [("izq", 180)], "ManoD": [("izq", 180)]},
                      contactos=[("CSGBox", ("pos", "y"), -0.005, 120, "tocar"), ("Tablero|cassette|Travesano", ("pos", "z"), 0.01, 30)]),
     "camara": {"desde": [(1.65, 1.62, -0.95), (1.48, 1.58, -1.12)], "mira": [(0.45, 1.50, -1.62), (0.45, 1.48, -1.64)],
                "lente": 35, "foco": (0.45, 1.48, -1.62)},
     "luz": {"desde": (1.9, 2.3, -2.7), "mira": (0.8, 1.5, -1.65)},
     "libres": [((0.9, 1.8, 0.2), (0.7, 1.3, -1.7)), ((2.5, 1.5, -1.7), (0.7, 1.3, -1.7))]},

    # 6 · 03:12 · De puntas, con el reloj recién descolgado en las manos, voltea hacia Porky
    {"k": 6, "nombre": "El reloj descolgado", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY, "Rejilla": TAPA_ADENTRO,
        "Reloj": {"pos": (0.0, 1.78, -4.68), "giro": [("x", 8)]}},
     "figura": figura((0.0, 0.5, -4.42), 180,
                      {"PieI": [("izq", 38)], "PieD": [("izq", 42)], "Columna": [("izq", -4), ("arriba", 10)], "Pecho": [("arriba", 12)],
                       "Cuello": [("arriba", 20)], "Cabeza": [("arriba", 48), ("izq", 10)]},
                      ik={"ManoI": (("obj", "Reloj", (-0.19, 0.0, 0.02)), ("local", 0.7, -0.3, -0.2)),
                          "ManoD": (("obj", "Reloj", (0.19, 0.0, 0.02)), ("local", -0.7, -0.3, -0.2))},
                      contactos=[("wall_clock", ("pos", "z"), 0.01, 10)]),
     "camara": {"desde": [(-1.55, 1.95, -3.25), (-1.35, 1.95, -3.45)], "mira": [(0.0, 1.95, -4.50), (0.0, 1.98, -4.52)],
                "lente": 30, "foco": (0.0, 1.6, -4.6)},
     "luz": {"desde": (-1.4, 2.6, -4.2), "mira": (0.0, 1.6, -4.5)},
     "libres": [((-0.3, 1.6, -2.9), (0.0, 1.4, -4.6))]},

    # 7 · 03:12 · El reloj suspendido a mitad de la caída; la figura, de espaldas, ya se va hacia la rejilla
    {"k": 7, "nombre": "El reloj cae", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY, "Rejilla": TAPA_ADENTRO,
        "Reloj": {"pos": (0.28, 1.02, -4.42), "giro": [("x", -55), ("y", 25), ("z", 18)]}},
     "figura": figura((-1.10, 0.5, -3.95), -17,
                      {"MusloI": [("izq", 28)], "PiernaI": [("izq", -18)], "MusloD": [("izq", -22)], "PiernaD": [("izq", -40)],
                       "PieD": [("izq", 20)], "Columna": [("izq", 8)],
                       "BrazoI": [("frente", -70), ("izq", -25)], "BrazoD": [("frente", 70), ("izq", 25)],
                       "AntebrazoI": [("izq", 20)], "AntebrazoD": [("izq", 25)], "Cabeza": [("arriba", -25)]}),
     "camara": {"desde": [(1.25, 1.05, -4.80), (1.22, 1.05, -4.78)], "mira": [(-0.80, 1.20, -3.95), (-0.82, 1.20, -3.93)],
                "lente": 28, "foco": (0.28, 1.02, -4.42)},
     "luz": {"desde": (-1.4, 2.7, -2.2), "mira": (-1.1, 1.3, -3.95)},
     "libres": [((-2.5, 1.6, -4.4), (-0.6, 1.0, -3.4))]},

    # 8 · 03:12 · Inserto: la carátula en el piso en 3:12; arriba, la pluma negra en la rejilla mal cerrada
    {"k": 8, "nombre": "La carátula y la pluma", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY,
        "Reloj": {"pos": RELOJ_PISO, "giro": [("x", -90), ("y", 24)]},
        "Rejilla": {"pos": (-2.165, 3.484, 1.0), "giro": [("y", 6), ("x", 3)]}},
     "camara": {"desde": [(2.40, 1.60, -3.40)] * 2, "mira": [RELOJ_PISO, (-2.15, 3.47, 1.02)],
                "lente": 85, "foco": RELOJ_PISO},
     "luz": {"desde": (0.32, 2.3, -4.3), "mira": RELOJ_PISO},
     "libres": [((0.6, 1.2, -3.8), RELOJ_PISO)]},

    # 9 · 03:15 · Porky despierta de golpe, los ojos muy abiertos, mirando el reloj en el piso
    {"k": 9, "nombre": "Porky despierta", "objetos": {"Silla": {"mover": (X_PORKY, 0, -0.10)},
        "Reloj": {"pos": RELOJ_PISO, "giro": [("x", -90), ("y", 24)]},
        "Rejilla": {"pos": (-2.165, 3.484, 1.0), "giro": [("y", 6), ("x", 3)]}},
     "porky": {"pos": (X_PORKY, 0.95, -2.78), "giro": 0,
               "fk": {"MusloI": [("izq", 8)], "MusloD": [("izq", 4)], "PiernaI": [("izq", -100)], "PiernaD": [("izq", -96)],
                      "PieI": [("izq", -40)], "PieD": [("izq", -40)], "TailRoot": [("izq", -50)],
                      "Columna": [("izq", 2), ("arriba", 20)], "Pecho": [("izq", -4), ("arriba", 15)],
                      "Cuello": [("arriba", 25)], "Cabeza": [("arriba", 55), ("izq", 18)],
                      "Ear_1_L": [("izq", -35)], "Ear_1_R": [("izq", -35)], "KagoRoot": [("frente", 14)],
                      "BrazoI": [("frente", 35)], "BrazoD": [("frente", -35)], "AntebrazoI": [("arriba", 40)], "AntebrazoD": [("arriba", -40)]},
               "contactos": [("Asiento", ("pos", "y"), -0.005, 60, "tocar"), ("Respaldo", ("pos", "z"), 0.01, 20),
                             ("Tablero|Travesano", ("Columna", "izq"), -1.0, 20), ("Asiento", ("pos", "y"), 0.005, 10)]},
     "camara": {"desde": [(1.35, 1.35, -4.45), (1.25, 1.38, -4.32)], "mira": [(-0.45, 1.70, -2.75), (-0.45, 1.70, -2.75)],
                "lente": 35, "foco": (-0.40, 1.75, -2.85)},
     "luz": {"desde": (0.9, 2.4, -3.6), "mira": (-0.45, 1.7, -2.8)},
     "libres": [((-0.45, 1.9, -1.0), (-0.45, 1.6, -2.8))]},

    # 10 · 03:15 · De pie sobre su silla, junto a la pared, cuelga el reloj con las dos manos
    {"k": 10, "nombre": "Porky cuelga el reloj", "objetos": {
        "Silla": {"mover": (-0.18, 0, -1.85), "giro": [("y", 90)]},
        "Rejilla": {"pos": (-2.165, 3.484, 1.0), "giro": [("y", 6), ("x", 3)]}},
     "porky": {"pos": (-0.05, 1.06, -4.62), "giro": 180,
               "fk": {"PieI": [("izq", 25)], "PieD": [("izq", 25)], "Columna": [("izq", -10)], "Pecho": [("izq", -8)],
                      "Cuello": [("izq", -20)], "Cabeza": [("izq", -38), ("arriba", 14)], "KagoRoot": [("frente", 14)]},
               "ik": {"ManoI": (("obj", "Reloj", (-0.12, -0.11, 0.02)), ("local", 0.6, -0.2, 0.0)),
                      "ManoD": (("obj", "Reloj", (0.12, -0.11, 0.02)), ("local", -0.6, -0.2, 0.0))},
               "contactos": [("Asiento", ("pos", "y"), -0.005, 40, "tocar"), ("Pared|wall_clock", ("pos", "z"), 0.01, 20),
                             ("Pared|wall_clock", ("Cabeza", "izq"), -2.0, 15)]},
     "camara": {"desde": [(2.1, 1.55, -2.7), (2.0, 1.55, -2.85)], "mira": [(0.0, 1.55, -4.6), (0.0, 1.6, -4.65)],
                "lente": 35, "foco": (0.0, 1.6, -4.7)},
     "luz": {"desde": (0.0, 3.0, -3.6), "mira": (0.0, 1.8, -4.8)},
     "libres": [((-1.6, 1.6, -4.3), (0.0, 1.5, -4.7))]},

    # 11 · 03:15 · Frente a la grabadora, la cara roja por la luz de REC, la cinta a medio salir en la mano
    {"k": 11, "nombre": "Porky saca la cinta", "objetos": {
        "Rejilla": {"pos": (-2.165, 3.484, 1.0), "giro": [("y", 6), ("x", 3)]},
        "Cinta": {"pos": (0.39, 1.352, -1.672), "giro": [("x", 32)]}},
     "porky": {"pos": (0.05, 0.95, -1.20), "giro": 180,
               "fk": {"MusloI": [("izq", 8)], "MusloD": [("izq", 4)], "PiernaI": [("izq", -100)], "PiernaD": [("izq", -96)],
                      "PieI": [("izq", -40)], "PieD": [("izq", -40)], "TailRoot": [("izq", -50)],
                      "Columna": [("izq", 24)], "Pecho": [("izq", 12)], "Cuello": [("izq", 6)],
                      "Cabeza": [("izq", 12), ("arriba", -10)], "Ear_1_L": [("izq", -25)], "Ear_1_R": [("izq", -25)],
                      "KagoRoot": [("frente", 14)]},
               "ik": {"ManoD": ((0.37, 1.39, -1.64), ("local", -0.6, 0.0, -0.2)), "ManoI": ((-0.05, 1.32, -1.58), ("local", 0.6, 0.0, -0.2))},
               "contactos": [("Asiento", ("pos", "y"), -0.005, 60, "tocar"), ("Respaldo", ("pos", "z"), -0.01, 20),
                             ("Tablero|Travesano|cassette", ("Columna", "izq"), -1.0, 25), ("Asiento", ("pos", "y"), 0.005, 10)]},
     "camara": {"desde": [(1.30, 1.50, -2.65), (1.18, 1.52, -2.52)], "mira": [(0.10, 1.65, -1.45), (0.10, 1.65, -1.45)],
                "lente": 28, "foco": (0.10, 1.70, -1.40)},
     "libres": [((1.6, 1.6, -1.4), (0.2, 1.5, -1.5))]},

    # 12 · 03:16 · En la puerta, de espaldas, con la cinta; sus botas en primer plano, dejando huella
    {"k": 12, "nombre": "Porky se va", "objetos": {
        "Rejilla": {"pos": (-2.165, 3.484, 1.0), "giro": [("y", 6), ("x", 3)]},
        "Puerta": {"pos": (2.928, 1.55, 3.499), "giro": [("y", -88)]},
        "Cinta": {"en_mano": ("Porky", "ManoD", (-0.09, -0.03, 0.0)), "giro": [("x", 70), ("z", -20)]}},
     "porky": {"pos": (2.47, 0.5, 2.40), "giro": 0,
               "fk": {"MusloI": [("izq", 26)], "PiernaI": [("izq", -12)], "PieI": [("izq", -8)],
                      "MusloD": [("izq", -20)], "PiernaD": [("izq", -38)], "PieD": [("izq", 28)],
                      "Columna": [("izq", 6), ("arriba", 8)], "Cabeza": [("izq", 10), ("arriba", -12)],
                      "BrazoI": [("frente", -75)], "BrazoD": [("frente", 75)]},
               "contactos": [("Marco|Puerta|Pared", ("pos", "z"), -0.01, 30)]},
     "luz": {"desde": (2.15, 1.8, 0.9), "mira": (2.45, 0.6, 2.4)},
     "camara": {"desde": [(2.20, 0.60, 1.25), (2.26, 0.60, 1.45)], "mira": [(2.45, 0.62, 2.60), (2.45, 0.64, 2.70)],
                "lente": 24, "foco": (2.45, 0.6, 2.9)},
     "libres": [((3.6, 1.5, 1.6), (2.5, 1.0, 3.0))]},
]
