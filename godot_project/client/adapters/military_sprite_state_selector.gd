class_name MilitarySpriteStateSelector
extends RefCounted

## Maps military render-data and events to registry sprite keys (viewer-only).

const SPRITE_BUDGET_CAP: int = 64


static func select_states(
	render_data: Dictionary,
	snapshot: Dictionary,
	effect_events: Array,
) -> Dictionary:
	var states: Dictionary = {}
	_apply_buildings(render_data, states)
	_apply_units(render_data, states)
	_apply_construction(render_data, states)
	_apply_projectiles(snapshot, states)
	_apply_combat_markers(effect_events, states)
	_apply_harvest_routes(snapshot, states)
	return states


static func _apply_buildings(render_data: Dictionary, states: Dictionary) -> void:
	for building in render_data.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		var btype: String = str(building.get("building_type", ""))
		var key: String = "building:%d" % int(building.get("id", 0))
		states[key] = _entry(
			"building",
			btype,
			int(building.get("q", building.get("anchor_q", 0))),
			int(building.get("r", building.get("anchor_r", 0))),
		)


static func _apply_units(render_data: Dictionary, states: Dictionary) -> void:
	for unit in render_data.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		var utype: String = str(unit.get("unit_type", ""))
		var key: String = "unit:%d" % int(unit.get("id", 0))
		states[key] = _entry(
			"unit",
			utype,
			int(unit.get("q", 0)),
			int(unit.get("r", 0)),
		)


static func _apply_construction(render_data: Dictionary, states: Dictionary) -> void:
	for site in render_data.get("construction_sites", []):
		if typeof(site) != TYPE_DICTIONARY:
			continue
		var key: String = "site:%d" % int(site.get("id", 0))
		states[key] = _entry(
			"construction_site",
			str(site.get("building_type", "construction_site")),
			int(site.get("q", site.get("anchor_q", 0))),
			int(site.get("r", site.get("anchor_r", 0))),
		)


static func _apply_projectiles(snapshot: Dictionary, states: Dictionary) -> void:
	var idx: int = 0
	for projectile in snapshot.get("projectiles", []):
		if typeof(projectile) != TYPE_DICTIONARY:
			continue
		var key: String = "projectile:%d" % idx
		states[key] = {
			"entity_kind": "effect",
			"effect_key": "projectile",
			"q": float(projectile.get("q", 0)),
			"r": float(projectile.get("r", 0)),
		}
		idx += 1


static func _apply_combat_markers(effect_events: Array, states: Dictionary) -> void:
	var idx: int = 0
	for evt in effect_events:
		if typeof(evt) != TYPE_DICTIONARY:
			continue
		var event_type: String = str(evt.get("type", "")).to_lower()
		if not ("damage" in event_type or "combat" in event_type or "attack" in event_type or "fired" in event_type):
			continue
		var payload: Dictionary = evt.get("payload", {}) if typeof(evt.get("payload", {})) == TYPE_DICTIONARY else {}
		var q: float = float(payload.get("q", payload.get("target_q", -1)))
		var r: float = float(payload.get("r", payload.get("target_r", -1)))
		if q < 0:
			continue
		var key: String = "combat:%d" % idx
		states[key] = {
			"entity_kind": "effect",
			"effect_key": "attack_marker" if "attack" in event_type or "fired" in event_type else "damage_flash",
			"q": q,
			"r": r,
		}
		idx += 1


static func _apply_harvest_routes(snapshot: Dictionary, states: Dictionary) -> void:
	var idx: int = 0
	for unit in snapshot.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		if int(unit.get("cargo", 0)) <= 0 and int(unit.get("harvest_field_id", 0)) <= 0:
			continue
		var utype: String = str(unit.get("unit_type", ""))
		if MilitaryCatalog.catalog_key(utype) != "harvester" and utype != "collector":
			continue
		var field_id: int = int(unit.get("harvest_field_id", 0))
		var fq: float = float(unit.get("q", 0))
		var fr: float = float(unit.get("r", 0))
		var tq: float = fq
		var tr: float = fr
		for field in snapshot.get("resource_fields", []):
			if typeof(field) != TYPE_DICTIONARY:
				continue
			if int(field.get("id", 0)) == field_id:
				tq = float(field.get("q", fq))
				tr = float(field.get("r", fr))
				break
		var key: String = "harvest_route:%d" % int(unit.get("id", idx))
		states[key] = {
			"entity_kind": "effect",
			"effect_key": "harvest_route",
			"q": fq + (tq - fq) * 0.35,
			"r": fr + (tr - fr) * 0.35,
			"source": {"route_points": [{"q": fq, "r": fr}, {"q": tq, "r": tr}]},
		}
		idx += 1


static func _entry(entity_kind: String, sim_type: String, q: int, r: int) -> Dictionary:
	return {
		"entity_kind": entity_kind,
		"sim_type": sim_type,
		"catalog_key": MilitaryCatalog.catalog_key(sim_type),
		"q": q,
		"r": r,
	}


static func cap_states(states: Dictionary) -> Dictionary:
	if states.size() <= SPRITE_BUDGET_CAP:
		return states
	var keys: Array = states.keys()
	keys.sort()
	var capped: Dictionary = {}
	for i in range(mini(keys.size(), SPRITE_BUDGET_CAP)):
		capped[str(keys[i])] = states[keys[i]]
	return capped
