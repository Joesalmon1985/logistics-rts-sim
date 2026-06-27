class_name HumanUnderstandableHybridReviewUtil
extends RefCounted

const VIEWPORT_SIZE := Vector2i(1920, 1080)

const HYBRID_CONTEXTS: Dictionary = {
	"tutorial_start": {
		"panels": ["tutorial_panel", "objective_panel", "build_palette", "inspector", "help_panel"],
		"tutorial_title_min": 6,
		"tutorial_instruction_min": 40,
		"objective_min": 15,
	},
	"hq_selected": {
		"panels": ["inspector", "objective_panel", "build_palette"],
		"inspector_min": 20,
	},
	"military_site": {
		"panels": ["production_panel", "supply_label", "warnings_panel"],
		"production_min": 8,
	},
}


static func screenshot_output_dir() -> String:
	return HybridMvpReviewUtil.screenshot_output_dir()


static func ensure_output_dir() -> String:
	return HybridMvpReviewUtil.ensure_output_dir()


static func probe_game_context(scene: Node, context: String) -> Dictionary:
	var review: Dictionary = {
		"context": context,
		"visibility_gate_pass": true,
		"failures": [],
	}
	var spec: Dictionary = HYBRID_CONTEXTS.get(context, {})
	var path_map: Dictionary = {
		"tutorial_panel": "%TutorialPanel",
		"objective_panel": "%ObjectivePanel",
		"build_palette": "%BuildPalette",
		"inspector": "%Inspector",
		"help_panel": "%HelpPanel",
		"production_panel": "%ProductionPanel",
		"supply_label": "%SupplyLabel",
		"warnings_panel": "%WarningsPanel",
	}
	for panel_key in spec.get("panels", []):
		var node: Control = scene.get_node_or_null(NodePath(str(path_map.get(panel_key, "")))) as Control
		var probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(node, VIEWPORT_SIZE, 0)
		if not bool(probe.get("visible", false)):
			review["visibility_gate_pass"] = false
			review["failures"].append("missing:%s" % panel_key)
	if context == "tutorial_start" and scene.has_method("get_tutorial_instruction_text"):
		if scene.get_tutorial_instruction_text().length() < int(spec.get("tutorial_instruction_min", 0)):
			review["visibility_gate_pass"] = false
			review["failures"].append("tutorial_instruction_short")
	return review
