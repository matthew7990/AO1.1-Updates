extends Node



## Equivalente a LoadInterface() + InterfaceLoader del proyecto AO.

const ResourcesRootScript := preload("res://scripts/resources/resources_root.gd")



const LANG_PREFIX := "es_"

const INTERFACE_DIR := "res://assets/interface/"



func _resources_interface_dir() -> String:

	var root := ResourcesRootScript.resolve()

	if root.is_empty():

		return ""

	return root.path_join("interface")



func load_interface(filename: String, use_locale: bool = true) -> Texture2D:

	var names: Array[String] = []

	if use_locale:

		names.append(LANG_PREFIX + filename)

	names.append(filename)



	var ext_iface := _resources_interface_dir()

	if not ext_iface.is_empty():

		for name in names:

			var path := ext_iface.path_join(name)

			if FileAccess.file_exists(path):

				var img := Image.load_from_file(path)

				if img:

					return ImageTexture.create_from_image(img)



	for name in names:

		var res_path := INTERFACE_DIR + name

		if ResourceLoader.exists(res_path):

			var tex: Texture2D = load(res_path) as Texture2D

			if tex:

				return tex

		var fs_path := ProjectSettings.globalize_path(res_path)

		if FileAccess.file_exists(fs_path):

			var img := Image.load_from_file(fs_path)

			if img:

				return ImageTexture.create_from_image(img)



	push_warning("Interface no encontrada: %s" % filename)

	return null



func twip_to_px(twip: float) -> int:

	return int(round(twip / 15.0))

