class_name MilitaryCombatSystem
extends RefCounted

## Target acquisition, weapon cooldowns, firing (instant or projectile), and
## deterministic damage resolution. Firing runs in the `combat` step; damage is
## applied in the `damage` step against the pre-damage snapshot so simultaneous
## shots resolve once. Instant-hit weapons queue a damage entry this tick;
## projectile weapons apply damage only when travel completes.


# --- Shared helpers (used by the order system too) ---------------------------

static func weapon_range(state: GameState, unit: Dictionary) -> int:
	var weapon_id: String = str(unit.get("weapon", ""))
	if weapon_id == "":
		return 0
	return int(MilitaryState.get_config(state).get_weapon_def(weapon_id).get("range", 0))


static func can_target(state: GameState, attacker: Dictionary, target: Dictionary) -> bool:
	var weapon_id: String = str(attacker.get("weapon", ""))
	if weapon_id == "":
		return false
	var wdef: Dictionary = MilitaryState.get_config(state).get_weapon_def(weapon_id)
	return str(target.get("category", "")) in wdef.get("valid_targets", [])


static func target_info(state: GameState, target_id: int) -> Dictionary:
	var unit: Dictionary = MilitaryState.get_unit(state, target_id)
	if not unit.is_empty():
		return {
			"id": target_id,
			"kind": "unit",
			"q": int(unit.get("q", 0)),
			"r": int(unit.get("r", 0)),
			"player_id": str(unit.get("player_id", "")),
			"category": str(unit.get("category", "infantry")),
			"armour_type": str(unit.get("armour_type", "infantry")),
			"alive": str(unit.get("state", "")) != MilitaryConstants.UNIT_DEAD,
		}
	var building: Dictionary = MilitaryState.get_building(state, target_id)
	if not building.is_empty():
		return {
			"id": target_id,
			"kind": "building",
			"q": int(building.get("anchor_q", 0)),
			"r": int(building.get("anchor_r", 0)),
			"player_id": str(building.get("player_id", "")),
			"category": "structure",
			"armour_type": "structure",
			"alive": str(building.get("state", "")) != MilitaryConstants.BLD_DESTROYED,
		}
	return {}


static func all_targets(state: GameState) -> Array:
	var result: Array = []
	for unit in MilitaryState.all_units(state):
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		result.append(target_info(state, int(unit.get("id", 0))))
	for building in MilitaryState.all_buildings(state):
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			continue
		result.append(target_info(state, int(building.get("id", 0))))
	return result


# --- Combat step (fire + projectiles) ---------------------------------------

static func tick_combat(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	_advance_projectiles(state, cfg)

	# Units fire at their order-assigned engagement target.
	for unit in MilitaryState.all_units(state):
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		if int(unit.get("weapon_cooldown", 0)) > 0:
			unit["weapon_cooldown"] = int(unit.get("weapon_cooldown", 0)) - 1
		var target_id: int = int(unit.get("engage_target_id", 0))
		if target_id <= 0 or str(unit.get("weapon", "")) == "":
			continue
		_try_fire(state, unit, int(unit.get("q", 0)), int(unit.get("r", 0)), target_id, cfg, "unit")

	# Turrets auto-acquire and fire (MIL-COMBAT-007).
	for building in MilitaryState.all_buildings(state):
		if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		if str(building.get("weapon", "")) == "":
			continue
		if int(building.get("weapon_cooldown", 0)) > 0:
			building["weapon_cooldown"] = int(building.get("weapon_cooldown", 0)) - 1
		if not MilitaryPowerSystem.building_powered(state, building):
			continue
		var target: Dictionary = _nearest_target_for(state, building, int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
		if target.is_empty():
			continue
		_try_fire(state, building, int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)), int(target.get("id", 0)), cfg, "building")


static func _try_fire(state: GameState, attacker: Dictionary, aq: int, ar: int, target_id: int, cfg: MilitaryConfig, kind: String) -> void:
	if int(attacker.get("weapon_cooldown", 0)) > 0:
		return
	var target: Dictionary = target_info(state, target_id)
	if target.is_empty() or not bool(target.get("alive", false)):
		return
	if not can_target(state, attacker, target):
		return
	var wdef: Dictionary = cfg.get_weapon_def(str(attacker.get("weapon", "")))
	var dist: int = MilitaryMapRules.hex_distance(aq, ar, int(target.get("q", 0)), int(target.get("r", 0)))
	if dist > int(wdef.get("range", 0)) or dist < int(wdef.get("min_range", 0)):
		return

	attacker["weapon_cooldown"] = int(wdef.get("cooldown_ticks", 1))
	MilitaryState.emit_event(state, "unit_attacked", {
		"attacker_id": int(attacker.get("id", 0)),
		"attacker_kind": kind,
		"target_id": target_id,
		"weapon": str(attacker.get("weapon", "")),
	})

	if str(wdef.get("mode", MilitaryConstants.WEAPON_INSTANT)) == MilitaryConstants.WEAPON_PROJECTILE and int(wdef.get("travel_ticks", 0)) > 0:
		_spawn_projectile(state, attacker, aq, ar, target, wdef)
	else:
		_queue_damage(state, int(attacker.get("id", 0)), target_id, int(wdef.get("damage", 0)), str(wdef.get("damage_type", "")))


static func _spawn_projectile(state: GameState, attacker: Dictionary, aq: int, ar: int, target: Dictionary, wdef: Dictionary) -> void:
	var pid: int = state.entity_ids.allocate()
	MilitaryState.add_projectile(state, {
		"id": pid,
		"player_id": str(attacker.get("player_id", "")),
		"source_id": int(attacker.get("id", 0)),
		"target_id": int(target.get("id", 0)),
		"q": aq,
		"r": ar,
		"dest_q": int(target.get("q", 0)),
		"dest_r": int(target.get("r", 0)),
		"travel_progress": 0,
		"travel_ticks": int(wdef.get("travel_ticks", 1)),
		"damage": int(wdef.get("damage", 0)),
		"damage_type": str(wdef.get("damage_type", "")),
	})


