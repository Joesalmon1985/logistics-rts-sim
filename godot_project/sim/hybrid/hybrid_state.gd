class_name HybridState
extends RefCounted

static var _shared_config: HybridConfig = null
static var _merged_economy_config: EconomyConfig = null


static func get_config(_state: GameState) -> HybridConfig:
	if _shared_config == null:
		_shared_config = HybridConfig.load_all()
	return _shared_config


static func reset_config_cache() -> void:
	_shared_config = null
	_merged_economy_config = null


static func set_merged_economy_config(cfg: EconomyConfig) -> void:
	_merged_economy_config = cfg


static func get_merged_economy_config() -> EconomyConfig:
	return _merged_economy_config


static func ensure_buckets(state: GameState) -> void:
	for bucket in HybridConstants.ALL_BUCKETS:
		if not state.entities.has(bucket):
			state.entities[bucket] = {}


static func get_bucket(state: GameState, bucket: String) -> Dictionary:
	ensure_buckets(state)
	return state.entities[bucket]


static func emit_event(state: GameState, event_type: String, payload: Dictionary) -> void:
	state.event_log.append_event(event_type, state.tick, payload)


static func is_forbidden_player_command(state: GameState, command: SimCommand) -> bool:
	if not HybridConfig.is_hybrid(state):
		return false
	if command == null:
		return false
	var cfg: HybridConfig = get_config(state)
	return str(command.type) in cfg.forbidden_player_commands


static func set_building_policy(state: GameState, building_id: int, policy: Dictionary) -> void:
	get_bucket(state, HybridConstants.BUCKET_BUILDING_POLICIES)[str(building_id)] = policy


static func get_building_policy(state: GameState, building_id: int) -> Dictionary:
	return get_bucket(state, HybridConstants.BUCKET_BUILDING_POLICIES).get(str(building_id), {})


static func add_delegated_order(state: GameState, order: Dictionary) -> int:
	var order_id: int = state.entity_ids.allocate()
	order["id"] = order_id
	get_bucket(state, HybridConstants.BUCKET_DELEGATED_ORDERS)[str(order_id)] = order
	return order_id


static func all_delegated_orders(state: GameState) -> Array:
	return get_bucket(state, HybridConstants.BUCKET_DELEGATED_ORDERS).values()


static func remove_delegated_order(state: GameState, order_id: int) -> void:
	get_bucket(state, HybridConstants.BUCKET_DELEGATED_ORDERS).erase(str(order_id))


static func player_hq_store(state: GameState, player_id: String) -> Dictionary:
	var hq_ids: Dictionary = EconomyState.get_meta_value(state, "hq_building_ids", {})
	var hq_id: int = int(hq_ids.get(player_id, EconomyState.get_meta_value(state, "hq_building_id", 0)))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var store_id: int = int(hq.get("store_id", 0))
	return EconomyState.get_ware_store(state, store_id)


static func spend_wares_from_hq(state: GameState, player_id: String, costs: Dictionary) -> bool:
	var store: Dictionary = player_hq_store(state, player_id)
	for ware_type in costs:
		if str(ware_type) == "builder_work":
			continue
		var needed: int = int(costs[ware_type])
		if not EconomyState.inventory_has(store, str(ware_type), needed):
			return false
	for ware_type in costs:
		if str(ware_type) == "builder_work":
			continue
		EconomyState.inventory_remove(store, str(ware_type), int(costs[ware_type]))
	var hq_ids: Dictionary = EconomyState.get_meta_value(state, "hq_building_ids", {})
	var hq_id: int = int(hq_ids.get(player_id, EconomyState.get_meta_value(state, "hq_building_id", 0)))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	EconomyState.set_ware_store(state, int(hq.get("store_id", 0)), store)
	return true


static func has_wares_in_hq(state: GameState, player_id: String, costs: Dictionary) -> bool:
	var store: Dictionary = player_hq_store(state, player_id)
	for ware_type in costs:
		if str(ware_type) == "builder_work":
			continue
		if not EconomyState.inventory_has(store, str(ware_type), int(costs[ware_type])):
			return false
	return true


static func target_visible_to_player(state: GameState, player_id: String, q: int, r: int) -> bool:
	if int(MilitaryState.get_meta_value(state, "fog_enabled", 1)) <= 0:
		return true
	var vision: Dictionary = MilitaryState.get_vision(state, player_id)
	var levels: Dictionary = vision.get("levels", {})
	var key: String = MilitaryState.coord_key(q, r)
	var level: int = int(levels.get(key, MilitaryConstants.INTEL_UNEXPLORED))
	return level >= MilitaryConstants.INTEL_EXPLORED
