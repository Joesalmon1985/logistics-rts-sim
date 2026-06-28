"""Load modular teacher traces and build training tensors."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np

from python_tools.training.hybrid_ai.observation_schema import MACRO_COUNT, VECTOR_SIZE

REPO = Path(__file__).resolve().parents[3]
DEFAULT_DATASET = REPO / "docs/audit/neural_hybrid_ai/datasets/modular_teacher_traces.jsonl"

EXPERT_NAMES = [
    "economy",
    "logistics",
    "military_production",
    "combat",
    "defence",
    "scouting",
]


@dataclass
class TeacherRow:
    observation: np.ndarray
    legal_mask: np.ndarray
    chosen_macro: int
    source_expert: str
    arbiter_index: int
    reward: float
    player_id: str
    tick: int
    seed: int


def load_teacher_rows(path: Path | None = None) -> list[TeacherRow]:
    dataset = path or DEFAULT_DATASET
    if not dataset.exists():
        raise FileNotFoundError(f"missing teacher dataset: {dataset}")
    rows: list[TeacherRow] = []
    for line in dataset.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        raw = json.loads(line)
        obs = np.array(raw.get("observation_vector", [0.0] * VECTOR_SIZE), dtype=np.float32)
        if obs.size < VECTOR_SIZE:
            obs = np.pad(obs, (0, VECTOR_SIZE - obs.size))
        mask = np.array(raw.get("legal_action_mask", [1] * MACRO_COUNT), dtype=np.float32)
        if mask.size < MACRO_COUNT:
            mask = np.pad(mask, (0, MACRO_COUNT - mask.size), constant_values=1.0)
        rows.append(
            TeacherRow(
                observation=obs[:VECTOR_SIZE],
                legal_mask=mask[:MACRO_COUNT],
                chosen_macro=int(raw.get("chosen_action", 0)),
                source_expert=str(raw.get("candidate_source_expert", "")),
                arbiter_index=int(raw.get("chosen_candidate_index", 0)),
                reward=float(raw.get("reward", 0.0)),
                player_id=str(raw.get("player_id", "")),
                tick=int(raw.get("tick", 0)),
                seed=int(raw.get("seed", 0)),
            )
        )
    return rows


def expert_subset(rows: list[TeacherRow], expert: str) -> list[TeacherRow]:
    return [r for r in rows if r.source_expert == expert]


def to_tensors(rows: list[TeacherRow]) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    if not rows:
        raise RuntimeError("empty row set for tensor conversion")
    obs = np.stack([r.observation for r in rows])
    legal = np.stack([r.legal_mask for r in rows])
    labels = np.array([r.chosen_macro for r in rows], dtype=np.int64)
    return obs, legal, labels
