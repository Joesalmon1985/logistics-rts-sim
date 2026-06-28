"""Hybrid 4P training environment client (extends RUN_06 transport)."""

from __future__ import annotations

from python_tools.training.godot_env import GodotHeadlessEnv

HYBRID_SCENARIO = "scenarios/hybrid/hybrid_4p_training_ffa.json"


class HybridTrainingEnv:
    def __init__(self) -> None:
        self._env: GodotHeadlessEnv | None = None
        self._snapshot: dict = {}

    def reset(self, seed: int = 42, stage: int = 0) -> dict:
        self.close()
        self._env = GodotHeadlessEnv()
        resp = self._env.request(
            {
                "op": "reset",
                "scenario_path": HYBRID_SCENARIO,
                "seed": seed,
                "actor_id": "p1",
            }
        )
        if not resp.get("ok"):
            raise RuntimeError(f"hybrid reset failed: {resp}")
        self._snapshot = resp.get("snapshot", {})
        obs = {
            "stage": stage,
            "legal_action_mask": [1] * 16,
            "snapshot": self._snapshot,
        }
        return obs

    def step(self, action: int, player_id: str = "p1") -> tuple[dict, float, bool, dict]:
        if self._env is None:
            raise RuntimeError("env not reset")
        step = self._env.request({"op": "step", "macro_action_id": action, "actor_id": player_id})
        if not step.get("ok"):
            raise RuntimeError(f"hybrid step failed: {step}")
        self._snapshot = step.get("snapshot", self._snapshot)
        done = bool(step.get("done", False))
        obs = {"legal_action_mask": [1] * 16, "snapshot": self._snapshot}
        reward = 0.01 if step.get("accepted") else -0.05
        return obs, reward, done, {"player_id": player_id}

    def close(self) -> None:
        if self._env is not None:
            self._env.close()
            self._env = None
