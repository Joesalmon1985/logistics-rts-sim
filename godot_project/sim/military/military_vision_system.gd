class_name MilitaryVisionSystem
extends RefCounted

## Per-player fog/shroud/vision. Three integer levels:
##   0 unexplored, 1 explored (shroud), 2 visible.
## Visible is recomputed each tick from post-movement unit/building sight; explored
## persists once seen. Runs after movement so reveals reflect this tick's moves.


static func tick(state: GameState) -> void:
	var fog_enabled: bool = int(MilitaryState.get_meta_value(state, "fog_enabled", 1)) > 0
	for player in MilitaryState.all_players(state):
		var player_id: String = str(player.get("player_id", ""))
		if not fog_enabled:
			_reveal_all(state, player_id)
			continue
		_recompute_player(state, player_id)


static func _recompute_player(state: GameState, player_id: String) -> void:
	var vision: Dictionary = MilitaryState.get_vision(state, player_id)
	var levels: Dictionary = vision.get("levels", {})

	# Demote previously-visible hexes to explored.
	for key in levels.keys():
		if int(levels[key]) == MilitaryConstants.INTEL_VISIBLE:
			levels[key] = MilitaryConstants.INTEL_EXPLORED

	# Reveal from each owned unit.
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) != player_id:
			continue
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		_reveal_around(state, levels, int(unit.get("q", 0)), int(unit.get("r", 0)), int(unit.get("sight", 3)))

	# Reveal from each owned active building.
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			continue
		var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
		var sight: int = int(building.get("sight", bdef.get("sight", 3)))
		# Radar requires power to extend sight.
		if str(building.get("building_type", "")) == "radar_tower" and not MilitaryPowerSystem.building_powered(state, building):
			sight = 1
		for c in MilitaryState.footprint_coords(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)), bdef.get("footprint", [[0, 0]])):
			_reveal_around(state, levels, c.x, c.y, sight)

	vision["levels"] = levels


static func _reveal_around(state: GameState, levels: Dictionary, cq: int, cr: int, sight: int) -> void:
	for dq in range(-sight, sight + 1):
		for dr in range(-sight, sight + 1):
			var q: int = cq + dq
			var r: int = cr + dr
			var coord := HexCoord.new(q, r)
			if not state.map.in_bounds(coord):
				continue
			if HexCoord.new(cq, cr).distance(coord) > sight:
				continue
			levels[MilitaryState.coord_key(q, r)] = MilitaryConstants.INTEL_VISIBLE


static func _reveal_all(state: GameState, player_id: String) -> void:
	var vision: Dictionary = MilitaryState.get_vision(state, player_id)
	var levels: Dictionary = {}
	for r in range(state.map.height):
		for q in range(state.map.width):
			if state.map.in_bounds(HexCoord.new(q, r)):
				levels[MilitaryState.coord_key(q, r)] = MilitaryConstants.INTEL_VISIBLE
	vision["levels"] = levels


static func is_visible(state: GameState, player_id: String, q: int, r: int) -> bool:
	return MilitaryState.intel_level(state, player_id, q, r) == MilitaryConstants.INTEL_VISIBLE
