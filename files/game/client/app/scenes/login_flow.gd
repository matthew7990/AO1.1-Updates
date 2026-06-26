extends Control

## Flujo de login: conectar, cuenta y crear PJ en una sola pantalla.

enum Screen { CONNECT, ACCOUNT, CREATE_CHAR }

const GRH_CONNECT_FRAME := 1169
const GRH_ACCOUNT_UI := 3839
const GRH_CREATE_PANEL := 727
const GRH_CREATE_LOGO := 1171
const MAX_CHARS := 10

const Vb6FormShell := preload("res://scripts/ui/vb6_form_shell.gd")
const Vb6FormCoords := preload("res://scripts/ui/vb6_form_coords.gd")
const GraphicalButton := preload("res://scripts/ui/ao_graphical_button.gd")
const InterfaceGrhDrawer := preload("res://scripts/ui/interface_grh_drawer.gd")
const CreateCharTextLayer := preload("res://scripts/ui/create_char_text_layer.gd")
const CharacterOptions := preload("res://scripts/data/character_options.gd")

var _screen := Screen.CONNECT
var _selected_slot := 0
var _remember := false

var _login_shell: Vb6FormShell
var _new_account_shell: Vb6FormShell
var _email: LineEdit
var _password: LineEdit
var _new_email: LineEdit
var _new_password: LineEdit
var _new_captcha: LineEdit
var _captcha_lbl: Label
var _captcha_answer := 0

var _btn_play: GraphicalButton
var _btn_create: GraphicalButton
var _btn_logout: GraphicalButton
var _btn_close: GraphicalButton
var _btn_create_back: GraphicalButton
var _btn_create_ok: GraphicalButton

var _char_name: LineEdit
var _create_texts: CreateCharTextLayer
var _race_idx := 1
var _gender_idx := 1
var _class_idx := 1
var _home_idx := 1
var _head_idx := 0
var _head_ids: Array = []
var _heading := 3

var _status: Label
var _connecting: ColorRect
var _slot_areas: Array[Control] = []

func _ready() -> void:
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	set_anchors_preset(Control.PRESET_FULL_RECT)
	custom_minimum_size = Vector2i(1024, 768)

	_status = Label.new()
	_status.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	_status.offset_top = -28
	_status.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_status.add_theme_color_override("font_color", Color(1, 0.45, 0.3))
	add_child(_status)

	_connecting = ColorRect.new()
	_connecting.color = Color(0, 0, 0, 0.55)
	_connecting.set_anchors_preset(Control.PRESET_FULL_RECT)
	_connecting.visible = false
	add_child(_connecting)

	_build_login_shell()
	_build_account_slots()
	_build_screen_buttons()
	_build_create_char_ui()

	GameClient.connected_to_server.connect(func(): _set_status("Conectado."))
	GameClient.account_characters_received.connect(_on_characters)
	GameClient.logged_in.connect(_on_logged_in)
	GameClient.login_failed.connect(_on_error)
	GameClient.server_error.connect(_on_error)

	_show_connect()
	_set_status("Iniciando...")
	_connect_server()

func _build_login_shell() -> void:
	_login_shell = Vb6FormShell.new()
	_login_shell.set_background(AOAssets.load_interface("ventanaconectar.bmp"))
	add_child(_login_shell)

	_email = _login_shell.add_field(710, 1580, 1840, 285, false, 100)
	_password = _login_shell.add_field(2820, 1560, 1840, 285, true, 30)
	_login_shell.add_checkbox(640, 2150, 255, 255, func(on): _remember = on)
	_login_shell.add_button("boton-ingresar", 2750, 2055, 1980, 420, _on_login)
	_login_shell.add_button("boton-cuenta", 645, 3030, 1980, 420, _show_new_account)
	_login_shell.add_button("boton-salir", 2750, 3030, 1980, 420, func(): get_tree().quit())
	_email.text_submitted.connect(func(_t): _on_login())
	_password.text_submitted.connect(func(_t): _on_login())
	_place_form(_login_shell)

func _build_account_slots() -> void:
	for i in MAX_CHARS:
		var col := i % 5
		var row := i / 5
		var area := Control.new()
		area.position = Vector2(207 + col * 131, 246 + row * 158)
		area.size = Vector2(79, 93)
		area.mouse_filter = Control.MOUSE_FILTER_STOP
		area.gui_input.connect(_on_slot_input.bind(i + 1))
		area.visible = false
		add_child(area)
		_slot_areas.append(area)

