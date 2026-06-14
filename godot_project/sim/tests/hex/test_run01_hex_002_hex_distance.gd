extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_HEX_002_hex_distance"


func run() -> void:
	var a := HexCoord.new(0, 0)
	var b := HexCoord.new(3, 0)
	assert_eq(a.distance(b), 3)

	var c := HexCoord.new(0, 0)
	var d := HexCoord.new(2, -2)
	assert_eq(c.distance(d), 2)

	var e := HexCoord.new(1, 2)
	var f := HexCoord.new(4, 4)
	assert_eq(e.distance(f), 5)

	assert_eq(a.distance(a), 0)
