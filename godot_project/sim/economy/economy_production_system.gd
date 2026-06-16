class_name EconomyProductionSystem
extends RefCounted

## Recipe execution, forest/deposit depletion, mine food, surveyor reveals.


static func tick(state: GameState) -> void:
	for building in EconomyPrioritySystem.sort_buildings(EconomyState.all_buildings(state)):
		_tick_building(state, building)


static func tick_forest(state: GameState) -> void:
	var summary := {"total_density": 0, "forest_hexes": 0, "depleted_hexes": 0}
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var min_timber: int = cfg.int_val("tree_density_min_for_timber", 20)
	for key in state.map.tiles:
		var tile: HexTile = state.map.tiles[key]
		if tile.terrain_type != "forest" and tile.deposit_type != "forest":
			continue
		summary["forest_hexes"] += 1
		summary["total_density"] += tile.tree_density
		if tile.tree_density < min_timber:
			summary["depleted_hexes"] += 1
	EconomyState.set_meta_value(state, "forest_summary", summary)


static func tick_deposits(state: GameState) -> void:
	var summary := {"revealed": 0, "depleted": 0, "active": 0}
	for key in state.map.tiles:
		var tile: HexTile = state.map.tiles[key]
		if tile.deposit_type == "":
			continue
		if tile.deposit_revealed > 0:
			summary["revealed"] += 1
			if tile.deposit_amount <= 0:
				summary["depleted"] += 1
			else:
				summary["active"] += 1
	EconomyState.set_meta_value(state, "deposit_summary", summary)


static func _tick_building(state: GameState, building: Dictionary) -> void:
	var state_name: String = str(building.get("state", ""))
	if state_name in [
		EconomyConstants.BLD_PLANNED,
		EconomyConstants.BLD_UNDER_CONSTRUCTION,
		EconomyConstants.BLD_DISABLED,
		EconomyConstants.BLD_COMPLETED,
		EconomyConstants.BLD_BLOCKED,
	]:
		return
	if int(building.get("enabled", 1)) <= 0:
		building["state"] = EconomyConstants.BLD_DISABLED
		EconomyState.add_building(state, building)
		return

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
	var recipe_id: String = str(bdef.get("recipe_id", ""))
	if recipe_id == "":
		return

	var recipe: Dictionary = cfg.get_recipe(recipe_id)
	if recipe.is_empty():
		return

	if int(recipe.get("requires_worker", 0)) > 0:
		var slots: int = int(bdef.get("worker_slots", 0))
		if building.get("assigned_workers", []).size() < slots:
			building["state"] = EconomyConstants.BLD_PAUSED_MISSING_WORKER
			if str(building.get("paused_reason", "")) != "tool_missing":
				building["paused_reason"] = "missing_worker"
			EconomyState.add_building(state, building)
			return

	if not _check_food_support(state, building, recipe, bdef):
		building["state"] = EconomyConstants.BLD_PAUSED_MISSING_INPUT
		building["paused_reason"] = "food_missing"
		EconomyState.emit_event(state, "food_missing", {"building_id": int(building.get("id", 0))})
		EconomyState.add_building(state, building)
		return

	if not _check_resource_availability(state, building, recipe, bdef):
		building["state"] = EconomyConstants.BLD_PAUSED_MISSING_INPUT
		building["paused_reason"] = "input_missing"
		EconomyState.emit_event(state, "input_missing", {"building_id": int(building.get("id", 0))})
		EconomyState.add_building(state, building)
		return

	if not _check_output_space(state, building, recipe):
		building["state"] = EconomyConstants.BLD_PAUSED_NO_OUTPUT
		building["paused_reason"] = "output_blocked"
		EconomyState.emit_event(state, "output_blocked", {"building_id": int(building.get("id", 0))})
		EconomyState.add_building(state, building)
		return

	building["state"] = EconomyConstants.BLD_ACTIVE
	building["paused_reason"] = ""
	var progress: int = int(building.get("production_progress", 0)) + 1
	var ticks_needed: int = int(recipe.get("ticks", 1))

	if progress < ticks_needed:
		building["production_progress"] = progress
		building["last_production_tick"] = state.tick
		EconomyState.add_building(state, building)
		return

	_complete_production_cycle(state, building, recipe, bdef)
	building["production_progress"] = 0
	EconomyState.add_building(state, building)


static func _check_food_support(
	state: GameState,
	building: Dictionary,
	recipe: Dictionary,
	bdef: Dictionary,
) -> bool:
	if int(recipe.get("requires_food", 0)) <= 0:
		return true
	var food_ware: String = str(bdef.get("food_ware", "rations"))
	var store_id: int = int(building.get("store_id", 0))
	if store_id <= 0:
		return false
	var store: Dictionary = EconomyState.get_ware_store(state, store_id)
	var needed: int = int(recipe.get("inputs", {}).get(food_ware, 1))
	return EconomyState.inventory_has(store, food_ware, needed)


static func _check_resource_availability(
	state: GameState,
	building: Dictionary,
	recipe: Dictionary,
	bdef: Dictionary,
) -> bool:
	var store_id: int = int(building.get("store_id", 0))
	var store: Dictionary = EconomyState.get_ware_store(state, store_id) if store_id > 0 else {}
	for ware_type in recipe.get("inputs", {}):
		if not EconomyState.inventory_has(store, str(ware_type), int(recipe["inputs"][ware_type])):
			return false

	if int(recipe.get("consumes_tree_density", 0)) > 0:
		var tile: HexTile = state.map.get_tile(HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0))))
		var cfg: EconomyConfig = EconomyState.get_config(state)
		if tile == null or tile.tree_density < cfg.int_val("tree_density_min_for_timber", 20):
			return false

	if int(recipe.get("consumes_deposit", 0)) > 0:
		var tile: HexTile = state.map.get_tile(HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0))))
		if tile == null or tile.deposit_amount <= 0:
			return false
		var required: String = str(bdef.get("required_deposit", ""))
		if required != "" and tile.deposit_type != required and not (required == "forest"):
			return false

	return true