func _build_screen_buttons() -> void:
	_btn_play = _screen_btn("boton-jugar", _on_enter_char)
	_btn_create = _screen_btn("boton-crear-pj", _show_create_char)
	_btn_logout = _screen_btn("boton-home", _on_logout)
	_btn_close = _screen_btn("boton-cerrar", func(): get_tree().quit())
	_btn_create_back = _screen_btn("boton-volver-flecha", _show_account)
	_btn_create_ok = _screen_btn("boton-crear-pj", _on_confirm_create)
	if _btn_create_ok.texture_normal == null:
		_btn_create_ok.setup_bmp(
			"boton-crear-personaje-default.bmp",
			"boton-crear-personaje-over.bmp",
			"boton-crear-personaje-off.bmp"
		)
	_btn_play.place_at_design(604, 711)
	_btn_create.place_at_design(256, 710)
	_btn_logout.place_at_design(19, 21)
	_btn_close.place_at_design(971, 21)
	_btn_create_back.place_at_design(148, 630)
	_btn_create_ok.place_at_design(731, 630)
	_set_account_buttons(false)
	_set_create_buttons(false)

func _screen_btn(prefix: String, cb: Callable) -> GraphicalButton:
	var b: GraphicalButton = GraphicalButton.new()
	b.setup_bmp("%s-default.bmp" % prefix, "%s-over.bmp" % prefix, "%s-off.bmp" % prefix)
	b.visible = false
	b.pressed.connect(cb)
	add_child(b)
	return b

func _build_create_char_ui() -> void:
	_char_name = LineEdit.new()
	_char_name.max_length = 18
	_char_name.visible = false
	_char_name.position = Vector2(416, 224)
	_char_name.size = Vector2(142, 20)
	Vb6FormShell._style_field(_char_name)
	_char_name.text_changed.connect(_sync_create_texts)
	add_child(_char_name)

	_create_texts = CreateCharTextLayer.new()
	_create_texts.visible = false
	add_child(_create_texts)
	_refresh_heads()

func _place_form(shell: Vb6FormShell) -> void:
	shell.position = Vb6FormCoords.centered_on_connect(shell.design_size.x, shell.design_size.y)

func _show_connect() -> void:
	_screen = Screen.CONNECT
	GameState.set_state(GameState.State.CONNECT_SCREEN)
	_login_shell.visible = true
	if _new_account_shell:
		_new_account_shell.queue_free()
		_new_account_shell = null
	_char_name.visible = false
	_create_texts.visible = false
	_set_account_buttons(false)
	_set_create_buttons(false)
	for a in _slot_areas:
		a.visible = false
	queue_redraw()

func _show_new_account() -> void:
	_login_shell.visible = false
	if _new_account_shell:
		_new_account_shell.queue_free()
	_new_account_shell = Vb6FormShell.new()
	_new_account_shell.set_background(AOAssets.load_interface("ventanacrearcuenta.bmp"))
	add_child(_new_account_shell)
	_new_account_shell.add_field(720, 1605, 1815, 255) # nombre (decorativo)
	_new_account_shell.add_field(2835, 1605, 1815, 255) # apellido
	_new_email = _new_account_shell.add_field(720, 2370, 1815, 255)
	_new_password = _new_account_shell.add_field(2820, 2370, 1605, 255, true)
	_captcha_lbl = _new_account_shell.add_caption(1200, 2970, 855, 255, "", Color(0, 0.25, 0.5))
	_new_captcha = _new_account_shell.add_field(2160, 2970, 1095, 285)
	_new_account_shell.add_button("boton-cancelar", 645, 3630, 1935, 375, _show_connect)
	_new_account_shell.add_button("boton-crear-cuenta-rojo", 2775, 3630, 1920, 390, _on_create_account)
	_roll_captcha()
	_place_form(_new_account_shell)
	queue_redraw()

func _show_account() -> void:
	_screen = Screen.ACCOUNT
	GameState.set_state(GameState.State.ACCOUNT_SCREEN)
	_login_shell.visible = false
	if _new_account_shell:
		_new_account_shell.visible = false
	_char_name.visible = false
	_create_texts.visible = false
	_selected_slot = 0
	_set_account_buttons(true)
	_set_create_buttons(false)
	for a in _slot_areas:
		a.visible = true
	if GameClient.account_characters.size() > 0:
		_selected_slot = 1
	queue_redraw()

