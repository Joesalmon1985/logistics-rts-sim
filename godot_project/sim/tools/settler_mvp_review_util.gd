class_name SettlerMvpReviewUtil
extends RefCounted

## Probes Settler MVP scene layout/input for screenshot review artefacts.

const MIN_PNG_BYTES: int = 1000
const VIEWPORT_SIZE := Vector2i(1920, 1080)

const PANEL_NODE_PATHS: Dictionary = {
	"map_viewport": "%MapViewport",
	"objective_panel": "%ObjectivePanel",
	"stockpile_status": "%StockLabel",
	"inspector": "%Inspector",
	"controls_help": "%HelpPanel",
	"event_warnings": "%WarningsPanel",
	"play_button": "%PlayBtn",
	"status_label": "%StatusLabel",
}


static func screenshot_output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("../docs/audit/screenshots")


static func ensure_output_dir() -> String:
	var dir: String = screenshot_output_dir()
	DirAccess.make_dir_recursive_absolute(dir)
	return dir


static func probe_scene(scene: Node, viewport_size: Vector2i = VIEWPORT_SIZE) -> Dictionary:
	var review: Dictionary = {
		"scene_loaded": scene != null,
		"viewport_size": "%dx%d" % [viewport_size.x, viewport_size.y],
		"map_viewport_found": false,
		"hud_found": false,
		"objective_panel_found": false,
		"help_text_found": false,
		"map_rect": {},
		"panel_rects": {},
		"top_level_control_rects": [],
		"possible_fullscreen_input_blockers": [],
		"selected_hex_initial": {},
		"paused_initial": true,
		"panels_on_screen": true,
		"off_screen_panels": [],
	}
	if scene == null:
		return review

	var map: MapViewport = scene.get_node_or_null("%MapViewport") as MapViewport
	review["map_viewport_found"] = map != null
	if map != null:
		review["map_rect"] = _rect_dict(map.get_global_rect())
		review["selected_hex_initial"] = {"q": map.selected_q, "r": map.selected_r}

	var objective: Node = scene.get_node_or_null("%ObjectivePanel")
	review["objective_panel_found"] = objective != null
	var help: Node = scene.get_node_or_null("%HelpPanel")
	review["help_text_found"] = help != null
	var status: Node = scene.get_node_or_null("%StatusLabel")
	review["hud_found"] = status != null

	if "controller" in scene:
		review["paused_initial"] = bool(scene.controller.paused)

	for key in PANEL_NODE_PATHS:
		var node: Control = scene.get_node_or_null(str(PANEL_NODE_PATHS[key])) as Control
		if node == null:
			continue
		var rect: Rect2 = node.get_rect()
		var global_rect: Rect2 = node.get_global_rect()
		review["panel_rects"][key] = _rect_dict(global_rect)
		var visible_size: bool = rect.size.x > 1.0 and rect.size.y > 1.0
		if scene.get_parent() is SubViewport:
			visible_size = node.visible and (rect.size.x > 1.0 or node.custom_minimum_size.x > 1.0) and (rect.size.y > 1.0 or node.custom_minimum_size.y > 1.0)
		if not visible_size:
			review["panels_on_screen"] = false
			review["off_screen_panels"].append(key)
		elif not _rect_on_screen(global_rect, viewport_size) and not (scene.get_parent() is SubViewport):
			review["panels_on_screen"] = false
			review["off_screen_panels"].append(key)

	_collect_top_level_controls(scene, review)
	_collect_input_blockers(scene, map, review)
	return review


static func _collect_top_level_controls(scene: Node, review: Dictionary) -> void:
	if not (scene is Control):
		return
	for child in scene.get_children():
		if child is Control:
			review["top_level_control_rects"].append({
				"name": child.name,
				"rect": _rect_dict((child as Control).get_global_rect()),
				"mouse_filter": (child as Control).mouse_filter,
			})


