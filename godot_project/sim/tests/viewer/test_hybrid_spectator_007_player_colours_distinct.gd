extends TestCase


func get_name() -> String:
	return "test_hybrid_spectator_007_player_colours_distinct"


func _run_body() -> void:
	assert_eq(HybridBotSpectatorController.PLAYER_COLORS.size(), 4, "four player colors")
