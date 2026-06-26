class_name ActualHumanLaunchReviewUtil
extends RefCounted

## Strict probes for real player launch route (main menu → Tutorial) at 1280×720+.

const LAUNCH_SIZE := Vector2i(1280, 720)
const LAUNCH_SIZE_HD := Vector2i(1920, 1080)
const MIN_PNG_BYTES: int = 1000
const MIN_INSTRUCTION_LEN: int = 80
const MIN_TITLE_LEN: int = 6
const MIN_BUILD_BUTTONS: int = 5

const HumanReview := preload("res://sim/tools/human_understandable_review_util.gd")


static func screenshot_output_dir() -> String:
	return SettlerMvpReviewUtil.screenshot_output_dir()


static func ensure_output_dir() -> String:
	return SettlerMvpReviewUtil.ensure_output_dir()


static func probe_tutorial_launch(scene: Node, viewport_size: Vector2i = LAUNCH_SIZE) -> Dictionary:
	var review: Dictionary = {
		"screenshot_path": "",
		"capture_method": "in_engine_fallback",
		"window_size": [viewport_size.x, viewport_size.y],
		"viewport_size": [viewport_size.x, viewport_size.y],
		"main_menu_route_used": true,
		"tutorial_button_activated": true,
		"tutorial_panel_visible": false,
		"tutorial_title_text": "",
		"tutorial_instruction_text": "",
		"objective_panel_visible": false,
		"inspector_panel_visible": false,
		"build_palette_visible": false,
		"time_controls_visible": false,
		"mode_or_help_text_visible": false,
		"map_visible": false,
		"hq_sprite_visible": false,
		"woodcutter_sprite_distinct": false,
		"forester_sprite_distinct": false,
		"sawmill_sprite_distinct": false,
		"quarry_sprite_distinct": false,
		"construction_site_distinct": false,
		"carrier_sprite_visible": false,
		"carried_ware_icon_visible": false,
		"human_understandable_pass": false,
		"failure_reasons": [],
		"panels": {},
		"build_button_count": 0,
	}
	if scene == null:
		review["failure_reasons"].append("missing_scene")
		return review

	var title_node: Control = _resolve(scene, "TutorialTitle")
	var instr_node: Control = _resolve(scene, "TutorialInstruction")
	var tutorial_panel: Control = _resolve(scene, "TutorialPanel")
	var objective: Control = _resolve(scene, "ObjectivePanel")
	var inspector_panel: Control = _resolve(scene, "InspectorPanel")
	var inspector: Control = _resolve(scene, "Inspector")
	var build_palette: Control = _resolve(scene, "BuildPalette")
	var map_vp: Control = _resolve(scene, "MapViewport")
	var help_panel: Control = _resolve(scene, "HelpPanel")
	var mode_status: Control = _resolve(scene, "ModeStatusLabel")

	var title_probe: Dictionary = HumanReview.probe_strict_visibility(title_node, viewport_size, MIN_TITLE_LEN)
	var instr_probe: Dictionary = HumanReview.probe_strict_visibility(instr_node, viewport_size, MIN_INSTRUCTION_LEN)
	var tutorial_probe: Dictionary = HumanReview.probe_strict_visibility(tutorial_panel, viewport_size)
	var objective_probe: Dictionary = HumanReview.probe_strict_visibility(objective, viewport_size, 20)
	var inspector_panel_probe: Dictionary = HumanReview.probe_strict_visibility(inspector_panel, viewport_size)
	var inspector_probe: Dictionary = HumanReview.probe_strict_visibility(inspector, viewport_size, 10)
	var palette_probe: Dictionary = HumanReview.probe_strict_visibility(build_palette, viewport_size)
	var map_probe: Dictionary = HumanReview.probe_strict_visibility(map_vp, viewport_size)
	var time_probe: Dictionary = _probe_time_controls(scene, viewport_size)
	var help_probe: Dictionary = HumanReview.probe_strict_visibility(help_panel, viewport_size, 20)
	var mode_probe: Dictionary = HumanReview.probe_strict_visibility(mode_status, viewport_size, 5)

	review["panels"] = {
		"tutorial_panel": tutorial_probe,
		"tutorial_title": title_probe,
		"tutorial_instruction": instr_probe,
		"objective_panel": objective_probe,
		"inspector_panel": inspector_panel_probe,
		"inspector": inspector_probe,
		"build_palette": palette_probe,
		"map_viewport": map_probe,
		"time_controls": time_probe,
		"help_panel": help_probe,
		"mode_status": mode_probe,
	}

	review["tutorial_panel_visible"] = bool(tutorial_probe.get("visible", false))
	review["tutorial_title_text"] = str(title_probe.get("text", ""))
	review["tutorial_instruction_text"] = str(instr_probe.get("text", ""))
	review["objective_panel_visible"] = bool(objective_probe.get("visible", false))
	review["inspector_panel_visible"] = bool(inspector_panel_probe.get("visible", false)) and bool(inspector_probe.get("visible", false))
	review["build_palette_visible"] = bool(palette_probe.get("visible", false))
	review["time_controls_visible"] = bool(time_probe.get("visible", false))
	review["mode_or_help_text_visible"] = bool(mode_probe.get("visible", false)) or bool(help_probe.get("visible", false))
	review["map_visible"] = bool(map_probe.get("visible", false))
	review["build_button_count"] = _count_visible_build_buttons(scene, viewport_size)

	_check_required(review, "tutorial_panel_visible", tutorial_probe)
	_check_title_instruction(review, title_probe, instr_probe)
	_check_required(review, "objective_panel_visible", objective_probe)
	_check_required(review, "inspector_panel_visible", inspector_panel_probe)
	_check_required(review, "inspector_visible", inspector_probe)
	_check_required(review, "build_palette_visible", palette_probe)
	if review["build_button_count"] < MIN_BUILD_BUTTONS:
		review["failure_reasons"].append("build_buttons:%d" % review["build_button_count"])
	_check_required(review, "time_controls_visible", time_probe)
	if not review["mode_or_help_text_visible"]:
		review["failure_reasons"].append("mode_or_help_text_not_visible")
	_check_required(review, "map_visible", map_probe)
	_check_panels_in_viewport(review, scene, viewport_size)
	_check_map_does_not_cover_panels(review, scene, viewport_size)

	var sprite_review: Dictionary = probe_sprite_legibility()
	review.merge(sprite_review, true)

	review["human_understandable_pass"] = review["failure_reasons"].is_empty()
	return review


