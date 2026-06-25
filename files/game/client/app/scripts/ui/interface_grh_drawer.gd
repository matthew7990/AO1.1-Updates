class_name InterfaceGrhDrawer
extends RefCounted

const DESIGN_SIZE := Vector2(1024, 768)

static func draw_fullscreen(canvas: CanvasItem, grhs, textures, grh_index: int) -> void:
	var def := grhs.resolve_drawable(grh_index)
	if def.is_empty():
		return
	var tex := textures.get_texture(def.get("file_num", 0))
	if tex == null:
		return
	var src := Rect2(def.get("sx", 0), def.get("sy", 0), def.get("width", 0), def.get("height", 0))
	canvas.draw_texture_rect_region(tex, Rect2(Vector2.ZERO, DESIGN_SIZE), src)

static func draw_at(canvas: CanvasItem, grhs, textures, grh_index: int, pos: Vector2) -> void:
	var def := grhs.resolve_drawable(grh_index)
	if def.is_empty():
		return
	var tex := textures.get_texture(def.get("file_num", 0))
	if tex == null:
		return
	var size := Vector2(def.get("width", 0), def.get("height", 0))
	var src := Rect2(def.get("sx", 0), def.get("sy", 0), size)
	canvas.draw_texture_rect_region(tex, Rect2(pos, size), src)
