class_name PolicyObservationExporter
extends RefCounted

## Compact policy observation export (RUN_06b). Authoritative state only.

const SCHEMA_VERSION: int = 1
const FIXED_WIDTH: int = 16
const FIXED_HEIGHT: int = 16

const BOARD_CHANNELS: Array[String] = [
	"terrain_type",
	"owner_relation",
	"visibility",
	"resource_amount",
	"road_present",
	"has_building",
	"building_type",
	"building_hp_progress",
	"has_unit",
	"unit_type",
	"unit_hp_count",
]

const PLAYER_FEATURES: Array[String] = [
	"wood",
	"stone",
	"food",
	"population",
	"idle_workers",
	"building_count",
	"unit_count",
	"victory_points",
	"known_enemy_strength",
]

const SUMMARY_FEATURES: Array[String] = [
	"tick_norm",
	"own_unit_count",
	"enemy_unit_count",
	"own_building_count",
	"enemy_building_count",
	"objective_progress",
]


static func export_policy_observation(state: GameState, actor_id: String = "") -> Dictionary:
	if state == null:
		return {"schema_version": SCHEMA_VERSION, "error": "no_state"}

	var resolved_actor: String = actor_id
	if resolved_actor == "":
		resolved_actor = "p1"
	if state.map == null:
		return {"schema_version": SCHEMA_VERSION, "error": "no_map"}

	if state.map.width != FIXED_WIDTH or state.map.height != FIXED_HEIGHT:
		return {
			"schema_version": SCHEMA_VERSION,
			"error": "unsupported_map_size",
			"width": state.map.width,
			"height": state.map.height,
			"required_width": FIXED_WIDTH,
			"required_height": FIXED_HEIGHT,
		}

	var board_cells: Array = _export_board(state, resolved_actor)
	var player_state: Array = _export_player_vector(state, resolved_actor)
	var summary: Array = _export_summary(state, resolved_actor)

	return {
		"schema_version": SCHEMA_VERSION,
		"variant": state.variant_id,
		"tick": state.tick,
		"player_id": resolved_actor,
		"phase": "main",
		"map_width": FIXED_WIDTH,
		"map_height": FIXED_HEIGHT,
		"board_channels": BOARD_CHANNELS.duplicate(),
		"board_cells": board_cells,
		"player_feature_names": PLAYER_FEATURES.duplicate(),
		"player_state": player_state,
		"summary_feature_names": SUMMARY_FEATURES.duplicate(),
		"summary": summary,
	}


static func _export_board(state: GameState, actor_id: String) -> Array:
	var cells: Array = []
	for r in range(FIXED_HEIGHT):
		for q in range(FIXED_WIDTH):
			cells.append(_cell_features(state, actor_id, q, r))
	return cells


static func _cell_features(state: GameState, actor_id: String, q: int, r: int) -> Dictionary:
	var coord := HexCoord.new(q, r)
	var tile: HexTile = state.map.get_tile(coord) if state.map.in_bounds(coord) else null
	var terrain_idx: int = _terrain_index(tile)
	var owner_relation: int = _owner_relation(state, actor_id, q, r)
	var visibility: int = 1
	var resource_amount: int = 0
	if tile != null:
		resource_amount = int(tile.tree_density)
		if tile.deposit_type != "":
			resource_amount = maxi(resource_amount, int(tile.deposit_amount))

	var road_present: int = _road_present(state, q, r)
	var building_info: Dictionary = _building_at(state, q, r)
	var unit_info: Dictionary = _unit_at(state, q, r)

	return {
		"q": q,
		"r": r,
		"terrain_type": terrain_idx,
		"owner_relation": owner_relation,
		"visibility": visibility,
		"resource_amount": resource_amount,
		"road_present": road_present,
		"has_building": 1 if not building_info.is_empty() else 0,
		"building_type": int(building_info.get("type_idx", 0)),
		"building_hp_progress": int(building_info.get("hp_progress", 0)),
		"has_unit": 1 if not unit_info.is_empty() else 0,
		"unit_type": int(unit_info.get("type_idx", 0)),
		"unit_hp_count": int(unit_info.get("hp_count", 0)),
	}


static func _terrain_index(tile: HexTile) -> int:
	if tile == null:
		return 0
	match str(tile.terrain_type):
		"grass":
			return 1
		"forest":
			return 2
		"mountain":
			return 3
		"water":
			return 4
		_:
			return 0


static func _owner_relation(state: GameState, actor_id: String, q: int, r: int) -> int:
	# 0=neutral 1=self 2=enemy 3=ally
	for building in _all_buildings(state):
		if int(building.get("q", building.get("anchor_q", -99))) != q:
			continue
		if int(building.get("r", building.get("anchor_r", -99))) != r:
			continue
		var owner: String = str(building.get("player_id", ""))
		if owner == actor_id:
			return 1
		return 2
	for unit in _all_units(state):
		if int(unit.get("q", -99)) != q or int(unit.get("r", -99)) != r:
			continue
		var owner_u: String = str(unit.get("player_id", unit.get("owner", "")))
		if owner_u == actor_id:
			return 1
		return 2
	return 0


