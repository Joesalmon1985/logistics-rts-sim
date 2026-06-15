import json

from python_tools.tests.conftest import require_godot
from python_tools.training.godot_env import GodotHeadlessEnv


def test_trn_smoke_001_deterministic_rollouts():
    require_godot()
    logs: list[str] = []
    with GodotHeadlessEnv() as env:
        for _ in range(10):
            resp = env.request(
                {
                    "op": "rollout_stub",
                    "scenario_path": "scenarios/economy/tiny_success_map.json",
                    "seed": 42,
                    "policy_seed": 7,
                    "max_ticks": 100,
                    "actor_id": "p1",
                }
            )
            assert resp.get("ok"), resp
            logs.append(json.dumps(resp.get("command_log", [])))
    assert len(set(logs)) == 1