static func _advance_projectiles(state: GameState, _cfg: MilitaryConfig) -> void:
	var to_remove: Array = []
	for projectile in MilitaryState.all_projectiles(state):
		projectile["travel_progress"] = int(projectile.get("travel_progress", 0)) + 1
		if int(projectile.get("travel_progress", 0)) >= int(projectile.get("travel_ticks", 1)):
			_queue_damage(state, int(projectile.get("source_id", 0)), int(projectile.get("target_id", 0)), int(projectile.get("damage", 0)), str(projectile.get("damage_type", "")))
			MilitaryState.emit_event(state, "projectile_hit", {
				"projectile_id": int(projectile.get("id", 0)),
				"target_id": int(projectile.get("target_id", 0)),
			})
			to_remove.append(int(projectile.get("id", 0)))
	for pid in to_remove:
		MilitaryState.remove_projectile(state, pid)


static func _queue_damage(state: GameState, source_id: int, target_id: int, base_damage: int, damage_type: String) -> void:
	var pending: Array = MilitaryState.get_meta_value(state, "pending_damage", [])
	pending.append({
		"source_id": source_id,
		"target_id": target_id,
		"base_damage": base_damage,
		"damage_type": damage_type,
	})
	MilitaryState.set_meta_value(state, "pending_damage", pending)


# --- Damage step ------------------------------------------------------------

static func tick_damage(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var pending: Array = MilitaryState.get_meta_value(state, "pending_damage", [])
	for entry in pending:
		_apply_damage(state, entry, cfg)
	MilitaryState.set_meta_value(state, "pending_damage", [])


static func _apply_damage(state: GameState, entry: Dictionary, cfg: MilitaryConfig) -> void:
	var target_id: int = int(entry.get("target_id", 0))
	var unit: Dictionary = MilitaryState.get_unit(state, target_id)
	if not unit.is_empty():
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			return
		var dmg_u: int = _modified_damage(cfg, int(entry.get("base_damage", 0)), str(entry.get("damage_type", "")), str(unit.get("armour_type", "infantry")))
		unit["hp"] = int(unit.get("hp", 0)) - dmg_u
		MilitaryState.append_combat_log(state, {"tick": state.tick, "target_id": target_id, "damage": dmg_u, "kind": "unit"})
		MilitaryState.emit_event(state, "unit_damaged", {"unit_id": target_id, "damage": dmg_u, "hp": int(unit.get("hp", 0))})
		if int(unit.get("hp", 0)) <= 0:
			_destroy_unit(state, unit)
		return
	var building: Dictionary = MilitaryState.get_building(state, target_id)
	if not building.is_empty():
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			return
		var dmg_b: int = _modified_damage(cfg, int(entry.get("base_damage", 0)), str(entry.get("damage_type", "")), "structure")
		building["hp"] = int(building.get("hp", 0)) - dmg_b
		MilitaryState.append_combat_log(state, {"tick": state.tick, "target_id": target_id, "damage": dmg_b, "kind": "building"})
		MilitaryState.emit_event(state, "building_damaged", {"building_id": target_id, "damage": dmg_b, "hp": int(building.get("hp", 0))})
		if int(building.get("hp", 0)) <= 0:
			_destroy_building(state, building)


static func _modified_damage(cfg: MilitaryConfig, base_damage: int, damage_type: String, armour_type: String) -> int:
	var pct: int = cfg.damage_modifier(damage_type, armour_type)
	var dmg: int = (base_damage * pct) / 100
	return maxi(0, dmg)


static func _destroy_unit(state: GameState, unit: Dictionary) -> void:
	unit["state"] = MilitaryConstants.UNIT_DEAD
	unit["hp"] = 0
	MilitaryState.emit_event(state, "unit_destroyed", {
		"unit_id": int(unit.get("id", 0)),
		"unit_type": str(unit.get("unit_type", "")),
		"player_id": str(unit.get("player_id", "")),
	})
	MilitaryState.remove_unit(state, int(unit.get("id", 0)))


static func _destroy_building(state: GameState, building: Dictionary) -> void:
	building["state"] = MilitaryConstants.BLD_DESTROYED
	building["hp"] = 0
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
	if int(bdef.get("is_command_hub", 0)) > 0:
		var player: Dictionary = MilitaryState.get_player(state, str(building.get("player_id", "")))
		player["hub_count"] = maxi(0, int(player.get("hub_count", 0)) - 1)
	MilitaryState.emit_event(state, "building_destroyed", {
		"building_id": int(building.get("id", 0)),
		"building_type": str(building.get("building_type", "")),
		"player_id": str(building.get("player_id", "")),
	})


static func _nearest_target_for(state: GameState, attacker: Dictionary, aq: int, ar: int) -> Dictionary:
	var wdef: Dictionary = MilitaryState.get_config(state).get_weapon_def(str(attacker.get("weapon", "")))
	var rng: int = int(wdef.get("range", 0))
	var min_rng: int = int(wdef.get("min_range", 0))
	var pid: String = str(attacker.get("player_id", ""))
	var best: Dictionary = {}
	var best_dist: int = 1 << 30
	for target in all_targets(state):
		if str(target.get("player_id", "")) == pid:
			continue
		if not can_target(state, attacker, target):
			continue
		var d: int = MilitaryMapRules.hex_distance(aq, ar, int(target.get("q", 0)), int(target.get("r", 0)))
		if d <= rng and d >= min_rng and d < best_dist:
			best_dist = d
			best = target
	return best
