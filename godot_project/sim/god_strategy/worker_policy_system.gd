class_name WorkerPolicySystem
extends RefCounted

## Applies pluggable worker policies on refresh interval.

const BUCKET_POLICY: String = "god_strategy_worker_policy"
const REFRESH_TICKS: int = 300


static func init_match(state: GameState, scenario: Dictionary = {}) -> void:
	if not WorkerNeedsSystem.is_active(state):
		return
	var bucket: Dictionary = _bucket(state)
	bucket.clear()
	bucket["last_refresh_tick"] = -1
	bucket["action_log"] = []
	bucket["policy_mode"] = str(scenario.get("worker_policy_mode", "deterministic"))
	bucket["perf_ticks"] = 0
	bucket["perf_policy_calls"] = 0


static func tick(state: GameState, rng: SeededRng) -> void:
	if not WorkerNeedsSystem.is_active(state):
		return
	var bucket: Dictionary = _bucket(state)
	var last: int = int(bucket.get("last_refresh_tick", -1))
	if state.tick - last < REFRESH_TICKS and last >= 0:
		return
	bucket["last_refresh_tick"] = state.tick
	var t0: int = Time.get_ticks_usec()
	var actions: Array = []
	for worker in EconomyState.all_workers(state):
		var pid: String = str(worker.get("player_id", ""))
		var wtype: String = str(worker.get("worker_type", ""))
		if pid == "" or wtype == "":
			continue
		var obs: Dictionary = WorkerPolicyTypes.build_observation(state, pid, wtype)
		var mask: Dictionary = WorkerPolicyTypes.build_legal_mask(obs)
		var action: String = _decide_action(bucket, obs, mask, rng)
		if not WorkerPolicyTypes.is_action_legal(mask, action):
			continue
		_apply_action(state, pid, wtype, action)
		actions.append({"player_id": pid, "worker_type": wtype, "action": action, "tick": state.tick})
	var t1: int = Time.get_ticks_usec()
	bucket["perf_ticks"] = int(bucket.get("perf_ticks", 0)) + 1
	bucket["perf_policy_calls"] = int(bucket.get("perf_policy_calls", 0)) + actions.size()
	bucket["last_refresh_us"] = t1 - t0
	var log: Array = bucket.get("action_log", [])
	for entry in actions:
		log.append(entry)
	if log.size() > 200:
		log = log.slice(log.size() - 200)
	bucket["action_log"] = log


static func get_perf_note(state: GameState) -> Dictionary:
	var bucket: Dictionary = _bucket(state)
	var ticks: int = maxi(int(bucket.get("perf_ticks", 0)), 1)
	var calls: int = int(bucket.get("perf_policy_calls", 0))
	return {
		"refresh_ticks": REFRESH_TICKS,
		"policy_refresh_count": ticks,
		"total_policy_calls": calls,
		"avg_calls_per_refresh": calls / ticks,
		"last_refresh_us": int(bucket.get("last_refresh_us", 0)),
	}


static func export_snapshot(state: GameState) -> Dictionary:
	if not WorkerNeedsSystem.is_active(state):
		return {}
	var bucket: Dictionary = _bucket(state)
	return {
		"policy_mode": str(bucket.get("policy_mode", "deterministic")),
		"last_refresh_tick": int(bucket.get("last_refresh_tick", -1)),
		"recent_actions": (bucket.get("action_log", []) as Array).slice(-20),
		"perf": get_perf_note(state),
	}


static func _decide_action(bucket: Dictionary, obs: Dictionary, mask: Dictionary, rng: SeededRng) -> String:
	var mode: String = str(bucket.get("policy_mode", "deterministic"))
	if mode == "random_legal_test":
		return RandomLegalTestWorkerPolicy.decide(obs, mask, rng)
	return DeterministicWorkerPolicy.decide(obs, mask)


static func _apply_action(state: GameState, player_id: String, worker_type: String, action: String) -> void:
	match action:
		WorkerPolicyTypes.ACTION_SEEK_FOOD:
			WorkerNeedsSystem.apply_seek_food_action(state, player_id, worker_type)
		WorkerPolicyTypes.ACTION_REST:
			WorkerNeedsSystem.apply_rest_action(state, player_id, worker_type)
		WorkerPolicyTypes.ACTION_SEEK_LEISURE:
			var bucket: Dictionary = WorkerNeedsSystem.get_cohort(state, player_id, worker_type)
			bucket["happiness"] = clampi(int(bucket.get("happiness", 0)) + 120, 0, WorkerNeedsSystem.NEED_SCALE)
		WorkerPolicyTypes.ACTION_SEEK_SAFETY:
			var safe_bucket: Dictionary = WorkerNeedsSystem.get_cohort(state, player_id, worker_type)
			safe_bucket["safety"] = clampi(int(safe_bucket.get("safety", 0)) + 150, 0, WorkerNeedsSystem.NEED_SCALE)
		_:
			pass


static func _bucket(state: GameState) -> Dictionary:
	if not state.entities.has(BUCKET_POLICY):
		state.entities[BUCKET_POLICY] = {}
	return state.entities[BUCKET_POLICY]
