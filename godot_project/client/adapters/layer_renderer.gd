class_name LayerRenderer
extends RefCounted

## Builds read-only render data from snapshot dictionaries only.

const WARE_SHORT: Dictionary = {
	"timber": "T",
	"planks": "P",
	"stone": "S",
	"tools": "X",
	"food": "F",
	"rations": "R",
}


static func build_render_data(snapshot: Dictionary, layer_visibility: Dictionary = {}) -> Dictionary:
	var layers: Dictionary = snapshot.get("display_layers", {})
	var variant: String = str(snapshot.get("variant", ""))
	var terrain: Array = _filter_layer(layers.get("terrain", []), layer_visibility, "terrain", true)
	var buildings: Array = _build_building_markers(snapshot, layer_visibility)
	var units: Array = _build_unit_markers(snapshot, layer_visibility)
	var roads: Array = _build_road_markers(snapshot, layer_visibility)
	var construction_sites: Array = _build_construction_sites(snapshot, layer_visibility)
	var node_lookup: Dictionary = _road_node_lookup(snapshot)
	var carriers: Array = _build_carrier_markers(snapshot, layer_visibility, node_lookup)
	var worker_badges: Dictionary = _build_worker_badges(snapshot, layer_visibility)
	var tree_density: Array = _filter_layer(layers.get("tree_density", []), layer_visibility, "forest_density", true)
	var building_stores: Array = _build_building_stores(snapshot, layer_visibility)
	var production_activity: Array = _build_production_activity(snapshot, layer_visibility)
	var ware_routes: Array = _build_ware_routes(snapshot, layer_visibility, node_lookup)
	var delivery_jobs: Array = _build_delivery_jobs(snapshot, layer_visibility, node_lookup)
	var road_node_markers: Array = _build_road_node_markers(snapshot, layer_visibility)
	var building_ports: Array = _build_building_ports(snapshot, layer_visibility)
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
		"tree_density": tree_density,
		"building_stores": building_stores,
		"production_activity": production_activity,
		"ware_routes": ware_routes,
		"delivery_jobs": delivery_jobs,
		"road_node_markers": road_node_markers,
		"building_ports": building_ports,
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
		"tree_density": render_data.get("tree_density", []).size(),
		"building_stores": render_data.get("building_stores", []).size(),
		"ware_routes": render_data.get("ware_routes", []).size(),
		"delivery_jobs": render_data.get("delivery_jobs", []).size(),
		"road_node_markers": render_data.get("road_node_markers", []).size(),
		"building_ports": render_data.get("building_ports", []).size(),
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


static func chain_fingerprint(render_data: Dictionary, forest_q: int = 8, forest_r: int = 5) -> Dictionary:
	var forest_density: int = -1
	for entry in render_data.get("tree_density", []):
		if int(entry.get("q", -1)) == forest_q and int(entry.get("r", -1)) == forest_r:
			forest_density = int(entry.get("tree_density", -1))
	var wood_timber: int = 0
	var saw_timber: int = 0
	var saw_planks: int = 0
	for store in render_data.get("building_stores", []):
		var btype: String = str(store.get("building_type", ""))
		var wares: Dictionary = store.get("wares", {})
		if btype == "woodcutter":
			wood_timber = int(wares.get("timber", 0))
		elif btype == "sawmill":
			saw_timber = int(wares.get("timber", 0))
			saw_planks = int(wares.get("planks", 0))
	var carrier_moving: int = 0
	var carrier_ware: String = ""
	for carrier in render_data.get("carriers", []):
		if str(carrier.get("state", "")) in ["assigned", "moving"]:
			carrier_moving += 1
			if str(carrier.get("ware_type", "")) != "":
				carrier_ware = str(carrier.get("ware_type", ""))
	return {
		"forest_density": forest_density,
		"woodcutter_timber": wood_timber,
		"sawmill_timber": saw_timber,
		"sawmill_planks": saw_planks,
		"carrier_moving": carrier_moving,
		"carrier_ware": carrier_ware,
		"ware_routes": render_data.get("ware_routes", []).size(),
	}


static func _layer_visible(visibility: Dictionary, key: String, default_visible: bool) -> bool:
	if not visibility.has(key):
		return default_visible
	return bool(visibility[key])


static func _filter_layer(items: Array, visibility: Dictionary, key: String, default_visible: bool) -> Array:
	if not _layer_visible(visibility, key, default_visible):
		return []
	return items.duplicate(true)


static func _ware_store_lookup(snapshot: Dictionary) -> Dictionary:
	var lookup: Dictionary = {}
	var entities: Dictionary = snapshot.get("entities", {})
	var stores: Dictionary = entities.get("ware_stores", {})
	for key in stores:
		if typeof(stores[key]) == TYPE_DICTIONARY:
			lookup[int(stores[key].get("id", key))] = stores[key]
	return lookup


static func _building_by_store_id(snapshot: Dictionary) -> Dictionary:
	var lookup: Dictionary = {}
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id > 0:
			lookup[store_id] = building
	return lookup


