# RUN_02.5 Visual Assessment Report

**Status:** Phase A pre-implementation assessment (updated post Phase C).

## Executive Summary

RUN_05 delivered a working step-through debug viewer for economy and military trials. RUN_06
delivered neural training with headless rollouts but **no per-step visual snapshot sequences**.
RUN_02.5 closes the gap with saved rollout artefacts and a replay observer.

## Current RUN_05 Viewer Assessment (A–E)

### A. Player/reviewer comprehension

| Question | Current state | Gap |
|----------|---------------|-----|
| 3-second map read | Good with production demo | Rollout context (policy, step) absent |
| Tile selection | Works via inspector | — |
| Specialist panels | Summary, event log exist | No metrics/reward panel |
| Debug info | Counts only | No raw JSON panel |

### B. Neural observer comprehension

| Question | Current state | Gap |
|----------|---------------|-----|
| See policy action | Bot step only | No rollout replay |
| See success/failure | Last result label | No previous vs next labelling |
| Objective progress | Summary partial | Needs metrics sidecar |
| Compare policies | Not supported | Phase C report only |

### C. Economy readability

Production chain demo shows routes, carriers, construction progress from snapshot. Missing-input
detail depends on `diagnostics` depth — stall reasons not always human-readable in panel.

### D. Military readability

Units, HP bars, fog, attack targets work. Patrol/guard state **not consistently exported** —
document as missing field; do not invent in client.

### E. Hybrid readiness

Rollout timeline + mode presets will reuse in RUN_09. Future snapshot fields needed:
delegated orders, patrol zones, economy→military production links.

## Required v1 Viewer Modes

Implement presets: **World, Economy, Military, Neural, Debug**.

Document but defer separate UI: Construction, Command (live picker), Visual Legibility (capture tooling).

## Required Rollout Schema Fields

- `metadata.json`: schema_version, scenario_name, game_type, policy_type, seed, trained_available
- `metrics.json`: steps, no_ops, rejected_commands, final_outcome
- `commands.jsonl`: step_index, tick, command_label, command_type, was_accepted, result_message
- `events.jsonl`: step_index, event_type
- `snapshots/NNNNNN.json`: full debug snapshot
- Alignment: `len(snapshots) == len(commands) + 1`

## Known Missing Snapshot Fields

| Field | Impact | Action |
|-------|--------|--------|
| `policy_confidence` | Cannot show confidence | Do not fake; optional in commands.jsonl |
| Patrol/guard assignment detail | Military mode incomplete | Document; exporter in future run |
| Explicit stall reason string | Economy diagnosis | Use diagnostics counts for v1 |
| Reward per step | Neural panel sparse | Include when training env returns it |

## v1 Implementation Cut-Line

See `docs/runs/RUN_02_5_VISUAL_IMPROVEMENTS_NEURAL_OBSERVER.md`. Minimum observer + export + fixture + capture.

## Deferred Post-RUN_02.5

- FileDialog rollout picker
- Dual viewport compare
- Live neural inference in Godot
- Construction-specific mode UI
- Full patrol/guard overlays
- Final art/icons
- Hybrid observer (RUN_09)

## Widelands Influence

Documented as **pattern inspiration only** in design 12. No asset/code copying.

## Phase C Findings

*(Updated after legibility review execution.)*

TBD — populated at run completion.
