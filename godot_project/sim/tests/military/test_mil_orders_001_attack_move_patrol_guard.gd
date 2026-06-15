extends "res://sim/tests/test_case.gd"

## MIL-ORDERS-001/002/003: attack-move engages a hostile encountered en route;
## patrol keeps a unit moving between waypoints; guard engages an intruder near the
## guarded anchor.


func get_name() -> String:
	return "test_MIL_ORDERS_001_attack_move_patrol_guard"


func _run_body() -> void:
	_check_attack_move()
	_check_patrol()
	_check_guard()


func _check_attack_move() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 21,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "light_vehicle", "q": 3, "r": 5}]},
			{"player_id": "p2", "q": 14, "r": 14, "units": [{"type": "scout", "q": 8, "r": 5}]},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var unit_id: int = _first_unit_of(state, "p1", "light_vehicle")
	var enemy_id: int = _first_unit_of(state, "p2", "scout")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, AttackMoveCommand.new("p1", 0, {"unit_id": unit_id, "q": 14, "r": 5}))
	state = runner.run_ticks(state, 80)
	assert_true(MilitaryState.get_unit(state, enemy_id).is_empty(), "attack-move engaged and destroyed hostile en route")


func _check_patrol() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 22,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "scout", "q": 3, "r": 3}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var unit_id: int = _first_unit_of(state, "p1", "scout")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, PatrolCommand.new("p1", 0, {
		"unit_id": unit_id,
		"waypoints": [{"q": 3, "r": 3}, {"q": 10, "r": 3}],
	}))
	assert_true(res.success, "patrol accepted")
	state = runner.run_ticks(state, 60)
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	assert_eq(str(unit.get("order", {}).get("type", "")), MilitaryConstants.ORDER_PATROL, "patrol order persists")
	assert_true(int(unit.get("q", 3)) != 3 or int(unit.get("r", 3)) != 3, "patrol drove movement away from start")


func _check_guard() -> void:
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
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, GuardCommand.new("p1", 0, {"unit_id": unit_id, "q": 5, "r": 5}))
	assert_true(res.success, "guard accepted")
	state = runner.run_ticks(state, 40)
	assert_true(MilitaryState.get_unit(state, enemy_id).is_empty(), "guard engaged nearby intruder")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
