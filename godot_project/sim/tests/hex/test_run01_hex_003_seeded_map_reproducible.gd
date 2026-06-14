extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_HEX_003_seeded_map_reproducible"


func run() -> void:
	var terrain_config: Dictionary = ConfigLoader.load_terrain_config()
	var map_a: HexMap = MapGenerator.generate_from_seed(555, 10, 10, terrain_config)
	var map_b: HexMap = MapGenerator.generate_from_seed(555, 10, 10, terrain_config)

	var hash_a: String = MapGenerator.compute_map_hash(map_a)
	var hash_b: String = MapGenerator.compute_map_hash(map_b)
	assert_eq(hash_a, hash_b, "same seed/config must produce same map hash")

	var map_c: HexMap = MapGenerator.generate_from_seed(556, 10, 10, terrain_config)
	var hash_c: String = MapGenerator.compute_map_hash(map_c)
	assert_true(hash_a != hash_c, "different seed should produce different map hash")
