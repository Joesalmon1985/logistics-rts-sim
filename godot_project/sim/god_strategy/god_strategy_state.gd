class_name GodStrategyState
extends RefCounted

static func ensure_buckets(state: GameState) -> void:
	for bucket in [GodStrategyConstants.BUCKET_MATCH, GodStrategyConstants.BUCKET_DRAFT, GodStrategyConstants.BUCKET_UNLOCKS]:
		if not state.entities.has(bucket):
			state.entities[bucket] = {}


static func get_bucket(state: GameState, bucket: String) -> Dictionary:
	ensure_buckets(state)
	return state.entities[bucket]


static func init_match(state: GameState, scenario: Dictionary = {}) -> void:
	ensure_buckets(state)
	var match_bucket: Dictionary = get_bucket(state, GodStrategyConstants.BUCKET_MATCH)
	match_bucket.clear()
	match_bucket["current_age_index"] = 0
	match_bucket["current_age_id"] = GodStrategyConstants.AGES[0]
	match_bucket["draft_interval_ticks"] = int(scenario.get("draft_interval_ticks", GodStrategyConstants.DRAFT_INTERVAL_TICKS))
	match_bucket["last_draft_tick"] = -1
	match_bucket["age3_resolved"] = 0
	for pid in scenario.get("players", ["p1"]):
		VictoryPointSystem.set_vp(state, str(pid), 0)


static func current_age_id(state: GameState) -> String:
	var match_bucket: Dictionary = get_bucket(state, GodStrategyConstants.BUCKET_MATCH)
	return str(match_bucket.get("current_age_id", GodStrategyConstants.AGES[0]))


static func current_age_index(state: GameState) -> int:
	return int(get_bucket(state, GodStrategyConstants.BUCKET_MATCH).get("current_age_index", 0))
