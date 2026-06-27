class_name EconomyState
extends RefCounted

## Stable schema accessors for economy entities in GameState.entities.
## All subsystems must use these helpers — no ad hoc keys.

static func ensure_buckets(state: GameState) -> void:
	for bucket in [
		EconomyConstants.BUCKET_ROADS,
		EconomyConstants.BUCKET_ROAD_NODES,
		EconomyConstants.BUCKET_BUILDINGS,
		EconomyConstants.BUCKET_CONSTRUCTION_SITES,
		EconomyConstants.BUCKET_CARRIERS,
		EconomyConstants.BUCKET_WORKERS,
		EconomyConstants.BUCKET_DELIVERY_JOBS,
		EconomyConstants.BUCKET_WARE_STORES,
		EconomyConstants.BUCKET_RESERVATIONS,
		EconomyConstants.BUCKET_ECONOMY_META,
	]:
		if not state.entities.has(bucket):
			state.entities[bucket] = {}


static func get_bucket(state: GameState, bucket: String) -> Dictionary:
	ensure_buckets(state)
	return state.entities[bucket]


static func get_economy_meta(state: GameState) -> Dictionary:
	return get_bucket(state, EconomyConstants.BUCKET_ECONOMY_META)


static func set_meta_value(state: GameState, key: String, value: Variant) -> void:
	var meta: Dictionary = get_economy_meta(state)
	meta[key] = value


static func get_meta_value(state: GameState, key: String, default_value: Variant = null) -> Variant:
	var meta: Dictionary = get_economy_meta(state)
	return meta.get(key, default_value)


static var _shared_config: EconomyConfig = null


static func get_config(state: GameState) -> EconomyConfig:
	if state != null and state.variant_id == "hybrid":
		var hybrid_cfg: EconomyConfig = HybridState.get_merged_economy_config()
		if hybrid_cfg != null:
			return hybrid_cfg
	if state != null:
		ensure_buckets(state)
		var override: Variant = get_meta_value(state, "economy_config_override", null)
		if override is EconomyConfig:
			return override as EconomyConfig
	if _shared_config == null:
		_shared_config = EconomyConfig.load_all()
	return _shared_config


static func set_config_override(state: GameState, cfg: EconomyConfig) -> void:
	ensure_buckets(state)
	set_meta_value(state, "economy_config_override", cfg)


static func clear_config_override(state: GameState) -> void:
	set_meta_value(state, "economy_config_override", null)


static func has_config_override(state: GameState) -> bool:
	return get_meta_value(state, "economy_config_override", null) is EconomyConfig


static func reset_config_cache() -> void:
	_shared_config = null


static func add_road(state: GameState, road: Dictionary) -> void:
	var roads: Dictionary = get_bucket(state, EconomyConstants.BUCKET_ROADS)
	roads[str(road["id"])] = road


static func get_road(state: GameState, road_id: int) -> Dictionary:
	var roads: Dictionary = get_bucket(state, EconomyConstants.BUCKET_ROADS)
	return roads.get(str(road_id), {})


static func add_road_node(state: GameState, node: Dictionary) -> void:
	var nodes: Dictionary = get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES)
	nodes[str(node["id"])] = node


static func get_road_node(state: GameState, node_id: int) -> Dictionary:
	var nodes: Dictionary = get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES)
	return nodes.get(str(node_id), {})


static func add_building(state: GameState, building: Dictionary) -> void:
	var bld: Dictionary = get_bucket(state, EconomyConstants.BUCKET_BUILDINGS)
	bld[str(building["id"])] = building


static func get_building(state: GameState, building_id: int) -> Dictionary:
	var bld: Dictionary = get_bucket(state, EconomyConstants.BUCKET_BUILDINGS)
	return bld.get(str(building_id), {})


static func add_construction_site(state: GameState, site: Dictionary) -> void:
	var sites: Dictionary = get_bucket(state, EconomyConstants.BUCKET_CONSTRUCTION_SITES)
	sites[str(site["id"])] = site


