# RUN_02.5 Visual Legibility Review

**Status:** Phase C complete (local generation). Screenshots remain under gitignored `training_runs/visual_rollouts/`.

## Rollouts generated

| Rollout | Policy | Scenario | Steps | Snapshots |
|---------|--------|----------|-------|-----------|
| `economy_random_s11` | random | eval_build_obvious | 30 | 31 |
| `economy_untrained_s11` | untrained | eval_build_obvious | 17 | 18 |
| `economy_trained_s11` | trained | eval_build_obvious | 27 | 28 |
| `military_random_s21` | random | eval_focus_fire_hub | 30 | 31 |
| `military_untrained_s21` | untrained | eval_focus_fire_hub | 30 | 31 |
| `military_trained_s21` | trained | eval_focus_fire_hub | 30 | 31 |

Trained rollouts used checkpoints at `checkpoints/run06_smoke/*.pt`.

## Screenshots captured

- `economy_random_s11/screenshots/` — steps 0, 10 (world/economy/military/neural/debug + state JSON)
- `military_random_s21/screenshots/` — steps 0, 10

Per-step review stubs: `{step}_review.md` beside captures (via `visual_legibility_report.py`).

## Findings summary

### What is easier to understand

- Policy type, scenario, seed, step, and tick visible in status bar
- **Previous command** vs **current/next decision** labels reduce off-by-one confusion
- Economy logistics layers (routes, forest density) readable in Economy mode
- Military units/buildings visible in Military mode with HP bars from snapshot
- Metrics panel shows no-op/reject counts and outcome from sidecar JSON

### What remains unclear

- Policy confidence not exported — panel correctly omits it
- Patrol/guard assignment detail not in snapshot — Military mode cannot show patrol state
- Stall reasons are diagnostic counts only — human-readable stall text not exported
- Untrained economy rollouts may produce sparse event logs (empty `events.jsonl` allowed)

### Missing snapshot/exporter fields (future)

- `policy_confidence` / logit in commands sidecar (optional, from training only)
- Explicit patrol/guard/patrol-zone export for military
- Human-readable economy stall reason strings
- Per-step reward/objective delta in sidecar when training env provides it

### Panel/mode notes

- Debug JSON panel sufficient for exporter gap identification
- Construction mode deferred — construction visible under Economy preset
- Capture standardized at **1920×1080** full observer root (RUN_02.5b)
- BBCode enabled on panel RichTextLabels; command overlays in Neural/Debug modes
- Batch capture via `python_tools/training/visual_review_batch.py`

### Issue 10 — `eval_build_obvious` worker-policy wins

**Classification:** (a) intended scenario design + (d) misleading scenario name. Scenario JSON declares `victory.type = worker_policy`; win after `SetWorkerPolicyCommand` is correct per `PolicyEpisodeResult`. Rename or add a true build-completion eval is a RUN_06b/RUN_07 follow-up.

## Commands

```bash
# Export rollout
python3 -m python_tools.training.visual_rollout_export \
  --scenario scenarios/economy/eval_build_obvious.json \
  --seed 11 --policy random --max-steps 30 \
  --output training_runs/visual_rollouts/economy_random_s11

# Open observer
$GODOT --path godot_project --resolution 1920x1080 \
  res://client/scenes/rollout_observer_demo.tscn \
  -- --rollout-path=training_runs/visual_rollouts/economy_random_s11

# Capture review frames (non-headless)
$GODOT --path godot_project --script res://sim/tools/run_rollout_review_capture.gd \
  -- --rollout-path=... --output=.../screenshots --steps=0,10,25
```
