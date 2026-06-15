class_name MapViewport
extends Control

const HEX_SIZE: float = 24.0

var render_data: Dictionary = {}
var show_grid: bool = true
var show_coord_labels: bool = true
var selected_q: int = -1
var selected_r: int = -1
var attack_target_q: int = -1
var attack_target_r: int = -1
var pan_offset: Vector2 = Vector2.ZERO
var zoom: float = 1.0
var show_forest_density: bool = true
var show_logistics: bool = true
var production_pulse_ids: Array = []
var map_title: String = "Map"

## Palette constants shared with viewport pixel probes (read-only display).
const COLOR_GRASS := Color(0.38, 0.72, 0.32)
const COLOR_FOREST := Color(0.12, 0.42, 0.18)
const COLOR_MAP_BG := Color(0.12, 0.14, 0.16)
var _terrain_colors: Dictionary = {
	"grass": COLOR_GRASS,
	"forest": COLOR_FOREST,
	"water": Color(0.22, 0.48, 0.82),
	"mountain": Color(0.48, 0.44, 0.40),
	"desert": Color(0.78, 0.68, 0.38),
}

var _building_colors: Dictionary = {
	"headquarters": Color(0.92, 0.78, 0.18),
	"command_hub": Color(0.92, 0.78, 0.18),
	"woodcutter": Color(0.62, 0.38, 0.18),
	"sawmill": Color(0.72, 0.52, 0.28),
	"storehouse": Color(0.55, 0.55, 0.62),
	"power_generator": Color(0.92, 0.58, 0.12),
	"power_plant": Color(0.92, 0.58, 0.12),
	"barracks": Color(0.52, 0.52, 0.78),
	"resource_processor": Color(0.38, 0.72, 0.88),
}

var _building_short_labels: Dictionary = {
	"headquarters": "HQ",
	"command_hub": "HUB",
	"woodcutter": "WOOD",
	"sawmill": "SAW",
	"storehouse": "STORE",
	"power_generator": "PWR",
	"power_plant": "PWR",
	"barracks": "BAR",
	"resource_processor": "PROC",
}


func set_render_data(data: Dictionary) -> void:
	render_data = data
	fit_to_content()
	queue_redraw()


func set_selection(q: int, r: int) -> void:
	selected_q = q
	selected_r = r
	queue_redraw()


func set_attack_target(q: int, r: int) -> void:
	attack_target_q = q
	attack_target_r = r
	queue_redraw()


func fit_to_content() -> void:
	var terrain: Array = render_data.get("terrain", [])
	if terrain.is_empty() or size.x < 10 or size.y < 10:
		return
	var min_p := Vector2(INF, INF)
	var max_p := Vector2(-INF, -INF)
	for tile in terrain:
		var q: int = int(tile.get("q", 0))
		var r: int = int(tile.get("r", 0))
		var p: Vector2 = _hex_offset(q, r)
		min_p.x = minf(min_p.x, p.x)
		min_p.y = minf(min_p.y, p.y)
		max_p.x = maxf(max_p.x, p.x)
		max_p.y = maxf(max_p.y, p.y)
	var content_size: Vector2 = max_p - min_p + Vector2(HEX_SIZE * 2.5, HEX_SIZE * 2.5)
	var avail: Vector2 = size - Vector2(48, 48)
	if content_size.x > 0 and content_size.y > 0:
		zoom = minf(avail.x / content_size.x, avail.y / content_size.y)
		zoom = clampf(zoom, 1.2, 3.2)
	var center: Vector2 = (min_p + max_p) * 0.5
	pan_offset = -center * zoom


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		if not render_data.is_empty() and size.x >= 10 and size.y >= 10:
			fit_to_content()
			queue_redraw()


func hex_center_local(q: int, r: int) -> Vector2:
	var origin: Vector2 = size * 0.5 + pan_offset
	return _hex_center(origin, q, r)


