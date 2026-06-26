extends TestCase

func get_name() -> String:
	return "test_human_understandable_carrier_ware_icons"


func _run_body() -> void:
	SettlerAssetRegistry.load_registry()
	for ware in ["timber", "planks", "stone"]:
		var tex: Texture2D = SettlerAssetRegistry.get_texture("wares", ware)
		assert_true(tex != null, "missing ware icon %s" % ware)
