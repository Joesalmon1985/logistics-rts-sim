class_name HybridOrderSystem
extends RefCounted

## Executes delegated military orders via sim AI (HYB-CTRL-006, HYB-MIL-003).


static func tick(state: GameState) -> void:
	if not HybridConfig.is_hybrid(state):
		return
	for order in HybridState.all_delegated_orders(state):
		if int(order.get("active", 1)) <= 0:
			continue
		match str(order.get("type", "")):
			HybridConstants.ORDER_ATTACK:
				_execute_attack(state, order)
			HybridConstants.ORDER_PATROL:
				_execute_patrol(state, order)
			HybridConstants.ORDER_GUARD:
				_execute_guard(state, order)


static func _execute_attack(state: GameState, order: Dictionary) -> void:
	var player_id: String = str(order.get("player_id", ""))
	var target_q: int = int(order.get("target_q", 0))
	var target_r: int = int(order.get("target_r", 0))
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) != player_id:
			continue
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		if str(unit.get("weapon", "")) == "":
			continue
		MilitaryOrderSystem.issue_attack_move(state, unit, target_q, target_r)


static func _execute_patrol(state: GameState, order: Dictionary) -> void:
	var player_id: String = str(order.get("player_id", ""))
	var waypoints: Array = order.get("waypoints", [])
	if waypoints.is_empty():
		return
	var units: Array = _combat_units_for_player(state, player_id, int(order.get("building_id", 0)))
	for unit in units:
		MilitaryOrderSystem.issue_patrol(state, unit, waypoints)


static func _execute_guard(state: GameState, order: Dictionary) -> void:
	var player_id: String = str(order.get("player_id", ""))
	var guard_q: int = int(order.get("guard_q", 0))
	var guard_r: int = int(order.get("guard_r", 0))
	var units: Array = _combat_units_for_player(state, player_id, int(order.get("building_id", 0)))
	for unit in units:
		MilitaryOrderSystem.issue_guard(state, unit, int(order.get("building_id", 0)), guard_q, guard_r)


static func _combat_units_for_player(state: GameState, player_id: String, building_id: int) -> Array:
	var result: Array = []
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) != player_id:
			continue
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		if str(unit.get("weapon", "")) == "":
			continue
		if building_id > 0:
			var building: Dictionary = MilitaryState.get_building(state, building_id)
			if not building.is_empty():
				var dist: int = MilitaryMapRules.hex_distance(
					int(unit.get("q", 0)), int(unit.get("r", 0)),
					int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)),
				)
				if dist > 6:
					continue
		result.append(unit)
	return result
