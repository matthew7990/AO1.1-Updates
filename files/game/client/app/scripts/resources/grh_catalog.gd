class_name GrhCatalog
extends RefCounted

var _defs: Array = []

func load_from_root(root: String) -> bool:
	var ini_path := root.path_join("init").path_join("Graficos.ini")
	if not FileAccess.file_exists(ini_path):
		push_warning("GrhCatalog: no existe %s" % ini_path)
		return false
	_defs = []
	_defs.append(null)
	var max_grh := 0
	var in_graphics := false
	var file := FileAccess.open(ini_path, FileAccess.READ)
	while not file.eof_reached():
		var line := file.get_line().strip_edges()
		if line.is_empty():
			continue
		if max_grh == 0:
			if line.to_lower().begins_with("numgrh="):
				max_grh = int(line.substr(7))
				_defs.resize(max_grh + 1)
			continue
		if not in_graphics:
			if line.to_lower() == "[graphics]":
				in_graphics = true
			continue
		if not line.to_lower().begins_with("grh"):
			continue
		var eq := line.find("=")
		if eq <= 3:
			continue
		var grh := int(line.substr(3, eq - 3))
		if grh <= 0 or grh >= _defs.size():
			continue
		var fields := line.substr(eq + 1).split("-")
		if fields.size() < 2:
			continue
		var num_frames := int(fields[0])
		if num_frames == 1 and fields.size() >= 6:
			_defs[grh] = {
				"file_num": int(fields[1]),
				"sx": int(fields[2]),
				"sy": int(fields[3]),
				"width": int(fields[4]),
				"height": int(fields[5]),
				"frames": [grh],
			}
		elif num_frames > 1 and fields.size() >= num_frames + 2:
			var frames: Array = []
			for i in num_frames:
				frames.append(int(fields[i + 1]))
			_defs[grh] = {
				"file_num": 0,
				"sx": 0,
				"sy": 0,
				"width": 0,
				"height": 0,
				"frames": frames,
			}
	file.close()
	for i in range(1, _defs.size()):
		var def = _defs[i]
		if def == null or def.get("width", 0) > 0:
			continue
		var frames: Array = def.get("frames", [])
		if frames.is_empty():
			continue
		var first = _defs[int(frames[0])]
		if first != null:
			def["file_num"] = first.get("file_num", 0)
			def["sx"] = first.get("sx", 0)
			def["sy"] = first.get("sy", 0)
			def["width"] = first.get("width", 0)
			def["height"] = first.get("height", 0)
	return _defs.size() > 1

func resolve_drawable(grh_index: int, _tick: int = 0) -> Dictionary:
	if grh_index <= 0 or grh_index >= _defs.size():
		return {}
	var def = _defs[grh_index]
	if def == null:
		return {}
	if def.get("file_num", 0) > 0 and def.get("width", 0) > 0:
		return def
	var frames: Array = def.get("frames", [])
	if frames.is_empty():
		return {}
	return resolve_drawable(int(frames[0]), _tick)
