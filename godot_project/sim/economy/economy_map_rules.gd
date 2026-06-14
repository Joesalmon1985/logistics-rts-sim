class_name EconomyMapRules
extends RefCounted

## Terrain, slope, buildability, ownership, and footprint validation.


static func slope_between(state: GameState, coord_a: HexCoord, coord_b: HexCoord) -> int:
	var tile_a: HexTile = state.map.get_tile(coord_a)
	var tile_b: HexTile = state.map.get_tile(coord_b)
	if tile_a == null or tile_b == null:
		return 99
	return absi(tile_a.height_level - tile_b.height_level)


static func get_footprint_coords(anchor_q: int, anchor_r: int, footprint: Array) -> Array[HexCoord]:
	var coords: Array[HexCoord] = []
	for offset in footprint:
		var dq: int = int(offset[0])
		var dr: int = int(offset[1])
		coords.append(HexCoord.new(anchor_q + dq, anchor_r + dr))
	return coords


static func footprint_overlaps_existing(
	state: GameState,
	anchor_q: int,
	anchor_r: int,
	footprint: Array,
	exclude_building_id: int = 0,
) -> bool:
	for coord in get_footprint_coords(anchor_q, anchor_r, footprint):
		if _hex_has_building_or_site(state, coord, exclude_building_id):
			return true
	return false


static func _hex_has_building_or_site(
	state: GameState,
	coord: HexCoord,
	exclude_building_id: int = 0,
) -> bool:
	for building in EconomyState.all_buildings(state):
		if exclude_building_id > 0 and int(building.get("id", 0)) == exclude_building_id:
			continue
		var bdef: Dictionary = EconomyState.get_config(state).get_building_def(str(building.get("building_type", "")))
		for fc in get_footprint_coords(
			int(building.get("anchor_q", 0)),
			int(building.get("anchor_r", 0)),
			bdef.get("footprint", [[0, 0]]),
		):
			if fc.q == coord.q and fc.r == coord.r:
				return true
	for site in EconomyState.all_construction_sites(state):
		var site_state: String = str(site.get("state", ""))
		if site_state in [EconomyConstants.CONS_COMPLETED, EconomyConstants.CONS_CANCELLED]:
			continue
		var sdef: Dictionary = EconomyState.get_config(state).get_building_def(str(site.get("building_type", "")))
		for fc in get_footprint_coords(
			int(site.get("anchor_q", 0)),
			int(site.get("anchor_r", 0)),
			sdef.get("footprint", [[0, 0]]),
		):
			if fc.q == coord.q and fc.r == coord.r:
				return true
	return false


static func hex_distance(a_q: int, a_r: int, b_q: int, b_r: int) -> int:
	return HexCoord.new(a_q, a_r).distance(HexCoord.new(b_q, b_r))


static func within_build_radius(state: GameState, q: int, r: int, player_id: String) -> bool:
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
	var radius: int = int(EconomyState.get_meta_value(state, "build_radius", 8))
	return hex_distance(q, r, hq_q, hq_r) <= radius


static func deposit_satisfied(tile: HexTile, required_deposit: String, cfg: EconomyConfig) -> bool:
	if required_deposit == "":
		return true
	if required_deposit == "forest":
		return tile.tree_density >= cfg.int_val("tree_density_min_for_timber", 20)
	if required_deposit == "water_source":
		return tile.has_water_source > 0 or tile.deposit_type == "water_source"
	if required_deposit == "fertile":
		return tile.fertility > 0 or tile.deposit_type == "fertile"
	if tile.deposit_type != required_deposit:
		return false
	if tile.deposit_revealed <= 0:
		return false
	return tile.deposit_amount > 0


