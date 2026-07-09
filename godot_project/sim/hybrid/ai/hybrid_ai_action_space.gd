class_name HybridAiActionSpace
extends RefCounted

## Macro-action space for hybrid neural/modular bots (PRD A.8).

const MACRO_NOOP := 0
const MACRO_BUILD_WOOD_ECONOMY := 1
const MACRO_BUILD_STONE_ECONOMY := 2
const MACRO_BUILD_FORESTER := 3
const MACRO_BUILD_SAWMILL := 4
const MACRO_CONNECT_SITE := 5
const MACRO_BUILD_BARRACKS := 6
const MACRO_BUILD_GUARD_POST := 7
const MACRO_QUEUE_RIFLE := 8
const MACRO_DEFEND_BASE := 9
const MACRO_ATTACK_POSTURE := 10
const MACRO_ATTACK_WEAKEST := 11
const MACRO_EXPAND_RESOURCES := 12
const MACRO_REPAIR_ROAD := 13
const MACRO_INC_MILITARY := 14
const MACRO_INC_ECONOMY := 15

const MACRO_COUNT := 16

const MACRO_NAMES: Array[String] = [
	"noop_wait",
	"build_wood_economy",
	"build_stone_economy",
	"build_forester_support",
	"build_sawmill",
	"connect_nearest_important_site",
	"build_barracks",
	"build_guard_post",
	"queue_rifle_squad",
	"set_defend_base_posture",
	"set_attack_visible_enemy_posture",
	"attack_weakest_visible_enemy",
	"expand_toward_nearest_resource_cluster",
	"repair_or_replace_disconnected_road",
	"increase_military_priority",
	"increase_economy_priority",
]


static func macro_name(macro_id: int) -> String:
	if macro_id < 0 or macro_id >= MACRO_NAMES.size():
		return "unknown"
	return MACRO_NAMES[macro_id]


static func legal_mask(state: GameState, player_id: String, snapshot: Dictionary) -> PackedInt32Array:
	var mask := PackedInt32Array()
	mask.resize(MACRO_COUNT)
	for i in range(MACRO_COUNT):
		mask[i] = 1 if _precondition_pass(state, player_id, snapshot, i) else 0
	return mask


static func compile_command(
	state: GameState,
	player_id: String,
	snapshot: Dictionary,
	macro_id: int,
	tick: int,
) -> Dictionary:
	if macro_id < 0 or macro_id >= MACRO_COUNT:
		return {"ok": false, "reason": "invalid_macro_id"}
	if _precondition_pass(state, player_id, snapshot, macro_id) == false:
		return {"ok": false, "reason": "precondition_failed"}
	var cmd: SimCommand = _build_command(state, player_id, snapshot, macro_id, tick)
	if cmd == null:
		return {"ok": false, "reason": "compile_null"}
	return {"ok": true, "command": cmd, "macro_id": macro_id, "macro_name": macro_name(macro_id)}


static func _precondition_pass(
	_state: GameState,
	_player_id: String,
	_snapshot: Dictionary,
	macro_id: int,
) -> bool:
	match macro_id:
		MACRO_NOOP:
			return true
		MACRO_BUILD_WOOD_ECONOMY, MACRO_BUILD_STONE_ECONOMY, MACRO_BUILD_FORESTER, MACRO_BUILD_SAWMILL:
			return true
		MACRO_CONNECT_SITE, MACRO_REPAIR_ROAD, MACRO_EXPAND_RESOURCES:
			return true
		MACRO_BUILD_BARRACKS, MACRO_BUILD_GUARD_POST, MACRO_QUEUE_RIFLE:
			return true
		MACRO_DEFEND_BASE, MACRO_ATTACK_POSTURE, MACRO_ATTACK_WEAKEST:
			return _has_visible_enemy(_snapshot) or macro_id == MACRO_DEFEND_BASE
		MACRO_INC_MILITARY, MACRO_INC_ECONOMY:
			return true
	return false


static func _has_visible_enemy(snapshot: Dictionary) -> bool:
	for building in snapshot.get("military_buildings", []):
		if str(building.get("player_id", "")) != str(snapshot.get("player_id", "")):
			return true
	for unit in snapshot.get("units", []):
		if str(unit.get("player_id", "")) != str(snapshot.get("player_id", "")):
			return true
	return false


