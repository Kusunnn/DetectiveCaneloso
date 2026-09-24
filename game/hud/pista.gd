class_name Pista
extends Resource
## Identificador único: recoger dos veces el mismo objeto no lo duplica.
@export var id: StringName
@export var titulo: String = "Pista sin identificar"
@export_multiline var descripcion: String
@export var imagen: Texture2D
