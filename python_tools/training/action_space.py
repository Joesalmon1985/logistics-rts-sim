"""Action masks and max action sizes for trial variants."""

from __future__ import annotations

MAX_ECONOMY_ACTIONS = 64
MAX_MILITARY_ACTIONS = 128


def max_actions(variant: str) -> int:
    if variant == "economy_trial":
        return MAX_ECONOMY_ACTIONS
    if variant == "military_trial":
        return MAX_MILITARY_ACTIONS
    raise ValueError(f"unsupported variant: {variant}")


def noop_index(variant: str) -> int:
    return max_actions(variant) - 1


def action_mask(available_commands: list, variant: str) -> list[float]:
    limit = max_actions(variant)
    noop = noop_index(variant)
    mask = [0.0] * limit
    legal = min(len(available_commands), noop)
    for index in range(legal):
        mask[index] = 1.0
    mask[noop] = 1.0
    return mask
