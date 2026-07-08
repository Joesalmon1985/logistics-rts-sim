class_name GodStrategyTestHelper
extends RefCounted

const SCENARIO_PATH: String = "scenarios/god_strategy/god_strategy_4p_mvp_a.json"
const REGRESSION_SCENARIO_PATH: String = "scenarios/god_strategy/god_strategy_domination_regression.json"


static func load_scenario() -> Dictionary:
	return ConfigLoader.load_scenario(SCENARIO_PATH)


static func load_regression_scenario() -> Dictionary:
	return ConfigLoader.load_scenario(REGRESSION_SCENARIO_PATH)


static func load_micro_scenario(name: String) -> Dictionary:
	return ConfigLoader.load_scenario("scenarios/god_strategy/micro/%s" % name)


static func prep_all_spawns(state: GameState, scenario: Dictionary) -> void:
	var w: int = state.map.width - 1
	var h: int = state.map.height - 1
	var center_q: int = state.map.width / 2
	var center_r: int = state.map.height / 2
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var hq_q: int = int(spawn.get("q", 0))
		var hq_r: int = int(spawn.get("r", 0))
		var pid: String = str(spawn.get("player_id", ""))
		HybridMapSetup.prepare_playable_map(state, 0, 0, w, h, hq_q, hq_r, center_q, center_r, pid)
	if _god_strategy_scenario(scenario):
		_stabilize_god_strategy_ownership(state, scenario)
		_ensure_military_build_pads(state, scenario)
		_prep_chain_road_networks(state, scenario)


static func _god_strategy_scenario(scenario: Dictionary) -> bool:
	return bool(scenario.get("god_strategy_mvp_a", false)) or bool(scenario.get("god_strategy_regression", false))


static func _stabilize_god_strategy_ownership(state: GameState, scenario: Dictionary) -> void:
	var home_radius: int = mini(int(EconomyState.get_meta_value(state, "build_radius", 8)), 10)
	var best_owner: Dictionary = {}
	var best_dist: Dictionary = {}
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		var hq := HexCoord.new(int(spawn.get("q", 0)), int(spawn.get("r", 0)))
		for dq in range(-home_radius, home_radius + 1):
			for dr in range(-home_radius, home_radius + 1):
				if abs(dq + dr) > home_radius:
					continue
				var coord := HexCoord.new(hq.q + dq, hq.r + dr)
				if not state.map.in_bounds(coord):
					continue
				var dist: int = coord.distance(hq)
				var key: String = coord.key()
				if not best_dist.has(key) or dist < int(best_dist[key]):
					best_dist[key] = dist
					best_owner[key] = pid
	for key in best_owner.keys():
		var parts: PackedStringArray = key.split(",")
		if parts.size() != 2:
			continue
		var coord := HexCoord.new(int(parts[0]), int(parts[1]))
		var tile: HexTile = state.map.get_tile(coord)
		if tile != null:
			tile.owner_player_id = str(best_owner[key])


static func _ensure_military_build_pads(state: GameState, scenario: Dictionary) -> void:
	var center_q: int = state.map.width / 2
	var center_r: int = state.map.height / 2
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		var hq_q: int = int(spawn.get("q", 0))
		var hq_r: int = int(spawn.get("r", 0))
		var dir_q: int = clampi(center_q - hq_q, -1, 1)
		var dir_r: int = clampi(center_r - hq_r, -1, 1)
		if dir_q == 0 and dir_r == 0:
			dir_q = 1
		var perp_q: int = -dir_r
		var perp_r: int = dir_q
		var anchor_q: int = hq_q + dir_q * 6 + perp_q * 2
		var anchor_r: int = hq_r + dir_r * 6 + perp_r * 2
		for dq in range(0, 2):
			for dr in range(0, 2):
				var q: int = anchor_q + dq
				var r: int = anchor_r + dr
				var coord := HexCoord.new(q, r)
				if not state.map.in_bounds(coord):
					continue
				var tile := HexTile.new("grass", 0, 1, 1)
				tile.owner_player_id = pid
				state.map.set_tile(coord, tile)
		_place_road_path(state, pid, hq_q, hq_r, anchor_q, anchor_r)


