class_name EconomyDemo
extends RefCounted

## Curated economy trial demo state for RUN_05 viewer — compact, readable layout.


static func build_state() -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 42,
		"map_width": 12,
		"map_height": 12,
		"players": ["p1"],
		"hq_position": {"q": 5, "r": 5},
		"build_radius": 8,
		"starting_wares": {"planks": 20, "stone": 10, "tools": 5},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 11, 11)
	state.map.set_tile(HexCoord.new(8, 5), _forest_tile())
	state.map.set_tile(HexCoord.new(9, 5), _forest_tile())
	_connect_roads(state, 5, 5, 8, 5)
	EconomyInit._spawn_completed_building(
		state, "woodcutter", 8, 5, "p1", EconomyState.get_config(state),
	)
	EconomyInit._spawn_completed_building(
		state, "sawmill", 5, 7, "p1", EconomyState.get_config(state),
	)
	_connect_roads(state, 5, 5, 5, 7)
	_connect_roads(state, 5, 7, 5, 8)
	var site_result: Dictionary = EconomyConstructionSystem.create_site(state, "storehouse", 5, 8, "p1")
	if site_result.get("ok", false):
		var site_id: int = int(site_result.get("site_id", 0))
		EconomyConstructionSystem.deliver_to_site(state, site_id, "planks", 1)
	return state


static func verify_progression_within_ticks(max_ticks: int = 50) -> Dictionary:
	var runner := SimulationTickRunner.new()
	var state: GameState = build_state()
	var baseline: Dictionary = _progress_fingerprint(state)
	for _i in range(max_ticks):
		state = runner.tick(state)
		var current: Dictionary = _progress_fingerprint(state)
		if current.hash() != baseline.hash():
			return {"ok": true, "ticks": _i + 1, "fingerprint": current}
	return {"ok": false, "ticks": max_ticks, "fingerprint": baseline}


static func _progress_fingerprint(state: GameState) -> Dictionary:
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var render: Dictionary = LayerRenderer.build_render_data(snapshot)
	var delivered_total: int = 0
	for site in snapshot.get("construction_sites", []):
		for ware_type in site.get("delivered", {}):
			delivered_total += int(site.get("delivered", {}).get(ware_type, 0))
	var carrier_moving: int = 0
	for carrier in snapshot.get("carrier_markers", []):
		if str(carrier.get("state", "")) != EconomyConstants.CARRIER_IDLE:
			carrier_moving += 1
	return {
		"tick": state.tick,
		"event_count": state.event_log.events.size(),
		"delivered_total": delivered_total,
		"carrier_moving": carrier_moving,
		"construction_sites": render.get("construction_sites", []).size(),
		"carriers": render.get("carriers", []).size(),
	}


static func _flatten_grass(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			var coord := HexCoord.new(q, r)
			if state.map.in_bounds(coord):
				state.map.set_tile(coord, HexTile.new("grass", 0, 1, 1))


static func _forest_tile() -> HexTile:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = 80
	tile.deposit_type = "forest"
	tile.deposit_amount = 50
	tile.deposit_revealed = 1
	return tile


static func _connect_roads(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> void:
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
