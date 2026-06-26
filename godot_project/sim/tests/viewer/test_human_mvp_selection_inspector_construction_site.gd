extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_selection_inspector_construction_site"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	var controller: SettlerMvpController = SettlerMvpTestHelper.get_controller(scene)
	controller.set_build_mode("sawmill")
	# Attempt placement on valid grass near HQ — may fail if no valid hex; check formatter via snapshot fixture
	var snap: Dictionary = controller.get_snapshot()
	var formatted: String = SelectionFormatter.format(
		{"kind": "construction_site", "data": {
			"building_type": "sawmill", "anchor_q": 8, "anchor_r": 6,
			"delivered": {"planks": 1}, "required": {"planks": 2, "stone": 1},
			"state": "waiting",
		}},
		snap,
	)
	assert_true(formatted.find("Construction Site") >= 0, "site header")
	assert_true(formatted.find("planks") >= 0, "materials listed")
	SettlerMvpTestHelper.teardown(ctx, host)
