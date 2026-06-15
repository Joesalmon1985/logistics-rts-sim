class_name MilitarySnapshotExporter
extends RefCounted

## Variant-aware snapshot for the trial military game: JSON-compatible, integer
## only, enough for a bot or future Godot viewer to render the battlefield.

const SCHEMA_VERSION: int = 1


static func export_snapshot(state: GameState) -> Dictionary:
	var player_data: Array = []
	for player in state.players:
		player_data.append(player.to_dict())

	return {
		"schema_version": SCHEMA_VERSION,
		"variant": state.variant_id,
		"tick": state.tick,
		"seed": state.seed,
		"rng_state": state.rng_state,
		"map": state.map.to_dict() if state.map != null else {},
		"display_layers": _build_display_layers(state),
		# Cross-variant snapshot shape contract (RUN_01 KERNEL_006): economy-only
		# layers are present but empty in the military variant.
		"roads": [],
		"road_nodes": [],
		"resource_fields": _bucket_list(state, MilitaryConstants.BUCKET_RESOURCE_FIELDS),
		"buildings": _bucket_list(state, MilitaryConstants.BUCKET_BUILDINGS),
		"construction_sites": _bucket_list(state, MilitaryConstants.BUCKET_CONSTRUCTION_SITES),
		"production_queues": _bucket_list(state, MilitaryConstants.BUCKET_PRODUCTION_QUEUES),
		"units": _bucket_list(state, MilitaryConstants.BUCKET_UNITS),
		"projectiles": _bucket_list(state, MilitaryConstants.BUCKET_PROJECTILES),
		"repair_jobs": _bucket_list(state, MilitaryConstants.BUCKET_REPAIR_JOBS),
		"vision": _vision_layer(state),
		"power": _power_layer(state),
		"tech": _tech_layer(state),
		"military_players": _bucket_list(state, MilitaryConstants.BUCKET_PLAYERS),
		"combat_log": _combat_log(state),
		"entities": state.entities.duplicate(true),
		"diagnostics": state.diagnostics.duplicate(true),
		"players": player_data,
	}


static func _bucket_list(state: GameState, bucket: String) -> Array:
	var result: Array = []
	if not state.entities.has(bucket):
		return result
	for key in state.entities[bucket]:
		var value: Variant = state.entities[bucket][key]
		if typeof(value) == TYPE_DICTIONARY:
			result.append((value as Dictionary).duplicate(true))
	return result


static func _build_display_layers(state: GameState) -> Dictionary:
	var terrain_layer: Array = []
	var passability_layer: Array = []
	if state.map == null:
		return {"terrain": terrain_layer, "passability": passability_layer, "hex_grid_visible": 0}
	for q in range(state.map.width):
		for r in range(state.map.height):
			var coord := HexCoord.new(q, r)
			if not state.map.in_bounds(coord):
				continue
			var tile: HexTile = state.map.get_tile(coord)
			if tile == null:
				continue
			terrain_layer.append({"q": q, "r": r, "terrain_type": tile.terrain_type, "height_level": tile.height_level})
			passability_layer.append({"q": q, "r": r, "passability": tile.passability, "buildability": tile.buildability})
	return {"terrain": terrain_layer, "passability": passability_layer, "hex_grid_visible": 0}


static func _combat_log(state: GameState) -> Array:
	var bucket: Dictionary = state.entities.get(MilitaryConstants.BUCKET_COMBAT_LOG, {})
	return bucket.get("entries", []).duplicate(true)


static func _players(state: GameState) -> Array:
	return state.entities.get(MilitaryConstants.BUCKET_PLAYERS, {}).values()


static func _vision_layer(state: GameState) -> Dictionary:
	var result: Dictionary = {}
	var bucket: Dictionary = state.entities.get(MilitaryConstants.BUCKET_VISION, {})
	for player_id in bucket:
		result[player_id] = (bucket[player_id] as Dictionary).duplicate(true)
	return result


static func _power_layer(state: GameState) -> Array:
	var result: Array = []
	for player in _players(state):
		result.append({
			"player_id": str(player.get("player_id", "")),
			"supply": int(player.get("power_supply", 0)),
			"demand": int(player.get("power_demand", 0)),
			"powered": int(player.get("powered", 1)),
		})
	return result


static func _tech_layer(state: GameState) -> Dictionary:
	var result: Dictionary = {}
	var research_bucket: Dictionary = state.entities.get(MilitaryConstants.BUCKET_RESEARCH, {})
	for player in _players(state):
		var pid: String = str(player.get("player_id", ""))
		result[pid] = {
			"unlocked": player.get("tech_unlocked", []).duplicate(),
			"research": (research_bucket.get(pid, {}) as Dictionary).duplicate(true),
		}
	return result


static func export_training_snapshot(state: GameState, actor_id: String = "") -> Dictionary:
	var units_compact: Array = []
	for unit in _bucket_list(state, MilitaryConstants.BUCKET_UNITS):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		units_compact.append({
			"owner": str(unit.get("owner", unit.get("owner_player_id", ""))),
			"visibility": str(unit.get("visibility", "visible")),
		})

	return {
		"schema_version": SCHEMA_VERSION,
		"variant": state.variant_id,
		"tick": state.tick,
		"seed": state.seed,
		"actor_id": actor_id,
		"units": units_compact,
		"buildings": _bucket_list(state, MilitaryConstants.BUCKET_BUILDINGS),
		"construction_sites": _bucket_list(state, MilitaryConstants.BUCKET_CONSTRUCTION_SITES),
		"combat_log": _combat_log(state),
		"diagnostics": state.diagnostics.duplicate(true),
		"available_commands": CommandAvailabilityProvider.enumerate_legal_commands(state, actor_id),
	}
