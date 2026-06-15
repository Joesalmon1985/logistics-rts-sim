class_name LayerRenderer
extends RefCounted

## Builds read-only render data from snapshot dictionaries only.


static func build_render_data(snapshot: Dictionary, layer_visibility: Dictionary = {}) -> Dictionary:
	var layers: Dictionary = snapshot.get("display_layers", {})
	var variant: String = str(snapshot.get("variant", ""))
	var terrain: Array = _filter_layer(layers.get("terrain", []), layer_visibility, "terrain", true)
	var buildings: Array = _build_building_markers(snapshot, layer_visibility)
	var units: Array = _build_unit_markers(snapshot, layer_visibility)
	var roads: Array = _build_road_markers(snapshot, layer_visibility)
	var construction_sites: Array = _build_construction_sites(snapshot, layer_visibility)
	var carriers: Array = _build_carrier_markers(snapshot, layer_visibility)
	var worker_badges: Dictionary = _build_worker_badges(snapshot, layer_visibility)
	var resources: Array = _filter_layer(layers.get("resources", []), layer_visibility, "resources", true)
	for field in snapshot.get("resource_fields", []):
		if typeof(field) == TYPE_DICTIONARY:
			resources.append(field.duplicate(true))
	var fog: Dictionary = snapshot.get("vision", {})
	return {
		"variant": variant,
		"terrain": terrain,
		"buildings": buildings,
		"units": units,
		"roads": roads,
		"construction_sites": construction_sites,
		"carriers": carriers,
		"worker_badges": worker_badges,
		"resources": resources,
		"fog": fog if _layer_visible(layer_visibility, "fog", true) else {},
		"map_width": int(snapshot.get("map", {}).get("width", 0)),
		"map_height": int(snapshot.get("map", {}).get("height", 0)),
		"diagnostics": snapshot.get("diagnostics", {}) if _layer_visible(layer_visibility, "diagnostics", true) else {},
	}


static func count_entities(render_data: Dictionary) -> Dictionary:
	return {
		"terrain": render_data.get("terrain", []).size(),
		"buildings": render_data.get("buildings", []).size(),
		"units": render_data.get("units", []).size(),
		"roads": render_data.get("roads", []).size(),
		"construction_sites": render_data.get("construction_sites", []).size(),
		"carriers": render_data.get("carriers", []).size(),
	}


static func site_progress_percent(site: Dictionary) -> int:
	if site.has("required_ticks"):
		var required_ticks: int = maxi(1, int(site.get("required_ticks", 1)))
		return clampi(int(site.get("progress", 0)) * 100 / required_ticks, 0, 100)
	var required: Dictionary = site.get("required", {})
	if required.is_empty():
		return 0
	var total_req: int = 0
	var total_del: int = 0
	for ware_type in required:
		total_req += int(required[ware_type])
		total_del += int(site.get("delivered", {}).get(ware_type, 0))
	if total_req <= 0:
		return 0
	return clampi(total_del * 100 / total_req, 0, 100)


static func _layer_visible(visibility: Dictionary, key: String, default_visible: bool) -> bool:
	if not visibility.has(key):
		return default_visible
	return bool(visibility[key])


static func _filter_layer(items: Array, visibility: Dictionary, key: String, default_visible: bool) -> Array:
	if not _layer_visible(visibility, key, default_visible):
		return []
	return items.duplicate(true)


static func _build_building_markers(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "buildings", true):
		return []
	var markers: Array = []
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		var b: Dictionary = building
		markers.append({
			"id": int(b.get("id", 0)),
			"building_type": str(b.get("building_type", "")),
			"q": int(b.get("anchor_q", b.get("q", 0))),
			"r": int(b.get("anchor_r", b.get("r", 0))),
			"player_id": str(b.get("player_id", "")),
			"state": str(b.get("state", "")),
			"hp": int(b.get("hp", 0)),
			"max_hp": int(b.get("max_hp", 0)),
		})
	return markers


