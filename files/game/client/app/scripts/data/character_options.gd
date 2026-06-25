class_name CharacterOptions
extends RefCounted

const RACES: PackedStringArray = [
	"", "Humano", "Elfo", "Elfo Oscuro", "Gnomo", "Enano", "Orco",
]

const GENDERS: PackedStringArray = ["", "Hombre", "Mujer"]

const CLASSES: PackedStringArray = [
	"", "Mago", "Clerigo", "Guerrero", "Asesino", "Ladron", "Bardo",
	"Druida", "Bandido", "Paladin", "Cazador", "Trabajador", "Pirata",
]

const HOMES: PackedStringArray = [
	"", "Ullathorpe", "Nix", "Banderbill", "Lindos", "Arghal",
	"Arkhein", "Forgat", "Eldoria", "Penthar", "Morgrim",
]

static func heads_for(race: int, gender: int) -> Array:
	match "%d_%d" % [race, gender]:
		"1_1":
			return _range(1, 41) + _range(778, 791)
		"1_2":
			return _range(50, 80) + _range(187, 190) + _range(230, 246)
		"2_1":
			return _range(101, 132) + _range(531, 545)
		"2_2":
			return _range(150, 179) + _range(758, 777)
		"3_1":
			return _range(200, 229) + _range(792, 810)
		"3_2":
			return _range(250, 279)
		"4_1":
			return _range(400, 429)
		"4_2":
			return _range(450, 479)
		"5_1":
			return _range(300, 344)
		"5_2":
			return _range(350, 379)
		"6_1":
			return _range(500, 529)
		"6_2":
			return _range(550, 579)
		_:
			return [1]

static func _range(from: int, to: int) -> Array:
	var a: Array = []
	for i in range(from, to + 1):
		a.append(i)
	return a

static func get_class_label(class_id: int) -> String:
	if class_id >= 0 and class_id < CLASSES.size():
		return CLASSES[class_id]
	return "?"

static func race_name(race_id: int) -> String:
	if race_id >= 0 and race_id < RACES.size():
		return RACES[race_id]
	return "?"

static func home_name(home_id: int) -> String:
	if home_id >= 0 and home_id < HOMES.size():
		return HOMES[home_id]
	return "?"
