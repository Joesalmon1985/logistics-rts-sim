class_name MilitaryPowerSystem
extends RefCounted

## Recomputes per-player power supply/demand and sets per-building powered flags.
## Runs first in the tick so all gated systems see current power (MIL-POWER-003).


static func tick(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	for player in MilitaryState.all_players(state):
		_recompute_player(state, str(player.get("player_id", "")), cfg)


static func _recompute_player(state: GameState, player_id: String, cfg: MilitaryConfig) -> void:
	var supply: int = 0
	var demand: int = 0
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		if int(building.get("enabled", 1)) <= 0:
			continue
		var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
		supply += int(bdef.get("power_supply", 0))
		demand += int(bdef.get("power_demand", 0))

	var player: Dictionary = MilitaryState.get_player(state, player_id)
	var prev_powered: int = int(player.get("powered", 1))
	player["power_supply"] = supply
	player["power_demand"] = demand
	var powered: int = 1 if supply >= demand else 0
	player["powered"] = powered

	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		var bdef2: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
		var consumes: bool = int(bdef2.get("power_demand", 0)) > 0
		var enabled: bool = int(building.get("enabled", 1)) > 0
		var active: bool = str(building.get("state", "")) == MilitaryConstants.BLD_ACTIVE
		var b_powered: int = 1
		if consumes and enabled and active:
			b_powered = powered
		elif not enabled or not active:
			b_powered = 0
		building["powered"] = b_powered

	if powered != prev_powered:
		MilitaryState.emit_event(state, "power_state_changed", {
			"player_id": player_id,
			"powered": powered,
			"supply": supply,
			"demand": demand,
		})


static func building_powered(state: GameState, building: Dictionary) -> bool:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
	if int(bdef.get("power_demand", 0)) <= 0:
		return true
	return int(building.get("powered", 1)) > 0
