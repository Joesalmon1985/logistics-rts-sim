class_name HumanMvpReviewUtil
extends RefCounted

## Probes Human-Playable Settler MVP UI visibility at 1920x1080.

const MIN_PNG_BYTES: int = 1000
const VIEWPORT_SIZE := Vector2i(1920, 1080)

const REQUIRED_VISIBLE: Dictionary = {
	"main_menu": ["tutorial_btn", "free_play_btn", "debug_btn", "quit_btn"],
	"tutorial_start": ["tutorial_panel", "objective_panel", "inspector", "time_controls"],
	"hq_inspector": ["inspector", "objective_panel", "time_controls"],
	"build_menu": ["build_palette", "time_controls"],
	"woodcutter_preview": ["build_palette", "mode_banner", "preview_label"],
	"road_mode": ["build_palette", "mode_banner", "preview_label"],
	"building_selected": ["inspector", "objective_panel"],
	"free_play_hud": ["objective_panel", "inspector", "build_palette", "time_controls", "help_panel"],
}


static func screenshot_output_dir() -> String:
	return SettlerMvpReviewUtil.screenshot_output_dir()


static func ensure_output_dir() -> String:
	return SettlerMvpReviewUtil.ensure_output_dir()


static func probe_main_menu(scene: Node) -> Dictionary:
	var review: Dictionary = {
		"scene": "main_menu",
		"buttons": {},
		"all_visible": true,
		"off_screen": [],
	}
	var in_subvp: bool = scene.get_parent() is SubViewport
	for key in ["TutorialBtn", "FreePlayBtn", "DebugViewersBtn", "QuitBtn"]:
		var btn: Control = scene.get_node_or_null(NodePath("%" + key)) as Control
		if btn == null:
			btn = _find_button_by_text(scene, _button_label(key)) as Control
		var id: String = key.to_lower().replace("btn", "_btn")
		review["buttons"][id] = _visible_probe(btn, in_subvp)
		if not review["buttons"][id].get("visible", false):
			review["all_visible"] = false
			review["off_screen"].append(id)
	return review


static func probe_game_scene(scene: Node, context: String = "free_play") -> Dictionary:
	var base: Dictionary = SettlerMvpReviewUtil.probe_scene(scene, VIEWPORT_SIZE)
	var review: Dictionary = base.duplicate(true)
	review["context"] = context
	review["panels"] = {}
	review["visibility_gate_pass"] = true
	review["off_screen_panels"] = []
	var in_subvp: bool = scene.get_parent() is SubViewport
	var paths: Dictionary = {
		"tutorial_panel": "%TutorialPanel",
		"mode_banner": "%ModeBanner",
		"build_palette": "%BuildPalette",
		"objective_panel": "%ObjectivePanel",
		"inspector": "%Inspector",
		"help_panel": "%HelpPanel",
		"preview_label": "%PreviewLabel",
		"play_button": "%PlayBtn",
		"time_controls": "%PlayBtn",
	}
	for key in paths:
		var node: Control = scene.get_node_or_null(str(paths[key])) as Control
		review["panels"][key] = _visible_probe(node, in_subvp)
	var required: Array = REQUIRED_VISIBLE.get(context, [])
	for key in required:
		if not review["panels"].has(key):
			if key == "tutorial_btn" or key == "free_play_btn":
				continue
			review["visibility_gate_pass"] = false
			review["off_screen_panels"].append(key)
			continue
		if key in ["time_controls"]:
			if not review["panels"].get("play_button", {}).get("visible", false):
				review["visibility_gate_pass"] = false
				review["off_screen_panels"].append(key)
		elif key.ends_with("_btn"):
			pass
		elif not review["panels"].get(key, {}).get("visible", false):
			if key == "mode_banner" and context in ["woodcutter_preview", "road_mode"]:
				review["visibility_gate_pass"] = false
				review["off_screen_panels"].append(key)
			elif key != "mode_banner":
				if key == "tutorial_panel" and context != "tutorial_start":
					continue
				if not review["panels"].get(key, {}).get("exists", false):
					review["visibility_gate_pass"] = false
					review["off_screen_panels"].append(key)
				elif context in ["woodcutter_preview", "road_mode", "tutorial_start", "build_menu", "free_play_hud", "hq_inspector", "building_selected"]:
					if not review["panels"].get(key, {}).get("visible", false):
						review["visibility_gate_pass"] = false
						review["off_screen_panels"].append(key)
	if context in ["woodcutter_preview", "road_mode"]:
		var banner: Control = scene.get_node_or_null("%ModeBanner") as Control
		var mode_title: Label = scene.get_node_or_null("%ModeTitle") as Label
		var banner_active: bool = (
			banner != null
			and banner.visible
			and mode_title != null
			and not mode_title.text.is_empty()
		)
		if not banner_active:
			review["visibility_gate_pass"] = false
			review["off_screen_panels"].append("mode_banner")
		elif "mode_banner" in review["off_screen_panels"]:
			review["off_screen_panels"].erase("mode_banner")
			review["panels"]["mode_banner"] = {"exists": true, "visible": true}
	return review


