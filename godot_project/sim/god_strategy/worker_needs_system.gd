class_name WorkerNeedsSystem
extends RefCounted

## Cohort-level worker needs — hunger, tiredness, happiness, safety (0–1000 integers).

const BUCKET_NEEDS: String = "god_strategy_worker_needs"
const NEED_SCALE: int = 1000
const HUNGER_DRIFT: int = 6
const TIREDNESS_DRIFT_WORKING: int = 10
const TIREDNESS_RECOVERY_IDLE: int = 4
const HAPPINESS_DRIFT_LOW_NEEDS: int = 8
const HAPPINESS_RECOVERY: int = 2
const FOOD_SATISFACTION_GAIN: int = 200
const REST_TIREDNESS_RECOVERY: int = 80


static func is_active(state: GameState) -> bool:
	return int(EconomyState.get_meta_value(state, "god_strategy_active", 0)) > 0


static func init_match(state: GameState, scenario: Dictionary = {}) -> void:
	var bucket: Dictionary = _bucket(state)
	bucket.clear()
	for pid in scenario.get("players", ["p1"]):
		_init_player_cohorts(state, str(pid), bucket)


static func _init_player_cohorts(state: GameState, player_id: String, bucket: Dictionary) -> void:
	var types: Dictionary = {}
	for worker in EconomyState.all_workers(state):
		if str(worker.get("player_id", "")) != player_id:
			continue
		types[str(worker.get("worker_type", ""))] = true
	for worker_type in types.keys():
		if worker_type == "":
			continue
		bucket[_cohort_key(player_id, worker_type)] = _default_cohort()


static func _default_cohort() -> Dictionary:
	return {
		"hunger": NEED_SCALE,
		"tiredness": 0,
		"happiness": NEED_SCALE,
		"safety": NEED_SCALE,
	}


static func tick(state: GameState) -> void:
	if not is_active(state):
		return
	_ensure_cohorts(state)
	var bucket: Dictionary = _bucket(state)
	for key in bucket.keys():
		var cohort: Dictionary = bucket[key]
		_drift_cohort(cohort)
		_try_satisfy_from_food(state, key, cohort)
		bucket[key] = cohort
		_apply_cohort_to_workers(state, key, cohort)


static func set_cohort_needs(
	state: GameState,
	player_id: String,
	worker_type: String,
	hunger: int,
	tiredness: int,
	happiness: int,
	safety: int,
) -> void:
	var bucket: Dictionary = _bucket(state)
	bucket[_cohort_key(player_id, worker_type)] = {
		"hunger": clampi(hunger, 0, NEED_SCALE),
		"tiredness": clampi(tiredness, 0, NEED_SCALE),
		"happiness": clampi(happiness, 0, NEED_SCALE),
		"safety": clampi(safety, 0, NEED_SCALE),
	}


static func get_cohort_averages(state: GameState, player_id: String = "") -> Dictionary:
	var bucket: Dictionary = _bucket(state)
	var sums: Dictionary = {}
	var counts: Dictionary = {}
	for key in bucket.keys():
		var parts: PackedStringArray = key.split(":")
		if parts.size() != 2:
			continue
		if player_id != "" and str(parts[0]) != player_id:
			continue
		var cohort: Dictionary = bucket[key]
		for need in ["hunger", "tiredness", "happiness", "safety"]:
			sums[need] = int(sums.get(need, 0)) + int(cohort.get(need, 0))
		counts["n"] = int(counts.get("n", 0)) + 1
	if int(counts.get("n", 0)) <= 0:
		return {}
	var n: int = int(counts["n"])
	return {
		"hunger": int(sums.get("hunger", 0)) / n,
		"tiredness": int(sums.get("tiredness", 0)) / n,
		"happiness": int(sums.get("happiness", 0)) / n,
		"safety": int(sums.get("safety", 0)) / n,
		"cohort_count": n,
	}


static func get_cohort(state: GameState, player_id: String, worker_type: String) -> Dictionary:
	var bucket: Dictionary = _bucket(state)
	var key: String = _cohort_key(player_id, worker_type)
	if not bucket.has(key):
		bucket[key] = _default_cohort()
	return bucket[key]


static func cohort_key(player_id: String, worker_type: String) -> String:
	return _cohort_key(player_id, worker_type)


static func get_throughput_multiplier(state: GameState, player_id: String, worker_type: String) -> int:
	if not is_active(state):
		return NEED_SCALE
	var bucket: Dictionary = _bucket(state)
	var cohort: Dictionary = bucket.get(_cohort_key(player_id, worker_type), _default_cohort())
	var hunger: int = int(cohort.get("hunger", NEED_SCALE))
	var tiredness: int = int(cohort.get("tiredness", 0))
	var happiness: int = int(cohort.get("happiness", NEED_SCALE))
	var safety: int = int(cohort.get("safety", NEED_SCALE))
	var rested: int = NEED_SCALE - tiredness
	var base: int = (hunger + rested + happiness + safety) / 4
	return clampi(base, 0, NEED_SCALE)


