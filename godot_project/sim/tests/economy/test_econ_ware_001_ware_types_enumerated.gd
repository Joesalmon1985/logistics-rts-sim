extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_WARE_001_ware_types_enumerated"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 37,
		"map_width": 4,
		"map_height": 4,
		"players": ["p1"],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var wares: Dictionary = cfg.wares
	assert_true(wares.size() >= 10, "economy config must enumerate ware types")

	var required: Array[String] = [
		"timber", "planks", "stone", "grain", "tools", "coal", "iron_ore", "rations",
	]
	for ware_type in required:
		assert_true(wares.has(ware_type), "missing ware type: %s" % ware_type)
		var wdef: Dictionary = wares[ware_type]
		assert_has_keys(wdef, ["display_name", "transportable"], "ware def shape for %s" % ware_type)

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var store: Dictionary = EconomyState.get_ware_store(state, int(hq.get("store_id", 0)))
	assert_true(store.get("wares", {}).size() >= 1, "HQ starting wares must populate a store")
