class_name EconomySpriteStateSelector
extends RefCounted

## Maps authoritative render-data and diagnostics to placeholder animation names (RUN_05C.2).


static func select_states(
	render_data: Dictionary,
	snapshot: Dictionary,
	diagnostics_prev: Dictionary,
	effect_events: Array,
) -> Dictionary:
	var states: Dictionary = {}
	_apply_carriers(render_data, states)
	_apply_ware_routes(render_data, states)
	_apply_construction(render_data, states)
	_apply_buildings(render_data, snapshot, states)
	_apply_stores(render_data, diagnostics_prev, states)
	_apply_road_flow(render_data, states)
	_apply_blocked(render_data, snapshot, states)
	_apply_event_pulses(effect_events, states)
	return states


static func _apply_carriers(render_data: Dictionary, states: Dictionary) -> void:
	for carrier in render_data.get("carriers", []):
		if typeof(carrier) != TYPE_DICTIONARY:
			continue
		var key: String = "carrier:%d" % int(carrier.get("id", 0))
		var carrier_state: String = str(carrier.get("state", "")).to_lower()
		var ware_type: String = str(carrier.get("ware_type", ""))
		var ware_amount: int = int(carrier.get("ware_amount", 0))
		var moving: bool = carrier_state in ["moving", "transit", "en_route", "assigned"]
		if moving and (ware_amount > 0 or ware_type != ""):
			states[key] = _sprite_entry(
				"carrier_carrying_loop",
				carrier,
				"carrier_carrying",
				"ware_type",
				key,
				ware_type,
			)
		elif moving:
			states[key] = _sprite_entry(
				"carrier_move_loop",
				carrier,
				"carrier_moving",
				"state",
				key,
				carrier_state,
			)
		else:
			states[key] = _sprite_entry(
				"carrier_idle",
				carrier,
				"carrier_idle",
				"state",
				key,
				carrier_state,
			)


static func _ware_animation_for_type(ware: String) -> String:
	var w: String = ware.to_lower()
	if w in ["timber", "log", "logs"]:
		return "ware_log_move_loop"
	if w in ["planks", "plank"]:
		return "ware_plank_move_loop"
	if w == "stone":
		return "ware_stone_move_loop"
	return "ware_generic_move_loop"


static func _apply_ware_routes(render_data: Dictionary, states: Dictionary) -> void:
	var idx: int = 0
	for route in render_data.get("ware_routes", []):
		if typeof(route) != TYPE_DICTIONARY or route.is_empty():
			continue
		var ware: String = str(route.get("ware_type", ""))
		var anim: String = _ware_animation_for_type(ware)
		var key: String = "ware_route:%d" % idx
		states[key] = _sprite_entry(anim, route, "ware_route", "ware_type", key, ware)
		idx += 1
	for job in render_data.get("delivery_jobs", []):
		if typeof(job) != TYPE_DICTIONARY:
			continue
		var ware: String = str(job.get("ware_type", ""))
		var anim: String = _ware_animation_for_type(ware)
		var key: String = "delivery_job:%d" % int(job.get("id", idx))
		states[key] = _sprite_entry(anim, job, "delivery_job", "ware_type", key, ware)
		idx += 1


static func _construction_animation_for_progress(progress: int) -> String:
	if progress <= 0:
		return "construction_idle"
	if progress <= 33:
		return "construction_progress_25"
	if progress <= 66:
		return "construction_progress_50"
	if progress < 100:
		return "construction_progress_75"
	return "construction_progress_loop"


static func _apply_construction(render_data: Dictionary, states: Dictionary) -> void:
	for site in render_data.get("construction_sites", []):
		if typeof(site) != TYPE_DICTIONARY:
			continue
		var key: String = "construction:%d" % int(site.get("id", 0))
		var progress: int = int(site.get("progress_percent", 0))
		var anim: String = _construction_animation_for_progress(progress)
		states[key] = _sprite_entry(
			anim,
			site,
			"construction_progress",
			"progress_percent",
			key,
			str(progress),
		)


static func _apply_buildings(render_data: Dictionary, snapshot: Dictionary, states: Dictionary) -> void:
	var blocked_ids: Dictionary = {}
	for job in snapshot.get("blocked_job_markers", []):
		if typeof(job) == TYPE_DICTIONARY:
			blocked_ids[int(job.get("building_id", job.get("id", 0)))] = true
	for building in render_data.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		var bid: int = int(building.get("id", 0))
		if str(building.get("blocked", "")) == "true" or bool(building.get("blocked", false)) or blocked_ids.has(bid):
			states["building:%d" % bid] = _sprite_entry(
				"building_blocked_loop",
				building,
				"building_blocked",
				"blocked",
				"building:%d" % bid,
				"true",
			)
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		var bid: int = int(building.get("id", 0))
		var key: String = "building:%d" % bid
		if states.has(key):
			continue
		var progress: int = int(building.get("production_progress", 0))
		if progress > 0:
			states[key] = _sprite_entry(
				"building_working_loop",
				building,
				"production_active",
				"production_progress",
				key,
				str(progress),
			)
		else:
			states[key] = _sprite_entry(
				"building_idle",
				building,
				"building_idle",
				"production_progress",
				key,
				"0",
			)
	for activity in render_data.get("production_activity", []):
		if typeof(activity) != TYPE_DICTIONARY:
			continue
		var bid: int = int(activity.get("building_id", 0))
		if bid > 0:
			states["building:%d" % bid] = _sprite_entry(
				"building_working_loop",
				activity,
				"production_activity",
				"building_id",
				"building:%d" % bid,
				str(bid),
			)


