extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_SNAP_003_logistics_arrays_present"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 601,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 0, 0, 9, 9)
	_connect_roads(state, 2, 2, 6, 3)
	var result: Dictionary = EconomyConstructionSystem.create_site(
		state, "storehouse", 6, 3, "p1", 0, {"planks": 2, "builder_work": 2},
	)
	assert_true(int(result.get("site_id", 0)) > 0, "construction site required")
	state = runner.run_ticks(state, 60)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_has_keys(snapshot, [
		"roads", "road_nodes", "carrier_markers", "construction_sites",
		"delivery_jobs", "diagnostics", "buildings",
	], "full snapshot logistics keys")
	assert_true(snapshot.get("roads", []).size() > 0, "roads must be populated")
	assert_true(snapshot.get("road_nodes", []).size() > 0, "road_nodes must be populated")
	assert_true(snapshot.get("construction_sites", []).size() > 0, "construction_sites must be populated")

	var hq: Dictionary = {}
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == "headquarters":
			hq = building
			break
	assert_false(hq.is_empty(), "HQ building required")
	assert_true(hq.has("port_node_ids"), "building must export port_node_ids")
	assert_true(hq.get("port_node_ids", []).size() >= 1, "HQ must have port nodes")

	var delivery_diag: Dictionary = snapshot.get("diagnostics", {}).get("delivery", {})
	assert_true(delivery_diag.has("jobs"), "delivery diagnostics must include jobs")

	var saw_logistics_marker: bool = (
		snapshot.get("carrier_markers", []).size() > 0
		or snapshot.get("ware_movement_markers", []).size() > 0
		or snapshot.get("blocked_job_markers", []).size() > 0
		or snapshot.get("delivery_jobs", []).size() > 0
	)
	assert_true(saw_logistics_marker, "snapshot must include carrier/job logistics markers")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))


func _connect_roads(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> void:
	var current := HexCoord.new(from_q, from_r)
	var target := HexCoord.new(to_q, to_r)
	while current.q != target.q:
		var next := HexCoord.new(current.q + (1 if target.q > current.q else -1), current.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
	while current.r != target.r:
		var next := HexCoord.new(current.q, current.r + (1 if target.r > current.r else -1))
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
