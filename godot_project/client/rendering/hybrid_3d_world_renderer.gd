class_name Hybrid3dWorldRenderer
extends Node3D

## Snapshot-driven 3D placeholder meshes for hybrid bot spectator (client-only).

const HEX_SIZE: float = 1.2
const PLAYER_COLORS: Dictionary = {
	"p1": Color(0.28, 0.55, 0.95),
	"p2": Color(0.92, 0.28, 0.28),
}

var overlay_visibility: Dictionary = {}
var snapshot: Dictionary = {}
var selected_q: int = -1
var selected_r: int = -1

var _terrain_root: Node3D
var _roads_root: Node3D
var _buildings_root: Node3D
var _units_root: Node3D
var _overlays_root: Node3D
var _resources_root: Node3D
var _selection_marker: MeshInstance3D
var _box_mesh: BoxMesh
var _cylinder_mesh: CylinderMesh


func _ready() -> void:
	_terrain_root = Node3D.new()
	_terrain_root.name = "Terrain"
	add_child(_terrain_root)
	_roads_root = Node3D.new()
	_roads_root.name = "Roads"
	add_child(_roads_root)
	_buildings_root = Node3D.new()
	_buildings_root.name = "Buildings"
	add_child(_buildings_root)
	_units_root = Node3D.new()
	_units_root.name = "Units"
	add_child(_units_root)
	_overlays_root = Node3D.new()
	_overlays_root.name = "Overlays"
	add_child(_overlays_root)
	_resources_root = Node3D.new()
	_resources_root.name = "Resources"
	add_child(_resources_root)
	_box_mesh = BoxMesh.new()
	_cylinder_mesh = CylinderMesh.new()
	_cylinder_mesh.top_radius = 0.35
	_cylinder_mesh.bottom_radius = 0.35
	_cylinder_mesh.height = 0.7
	_selection_marker = _make_mesh_instance(_box_mesh, Color(1, 1, 1, 0.35))
	_selection_marker.scale = Vector3(1.4, 0.15, 1.4)
	_selection_marker.visible = false
	add_child(_selection_marker)


static func hex_to_world(q: float, r: float) -> Vector3:
	var x: float = HEX_SIZE * sqrt(3.0) * (q + r * 0.5)
	var z: float = HEX_SIZE * 1.5 * r
	return Vector3(x, 0.0, z)


func apply_snapshot(data: Dictionary, overlays: Dictionary, sel_q: int = -1, sel_r: int = -1) -> void:
	snapshot = _normalize_snapshot(data)
	overlay_visibility = overlays
	selected_q = sel_q
	selected_r = sel_r
	_rebuild_world()


func get_entity_count() -> Dictionary:
	return {
		"terrain": _terrain_root.get_child_count(),
		"roads": _roads_root.get_child_count(),
		"buildings": _buildings_root.get_child_count(),
		"units": _units_root.get_child_count(),
		"resources": _resources_root.get_child_count(),
	}


func _rebuild_world() -> void:
	_clear_children(_terrain_root)
	_clear_children(_roads_root)
	_clear_children(_buildings_root)
	_clear_children(_units_root)
	_clear_children(_overlays_root)
	_clear_children(_resources_root)
	if snapshot.is_empty():
		_selection_marker.visible = false
		return
	for tile in _terrain_tiles(snapshot):
		_spawn_terrain(tile)
	_spawn_resource_markers(snapshot)
	for road in snapshot.get("roads", []):
		if overlay_visibility.get("roads", true):
			_spawn_road(road)
	for building in snapshot.get("buildings", []):
		_spawn_economy_building(building)
	for building in snapshot.get("military_buildings", []):
		_spawn_military_building(building)
	for site in snapshot.get("construction_sites", []):
		if overlay_visibility.get("construction", true):
			_spawn_construction_site(site)
	for carrier in _carriers(snapshot):
		_spawn_carrier(carrier)
	for unit in snapshot.get("units", []):
		_spawn_unit(unit)
	if overlay_visibility.get("supply", true):
		_spawn_supply_overlays()
	if overlay_visibility.get("attack_targets", true):
		_spawn_attack_overlays()
	if overlay_visibility.get("combat_events", true):
		_spawn_combat_markers()
	if selected_q >= 0:
		_selection_marker.position = hex_to_world(selected_q, selected_r) + Vector3(0, 0.08, 0)
		_selection_marker.visible = true
	else:
		_selection_marker.visible = false