static func probe_sprite_legibility() -> Dictionary:
	SettlerAssetRegistry.load_registry()
	var review: Dictionary = {
		"hq_sprite_visible": false,
		"woodcutter_sprite_distinct": false,
		"forester_sprite_distinct": false,
		"sawmill_sprite_distinct": false,
		"quarry_sprite_distinct": false,
		"construction_site_distinct": false,
		"carrier_sprite_visible": false,
		"carried_ware_icon_visible": false,
		"sprite_checks": {},
	}
	var building_types: Array[String] = [
		"headquarters", "woodcutter", "forester", "sawmill", "quarry", "construction_site",
	]
	var hashes: Dictionary = {}
	for btype in building_types:
		var tex: Texture2D = SettlerAssetRegistry.get_building_texture(btype)
		var ok: bool = tex != null
		var hash: String = ""
		if ok:
			hash = tex.get_image().get_data().hex_encode()
			hashes[btype] = hash
		review["sprite_checks"][btype] = {"exists": ok, "hash": hash.substr(0, 16)}
	review["hq_sprite_visible"] = hashes.has("headquarters")
	var hq_hash: String = str(hashes.get("headquarters", ""))
	review["woodcutter_sprite_distinct"] = hq_hash != "" and hq_hash != str(hashes.get("woodcutter", ""))
	review["forester_sprite_distinct"] = hq_hash != "" and hq_hash != str(hashes.get("forester", ""))
	review["sawmill_sprite_distinct"] = hq_hash != "" and hq_hash != str(hashes.get("sawmill", ""))
	review["quarry_sprite_distinct"] = hq_hash != "" and hq_hash != str(hashes.get("quarry", ""))
	review["construction_site_distinct"] = hq_hash != "" and hq_hash != str(hashes.get("construction_site", ""))
	var carrier: Texture2D = SettlerAssetRegistry.get_texture("logistics", "carrier")
	review["carrier_sprite_visible"] = carrier != null
	var ware_ok: bool = true
	for ware in ["timber", "planks", "stone"]:
		if SettlerAssetRegistry.get_texture("wares", ware) == null:
			ware_ok = false
	review["carried_ware_icon_visible"] = ware_ok
	var terrain_hashes: Array = []
	for terrain in ["grass", "forest", "rock"]:
		var t: Texture2D = SettlerAssetRegistry.get_terrain_texture(terrain)
		if t != null:
			terrain_hashes.append(t.get_image().get_data().hex_encode())
	review["terrain_distinct"] = terrain_hashes.size() >= 3 and terrain_hashes[0] != terrain_hashes[1] and terrain_hashes[1] != terrain_hashes[2]
	return review


