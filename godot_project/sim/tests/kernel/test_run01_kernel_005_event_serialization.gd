extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_KERNEL_005_event_serialization"


func run() -> void:
	var log := EventLog.new()
	var event: SimEvent = log.append_event("tick_advanced", 5, {"tick": 5})
	var serialized: Dictionary = event.to_dict()
	assert_has_keys(serialized, ["id", "type", "tick", "payload"])
	assert_eq(serialized["type"], "tick_advanced")
	assert_eq(int(serialized["tick"]), 5)

	var round_trip: SimEvent = SimEvent.from_dict(serialized)
	assert_eq(round_trip.id, event.id)
	assert_eq(round_trip.type, event.type)
	assert_eq(round_trip.tick, event.tick)
	assert_eq(JSON.stringify(round_trip.payload), JSON.stringify(event.payload))

	var log_data: Dictionary = log.to_dict()
	var restored: EventLog = EventLog.from_dict(log_data)
	assert_eq(restored.events.size(), 1)
	assert_eq(restored.events[0].id, "evt_001")
