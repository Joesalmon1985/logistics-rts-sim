extends "res://sim/tests/test_case.gd"

## MIL-COMBAT-003: weapon cooldown prevents refiring until elapsed.


func get_name() -> String:
	return "test_MIL_COMBAT_003_cooldown_blocks_refire"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 11,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rifle_squad", "q": 5, "r": 5}]},
			{"player_id": "p2", "q": 14, "r": 14, "units": [{"type": "scout", "q": 7, "r": 5}]},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var attacker_id: int = _first_unit_of(state, "p1", "rifle_squad")
	var target_id: int = _first_unit_of(state, "p2", "scout")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": attacker_id, "target_id": target_id}))
	state = runner.run_ticks(state, 1)
	var hp_after_first: int = int(MilitaryState.get_unit(state, target_id).get("hp", -1))
	assert_true(int(MilitaryState.get_unit(state, attacker_id).get("weapon_cooldown", 0)) > 0, "cooldown set after first shot")

	state = runner.run_ticks(state, 2)
	assert_eq(int(MilitaryState.get_unit(state, target_id).get("hp", -1)), hp_after_first, "cooldown blocked second hit")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