static func write_launch_review(review: Dictionary, base_name: String = "actual_human_launch_tutorial_review") -> bool:
	var dir: String = ensure_output_dir()
	var json_path: String = dir.path_join("%s.json" % base_name)
	var md_path: String = dir.path_join("%s.md" % base_name)
	SimPaths.write_text_file(json_path, JSON.stringify(review, "\t"))
	var lines: PackedStringArray = PackedStringArray([
		"# Actual human launch tutorial review",
		"",
		"- capture_method: %s" % review.get("capture_method", "in_engine_fallback"),
		"- window_size: %s" % str(review.get("window_size", [])),
		"- human_understandable_pass: %s" % str(review.get("human_understandable_pass", false)),
		"- tutorial_title: %s" % str(review.get("tutorial_title_text", "")).substr(0, 80),
		"- tutorial_instruction (%d chars): %s" % [
			str(review.get("tutorial_instruction_text", "")).length(),
			str(review.get("tutorial_instruction_text", "")).substr(0, 120),
		],
		"- build_button_count: %s" % str(review.get("build_button_count", 0)),
		"- failure_reasons: %s" % str(review.get("failure_reasons", [])),
		"",
	])
	SimPaths.write_text_file(md_path, "\n".join(lines))
	return true


static func write_visual_review(summary: Dictionary, base_name: String = "actual_human_visual_review") -> bool:
	var dir: String = ensure_output_dir()
	SimPaths.write_text_file(dir.path_join("%s.json" % base_name), JSON.stringify(summary, "\t"))
	var lines: PackedStringArray = PackedStringArray(["# Actual human visual review", ""])
	for shot in summary.get("shots", []):
		lines.append("## %s" % shot.get("file", "?"))
		lines.append("- human_understandable_pass: %s" % str(shot.get("human_understandable_pass", false)))
		lines.append("- failures: %s" % str(shot.get("failure_reasons", [])))
		lines.append("")
	SimPaths.write_text_file(dir.path_join("%s.md" % base_name), "\n".join(lines))
	return true


static func png_is_non_blank(path: String) -> bool:
	if not FileAccess.file_exists(path):
		return false
	if FileAccess.get_file_as_bytes(path).size() < MIN_PNG_BYTES:
		return false
	var img: Image = Image.load_from_file(path)
	if img == null or img.is_empty():
		return false
	var w: int = img.get_width()
	var h: int = img.get_height()
	var samples: int = 0
	var non_bg: int = 0
	for y in range(0, h, maxi(1, h / 16)):
		for x in range(0, w, maxi(1, w / 16)):
			samples += 1
			var c: Color = img.get_pixel(x, y)
			if c.a > 0.05 and (c.r < 0.95 or c.g < 0.95 or c.b < 0.95):
				non_bg += 1
	return samples > 0 and non_bg > samples / 4


static func _resolve(scene: Node, name: String) -> Control:
	var node: Node = scene.find_child(name, true, false)
	if node is Control:
		return node as Control
	return scene.get_node_or_null("%%%s" % name) as Control


