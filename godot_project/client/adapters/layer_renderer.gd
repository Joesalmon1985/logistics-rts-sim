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
	var resources: Array = _filter_layer(layers.get("resources", []), layer_visibility, "resources", true)
	var fog: Dictionary = snapshot.get("vision", {})
	return {
		"variant": variant,
		"terrain": terrain,
		"buildings": buildings,
		"units": units,
		"roads": roads,
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
	}


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
			"from_q": int(r.get("from_q", 0)),
			"from_r": int(r.get("from_r", 0)),
			"to_q": int(r.get("to_q", 0)),
			"to_r": int(r.get("to_r", 0)),
		})
	return markers
