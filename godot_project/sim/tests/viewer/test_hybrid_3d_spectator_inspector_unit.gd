extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_inspector_unit"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	scene.call("advance_for_tests", 200)
	Helper.force_layout(ctx)
	var controller: Hybrid3dSpectatorController = scene.controller
	var picked: Dictionary = {}
	for unit in controller.snapshot.get("units", []):
		picked = {"kind": "unit", "data": unit}
		break
	if picked.is_empty():
		picked = {"kind": "carrier", "data": {"player_id": "p1", "carried_ware": "planks"}}
	var text: String = Hybrid3dInspectorPanel.format_selection(picked, controller.snapshot)
	assert_true("Carrier" in text or "Squad" in text or "Scout" in text or "Harvester" in text, text)
	Helper.teardown(ctx, host)
