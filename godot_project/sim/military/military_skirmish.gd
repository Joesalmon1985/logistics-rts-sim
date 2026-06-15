class_name MilitarySkirmish
extends RefCounted

## Seeded skirmish setup hooks. Produces a deterministic scenario dictionary
## (seeded terrain via MapGenerator + mirrored spawns + scattered resource fields)
## suitable for create_game. Determinism: identical seed/args => identical scenario.


static func build_scenario(seed_value: int, player_count: int = 2, map_w: int = 24, map_h: int = 24) -> Dictionary:
	var players: Array = []
	for i in range(maxi(2, player_count)):
		players.append("p%d" % (i + 1))

	var margin: int = 4
	var spawns: Array = []
	# Two opposing corners; additional players fill remaining corners deterministically.
	var corners: Array = [
		Vector2i(margin, margin),
		Vector2i(map_w - 1 - margin, map_h - 1 - margin),
		Vector2i(map_w - 1 - margin, margin),
		Vector2i(margin, map_h - 1 - margin),
	]
	for idx in range(players.size()):
		var c: Vector2i = corners[idx % corners.size()]
		spawns.append({"player_id": str(players[idx]), "q": c.x, "r": c.y})

	# Resource fields: one near each spawn (mirrored) plus a deterministic centre field.
	var rng := SeededRng.new(maxi(1, seed_value))
	var fields: Array = []
	for spawn in spawns:
		var fq: int = clampi(int(spawn.get("q", 0)) + rng.next_int(-2, 2), 1, map_w - 2)
		var fr: int = clampi(int(spawn.get("r", 0)) + rng.next_int(2, 4), 1, map_h - 2)
		fields.append({"q": fq, "r": fr, "field_type": "mineral_field"})
	fields.append({"q": map_w / 2, "r": map_h / 2, "field_type": "rich_mineral_field"})

	return {
		"variant": "military_trial",
		"seed": maxi(1, seed_value),
		"map_width": map_w,
		"map_height": map_h,
		"players": players,
		"build_radius": 12,
		"starting_credits": 3000,
		"fog_enabled": 1,
		"spawns": spawns,
		"resource_fields": fields,
		"win_condition": "destroy_all_hubs",
	}
