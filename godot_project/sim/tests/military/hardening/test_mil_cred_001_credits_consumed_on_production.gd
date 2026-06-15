extends "res://sim/tests/test_case.gd"

## MIL-CRED-001: trial-mode production spends player credits when queuing units.


func get_name() -> String:
	return "test_MIL_CRED_001_credits_consumed_on_production"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 4,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 50000,
		"fog_enabled": 0,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [
					{"type": "barracks", "q": 6, "r": 2},
					{"type": "power_generator", "q": 2, "r": 8},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var barracks_id: int = _building_of_type(state, "barracks")
	var credits_before: int = MilitaryState.player_credits(state, "p1")
	var cost: int = int(MilitaryState.get_config(state).get_unit_def("rifle_squad").get("credit_cost", 0))

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	assert_true(res.success, "production queue accepted")
	assert_eq(MilitaryState.player_credits(state, "p1"), credits_before - cost, "credits consumed on queue")

	state = runner.run_ticks(state, 25)
	assert_true(_count_units_of_type(state, "rifle_squad") >= 1, "unit produced after credit spend")

	var queue: Dictionary = MilitaryState.get_production_queue(state, barracks_id)
	assert_eq(queue.get("items", []).size(), 0, "queue emptied after production completed")


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0


func _count_units_of_type(state: GameState, unit_type: String) -> int:
	var n: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			n += 1
	return n
