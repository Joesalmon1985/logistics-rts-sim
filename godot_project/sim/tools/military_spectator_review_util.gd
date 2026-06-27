class_name MilitarySpectatorReviewUtil
extends RefCounted

## Visibility and plain-English probes for military spectator screens.

const VIEWPORT_SIZE := Vector2i(1920, 1080)
const MIN_PNG_BYTES: int = 1000
const REQUIRED_PLAIN_ENGLISH: Array = [
	"Command Hub",
	"Rifle Squad",
	"Barracks",
	"Credits",
	"Power",
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
		"asset_gate_pass": true,
		"panels": {},
		"failures": [],
	}
	var panel_specs: Dictionary = {
		"summary": {"path": "%SummaryLabel", "min_len": 20, "need": ["Credits", "Power"]},
		"production": {"path": "%ProductionQueuesPanel", "min_len": 12, "need": ["Production"]},
		"inspector": {"path": "%Inspector", "min_len": 10, "need": []},
		"events": {"path": "%EventLog", "min_len": 4, "need": []},
		"recent_changes": {"path": "%RecentChangesPanel", "min_len": 4, "need": []},
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
		for token in spec.get("need", []):
			if not (str(token) in text):
				review["plain_english_gate_pass"] = false
				review["failures"].append("plain:%s:missing:%s" % [key, token])
	review["asset_gate_pass"] = _assets_ready()
	if not review["asset_gate_pass"]:
		review["failures"].append("assets:missing_registry_textures")
	review["visibility_gate_pass"] = bool(review["visibility_gate_pass"]) and bool(review["asset_gate_pass"])
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
		"passes": probe.get("visible", false) and missing.is_empty(),
	}


static func write_visual_review(summary: Dictionary, base_name: String = "military_spectator_visual_review") -> bool:
	var dir: String = ensure_output_dir()
	var json_path: String = dir.path_join("%s.json" % base_name)
	var md_path: String = dir.path_join("%s.md" % base_name)
	SimPaths.write_text_file(json_path, JSON.stringify(summary, "\t"))
	var lines: PackedStringArray = PackedStringArray(["# Military spectator visual review", ""])
	for shot in summary.get("shots", []):
		lines.append("## %s" % shot.get("file", "?"))
		lines.append("- visibility_gate_pass: %s" % str(shot.get("visibility_gate_pass", false)))
		if shot.has("failures"):
			lines.append("- failures: %s" % str(shot.get("failures", [])))
		lines.append("")
	SimPaths.write_text_file(md_path, "\n".join(lines))
	return true


static func _assets_ready() -> bool:
	MilitaryAssetRegistry.load_registry()
	for pair in MilitaryAssetRegistry.required_concepts():
		if MilitaryAssetRegistry.get_texture(str(pair[0]), str(pair[1])) == null:
			return false
	return true


static func find_first_unit_hex(scene: Node, unit_type: String, player_id: String = "p1") -> Vector2i:
	if scene == null or not ("controller" in scene):
		return Vector2i(-1, -1)
	var controller = scene.controller
	if controller == null or controller.snapshot.is_empty():
		return Vector2i(-1, -1)
	for unit in controller.snapshot.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		if str(unit.get("unit_type", "")) != unit_type:
			continue
		if str(unit.get("player_id", "")) != player_id:
			continue
		return Vector2i(int(unit.get("q", -1)), int(unit.get("r", -1)))
	return Vector2i(-1, -1)


static func find_first_building_hex(scene: Node, building_type: String, player_id: String = "p1") -> Vector2i:
	if scene == null or not ("controller" in scene):
		return Vector2i(-1, -1)
	var controller = scene.controller
	if controller == null or controller.snapshot.is_empty():
		return Vector2i(-1, -1)
	for building in controller.snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		if str(building.get("building_type", "")) != building_type:
			continue
		if str(building.get("player_id", "")) != player_id:
			continue
		return Vector2i(
			int(building.get("anchor_q", building.get("q", -1))),
			int(building.get("anchor_r", building.get("r", -1))),
		)
	return Vector2i(-1, -1)
