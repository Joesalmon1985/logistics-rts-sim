class_name HybridCommandAvailability
extends RefCounted

## Bounded hybrid command candidates for policy clients.


static func list_candidates(state: GameState, actor_id: String) -> Array:
	var raw: Array = []
	var snapshot: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, actor_id)
	for macro_id in range(HybridAiActionSpace.MACRO_COUNT):
		if not HybridAiActionSpace._precondition_pass(state, actor_id, snapshot, macro_id):
			continue
		var compiled: Dictionary = HybridAiActionSpace.compile_command(state, actor_id, snapshot, macro_id, state.tick)
		if not bool(compiled.get("ok", false)):
			continue
		var cmd: SimCommand = compiled.get("command")
		if cmd == null:
			continue
		raw.append(CommandAvailabilityDto.from_command(
			cmd,
			HybridAiActionSpace.macro_name(macro_id),
			{"macro_id": macro_id},
		))
	return raw
