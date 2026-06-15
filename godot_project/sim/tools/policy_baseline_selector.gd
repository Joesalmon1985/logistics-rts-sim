class_name PolicyBaselineSelector
extends RefCounted

## Selects a legal command for RUN_06b evaluation baselines.


static func select_command(
	policy_kind: String,
	state: GameState,
	actor_id: String,
	scenario: Dictionary,
	legal_commands: Array,
	rng: RandomNumberGenerator,
	script_index: int,
) -> Dictionary:
	match policy_kind:
		"pass_only":
			return _select_pass(legal_commands)
		"random_legal":
			return _select_random(legal_commands, rng)
		"scripted_good":
			return _select_scripted(state, actor_id, scenario, legal_commands, script_index)
		"heuristic":
			return {}
		_:
			return _select_pass(legal_commands)


static func _select_pass(legal_commands: Array) -> Dictionary:
	for entry in legal_commands:
		if str(entry.get("type", "")) == "NoopCommand":
			return entry
	if not legal_commands.is_empty():
		return legal_commands[0]
	return {}


static func _select_random(legal_commands: Array, rng: RandomNumberGenerator) -> Dictionary:
	if legal_commands.is_empty():
		return {}
	return legal_commands[rng.randi_range(0, legal_commands.size() - 1)]


static func _select_scripted(
	state: GameState,
	actor_id: String,
	scenario: Dictionary,
	legal_commands: Array,
	script_index: int,
) -> Dictionary:
	var script: Array = scenario.get("eval_script", [])
	if script_index < script.size():
		var step: Dictionary = script[script_index]
		var action: String = str(step.get("action", ""))
		match action:
			"set_worker_policy":
				for entry in legal_commands:
					if str(entry.get("type", "")) != "SetWorkerPolicyCommand":
						continue
					if str(entry.get("payload", {}).get("policy", "")) == str(step.get("policy", "auto")):
						return entry
			"place_building":
				var btype: String = str(step.get("building_type", ""))
				var q: int = int(step.get("q", -1))
				var r: int = int(step.get("r", -1))
				for entry in legal_commands:
					if str(entry.get("type", "")) != "PlaceBuildingPlanCommand":
						continue
					var payload: Dictionary = entry.get("payload", {})
					if str(payload.get("building_type", "")) == btype:
						if q >= 0 and int(payload.get("q", -99)) != q:
							continue
						if r >= 0 and int(payload.get("r", -99)) != r:
							continue
						return entry
			"attack_enemy_hub":
				var hub_id: int = _enemy_hub_id(state, actor_id)
				if hub_id > 0:
					for entry in legal_commands:
						if str(entry.get("type", "")) != "AttackUnitCommand":
							continue
						if int(entry.get("payload", {}).get("target_id", 0)) == hub_id:
							return entry
			"attack_nearest_enemy":
				for entry in legal_commands:
					if str(entry.get("type", "")) == "AttackUnitCommand":
						return entry
					if str(entry.get("type", "")) == "MoveUnitCommand":
						return entry
	return _select_pass(legal_commands)


static func _enemy_hub_id(state: GameState, actor_id: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) != "command_hub":
			continue
		if str(building.get("player_id", "")) != actor_id:
			return int(building.get("id", 0))
	return 0


static func should_advance_script(policy_kind: String, chosen: Dictionary, scenario: Dictionary, script_index: int) -> bool:
	if policy_kind != "scripted_good":
		return false
	var script: Array = scenario.get("eval_script", [])
	if script_index >= script.size():
		return false
	var step: Dictionary = script[script_index]
	var action: String = str(step.get("action", ""))
	if action == "set_worker_policy" and str(chosen.get("type", "")) == "SetWorkerPolicyCommand":
		return true
	if action == "place_building" and str(chosen.get("type", "")) == "PlaceBuildingPlanCommand":
		return true
	if action in ["attack_enemy_hub", "attack_nearest_enemy"] and str(chosen.get("type", "")) in ["AttackUnitCommand", "MoveUnitCommand"]:
		return false
	return false
