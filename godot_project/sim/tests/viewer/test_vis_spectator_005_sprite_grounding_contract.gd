extends TestCase

## VIS-SPECTATOR-005 — sprite grounding contract with synthetic fixtures.


func get_name() -> String:
	return "test_VIS_SPECTATOR_005_sprite_grounding_contract"


func _run_body() -> void:
	var empty_render: Dictionary = {
		"carriers": [],
		"ware_routes": [],
		"delivery_jobs": [],
		"construction_sites": [],
		"buildings": [],
		"roads": [],
		"building_stores": [],
		"diagnostics": {},
	}
	var empty_states: Dictionary = EconomySpriteStateSelector.select_states(empty_render, {}, {}, [])
	for key in empty_states.keys():
		var anim: String = str(empty_states[key].get("animation", ""))
		assert_false(anim.begins_with("carrier_"), "carrier sprite without carriers: %s" % key)
		assert_false(anim.begins_with("construction_"), "construction sprite without sites: %s" % key)
		assert_false(anim.begins_with("ware_"), "ware sprite without routes: %s" % key)

	_assert_grounding_on_all(_carrier_carrying_fixture())
	_assert_grounding_on_all(_ware_plank_fixture())
	_assert_grounding_on_all(_construction_stage_fixture())
	_assert_grounding_on_all(_storage_pulse_fixture())
	_assert_grounding_on_all(_warning_fixture())


func _assert_grounding_on_all(states: Dictionary) -> void:
	for key in states.keys():
		var entry: Dictionary = states[key]
		var grounding: Dictionary = entry.get("grounding", {})
		assert_false(str(grounding.get("reason", "")) == "", "missing grounding.reason on %s" % key)
		assert_false(str(grounding.get("entity_key", "")) == "", "missing grounding.entity_key on %s" % key)


func _carrier_carrying_fixture() -> Dictionary:
	var render: Dictionary = {
		"carriers": [{
			"id": 1, "q": 2, "r": 3, "state": "moving", "ware_type": "timber", "ware_amount": 2,
			"route_points": [{"q": 2, "r": 3}, {"q": 3, "r": 3}],
		}],
		"ware_routes": [], "delivery_jobs": [], "construction_sites": [],
		"buildings": [], "roads": [], "building_stores": [], "diagnostics": {},
	}
	var states: Dictionary = EconomySpriteStateSelector.select_states(render, {}, {}, [])
	assert_true(states.has("carrier:1"), "expected carrier state")
	assert_eq(str(states["carrier:1"].get("animation", "")), "carrier_carrying_loop", "carrying anim")
	return states


func _ware_plank_fixture() -> Dictionary:
	var render: Dictionary = {
		"carriers": [],
		"ware_routes": [{"ware_type": "planks", "q": 1, "r": 1, "points": [{"q": 1, "r": 1}, {"q": 2, "r": 1}]}],
		"delivery_jobs": [], "construction_sites": [],
		"buildings": [], "roads": [], "building_stores": [], "diagnostics": {},
	}
	var states: Dictionary = EconomySpriteStateSelector.select_states(render, {}, {}, [])
	var found: bool = false
	for key in states.keys():
		if str(states[key].get("animation", "")) == "ware_plank_move_loop":
			found = true
	assert_true(found, "expected ware_plank_move_loop")
	return states


func _construction_stage_fixture() -> Dictionary:
	var render: Dictionary = {
		"carriers": [], "ware_routes": [], "delivery_jobs": [],
		"construction_sites": [{"id": 5, "q": 4, "r": 4, "progress_percent": 50}],
		"buildings": [], "roads": [], "building_stores": [], "diagnostics": {},
	}
	var states: Dictionary = EconomySpriteStateSelector.select_states(render, {}, {}, [])
	assert_true(states.has("construction:5"), "expected construction state")
	assert_eq(str(states["construction:5"].get("animation", "")), "construction_progress_50", "50pct stage")
	return states


func _storage_pulse_fixture() -> Dictionary:
	var render: Dictionary = {
		"carriers": [], "ware_routes": [], "delivery_jobs": [], "construction_sites": [],
		"buildings": [], "roads": [],
		"building_stores": [{"building_id": 9, "q": 0, "r": 0}],
		"diagnostics": {"ware_totals": {"by_type": {"timber": 5}}},
	}
	var prev_diag: Dictionary = {"ware_totals": {"by_type": {"timber": 2}}}
	var states: Dictionary = EconomySpriteStateSelector.select_states(render, {}, prev_diag, [])
	assert_true(states.has("store:9"), "expected store state")
	assert_eq(str(states["store:9"].get("animation", "")), "storage_activity_pulse", "storage pulse")
	return states


func _warning_fixture() -> Dictionary:
	var render: Dictionary = {
		"carriers": [], "ware_routes": [], "delivery_jobs": [], "construction_sites": [],
		"buildings": [], "roads": [], "building_stores": [], "diagnostics": {},
	}
	var snap: Dictionary = {"blocked_job_markers": [{"id": 7, "q": 1, "r": 2, "reason": "no_route"}]}
	var states: Dictionary = EconomySpriteStateSelector.select_states(render, snap, {}, [])
	assert_true(states.has("blocked:7"), "expected warning state")
	assert_eq(str(states["blocked:7"].get("animation", "")), "warning_blocked_loop", "warning anim")
	return states
