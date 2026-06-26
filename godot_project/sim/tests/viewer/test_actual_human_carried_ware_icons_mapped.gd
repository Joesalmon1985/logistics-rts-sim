extends TestCase

const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")


func get_name() -> String:
	return "test_actual_human_carried_ware_icons_mapped"


func _run_body() -> void:
	var review: Dictionary = ReviewUtil.probe_sprite_legibility()
	assert_true(bool(review.get("carrier_sprite_visible", false)), "carrier sprite")
	assert_true(bool(review.get("carried_ware_icon_visible", false)), "ware icons")
	SettlerAssetRegistry.load_registry()
	for ware in ["timber", "planks", "stone"]:
		var tex: Texture2D = SettlerAssetRegistry.get_texture("wares", ware)
		assert_true(tex != null, "ware icon %s" % ware)
		var carrier: Texture2D = SettlerAssetRegistry.get_texture("logistics", "carrier")
		assert_true(tex.get_image().get_data() != carrier.get_image().get_data(), "ware differs from carrier %s" % ware)