func _spawn_terrain(tile: Dictionary) -> void:
	var q: int = int(tile.get("q", 0))
	var r: int = int(tile.get("r", 0))
	var terrain: String = str(tile.get("terrain_type", tile.get("terrain", "grass")))
	var height: float = 0.12
	var color: Color = Color(0.38, 0.72, 0.32)
	match terrain:
		"forest":
			color = Color(0.12, 0.42, 0.18)
			height = 0.18
		"rock":
			color = Color(0.52, 0.52, 0.58)
			height = 0.22
		"water":
			color = Color(0.22, 0.48, 0.82)
			height = 0.05
		"mountain":
			color = Color(0.48, 0.44, 0.40)
			height = 0.35
		"desert":
			color = Color(0.78, 0.68, 0.38)
	var mesh_inst := _make_mesh_instance(_box_mesh, color)
	mesh_inst.scale = Vector3(1.05, height, 1.05)
	mesh_inst.position = hex_to_world(q, r) + Vector3(0, height * 0.5, 0)
	_terrain_root.add_child(mesh_inst)


func _spawn_road(road: Dictionary) -> void:
	var fq: int = int(road.get("from_q", 0))
	var fr: int = int(road.get("from_r", 0))
	var tq: int = int(road.get("to_q", 0))
	var tr: int = int(road.get("to_r", 0))
	var a: Vector3 = hex_to_world(fq, fr)
	var b: Vector3 = hex_to_world(tq, tr)
	var mid: Vector3 = (a + b) * 0.5
	var mesh_inst := _make_mesh_instance(_box_mesh, Color(0.55, 0.45, 0.28))
	var delta: Vector3 = b - a
	var length: float = maxf(delta.length(), 0.2)
	mesh_inst.scale = Vector3(0.18, 0.06, length)
	mesh_inst.position = mid + Vector3(0, 0.08, 0)
	if delta.length() > 0.01:
		mesh_inst.look_at(mid + delta.normalized(), Vector3.UP)
	_roads_root.add_child(mesh_inst)


func _spawn_economy_building(building: Dictionary) -> void:
	var btype: String = str(building.get("building_type", "building"))
	var player_id: String = str(building.get("player_id", "p1"))
	var q: int = int(building.get("anchor_q", building.get("q", 0)))
	var r: int = int(building.get("anchor_r", building.get("r", 0)))
	var spec: Dictionary = _building_spec(btype, false)
	var mesh_inst := _make_mesh_instance(_box_mesh, _tint(PLAYER_COLORS.get(player_id, Color.WHITE), spec.get("color", Color.GRAY)))
	mesh_inst.scale = spec.get("scale", Vector3(0.8, 0.8, 0.8))
	mesh_inst.position = hex_to_world(q, r) + Vector3(0, float(mesh_inst.scale.y) * 0.5, 0)
	_buildings_root.add_child(mesh_inst)


func _spawn_military_building(building: Dictionary) -> void:
	var btype: String = str(building.get("building_type", "command_hub"))
	var player_id: String = str(building.get("player_id", "p1"))
	var q: int = int(building.get("anchor_q", building.get("q", 0)))
	var r: int = int(building.get("anchor_r", building.get("r", 0)))
	var spec: Dictionary = _building_spec(btype, true)
	var mesh_inst := _make_mesh_instance(_box_mesh, _tint(PLAYER_COLORS.get(player_id, Color.WHITE), spec.get("color", Color.GRAY)))
	mesh_inst.scale = spec.get("scale", Vector3(0.9, 0.9, 0.9))
	mesh_inst.position = hex_to_world(q, r) + Vector3(0, float(mesh_inst.scale.y) * 0.5, 0)
	_buildings_root.add_child(mesh_inst)


func _spawn_construction_site(site: Dictionary) -> void:
	var q: int = int(site.get("anchor_q", site.get("q", 0)))
	var r: int = int(site.get("anchor_r", site.get("r", 0)))
	var progress: float = clampf(float(site.get("progress", 0)) / maxf(float(site.get("required_progress", 100)), 1.0), 0.05, 1.0)
	var mesh_inst := _make_mesh_instance(_box_mesh, Color(0.85, 0.72, 0.22, 0.85))
	mesh_inst.scale = Vector3(0.7, 0.25 + progress * 0.6, 0.7)
	mesh_inst.position = hex_to_world(q, r) + Vector3(0, float(mesh_inst.scale.y) * 0.5, 0)
	_buildings_root.add_child(mesh_inst)


