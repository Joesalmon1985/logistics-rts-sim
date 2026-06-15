extends "res://sim/tests/test_case.gd"

## MIL-MAP-001: the military variant reuses the shared RUN_01 axial hex map. There
## must be no parallel/square grid, and military map rules must read passability
## straight from the shared HexMap tiles.


func get_name() -> String:
	return "test_MIL_MAP_001_shared_hex_map_no_parallel_grid"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game({
		"variant": "military_trial",
		"seed": 3,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
	})

	assert_not_null(state.map, "shared HexMap present")
	assert_true(state.map is HexMap, "map is the shared axial HexMap type")
	assert_eq(state.map.width, 10)
	assert_eq(state.map.height, 10)

	# Axial hex topology: every in-bounds tile uses 6-neighbour adjacency.
	assert_eq(HexCoord.new(4, 4).neighbors().size(), 6, "axial hex has 6 neighbours")

	# No parallel grid stored in entities (no "grid"/"tiles"/"map" bucket).
	assert_false(state.entities.has("grid"), "no parallel grid bucket")
	assert_false(state.entities.has("tiles"), "no parallel tile bucket")
	assert_false(state.entities.has("map"), "no parallel map bucket")

	# Military passability is derived from the shared map tiles, not a copy.
	for q in range(state.map.width):
		for r in range(state.map.height):
			var coord := HexCoord.new(q, r)
			var tile: HexTile = state.map.get_tile(coord)
			if tile == null:
				continue
			var expected: bool = tile.passability > 0
			assert_eq(MilitaryMapRules.is_passable(state, q, r), expected, "passability matches shared tile at %d,%d" % [q, r])
