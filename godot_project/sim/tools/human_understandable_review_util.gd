class_name HumanUnderstandableReviewUtil
extends RefCounted

## Strict visibility probes for Tutorial Legibility / Human-Understandable screens.

const VIEWPORT_SIZE := Vector2i(1920, 1080)
const MIN_PNG_BYTES: int = 1000
const MIN_RECT_W: float = 24.0
const MIN_RECT_H: float = 10.0
const MIN_RECT_AREA: float = 240.0
const MIN_ALPHA: float = 0.08

const REQUIRED_CONTEXTS: Dictionary = {
	"tutorial_start": {
		"panels": ["tutorial_panel", "objective_panel", "inspector_panel", "build_palette", "time_controls", "help_panel", "map_viewport"],
		"tutorial_title_min": 6,
		"tutorial_instruction_min": 41,
		"objective_min": 21,
	},
	"hq_selected": {
		"panels": ["inspector_panel", "objective_panel", "build_palette", "time_controls"],
		"inspector_min": 40,
	},
	"build_mode": {
		"panels": ["mode_title", "build_palette", "preview_label"],
		"mode_title_min": 5,
	},
	"road_mode": {
		"panels": ["mode_title", "build_palette", "preview_label"],
		"mode_title_min": 4,
	},
}

const PANEL_PATHS: Dictionary = {
	"tutorial_panel": "%TutorialPanel",
	"mode_banner": "%ModeBanner",
	"build_palette": "%BuildPalette",
	"objective_panel": "%ObjectivePanel",
	"inspector_panel": "%InspectorPanel",
	"inspector": "%Inspector",
	"help_panel": "%HelpPanel",
	"preview_label": "%PreviewLabel",
	"play_button": "%PlayBtn",
	"time_controls": "%PlayBtn",
	"map_viewport": "%MapViewport",
	"tutorial_title": "%TutorialTitle",
	"tutorial_instruction": "%TutorialInstruction",
	"tutorial_progress": "%TutorialProgress",
	"mode_title": "%ModeTitle",
	"mode_instruction": "%ModeInstruction",
}


static func screenshot_output_dir() -> String:
	return SettlerMvpReviewUtil.screenshot_output_dir()


static func ensure_output_dir() -> String:
	return SettlerMvpReviewUtil.ensure_output_dir()


static func viewport_rect(viewport_size: Vector2i = VIEWPORT_SIZE) -> Rect2:
	return Rect2(Vector2.ZERO, Vector2(viewport_size))


static func extract_node_text(node: Node) -> String:
	if node == null:
		return ""
	if node is RichTextLabel:
		var rtl: RichTextLabel = node as RichTextLabel
		var parsed: String = rtl.get_parsed_text()
		if parsed != "":
			return parsed
		return rtl.text
	if node is Label:
		return (node as Label).text
	if node is Button:
		return (node as Button).text
	if node is LineEdit:
		return (node as LineEdit).text
	return ""


static func probe_strict_visibility(node: Control, viewport_size: Vector2i = VIEWPORT_SIZE, min_text_len: int = 0) -> Dictionary:
	var result: Dictionary = {
		"exists": node != null,
		"visible": false,
		"reasons": [],
		"text": "",
		"text_len": 0,
		"rect": {},
	}
	if node == null:
		result["reasons"].append("missing")
		return result
	if not node.is_inside_tree():
		result["reasons"].append("not_in_tree")
		return result
	if not node.visible:
		result["reasons"].append("visible_false")
		return result
	var alpha: float = node.modulate.a * node.self_modulate.a
	if alpha < MIN_ALPHA:
		result["reasons"].append("transparent")
		return result
	var in_subvp: bool = _in_subviewport(node)
	var rect: Rect2 = node.get_rect()
	var global_rect: Rect2 = node.get_global_rect()
	result["rect"] = {
		"x": global_rect.position.x,
		"y": global_rect.position.y,
		"w": global_rect.size.x,
		"h": global_rect.size.y,
	}
	var size_ok: bool = false
	if in_subvp:
		size_ok = (
			(rect.size.x >= MIN_RECT_W or node.custom_minimum_size.x >= MIN_RECT_W)
			and (rect.size.y >= MIN_RECT_H or node.custom_minimum_size.y >= MIN_RECT_H)
		)
	else:
		size_ok = global_rect.size.x >= MIN_RECT_W and global_rect.size.y >= MIN_RECT_H
	if not size_ok:
		result["reasons"].append("too_small")
		return result
	if not in_subvp and not viewport_rect(viewport_size).intersects(global_rect):
		result["reasons"].append("off_viewport")
		return result
	if not in_subvp and _is_clipped_outside_parent(node, global_rect):
		result["reasons"].append("clipped")
		return result
	if not in_subvp and _is_fully_covered_by_opaque_panel(node, global_rect):
		result["reasons"].append("covered")
		return result
	var text: String = extract_node_text(node)
	result["text"] = text
	result["text_len"] = text.length()
	if min_text_len > 0 and text.length() < min_text_len:
		result["reasons"].append("text_too_short")
		return result
	result["visible"] = true
	return result


