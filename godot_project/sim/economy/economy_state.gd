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


static func get_config(_state: GameState) -> EconomyConfig:
	if _shared_config == null:
		_shared_config = EconomyConfig.load_all()
	return _shared_config


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