static func _entity_coords(snapshot: Dictionary, entity_id: int, entity_type: String) -> Vector2i:
	if entity_type == "construction_site":
		for site in snapshot.get("construction_sites", []):
			if int(site.get("id", -1)) == entity_id:
				return Vector2i(int(site.get("anchor_q", 0)), int(site.get("anchor_r", 0)))
	elif entity_type == "building":
		for building in snapshot.get("buildings", []):
			if int(building.get("id", -1)) == entity_id:
				return Vector2i(
					int(building.get("anchor_q", building.get("q", 0))),
					int(building.get("anchor_r", building.get("r", 0))),
				)
	return Vector2i(-1, -1)


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
			"store_id": int(b.get("store_id", 0)),
			"production_progress": int(b.get("production_progress", 0)),
			"paused_reason": str(b.get("paused_reason", "")),
			"assigned_workers": b.get("assigned_workers", []).duplicate() if typeof(b.get("assigned_workers", [])) == TYPE_ARRAY else [],
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


static func _build_carrier_markers(snapshot: Dictionary, visibility: Dictionary, node_lookup: Dictionary) -> Array:
	if not _layer_visible(visibility, "logistics", true):
		return []
	var markers: Array = []
	for carrier in snapshot.get("carrier_markers", []):
		if typeof(carrier) != TYPE_DICTIONARY:
			continue
		var c: Dictionary = carrier
		var pos: Dictionary = _carrier_display_position(c, node_lookup)
		var route_points: Array = _route_hex_points(c, node_lookup)
		markers.append({
			"id": int(c.get("id", 0)),
			"q": pos.get("q", -1),
			"r": pos.get("r", -1),
			"player_id": str(c.get("player_id", "")),
			"state": str(c.get("state", "")),
			"ware_type": str(c.get("ware_type", "")),
			"ware_amount": int(c.get("ware_amount", 0)),
			"current_node_id": int(c.get("current_node_id", 0)),
			"route_points": route_points,
			"edge_progress": int(c.get("edge_progress", 0)),
		})
	return markers


static func _carrier_display_position(carrier: Dictionary, node_lookup: Dictionary) -> Dictionary:
	var route: Array = carrier.get("route_node_ids", [])
	var route_index: int = int(carrier.get("route_index", 0))
	var edge_progress: int = int(carrier.get("edge_progress", 0))
	if route.size() >= 2 and route_index < route.size() - 1:
		var from_node: Dictionary = node_lookup.get(int(route[route_index]), {})
		var to_node: Dictionary = node_lookup.get(int(route[route_index + 1]), {})
		if not from_node.is_empty() and not to_node.is_empty():
			var ticks_per_edge: int = 1
			var t: float = clampf(float(edge_progress) / float(maxi(1, ticks_per_edge)), 0.0, 1.0)
			var fq: float = float(from_node.get("q", 0))
			var fr: float = float(from_node.get("r", 0))
			var tq: float = float(to_node.get("q", 0))
			var tr: float = float(to_node.get("r", 0))
			return {"q": fq + (tq - fq) * t, "r": fr + (tr - fr) * t}
	var node_id: int = int(carrier.get("current_node_id", 0))
	var node: Dictionary = node_lookup.get(node_id, {})
	return {"q": int(node.get("q", -1)), "r": int(node.get("r", -1))}


static func _route_hex_points(carrier: Dictionary, node_lookup: Dictionary) -> Array:
	var points: Array = []
	for node_id in carrier.get("route_node_ids", []):
		var node: Dictionary = node_lookup.get(int(node_id), {})
		if node.is_empty():
			continue
		points.append({"q": int(node.get("q", 0)), "r": int(node.get("r", 0))})
	return points


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


static func _build_building_stores(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "logistics", true):
		return []
	var store_lookup: Dictionary = _ware_store_lookup(snapshot)
	var markers: Array = []
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0:
			continue
		var store: Dictionary = store_lookup.get(store_id, {})
		if store.is_empty():
			continue
		var wares: Dictionary = {}
		for ware_type in store.get("wares", {}):
			var amount: int = int(store["wares"][ware_type])
			if amount > 0:
				wares[str(ware_type)] = amount
		if wares.is_empty():
			continue
		markers.append({
			"building_id": int(building.get("id", 0)),
			"building_type": str(building.get("building_type", "")),
			"q": int(building.get("anchor_q", building.get("q", 0))),
			"r": int(building.get("anchor_r", building.get("r", 0))),
			"wares": wares,
			"label": _format_store_label(wares),
		})
	return markers


static func _format_store_label(wares: Dictionary) -> String:
	var parts: PackedStringArray = PackedStringArray()
	var keys: Array = wares.keys()
	keys.sort()
	for ware_type in keys:
		var short: String = str(WARE_SHORT.get(str(ware_type), str(ware_type).substr(0, 1).to_upper()))
		parts.append("%s:%d" % [short, int(wares[ware_type])])
	return " ".join(parts)


