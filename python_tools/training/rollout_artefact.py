"""Visual rollout artefact schema, writers, and validators (RUN_02.5)."""

from __future__ import annotations

import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

SCHEMA_VERSION = 1
SNAPSHOT_DIR = "snapshots"
COMMANDS_FILE = "commands.jsonl"
EVENTS_FILE = "events.jsonl"
METADATA_FILE = "metadata.json"
METRICS_FILE = "metrics.json"


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def snapshot_path(step_index: int) -> str:
    return f"{SNAPSHOT_DIR}/{step_index:06d}.json"


def read_jsonl(path: Path) -> list[dict[str, Any]]:
    if not path.exists():
        return []
    rows: list[dict[str, Any]] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        rows.append(json.loads(line))
    return rows


def write_jsonl_row(path: Path, row: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(row, sort_keys=True) + "\n")


def write_snapshot(output_dir: Path, step_index: int, snapshot: dict[str, Any]) -> Path:
    snap_dir = output_dir / SNAPSHOT_DIR
    snap_dir.mkdir(parents=True, exist_ok=True)
    path = snap_dir / f"{step_index:06d}.json"
    path.write_text(json.dumps(snapshot, indent=2), encoding="utf-8")
    return path


def write_metadata(output_dir: Path, metadata: dict[str, Any]) -> None:
    payload = dict(metadata)
    payload.setdefault("schema_version", SCHEMA_VERSION)
    path = output_dir / METADATA_FILE
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")


def write_metrics(output_dir: Path, metrics: dict[str, Any]) -> None:
    path = output_dir / METRICS_FILE
    path.write_text(json.dumps(metrics, indent=2), encoding="utf-8")


def git_commit_short() -> str | None:
    try:
        result = subprocess.run(
            ["git", "rev-parse", "--short", "HEAD"],
            cwd=repo_root(),
            capture_output=True,
            text=True,
            check=True,
        )
        return result.stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None


def utc_timestamp() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def command_row_from_entry(
    *,
    step_index: int,
    tick: int,
    actor_id: str,
    entry: dict[str, Any] | None,
    was_accepted: bool,
    result_message: str,
    reward_delta: float | None = None,
    policy_confidence: float | None = None,
) -> dict[str, Any]:
    if entry is None:
        return {
            "step_index": step_index,
            "tick": tick,
            "actor_id": actor_id,
            "command_id": "noop",
            "command_label": "No-op",
            "command_type": "NoopCommand",
            "command_target": None,
            "policy_confidence": policy_confidence,
            "was_accepted": was_accepted,
            "result_message": result_message,
            "reward_delta": reward_delta,
        }
    target = entry.get("target")
    return {
        "step_index": step_index,
        "tick": tick,
        "actor_id": actor_id,
        "command_id": str(entry.get("id", "")),
        "command_label": str(entry.get("label", entry.get("type", ""))),
        "command_type": str(entry.get("type", "")),
        "command_target": target if isinstance(target, dict) else None,
        "policy_confidence": policy_confidence,
        "was_accepted": was_accepted,
        "result_message": result_message,
        "reward_delta": reward_delta,
    }


def find_matching_entry(
    command: dict[str, Any] | None,
    available_commands: list[dict[str, Any]],
) -> dict[str, Any] | None:
    if command is None:
        return None
    cmd_type = str(command.get("type", ""))
    cmd_payload = command.get("payload", {})
    for entry in available_commands:
        if str(entry.get("type", "")) != cmd_type:
            continue
        entry_payload = dict(entry.get("payload", {}))
        entry_payload.pop("tick", None)
        payload = dict(cmd_payload) if isinstance(cmd_payload, dict) else {}
        payload.pop("tick", None)
        if payload == entry_payload:
            return entry
    return None


def count_snapshots(output_dir: Path) -> int:
    snap_dir = output_dir / SNAPSHOT_DIR
    if not snap_dir.is_dir():
        return 0
    return len(list(snap_dir.glob("*.json")))


def validate_alignment(output_dir: Path) -> tuple[bool, str]:
    commands = read_jsonl(output_dir / COMMANDS_FILE)
    snapshot_count = count_snapshots(output_dir)
    expected = len(commands) + 1
    if snapshot_count != expected:
        return (
            False,
            f"snapshot/command misalignment: {snapshot_count} snapshots, "
            f"{len(commands)} commands (expected {expected})",
        )
    snap_dir = output_dir / SNAPSHOT_DIR
    for index in range(snapshot_count):
        path = snap_dir / f"{index:06d}.json"
        if not path.is_file():
            return False, f"missing snapshot file: {path.name}"
    for index, row in enumerate(commands):
        if int(row.get("step_index", -1)) != index:
            return False, f"commands.jsonl row {index} has step_index={row.get('step_index')}"
    return True, "ok"


def validate_rollout_folder(output_dir: Path) -> tuple[bool, str]:
    if not output_dir.is_dir():
        return False, f"not a directory: {output_dir}"
    for name in (METADATA_FILE, METRICS_FILE, COMMANDS_FILE, EVENTS_FILE):
        if not (output_dir / name).is_file():
            return False, f"missing required file: {name}"
    meta_path = output_dir / METADATA_FILE
    try:
        metadata = json.loads(meta_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        return False, f"invalid metadata.json: {exc}"
    if int(metadata.get("schema_version", 0)) != SCHEMA_VERSION:
        return False, f"unsupported schema_version: {metadata.get('schema_version')}"
    ok, msg = validate_alignment(output_dir)
    if not ok:
        return False, msg
    return True, "ok"