static func _build_unit_markers(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "units", true):
		return []
	var markers: Array = []
	for unit in snapshot.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		var u: Dictionary = unit
		markers.append({
			"id": int(u.get("id", 0)),
			"unit_type": str(u.get("unit_type", "")),
			"q": int(u.get("q", 0)),
			"r": int(u.get("r", 0)),
			"player_id": str(u.get("player_id", "")),
			"state": str(u.get("state", "")),
			"hp": int(u.get("hp", 0)),
			"max_hp": int(u.get("max_hp", 0)),
			"weapon": str(u.get("weapon", "")),
			"order": str(u.get("order", "")),
			"engage_target_id": int(u.get("engage_target_id", 0)),
		})
	return markers


static func _build_road_markers(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "roads", true):
		return []
	var markers: Array = []
	for road in snapshot.get("roads", []):
		if typeof(road) != TYPE_DICTIONARY:
			continue
		var r: Dictionary = road
		markers.append({
			"from_q": int(r.get("hex_a_q", r.get("from_q", 0))),
			"from_r": int(r.get("hex_a_r", r.get("from_r", 0))),
			"to_q": int(r.get("hex_b_q", r.get("to_q", 0))),
			"to_r": int(r.get("hex_b_r", r.get("to_r", 0))),
		})
	return markers


static func _build_construction_sites(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "buildings", true):
		return []
	var markers: Array = []
	for site in snapshot.get("construction_sites", []):
		if typeof(site) != TYPE_DICTIONARY:
			continue
		var s: Dictionary = site
		if str(s.get("state", "")) in ["completed", "cancelled"]:
			continue
		var progress: int = site_progress_percent(s)
		markers.append({
			"id": int(s.get("id", 0)),
			"building_type": str(s.get("building_type", "")),
			"q": int(s.get("anchor_q", s.get("q", 0))),
			"r": int(s.get("anchor_r", s.get("r", 0))),
			"player_id": str(s.get("player_id", "")),
			"state": str(s.get("state", "")),
			"delivered": s.get("delivered", {}).duplicate(true) if typeof(s.get("delivered", {})) == TYPE_DICTIONARY else {},
			"required": s.get("required", {}).duplicate(true) if typeof(s.get("required", {})) == TYPE_DICTIONARY else {},
			"progress": int(s.get("progress", 0)),
			"required_ticks": int(s.get("required_ticks", 0)),
			"progress_percent": progress,
		})
	return markers


static func _road_node_lookup(snapshot: Dictionary) -> Dictionary:
	var lookup: Dictionary = {}
	for node in snapshot.get("road_nodes", []):
		if typeof(node) != TYPE_DICTIONARY:
			continue
		lookup[int(node.get("id", 0))] = node
	return lookup


static func _build_carrier_markers(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "units", true):
		return []
	var node_lookup: Dictionary = _road_node_lookup(snapshot)
	var markers: Array = []
	for carrier in snapshot.get("carrier_markers", []):
		if typeof(carrier) != TYPE_DICTIONARY:
			continue
		var c: Dictionary = carrier
		var node_id: int = int(c.get("current_node_id", 0))
		var node: Dictionary = node_lookup.get(node_id, {})
		markers.append({
			"id": int(c.get("id", 0)),
			"q": int(node.get("q", -1)),
			"r": int(node.get("r", -1)),
			"player_id": str(c.get("player_id", "")),
			"state": str(c.get("state", "")),
			"ware_type": str(c.get("ware_type", "")),
			"ware_amount": int(c.get("ware_amount", 0)),
			"current_node_id": node_id,
		})
	return markers


static func _build_worker_badges(snapshot: Dictionary, visibility: Dictionary) -> Dictionary:
	if not _layer_visible(visibility, "buildings", true):
		return {}
	var badges: Dictionary = {}
	for worker in snapshot.get("worker_markers", []):
		if typeof(worker) != TYPE_DICTIONARY:
			continue
		var building_id: int = int(worker.get("assigned_building_id", 0))
		if building_id <= 0:
			continue
		var key: String = str(building_id)
		badges[key] = int(badges.get(key, 0)) + 1
	return badges
