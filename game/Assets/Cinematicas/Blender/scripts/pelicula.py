# La cinemática de la cinta (Caso 0) como película: presente → cinta → presente. Lo lee construir_momentos.py
# con --datos pelicula. Mismo formato que momentos.py (de donde toma las poses del pasado), más:
# - Caneloso en el presente (su modelo del juego, exportado con Herramientas/exportar_modelo_blender.gd).
# - "godot": lo que reproduce CinematicaCintaPelicula.cs y no va en el .glb (se exporta a cinta_pelicula.json):
#     dur (s), epoca ("presente" o "cinta"), entrada (corte, match, tracking, seco, negro), hora (título a máquina),
#     luces [{tipo, desde, mira, color, energia, angulo, rango}], mueve (carretes, caida, rebobina),
#     sonidos [{t (desde el inicio del cuadro; negativo = empieza antes del corte), archivo, vol, limpio}],
#     subtitulo {linea, desde, hasta}, musica (0–1, volumen al que llega en el cuadro).
# Coordenadas de Godot; luces con color RGB 0–1.
from momentos import PISO, REJILLA, TECHO, X_PORKY, SILLA_PORKY, RELOJ_PISO, TAPA_ADENTRO, DORMIDO, figura
import momentos as MM

CANELOSO = True


def de(k, quien):
    "La pose de un personaje en el cuadro k de los momentos congelados"
    return next(c for c in MM.CUADROS if c["k"] == k)[quien]


def objetos_de(k):
    return dict(next(c for c in MM.CUADROS if c["k"] == k).get("objetos", {}))


# ---------- Luces (motivadas) ----------
def spot(desde, mira, color, energia, angulo=30, rango=8):
    return {"tipo": "spot", "desde": desde, "mira": mira, "color": color, "energia": energia, "angulo": angulo, "rango": rango}


def omni(desde, color, energia, rango=6):
    return {"tipo": "omni", "desde": desde, "color": color, "energia": energia, "rango": rango}


CALIDA = (1.0, 0.82, 0.6)
FRIA = (0.62, 0.74, 1.0)
LAMPARA = (0.0, 2.55, -2.0)            # la lámpara del techo sobre la mesa
DUCTO = (-2.2, 3.4, 1.0)
REJILLA_PLUMA = (-2.165, 3.48, 1.0)
# Relleno frío de la lámpara para todo el pasado: oscuro pero legible
RELLENO_CINTA = omni((0.0, 2.4, -1.6), (0.7, 0.8, 1.0), 1.1, 9)
RELLENO_PRESENTE = omni((1.2, 2.2, -0.6), (1.0, 0.86, 0.7), 0.7, 9)

# ---------- Caneloso en el presente ----------
# Es pequeño y cabezón (los hombros le quedan a la altura del tablero): se para pegado a la mesa, a la derecha
# de la silla del detective, y alcanza la grabadora con la mano izquierda.
BOTON_PLAY = (0.405, 1.356, -1.598)
BOTON_REW = (0.372, 1.356, -1.598)
CANELOSO_X, CANELOSO_Z = 0.66, -1.30
PLAY = {"pos": (CANELOSO_X, 0.5, CANELOSO_Z), "giro": 180,
        "fk": {"Columna": [("izq", 10)], "Pecho": [("izq", 6)], "Cuello": [("izq", 8)],
               "Cabeza": [("izq", 18), ("arriba", 18)], "BrazoD": [("frente", 70)], "AntebrazoD": [("izq", 15)],
               "PieI": [("izq", 28)], "PieD": [("izq", 20)]},
        "ik": {"ManoI": (BOTON_PLAY, ("local", 0.5, -0.2, -0.2))},
        "contactos": [("Tablero|cassette|Asiento|Respaldo|Poste", ("pos", "z"), 0.01, 8),
                      ("Tablero|cassette", ("Cabeza", "izq"), -2.0, 15), ("Tablero|cassette", ("Pecho", "izq"), -2.0, 8)]}
REW = dict(PLAY, ik={"ManoI": (BOTON_REW, ("local", 0.5, -0.2, -0.2))})
# Voltea hacia la rejilla del techo, atrás y a su izquierda, con la mano todavía en la grabadora
MIRA = dict(PLAY, fk={"Columna": [("izq", 2), ("arriba", 16)], "Pecho": [("arriba", 18)], "Cuello": [("arriba", 22), ("izq", -12)],
                      "Cabeza": [("arriba", 34), ("izq", -24)], "BrazoD": [("frente", 70)], "AntebrazoD": [("izq", 15)],
                      "Ear_1_L": [], })
