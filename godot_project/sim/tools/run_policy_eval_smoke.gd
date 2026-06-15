extends SceneTree

## RUN_06b policy evaluation smoke — EVAL-SMOKE-001/002 and contract gates.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ServerScript := preload("res://sim/tools/training_env_server.gd")
const RunnerScript := preload("res://sim/tools/policy_eval_runner.gd")

const POLICIES: Array[String] = [
	"pass_only",
	"random_legal",
	"scripted_good",
]

const OPTIONAL_POLICIES: Array[String] = ["heuristic"]

const ECONOMY_SCENARIOS: Array[String] = [
	"scenarios/economy/eval_build_obvious.json",
]

const MILITARY_SCENARIOS: Array[String] = [
	"scenarios/military/eval_focus_fire_hub.json",
]

const ECONOMY_SEEDS: Array[int] = [11]
const MILITARY_SEEDS: Array[int] = [21]

const ECONOMY_TICK_CAP: int = 40
const MILITARY_TICK_CAP: int = 120
const HEURISTIC_TICK_CAP: int = 40


func _initialize() -> void:
	BootstrapScript.load_all()
	var failed: bool = false
	failed = failed or not _run_contract_checks()
	failed = failed or not _run_eval_smoke_economy()
	failed = failed or not _run_eval_smoke_military()
	failed = failed or not _run_determinism_check()

	if failed:
		print("POLICY EVAL SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	print("POLICY EVAL SMOKE PASS")
	print("EXIT: 0 (pass)")
	call_deferred("quit", 0)


func _run_contract_checks() -> bool:
	var server := ServerScript.new()
	var reset: Dictionary = server.handle_request({
		"op": "reset",
		"scenario_path": ECONOMY_SCENARIOS[0],
		"seed": 11,
		"actor_id": "p1",
	})
	if not reset.get("ok", false):
		print("POLICY-CONTRACT FAIL: reset")
		return false

	var legal: Array = reset.get("legal_commands", [])
	if legal.is_empty():
		print("POLICY-CONTRACT-001 FAIL: empty legal list on reset")
		return false
	var has_pass: bool = false
	for entry in legal:
		if str(entry.get("type", "")) == "NoopCommand":
			has_pass = true
			break
	if not has_pass:
		print("POLICY-CONTRACT-001 FAIL: pass/noop missing from legal list")
		return false

	var obs: Dictionary = reset.get("policy_observation", {})
	if obs.is_empty() or obs.has("error"):
		print("POLICY-CONTRACT FAIL: policy observation export")
		return false

	print("POLICY-CONTRACT-001 PASS")
	return true


func _run_eval_smoke_economy() -> bool:
	var outcomes: Dictionary = {}
	for scenario_path in ECONOMY_SCENARIOS:
		for policy in POLICIES + OPTIONAL_POLICIES:
			for seed in ECONOMY_SEEDS:
				var tick_cap: int = HEURISTIC_TICK_CAP if policy in OPTIONAL_POLICIES else ECONOMY_TICK_CAP
				if policy == "scripted_good":
					tick_cap = 150
				var key: String = "%s|%s|%d" % [scenario_path, policy, seed]
				var server := ServerScript.new()
				var result: Dictionary = RunnerScript.run_episode(
					server, scenario_path, seed, policy, "p1", -1, tick_cap,
				)
				if not result.get("ok", false):
					print("EVAL-SMOKE-001 FAIL: %s" % result.get("error", "unknown"))
					return false
				var episode: Dictionary = result.get("episode_result", {})
				outcomes[key] = float(episode.get("fitness", 0.0))

	var pass_fitness: float = _mean_fitness(outcomes, "pass_only")
	var scripted_fitness: float = _mean_fitness(outcomes, "scripted_good")
	if scripted_fitness <= pass_fitness:
		print("EVAL-SMOKE-001 FAIL: scripted_good must beat pass_only (scripted=%.2f pass=%.2f)" % [scripted_fitness, pass_fitness])
		return false

	if not _policies_differ(outcomes, ECONOMY_SCENARIOS[0], ECONOMY_SEEDS[0]):
		print("EVAL-SMOKE-001 FAIL: policies produce identical outcomes")
		return false

	print("EVAL-SMOKE-001 PASS")
	return true


func _run_eval_smoke_military() -> bool:
	var outcomes: Dictionary = {}
	for scenario_path in MILITARY_SCENARIOS:
		for policy in POLICIES + OPTIONAL_POLICIES:
			for seed in MILITARY_SEEDS:
				var tick_cap: int = HEURISTIC_TICK_CAP if policy in OPTIONAL_POLICIES else MILITARY_TICK_CAP
				if policy == "scripted_good":
					tick_cap = 250
				var key: String = "%s|%s|%d" % [scenario_path, policy, seed]
				var server := ServerScript.new()
				var result: Dictionary = RunnerScript.run_episode(
					server, scenario_path, seed, policy, "p1", 0, tick_cap,
				)
				if not result.get("ok", false):
					print("EVAL-SMOKE-002 FAIL: %s" % result.get("error", "unknown"))
					return false
				var episode: Dictionary = result.get("episode_result", {})
				outcomes[key] = float(episode.get("fitness", 0.0))

	var pass_fitness: float = _mean_fitness(outcomes, "pass_only")
	var scripted_fitness: float = _mean_fitness(outcomes, "scripted_good")
	if scripted_fitness <= pass_fitness:
		print("EVAL-SMOKE-002 FAIL: scripted_good must beat pass_only (scripted=%.2f pass=%.2f)" % [scripted_fitness, pass_fitness])
		return false

	if not _policies_differ(outcomes, MILITARY_SCENARIOS[0], MILITARY_SEEDS[0]):
		print("EVAL-SMOKE-002 FAIL: policies produce identical outcomes")
		return false

	print("EVAL-SMOKE-002 PASS")
	return true


func _run_determinism_check() -> bool:
	var scenario_path: String = MILITARY_SCENARIOS[0]
	var seed: int = 21
	var hashes: Array[String] = []
	for _i in range(2):
		var server := ServerScript.new()
		var result: Dictionary = RunnerScript.run_episode(
			server, scenario_path, seed, "pass_only", "p1", 0, MILITARY_TICK_CAP,
		)
		if not result.get("ok", false):
			print("EVAL-DETERMINISM-001 FAIL: episode error")
			return false
		hashes.append(str(result.get("hash", "")))
	if hashes[0] != hashes[1]:
		print("EVAL-DETERMINISM-001 FAIL: hash mismatch")
		return false
	print("EVAL-DETERMINISM-001 PASS")
	return true


func _mean_fitness(outcomes: Dictionary, policy: String) -> float:
	var total: float = 0.0
	var count: int = 0
	for key in outcomes.keys():
		var parts: PackedStringArray = key.split("|")
		if parts.size() >= 2 and parts[1] == policy:
			total += float(outcomes[key])
			count += 1
	return total / float(maxi(1, count))


func _policies_differ(outcomes: Dictionary, scenario_path: String, seed: int) -> bool:
	var values: Dictionary = {}
	for policy in POLICIES:
		var key: String = "%s|%s|%d" % [scenario_path, policy, seed]
		if outcomes.has(key):
			values[policy] = outcomes[key]
	var uniq: Array = []
	for policy in values.keys():
		var fitness: float = values[policy]
		if fitness not in uniq:
			uniq.append(fitness)
	return uniq.size() > 1