static func can_place_building(
	state: GameState,
	building_type: String,
	anchor_q: int,
	anchor_r: int,
	player_id: String,
	exclude_building_id: int = 0,
) -> Dictionary:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(building_type)
	if bdef.is_empty():
		return {"ok": false, "reason": "UNKNOWN_BUILDING"}

	var footprint: Array = bdef.get("footprint", [[0, 0]])
	if footprint_overlaps_existing(state, anchor_q, anchor_r, footprint, exclude_building_id):
		return {"ok": false, "reason": "FOOTPRINT_OVERLAP"}

	var required_terrain: String = str(bdef.get("required_terrain", ""))
	var required_deposit: String = str(bdef.get("required_deposit", ""))
	var max_slope_build: int = cfg.int_val("max_slope_for_build", 1)

	for coord in get_footprint_coords(anchor_q, anchor_r, footprint):
		if not state.map.in_bounds(coord):
			return {"ok": false, "reason": "OUT_OF_BOUNDS"}
		if not within_build_radius(state, coord.q, coord.r, player_id):
			return {"ok": false, "reason": "OUTSIDE_BUILD_RADIUS"}
		var tile: HexTile = state.map.get_tile(coord)
		if tile == null:
			return {"ok": false, "reason": "MISSING_TILE"}
		if tile.buildability <= 0:
			var deposit_ok: bool = required_deposit != "" and deposit_satisfied(tile, required_deposit, cfg)
			if not deposit_ok:
				return {"ok": false, "reason": "NOT_BUILDABLE"}
		if required_terrain != "" and tile.terrain_type != required_terrain:
			if not (required_terrain == "grass" and tile.terrain_type == "fertile"):
				return {"ok": false, "reason": "TERRAIN_MISMATCH"}
		if not deposit_satisfied(tile, required_deposit, cfg):
			return {"ok": false, "reason": "DEPOSIT_REQUIRED"}
		for neighbour in coord.neighbors():
			if not state.map.in_bounds(neighbour):
				continue
			var ns: int = slope_between(state, coord, neighbour)
			if ns > max_slope_build:
				return {"ok": false, "reason": "SLOPE_TOO_STEEP"}

	return {"ok": true, "reason": ""}


static func can_place_road(
	state: GameState,
	hex_a: HexCoord,
	hex_b: HexCoord,
	player_id: String,
) -> Dictionary:
	if hex_a.distance(hex_b) != 1:
		return {"ok": false, "reason": "NOT_ADJACENT"}
	if not state.map.in_bounds(hex_a) or not state.map.in_bounds(hex_b):
		return {"ok": false, "reason": "OUT_OF_BOUNDS"}

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var max_slope_road: int = cfg.int_val("max_slope_for_road", 2)
	var slope: int = slope_between(state, hex_a, hex_b)
	if slope > max_slope_road:
		return {"ok": false, "reason": "SLOPE_TOO_STEEP"}

	for coord in [hex_a, hex_b]:
		var tile: HexTile = state.map.get_tile(coord)
		if tile == null:
			return {"ok": false, "reason": "MISSING_TILE"}
		var tdef: Dictionary = cfg.get_terrain_def(tile.terrain_type)
		if int(tdef.get("road_legal", 0)) <= 0:
			return {"ok": false, "reason": "TERRAIN_BLOCKED"}

	if EconomyRoadSystem.has_road_between(state, hex_a, hex_b):
		return {"ok": false, "reason": "ROAD_EXISTS"}

	return {"ok": true, "reason": ""}


static func movement_cost(state: GameState, from_coord: HexCoord, to_coord: HexCoord) -> int:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var tile: HexTile = state.map.get_tile(to_coord)
	if tile == null:
		return 99
	var tdef: Dictionary = cfg.get_terrain_def(tile.terrain_type)
	var base_cost: int = int(tdef.get("movement_cost", 1))
	var slope: int = slope_between(state, from_coord, to_coord)
	if slope > 0:
		base_cost *= cfg.int_val("slope_path_cost_multiplier", 2)
	return base_cost


static func apply_hex_metadata_from_map(state: GameState, terrain_config: Dictionary) -> void:
	var terrain_defs: Dictionary = terrain_config.get("terrain_defs", {})
	for key in state.map.tiles:
		var tile: HexTile = state.map.tiles[key]
		var tdef: Dictionary = terrain_defs.get(tile.terrain_type, {})
		if tile.terrain_type == "forest" and tile.tree_density <= 0:
			tile.tree_density = int(tdef.get("default_tree_density", 80))
		if tile.terrain_type == "fertile" and tile.fertility <= 0:
			tile.fertility = int(tdef.get("default_fertility", 100))
		if tile.deposit_type == "forest" and tile.tree_density <= 0:
			tile.tree_density = int(tdef.get("default_tree_density", 80))


static func set_ownership_radius(state: GameState, center_q: int, center_r: int, radius: int, player_id: String) -> void:
	for r in range(state.map.height):
		for q in range(state.map.width):
			var coord := HexCoord.new(q, r)
			if hex_distance(q, r, center_q, center_r) <= radius:
				var tile: HexTile = state.map.get_tile(coord)
				if tile != null:
					tile.owner_player_id = player_id


static func sync_road_segment_refs(state: GameState) -> void:
	for key in state.map.tiles:
		var tile: HexTile = state.map.tiles[key]
		tile.road_segment_id = 0
	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	for road_key in roads:
		var road: Dictionary = roads[road_key]
		for coord in [
			HexCoord.new(int(road.get("hex_a_q", 0)), int(road.get("hex_a_r", 0))),
			HexCoord.new(int(road.get("hex_b_q", 0)), int(road.get("hex_b_r", 0))),
		]:
			var tile: HexTile = state.map.get_tile(coord)
			if tile != null:
				tile.road_segment_id = int(road.get("id", 0))