static func _apply_stores(
	render_data: Dictionary,
	diagnostics_prev: Dictionary,
	states: Dictionary,
) -> void:
	var prev_totals: Dictionary = diagnostics_prev.get("ware_totals", {}).get("by_type", {})
	var curr_totals: Dictionary = render_data.get("diagnostics", {}).get("ware_totals", {}).get("by_type", {})
	var pulse_ware: String = ""
	for ware_type in curr_totals.keys():
		if int(curr_totals[ware_type]) > int(prev_totals.get(ware_type, 0)):
			pulse_ware = str(ware_type)
			break
	for store in render_data.get("building_stores", []):
		if typeof(store) != TYPE_DICTIONARY:
			continue
		var key: String = "store:%d" % int(store.get("building_id", 0))
		if pulse_ware != "":
			states[key] = _sprite_entry(
				"storage_activity_pulse",
				store,
				"storage_delta",
				"ware_totals",
				key,
				str(curr_totals.get(pulse_ware, 0)),
				str(prev_totals.get(pulse_ware, 0)),
			)
		else:
			states[key] = _sprite_entry(
				"storage_idle",
				store,
				"storage_idle",
				"building_stores",
				key,
				"present",
			)


static func _apply_road_flow(render_data: Dictionary, states: Dictionary) -> void:
	var active_roads: Dictionary = {}
	for carrier in render_data.get("carriers", []):
		for point in carrier.get("route_points", []):
			if typeof(point) != TYPE_DICTIONARY:
				continue
			var rk: String = "%d,%d" % [int(point.get("q", 0)), int(point.get("r", 0))]
			active_roads[rk] = true
	for road in render_data.get("roads", []):
		if typeof(road) != TYPE_DICTIONARY:
			continue
		var from_key: String = "%d,%d" % [int(road.get("from_q", 0)), int(road.get("from_r", 0))]
		var to_key: String = "%d,%d" % [int(road.get("to_q", 0)), int(road.get("to_r", 0))]
		if active_roads.has(from_key) or active_roads.has(to_key):
			var key: String = "road:%d" % int(road.get("id", 0))
			states[key] = _sprite_entry(
				"road_flow_loop",
				road,
				"road_flow",
				"route_points",
				key,
				"active",
			)


static func _apply_blocked(render_data: Dictionary, snapshot: Dictionary, states: Dictionary) -> void:
	for job in snapshot.get("blocked_job_markers", []):
		if typeof(job) != TYPE_DICTIONARY:
			continue
		var key: String = "blocked:%d" % int(job.get("id", 0))
		states[key] = _sprite_entry(
			"warning_blocked_loop",
			job,
			"blocked_job",
			"blocked_job_markers",
			key,
			str(job.get("reason", "blocked")),
		)


static func _apply_event_pulses(effect_events: Array, states: Dictionary) -> void:
	for evt in effect_events:
		if typeof(evt) != TYPE_DICTIONARY:
			continue
		var event_type: String = str(evt.get("type", "")).to_lower()
		if "construction_completed" in event_type or "building_completed" in event_type:
			var site_id: int = int(evt.get("entity_id", evt.get("site_id", 0)))
			var key: String = "construction_pulse:%d" % site_id
			states[key] = _sprite_entry(
				"construction_complete_pulse",
				evt,
				"construction_complete_event",
				"type",
				key,
				event_type,
			)
		elif "production" in event_type:
			var bid: int = int(evt.get("payload", {}).get("building_id", 0)) if typeof(evt.get("payload", {})) == TYPE_DICTIONARY else 0
			if bid > 0:
				states["building_pulse:%d" % bid] = _sprite_entry(
					"building_activity_pulse",
					evt,
					"production_event",
					"type",
					"building:%d" % bid,
					event_type,
				)
		elif "ware_delivered" in event_type or "delivery" in event_type:
			var key: String = "delivery_pulse:%d" % int(evt.get("tick", 0))
			states[key] = _sprite_entry(
				"storage_activity_pulse",
				evt,
				"delivery_event",
				"type",
				key,
				event_type,
			)


static func _sprite_entry(
	animation: String,
	source: Dictionary,
	reason: String,
	field: String,
	entity_key: String,
	current: String,
	prev: String = "",
) -> Dictionary:
	return {
		"animation": animation,
		"q": float(source.get("q", source.get("anchor_q", 0))),
		"r": float(source.get("r", source.get("anchor_r", 0))),
		"source": source,
		"grounding": {
			"reason": reason,
			"field": field,
			"entity_key": entity_key,
			"value": current,
			"prev": prev,
			"current": current,
		},
	}
