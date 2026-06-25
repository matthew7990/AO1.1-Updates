class_name TextureCache
extends RefCounted

var _root: String = ""
var _cache: Dictionary = {}

func setup(root: String) -> void:
	_root = root

func get_texture(file_num: int) -> Texture2D:
	if file_num <= 0:
		return null
	if _cache.has(file_num):
		return _cache[file_num]
	for ext in [".png", ".bmp", ".jpg", ".gif"]:
		var path := _root.path_join("Graficos").path_join("%d%s" % [file_num, ext])
		if not FileAccess.file_exists(path):
			continue
		var img := Image.load_from_file(path)
		if img == null:
			continue
		var tex := ImageTexture.create_from_image(img)
		_cache[file_num] = tex
		return tex
	return null
