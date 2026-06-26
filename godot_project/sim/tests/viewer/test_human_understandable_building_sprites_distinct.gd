extends TestCase


func get_name() -> String:
	return "test_human_understandable_building_sprites_distinct"


func _run_body() -> void:
	SettlerAssetRegistry.load_registry()
	var types: Array[String] = ["headquarters", "woodcutter", "forester", "sawmill", "quarry"]
	var hashes: Array = []
	for btype in types:
		var tex: Texture2D = SettlerAssetRegistry.get_building_texture(btype)
		assert_true(tex != null, "missing texture for %s" % btype)
		var img: Image = tex.get_image()
		hashes.append(img.get_data().hex_encode())
	for i in range(hashes.size()):
		for j in range(i + 1, hashes.size()):
			assert_true(hashes[i] != hashes[j], "sprites not distinct")
