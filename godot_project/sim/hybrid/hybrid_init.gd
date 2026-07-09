class_name HybridInit
extends RefCounted

## Compose economy + military on a shared hex map (HYB-ECON-001).


static func initialize(state: GameState, scenario: Dictionary, terrain_config: Dictionary) -> void:
	HybridState.ensure_buckets(state)
	var hcfg: HybridConfig = HybridState.get_config(state)

	var econ_cfg: EconomyConfig = EconomyConfig.load_all()
	hcfg.apply_economy_overrides(econ_cfg)
	HybridState.set_merged_economy_config(econ_cfg)

	EconomyInit.initialize(state, scenario, terrain_config)
	_initialize_military(state, scenario)

	for player_id in scenario.get("players", ["p1"]):
		HybridConstructionSystem.register_hq_as_command_hub(state, str(player_id))

	_apply_military_spawns(state, scenario)
	_zero_credits(state, scenario)

	HybridState.emit_event(state, "hybrid_game_created", {
		"player_count": scenario.get("players", ["p1"]).size(),
		"seed": state.seed,
	})

	if GodStrategyMatchEnd.is_god_strategy_scenario(scenario):
		EconomyState.set_meta_value(state, "god_strategy_active", 1)
		GodStrategyState.init_match(state, scenario)
		WorkerNeedsSystem.init_match(state, scenario)
		WorkerPolicySystem.init_match(state, scenario)
		WorkerSelectionSystem.init_match(state, scenario)
		PollutionSystem.init_match(state, scenario)


static func _initialize_military(state: GameState, scenario: Dictionary) -> void:
	MilitaryState.ensure_buckets(state)
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var players: Array = scenario.get("players", ["p1"])
	var primary_player_id: String = str(players[0]) if not players.is_empty() else "p1"
	MilitaryState.set_meta_value(state, "primary_player_id", primary_player_id)
	MilitaryState.set_meta_value(state, "build_radius", int(scenario.get("build_radius", cfg.global_int("default_build_radius", 10))))
	MilitaryState.set_meta_value(state, "fog_enabled", int(scenario.get("fog_enabled", 1)))
	MilitaryState.set_meta_value(state, "victory_state", MilitaryConstants.VICTORY_NONE)
	MilitaryState.set_meta_value(state, "victory_decided", 0)
	MilitaryState.set_meta_value(state, "pending_damage", [])
	MilitaryState.set_meta_value(state, "progress_score", -1)
	MilitaryState.set_meta_value(state, "last_progress_tick", state.tick)
	MilitaryState.set_meta_value(state, "stall_ticks", 0)
	if scenario.has("auto_acquire_idle"):
		MilitaryState.set_meta_value(state, "auto_acquire_idle", int(scenario.get("auto_acquire_idle", 1)))

	for player_id in players:
		MilitaryState.add_player(state, {
			"player_id": str(player_id),
			"credits": 0,
			"power_supply": 0,
			"power_demand": 0,
			"powered": 1,
			"hub_count": 0,
			"defeated": 0,
			"tech_unlocked": [],
		})

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

	MilitaryState.emit_event(state, "military_initialized", {
		"player_count": players.size(),
		"variant": "hybrid",
	})


static func _apply_military_spawns(state: GameState, scenario: Dictionary) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	for spawn in scenario.get("spawns", []):
		var pid: String = str(spawn.get("player_id", "p1"))
		var hub_q: int = int(spawn.get("q", state.map.width / 2))
		var hub_r: int = int(spawn.get("r", state.map.height / 2))
		for entry in spawn.get("buildings", []):
			MilitaryConstructionSystem.spawn_building(
				state,
				str(entry.get("type", "")),
				int(entry.get("q", hub_q)),
				int(entry.get("r", hub_r)),
				pid,
				cfg,
			)
		for entry2 in spawn.get("units", []):
			var count: int = int(entry2.get("count", 1))
			for _i in range(count):
				var hex: Vector2i = _free_hex_near(
					state,
					int(entry2.get("q", hub_q)),
					int(entry2.get("r", hub_r)),
				)
				MilitaryProductionSystem.create_unit(state, str(entry2.get("type", "")), hex.x, hex.y, pid, cfg)
		for tech_id in spawn.get("tech", []):
			var p2: Dictionary = MilitaryState.get_player(state, pid)
			var unlocked: Array = p2.get("tech_unlocked", [])
			if str(tech_id) not in unlocked:
				unlocked.append(str(tech_id))
			p2["tech_unlocked"] = unlocked


static func _zero_credits(state: GameState, scenario: Dictionary) -> void:
	for player in MilitaryState.all_players(state):
		player["credits"] = 0


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