func map_panel_rect_in_viewport(viewport_size: Vector2i) -> Rect2i:
	var global_pos: Vector2 = get_global_position()
	return Rect2i(int(global_pos.x), int(global_pos.y), int(size.x), int(size.y))


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), Color(0.12, 0.14, 0.16))
	if render_data.is_empty():
		draw_string(_default_font(), Vector2(16, 32), "No snapshot loaded — click Load/Reset Demo", HORIZONTAL_ALIGNMENT_LEFT, -1, 18)
		return

	_draw_title_bar()
	var origin: Vector2 = size * 0.5 + pan_offset
	_draw_fog(origin)
	for tile in render_data.get("terrain", []):
		var q: int = int(tile.get("q", 0))
		var r: int = int(tile.get("r", 0))
		var terrain_type: String = str(tile.get("terrain_type", "grass"))
		var color: Color = _terrain_colors.get(terrain_type, Color(0.45, 0.45, 0.45))
		_draw_hex(origin, q, r, color)
	if show_forest_density:
		for td in render_data.get("tree_density", []):
			_draw_tree_density(origin, td)
	for res in render_data.get("resources", []):
		_draw_resource(origin, res)
	for road in render_data.get("roads", []):
		var a: Vector2 = _hex_center(origin, int(road.get("from_q", 0)), int(road.get("from_r", 0)))
		var b: Vector2 = _hex_center(origin, int(road.get("to_q", 0)), int(road.get("to_r", 0)))
		draw_line(a, b, Color(0.95, 0.82, 0.25), 4.0 * zoom)
	if show_logistics:
		for route in render_data.get("ware_routes", []):
			_draw_ware_route(origin, route)
		for job in render_data.get("delivery_jobs", []):
			_draw_delivery_job(origin, job)
	for site in render_data.get("construction_sites", []):
		_draw_construction_site(origin, site)
	for building in render_data.get("buildings", []):
		_draw_building(origin, building)
	if show_logistics:
		for carrier in render_data.get("carriers", []):
			_draw_carrier(origin, carrier)
	for unit in render_data.get("units", []):
		_draw_unit(origin, unit)
	if attack_target_q >= 0 and attack_target_r >= 0:
		_draw_hex(origin, attack_target_q, attack_target_r, Color(1, 0.2, 0.2, 0.15), 1.0, true)
		var atk_center: Vector2 = _hex_center(origin, attack_target_q, attack_target_r)
		draw_arc(atk_center, HEX_SIZE * zoom * 0.75, 0, TAU, 24, Color(1, 0.35, 0.35, 0.9), 2.5)
	if show_grid or show_coord_labels:
		for tile2 in render_data.get("terrain", []):
			var q3: int = int(tile2.get("q", 0))
			var r3: int = int(tile2.get("r", 0))
			if show_coord_labels:
				var center3: Vector2 = _hex_center(origin, q3, r3)
				draw_string(_default_font(), center3 + Vector2(-12, 4), "%d,%d" % [q3, r3], HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color(1, 1, 1, 0.85))
	if selected_q >= 0 and selected_r >= 0:
		_draw_hex(origin, selected_q, selected_r, Color(1, 1, 1, 0.12), 1.0, true)
		var sel_center: Vector2 = _hex_center(origin, selected_q, selected_r)
		draw_arc(sel_center, HEX_SIZE * zoom * 0.95, 0, TAU, 32, Color(1, 1, 0.4), 3.0)


func _draw_title_bar() -> void:
	var counts: Dictionary = LayerRenderer.count_entities(render_data)
	draw_rect(Rect2(8, 8, size.x - 16, 28), Color(0, 0, 0, 0.55))
	draw_string(_default_font(), Vector2(16, 28), "%s — bld:%d sites:%d roads:%d carriers:%d routes:%d (click hex)" % [
		map_title,
		int(counts.get("buildings", 0)),
		int(counts.get("construction_sites", 0)),
		int(counts.get("roads", 0)),
		int(counts.get("carriers", 0)),
		int(counts.get("ware_routes", 0)),
	], HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color(0.95, 0.95, 0.95))


