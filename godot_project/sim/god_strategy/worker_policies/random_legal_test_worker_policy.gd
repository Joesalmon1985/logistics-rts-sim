class_name RandomLegalTestWorkerPolicy
extends RefCounted

## Test-only policy — picks uniformly among legal actions using seeded RNG.


static func decide(obs: Dictionary, mask: Dictionary, rng: SeededRng) -> String:
	var legal: Array[String] = []
	for action in WorkerPolicyTypes.ALL_ACTIONS:
		if WorkerPolicyTypes.is_action_legal(mask, action):
			legal.append(action)
	if legal.is_empty():
		return WorkerPolicyTypes.ACTION_IDLE
	return legal[rng.next_int(0, legal.size() - 1)]
