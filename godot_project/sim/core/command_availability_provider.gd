class_name CommandAvailabilityProvider
extends RefCounted

## Sim-owned command availability for viewer/debug clients.
## Legality is determined solely by command.validate(state); this class only enumerates
## bounded candidates and filters to legal commands.


static func list_available_commands(state: GameState, actor_id: String = "") -> Array:
	if state == null:
		return []
	var resolved_actor: String = actor_id
	if resolved_actor == "":
		resolved_actor = _default_actor(state)
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


static func _default_actor(state: GameState) -> String:
	if not state.players.is_empty():
		return state.players[0].player_id
	return "p1"
