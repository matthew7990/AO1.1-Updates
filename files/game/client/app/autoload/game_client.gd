extends Node

## Cliente Aurora — replica modNetwork.bas + Mod_TCP.Login (PYMMO=0).

const AuroraWriterScript := preload("res://scripts/network/aurora_writer.gd")
const AuroraReaderScript := preload("res://scripts/network/aurora_reader.gd")
const Packets := preload("res://scripts/protocol/packet_ids.gd")

signal connected_to_server
signal login_failed(reason: String)
signal server_error(message: String)
signal account_characters_received(characters: Array)
signal logged_in(new_user: bool)
signal disconnected

enum LoginMode { NONE, INGRESANDO_CUENTA, CREANDO_CUENTA, NORMAL, CREAR_PJ }

const DEFAULT_HOST := "127.0.0.1"
const DEFAULT_PORT := 7667
const DEBUG_CONNECTED_PAYLOAD := true

var host: String = DEFAULT_HOST
var port: int = DEFAULT_PORT
var account_characters: Array = []
var is_busy := false

var account_email := ""
var account_password := ""

var _peer: StreamPeerTCP
var _rx_buffer: PackedByteArray = PackedByteArray()
var _pending_mode: LoginMode = LoginMode.NONE
var _got_connected_packet := false
var _pending_char_create: Dictionary = {}
var _connecting := false
var _request_deadline_ms := 0

const REQUEST_TIMEOUT_MS := 15000

func _ready() -> void:
	_reset_peer()

func _reset_peer() -> void:
	if _peer and _peer.get_status() == StreamPeerTCP.STATUS_CONNECTED:
		_peer.disconnect_from_host()
	_peer = StreamPeerTCP.new()

func _process(_delta: float) -> void:
	var status := _peer.get_status()
	if status == StreamPeerTCP.STATUS_NONE:
		return
	_peer.poll()
	status = _peer.get_status()
	if status == StreamPeerTCP.STATUS_CONNECTING:
		return
	if status != StreamPeerTCP.STATUS_CONNECTED:
		return
	var avail: int = _peer.get_available_bytes()
	if avail > 0:
		var chunk: Array = _peer.get_data(avail)
		if chunk[0] == OK:
			_rx_buffer.append_array(chunk[1])
			_pump_frames()
	_check_request_timeout()

func set_credentials(email: String, password: String) -> void:
	account_email = email.strip_edges()
	account_password = password

func login_or_connect(mode: LoginMode) -> void:
	if is_busy:
		return
	_pending_mode = mode
	_begin_request()
	if peer_connected() and _got_connected_packet:
		_login()
	elif not peer_connected():
		_connect_async()

func create_character(name: String, race: int, gender: int, class_id: int, head: int, home: int) -> void:
	if is_busy:
		return
	_begin_request()
	_pending_char_create = {
		"name": name.strip_edges(),
		"race": race,
		"gender": gender,
		"class": class_id,
		"head": head,
		"home": home,
	}
	if peer_connected() and _got_connected_packet:
		_send_login_new_char()
	elif not peer_connected():
		_pending_mode = LoginMode.CREAR_PJ
		_connect_async()
	else:
		_pending_mode = LoginMode.CREAR_PJ

func login_existing_character(char_id: int, char_name: String) -> void:
	if is_busy:
		return
	_begin_request()
	var w: AuroraWriter = AuroraWriterScript.new()
	w.write_int16(Packets.CLIENT_LOGIN_EXISTING_CHAR)
	w.write_int32(char_id)
	w.write_string8(char_name)
	_send_framed(w)

func _connect_async() -> void:
	if _connecting:
		return
	_connecting = true
	is_busy = true
	var err: Error = await connect_to_game_server()
	_connecting = false
	if err != OK:
		is_busy = false
		_request_deadline_ms = 0
		login_failed.emit(_friendly_error(err))
		_pending_mode = LoginMode.NONE
		_pending_char_create.clear()
	# Si OK: is_busy sigue true hasta respuesta del servidor (eAccountCharacterList / error)

func _begin_request() -> void:
	is_busy = true
	_request_deadline_ms = Time.get_ticks_msec() + REQUEST_TIMEOUT_MS

func _end_request() -> void:
	is_busy = false
	_request_deadline_ms = 0

func _check_request_timeout() -> void:
	if _request_deadline_ms <= 0:
		return
	if Time.get_ticks_msec() > _request_deadline_ms:
		_request_deadline_ms = 0
		is_busy = false
		_pending_mode = LoginMode.NONE
		login_failed.emit("El servidor no respondio a tiempo.")

func connect_to_game_server(p_host: String = DEFAULT_HOST, p_port: int = DEFAULT_PORT) -> Error:
	await _close_peer_async()
	_got_connected_packet = false
	_rx_buffer = PackedByteArray()
	_reset_peer()
	host = p_host
	port = p_port
	var err: Error = _peer.connect_to_host(host, port)
	if err != OK:
		return err
	var deadline: int = Time.get_ticks_msec() + 8000
	while _peer.get_status() == StreamPeerTCP.STATUS_CONNECTING:
		_peer.poll()
		if Time.get_ticks_msec() > deadline:
			return ERR_TIMEOUT
		await get_tree().process_frame
	_peer.poll()
	if _peer.get_status() != StreamPeerTCP.STATUS_CONNECTED:
		return ERR_CANT_CONNECT
	return OK

