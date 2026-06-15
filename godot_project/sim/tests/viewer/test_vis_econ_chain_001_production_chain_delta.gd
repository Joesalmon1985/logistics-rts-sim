extends TestCase

## VIS-ECON-CHAIN-001 — production chain render deltas after 30 ticks.


func get_name() -> String:
	return "test_VIS_ECON_CHAIN_001_production_chain_delta"


func _run_body() -> void:
	var controller := ViewerController.new()
	controller.load_demo_state(EconomyDemo.build_production_chain(), "p1")
	var render_a: Dictionary = controller.get_render_data()
	var fp_a: Dictionary = LayerRenderer.chain_fingerprint(render_a)
	var events_before: int = controller.state.event_log.events.size()

	controller.step_ticks(30)

	var render_b: Dictionary = controller.get_render_data()
	var fp_b: Dictionary = LayerRenderer.chain_fingerprint(render_b)
	var deltas: int = _count_chain_deltas(fp_a, fp_b, controller.state, events_before)
	assert_true(deltas >= 2, "expected at least 2 production chain deltas after 30 ticks (got %d)" % deltas)


static func _count_chain_deltas(a: Dictionary, b: Dictionary, state: GameState, events_before: int) -> int:
	var count: int = 0
	if int(b.get("forest_density", -1)) >= 0 and int(b.get("forest_density", -1)) < int(a.get("forest_density", 999)):
		count += 1
	if int(b.get("woodcutter_timber", 0)) > int(a.get("woodcutter_timber", 0)):
		count += 1
	if int(b.get("sawmill_timber", 0)) > int(a.get("sawmill_timber", 0)) or int(b.get("sawmill_planks", 0)) > int(a.get("sawmill_planks", 0)):
		count += 1
	if int(b.get("carrier_moving", 0)) > int(a.get("carrier_moving", 0)) or int(b.get("ware_routes", 0)) > int(a.get("ware_routes", 0)):
		count += 1
	if str(b.get("carrier_ware", "")) == "timber" and str(a.get("carrier_ware", "")) != "timber":
		count += 1
	for i in range(events_before, state.event_log.events.size()):
		var evt: SimEvent = state.event_log.events[i]
		var et: String = str(evt.type).to_lower()
		if "production_completed" in et or "ware_delivered" in et or "ware_moved" in et or "tree_density" in et:
			count += 1
			break
	return count