static func _build_production_activity(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "logistics", true):
		return []
	var markers: Array = []
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		var progress: int = int(building.get("production_progress", 0))
		var state_name: String = str(building.get("state", ""))
		if progress <= 0 and state_name == "":
			continue
		markers.append({
			"building_id": int(building.get("id", 0)),
			"building_type": str(building.get("building_type", "")),
			"q": int(building.get("anchor_q", building.get("q", 0))),
			"r": int(building.get("anchor_r", building.get("r", 0))),
			"production_progress": progress,
			"state": state_name,
			"paused_reason": str(building.get("paused_reason", "")),
			"worker_count": building.get("assigned_workers", []).size() if typeof(building.get("assigned_workers", [])) == TYPE_ARRAY else 0,
		})
	return markers


static func _build_ware_routes(snapshot: Dictionary, visibility: Dictionary, node_lookup: Dictionary) -> Array:
	if not _layer_visible(visibility, "logistics", true):
		return []
	var routes: Array = []
	for carrier in snapshot.get("carrier_markers", []):
		if typeof(carrier) != TYPE_DICTIONARY:
			continue
		if str(carrier.get("state", "")) not in ["assigned", "moving"]:
			continue
		var points: Array = _route_hex_points(carrier, node_lookup)
		if points.size() < 2:
			continue
		routes.append({
			"carrier_id": int(carrier.get("id", 0)),
			"ware_type": str(carrier.get("ware_type", "")),
			"points": points,
		})
	return routes


static func _build_delivery_jobs(snapshot: Dictionary, visibility: Dictionary, node_lookup: Dictionary) -> Array:
	if not _layer_visible(visibility, "logistics", true):
		return []
	var store_lookup: Dictionary = _ware_store_lookup(snapshot)
	var building_by_store: Dictionary = _building_by_store_id(snapshot)
	var markers: Array = []
	for job in snapshot.get("ware_movement_markers", []):
		if typeof(job) != TYPE_DICTIONARY:
			continue
		markers.append(_delivery_job_marker(snapshot, job, building_by_store, false))
	for job in snapshot.get("blocked_job_markers", []):
		if typeof(job) != TYPE_DICTIONARY:
			continue
		markers.append(_delivery_job_marker(snapshot, job, building_by_store, true))
	return markers


static func _delivery_job_marker(
	snapshot: Dictionary,
	job: Dictionary,
	building_by_store: Dictionary,
	blocked: bool,
) -> Dictionary:
	var dest_id: int = int(job.get("dest_entity_id", 0))
	var dest_type: String = str(job.get("dest_entity_type", ""))
	var dest: Vector2i = _entity_coords(snapshot, dest_id, dest_type)
	var source_store_id: int = int(job.get("source_store_id", 0))
	var source_building: Dictionary = building_by_store.get(source_store_id, {})
	var from_q: int = -1
	var from_r: int = -1
	if not source_building.is_empty():
		from_q = int(source_building.get("anchor_q", source_building.get("q", -1)))
		from_r = int(source_building.get("anchor_r", source_building.get("r", -1)))
	var ware_type: String = str(job.get("ware_type", ""))
	var amount: int = int(job.get("amount", 0))
	var dest_label: String = dest_type
	if dest_type == "building":
		for b in snapshot.get("buildings", []):
			if int(b.get("id", -1)) == dest_id:
				dest_label = str(b.get("building_type", "building"))
				break
	elif dest_type == "construction_site":
		for s in snapshot.get("construction_sites", []):
			if int(s.get("id", -1)) == dest_id:
				dest_label = "%s site" % str(s.get("building_type", "building"))
				break
	var status: String = str(job.get("status", ""))
	var block_reason: String = str(job.get("block_reason", ""))
	var label: String = "%s x%d -> %s" % [ware_type, amount, dest_label]
	if blocked and block_reason != "":
		label = "%s (%s)" % [label, block_reason]
	return {
		"job_id": int(job.get("id", 0)),
		"ware_type": ware_type,
		"amount": amount,
		"from_q": from_q,
		"from_r": from_r,
		"to_q": dest.x,
		"to_r": dest.y,
		"label": label,
		"status": status,
		"block_reason": block_reason,
		"blocked": blocked,
	}


static func _build_road_node_markers(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "logistics", true):
		return []
	var markers: Array = []
	for node in snapshot.get("road_nodes", []):
		if typeof(node) != TYPE_DICTIONARY:
			continue
		markers.append({
			"id": int(node.get("id", 0)),
			"q": int(node.get("q", 0)),
			"r": int(node.get("r", 0)),
			"building_id": int(node.get("building_id", 0)),
		})
	return markers


static func _build_building_ports(snapshot: Dictionary, visibility: Dictionary) -> Array:
	if not _layer_visible(visibility, "logistics", true):
		return []
	var markers: Array = []
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		for port in building.get("port_node_ids", []):
			if typeof(port) != TYPE_DICTIONARY:
				continue
			markers.append({
				"building_id": int(building.get("id", 0)),
				"building_type": str(building.get("building_type", "")),
				"q": int(port.get("q", 0)),
				"r": int(port.get("r", 0)),
				"dq": int(port.get("dq", 0)),
				"dr": int(port.get("dr", 0)),
				"node_id": int(port.get("node_id", 0)),
			})
	return markers
