class_name PollutionSystem
extends RefCounted

## Local + global pollution for Ages 3–4; non-linear harm curve.

const BUCKET_POLLUTION: String = "god_strategy_pollution"
const GLOBAL_COLLAPSE_THRESHOLD: int = 9000
const RESTORATION_VICTORY_THRESHOLD: int = 2000
const EMIT_PER_INDUSTRIAL_TICK: int = 15
const LOCAL_DECAY: int = 1
const GLOBAL_HARM_EXPONENT: int = 2


static func init_match(state: GameState, scenario: Dictionary = {}) -> void:
	if not WorkerNeedsSystem.is_active(state):
		return
	var bucket: Dictionary = _bucket(state)
	bucket.clear()
	bucket["global_pollution"] = int(scenario.get("initial_global_pollution", 0))
	bucket["local"] = {}
	bucket["restoration_score"] = 0
	bucket["industrial_buildings"] = _count_industrial(state)


static func tick(state: GameState) -> void:
	if not WorkerNeedsSystem.is_active(state):
		return
	if GodStrategyState.current_age_index(state) < 2:
		return
	var bucket: Dictionary = _bucket(state)
	var local: Dictionary = bucket.get("local", {})
	var global_poll: int = int(bucket.get("global_pollution", 0))
	for building in EconomyState.all_buildings(state):
		if not _is_industrial(building):
			continue
		var q: int = int(building.get("anchor_q", 0))
		var r: int = int(building.get("anchor_r", 0))
		var key: String = "%d,%d" % [q, r]
		local[key] = int(local.get(key, 0)) + EMIT_PER_INDUSTRIAL_TICK
		global_poll += EMIT_PER_INDUSTRIAL_TICK
	for key in local.keys():
		local[key] = maxi(int(local[key]) - LOCAL_DECAY, 0)
	bucket["local"] = local
	bucket["global_pollution"] = global_poll
	_apply_forest_harm(state, local)
	_apply_worker_safety_penalty(state, local)


static func worker_throughput_penalty(state: GameState, player_id: String) -> int:
	if not WorkerNeedsSystem.is_active(state):
		return 0
	var bucket: Dictionary = _bucket(state)
	var global_poll: int = int(bucket.get("global_pollution", 0))
	if global_poll < 1000:
		return 0
	var harm: int = (global_poll * global_poll) / 1000000
	return clampi(harm / 4, 0, 400)


static func apply_restoration(state: GameState, amount: int) -> void:
	var bucket: Dictionary = _bucket(state)
	bucket["restoration_score"] = int(bucket.get("restoration_score", 0)) + amount
	bucket["global_pollution"] = maxi(int(bucket.get("global_pollution", 0)) - amount * 2, 0)


static func is_shared_collapse(state: GameState) -> bool:
	var bucket: Dictionary = _bucket(state)
	return int(bucket.get("global_pollution", 0)) >= GLOBAL_COLLAPSE_THRESHOLD


static func restoration_victory_leader(state: GameState) -> String:
	if GodStrategyState.current_age_id(state) != "futuristic_collapse":
		return ""
	var bucket: Dictionary = _bucket(state)
	if int(bucket.get("global_pollution", 0)) > RESTORATION_VICTORY_THRESHOLD:
		return ""
	if int(bucket.get("restoration_score", 0)) < 5000:
		return ""
	var best_pid: String = ""
	var best: int = -1
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "":
			continue
		if not _player_alive(state, pid):
			continue
		var score: int = int(bucket.get("restoration_score", 0)) + VictoryPointSystem.get_vp(state, pid) * 100
		if score > best:
			best = score
			best_pid = pid
	return best_pid


static func export_snapshot(state: GameState) -> Dictionary:
	if not WorkerNeedsSystem.is_active(state):
		return {}
	var bucket: Dictionary = _bucket(state)
	return {
		"global_pollution": int(bucket.get("global_pollution", 0)),
		"local_hotspots": bucket.get("local", {}),
		"restoration_score": int(bucket.get("restoration_score", 0)),
		"collapse_imminent": is_shared_collapse(state),
	}


static func set_global_pollution(state: GameState, value: int) -> void:
	_bucket(state)["global_pollution"] = clampi(value, 0, 20000)


static func _is_industrial(building: Dictionary) -> bool:
	var btype: String = str(building.get("building_type", ""))
	return btype in ["sawmill", "quarry", "smelter", "factory"]


static func _count_industrial(state: GameState) -> int:
	var n: int = 0
	for building in EconomyState.all_buildings(state):
		if _is_industrial(building):
			n += 1
	return n


static func _apply_forest_harm(state: GameState, local: Dictionary) -> void:
	for key in local.keys():
		if int(local[key]) < 500:
			continue
		var parts: PackedStringArray = key.split(",")
		if parts.size() != 2:
			continue
		var tile: HexTile = state.map.get_tile(HexCoord.new(int(parts[0]), int(parts[1])))
		if tile == null:
			continue
		if tile.terrain_type == "forest" or tile.deposit_type == "forest":
			tile.tree_density = maxi(int(tile.tree_density) - 2, 0)


static func _apply_worker_safety_penalty(state: GameState, local: Dictionary) -> void:
	for worker in EconomyState.all_workers(state):
		var pid: String = str(worker.get("player_id", ""))
		var wtype: String = str(worker.get("worker_type", ""))
		if pid == "" or wtype == "":
			continue
		var cohort: Dictionary = WorkerNeedsSystem.get_cohort(state, pid, wtype)
		var penalty: int = worker_throughput_penalty(state, pid)
		if penalty > 0:
			cohort["safety"] = clampi(int(cohort.get("safety", WorkerNeedsSystem.NEED_SCALE)) - maxi(1, penalty / 5), 0, WorkerNeedsSystem.NEED_SCALE)


static func _bucket(state: GameState) -> Dictionary:
	if not state.entities.has(BUCKET_POLLUTION):
		state.entities[BUCKET_POLLUTION] = {}
	return state.entities[BUCKET_POLLUTION]


static func _player_alive(state: GameState, player_id: String) -> bool:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != "command_hub":
			continue
		if str(building.get("state", "")) != MilitaryConstants.BLD_DESTROYED:
			return true
	return false
