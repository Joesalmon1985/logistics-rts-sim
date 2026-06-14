class_name EconomyDiagnosticsSystem
extends RefCounted

## Snapshot diagnostics, stall detection, progress score, statistics.


static func tick(state: GameState) -> void:
	var diag := _build_diagnostics(state)
	state.diagnostics = diag
	_detect_stall(state, diag)


static func tick_statistics(state: GameState) -> void:
	var stats := {
		"tick": state.tick,
		"ware_totals": _ware_totals(state),
		"carrier_utilisation": _carrier_utilisation(state),
		"worker_utilisation": _worker_utilisation(state),
		"progress_score": int(EconomyState.get_meta_value(state, "progress_score", 0)),
	}
	EconomyState.set_meta_value(state, "statistics", stats)


static func _build_diagnostics(state: GameState) -> Dictionary:
	return {
		"tick": state.tick,
		"ware_totals": _ware_totals(state),
		"production": _production_diagnostics(state),
		"construction": _construction_diagnostics(state),
		"delivery": _delivery_diagnostics(state),
		"utilisation": {
			"carrier": _carrier_utilisation(state),
			"worker": _worker_utilisation(state),
		},
		"network": _network_diagnostics(state),
		"resources": {
			"forest": EconomyState.get_meta_value(state, "forest_summary", {}),
			"deposits": EconomyState.get_meta_value(state, "deposit_summary", {}),
		},
		"shortages": _shortage_diagnostics(state),
		"routing": EconomyState.get_meta_value(state, "blocked_routes", {}),
		"progress_score": int(EconomyState.get_meta_value(state, "progress_score", 0)),
		"stall": _stall_summary(state),
	}


static func _ware_totals(state: GameState) -> Dictionary:
	var totals := {"by_type": {}, "by_location": {}}
	var stores: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_WARE_STORES)
	for store_key in stores:
		var store: Dictionary = stores[store_key]
		totals["by_location"][store_key] = {}
		for ware_type in store.get("wares", {}):
			var amount: int = int(store["wares"][ware_type])
			totals["by_location"][store_key][ware_type] = amount
			totals["by_type"][ware_type] = int(totals["by_type"].get(ware_type, 0)) + amount
	return totals


static func _production_diagnostics(state: GameState) -> Array:
	var records: Array = []
	for building in EconomyState.all_buildings(state):
		records.append({
			"building_id": int(building.get("id", 0)),
			"building_type": str(building.get("building_type", "")),
			"state": str(building.get("state", "")),
			"paused_reason": str(building.get("paused_reason", "")),
			"workers": building.get("assigned_workers", []).size(),
		})
	return records


static func _construction_diagnostics(state: GameState) -> Dictionary:
	var active: int = 0
	var blocked: int = 0
	var sites: Array = []
	for site in EconomyState.all_construction_sites(state):
		var site_state: String = str(site.get("state", ""))
		if site_state == EconomyConstants.CONS_ACTIVE:
			active += 1
		if site_state == EconomyConstants.CONS_BLOCKED:
			blocked += 1
		var missing: Dictionary = {}
		for ware_type in site.get("required", {}):
			if ware_type == "builder_work":
				continue
			var need: int = int(site["required"][ware_type])
			var have: int = int(site.get("delivered", {}).get(ware_type, 0))
			if have < need:
				missing[ware_type] = need - have
		sites.append({
			"site_id": int(site.get("id", 0)),
			"building_type": str(site.get("building_type", "")),
			"state": site_state,
			"missing_wares": missing,
		})
	return {"active": active, "blocked": blocked, "sites": sites}


