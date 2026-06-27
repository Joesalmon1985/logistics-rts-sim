class_name HybridMvpReviewUtil
extends RefCounted

const VIEWPORT_SIZE := Vector2i(1920, 1080)


static func screenshot_output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("../docs/audit/screenshots")


static func ensure_output_dir() -> String:
	var dir: String = screenshot_output_dir()
	DirAccess.make_dir_recursive_absolute(dir)
	return dir


static func probe_game(scene: Node) -> Dictionary:
	var review: Dictionary = {
		"visibility_gate_pass": true,
		"failures": [],
		"panels": {},
	}
	var specs: Dictionary = {
		"objective": {"path": "%ObjectivePanel", "min_len": 15, "need": ["Hybrid", "objective"]},
		"supply": {"path": "%SupplyLabel", "min_len": 8, "need": ["Stockpile"]},
		"production": {"path": "%ProductionPanel", "min_len": 8, "need": ["production"]},
		"build_palette": {"path": "%BuildPalette", "min_len": 0, "need": []},
		"inspector": {"path": "%Inspector", "min_len": 8, "need": []},
		"help": {"path": "%HelpPanel", "min_len": 20, "need": ["Hybrid"]},
	}
	for key in specs:
		var spec: Dictionary = specs[key]
		var node: Control = scene.get_node_or_null(NodePath(str(spec.get("path", "")))) as Control
		var probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(
			node, VIEWPORT_SIZE, int(spec.get("min_len", 0)),
		)
		review["panels"][key] = probe
		if not bool(probe.get("visible", false)):
			review["visibility_gate_pass"] = false
			review["failures"].append("panel:%s" % key)
			continue
		var text: String = str(probe.get("text", "")).to_lower()
		for token in spec.get("need", []):
			if not (str(token).to_lower() in text):
				review["failures"].append("plain:%s:%s" % [key, token])
				review["visibility_gate_pass"] = false
	return review