static func _in_subviewport(node: Control) -> bool:
	var n: Node = node
	while n != null:
		if n is SubViewport:
			return true
		n = n.get_parent()
	return false


static func probe_game_context(scene: Node, context: String) -> Dictionary:
	var review: Dictionary = {
		"context": context,
		"panels": {},
		"text_nodes": {},
		"visibility_gate_pass": true,
		"failures": [],
	}
	var spec: Dictionary = REQUIRED_CONTEXTS.get(context, {})
	for key in PANEL_PATHS:
		var node: Control = _resolve_panel_node(scene, key)
		if node == null:
			continue
		var min_len: int = 0
		if key == "tutorial_title":
			min_len = int(spec.get("tutorial_title_min", 0))
		elif key == "tutorial_instruction":
			min_len = int(spec.get("tutorial_instruction_min", 0))
		elif key == "objective_panel":
			min_len = int(spec.get("objective_min", 0))
		elif key == "inspector":
			min_len = int(spec.get("inspector_min", 0))
		elif key == "mode_title":
			min_len = int(spec.get("mode_title_min", 0))
		review["panels"][key] = probe_strict_visibility(node, VIEWPORT_SIZE, min_len)
		if key in ["tutorial_title", "tutorial_instruction", "mode_title", "mode_instruction", "inspector", "objective_panel"]:
			review["text_nodes"][key] = review["panels"][key]
	for required_panel in spec.get("panels", []):
		var probe: Dictionary = review["panels"].get(required_panel, {"visible": false, "reasons": ["missing"]})
		if required_panel == "time_controls":
			probe = _probe_time_controls(scene)
			review["panels"]["time_controls"] = probe
		elif required_panel == "mode_title":
			var mode_node: Control = _resolve_panel_node(scene, "mode_title")
			probe = probe_strict_visibility(mode_node, VIEWPORT_SIZE, int(spec.get("mode_title_min", 0)))
			review["panels"]["mode_title"] = probe
		if not bool(probe.get("visible", false)):
			review["visibility_gate_pass"] = false
			review["failures"].append("panel:%s:%s" % [required_panel, str(probe.get("reasons", []))])
	if context == "tutorial_start":
		_check_tutorial_text(review)
	if context in ["build_mode", "road_mode"]:
		_check_mode_banner(review, context)
	if context == "hq_selected":
		_check_inspector_plain_english(review)
	review["build_button_count"] = _count_visible_build_buttons(scene)
	if context in ["tutorial_start", "build_mode", "road_mode", "hq_selected"]:
		if review["build_button_count"] < 3:
			review["visibility_gate_pass"] = false
			review["failures"].append("build_buttons:%d" % review["build_button_count"])
	return review


static func _resolve_panel_node(scene: Node, key: String) -> Control:
	var path: String = str(PANEL_PATHS.get(key, ""))
	if path.begins_with("%"):
		var node: Node = scene.get_node_or_null(NodePath(path))
		if node is Control:
			return node as Control
	return scene.find_child(path.get_file() if path != "" else key, true, false) as Control


static func probe_main_menu(scene: Node) -> Dictionary:
	var review: Dictionary = HumanMvpReviewUtil.probe_main_menu(scene)
	review["visibility_gate_pass"] = bool(review.get("all_visible", false))
	for key in ["tutorial_btn", "free_play_btn", "debug_btn", "quit_btn"]:
		var btn: Control = _find_menu_button(scene, key)
		var probe: Dictionary = probe_strict_visibility(btn)
		review["strict_buttons"] = review.get("strict_buttons", {})
		review["strict_buttons"][key] = probe
		if not probe.get("visible", false):
			review["visibility_gate_pass"] = false
	return review


static func write_visual_review(summary: Dictionary, base_name: String = "human_understandable_visual_review") -> bool:
	var dir: String = ensure_output_dir()
	var json_path: String = dir.path_join("%s.json" % base_name)
	var md_path: String = dir.path_join("%s.md" % base_name)
	SimPaths.write_text_file(json_path, JSON.stringify(summary, "\t"))
	var lines: PackedStringArray = PackedStringArray(["# Human understandable visual review", ""])
	for shot in summary.get("shots", []):
		lines.append("## %s" % shot.get("file", "?"))
		lines.append("- context: %s" % str(shot.get("context", "")))
		lines.append("- visibility_gate_pass: %s" % str(shot.get("visibility_gate_pass", false)))
		if shot.has("failures"):
			lines.append("- failures: %s" % str(shot.get("failures", [])))
		for text_key in shot.get("text_nodes", {}):
			var node_probe: Dictionary = shot["text_nodes"][text_key]
			lines.append("- %s text (%d): %s" % [
				text_key,
				int(node_probe.get("text_len", 0)),
				str(node_probe.get("text", "")).substr(0, 80),
			])
		lines.append("")
	SimPaths.write_text_file(md_path, "\n".join(lines))
	return true


