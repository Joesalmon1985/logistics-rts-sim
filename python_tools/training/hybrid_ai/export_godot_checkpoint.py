"""Export PyTorch modular checkpoint to Godot JSON weights."""

from __future__ import annotations

import json
from pathlib import Path

import torch

from python_tools.training.hybrid_ai.modular_policy import EXPERT_NAMES, ModularHybridPolicy
from python_tools.training.hybrid_ai.observation_schema import MACRO_COUNT, VECTOR_SIZE

REPO = Path(__file__).resolve().parents[3]
DEFAULT_PT = REPO / "python_tools/training/hybrid_ai/checkpoints/latest.pt"
DEFAULT_JSON = REPO / "godot_project/sim/hybrid/ai/checkpoints/latest_modular_policy.json"
DEFAULT_MANIFEST = REPO / "docs/audit/neural_hybrid_ai/training/latest_checkpoint_manifest.json"


def _linear_to_dict(layer) -> dict:
    return {
        "weight": layer.weight.detach().cpu().tolist(),
        "bias": layer.bias.detach().cpu().tolist(),
    }


def export_policy(policy: ModularHybridPolicy, meta: dict) -> dict:
    enc = policy.encoder
    return {
        "schema_version": 2,
        "vector_size": VECTOR_SIZE,
        "hidden": policy.hidden,
        "macro_count": MACRO_COUNT,
        "max_candidates": policy.max_candidates,
        "expert_names": EXPERT_NAMES,
        "meta": meta,
        "encoder": {
            "fc1": _linear_to_dict(enc[0]),
            "fc2": _linear_to_dict(enc[2]),
        },
        "heads": {
            "economy": _linear_to_dict(policy.economy_head),
            "logistics": _linear_to_dict(policy.logistics_head),
            "military_production": _linear_to_dict(policy.military_production_head),
            "combat": _linear_to_dict(policy.combat_head),
            "defence": _linear_to_dict(policy.defence_head),
            "scouting": _linear_to_dict(policy.scouting_head),
            "arbiter": _linear_to_dict(policy.arbiter_head),
        },
    }


def main(pt_path: Path | None = None, json_path: Path | None = None) -> Path:
    pt = pt_path or DEFAULT_PT
    out = json_path or DEFAULT_JSON
    policy = ModularHybridPolicy()
    meta = policy.load_checkpoint(pt)
    payload = export_policy(policy, meta)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(payload), encoding="utf-8")
    manifest = {
        "checkpoint_pt": pt.as_posix(),
        "checkpoint_godot": out.as_posix(),
        "meta": meta,
        "trained_stages": meta.get("trained_stages", []),
    }
    DEFAULT_MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    DEFAULT_MANIFEST.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Exported Godot checkpoint: {out}")
    return out


if __name__ == "__main__":
    main()
