extends "res://sim/tests/test_case.gd"

## MIL-COMBAT-001: an instant-hit weapon damages an in-range target, respects its
## cooldown (no second hit before cooldown elapses), and destroys the target when
## HP reaches zero, emitting the expected events.


func get_name() -> String:
	return "test_MIL_COMBAT_001_range_cooldown_damage"


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
	assert_true(attacker_id > 0 and target_id > 0, "attacker and target spawned")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": attacker_id, "target_id": target_id}))
	assert_true(res.success, "attack order accepted")

	# One tick: rifle (15 small_arms vs infantry = 100%) hits once.
	state = runner.run_ticks(state, 1)
	var target: Dictionary = MilitaryState.get_unit(state, target_id)
	assert_eq(int(target.get("hp", -1)), 45, "one hit applied (60-15)")
	var attacker: Dictionary = MilitaryState.get_unit(state, attacker_id)
	assert_true(int(attacker.get("weapon_cooldown", 0)) > 0, "cooldown set after firing")

	# Two more ticks must NOT produce another hit (cooldown is 4).
	state = runner.run_ticks(state, 2)
	target = MilitaryState.get_unit(state, target_id)
	assert_eq(int(target.get("hp", -1)), 45, "cooldown prevents immediate second hit")

	var saw_attacked: bool = false
	var saw_damaged: bool = false
	for event in state.event_log.events:
		if event.type == "unit_attacked":
			saw_attacked = true
		if event.type == "unit_damaged":
			saw_damaged = true
	assert_true(saw_attacked, "unit_attacked emitted")
	assert_true(saw_damaged, "unit_damaged emitted")

	# Run to destruction.
	state = runner.run_ticks(state, 40)
	assert_true(MilitaryState.get_unit(state, target_id).is_empty(), "target removed after destruction")
	var saw_destroyed: bool = false
	for event in state.event_log.events:
		if event.type == "unit_destroyed":
			saw_destroyed = true
	assert_true(saw_destroyed, "unit_destroyed emitted")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
