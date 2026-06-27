extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_build_palette_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var palette: MilitaryBuildPalette = scene.get_node_or_null("%BuildPalette") as MilitaryBuildPalette
	assert_true(palette != null, "build palette missing")
	if palette != null:
		palette.ensure_built()
	assert_true(palette.get_button("power_generator") != null, "power generator button")
	assert_true(palette.get_button("barracks") != null, "barracks button")
	assert_true(palette.get_button("rifle_squad") != null, "queue rifle button")
	Helper.teardown(ctx, host)