func _draw_building(origin: Vector2, building: Dictionary) -> void:
	var q: int = int(building.get("q", 0))
	var r: int = int(building.get("r", 0))
	var btype: String = str(building.get("building_type", ""))
	var player_id: String = str(building.get("player_id", ""))
	var color: Color = _building_colors.get(btype, Color(0.7, 0.7, 0.7))
	_draw_hex(origin, q, r, color, 0.88)
	var center: Vector2 = _hex_center(origin, q, r)
	var border: Color = _player_color(player_id)
	draw_arc(center, HEX_SIZE * zoom * 0.82, 0, TAU, 24, border, 2.5)
	var label: String = str(_building_short_labels.get(btype, btype.substr(0, 4).to_upper()))
	draw_string(_default_font(), center + Vector2(-16, 5), label, HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color(1, 1, 1))
	var store_label: String = _store_label_for_building(building.get("id", 0))
	if store_label != "":
		draw_string(_default_font(), center + Vector2(-18, 18), store_label, HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color(0.85, 1.0, 0.85))
	var prod: Dictionary = _production_for_building(building.get("id", 0))
	if int(prod.get("production_progress", 0)) > 0:
		draw_string(_default_font(), center + Vector2(-10, -16 * zoom), "P%d" % int(prod.get("production_progress", 0)), HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color(1, 0.9, 0.5))
	if int(building.get("id", 0)) in production_pulse_ids:
		draw_arc(center, HEX_SIZE * zoom * 0.95, 0, TAU, 32, Color(0.4, 1.0, 0.5, 0.85), 3.0)
	var worker_badges: Dictionary = render_data.get("worker_badges", {})
	var badge_count: int = int(worker_badges.get(str(building.get("id", 0)), 0))
	if badge_count > 0:
		draw_circle(center + Vector2(14 * zoom, -12 * zoom), 8.0 * zoom, Color(0.15, 0.15, 0.2, 0.85))
		draw_string(_default_font(), center + Vector2(10 * zoom, -8 * zoom), "W%d" % badge_count, HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color(0.9, 0.95, 1.0))
	var max_hp: int = int(building.get("max_hp", 0))
	if max_hp > 0:
		_draw_health_bar(center, int(building.get("hp", 0)), max_hp)


func _draw_construction_site(origin: Vector2, site: Dictionary) -> void:
	var q: int = int(site.get("q", 0))
	var r: int = int(site.get("r", 0))
	var btype: String = str(site.get("building_type", ""))
	var player_id: String = str(site.get("player_id", ""))
	var center: Vector2 = _hex_center(origin, q, r)
	_draw_hex(origin, q, r, Color(0.25, 0.25, 0.28, 0.35), 0.92, true)
	draw_arc(center, HEX_SIZE * zoom * 0.82, 0, TAU, 24, _player_color(player_id), 2.0)
	var progress: int = int(site.get("progress_percent", 0))
	var arc_end: float = TAU * float(progress) / 100.0
	if progress > 0:
		draw_arc(center, HEX_SIZE * zoom * 0.72, -PI / 2.0, -PI / 2.0 + arc_end, 24, Color(0.35, 0.95, 0.45), 3.5)
	var short_label: String = str(_building_short_labels.get(btype, btype.substr(0, 3).to_upper()))
	draw_string(_default_font(), center + Vector2(-18, -2), short_label, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color(0.95, 0.95, 0.95))
	draw_string(_default_font(), center + Vector2(-14, 12), "%d%%" % progress, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color(0.75, 1.0, 0.75))


func _draw_carrier(origin: Vector2, carrier: Dictionary) -> void:
	var qf: float = float(carrier.get("q", -1))
	var rf: float = float(carrier.get("r", -1))
	if qf < 0 or rf < 0:
		return
	var center: Vector2 = _hex_center_float(origin, qf, rf)
	var carrying: bool = str(carrier.get("ware_type", "")) != ""
	var dot_color: Color = Color(0.95, 0.55, 0.15) if carrying else Color(0.55, 0.75, 0.95)
	draw_circle(center + Vector2(0, 0), 7.0 * zoom, dot_color)
	draw_circle(center + Vector2(0, 0), 7.0 * zoom, Color(1, 1, 1, 0.35), false, 1.5)
	if carrying:
		draw_string(_default_font(), center + Vector2(8 * zoom, -4 * zoom), str(carrier.get("ware_type", "")).substr(0, 3), HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color(1, 0.95, 0.7))


