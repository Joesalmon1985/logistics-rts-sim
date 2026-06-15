"""Export visual rollout artefacts for Godot observer replay (RUN_02.5)."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from python_tools.training import eval_config, metrics
from python_tools.training.command_match import find_index, index_to_command
from python_tools.training.godot_env import GodotHeadlessEnv, repo_root
from python_tools.training.policy import RandomPolicy, SeededStubPolicy, action_from_policy, build_policy
from python_tools.training import rollout_artefact as artefact

try:
    import torch
except ImportError:  # pragma: no cover
    torch = None


def _game_type_from_variant(variant: str) -> str:
    if "military" in variant:
        return "military"
    return "economy"


def _bot_kind_from_variant(variant: str) -> str:
    return "military" if "military" in variant else "economy"


def _resolve_checkpoint(variant: str) -> tuple[Path | None, bool, str | None]:
    name = "economy_policy.pt" if "economy" in variant else "military_policy.pt"
    path = repo_root() / eval_config.CHECKPOINT_DIR / name
    if path.is_file():
        return path, True, None
    return None, False, f"checkpoint not found at {path.as_posix()}"


def _load_policy(policy_type: str, variant: str, seed: int):
    if policy_type == "random":
        return RandomPolicy(seed * 17 + 3, variant), None
    if policy_type == "untrained":
        policy = build_policy(variant)
        return policy, None
    if policy_type == "trained":
        ckpt_path, available, reason = _resolve_checkpoint(variant)
        if not available:
            raise FileNotFoundError(reason or "trained checkpoint missing")
        if torch is None:
            raise RuntimeError("torch required for trained policy export")
        policy = build_policy(variant)
        data = torch.load(ckpt_path, map_location="cpu", weights_only=False)
        policy.load_state_dict(data["state_dict"])
        policy.eval()
        return policy, str(ckpt_path)
    raise ValueError(f"unsupported policy_type for python export: {policy_type}")


def _request_debug_snapshot(env: GodotHeadlessEnv) -> dict[str, Any]:
    resp = env.request({"op": "snapshot"})
    if not resp.get("ok"):
        raise RuntimeError(f"snapshot op failed: {resp}")
    return resp.get("snapshot", {})


def _append_events(output_dir: Path, step_index: int, events: list[dict[str, Any]]) -> None:
    path = output_dir / artefact.EVENTS_FILE
    for event in events:
        row = {
            "step_index": step_index,
            "tick": int(event.get("tick", 0)),
            "event_type": str(event.get("type", event.get("event_type", "unknown"))),
            "payload": event.get("payload", event),
        }
        artefact.write_jsonl_row(path, row)


def export_visual_rollout(
    *,
    scenario_path: str,
    seed: int,
    policy_type: str,
    output_dir: Path,
    max_steps: int = 50,
    actor_id: str = "p1",
    fog_enabled: int | None = None,
    deterministic: bool = True,
) -> dict[str, Any]:
    output_dir.mkdir(parents=True, exist_ok=True)
    for name in (artefact.COMMANDS_FILE, artefact.EVENTS_FILE):
        path = output_dir / name
        if path.exists():
            path.unlink()
    snap_dir = output_dir / artefact.SNAPSHOT_DIR
    if snap_dir.exists():
        for child in snap_dir.glob("*.json"):
            child.unlink()

    scenario_name = Path(scenario_path).stem
    checkpoint_path: str | None = None
    trained_available = True
    trained_absence_reason: str | None = None

    stats = {
        "valid_commands": 0,
        "no_ops": 0,
        "rejected_commands": 0,
        "steps": 0,
    }

    with GodotHeadlessEnv() as env:
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

        variant = str(reset.get("variant", "economy_trial"))
        game_type = _game_type_from_variant(variant)

        policy = None
        if policy_type in ("random", "untrained", "trained"):
            try:
                policy, checkpoint_path = _load_policy(policy_type, variant, seed)
            except FileNotFoundError as exc:
                if policy_type == "trained":
                    trained_available = False
                    trained_absence_reason = str(exc)
                    policy_type = "untrained"
                    policy, checkpoint_path = _load_policy("untrained", variant, seed)
                else:
                    raise

        debug_snap = _request_debug_snapshot(env)
        artefact.write_snapshot(output_dir, 0, debug_snap)
        available = list(debug_snap.get("available_commands", []))
        all_events: list[dict[str, Any]] = []

        for step_index in range(max_steps):
            tick = int(debug_snap.get("tick", 0))
            command: dict[str, Any] | None = None
            selected_entry: dict[str, Any] | None = None

            if policy_type == "heuristic":
                bot_resp = env.request({"op": "bot_decide", "bot_kind": _bot_kind_from_variant(variant)})
                if not bot_resp.get("ok"):
                    raise RuntimeError(f"bot_decide failed: {bot_resp}")
                available = list(bot_resp.get("available_commands", available))
                raw_cmd = bot_resp.get("command")
                if raw_cmd is not None and isinstance(raw_cmd, dict):
                    command = {
                        "type": str(raw_cmd.get("type", "")),
                        "player_id": str(raw_cmd.get("player_id", actor_id)),
                        "tick": tick,
                        "payload": dict(raw_cmd.get("payload", {})),
                    }
                    selected_entry = artefact.find_matching_entry(command, available)
                else:
                    selected_entry = None
            elif policy is not None:
                command = action_from_policy(
                    policy,
                    debug_snap,
                    available,
                    tick,
                    deterministic=deterministic,
                    actor_id=actor_id,
                )
                if command is None:
                    selected_entry = None
                    stats["no_ops"] += 1
                else:
                    idx = find_index(command, available, variant=variant)
                    if idx is not None and idx < len(available):
                        selected_entry = available[idx]
                    stats["valid_commands"] += 1
            else:
                raise ValueError(f"unknown policy handling for {policy_type}")

            step_req: dict[str, Any] = {"op": "step", "include_policy_observation": False}
            if command is not None:
                step_req["command"] = command
            step = env.request(step_req)
            if not step.get("ok"):
                raise RuntimeError(f"step failed: {step}")

            accepted = bool(step.get("accepted", False))
            if command is not None and not accepted:
                stats["rejected_commands"] += 1
            result_message = "accepted" if accepted or command is None else str(step.get("error_code", "rejected"))

            cmd_row = artefact.command_row_from_entry(
                step_index=step_index,
                tick=tick,
                actor_id=actor_id,
                entry=selected_entry,
                was_accepted=accepted or command is None,
                result_message=result_message,
            )
            artefact.write_jsonl_row(output_dir / artefact.COMMANDS_FILE, cmd_row)

            step_events = list(step.get("events", []))
            _append_events(output_dir, step_index, step_events)
            all_events.extend(step_events)

            debug_snap = _request_debug_snapshot(env)
            artefact.write_snapshot(output_dir, step_index + 1, debug_snap)
            available = list(debug_snap.get("available_commands", []))
            stats["steps"] = step_index + 1

            if step.get("terminal", {}).get("done"):
                break

        episode_resp = env.request({"op": "episode_result"})
        episode_result = episode_resp.get("episode_result", {}) if episode_resp.get("ok") else {}

    result_metrics = metrics.compute_metrics(variant, debug_snap, all_events)
    metrics_payload = {
        "total_reward": None,
        "total_score": result_metrics.get(
            eval_config.ECONOMY_PRIMARY_METRIC if game_type == "economy" else eval_config.MILITARY_PRIMARY_METRIC,
            result_metrics.get("economy_score", result_metrics.get("military_score")),
        ),
        "objective_progress": episode_result,
        "steps": stats["steps"],
        "valid_commands": stats["valid_commands"],
        "no_ops": stats["no_ops"],
        "rejected_commands": stats["rejected_commands"],
        "final_outcome": str(episode_result.get("outcome", result_metrics.get("outcome", "unknown"))),
        "comparison_baseline": None,
    }
    artefact.write_metrics(output_dir, metrics_payload)

    metadata = {
        "schema_version": artefact.SCHEMA_VERSION,
        "scenario_name": scenario_name,
        "scenario_path": scenario_path.replace("\\", "/"),
        "game_type": game_type,
        "policy_type": policy_type,
        "checkpoint_path": checkpoint_path,
        "checkpoint_id": Path(checkpoint_path).stem if checkpoint_path else None,
        "trained_available": trained_available,
        "trained_absence_reason": trained_absence_reason,
        "seed": seed,
        "start_tick": 0,
        "max_steps": max_steps,
        "created_timestamp": artefact.utc_timestamp(),
        "code_commit": artefact.git_commit_short(),
        "observation_encoder_version": None,
        "variant": variant,
    }
    artefact.write_metadata(output_dir, metadata)

    ok, msg = artefact.validate_rollout_folder(output_dir)
    if not ok:
        raise RuntimeError(f"rollout validation failed: {msg}")

    return {
        "output_dir": str(output_dir),
        "steps": stats["steps"],
        "snapshots": artefact.count_snapshots(output_dir),
        "validation": msg,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Export visual rollout artefact folder")
    parser.add_argument("--scenario", required=True, help="Scenario path relative to godot_project")
    parser.add_argument("--seed", type=int, default=11)
    parser.add_argument(
        "--policy",
        choices=["random", "untrained", "trained", "heuristic"],
        default="random",
    )
    parser.add_argument("--max-steps", type=int, default=50)
    parser.add_argument("--output", required=True, help="Output directory")
    parser.add_argument("--actor-id", default="p1")
    parser.add_argument("--fog-enabled", type=int, default=None)
    args = parser.parse_args()

    result = export_visual_rollout(
        scenario_path=args.scenario,
        seed=args.seed,
        policy_type=args.policy,
        output_dir=Path(args.output),
        max_steps=args.max_steps,
        actor_id=args.actor_id,
        fog_enabled=args.fog_enabled,
    )
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
