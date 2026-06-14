class_name MapGenerator
extends RefCounted

const DEFAULT_TERRAIN_TYPES: Array[String] = ["grass", "water", "rock", "forest"]


static func generate_from_seed(
	seed_value: int,
	width: int,
	height: int,
	terrain_config: Dictionary = {},
) -> HexMap:
	var rng := SeededRng.from_seed(seed_value)
	var terrain_types: Array = terrain_config.get("terrain_types", DEFAULT_TERRAIN_TYPES)
	var terrain_defs: Dictionary = terrain_config.get("terrain_defs", {})
	var game_map := HexMap.new(width, height)

	for r in range(height):
		for q in range(width):
			var coord := HexCoord.new(q, r)
			var terrain_index: int = rng.next_int(0, terrain_types.size() - 1)
			var terrain_type: String = str(terrain_types[terrain_index])
			var def: Dictionary = terrain_defs.get(terrain_type, {})
			var height_level: int = rng.next_int(0, 3)
			var passability: int = int(def.get("passability", 1))
			var buildability: int = int(def.get("buildability", 1))
			game_map.set_tile(
				coord,
				HexTile.new(terrain_type, height_level, passability, buildability),
			)

	return game_map


static func load_from_file(path: String) -> HexMap:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("MapGenerator.load_from_file: cannot open %s" % path)
		return HexMap.new()
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		push_error("MapGenerator.load_from_file: invalid JSON")
		return HexMap.new()
	return load_from_dict(parsed)


static func load_from_dict(data: Dictionary) -> HexMap:
	var game_map := HexMap.new(int(data.get("width", 0)), int(data.get("height", 0)))
	for hex_data in data.get("hexes", []):
		var coord := HexCoord.new(int(hex_data.get("q", 0)), int(hex_data.get("r", 0)))
		game_map.set_tile(coord, HexTile.from_dict(hex_data))
	return game_map


static func compute_map_hash(game_map: HexMap) -> String:
	return ReplayHash.sha256_hex(CanonicalJson.stringify(game_map.to_dict()))
