extends TestCase

func get_name() -> String:
	return "test_human_understandable_sprite_registry_complete"


func _run_body() -> void:
	SettlerAssetRegistry.load_registry()
	for entry in SettlerAssetRegistry.required_concepts():
		assert_true(
			SettlerAssetRegistry.file_exists_for(str(entry[0]), str(entry[1])),
			"missing %s:%s" % [entry[0], entry[1]],
		)
