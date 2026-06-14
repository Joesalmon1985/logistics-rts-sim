class_name MilitaryState
extends RefCounted

## Stable schema accessors/factories for military entities in GameState.entities.
## All subsystems MUST use these helpers — no ad hoc dictionary keys.

static var _shared_config: MilitaryConfig = null


static func get_config(_state: GameState) -> MilitaryConfig:
	if _shared_config == null:
		_shared_config = MilitaryConfig.load_all()
	return _shared_config


static func reset_config_cache() -> void:
	_shared_config = null


static func ensure_buckets(state: GameState) -> void:
	for bucket in MilitaryConstants.ALL_BUCKETS:
		if not state.entities.has(bucket):
			state.entities[bucket] = {}


static func get_bucket(state: GameState, bucket: String) -> Dictionary:
	ensure_buckets(state)
	return state.entities[bucket]


# --- Meta -------------------------------------------------------------------

static func meta_dict(state: GameState) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_META)


static func set_meta_value(state: GameState, key: String, value: Variant) -> void:
	meta_dict(state)[key] = value


static func get_meta_value(state: GameState, key: String, default_value: Variant = null) -> Variant:
	return meta_dict(state).get(key, default_value)


# --- Players ----------------------------------------------------------------

static func add_player(state: GameState, player: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_PLAYERS)[str(player["player_id"])] = player


static func get_player(state: GameState, player_id: String) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_PLAYERS).get(player_id, {})


static func all_players(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_PLAYERS).values()


static func player_credits(state: GameState, player_id: String) -> int:
	return int(get_player(state, player_id).get("credits", 0))


static func add_credits(state: GameState, player_id: String, amount: int) -> void:
	var p: Dictionary = get_player(state, player_id)
	if p.is_empty():
		return
	p["credits"] = int(p.get("credits", 0)) + amount


static func spend_credits(state: GameState, player_id: String, amount: int) -> bool:
	var p: Dictionary = get_player(state, player_id)
	if int(p.get("credits", 0)) < amount:
		return false
	p["credits"] = int(p.get("credits", 0)) - amount
	return true


static func player_has_tech(state: GameState, player_id: String, tech_id: String) -> bool:
	return tech_id in get_player(state, player_id).get("tech_unlocked", [])


# --- Resource fields --------------------------------------------------------

static func add_resource_field(state: GameState, field: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_RESOURCE_FIELDS)[str(field["id"])] = field


static func get_resource_field(state: GameState, field_id: int) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_RESOURCE_FIELDS).get(str(field_id), {})


static func all_resource_fields(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_RESOURCE_FIELDS).values()


# --- Buildings --------------------------------------------------------------

static func add_building(state: GameState, building: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_BUILDINGS)[str(building["id"])] = building


static func get_building(state: GameState, building_id: int) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_BUILDINGS).get(str(building_id), {})


static func all_buildings(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_BUILDINGS).values()


static func remove_building(state: GameState, building_id: int) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_BUILDINGS).erase(str(building_id))


# --- Construction sites -----------------------------------------------------

static func add_construction_site(state: GameState, site: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_CONSTRUCTION_SITES)[str(site["id"])] = site


static func get_construction_site(state: GameState, site_id: int) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_CONSTRUCTION_SITES).get(str(site_id), {})


static func all_construction_sites(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_CONSTRUCTION_SITES).values()


# --- Production queues -------------------------------------------------------

static func get_production_queue(state: GameState, building_id: int) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_PRODUCTION_QUEUES).get(str(building_id), {})


static func set_production_queue(state: GameState, building_id: int, queue: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_PRODUCTION_QUEUES)[str(building_id)] = queue


static func ensure_production_queue(state: GameState, building_id: int, player_id: String) -> Dictionary:
	var q: Dictionary = get_production_queue(state, building_id)
	if q.is_empty():
		q = {"building_id": building_id, "player_id": player_id, "items": []}
		set_production_queue(state, building_id, q)
	return q


static func all_production_queues(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_PRODUCTION_QUEUES).values()


# --- Units ------------------------------------------------------------------

static func add_unit(state: GameState, unit: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_UNITS)[str(unit["id"])] = unit


static func get_unit(state: GameState, unit_id: int) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_UNITS).get(str(unit_id), {})


static func all_units(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_UNITS).values()


static func remove_unit(state: GameState, unit_id: int) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_UNITS).erase(str(unit_id))


# --- Projectiles ------------------------------------------------------------

static func add_projectile(state: GameState, projectile: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_PROJECTILES)[str(projectile["id"])] = projectile


static func all_projectiles(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_PROJECTILES).values()


static func remove_projectile(state: GameState, projectile_id: int) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_PROJECTILES).erase(str(projectile_id))


# --- Repair jobs ------------------------------------------------------------

static func add_repair_job(state: GameState, job: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_REPAIR_JOBS)[str(job["id"])] = job


static func get_repair_job(state: GameState, job_id: int) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_REPAIR_JOBS).get(str(job_id), {})


static func all_repair_jobs(state: GameState) -> Array:
	return get_bucket(state, MilitaryConstants.BUCKET_REPAIR_JOBS).values()


static func remove_repair_job(state: GameState, job_id: int) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_REPAIR_JOBS).erase(str(job_id))


# --- Vision -----------------------------------------------------------------

static func get_vision(state: GameState, player_id: String) -> Dictionary:
	var bucket: Dictionary = get_bucket(state, MilitaryConstants.BUCKET_VISION)
	if not bucket.has(player_id):
		bucket[player_id] = {"player_id": player_id, "levels": {}}
	return bucket[player_id]


static func intel_level(state: GameState, player_id: String, q: int, r: int) -> int:
	var levels: Dictionary = get_vision(state, player_id).get("levels", {})
	return int(levels.get(coord_key(q, r), MilitaryConstants.INTEL_UNEXPLORED))


static func set_intel_level(state: GameState, player_id: String, q: int, r: int, level: int) -> void:
	var levels: Dictionary = get_vision(state, player_id)["levels"]
	levels[coord_key(q, r)] = level


# --- Research ---------------------------------------------------------------

static func get_research(state: GameState, player_id: String) -> Dictionary:
	return get_bucket(state, MilitaryConstants.BUCKET_RESEARCH).get(player_id, {})


static func set_research(state: GameState, player_id: String, research: Dictionary) -> void:
	get_bucket(state, MilitaryConstants.BUCKET_RESEARCH)[player_id] = research


# --- Combat log -------------------------------------------------------------

static func append_combat_log(state: GameState, entry: Dictionary, max_entries: int = 50) -> void:
	var bucket: Dictionary = get_bucket(state, MilitaryConstants.BUCKET_COMBAT_LOG)
	var entries: Array = bucket.get("entries", [])
	entries.append(entry)
	while entries.size() > max_entries:
		entries.pop_front()
	bucket["entries"] = entries


# --- Helpers ----------------------------------------------------------------

static func coord_key(q: int, r: int) -> String:
	return "%d,%d" % [q, r]


static func emit_event(state: GameState, event_type: String, payload: Dictionary = {}) -> Dictionary:
	state.event_log.append_event(event_type, state.tick, payload)
	return {"type": event_type, "tick": state.tick, "payload": payload}


static func footprint_coords(anchor_q: int, anchor_r: int, footprint: Array) -> Array:
	var coords: Array = []
	for offset in footprint:
		coords.append(Vector2i(anchor_q + int(offset[0]), anchor_r + int(offset[1])))
	return coords
