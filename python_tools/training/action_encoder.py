"""Decomposed legal-action encoding for scoring policies."""

from __future__ import annotations

from typing import Any

import numpy as np

from python_tools.training import eval_config
from python_tools.training.observation_encoder import ACTION_TYPE_NAMES

BUILDING_TYPES = [
    "",
    "headquarters",
    "storehouse",
    "woodcutter",
    "sawmill",
    "workshop",
    "command_hub",
    "power_generator",
    "resource_processor",
    "barracks",
]

UNIT_TYPES = ["", "rifle_squad", "rocket_squad", "scout", "engineer"]

COMMAND_TYPE_TO_ACTION = {
    "NoopCommand": "pass",
    "PlaceBuildingPlanCommand": "build",
    "PlaceBuildingCommand": "build",
    "MoveUnitCommand": "move",
    "AttackUnitCommand": "attack",
    "AttackMoveCommand": "attack_move",
    "QueueUnitCommand": "produce",
    "PlaceRoadCommand": "road",
    "SetWorkerPolicyCommand": "policy",
    "StartResearchCommand": "research",
    "RepairTargetCommand": "repair",
    "StopUnitCommand": "stop",
}


def _one_hot(index: int, size: int) -> np.ndarray:
    vec = np.zeros(size, dtype=np.float32)
    if 0 <= index < size:
        vec[index] = 1.0
    return vec


def _norm_coord(value: int, scale: int = eval_config.FIXED_MAP_WIDTH) -> float:
    if scale <= 0:
        return 0.0
    return float(value) / float(scale)


def _action_type_index(command: dict[str, Any]) -> int:
    cmd_type = str(command.get("type", ""))
    action_name = COMMAND_TYPE_TO_ACTION.get(cmd_type, "other")
    if action_name in ACTION_TYPE_NAMES:
        return ACTION_TYPE_NAMES.index(action_name)
    return ACTION_TYPE_NAMES.index("other")


def _target_relation(payload: dict[str, Any]) -> int:
    if "target_id" in payload:
        return 2
    return 0


def encode_action(command: dict[str, Any], *, variant: str = "economy_trial") -> np.ndarray:
    payload = command.get("payload", {})
    if not isinstance(payload, dict):
        payload = {}

    type_vec = _one_hot(_action_type_index(command), eval_config.ACTION_TYPE_COUNT)

    source_q = int(payload.get("from_q", payload.get("q", payload.get("source_q", 0))))
    source_r = int(payload.get("from_r", payload.get("r", payload.get("source_r", 0))))
    target_q = int(payload.get("to_q", payload.get("target_q", payload.get("q", 0))))
    target_r = int(payload.get("to_r", payload.get("target_r", payload.get("r", 0))))

    building_type = str(payload.get("building_type", ""))
    building_index = BUILDING_TYPES.index(building_type) if building_type in BUILDING_TYPES else 0
    unit_type = str(payload.get("unit_type", ""))
    unit_index = UNIT_TYPES.index(unit_type) if unit_type in UNIT_TYPES else 0

    coords = np.asarray(
        [
            _norm_coord(source_q),
            _norm_coord(source_r),
            _norm_coord(target_q),
            _norm_coord(target_r),
        ],
        dtype=np.float32,
    )
    building_vec = _one_hot(building_index, len(BUILDING_TYPES))
    unit_vec = _one_hot(unit_index, len(UNIT_TYPES))
    relation_vec = _one_hot(_target_relation(payload), 4)

    args = np.zeros(eval_config.ACTION_ARG_COUNT, dtype=np.float32)
    numeric_keys = ["unit_id", "target_id", "building_id", "amount", "policy_id"]
    for index, key in enumerate(numeric_keys):
        if index >= eval_config.ACTION_ARG_COUNT:
            break
        if key in payload:
            args[index] = float(int(payload[key])) / 1000.0

    _ = variant
    return np.concatenate([type_vec, coords, building_vec, unit_vec, relation_vec, args], axis=0)


def action_dim() -> int:
    return (
        eval_config.ACTION_TYPE_COUNT
        + 4
        + len(BUILDING_TYPES)
        + len(UNIT_TYPES)
        + 4
        + eval_config.ACTION_ARG_COUNT
    )
