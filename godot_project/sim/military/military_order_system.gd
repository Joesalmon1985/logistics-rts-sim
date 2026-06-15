class_name MilitaryOrderSystem
extends RefCounted

## Resolves per-unit order intents into movement paths and an engagement target
## (unit["engage_target_id"]) consumed by the combat system. Decides only; the
## movement system executes movement later in the tick.


static func tick(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	for unit in MilitaryState.all_units(state):
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		var unit_type: String = str(unit.get("unit_type", ""))
		var udef: Dictionary = cfg.get_unit_def(unit_type)
		if int(udef.get("can_harvest", 0)) > 0:
			continue  # collectors are driven by the resource system
		_resolve_unit(state, unit, cfg)


static func _resolve_unit(state: GameState, unit: Dictionary, cfg: MilitaryConfig) -> void:
	unit["engage_target_id"] = 0
	var order: Dictionary = unit.get("order", {"type": MilitaryConstants.ORDER_NONE})
	var otype: String = str(order.get("type", MilitaryConstants.ORDER_NONE))

	match otype:
		MilitaryConstants.ORDER_MOVE:
			_resolve_move(state, unit)
		MilitaryConstants.ORDER_ATTACK:
			_resolve_attack(state, unit)
		MilitaryConstants.ORDER_ATTACK_MOVE:
			_resolve_attack_move(state, unit, cfg)
		MilitaryConstants.ORDER_PATROL:
			_resolve_patrol(state, unit, cfg)
		MilitaryConstants.ORDER_GUARD:
			_resolve_guard(state, unit, cfg)
		_:
			_resolve_idle(state, unit, cfg)


static func _resolve_idle(state: GameState, unit: Dictionary, cfg: MilitaryConfig) -> void:
	unit["state"] = MilitaryConstants.UNIT_IDLE
	unit["path"] = []
	var auto_acquire: int = int(MilitaryState.get_meta_value(state, "auto_acquire_idle", cfg.order_int("auto_acquire_idle", 1)))
	if auto_acquire <= 0:
		return
	if str(unit.get("weapon", "")) == "":
		return
	var target: Dictionary = _nearest_hostile_in_range(state, unit, MilitaryCombatSystem.weapon_range(state, unit))
	if not target.is_empty():
		unit["engage_target_id"] = int(target.get("id", 0))


static func _resolve_move(state: GameState, unit: Dictionary) -> void:
	unit["state"] = MilitaryConstants.UNIT_MOVING
	var order: Dictionary = unit["order"]
	if int(unit.get("q", 0)) == int(order.get("q", 0)) and int(unit.get("r", 0)) == int(order.get("r", 0)):
		unit["order"] = {"type": MilitaryConstants.ORDER_NONE}
		unit["path"] = []
		unit["state"] = MilitaryConstants.UNIT_IDLE


static func _resolve_attack(state: GameState, unit: Dictionary) -> void:
	var order: Dictionary = unit["order"]
	var target_id: int = int(order.get("target_id", 0))
	var target: Dictionary = MilitaryCombatSystem.target_info(state, target_id)
	if target.is_empty() or not bool(target.get("alive", false)):
		unit["order"] = {"type": MilitaryConstants.ORDER_NONE}
		unit["path"] = []
		unit["state"] = MilitaryConstants.UNIT_IDLE
		return
	_engage_or_chase(state, unit, target)


static func _resolve_attack_move(state: GameState, unit: Dictionary, cfg: MilitaryConfig) -> void:
	unit["state"] = MilitaryConstants.UNIT_MOVING
	var order: Dictionary = unit["order"]
	var scan: int = cfg.order_int("attack_move_scan_radius", 4)
	var target: Dictionary = _nearest_hostile_in_range(state, unit, scan)
	if not target.is_empty():
		_engage_or_chase(state, unit, target)
		return
	# No hostiles: continue to destination.
	var dq: int = int(order.get("q", 0))
	var dr: int = int(order.get("r", 0))
	if int(unit.get("q", 0)) == dq and int(unit.get("r", 0)) == dr:
		unit["order"] = {"type": MilitaryConstants.ORDER_NONE}
		unit["path"] = []
		unit["state"] = MilitaryConstants.UNIT_IDLE
		return
	if unit.get("path", []).is_empty():
		unit["path"] = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), dq, dr)
		unit["path_index"] = 0


