class_name InformeCaso
extends Resource
## Datos del expediente de un caso: el informe que lee el jugador al pulsar TAB.
## Cada caso tiene su propio archivo: res://hud/expediente/casos/caso_XX.tres
## (caso_00.tres para el tutorial, caso_01.tres para el primer caso, etc.).
## Las secciones que se dejen vacías no se muestran.

@export_group("Encabezado")
@export var titulo := ""
@export var lugar := ""
@export var fecha := ""
@export var etiqueta_victima := "VÍCTIMA"
@export var victima := ""

@export_group("Foto y ficha")
@export var foto: Texture2D
@export var pie_foto := ""
@export var sello := "CONFIDENCIAL"
@export var a_cargo := "Det. Caneloso"
@export var estado := "ABIERTO"
@export var examen: PackedStringArray = []

@export_group("Hechos")
@export_multiline var hallazgo := ""
@export_multiline var circunstancias := ""
## Cada entrada: "hora|texto". Añade "|!" al final para resaltarla en rojo.
@export var linea_tiempo: PackedStringArray = []
@export var notas: PackedStringArray = []
@export_multiline var antecedente := ""

@export_group("Personas")
@export var titulo_personas := "PERSONAS PRESENTES ESA NOCHE"
## Cada entrada: "nombre|edad|papel actual|pasado". La edad y el pasado son opcionales.
@export var personas: PackedStringArray = []
## Encabezado de la última línea de cada ficha. Vacío = no se muestra.
@export var etiqueta_pasado := "HACE 20 AÑOS"

@export_group("Objetivo")
@export_multiline var objetivo := ""
