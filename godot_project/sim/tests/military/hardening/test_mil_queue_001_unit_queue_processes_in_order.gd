extends "res://sim/tests/test_case.gd"

## MIL-QUEUE-001: barracks process queued units in FIFO order.


func get_name() -> String:
	return "test_MIL_QUEUE_001_unit_queue_processes_in_order"


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
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "scout"}))
	validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))

	var scout_ticks: int = int(MilitaryState.get_config(state).get_unit_def("scout").get("build_ticks", 15))
	state = runner.run_ticks(state, scout_ticks + 2)
	assert_eq(_count_units_of_type(state, "scout"), 1, "first queued unit (scout) spawned")
	assert_eq(_count_units_of_type(state, "rifle_squad"), 0, "second unit not yet spawned")

	state = runner.run_ticks(state, 25)
	assert_true(_count_units_of_type(state, "rifle_squad") >= 1, "second queued unit spawned after first")


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