func _draw_tree_density(origin: Vector2, td: Dictionary) -> void:
	var q: int = int(td.get("q", 0))
	var r: int = int(td.get("r", 0))
	var density: int = int(td.get("tree_density", 0))
	var center: Vector2 = _hex_center(origin, q, r)
	draw_string(_default_font(), center + Vector2(-10, -14 * zoom), "D:%d" % density, HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color(0.75, 1.0, 0.75))


func _draw_ware_route(origin: Vector2, route: Dictionary) -> void:
	var points: Array = route.get("points", [])
	for i in range(points.size() - 1):
		var a: Dictionary = points[i]
		var b: Dictionary = points[i + 1]
		var pa: Vector2 = _hex_center(origin, int(a.get("q", 0)), int(a.get("r", 0)))
		var pb: Vector2 = _hex_center(origin, int(b.get("q", 0)), int(b.get("r", 0)))
		draw_dashed_line(pa, pb, Color(0.95, 0.65, 0.15, 0.85), 2.5 * zoom, 6.0 * zoom)


func _draw_delivery_job(origin: Vector2, job: Dictionary) -> void:
	var fq: int = int(job.get("from_q", -1))
	var fr: int = int(job.get("from_r", -1))
	var tq: int = int(job.get("to_q", -1))
	var tr: int = int(job.get("to_r", -1))
	if fq < 0 or tq < 0:
		return
	var a: Vector2 = _hex_center(origin, fq, fr)
	var b: Vector2 = _hex_center(origin, tq, tr)
	draw_line(a, b, Color(0.55, 0.85, 1.0, 0.45), 2.0 * zoom)
	var mid: Vector2 = (a + b) * 0.5
	draw_string(_default_font(), mid + Vector2(-8, -4), str(job.get("ware_type", "")).substr(0, 3), HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color(0.8, 0.95, 1.0))


func _store_label_for_building(building_id: int) -> String:
	for store in render_data.get("building_stores", []):
		if int(store.get("building_id", -1)) == building_id:
			return str(store.get("label", ""))
	return ""


func _production_for_building(building_id: int) -> Dictionary:
	for prod in render_data.get("production_activity", []):
		if int(prod.get("building_id", -1)) == building_id:
			return prod
	return {}


func _draw_unit(origin: Vector2, unit: Dictionary) -> void:
	var q2: int = int(unit.get("q", 0))
	var r2: int = int(unit.get("r", 0))
	var center2: Vector2 = _hex_center(origin, q2, r2)
	var player_id: String = str(unit.get("player_id", ""))
	var color: Color = _player_color(player_id)
	draw_circle(center2, 10.0 * zoom, color)
	var unit_label: String = str(unit.get("unit_type", "unit")).substr(0, 4)
	draw_string(_default_font(), center2 + Vector2(-14, 5), unit_label, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color(1, 1, 1))
	var max_hp: int = int(unit.get("max_hp", 0))
	if max_hp > 0:
		_draw_health_bar(center2, int(unit.get("hp", 0)), max_hp)
	var order: String = str(unit.get("order", ""))
	if order != "":
		draw_string(_default_font(), center2 + Vector2(-12, -14 * zoom), order.substr(0, 6), HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color(1, 0.85, 0.4))


func _draw_health_bar(center: Vector2, hp: int, max_hp: int) -> void:
	if max_hp <= 0:
		return
	var ratio: float = clampf(float(hp) / float(max_hp), 0.0, 1.0)
	var bar_w: float = 28.0 * zoom
	var bar_h: float = 5.0 * zoom
	var top_left: Vector2 = center + Vector2(-bar_w * 0.5, -18.0 * zoom)
	draw_rect(Rect2(top_left, Vector2(bar_w, bar_h)), Color(0.15, 0.15, 0.15, 0.85))
	var fill_color: Color = Color(0.35, 0.9, 0.35) if ratio > 0.5 else Color(0.95, 0.75, 0.2) if ratio > 0.25 else Color(0.95, 0.3, 0.25)
	draw_rect(Rect2(top_left, Vector2(bar_w * ratio, bar_h)), fill_color)
	draw_string(_default_font(), top_left + Vector2(0, -2), "%d/%d" % [hp, max_hp], HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color(1, 1, 1, 0.9))


