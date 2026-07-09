class_name DeterministicWorkerPolicy
extends RefCounted

## Default needs-driven worker policy — legal fallback per PRD §15.


static func decide(obs: Dictionary, mask: Dictionary) -> String:
	var hunger: int = int(obs.get("hunger", WorkerNeedsSystem.NEED_SCALE))
	var tiredness: int = int(obs.get("tiredness", 0))
	if hunger < 350 and WorkerPolicyTypes.is_action_legal(mask, WorkerPolicyTypes.ACTION_SEEK_FOOD):
		return WorkerPolicyTypes.ACTION_SEEK_FOOD
	if tiredness > 700 and WorkerPolicyTypes.is_action_legal(mask, WorkerPolicyTypes.ACTION_REST):
		return WorkerPolicyTypes.ACTION_REST
	if int(obs.get("happiness", WorkerNeedsSystem.NEED_SCALE)) < 300 and WorkerPolicyTypes.is_action_legal(mask, WorkerPolicyTypes.ACTION_SEEK_LEISURE):
		return WorkerPolicyTypes.ACTION_SEEK_LEISURE
	if int(obs.get("safety", WorkerNeedsSystem.NEED_SCALE)) < 300 and WorkerPolicyTypes.is_action_legal(mask, WorkerPolicyTypes.ACTION_SEEK_SAFETY):
		return WorkerPolicyTypes.ACTION_SEEK_SAFETY
	if WorkerPolicyTypes.is_action_legal(mask, WorkerPolicyTypes.ACTION_WORK_ASSIGNED_JOB):
		return WorkerPolicyTypes.ACTION_WORK_ASSIGNED_JOB
	return WorkerPolicyTypes.ACTION_IDLE