static func _hq_hex(snapshot: Dictionary, player_id: String) -> Vector2i:
	for building in snapshot.get("buildings", []):
		if str(building.get("player_id", "")) == player_id:
			if str(building.get("building_type", "")) in ["headquarters", "command_hub"]:
				return Vector2i(
					int(building.get("anchor_q", building.get("q", 2))),
					int(building.get("anchor_r", building.get("r", 2))),
				)
	return Vector2i(2, 2)


static func _build_command(
	_state: GameState,
	player_id: String,
	snapshot: Dictionary,
	macro_id: int,
	tick: int,
) -> SimCommand:
	var hq: Vector2i = _hq_hex(snapshot, player_id)
	match macro_id:
		MACRO_NOOP:
			return NoopCommand.new(player_id, tick)
		MACRO_BUILD_WOOD_ECONOMY:
			return PlaceBuildingPlanCommand.new(player_id, tick, {
				"building_type": "woodcutter",
				"q": hq.x + 3,
				"r": hq.y,
				"player_id": player_id,
			})
		MACRO_BUILD_STONE_ECONOMY:
			return PlaceBuildingPlanCommand.new(player_id, tick, {
				"building_type": "quarry",
				"q": hq.x + 4,
				"r": hq.y + 1,
				"player_id": player_id,
			})
		MACRO_BUILD_FORESTER:
			return PlaceBuildingPlanCommand.new(player_id, tick, {
				"building_type": "forester",
				"q": hq.x + 2,
				"r": hq.y - 1,
				"player_id": player_id,
			})
		MACRO_BUILD_SAWMILL:
			return PlaceBuildingPlanCommand.new(player_id, tick, {
				"building_type": "sawmill",
				"q": hq.x + 5,
				"r": hq.y,
				"player_id": player_id,
			})
		MACRO_CONNECT_SITE:
			return PlaceRoadCommand.new(player_id, tick, {
				"from_q": hq.x,
				"from_r": hq.y,
				"to_q": hq.x + 1,
				"to_r": hq.y,
			})
		MACRO_BUILD_BARRACKS:
			return PlaceBuildingPlanCommand.new(player_id, tick, {
				"building_type": "barracks",
				"q": hq.x + 6,
				"r": hq.y + 1,
				"player_id": player_id,
			})
		MACRO_BUILD_GUARD_POST:
			return PlaceBuildingPlanCommand.new(player_id, tick, {
				"building_type": "guard_post",
				"q": hq.x + 5,
				"r": hq.y - 1,
				"player_id": player_id,
			})
		MACRO_QUEUE_RIFLE:
			var bid: int = _building_id(snapshot, player_id, "barracks")
			if bid <= 0:
				return null
			return QueueUnitCommand.new(player_id, tick, {
				"building_id": bid,
				"unit_type": "rifle_squad",
			})
		MACRO_DEFEND_BASE:
			return SetBuildingStanceCommand.new(player_id, tick, {
				"stance": "defend",
				"player_id": player_id,
			})
		MACRO_ATTACK_POSTURE:
			return SetBuildingStanceCommand.new(player_id, tick, {
				"stance": "attack",
				"player_id": player_id,
			})
		MACRO_ATTACK_WEAKEST:
			for building in snapshot.get("military_buildings", []):
				if str(building.get("player_id", "")) != player_id and str(building.get("building_type", "")) == "command_hub":
					return DelegatedAttackCommand.new(player_id, tick, {
						"target_q": int(building.get("anchor_q", building.get("q", 0))),
						"target_r": int(building.get("anchor_r", building.get("r", 0))),
					})
			return null
		MACRO_EXPAND_RESOURCES:
			return PlaceRoadCommand.new(player_id, tick, {
				"from_q": hq.x,
				"from_r": hq.y,
				"to_q": hq.x + 2,
				"to_r": hq.y,
			})
		MACRO_REPAIR_ROAD:
			return SetRepairPriorityCommand.new(player_id, tick, {"priority": "roads", "player_id": player_id})
		MACRO_INC_MILITARY:
			return SetWorkerPolicyCommand.new(player_id, tick, {"policy": "military_focus", "player_id": player_id})
		MACRO_INC_ECONOMY:
			return SetWorkerPolicyCommand.new(player_id, tick, {"policy": "auto", "player_id": player_id})
	return null


static func _building_id(snapshot: Dictionary, player_id: String, building_type: String) -> int:
	for building in snapshot.get("military_buildings", []):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	return 0
