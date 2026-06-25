extends Node

## Carga recursos oficiales desde reference/resources (baseline AO).

const ResourcesRootScript := preload("res://scripts/resources/resources_root.gd")
const GrhCatalogScript := preload("res://scripts/resources/grh_catalog.gd")
const TextureCacheScript := preload("res://scripts/resources/texture_cache.gd")

var root: String = ""
var grhs
var textures
var ready_resources := false

func _ready() -> void:
	_load()

func _load() -> void:
	root = ResourcesRootScript.resolve()
	if root.is_empty():
		push_warning("AO_RESOURCES no encontrado. Ejecutá tools/setup_reference.ps1")
		return
	grhs = GrhCatalogScript.new()
	if not grhs.load_from_root(root):
		push_warning("No se pudo cargar Graficos.ini desde %s" % root)
		return
	textures = TextureCacheScript.new()
	textures.setup(root)
	ready_resources = true
	print("Recursos AO: %s" % root)
