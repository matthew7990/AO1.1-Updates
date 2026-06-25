class_name ResourcesRoot
extends RefCounted

## Resuelve reference/resources (baseline pinneado del proyecto AO).

static func resolve() -> String:
	var env := OS.get_environment("AO_RESOURCES").strip_edges()
	if not env.is_empty() and DirAccess.dir_exists_absolute(env):
		var norm := env.replace("\\", "/")
		if norm.ends_with("/interface"):
			return norm.substr(0, norm.rfind("/"))
		return env

	var project_dir := ProjectSettings.globalize_path("res://")
	var candidates := [
		project_dir.path_join("../reference/resources"),
	]
	for path in candidates:
		var full := ProjectSettings.globalize_path(path)
		if DirAccess.dir_exists_absolute(full.path_join("init")):
			return full
	return ""
