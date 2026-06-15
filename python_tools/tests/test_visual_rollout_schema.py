"""VIS-ROLL-001 / VIS-ROLL-001b rollout schema tests."""

from __future__ import annotations

import json
from pathlib import Path

from python_tools.training import rollout_artefact as artefact


def _write_minimal_rollout(tmp_path: Path, command_count: int = 2) -> Path:
    output_dir = tmp_path / "minimal"
    output_dir.mkdir()
    snap_dir = output_dir / artefact.SNAPSHOT_DIR
    snap_dir.mkdir()
    for index in range(command_count + 1):
        snap = {
            "schema_version": 1,
            "variant": "economy_trial",
            "tick": index,
            "available_commands": [],
            "display_layers": {"terrain": []},
        }
        (snap_dir / f"{index:06d}.json").write_text(json.dumps(snap), encoding="utf-8")
    for index in range(command_count):
        row = artefact.command_row_from_entry(
            step_index=index,
            tick=index,
            actor_id="p1",
            entry=None,
            was_accepted=True,
            result_message="accepted",
        )
        artefact.write_jsonl_row(output_dir / artefact.COMMANDS_FILE, row)
    artefact.write_jsonl_row(
        output_dir / artefact.EVENTS_FILE,
        {"step_index": 0, "tick": 1, "event_type": "noop", "payload": {}},
    )
    artefact.write_metadata(
        output_dir,
        {
            "schema_version": 1,
            "scenario_name": "test",
            "game_type": "economy",
            "policy_type": "random",
            "seed": 1,
        },
    )
    artefact.write_metrics(
        output_dir,
        {"steps": command_count, "no_ops": command_count, "rejected_commands": 0},
    )
    return output_dir


def test_validate_alignment_passes(tmp_path):
    output_dir = _write_minimal_rollout(tmp_path, command_count=2)
    ok, msg = artefact.validate_alignment(output_dir)
    assert ok, msg


def test_validate_alignment_fails_on_off_by_one(tmp_path):
    output_dir = _write_minimal_rollout(tmp_path, command_count=2)
    # Remove one snapshot to break alignment
    (output_dir / artefact.SNAPSHOT_DIR / "000002.json").unlink()
    ok, msg = artefact.validate_alignment(output_dir)
    assert not ok
    assert "misalignment" in msg


def test_validate_rollout_folder_requires_metadata(tmp_path):
    output_dir = tmp_path / "empty"
    output_dir.mkdir()
    ok, msg = artefact.validate_rollout_folder(output_dir)
    assert not ok
    assert "metadata" in msg


def test_command_row_noop_defaults():
    row = artefact.command_row_from_entry(
        step_index=0,
        tick=0,
        actor_id="p1",
        entry=None,
        was_accepted=True,
        result_message="accepted",
    )
    assert row["command_type"] == "NoopCommand"
    assert row["step_index"] == 0