static func _resolve_patrol(state: GameState, unit: Dictionary, cfg: MilitaryConfig) -> void:
	unit["state"] = MilitaryConstants.UNIT_MOVING
	var order: Dictionary = unit["order"]
	var scan: int = cfg.order_int("patrol_reengage_radius", 4)
	var target: Dictionary = _nearest_hostile_in_range(state, unit, scan)
	if not target.is_empty():
		_engage_or_chase(state, unit, target)
		return
	var waypoints: Array = order.get("waypoints", [])
	if waypoints.size() < 2:
		return
	var idx: int = int(order.get("wp_index", 0)) % waypoints.size()
	var wp: Dictionary = waypoints[idx]
	if int(unit.get("q", 0)) == int(wp.get("q", 0)) and int(unit.get("r", 0)) == int(wp.get("r", 0)):
		idx = (idx + 1) % waypoints.size()
		order["wp_index"] = idx
		wp = waypoints[idx]
		unit["path"] = []
	if unit.get("path", []).is_empty():
		unit["path"] = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), int(wp.get("q", 0)), int(wp.get("r", 0)))
		unit["path_index"] = 0


static func _resolve_guard(state: GameState, unit: Dictionary, cfg: MilitaryConfig) -> void:
	var order: Dictionary = unit["order"]
	var guard_radius: int = cfg.order_int("guard_radius", 3)
	var anchor_q: int = int(order.get("q", int(unit.get("q", 0))))
	var anchor_r: int = int(order.get("r", int(unit.get("r", 0))))
	# Follow a guarded entity if specified.
	var guard_id: int = int(order.get("target_id", 0))
	if guard_id > 0:
		var g: Dictionary = MilitaryCombatSystem.target_info(state, guard_id)
		if not g.is_empty() and bool(g.get("alive", false)):
			anchor_q = int(g.get("q", anchor_q))
			anchor_r = int(g.get("r", anchor_r))
	var target: Dictionary = _nearest_hostile_in_range(state, unit, guard_radius)
	if not target.is_empty():
		var chase_radius: int = cfg.order_int("guard_chase_radius", 5)
		if MilitaryMapRules.hex_distance(anchor_q, anchor_r, int(target.get("q", 0)), int(target.get("r", 0))) <= chase_radius:
			_engage_or_chase(state, unit, target)
			return
	# Return toward guard anchor if displaced.
	if MilitaryMapRules.hex_distance(int(unit.get("q", 0)), int(unit.get("r", 0)), anchor_q, anchor_r) > 1:
		unit["state"] = MilitaryConstants.UNIT_MOVING
		if unit.get("path", []).is_empty():
			unit["path"] = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), anchor_q, anchor_r)
			unit["path_index"] = 0
	else:
		unit["state"] = MilitaryConstants.UNIT_IDLE
		unit["path"] = []


static func _engage_or_chase(state: GameState, unit: Dictionary, target: Dictionary) -> void:
	var rng: int = MilitaryCombatSystem.weapon_range(state, unit)
	var dist: int = MilitaryMapRules.hex_distance(int(unit.get("q", 0)), int(unit.get("r", 0)), int(target.get("q", 0)), int(target.get("r", 0)))
	if dist <= rng and str(unit.get("weapon", "")) != "":
		# In range: stop and fire (combat system handles firing).
		unit["engage_target_id"] = int(target.get("id", 0))
		unit["path"] = []
		unit["state"] = MilitaryConstants.UNIT_ATTACKING
	else:
		# Chase: path to adjacent of target.
		unit["state"] = MilitaryConstants.UNIT_MOVING
		var dest: Vector2i = _approach_hex(state, unit, target, rng)
		unit["path"] = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), dest.x, dest.y)
		unit["path_index"] = 0
		# Still allow firing if somehow in range this tick.
		if dist <= rng and str(unit.get("weapon", "")) != "":
			unit["engage_target_id"] = int(target.get("id", 0))


