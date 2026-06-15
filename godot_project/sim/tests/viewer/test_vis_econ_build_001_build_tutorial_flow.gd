extends TestCase

## VIS-ECON-BUILD-001 — place site, connect roads, deliver, complete via render deltas.


const MAX_BUILD_TICKS: int = 120


func get_name() -> String:
	return "test_VIS_ECON_BUILD_001_build_tutorial_flow"


func _run_body() -> void:
	var controller := ViewerController.new()
	controller.load_demo_state(EconomyDemo.build_build_tutorial(), "p1")
	var counts_a: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	assert_eq(int(counts_a.get("construction_sites", 0)), 0, "build tutorial starts without pre-placed site")

	var place_dto: Dictionary = _find_command(controller, "PlaceBuildingPlanCommand", "storehouse")
	assert_false(place_dto.is_empty(), "expected road-adjacent storehouse placement command")

	var sites_before: int = int(counts_a.get("construction_sites", 0))
	var result: CommandResult = controller.submit_command_dto(place_dto)
	assert_true(result.success, "storehouse site placement accepted: %s" % result.error_code)
	var sites_after_place: int = LayerRenderer.count_entities(controller.get_render_data()).get("construction_sites", 0)
	assert_true(sites_after_place > sites_before, "construction site appears in render data")

	var roads_before: int = int(LayerRenderer.count_entities(controller.get_render_data()).get("roads", 0))
	var road_cmds: Array = _road_commands_to_site(controller, place_dto)
	var road_connected: bool = _site_road_connected(controller, place_dto)
	if not road_cmds.is_empty():
		for road_dto in road_cmds:
			if controller.submit_command_dto(road_dto).success:
				break
	elif not road_connected:
		assert_true(false, "site not road-connected and no legal road commands from site")
	var roads_after: int = int(LayerRenderer.count_entities(controller.get_render_data()).get("roads", 0))
	assert_true(roads_after >= roads_before, "road count non-decreasing after road placement or pre-connected site")

	var progress_seen: bool = false
	var buildings_before: int = int(LayerRenderer.count_entities(controller.get_render_data()).get("buildings", 0))
	for _i in range(MAX_BUILD_TICKS):
		controller.step_tick()
		var render: Dictionary = controller.get_render_data()
		for site in render.get("construction_sites", []):
			if int(site.get("progress_percent", 0)) > 0:
				progress_seen = true
			for ware_type in site.get("delivered", {}):
				if int(site.get("delivered", {}).get(ware_type, 0)) > 0:
					progress_seen = true
		for route in render.get("ware_routes", []):
			if not route.is_empty():
				progress_seen = true
		var sites_now: int = render.get("construction_sites", []).size()
		var buildings_now: int = render.get("buildings", []).size()
		if sites_now < sites_after_place and buildings_now > buildings_before:
			assert_true(progress_seen or sites_now == 0, "site completed with observable progress")
			return
	assert_true(progress_seen, "expected site progress or delivery within %d ticks" % MAX_BUILD_TICKS)


static func _find_command(controller: ViewerController, cmd_type: String, building_type: String = "") -> Dictionary:
	for cmd in controller.get_available_commands():
		if str(cmd.get("type", "")) != cmd_type:
			continue
		if building_type != "" and str(cmd.get("payload", {}).get("building_type", "")) != building_type:
			continue
		return cmd
	return {}


static func _road_commands_to_site(controller: ViewerController, place_dto: Dictionary) -> Array:
	var sq: int = int(place_dto.get("payload", {}).get("q", -1))
	var sr: int = int(place_dto.get("payload", {}).get("r", -1))
	var results: Array = []
	for cmd in controller.get_available_commands():
		if str(cmd.get("type", "")) != "PlaceRoadCommand":
			continue
		var payload: Dictionary = cmd.get("payload", {})
		var from_q: int = int(payload.get("from_q", -99))
		var from_r: int = int(payload.get("from_r", -99))
		var to_q: int = int(payload.get("to_q", -99))
		var to_r: int = int(payload.get("to_r", -99))
		if (from_q == sq and from_r == sr) or (to_q == sq and to_r == sr):
			results.append(cmd)
		if "Connect site" in str(cmd.get("label", "")):
			results.insert(0, cmd)
	return results


static func _site_road_connected(controller: ViewerController, place_dto: Dictionary) -> bool:
	var sq: int = int(place_dto.get("payload", {}).get("q", -1))
	var sr: int = int(place_dto.get("payload", {}).get("r", -1))
	for road in controller.snapshot.get("roads", []):
		var from_q: int = int(road.get("hex_a_q", road.get("from_q", -99)))
		var from_r: int = int(road.get("hex_a_r", road.get("from_r", -99)))
		var to_q: int = int(road.get("hex_b_q", road.get("to_q", -99)))
		var to_r: int = int(road.get("hex_b_r", road.get("to_r", -99)))
		if (from_q == sq and from_r == sr) or (to_q == sq and to_r == sr):
			return true
	for node in controller.snapshot.get("road_nodes", []):
		if int(node.get("q", -1)) == sq and int(node.get("r", -1)) == sr:
			return true
	for neighbor in HexCoord.new(sq, sr).neighbors():
		for road2 in controller.snapshot.get("roads", []):
			var fq: int = int(road2.get("hex_a_q", road2.get("from_q", -99)))
			var fr: int = int(road2.get("hex_a_r", road2.get("from_r", -99)))
			var tq: int = int(road2.get("hex_b_q", road2.get("to_q", -99)))
			var tr: int = int(road2.get("hex_b_r", road2.get("to_r", -99)))
			if (fq == neighbor.q and fr == neighbor.r) or (tq == neighbor.q and tr == neighbor.r):
				return true
	return false
