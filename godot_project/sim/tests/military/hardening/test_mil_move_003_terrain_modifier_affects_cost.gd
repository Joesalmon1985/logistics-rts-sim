extends "res://sim/tests/test_case.gd"

## MIL-MOVE-003: terrain movement_cost modifiers slow travel through costly tiles.


func get_name() -> String:
	return "test_MIL_MOVE_003_terrain_modifier_affects_cost"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 7,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 2, "r": 2, "units": [{"type": "scout", "q": 3, "r": 3}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_wall_corridor(state, false)
	var grass_cost: int = MilitaryMapRules.movement_cost(state, 5, 3)
	state.map.set_tile(HexCoord.new(5, 3), HexTile.new("forest", 0, 1, 1))
	var forest_cost: int = MilitaryMapRules.movement_cost(state, 5, 3)
	assert_true(forest_cost > grass_cost, "forest tile costs more than grass")

	var unit_id: int = _first_unit_of_type(state, "scout")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, MoveUnitCommand.new("p1", 0, {"unit_id": unit_id, "q": 8, "r": 3}))
	var grass_ticks: int = _ticks_until_reached(runner, state, unit_id, 8, 3)

	var runner2 := SimulationTickRunner.new()
	var state2: GameState = runner2.create_game(scenario)
	_wall_corridor(state2, true)
	var unit2_id: int = _first_unit_of_type(state2, "scout")
	validator.validate_and_apply(state2, MoveUnitCommand.new("p1", 0, {"unit_id": unit2_id, "q": 8, "r": 3}))
	var forest_ticks: int = _ticks_until_reached(runner2, state2, unit2_id, 8, 3)

	assert_true(forest_ticks > grass_ticks, "forced forest corridor took more ticks than grass corridor")
	assert_eq(int(MilitaryState.get_unit(state2, unit2_id).get("q", -1)), 8, "forest corridor still arrives")


func _wall_corridor(state: GameState, use_forest: bool) -> void:
	for q in range(4, 8):
		state.map.set_tile(HexCoord.new(q, 2), HexTile.new("water", 0, 0, 0))
		state.map.set_tile(HexCoord.new(q, 4), HexTile.new("water", 0, 0, 0))
		if use_forest:
			state.map.set_tile(HexCoord.new(q, 3), HexTile.new("forest", 0, 1, 1))


func _ticks_until_reached(runner: SimulationTickRunner, state: GameState, unit_id: int, target_q: int, target_r: int) -> int:
	for i in range(1, 201):
		state = runner.run_ticks(state, 1)
		var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
		if int(unit.get("q", -1)) == target_q and int(unit.get("r", -1)) == target_r:
			return i
	return 201


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
