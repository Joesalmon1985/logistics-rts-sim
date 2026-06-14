class_name MilitaryProductionSystem
extends RefCounted

## Per-building unit production queues. Items are paid for at queue time; ticks to
## complete; spawns the unit at the building rally point. Blocked when unpowered.


static func tick(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var per_tick: int = cfg.global_int("production_progress_per_tick", 1)
	for queue in MilitaryState.all_production_queues(state):
		var items: Array = queue.get("items", [])
		if items.is_empty():
			continue
		var building: Dictionary = MilitaryState.get_building(state, int(queue.get("building_id", 0)))
		if building.is_empty() or str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		if not MilitaryPowerSystem.building_powered(state, building):
			queue["blocked_reason"] = MilitaryConstants.ERR_UNPOWERED
			continue
		queue["blocked_reason"] = ""
		var item: Dictionary = items[0]
		item["progress"] = int(item.get("progress", 0)) + per_tick
		if int(item.get("progress", 0)) >= int(item.get("required_ticks", 1)):
			items.pop_front()
			_spawn_unit(state, building, str(item.get("unit_type", "")), cfg)


static func _spawn_unit(state: GameState, building: Dictionary, unit_type: String, cfg: MilitaryConfig) -> void:
	var spawn: Vector2i = _spawn_hex(state, building, cfg)
	var unit_id: int = create_unit(state, unit_type, spawn.x, spawn.y, str(building.get("player_id", "")), cfg)
	MilitaryState.emit_event(state, "production_completed", {
		"building_id": int(building.get("id", 0)),
		"unit_id": unit_id,
		"unit_type": unit_type,
	})
	# If the building has a rally point, order the unit to move there.
	var rally_q: int = int(building.get("rally_q", -1))
	var rally_r: int = int(building.get("rally_r", -1))
	if rally_q >= 0 and rally_r >= 0:
		var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
		MilitaryOrderSystem.issue_move(state, unit, rally_q, rally_r)


static func create_unit(state: GameState, unit_type: String, q: int, r: int, player_id: String, cfg: MilitaryConfig) -> int:
	var udef: Dictionary = cfg.get_unit_def(unit_type)
	var unit_id: int = state.entity_ids.allocate()
	var unit := {
		"id": unit_id,
		"unit_type": unit_type,
		"category": str(udef.get("category", "infantry")),
		"player_id": player_id,
		"q": q,
		"r": r,
		"hp": int(udef.get("hp", 100)),
		"max_hp": int(udef.get("hp", 100)),
		"armour_type": str(udef.get("armour_type", "infantry")),
		"sight": int(udef.get("sight", 4)),
		"speed": int(udef.get("speed", 50)),
		"weapon": str(udef.get("weapon", "")),
		"weapon_cooldown": 0,
		"move_progress": 0,
		"path": [],
		"path_index": 0,
		"order": {"type": MilitaryConstants.ORDER_NONE},
		"state": MilitaryConstants.UNIT_IDLE,
		"cargo": 0,
		"vet_level": 0,
	}
	MilitaryState.add_unit(state, unit)
	MilitaryState.emit_event(state, "unit_spawned", {
		"unit_id": unit_id,
		"unit_type": unit_type,
		"player_id": player_id,
		"q": q,
		"r": r,
	})
	return unit_id


static func _spawn_hex(state: GameState, building: Dictionary, cfg: MilitaryConfig) -> Vector2i:
	var coords: Array = MilitaryMapRules.footprint_coords(str(building.get("building_type", "")), int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)), cfg)
	var occupied: Dictionary = MilitaryMapRules.footprint_blocked_hexes(state)
	for c in coords:
		for n in HexCoord.new(c.x, c.y).neighbors():
			if MilitaryMapRules.is_passable(state, n.q, n.r) and not occupied.has(MilitaryState.coord_key(n.q, n.r)):
				return Vector2i(n.q, n.r)
	# Fallback: anchor neighbour even if occupied-ish.
	for n in HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0))).neighbors():
		if MilitaryMapRules.is_passable(state, n.q, n.r):
			return Vector2i(n.q, n.r)
	return Vector2i(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))


static func can_queue(state: GameState, building: Dictionary, unit_type: String) -> Dictionary:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var udef: Dictionary = cfg.get_unit_def(unit_type)
	if udef.is_empty():
		return {"ok": false, "reason": MilitaryConstants.ERR_UNKNOWN_UNIT}
	var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
	if unit_type not in bdef.get("produces", []):
		return {"ok": false, "reason": MilitaryConstants.ERR_NOT_PRODUCER}
	if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
		return {"ok": false, "reason": MilitaryConstants.ERR_UNPOWERED}
	if not MilitaryPowerSystem.building_powered(state, building):
		return {"ok": false, "reason": MilitaryConstants.ERR_UNPOWERED}
	if not MilitaryMapRules.prerequisites_met(state, str(building.get("player_id", "")), udef):
		return {"ok": false, "reason": MilitaryConstants.ERR_MISSING_PREREQUISITE}
	return {"ok": true, "reason": ""}
