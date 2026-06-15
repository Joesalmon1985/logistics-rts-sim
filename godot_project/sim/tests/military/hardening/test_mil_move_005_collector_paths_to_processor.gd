extends "res://sim/tests/test_case.gd"

## MIL-MOVE-005: collectors path to resource nodes and return to the processor.


func get_name() -> String:
	return "test_MIL_MOVE_005_collector_paths_to_processor"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 5,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 20,
		"starting_credits": 0,
		"fog_enabled": 0,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [{"type": "resource_processor", "q": 6, "r": 6}],
				"units": [{"type": "collector", "q": 8, "r": 8, "count": 1}],
			},
		],
		"resource_fields": [{"q": 10, "r": 10, "field_type": "mineral_field"}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var collector_id: int = _find_collector(state)
	var start_q: int = int(MilitaryState.get_unit(state, collector_id).get("q", 0))

	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, HarvestResourceCommand.new("p1", 0, {"unit_id": collector_id, "field_id": _find_field(state)}))
	state = runner.run_ticks(state, 40)

	var collector: Dictionary = MilitaryState.get_unit(state, collector_id)
	var moved: bool = int(collector.get("q", start_q)) != start_q or int(collector.get("r", 8)) != 8
	var state_name: String = str(collector.get("state", ""))
	assert_true(moved or state_name in [MilitaryConstants.UNIT_HARVESTING, MilitaryConstants.UNIT_RETURNING], "collector pathing toward field or processor")

	state = runner.run_ticks(state, 60)
	assert_true(MilitaryState.player_credits(state, "p1") > 0, "collector completed loop to processor")


func _find_collector(state: GameState) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == "collector":
			return int(unit.get("id", 0))
	return 0


func _find_field(state: GameState) -> int:
	for field in MilitaryState.all_resource_fields(state):
		return int(field.get("id", 0))
	return 0
