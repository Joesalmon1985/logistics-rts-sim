class_name SetWorkerPolicyCommand
extends SimCommand

const VALID_POLICIES: Array[String] = ["auto", "manual"]

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetWorkerPolicyCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var policy: String = str(payload.get("policy", ""))
	if policy == "":
		return CommandResult.fail("INVALID_PAYLOAD", "Missing policy")
	if policy not in VALID_POLICIES:
		return CommandResult.fail("INVALID_POLICY", "Policy must be auto or manual")

	var owner_id: String = str(payload.get("player_id", player_id))
	if owner_id != player_id:
		return CommandResult.fail("NOT_OWNER", "Player cannot set policy for another owner")

	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var policy: String = str(payload.get("policy", "auto"))
	var owner_id: String = str(payload.get("player_id", player_id))
	var policies: Dictionary = EconomyState.get_meta_value(state, "worker_policies", {})
	policies[owner_id] = policy
	EconomyState.set_meta_value(state, "worker_policies", policies)

	var events: Array = [{
		"type": "worker_policy_changed",
		"tick": tick,
		"payload": {
			"player_id": owner_id,
			"policy": policy,
		},
	}]
	return CommandResult.ok(true, events)
