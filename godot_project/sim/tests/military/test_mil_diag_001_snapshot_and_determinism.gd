extends "res://sim/tests/test_case.gd"

## MIL-DIAG-001: after ticks, diagnostics expose per-player integer metrics and the
## snapshot carries every military layer; the run is a deterministic replay.


func get_name() -> String:
	return "test_MIL_DIAG_001_snapshot_and_determinism"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 71,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [{"type": "power_generator", "q": 6, "r": 2}],
				"units": [{"type": "rifle_squad", "q": 8, "r": 8}],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 10)

	# Diagnostics.
	var military: Dictionary = state.diagnostics.get("military", {})
	assert_true(military.has("players"), "military diagnostics present")
	var p1: Dictionary = military.get("players", {}).get("p1", {})
	assert_true(p1.has("credits"), "per-player credits diagnostic")
	assert_eq(int(p1.get("unit_count", -1)), 1, "unit_count diagnostic")
	assert_true(int(p1.get("building_count", 0)) >= 2, "building_count diagnostic")

	# Snapshot completeness.
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	for layer in ["units", "buildings", "construction_sites", "production_queues", "projectiles", "repair_jobs", "vision", "power", "tech", "military_players", "resource_fields", "display_layers", "diagnostics"]:
		assert_true(snapshot.has(layer), "snapshot has layer: %s" % layer)
	assert_eq(int(snapshot.get("schema_version", 0)), 1, "snapshot schema_version=1")
	assert_eq(typeof(JSON.parse_string(JSON.stringify(snapshot))), TYPE_DICTIONARY, "snapshot is JSON-compatible")

	# Deterministic replay.
	var h1: String = ReplayHash.compute_state_hash(state)
	var runner2 := SimulationTickRunner.new()
	var state2: GameState = runner2.create_game(scenario)
	state2 = runner2.run_ticks(state2, 10)
	assert_eq(h1, ReplayHash.compute_state_hash(state2), "replay hash deterministic")
