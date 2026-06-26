extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## HUMAN-MVP-ASSET-001 — asset registry loads required concepts.


func get_name() -> String:
	return "test_human_mvp_asset_registry"


func _run_body() -> void:
	SettlerAssetRegistry.load_registry()
	for entry in SettlerAssetRegistry.required_concepts():
		var cat: String = str(entry[0])
		var key: String = str(entry[1])
		assert_true(SettlerAssetRegistry.file_exists_for(cat, key), "missing file %s:%s" % [cat, key])
