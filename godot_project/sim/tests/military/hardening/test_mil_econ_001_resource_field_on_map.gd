extends "res://sim/tests/test_case.gd"

## MIL-ECON-001: seeded scenarios include resource fields on the shared hex map.


func get_name() -> String:
	return "test_MIL_ECON_001_resource_field_on_map"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 5,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 20,
		"starting_credits": 0,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 2, "r": 2}],
		"resource_fields": [{"q": 10, "r": 10, "field_type": "mineral_field"}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	var fields: Array = MilitaryState.all_resource_fields(state)
	assert_true(fields.size() >= 1, "map contains at least one resource field")
	var field: Dictionary = fields[0]
	assert_true(int(field.get("amount", 0)) > 0, "field has harvestable amount")
	assert_eq(str(field.get("field_type", "")), "mineral_field", "field type preserved")

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_true(snapshot.get("resource_fields", []).size() >= 1, "resource_fields in snapshot")

	var diag: Dictionary = state.diagnostics.get("military", {})
	assert_true(diag.has("players"), "military diagnostics present after init")
