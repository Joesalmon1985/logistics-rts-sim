"""Verify GodotHeadlessEnv subprocess cleanup."""

from __future__ import annotations

from python_tools.tests.conftest import require_godot
from python_tools.training.godot_env import (
    GodotHeadlessEnv,
    find_stale_training_env_processes,
)


def test_godot_headless_env_closes_subprocess():
    require_godot()
    before = set(find_stale_training_env_processes())
    with GodotHeadlessEnv() as env:
        env.request({"op": "state_hash"})
        assert env.process is not None
        pid = env.process.pid
        assert pid not in before or pid in find_stale_training_env_processes()
    after = set(find_stale_training_env_processes())
    assert pid not in after
