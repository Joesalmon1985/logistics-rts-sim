extends "res://sim/tests/test_case.gd"

## MIL-ORDER-003: attack-move engages hostiles encountered en route.


func get_name() -> String:
	return "test_MIL_ORDER_003_attack_move_engages_en_route"


func _run_body() -> void:
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
	var res: CommandResult = validator.validate_and_apply(state, AttackMoveCommand.new("p1", 0, {"unit_id": unit_id, "q": 14, "r": 5}))
	assert_true(res.success, "attack-move accepted")

	state = runner.run_ticks(state, 80)
	assert_true(MilitaryState.get_unit(state, enemy_id).is_empty(), "attack-move destroyed hostile en route")

	var saw_attack: bool = false
	for event in state.event_log.events:
		if event.type == "unit_attacked":
			saw_attack = true
	assert_true(saw_attack, "unit_attacked event emitted")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