func _show_create_char() -> void:
	if GameClient.account_characters.size() >= MAX_CHARS:
		_set_status("Maximo de personajes por cuenta (10).")
		return
	_screen = Screen.CREATE_CHAR
	GameState.set_state(GameState.State.CREATE_CHAR_SCREEN)
	_login_shell.visible = false
	if _new_account_shell:
		_new_account_shell.visible = false
	_char_name.visible = true
	_char_name.text = ""
	_create_texts.visible = true
	_set_account_buttons(false)
	_set_create_buttons(true)
	for a in _slot_areas:
		a.visible = false
	_race_idx = 1
	_gender_idx = 1
	_class_idx = 1
	_home_idx = 1
	_heading = 3
	_refresh_heads()
	_sync_create_texts()
	queue_redraw()

func _set_account_buttons(on: bool) -> void:
	_btn_play.visible = on
	_btn_create.visible = on
	_btn_logout.visible = on
	_btn_close.visible = on

func _set_create_buttons(on: bool) -> void:
	_btn_create_back.visible = on
	_btn_create_ok.visible = on

func _draw() -> void:
	if not AOResources.ready_resources:
		return
	match _screen:
		Screen.CONNECT:
			if _login_shell.visible and (_new_account_shell == null or not _new_account_shell.visible):
				InterfaceGrhDrawer.draw_fullscreen(self, AOResources.grhs, AOResources.textures, GRH_CONNECT_FRAME)
		Screen.ACCOUNT:
			InterfaceGrhDrawer.draw_fullscreen(self, AOResources.grhs, AOResources.textures, GRH_ACCOUNT_UI)
			_draw_selected_char_info()
		Screen.CREATE_CHAR:
			InterfaceGrhDrawer.draw_fullscreen(self, AOResources.grhs, AOResources.textures, GRH_CONNECT_FRAME)
			InterfaceGrhDrawer.draw_at(self, AOResources.grhs, AOResources.textures, GRH_CREATE_PANEL, Vector2(475, 545))
			InterfaceGrhDrawer.draw_at(self, AOResources.grhs, AOResources.textures, GRH_CREATE_LOGO, Vector2(494, 190))

func _draw_selected_char_info() -> void:
	if _selected_slot <= 0 or _selected_slot > GameClient.account_characters.size():
		return
	var ch: Dictionary = GameClient.account_characters[_selected_slot - 1]
	var font := ThemeDB.fallback_font
	var name: String = ch.get("name", "")
	draw_string(font, Vector2(511, 565), name, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color.WHITE)
	draw_string(font, Vector2(511, 579), "Nivel %d" % int(ch.get("level", 1)), HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color.WHITE)

func _gui_input(event: InputEvent) -> void:
	if _screen != Screen.CREATE_CHAR:
		return
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if _handle_create_char_click(event.position):
			accept_event()

func _handle_create_char_click(pos: Vector2) -> bool:
	if _hit_rect(Rect2(148, 630, 98, 40), pos) or _hit_rect(Rect2(289, 525, 160, 37), pos):
		_show_account()
		return true
	if _hit_rect(Rect2(731, 630, 98, 40), pos) or _hit_rect(Rect2(532, 525, 160, 37), pos):
		_on_confirm_create()
		return true
	if _hit_rect(Rect2(540, 278, 14, 13), pos):
		_class_idx = _cycle_val(_class_idx, CharacterOptions.CLASSES.size() - 1, -1)
	elif _hit_rect(Rect2(658, 278, 13, 13), pos):
		_class_idx = _cycle_val(_class_idx, CharacterOptions.CLASSES.size() - 1, 1)
	elif _hit_rect(Rect2(657, 321, 15, 17), pos):
		_race_idx = _cycle_val(_race_idx, CharacterOptions.RACES.size() - 1, -1)
		_refresh_heads()
	elif _hit_rect(Rect2(539, 322, 14, 13), pos):
		_race_idx = _cycle_val(_race_idx, CharacterOptions.RACES.size() - 1, 1)
		_refresh_heads()
	elif _hit_rect(Rect2(415, 277, 16, 18), pos):
		_gender_idx = _cycle_val(_gender_idx, 2, -1)
		_refresh_heads()
	elif _hit_rect(Rect2(298, 276, 16, 15), pos):
		_gender_idx = _cycle_val(_gender_idx, 2, 1)
		_refresh_heads()
	elif _hit_rect(Rect2(297, 321, 17, 19), pos):
		_home_idx = _cycle_val(_home_idx, CharacterOptions.HOMES.size() - 1, -1)
	elif _hit_rect(Rect2(416, 323, 17, 15), pos):
		_home_idx = _cycle_val(_home_idx, CharacterOptions.HOMES.size() - 1, 1)
	elif _hit_rect(Rect2(325, 371, 19, 16), pos):
		_head_idx = _cycle_val(_head_idx, max(0, _head_ids.size() - 1), -1, true)
	elif _hit_rect(Rect2(394, 373, 17, 13), pos):
		_head_idx = _cycle_val(_head_idx, max(0, _head_ids.size() - 1), 1, true)
	else:
		return false
	_sync_create_texts()
	queue_redraw()
	return true