func _spawn_carrier(carrier: Dictionary) -> void:
	var player_id: String = str(carrier.get("player_id", "p1"))
	var qf: float = float(carrier.get("q", carrier.get("anchor_q", 0)))
	var rf: float = float(carrier.get("r", carrier.get("anchor_r", 0)))
	var mesh_inst := _make_mesh_instance(_cylinder_mesh, _tint(PLAYER_COLORS.get(player_id, Color.WHITE), Color(0.82, 0.62, 0.18)))
	mesh_inst.position = hex_to_world(qf, rf) + Vector3(0, 0.45, 0)
	_units_root.add_child(mesh_inst)


func _spawn_unit(unit: Dictionary) -> void:
	var player_id: String = str(unit.get("player_id", "p1"))
	var q: int = int(unit.get("q", 0))
	var r: int = int(unit.get("r", 0))
	var utype: String = str(unit.get("unit_type", "rifle_squad"))
	var spec: Dictionary = _unit_spec(utype)
	var mesh_inst := _make_mesh_instance(_box_mesh, _tint(PLAYER_COLORS.get(player_id, Color.WHITE), spec.get("color", Color(0.7, 0.7, 0.7))))
	mesh_inst.scale = spec.get("scale", Vector3(0.35, 0.55, 0.35))
	mesh_inst.position = hex_to_world(q, r) + Vector3(0, float(mesh_inst.scale.y) * 0.5, 0)
	_units_root.add_child(mesh_inst)


func _spawn_supply_overlays() -> void:
	for route in snapshot.get("logistics_routes", []):
		if typeof(route) != TYPE_DICTIONARY:
			continue
		var path: Array = route.get("path", [])
		for i in range(path.size() - 1):
			var a: Dictionary = path[i]
			var b: Dictionary = path[i + 1]
			var mid: Vector3 = (hex_to_world(float(a.get("q", 0)), float(a.get("r", 0))) + hex_to_world(float(b.get("q", 0)), float(b.get("r", 0)))) * 0.5
			var marker := _make_mesh_instance(_box_mesh, Color(0.95, 0.85, 0.15, 0.55))
			marker.scale = Vector3(0.12, 0.12, 0.12)
			marker.position = mid + Vector3(0, 0.25, 0)
			_overlays_root.add_child(marker)


func _spawn_attack_overlays() -> void:
	for order in snapshot.get("delegated_orders", []):
		if typeof(order) != TYPE_DICTIONARY:
			continue
		var tq: int = int(order.get("target_q", order.get("q", -1)))
		var tr: int = int(order.get("target_r", order.get("r", -1)))
		if tq < 0:
			continue
		var marker := _make_mesh_instance(_box_mesh, Color(1.0, 0.2, 0.2, 0.65))
		marker.scale = Vector3(0.5, 0.08, 0.5)
		marker.position = hex_to_world(tq, tr) + Vector3(0, 0.2, 0)
		_overlays_root.add_child(marker)


func _spawn_combat_markers() -> void:
	for entry in snapshot.get("combat_log", []):
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var q: int = int(entry.get("q", entry.get("target_q", -1)))
		var r: int = int(entry.get("r", entry.get("target_r", -1)))
		if q < 0:
			continue
		var marker := _make_mesh_instance(_box_mesh, Color(1.0, 0.35, 0.1, 0.75))
		marker.scale = Vector3(0.35, 0.35, 0.35)
		marker.position = hex_to_world(q, r) + Vector3(0, 0.6, 0)
		_overlays_root.add_child(marker)


func _spawn_resource_markers(data: Dictionary) -> void:
	var snap: Dictionary = _normalize_snapshot(data)
	var layers: Dictionary = snap.get("display_layers", {})
	if typeof(layers) != TYPE_DICTIONARY:
		layers = {}
	for td in snap.get("tree_density", layers.get("tree_density", [])):
		if typeof(td) != TYPE_DICTIONARY:
			continue
		var density: int = int(td.get("tree_density", 0))
		if density < 40:
			continue
		var q: int = int(td.get("q", 0))
		var r: int = int(td.get("r", 0))
		var tree := _make_mesh_instance(_cylinder_mesh, Color(0.18, 0.72, 0.28))
		tree.scale = Vector3(0.25, 0.9 + float(mini(density, 80)) * 0.008, 0.25)
		tree.position = hex_to_world(q, r) + Vector3(0, 0.55, 0)
		_resources_root.add_child(tree)
	for res in snap.get("resources", layers.get("resources", [])):
		if typeof(res) != TYPE_DICTIONARY:
			continue
		var rq: int = int(res.get("q", 0))
		var rr: int = int(res.get("r", 0))
		var dtype: String = str(res.get("deposit_type", res.get("field_type", "")))
		var res_color: Color = Color(0.72, 0.72, 0.78)
		if dtype == "rock":
			res_color = Color(0.58, 0.58, 0.65)
		elif dtype == "forest":
			res_color = Color(0.22, 0.68, 0.32)
		var marker := _make_mesh_instance(_box_mesh, res_color)
		marker.scale = Vector3(0.35, 0.5, 0.35)
		marker.position = hex_to_world(rq, rr) + Vector3(0, 0.35, 0)
		_resources_root.add_child(marker)


