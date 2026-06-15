from python_tools.tests.conftest import require_godot
from python_tools.training.godot_env import GodotHeadlessEnv


def test_trn_smoke_002_replay_equivalence():
    require_godot()
    replay_path: str
    live_hash: str
    with GodotHeadlessEnv() as env:
        rollout = env.request(
            {
                "op": "rollout_stub",
                "scenario_path": "scenarios/economy/tiny_success_map.json",
                "seed": 42,
                "policy_seed": 7,
                "max_ticks": 100,
                "actor_id": "p1",
            }
        )
        assert rollout.get("ok"), rollout
        live_hash = rollout["hash"]
        replay_path = (env.session_dir / "replay_verify.json").as_posix()
        save = env.request({"op": "save_replay", "path": replay_path})
        assert save.get("ok"), save

    with GodotHeadlessEnv() as verify_env:
        verify = verify_env.request({"op": "verify_replay", "path": replay_path})
        assert verify.get("ok"), verify
        assert verify["verify"]["final_hash"] == live_hash
