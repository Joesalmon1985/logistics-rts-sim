extends SceneTree

## RUN_05 viewer smoke runner — GOD-SMOKE-001 / GOD-SMOKE-002.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var smoke_id: String = "all"
	for arg in args:
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())

	var valid: Array[String] = ["all", "001", "002"]
	if smoke_id not in valid:
		print("VIEWER SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	var failed: bool = false
	if smoke_id == "all" or smoke_id == "001":
		failed = failed or not _run_smoke_001()
	if smoke_id == "all" or smoke_id == "002":
		failed = failed or not _run_smoke_002()

	if failed:
		print("VIEWER SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("VIEWER SMOKE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _run_smoke_001() -> bool:
	for label_variant in ["economy", "military"]:
		var state: GameState
		if label_variant == "economy":
			state = EconomyDemo.build_state()
		else:
			state = MilitaryDemo.build_state()
		var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, "p1")
		var parsed: Dictionary = SnapshotLoader.parse(snapshot)
		if not parsed.get("ok", false):
			print("GOD-SMOKE-001 FAIL: %s snapshot parse" % label_variant)
			return false
		var render: Dictionary = LayerRenderer.build_render_data(parsed.get("snapshot", {}))
		if render.get("terrain", []).is_empty():
			print("GOD-SMOKE-001 FAIL: %s empty terrain" % label_variant)
			return false
		if render.get("buildings", []).is_empty():
			print("GOD-SMOKE-001 FAIL: %s empty buildings" % label_variant)
			return false
		if label_variant == "economy":
			if render.get("roads", []).is_empty():
				print("GOD-SMOKE-001 FAIL: economy roads not in render data")
				return false
			if render.get("construction_sites", []).is_empty():
				print("GOD-SMOKE-001 FAIL: economy construction sites not in render data")
				return false
			if render.get("carriers", []).is_empty():
				print("GOD-SMOKE-001 FAIL: economy carriers not in render data")
				return false
			var progression: Dictionary = EconomyDemo.verify_progression_within_ticks(50)
			if not bool(progression.get("ok", false)):
				print("GOD-SMOKE-001 FAIL: economy demo shows no observable progression within 50 ticks")
				return false
	print("GOD-SMOKE-001 PASS: economy+military render data from snapshot")
	return true


func _run_smoke_002() -> bool:
	var controller := ViewerController.new()
	controller.load_demo_state(EconomyDemo.build_state(), "p1")
	var render_a: Dictionary = LayerRenderer.build_render_data(controller.snapshot)
	var fingerprint_a: Dictionary = _economy_fingerprint(controller.state, render_a)

	var commands: Array = controller.get_available_commands()
	if commands.is_empty():
		print("GOD-SMOKE-002 FAIL: no available commands")
		return false
	if ClientCommandAvailability.count(commands) != commands.size():
		print("GOD-SMOKE-002 FAIL: command count mismatch")
		return false

	var road_dto: Dictionary = {}
	for cmd in commands:
		if str(cmd.get("type", "")) == "PlaceRoadCommand":
			road_dto = cmd
			break
	if road_dto.is_empty():
		print("GOD-SMOKE-002 FAIL: no road command for economy delta test")
		return false
	if not controller.submit_command_dto(road_dto).success:
		print("GOD-SMOKE-002 FAIL: economy road command not accepted")
		return false
	controller.step_ticks(10)
	var render_b: Dictionary = LayerRenderer.build_render_data(controller.snapshot)
	var fingerprint_b: Dictionary = _economy_fingerprint(controller.state, render_b)
	if not _fingerprint_changed(fingerprint_a, fingerprint_b):
		print("GOD-SMOKE-002 FAIL: economy place+step showed no observable delta")
		return false

	var invalid: CommandResult = controller.submit_invalid_command()
	if invalid.success:
		print("GOD-SMOKE-002 FAIL: invalid command accepted")
		return false
	if str(invalid.error_code) == "" and str(invalid.message) == "":
		print("GOD-SMOKE-002 FAIL: invalid command missing reason")
		return false

	var base: Dictionary = SnapshotExporter.export_snapshot(controller.state)
	if base.has("available_commands"):
		print("GOD-SMOKE-002 FAIL: base export includes availability")
		return false

	if not _military_roundtrip():
		return false

	print("GOD-SMOKE-002 PASS: availability + command round-trip")
	return true


func _military_roundtrip() -> bool:
	var mil := ViewerController.new()
	mil.load_demo_state(MilitaryDemo.build_state(), "p1")
	var unit_before: Dictionary = _first_owned_unit(mil.snapshot, "p1")
	if unit_before.is_empty():
		print("GOD-SMOKE-002 FAIL: military demo missing p1 unit")
		return false
	var uq: int = int(unit_before.get("q", 0))
	var ur: int = int(unit_before.get("r", 0))
	var unit_id: int = int(unit_before.get("id", 0))

	var move_dto: Dictionary = {}
	for cmd in mil.get_available_commands():
		if str(cmd.get("type", "")) == "MoveUnitCommand":
			if int(cmd.get("payload", {}).get("unit_id", 0)) == unit_id:
				move_dto = cmd
				break
	if move_dto.is_empty():
		print("GOD-SMOKE-002 FAIL: no move command for military unit")
		return false
	if not mil.submit_command_dto(move_dto).success:
		print("GOD-SMOKE-002 FAIL: military move not accepted")
		return false
	mil.step_ticks(5)
	var unit_after: Dictionary = _find_unit(mil.snapshot, unit_id)
	if int(unit_after.get("q", uq)) == uq and int(unit_after.get("r", ur)) == ur:
		print("GOD-SMOKE-002 FAIL: military unit position unchanged after move+steps")
		return false

	var attack_dto: Dictionary = {}
	for cmd in mil.get_available_commands():
		if str(cmd.get("type", "")) == "AttackUnitCommand":
			attack_dto = cmd
			break
	if attack_dto.is_empty():
		print("GOD-SMOKE-002 FAIL: no attack command enumerated")
		return false
	var target_id: int = int(attack_dto.get("payload", {}).get("target_id", 0))
	var target_before: Dictionary = _find_entity(mil.snapshot, target_id)
	var target_hp_before: int = int(target_before.get("hp", -1))
	if not mil.submit_command_dto(attack_dto).success:
		print("GOD-SMOKE-002 FAIL: attack command not accepted")
		return false
	mil.step_ticks(20)
	var target_after: Dictionary = _find_entity(mil.snapshot, target_id)
	var target_hp_after: int = int(target_after.get("hp", -1))
	var combat_event: bool = _has_event_substring(mil.state, "damage") or _has_event_substring(mil.state, "combat") or _has_event_substring(mil.state, "order")
	if target_hp_before >= 0 and target_hp_after >= 0 and target_hp_after >= target_hp_before and not combat_event:
		print("GOD-SMOKE-002 FAIL: attack did not reduce target HP or emit combat/damage/order event")
		return false

	var place_dto: Dictionary = {}
	for cmd in mil.get_available_commands():
		if str(cmd.get("type", "")) == "PlaceBuildingCommand":
			place_dto = cmd
			break
	if not place_dto.is_empty():
		var sites_before: int = LayerRenderer.count_entities(mil.get_render_data()).get("construction_sites", 0)
		if mil.submit_command_dto(place_dto).success:
			var sites_after: int = LayerRenderer.count_entities(mil.get_render_data()).get("construction_sites", 0)
			if sites_after <= sites_before:
				print("GOD-SMOKE-002 FAIL: military build did not increase construction site render count")
				return false
	return true


func _economy_fingerprint(state: GameState, render: Dictionary) -> Dictionary:
	var delivered_total: int = 0
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		for ware_type in site.get("delivered", {}):
			delivered_total += int(site.get("delivered", {}).get(ware_type, 0))
	return {
		"roads": int(LayerRenderer.count_entities(render).get("roads", 0)),
		"construction_sites": render.get("construction_sites", []).size(),
		"carriers": render.get("carriers", []).size(),
		"delivered_total": delivered_total,
		"event_count": state.event_log.events.size(),
	}


func _fingerprint_changed(a: Dictionary, b: Dictionary) -> bool:
	for key in a.keys():
		if int(a.get(key, 0)) != int(b.get(key, 0)):
			return true
	return false


func _first_owned_unit(snapshot: Dictionary, player_id: String) -> Dictionary:
	for unit in snapshot.get("units", []):
		if str(unit.get("player_id", "")) == player_id:
			return unit
	return {}


func _find_unit(snapshot: Dictionary, unit_id: int) -> Dictionary:
	for unit in snapshot.get("units", []):
		if int(unit.get("id", -1)) == unit_id:
			return unit
	return {}


func _find_entity(snapshot: Dictionary, entity_id: int) -> Dictionary:
	for unit in snapshot.get("units", []):
		if int(unit.get("id", -1)) == entity_id:
			return unit
	for building in snapshot.get("buildings", []):
		if int(building.get("id", -1)) == entity_id:
			return building
	return {}


func _has_event_substring(state: GameState, substring: String) -> bool:
	for event in state.event_log.events:
		if substring in str(event.type).to_lower():
			return true
	return false
