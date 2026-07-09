class_name GodStrategyTick
extends RefCounted

## God-Strategy subsystem dispatch — ages, workers, pollution.


static func tick(state: GameState, rng: SeededRng) -> void:
	if not WorkerNeedsSystem.is_active(state):
		return
	AgeSystem.tick(state)
	WorkerNeedsSystem.tick(state)
	WorkerPolicySystem.tick(state, rng)
	WorkerSelectionSystem.tick(state, rng)
	PollutionSystem.tick(state)
	_check_age3_gate(state)


static func export_diagnostics(state: GameState) -> Dictionary:
	if not WorkerNeedsSystem.is_active(state):
		return {}
	return {
		"match": GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH),
		"worker_needs": WorkerNeedsSystem.export_snapshot(state),
		"worker_policy": WorkerPolicySystem.export_snapshot(state),
		"worker_selection": WorkerSelectionSystem.export_snapshot(state),
		"pollution": PollutionSystem.export_snapshot(state),
	}


static func _check_age3_gate(state: GameState) -> void:
	var result: Dictionary = AgeSystem.resolve_age3_gate(state)
	if str(result.get("action", "")) == "win":
		EconomyState.set_meta_value(state, "god_strategy_pending_winner", str(result.get("winner_player_id", "")))
		EconomyState.set_meta_value(state, "god_strategy_end_reason", "age3_vp")
