class_name MilitaryResourceSystem
extends RefCounted

## Resource fields deplete; collectors harvest adjacent fields, return cargo to a
## resource processor which converts it to credits (trial simplification).


static func tick(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	for unit in MilitaryState.all_units(state):
		if int(cfg.get_unit_def(str(unit.get("unit_type", ""))).get("can_harvest", 0)) <= 0:
			continue
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		_tick_collector(state, unit, cfg)


static func _tick_collector(state: GameState, unit: Dictionary, cfg: MilitaryConfig) -> void:
	var unit_state: String = str(unit.get("state", ""))
	var capacity: int = cfg.resource_int("collector_capacity", 50)

	if unit_state == MilitaryConstants.UNIT_HARVESTING:
		_progress_harvest(state, unit, cfg, capacity)
	elif unit_state == MilitaryConstants.UNIT_RETURNING:
		_progress_return(state, unit, cfg)
	# Movement toward fields/processors is handled by the movement system when the
	# unit has a harvest order; arrival transitions are handled there/here.


static func _progress_harvest(state: GameState, unit: Dictionary, cfg: MilitaryConfig, capacity: int) -> void:
	var field_id: int = int(unit.get("harvest_field_id", 0))
	var field: Dictionary = MilitaryState.get_resource_field(state, field_id)
	if field.is_empty() or int(field.get("amount", 0)) <= 0:
		# Field gone/depleted — go return whatever we have.
		_begin_return(state, unit)
		return
	# Must be adjacent to (or on) the field to harvest.
	if MilitaryMapRules.hex_distance(int(unit.get("q", 0)), int(unit.get("r", 0)), int(field.get("q", 0)), int(field.get("r", 0))) > 1:
		return
	var progress: int = int(unit.get("harvest_progress", 0)) + 1
	unit["harvest_progress"] = progress
	if progress < cfg.resource_int("harvest_ticks", 5):
		return
	unit["harvest_progress"] = 0
	var yield_amt: int = mini(cfg.get_field_def(str(field.get("field_type", "mineral_field"))).get("yield_per_harvest", 50), int(field.get("amount", 0)))
	var deplete: int = mini(int(field.get("amount", 0)), yield_amt)
	field["amount"] = int(field.get("amount", 0)) - deplete
	unit["cargo"] = mini(capacity, int(unit.get("cargo", 0)) + yield_amt)
	MilitaryState.emit_event(state, "resource_harvested", {
		"unit_id": int(unit.get("id", 0)),
		"field_id": field_id,
		"amount": yield_amt,
		"field_remaining": int(field.get("amount", 0)),
	})
	if int(field.get("amount", 0)) <= 0:
		field["depleted"] = 1
		MilitaryState.emit_event(state, "node_depleted", {"field_id": field_id})
	if int(unit.get("cargo", 0)) >= capacity or int(field.get("amount", 0)) <= 0:
		_begin_return(state, unit)


static func _begin_return(state: GameState, unit: Dictionary) -> void:
	var processor: Dictionary = nearest_processor(state, str(unit.get("player_id", "")), int(unit.get("q", 0)), int(unit.get("r", 0)))
	if processor.is_empty():
		unit["state"] = MilitaryConstants.UNIT_IDLE
		return
	unit["state"] = MilitaryConstants.UNIT_RETURNING
	unit["return_building_id"] = int(processor.get("id", 0))
	var dest: Vector2i = _adjacent_target(state, processor)
	var path: Array = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), dest.x, dest.y)
	unit["path"] = path
	unit["path_index"] = 0
	unit["move_progress"] = 0


