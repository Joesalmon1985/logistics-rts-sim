extends "res://sim/tests/test_case.gd"

## MIL-DEFENCE-001 (MIL-COMBAT-007): a powered defence turret auto-acquires an
## enemy in range and destroys it without any explicit order.


func get_name() -> String:
	return "test_MIL_DEFENCE_001_turret_auto_targets"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 51,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 2, "r": 2, "buildings": [{"type": "defence_turret", "q": 8, "r": 8}]},
			{"player_id": "p2", "q": 14, "r": 14, "units": [{"type": "collector", "q": 10, "r": 8}]},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var enemy_id: int = _first_unit_of(state, "p2", "collector")
	assert_true(enemy_id > 0, "enemy spawned in turret range")
	assert_eq(int(MilitaryState.get_player(state, "p1").get("powered", 1)), 1, "p1 base powered")

	state = runner.run_ticks(state, 40)
	assert_true(MilitaryState.get_unit(state, enemy_id).is_empty(), "turret destroyed the intruder")

	var saw_attack: bool = false
	for event in state.event_log.events:
		if event.type == "unit_attacked" and str(event.payload.get("attacker_kind", "")) == "building":
			saw_attack = true
	assert_true(saw_attack, "turret recorded as building attacker")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