static func _check_output_space(state: GameState, building: Dictionary, recipe: Dictionary) -> bool:
	var outputs: Dictionary = recipe.get("outputs", {})
	if outputs.is_empty():
		return true
	var store_id: int = int(building.get("store_id", 0))
	if store_id <= 0:
		return true
	var store: Dictionary = EconomyState.get_ware_store(state, store_id)
	var total_output: int = 0
	for ware_type in outputs:
		total_output += int(outputs[ware_type])
	return EconomyState.inventory_free(store) >= total_output


static func _complete_production_cycle(
	state: GameState,
	building: Dictionary,
	recipe: Dictionary,
	bdef: Dictionary,
) -> void:
	var store_id: int = int(building.get("store_id", 0))
	var store: Dictionary = EconomyState.get_ware_store(state, store_id) if store_id > 0 else {}

	for ware_type in recipe.get("inputs", {}):
		EconomyState.inventory_remove(store, str(ware_type), int(recipe["inputs"][ware_type]))
		EconomyState.emit_event(state, "ware_consumed", {
			"building_id": int(building.get("id", 0)),
			"ware": str(ware_type),
			"amount": int(recipe["inputs"][ware_type]),
		})

	for ware_type in recipe.get("outputs", {}):
		var amount: int = int(recipe["outputs"][ware_type])
		if not EconomyState.inventory_add(store, str(ware_type), amount):
			EconomyState.emit_event(state, "storage_full", {"building_id": int(building.get("id", 0))})
		else:
			EconomyState.emit_event(state, "production_completed", {
				"building_id": int(building.get("id", 0)),
				"ware": str(ware_type),
				"amount": amount,
			})

	if store_id > 0:
		EconomyState.set_ware_store(state, store_id, store)

	if int(recipe.get("consumes_tree_density", 0)) > 0:
		_deplete_forest(state, building)
	if int(recipe.get("increases_tree_density", 0)) > 0:
		_renew_forest_adjacent(state, building)
	if int(recipe.get("consumes_deposit", 0)) > 0:
		_deplete_deposit(state, building)
	if int(recipe.get("reveals_deposits", 0)) > 0:
		_reveal_deposits_adjacent(state, building)

	EconomyState.emit_event(state, "production_started", {"building_id": int(building.get("id", 0))})
	building["last_production_tick"] = state.tick
	EconomyState.add_building(state, building)
	_increment_progress_score(state, 1)


static func _deplete_forest(state: GameState, building: Dictionary) -> void:
	var coord := HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
	var tile: HexTile = state.map.get_tile(coord)
	if tile == null:
		return
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var decrement: int = cfg.int_val("tree_density_decrement_per_cycle", 5)
	tile.tree_density = maxi(0, tile.tree_density - decrement)
	EconomyState.emit_event(state, "tree_density_changed", {
		"q": coord.q,
		"r": coord.r,
		"tree_density": tile.tree_density,
	})
	if tile.tree_density < cfg.int_val("tree_density_min_for_timber", 20):
		EconomyState.emit_event(state, "forest_depleted", {"q": coord.q, "r": coord.r})


static func _renew_forest_adjacent(state: GameState, building: Dictionary) -> void:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var increment: int = cfg.int_val("tree_density_increment_per_cycle", 3)
	var max_density: int = cfg.int_val("tree_density_max", 100)
	var anchor := HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
	for neighbour in anchor.neighbors():
		if not state.map.in_bounds(neighbour):
			continue
		var tile: HexTile = state.map.get_tile(neighbour)
		if tile == null or tile.terrain_type != "forest":
			continue
		tile.tree_density = mini(max_density, tile.tree_density + increment)
		EconomyState.emit_event(state, "forest_renewed", {
			"q": neighbour.q,
			"r": neighbour.r,
			"tree_density": tile.tree_density,
		})


static func _deplete_deposit(state: GameState, building: Dictionary) -> void:
	var coord := HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
	var tile: HexTile = state.map.get_tile(coord)
	if tile == null:
		return
	var cfg: EconomyConfig = EconomyState.get_config(state)
	tile.deposit_amount = maxi(0, tile.deposit_amount - cfg.int_val("deposit_decrement_per_cycle", 1))
	EconomyState.emit_event(state, "resource_extracted", {
		"q": coord.q,
		"r": coord.r,
		"deposit_type": tile.deposit_type,
		"remaining": tile.deposit_amount,
	})
	if tile.deposit_amount <= 0:
		EconomyState.emit_event(state, "resource_depleted", {
			"q": coord.q,
			"r": coord.r,
			"deposit_type": tile.deposit_type,
		})


static func _reveal_deposits_adjacent(state: GameState, building: Dictionary) -> void:
	var anchor := HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
	for neighbour in anchor.neighbors():
		if not state.map.in_bounds(neighbour):
			continue
		var tile: HexTile = state.map.get_tile(neighbour)
		if tile == null or tile.deposit_type == "":
			continue
		if tile.deposit_revealed <= 0:
			tile.deposit_revealed = 1
			EconomyState.emit_event(state, "deposit_revealed", {
				"q": neighbour.q,
				"r": neighbour.r,
				"deposit_type": tile.deposit_type,
			})


static func _increment_progress_score(state: GameState, amount: int) -> void:
	var score: int = int(EconomyState.get_meta_value(state, "progress_score", 0)) + amount
	EconomyState.set_meta_value(state, "progress_score", score)
	EconomyState.set_meta_value(state, "last_progress_tick", state.tick)
