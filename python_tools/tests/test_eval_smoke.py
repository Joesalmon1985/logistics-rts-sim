"""RUN_06b evaluation smoke tests (Python side)."""

from __future__ import annotations

from pathlib import Path

import pytest

from python_tools.training import eval_config
from python_tools.training.godot_env import GodotHeadlessEnv
from python_tools.training.policy_eval import run_baseline_episode, summarize_rows


@pytest.mark.skipif(not Path("godot_project").exists(), reason="requires godot project")
def test_eval_smoke_scripted_beats_pass_military():
    with GodotHeadlessEnv() as env:
        scripted = run_baseline_episode(
            env,
            scenario_path=eval_config.MILITARY_EVAL_SCENARIOS[0],
            seed=21,
            policy_kind="scripted_good",
            fog_enabled=0,
        )
    with GodotHeadlessEnv() as env:
        pass_only = run_baseline_episode(
            env,
            scenario_path=eval_config.MILITARY_EVAL_SCENARIOS[0],
            seed=21,
            policy_kind="pass_only",
            fog_enabled=0,
        )
    assert float(scripted["episode_result"]["fitness"]) > float(pass_only["episode_result"]["fitness"])


def test_summarize_rows():
    rows = [
        {"policy_kind": "pass_only", "fitness": 0.0},
        {"policy_kind": "scripted_good", "fitness": 1.0},
    ]
    summary = summarize_rows(rows)
    assert summary["policy_mean_fitness"]["scripted_good"] == 1.0
