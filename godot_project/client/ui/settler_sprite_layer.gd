class_name SettlerSpriteLayer
extends Control

## Draws registry sprites over MapViewport hex centers (viewer-only).

var map_viewport: MapViewport = null
var use_sprites: bool = true
var animation_enabled: bool = true
var _logged_fallback: bool = false
var _snapshot: Dictionary = {}
var _anim_phase: float = 0.0
var _process_connected: bool = false


func setup(viewport: MapViewport) -> void:
	map_viewport = viewport
	if not _process_connected:
		_process_connected = true
		set_process(true)


func set_snapshot(snapshot: Dictionary) -> void:
	_snapshot = snapshot


func _process(delta: float) -> void:
	if not animation_enabled:
		return
	_anim_phase += delta
	queue_redraw()


func _draw() -> void:
	if map_viewport == null or not use_sprites:
		return
	var render: Dictionary = map_viewport.render_data
	if render.is_empty():
		return
	var origin: Vector2 = map_viewport.size * 0.5 + map_viewport.pan_offset
	var zoom: float = map_viewport.zoom
	for tile in render.get("terrain", []):
		var tt: String = str(tile.get("terrain_type", "grass"))
		_draw_at_hex(origin, int(tile.get("q", 0)), int(tile.get("r", 0)), SettlerAssetRegistry.get_terrain_texture(tt), 0.45, zoom)
	for road in render.get("roads", []):
		_draw_road_segment(origin, road, zoom)
	for site in render.get("construction_sites", []):
		var sq: int = int(site.get("q", 0))
		var sr: int = int(site.get("r", 0))
		var pulse: float = 0.85 + 0.1 * sin(_anim_phase * 4.0 + float(sq + sr))
		_draw_at_hex(origin, sq, sr, SettlerAssetRegistry.get_building_texture("construction_site"), pulse, zoom)
		_draw_progress_ring(origin, sq, sr, int(site.get("progress_percent", 0)), zoom)
	for building in render.get("buildings", []):
		var btype: String = str(building.get("building_type", ""))
		var bq: int = int(building.get("q", 0))
		var br: int = int(building.get("r", 0))
		var alpha: float = 0.92
		if _building_is_waiting(building):
			alpha = 0.75 + 0.15 * sin(_anim_phase * 5.0)
			_draw_warning_icon(origin, bq, br, zoom)
		elif _building_is_working(building):
			alpha = 0.9 + 0.08 * sin(_anim_phase * 3.0 + float(bq))
		_draw_at_hex(origin, bq, br, SettlerAssetRegistry.get_building_texture(btype), alpha, zoom)
	for carrier in render.get("carriers", []):
		var cq: float = float(carrier.get("q", -1))
		var cr: float = float(carrier.get("r", -1))
		if cq < 0:
			continue
		var bob: float = 0.0
		if animation_enabled and str(carrier.get("state", "")) in ["moving", "assigned"]:
			bob = sin(_anim_phase * 6.0 + cq + cr) * 4.0 * zoom
		var center: Vector2 = map_viewport.hex_center_pixel_float(cq, cr) + Vector2(0, bob)
		_draw_texture_at(center, SettlerAssetRegistry.get_texture("logistics", "carrier"), 0.62 * zoom)
		var ware: String = str(carrier.get("ware_type", ""))
		if ware != "":
			var ware_tex: Texture2D = SettlerAssetRegistry.get_texture("wares", ware)
			if ware_tex != null:
				_draw_texture_at(center + Vector2(0, -18 * zoom), ware_tex, 0.35 * zoom)


func _draw_road_segment(origin: Vector2, road: Dictionary, zoom: float) -> void:
	var fq: int = int(road.get("from_q", road.get("hex_a_q", -1)))
	var fr: int = int(road.get("from_r", road.get("hex_a_r", -1)))
	var tq: int = int(road.get("to_q", road.get("hex_b_q", -1)))
	var tr: int = int(road.get("to_r", road.get("hex_b_r", -1)))
	if fq < 0 or tq < 0:
		return
	var a: Vector2 = map_viewport.hex_center_pixel(fq, fr)
	var b: Vector2 = map_viewport.hex_center_pixel(tq, tr)
	var road_tex: Texture2D = SettlerAssetRegistry.get_texture("logistics", "road")
	if road_tex != null:
		var mid: Vector2 = (a + b) * 0.5
		var dir: Vector2 = (b - a).normalized()
		var angle: float = dir.angle()
		var length: float = a.distance_to(b)
		var size := Vector2(length, 14.0 * zoom)
		draw_set_transform(mid, angle, Vector2.ONE)
		draw_texture_rect(road_tex, Rect2(-size * 0.5, size), false, Color(1, 1, 1, 0.85))
		draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	else:
		draw_line(a, b, Color(0.55, 0.42, 0.28, 0.9), 6.0 * zoom)


func _draw_progress_ring(origin: Vector2, q: int, r: int, percent: int, zoom: float) -> void:
	if percent <= 0:
		return
	var center: Vector2 = map_viewport.hex_center_pixel(q, r)
	var radius: float = 22.0 * zoom
	var start_angle: float = -PI * 0.5
	var end_angle: float = start_angle + TAU * clampf(float(percent) / 100.0, 0.0, 1.0)
	draw_arc(center, radius, start_angle, end_angle, 24, Color(0.35, 0.95, 0.45, 0.9), 3.0 * zoom)


func _draw_warning_icon(origin: Vector2, q: int, r: int, zoom: float) -> void:
	var center: Vector2 = map_viewport.hex_center_pixel(q, r) + Vector2(14 * zoom, -18 * zoom)
	var pulse: float = 0.7 + 0.3 * sin(_anim_phase * 7.0)
	draw_circle(center, 6.0 * zoom * pulse, Color(1.0, 0.75, 0.2, 0.95))


func _building_is_waiting(building: Dictionary) -> bool:
	var state: String = str(building.get("state", "")).to_lower()
	var paused: String = str(building.get("paused_reason", ""))
	return paused != "" or state.begins_with("paused") or state == "blocked"


func _building_is_working(building: Dictionary) -> bool:
	return str(building.get("state", "")).to_lower() in ["active", "working"]


func _draw_at_hex(origin: Vector2, q: int, r: int, tex: Texture2D, alpha: float, zoom: float) -> void:
	if tex == null:
		if not _logged_fallback:
			_logged_fallback = true
		return
	var center: Vector2 = map_viewport.hex_center_pixel(q, r)
	_draw_texture_at(center, tex, 0.78 * zoom, alpha)


func _draw_texture_at(center: Vector2, tex: Texture2D, scale_factor: float, alpha: float = 1.0) -> void:
	if tex == null:
		return
	var tex_size: Vector2 = tex.get_size() * scale_factor
	var rect := Rect2(center - tex_size * 0.5, tex_size)
	draw_texture_rect(tex, rect, false, Color(1, 1, 1, alpha))
