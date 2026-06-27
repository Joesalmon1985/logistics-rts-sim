extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const ReviewUtil := preload("res://sim/tools/manual_playability_review_util.gd")

const ROUTES: Array = [
	"settler_tutorial", "settler_free_play", "skirmish_tutorial", "skirmish_mvp",
	"hybrid_tutorial", "hybrid_mvp", "hybrid_3d",
]


func get_name() -> String:
	return "test_manual_qa_required_panels_visible_all_modes"


func _run_body() -> void:
	for route in ROUTES:
		var host := Node.new()
		Engine.get_main_loop().root.add_child(host)
		var ctx: Dictionary = Helper.mount_menu_route(host, route)
		var scene: Node = ctx.get("scene")
		var entry: Dictionary = ReviewUtil.probe_scene(scene, "launch", route)
		var play_ok: bool = bool(entry.get("required_ui", {}).get("play_pause_visible", false))
		if route == "hybrid_3d":
			var pp: Button = scene.find_child("PlayPauseBtn", true, false) as Button
			play_ok = pp != null and pp.visible
		assert_true(play_ok, "%s play/pause" % route)
		CaptureHelper.teardown(ctx, host)
