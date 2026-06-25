class_name AuroraReader
extends RefCounted

var _data: PackedByteArray
var _pos: int = 0

func _init(data: PackedByteArray) -> void:
	_data = data

func available() -> int:
	return _data.size() - _pos

func read_int16() -> int:
	var v := _data[_pos] | (_data[_pos + 1] << 8)
	_pos += 2
	if v >= 0x8000:
		v -= 0x10000
	return v

func read_varint() -> int:
	var result := 0
	var shift := 0
	for _i in range(10):
		var b: int = _data[_pos]
		_pos += 1
		result |= (b & 0x7F) << shift
		if (b & 0x80) == 0:
			return result
		shift += 7
	push_error("varint demasiado largo")
	return 0

func read_int() -> int:
	return read_varint()

func read_string8() -> String:
	var n := read_varint()
	if n <= 0:
		return ""
	var slice := _data.slice(_pos, _pos + n)
	_pos += n
	return slice.get_string_from_utf8()

func read_bool() -> bool:
	var v := _data[_pos] != 0
	_pos += 1
	return v

func skip_safe_array_int8() -> void:
	var dims := read_varint()
	var total := 0
	for _i in range(dims):
		var _lower := read_varint()
		var count := read_varint()
		total += count
	_pos += total