static func _prep_chain_road_networks(state: GameState, scenario: Dictionary) -> void:
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		if pid == "":
			continue
		var hq: Vector2i = EconomyState.get_hq_position(state, pid)
		var slots: Dictionary = spawn.get("build_slots", {})
		if not slots.is_empty():
			for btype in GodStrategyMapViability.MVP_BUILDINGS:
				if not slots.has(btype):
					continue
				var slot: Dictionary = slots[btype]
				_place_road_path(
					state, pid, hq.x, hq.y, int(slot.get("q", -1)), int(slot.get("r", -1)),
				)
			continue
		for btype in GodStrategyMapViability.MVP_BUILDINGS:
			var site: Vector2i = _find_owned_chain_site(state, pid, str(btype))
			if site.x < 0:
				continue
			_place_road_path(state, pid, hq.x, hq.y, site.x, site.y)


static func _find_owned_chain_site(state: GameState, player_id: String, building_type: String) -> Vector2i:
	for site in GodStrategyMapViability.find_all_legal_hexes(state, player_id, building_type, 32, true):
		return Vector2i(int(site.get("q", -1)), int(site.get("r", -1)))
	return Vector2i(-1, -1)


static func _place_road_path(
	state: GameState,
	player_id: String,
	from_q: int,
	from_r: int,
	to_q: int,
	to_r: int,
) -> void:
	var segments: Array = GodStrategyRoadPlanner.plan_road_segments(state, player_id, from_q, from_r, to_q, to_r)
	if segments.is_empty():
		_place_axis_fallback_roads(state, player_id, from_q, from_r, to_q, to_r)
		return
	for segment in segments:
		for coord in [
			HexCoord.new(int(segment.get("from_q", -1)), int(segment.get("from_r", -1))),
			HexCoord.new(int(segment.get("to_q", -1)), int(segment.get("to_r", -1))),
		]:
			_claim_tile_for_player(state, coord, player_id)
		EconomyRoadSystem.place_road(
			state,
			HexCoord.new(int(segment.get("from_q", -1)), int(segment.get("from_r", -1))),
			HexCoord.new(int(segment.get("to_q", -1)), int(segment.get("to_r", -1))),
			player_id,
		)


static func _claim_tile_for_player(state: GameState, coord: HexCoord, player_id: String, force: bool = true) -> void:
	if not state.map.in_bounds(coord):
		return
	var tile: HexTile = state.map.get_tile(coord)
	if tile == null:
		return
	if not force and tile.owner_player_id != "" and tile.owner_player_id != player_id:
		return
	tile.owner_player_id = player_id
	state.map.set_tile(coord, tile)


static func _place_axis_fallback_roads(
	state: GameState,
	player_id: String,
	from_q: int,
	from_r: int,
	to_q: int,
	to_r: int,
) -> void:
	var current := GodStrategyRoadPlanner._resolve_road_start(state, from_q, from_r, to_q, to_r)
	var hq := HexCoord.new(from_q, from_r)
	if current.q != hq.q or current.r != hq.r:
		_claim_tile_for_player(state, hq, player_id)
		_claim_tile_for_player(state, current, player_id)
		EconomyRoadSystem.place_road(state, hq, current, player_id)
	var goal := HexCoord.new(to_q, to_r)
	var guard: int = 0
	while current.q != goal.q and guard < 64:
		guard += 1
		var step: int = 1 if goal.q > current.q else -1
		var next := HexCoord.new(current.q + step, current.r)
		_claim_tile_for_player(state, current, player_id)
		_claim_tile_for_player(state, next, player_id)
		EconomyRoadSystem.place_road(state, current, next, player_id)
		current = next
	while current.r != goal.r and guard < 128:
		guard += 1
		var step_r: int = 1 if goal.r > current.r else -1
		var next_r := HexCoord.new(current.q, current.r + step_r)
		_claim_tile_for_player(state, current, player_id)
		_claim_tile_for_player(state, next_r, player_id)
		EconomyRoadSystem.place_road(state, current, next_r, player_id)
		current = next_r


static func prep_domination_regression(state: GameState, scenario: Dictionary) -> void:
	prep_all_spawns(state, scenario)
	EconomyState.set_meta_value(state, "god_strategy_player_wide_wares", true)
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		var slots: Dictionary = spawn.get("build_slots", {})
		if not slots.is_empty():
			EconomyState.set_meta_value(state, "god_strategy_build_slots_%s" % pid, slots)
		_apply_spawn_starting_roads(state, spawn, pid)
		# Units/buildings from scenario spawns are applied by hybrid_init during create_game.
		if spawn.has("hub_hp_override"):
			_apply_hub_hp_override(state, pid, int(spawn.get("hub_hp_override", 0)))
	if bool(scenario.get("domination_combat_speed", false)):
		_flatten_domination_combat_corridor(state)
	_expand_regression_hq_stores(state, scenario)


