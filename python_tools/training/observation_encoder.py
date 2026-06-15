"""Policy observation encoder — fixed-size tensors from export_policy_observation JSON."""

from __future__ import annotations

from typing import Any

import numpy as np

from python_tools.training import eval_config

BOARD_CHANNEL_NAMES = [
    "terrain_type",
    "owner_relation",
    "visibility",
    "resource_amount",
    "road_present",
    "has_building",
    "building_type",
    "building_hp_progress",
    "has_unit",
    "unit_type",
    "unit_hp_count",
]

PLAYER_FEATURE_NAMES = [
    "wood",
    "stone",
    "food",
    "population",
    "idle_workers",
    "building_count",
    "unit_count",
    "victory_points",
    "known_enemy_strength",
]

SUMMARY_FEATURE_NAMES = [
    "tick_norm",
    "own_unit_count",
    "enemy_unit_count",
    "own_building_count",
    "enemy_building_count",
    "objective_progress",
]

ACTION_TYPE_NAMES = [
    "pass",
    "build",
    "move",
    "attack",
    "produce",
    "road",
    "policy",
    "research",
    "repair",
    "stop",
    "attack_move",
    "other",
]


def _require_fixed_map(observation: dict[str, Any]) -> None:
    width = int(observation.get("map_width", 0))
    height = int(observation.get("map_height", 0))
    if width != eval_config.FIXED_MAP_WIDTH or height != eval_config.FIXED_MAP_HEIGHT:
        raise ValueError(
            f"unsupported map size {width}x{height}; "
            f"expected {eval_config.FIXED_MAP_WIDTH}x{eval_config.FIXED_MAP_HEIGHT}"
        )
    if observation.get("error"):
        raise ValueError(f"policy observation error: {observation.get('error')}")


def encode_board(observation: dict[str, Any]) -> np.ndarray:
    _require_fixed_map(observation)
    width = eval_config.FIXED_MAP_WIDTH
    height = eval_config.FIXED_MAP_HEIGHT
    channels = eval_config.BOARD_CHANNEL_COUNT
    board = np.zeros((channels, height, width), dtype=np.float32)
    for cell in observation.get("board_cells", []):
        q = int(cell.get("q", 0))
        r = int(cell.get("r", 0))
        if q < 0 or q >= width or r < 0 or r >= height:
            continue
        for channel_index, name in enumerate(BOARD_CHANNEL_NAMES):
            value = float(cell.get(name, 0))
            if name in {"terrain_type", "owner_relation", "building_type", "unit_type"}:
                value = value / 10.0
            elif name == "building_hp_progress":
                value = value / 1000.0
            elif name == "resource_amount":
                value = min(value / 100.0, 1.0)
            board[channel_index, r, q] = value
    return board.reshape(-1)


def encode_player_state(observation: dict[str, Any]) -> np.ndarray:
    _require_fixed_map(observation)
    values = observation.get("player_state", [])
    if len(values) != eval_config.PLAYER_FEATURE_COUNT:
        raise ValueError(
            f"player_state length {len(values)} != {eval_config.PLAYER_FEATURE_COUNT}"
        )
    return np.asarray(values, dtype=np.float32)


def encode_summary(observation: dict[str, Any]) -> np.ndarray:
    _require_fixed_map(observation)
    values = observation.get("summary", [])
    if len(values) != eval_config.SUMMARY_FEATURE_COUNT:
        raise ValueError(
            f"summary length {len(values)} != {eval_config.SUMMARY_FEATURE_COUNT}"
        )
    summary = np.asarray(values, dtype=np.float32)
    summary[0] = min(summary[0], 1.0)
    return summary


def encode_observation(observation: dict[str, Any]) -> np.ndarray:
    if int(observation.get("schema_version", 0)) != eval_config.SCHEMA_VERSION:
        raise ValueError("unsupported observation schema_version")
    parts = [
        encode_board(observation),
        encode_player_state(observation),
        encode_summary(observation),
    ]
    return np.concatenate(parts, axis=0)


def observation_dim() -> int:
    board_size = eval_config.BOARD_CHANNEL_COUNT * eval_config.FIXED_MAP_WIDTH * eval_config.FIXED_MAP_HEIGHT
    return board_size + eval_config.PLAYER_FEATURE_COUNT + eval_config.SUMMARY_FEATURE_COUNT
