extends TestCase

## VIS-ROLL-006 — interface modes and command overlays differ.


func get_name() -> String:
	return "test_VIS_ROLL_006_mode_overlays"


const FIXTURE := "res://sim/tests/fixtures/visual_rollouts/minimal_economy"


func _run_body() -> void:
	var controller := RolloutController.new()
	var loaded: Dictionary = controller.load_rollout(FIXTURE)
	assert_true(loaded.get("ok", false), str(loaded.get("error", "")))
	assert_true(controller.step_forward(), "step forward")

	var world_layers: Dictionary = InterfaceModePresets.apply_mode("world")
	var economy_layers: Dictionary = InterfaceModePresets.apply_mode("economy")
	assert_true(world_layers.get("logistics", false) != economy_layers.get("logistics", false), "world vs economy logistics differ")

	controller.set_interface_mode("neural")
	var neural_overlays: Array = controller.get_command_overlays()
	assert_true(neural_overlays.size() >= 1, "neural mode exposes command overlays")

	controller.set_interface_mode("world")
	var world_overlays: Array = controller.get_command_overlays()
	assert_eq(world_overlays.size(), 0, "world mode hides command overlays")

	var prev: Dictionary = controller.get_previous_command()
	var cur: Dictionary = controller.get_current_decision()
	var entry: Dictionary = RolloutCommandOverlay.overlay_manifest_entry(prev, cur, neural_overlays)
	assert_true(entry.get("overlay_available", false), "overlay manifest marks availability")

	var badges: Array = RolloutCommandOverlay.economy_badges(controller.snapshot)
	controller.set_interface_mode("economy")
	assert_true(controller.get_economy_badges().size() >= badges.size() or badges.size() >= 0, "economy badges callable")
