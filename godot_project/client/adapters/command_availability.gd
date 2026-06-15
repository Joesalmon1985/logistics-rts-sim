class_name ClientCommandAvailability
extends RefCounted

## Client bridge to sim-owned command availability. Never derives legality.


static func list_from_state(state: GameState, actor_id: String = "") -> Array:
	return CommandAvailabilityProvider.list_available_commands(state, actor_id)


static func list_from_snapshot(snapshot: Dictionary) -> Array:
	if snapshot.has("available_commands"):
		return snapshot.get("available_commands", []).duplicate(true)
	return []


static func count(commands: Array, enabled_only: bool = true) -> int:
	if not enabled_only:
		return commands.size()
	var n: int = 0
	for cmd in commands:
		if bool(cmd.get("enabled", true)):
			n += 1
	return n
