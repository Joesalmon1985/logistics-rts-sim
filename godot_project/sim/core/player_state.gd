class_name PlayerState
extends RefCounted

var player_id: String = ""


func _init(id: String = "") -> void:
	player_id = id


func to_dict() -> Dictionary:
	return {"player_id": player_id}


static func from_dict(data: Dictionary) -> PlayerState:
	return PlayerState.new(str(data.get("player_id", "")))
