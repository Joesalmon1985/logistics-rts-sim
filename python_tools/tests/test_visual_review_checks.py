"""RUN_02.5b visual review manifest and policy diagnostics tests."""

from __future__ import annotations

import json
from pathlib import Path

from python_tools.training import rollout_artefact as artefact
from python_tools.training.policy import RandomPolicy, build_policy, policy_selection_details
from python_tools.training.visual_review_batch import (
    check_mode_uniqueness,
    check_panel_markup,
    check_screenshot_coverage,
    choose_capture_steps,
)


def test_choose_capture_steps_includes_start_mid_final():
    steps = choose_capture_steps(25)
    assert steps[0] == 0
    assert 10 in steps
    assert steps[-1] == 25


def test_command_row_includes_policy_diagnostics():
    row = artefact.command_row_from_entry(
        step_index=0,
        tick=0,
        actor_id="p1",
        entry=None,
        was_accepted=True,
        result_message="accepted",
        policy_diagnostics={
            "selected_action_index": 3,
            "valid_action_count": 10,
            "policy_confidence_unavailable_reason": "random policy does not expose probabilities",
        },
    )
    assert row["selected_action_index"] == 3
    assert "policy_confidence_unavailable_reason" in row


def test_random_policy_exports_null_confidence_reason():
    policy = RandomPolicy(11, "economy_trial")
    snapshot = {"available_commands": [], "variant": "economy_trial"}
    _, details = policy_selection_details(policy, snapshot, [], deterministic=True)
    assert details["policy_confidence"] is None
    assert "random policy" in details["policy_confidence_unavailable_reason"]


def test_untrained_policy_exports_action_index():
    policy = build_policy("economy_trial")
    available = [
        {
            "type": "NoopCommand",
            "payload": {"player_id": "p1"},
            "label": "Pass / wait",
            "target": {},
        }
    ]
    snapshot = {"available_commands": available, "variant": "economy_trial"}
    index, details = policy_selection_details(policy, snapshot, available, deterministic=True)
    assert index >= 0
    assert details["selected_action_index"] == index
    assert details["valid_action_count"] >= 1


def test_manifest_coverage_detects_missing_screenshot():
    manifest = {
        "captured_steps": [0],
        "captured_modes": ["world"],
        "screenshots": [],
        "screenshots_intentionally_unavailable": False,
    }
    issues = check_screenshot_coverage(manifest, Path("."))
    assert any("missing screenshot" in issue for issue in issues)


def test_manifest_mode_uniqueness_warning():
    issues = check_mode_uniqueness(
        {"warnings": ["step 0 mode screenshots are byte-identical for modes: world, economy"]}
    )
    assert len(issues) == 1


def test_manifest_panel_metadata_requires_neural_overlay():
    issues = check_panel_markup(
        {
            "screenshots": [
                {"step_index": 0, "mode": "neural", "overlay": {"overlay_available": False}}
            ]
        },
        Path("."),
    )
    assert any("neural" in issue for issue in issues)


def test_overall_contact_sheet_uses_relpath(tmp_path):
    from python_tools.training.visual_review_batch import write_overall_contact_sheet

    rollouts = tmp_path / "training_runs" / "visual_rollouts"
    review = rollouts / "_review"
    shots = rollouts / "economy_random_s11" / "screenshots"
    shots.mkdir(parents=True)
    png = shots / "000_world.png"
    png.write_bytes(b"png")
    write_overall_contact_sheet(review, tmp_path)
    html = (review / "overall_contact_sheet.html").read_text(encoding="utf-8")
    assert "../economy_random_s11/screenshots/000_world.png" in html
