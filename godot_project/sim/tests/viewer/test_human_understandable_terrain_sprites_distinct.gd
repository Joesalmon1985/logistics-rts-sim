extends TestCase

func get_name() -> String:
	return "test_human_understandable_terrain_sprites_distinct"


func _run_body() -> void:
	SettlerAssetRegistry.load_registry()
	var grass: Texture2D = SettlerAssetRegistry.get_terrain_texture("grass")
	var forest: Texture2D = SettlerAssetRegistry.get_terrain_texture("forest")
	var rock: Texture2D = SettlerAssetRegistry.get_terrain_texture("rock")
	assert_true(grass != null and forest != null and rock != null, "terrain textures missing")
	assert_true(grass.get_image().get_data().hex_encode() != forest.get_image().get_data().hex_encode(), "grass/forest same")
	assert_true(rock.get_image().get_data().hex_encode() != grass.get_image().get_data().hex_encode(), "rock/grass same")
