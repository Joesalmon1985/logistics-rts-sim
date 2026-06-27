class_name HybridObserverReviewUtil
extends RefCounted

## Visibility probes for hybrid observer screens (MRP C2).


const VIEWPORT_SIZE := Vector2i(1920, 1080)


static func screenshot_output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("../docs/audit/screenshots")


static func ensure_output_dir() -> String:
	var dir: String = screenshot_output_dir()
	DirAccess.make_dir_recursive_absolute(dir)
	return dir


static func probe_scene(scene: Node) -> Dictionary:
	var review: Dictionary = {
		"visibility_gate_pass": true,
		"plain_english_gate_pass": true,
		"panels": {},
		"failures": [],
	}
	var panel_specs: Dictionary = {
		"summary": {"path": "%SummaryLabel", "min_len": 12, "need": ["Stockpile", "Power"]},
		"chain": {"path": "%ChainPanel", "min_len": 20, "need": ["Logistics", "military"]},
		"production": {"path": "%ProductionPanel", "min_len": 10, "need": ["production"]},
		"inspector": {"path": "%Inspector", "min_len": 8, "need": []},
		"help": {"path": "%HelpPanel", "min_len": 20, "need": ["carrier"]},
	}
	for key in panel_specs:
		var spec: Dictionary = panel_specs[key]
		var node: Control = scene.get_node_or_null(NodePath(str(spec.get("path", "")))) as Control
		var probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(
			node, VIEWPORT_SIZE, int(spec.get("min_len", 0)),
		)
		review["panels"][key] = probe
		if not bool(probe.get("visible", false)):
			review["visibility_gate_pass"] = false
			review["failures"].append("panel:%s:%s" % [key, str(probe.get("reasons", []))])
			continue
		var text: String = str(probe.get("text", "")).to_lower()
		for token in spec.get("need", []):
			if not (str(token).to_lower() in text):
				review["plain_english_gate_pass"] = false
				review["failures"].append("plain:%s:missing:%s" % [key, token])
	if not review["plain_english_gate_pass"]:
		review["visibility_gate_pass"] = false
	return review


static func write_visual_review(summary: Dictionary, base_name: String = "hybrid_observer_visual_review") -> bool:
	var dir: String = ensure_output_dir()
	SimPaths.write_text_file(dir.path_join("%s.json" % base_name), JSON.stringify(summary, "\t"))
	var lines: PackedStringArray = PackedStringArray(["# Hybrid observer visual review", ""])
	for shot in summary.get("shots", []):
		lines.append("## %s" % shot.get("file", "?"))
		lines.append("- visibility_gate_pass: %s" % str(shot.get("visibility_gate_pass", false)))
		if shot.has("failures"):
			lines.append("- failures: %s" % str(shot.get("failures", [])))
		lines.append("")
	SimPaths.write_text_file(dir.path_join("%s.md" % base_name), "\n".join(lines))
	return true


static func find_first_military_building_hex(scene: Node, building_type: String, player_id: String = "p1") -> Vector2i:
	if scene == null or not ("controller" in scene):
		return Vector2i(-1, -1)
	var controller = scene.controller
	if controller == null or controller.snapshot.is_empty():
		return Vector2i(-1, -1)
	for building in controller.snapshot.get("buildings", []):
		if str(building.get("building_type", "")) != building_type:
			continue
		if str(building.get("player_id", "")) != player_id:
			continue
		return Vector2i(
			int(building.get("anchor_q", building.get("q", -1))),
			int(building.get("anchor_r", building.get("r", -1))),
		)
	return Vector2i(-1, -1)
