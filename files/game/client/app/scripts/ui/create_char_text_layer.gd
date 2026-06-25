extends Control

const CharacterOptions := preload("res://scripts/data/character_options.gd")

const BASE_ATTR := 18

var class_text := ""
var race_text := ""
var gender_text := ""
var home_text := ""
var display_name := ""
var fuerza := BASE_ATTR
var agilidad := BASE_ATTR
var inteligencia := BASE_ATTR
var constitucion := BASE_ATTR
var carisma := BASE_ATTR

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func _draw() -> void:
	_draw_centered(605, 277, class_text, Color(0.78, 0.78, 0.78))
	_draw_centered(600, 323, race_text, Color(0.78, 0.78, 0.78))
	_draw_centered(365, 277, gender_text, Color(0.78, 0.78, 0.78))
	_draw_centered(365, 322, home_text, Color(0.78, 0.78, 0.78))
	_draw_attr(645, 375, fuerza)
	_draw_attr(645, 405, agilidad)
	_draw_attr(645, 435, inteligencia)
	_draw_attr(645, 465, constitucion)
	_draw_attr(645, 495, carisma)
	if not display_name.is_empty():
		_draw_centered(365, 478, display_name, Color(0, 0.5, 0.75))

func _draw_centered(x: float, y: float, text: String, color: Color) -> void:
	if text.is_empty():
		return
	var font := ThemeDB.fallback_font
	var sz := 11
	var ts := font.get_string_size(text, HORIZONTAL_ALIGNMENT_CENTER, -1, sz)
	draw_string(font, Vector2(x, y) - ts / 2.0 + Vector2(0, ts.y * 0.75), text, HORIZONTAL_ALIGNMENT_LEFT, -1, sz, color)

func _draw_attr(x: float, y: float, value: int) -> void:
	var color := Color.WHITE
	if value > BASE_ATTR:
		color = Color(0.2, 1, 0.2)
	elif value < BASE_ATTR:
		color = Color(1, 0.3, 0.3)
	_draw_centered(x, y, str(value), color)

func sync_from_indices(class_idx: int, race_idx: int, gender_idx: int, home_idx: int, name: String) -> void:
	class_text = CharacterOptions.get_class_label(class_idx)
	race_text = CharacterOptions.race_name(race_idx)
	gender_text = CharacterOptions.GENDERS[gender_idx] if gender_idx < CharacterOptions.GENDERS.size() else ""
	home_text = CharacterOptions.home_name(home_idx)
	display_name = name
	queue_redraw()
