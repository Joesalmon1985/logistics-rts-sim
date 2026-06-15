"""RUN_06b policy evaluation harness and reporting."""

from __future__ import annotations

import csv
import json
from pathlib import Path
from typing import Any

from python_tools.training import eval_config
from python_tools.training.godot_env import GodotHeadlessEnv
from python_tools.training.scoring_policy import ScoringPolicy, build_scoring_policy


def _command_key(entry: dict[str, Any]) -> tuple[str, str]:
    payload = dict(entry.get("payload", {}))
    payload.pop("tick", None)
    return str(entry.get("type", "")), json.dumps(payload, sort_keys=True)


def _is_legal_choice(chosen: dict[str, Any], legal_commands: list[dict[str, Any]]) -> bool:
    key = _command_key(chosen)
    return any(_command_key(command) == key for command in legal_commands)


def _command_from_entry(entry: dict[str, Any], tick: int, actor_id: str) -> dict[str, Any]:
    payload = dict(entry.get("payload", {}))
    payload["tick"] = tick
    return {
        "type": str(entry.get("type", "")),
        "player_id": str(entry.get("actor_id", actor_id)),
        "tick": tick,
        "payload": payload,
    }


def run_baseline_episode(
    env: GodotHeadlessEnv,
    *,
    scenario_path: str,
    seed: int,
    policy_kind: str,
    actor_id: str = "p1",
    fog_enabled: int | None = None,
    max_ticks: int | None = None,
) -> dict[str, Any]:
    req: dict[str, Any] = {
        "op": "play_episode",
        "scenario_path": scenario_path,
        "seed": seed,
        "actor_id": actor_id,
        "policy_kind": policy_kind,
        "policy_seed": seed * 17 + 3,
    }
    if fog_enabled is not None:
        req["fog_enabled"] = fog_enabled
    if max_ticks is not None:
        req["max_ticks"] = max_ticks
    result = env.request(req)
    if not result.get("ok"):
        raise RuntimeError(f"play_episode failed: {result}")
    return result


def run_neural_scoring_episode(
    env: GodotHeadlessEnv,
    policy: ScoringPolicy,
    *,
    scenario_path: str,
    seed: int,
    actor_id: str = "p1",
    fog_enabled: int | None = None,
    max_ticks: int = 500,
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

    command_log: list[dict[str, Any]] = []
    observation: dict[str, Any] = reset.get("policy_observation", {})
    tick = int(reset.get("tick", 0))

    for _ in range(max_ticks):
        legal_resp = env.request({"op": "legal_commands"})
        legal_commands: list[dict[str, Any]] = legal_resp.get("legal_commands", [])
        if not legal_commands:
            raise RuntimeError("empty legal_commands")
        chosen = policy.select_command(observation, legal_commands)
        if not _is_legal_choice(chosen, legal_commands):
            raise RuntimeError("policy selected command not in legal_commands")

        step = env.request({"op": "step", "command": _command_from_entry(chosen, tick, actor_id)})
        if not step.get("ok"):
            raise RuntimeError(f"step failed: {step}")
        if step.get("accepted") and str(chosen.get("type", "")) != "NoopCommand":
            command_log.append(_command_from_entry(chosen, tick, actor_id))

        observation = step.get("policy_observation", observation)
        tick = int(step.get("tick", tick))
        terminal = step.get("terminal", {})
        if bool(terminal.get("done", False)):
            episode_result = terminal.get("episode_result", {})
            hash_resp = env.request({"op": "state_hash"})
            return {
                "episode_result": episode_result,
                "command_log": command_log,
                "hash": hash_resp.get("hash", ""),
                "tick": tick,
            }

    episode_resp = env.request({"op": "episode_result"})
    hash_resp = env.request({"op": "state_hash"})
    return {
        "episode_result": episode_resp.get("episode_result", {}),
        "command_log": command_log,
        "hash": hash_resp.get("hash", ""),
        "tick": tick,
    }


def evaluate_all(output_dir: Path | None = None) -> list[dict[str, Any]]:
    root = Path(__file__).resolve().parents[2]
    output_dir = output_dir or (root / eval_config.RUN_06B_EVAL_DIR)
    command_logs_dir = output_dir / "command_logs"
    output_dir.mkdir(parents=True, exist_ok=True)
    command_logs_dir.mkdir(parents=True, exist_ok=True)

    rows: list[dict[str, Any]] = []
    scenarios = [
        ("economy_trial", eval_config.ECONOMY_EVAL_SCENARIOS, eval_config.ECONOMY_EVAL_SEEDS, None),
        ("military_trial", eval_config.MILITARY_EVAL_SCENARIOS, eval_config.MILITARY_EVAL_SEEDS, 0),
    ]

    for variant, scenario_paths, seeds, fog in scenarios:
        for scenario_path in scenario_paths:
            for seed in seeds:
                for policy_kind in eval_config.POLICY_KINDS:
                    with GodotHeadlessEnv() as env:
                        if policy_kind == "neural_random":
                            policy = build_scoring_policy("neural_random", seed=seed, variant=variant)
                            episode = run_neural_scoring_episode(
                                env,
                                policy,
                                scenario_path=scenario_path,
                                seed=seed,
                                fog_enabled=fog,
                            )
                        else:
                            episode = run_baseline_episode(
                                env,
                                scenario_path=scenario_path,
                                seed=seed,
                                policy_kind=policy_kind,
                                fog_enabled=fog,
                            )

                    episode_result = episode.get("episode_result", {})
                    row = {
                        "variant": variant,
                        "scenario_path": scenario_path,
                        "seed": seed,
                        "policy_kind": policy_kind,
                        "outcome": str(episode_result.get("outcome", "")),
                        "fitness": float(episode_result.get("fitness", 0.0)),
                        "hash": str(episode.get("hash", "")),
                        "tick": int(episode.get("tick", 0)),
                    }
                    rows.append(row)
                    log_name = f"{variant}_{Path(scenario_path).stem}_{policy_kind}_{seed}.json"
                    (command_logs_dir / log_name).write_text(
                        json.dumps(episode.get("command_log", []), indent=2),
                        encoding="utf-8",
                    )

    write_report(output_dir / "report.csv", rows)
    summary = summarize_rows(rows)
    (output_dir / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    return rows


def write_report(path: Path, rows: list[dict[str, Any]]) -> None:
    if not rows:
        path.write_text("", encoding="utf-8")
        return
    fieldnames = list(rows[0].keys())
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def summarize_rows(rows: list[dict[str, Any]]) -> dict[str, Any]:
    by_policy: dict[str, list[float]] = {}
    for row in rows:
        by_policy.setdefault(str(row["policy_kind"]), []).append(float(row["fitness"]))
    return {
        "policy_mean_fitness": {
            policy: (sum(values) / len(values) if values else 0.0)
            for policy, values in by_policy.items()
        },
        "row_count": len(rows),
    }


if __name__ == "__main__":
    evaluate_all()
