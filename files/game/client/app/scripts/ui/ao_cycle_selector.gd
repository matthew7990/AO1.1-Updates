class_name AOCycleSelector
extends HBoxContainer

signal changed(index: int)

var _items: PackedStringArray = PackedStringArray()
var _index := 0
var _lbl: Label

func _ready() -> void:
	custom_minimum_size = Vector2(200, 24)
	var btn_l := _make_arrow("<")
	btn_l.pressed.connect(_prev)
	add_child(btn_l)
	_lbl = Label.new()
	_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_lbl.add_theme_color_override("font_color", Color(0.78, 0.78, 0.72))
	add_child(_lbl)
	var btn_r := _make_arrow(">")
	btn_r.pressed.connect(_next)
	add_child(btn_r)

func setup(items: PackedStringArray, start_index: int = 1) -> void:
	_items = items
	_index = clampi(start_index, 1, max(1, items.size() - 1))
	_refresh()

func get_selected_index() -> int:
	return _index

func get_selected_label() -> String:
	if _index >= 0 and _index < _items.size():
		return _items[_index]
	return ""

func _make_arrow(text: String) -> Button:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(28, 24)
	b.focus_mode = Control.FOCUS_NONE
	return b

func _prev() -> void:
	if _items.is_empty():
		return
	_index -= 1
	if _index < 1:
		_index = _items.size() - 1
	_refresh()
	changed.emit(_index)

func _next() -> void:
	if _items.is_empty():
		return
	_index += 1
	if _index >= _items.size():
		_index = 1
	_refresh()
	changed.emit(_index)

func _refresh() -> void:
	_lbl.text = get_selected_label()
