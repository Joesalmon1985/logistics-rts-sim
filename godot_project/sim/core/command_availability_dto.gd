class_name CommandAvailabilityDto
extends RefCounted

## Builds sim-owned command availability DTOs for viewer/debug export.
## `id` is UI/debug-only; submission uses type + payload via CommandRegistry.


static func from_command(cmd: SimCommand, label: String, target: Dictionary = {}) -> Dictionary:
	var payload: Dictionary = cmd.payload.duplicate(true)
	payload["player_id"] = cmd.player_id
	payload["tick"] = cmd.tick
	return {
		"id": make_id(cmd),
		"label": label,
		"type": cmd.type,
		"actor_id": cmd.player_id,
		"target": target.duplicate(true),
		"payload": payload,
		"enabled": true,
		"disabled_reason": "",
		"source": "sim",
	}


static func make_id(cmd: SimCommand) -> String:
	var parts: Array[String] = [cmd.type, cmd.player_id]
	var keys: Array = cmd.payload.keys()
	keys.sort()
	for key in keys:
		parts.append("%s=%s" % [str(key), str(cmd.payload[key])])
	return "_".join(parts)


static func sort_commands(commands: Array) -> Array:
	var sorted: Array = commands.duplicate()
	sorted.sort_custom(_compare_dto)
	return sorted


static func _compare_dto(a: Dictionary, b: Dictionary) -> bool:
	var actor_a: String = str(a.get("actor_id", ""))
	var actor_b: String = str(b.get("actor_id", ""))
	if actor_a != actor_b:
		return actor_a < actor_b
	var type_a: String = str(a.get("type", ""))
	var type_b: String = str(b.get("type", ""))
	if type_a != type_b:
		return type_a < type_b
	var target_a: Dictionary = a.get("target", {})
	var target_b: Dictionary = b.get("target", {})
	var qa: int = int(target_a.get("q", target_a.get("from_q", -9999)))
	var qb: int = int(target_b.get("q", target_b.get("from_q", -9999)))
	if qa != qb:
		return qa < qb
	var ra: int = int(target_a.get("r", target_a.get("from_r", -9999)))
	var rb: int = int(target_b.get("r", target_b.get("from_r", -9999)))
	if ra != rb:
		return ra < rb
	return str(a.get("id", "")) < str(b.get("id", ""))
