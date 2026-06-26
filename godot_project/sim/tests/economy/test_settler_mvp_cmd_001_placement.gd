extends "res://sim/tests/test_case.gd"

## SETTLER-CMD-001 — legal building placement via SettlerMvpController command path.


func get_name() -> String:
	return "test_settler_mvp_cmd_001_placement"


func _run_body() -> void:
	var controller := SettlerMvpController.new()
	controller.reset_game()
	var state: GameState = controller.get_state()
	assert_true(state != null, "game state loaded")

	var target: Vector2i = Vector2i(-1, -1)
	for r in range(state.map.height):
		for q in range(state.map.width):
			var check: Dictionary = EconomyMapRules.can_place_building(state, "sawmill", q, r, "p1")
			if check.get("ok", false):
				target = Vector2i(q, r)
				break
		if target.x >= 0:
			break
	assert_true(target.x >= 0, "legal sawmill hex exists")

	controller.set_build_mode("sawmill")
	var outcome: Dictionary = controller.handle_hex_click(target.x, target.y)
	assert_true(outcome.get("success", false), "placement accepted")

	var sites: Array = controller.get_snapshot().get("construction_sites", [])
	var found: bool = false
	for site in sites:
		if str(site.get("building_type", "")) == "sawmill":
			if int(site.get("anchor_q", site.get("q", -1))) == target.x:
				found = true
				break
	assert_true(found, "construction site in snapshot")

	_test_invalid_placement(controller)


func _test_invalid_placement(controller: SettlerMvpController) -> void:
	var before_sites: int = controller.get_snapshot().get("construction_sites", []).size()
	controller.set_build_mode("quarry")
	var outcome: Dictionary = controller.handle_hex_click(6, 6)
	assert_false(outcome.get("success", true), "HQ hex rejects quarry")
	assert_eq(controller.get_snapshot().get("construction_sites", []).size(), before_sites, "no extra site")
