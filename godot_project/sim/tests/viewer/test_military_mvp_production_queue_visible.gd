extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_production_queue_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var panel: RichTextLabel = scene.get_node_or_null("%ProductionQueuesPanel") as RichTextLabel
	assert_true(panel != null, "production panel missing")
	var text: String = panel.get_parsed_text()
	assert_true("Production" in text, "production header visible")
	Helper.teardown(ctx, host)
