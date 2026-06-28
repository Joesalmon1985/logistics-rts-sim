"""Stage-specific rewards for hybrid curriculum (PRD A.9)."""

from __future__ import annotations


def stage_reward(stage: int, info: dict) -> float:
    if info.get("invalid_action"):
        return -0.05
    if info.get("noop"):
        return -0.001
    if stage == 0:
        return 0.01 if info.get("valid_command") else -0.05
    if stage == 2 and info.get("useful_building"):
        return 0.5
    if info.get("win"):
        return 30.0
    if info.get("eliminated"):
        return -10.0
    return 0.0
