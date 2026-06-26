extends TestCase

const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_inspector_construction_site_plain_english"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerHelper.mount_settler(host)
	var snapshot: Dictionary = ctx.get("scene").controller.get_snapshot()
	var selection: Dictionary = {
		"kind": "construction_site",
		"data": {
			"building_type": "woodcutter",
			"anchor_q": 4,
			"anchor_r": 4,
			"delivered": {"planks": 1},
			"required": {"planks": 2, "stone": 1},
		},
	}
	var text: String = SelectionFormatter.format(selection, snapshot, {})
	assert_true(text.contains("Construction Site"), text)
	assert_true(text.contains("Materials") or text.contains("planks"), text)
	SettlerHelper.teardown(ctx, host)
