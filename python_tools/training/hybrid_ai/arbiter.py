"""Arbiter selection over expert candidates."""

from __future__ import annotations


def choose_candidate(candidates: list[dict], legal_mask: list[int]) -> dict:
    best = None
    best_score = -1.0
    for c in candidates:
        macro_id = int(c.get("macro_action_id", 0))
        if macro_id < 0 or macro_id >= len(legal_mask) or legal_mask[macro_id] == 0:
            continue
        score = float(c.get("urgency_score", 0.0))
        if score > best_score:
            best_score = score
            best = c
    if best is None:
        return {"chosen_macro_action": 0, "plain_english_reason": "safe noop"}
    return {
        "chosen_macro_action": int(best.get("macro_action_id", 0)),
        "plain_english_reason": str(best.get("plain_english_reason", "")),
    }
