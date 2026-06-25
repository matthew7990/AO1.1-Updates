class_name Vb6FormShell
extends Control

const GraphicalButton := preload("res://scripts/ui/ao_graphical_button.gd")

const Vb6FormCoords := preload("res://scripts/ui/vb6_form_coords.gd")

var design_size := Vector2.ZERO
var _background: TextureRect

func _init() -> void:
	clip_contents = true
	_background = TextureRect.new()
	_background.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_background.stretch_mode = TextureRect.STRETCH_SCALE
	_background.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	add_child(_background)

func set_background(tex: Texture2D) -> void:
	_background.texture = tex
	if tex:
		design_size = tex.get_size()
		custom_minimum_size = design_size
		size = design_size
		_background.size = design_size

func add_field(left: int, top: int, width: int, height: int, secret := false, max_length := 0) -> LineEdit:
	var field := LineEdit.new()
	field.position = Vb6FormCoords.position(left, top)
	field.size = Vb6FormCoords.size(width, height)
	field.secret = secret
	if max_length > 0:
		field.max_length = max_length
	_style_field(field)
	add_child(field)
	return field

func add_button(base_name: String, left: int, top: int, _width: int, _height: int, cb: Callable) -> GraphicalButton:
	var button: GraphicalButton = GraphicalButton.new()
	button.setup_bmp(
		"%s-default.bmp" % base_name,
		"%s-over.bmp" % base_name,
		"%s-off.bmp" % base_name
	)
	button.position = Vb6FormCoords.position(left, top)
	if button.texture_normal:
		button.size = button.texture_normal.get_size()
	button.pressed.connect(cb)
	add_child(button)
	return button

func add_checkbox(left: int, top: int, width: int, height: int, on_toggle: Callable) -> TextureButton:
	var btn := TextureButton.new()
	btn.position = Vb6FormCoords.position(left, top)
	btn.size = Vb6FormCoords.size(width, height)
	btn.toggle_mode = true
	btn.texture_pressed = AOAssets.load_interface("check-amarillo.bmp")
	btn.ignore_texture_size = true
	btn.stretch_mode = TextureButton.STRETCH_KEEP
	btn.toggled.connect(on_toggle)
	add_child(btn)
	return btn

func add_caption(left: int, top: int, width: int, height: int, text: String, color := Color(0.75, 0.75, 0.75)) -> Label:
	var lbl := Label.new()
	lbl.position = Vb6FormCoords.position(left, top)
	lbl.size = Vb6FormCoords.size(width, height)
	lbl.text = text
	lbl.modulate = color
	lbl.add_theme_font_size_override("font_size", 11)
	add_child(lbl)
	return lbl

static func _style_field(field: LineEdit) -> void:
	var empty := StyleBoxEmpty.new()
	field.add_theme_stylebox_override("normal", empty)
	field.add_theme_stylebox_override("focus", empty)
	field.add_theme_stylebox_override("read_only", empty)
	field.add_theme_color_override("font_color", Color(0.75, 0.75, 0.75))
	field.add_theme_color_override("caret_color", Color(0.75, 0.75, 0.75))
	field.add_theme_font_size_override("font_size", 11)