static func write_visual_review(summary: Dictionary) -> bool:
	var dir: String = ensure_output_dir()
	var json_path: String = dir.path_join("human_mvp_visual_review.json")
	var md_path: String = dir.path_join("human_mvp_visual_review.md")
	SimPaths.write_text_file(json_path, JSON.stringify(summary, "\t"))
	var lines: PackedStringArray = PackedStringArray(["# Human MVP visual review", ""])
	for shot in summary.get("shots", []):
		lines.append("## %s" % shot.get("file", "?"))
		lines.append("- visibility_gate: %s" % str(shot.get("visibility_gate_pass", false)))
		if shot.has("off_screen_panels"):
			lines.append("- off_screen: %s" % str(shot.get("off_screen_panels", [])))
		lines.append("")
	SimPaths.write_text_file(md_path, "\n".join(lines))
	return true


static func _visible_probe(node: Control, in_subviewport: bool = false) -> Dictionary:
	if node == null:
		return {"exists": false, "visible": false}
	var rect: Rect2 = node.get_rect()
	var global_rect: Rect2 = node.get_global_rect()
	if in_subviewport:
		if node is Button:
			var btn := node as Button
			return {
				"exists": true,
				"visible": btn.visible and not btn.text.is_empty(),
				"rect": {"x": rect.position.x, "y": rect.position.y, "w": rect.size.x, "h": rect.size.y},
			}
		var has_extent: bool = (
			rect.size.x > 1.0 or node.custom_minimum_size.x > 1.0
			or rect.size.y > 1.0 or node.custom_minimum_size.y > 1.0
		)
		var visible_ok: bool = node.visible and has_extent
		return {
			"exists": true,
			"visible": visible_ok,
			"rect": {"x": rect.position.x, "y": rect.position.y, "w": rect.size.x, "h": rect.size.y},
		}
	var size_ok: bool = rect.size.x > 1.0 and rect.size.y > 1.0
	var on_screen: bool = global_rect.position.x >= -4 and global_rect.position.y >= -4
	on_screen = on_screen and global_rect.end.x <= float(VIEWPORT_SIZE.x) + 4
	on_screen = on_screen and global_rect.end.y <= float(VIEWPORT_SIZE.y) + 4
	return {
		"exists": true,
		"visible": node.visible and size_ok and on_screen,
		"rect": {"x": global_rect.position.x, "y": global_rect.position.y, "w": global_rect.size.x, "h": global_rect.size.y},
	}


static func _find_button_by_text(root: Node, text: String) -> Control:
	for child in root.find_children("*", "Button", true, false):
		if child is Button and (child as Button).text == text:
			return child as Control
	return null


static func _button_label(key: String) -> String:
	match key:
		"TutorialBtn":
			return "Tutorial"
		"FreePlayBtn":
			return "Free Play / Settler MVP"
		"DebugViewersBtn":
			return "Debug Viewers"
		_:
			return "Quit"


static func btn_min_size(node: Control) -> bool:
	if node is Button:
		return (node as Button).size.x > 1.0 or (node as Button).custom_minimum_size.x > 1.0
	return node.custom_minimum_size.x > 1.0 or node.custom_minimum_size.y > 1.0
