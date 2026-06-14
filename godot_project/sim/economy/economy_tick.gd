class_name EconomyTick
extends RefCounted

## Per-tick economy subsystem dispatch.


static func step_subsystem(name: String, state: GameState, _rng: SeededRng) -> void:
	match name:
		"map":
			EconomyMapRules.sync_road_segment_refs(state)
		"forest":
			EconomyProductionSystem.tick_forest(state)
		"deposits":
			EconomyProductionSystem.tick_deposits(state)
		"worker_requests":
			EconomyWorkerSystem.tick(state)
		"production":
			EconomyProductionSystem.tick(state)
			_request_production_inputs(state)
		"construction":
			EconomyConstructionSystem.tick(state)
		"upgrades":
			EconomyUpgradeSystem.tick(state)
		"carriers":
			EconomyLogisticsSystem.tick(state)
		"diagnostics":
			EconomyDiagnosticsSystem.tick(state)
		"statistics":
			EconomyDiagnosticsSystem.tick_statistics(state)
		"economy":
			_step_full_economy(state)
		_:
			push_warning("EconomyTick: unknown subsystem %s" % name)


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
		"diagnostics",
		"statistics",
	]


static func _step_full_economy(state: GameState) -> void:
	for subsystem_name in get_subsystem_order():
		if subsystem_name == "diagnostics":
			continue
		step_subsystem(subsystem_name, state, null)


static func _request_production_inputs(state: GameState) -> void:
	for building in EconomyState.all_buildings(state):
		var state_name: String = str(building.get("state", ""))
		if state_name in [
			EconomyConstants.BLD_PLANNED,
			EconomyConstants.BLD_UNDER_CONSTRUCTION,
			EconomyConstants.BLD_DISABLED,
		]:
			continue
		EconomyLogisticsSystem.request_building_input(state, building)
