class_name EconomyConstructionSystem
extends RefCounted

## Construction site progress, material delivery, completion, cancellation.


static func tick(state: GameState) -> void:
	_request_missing_materials(state)
	for site in EconomyPrioritySystem.sort_construction_sites(EconomyState.all_construction_sites(state)):
		if str(site.get("state", "")) == EconomyConstants.CONS_BLOCKED and _materials_complete(site):
			site["state"] = EconomyConstants.CONS_ACTIVE
			EconomyState.add_construction_site(state, site)
		_tick_site(state, site)


static func create_site(
	state: GameState,
	building_type: String,
	anchor_q: int,
	anchor_r: int,
	player_id: String,
	upgrade_source_id: int = 0,
	custom_required: Dictionary = {},
) -> Dictionary:
	var check: Dictionary = EconomyMapRules.can_place_building(
		state,
		building_type,
		anchor_q,
		anchor_r,
		player_id,
		upgrade_source_id,
	)
	if not check.get("ok", false):
		return {"ok": false, "reason": str(check.get("reason", "REJECTED")), "site_id": 0}

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var required: Dictionary = custom_required if not custom_required.is_empty() else cfg.get_construction_cost(building_type)
	var site_id: int = state.entity_ids.allocate()
	var site := {
		"id": site_id,
		"building_type": building_type,
		"anchor_q": anchor_q,
		"anchor_r": anchor_r,
		"player_id": player_id,
		"state": EconomyConstants.CONS_ACTIVE,
		"delivered": {},
		"required": EconomyState.int_dict(required),
		"builder_work_remaining": int(required.get("builder_work", 0)),
		"priority": 0,
		"upgrade_source_id": upgrade_source_id,
	}
	EconomyState.add_construction_site(state, site)
	EconomyState.emit_event(state, "building_plan_placed", {
		"site_id": site_id,
		"building_type": building_type,
		"q": anchor_q,
		"r": anchor_r,
	})
	EconomyState.emit_event(state, "construction_started", {"site_id": site_id})
	return {"ok": true, "reason": "", "site_id": site_id}


static func cancel_site(state: GameState, site_id: int) -> Dictionary:
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	if site.is_empty():
		return {"ok": false, "reason": "SITE_NOT_FOUND"}
	_return_materials_to_hq(state, site)
	site["state"] = EconomyConstants.CONS_CANCELLED
	var sites: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_CONSTRUCTION_SITES)
	sites[str(site_id)] = site
	EconomyState.emit_event(state, "construction_cancelled", {"site_id": site_id})
	return {"ok": true, "reason": ""}


static func _request_missing_materials(state: GameState) -> void:
	for site in EconomyState.all_construction_sites(state):
		var site_state: String = str(site.get("state", ""))
		if site_state not in [EconomyConstants.CONS_ACTIVE, EconomyConstants.CONS_BLOCKED]:
			continue
		for ware_type in site.get("required", {}):
			if ware_type == "builder_work":
				continue
			var needed: int = int(site["required"][ware_type]) - int(site.get("delivered", {}).get(ware_type, 0))
			if needed <= 0:
				continue
			EconomyLogisticsSystem.request_delivery(
				state,
				str(ware_type),
				1,
				int(site.get("id", 0)),
				"construction_site",
				str(site.get("player_id", "p1")),
				int(site.get("priority", 0)),
			)


static func _tick_site(state: GameState, site: Dictionary) -> void:
	if str(site.get("state", "")) != EconomyConstants.CONS_ACTIVE:
		return

	if not _materials_complete(site):
		site["state"] = EconomyConstants.CONS_BLOCKED
		EconomyState.add_construction_site(state, site)
		return

	var remaining: int = int(site.get("builder_work_remaining", 0))
	if remaining > 0:
		site["builder_work_remaining"] = remaining - 1
		EconomyState.emit_event(state, "construction_progressed", {
			"site_id": int(site.get("id", 0)),
			"builder_work_remaining": site["builder_work_remaining"],
		})
		EconomyState.add_construction_site(state, site)
		if int(site["builder_work_remaining"]) > 0:
			return

	_complete_site(state, site)


static func _materials_complete(site: Dictionary) -> bool:
	for ware_type in site.get("required", {}):
		if ware_type == "builder_work":
			continue
		var needed: int = int(site["required"][ware_type])
		var delivered: int = int(site.get("delivered", {}).get(ware_type, 0))
		if delivered < needed:
			return false
	return true


static func deliver_to_site(state: GameState, site_id: int, ware_type: String, amount: int) -> bool:
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	if site.is_empty():
		return false
	if not site.get("delivered", {}).is_empty() or true:
		if not site.has("delivered"):
			site["delivered"] = {}
	site["delivered"][ware_type] = int(site["delivered"].get(ware_type, 0)) + amount
	EconomyState.add_construction_site(state, site)
	return true


static func _complete_site(state: GameState, site: Dictionary) -> void:
	var site_id: int = int(site.get("id", 0))
	var upgrade_source_id: int = int(site.get("upgrade_source_id", 0))
	var building_type: String = str(site.get("building_type", ""))
	var player_id: String = str(site.get("player_id", "p1"))
	var anchor_q: int = int(site.get("anchor_q", 0))
	var anchor_r: int = int(site.get("anchor_r", 0))

	if upgrade_source_id > 0:
		_apply_upgrade_completion(state, upgrade_source_id, building_type)
	elif HybridConfig.is_hybrid(state) and HybridState.get_config(state).is_military_building(building_type):
		HybridConstructionSystem.spawn_from_completed_site(state, site)
	else:
		EconomyInit._spawn_completed_building(state, building_type, anchor_q, anchor_r, player_id, EconomyState.get_config(state))

	site["state"] = EconomyConstants.CONS_COMPLETED
	var sites: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_CONSTRUCTION_SITES)
	sites[str(site_id)] = site
	EconomyState.emit_event(state, "construction_completed", {
		"site_id": site_id,
		"building_type": building_type,
	})


static func _apply_upgrade_completion(state: GameState, source_id: int, target_type: String) -> void:
	var source: Dictionary = EconomyState.get_building(state, source_id)
	if source.is_empty():
		return
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(target_type)
	source["building_type"] = target_type
	source["worker_type"] = str(bdef.get("worker_type", ""))
	source["assigned_workers"] = []
	source["production_progress"] = 0
	source["state"] = EconomyConstants.BLD_ACTIVE
	EconomyState.add_building(state, source)
	EconomyState.emit_event(state, "upgrade_completed", {
		"building_id": source_id,
		"target_type": target_type,
	})


static func _return_materials_to_hq(state: GameState, site: Dictionary) -> void:
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var store_id: int = int(hq.get("store_id", 0))
	var store: Dictionary = EconomyState.get_ware_store(state, store_id)
	for ware_type in site.get("delivered", {}):
		if ware_type == "builder_work":
			continue
		EconomyState.inventory_add(store, str(ware_type), int(site["delivered"][ware_type]))
	EconomyState.set_ware_store(state, store_id, store)