MIRA["fk"].pop("Ear_1_L")

# La cinta adentro de la grabadora en el presente (Caneloso la acaba de poner); los carretes, sobre ella
CARRETES = [((0.367, 1.336, -1.680), 0.0115, (0.367, 1.5, -1.680)), ((0.413, 1.336, -1.680), 0.0115, (0.413, 1.5, -1.680))]

CUADROS = [
    # ================= A. PRESENTE =================
    {"k": 1, "nombre": "Caneloso pone PLAY", "caneloso": PLAY,
     "camara": {"desde": [(1.95, 1.85, -2.85), (1.85, 1.85, -2.75)], "mira": [(0.55, 1.45, -1.40), (0.55, 1.46, -1.40)],
                "lente": 28, "foco": (0.6, 1.4, -1.35)},
     "godot": {"dur": 3.6, "epoca": "presente", "entrada": "negro", "hora": "SALA 1 · 06:58",
               "luces": [spot(LAMPARA, (0.6, 1.4, -1.35), CALIDA, 2.0, 40), spot((-0.4, 2.2, -0.3), (0.6, 1.5, -1.3), (1.0, 0.9, 0.8), 2.0, 30),
                         RELLENO_PRESENTE],
               "sonidos": [{"t": 2.6, "archivo": "Cinta/cinta_play.wav", "vol": -4, "limpio": True}], "musica": 0.0},
     "libres": [((2.4, 1.5, -1.0), (0.6, 1.3, -1.3)), ((0.6, 1.9, -2.6), (0.6, 1.3, -1.3))]},

    {"k": 2, "nombre": "Los carretes giran", "caneloso": PLAY, "carretes": CARRETES,
     "camara": {"desde": [(0.40, 1.52, -1.48), (0.413, 1.385, -1.655)], "mira": [(0.392, 1.333, -1.682), (0.413, 1.336, -1.679)],
                "lente": 50, "foco": (0.413, 1.336, -1.68)},
     "godot": {"dur": 3.4, "epoca": "presente", "entrada": "corte", "mueve": "carretes", "exposicion": 0.45,
               "luces": [spot((0.45, 1.75, -1.45), (0.4, 1.33, -1.68), CALIDA, 0.45, 30), RELLENO_PRESENTE],
               "sonidos": [{"t": 0.0, "archivo": "Cinta/siseo_cinta.wav", "vol": -10, "limpio": True}],
               "subtitulo": {"linea": 0, "desde": 0.2, "hasta": 3.4}, "musica": 0.0}},

    # ================= B. LA CINTA =================
    {"k": 3, "nombre": "El reloj marca las 3:09", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY},
     "camara": {"desde": [(0.0, 1.90, -4.62), (0.0, 1.90, -4.50)], "mira": [(0.0, 1.90, -4.95), (0.0, 1.90, -4.95)],
                "lente": 50, "foco": (0.0, 1.9, -4.95)},
     "godot": {"dur": 2.6, "epoca": "cinta", "entrada": "match", "hora": "SALA 1 · 03:09",
               "luces": [spot((0.5, 2.4, -4.2), (0.0, 1.9, -4.95), FRIA, 0.6, 35), RELLENO_CINTA], "musica": 0.15}},

    {"k": 4, "nombre": "Plano de situación", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY},
     "camara": {"desde": [(3.80, 3.30, 2.30), (3.75, 3.26, 2.10)], "mira": [(-1.0, 1.45, -2.2), (-1.0, 1.45, -2.35)],
                "lente": 16, "foco": (-0.4, 1.5, -2.6)},
     "godot": {"dur": 3.6, "epoca": "cinta", "entrada": "corte",
               "luces": [spot(LAMPARA, (-0.3, 1.3, -2.3), FRIA, 2.2, 50), omni((0.0, 2.3, -1.0), (0.7, 0.8, 1.0), 1.6, 10)],
               "sonidos": [{"t": 0.0, "archivo": "Cinta/ronquido.wav", "vol": -10}], "musica": 0.2}},

    {"k": 5, "nombre": "REC", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY},
     "camara": {"desde": [(0.78, 1.40, -1.18), (0.66, 1.37, -1.36)], "mira": [(0.36, 1.32, -1.72), (0.38, 1.32, -1.72)],
                "lente": 50, "foco": (0.40, 1.32, -1.68)},
     "godot": {"dur": 3.0, "epoca": "cinta", "entrada": "corte",
               "luces": [spot((0.7, 1.9, -1.2), (0.4, 1.32, -1.7), FRIA, 2, 30), RELLENO_CINTA], "musica": 0.22}},

    {"k": 6, "nombre": "Porky dormido", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY},
     "camara": {"desde": [(-2.15, 1.55, -1.95), (-2.05, 1.55, -2.10)], "mira": [(-0.45, 1.45, -2.55), (-0.45, 1.45, -2.58)],
                "lente": 35, "foco": (-0.4, 1.5, -2.55)},
     "godot": {"dur": 3.0, "epoca": "cinta", "entrada": "corte",
               "luces": [spot(LAMPARA, (-0.45, 1.6, -2.6), FRIA, 2.2, 38), spot((-1.7, 2.3, -3.7), (-0.45, 1.6, -2.6), FRIA, 3, 30),
                         RELLENO_CINTA],
               "sonidos": [{"t": -0.3, "archivo": "Cinta/ronquido.wav", "vol": -6}], "musica": 0.28}},

    {"k": 7, "nombre": "La rejilla se abre", "porky": DORMIDO, "objetos": objetos_de(3),
     "figura": de(3, "figura"), "polvo": {"centro": (-2.15, 3.42, 1.0), "radio": 0.5, "cuantos": 320},
     "camara": {"desde": [(-1.25, 0.70, 2.15), (-1.35, 0.70, 2.05)], "mira": [(-2.2, 3.15, 1.0), (-2.2, 3.22, 1.0)],
                "lente": 22, "foco": (-2.2, 3.35, 0.95)},
     "godot": {"dur": 3.6, "epoca": "cinta", "entrada": "corte", "hora": "SALA 1 · 03:12",
               "luces": [spot(DUCTO, (-2.2, 2.0, 1.0), FRIA, 10, 34), spot((-1.0, 1.4, 1.9), (-2.1, 3.4, 0.9), FRIA, 3, 25),
                         RELLENO_CINTA],
               "sonidos": [{"t": -0.4, "archivo": "Cinta/rejilla_metal.wav", "vol": -3}],
               "subtitulo": {"linea": 1, "desde": 0.1, "hasta": 3.0}, "musica": 0.4}},

    # La figura baja por la cuerda, recortada contra la luz del ducto; Porky duerme en primer plano
    {"k": 8, "nombre": "La figura baja por la cuerda", "porky": DORMIDO, "objetos": {"Silla": SILLA_PORKY, "Rejilla": TAPA_ADENTRO},
     "figura": figura((-2.20, 1.15, 1.02), 0,
                      {"MusloI": [("izq", 30)], "PiernaI": [("izq", -55)], "MusloD": [("izq", 8)], "PiernaD": [("izq", -20)],
                       "PieI": [("izq", 25)], "PieD": [("izq", 30)], "Cabeza": [("izq", 24), ("arriba", 10)]},
                      ik={"ManoI": ((-2.13, 3.02, 1.11), ("local", 0.6, 2.0, 0.3)), "ManoD": ((-1.99, 2.80, 1.11), ("local", -0.6, 2.0, 0.3))}),
     "cuerda": ((-2.06, 3.47, 1.20), (-2.06, 0.52, 1.20)),
     "polvo": {"centro": (-2.2, 3.0, 1.0), "radio": 0.7, "cuantos": 120},
     "camara": {"desde": [(1.15, 1.32, -3.65), (1.05, 1.32, -3.52)], "mira": [(-1.9, 2.05, 0.8), (-1.95, 2.10, 0.85)],
                "lente": 28, "foco": (-2.1, 2.2, 1.0)},
     "godot": {"dur": 3.6, "epoca": "cinta", "entrada": "corte",
               "luces": [spot(DUCTO, (-2.2, 1.6, 1.0), FRIA, 14, 30), spot((-2.2, 2.6, 2.6), (-2.2, 2.2, 1.0), FRIA, 8, 25),
                         spot(LAMPARA, (-0.45, 1.6, -2.6), FRIA, 1.5, 35), RELLENO_CINTA],
               "sonidos": [{"t": 0.0, "archivo": "Cinta/ronquido.wav", "vol": -14}], "musica": 0.5}},

    # Sobre el hombro de Porky: la figura agachada junto al micrófono
    {"k": 9, "nombre": "El susurro", "porky": DORMIDO, "objetos": objetos_de(5), "figura": de(5, "figura"),
     "camara": {"desde": [(-1.60, 1.80, -2.20), (-1.50, 1.78, -2.10)], "mira": [(0.45, 1.42, -1.50), (0.45, 1.42, -1.50)],
                "lente": 35, "foco": (0.45, 1.45, -1.5)},
     "godot": {"dur": 4.4, "epoca": "cinta", "entrada": "corte",
               "luces": [spot(LAMPARA, (0.45, 1.4, -1.5), FRIA, 2.2, 35), spot((1.1, 2.3, -0.4), (0.5, 1.5, -1.2), FRIA, 6, 30),
                         RELLENO_CINTA],
               "sonidos": [{"t": 0.6, "archivo": "Cinta/susurro.wav", "vol": -2.5}],
               "subtitulo": {"linea": 2, "desde": 0.6, "hasta": 3.6}, "musica": 0.55}},

    {"k": 10, "nombre": "El reloj en sus manos", "porky": DORMIDO, "objetos": objetos_de(6), "figura": de(6, "figura"),
     "camara": {"desde": [(-1.95, 1.90, -3.05), (-1.80, 1.90, -3.22)], "mira": [(0.0, 1.80, -4.45), (0.0, 1.80, -4.47)],
                "lente": 32, "foco": (0.0, 1.7, -4.5)},
     "godot": {"dur": 3.0, "epoca": "cinta", "entrada": "corte",
               "luces": [spot(LAMPARA, (0.0, 1.8, -4.4), FRIA, 2.5, 35), spot((-1.4, 2.6, -4.2), (0.0, 1.6, -4.5), FRIA, 6, 28),
                         RELLENO_CINTA],
               "sonidos": [{"t": 0.0, "archivo": "Ambiente/reloj_pared.wav", "vol": 0}], "musica": 0.7}},

    # La caída en cámara lenta: el reloj se mueve (lo anima Godot entre los vacíos); el golpe congela el cuadro
    {"k": 11, "nombre": "El reloj cae", "porky": DORMIDO,
     "objetos": {"Silla": SILLA_PORKY, "Rejilla": TAPA_ADENTRO, "Reloj": {"pos": RELOJ_PISO, "giro": [("x", -90), ("y", 24)]}},
     "figura": figura((0.0, 0.5, -4.42), 180,
                      {"PieI": [("izq", 30)], "PieD": [("izq", 34)], "Columna": [("izq", -2), ("arriba", 10)], "Pecho": [("arriba", 12)],
                       "Cuello": [("arriba", 20)], "Cabeza": [("arriba", 48), ("izq", 14)],
                       "BrazoI": [("frente", -30), ("izq", 50)], "BrazoD": [("frente", 30), ("izq", 50)]}),
     "vacios": {"Reloj_Caida_Ini": {"de": "Reloj", "pos": (0.0, 1.66, -4.62), "giro": [("x", 8)]},
                "Reloj_Caida_Fin": {"de": "Reloj", "pos": RELOJ_PISO, "giro": [("x", -90), ("y", 24)]}},
     "astillas": {"centro": RELOJ_PISO, "radio": 0.32, "cuantos": 80},
     "camara": {"desde": [(1.55, 1.40, -4.45), (1.55, 1.05, -4.40)], "mira": [(0.0, 1.66, -4.62), RELOJ_PISO],
                "lente": 40, "foco": (0.1, 1.2, -4.45)},
     "godot": {"dur": 4.4, "epoca": "cinta", "entrada": "corte", "mueve": "caida", "golpe": 3.0,
               "luces": [spot((0.4, 2.7, -3.7), (0.1, 1.0, -4.4), FRIA, 2.5, 38), spot((-1.4, 2.6, -4.2), (0.0, 1.4, -4.5), FRIA, 5, 30),
                         RELLENO_CINTA],
               "sonidos": [{"t": 3.0, "archivo": "Cinta/reloj_golpe.wav", "vol": 5}, {"t": 3.13, "archivo": "Cinta/reloj_rebote.wav", "vol": -10}],
               "musica": 1.0}},

    # Inserto de la carátula, legible, y paneo hacia arriba hasta la pluma en la rejilla
    {"k": 12, "nombre": "La carátula y la pluma", "porky": DORMIDO,
     "objetos": {"Silla": SILLA_PORKY, "Reloj": {"pos": RELOJ_PISO, "giro": [("x", -90), ("y", 24)]},
                 "Rejilla": {"pos": (-2.165, 3.484, 1.0), "giro": [("y", 6), ("x", 3)]}},
     "camara": {"desde": [(2.60, 1.10, -4.80), (2.58, 1.06, -4.78)], "mira": [RELOJ_PISO, REJILLA_PLUMA],
                "lente": [42, 110], "foco": RELOJ_PISO},
     "godot": {"dur": 4.4, "epoca": "cinta", "entrada": "corte",
               "luces": [spot((0.32, 2.2, -4.15), RELOJ_PISO, (0.85, 0.9, 1.0), 2.5, 16), spot((-1.85, 2.3, 0.45), REJILLA_PLUMA, FRIA, 7, 16),
                         RELLENO_CINTA], "musica": 0.0, "silencio": True}},

    {"k": 13, "nombre": "Porky despierta", "porky": de(9, "porky"), "objetos": objetos_de(9),
     "camara": {"desde": [(1.15, 1.62, -1.30), (1.08, 1.62, -1.40)], "mira": [(-0.45, 1.72, -2.75), (-0.45, 1.72, -2.75)],
                "lente": 28, "foco": (-0.4, 1.65, -2.8)},
     "godot": {"dur": 3.0, "epoca": "cinta", "entrada": "corte", "hora": "SALA 1 · 03:15",
               "luces": [spot((0.9, 2.4, -3.6), (-0.45, 1.7, -2.8), FRIA, 2.2, 32), spot(LAMPARA, (-0.45, 1.6, -2.6), FRIA, 1.2, 40),
                         RELLENO_CINTA],
               "sonidos": [{"t": -0.3, "archivo": "Cinta/sobresalto.wav", "vol": -2}], "musica": 0.35}},

    {"k": 14, "nombre": "Porky cuelga el reloj", "porky": de(10, "porky"), "objetos": objetos_de(10),
     "camara": {"desde": [(2.60, 1.75, -1.90), (2.45, 1.75, -2.10)], "mira": [(0.0, 1.62, -4.55), (0.0, 1.62, -4.57)],
                "lente": 28, "foco": (0.0, 1.6, -4.6)},
     "godot": {"dur": 3.0, "epoca": "cinta", "entrada": "corte",
               "luces": [spot((0.0, 2.8, -3.4), (0.0, 1.8, -4.7), FRIA, 3, 38), spot((-1.6, 2.2, -3.9), (0.0, 1.6, -4.6), FRIA, 4, 30),
                         RELLENO_CINTA],
               "sonidos": [{"t": 0.8, "archivo": "Cinta/reloj_rebote.wav", "vol": -12}], "musica": 0.35}},

    {"k": 15, "nombre": "Porky saca la cinta", "porky": de(11, "porky"), "objetos": objetos_de(11),
     "camara": {"desde": [(1.70, 1.60, -2.75), (1.60, 1.61, -2.62)], "mira": [(0.10, 1.62, -1.45), (0.10, 1.62, -1.45)],
                "lente": 26, "foco": (0.1, 1.6, -1.45)},
     "godot": {"dur": 4.4, "epoca": "cinta", "entrada": "corte",
               "luces": [omni((0.42, 1.42, -1.62), (1.0, 0.08, 0.05), 2.6, 1.4), RELLENO_CINTA],
               "sonidos": [{"t": 0.4, "archivo": "Cinta/cinta_stop.wav", "vol": -3}, {"t": 0.9, "archivo": "Cinta/cinta_rebobinar.wav", "vol": -3},
                           {"t": 3.5, "archivo": "Cinta/cinta_expulsar.wav", "vol": -2}],
               "mueve": "rebobina", "musica": 0.4}},

    {"k": 16, "nombre": "Porky se va", "porky": de(12, "porky"), "objetos": objetos_de(12),
     "camara": {"desde": [(2.28, 0.60, 0.55), (2.31, 0.60, 0.80)], "mira": [(2.45, 1.05, 2.60), (2.45, 1.08, 2.70)],
                "lente": 24, "foco": (2.45, 0.8, 2.4)},
     "godot": {"dur": 3.6, "epoca": "cinta", "entrada": "corte", "hora": "SALA 1 · 03:16",
               "luces": [omni((2.5, 2.0, 4.3), (0.95, 0.88, 0.75), 6, 6), spot((2.2, 1.3, 1.0), (2.45, 0.55, 2.4), FRIA, 2.0, 32),
                         RELLENO_CINTA],
               "sonidos": [{"t": 0.2, "archivo": "Cinta/puerta_abrir.wav", "vol": -4}, {"t": 0.8, "archivo": "Efectos/paso_1.wav", "vol": -13},
                           {"t": 1.3, "archivo": "Efectos/paso_3.wav", "vol": -13}, {"t": 1.8, "archivo": "Efectos/paso_2.wav", "vol": -15}],
               "musica": 0.3}},

    # ================= C. REGRESO AL PRESENTE =================
    {"k": 17, "nombre": "Caneloso, la mano en el botón", "caneloso": PLAY,
     "camara": {"desde": [(0.15, 1.85, -3.05), (0.20, 1.85, -2.90)], "mira": [(0.60, 1.55, -1.35), (0.60, 1.56, -1.35)],
                "lente": 32, "foco": (0.62, 1.45, -1.3)},
     "godot": {"dur": 3.4, "epoca": "presente", "entrada": "seco", "hora": "SALA 1 · 07:01",
               "luces": [spot(LAMPARA, (0.6, 1.4, -1.35), CALIDA, 2.0, 40), spot((-0.4, 2.2, -0.3), (0.6, 1.5, -1.3), (1.0, 0.9, 0.8), 2.0, 30),
                         RELLENO_PRESENTE],
               "sonidos": [{"t": 0.3, "archivo": "Cinta/susurro.wav", "vol": -6}],
               "subtitulo": {"linea": 2, "desde": 0.3, "hasta": 3.1}, "musica": 0.15}},

    {"k": 18, "nombre": "Rebobina y la vuelve a oír", "caneloso": REW,
     "camara": {"desde": [(0.22, 1.58, -2.15), (0.25, 1.55, -2.05)], "mira": [(0.40, 1.34, -1.64), (0.40, 1.34, -1.64)],
                "lente": 40, "foco": (0.38, 1.34, -1.64)},
     "godot": {"dur": 3.8, "epoca": "presente", "entrada": "corte", "mueve": "rebobina",
               "luces": [spot((0.45, 1.85, -1.4), (0.38, 1.33, -1.66), CALIDA, 0.8, 30), RELLENO_PRESENTE],
               "sonidos": [{"t": 0.2, "archivo": "Cinta/cinta_rebobinar.wav", "vol": -6, "limpio": True},
                           {"t": 1.9, "archivo": "Cinta/cinta_play.wav", "vol": -6, "limpio": True},
                           {"t": 2.3, "archivo": "Cinta/susurro.wav", "vol": 0, "limpio": True}],
               "subtitulo": {"linea": 2, "desde": 2.3, "hasta": 3.8}, "musica": 0.3}},

    {"k": 19, "nombre": "Caneloso mira la rejilla", "caneloso": MIRA,
     "objetos": {"Rejilla": {"pos": (-2.165, 3.484, 1.0), "giro": [("y", 6), ("x", 3)]}},
     "camara": {"desde": [(-1.10, 2.30, 0.25), (-1.00, 2.25, 0.15)], "mira": [(0.60, 1.55, -1.30), (0.60, 1.56, -1.30)],
                "lente": 28, "foco": (0.62, 1.55, -1.3)},
     "godot": {"dur": 3.2, "epoca": "presente", "entrada": "corte",
               "luces": [spot(LAMPARA, (0.6, 1.4, -1.35), CALIDA, 2.0, 40), spot((-1.85, 2.3, 0.45), REJILLA_PLUMA, (0.8, 0.85, 1.0), 6, 16),
                         RELLENO_PRESENTE], "musica": 0.6, "titulo": "¿Quién es La Garra?"}},
]

# El ducto queda abierto (con su luz fría) mientras la figura está en la sala; la pluma se ve desde que la tapa
# quedó mal puesta y, en el presente, cuando Caneloso mira la rejilla
for _c in CUADROS:
    _c["godot"]["ducto"] = 7 <= _c["k"] <= 11
    _c["godot"]["pluma"] = 12 <= _c["k"] <= 16 or _c["k"] == 19
    _c["godot"]["huellas"] = _c["k"] >= 16
