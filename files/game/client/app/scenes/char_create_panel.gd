extends Control

## Overlay crear PJ — replica e_state_createchar_screen + RenderUICrearPJ.

signal cancelled
signal submit_requested

const GraphicalButton := preload("res://scripts/ui/ao_graphical_button.gd")
const CycleSelector := preload("res://scripts/ui/ao_cycle_selector.gd")
const CharacterOptions := preload("res://scripts/data/character_options.gd")

var _name_f: LineEdit
var _class_sel: CycleSelector
var _race_sel: CycleSelector
var _gender_sel: CycleSelector
var _home_sel: CycleSelector
var _head_ids: Array = []
var _head_idx := 0
var _head_lbl: Label
var _status: Label

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP
	_build()

func _build() -> void:
	var dim := ColorRect.new()
	dim.color = Color(0, 0, 0, 0.55)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(dim)

	var panel := PanelContainer.new()
	panel.position = Vector2(520, 120)
	panel.custom_minimum_size = Vector2(460, 520)
	add_child(panel)

	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 8)
	panel.add_child(v)

	var title := Label.new()
	title.text = "Crear personaje"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Color(0.97, 0.42, 0.01))
	v.add_child(title)

	_name_f = LineEdit.new()
	_name_f.placeholder_text = "Nombre del personaje"
	_name_f.custom_minimum_size = Vector2(0, 28)
	v.add_child(_name_f)

	_class_sel = _add_cycle(v, "Clase", CharacterOptions.CLASSES)
	_race_sel = _add_cycle(v, "Raza", CharacterOptions.RACES)
	_gender_sel = _add_cycle(v, "Genero", CharacterOptions.GENDERS)
	_home_sel = _add_cycle(v, "Ciudad natal", CharacterOptions.HOMES)

	var head_row := HBoxContainer.new()
	v.add_child(head_row)
	var hl := Label.new()
	hl.text = "Cabeza"
	hl.custom_minimum_size = Vector2(90, 0)
	head_row.add_child(hl)
	var hb_l := Button.new()
	hb_l.text = "<"
	hb_l.pressed.connect(_head_prev)
	head_row.add_child(hb_l)
	_head_lbl = Label.new()
	_head_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_head_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	head_row.add_child(_head_lbl)
	var hb_r := Button.new()
	hb_r.text = ">"
	hb_r.pressed.connect(_head_next)
	head_row.add_child(hb_r)

	_race_sel.changed.connect(_on_race_gender_changed)
	_gender_sel.changed.connect(_on_race_gender_changed)
	_on_race_gender_changed(0)

	_status = Label.new()
	_status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_status.add_theme_color_override("font_color", Color(1, 0.45, 0.3))
	v.add_child(_status)

	var row := HBoxContainer.new()
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	v.add_child(row)

	var btn_back: GraphicalButton = _btn("boton-volver-flecha", Vector2.ZERO)
	btn_back.pressed.connect(func(): cancelled.emit())
	row.add_child(btn_back)

	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(16, 0)
	row.add_child(spacer)

	var btn_ok: GraphicalButton = _btn("boton-crear-personaje", Vector2.ZERO)
	btn_ok.pressed.connect(_on_submit)
	row.add_child(btn_ok)

func _add_cycle(parent: Control, title: String, items: PackedStringArray) -> CycleSelector:
	var row := HBoxContainer.new()
	parent.add_child(row)
	var lbl := Label.new()
	lbl.text = title
	lbl.custom_minimum_size = Vector2(90, 0)
	row.add_child(lbl)
	var sel: CycleSelector = CycleSelector.new()
	sel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	sel.setup(items, 1)
	row.add_child(sel)
	return sel

func _btn(prefix: String, pos: Vector2) -> GraphicalButton:
	var b: GraphicalButton = GraphicalButton.new()
	b.default_bmp = "%s-default.bmp" % prefix
	b.over_bmp = "%s-over.bmp" % prefix
	b.off_bmp = "%s-off.bmp" % prefix
	b.position = pos
	return b

func _on_race_gender_changed(_i: int) -> void:
	_head_ids = CharacterOptions.heads_for(_race_sel.get_selected_index(), _gender_sel.get_selected_index())
	_head_idx = 0
	_refresh_head()

func _head_prev() -> void:
	if _head_ids.is_empty():
		return
	_head_idx = (_head_idx - 1 + _head_ids.size()) % _head_ids.size()
	_refresh_head()

func _head_next() -> void:
	if _head_ids.is_empty():
		return
	_head_idx = (_head_idx + 1) % _head_ids.size()
	_refresh_head()

func _refresh_head() -> void:
	if _head_ids.is_empty():
		_head_lbl.text = "-"
	else:
		_head_lbl.text = str(_head_ids[_head_idx])

func _on_submit() -> void:
	var char_name := _name_f.text.strip_edges()
	if char_name.length() < 3:
		_status.text = "El nombre debe tener al menos 3 caracteres."
		return
	if _head_ids.is_empty():
		_status.text = "Selecciona raza y genero."
		return
	_status.text = "Creando personaje..."
	GameClient.create_character(
		char_name,
		_race_sel.get_selected_index(),
		_gender_sel.get_selected_index(),
		_class_sel.get_selected_index(),
		int(_head_ids[_head_idx]),
		_home_sel.get_selected_index(),
	)
	submit_requested.emit()

func show_panel() -> void:
	visible = true
	_name_f.text = ""
	_status.text = ""
	_on_race_gender_changed(0)

func hide_panel() -> void:
	visible = false

func set_status(msg: String) -> void:
	_status.text = msg
