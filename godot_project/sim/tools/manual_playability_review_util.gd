class_name ManualPlayabilityReviewUtil
extends RefCounted

const OUTPUT_SUBDIR := "manual_playability"
const MIN_PNG_BYTES: int = 1000
const MIN_FONT: int = 14

const PANEL_PATHS: Dictionary = {
	"tutorial_panel": "%TutorialPanel",
	"objective_panel": "%ObjectivePanel",
	"inspector_panel": "%InspectorPanel",
	"inspector": "%Inspector",
	"build_palette": "%BuildPalette",
	"play_button": "%PlayBtn",
	"pause_button": "%PauseBtn",
	"step_button": "%StepBtn",
	"status_label": "%StatusLabel",
	"map_viewport": "%MapViewport",
}


static func output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("../docs/audit/screenshots").path_join(OUTPUT_SUBDIR)


static func ensure_output_dir() -> String:
	var dir: String = output_dir()
	DirAccess.make_dir_recursive_absolute(dir)
	return dir


static func probe_scene(scene: Node, milestone: String = "", mode: String = "") -> Dictionary:
	var entry: Dictionary = {
		"mode": mode,
		"milestone": milestone,
		"window_size": "1280x720",
		"viewport_size": "1280x720",
		"capture_method": ManualPlayabilityCaptureHelper.capture_method(),
		"pass": true,
		"failure_reasons": [],
		"visible_text_blocks": [],
		"required_ui": {},
		"readability": {},
		"functionality": {},
	}
	entry.merge(TutorialNavigationUI.probe_button_texts(scene))
	_probe_panels(scene, entry)
	_evaluate_tutorial_buttons(entry)
	_evaluate_time_controls(scene, entry)
	return entry


static func evaluate_entry(entry: Dictionary) -> bool:
	var ok: bool = true
	for reason in entry.get("failure_reasons", []):
		if str(reason) != "":
			ok = false
	if not bool(entry.get("pass", false)):
		ok = false
	entry["pass"] = ok
	return ok


static func write_review_files(entries: Array, summary_path: String = "") -> void:
	ensure_output_dir()
	var json_path: String = output_dir().path_join("manual_playability_visual_review.json")
	var payload: Dictionary = {
		"generated_at": Time.get_datetime_string_from_system(),
		"capture_method": ManualPlayabilityCaptureHelper.capture_method(),
		"entries": entries,
	}
	var f := FileAccess.open(json_path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(payload, "\t"))
	var md_lines: PackedStringArray = PackedStringArray([
		"# Manual Playability Visual Review",
		"",
		"Capture method: `%s`" % ManualPlayabilityCaptureHelper.capture_method(),
		"",
		"| Screenshot | Mode | Pass | Failures |",
		"|---|---|---|---|",
	])
	for entry in entries:
		var reasons: Array = entry.get("failure_reasons", [])
		md_lines.append("| %s | %s | %s | %s |" % [
			str(entry.get("screenshot_path", "")),
			str(entry.get("mode", "")),
			str(entry.get("pass", false)),
			"; ".join(PackedStringArray(reasons.map(func(r): return str(r)))),
		])
	FileAccess.open(output_dir().path_join("manual_playability_visual_review.md"), FileAccess.WRITE).store_string("\n".join(md_lines))


static func _probe_panels(scene: Node, entry: Dictionary) -> void:
	var required: Dictionary = {}
	for key in PANEL_PATHS.keys():
		var node: Control = scene.get_node_or_null(str(PANEL_PATHS[key])) as Control
		var visible: bool = node != null and node.visible
		required[key + "_visible"] = visible
		if node != null and visible:
			entry["visible_text_blocks"].append({
				"text": _node_text(node),
				"rect": _rect_str(node),
				"font_size_or_estimate": _font_estimate(node),
				"contrast_notes": "panel_present",
			})
	entry["required_ui"] = required


