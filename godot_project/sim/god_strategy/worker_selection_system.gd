class_name WorkerSelectionSystem
extends RefCounted

## Societal selection — trait prevalence shifts from fitness + culture.

const BUCKET_SELECTION: String = "god_strategy_worker_selection"
const REFRESH_TICKS: int = 300


static func init_match(state: GameState, scenario: Dictionary = {}) -> void:
	if not WorkerNeedsSystem.is_active(state):
		return
	var bucket: Dictionary = _bucket(state)
	bucket.clear()
	bucket["last_refresh_tick"] = -1
	bucket["cohort_traits"] = {}
	bucket["behaviour_histogram"] = {}
	bucket["selection_events"] = []
	bucket["culture_path"] = {}
	for pid in scenario.get("players", ["p1"]):
		bucket["culture_path"][str(pid)] = str(scenario.get("culture_path_%s" % pid, "balanced"))
	_init_traits(state, bucket)


static func tick(state: GameState, rng: SeededRng) -> void:
	if not WorkerNeedsSystem.is_active(state):
		return
	var bucket: Dictionary = _bucket(state)
	var last: int = int(bucket.get("last_refresh_tick", -1))
	if state.tick - last < REFRESH_TICKS and last >= 0:
		return
	bucket["last_refresh_tick"] = state.tick
	_run_selection(state, bucket, rng)
	_update_histogram(state, bucket)


static func get_cohort_traits(state: GameState, player_id: String, worker_type: String) -> Dictionary:
	var bucket: Dictionary = _bucket(state)
	var traits: Dictionary = bucket.get("cohort_traits", {})
	return traits.get(_key(player_id, worker_type), _default_traits(player_id, bucket)).duplicate()


static func set_culture_path(state: GameState, player_id: String, path: String) -> void:
	var bucket: Dictionary = _bucket(state)
	var paths: Dictionary = bucket.get("culture_path", {})
	paths[player_id] = path
	bucket["culture_path"] = paths
	_init_traits(state, bucket)


static func export_snapshot(state: GameState) -> Dictionary:
	if not WorkerNeedsSystem.is_active(state):
		return {}
	var bucket: Dictionary = _bucket(state)
	return {
		"cohort_traits": bucket.get("cohort_traits", {}),
		"behaviour_histogram": bucket.get("behaviour_histogram", {}),
		"culture_paths": bucket.get("culture_path", {}),
		"recent_selection_events": (bucket.get("selection_events", []) as Array).slice(-20),
	}


static func _run_selection(state: GameState, bucket: Dictionary, rng: SeededRng) -> void:
	var traits: Dictionary = bucket.get("cohort_traits", {})
	var events: Array = bucket.get("selection_events", [])
	for key in traits.keys():
		var parts: PackedStringArray = key.split(":")
		if parts.size() != 2:
			continue
		var pid: String = str(parts[0])
		var wtype: String = str(parts[1])
		var fitness: int = _score_fitness(state, pid, wtype)
		var current: Dictionary = traits[key]
		var culture: String = str(bucket.get("culture_path", {}).get(pid, "balanced"))
		var target: Dictionary = _culture_target_traits(culture)
		var blend: int = 80 + rng.next_int(0, 39)
		for trait_name in ["productivity", "welfare", "safety_preference"]:
			var cur: int = int(current.get(trait_name, 500))
			var tgt: int = int(target.get(trait_name, 500))
			var fitness_bias: int = 0
			if trait_name == "productivity" and fitness > 600:
				fitness_bias = 40
			if trait_name == "welfare" and fitness < 400:
				fitness_bias = 40
			current[trait_name] = clampi((cur * (1000 - blend) + (tgt + fitness_bias) * blend) / 1000, 0, 1000)
		traits[key] = current
		events.append({
			"tick": state.tick,
			"cohort": key,
			"fitness": fitness,
			"traits": current.duplicate(),
			"culture_path": culture,
		})
	bucket["cohort_traits"] = traits
	if events.size() > 300:
		events = events.slice(events.size() - 300)
	bucket["selection_events"] = events


static func _score_fitness(state: GameState, player_id: String, worker_type: String) -> int:
	var mult: int = WorkerNeedsSystem.get_throughput_multiplier(state, player_id, worker_type)
	var avg: Dictionary = WorkerNeedsSystem.get_cohort_averages(state, player_id)
	var welfare: int = int(avg.get("happiness", 500))
	return (mult + welfare) / 2


static func _culture_target_traits(path: String) -> Dictionary:
	match path:
		"industrial":
			return {"productivity": 900, "welfare": 300, "safety_preference": 400}
		"welfare":
			return {"productivity": 400, "welfare": 900, "safety_preference": 700}
		_:
			return {"productivity": 600, "welfare": 600, "safety_preference": 600}


static func _update_histogram(state: GameState, bucket: Dictionary) -> void:
	var hist: Dictionary = {}
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "":
			continue
		var traits: Dictionary = bucket.get("cohort_traits", {})
		var prod_sum: int = 0
		var count: int = 0
		for key in traits.keys():
			if not str(key).begins_with("%s:" % pid):
				continue
			prod_sum += int(traits[key].get("productivity", 500))
			count += 1
		if count > 0:
			hist[pid] = {"avg_productivity_trait": prod_sum / count, "cohorts": count}
	bucket["behaviour_histogram"] = hist


static func _init_traits(state: GameState, bucket: Dictionary) -> void:
	var traits: Dictionary = bucket.get("cohort_traits", {})
	for worker in EconomyState.all_workers(state):
		var pid: String = str(worker.get("player_id", ""))
		var wtype: String = str(worker.get("worker_type", ""))
		if pid == "" or wtype == "":
			continue
		var key: String = _key(pid, wtype)
		if not traits.has(key):
			traits[key] = _default_traits(pid, bucket)
	bucket["cohort_traits"] = traits


static func _default_traits(player_id: String, bucket: Dictionary) -> Dictionary:
	var culture: String = str(bucket.get("culture_path", {}).get(player_id, "balanced"))
	return _culture_target_traits(culture)


static func _key(player_id: String, worker_type: String) -> String:
	return "%s:%s" % [player_id, worker_type]


static func _bucket(state: GameState) -> Dictionary:
	if not state.entities.has(BUCKET_SELECTION):
		state.entities[BUCKET_SELECTION] = {}
	return state.entities[BUCKET_SELECTION]
