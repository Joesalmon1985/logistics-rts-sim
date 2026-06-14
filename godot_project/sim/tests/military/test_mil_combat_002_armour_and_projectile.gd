extends "res://sim/tests/test_case.gd"

## MIL-COMBAT-002/003: a projectile weapon spawns a projectile entity and applies
## damage only when travel completes; the damage is scaled by the armour/damage-type
## modifier (explosive vs vehicle = 130%).


func get_name() -> String:
	return "test_MIL_COMBAT_002_armour_and_projectile"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 13,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rocket_squad", "q": 5, "r": 5}]},
			{"player_id": "p2", "q": 14, "r": 14, "units": [{"type": "collector", "q": 7, "r": 5}]},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var attacker_id: int = _first_unit_of(state, "p1", "rocket_squad")
	var target_id: int = _first_unit_of(state, "p2", "collector")
	assert_true(attacker_id > 0 and target_id > 0, "attacker and target spawned")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": attacker_id, "target_id": target_id}))

	# Tick 1: rocket fires, projectile spawned, no damage yet (travel_ticks = 2).
	state = runner.run_ticks(state, 1)
	assert_true(MilitaryState.all_projectiles(state).size() >= 1, "projectile spawned")
	assert_eq(int(MilitaryState.get_unit(state, target_id).get("hp", -1)), 150, "no damage before travel completes")

	# By tick 3 the projectile has landed: explosive(40) vs vehicle(130%) = 52.
	state = runner.run_ticks(state, 2)
	assert_eq(int(MilitaryState.get_unit(state, target_id).get("hp", -1)), 98, "projectile damage applied with armour modifier (150-52)")

	var saw_hit: bool = false
	for event in state.event_log.events:
		if event.type == "projectile_hit":
			saw_hit = true
	assert_true(saw_hit, "projectile_hit emitted")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