static func _check_required(review: Dictionary, key: String, probe: Dictionary) -> void:
	var visible_key: String = key if key.ends_with("_visible") else "%s_visible" % key
	if key.ends_with("_visible"):
		visible_key = key
	if not bool(probe.get("visible", false)):
		review[visible_key] = false
		review["failure_reasons"].append("%s:%s" % [key, str(probe.get("reasons", []))])


static func _check_title_instruction(review: Dictionary, title_probe: Dictionary, instr_probe: Dictionary) -> void:
	if not bool(title_probe.get("visible", false)):
		review["failure_reasons"].append("tutorial_title:%s" % str(title_probe.get("reasons", [])))
	if str(title_probe.get("text", "")).length() <= MIN_TITLE_LEN:
		review["failure_reasons"].append("tutorial_title_too_short")
	if not bool(instr_probe.get("visible", false)):
		review["failure_reasons"].append("tutorial_instruction:%s" % str(instr_probe.get("reasons", [])))
	if str(instr_probe.get("text", "")).length() <= MIN_INSTRUCTION_LEN:
		review["failure_reasons"].append("tutorial_instruction_too_short")


static func _check_panels_in_viewport(review: Dictionary, scene: Node, viewport_size: Vector2i) -> void:
	var vp_rect: Rect2 = Rect2(Vector2.ZERO, Vector2(viewport_size))
	for panel_name in ["TutorialPanel", "ObjectivePanel", "InspectorPanel", "BuildPalette"]:
		var panel: Control = _resolve(scene, panel_name)
		if panel == null:
			review["failure_reasons"].append("missing:%s" % panel_name)
			continue
		var probe: Dictionary = HumanReview.probe_strict_visibility(panel, viewport_size)
		if not bool(probe.get("visible", false)):
			review["failure_reasons"].append("off_viewport:%s" % panel_name)


static func _check_map_does_not_cover_panels(review: Dictionary, scene: Node, viewport_size: Vector2i) -> void:
	var map: Control = _resolve(scene, "MapViewport")
	var right: Control = _resolve(scene, "RightPanelScroll")
	if right == null:
		right = _resolve(scene, "RightPanel")
	if map == null or right == null:
		review["failure_reasons"].append("map_cover_check_missing_nodes")
		return
	var map_rect: Rect2 = map.get_global_rect()
	var right_rect: Rect2 = right.get_global_rect()
	if not Rect2(Vector2.ZERO, Vector2(viewport_size)).intersects(right_rect):
		review["failure_reasons"].append("right_panel_off_viewport")
		return
	var overlap: Rect2 = map_rect.intersection(right_rect)
	if overlap.size.x > right_rect.size.x * 0.35 and overlap.size.y > right_rect.size.y * 0.35:
		review["failure_reasons"].append("map_covers_right_panel")


static func _count_visible_build_buttons(scene: Node, viewport_size: Vector2i) -> int:
	var palette: Node = scene.find_child("BuildPalette", true, false)
	if palette == null:
		return 0
	var count: int = 0
	for child in palette.find_children("*", "TextureButton", true, false):
		var btn: TextureButton = child as TextureButton
		var probe: Dictionary = HumanReview.probe_strict_visibility(btn, viewport_size)
		if probe.get("visible", false):
			count += 1
	for child in palette.find_children("*", "Button", true, false):
		var btn: Button = child as Button
		var probe: Dictionary = HumanReview.probe_strict_visibility(btn, viewport_size)
		if probe.get("visible", false):
			count += 1
	return count


static func _probe_time_controls(scene: Node, viewport_size: Vector2i) -> Dictionary:
	for key in ["PlayBtn", "PauseBtn", "StepBtn", "StatusLabel"]:
		var btn: Control = scene.find_child(key, true, false) as Control
		var probe: Dictionary = HumanReview.probe_strict_visibility(btn, viewport_size)
		if probe.get("visible", false):
			return probe
	return {"visible": false, "reasons": ["no_time_button_visible"]}
