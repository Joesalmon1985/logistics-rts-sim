class_name ViewerPixelProbe
extends RefCounted

## Read-only viewport pixel sampling for RUN_05 observability tests (no sim logic).

const DEFAULT_TOLERANCE: float = 0.12


static func map_region_rect(img: Image) -> Rect2i:
	var w: int = img.get_width()
	var h: int = img.get_height()
	var map_w: int = maxi(1, int(float(w) * 0.65))
	return Rect2i(0, 0, map_w, h)


static func count_non_background(img: Image, region: Rect2i, bg: Color = MapViewport.COLOR_MAP_BG, tolerance: float = DEFAULT_TOLERANCE) -> int:
	var count: int = 0
	for y in range(region.position.y, region.position.y + region.size.y):
		if y < 0 or y >= img.get_height():
			continue
		for x in range(region.position.x, region.position.x + region.size.x):
			if x < 0 or x >= img.get_width():
				continue
			if not _color_near(img.get_pixel(x, y), bg, tolerance):
				count += 1
	return count


static func count_color_near(img: Image, target: Color, region: Rect2i, tolerance: float = DEFAULT_TOLERANCE) -> int:
	var count: int = 0
	for y in range(region.position.y, region.position.y + region.size.y):
		if y < 0 or y >= img.get_height():
			continue
		for x in range(region.position.x, region.position.x + region.size.x):
			if x < 0 or x >= img.get_width():
				continue
			if _color_near(img.get_pixel(x, y), target, tolerance):
				count += 1
	return count


static func image_diff_score(img_a: Image, img_b: Image, region: Rect2i, tolerance: float = DEFAULT_TOLERANCE) -> int:
	if img_a.get_width() != img_b.get_width() or img_a.get_height() != img_b.get_height():
		return 999999
	var diff: int = 0
	for y in range(region.position.y, region.position.y + region.size.y):
		if y < 0 or y >= img_a.get_height():
			continue
		for x in range(region.position.x, region.position.x + region.size.x):
			if x < 0 or x >= img_a.get_width():
				continue
			if not _color_near(img_a.get_pixel(x, y), img_b.get_pixel(x, y), tolerance):
				diff += 1
	return diff


static func sample_disk(img: Image, center: Vector2, radius: float, tolerance: float = DEFAULT_TOLERANCE) -> Dictionary:
	var counts: Dictionary = {}
	var r: int = maxi(1, int(ceil(radius)))
	var cx: int = int(round(center.x))
	var cy: int = int(round(center.y))
	for dy in range(-r, r + 1):
		for dx in range(-r, r + 1):
			if Vector2(dx, dy).length() > radius:
				continue
			var x: int = cx + dx
			var y: int = cy + dy
			if x < 0 or y < 0 or x >= img.get_width() or y >= img.get_height():
				continue
			var c: Color = img.get_pixel(x, y)
			if _color_near(c, MapViewport.COLOR_MAP_BG, tolerance):
				continue
			var key: String = _color_key(c)
			counts[key] = int(counts.get(key, 0)) + 1
	return counts


static func assert_economy_terrain_visible(img: Image) -> Dictionary:
	var region: Rect2i = map_region_rect(img)
	var total: int = region.size.x * region.size.y
	if total <= 0:
		return {"ok": false, "reason": "empty image region"}
	var non_bg: int = count_non_background(img, region)
	var grass: int = count_color_near(img, MapViewport.COLOR_GRASS, region)
	var forest: int = count_color_near(img, MapViewport.COLOR_FOREST, region)
	var min_non_bg: int = maxi(100, int(float(total) * 0.05))
	if non_bg < min_non_bg:
		return {"ok": false, "reason": "map region mostly background (%d/%d non-bg)" % [non_bg, total]}
	if grass + forest < 50:
		return {"ok": false, "reason": "grass/forest pixels too few (grass=%d forest=%d)" % [grass, forest]}
	return {"ok": true, "non_bg": non_bg, "grass": grass, "forest": forest}


static func assert_military_entities_visible(img: Image) -> Dictionary:
	var region: Rect2i = map_region_rect(img)
	var non_bg: int = count_non_background(img, region)
	var unit_blue: int = count_color_near(img, Color(0.28, 0.55, 0.95), region, 0.18)
	var unit_red: int = count_color_near(img, Color(0.92, 0.28, 0.28), region, 0.18)
	var hub_gold: int = count_color_near(img, Color(0.92, 0.78, 0.18), region, 0.18)
	if non_bg < 500:
		return {"ok": false, "reason": "military map region mostly blank"}
	if unit_blue + unit_red + hub_gold < 20:
		return {"ok": false, "reason": "military unit/building colors not detected"}
	return {"ok": true, "non_bg": non_bg}


static func hex_global_from_map(map: MapViewport, q: int, r: int) -> Vector2:
	return map.get_global_transform() * map.hex_center_local(q, r)


static func hex_region_in_viewport_image(map: MapViewport, img: Image, q: int, r: int, radius: float = 28.0) -> Rect2i:
	var center: Vector2 = hex_global_from_map(map, q, r)
	var cx: int = int(round(center.x))
	var cy: int = int(round(center.y))
	var ri: int = int(ceil(radius))
	return Rect2i(cx - ri, cy - ri, ri * 2, ri * 2)


static func _color_near(a: Color, b: Color, tolerance: float) -> bool:
	return absf(a.r - b.r) <= tolerance \
		and absf(a.g - b.g) <= tolerance \
		and absf(a.b - b.b) <= tolerance


static func _color_key(c: Color) -> String:
	return "%d,%d,%d" % [int(c.r * 255), int(c.g * 255), int(c.b * 255)]
