class_name SettlerSpriteLayer
extends Control

## Draws registry sprites over MapViewport hex centers (viewer-only).

var map_viewport: MapViewport = null
var use_sprites: bool = true
var _logged_fallback: bool = false


func setup(viewport: MapViewport) -> void:
	map_viewport = viewport


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
		_draw_at_hex(origin, int(tile.get("q", 0)), int(tile.get("r", 0)), SettlerAssetRegistry.get_terrain_texture(tt), 0.35, zoom)
	for site in render.get("construction_sites", []):
		_draw_at_hex(origin, int(site.get("q", 0)), int(site.get("r", 0)), SettlerAssetRegistry.get_building_texture("construction_site"), 0.85, zoom)
	for building in render.get("buildings", []):
		var btype: String = str(building.get("building_type", ""))
		_draw_at_hex(origin, int(building.get("q", 0)), int(building.get("r", 0)), SettlerAssetRegistry.get_building_texture(btype), 0.9, zoom)
	for carrier in render.get("carriers", []):
		var cq: float = float(carrier.get("q", -1))
		var cr: float = float(carrier.get("r", -1))
		if cq >= 0:
			var center: Vector2 = map_viewport.hex_center_pixel_float(cq, cr)
			_draw_texture_at(center, SettlerAssetRegistry.get_texture("logistics", "carrier"), 0.55 * zoom)


func _draw_at_hex(origin: Vector2, q: int, r: int, tex: Texture2D, alpha: float, zoom: float) -> void:
	if tex == null:
		if not _logged_fallback:
			_logged_fallback = true
		return
	var center: Vector2 = map_viewport.hex_center_pixel(q, r)
	_draw_texture_at(center, tex, 0.7 * zoom, alpha)


func _draw_texture_at(center: Vector2, tex: Texture2D, scale_factor: float, alpha: float = 1.0) -> void:
	if tex == null:
		return
	var tex_size: Vector2 = tex.get_size() * scale_factor
	var rect := Rect2(center - tex_size * 0.5, tex_size)
	draw_texture_rect(tex, rect, false, Color(1, 1, 1, alpha))