func _draw_resource(origin: Vector2, res: Dictionary) -> void:
	var q: int = int(res.get("q", 0))
	var r: int = int(res.get("r", 0))
	var center: Vector2 = _hex_center(origin, q, r)
	draw_circle(center + Vector2(0, -8 * zoom), 5.0 * zoom, Color(0.95, 0.65, 0.15))
	var dtype: String = str(res.get("deposit_type", res.get("field_type", "res")))
	draw_string(_default_font(), center + Vector2(-10, -12 * zoom), dtype.substr(0, 3), HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color(1, 0.9, 0.5))


func _draw_fog(origin: Vector2) -> void:
	var fog: Dictionary = render_data.get("fog", {})
	if fog.is_empty():
		return
	for player_id in fog:
		var pdata: Dictionary = fog[player_id]
		var levels: Dictionary = pdata.get("levels", {})
		for key in levels:
			var parts: PackedStringArray = str(key).split(",")
			if parts.size() < 2:
				continue
			var q: int = int(parts[0])
			var r: int = int(parts[1])
			var level: int = int(levels[key])
			var alpha: float = 0.55 if level <= 0 else 0.25
			_draw_hex(origin, q, r, Color(0.05, 0.05, 0.08, alpha))


func _player_color(player_id: String) -> Color:
	if player_id == "p2":
		return Color(0.92, 0.28, 0.28)
	if player_id == "p1":
		return Color(0.28, 0.55, 0.95)
	return Color(0.85, 0.85, 0.85)


func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var origin: Vector2 = size * 0.5 + pan_offset
		var local: Vector2 = event.position
		var best_q: int = -1
		var best_r: int = -1
		var best_dist: float = 999999.0
		for tile in render_data.get("terrain", []):
			var q: int = int(tile.get("q", 0))
			var r: int = int(tile.get("r", 0))
			var center: Vector2 = _hex_center(origin, q, r)
			var dist: float = center.distance_to(local)
			if dist < HEX_SIZE * zoom * 1.05 and dist < best_dist:
				best_dist = dist
				best_q = q
				best_r = r
		if best_q >= 0:
			set_selection(best_q, best_r)
			selection_clicked.emit(best_q, best_r)


signal selection_clicked(q: int, r: int)


func _hex_offset(q: int, r: int) -> Vector2:
	var x: float = HEX_SIZE * sqrt(3.0) * (float(q) + float(r) * 0.5)
	var y: float = HEX_SIZE * 1.5 * float(r)
	return Vector2(x, y)


func _hex_center(origin: Vector2, q: int, r: int) -> Vector2:
	return origin + _hex_offset(q, r) * zoom


func _hex_center_float(origin: Vector2, q: float, r: float) -> Vector2:
	var x: float = HEX_SIZE * sqrt(3.0) * (q + r * 0.5)
	var y: float = HEX_SIZE * 1.5 * r
	return origin + Vector2(x, y) * zoom


func _draw_hex(origin: Vector2, q: int, r: int, color: Color, scale: float = 1.0, outline_only: bool = false) -> void:
	var center: Vector2 = _hex_center(origin, q, r)
	var points: PackedVector2Array = PackedVector2Array()
	for i in range(6):
		var angle: float = PI / 6.0 + float(i) * PI / 3.0
		var radius: float = HEX_SIZE * zoom * scale
		points.append(center + Vector2(cos(angle), sin(angle)) * radius)
	if outline_only:
		draw_polyline(points + PackedVector2Array([points[0]]), color, 2.5)
	else:
		draw_colored_polygon(points, color)
		draw_polyline(points + PackedVector2Array([points[0]]), Color(0, 0, 0, 0.45), 1.5)


func _default_font() -> Font:
	return ThemeDB.fallback_font
