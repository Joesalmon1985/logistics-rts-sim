class_name GameState
extends RefCounted

var tick: int = 0
var seed: int = 0
var rng_state: int = 0
var variant_id: String = ""
var players: Array[PlayerState] = []
var map: HexMap = null
var entities: Dictionary = {}
var event_log: EventLog = null
var entity_ids: EntityIdAllocator = null
var command_queue: Array[SimCommand] = []
var diagnostics: Dictionary = {}


func _init() -> void:
	map = HexMap.new()
	event_log = EventLog.new()
	entity_ids = EntityIdAllocator.new()


func to_hash_dict() -> Dictionary:
	var player_data: Array = []
	for player in players:
		player_data.append(player.to_dict())

	return {
		"tick": tick,
		"seed": seed,
		"rng_state": rng_state,
		"variant_id": variant_id,
		"players": player_data,
		"map": map.to_dict() if map != null else {},
		"entities": CanonicalJson.sort_value(entities),
		"entity_ids": entity_ids.to_dict(),
		"diagnostics": CanonicalJson.sort_value(diagnostics),
	}


func to_dict() -> Dictionary:
	var data: Dictionary = to_hash_dict()
	data["event_log"] = event_log.to_dict()
	data["command_queue_size"] = command_queue.size()
	return data
