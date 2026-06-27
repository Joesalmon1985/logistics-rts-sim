extends TestCase

## Functional smoke for 3D spectator transport — bot stepping without hang.


func get_name() -> String:
	return "test_manual_qa_hybrid_3d_bot_match_functional"


func _run_body() -> void:
	SimBootstrap.load_all()
	var controller := Hybrid3dSpectatorController.new()
	controller.reset_match()
	var tick0: int = int(controller.snapshot.get("tick", 0))
	for _i in range(20):
		controller.bot_step_once()
	assert_true(int(controller.snapshot.get("tick", 0)) > tick0, "3d spectator advances")
