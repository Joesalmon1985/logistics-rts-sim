# Visual Legibility Review (RUN_02.5 Phase C)

Repeatable process to answer: **does the screenshot match what the JSON says?**

## Prerequisites

- Rollout export CLI working
- Godot rollout observer working
- `run_rollout_review_capture.gd` capture tooling

## Rollout Generation Requirements

**Required:**

- random rollout — economy
- untrained rollout — economy
- random rollout — military
- untrained rollout — military

**Required if checkpoint exists:**

- trained rollout — economy
- trained rollout — military

**If no trained checkpoint:**

```json
"trained_available": false,
"trained_absence_reason": "checkpoint not found at checkpoints/run06_smoke/economy_policy.pt"
```

Scenarios from `python_tools/training/eval_config.py`:

- Economy: `eval_build_obvious`, `eval_outproduce`
- Military: `eval_focus_fire_hub`, `eval_survive_rush`

## Capture Steps

Fixed step indices: **0, 10, 25, 50, terminal** (or max available).

Per step, per rollout, save under `training_runs/visual_rollouts/<run_id>/screenshots/<step>/`:

| File | Mode |
|------|------|
| `{step}_world.png` | World |
| `{step}_economy.png` | Economy |
| `{step}_military.png` | Military |
| `{step}_neural.png` | Neural |
| `{step}_debug.png` | Debug |
| `{step}_state.json` | Snapshot at step K |
| `{step}_review.md` | Human/agent review stub |

## Review Template (`{step}_review.md`)

Answer for each capture:

1. What does the JSON say is happening?
2. What command did the policy choose (previous vs current/next)?
3. Is that command visible on screen?
4. Is the command result visible?
5. Is objective/progress visible?
6. What important state is **missing** from snapshot?
7. Labels too small?
8. Colours ambiguous?
9. Panels overcrowded?
10. Information belongs in a different mode?
11. Needs exporter change vs client guess?

## Consolidated Report

`docs/audit/RUN_02_5_VISUAL_LEGIBILITY_REVIEW.md` aggregates findings across rollouts.

## Assessment Questions (A–E)

### A. Player/reviewer comprehension

- 3-second map read; selection for tile detail; panels for specialist info; debug JSON last

### B. Neural observer comprehension

- Action visible; success/failure visible; objective trend visible; trained vs random distinguishable;
  bad loops (no-op, reject, stall) spotable

### C. Economy readability

- Production, missing inputs, construction, ware flow, failure reasons (from snapshot only)

### D. Military readability

- Ownership, HP, orders, hostiles, combat consequences

### E. Hybrid readiness

- Concepts that support RUN_09; snapshot fields needed later for unit-building links, patrol, transfers

## Non-Headless Requirements

- Capture at 1920×1080 without `--headless`
- Non-blank map assert (reuse `ViewerPixelProbe`)
- Screenshots differ across steps when state changes
- Godot exits cleanly; no stale processes

## Automation

```bash
python -m python_tools.training.visual_legibility_report \
  --rollout-dir training_runs/visual_rollouts/<run_id>
```

```bash
$GODOT --path godot_project --script res://sim/tools/run_rollout_review_capture.gd \
  -- --rollout-path=... --steps=0,10,25
```

## Git Policy

All generated rollouts and screenshots stay gitignored. Only minimal schema fixture may be committed.
