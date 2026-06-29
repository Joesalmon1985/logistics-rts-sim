class_name AgeSystem
extends RefCounted


static func tick(state: GameState) -> void:
	if GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH).is_empty():
		GodStrategyState.init_match(state)
	var interval: int = int(
		GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH).get(
			"draft_interval_ticks",
			GodStrategyConstants.DRAFT_INTERVAL_TICKS,
		)
	)
	if interval <= 0:
		return
	if state.tick > 0 and state.tick % interval == 0:
		CultureDraftSystem.start_draft_for_current_age(state)


static func advance_age(state: GameState) -> String:
	var match_bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH)
	var next_index: int = int(match_bucket.get("current_age_index", 0)) + 1
	if next_index >= GodStrategyConstants.AGES.size():
		next_index = GodStrategyConstants.AGES.size() - 1
	match_bucket["current_age_index"] = next_index
	match_bucket["current_age_id"] = GodStrategyConstants.AGES[next_index]
	return str(match_bucket["current_age_id"])


static func resolve_age3_gate(state: GameState) -> Dictionary:
	if GodStrategyState.current_age_id(state) != "modern":
		return {"action": "none"}
	var result: Dictionary = VictoryPointSystem.evaluate_age3_end(state)
	if bool(result.get("match_over", false)):
		return {"action": "win", "winner_player_id": str(result.get("winner_player_id", ""))}
	if bool(result.get("advance_to_age4", false)):
		advance_age(state)
		return {"action": "advance_age4"}
	return {"action": "continue"}
