"""Fixed-size snapshot observation encoders (snapshot JSON only)."""

from __future__ import annotations

from typing import Any

import numpy as np

ECONOMY_FEATURE_NAMES = [
    "tick",
    "seed",
    "building_count",
    "construction_site_count",
    "carrier_count",
    "blocked_job_count",
    "road_count",
    "tree_density_cells",
    "resource_cells",
    "diagnostic_building_count",
    "active_building_count",
    "player_ware_total",
]

MILITARY_FEATURE_NAMES = [
    "tick",
    "seed",
    "unit_count",
    "building_count",
    "credits",
    "power_ok",
    "visible_enemy_units",
    "combat_log_len",
    "construction_sites",
    "queued_production",
    "victory_decided",
]


def encode_economy(snapshot: dict[str, Any]) -> np.ndarray:
    diagnostics = snapshot.get("diagnostics", {})
    display_layers = snapshot.get("display_layers", {})
    layer_counts = snapshot.get("display_layer_counts", {})
    tree_cells = int(
        layer_counts.get("tree_density", len(display_layers.get("tree_density", [])))
    )
    resource_cells = int(
        layer_counts.get("resources", len(display_layers.get("resources", [])))
    )
    buildings = snapshot.get("buildings", [])
    active = 0
    for building in buildings:
        if str(building.get("state", "")) == "active":
            active += 1
    diag_buildings = diagnostics.get("buildings", [])
    if not isinstance(diag_buildings, list):
        diag_buildings = []
    ware_total = 0
    for player in snapshot.get("players", []):
        stores = player.get("stores", {})
        if isinstance(stores, dict):
            for amount in stores.values():
                ware_total += int(amount)
    values = [
        int(snapshot.get("tick", 0)),
        int(snapshot.get("seed", 0)),
        len(buildings),
        len(snapshot.get("construction_sites", [])),
        len(snapshot.get("carrier_markers", [])),
        len(snapshot.get("blocked_job_markers", [])),
        len(snapshot.get("roads", [])),
        tree_cells,
        resource_cells,
        len(diag_buildings),
        active,
        ware_total,
    ]
    return np.asarray(values, dtype=np.float32)


def encode_military(snapshot: dict[str, Any]) -> np.ndarray:
    diagnostics = snapshot.get("diagnostics", {})
    military = diagnostics.get("military", {})
    if not isinstance(military, dict):
        military = {}
    units = snapshot.get("units", [])
    buildings = snapshot.get("buildings", [])
    credits = int(military.get("credits", snapshot.get("credits", 0)))
    power_ok = 1 if str(military.get("power_state", "")) in ("ok", "normal", "stable") else 0
    visible_enemies = 0
    for unit in units:
        if str(unit.get("owner", "")) not in ("p1", snapshot.get("actor_id", "p1"), ""):
            if str(unit.get("visibility", "")) in ("visible", "revealed", "known"):
                visible_enemies += 1
    values = [
        int(snapshot.get("tick", 0)),
        int(snapshot.get("seed", 0)),
        len(units),
        len(buildings),
        credits,
        power_ok,
        visible_enemies,
        len(snapshot.get("combat_log", [])),
        len(snapshot.get("construction_sites", [])),
        int(military.get("queued_production", 0)),
        int(military.get("victory_decided", 0)),
    ]
    return np.asarray(values, dtype=np.float32)


def encode_snapshot(snapshot: dict[str, Any]) -> np.ndarray:
    variant = str(snapshot.get("variant", ""))
    if variant == "economy_trial":
        return encode_economy(snapshot)
    if variant == "military_trial":
        return encode_military(snapshot)
    raise ValueError(f"unsupported variant for observation encoding: {variant}")


def feature_dim(variant: str) -> int:
    if variant == "economy_trial":
        return len(ECONOMY_FEATURE_NAMES)
    if variant == "military_trial":
        return len(MILITARY_FEATURE_NAMES)
    raise ValueError(f"unsupported variant: {variant}")
