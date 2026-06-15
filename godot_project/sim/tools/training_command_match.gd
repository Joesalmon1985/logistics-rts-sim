class_name TrainingCommandMatch
extends RefCounted

const IGNORE_KEYS: Array[String] = ["tick"]
const MAX_ECONOMY_ACTIONS: int = 64
const MAX_MILITARY_ACTIONS: int = 128


static func noop_index(variant: String) -> int:
	if variant == "economy_trial":
		return MAX_ECONOMY_ACTIONS - 1
	if variant == "military_trial":
		return MAX_MILITARY_ACTIONS - 1
	return 0


static func find_index(
	command: Variant,
	available_commands: Array,
	registry: CommandRegistry,
	variant: String = "",
) -> int:
	if command == null:
		return noop_index(variant)
	if command is SimCommand:
		return _find_for_sim_command(command as SimCommand, available_commands, registry)
	if typeof(command) == TYPE_DICTIONARY:
		var cmd_dict: Dictionary = command as Dictionary
		var cmd_type: String = str(cmd_dict.get("type", ""))
		var payload: Dictionary = cmd_dict.get("payload", {}).duplicate(true)
		payload.erase("tick")
		payload["player_id"] = str(cmd_dict.get("player_id", payload.get("player_id", "")))
		var recreated: SimCommand = registry.create(cmd_type, payload)
		if recreated != null:
			return _find_for_sim_command(recreated, available_commands, registry)
	return -1


static func _find_for_sim_command(
	command: SimCommand,
	available_commands: Array,
	registry: CommandRegistry,
) -> int:
	for index in range(available_commands.size()):
		var entry: Dictionary = available_commands[index]
		var payload: Dictionary = entry.get("payload", {}).duplicate(true)
		for key in IGNORE_KEYS:
			payload.erase(key)
		var recreated: SimCommand = registry.create(str(entry.get("type", "")), payload)
		if recreated != null and _commands_equal(command, recreated):
			return index
	return -1


static func _commands_equal(left: SimCommand, right: SimCommand) -> bool:
	if left.type != right.type:
		return false
	var left_payload: Dictionary = left.payload.duplicate(true)
	var right_payload: Dictionary = right.payload.duplicate(true)
	for key in IGNORE_KEYS:
		left_payload.erase(key)
		right_payload.erase(key)
	return JSON.stringify(left_payload) == JSON.stringify(right_payload)


static func availability_entry_for_command(command: SimCommand) -> Dictionary:
	var payload: Dictionary = command.payload.duplicate(true)
	payload["player_id"] = command.player_id
	return CommandAvailabilityDto.from_command(command, "bc_fallback", {})
