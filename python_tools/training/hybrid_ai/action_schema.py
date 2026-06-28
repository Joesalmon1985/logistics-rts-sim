"""Hybrid macro-action schema mirror (PRD A.8)."""

from python_tools.training.hybrid_ai.observation_schema import MACRO_COUNT

MACRO_NAMES = [
    "noop_wait",
    "build_wood_economy",
    "build_stone_economy",
    "build_forester_support",
    "build_sawmill",
    "connect_nearest_important_site",
    "build_barracks",
    "build_guard_post",
    "queue_rifle_squad",
    "set_defend_base_posture",
    "set_attack_visible_enemy_posture",
    "attack_weakest_visible_enemy",
    "expand_toward_nearest_resource_cluster",
    "repair_or_replace_disconnected_road",
    "increase_military_priority",
    "increase_economy_priority",
]


def validate_macro_id(macro_id: int) -> bool:
    return 0 <= macro_id < MACRO_COUNT
