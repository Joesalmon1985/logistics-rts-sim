"""Canonical command <-> availability index matching for BC labels."""

from __future__ import annotations

from typing import Any

IGNORE_PAYLOAD_KEYS = frozenset({"tick"})


from python_tools.training import action_space


def normalize_command(command: dict[str, Any] | None) -> dict[str, Any] | None:
    if command is None:
        return None
    payload = command.get("payload", {})
    if not isinstance(payload, dict):
        payload = {}
    normalized_payload: dict[str, Any] = {}
    for key in sorted(payload.keys()):
        if key in IGNORE_PAYLOAD_KEYS:
            continue
        normalized_payload[str(key)] = payload[key]
    return {
        "type": str(command.get("type", "")),
        "player_id": str(command.get("player_id", payload.get("player_id", ""))),
        "payload": normalized_payload,
    }


def normalize_availability_entry(entry: dict[str, Any]) -> dict[str, Any]:
    payload = entry.get("payload", {})
    if not isinstance(payload, dict):
        payload = {}
    normalized_payload: dict[str, Any] = {}
    for key in sorted(payload.keys()):
        if key in IGNORE_PAYLOAD_KEYS:
            continue
        normalized_payload[str(key)] = payload[key]
    return {
        "type": str(entry.get("type", "")),
        "player_id": str(entry.get("actor_id", payload.get("player_id", ""))),
        "payload": normalized_payload,
    }


def commands_equal(left: dict[str, Any] | None, right: dict[str, Any] | None) -> bool:
    return normalize_command(left) == normalize_command(right)


def find_index(
    command: dict[str, Any] | None,
    available_commands: list[dict[str, Any]],
    variant: str = "economy_trial",
) -> int | None:
    if command is None:
        return action_space.noop_index(variant)
    target = normalize_command(command)
    for index, entry in enumerate(available_commands):
        if commands_equal(target, normalize_availability_entry(entry)):
            return index
    return None


def index_to_command(
    index: int,
    available_commands: list[dict[str, Any]],
    tick: int,
    actor_id: str = "p1",
    variant: str = "economy_trial",
) -> dict[str, Any] | None:
    if index == action_space.noop_index(variant):
        return None
    if index < 0 or index >= len(available_commands):
        raise ValueError(f"invalid action index {index}")
    entry = available_commands[index]
    payload = dict(entry.get("payload", {}))
    payload["tick"] = tick
    return {
        "type": str(entry.get("type", "")),
        "player_id": str(entry.get("actor_id", actor_id)),
        "tick": tick,
        "payload": payload,
    }
