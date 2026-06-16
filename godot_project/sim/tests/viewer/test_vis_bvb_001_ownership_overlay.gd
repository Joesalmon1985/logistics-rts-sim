extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_VIS_BVB_001_ownership_overlay"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/two_player_init_test.json")
	var state: GameState = runner.create_game(scenario)
	EconomyDiagnosticsSystem.tick(state)
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var render: Dictionary = LayerRenderer.build_render_data(snapshot)

	assert_true(render.has("ownership"), "ownership layer present")
	assert_true(render.get("ownership", []).size() > 0, "ownership hexes exported")

	for road in render.get("roads", []):
		assert_true(road.has("player_id"), "road marker has player_id")

	for node in render.get("road_node_markers", []):
		assert_true(node.has("player_id"), "road node marker has player_id")

	for carrier in render.get("carriers", []):
		assert_true(str(carrier.get("player_id", "")) != "", "carrier has player_id")

	for site in render.get("construction_sites", []):
		assert_true(str(site.get("player_id", "")) != "", "construction site has player_id")
