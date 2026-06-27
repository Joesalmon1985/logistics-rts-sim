class_name StartResearchCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("StartResearchCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var tech_id: String = str(payload.get("tech_id", ""))
	var check: Dictionary = MilitaryTechSystem.can_start(state, player_id, tech_id)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	if HybridConfig.is_hybrid(state):
		var costs: Dictionary = HybridState.get_config(state).get_research_cost(tech_id)
		if costs.is_empty() or not HybridState.has_wares_in_hq(state, player_id, costs):
			return CommandResult.fail(HybridConstants.ERR_INSUFFICIENT_WARES)
		return CommandResult.ok()
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var cost: int = int(cfg.get_tech_def(tech_id).get("credit_cost", 0))
	if MilitaryState.player_credits(state, player_id) < cost:
		return CommandResult.fail(MilitaryConstants.ERR_INSUFFICIENT_CREDITS)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var tech_id: String = str(payload.get("tech_id", ""))
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var tdef: Dictionary = cfg.get_tech_def(tech_id)
	if HybridConfig.is_hybrid(state):
		var costs: Dictionary = HybridState.get_config(state).get_research_cost(tech_id)
		if not HybridState.spend_wares_from_hq(state, player_id, costs):
			return CommandResult.fail(HybridConstants.ERR_INSUFFICIENT_WARES)
		MilitaryState.set_research(state, player_id, {
			"player_id": player_id,
			"tech_id": tech_id,
			"active": 1,
			"progress": 0,
			"required_ticks": int(tdef.get("research_ticks", 1)),
			"paused_reason": "",
			"ware_costs": costs.duplicate(true),
		})
		return CommandResult.ok(true, [
			{"type": "research_started", "tick": tick, "payload": {"player_id": player_id, "tech_id": tech_id, "ware_costs": costs}},
		])
	var cost: int = int(tdef.get("credit_cost", 0))
	if not MilitaryState.spend_credits(state, player_id, cost):
		return CommandResult.fail(MilitaryConstants.ERR_INSUFFICIENT_CREDITS)
	MilitaryState.set_research(state, player_id, {
		"player_id": player_id,
		"tech_id": tech_id,
		"active": 1,
		"progress": 0,
		"required_ticks": int(tdef.get("research_ticks", 1)),
		"paused_reason": "",
	})
	return CommandResult.ok(true, [
		{"type": "research_started", "tick": tick, "payload": {"player_id": player_id, "tech_id": tech_id, "cost": cost}},
	])
