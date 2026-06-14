class_name EventLog
extends RefCounted

var events: Array[SimEvent] = []
var next_event_number: int = 1


func append_event(event_type: String, tick: int, payload: Dictionary = {}) -> SimEvent:
	var event_id: String = "evt_%03d" % next_event_number
	next_event_number += 1
	var event := SimEvent.new(event_id, event_type, tick, payload)
	events.append(event)
	return event


func to_dict() -> Dictionary:
	var serialized: Array = []
	for event in events:
		serialized.append(event.to_dict())
	return {
		"events": serialized,
		"next_event_number": next_event_number,
	}


static func from_dict(data: Dictionary) -> EventLog:
	var log := EventLog.new()
	log.next_event_number = int(data.get("next_event_number", 1))
	for event_data in data.get("events", []):
		log.events.append(SimEvent.from_dict(event_data))
	return log
