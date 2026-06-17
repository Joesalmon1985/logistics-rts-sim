# RUN_02.8d — Lessons Learned

Date: 2026-06-17

## What worked

- Targeted `TEST_FILTER=econ_bot_002` (~157s) isolated the buildings gate without full 900s suite loops.
- Root cause identified: `forester` was in bot `CHAIN_TYPES` but missing from `EconomyCommandAvailability` forest shortlist — chain stalled after woodcutter/storehouse.
- HQ proof-profile road spine in `EconomyBvbSetup` improved storehouse connectivity.
- Removing synthetic `PlaceRoadCommand` from `_next_queued_road` eliminated rejected-command failures.
- Regression guards B–E/F/G remain green after bot/availability changes.

## What broke / surprised

- `ECON_BOT_002` still fails at **2/5** bot-built buildings per player at 800 ticks (symmetric ~b=2, d≥380, r≥41).
- Only **4** `PlaceBuildingPlanCommand` entries fire in 800 ticks; forester/sawmill/workshop plans never issue before road cap.
- Storehouse-first chain order regresses p2 logistics (stall/delivery); woodcutter-first works better with HQ spine.
- Expanded storehouse/sawmill/workshop candidate search increases shortlist cost (~130s+ of 800-tick match wall time).
- `ECON-SMOKE-006` hash stable at `sha256:27d91679…` on branch and `ae78996` (functional checks pass with `--no-hash-check`).
- `REP-SMOKE-001` still fails export/import `hash_mismatch` (pre-existing on `ae78996`).

## Verification

| Command | Exit | Notes |
|---------|------|-------|
| `TEST_FILTER=econ_bot_002 run_tests.gd` | 1 | buildings gate — p1/p2 b=2 need 5 |
| `TEST_FILTERS=b001,c001,d001,e001,f001,g001 run_tests.gd` | 0 | 6/6 pass |
| `run_economy_smoke.gd --smoke=006` | 1 | hash mismatch only |
| `run_bot_smoke.gd --smoke=003` | 1 | REP hash_mismatch |
| `ae78996` quick_metrics @800 | — | same b=1 pattern pre-forester fix |

## Risks for next run

- Forester placement after road cap requires either reserved road budget or network-aware forest targets without weakening G001 cap (45).
- Shortlist expansion needs performance guard (cap node scans) before parallel wrapper is authoritative.
- Do not re-pin smoke hashes until `ECON_BOT_002` is green (plan gate).

## Suggested next run

Continue on `feature/run-02-8c-test-runtime-efficiency`: finish forester+ chain placement within 800 ticks, then Phase D smoke hash work, then Tier 2 closure.
