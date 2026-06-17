class_name EconomySpectatorMetrics
extends RefCounted

## Start→end activity delta counters for RUN_05C spectator tests and smokes.


static func entity_categories_changed(before: Dictionary, after: Dictionary) -> int:
	var changed: int = 0
	for key in after.keys():
		if int(after.get(key, 0)) != int(before.get(key, 0)):
			changed += 1
	return changed


static func chain_fingerprint(render_data: Dictionary) -> Dictionary:
	var fp: Dictionary = LayerRenderer.chain_fingerprint(render_data)
	fp["carriers"] = render_data.get("carriers", []).size()
	fp["construction_sites"] = render_data.get("construction_sites", []).size()
	fp["delivery_jobs"] = render_data.get("delivery_jobs", []).size()
	fp["ware_routes"] = render_data.get("ware_routes", []).size()
	return fp


static func chain_fingerprint_changed(before: Dictionary, after: Dictionary) -> bool:
	for key in [
		"forest_density", "woodcutter_timber", "sawmill_timber", "sawmill_planks",
		"carrier_moving", "ware_routes", "carriers", "construction_sites", "delivery_jobs",
	]:
		if int(after.get(key, 0)) != int(before.get(key, 0)):
			return true
	if str(after.get("carrier_ware", "")) != str(before.get("carrier_ware", "")):
		return true
	return false


static func production_progress_changed(snapshot_before: Dictionary, snapshot_after: Dictionary) -> bool:
	for building_after in snapshot_after.get("buildings", []):
		if typeof(building_after) != TYPE_DICTIONARY:
			continue
		var bid: int = int(building_after.get("id", 0))
		var progress_after: int = int(building_after.get("production_progress", 0))
		for building_before in snapshot_before.get("buildings", []):
			if typeof(building_before) != TYPE_DICTIONARY:
				continue
			if int(building_before.get("id", 0)) == bid:
				if progress_after > int(building_before.get("production_progress", 0)):
					return true
				break
	return false


static func construction_progress_changed(snapshot_before: Dictionary, snapshot_after: Dictionary) -> bool:
	var before_lookup: Dictionary = {}
	for site in snapshot_before.get("construction_sites", []):
		if typeof(site) != TYPE_DICTIONARY:
			continue
		before_lookup[int(site.get("id", 0))] = LayerRenderer.site_progress_percent(site)
	for site in snapshot_after.get("construction_sites", []):
		if typeof(site) != TYPE_DICTIONARY:
			continue
		var sid: int = int(site.get("id", 0))
		var after_pct: int = LayerRenderer.site_progress_percent(site)
		if after_pct > int(before_lookup.get(sid, 0)):
			return true
	return before_lookup.size() != snapshot_after.get("construction_sites", []).size()


static func sprite_state_set_changed(before: Dictionary, after: Dictionary) -> bool:
	if before.size() != after.size():
		return true
	for key in after.keys():
		if not before.has(key):
			return true
		var anim_before: String = str(before[key].get("animation", "")) if typeof(before[key]) == TYPE_DICTIONARY else ""
		var anim_after: String = str(after[key].get("animation", "")) if typeof(after[key]) == TYPE_DICTIONARY else ""
		if anim_before != anim_after:
			return true
	return false


static func sprite_state_signature(states: Dictionary) -> String:
	var keys: Array = states.keys()
	keys.sort()
	var parts: PackedStringArray = PackedStringArray()
	for key in keys:
		var entry: Dictionary = states[key] if typeof(states[key]) == TYPE_DICTIONARY else {}
		parts.append("%s=%s" % [str(key), str(entry.get("animation", ""))])
	return "|".join(parts)


static func measure_activity(
	counts_before: Dictionary,
	counts_after: Dictionary,
	fp_before: Dictionary,
	fp_after: Dictionary,
	sprite_states_before: Dictionary,
	sprite_states_after: Dictionary,
	snapshot_before: Dictionary,
	snapshot_after: Dictionary,
	events_before: int,
	events_after: int,
) -> Dictionary:
	var signals: Dictionary = {
		"events_grew": events_after > events_before,
		"production_progress_changed": production_progress_changed(snapshot_before, snapshot_after),
		"carrier_or_route_count_changed": (
			int(fp_after.get("carrier_moving", 0)) != int(fp_before.get("carrier_moving", 0))
			or int(fp_after.get("ware_routes", 0)) != int(fp_before.get("ware_routes", 0))
			or int(fp_after.get("carriers", 0)) != int(fp_before.get("carriers", 0))
		),
		"sprite_state_set_changed": sprite_state_set_changed(sprite_states_before, sprite_states_after),
		"construction_progress_changed": construction_progress_changed(snapshot_before, snapshot_after),
		"chain_fingerprint_changed": chain_fingerprint_changed(fp_before, fp_after),
		"entity_categories_changed": entity_categories_changed(counts_before, counts_after) >= 1,
	}
	var meaningful: int = 0
	var non_sprite: int = 0
	for key in signals.keys():
		if bool(signals[key]):
			meaningful += 1
			if key != "sprite_state_set_changed":
				non_sprite += 1
	return {
		"entity_categories_changed": entity_categories_changed(counts_before, counts_after),
		"sprite_signature_before": sprite_state_signature(sprite_states_before),
		"sprite_signature_after": sprite_state_signature(sprite_states_after),
		"events_delta": events_after - events_before,
		"signals": signals,
		"meaningful_count": meaningful,
		"non_sprite_delta_count": non_sprite,
	}


