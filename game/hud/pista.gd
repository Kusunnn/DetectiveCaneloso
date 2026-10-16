class_name Pista
extends Resource
## Identificador único: recoger dos veces el mismo objeto no lo duplica.
@export var id: StringName
@export var titulo: String = "Pista sin identificar"
@export_multiline var descripcion: String
@export var imagen: Texture2D
## HU-07: vista de examen. Modelo 3D propio (vacío = se copian las mallas del objeto en la escena).
@export var modelo: PackedScene
## Texto largo de la caja de la vista de examen (vacío = se usa la descripción).
@export_multiline var descripcion_ampliada: String
## Dónde se encontró ("Sala 1", "Cabaña de Porky"...).
@export var lugar: String
## true cuando el jugador ya la abrió en la vista de examen.
@export var examinada: bool = false
## Solo en los detalles descubiertos: id de la evidencia de la que salieron.
@export var origen_id: StringName
## Detalles ocultos (recursos DetalleOculto) que se pueden descubrir al examinarla.
@export var detalles: Array[Resource] = []

func es_detalle() -> bool:
	return origen_id != &""
