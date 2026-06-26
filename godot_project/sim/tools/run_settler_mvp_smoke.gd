extends SceneTree

## Headless smoke for Settler MVP scenario and command path.

const SCENARIO := "scenarios/economy/settler_mvp.json"


func _init() -> void:
	SimBootstrap.load_all()
	var ok: bool = _run_smoke()
	quit(0 if ok else 1)


func _run_smoke() -> bool:
	print("SETTLER-MVP-SMOKE-001: scenario init")
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	var state: GameState = runner.create_game(scenario)
	if int(EconomyState.get_meta_value(state, "hq_building_id", 0)) <= 0:
		print("FAIL: no HQ")
		return false

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	if not snapshot.has("objective"):
		print("FAIL: objective missing from snapshot")
		return false

	print("SETTLER-MVP-SMOKE-002: placement command path")
	var controller := SettlerMvpController.new()
	controller.reset_game()
	var legal: Vector2i = Vector2i(-1, -1)
	for r in range(controller.get_state().map.height):
		for q in range(controller.get_state().map.width):
			if EconomyMapRules.can_place_building(controller.get_state(), "sawmill", q, r, "p1").get("ok", false):
				legal = Vector2i(q, r)
				break
		if legal.x >= 0:
			break
	if legal.x < 0:
		print("FAIL: no legal sawmill hex")
		return false
	controller.set_build_mode("sawmill")
	if not controller.handle_hex_click(legal.x, legal.y).get("success", false):
		print("FAIL: placement rejected")
		return false

	print("SETTLER-MVP-SMOKE-003: tick progression")
	for _i in range(5):
		controller.step_tick()
	if int(controller.get_snapshot().get("tick", 0)) <= 0:
		print("FAIL: tick did not advance")
		return false

	var export_path: String = SimPaths.default_export_file("settler_mvp_smoke/snapshot.json")
	if not SimPaths.write_text_file(export_path, JSON.stringify(controller.get_snapshot())):
		print("FAIL: cannot write snapshot export")
		return false

	print("SETTLER MVP SMOKE PASS")
	print("EXIT: 0 (pass)")
	return true
