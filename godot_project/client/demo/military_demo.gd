class_name MilitaryDemo
extends RefCounted

## Curated military trial demo state for RUN_05 viewer.


static func build_state() -> GameState:
	var scenario: Dictionary = MilitarySkirmish.build_scenario(7, 2, 20, 20)
	scenario["starting_credits"] = 5000
	scenario["fog_enabled"] = 0
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var p1_hub: HexCoord = _hub_coord(state, "p1")
	MilitaryConstructionSystem.spawn_building(state, "power_plant", p1_hub.q + 2, p1_hub.r, "p1", cfg)
	MilitaryConstructionSystem.spawn_building(state, "resource_processor", p1_hub.q + 1, p1_hub.r + 1, "p1", cfg)
	MilitaryConstructionSystem.spawn_building(state, "barracks", p1_hub.q, p1_hub.r + 2, "p1", cfg)
	MilitaryProductionSystem.create_unit(state, "infantry", p1_hub.q + 1, p1_hub.r, "p1", cfg)
	return state


static func _hub_coord(state: GameState, player_id: String) -> HexCoord:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == "command_hub":
			if str(building.get("player_id", "")) == player_id:
				return HexCoord.new(int(building.get("q", 0)), int(building.get("r", 0)))
	return HexCoord.new(state.map.width / 2, state.map.height / 2)
