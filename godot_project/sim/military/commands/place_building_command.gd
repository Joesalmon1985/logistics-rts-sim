class_name PlaceBuildingCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("PlaceBuildingCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var owner_id: String = str(payload.get("player_id", player_id))
	if owner_id != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	var building_type: String = str(payload.get("building_type", ""))
	var check: Dictionary = MilitaryMapRules.can_place_building(state, building_type, int(payload.get("q", 0)), int(payload.get("r", 0)), owner_id)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var cost: int = int(cfg.get_building_def(building_type).get("credit_cost", 0))
	if MilitaryState.player_credits(state, owner_id) < cost:
		return CommandResult.fail(MilitaryConstants.ERR_INSUFFICIENT_CREDITS)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var owner_id: String = str(payload.get("player_id", player_id))
	var building_type: String = str(payload.get("building_type", ""))
	var q: int = int(payload.get("q", 0))
	var r: int = int(payload.get("r", 0))
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var cost: int = int(cfg.get_building_def(building_type).get("credit_cost", 0))
	if not MilitaryState.spend_credits(state, owner_id, cost):
		return CommandResult.fail(MilitaryConstants.ERR_INSUFFICIENT_CREDITS)
	var result: Dictionary = MilitaryConstructionSystem.create_site(state, building_type, q, r, owner_id)
	if not result.get("ok", false):
		MilitaryState.add_credits(state, owner_id, cost)
		return CommandResult.fail(str(result.get("reason", "REJECTED")))
	var site_id: int = int(result.get("site_id", 0))
	return CommandResult.ok(true, [
		{"type": "building_plan_placed", "tick": tick, "payload": {"site_id": site_id, "building_type": building_type, "q": q, "r": r, "player_id": owner_id, "cost": cost}},
		{"type": "construction_started", "tick": tick, "payload": {"site_id": site_id}},
	])
