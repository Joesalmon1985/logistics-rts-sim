class_name MilitaryMapRules
extends RefCounted

## Placement, build radius, footprint, passability and movement cost — all derived
## from the shared RUN_01 axial hex map (DDR-029). No parallel grid.


static func hex_distance(a_q: int, a_r: int, b_q: int, b_r: int) -> int:
	return HexCoord.new(a_q, a_r).distance(HexCoord.new(b_q, b_r))


static func is_passable(state: GameState, q: int, r: int) -> bool:
	var coord := HexCoord.new(q, r)
	if not state.map.in_bounds(coord):
		return false
	var tile: HexTile = state.map.get_tile(coord)
	if tile == null:
		return false
	return tile.passability > 0


static func movement_cost(state: GameState, to_q: int, to_r: int) -> int:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var coord := HexCoord.new(to_q, to_r)
	var tile: HexTile = state.map.get_tile(coord)
	if tile == null or tile.passability <= 0:
		return 9999
	return cfg.terrain_movement_cost(tile.terrain_type) * cfg.global_int("move_cost_scale", 100)


static func footprint_coords(building_type: String, anchor_q: int, anchor_r: int, cfg: MilitaryConfig) -> Array:
	var bdef: Dictionary = cfg.get_building_def(building_type)
	return MilitaryState.footprint_coords(anchor_q, anchor_r, bdef.get("footprint", [[0, 0]]))


static func footprint_blocked_hexes(state: GameState) -> Dictionary:
	## Returns map of "q,r" -> true for every hex occupied by an active building or
	## a non-terminal construction site. Excludes completed/cancelled sites
	## (RUN_02 lesson) and destroyed buildings.
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var blocked: Dictionary = {}
	for building in MilitaryState.all_buildings(state):
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			continue
		for c in footprint_coords(str(building.get("building_type", "")), int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)), cfg):
			blocked[MilitaryState.coord_key(c.x, c.y)] = true
	for site in MilitaryState.all_construction_sites(state):
		if str(site.get("state", "")) in [MilitaryConstants.CONS_COMPLETED, MilitaryConstants.CONS_CANCELLED]:
			continue
		for c in footprint_coords(str(site.get("building_type", "")), int(site.get("anchor_q", 0)), int(site.get("anchor_r", 0)), cfg):
			blocked[MilitaryState.coord_key(c.x, c.y)] = true
	return blocked


static func within_build_radius(state: GameState, q: int, r: int, player_id: String) -> bool:
	var radius: int = int(MilitaryState.get_meta_value(state, "build_radius", 10))
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		var cfg: MilitaryConfig = MilitaryState.get_config(state)
		var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
		if int(bdef.get("is_build_radius_source", 0)) <= 0:
			continue
		if hex_distance(q, r, int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0))) <= radius:
			return true
	return false


static func can_place_building(
	state: GameState,
	building_type: String,
	anchor_q: int,
	anchor_r: int,
	player_id: String,
) -> Dictionary:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(building_type)
	if bdef.is_empty():
		return {"ok": false, "reason": MilitaryConstants.ERR_UNKNOWN_BUILDING}

	var blocked: Dictionary = footprint_blocked_hexes(state)
	var coords: Array = footprint_coords(building_type, anchor_q, anchor_r, cfg)
	var radius_ok: bool = false
	for c in coords:
		var coord := HexCoord.new(c.x, c.y)
		if not state.map.in_bounds(coord):
			return {"ok": false, "reason": MilitaryConstants.ERR_OUT_OF_BOUNDS}
		var tile: HexTile = state.map.get_tile(coord)
		if tile == null or tile.buildability <= 0:
			return {"ok": false, "reason": MilitaryConstants.ERR_NOT_BUILDABLE}
		if blocked.has(MilitaryState.coord_key(c.x, c.y)):
			return {"ok": false, "reason": MilitaryConstants.ERR_FOOTPRINT_OVERLAP}
		if within_build_radius(state, c.x, c.y, player_id):
			radius_ok = true

	# The command hub establishes its own radius; first/source buildings bypass.
	if int(bdef.get("is_command_hub", 0)) <= 0 and not radius_ok:
		# Allow placement only when at least one footprint hex is in radius.
		if not _player_has_radius_source(state, player_id):
			# No radius source yet: only the hub can be placed.
			return {"ok": false, "reason": MilitaryConstants.ERR_OUTSIDE_BUILD_RADIUS}
		return {"ok": false, "reason": MilitaryConstants.ERR_OUTSIDE_BUILD_RADIUS}

	# Prerequisites (buildings + tech).
	if not prerequisites_met(state, player_id, bdef):
		return {"ok": false, "reason": MilitaryConstants.ERR_MISSING_PREREQUISITE}

	return {"ok": true, "reason": ""}


static func _player_has_radius_source(state: GameState, player_id: String) -> bool:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
		if int(bdef.get("is_build_radius_source", 0)) > 0:
			return true
	return false


static func prerequisites_met(state: GameState, player_id: String, def: Dictionary) -> bool:
	for req_building in def.get("requires_building", []):
		if not _player_owns_active_building(state, player_id, str(req_building)):
			return false
	for req_tech in def.get("requires_tech", []):
		if not MilitaryState.player_has_tech(state, player_id, str(req_tech)):
			return false
	return true


static func _player_owns_active_building(state: GameState, player_id: String, building_type: String) -> bool:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != building_type:
			continue
		if str(building.get("state", "")) == MilitaryConstants.BLD_ACTIVE:
			return true
	return false