func disconnect_from_server() -> void:
	await _close_peer_async()
	_rx_buffer = PackedByteArray()
	_pending_mode = LoginMode.NONE
	_pending_char_create.clear()
	_got_connected_packet = false
	_connecting = false
	is_busy = false
	_request_deadline_ms = 0
	disconnected.emit()

func _close_peer_async() -> void:
	var status := _peer.get_status()
	if status == StreamPeerTCP.STATUS_CONNECTED or status == StreamPeerTCP.STATUS_CONNECTING:
		_peer.disconnect_from_host()
		var deadline := Time.get_ticks_msec() + 2000
		while _peer.get_status() != StreamPeerTCP.STATUS_NONE:
			if Time.get_ticks_msec() > deadline:
				break
			await get_tree().process_frame

func peer_connected() -> bool:
	return _peer.get_status() == StreamPeerTCP.STATUS_CONNECTED

func _login() -> void:
	_login_with_mode(_pending_mode)

func _login_with_mode(mode: LoginMode) -> void:
	match mode:
		LoginMode.INGRESANDO_CUENTA:
			_send_login_account()
		LoginMode.CREANDO_CUENTA:
			_send_create_account()
		LoginMode.CREAR_PJ:
			_send_login_new_char()
		_:
			return
	if mode != LoginMode.CREAR_PJ:
		_pending_mode = LoginMode.NONE

func _send_login_account() -> void:
	var w: AuroraWriter = AuroraWriterScript.new()
	w.write_int16(Packets.CLIENT_LOGIN_ACCOUNT)
	w.write_string8(account_email)
	w.write_string8(account_password)
	_send_framed(w)

func _send_create_account() -> void:
	var w: AuroraWriter = AuroraWriterScript.new()
	w.write_int16(Packets.CLIENT_CREATE_ACCOUNT)
	w.write_string8(account_email)
	w.write_string8(account_password)
	_send_framed(w)

func _send_login_new_char() -> void:
	if _pending_char_create.is_empty():
		return
	var w: AuroraWriter = AuroraWriterScript.new()
	w.write_int16(Packets.CLIENT_LOGIN_NEW_CHAR)
	w.write_string8(_pending_char_create.get("name", ""))
	w.write_int(_pending_char_create.get("race", 1))
	w.write_int(_pending_char_create.get("gender", 1))
	w.write_int(_pending_char_create.get("class", 1))
	w.write_int(_pending_char_create.get("head", 1))
	w.write_int(_pending_char_create.get("home", 1))
	_send_framed(w)
	_pending_char_create.clear()
	_pending_mode = LoginMode.NONE

func _send_framed(writer: AuroraWriter) -> void:
	if _peer.get_status() != StreamPeerTCP.STATUS_CONNECTED:
		login_failed.emit("Sin conexion al servidor.")
		return
	_peer.put_data(writer.frame())

func _pump_frames() -> void:
	while _rx_buffer.size() >= 2:
		var length: int = _rx_buffer[0] | (_rx_buffer[1] << 8)
		if _rx_buffer.size() < 2 + length:
			return
		var body: PackedByteArray = _rx_buffer.slice(2, 2 + length)
		_rx_buffer = _rx_buffer.slice(2 + length)
		_handle_packet(body)

func _handle_packet(body: PackedByteArray) -> void:
	if body.is_empty():
		return
	var reader: AuroraReader = AuroraReaderScript.new(body)
	var packet_id: int = reader.read_int16()
	match packet_id:
		Packets.SERVER_CONNECTED:
			if DEBUG_CONNECTED_PAYLOAD and reader.available() > 0:
				reader.skip_safe_array_int8()
			_got_connected_packet = true
			connected_to_server.emit()
			if _pending_mode != LoginMode.NONE:
				_login()
		Packets.SERVER_ACCOUNT_CHARACTER_LIST:
			account_characters = _read_character_list(reader)
			_end_request()
			account_characters_received.emit(account_characters)
		Packets.SERVER_LOGGED:
			var new_user: bool = reader.read_bool()
			_end_request()
			logged_in.emit(new_user)
		Packets.SERVER_ERROR_MSG:
			var msg: String = reader.read_string8()
			_end_request()
			server_error.emit(msg)
			login_failed.emit(msg)
		_:
			print("Paquete servidor: ", packet_id)

func _read_character_list(reader: AuroraReader) -> Array:
	var count: int = reader.read_int()
	var chars: Array = []
	for _i in range(count):
		chars.append({
			"id": reader.read_int(),
			"name": reader.read_string8(),
			"body": reader.read_int(),
			"head": reader.read_int(),
			"class": reader.read_int(),
			"map": reader.read_int(),
			"pos_x": reader.read_int(),
			"pos_y": reader.read_int(),
			"level": reader.read_int(),
			"status": reader.read_int(),
			"helmet": reader.read_int(),
			"shield": reader.read_int(),
			"weapon": reader.read_int(),
			"backpack": reader.read_int(),
		})
	return chars

func _friendly_error(err: Error) -> String:
	match err:
		ERR_ALREADY_IN_USE:
			return "Conexion ocupada. Espera un momento o reinicia el cliente."
		ERR_CANT_CONNECT:
			return "No se pudo conectar al servidor (¿esta corriendo en el puerto %d?)." % port
		ERR_TIMEOUT:
			return "Tiempo de espera agotado al conectar."
		_:
			var raw := error_string(err)
			if raw.to_lower().contains("already"):
				return "Conexion ocupada. Cerra Godot y volve a abrir, o usa otro email."
			return "Error de red: %s" % raw
