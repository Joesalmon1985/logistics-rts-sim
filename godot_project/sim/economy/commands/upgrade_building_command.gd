class_name UpgradeBuildingCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("UpgradeBuildingCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail("BUILDING_NOT_FOUND", "Building not found: %d" % building_id)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Building owned by another player")
	if str(building.get("state", "")) != EconomyConstants.BLD_ACTIVE:
		return CommandResult.fail("BUILDING_NOT_ACTIVE", "Building is not active")

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var source_type: String = str(building.get("building_type", ""))
	var upgrade_def: Dictionary = cfg.get_upgrade_def(source_type)
	if upgrade_def.is_empty():
		return CommandResult.fail("NO_UPGRADE", "Building has no upgrade target")

	for site in EconomyState.all_construction_sites(state):
		if int(site.get("upgrade_source_id", 0)) == building_id:
			return CommandResult.fail("UPGRADE_IN_PROGRESS", "Upgrade already in progress")

	var target_type: String = str(upgrade_def.get("target", ""))
	var check: Dictionary = EconomyMapRules.can_place_building(
		state,
		target_type,
		int(building.get("anchor_q", 0)),
		int(building.get("anchor_r", 0)),
		player_id,
		building_id,
	)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var result: Dictionary = EconomyUpgradeSystem.start_upgrade(state, building_id, player_id)
	if not result.get("ok", false):
		return CommandResult.fail(str(result.get("reason", "REJECTED")))
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	var source_type: String = str(building.get("building_type", ""))
	var target_type: String = str(cfg.get_upgrade_def(source_type).get("target", ""))
	var events: Array = [{
		"type": "upgrade_started",
		"tick": tick,
		"payload": {
			"site_id": int(result.get("site_id", 0)),
			"building_id": building_id,
			"source_type": source_type,
			"target_type": target_type,
		},
	}]
	return CommandResult.ok(true, events)
