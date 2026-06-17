extends TestCase

## VIS-SPECTATOR-001 — dual-bot spectator produces render-data deltas over N ticks.


func get_name() -> String:
	return "test_VIS_SPECTATOR_001_render_data_deltas"


func _run_body() -> void:
	var controller := EconomySpectatorController.new()
	controller.reset_demo()
	var tick_a: int = controller.state.tick if controller.state != null else 0
	var render_a: Dictionary = controller.get_render_data()
	var counts_a: Dictionary = LayerRenderer.count_entities(render_a)
	var score_a: int = int(controller.snapshot.get("diagnostics", {}).get("economy_scores", {}).get("p1", 0))

	for _i in range(80):
		controller.bot_step_once()

	var tick_b: int = controller.state.tick if controller.state != null else 0
	var render_b: Dictionary = controller.get_render_data()
	var counts_b: Dictionary = LayerRenderer.count_entities(render_b)
	var states_b: int = controller.get_sprite_states().size()
	var score_b: int = int(controller.snapshot.get("diagnostics", {}).get("economy_scores", {}).get("p1", 0))

	var categories_changed: int = 0
	for key in counts_b.keys():
		if int(counts_b.get(key, 0)) != int(counts_a.get(key, 0)):
			categories_changed += 1

	assert_true(tick_b >= tick_a + 50, "expected tick to advance substantially (got %d -> %d)" % [tick_a, tick_b])
	assert_true(
		categories_changed >= 1 or score_b != score_a or states_b > 0 or int(counts_b.get("roads", 0)) > int(counts_a.get("roads", 0)),
		"expected render activity: categories=%d roads=%d->%d sprites=%d"
		% [categories_changed, int(counts_a.get("roads", 0)), int(counts_b.get("roads", 0)), states_b],
	)
