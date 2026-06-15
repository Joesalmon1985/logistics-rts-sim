"""Episode rollout helpers."""

from __future__ import annotations

from typing import Any

from python_tools.training import metrics
from python_tools.training.godot_env import GodotHeadlessEnv
from python_tools.training.policy import Policy, action_from_policy


def run_episode(
    env: GodotHeadlessEnv,
    policy: Policy,
    *,
    scenario_path: str,
    seed: int,
    variant: str,
    max_ticks: int,
    actor_id: str = "p1",
    deterministic: bool = True,
    fog_enabled: int | None = None,
    include_hash: bool = False,
) -> dict[str, Any]:
    reset_req: dict[str, Any] = {
        "op": "reset",
        "scenario_path": scenario_path,
        "seed": seed,
        "actor_id": actor_id,
    }
    if fog_enabled is not None:
        reset_req["fog_enabled"] = fog_enabled
    reset = env.request(reset_req)
    if not reset.get("ok"):
        raise RuntimeError(f"reset failed: {reset}")

    events: list[dict[str, Any]] = []
    command_log: list[dict[str, Any]] = []
    snapshot: dict[str, Any] = reset.get("snapshot", {})

    for _ in range(max_ticks):
        available = snapshot.get("available_commands", [])
        tick = int(snapshot.get("tick", 0))
        command = action_from_policy(
            policy,
            snapshot,
            available,
            tick,
            deterministic=deterministic,
            actor_id=actor_id,
        )
        step_req: dict[str, Any] = {"op": "step"}
        if command is not None:
            step_req["command"] = command
        step = env.request(step_req)
        if not step.get("ok"):
            raise RuntimeError(f"step failed: {step}")
        if step.get("accepted") and command is not None:
            command_log.append(command)
        events.extend(step.get("events", []))
        snapshot = step.get("snapshot", snapshot)
        if step.get("terminal", {}).get("done"):
            break

    result_metrics = metrics.compute_metrics(variant, snapshot, events)
    episode_hash = ""
    if include_hash:
        hash_resp = env.request({"op": "state_hash"})
        episode_hash = hash_resp.get("hash", "")
    return {
        "metrics": result_metrics,
        "events": events,
        "command_log": command_log,
        "hash": episode_hash,
        "final_snapshot": snapshot,
    }
