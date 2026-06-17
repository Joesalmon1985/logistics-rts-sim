class_name SimTestLoop
extends RefCounted

## Shared early-exit tick helpers for long-running simulation tests (RUN_02.8c).


static func run_ticks_until(
	state: GameState,
	runner: SimulationTickRunner,
	max_ticks: int,
	predicate: Callable,
) -> Dictionary:
	var ticks: int = 0
	while ticks < max_ticks:
		if predicate.is_valid() and bool(predicate.call(state)):
			return {"state": state, "ticks": ticks, "stopped_early": true}
		state = runner.tick(state)
		ticks += 1
	return {"state": state, "ticks": ticks, "stopped_early": false}


static func run_until_event(
	state: GameState,
	runner: SimulationTickRunner,
	max_ticks: int,
	event_types: Array,
) -> Dictionary:
	var type_set: Dictionary = {}
	for event_type in event_types:
		type_set[str(event_type)] = true
	var start_event_count: int = state.event_log.events.size()
	return run_ticks_until(
		state,
		runner,
		max_ticks,
		func(current: GameState) -> bool:
			for i in range(start_event_count, current.event_log.events.size()):
				var event = current.event_log.events[i]
				if type_set.has(str(event.type)):
					return true
			return false,
	)


static func run_bot_until(
	state: GameState,
	bot_runner: BotRunner,
	bot: Bot,
	max_ticks: int,
	stop_predicate: Callable,
) -> Dictionary:
	bot_runner.max_ticks = max_ticks
	bot_runner.bot = bot
	bot_runner.decisions.clear()
	bot_runner.recorder.clear()
	bot.reset(bot.player_id)

	var ticks: int = 0
	while ticks < max_ticks:
		if stop_predicate.is_valid() and bool(stop_predicate.call(state, bot_runner)):
			break

		EconomyDiagnosticsSystem.tick(state, "bot")
		var snapshot: Dictionary = bot_runner._snapshot_for_bot(state, bot)
		if bot.is_complete(snapshot):
			break

		bot.observe(snapshot, bot.player_id)
		var command: SimCommand = bot.decide(state.tick)
		if command != null:
			var result: CommandResult = bot_runner.validator.validate_and_apply(state, command)
			bot_runner.recorder.record(state.tick, command, result)
			bot_runner._record_decision(state, command, result)
			if bot.has_method("on_command_applied"):
				bot.on_command_applied(command, result)

		state = bot_runner.runner.tick(state)
		ticks += 1

		if bot_runner._variant_victory(state):
			break

	bot_runner.decisions = bot.get_decisions()
	return {"state": state, "ticks": ticks, "stopped_early": ticks < max_ticks}


static func has_delivery_progress(state: GameState, player_id: String) -> bool:
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) != player_id:
			continue
		for ware_type in site.get("delivered", {}):
			if int(site.get("delivered", {}).get(ware_type, 0)) > 0:
				return true
		if int(site.get("progress_percent", 0)) > 0:
			return true
		var required_work: int = int(site.get("required", {}).get("builder_work", 0))
		if required_work > 0 and int(site.get("builder_work_remaining", required_work)) < required_work:
			return true
	return false


static func has_completed_building_type(state: GameState, player_id: String, building_type: String) -> bool:
	var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if int(building.get("id", 0)) == hq_id:
			continue
		if str(building.get("building_type", "")) != building_type:
			continue
		var state_name: String = str(building.get("state", ""))
		if state_name in [
			EconomyConstants.BLD_PLANNED,
			EconomyConstants.BLD_UNDER_CONSTRUCTION,
		]:
			continue
		return true
	return false


static func has_accepted_command_type(bot_runner: BotRunner, command_type: String) -> bool:
	for entry in bot_runner.get_recorder().get_command_entries():
		if not bool(entry.get("accepted", false)):
			continue
		var cmd: Dictionary = entry.get("command", {})
		if str(cmd.get("type", "")) == command_type:
			return true
	return false


static func site_road_adjacent(state: GameState, site: Dictionary, player_id: String) -> bool:
	var q: int = int(site.get("anchor_q", site.get("q", -1)))
	var r: int = int(site.get("anchor_r", site.get("r", -1)))
	if q < 0:
		return false
	var site_coord := HexCoord.new(q, r)
	for neighbor in site_coord.neighbors():
		if _hex_has_player_road(state, neighbor, player_id):
			return true
	return false


static func _hex_has_player_road(state: GameState, coord: HexCoord, player_id: String) -> bool:
	for road in EconomyState.all_roads(state):
		if str(road.get("player_id", "")) != player_id:
			continue
		var a := HexCoord.new(int(road.get("hex_a_q", road.get("from_q", -99))), int(road.get("hex_a_r", road.get("from_r", -99))))
		var b := HexCoord.new(int(road.get("hex_b_q", road.get("to_q", -99))), int(road.get("hex_b_r", road.get("to_r", -99))))
		if coord.distance(a) == 0 or coord.distance(b) == 0:
			return true
	for node in EconomyState.all_road_nodes(state):
		if str(node.get("player_id", "")) != player_id:
			continue
		if coord.distance(HexCoord.new(int(node.get("q", 0)), int(node.get("r", 0)))) == 0:
			return true
	return false