static func _evaluate_tutorial_buttons(entry: Dictionary) -> void:
	var back: String = str(entry.get("tutorial_back_button_text", ""))
	var nxt: String = str(entry.get("tutorial_next_button_text", ""))
	var skip: String = str(entry.get("tutorial_skip_button_text", ""))
	var exit_text: String = str(entry.get("tutorial_exit_button_text", ""))
	var step_text: String = str(entry.get("tutorial_step_number_text", ""))
	var is_tutorial: bool = str(entry.get("milestone", "")).contains("tutorial") or str(entry.get("mode", "")).contains("tutorial")
	if str(entry.get("milestone", "")).contains("main_menu"):
		return
	if not is_tutorial:
		return
	entry["required_ui"]["tutorial_panel_visible"] = str(entry.get("tutorial_title_text", "")).length() > 0
	entry["required_ui"]["tutorial_title_visible"] = str(entry.get("tutorial_title_text", "")).length() > 3
	entry["required_ui"]["tutorial_instruction_visible"] = str(entry.get("tutorial_instruction_text", "")).length() > 10
	entry["required_ui"]["tutorial_back_visible"] = back.length() > 0
	entry["required_ui"]["tutorial_next_visible"] = nxt.length() > 0
	entry["required_ui"]["tutorial_skip_visible"] = skip.length() > 0
	entry["required_ui"]["tutorial_exit_visible"] = exit_text.length() > 0
	if back.is_empty():
		_add_fail(entry, "tutorial_back_button_text missing")
	if nxt.is_empty():
		_add_fail(entry, "tutorial_next_button_text missing")
	if skip.is_empty():
		_add_fail(entry, "tutorial_skip_button_text missing")
	if exit_text.is_empty():
		_add_fail(entry, "tutorial_exit_button_text missing")
	if step_text.is_empty() or not step_text.contains("Step"):
		_add_fail(entry, "tutorial_step_number_text missing or invalid")
	if nxt.is_empty() and back.is_empty() and not skip.is_empty() and not exit_text.is_empty():
		_add_fail(entry, "stranded: only Skip and Back to Main Menu available")


static func _evaluate_time_controls(scene: Node, entry: Dictionary) -> void:
	if str(entry.get("milestone", "")).contains("main_menu"):
		return
	var play: Button = _find_btn(scene, "PlayBtn")
	var pause: Button = _find_btn(scene, "PauseBtn")
	var step: Button = _find_btn(scene, "StepBtn")
	var play_pause: Button = _find_btn(scene, "PlayPauseBtn")
	var status: Label = scene.get_node_or_null("%StatusLabel") as Label
	var has_play: bool = (play != null and play.visible) or (play_pause != null and play_pause.visible)
	entry["required_ui"]["play_pause_visible"] = has_play
	entry["required_ui"]["time_controls_visible"] = has_play and (step != null or play_pause != null)
	entry["required_ui"]["tick_visible"] = status != null and (status.text.contains("Tick") or status.text.contains("tick"))
	if not has_play:
		_add_fail(entry, "Play button missing or hidden")
	if status == null or (not status.text.contains("Simulation:") and not status.text.contains("Tick") and not status.text.contains("PAUSED") and not status.text.contains("AUTOPLAY")):
		_add_fail(entry, "status label missing tick/pause info")


static func _add_fail(entry: Dictionary, reason: String) -> void:
	entry["pass"] = false
	var reasons: Array = entry.get("failure_reasons", [])
	reasons.append(reason)
	entry["failure_reasons"] = reasons


static func _node_text(node: Control) -> String:
	if node is Label:
		return (node as Label).text
	if node is RichTextLabel:
		return (node as RichTextLabel).get_parsed_text()
	if node is Button:
		return (node as Button).text
	return ""


static func _rect_str(node: Control) -> String:
	var r: Rect2 = node.get_global_rect()
	return "%d,%d %dx%d" % [int(r.position.x), int(r.position.y), int(r.size.x), int(r.size.y)]


static func _font_estimate(node: Control) -> int:
	if node is Label:
		return int((node as Label).get_theme_font_size("font_size"))
	if node is RichTextLabel:
		return int((node as RichTextLabel).get_theme_font_size("normal_font_size"))
	if node is Button:
		return int((node as Button).get_theme_font_size("font_size"))
	return MIN_FONT


static func _find_btn(scene: Node, name: String) -> Button:
	if scene == null:
		return null
	var via_unique: Button = scene.get_node_or_null("%" + name) as Button
	if via_unique != null:
		return via_unique
	return scene.find_child(name, true, false) as Button
