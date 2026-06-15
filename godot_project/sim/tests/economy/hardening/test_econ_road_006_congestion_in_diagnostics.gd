extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_ROAD_006_congestion_in_diagnostics"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 166,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_set_grass_tile(state, 1, 1, 0)
	_set_grass_tile(state, 2, 1, 0)
	var road_result: Dictionary = EconomyRoadSystem.place_road(state, HexCoord.new(1, 1), HexCoord.new(2, 1), "p1")
	var road_id: int = int(road_result.get("road_id", 0))
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var limit: int = cfg.int_val("road_congestion_limit", 3)
	for _i in range(limit):
		EconomyRoadSystem.increment_congestion(state, road_id)

	var congested_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "road_congested" and int(event.payload.get("road_id", 0)) == road_id:
			congested_event = true
			break
	assert_true(congested_event, "road_congested event expected when segment hits limit")

	EconomyDiagnosticsSystem.tick(state)
	var network: Dictionary = state.diagnostics.get("network", {})
	assert_true(int(network.get("congested_segments", 0)) >= 1, "congestion must appear in network diagnostics")


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))
