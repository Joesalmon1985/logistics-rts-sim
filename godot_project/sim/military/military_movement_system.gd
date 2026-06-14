class_name MilitaryMovementSystem
extends RefCounted

## Executes integer movement along unit paths set by the order/resource systems.
## Accumulates the unit's speed and steps through hex neighbours when enough
## progress is banked to enter the next hex (cost from terrain movement_cost).


static func tick(state: GameState) -> void:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		_move_unit(state, unit)


static func _move_unit(state: GameState, unit: Dictionary) -> void:
	var path: Array = unit.get("path", [])
	var idx: int = int(unit.get("path_index", 0))
	if path.is_empty() or idx >= path.size():
		return

	var speed: int = maxi(1, int(unit.get("speed", 50)))
	var progress: int = int(unit.get("move_progress", 0)) + speed
	var start_q: int = int(unit.get("q", 0))
	var start_r: int = int(unit.get("r", 0))
	var moved: bool = false

	while idx < path.size():
		var step: Dictionary = path[idx]
		var nq: int = int(step.get("q", 0))
		var nr: int = int(step.get("r", 0))
		if not MilitaryMapRules.is_passable(state, nq, nr):
			# Path became blocked — try to recompute toward the order destination.
			if not _recompute_to_destination(state, unit):
				MilitaryState.emit_event(state, "path_blocked", {
					"unit_id": int(unit.get("id", 0)),
					"q": int(unit.get("q", 0)),
					"r": int(unit.get("r", 0)),
				})
				unit["path"] = []
				unit["path_index"] = 0
			unit["move_progress"] = 0
			return
		var cost: int = MilitaryMapRules.movement_cost(state, nq, nr)
		if progress < cost:
			break
		progress -= cost
		unit["q"] = nq
		unit["r"] = nr
		idx += 1
		moved = true

	unit["move_progress"] = progress
	unit["path_index"] = idx
	if idx >= path.size():
		unit["path"] = []
		unit["path_index"] = 0
	if moved:
		MilitaryState.emit_event(state, "unit_moved", {
			"unit_id": int(unit.get("id", 0)),
			"from_q": start_q,
			"from_r": start_r,
			"to_q": int(unit.get("q", 0)),
			"to_r": int(unit.get("r", 0)),
		})


static func _recompute_to_destination(state: GameState, unit: Dictionary) -> bool:
	var order: Dictionary = unit.get("order", {})
	var otype: String = str(order.get("type", MilitaryConstants.ORDER_NONE))
	var dest_q: int = -1
	var dest_r: int = -1
	if otype in [MilitaryConstants.ORDER_MOVE, MilitaryConstants.ORDER_ATTACK_MOVE]:
		dest_q = int(order.get("q", -1))
		dest_r = int(order.get("r", -1))
	if dest_q < 0:
		return false
	var path: Array = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), dest_q, dest_r)
	if path.is_empty():
		return false
	unit["path"] = path
	unit["path_index"] = 0
	return true
