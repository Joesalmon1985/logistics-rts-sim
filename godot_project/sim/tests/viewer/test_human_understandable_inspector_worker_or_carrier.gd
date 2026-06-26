extends TestCase

const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_inspector_worker_or_carrier"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerHelper.mount_settler(host)
	var snapshot: Dictionary = ctx.get("scene").controller.get_snapshot()
	var selection: Dictionary = {
		"kind": "carrier",
		"data": {
			"state": "moving",
			"ware_type": "timber",
			"ware_amount": 1,
			"route_points": [{"q": 6, "r": 6}, {"q": 7, "r": 6}],
		},
	}
	var text: String = SelectionFormatter.format(selection, snapshot, {})
	assert_true(text.contains("Carrier"), text)
	assert_true(text.contains("timber") or text.contains("Timber"), text)
	SettlerHelper.teardown(ctx, host)
