class_name WorkerPolicyTypes
extends RefCounted

## Worker policy action vocabulary and observation DTOs.

const ACTION_WORK_ASSIGNED_JOB: String = "WORK_ASSIGNED_JOB"
const ACTION_SEEK_FOOD: String = "SEEK_FOOD"
const ACTION_REST: String = "REST"
const ACTION_SEEK_LEISURE: String = "SEEK_LEISURE"
const ACTION_SEEK_SAFETY: String = "SEEK_SAFETY"
const ACTION_IDLE: String = "IDLE"

const ALL_ACTIONS: Array[String] = [
	ACTION_WORK_ASSIGNED_JOB,
	ACTION_SEEK_FOOD,
	ACTION_REST,
	ACTION_SEEK_LEISURE,
	ACTION_SEEK_SAFETY,
	ACTION_IDLE,
]


static func build_observation(state: GameState, player_id: String, worker_type: String) -> Dictionary:
	var cohort: Dictionary = WorkerNeedsSystem.get_cohort(state, player_id, worker_type)
	if cohort.is_empty():
		cohort = {
			"hunger": WorkerNeedsSystem.NEED_SCALE,
			"tiredness": 0,
			"happiness": WorkerNeedsSystem.NEED_SCALE,
			"safety": WorkerNeedsSystem.NEED_SCALE,
		}
	var traits: Dictionary = WorkerSelectionSystem.get_cohort_traits(state, player_id, worker_type)
	return {
		"player_id": player_id,
		"worker_type": worker_type,
		"tick": state.tick,
		"hunger": int(cohort.get("hunger", WorkerNeedsSystem.NEED_SCALE)),
		"tiredness": int(cohort.get("tiredness", 0)),
		"happiness": int(cohort.get("happiness", WorkerNeedsSystem.NEED_SCALE)),
		"safety": int(cohort.get("safety", WorkerNeedsSystem.NEED_SCALE)),
		"traits": traits,
	}


static func build_legal_mask(obs: Dictionary) -> Dictionary:
	var mask: Dictionary = {}
	for action in ALL_ACTIONS:
		mask[action] = 1
	if int(obs.get("hunger", WorkerNeedsSystem.NEED_SCALE)) >= WorkerNeedsSystem.NEED_SCALE - 100:
		mask[ACTION_SEEK_FOOD] = 0
	if int(obs.get("tiredness", 0)) < 200:
		mask[ACTION_REST] = 0
	return mask


static func is_action_legal(mask: Dictionary, action: String) -> bool:
	return int(mask.get(action, 0)) > 0
