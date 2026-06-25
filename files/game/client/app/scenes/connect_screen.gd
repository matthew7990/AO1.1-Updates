extends Control
## Redirige al cliente C# (compatibilidad con escenas GDScript viejas).
func _ready() -> void:
	get_tree().change_scene_to_file("res://scenes/main.tscn")