static func passes_smoke_gate(activity: Dictionary, min_meaningful: int = 2) -> bool:
	if int(activity.get("meaningful_count", 0)) < min_meaningful:
		return false
	return int(activity.get("non_sprite_delta_count", 0)) >= 1


static func image_pixel_delta(img_a: Image, img_b: Image, sample_step: int = 16) -> int:
	if img_a == null or img_b == null or img_a.is_empty() or img_b.is_empty():
		return 0
	if img_a.get_width() != img_b.get_width() or img_a.get_height() != img_b.get_height():
		return 999
	var diff: int = 0
	for y in range(0, img_a.get_height(), sample_step):
		for x in range(0, img_a.get_width(), sample_step):
			var ca: Color = img_a.get_pixel(x, y)
			var cb: Color = img_b.get_pixel(x, y)
			if ca.is_equal_approx(cb):
				continue
			if ca.a > 0.05 or cb.a > 0.05:
				diff += 1
	return diff


static func animation_family(animation: String) -> String:
	var anim: String = animation.to_lower()
	if anim.begins_with("carrier_") or anim.begins_with("ware_") or anim == "road_flow_loop":
		return "logistics"
	if anim.begins_with("building_"):
		return "building"
	if anim.begins_with("construction_"):
		return "construction"
	if anim.begins_with("storage_"):
		return "storage"
	if anim.begins_with("warning_"):
		return "warning"
	return "other"


static func detect_exposable_families(render_data: Dictionary, snapshot: Dictionary) -> Dictionary:
	var families: Dictionary = {}
	if not render_data.get("carriers", []).is_empty() or not render_data.get("ware_routes", []).is_empty() or not render_data.get("delivery_jobs", []).is_empty():
		families["logistics"] = "carriers/routes/jobs present"
	var has_building_activity: bool = false
	for building in snapshot.get("buildings", []):
		if typeof(building) == TYPE_DICTIONARY and int(building.get("production_progress", 0)) > 0:
			has_building_activity = true
			break
	if not has_building_activity:
		for activity in render_data.get("production_activity", []):
			if typeof(activity) == TYPE_DICTIONARY:
				has_building_activity = true
				break
	if has_building_activity or not render_data.get("buildings", []).is_empty():
		families["building"] = "buildings present"
	if not render_data.get("construction_sites", []).is_empty():
		families["construction"] = "construction_sites present"
	if not render_data.get("building_stores", []).is_empty():
		families["storage"] = "building_stores present"
	if not snapshot.get("blocked_job_markers", []).is_empty():
		families["warning"] = "blocked_job_markers present"
	for building in render_data.get("buildings", []):
		if typeof(building) == TYPE_DICTIONARY and (bool(building.get("blocked", false)) or str(building.get("blocked", "")) == "true"):
			families["warning"] = "blocked building present"
	return families


static func count_active_families(sprite_states: Dictionary) -> Dictionary:
	var active: Dictionary = {}
	for key in sprite_states.keys():
		var entry: Dictionary = sprite_states[key] if typeof(sprite_states[key]) == TYPE_DICTIONARY else {}
		var anim: String = str(entry.get("animation", ""))
		var family: String = animation_family(anim)
		if family != "other":
			active[family] = true
	return active


static func evaluate_visual_families(render_data: Dictionary, snapshot: Dictionary, sprite_states: Dictionary) -> Dictionary:
	var exposable: Dictionary = detect_exposable_families(render_data, snapshot)
	var active: Dictionary = count_active_families(sprite_states)
	var missing: PackedStringArray = PackedStringArray()
	for family in exposable.keys():
		if not active.has(family):
			missing.append("%s: %s" % [family, str(exposable[family])])
	var exposable_count: int = exposable.size()
	var active_count: int = active.size()
	var passes: bool = true
	if exposable_count >= 3:
		passes = active_count >= 3
	else:
		passes = missing.is_empty()
	return {
		"exposable_families": exposable,
		"active_families": active,
		"exposable_count": exposable_count,
		"active_count": active_count,
		"missing_families": missing,
		"passes": passes,
	}


static func passes_visual_family_gate(render_data: Dictionary, snapshot: Dictionary, sprite_states: Dictionary) -> bool:
	return bool(evaluate_visual_families(render_data, snapshot, sprite_states).get("passes", false))

