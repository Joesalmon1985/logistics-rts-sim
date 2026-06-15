class_name MilitaryInit
extends RefCounted

## Scenario/map bootstrap for the trial military variant.


static func initialize(state: GameState, scenario: Dictionary, _terrain_config: Dictionary) -> void:
	MilitaryState.ensure_buckets(state)
	var cfg: MilitaryConfig = MilitaryState.get_config(state)

	var players: Array = scenario.get("players", ["p1"])
	var primary_player_id: String = "p1"
	if not players.is_empty():
		primary_player_id = str(players[0])
	MilitaryState.set_meta_value(state, "primary_player_id", primary_player_id)

	var build_radius: int = int(scenario.get("build_radius", cfg.global_int("default_build_radius", 10)))
	MilitaryState.set_meta_value(state, "build_radius", build_radius)
	MilitaryState.set_meta_value(state, "fog_enabled", int(scenario.get("fog_enabled", 1)))
	MilitaryState.set_meta_value(state, "victory_state", MilitaryConstants.VICTORY_NONE)
	MilitaryState.set_meta_value(state, "victory_decided", 0)
	MilitaryState.set_meta_value(state, "pending_damage", [])
	MilitaryState.set_meta_value(state, "progress_score", -1)
	MilitaryState.set_meta_value(state, "last_progress_tick", state.tick)
	MilitaryState.set_meta_value(state, "stall_ticks", 0)

	if scenario.has("auto_acquire_idle"):
		MilitaryState.set_meta_value(state, "auto_acquire_idle", int(scenario.get("auto_acquire_idle", 1)))

	var default_credits: int = int(scenario.get("starting_credits", cfg.global_int("default_starting_credits", 2000)))
	for player_id in players:
		MilitaryState.add_player(state, {
			"player_id": str(player_id),
			"credits": default_credits,
			"power_supply": 0,
			"power_demand": 0,
			"powered": 1,
			"hub_count": 0,
			"defeated": 0,
			"tech_unlocked": [],
		})

	_apply_spawns(state, scenario, players, cfg)
	_apply_resource_fields(state, scenario, cfg)

	MilitaryState.emit_event(state, "military_initialized", {
		"player_count": players.size(),
		"build_radius": build_radius,
	})


static func _apply_spawns(state: GameState, scenario: Dictionary, players: Array, cfg: MilitaryConfig) -> void:
	var spawns: Array = scenario.get("spawns", [])
	if spawns.is_empty():
		# Default single-player spawn: a command hub at the map centre.
		var pid: String = str(players[0]) if not players.is_empty() else "p1"
		var cq: int = state.map.width / 2
		var cr: int = state.map.height / 2
		MilitaryConstructionSystem.spawn_building(state, "command_hub", cq, cr, pid, cfg)
		return
	for spawn in spawns:
		var pid2: String = str(spawn.get("player_id", "p1"))
		var hub_q: int = int(spawn.get("q", state.map.width / 2))
		var hub_r: int = int(spawn.get("r", state.map.height / 2))
		MilitaryConstructionSystem.spawn_building(state, "command_hub", hub_q, hub_r, pid2, cfg)
		for entry in spawn.get("buildings", []):
			MilitaryConstructionSystem.spawn_building(state, str(entry.get("type", "")), int(entry.get("q", 0)), int(entry.get("r", 0)), pid2, cfg)
		for entry2 in spawn.get("units", []):
			var count: int = int(entry2.get("count", 1))
			for _i in range(count):
				var hex: Vector2i = _free_hex_near(state, int(entry2.get("q", hub_q)), int(entry2.get("r", hub_r)))
				MilitaryProductionSystem.create_unit(state, str(entry2.get("type", "")), hex.x, hex.y, pid2, cfg)
		if spawn.has("credits"):
			var p: Dictionary = MilitaryState.get_player(state, pid2)
			p["credits"] = int(spawn.get("credits", 0))
		for tech_id in spawn.get("tech", []):
			var p2: Dictionary = MilitaryState.get_player(state, pid2)
			var unlocked: Array = p2.get("tech_unlocked", [])
			if str(tech_id) not in unlocked:
				unlocked.append(str(tech_id))
			p2["tech_unlocked"] = unlocked


static func _apply_resource_fields(state: GameState, scenario: Dictionary, cfg: MilitaryConfig) -> void:
	for entry in scenario.get("resource_fields", []):
		var field_type: String = str(entry.get("field_type", "mineral_field"))
		var fdef: Dictionary = cfg.get_field_def(field_type)
		var field_id: int = state.entity_ids.allocate()
		MilitaryState.add_resource_field(state, {
			"id": field_id,
			"field_type": field_type,
			"q": int(entry.get("q", 0)),
			"r": int(entry.get("r", 0)),
			"amount": int(entry.get("amount", fdef.get("start_amount", 2000))),
			"depleted": 0,
		})


static func _free_hex_near(state: GameState, q: int, r: int) -> Vector2i:
	var occupied: Dictionary = MilitaryMapRules.footprint_blocked_hexes(state)
	if MilitaryMapRules.is_passable(state, q, r) and not occupied.has(MilitaryState.coord_key(q, r)):
		return Vector2i(q, r)
	for radius in range(1, 6):
		for dq in range(-radius, radius + 1):
			for dr in range(-radius, radius + 1):
				var nq: int = q + dq
				var nr: int = r + dr
				if HexCoord.new(q, r).distance(HexCoord.new(nq, nr)) != radius:
					continue
				if MilitaryMapRules.is_passable(state, nq, nr) and not occupied.has(MilitaryState.coord_key(nq, nr)):
					return Vector2i(nq, nr)
	return Vector2i(q, r)