static func _flatten_domination_combat_corridor(state: GameState) -> void:
	# East-west march lane for p1 squads plus vertical spokes toward p3/p4 hubs.
	for q in range(0, state.map.width):
		for r in range(0, state.map.height):
			var coord := HexCoord.new(q, r)
			if not state.map.in_bounds(coord):
				continue
			var on_lane: bool = r == 8 and q >= 2 and q <= 14
			var on_spoke: bool = q == 8 and r >= 2 and r <= 13
			if not on_lane and not on_spoke:
				continue
			var tile := HexTile.new("grass", 0, 1, 1)
			if state.map.get_tile(coord) != null:
				tile.owner_player_id = state.map.get_tile(coord).owner_player_id
			state.map.set_tile(coord, tile)


static func _expand_regression_hq_stores(state: GameState, scenario: Dictionary) -> void:
	var starting: Dictionary = scenario.get("starting_wares", {})
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		if pid == "":
			continue
		var hq_ids: Dictionary = EconomyState.get_meta_value(state, "hq_building_ids", {})
		var hq_id: int = int(hq_ids.get(pid, 0))
		var hq: Dictionary = EconomyState.get_building(state, hq_id)
		var store_id: int = int(hq.get("store_id", 0))
		if store_id <= 0:
			continue
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		store["capacity"] = maxi(int(store.get("capacity", 100)), 400)
		for ware_type in starting.keys():
			var want: int = int(starting[ware_type])
			var have: int = int(store.get("wares", {}).get(str(ware_type), 0))
			if have < want:
				EconomyState.inventory_add(store, str(ware_type), want - have)
		EconomyState.set_ware_store(state, store_id, store)


static func prep_micro(state: GameState, scenario: Dictionary) -> void:
	prep_all_spawns(state, scenario)
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		_apply_spawn_starting_roads(state, spawn, pid)
		_apply_spawn_prebuilt(state, spawn, pid)
		if spawn.has("hub_hp_override"):
			_apply_hub_hp_override(state, pid, int(spawn.get("hub_hp_override", 0)))


static func _apply_spawn_starting_roads(state: GameState, spawn: Dictionary, player_id: String) -> void:
	for segment in spawn.get("starting_roads", []):
		if typeof(segment) != TYPE_DICTIONARY:
			continue
		EconomyRoadSystem.place_road(
			state,
			HexCoord.new(int(segment.get("from_q", -1)), int(segment.get("from_r", -1))),
			HexCoord.new(int(segment.get("to_q", -1)), int(segment.get("to_r", -1))),
			player_id,
		)


static func _apply_spawn_prebuilt(state: GameState, spawn: Dictionary, player_id: String) -> void:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	for entry in spawn.get("starting_buildings", []):
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var btype: String = str(entry.get("type", entry.get("building_type", "")))
		var q: int = int(entry.get("q", -1))
		var r: int = int(entry.get("r", -1))
		if btype == "" or q < 0 or r < 0:
			continue
		EconomyInit.spawn_completed_building(state, btype, q, r, player_id, cfg)
	for entry in spawn.get("units", []):
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var unit_type: String = str(entry.get("type", ""))
		var q: int = int(entry.get("q", -1))
		var r: int = int(entry.get("r", -1))
		var count: int = int(entry.get("count", 1))
		var mcfg: MilitaryConfig = MilitaryState.get_config(state)
		for _i in range(count):
			MilitaryProductionSystem.create_unit(state, unit_type, q, r, player_id, mcfg)


static func _apply_hub_hp_override(state: GameState, player_id: String, hp: int) -> void:
	if hp <= 0:
		return
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != "command_hub":
			continue
		building["hp"] = hp
		MilitaryState.add_building(state, building)


static func create_prepared_state(ticks: int = 0, scenario_path: String = SCENARIO_PATH) -> GameState:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
	var state: GameState = runner.create_game(scenario)
	if scenario_path.ends_with("god_strategy_domination_regression.json"):
		prep_domination_regression(state, scenario)
	elif scenario_path.contains("/micro/"):
		prep_micro(state, scenario)
	else:
		prep_all_spawns(state, scenario)
	if ticks > 0:
		state = runner.run_ticks(state, ticks)
	return state


static func run_scripted_match(max_ticks: int = GodStrategyConstants.PROOF_TICK_CAP) -> Dictionary:
	return run_scripted_match_for_scenario(load_scenario(), max_ticks, false)


