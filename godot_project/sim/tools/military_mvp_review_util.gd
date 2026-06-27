class_name MilitaryMvpReviewUtil
extends RefCounted

## Probes Skirmish MVP scene layout for screenshot review artefacts.

const MIN_PNG_BYTES: int = 1000
const VIEWPORT_SIZE := Vector2i(1920, 1080)

const PANEL_NODE_PATHS: Dictionary = {
	"map_viewport": "%MapViewport",
	"objective_panel": "%ObjectivePanel",
	"summary_label": "%SummaryLabel",
	"production_panel": "%ProductionQueuesPanel",
	"inspector": "%Inspector",
	"controls_help": "%HelpPanel",
	"build_palette": "%BuildPalette",
	"play_button": "%PlayBtn",
	"status_label": "%StatusLabel",
	"tutorial_panel": "%TutorialPanel",
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
		"build_palette_found": false,
		"production_panel_found": false,
		"help_text_found": false,
		"visibility_gate_pass": true,
		"plain_english_gate_pass": true,
		"failures": [],
		"panel_rects": {},
	}
	if scene == null:
		review["visibility_gate_pass"] = false
		return review

	review["map_viewport_found"] = scene.get_node_or_null("%MapViewport") != null
	review["objective_panel_found"] = scene.get_node_or_null("%ObjectivePanel") != null
	review["build_palette_found"] = scene.get_node_or_null("%BuildPalette") != null
	review["production_panel_found"] = scene.get_node_or_null("%ProductionQueuesPanel") != null
	review["help_text_found"] = scene.get_node_or_null("%HelpPanel") != null
	review["hud_found"] = scene.get_node_or_null("%StatusLabel") != null

	for key in PANEL_NODE_PATHS:
		var node: Control = scene.get_node_or_null(str(PANEL_NODE_PATHS[key])) as Control
		if node == null:
			continue
		review["panel_rects"][key] = HumanUnderstandableReviewUtil.probe_strict_visibility(node, viewport_size, 0)

	var summary: Control = scene.get_node_or_null("%SummaryLabel") as Control
	var summary_probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(summary, viewport_size, 8)
	if not bool(summary_probe.get("visible", false)):
		review["visibility_gate_pass"] = false
		review["failures"].append("summary:not_visible")
	var summary_text: String = str(summary_probe.get("text", ""))
	if not ("Credits" in summary_text):
		review["plain_english_gate_pass"] = false
		review["failures"].append("summary:missing_credits")

	var objective: Control = scene.get_node_or_null("%ObjectivePanel") as Control
	var obj_probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(objective, viewport_size, 20)
	if not bool(obj_probe.get("visible", false)):
		review["visibility_gate_pass"] = false
		review["failures"].append("objective:not_visible")

	var palette: Control = scene.get_node_or_null("%BuildPalette") as Control
	var palette_probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(palette, viewport_size, 0)
	if not bool(palette_probe.get("visible", false)):
		review["visibility_gate_pass"] = false
		review["failures"].append("build_palette:not_visible")

	return review


static func probe_game_context(scene: Node, context: String) -> Dictionary:
	var review: Dictionary = probe_scene(scene)
	review["context"] = context
	if context == "tutorial_start" and scene.has_method("enable_tutorial_for_tests"):
		scene.call("enable_tutorial_for_tests")
		if scene.has_method("_refresh_tutorial_ui"):
			scene.call("_refresh_tutorial_ui")
		var panel: Control = scene.get_node_or_null("%TutorialPanel") as Control
		var probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(panel, VIEWPORT_SIZE, 0)
		if not bool(probe.get("visible", false)):
			review["visibility_gate_pass"] = false
			review["failures"].append("tutorial_panel:not_visible")
		if scene.has_method("get_tutorial_instruction_text"):
			if str(scene.call("get_tutorial_instruction_text")).length() < 40:
				review["visibility_gate_pass"] = false
				review["failures"].append("tutorial_instruction:too_short")
	if context == "hub_selected":
		var inspector_text: String = ""
		if scene.has_method("get_inspector_text"):
			inspector_text = str(scene.call("get_inspector_text"))
		if not ("Command Hub" in inspector_text):
			review["plain_english_gate_pass"] = false
			review["failures"].append("inspector:missing_command_hub")
	if context == "build_mode":
		if scene.has_method("get_mode_banner_title"):
			if str(scene.call("get_mode_banner_title")).length() < 4:
				review["visibility_gate_pass"] = false
				review["failures"].append("mode_banner:missing")
	return review


static func write_review_files(base_name: String, review: Dictionary, png_path: String) -> bool:
	var dir: String = ensure_output_dir()
	var json_path: String = dir.path_join("%s_review.json" % base_name)
	review["screenshot_path"] = png_path
	SimPaths.write_text_file(json_path, JSON.stringify(review, "\t"))
	return true


static func write_visual_review(summary: Dictionary, base_name: String = "military_mvp_visual_review") -> bool:
	var dir: String = ensure_output_dir()
	SimPaths.write_text_file(dir.path_join("%s.json" % base_name), JSON.stringify(summary, "\t"))
	return true
