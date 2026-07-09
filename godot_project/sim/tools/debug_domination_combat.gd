extends SceneTree

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")
const EvalRunner := preload("res://sim/god_strategy/god_strategy_eval_runner.gd")


func _initialize() -> void:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = ConfigLoader.load_scenario(EvalRunner.SCENARIO_REGRESSION)
	scenario["max_ticks"] = 400
	scenario["stall_ticks_threshold"] = 0
	var state: GameState = runner.create_game(scenario)
	Helper.prep_domination_regression(state, scenario)
	quad.set_bot("p1", ScriptedGodStrategyBot.new("p1"))
	for pid in ["p2", "p3", "p4"]:
		quad.set_bot(str(pid), PassiveBot.new(str(pid)))
	state = quad.run_match(state, scenario)
	print("tick=", state.tick)
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) != "p1":
			continue
		print(
			"unit id=%s type=%s q=%s r=%s state=%s weapon=%s path_len=%s path_idx=%s order=%s engage=%s" % [
				unit.get("id", 0),
				unit.get("unit_type", ""),
				unit.get("q", 0),
				unit.get("r", 0),
				unit.get("state", ""),
				unit.get("weapon", ""),
				(unit.get("path", []) as Array).size(),
				unit.get("path_index", 0),
				JSON.stringify(unit.get("order", {})),
				unit.get("engage_target_id", 0),
			],
		)
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) != "command_hub":
			continue
		print(
			"hub pid=%s hp=%s state=%s q=%s r=%s" % [
				building.get("player_id", ""),
				building.get("hp", 0),
				building.get("state", ""),
				building.get("anchor_q", 0),
				building.get("anchor_r", 0),
			],
		)
	var end: Dictionary = GodStrategyMatchEnd.evaluate(state, scenario)
	print("match_end=", JSON.stringify(end))
	var dmg_events: int = 0
	for event in state.event_log.events:
		if str(event.type) == "damage_dealt":
			dmg_events += 1
	print("damage_events=", dmg_events)
	quit(0)
