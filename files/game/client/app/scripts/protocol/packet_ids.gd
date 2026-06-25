class_name PacketIds
extends RefCounted

# Server → Client (ServerPacketID ordinal)
const SERVER_CONNECTED := 1
const SERVER_LOGGED := 2
const SERVER_ERROR_MSG := 94
const SERVER_ACCOUNT_CHARACTER_LIST := 199

# Client → Server (ClientPacketID ordinal, PYMMO=0)
const CLIENT_LOGIN_EXISTING_CHAR := 307
const CLIENT_LOGIN_NEW_CHAR := 308
const CLIENT_CREATE_ACCOUNT := 541
const CLIENT_LOGIN_ACCOUNT := 542
