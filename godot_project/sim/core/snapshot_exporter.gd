class_name SnapshotExporter
extends RefCounted

const SCHEMA_VERSION: int = 1


static func export_snapshot(state: GameState) -> Dictionary:
	var player_data: Array = []
	for player in players_to_array(state):
		player_data.append(player.to_dict())

	var roads: Array = []
	var road_nodes: Array = []
	var buildings: Array = []
	var construction_sites: Array = []
	var worker_markers: Array = []
	var carrier_markers: Array = []
	var ware_movement_markers: Array = []
	var blocked_job_markers: Array = []

	if state.entities.has(EconomyConstants.BUCKET_ROADS):
		for key in state.entities[EconomyConstants.BUCKET_ROADS]:
			roads.append(state.entities[EconomyConstants.BUCKET_ROADS][key])
	if state.entities.has(EconomyConstants.BUCKET_ROAD_NODES):
		for key in state.entities[EconomyConstants.BUCKET_ROAD_NODES]:
			road_nodes.append(state.entities[EconomyConstants.BUCKET_ROAD_NODES][key])
	if state.entities.has(EconomyConstants.BUCKET_BUILDINGS):
		for key in state.entities[EconomyConstants.BUCKET_BUILDINGS]:
			buildings.append(state.entities[EconomyConstants.BUCKET_BUILDINGS][key])
	if state.entities.has(EconomyConstants.BUCKET_CONSTRUCTION_SITES):
		for key in state.entities[EconomyConstants.BUCKET_CONSTRUCTION_SITES]:
			construction_sites.append(state.entities[EconomyConstants.BUCKET_CONSTRUCTION_SITES][key])
	if state.entities.has(EconomyConstants.BUCKET_WORKERS):
		for key in state.entities[EconomyConstants.BUCKET_WORKERS]:
			worker_markers.append(state.entities[EconomyConstants.BUCKET_WORKERS][key])
	if state.entities.has(EconomyConstants.BUCKET_CARRIERS):
		for key in state.entities[EconomyConstants.BUCKET_CARRIERS]:
			carrier_markers.append(state.entities[EconomyConstants.BUCKET_CARRIERS][key])
	if state.entities.has(EconomyConstants.BUCKET_DELIVERY_JOBS):
		for key in state.entities[EconomyConstants.BUCKET_DELIVERY_JOBS]:
			var job: Dictionary = state.entities[EconomyConstants.BUCKET_DELIVERY_JOBS][key]
			if str(job.get("status", "")) == EconomyConstants.JOB_IN_TRANSIT:
				ware_movement_markers.append(job)
			elif str(job.get("status", "")) in [
				EconomyConstants.JOB_BLOCKED,
				EconomyConstants.JOB_WAITING_ROUTE,
				EconomyConstants.JOB_FAILED,
			]:
				blocked_job_markers.append(job)

	var map_data: Dictionary = state.map.to_dict() if state.map != null else {}
	var display_layers: Dictionary = _build_display_layers(state)

	return {
		"schema_version": SCHEMA_VERSION,
		"variant": state.variant_id,
		"tick": state.tick,
		"seed": state.seed,
		"rng_state": state.rng_state,
		"map": map_data,
		"display_layers": display_layers,
		"roads": roads,
		"road_nodes": road_nodes,
		"buildings": buildings,
		"construction_sites": construction_sites,
		"worker_markers": worker_markers,
		"carrier_markers": carrier_markers,
		"ware_movement_markers": ware_movement_markers,
		"blocked_job_markers": blocked_job_markers,
		"entities": state.entities.duplicate(true),
		"diagnostics": state.diagnostics.duplicate(true),
		"players": player_data,
	}


static func _build_display_layers(state: GameState) -> Dictionary:
	var terrain_layer: Array = []
	var height_layer: Array = []
	var resource_layer: Array = []
	var tree_density_layer: Array = []
	var ownership_layer: Array = []

	if state.map == null:
		return {
			"terrain": terrain_layer,
			"height_buildability": height_layer,
			"resources": resource_layer,
			"tree_density": tree_density_layer,
			"ownership": ownership_layer,
			"hex_grid_visible": 0,
		}

	for q in range(state.map.width):
		for r in range(state.map.height):
			var coord := HexCoord.new(q, r)
			if not state.map.in_bounds(coord):
				continue
			var tile: HexTile = state.map.get_tile(coord)
			terrain_layer.append({
				"q": q, "r": r,
				"terrain_type": tile.terrain_type,
			})
			height_layer.append({
				"q": q, "r": r,
				"height_level": tile.height_level,
				"buildable": tile.buildability,
			})
			if tile.deposit_type != "":
				resource_layer.append({
					"q": q, "r": r,
					"deposit_type": tile.deposit_type,
					"deposit_amount": tile.deposit_amount,
					"deposit_revealed": tile.deposit_revealed,
				})
			if tile.tree_density > 0:
				tree_density_layer.append({
					"q": q, "r": r,
					"tree_density": tile.tree_density,
				})
			if tile.owner_player_id != "":
				ownership_layer.append({
					"q": q, "r": r,
					"owner_player_id": tile.owner_player_id,
				})

	return {
		"terrain": terrain_layer,
		"height_buildability": height_layer,
		"resources": resource_layer,
		"tree_density": tree_density_layer,
		"ownership": ownership_layer,
		"hex_grid_visible": 0,
	}


static func players_to_array(state: GameState) -> Array[PlayerState]:
	return state.players
