extends "res://sim/tests/test_case.gd"

## MIL-POWER-005: low power is reported in per-player diagnostics and power layer.


func get_name() -> String:
	return "test_MIL_POWER_005_low_power_in_diagnostics"


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
					{"type": "vehicle_plant", "q": 9, "r": 2},
					{"type": "research_lab", "q": 12, "r": 2},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	var p1: Dictionary = state.diagnostics.get("military", {}).get("players", {}).get("p1", {})
	assert_eq(int(p1.get("powered", 1)), 0, "diagnostics report underpowered")
	assert_true(int(p1.get("power_demand", 0)) > int(p1.get("power_supply", 0)), "demand exceeds supply in diagnostics")

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var power_layer: Array = snapshot.get("power", [])
	assert_true(power_layer.size() >= 1, "power layer present")
	assert_eq(int(power_layer[0].get("powered", 1)), 0, "snapshot power layer shows underpowered")

	var saw_power_event: bool = false
	for event in state.event_log.events:
		if event.type == "power_state_changed":
			saw_power_event = true
	assert_true(saw_power_event, "power_state_changed event emitted")
