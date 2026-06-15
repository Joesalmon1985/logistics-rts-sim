extends "res://sim/tests/test_case.gd"

## MIL-VISION-003: scout units have extended sight compared to standard infantry.


func get_name() -> String:
	return "test_MIL_VISION_003_scout_bonus_sight"


func _run_body() -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(GameState.new())
	var scout_sight: int = int(cfg.get_unit_def("scout").get("sight", 0))
	var rifle_sight: int = int(cfg.get_unit_def("rifle_squad").get("sight", 0))
	assert_true(scout_sight > rifle_sight, "scout config sight exceeds rifle squad")

	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 44,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "scout", "q": 8, "r": 8}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	var scout: Dictionary = MilitaryState.get_unit(state, _first_unit_of_type(state, "scout"))
	var sq: int = int(scout.get("q", 8))
	var sr: int = int(scout.get("r", 8))
	var sight: int = int(scout.get("sight", 0))
	assert_true(sight > int(MilitaryState.get_config(state).get_unit_def("rifle_squad").get("sight", 4)), "scout sight exceeds rifle squad")

	assert_eq(MilitaryState.intel_level(state, "p1", sq, sr), MilitaryConstants.INTEL_VISIBLE, "scout hex visible")

	var found_at_radius: bool = false
	var found_beyond: bool = false
	for q in range(state.map.width):
		for r in range(state.map.height):
			var dist: int = HexCoord.new(sq, sr).distance(HexCoord.new(q, r))
			var level: int = MilitaryState.intel_level(state, "p1", q, r)
			if dist == sight and level == MilitaryConstants.INTEL_VISIBLE:
				found_at_radius = true
			if dist == sight + 1 and level != MilitaryConstants.INTEL_VISIBLE:
				found_beyond = true
	assert_true(found_at_radius, "hex at scout sight radius visible")
	assert_true(found_beyond, "hex beyond scout sight not visible")


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
