class_name SelectionFormatter
extends RefCounted

## Formats snapshot selection data for human-readable inspector display.


static func format(selection: Dictionary, snapshot: Dictionary = {}) -> String:
	var kind: String = str(selection.get("kind", "empty"))
	var data: Dictionary = selection.get("data", {})
	var q: int = int(data.get("q", data.get("anchor_q", -1)))
	var r: int = int(data.get("r", data.get("anchor_r", -1)))
	if q < 0 and data.has("anchor_q"):
		q = int(data.get("anchor_q", -1))
		r = int(data.get("anchor_r", -1))

	match kind:
		"building":
			return _format_building(data, q, r)
		"unit":
			return _format_unit(data, q, r)
		"tile":
			return _format_tile(data, q, r, snapshot)
		_:
			return "Empty hex (%d, %d)\nClick a coloured hex to inspect terrain, buildings, or units." % [q, r]


static func _format_building(data: Dictionary, q: int, r: int) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"BUILDING",
		"Type: %s" % str(data.get("building_type", "?")),
		"Player: %s" % str(data.get("player_id", "?")),
		"Position: (%d, %d)" % [q, r],
		"State: %s" % str(data.get("state", "?")),
		"ID: %s" % str(data.get("id", "?")),
	])
	return "\n".join(lines)


static func _format_unit(data: Dictionary, q: int, r: int) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"UNIT",
		"Type: %s" % str(data.get("unit_type", "?")),
		"Player: %s" % str(data.get("player_id", "?")),
		"Position: (%d, %d)" % [q, r],
		"State: %s" % str(data.get("state", "?")),
		"HP: %s" % str(data.get("hp", data.get("health", "?"))),
		"ID: %s" % str(data.get("id", "?")),
	])
	return "\n".join(lines)


static func _format_tile(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"TERRAIN HEX",
		"Position: (%d, %d)" % [q, r],
		"Terrain: %s" % str(data.get("terrain_type", "?")),
	])
	var layers: Dictionary = snapshot.get("display_layers", {})
	for res in layers.get("resources", []):
		if int(res.get("q", -1)) == q and int(res.get("r", -1)) == r:
			lines.append("Resource: %s (amount %s)" % [
				str(res.get("deposit_type", "?")),
				str(res.get("deposit_amount", "?")),
			])
	for res2 in snapshot.get("resource_fields", []):
		if int(res2.get("q", -1)) == q and int(res2.get("r", -1)) == r:
			lines.append("Resource field: %s" % str(res2.get("field_type", "?")))
	return "\n".join(lines)