static func _approach_hex(state: GameState, unit: Dictionary, target: Dictionary, rng: int) -> Vector2i:
	var tq: int = int(target.get("q", 0))
	var tr: int = int(target.get("r", 0))
	if MilitaryMapRules.is_passable(state, tq, tr):
		return Vector2i(tq, tr)
	for n in HexCoord.new(tq, tr).neighbors():
		if MilitaryMapRules.is_passable(state, n.q, n.r):
			return Vector2i(n.q, n.r)
	return Vector2i(int(unit.get("q", 0)), int(unit.get("r", 0)))


static func _nearest_hostile_in_range(state: GameState, unit: Dictionary, radius: int) -> Dictionary:
	var best: Dictionary = {}
	var best_dist: int = 1 << 30
	var uq: int = int(unit.get("q", 0))
	var ur: int = int(unit.get("r", 0))
	var pid: String = str(unit.get("player_id", ""))
	for target in MilitaryCombatSystem.all_targets(state):
		if str(target.get("player_id", "")) == pid:
			continue
		if not bool(target.get("alive", false)):
			continue
		if not MilitaryCombatSystem.can_target(state, unit, target):
			continue
		var d: int = MilitaryMapRules.hex_distance(uq, ur, int(target.get("q", 0)), int(target.get("r", 0)))
		if d <= radius and d < best_dist:
			best_dist = d
			best = target
	return best


# --- Order issuing (called from commands) -----------------------------------

static func issue_move(state: GameState, unit: Dictionary, q: int, r: int) -> void:
	unit["order"] = {"type": MilitaryConstants.ORDER_MOVE, "q": q, "r": r}
	unit["path"] = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), q, r)
	unit["path_index"] = 0
	unit["move_progress"] = 0
	unit["state"] = MilitaryConstants.UNIT_MOVING


static func issue_attack(state: GameState, unit: Dictionary, target_id: int) -> void:
	unit["order"] = {"type": MilitaryConstants.ORDER_ATTACK, "target_id": target_id}
	unit["path"] = []
	unit["path_index"] = 0
	unit["state"] = MilitaryConstants.UNIT_ATTACKING


static func issue_attack_move(state: GameState, unit: Dictionary, q: int, r: int) -> void:
	unit["order"] = {"type": MilitaryConstants.ORDER_ATTACK_MOVE, "q": q, "r": r}
	unit["path"] = MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), q, r)
	unit["path_index"] = 0
	unit["move_progress"] = 0
	unit["state"] = MilitaryConstants.UNIT_MOVING


static func issue_patrol(state: GameState, unit: Dictionary, waypoints: Array) -> void:
	unit["order"] = {"type": MilitaryConstants.ORDER_PATROL, "waypoints": waypoints, "wp_index": 0}
	unit["path"] = []
	unit["path_index"] = 0
	unit["state"] = MilitaryConstants.UNIT_MOVING


static func issue_guard(state: GameState, unit: Dictionary, target_id: int, q: int, r: int) -> void:
	unit["order"] = {"type": MilitaryConstants.ORDER_GUARD, "target_id": target_id, "q": q, "r": r}
	unit["path"] = []
	unit["path_index"] = 0
	unit["state"] = MilitaryConstants.UNIT_IDLE


static func issue_stop(state: GameState, unit: Dictionary) -> void:
	unit["order"] = {"type": MilitaryConstants.ORDER_NONE}
	unit["path"] = []
	unit["path_index"] = 0
	unit["engage_target_id"] = 0
	unit["state"] = MilitaryConstants.UNIT_IDLE
