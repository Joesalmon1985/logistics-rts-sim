class_name MapViewport
extends Control

const HEX_SIZE: float = 22.0

var render_data: Dictionary = {}
var show_grid: bool = false
var selected_q: int = -1
var selected_r: int = -1
var pan_offset: Vector2 = Vector2.ZERO
var zoom: float = 1.0

var _terrain_colors: Dictionary = {
	"grass": Color(0.35, 0.65, 0.30),
	"forest": Color(0.15, 0.45, 0.20),
	"water": Color(0.20, 0.40, 0.75),
	"mountain": Color(0.45, 0.42, 0.38),
	"desert": Color(0.75, 0.65, 0.35),
}

var _building_colors: Dictionary = {
	"headquarters": Color(0.85, 0.75, 0.20),
	"command_hub": Color(0.85, 0.75, 0.20),
	"woodcutter": Color(0.55, 0.35, 0.15),
	"sawmill": Color(0.70, 0.50, 0.25),
	"power_plant": Color(0.90, 0.55, 0.10),
	"barracks": Color(0.55, 0.55, 0.70),
	"resource_processor": Color(0.40, 0.70, 0.85),
}


func set_render_data(data: Dictionary) -> void:
	render_data = data
	queue_redraw()


func set_selection(q: int, r: int) -> void:
	selected_q = q
	selected_r = r
	queue_redraw()


func _draw() -> void:
	if render_data.is_empty():
		draw_string(_default_font(), Vector2(12, 24), "No snapshot loaded", HORIZONTAL_ALIGNMENT_LEFT, -1, 16)
		return
	var origin: Vector2 = size * 0.5 + pan_offset
	for tile in render_data.get("terrain", []):
		var q: int = int(tile.get("q", 0))
		var r: int = int(tile.get("r", 0))
		var terrain_type: String = str(tile.get("terrain_type", "grass"))
		var color: Color = _terrain_colors.get(terrain_type, Color(0.4, 0.4, 0.4))
		_draw_hex(origin, q, r, color)
	for road in render_data.get("roads", []):
		var a: Vector2 = _hex_center(origin, int(road.get("from_q", 0)), int(road.get("from_r", 0)))
		var b: Vector2 = _hex_center(origin, int(road.get("to_q", 0)), int(road.get("to_r", 0)))
		draw_line(a, b, Color(0.85, 0.75, 0.35), 3.0 * zoom)
	for building in render_data.get("buildings", []):
		var q: int = int(building.get("q", 0))
		var r: int = int(building.get("r", 0))
		var btype: String = str(building.get("building_type", ""))
		var color: Color = _building_colors.get(btype, Color(0.7, 0.7, 0.7))
		_draw_hex(origin, q, r, color, 0.85)
		var center: Vector2 = _hex_center(origin, q, r)
		draw_string(_default_font(), center + Vector2(-8, 4), btype.substr(0, 3), HORIZONTAL_ALIGNMENT_LEFT, -1, 10)
	for unit in render_data.get("units", []):
		var q2: int = int(unit.get("q", 0))
		var r2: int = int(unit.get("r", 0))
		var center2: Vector2 = _hex_center(origin, q2, r2)
		draw_circle(center2, 8.0 * zoom, Color(0.90, 0.25, 0.25))
		draw_string(_default_font(), center2 + Vector2(-6, 4), "U", HORIZONTAL_ALIGNMENT_LEFT, -1, 12)
	if show_grid:
		for tile2 in render_data.get("terrain", []):
			var q3: int = int(tile2.get("q", 0))
			var r3: int = int(tile2.get("r", 0))
			var center3: Vector2 = _hex_center(origin, q3, r3)
			draw_string(_default_font(), center3 + Vector2(-10, -8), "%d,%d" % [q3, r3], HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color(1, 1, 1, 0.7))
	if selected_q >= 0 and selected_r >= 0:
		_draw_hex(origin, selected_q, selected_r, Color(1, 1, 1, 0.15), 1.0, true)


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
			if dist < HEX_SIZE * zoom and dist < best_dist:
				best_dist = dist
				best_q = q
				best_r = r
		if best_q >= 0:
			set_selection(best_q, best_r)
			selection_clicked.emit(best_q, best_r)


signal selection_clicked(q: int, r: int)


func _hex_center(origin: Vector2, q: int, r: int) -> Vector2:
	var x: float = HEX_SIZE * sqrt(3.0) * (float(q) + float(r) * 0.5) * zoom
	var y: float = HEX_SIZE * 1.5 * float(r) * zoom
	return origin + Vector2(x, y)


func _draw_hex(origin: Vector2, q: int, r: int, color: Color, scale: float = 1.0, outline_only: bool = false) -> void:
	var center: Vector2 = _hex_center(origin, q, r)
	var points: PackedVector2Array = PackedVector2Array()
	for i in range(6):
		var angle: float = PI / 6.0 + float(i) * PI / 3.0
		var radius: float = HEX_SIZE * zoom * scale
		points.append(center + Vector2(cos(angle), sin(angle)) * radius)
	if outline_only:
		draw_polyline(points + PackedVector2Array([points[0]]), Color(1, 1, 1, 0.9), 2.0)
	else:
		draw_colored_polygon(points, color)
		draw_polyline(points + PackedVector2Array([points[0]]), Color(0, 0, 0, 0.35), 1.0)


func _default_font() -> Font:
	return ThemeDB.fallback_font
