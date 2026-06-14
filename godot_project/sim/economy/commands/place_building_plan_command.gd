class_name PlaceBuildingPlanCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("PlaceBuildingPlanCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var building_type: String = str(payload.get("building_type", ""))
	var anchor_q: int = int(payload.get("q", 0))
	var anchor_r: int = int(payload.get("r", 0))
	var owner_id: String = str(payload.get("player_id", player_id))
	if owner_id != player_id:
		return CommandResult.fail("NOT_OWNER", "Player cannot place for another owner")
	var check: Dictionary = EconomyMapRules.can_place_building(
		state,
		building_type,
		anchor_q,
		anchor_r,
		owner_id,
	)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_type: String = str(payload.get("building_type", ""))
	var anchor_q: int = int(payload.get("q", 0))
	var anchor_r: int = int(payload.get("r", 0))
	var owner_id: String = str(payload.get("player_id", player_id))
	var result: Dictionary = EconomyConstructionSystem.create_site(
		state,
		building_type,
		anchor_q,
		anchor_r,
		owner_id,
	)
	if not result.get("ok", false):
		return CommandResult.fail(str(result.get("reason", "REJECTED")))
	var site_id: int = int(result.get("site_id", 0))
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var events: Array = [
		{
			"type": "building_plan_placed",
			"tick": tick,
			"payload": {
				"site_id": site_id,
				"building_type": building_type,
				"q": anchor_q,
				"r": anchor_r,
				"player_id": owner_id,
				"required": cfg.get_construction_cost(building_type),
			},
		},
		{
			"type": "construction_started",
			"tick": tick,
			"payload": {"site_id": site_id},
		},
	]
	return CommandResult.ok(true, events)
