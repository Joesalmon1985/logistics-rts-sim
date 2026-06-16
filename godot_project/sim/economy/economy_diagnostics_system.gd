class_name EconomyDiagnosticsSystem
extends RefCounted

## Snapshot diagnostics, stall detection, progress score, statistics.


static func tick(state: GameState, mode: String = "full") -> void:
	if mode == "bot":
		_tick_bot(state)
		return
	var diag := _build_diagnostics(state)
	state.diagnostics = diag
	_detect_stall(state, diag)


static func _tick_bot(state: GameState) -> void:
	var components: Dictionary = _economy_score_components(state)
	var economy_scores: Dictionary = {}
	for player_id in components:
		economy_scores[player_id] = _compute_economy_score_from_components(components[player_id])
	var diag: Dictionary = {
		"tick": state.tick,
		"economy_scores": economy_scores,
		"economy_score_components": components,
		"shortages": _shortage_diagnostics(state),
		"stall": _stall_summary(state),
		"player_stalls": {},
	}
	state.diagnostics = diag
	_detect_stall(state, diag)
	diag["player_stalls"] = _player_stall_summary(state, economy_scores)
	state.diagnostics = diag


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
	var economy_scores: Dictionary = _compute_economy_scores(state)
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
		"economy_scores": economy_scores,
		"economy_score_components": _economy_score_components(state),
		"stall": _stall_summary(state),
		"player_stalls": _player_stall_summary(state, economy_scores),
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
		elif reason == "tool_missing":
			tool_missing += 1
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
	var threshold: int = int(EconomyState.get_meta_value(state, "stall_ticks_threshold", cfg.int_val("stall_ticks_threshold", 50)))
	var economy_scores: Dictionary = diag.get("economy_scores", {})
	var score_history: Variant = EconomyState.get_meta_value(state, "player_score_history", {})
	if typeof(score_history) != TYPE_DICTIONARY:
		score_history = {}

	var global_stall: int = 0
	for player_id in economy_scores:
		var score: int = int(economy_scores[player_id])
		if not score_history.has(player_id):
			score_history[player_id] = {"score": score, "last_change_tick": state.tick, "stall_ticks": 0}
			continue
		var entry: Dictionary = score_history[player_id]
		if int(entry.get("score", -1)) != score:
			entry["score"] = score
			entry["last_change_tick"] = state.tick
			entry["stall_ticks"] = 0
		else:
			entry["stall_ticks"] = state.tick - int(entry.get("last_change_tick", state.tick))
		score_history[player_id] = entry
		global_stall = maxi(global_stall, int(entry.get("stall_ticks", 0)))

	EconomyState.set_meta_value(state, "player_score_history", score_history)
	EconomyState.set_meta_value(state, "stall_ticks", global_stall)

	if global_stall < threshold:
		EconomyState.set_meta_value(state, "stall_reason", "")
		return

	var reason: String = _primary_stall_reason(diag)
	EconomyState.set_meta_value(state, "stall_reason", reason)
	EconomyState.emit_event(state, "economy_stalled", {
		"stall_ticks": global_stall,
		"reason": reason,
		"player_score_history": score_history,
	})


const ACTIVE_PRODUCTION_WINDOW: int = 50


static func _player_ids(state: GameState) -> Array:
	var ids: Array = []
	for player in state.players:
		ids.append(str(player.player_id))
	if ids.is_empty():
		ids.append(str(EconomyState.get_meta_value(state, "primary_player_id", "p1")))
	return ids


static func _is_scored_building(building_type: String) -> bool:
	if building_type in ["headquarters", "decorative_marker", ""]:
		return false
	return true


static func _completed_buildings_for_player(state: GameState, player_id: String) -> int:
	var count: int = 0
	var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if int(building.get("id", 0)) == hq_id:
			continue
		var btype: String = str(building.get("building_type", ""))
		if not _is_scored_building(btype):
			continue
		if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
			count += 1
	return count


static func _active_production_buildings_for_player(state: GameState, player_id: String) -> int:
	var count: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		var btype: String = str(building.get("building_type", ""))
		if not _is_scored_building(btype):
			continue
		var cfg: EconomyConfig = EconomyState.get_config(state)
		var bdef: Dictionary = cfg.get_building_def(btype)
		if str(bdef.get("recipe_id", "")) == "":
			continue
		var last_tick: int = int(building.get("last_production_tick", -999999))
		if state.tick - last_tick <= ACTIVE_PRODUCTION_WINDOW:
			count += 1
	return count