static func run_scripted_regression(max_ticks: int = GodStrategyConstants.PROOF_TICK_CAP) -> Dictionary:
	return run_scripted_match_for_scenario(load_regression_scenario(), max_ticks, true, true)


static func run_scripted_micro(
	scenario_name: String,
	max_ticks: int = GodStrategyConstants.MICRO_TICK_CAP,
	p1_only: bool = true,
) -> Dictionary:
	return run_scripted_match_for_scenario(load_micro_scenario(scenario_name), max_ticks, false, p1_only, true)


static func run_scripted_match_for_scenario(
	scenario: Dictionary,
	max_ticks: int = GodStrategyConstants.PROOF_TICK_CAP,
	is_regression: bool = false,
	p1_only_scripted: bool = false,
	is_micro: bool = false,
) -> Dictionary:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario_copy: Dictionary = scenario.duplicate(true)
	scenario_copy["max_ticks"] = max_ticks
	scenario_copy["stall_ticks_threshold"] = 0
	var state: GameState = runner.create_game(scenario_copy)
	if is_regression:
		prep_domination_regression(state, scenario_copy)
	elif is_micro:
		prep_micro(state, scenario_copy)
	else:
		prep_all_spawns(state, scenario_copy)

	var players: Array = scenario_copy.get("players", ["p1", "p2", "p3", "p4"])
	for pid in players:
		var pid_str: String = str(pid)
		if p1_only_scripted and pid_str != "p1":
			quad.set_bot(pid_str, PassiveBot.new(pid_str))
		else:
			quad.set_bot(pid_str, ScriptedGodStrategyBot.new(pid_str))

	state = quad.run_match(state, scenario_copy)
	var p1_bot: ScriptedGodStrategyBot = quad.bots.get("p1") as ScriptedGodStrategyBot
	return {
		"state": state,
		"scenario": scenario_copy,
		"recorder": quad.get_recorder(),
		"match_end": GodStrategyMatchEnd.evaluate(state, scenario_copy),
		"p1_progression": p1_bot.get_progression_report() if p1_bot != null else {},
	}


static func write_layout_sweep_artefact() -> String:
	var out_path: String = ProjectSettings.globalize_path("res://").path_join(
		"../docs/audit/god_strategy/map_layout_sweep.json",
	)
	var sweep: Dictionary = {
		"mvp_a": GodStrategyMapViability.export_layout_sweep(SCENARIO_PATH),
		"regression": GodStrategyMapViability.export_layout_sweep(REGRESSION_SCENARIO_PATH),
	}
	var file := FileAccess.open(out_path, FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(sweep, "\t"))
		file.close()
	return out_path


static func building_of_type(state: GameState, building_type: String, player_id: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	for building in EconomyState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	return 0


static func count_units(state: GameState, unit_type: String, player_id: String) -> int:
	var count: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type and str(unit.get("player_id", "")) == player_id:
			count += 1
	return count


static func phase_index(phase: String) -> int:
	var idx: int = GodStrategyConstants.PHASE_ORDER.find(phase)
	if idx < 0 and phase.begins_with("place_"):
		idx = GodStrategyConstants.PHASE_ORDER.find(phase.replace("place_", ""))
	if idx < 0 and phase == "wait_construction":
		idx = GodStrategyConstants.PHASE_ORDER.find("roads")
	if idx < 0 and phase == "wait_production":
		idx = GodStrategyConstants.PHASE_ORDER.find("barracks")
	return maxi(idx, 0)


static func find_barracks_pad_site(state: GameState, player_id: String) -> Vector2i:
	var hq: Vector2i = EconomyState.get_hq_position(state, player_id)
	var center_q: int = state.map.width / 2
	var center_r: int = state.map.height / 2
	var dir_q: int = clampi(center_q - hq.x, -1, 1)
	var dir_r: int = clampi(center_r - hq.y, -1, 1)
	if dir_q == 0 and dir_r == 0:
		dir_q = 1
	var perp_q: int = -dir_r
	var perp_r: int = dir_q
	var anchor_q: int = hq.x + dir_q * 6 + perp_q * 2
	var anchor_r: int = hq.y + dir_r * 6 + perp_r * 2
	if EconomyMapRules.can_place_building(state, "barracks", anchor_q, anchor_r, player_id).get("ok", false):
		return Vector2i(anchor_q, anchor_r)
	for site in GodStrategyMapViability.find_all_legal_hexes(state, player_id, "barracks", 16, true):
		return Vector2i(int(site.get("q", -1)), int(site.get("r", -1)))
	return Vector2i(-1, -1)
