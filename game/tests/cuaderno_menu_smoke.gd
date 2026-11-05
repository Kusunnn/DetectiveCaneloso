extends SceneTree
var fallos := 0
func comprobar(ok: bool, mensaje: String) -> void:
    print(("✓ " if ok else "✗ ") + mensaje)
    if not ok: fallos += 1
func _initialize() -> void:
    call_deferred("probar")
func probar() -> void:
    var caso = load("res://tests/CuadernoMenuPrueba.cs").new()
    root.add_child(caso)
    current_scene = caso
    var menu = load("res://Escenas/Menus/menu_pausa.tscn").instantiate()
    caso.add_child(menu)
    await process_frame
    menu.get_node("%Raiz").show()
    paused = true
    var pistas: Button = menu.find_child("BotonPistas", true, false)
    comprobar(pistas != null, "Pausa conserva sus opciones y añade Pistas")
    if pistas == null:
        paused = false
        quit(1)
        return
    pistas.emit_signal("pressed")
    await process_frame
    var panel = menu.find_child("PanelPistas", true, false)
    var historial: RichTextLabel = panel.find_child("Historial", true, false)
    var pedir: Button = panel.find_child("PedirPista", true, false)
    comprobar(panel.visible and paused, "El cuaderno se abre manteniendo la pausa")
    comprobar("vacío" in historial.get_parsed_text(), "Estado inicial sin revelar ayudas")
    pedir.emit_signal("pressed")
    await process_frame
    comprobar("fogata" in historial.get_parsed_text() and caso.EstadoCuaderno().usos == 1, "Pedir añade texto inmediato durante la pausa")
    pedir.emit_signal("pressed")
    comprobar(caso.EstadoCuaderno().usos == 1, "Repetir no duplica ni aumenta usos")
    panel.find_child("BotonVolver", true, false).emit_signal("pressed")
    comprobar(not panel.visible and paused, "Volver mantiene la pausa")
    pistas.emit_signal("pressed")
    comprobar("fogata" in historial.get_parsed_text(), "Reabrir conserva el texto")
    var esc := InputEventAction.new()
    esc.action = "ui_cancel"
    esc.pressed = true
    panel._UnhandledInput(esc)
    comprobar(not panel.visible and paused, "Esc vuelve al menú sin reanudar")
    comprobar(not InputMap.has_action("ayuda"), "La ayuda ya no depende de H")
    if "--captura" in OS.get_cmdline_user_args():
        pistas.emit_signal("pressed")
        await process_frame
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("/tmp/caneloso-cuaderno.png")
    paused = false
    print("Cuaderno menú: %d fallos" % fallos)
    quit(0 if fallos == 0 else 1)