func _cycle_val(current: int, max_index: int, delta: int, zero_based := false) -> int:
	var idx := current + delta
	if zero_based:
		if idx < 0:
			return max_index
		if idx > max_index:
			return 0
		return idx
	if idx < 1:
		return max_index
	if idx > max_index:
		return 1
	return idx

func _hit_rect(rect: Rect2, pos: Vector2) -> bool:
	return rect.has_point(pos)

func _connect_server() -> void:
	_set_connecting(true)
	_set_status("Conectando...")
	var err: Error = await GameClient.connect_to_game_server()
	_set_connecting(false)
	if err != OK:
		_set_status("No se pudo conectar al servidor (puerto 7667).")

func _on_login() -> void:
	if not _valid_email(_email.text):
		_set_status("Email invalido.")
		return
	if _password.text.length() <= 3:
		_set_status("La contraseña debe tener mas de 3 caracteres.")
		return
	GameClient.set_credentials(_email.text.strip_edges(), _password.text)
	_set_status("Iniciando sesion...")
	GameClient.login_or_connect(GameClient.LoginMode.INGRESANDO_CUENTA)

func _on_create_account() -> void:
	if int(_new_captcha.text) != _captcha_answer:
		_set_status("Captcha incorrecto.")
		_roll_captcha()
		return
	if not _valid_email(_new_email.text):
		_set_status("Email invalido.")
		return
	if _new_password.text.length() <= 3:
		_set_status("La contraseña debe tener mas de 3 caracteres.")
		return
	GameClient.set_credentials(_new_email.text.strip_edges(), _new_password.text)
	_set_status("Creando cuenta...")
	GameClient.login_or_connect(GameClient.LoginMode.CREANDO_CUENTA)

func _on_characters(_chars: Array) -> void:
	_set_status("%d personaje(s)." % _chars.size())
	_show_account()

func _on_logged_in(_new: bool) -> void:
	_set_status("Personaje creado.")
	# lista llega en account_characters_received

func _on_enter_char() -> void:
	if _selected_slot <= 0 or _selected_slot > GameClient.account_characters.size():
		_set_status("Selecciona un personaje.")
		return
	var ch: Dictionary = GameClient.account_characters[_selected_slot - 1]
	_set_status("Entrando como %s..." % ch.get("name", ""))
	GameClient.login_existing_character(int(ch.get("id", 0)), str(ch.get("name", "")))

func _on_confirm_create() -> void:
	var name := _char_name.text.strip_edges()
	if name.length() < 3:
		_set_status("El nombre debe tener al menos 3 caracteres.")
		return
	if _head_ids.is_empty():
		_set_status("Selecciona raza y genero.")
		return
	_set_status("Creando personaje...")
	GameClient.create_character(
		name, _race_idx, _gender_idx, _class_idx,
		int(_head_ids[_head_idx]), _home_idx
	)

func _on_logout() -> void:
	await GameClient.disconnect_from_server()
	_show_connect()
	_set_status("Desconectado.")
	_connect_server()

func _on_slot_input(event: InputEvent, slot: int) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if slot <= GameClient.account_characters.size():
			_selected_slot = slot
			queue_redraw()

func _on_error(msg: String) -> void:
	_set_status(msg)
	if _screen == Screen.CREATE_CHAR:
		pass
	elif _screen == Screen.ACCOUNT:
		pass
	else:
		_show_connect()

func _roll_captcha() -> void:
	var a := randi() % 10
	var b := randi() % 10
	_captcha_answer = a + b
	if _captcha_lbl:
		_captcha_lbl.text = "%d + %d" % [a, b]
	if _new_captcha:
		_new_captcha.text = ""

func _refresh_heads() -> void:
	_head_ids = CharacterOptions.heads_for(_race_idx, _gender_idx)
	_head_idx = 0

func _sync_create_texts() -> void:
	_create_texts.sync_from_indices(_class_idx, _race_idx, _gender_idx, _home_idx, _char_name.text.strip_edges())

func _valid_email(email: String) -> bool:
	var e := email.strip_edges()
	return e.contains("@") and e.contains(".") and e.length() >= 5

func _set_status(msg: String) -> void:
	_status.text = msg

func _set_connecting(on: bool) -> void:
	_connecting.visible = on
