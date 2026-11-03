# HU-9.5 CA-01: sonidos de las cinemáticas del Caso 1, generados por completo aquí (sin grabaciones de terceros).
# Uso: python generar_sonidos.py  (escribe los .wav junto a este archivo)
import math
import os
import random
import struct
import wave

TASA = 44100
CARPETA = os.path.dirname(os.path.abspath(__file__))
random.seed(1014)


def guardar(nombre, muestras):
    pico = max(0.0001, max(abs(m) for m in muestras))
    with wave.open(os.path.join(CARPETA, nombre), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(TASA)
        w.writeframes(b"".join(struct.pack("<h", int(m / pico * 0.9 * 32767)) for m in muestras))


def silbido():
    # Silbato de monitor: corto, con la bolita que hace vibrar el tono
    n = int(TASA * 0.42)
    fase = 0.0
    salida = []
    for i in range(n):
        t = i / TASA
        f = 2650 + 90 * math.sin(math.tau * 34 * t) + 40 * t
        fase += math.tau * f / TASA
        envolvente = min(1, t / 0.02) * min(1, (0.42 - t) / 0.09)
        salida.append(envolvente * (math.sin(fase) + 0.25 * math.sin(2 * fase) + 0.06 * random.uniform(-1, 1)))
    return salida + [0.0] * int(TASA * 0.1)


def filtrado(n, corte, envolvente, ganancia=1.0):
    # Ruido con un paso bajo de un polo
    a = 1 - math.exp(-math.tau * corte / TASA)
    y = 0.0
    salida = []
    for i in range(n):
        y += a * (random.uniform(-1, 1) - y)
        salida.append(y * envolvente(i / TASA) * ganancia)
    return salida


def chapoteo():
    golpe = filtrado(int(TASA * 1.6), 1400, lambda t: min(1, t / 0.008) * math.exp(-t * 4.5), 2.2)
    grave = [0.9 * math.sin(math.tau * (70 + 40 * math.exp(-t / TASA * 8)) * t / TASA) * math.exp(-t / TASA * 6) for t in range(int(TASA * 1.6))]
    salida = [g + h for g, h in zip(golpe, grave)]
    # Gotas al caer
    for _ in range(38):
        inicio = random.uniform(0.25, 1.35)
        f = random.uniform(900, 2600)
        for i in range(int(TASA * 0.05)):
            k = int(inicio * TASA) + i
            if k < len(salida):
                salida[k] += 0.1 * math.sin(math.tau * f * i / TASA) * math.exp(-i / TASA * 90)
    return salida


def viento():
    n = int(TASA * 8)
    base = filtrado(n, 500, lambda t: 0.6 + 0.4 * math.sin(math.tau * t / 8) ** 2)
    # El final empalma con el principio
    cruce = int(TASA * 0.4)
    for i in range(cruce):
        k = i / cruce
        base[i] = base[i] * k + base[n - cruce + i] * (1 - k)
    return base[:n - cruce]


def bombas():
    n = int(TASA * 6)
    salida = []
    for i in range(n):
        t = i / TASA
        salida.append(0.5 * math.sin(math.tau * 48 * t) + 0.3 * math.sin(math.tau * 96 * t) + 0.15 * math.sin(math.tau * 144 * t + 1))
    ruido = filtrado(n, 260, lambda t: 1.0, 1.2)
    return [(a + b) * (0.85 + 0.15 * math.sin(math.tau * 6 * i / TASA)) for i, (a, b) in enumerate(zip(salida, ruido))]


guardar("silbido_corto.wav", silbido())
guardar("chapoteo.wav", chapoteo())
guardar("viento_lago.wav", viento())
guardar("bombas.wav", bombas())
