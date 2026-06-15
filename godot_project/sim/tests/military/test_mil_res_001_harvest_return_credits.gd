extends "res://sim/tests/test_case.gd"

## MIL-RES-001: a collector harvests a resource field, returns to a processor, and
## the delivered cargo is converted into player credits; the field depletes.


func get_name() -> String:
	return "test_MIL_RES_001_harvest_return_credits"


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
				"player_id": "p1",
				"q": 2, "r": 2,
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
	assert_true(collector_id > 0, "collector spawned")
	assert_true(field_id > 0, "resource field present")

	var field_start: int = int(MilitaryState.get_resource_field(state, field_id).get("amount", 0))
	assert_eq(MilitaryState.player_credits(state, "p1"), 0, "starts with zero credits")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var cmd := HarvestResourceCommand.new("p1", 0, {"unit_id": collector_id, "field_id": field_id})
	var result: CommandResult = validator.validate_and_apply(state, cmd)
	assert_true(result.success, "harvest order accepted")

	runner.run_ticks(state, 80)

	var credits_after: int = MilitaryState.player_credits(state, "p1")
	assert_true(credits_after > 0, "credits earned from delivered cargo (got %d)" % credits_after)

	var field_after: int = int(MilitaryState.get_resource_field(state, field_id).get("amount", 0))
	assert_true(field_after < field_start, "field depleted by harvesting (%d -> %d)" % [field_start, field_after])


func _find_collector(state: GameState) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == "collector":
			return int(unit.get("id", 0))
	return 0


func _find_field(state: GameState) -> int:
	for field in MilitaryState.all_resource_fields(state):
		return int(field.get("id", 0))
	return 0