static func get_production_progress_fp(state: GameState, building: Dictionary) -> int:
	var player_id: String = str(building.get("player_id", ""))
	var worker_type: String = str(building.get("worker_type", ""))
	if worker_type == "" or not is_active(state):
		return NEED_SCALE
	return get_throughput_multiplier(state, player_id, worker_type)


static func export_snapshot(state: GameState) -> Dictionary:
	if not is_active(state):
		return {}
	return {
		"cohorts": _bucket(state).duplicate(true),
		"player_averages": _player_average_map(state),
	}


static func _player_average_map(state: GameState) -> Dictionary:
	var result: Dictionary = {}
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "":
			continue
		var avg: Dictionary = get_cohort_averages(state, pid)
		if not avg.is_empty():
			result[pid] = avg
	return result


static func apply_rest_action(state: GameState, player_id: String, worker_type: String) -> void:
	var bucket: Dictionary = _bucket(state)
	var key: String = _cohort_key(player_id, worker_type)
	var cohort: Dictionary = bucket.get(key, _default_cohort())
	cohort["tiredness"] = clampi(int(cohort.get("tiredness", 0)) - REST_TIREDNESS_RECOVERY, 0, NEED_SCALE)
	cohort["happiness"] = clampi(int(cohort.get("happiness", 0)) + HAPPINESS_RECOVERY, 0, NEED_SCALE)
	bucket[key] = cohort


static func apply_seek_food_action(state: GameState, player_id: String, worker_type: String) -> void:
	var bucket: Dictionary = _bucket(state)
	var key: String = _cohort_key(player_id, worker_type)
	var cohort: Dictionary = bucket.get(key, _default_cohort())
	cohort["hunger"] = clampi(int(cohort.get("hunger", 0)) + FOOD_SATISFACTION_GAIN, 0, NEED_SCALE)
	bucket[key] = cohort


static func _bucket(state: GameState) -> Dictionary:
	if not state.entities.has(BUCKET_NEEDS):
		state.entities[BUCKET_NEEDS] = {}
	return state.entities[BUCKET_NEEDS]


static func _cohort_key(player_id: String, worker_type: String) -> String:
	return "%s:%s" % [player_id, worker_type]


static func _ensure_cohorts(state: GameState) -> void:
	var bucket: Dictionary = _bucket(state)
	for worker in EconomyState.all_workers(state):
		var pid: String = str(worker.get("player_id", ""))
		var wtype: String = str(worker.get("worker_type", ""))
		if pid == "" or wtype == "":
			continue
		var key: String = _cohort_key(pid, wtype)
		if not bucket.has(key):
			bucket[key] = _default_cohort()


static func _drift_cohort(cohort: Dictionary) -> void:
	cohort["hunger"] = clampi(int(cohort.get("hunger", NEED_SCALE)) - HUNGER_DRIFT, 0, NEED_SCALE)
	cohort["tiredness"] = clampi(int(cohort.get("tiredness", 0)) + TIREDNESS_DRIFT_WORKING, 0, NEED_SCALE)
	var avg_need: int = (
		int(cohort.get("hunger", 0))
		+ (NEED_SCALE - int(cohort.get("tiredness", 0)))
		+ int(cohort.get("happiness", 0))
	) / 3
	if avg_need < NEED_SCALE / 2:
		cohort["happiness"] = clampi(int(cohort.get("happiness", NEED_SCALE)) - HAPPINESS_DRIFT_LOW_NEEDS, 0, NEED_SCALE)


static func _try_satisfy_from_food(state: GameState, cohort_key: String, cohort: Dictionary) -> void:
	var parts: PackedStringArray = cohort_key.split(":")
	if parts.size() != 2:
		return
	var player_id: String = str(parts[0])
	if int(cohort.get("hunger", NEED_SCALE)) >= NEED_SCALE - 50:
		return
	for store_id in EconomyState.get_bucket(state, EconomyConstants.BUCKET_WARE_STORES).keys():
		var store: Dictionary = EconomyState.get_ware_store(state, int(store_id))
		if str(store.get("player_id", "")) != player_id:
			continue
		for ware in ["rations", "food"]:
			if EconomyState.inventory_has(store, ware, 1):
				EconomyState.inventory_remove(store, ware, 1)
				EconomyState.set_ware_store(state, int(store.get("id", 0)), store)
				cohort["hunger"] = clampi(int(cohort.get("hunger", 0)) + FOOD_SATISFACTION_GAIN, 0, NEED_SCALE)
				return


static func _apply_cohort_to_workers(state: GameState, cohort_key: String, cohort: Dictionary) -> void:
	var parts: PackedStringArray = cohort_key.split(":")
	if parts.size() != 2:
		return
	var player_id: String = str(parts[0])
	var worker_type: String = str(parts[1])
	for worker in EconomyState.all_workers(state):
		if str(worker.get("player_id", "")) != player_id:
			continue
		if str(worker.get("worker_type", "")) != worker_type:
			continue
		worker["needs_hunger"] = int(cohort.get("hunger", NEED_SCALE))
		worker["needs_tiredness"] = int(cohort.get("tiredness", 0))
		worker["needs_happiness"] = int(cohort.get("happiness", NEED_SCALE))
		worker["needs_safety"] = int(cohort.get("safety", NEED_SCALE))
		EconomyState.add_worker(state, worker)
