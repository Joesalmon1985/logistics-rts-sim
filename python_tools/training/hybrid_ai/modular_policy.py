"""Modular hybrid policy — shared encoder + 7 specialist heads + arbiter."""

from __future__ import annotations

from pathlib import Path
from typing import Any

import torch
import torch.nn as nn
import torch.nn.functional as F

from python_tools.training.hybrid_ai.observation_schema import MACRO_COUNT, VECTOR_SIZE

EXPERT_NAMES = [
    "economy",
    "logistics",
    "military_production",
    "combat",
    "defence",
    "scouting",
]


class ModularHybridPolicy(nn.Module):
    def __init__(self, hidden: int = 64, max_candidates: int = 24) -> None:
        super().__init__()
        self.hidden = hidden
        self.max_candidates = max_candidates
        self.encoder = nn.Sequential(
            nn.Linear(VECTOR_SIZE, hidden),
            nn.ReLU(),
            nn.Linear(hidden, hidden),
            nn.ReLU(),
        )
        self.economy_head = nn.Linear(hidden, MACRO_COUNT)
        self.logistics_head = nn.Linear(hidden, MACRO_COUNT)
        self.military_production_head = nn.Linear(hidden, MACRO_COUNT)
        self.combat_head = nn.Linear(hidden, MACRO_COUNT)
        self.defence_head = nn.Linear(hidden, MACRO_COUNT)
        self.scouting_head = nn.Linear(hidden, MACRO_COUNT)
        self.arbiter_head = nn.Linear(hidden, max_candidates)

    def encode(self, obs: torch.Tensor) -> torch.Tensor:
        return self.encoder(obs)

    def expert_logits(self, h: torch.Tensor) -> dict[str, torch.Tensor]:
        return {
            "economy": self.economy_head(h),
            "logistics": self.logistics_head(h),
            "military_production": self.military_production_head(h),
            "combat": self.combat_head(h),
            "defence": self.defence_head(h),
            "scouting": self.scouting_head(h),
        }

    def forward(
        self,
        obs_vector: torch.Tensor,
        legal_mask: torch.Tensor | None = None,
        candidate_mask: torch.Tensor | None = None,
    ) -> dict[str, Any]:
        h = self.encode(obs_vector)
        logits_map = self.expert_logits(h)
        masked_expert: dict[str, torch.Tensor] = {}
        for name, logits in logits_map.items():
            if legal_mask is not None:
                masked_expert[name] = logits.masked_fill(legal_mask < 0.5, -1e9)
            else:
                masked_expert[name] = logits

        candidates: list[dict[str, Any]] = []
        for expert in EXPERT_NAMES:
            lg = masked_expert[expert]
            macro_id = int(torch.argmax(lg, dim=-1).item())
            score = float(torch.max(lg, dim=-1).values.item())
            candidates.append(
                {
                    "source_expert": expert,
                    "macro_action_id": macro_id,
                    "urgency_score": score,
                }
            )

        arb_in = h
        arb_logits = self.arbiter_head(arb_in)
        n_cand = min(len(candidates), self.max_candidates)
        arb_slice = arb_logits[..., :n_cand]
        if candidate_mask is not None:
            arb_slice = arb_slice.masked_fill(candidate_mask[..., :n_cand] < 0.5, -1e9)
        chosen_idx = int(torch.argmax(arb_slice, dim=-1).item())
        chosen_idx = max(0, min(chosen_idx, n_cand - 1))
        chosen = candidates[chosen_idx]
        return {
            "hidden": h,
            "expert_logits": masked_expert,
            "candidates": candidates,
            "arbiter_logits": arb_slice,
            "chosen_candidate_index": chosen_idx,
            "chosen_macro_action": int(chosen["macro_action_id"]),
            "chosen_source_expert": str(chosen["source_expert"]),
            "inference_mode": "neural",
        }

    def save_checkpoint(self, path: Path, meta: dict | None = None) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        payload = {"state_dict": self.state_dict(), "meta": meta or {}}
        torch.save(payload, path)

    def load_checkpoint(self, path: Path) -> dict:
        raw = torch.load(path, map_location="cpu", weights_only=False)
        if isinstance(raw, dict) and "state_dict" in raw:
            self.load_state_dict(raw["state_dict"])
            return raw.get("meta", {})
        self.load_state_dict(raw)
        return {}


def train_expert_head(
    policy: ModularHybridPolicy,
    expert: str,
    obs: torch.Tensor,
    labels: torch.Tensor,
    legal_mask: torch.Tensor,
    *,
    epochs: int = 8,
    lr: float = 1e-3,
) -> list[float]:
    head_map = {
        "economy": policy.economy_head,
        "logistics": policy.logistics_head,
        "military_production": policy.military_production_head,
        "combat": policy.combat_head,
        "defence": policy.defence_head,
        "scouting": policy.scouting_head,
    }
    head = head_map[expert]
    params = list(policy.encoder.parameters()) + list(head.parameters())
    opt = torch.optim.Adam(params, lr=lr)
    losses: list[float] = []
    for _ in range(epochs):
        opt.zero_grad()
        h = policy.encode(obs)
        logits = head(h)
        logits = logits.masked_fill(legal_mask < 0.5, -1e9)
        loss = F.cross_entropy(logits, labels)
        loss.backward()
        opt.step()
        losses.append(float(loss.item()))
    return losses


def train_arbiter(
    policy: ModularHybridPolicy,
    obs: torch.Tensor,
    arbiter_labels: torch.Tensor,
    *,
    epochs: int = 8,
    lr: float = 1e-3,
) -> list[float]:
    opt = torch.optim.Adam(list(policy.encoder.parameters()) + list(policy.arbiter_head.parameters()), lr=lr)
    losses: list[float] = []
    for _ in range(epochs):
        opt.zero_grad()
        h = policy.encode(obs)
        logits = policy.arbiter_head(h)
        loss = F.cross_entropy(logits, arbiter_labels)
        loss.backward()
        opt.step()
        losses.append(float(loss.item()))
    return losses