static func _road_present(state: GameState, q: int, r: int) -> int:
	if state.variant_id == "economy_trial":
		for node in EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES).values():
			if int(node.get("q", -1)) == q and int(node.get("r", -1)) == r:
				return 1
	return 0


static func _building_at(state: GameState, q: int, r: int) -> Dictionary:
	for building in _all_buildings(state):
		var bq: int = int(building.get("q", building.get("anchor_q", -99)))
		var br: int = int(building.get("r", building.get("anchor_r", -99)))
		if bq != q or br != r:
			continue
		var hp: int = int(building.get("hp", 100))
		var max_hp: int = maxi(1, int(building.get("max_hp", 100)))
		return {
			"type_idx": _building_type_index(str(building.get("building_type", ""))),
			"hp_progress": (hp * 1000) / max_hp,
		}
	return {}


static func _unit_at(state: GameState, q: int, r: int) -> Dictionary:
	for unit in _all_units(state):
		if int(unit.get("q", -99)) != q or int(unit.get("r", -99)) != r:
			continue
		var hp: int = int(unit.get("hp", unit.get("health", 1)))
		return {
			"type_idx": _unit_type_index(str(unit.get("unit_type", unit.get("type", "")))),
			"hp_count": hp,
		}
	return {}


static func _all_buildings(state: GameState) -> Array:
	if state.variant_id == "economy_trial":
		return EconomyState.all_buildings(state)
	if state.variant_id == "military_trial":
		return MilitaryState.all_buildings(state)
	return []


static func _all_units(state: GameState) -> Array:
	if state.variant_id == "military_trial":
		return MilitaryState.all_units(state)
	return []


static func _building_type_index(building_type: String) -> int:
	var known: Array[String] = [
		"", "headquarters", "storehouse", "woodcutter", "sawmill", "workshop",
		"command_hub", "power_generator", "resource_processor", "barracks",
	]
	var idx: int = known.find(building_type)
	return maxi(0, idx)


static func _unit_type_index(unit_type: String) -> int:
	var known: Array[String] = [
		"", "rifle_squad", "rocket_squad", "scout", "engineer",
	]
	var idx: int = known.find(unit_type)
	return maxi(0, idx)


static func _export_player_vector(state: GameState, actor_id: String) -> Array:
	var wood: int = 0
	var stone: int = 0
	var food: int = 0
	var buildings: int = 0
	var units: int = 0
	var idle_workers: int = 0
	var enemy_units: int = 0

	if state.variant_id == "economy_trial":
		for building in EconomyState.all_buildings(state):
			if str(building.get("player_id", "")) != actor_id:
				continue
			buildings += 1
			var store_id: int = int(building.get("store_id", 0))
			if store_id > 0:
				var store: Dictionary = EconomyState.get_ware_store(state, store_id)
				var amounts: Dictionary = store.get("wares", {})
				wood += int(amounts.get("wood", amounts.get("planks", 0)))
				stone += int(amounts.get("stone", 0))
				food += int(amounts.get("rations", amounts.get("food", amounts.get("meals", 0))))
		for worker in EconomyState.all_workers(state):
			if str(worker.get("player_id", "")) == actor_id:
				if str(worker.get("state", "")) == EconomyConstants.WORKER_IDLE:
					idle_workers += 1
	elif state.variant_id == "military_trial":
		for building in MilitaryState.all_buildings(state):
			if str(building.get("player_id", "")) == actor_id:
				buildings += 1
		for unit in MilitaryState.all_units(state):
			var owner: String = str(unit.get("player_id", ""))
			if owner == actor_id:
				units += 1
			else:
				enemy_units += 1

	return [
		_normalize(wood, 100),
		_normalize(stone, 100),
		_normalize(food, 100),
		_normalize(idle_workers + units, 50),
		_normalize(idle_workers, 20),
		_normalize(buildings, 20),
		_normalize(units, 50),
		0.0,
		_normalize(enemy_units, 50),
	]


static func _export_summary(state: GameState, actor_id: String) -> Array:
	var own_units: int = 0
	var enemy_units: int = 0
	var own_buildings: int = 0
	var enemy_buildings: int = 0
	for unit in _all_units(state):
		if str(unit.get("player_id", unit.get("owner", ""))) == actor_id:
			own_units += 1
		else:
			enemy_units += 1
	for building in _all_buildings(state):
		if str(building.get("player_id", "")) == actor_id:
			own_buildings += 1
		else:
			enemy_buildings += 1
	var progress: float = 0.0
	if state.variant_id == "military_trial":
		progress = 1.0 if int(MilitaryState.get_meta_value(state, "victory_decided", 0)) == 1 else 0.0
	return [
		_normalize(state.tick, 1000),
		float(own_units),
		float(enemy_units),
		float(own_buildings),
		float(enemy_buildings),
		progress,
	]


static func _normalize(value: int, scale: int) -> float:
	if scale <= 0:
		return 0.0
	return clampf(float(value) / float(scale), 0.0, 1.0)