static func _progress_return(state: GameState, unit: Dictionary, cfg: MilitaryConfig) -> void:
	var processor: Dictionary = MilitaryState.get_building(state, int(unit.get("return_building_id", 0)))
	if processor.is_empty() or str(processor.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
		processor = nearest_processor(state, str(unit.get("player_id", "")), int(unit.get("q", 0)), int(unit.get("r", 0)))
		if processor.is_empty():
			unit["state"] = MilitaryConstants.UNIT_IDLE
			return
		unit["return_building_id"] = int(processor.get("id", 0))
	# Adjacent to processor? Unload.
	if _adjacent_to_building(unit, processor, cfg):
		_unload(state, unit, processor, cfg)


static func _unload(state: GameState, unit: Dictionary, processor: Dictionary, cfg: MilitaryConfig) -> void:
	var cargo: int = int(unit.get("cargo", 0))
	if cargo <= 0:
		_resume_harvest(state, unit)
		return
	var credits: int = cargo * cfg.resource_int("credits_per_cargo_unit", 1)
	MilitaryState.add_credits(state, str(unit.get("player_id", "")), credits)
	unit["cargo"] = 0
	MilitaryState.emit_event(state, "resource_delivered", {
		"unit_id": int(unit.get("id", 0)),
		"building_id": int(processor.get("id", 0)),
		"credits": credits,
	})
	_resume_harvest(state, unit)


static func _resume_harvest(state: GameState, unit: Dictionary) -> void:
	var field_id: int = int(unit.get("harvest_field_id", 0))
	var field: Dictionary = MilitaryState.get_resource_field(state, field_id)
	if field.is_empty() or int(field.get("amount", 0)) <= 0:
		field = nearest_field(state, int(unit.get("q", 0)), int(unit.get("r", 0)))
		if field.is_empty():
			unit["state"] = MilitaryConstants.UNIT_IDLE
			return
		unit["harvest_field_id"] = int(field.get("id", 0))
	assign_harvest(state, unit, int(unit.get("harvest_field_id", 0)))


static func assign_harvest(state: GameState, unit: Dictionary, field_id: int) -> void:
	var field: Dictionary = MilitaryState.get_resource_field(state, field_id)
	if field.is_empty():
		return
	unit["harvest_field_id"] = field_id
	unit["order"] = {"type": MilitaryConstants.ORDER_HARVEST, "field_id": field_id}
	# If already adjacent, harvest; else move adjacent.
	if MilitaryMapRules.hex_distance(int(unit.get("q", 0)), int(unit.get("r", 0)), int(field.get("q", 0)), int(field.get("r", 0))) <= 1:
		unit["state"] = MilitaryConstants.UNIT_HARVESTING
		unit["path"] = []
		unit["path_index"] = 0
	else:
		unit["state"] = MilitaryConstants.UNIT_HARVESTING
		var dest := _adjacent_field_target(state, field)
		var path: Array = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), dest.x, dest.y)
		unit["path"] = path
		unit["path_index"] = 0
		unit["move_progress"] = 0


static func nearest_processor(state: GameState, player_id: String, q: int, r: int) -> Dictionary:
	var best: Dictionary = {}
	var best_dist: int = 1 << 30
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != "resource_processor":
			continue
		if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		var d: int = MilitaryMapRules.hex_distance(q, r, int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
		if d < best_dist:
			best_dist = d
			best = building
	return best


static func nearest_field(state: GameState, q: int, r: int) -> Dictionary:
	var best: Dictionary = {}
	var best_dist: int = 1 << 30
	for field in MilitaryState.all_resource_fields(state):
		if int(field.get("amount", 0)) <= 0:
			continue
		var d: int = MilitaryMapRules.hex_distance(q, r, int(field.get("q", 0)), int(field.get("r", 0)))
		if d < best_dist:
			best_dist = d
			best = field
	return best


static func _adjacent_to_building(unit: Dictionary, building: Dictionary, cfg: MilitaryConfig) -> bool:
	var coords: Array = MilitaryMapRules.footprint_coords(str(building.get("building_type", "")), int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)), cfg)
	for c in coords:
		if MilitaryMapRules.hex_distance(int(unit.get("q", 0)), int(unit.get("r", 0)), c.x, c.y) <= 1:
			return true
	return false


static func _adjacent_target(state: GameState, building: Dictionary) -> Vector2i:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var coords: Array = MilitaryMapRules.footprint_coords(str(building.get("building_type", "")), int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)), cfg)
	for c in coords:
		for n in HexCoord.new(c.x, c.y).neighbors():
			if MilitaryMapRules.is_passable(state, n.q, n.r):
				return Vector2i(n.q, n.r)
	return Vector2i(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))


static func _adjacent_field_target(state: GameState, field: Dictionary) -> Vector2i:
	var fq: int = int(field.get("q", 0))
	var fr: int = int(field.get("r", 0))
	if MilitaryMapRules.is_passable(state, fq, fr):
		return Vector2i(fq, fr)
	for n in HexCoord.new(fq, fr).neighbors():
		if MilitaryMapRules.is_passable(state, n.q, n.r):
			return Vector2i(n.q, n.r)
	return Vector2i(fq, fr)
