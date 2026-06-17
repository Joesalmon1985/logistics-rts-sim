extends TestCase

## VIS-SPECTATOR-006 — overlay toggles are viewer-only; sprite budget bounded.


func get_name() -> String:
	return "test_VIS_SPECTATOR_006_overlay_toggles_sprite_budget"


func _run_body() -> void:
	var controller := EconomySpectatorController.new()
	controller.reset_demo(EconomySpectatorController.DEMO_PRODUCTION_CHAIN)
	for _i in range(30):
		controller.step_tick()

	var hash_before: String = ReplayHash.compute_state_hash(controller.state)
	var snap_before: Dictionary = controller.snapshot.duplicate(true)
	var sprites_before: Dictionary = controller.get_sprite_states().duplicate(true)

	controller.set_layer_visible("sprites", false)
	controller.set_layer_visible("routes", false)
	controller.set_layer_visible("sprites", true)
	controller.set_layer_visible("routes", true)

	var hash_after: String = ReplayHash.compute_state_hash(controller.state)
	assert_eq(hash_before, hash_after, "overlay toggles must not change authoritative state hash")
	assert_eq(
		JSON.stringify(snap_before),
		JSON.stringify(controller.snapshot),
		"overlay toggles must not change exported snapshot",
	)

	var states: Dictionary = controller.get_sprite_states()
	assert_true(states.size() <= EconomySpectatorMetrics.SPRITE_BUDGET_CAP, "sprite states exceed budget cap")
	assert_true(sprites_before.size() <= EconomySpectatorMetrics.SPRITE_BUDGET_CAP, "sprite states exceed budget cap after steps")

	# Duplicate refresh must not grow sprite state keys.
	var sig_a: String = EconomySpectatorMetrics.sprite_state_signature(states)
	controller.render_bridge.update(controller.state, controller.snapshot, controller.layer_visibility)
	var sig_b: String = EconomySpectatorMetrics.sprite_state_signature(controller.get_sprite_states())
	assert_eq(sig_a, sig_b, "duplicate snapshot refresh must not change sprite signature")

	# Scene mount: toggling sprites changes presentation only.
	SimBootstrap.load_all()
	var host := Node.new()
	var ctx: Dictionary = ViewerTestHarness.mount(host, "spectator")
	assert_true(bool(ctx.get("ok", false)), "spectator mount failed")
	var scene: Node = ctx.get("scene")
	var scene_controller: EconomySpectatorController = scene.controller
	scene_controller.reset_demo(EconomySpectatorController.DEMO_PRODUCTION_CHAIN)
	for _j in range(5):
		scene_controller.step_tick()
	if scene.has_method("_refresh_map"):
		scene._refresh_map()
	var sprite_layer: EconomySpriteLayer = scene.get_node_or_null("%SpriteLayer") as EconomySpriteLayer
	assert_true(sprite_layer != null, "missing sprite layer")
	var count_on: int = sprite_layer.active_count
	scene_controller.set_layer_visible("sprites", false)
	if scene.has_method("_refresh_map"):
		scene._refresh_map()
	var count_off: int = sprite_layer.active_count
	assert_true(count_on >= 0, "sprite count invalid")
	assert_eq(count_off, 0, "sprites hidden should yield zero active sprites")
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