static func _delivery_diagnostics(state: GameState) -> Dictionary:
	var active: int = 0
	var blocked: int = 0
	var jobs: Array = []
	for job in EconomyState.all_delivery_jobs(state):
		var status: String = str(job.get("status", ""))
		if status in [EconomyConstants.JOB_DELIVERED, EconomyConstants.JOB_CANCELLED, EconomyConstants.JOB_FAILED]:
			continue
		active += 1
		if status in [EconomyConstants.JOB_BLOCKED, EconomyConstants.JOB_WAITING_ROUTE]:
			blocked += 1
		jobs.append({
			"job_id": int(job.get("id", 0)),
			"status": status,
			"ware_type": str(job.get("ware_type", "")),
			"block_reason": str(job.get("block_reason", "")),
		})
	return {"active": active, "blocked": blocked, "jobs": jobs}


static func _carrier_utilisation(state: GameState) -> Dictionary:
	var total: int = 0
	var busy: int = 0
	for carrier in EconomyState.all_carriers(state):
		total += 1
		if str(carrier.get("state", "")) != EconomyConstants.CARRIER_IDLE:
			busy += 1
	return {"total": total, "busy": busy, "idle": total - busy}


static func _worker_utilisation(state: GameState) -> Dictionary:
	var total: int = 0
	var assigned: int = 0
	for worker in EconomyState.all_workers(state):
		total += 1
		if str(worker.get("state", "")) == EconomyConstants.WORKER_ASSIGNED:
			assigned += 1
	return {"total": total, "assigned": assigned, "idle": total - assigned}


static func _network_diagnostics(state: GameState) -> Dictionary:
	var congested: int = 0
	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var limit: int = cfg.int_val("road_congestion_limit", 3)
	for key in roads:
		if int(roads[key].get("congestion", 0)) >= limit:
			congested += 1
	return {
		"road_segments": roads.size(),
		"road_nodes": EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES).size(),
		"congested_segments": congested,
		"connectivity_gaps": EconomyState.get_meta_value(state, "blocked_routes", {}),
	}


static func _shortage_diagnostics(state: GameState) -> Dictionary:
	var food_missing: int = 0
	var tool_missing: int = 0
	var worker_missing: int = 0
	for building in EconomyState.all_buildings(state):
		var reason: String = str(building.get("paused_reason", ""))
		if reason == "food_missing":
			food_missing += 1
		elif reason == "missing_worker":
			worker_missing += 1
	return {
		"food": food_missing,
		"tools": tool_missing,
		"workers": worker_missing,
	}


static func _stall_summary(state: GameState) -> Dictionary:
	return {
		"stall_ticks": int(EconomyState.get_meta_value(state, "stall_ticks", 0)),
		"primary_reason": str(EconomyState.get_meta_value(state, "stall_reason", "")),
		"last_progress_tick": int(EconomyState.get_meta_value(state, "last_progress_tick", 0)),
	}


static func _detect_stall(state: GameState, diag: Dictionary) -> void:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var threshold: int = cfg.int_val("stall_ticks_threshold", 50)
	var last_progress: int = int(EconomyState.get_meta_value(state, "last_progress_tick", 0))
	var stall_ticks: int = state.tick - last_progress
	EconomyState.set_meta_value(state, "stall_ticks", stall_ticks)

	if stall_ticks < threshold:
		EconomyState.set_meta_value(state, "stall_reason", "")
		return

	var reason: String = _primary_stall_reason(diag)
	EconomyState.set_meta_value(state, "stall_reason", reason)
	EconomyState.emit_event(state, "economy_stalled", {
		"stall_ticks": stall_ticks,
		"reason": reason,
	})


static func _primary_stall_reason(diag: Dictionary) -> String:
	var shortages: Dictionary = diag.get("shortages", {})
	if int(shortages.get("food", 0)) > 0:
		return "food_shortage"
	if int(shortages.get("workers", 0)) > 0:
		return "worker_shortage"
	var delivery: Dictionary = diag.get("delivery", {})
	if int(delivery.get("blocked", 0)) > 0:
		return "blocked_delivery"
	var construction: Dictionary = diag.get("construction", {})
	if int(construction.get("blocked", 0)) > 0:
		return "blocked_construction"
	return "no_progress"
