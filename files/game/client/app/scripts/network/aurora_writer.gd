class_name AuroraWriter
extends RefCounted

var _buf: PackedByteArray = PackedByteArray()

func clear() -> void:
	_buf = PackedByteArray()

func get_data() -> PackedByteArray:
	return _buf

func write_int16(value: int) -> void:
	_buf.append(value & 0xFF)
	_buf.append((value >> 8) & 0xFF)

func write_varint(value: int) -> void:
	var v := value
	while v > 0x7F:
		_buf.append((v & 0x7F) | 0x80)
		v = v >> 7
	_buf.append(v & 0x7F)

func write_int32(value: int) -> void:
	_buf.append(value & 0xFF)
	_buf.append((value >> 8) & 0xFF)
	_buf.append((value >> 16) & 0xFF)
	_buf.append((value >> 24) & 0xFF)

func write_string8(text: String) -> void:
	var bytes := text.to_utf8_buffer()
	write_varint(bytes.size())
	_buf.append_array(bytes)

func write_int(value: int) -> void:
	write_varint(value)

func frame() -> PackedByteArray:
	var framed := PackedByteArray()
	framed.append(_buf.size() & 0xFF)
	framed.append((_buf.size() >> 8) & 0xFF)
	framed.append_array(_buf)
	return framed
