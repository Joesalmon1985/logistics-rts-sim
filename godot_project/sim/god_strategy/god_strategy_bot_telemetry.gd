class_name GodStrategyBotTelemetry
extends RefCounted

var player_id: String = "p1"
var furthest_phase: String = "init"
var phase_history: Array = []
var milestones: Dictionary = {
	"worker_policy_set": false,
	"road_segment_placed": false,
	"woodcutter_placed": false,
	"sawmill_placed": false,
	"barracks_placed": false,
	"unit_queued": false,
	"unit_produced": false,
	"attack_issued": false,
	"enemy_hub_damaged": false,
}
var _history_cap: int = 200


func reset(p_player_id: String) -> void:
	player_id = p_player_id
	furthest_phase = "init"
	phase_history.clear()
	for key in milestones.keys():
		milestones[key] = false


func record_phase(tick: int, phase: String, intent: String, rejection: String = "") -> void:
	_advance_furthest(phase)
	if phase_history.is_empty() or str(phase_history[-1].get("phase", "")) != phase:
		phase_history.append({
			"tick": tick,
			"phase": phase,
			"intent": intent,
			"rejection": rejection,
		})
		if phase_history.size() > _history_cap:
			phase_history.pop_front()


func on_command_applied(command: SimCommand, result: CommandResult) -> void:
	if command == null:
		return
	if not result.success:
		return
	match command.type:
		"SetWorkerPolicyCommand":
			milestones["worker_policy_set"] = true
		"PlaceRoadCommand":
			milestones["road_segment_placed"] = true
		"PlaceBuildingPlanCommand":
			var btype: String = str(command.payload.get("building_type", ""))
			if btype == "woodcutter":
				milestones["woodcutter_placed"] = true
			elif btype == "sawmill":
				milestones["sawmill_placed"] = true
			elif btype == "barracks":
				milestones["barracks_placed"] = true
		"QueueUnitCommand":
			milestones["unit_queued"] = true
		"DelegatedAttackCommand":
			milestones["attack_issued"] = true


func update_from_snapshot(snapshot: Dictionary, state: GameState) -> void:
	if _has_building(snapshot, "woodcutter"):
		milestones["woodcutter_placed"] = true
	if _has_building(snapshot, "sawmill"):
		milestones["sawmill_placed"] = true
	if _has_building(snapshot, "barracks"):
		milestones["barracks_placed"] = true
	if _count_units(snapshot, "rifle_squad") > 0:
		milestones["unit_produced"] = true
	if state != null:
		for event in state.event_log.events:
			if str(event.type) == "damage_dealt":
				milestones["enemy_hub_damaged"] = true
	_update_furthest_from_milestones()


func get_progression_report(current: Dictionary) -> Dictionary:
	_update_furthest_from_milestones()
	return {
		"player_id": player_id,
		"furthest_phase": furthest_phase,
		"phase": str(current.get("phase", "")),
		"intent": str(current.get("intent", "")),
		"last_rejection_reason": str(current.get("last_rejection_reason", "")),
		"stall_ticks_in_phase": int(current.get("stall_ticks_in_phase", 0)),
		"commands_rejected_count": int(current.get("commands_rejected_count", 0)),
		"plan_index": int(current.get("plan_index", 0)),
		"target_q": int(current.get("target_q", -1)),
		"target_r": int(current.get("target_r", -1)),
		"milestones": milestones.duplicate(true),
		"phase_history": phase_history.duplicate(true),
	}


func past_barracks_phase() -> bool:
	return _phase_index(furthest_phase) >= _phase_index("barracks")


func _has_building(snapshot: Dictionary, building_type: String) -> bool:
	for b in snapshot.get("buildings", []):
		if str(b.get("building_type", "")) == building_type and str(b.get("player_id", "")) == player_id:
			return true
	for b in snapshot.get("military_buildings", []):
		if str(b.get("building_type", "")) == building_type and str(b.get("player_id", "")) == player_id:
			return true
	for s in snapshot.get("construction_sites", []):
		if str(s.get("building_type", "")) == building_type and str(s.get("player_id", "")) == player_id:
			return true
	return false


func _count_units(snapshot: Dictionary, unit_type: String) -> int:
	var count: int = 0
	for unit in snapshot.get("units", []):
		if str(unit.get("unit_type", "")) == unit_type and str(unit.get("player_id", "")) == player_id:
			count += 1
	return count


func _advance_furthest(phase: String) -> void:
	if _phase_index(phase) > _phase_index(furthest_phase):
		furthest_phase = phase


func _update_furthest_from_milestones() -> void:
	if milestones["attack_issued"] or milestones["enemy_hub_damaged"]:
		_advance_furthest("attack")
	if milestones["unit_produced"]:
		_advance_furthest("unit_produced")
	if milestones["unit_queued"]:
		_advance_furthest("queue_unit")
	if milestones["barracks_placed"]:
		_advance_furthest("barracks")
	if milestones["sawmill_placed"]:
		_advance_furthest("sawmill")
	if milestones["woodcutter_placed"]:
		_advance_furthest("woodcutter")
	if milestones["road_segment_placed"]:
		_advance_furthest("roads")
	if milestones["worker_policy_set"]:
		_advance_furthest("worker_policy")


func _phase_index(phase: String) -> int:
	var idx: int = GodStrategyConstants.PHASE_ORDER.find(phase)
	if idx < 0 and phase.begins_with("place_"):
		var btype: String = phase.replace("place_", "")
		idx = GodStrategyConstants.PHASE_ORDER.find(btype)
	if idx < 0 and phase == "wait_construction":
		idx = _phase_index("roads")
	if idx < 0 and phase == "wait_production":
		idx = _phase_index("barracks")
	return maxi(idx, 0)
