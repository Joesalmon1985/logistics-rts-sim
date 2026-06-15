extends "res://sim/tests/test_case.gd"

## MIL-ORDER-005: guard engages nearby hostiles while staying near the anchor.


func get_name() -> String:
	return "test_MIL_ORDER_005_guard_stays_near_target"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 23,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rifle_squad", "q": 5, "r": 5}]},
			{"player_id": "p2", "q": 14, "r": 14, "units": [{"type": "scout", "q": 6, "r": 5}]},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var unit_id: int = _first_unit_of(state, "p1", "rifle_squad")
	var enemy_id: int = _first_unit_of(state, "p2", "scout")
	var guard_q: int = 5
	var guard_r: int = 5

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, GuardCommand.new("p1", 0, {"unit_id": unit_id, "q": guard_q, "r": guard_r}))
	assert_true(res.success, "guard accepted")

	state = runner.run_ticks(state, 40)
	assert_true(MilitaryState.get_unit(state, enemy_id).is_empty(), "guard destroyed nearby intruder")

	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	var dist: int = MilitaryMapRules.hex_distance(int(unit.get("q", 0)), int(unit.get("r", 0)), guard_q, guard_r)
	assert_true(dist <= 4, "guard remained near anchor")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
