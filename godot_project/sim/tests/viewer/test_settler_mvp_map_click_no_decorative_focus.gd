extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-INPUT-003 — map click must not focus decorative title/header.


const HQ_Q: int = 6
const HQ_R: int = 6


func get_name() -> String:
	return "test_SETTLER_MVP_INPUT_003_map_click_no_decorative_focus"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, HQ_Q, HQ_R)
	assert_false(SettlerMvpTestHelper.focus_is_decorative(scene), "decorative label stole focus after map click")
	var selected: Vector2i = SettlerMvpTestHelper.get_selected_hex(scene)
	assert_eq(selected.x, HQ_Q, "selected q")
	assert_eq(selected.y, HQ_R, "selected r")
	assert_false(SettlerMvpTestHelper.get_inspector_text(scene).is_empty(), "inspector did not update")
	SettlerMvpTestHelper.teardown(ctx, host)
