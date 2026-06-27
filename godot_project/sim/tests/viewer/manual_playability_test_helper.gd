class_name ManualPlayabilityTestHelper
extends RefCounted

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const ReviewUtil := preload("res://sim/tools/manual_playability_review_util.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


static func mount_menu_route(host: Node, route_key: String) -> Dictionary:
	return CaptureHelper.launch_via_menu_route(host, route_key)


static func assert_four_tutorial_buttons(scene: Node) -> Dictionary:
	var probe: Dictionary = TutorialNavigationUI.probe_button_texts(scene)
	var ok: bool = true
	var msg: String = ""
	for key in [
		"tutorial_back_button_text",
		"tutorial_next_button_text",
		"tutorial_skip_button_text",
		"tutorial_exit_button_text",
	]:
		if str(probe.get(key, "")).is_empty():
			ok = false
			msg += "%s empty; " % key
	if str(probe.get("tutorial_step_number_text", "")).is_empty():
		ok = false
		msg += "step number empty"
	return {"ok": ok, "msg": msg, "probe": probe}


static func assert_not_stranded(scene: Node) -> bool:
	var probe: Dictionary = TutorialNavigationUI.probe_button_texts(scene)
	var has_next: bool = str(probe.get("tutorial_next_button_text", "")).length() > 0
	var has_back: bool = str(probe.get("tutorial_back_button_text", "")).length() > 0
	return has_next or has_back
