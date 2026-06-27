class_name HybridObjective
extends RefCounted

## Authoritative hybrid MVP objective — economy-fed military + delegated victory.


static func load_config(relative_path: String) -> Dictionary:
	if relative_path == "":
		return {}
	return ConfigLoader.load_json(relative_path)


static func evaluate(state: GameState, config: Dictionary, player_id: String = "p1") -> Dictionary:
	if config.is_empty() or state == null:
		return {"complete": false, "requirements": [], "victory": false}

	var requirements: Array = []
	var all_done: bool = true

	var mil_type: String = str(config.get("require_military_building", ""))
	if mil_type != "":
		var built: bool = _has_military_building(state, player_id, mil_type)
		if not built:
			all_done = false
		requirements.append({
			"key": "military_%s" % mil_type,
			"label": "Build supplied %s" % _display_name(mil_type),
			"target": 1,
			"current": 1 if built else 0,
			"done": built,
			"detail": "" if built else "Place and supply a military construction site.",
		})

	var unit_type: String = str(config.get("require_unit_type", ""))
	var unit_target: int = int(config.get("require_unit_count", 1))
	if unit_type != "":
		var count: int = _count_units(state, player_id, unit_type)
		var done_units: bool = count >= unit_target
		if not done_units:
			all_done = false
		requirements.append({
			"key": "unit_%s" % unit_type,
			"label": "Produce %s" % _display_name(unit_type),
			"target": unit_target,
			"current": count,
			"done": done_units,
			"detail": "" if done_units else "%d / %d ready." % [count, unit_target],
		})

	var victory: bool = false
	if bool(config.get("victory_on_enemy_hub_destroyed", false)):
		var victory_state: String = str(MilitaryState.get_meta_value(state, "victory_state", MilitaryConstants.VICTORY_NONE))
		victory = victory_state == MilitaryConstants.VICTORY_WON and _player_won(state, player_id)
		if not victory:
			all_done = false
		requirements.append({
			"key": "destroy_enemy_hub",
			"label": "Destroy enemy Command Hub",
			"target": 1,
			"current": 1 if victory else 0,
			"done": victory,
			"detail": "" if victory else "Set Attack Posture on the enemy base.",
		})

	return {
		"complete": all_done,
		"victory": victory or (all_done and mil_type == "" and unit_type == ""),
		"requirements": requirements,
		"player_id": player_id,
	}


static func _has_military_building(state: GameState, player_id: String, building_type: String) -> bool:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) != building_type:
			continue
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			continue
		return true
	return false


static func _count_units(state: GameState, player_id: String, unit_type: String) -> int:
	var count: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) != unit_type:
			continue
		if str(unit.get("player_id", "")) != player_id:
			continue
		count += 1
	return count


static func _player_won(state: GameState, player_id: String) -> bool:
	for event in state.event_log.events:
		if str(event.type) == "player_victory" and str(event.payload.get("player_id", "")) == player_id:
			return true
	return false


static func _display_name(sim_type: String) -> String:
	match sim_type:
		"guard_post":
			return "Guard Post"
		"barracks":
			return "Barracks"
		"power_generator":
			return "Power Generator"
		"rifle_squad":
			return "Rifle Squad"
		"command_hub":
			return "Command Hub"
	return sim_type.replace("_", " ").capitalize()
