extends Node

enum State {
	CONNECT_SCREEN = 1,
	ACCOUNT_SCREEN = 2,
	CREATE_CHAR_SCREEN = 3,
	GAMEPLAY_SCREEN = 0,
}

var _state: State = State.CONNECT_SCREEN

func state() -> State:
	return _state

func set_state(s: State) -> void:
	_state = s
