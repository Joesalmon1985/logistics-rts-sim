class_name Hybrid3dSpectatorReviewUtil
extends RefCounted

## Visibility probes for hybrid 3D spectator screens.

const VIEWPORT_SIZE := Vector2i(1920, 1080)
const MIN_PNG_BYTES: int = 1200
const REQUIRED_PLAIN_ENGLISH: Array = [
	"Bot A",
	"Bot B",
	"Headquarters",
	"Command Hub",
	"Barracks",
	"Carrier",
]


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
		"world_gate_pass": false,
		"panels": {},
		"failures": [],
	}
	var panel_specs: Dictionary = {
		"status": {"path": "%StatusLabel", "min_len": 8, "need": ["Tick"]},
		"bot_a": {"path": "%BotASummary", "min_len": 12, "need": ["Bot A"]},
		"bot_b": {"path": "%BotBSummary", "min_len": 12, "need": ["Bot B"]},
		"objective": {"path": "%ObjectiveLabel", "min_len": 10, "need": []},
		"inspector": {"path": "%Inspector", "min_len": 8, "need": []},
		"events": {"path": "%EventLog", "min_len": 4, "need": []},
	}
	for key in panel_specs:
		var spec: Dictionary = panel_specs[key]
		var node: Control = scene.get_node_or_null(NodePath(str(spec.get("path", "")))) as Control
		var probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(
			node,
			VIEWPORT_SIZE,
			int(spec.get("min_len", 0)),
		)
		review["panels"][key] = probe
		if not bool(probe.get("visible", false)):
			review["visibility_gate_pass"] = false
			review["failures"].append("panel:%s:%s" % [key, str(probe.get("reasons", []))])
			continue
		var text: String = str(probe.get("text", ""))
		var need_any: bool = false
		for token in spec.get("need", []):
			if str(token) in text:
				need_any = true
				break
		if spec.get("need", []).size() > 0 and not need_any:
			review["plain_english_gate_pass"] = false
			review["failures"].append("plain:%s:missing_any:%s" % [key, str(spec.get("need", []))])
	if scene != null and scene.has_method("get_world_entity_count"):
		var counts: Dictionary = scene.call("get_world_entity_count")
		review["world_gate_pass"] = int(counts.get("terrain", 0)) > 0
		if not review["world_gate_pass"]:
			review["failures"].append("world:no_terrain_meshes")
	else:
		review["failures"].append("world:missing_entity_count_method")
	review["visibility_gate_pass"] = bool(review["visibility_gate_pass"]) and bool(review["world_gate_pass"])
	return review


static func probe_inspector_plain_english(scene: Node, required_names: Array = REQUIRED_PLAIN_ENGLISH) -> Dictionary:
	var inspector: Control = scene.get_node_or_null("%Inspector") as Control
	var probe: Dictionary = HumanUnderstandableReviewUtil.probe_strict_visibility(inspector, VIEWPORT_SIZE, 10)
	var text: String = str(probe.get("text", ""))
	var missing: PackedStringArray = PackedStringArray()
	for name in required_names:
		if not (str(name) in text):
			missing.append(name)
	return {
		"visible": probe.get("visible", false),
		"text": text,
		"missing": missing,
		"passes": probe.get("visible", false) and missing.size() < required_names.size(),
	}


static func write_visual_review(summary: Dictionary, base_name: String = "hybrid_3d_spectator_visual_review") -> bool:
	var dir: String = ensure_output_dir()
	SimPaths.write_text_file(dir.path_join("%s.json" % base_name), JSON.stringify(summary, "\t"))
	var lines: PackedStringArray = PackedStringArray(["# Hybrid 3D spectator visual review", ""])
	for shot in summary.get("shots", []):
		lines.append("## %s" % shot.get("file", "?"))
		lines.append("- visibility_gate_pass: %s" % str(shot.get("visibility_gate_pass", false)))
		if shot.has("failures"):
			lines.append("- failures: %s" % str(shot.get("failures", [])))
		lines.append("")
	SimPaths.write_text_file(dir.path_join("%s.md" % base_name), "\n".join(lines))
	return true


static func find_first_building_hex(scene: Node, building_type: String, player_id: String = "p1") -> Vector2i:
	if scene == null or not ("controller" in scene):
		return Vector2i(-1, -1)
	var controller = scene.controller
	if controller == null or controller.snapshot.is_empty():
		return Vector2i(-1, -1)
	for building in controller.snapshot.get("buildings", []):
		if str(building.get("building_type", "")) in ["headquarters", "command_hub"] and str(building.get("player_id", "")) == player_id:
			return Vector2i(int(building.get("anchor_q", building.get("q", -1))), int(building.get("anchor_r", building.get("r", -1))))
	for building in controller.snapshot.get("military_buildings", []):
		if str(building.get("building_type", "")) in ["headquarters", "command_hub"] and str(building.get("player_id", "")) == player_id:
			return Vector2i(int(building.get("anchor_q", building.get("q", -1))), int(building.get("anchor_r", building.get("r", -1))))
	for building in controller.snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return Vector2i(int(building.get("anchor_q", building.get("q", -1))), int(building.get("anchor_r", building.get("r", -1))))
	for building in controller.snapshot.get("military_buildings", []):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return Vector2i(int(building.get("anchor_q", building.get("q", -1))), int(building.get("anchor_r", building.get("r", -1))))
	return Vector2i(-1, -1)
