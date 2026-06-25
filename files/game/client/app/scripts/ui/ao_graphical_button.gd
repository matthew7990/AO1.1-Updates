extends TextureButton

@export var default_bmp: String
@export var over_bmp: String
@export var off_bmp: String

func _ready() -> void:
	if default_bmp.is_empty():
		return
	_apply_textures()
	focus_mode = Control.FOCUS_NONE
	mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	ignore_texture_size = false
	stretch_mode = TextureButton.STRETCH_KEEP
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST

func setup_bmp(def: String, over: String, off: String) -> void:
	default_bmp = def
	over_bmp = over
	off_bmp = off
	if is_inside_tree():
		_apply_textures()

func _apply_textures() -> void:
	texture_normal = AOAssets.load_interface(default_bmp)
	texture_hover = AOAssets.load_interface(over_bmp)
	texture_pressed = AOAssets.load_interface(over_bmp)
	texture_disabled = AOAssets.load_interface(off_bmp)
	if texture_normal:
		custom_minimum_size = texture_normal.get_size()
		size = custom_minimum_size

func place_at_design(x: float, y: float) -> void:
	position = Vector2(x, y)
	if texture_normal:
		size = texture_normal.get_size()
