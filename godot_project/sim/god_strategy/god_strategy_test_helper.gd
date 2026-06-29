class_name GodStrategyTestHelper
extends RefCounted

const SCENARIO_PATH: String = "scenarios/god_strategy/god_strategy_4p_mvp_a.json"


static func load_scenario() -> Dictionary:
	return ConfigLoader.load_scenario(SCENARIO_PATH)


static func prep_all_spawns(state: GameState, scenario: Dictionary) -> void:
	var w: int = state.map.width - 1
	var h: int = state.map.height - 1
	var center_q: int = state.map.width / 2
	var center_r: int = state.map.height / 2
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var hq_q: int = int(spawn.get("q", 0))
		var hq_r: int = int(spawn.get("r", 0))
		var pid: String = str(spawn.get("player_id", ""))
		HybridMapSetup.prepare_playable_map(state, 0, 0, w, h, hq_q, hq_r, center_q, center_r, pid)


static func create_prepared_state(ticks: int = 0) -> GameState:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = load_scenario()
	var state: GameState = runner.create_game(scenario)
	prep_all_spawns(state, scenario)
	if ticks > 0:
		state = runner.run_ticks(state, ticks)
	return state


static func run_scripted_match(max_ticks: int = 8000) -> Dictionary:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = load_scenario()
	scenario["max_ticks"] = max_ticks
	var state: GameState = runner.create_game(scenario)
	prep_all_spawns(state, scenario)
	for pid in scenario.get("players", ["p1", "p2", "p3", "p4"]):
		quad.set_bot(str(pid), ScriptedGodStrategyBot.new(str(pid)))
	state = quad.run_match(state, scenario)
	return {
		"state": state,
		"scenario": scenario,
		"recorder": quad.get_recorder(),
		"match_end": GodStrategyMatchEnd.evaluate(state, scenario),
	}


static func building_of_type(state: GameState, building_type: String, player_id: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	for building in EconomyState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	return 0


static func count_units(state: GameState, unit_type: String, player_id: String) -> int:
	var count: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type and str(unit.get("player_id", "")) == player_id:
			count += 1
	return count