static func _collect_input_blockers(scene: Node, map: MapViewport, review: Dictionary) -> void:
	if map == null:
		return
	var map_rect: Rect2 = map.get_global_rect()
	_walk_controls(scene, func(node: Control) -> void:
		if node == map:
			return
		if node.mouse_filter != Control.MOUSE_FILTER_STOP:
			return
		var rect: Rect2 = node.get_global_rect()
		if rect.size.x < 4.0 or rect.size.y < 4.0:
			return
		if _rect_covers(rect, map_rect):
			review["possible_fullscreen_input_blockers"].append({
				"name": node.name,
				"path": str(node.get_path()),
				"rect": _rect_dict(rect),
			})
	)


static func _walk_controls(node: Node, callback: Callable) -> void:
	if node is Control:
		callback.call(node)
	for child in node.get_children():
		_walk_controls(child, callback)


static func _rect_dict(rect: Rect2) -> Dictionary:
	return {
		"x": rect.position.x,
		"y": rect.position.y,
		"w": rect.size.x,
		"h": rect.size.y,
	}


static func _rect_on_screen(rect: Rect2, viewport_size: Vector2i) -> bool:
	var viewport_rect := Rect2(Vector2.ZERO, Vector2(viewport_size))
	return viewport_rect.intersects(rect) and rect.size.x > 1.0 and rect.size.y > 1.0


static func _rect_covers(outer: Rect2, inner: Rect2) -> bool:
	return outer.encloses(inner)


static func check_png_sanity(path: String) -> Dictionary:
	var result: Dictionary = {
		"exists": false,
		"size_bytes": 0,
		"non_blank": false,
		"ok": false,
	}
	if not FileAccess.file_exists(path):
		return result
	result["exists"] = true
	result["size_bytes"] = FileAccess.get_file_as_bytes(path).size()
	if result["size_bytes"] < MIN_PNG_BYTES:
		return result
	var img := Image.new()
	if img.load(path) != OK:
		return result
	result["non_blank"] = not _image_is_blank(img)
	result["ok"] = result["non_blank"]
	return result


static func _image_is_blank(img: Image) -> bool:
	if img.is_empty():
		return true
	var w: int = img.get_width()
	var h: int = img.get_height()
	if w <= 0 or h <= 0:
		return true
	var samples: Array[Vector2i] = [
		Vector2i(0, 0),
		Vector2i(w / 2, h / 2),
		Vector2i(w - 1, h - 1),
		Vector2i(w / 4, h / 4),
		Vector2i(3 * w / 4, 3 * h / 4),
	]
	var non_transparent: int = 0
	for p in samples:
		var c: Color = img.get_pixel(p.x, p.y)
		if c.a > 0.05 and (c.r > 0.05 or c.g > 0.05 or c.b > 0.05):
			non_transparent += 1
	return non_transparent == 0


static func write_review_files(base_name: String, review: Dictionary, screenshot_path: String = "") -> bool:
	var dir: String = ensure_output_dir()
	review["screenshot_path"] = screenshot_path
	var json_path: String = dir.path_join("%s_review.json" % base_name)
	var md_path: String = dir.path_join("%s_review.md" % base_name)
	if not SimPaths.write_text_file(json_path, JSON.stringify(review, "\t")):
		return false
	return SimPaths.write_text_file(md_path, build_markdown(review))


static func build_markdown(review: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray(["# Settler MVP Screenshot Review", ""])
	for key in [
		"scene_loaded", "viewport_size", "map_viewport_found", "hud_found",
		"objective_panel_found", "help_text_found", "paused_initial",
		"panels_on_screen", "screenshot_path",
	]:
		if review.has(key):
			lines.append("- %s: %s" % [key, str(review[key])])
	if review.has("off_screen_panels"):
		lines.append("- off_screen_panels: %s" % str(review["off_screen_panels"]))
	if review.has("map_rect"):
		lines.append("- map_rect: %s" % str(review["map_rect"]))
	if review.has("panel_rects"):
		lines.append("- panel_rects: %s" % str(review["panel_rects"]))
	if review.has("possible_fullscreen_input_blockers"):
		lines.append("- possible_fullscreen_input_blockers: %s" % str(review["possible_fullscreen_input_blockers"]))
	return "\n".join(lines)
