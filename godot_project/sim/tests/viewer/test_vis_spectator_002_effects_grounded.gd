extends TestCase

## VIS-SPECTATOR-002 — sprite effects only from real snapshot/state changes.


func get_name() -> String:
	return "test_VIS_SPECTATOR_002_effects_grounded"


func _run_body() -> void:
	var controller := EconomySpectatorController.new()
	controller.reset_demo()
	var render: Dictionary = controller.get_render_data()
	var states: Dictionary = EconomySpriteStateSelector.select_states(
		render,
		controller.snapshot,
		{},
		[],
	)
	for key in states.keys():
		var anim: String = str(states[key].get("animation", ""))
		assert_false(anim == "carrier_move_loop" and render.get("carriers", []).is_empty(), "carrier_move_loop without carriers")
		assert_false(anim == "construction_progress_loop" and render.get("construction_sites", []).is_empty(), "construction progress without sites")

	var counts_before: Dictionary = LayerRenderer.count_entities(render)
	var frozen_states: Dictionary = controller.get_sprite_states()
	for _i in range(20):
		controller.bot_step_once()
	var after_states: Dictionary = controller.get_sprite_states()
	var render_after: Dictionary = controller.get_render_data()
	if int(LayerRenderer.count_entities(render_after).get("roads", 0)) > int(counts_before.get("roads", 0)):
		assert_true(after_states.size() >= 1, "expected sprite states when roads grow")

	# Production chain demo has known logistics activity.
	var chain_controller := ViewerController.new()
	chain_controller.load_demo_state(EconomyDemo.build_production_chain(), "p1")
	var chain_render: Dictionary = chain_controller.get_render_data()
	var chain_states: Dictionary = EconomySpriteStateSelector.select_states(
		chain_render,
		chain_controller.snapshot,
		{},
		[],
	)
	var has_logistics_sprite: bool = false
	for key in chain_states.keys():
		var anim: String = str(chain_states[key].get("animation", ""))
		if anim in ["carrier_move_loop", "carrier_idle", "ware_log_move_loop", "ware_generic_move_loop", "road_flow_loop"]:
			has_logistics_sprite = true
	assert_true(has_logistics_sprite or not chain_render.get("carriers", []).is_empty(), "production chain should ground logistics sprites or expose carriers")
