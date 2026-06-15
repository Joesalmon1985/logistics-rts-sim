extends TestCase

## VIS-RENDER-001 — construction site and carrier extraction in render data.


func get_name() -> String:
	return "test_VIS_RENDER_001_construction_sites_and_carriers"


func _run_body() -> void:
	_test_economy_sites_and_carriers()
	_test_economy_site_progress()
	_test_military_construction_sites()


func _test_economy_sites_and_carriers() -> void:
	var state: GameState = EconomyDemo.build_state()
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var render: Dictionary = LayerRenderer.build_render_data(snapshot)
	assert_true(render.get("carriers", []).size() >= 1, "economy demo exposes carriers in render data")
	assert_true(render.get("tree_density", []).size() >= 1, "economy demo exposes forest density layer")
	for carrier in render.get("carriers", []):
		assert_true(float(carrier.get("q", -1)) >= 0, "carrier q resolved from road_nodes")
		assert_true(float(carrier.get("r", -1)) >= 0, "carrier r resolved from road_nodes")

	var tutorial: GameState = EconomyDemo.build_build_tutorial()
	var tutorial_render: Dictionary = LayerRenderer.build_render_data(SnapshotExporter.export_snapshot(tutorial))
	assert_eq(int(LayerRenderer.count_entities(tutorial_render).get("construction_sites", 0)), 0, "build tutorial has no pre-placed site")


func _test_economy_site_progress() -> void:
	var state: GameState = EconomyDemo.build_state()
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		var progress: int = LayerRenderer.site_progress_percent(site)
		assert_true(progress >= 0 and progress <= 100, "site progress percent in range")


func _test_military_construction_sites() -> void:
	var controller := ViewerController.new()
	controller.load_demo_state(MilitaryDemo.build_state(), "p1")
	var cmds: Array = controller.get_available_commands()
	var place_dto: Dictionary = {}
	for cmd in cmds:
		if str(cmd.get("type", "")) == "PlaceBuildingCommand":
			place_dto = cmd
			break
	assert_false(place_dto.is_empty(), "military demo has legal place building command")
	var counts_before: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	var result: CommandResult = controller.submit_command_dto(place_dto)
	assert_true(result.success, "military place building accepted")
	controller.step_ticks(1)
	var counts_after: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	assert_true(
		int(counts_after.get("construction_sites", 0)) > int(counts_before.get("construction_sites", 0)),
		"military build creates visible construction site",
	)
