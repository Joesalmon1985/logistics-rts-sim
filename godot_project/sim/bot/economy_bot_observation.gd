class_name EconomyBotObservation
extends RefCounted

## Snapshot + legal-command observation boundary for economy bots (ECON-BOT-004).

var player_id: String = "p1"
var tick: int = 0
var snapshot: Dictionary = {}
var legal_commands: Array = []


static func from_snapshot(snapshot: Dictionary, player_id: String) -> EconomyBotObservation:
	var obs := EconomyBotObservation.new()
	obs.player_id = player_id
	obs.tick = int(snapshot.get("tick", 0))
	obs.snapshot = snapshot.duplicate(true)
	var availability: Dictionary = snapshot.get("available_commands", {})
	if typeof(availability) == TYPE_DICTIONARY:
		obs.legal_commands = availability.get(player_id, [])
	else:
		obs.legal_commands = []
	return obs


func own_buildings() -> Array:
	var result: Array = []
	for building in snapshot.get("buildings", []):
		if str(building.get("player_id", "")) == player_id:
			result.append(building)
	return result


func own_construction_sites() -> Array:
	var result: Array = []
	for site in snapshot.get("construction_sites", []):
		if str(site.get("player_id", "")) == player_id:
			result.append(site)
	return result


func own_roads() -> Array:
	var result: Array = []
	for road in snapshot.get("roads", []):
		if str(road.get("player_id", "")) == player_id:
			result.append(road)
	return result


func own_road_nodes() -> Array:
	var result: Array = []
	for node in snapshot.get("road_nodes", []):
		if str(node.get("player_id", "")) == player_id:
			result.append(node)
	return result


func diagnostics() -> Dictionary:
	return snapshot.get("diagnostics", {})


func economy_score() -> int:
	var scores: Dictionary = diagnostics().get("economy_scores", {})
	return int(scores.get(player_id, 0))


func hq_position() -> Vector2i:
	for building in own_buildings():
		if str(building.get("building_type", "")) == "headquarters":
			return Vector2i(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
	return Vector2i(0, 0)


func has_building_type(building_type: String) -> bool:
	for building in own_buildings():
		if str(building.get("building_type", "")) == building_type:
			if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
				return true
	for site in own_construction_sites():
		if str(site.get("building_type", "")) == building_type:
			return true
	return false


func forest_hexes() -> Array:
	var result: Array = []
	var ownership: Dictionary = {}
	for entry in snapshot.get("display_layers", {}).get("ownership", []):
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var key: String = "%d,%d" % [int(entry.get("q", 0)), int(entry.get("r", 0))]
		ownership[key] = str(entry.get("owner_player_id", ""))
	for entry in snapshot.get("display_layers", {}).get("tree_density", []):
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		if int(entry.get("tree_density", 0)) < 20:
			continue
		var q: int = int(entry.get("q", 0))
		var r: int = int(entry.get("r", 0))
		var owner: String = ownership.get("%d,%d" % [q, r], player_id)
		if owner != "" and owner != player_id:
			continue
		result.append(Vector2i(q, r))
	for hex_entry in snapshot.get("map", {}).get("hexes", []):
		if typeof(hex_entry) != TYPE_DICTIONARY:
			continue
		var q2: int = int(hex_entry.get("q", 0))
		var r2: int = int(hex_entry.get("r", 0))
		var key2: String = "%d,%d" % [q2, r2]
		var duplicate: bool = false
		for existing in result:
			if existing.x == q2 and existing.y == r2:
				duplicate = true
				break
		if duplicate:
			continue
		var owner2: String = str(hex_entry.get("owner_player_id", ownership.get(key2, player_id)))
		if owner2 != "" and owner2 != player_id:
			continue
		if int(hex_entry.get("tree_density", 0)) >= 20:
			result.append(Vector2i(q2, r2))
	result.sort_custom(func(a: Vector2i, b: Vector2i) -> bool:
		if a.x != b.x:
			return a.x < b.x
		return a.y < b.y
	)
	return result