static func get_construction_site(state: GameState, site_id: int) -> Dictionary:
	var sites: Dictionary = get_bucket(state, EconomyConstants.BUCKET_CONSTRUCTION_SITES)
	return sites.get(str(site_id), {})


static func add_carrier(state: GameState, carrier: Dictionary) -> void:
	var carriers: Dictionary = get_bucket(state, EconomyConstants.BUCKET_CARRIERS)
	carriers[str(carrier["id"])] = carrier


static func add_worker(state: GameState, worker: Dictionary) -> void:
	var workers: Dictionary = get_bucket(state, EconomyConstants.BUCKET_WORKERS)
	workers[str(worker["id"])] = worker


static func add_delivery_job(state: GameState, job: Dictionary) -> void:
	var jobs: Dictionary = get_bucket(state, EconomyConstants.BUCKET_DELIVERY_JOBS)
	jobs[str(job["id"])] = job


static func get_delivery_job(state: GameState, job_id: int) -> Dictionary:
	var jobs: Dictionary = get_bucket(state, EconomyConstants.BUCKET_DELIVERY_JOBS)
	return jobs.get(str(job_id), {})


static func get_ware_store(state: GameState, store_id: int) -> Dictionary:
	var stores: Dictionary = get_bucket(state, EconomyConstants.BUCKET_WARE_STORES)
	return stores.get(str(store_id), {})


static func set_ware_store(state: GameState, store_id: int, store: Dictionary) -> void:
	var stores: Dictionary = get_bucket(state, EconomyConstants.BUCKET_WARE_STORES)
	stores[str(store_id)] = store


static func create_empty_inventory(capacity: int) -> Dictionary:
	return {"wares": {}, "capacity": capacity, "reserved": {}}


static func inventory_total(inv: Dictionary) -> int:
	var total: int = 0
	for ware_type in inv.get("wares", {}):
		total += int(inv["wares"][ware_type])
	for ware_type in inv.get("reserved", {}):
		total += int(inv["reserved"][ware_type])
	return total


static func inventory_free(inv: Dictionary) -> int:
	return int(inv.get("capacity", 0)) - inventory_total(inv)


static func inventory_has(inv: Dictionary, ware_type: String, amount: int) -> bool:
	var available: int = int(inv.get("wares", {}).get(ware_type, 0))
	var reserved: int = int(inv.get("reserved", {}).get(ware_type, 0))
	return available - reserved >= amount


static func inventory_add(inv: Dictionary, ware_type: String, amount: int) -> bool:
	if inventory_free(inv) < amount:
		return false
	if not inv.has("wares"):
		inv["wares"] = {}
	inv["wares"][ware_type] = int(inv["wares"].get(ware_type, 0)) + amount
	return true


static func inventory_remove(inv: Dictionary, ware_type: String, amount: int) -> bool:
	if not inventory_has(inv, ware_type, amount):
		return false
	inv["wares"][ware_type] = int(inv["wares"].get(ware_type, 0)) - amount
	if inv["wares"][ware_type] <= 0:
		inv["wares"].erase(ware_type)
	return true


static func inventory_reserve(inv: Dictionary, ware_type: String, amount: int) -> bool:
	if not inventory_has(inv, ware_type, amount):
		return false
	if not inv.has("reserved"):
		inv["reserved"] = {}
	inv["reserved"][ware_type] = int(inv["reserved"].get(ware_type, 0)) + amount
	return true


static func inventory_unreserve(inv: Dictionary, ware_type: String, amount: int) -> void:
	if not inv.has("reserved"):
		return
	inv["reserved"][ware_type] = int(inv["reserved"].get(ware_type, 0)) - amount
	if inv["reserved"][ware_type] <= 0:
		inv["reserved"].erase(ware_type)


static func coord_key(q: int, r: int) -> String:
	return "%d,%d" % [q, r]


static func parse_coord_key(key: String) -> Vector2i:
	var parts: PackedStringArray = key.split(",")
	return Vector2i(int(parts[0]), int(parts[1]))


static func int_dict(source: Dictionary) -> Dictionary:
	var out: Dictionary = {}
	for key in source:
		out[str(key)] = int(source[key])
	return out


