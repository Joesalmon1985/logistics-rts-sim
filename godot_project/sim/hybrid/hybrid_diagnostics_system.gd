class_name HybridDiagnosticsSystem
extends RefCounted

## Combined economy + military stall diagnostics (HYB-DIAG-001).


static func tick(state: GameState) -> void:
	if not HybridConfig.is_hybrid(state):
		return
	state.diagnostics["hybrid"] = {
		"delegated_orders": HybridState.all_delegated_orders(state).size(),
		"economy_stall_ticks": int(EconomyState.get_meta_value(state, "stall_ticks", 0)),
		"military_stall_ticks": int(MilitaryState.get_meta_value(state, "stall_ticks", 0)),
		"hybrid_stall_ticks": int(HybridState.get_bucket(state, HybridConstants.BUCKET_META).get("stall_ticks", 0)),
	}
