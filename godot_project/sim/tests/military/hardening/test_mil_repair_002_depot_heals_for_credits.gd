extends "res://sim/tests/test_case.gd"

## MIL-REPAIR-002: a powered repair depot heals a damaged unit for credits.


func get_name() -> String:
	return "test_MIL_REPAIR_002_depot_heals_for_credits"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 61,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [
					{"type": "repair_depot", "q": 8, "r": 8},
					{"type": "power_generator", "q": 2, "r": 8},
				],
				"units": [{"type": "rifle_squad", "q": 9, "r": 8}],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	var unit_id: int = _first_unit_of_type(state, "rifle_squad")
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	unit["hp"] = 40
	var credits_before: int = MilitaryState.player_credits(state, "p1")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, RepairTargetCommand.new("p1", 0, {"target_id": unit_id}))
	assert_true(res.success, "repair accepted")

	state = runner.run_ticks(state, 30)
	var healed: Dictionary = MilitaryState.get_unit(state, unit_id)
	assert_eq(int(healed.get("hp", 0)), int(healed.get("max_hp", 0)), "unit fully repaired")
	assert_true(MilitaryState.player_credits(state, "p1") < credits_before, "repair spent credits")

	var saw_completed: bool = false
	for event in state.event_log.events:
		if event.type == "repair_completed":
			saw_completed = true
	assert_true(saw_completed, "repair_completed event emitted")


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
