"""Behaviour cloning dataset collection from heuristic bots."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import numpy as np

from python_tools.training import eval_config, observation
from python_tools.training.godot_env import GodotHeadlessEnv


@dataclass
class BCSample:
    observation: np.ndarray
    label: int
    variant: str


def collect_bc_episode_fast(
    env: GodotHeadlessEnv,
    *,
    scenario_path: str,
    seed: int,
    bot_kind: str,
    variant: str,
    max_ticks: int,
    dataset_path: Path,
    fog_enabled: int | None = None,
) -> tuple[list[BCSample], dict[str, Any]]:
    dataset_path = dataset_path.resolve()
    req: dict[str, Any] = {
        "op": "record_bc_episode",
        "scenario_path": scenario_path,
        "seed": seed,
        "bot_kind": bot_kind,
        "max_ticks": max_ticks,
        "actor_id": "p1",
        "dataset_path": dataset_path.as_posix(),
    }
    if fog_enabled is not None:
        req["fog_enabled"] = fog_enabled
    resp = env.request(req, timeout=900.0)
    if not resp.get("ok"):
        raise RuntimeError(f"record_bc_episode failed: {resp}")

    raw = json.loads(dataset_path.read_text(encoding="utf-8"))
    samples: list[BCSample] = []
    for step in raw.get("steps", []):
        snapshot = step.get("snapshot", {})
        samples.append(
            BCSample(
                observation=observation.encode_snapshot(snapshot),
                label=int(step.get("label", 0)),
                variant=variant,
            )
        )
    stats = resp.get("stats", {})
    return samples, stats


def load_bc_samples_from_dir(dataset_dir: Path, variant: str) -> list[BCSample]:
    samples: list[BCSample] = []
    for dataset_path in sorted(dataset_dir.glob(f"bc_{variant}_*.json")):
        raw = json.loads(dataset_path.read_text(encoding="utf-8"))
        for step in raw.get("steps", []):
            snapshot = step.get("snapshot", {})
            samples.append(
                BCSample(
                    observation=observation.encode_snapshot(snapshot),
                    label=int(step.get("label", 0)),
                    variant=variant,
                )
            )
    return samples


def collect_bc_dataset(
    variant: str,
    seeds: list[int],
    *,
    scenario_path: str,
    bot_kind: str,
    max_ticks: int,
    dataset_dir: Path,
    fog_enabled: int | None = None,
) -> tuple[list[BCSample], dict[str, Any]]:
    dataset_dir.mkdir(parents=True, exist_ok=True)
    all_samples: list[BCSample] = []
    per_seed: list[dict[str, Any]] = []
    with GodotHeadlessEnv() as env:
        for seed in seeds:
            dataset_path = dataset_dir / f"bc_{variant}_{seed}.json"
            samples, stats = collect_bc_episode_fast(
                env,
                scenario_path=scenario_path,
                seed=seed,
                bot_kind=bot_kind,
                variant=variant,
                max_ticks=max_ticks,
                dataset_path=dataset_path,
                fog_enabled=fog_enabled,
            )
            all_samples.extend(samples)
            per_seed.append({"seed": seed, **stats})

    aggregate = {
        "matched": sum(int(item.get("matched", 0)) for item in per_seed),
        "unmatched": sum(int(item.get("unmatched", 0)) for item in per_seed),
        "noop": sum(int(item.get("noop", 0)) for item in per_seed),
        "per_seed": per_seed,
    }
    aggregate["match_rate"] = aggregate["matched"] / max(
        1, aggregate["matched"] + aggregate["unmatched"]
    )
    return all_samples, aggregate


def assert_label_quality(variant: str, stats: dict[str, Any]) -> None:
    minimum = eval_config.BC_MIN_MATCH_RATE[variant]
    if stats["match_rate"] < minimum:
        raise RuntimeError(
            f"BC label match rate {stats['match_rate']:.3f} below minimum {minimum} for {variant}"
        )


def save_label_stats(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
