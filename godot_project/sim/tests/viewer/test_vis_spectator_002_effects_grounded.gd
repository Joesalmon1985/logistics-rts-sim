extends TestCase

## VIS-SPECTATOR-002 — sprite effects only from real snapshot/state changes.


func get_name() -> String:
	return "test_VIS_SPECTATOR_002_effects_grounded"


func _run_body() -> void:
	var controller := EconomySpectatorController.new()
	controller.reset_demo(EconomySpectatorController.DEMO_PRODUCTION_CHAIN)
	var render: Dictionary = controller.get_render_data()
	var states: Dictionary = EconomySpriteStateSelector.select_states(
		render,
		controller.snapshot,
		{},
		[],
	)
	for key in states.keys():
		var entry: Dictionary = states[key]
		var anim: String = str(entry.get("animation", ""))
		var grounding: Dictionary = entry.get("grounding", {})
		assert_false(str(grounding.get("reason", "")) == "", "missing grounding on %s" % key)
		assert_false(anim == "carrier_move_loop" and render.get("carriers", []).is_empty(), "carrier_move_loop without carriers")
		assert_false(anim == "carrier_carrying_loop" and render.get("carriers", []).is_empty(), "carrier_carrying without carriers")
		assert_false(anim.begins_with("construction_") and anim != "construction_complete_pulse" and render.get("construction_sites", []).is_empty(), "construction without sites")

	var empty_states: Dictionary = EconomySpriteStateSelector.select_states(
		{"carriers": [], "ware_routes": [], "delivery_jobs": [], "construction_sites": [], "buildings": [], "roads": [], "building_stores": [], "diagnostics": {}},
		{},
		{},
		[],
	)
	for key in empty_states.keys():
		var anim: String = str(empty_states[key].get("animation", ""))
		assert_false(anim.begins_with("carrier_"), "carrier anim on empty render")
		assert_false(anim.begins_with("ware_"), "ware anim on empty render")

	var sprite_before: Dictionary = controller.get_sprite_states().duplicate(true)
	for _i in range(20):
		controller.step_tick()
	var sprite_after: Dictionary = controller.get_sprite_states().duplicate(true)
	var activity: Dictionary = EconomySpectatorMetrics.measure_activity(
		LayerRenderer.count_entities(render),
		LayerRenderer.count_entities(controller.get_render_data()),
		EconomySpectatorMetrics.chain_fingerprint(render),
		EconomySpectatorMetrics.chain_fingerprint(controller.get_render_data()),
		sprite_before,
		sprite_after,
		controller.snapshot,
		controller.snapshot,
		0,
		controller.state.event_log.events.size() if controller.state != null else 0,
	)
	assert_true(
		bool(activity.get("signals", {}).get("sprite_state_set_changed", false))
		or bool(activity.get("signals", {}).get("chain_fingerprint_changed", false)),
		"stepping should change sprite set or chain fingerprint",
	)

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
		if anim in ["carrier_move_loop", "carrier_carrying_loop", "carrier_idle", "ware_log_move_loop", "ware_plank_move_loop", "ware_generic_move_loop", "road_flow_loop"]:
			has_logistics_sprite = true
	assert_true(has_logistics_sprite or not chain_render.get("carriers", []).is_empty(), "production chain should ground logistics sprites or expose carriers")
