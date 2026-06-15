class_name CommandAvailabilityProvider
extends RefCounted

## Sim-owned command availability for policy clients and debug export.
## Legality is determined solely by command.validate(state); this class enumerates
## bounded candidates, filters to legal commands, and always includes pass/noop.


static func list_available_commands(state: GameState, actor_id: String = "") -> Array:
	return _list_candidates(state, actor_id)


static func enumerate_legal_commands(state: GameState, actor_id: String = "") -> Array:
	var resolved_actor: String = _resolve_actor(state, actor_id)
	if resolved_actor == "":
		return []

	var commands: Array = _list_candidates(state, resolved_actor)
	if not _has_pass_command(commands):
		var pass_cmd := NoopCommand.new(resolved_actor, state.tick)
		commands.append(CommandAvailabilityDto.from_command(
			pass_cmd,
			"Pass / wait",
			{},
		))
	return CommandAvailabilityDto.sort_commands(commands)


static func _list_candidates(state: GameState, actor_id: String) -> Array:
	if state == null:
		return []
	var resolved_actor: String = _resolve_actor(state, actor_id)
	if resolved_actor == "":
		return []

	var raw: Array = []
	match state.variant_id:
		"economy_trial":
			raw = EconomyCommandAvailability.list_candidates(state, resolved_actor)
		"military_trial":
			raw = MilitaryCommandAvailability.list_candidates(state, resolved_actor)
		_:
			raw = []

	return CommandAvailabilityDto.sort_commands(raw)


static func _resolve_actor(state: GameState, actor_id: String) -> String:
	if actor_id != "":
		return actor_id
	return _default_actor(state)


static func _default_actor(state: GameState) -> String:
	if not state.players.is_empty():
		return state.players[0].player_id
	return "p1"


static func _has_pass_command(commands: Array) -> bool:
	for entry in commands:
		if str(entry.get("type", "")) == "NoopCommand":
			return true
	return false