static func _stock_value_for_player(state: GameState, player_id: String) -> int:
	var total: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0:
			continue
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		total += EconomyState.inventory_total(store)
	return total


static func _blocked_jobs_for_player(state: GameState, player_id: String) -> int:
	var count: int = 0
	for job in EconomyState.all_delivery_jobs(state):
		if str(job.get("player_id", "")) != player_id:
			continue
		var status: String = str(job.get("status", ""))
		if status in [EconomyConstants.JOB_BLOCKED, EconomyConstants.JOB_WAITING_ROUTE, EconomyConstants.JOB_WAITING_WARE]:
			count += 1
	return count


static func _economy_score_components(state: GameState) -> Dictionary:
	var player_ids: Array = _player_ids(state)
	var components: Dictionary = {}
	var hq_ids: Dictionary = {}
	for player_id in player_ids:
		var pid: String = str(player_id)
		hq_ids[pid] = EconomyState.get_hq_building_id(state, pid)
		components[pid] = {
			"completed_buildings": 0,
			"active_production_buildings": 0,
			"total_stock_value": 0,
			"completed_deliveries_count": EconomyState.get_player_stat(state, pid, "completed_deliveries_count"),
			"blocked_jobs_count": 0,
		}

	var cfg: EconomyConfig = EconomyState.get_config(state)
	for building in EconomyState.all_buildings(state):
		var pid: String = str(building.get("player_id", ""))
		if not components.has(pid):
			continue
		var comp: Dictionary = components[pid]
		var btype: String = str(building.get("building_type", ""))
		if not _is_scored_building(btype):
			continue
		if int(building.get("id", 0)) != int(hq_ids.get(pid, -1)) and str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
			comp["completed_buildings"] = int(comp.get("completed_buildings", 0)) + 1
		var bdef: Dictionary = cfg.get_building_def(btype)
		if str(bdef.get("recipe_id", "")) != "":
			var last_tick: int = int(building.get("last_production_tick", -999999))
			if state.tick - last_tick <= ACTIVE_PRODUCTION_WINDOW:
				comp["active_production_buildings"] = int(comp.get("active_production_buildings", 0)) + 1
		var store_id: int = int(building.get("store_id", 0))
		if store_id > 0:
			var store: Dictionary = EconomyState.get_ware_store(state, store_id)
			comp["total_stock_value"] = int(comp.get("total_stock_value", 0)) + EconomyState.inventory_total(store)

	for job in EconomyState.all_delivery_jobs(state):
		var pid: String = str(job.get("player_id", ""))
		if not components.has(pid):
			continue
		var status: String = str(job.get("status", ""))
		if status in [EconomyConstants.JOB_BLOCKED, EconomyConstants.JOB_WAITING_ROUTE, EconomyConstants.JOB_WAITING_WARE]:
			var comp: Dictionary = components[pid]
			comp["blocked_jobs_count"] = int(comp.get("blocked_jobs_count", 0)) + 1

	return components


static func _compute_economy_score_from_components(components: Dictionary) -> int:
	return (
		10 * int(components.get("completed_buildings", 0))
		+ 5 * int(components.get("active_production_buildings", 0))
		+ int(floor(float(components.get("total_stock_value", 0)) / 10.0))
		+ int(components.get("completed_deliveries_count", 0))
		- 2 * int(components.get("blocked_jobs_count", 0))
	)


static func _compute_economy_scores(state: GameState) -> Dictionary:
	var scores: Dictionary = {}
	var all_components: Dictionary = _economy_score_components(state)
	for player_id in all_components:
		scores[player_id] = _compute_economy_score_from_components(all_components[player_id])
	return scores


static func _player_stall_summary(state: GameState, economy_scores: Dictionary) -> Dictionary:
	var summary: Dictionary = {}
	var history: Variant = EconomyState.get_meta_value(state, "player_score_history", {})
	if typeof(history) != TYPE_DICTIONARY:
		history = {}
	for player_id in economy_scores:
		var entry: Dictionary = history.get(player_id, {})
		summary[player_id] = {
			"score": int(economy_scores.get(player_id, 0)),
			"stall_ticks": int(entry.get("stall_ticks", 0)),
			"last_change_tick": int(entry.get("last_change_tick", state.tick)),
		}
	return summary


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
