# RUN_02.8d — Closure Report

Date: 2026-06-17  
Branch: `feature/run-02-8c-test-runtime-efficiency`  
**Status: NOT MERGE-READY** — Tier 2 gate red

Environment:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

## Status summary

| Gate | Status | Exit | Notes |
|------|--------|------|-------|
| ECON_BOT_002 | **FAIL** | 1 | p1/p2 buildings b=2 (need ≥5); deliveries/roads OK |
| ECON-SMOKE-006 | **FAIL** | 1 | hash only — actual `sha256:27d91679…` stable |
| REP-SMOKE-001 | **FAIL** | 1 | import hash_mismatch (pre-existing on ae78996) |
| B–E/F/G regression | **PASS** | 0 | 6/6 |
| Full suite 188/188 | not run | — | blocked on 002 |
| Tier 2 closure | **FAIL** | — | |

## Phase A — Main vs branch (`ae78996`)

| Check | ae78996 | Branch | Deterministic |
|-------|---------|--------|---------------|
| quick_metrics @800 b/r/d p1 | 1/38/381 | 2/42/388 | yes |
| ECON-SMOKE-006 hash | `27d91679…` | `27d91679…` | yes (2 runs) |
| REP-SMOKE-003 | hash_mismatch | hash_mismatch | yes |

Failures pre-existing on `ae78996` except partial building progress from forester shortlist fix.

## Phase B — Diagnosis

- **Failing gate:** buildings (both players); threshold ≥5 delta buildings.
- **Root cause:** bot chain stalled at forester — not in command shortlist; after fix, road budget exhausted (~42–45 segments) before forester `PlaceBuildingPlan` fires.
- **Not the issue:** deliveries (d≥380), roads (r≥41), symmetry at b=2, nondeterminism.

## Phase C — Changes (partial)

| File | Change |
|------|--------|
| `economy_command_availability.gd` | Add `forester` to forest shortlist; network-aware storehouse/sawmill/workshop candidates |
| `economy_expansion_bot.gd` | Chain reorder; queue clear on advance; connected-site picks; no synthetic roads |
| `economy_bvb_setup.gd` | HQ road spine for proof profile |

**002 exit after changes:** 1 (not ready for Phase D hash commits per plan gate).

## Phase D — Smoke (blocked)

Deferred until `ECON_BOT_002` exits 0 twice. Observed stable hash for ECON-SMOKE-006; REP-SMOKE-001 needs replay path fix.

## Phase F — Tier 2

Not run (blocked).

## PR status

- Merge-ready: **no**
- Draft PR to be opened with this report as body
