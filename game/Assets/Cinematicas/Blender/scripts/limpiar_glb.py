# Godot 4.7 marca KHR_node_visibility como requerida al exportar a glTF y el importador de Blender 5.2 la rechaza.
# Quita esa marca del .glb (los nodos ocultos entran visibles; construir_2320.py los borra). Uso: python limpiar_glb.py <a.glb> [...]
import json, struct, sys
for ruta in sys.argv[1:]:
    with open(ruta, "rb") as f: datos = f.read()
    largo = struct.unpack("<I", datos[12:16])[0]
    doc = json.loads(datos[20:20 + largo])
    for clave in ("extensionsUsed", "extensionsRequired"):
        if clave in doc:
            doc[clave] = [e for e in doc[clave] if e != "KHR_node_visibility"]
            if not doc[clave]: del doc[clave]
    for n in doc.get("nodes", []):
        n.get("extensions", {}).pop("KHR_node_visibility", None)
        if "extensions" in n and not n["extensions"]: del n["extensions"]
    j = json.dumps(doc, ensure_ascii=False, separators=(",", ":")).encode("utf8")
    j += b" " * (-len(j) % 4)
    resto = datos[20 + largo:]
    with open(ruta, "wb") as f:
        f.write(datos[:8] + struct.pack("<I", 20 + len(j) + len(resto)) + struct.pack("<I", len(j)) + b"JSON" + j + resto)
    print("limpio:", ruta)