static func _check_tutorial_text(review: Dictionary) -> void:
	var title_probe: Dictionary = review["panels"].get("tutorial_title", {})
	var instr_probe: Dictionary = review["panels"].get("tutorial_instruction", {})
	if not bool(title_probe.get("visible", false)):
		review["visibility_gate_pass"] = false
		review["failures"].append("tutorial_title")
	if not bool(instr_probe.get("visible", false)):
		review["visibility_gate_pass"] = false
		review["failures"].append("tutorial_instruction")


static func _probe_time_controls(scene: Node) -> Dictionary:
	for key in ["PlayBtn", "PauseBtn", "StepBtn", "StatusLabel"]:
		var btn: Control = scene.find_child(key, true, false) as Control
		var probe: Dictionary = probe_strict_visibility(btn)
		if probe.get("visible", false):
			return probe
	return {"visible": false, "reasons": ["no_time_button_visible"]}


static func _check_mode_banner(review: Dictionary, context: String) -> void:
	var title: Dictionary = review["panels"].get("mode_title", {})
	if not bool(title.get("visible", false)):
		review["visibility_gate_pass"] = false
		review["failures"].append("mode_banner:%s" % context)


static func _check_inspector_plain_english(review: Dictionary) -> void:
	var inspector: Dictionary = review["panels"].get("inspector", {})
	var text: String = str(inspector.get("text", ""))
	if not bool(inspector.get("visible", false)):
		review["visibility_gate_pass"] = false
		review["failures"].append("inspector_not_visible")
		return
	if text.contains("Type:") and text.contains("ID:") and not text.contains("Headquarters"):
		review["visibility_gate_pass"] = false
		review["failures"].append("inspector_raw_ids")


static func _count_visible_build_buttons(scene: Node) -> int:
	var palette: Node = scene.get_node_or_null("%BuildPalette")
	if palette == null:
		return 0
	var count: int = 0
	for child in palette.find_children("*", "TextureButton", true, false):
		var btn: TextureButton = child as TextureButton
		var probe: Dictionary = probe_strict_visibility(btn)
		if probe.get("visible", false):
			count += 1
	return count


static func _find_menu_button(scene: Node, key: String) -> Control:
	match key:
		"tutorial_btn":
			return scene.get_node_or_null("%TutorialBtn") as Control
		"free_play_btn":
			return scene.get_node_or_null("%FreePlayBtn") as Control
		"debug_btn":
			return scene.get_node_or_null("%DebugViewersBtn") as Control
		"quit_btn":
			return scene.get_node_or_null("%QuitBtn") as Control
	return null


static func _is_clipped_outside_parent(node: Control, global_rect: Rect2) -> bool:
	var current: Node = node.get_parent()
	while current is Control:
		var parent: Control = current as Control
		if parent.clip_contents:
			var parent_rect: Rect2 = parent.get_global_rect()
			if not parent_rect.intersects(global_rect):
				return true
			var visible_rect: Rect2 = global_rect.intersection(parent_rect)
			if visible_rect.size.x < MIN_RECT_W or visible_rect.size.y < MIN_RECT_H:
				return true
		current = parent.get_parent()
	return false


static func _is_fully_covered_by_opaque_panel(node: Control, global_rect: Rect2) -> bool:
	var parent: Node = node.get_parent()
	if parent == null:
		return false
	var found_self: bool = false
	for sibling in parent.get_children():
		if sibling == node:
			found_self = true
			continue
		if not found_self:
			continue
		if not (sibling is Control):
			continue
		var cover: Control = sibling as Control
		if not cover.visible or not cover.is_inside_tree():
			continue
		if cover.modulate.a * cover.self_modulate.a < MIN_ALPHA:
			continue
		var cover_rect: Rect2 = cover.get_global_rect()
		if cover_rect.size.x < MIN_RECT_W or cover_rect.size.y < MIN_RECT_H:
			continue
		if cover_rect.encloses(global_rect):
			if cover is PanelContainer or cover is Panel or cover is ColorRect:
				return true
			if cover.mouse_filter == Control.MOUSE_FILTER_STOP and cover_rect.size.x * cover_rect.size.y > global_rect.size.x * global_rect.size.y * 1.2:
				return true
	return false
