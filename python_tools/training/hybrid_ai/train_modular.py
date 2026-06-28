"""Train modular hybrid policy stages M1-M7."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import numpy as np
import torch

from python_tools.training.hybrid_ai.export_godot_checkpoint import main as export_godot
from python_tools.training.hybrid_ai.modular_policy import (
    EXPERT_NAMES,
    ModularHybridPolicy,
    train_arbiter,
    train_expert_head,
)
from python_tools.training.hybrid_ai.teacher_dataset import (
    DEFAULT_DATASET,
    expert_subset,
    load_teacher_rows,
    to_tensors,
)

REPO = Path(__file__).resolve().parents[3]
TRAINING_DIR = REPO / "docs/audit/neural_hybrid_ai/training"
CHECKPOINT_DIR = REPO / "python_tools/training/hybrid_ai/checkpoints"
GODOT = REPO / "godot_project"


def _godot() -> str:
    import os

    return os.environ.get("GODOT", "/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64")


def collect_teacher_traces() -> None:
    cmd = [
        _godot(),
        "--headless",
        "--path",
        str(GODOT),
        "--script",
        "res://sim/tools/collect_modular_teacher_traces.gd",
    ]
    env = dict(**__import__("os").environ)
    env["GODOT_USER_DATA_DIR"] = str(GODOT / ".godot_user")
    subprocess.run(cmd, check=True, cwd=str(REPO), env=env, timeout=300)


def _accuracy(logits: torch.Tensor, labels: torch.Tensor) -> float:
    pred = torch.argmax(logits, dim=-1)
    return float((pred == labels).float().mean().item())


def _eval_expert(policy: ModularHybridPolicy, expert: str, rows) -> dict:
    if not rows:
        return {"samples": 0, "accuracy": 0.0}
    obs, legal, labels = to_tensors(rows)
    obs_t = torch.from_numpy(obs)
    legal_t = torch.from_numpy(legal)
    labels_t = torch.from_numpy(labels)
    with torch.no_grad():
        h = policy.encode(obs_t)
        head_map = {
            "economy": policy.economy_head,
            "logistics": policy.logistics_head,
            "military_production": policy.military_production_head,
            "combat": policy.combat_head,
            "defence": policy.defence_head,
            "scouting": policy.scouting_head,
        }
        logits = head_map[expert](h).masked_fill(legal_t < 0.5, -1e9)
    return {"samples": len(rows), "accuracy": _accuracy(logits, labels_t)}


def _write_stage(name: str, payload: dict) -> Path:
    TRAINING_DIR.mkdir(parents=True, exist_ok=True)
    path = TRAINING_DIR / f"{name}_results.json"
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return path


def main() -> None:
    if not DEFAULT_DATASET.exists():
        print("Collecting teacher traces via Godot...")
        collect_teacher_traces()
    rows = load_teacher_rows()
    if len(rows) < 50:
        raise RuntimeError(f"insufficient teacher rows: {len(rows)}")

    policy = ModularHybridPolicy()
    trained_stages: list[str] = []

    stage_map = {
        "economy": "m1_economy_imitation",
        "logistics": "m2_logistics_imitation",
        "military_production": "m3_military_production_imitation",
        "combat": "m4_combat_imitation",
        "defence": "m4_combat_imitation",
        "scouting": "m2_logistics_imitation",
    }
    for expert in EXPERT_NAMES:
        stage_name = stage_map[expert]
        subset = expert_subset(rows, expert)
        if len(subset) < 8:
            subset = rows[: max(8, len(rows) // 6)]
        obs, legal, labels = to_tensors(subset)
        losses = train_expert_head(
            policy,
            expert,
            torch.from_numpy(obs),
            torch.from_numpy(labels),
            torch.from_numpy(legal),
            epochs=10,
        )
        metrics = _eval_expert(policy, expert, subset)
        metrics["final_loss"] = losses[-1] if losses else 0.0
        metrics["expert"] = expert
        _write_stage(stage_name, metrics)
        trained_stages.append(stage_name)

    arb_rows = rows
    obs_arb, _, _ = to_tensors(arb_rows)
    arb_labels = torch.tensor([min(r.arbiter_index, policy.max_candidates - 1) for r in arb_rows], dtype=torch.long)
    arb_losses = train_arbiter(policy, torch.from_numpy(obs_arb), arb_labels, epochs=12)
    with torch.no_grad():
        h = policy.encode(torch.from_numpy(obs_arb))
        arb_logits = policy.arbiter_head(h)
        arb_acc = _accuracy(arb_logits, arb_labels)
    m5 = {"samples": len(arb_rows), "accuracy": arb_acc, "final_loss": arb_losses[-1] if arb_losses else 0.0}
    _write_stage("m5_arbiter_imitation", m5)
    trained_stages.append("m5_arbiter_imitation")

    # M6: expert tuning — extra epochs on all experts jointly
    m6_losses: list[float] = []
    for expert in EXPERT_NAMES:
        subset = expert_subset(rows, expert) or rows[:64]
        obs, legal, labels = to_tensors(subset)
        m6_losses.extend(
            train_expert_head(
                policy,
                expert,
                torch.from_numpy(obs),
                torch.from_numpy(labels),
                torch.from_numpy(legal),
                epochs=4,
                lr=5e-4,
            )
        )
    m6 = {"samples": len(rows), "expert_passes": len(EXPERT_NAMES), "mean_final_loss": float(np.mean(m6_losses))}
    _write_stage("m6_expert_tuning", m6)
    trained_stages.append("m6_expert_tuning")

    # M7: arbiter tuning
    m7_losses = train_arbiter(policy, torch.from_numpy(obs_arb), arb_labels, epochs=6, lr=5e-4)
    m7 = {"samples": len(arb_rows), "accuracy": arb_acc, "final_loss": m7_losses[-1] if m7_losses else 0.0}
    _write_stage("m7_arbiter_tuning", m7)
    trained_stages.append("m7_arbiter_tuning")

    meta = {
        "trained_stages": sorted(set(trained_stages)),
        "teacher_rows": len(rows),
        "inference_mode": "trained_neural",
    }
    CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)
    pt_path = CHECKPOINT_DIR / "latest.pt"
    policy.save_checkpoint(pt_path, meta)
    export_godot(pt_path)

    summary = TRAINING_DIR / "training_summary.md"
    summary.write_text(
        "\n".join(
            [
                "# Modular Hybrid Training Summary",
                "",
                f"- Teacher rows: {len(rows)}",
                f"- Checkpoint: `{pt_path}`",
                f"- Godot export: `godot_project/sim/hybrid/ai/checkpoints/latest_modular_policy.json`",
                f"- Stages: {', '.join(meta['trained_stages'])}",
                "",
                "## M5 arbiter",
                f"- accuracy: {m5['accuracy']:.3f}",
            ]
        ),
        encoding="utf-8",
    )
    print(json.dumps({"ok": True, "rows": len(rows), "checkpoint": str(pt_path), "stages": meta["trained_stages"]}))


if __name__ == "__main__":
    main()
