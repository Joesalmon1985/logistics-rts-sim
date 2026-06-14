class_name SimEvent
extends RefCounted

var id: String = ""
var type: String = ""
var tick: int = 0
var payload: Dictionary = {}


func _init(
	event_id: String = "",
	event_type: String = "",
	event_tick: int = 0,
	event_payload: Dictionary = {},
) -> void:
	id = event_id
	type = event_type
	tick = event_tick
	payload = event_payload.duplicate(true)


func to_dict() -> Dictionary:
	return {
		"id": id,
		"type": type,
		"tick": tick,
		"payload": payload.duplicate(true),
	}


static func from_dict(data: Dictionary) -> SimEvent:
	return SimEvent.new(
		str(data.get("id", "")),
		str(data.get("type", "")),
		int(data.get("tick", 0)),
		data.get("payload", {}),
	)
