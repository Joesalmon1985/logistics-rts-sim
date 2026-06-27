class_name HybridTick
extends RefCounted

## Composed tick order for hybrid variant: economy + military + integration.


static func get_subsystem_order() -> Array[String]:
	return [
		"map",
		"forest",
		"deposits",
		"worker_requests",
		"production",
		"construction",
		"upgrades",
		"carriers",
		"mil_meta",
		"mil_power",
		"mil_tech",
		"mil_production",
		"mil_orders",
		"mil_movement",
		"mil_vision",
		"mil_combat",
		"mil_damage",
		"hybrid_repair",
		"mil_victory",
		"hybrid",
		"diagnostics",
		"statistics",
	]


static func step_subsystem(name: String, state: GameState, rng: SeededRng) -> void:
	match name:
		"mil_meta", "mil_power", "mil_tech", "mil_production", "mil_orders", "mil_movement", "mil_vision", "mil_combat", "mil_damage", "mil_victory":
			MilitaryTick.step_subsystem(name.substr(4), state, rng)
		"hybrid_repair":
			HybridRepairSystem.tick(state)
		"hybrid":
			HybridOrderSystem.tick(state)
			_tick_stall_detection(state)
		"diagnostics":
			EconomyDiagnosticsSystem.tick(state)
			MilitaryDiagnosticsSystem.tick(state)
			HybridDiagnosticsSystem.tick(state)
		"statistics":
			EconomyDiagnosticsSystem.tick_statistics(state)
			MilitaryDiagnosticsSystem.tick_statistics(state)
		_:
			EconomyTick.step_subsystem(name, state, rng)


static func _tick_stall_detection(state: GameState) -> void:
	var progress: int = MilitaryState.all_units(state).size() + MilitaryState.all_buildings(state).size()
	var last: int = int(HybridState.get_bucket(state, HybridConstants.BUCKET_META).get("last_progress_score", -1))
	if progress != last:
		HybridState.get_bucket(state, HybridConstants.BUCKET_META)["last_progress_score"] = progress
		HybridState.get_bucket(state, HybridConstants.BUCKET_META)["stall_ticks"] = 0
		HybridState.get_bucket(state, HybridConstants.BUCKET_META)["last_progress_tick"] = state.tick
	else:
		var stall: int = int(HybridState.get_bucket(state, HybridConstants.BUCKET_META).get("stall_ticks", 0)) + 1
		HybridState.get_bucket(state, HybridConstants.BUCKET_META)["stall_ticks"] = stall
		var threshold: int = int(EconomyState.get_meta_value(state, "stall_ticks_threshold", 200))
		if stall >= threshold:
			HybridState.emit_event(state, "hybrid_stalled", {"stall_ticks": stall})