func _building_spec(btype: String, military: bool) -> Dictionary:
	if military:
		match btype:
			"command_hub", "headquarters":
				return {"scale": Vector3(1.4, 1.0, 1.4), "color": Color(0.92, 0.78, 0.18)}
			"barracks":
				return {"scale": Vector3(1.0, 0.75, 1.2), "color": Color(0.52, 0.52, 0.78)}
			"power_generator", "power_plant":
				return {"scale": Vector3(0.8, 0.55, 0.8), "color": Color(0.92, 0.58, 0.12)}
			"turret", "defence_turret":
				return {"scale": Vector3(0.55, 0.65, 0.55), "color": Color(0.62, 0.62, 0.62)}
			_:
				return {"scale": Vector3(0.9, 0.7, 0.9), "color": Color(0.55, 0.55, 0.62)}
	match btype:
		"headquarters":
			return {"scale": Vector3(1.3, 0.95, 1.3), "color": Color(0.92, 0.78, 0.18)}
		"woodcutter":
			return {"scale": Vector3(0.75, 0.55, 0.75), "color": Color(0.62, 0.38, 0.18)}
		"sawmill":
			return {"scale": Vector3(0.95, 0.65, 0.85), "color": Color(0.72, 0.52, 0.28)}
		"forester":
			return {"scale": Vector3(0.7, 0.5, 0.7), "color": Color(0.28, 0.62, 0.32)}
		"quarry":
			return {"scale": Vector3(0.85, 0.45, 0.85), "color": Color(0.55, 0.55, 0.58)}
		_:
			return {"scale": Vector3(0.8, 0.6, 0.8), "color": Color(0.65, 0.65, 0.68)}


func _unit_spec(utype: String) -> Dictionary:
	match utype:
		"rifle_squad", "soldier":
			return {"scale": Vector3(0.35, 0.55, 0.35), "color": Color(0.72, 0.72, 0.78)}
		"scout":
			return {"scale": Vector3(0.3, 0.45, 0.3), "color": Color(0.78, 0.78, 0.55)}
		"harvester", "collector":
			return {"scale": Vector3(0.55, 0.4, 0.75), "color": Color(0.82, 0.62, 0.18)}
		_:
			return {"scale": Vector3(0.35, 0.5, 0.35), "color": Color(0.7, 0.7, 0.7)}


func _make_mesh_instance(mesh: Mesh, color: Color) -> MeshInstance3D:
	var inst := MeshInstance3D.new()
	inst.mesh = mesh
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	inst.material_override = mat
	return inst


func _tint(team: Color, base: Color) -> Color:
	return Color(
		(base.r + team.r) * 0.5,
		(base.g + team.g) * 0.5,
		(base.b + team.b) * 0.5,
		base.a,
	)


func _clear_children(node: Node3D) -> void:
	for child in node.get_children():
		node.remove_child(child)
		child.free()


static func _normalize_snapshot(data: Dictionary) -> Dictionary:
	var normalized: Dictionary = data.duplicate(true)
	if normalized.get("terrain", []).is_empty():
		var layers: Dictionary = normalized.get("display_layers", {})
		if typeof(layers) == TYPE_DICTIONARY and not layers.get("terrain", []).is_empty():
			normalized["terrain"] = layers.get("terrain", [])
	if normalized.get("carriers", []).is_empty() and not normalized.get("carrier_markers", []).is_empty():
		normalized["carriers"] = normalized.get("carrier_markers", [])
	return normalized


static func _terrain_tiles(data: Dictionary) -> Array:
	var snap: Dictionary = _normalize_snapshot(data)
	return snap.get("terrain", [])


static func _carriers(data: Dictionary) -> Array:
	var snap: Dictionary = _normalize_snapshot(data)
	return snap.get("carriers", [])
