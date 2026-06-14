extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_HEX_001_hex_has_six_neighbours"


func run() -> void:
	var center := HexCoord.new(3, 4)
	var neighbors: Array[HexCoord] = center.neighbors()
	assert_eq(neighbors.size(), 6)

	var expected: Array[Vector2i] = [
		Vector2i(4, 4),
		Vector2i(4, 3),
		Vector2i(3, 3),
		Vector2i(2, 4),
		Vector2i(2, 5),
		Vector2i(3, 5),
	]
	for i in range(expected.size()):
		assert_eq(neighbors[i].q, expected[i].x)
		assert_eq(neighbors[i].r, expected[i].y)

	for direction_index in range(6):
		var neighbor: HexCoord = center.neighbor(direction_index)
		assert_eq(center.distance(neighbor), 1)
