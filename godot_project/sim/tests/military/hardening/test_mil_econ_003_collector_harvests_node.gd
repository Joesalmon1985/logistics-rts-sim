extends "res://sim/tests/test_case.gd"

## MIL-ECON-003: a collector gathers from an adjacent resource node.


func get_name() -> String:
	return "test_MIL_ECON_003_collector_harvests_node"


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
	var field_id: int = _find_field(state)
	var field_start: int = int(MilitaryState.get_resource_field(state, field_id).get("amount", 0))

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, HarvestResourceCommand.new("p1", 0, {"unit_id": collector_id, "field_id": field_id}))
	assert_true(res.success, "harvest order accepted")

	state = runner.run_ticks(state, 20)
	var collector: Dictionary = MilitaryState.get_unit(state, collector_id)
	var cargo: int = int(collector.get("cargo", 0))
	var field_after: int = int(MilitaryState.get_resource_field(state, field_id).get("amount", 0))
	assert_true(cargo > 0 or field_after < field_start, "collector harvested cargo or depleted field")

	var saw_harvest: bool = false
	for event in state.event_log.events:
		if event.type == "resource_harvested":
			saw_harvest = true
	assert_true(saw_harvest, "resource_harvested event emitted")


func _find_collector(state: GameState) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == "collector":
			return int(unit.get("id", 0))
	return 0


func _find_field(state: GameState) -> int:
	for field in MilitaryState.all_resource_fields(state):
		return int(field.get("id", 0))
	return 0
