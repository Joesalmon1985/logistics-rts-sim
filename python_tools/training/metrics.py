"""Episode metrics from exported snapshots and events."""

from __future__ import annotations

from typing import Any


def economy_success(snapshot: dict[str, Any]) -> int:
    for building in snapshot.get("buildings", []):
        if str(building.get("building_type", "")) == "workshop":
            if str(building.get("state", "")) == "active":
                return 1
    return 0


def count_event_type(events: list[dict[str, Any]], event_type: str) -> int:
    count = 0
    for event in events:
        if str(event.get("type", "")) == event_type:
            count += 1
    return count


def stalled_jobs(snapshot: dict[str, Any]) -> int:
    return len(snapshot.get("blocked_job_markers", []))


def construction_completions(events: list[dict[str, Any]]) -> int:
    return count_event_type(events, "construction_completed")


def enemy_damage_dealt(events: list[dict[str, Any]]) -> int:
    total = 0
    for event in events:
        event_type = str(event.get("type", ""))
        if event_type not in ("damage_dealt", "unit_damaged", "combat_damage"):
            continue
        payload = event.get("payload", {})
        if not isinstance(payload, dict):
            continue
        if str(payload.get("target_owner", "")) in ("p2", "enemy", "opponent"):
            total += int(payload.get("amount", payload.get("damage", 1)))
        elif str(payload.get("attacker_owner", payload.get("player_id", ""))) == "p1":
            total += int(payload.get("amount", payload.get("damage", 1)))
    return total


def objective_success(snapshot: dict[str, Any]) -> int:
    diagnostics = snapshot.get("diagnostics", {})
    military = diagnostics.get("military", {})
    if isinstance(military, dict):
        if str(military.get("victory_state", "")) == "victory":
            return 1
        if int(military.get("victory_decided", 0)) == 1 and str(military.get("winner_player_id", "")) == "p1":
            return 1
    return 0


def economy_score(snapshot: dict[str, Any], events: list[dict[str, Any]]) -> int:
    return (
        construction_completions(events) * 10
        + economy_success(snapshot) * 100
        - stalled_jobs(snapshot)
    )


def military_score(snapshot: dict[str, Any], events: list[dict[str, Any]]) -> int:
    return enemy_damage_dealt(events) + objective_success(snapshot) * 100


def compute_metrics(variant: str, snapshot: dict[str, Any], events: list[dict[str, Any]]) -> dict[str, int]:
    if variant == "economy_trial":
        return {
            "economy_success": economy_success(snapshot),
            "construction_completions": construction_completions(events),
            "stalled_jobs": stalled_jobs(snapshot),
            "economy_score": economy_score(snapshot, events),
        }
    if variant == "military_trial":
        return {
            "objective_success": objective_success(snapshot),
            "enemy_damage_dealt": enemy_damage_dealt(events),
            "military_score": military_score(snapshot, events),
        }
    raise ValueError(f"unsupported variant: {variant}")