static func emit_event(state: GameState, event_type: String, payload: Dictionary = {}) -> Dictionary:
	state.event_log.append_event(event_type, state.tick, payload)
	return {"type": event_type, "tick": state.tick, "payload": payload}


static func all_buildings(state: GameState) -> Array:
	var result: Array = []
	for key in get_bucket(state, EconomyConstants.BUCKET_BUILDINGS):
		result.append(get_bucket(state, EconomyConstants.BUCKET_BUILDINGS)[key])
	return result


static func all_construction_sites(state: GameState) -> Array:
	var result: Array = []
	for key in get_bucket(state, EconomyConstants.BUCKET_CONSTRUCTION_SITES):
		result.append(get_bucket(state, EconomyConstants.BUCKET_CONSTRUCTION_SITES)[key])
	return result


static func all_delivery_jobs(state: GameState) -> Array:
	var result: Array = []
	for key in get_bucket(state, EconomyConstants.BUCKET_DELIVERY_JOBS):
		result.append(get_bucket(state, EconomyConstants.BUCKET_DELIVERY_JOBS)[key])
	return result


static func all_carriers(state: GameState) -> Array:
	var result: Array = []
	for key in get_bucket(state, EconomyConstants.BUCKET_CARRIERS):
		result.append(get_bucket(state, EconomyConstants.BUCKET_CARRIERS)[key])
	return result


static func all_workers(state: GameState) -> Array:
	var result: Array = []
	for key in get_bucket(state, EconomyConstants.BUCKET_WORKERS):
		result.append(get_bucket(state, EconomyConstants.BUCKET_WORKERS)[key])
	return result


static func get_hq_positions(state: GameState) -> Dictionary:
	var positions: Variant = get_meta_value(state, "hq_positions", {})
	return positions if typeof(positions) == TYPE_DICTIONARY else {}


static func get_hq_position(state: GameState, player_id: String) -> Vector2i:
	var positions: Dictionary = get_hq_positions(state)
	if positions.has(player_id):
		var pos: Dictionary = positions[player_id]
		return Vector2i(int(pos.get("q", 0)), int(pos.get("r", 0)))
	return Vector2i(
		int(get_meta_value(state, "hq_position_q", 0)),
		int(get_meta_value(state, "hq_position_r", 0)),
	)


static func get_hq_building_id(state: GameState, player_id: String) -> int:
	var ids: Variant = get_meta_value(state, "hq_building_ids", {})
	if typeof(ids) == TYPE_DICTIONARY and ids.has(player_id):
		return int(ids[player_id])
	return int(get_meta_value(state, "hq_building_id", 0))


static func get_hq_store_id(state: GameState, player_id: String) -> int:
	var hq_id: int = get_hq_building_id(state, player_id)
	var hq: Dictionary = get_building(state, hq_id)
	return int(hq.get("store_id", 0))


static func all_roads(state: GameState) -> Array:
	var result: Array = []
	for key in get_bucket(state, EconomyConstants.BUCKET_ROADS):
		result.append(get_bucket(state, EconomyConstants.BUCKET_ROADS)[key])
	return result


static func all_road_nodes(state: GameState) -> Array:
	var result: Array = []
	for key in get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES):
		result.append(get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES)[key])
	return result


static func increment_player_stat(state: GameState, player_id: String, stat_key: String, delta: int = 1) -> void:
	var stats: Variant = get_meta_value(state, "player_economy_stats", {})
	if typeof(stats) != TYPE_DICTIONARY:
		stats = {}
	if not stats.has(player_id):
		stats[player_id] = {}
	stats[player_id][stat_key] = int(stats[player_id].get(stat_key, 0)) + delta
	set_meta_value(state, "player_economy_stats", stats)


static func get_player_stat(state: GameState, player_id: String, stat_key: String) -> int:
	var stats: Variant = get_meta_value(state, "player_economy_stats", {})
	if typeof(stats) != TYPE_DICTIONARY:
		return 0
	if not stats.has(player_id):
		return 0
	return int(stats[player_id].get(stat_key, 0))
