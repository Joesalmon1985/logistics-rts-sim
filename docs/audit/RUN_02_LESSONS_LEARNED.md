# RUN_02 Lessons Learned — Trial Economy Game

## What worked

- Test-first economy package under `godot_project/sim/economy/` integrated cleanly with RUN_01 kernel (command pipeline, snapshots, replay hash, variant registry).
- Headless harness scaled to **48 tests** (13 RUN_01 + 35 economy) with deterministic CLI pass/fail.
- ECON-SMOKE-001..005 and heuristic bot (ECON-SMOKE-005) validate real logistics-fed construction on `tiny_success_map`.
- Integer-only hashed state preserved; `EconomyConfig` kept out of `GameState.entities` meta.

## What broke / surprises

- **Paused production/buildings never resumed** until `_tick_building` allowed paused states to re-evaluate each tick.
- **Blocked construction sites stopped requesting materials** — must request for `CONS_BLOCKED` as well as `CONS_ACTIVE`.
- **Completed construction sites blocked footprints** — exclude `CONS_COMPLETED` / `CONS_CANCELLED` from overlap checks.
- **Bot axis-aligned road paths are invalid on hex grids** — must step through hex neighbours (greedy distance descent).
- **Build-radius + 2×2 footprints** — smelter/workshop anchors must keep all footprint hexes inside radius and avoid HQ/building overlap.
- **Stale delivery jobs** (`waiting_for_ware` with empty HQ source) blocked new jobs — source reassignment + `_has_pending_job` fix.
- **`economy_init.gd`** — `_create_hq` referenced `scenario` without parameter (compile error caught by smoke).

## economy_trial hash re-pin (deliberate)

| | Value |
|---|--------|
| Old (RUN_01 stub) | `sha256:4fe90e016f00e3b623f90ef1d984cc339d5dd696adfffd6eaf7efa6f64c0ca6a` |
| New (RUN_02 real economy) | `sha256:1d69994a924390478e89397dc3e0ff9b5d65118eeb2995fe1de8c53a5898332b` |
| Command | `run_smoke.gd --variant=economy_trial --ticks=10 --seed=1` on `scenarios/kernel_smoke.json` |
| Verified | Triple-run identical hash before pin |
| Reason | Real economy subsystems (init, tick, entities) replace stub variant behaviour |
| Untouched | `military_trial`, `hybrid` golden hashes |

## Verification commands (RUN_02)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=military_trial --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_military.json
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=hybrid --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_hybrid.json
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

## Closure pass (pre-merge)

Narrow closure pass on `feature/run-02-trial-economy-game` — no new economy features, only acceptance hardening:

- **Per-scenario tests split out.** The single bundled scenario test was replaced with **10 dedicated
  `test_scenario_<map>.gd`** files (one per §37 map). Each loads the map, runs it, asserts
  scenario-specific state/diagnostics, and asserts per-scenario replay-hash determinism. Suite is now
  48 tests. This closes the §35/§37 "one scenario test per bundled map" requirement concretely.
- **ECON-SMOKE-005 performance note corrected.** The earlier "~450s" figure was a debugging-era
  observation. After the bot/placement/logistics fixes the workshop completes in **~5s wall clock**,
  well under the `BOT_MAX_TICKS` budget; the cap is never reached. The 450s concern is obsolete.
- **Test matrices updated** (`docs/testing/01_…`, `02_…`) to record actual RUN_02 coverage and the
  honest per-rule-ID gap below.

## Known remaining caveat (do not over-claim)

`02_trial_economy_game_requirements.md` §35/§460 asks that **every individual rule ID** in §19–§31
additionally gain a test. RUN_02 delivers **at least one dedicated behaviour test per family**
(state change + event/diagnostic + downstream effect), plus the five smokes and the 10-scenario
suite, and every acceptance-checklist family in `RUN_02_TRIAL_ECONOMY_GAME.md` is implemented and
exercised. It does **not** yet have a separate test file for each individual rule ID (e.g.
ECON-WORKER-001/002/004/005, ECON-TOOL-001/003/004, ECON-FOREST-001/003/004/005, ECON-FOOD-001/003/004/005,
ECON-MINE-002..006, ECON-UPGRADE-001/003/004/005, ECON-ROAD-003..006, ECON-PATH-002/003, ECON-PRI-002/003,
ECON-CARR-*, ECON-CONS-001, ECON-DIAG-004, ECON-STALL-001, ECON-EVT-*). Those mechanics exist and are
covered indirectly; per-rule-ID test expansion is **explicit RUN_04 scope** (see
`docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` and `docs/testing/01_rule_contract_test_matrix.md`
RUN_04 per-rule-ID subsection). RUN_02 is complete at the family/DoD level; the strict per-rule-ID
test enumeration is the one explicit deferral to RUN_04.

## Risks for RUN_03 (Trial Military Game)

- Creating a **square grid** or separate passability map (forbidden by DDR-029).
- Credits/combat floats in hashed state — breaks replay hash (DDR-027).
- Duplicating hex map code instead of importing RUN_01 primitives.
- **Axis-aligned / axial Manhattan paths on hex grids are invalid** — movement and bot pathing must step through hex neighbours (greedy distance descent), not q/r Manhattan shortcuts.
- **Multi-hex building footprints** — check **every** footprint hex against build radius, HQ footprint, and completed/cancelled sites (exclude `CONS_COMPLETED` / `CONS_CANCELLED` from overlap checks).
- Skipping RUN_01 kernel regression before adding MIL-* mechanics.

## Risks for RUN_04 (Bots, Replays, Telemetry)

- **Per-rule-ID economy test hardening is explicit RUN_04 scope** — do not drop the deferral listed under "Known remaining caveat" and in `docs/testing/01_rule_contract_test_matrix.md` (RUN_02 implemented coverage subsection).
- Bot/smoke runtime can grow with chain depth — keep timeout wrappers generous (ECON-SMOKE-005 caps at 20000 bot ticks but finishes in ~5s).
- Per-tick cost rises with delivery-job churn; prune finished jobs and avoid doomed job creation.
- Heuristic bot pathing must use hex neighbour steps (same lesson as ECON-SMOKE-005 road placement).

## Risks for RUN_05 (Godot Trial Viewers)

- Economy snapshot display layers exported by RUN_02 (`ECON-SNAP-002`, `ECON-DIAG-001`) must render from snapshot JSON only — no re-deriving logistics rules in `client/`.

## Risks for RUN_06 (Neural Training)

- Observation encoders must consume exported economy snapshot/diagnostics fields from RUN_02 — not Godot scene or render state.

## Risks for RUN_07 (Headless Hybrid Game)

- Composing economy + military without shared map/event/snapshot contracts.
- Job-churn perf when economy delivery jobs run alongside military ticks — prune finished/doomed jobs.
- Paused production/buildings must re-evaluate each tick so logistics-fed military construction can resume when inputs return.
- Introducing credits in hybrid (forbidden by DDR-031).

## Risks for RUN_08 (Hybrid Bots, Telemetry, Balance)

- Hybrid bot rollouts inherit economy job-churn perf — monitor stall diagnostics (`ECON-STALL-001` family) under bot load.
- Bot timeout wrappers must stay generous for logistics-heavy scenarios.

## Risks for RUN_09–12

No direct RUN_02-specific edits required at propagation time. RUN_09 inherits viewer patterns from RUN_05; RUN_10–12 inherit hybrid/determinism concerns primarily from RUN_07 and RUN_01. Revisit only if a concrete RUN_02 lesson surfaces during those runs.

## Recommended changes to future run prompts/docs

- Require reading `docs/audit/RUN_02_LESSONS_LEARNED.md` in RUN_03+ pre-flight (alongside RUN_01 lessons).
- Always validate hex paths with neighbour steps, not axial q/r Manhattan paths.
- When placing multi-hex buildings, check **every** footprint hex against HQ footprint, build radius, and completed sites.
- After pausing a building, ensure the next tick can resume when inputs/workers return.
- Prune finished delivery jobs and avoid creating doomed jobs (empty HQ source, blocked routes).
- Keep hashed state integer-only; keep config objects out of `GameState.entities` meta.
- Report verification status as passed / failed / errored / timed out / not run — never collapse categories.

## Pre-flight checklist for RUN_03

1. `git checkout feature/run-03-trial-military-game` (from updated `main` after RUN_02 merge).
2. Confirm clean working tree.
3. Read `docs/audit/RUN_01_LESSONS_LEARNED.md` and this document.
4. `export GODOT=...` and `GODOT_USER_DATA_DIR=godot_project/.godot_user`.
5. `pgrep -af Godot` — kill stale project processes if any.
6. `timeout 120s $GODOT --headless --path godot_project --import` (if new `class_name` types added since last import).
7. `timeout 120s ... run_tests.gd` → expect 48/48 (13 RUN_01 + 35 RUN_02), exit 0.
8. Three kernel smoke commands → exit 0 each.
9. `timeout 120s ... run_economy_smoke.gd` → exit 0.
10. `timeout 120s ... verify_failure_modes.gd` → exit 0.
11. Read `RUN_03_TRIAL_MILITARY_GAME.md` acceptance checklist and "Carryover from RUN_02" note.
12. Only then begin MIL-* test-first implementation.

## Propagation (docs/run-02-lessons-propagation)

This subsection records where RUN_02 lessons were threaded into downstream planning docs (2026-06-14).

| Document | Change | Why |
|----------|--------|-----|
| `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` | Added RUN_02 lessons to prerequisites/pre-flight; added "Carryover from RUN_02"; updated test count to 48/48 | Future RUN_03 agents read RUN_02 lessons and apply hex pathing, footprint, job-churn, integer-state lessons |
| `docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` | Added pre-flight; In-Scope item + acceptance checkbox for per-rule-ID economy test hardening with explicit rule-ID list | Closes deferral gap — hardening is explicit RUN_04 scope, not an orphan caveat |
| `docs/runs/RUN_05_GODOT_2D_VIEWERS_FOR_TRIAL_GAMES.md` | RUN_02 carryover note on snapshot/diagnostics rendering | ECON-SNAP-002 / ECON-DIAG-001 fields must render from snapshot only |
| `docs/runs/RUN_06_NEURAL_TRAINING_FOR_TRIAL_GAMES.md` | RUN_02 carryover note on observation encoding | Observations must use exported snapshot/diagnostics, not scene state |
| `docs/runs/RUN_07_HEADLESS_HYBRID_GAME.md` | RUN_02 carryover note on job churn, pause/resume, hex pathing | Composition risks when economy logistics meet military ticks |
| `docs/runs/RUN_08_HYBRID_BOTS_TELEMETRY_BALANCE.md` | RUN_02 carryover note on bot job-churn perf and timeouts | Hybrid bot rollouts inherit economy delivery-job churn |
| `docs/testing/01_rule_contract_test_matrix.md` | Added RUN_04 per-rule-ID hardening subsection; tightened Known coverage gap wording | Matrix and RUN_04 run doc now agree on hardening ownership |
| `docs/testing/02_smoke_test_matrix.md` | Added RUN_04 per-rule-ID hardening pass criteria block | Smoke matrix cross-links RUN_04 acceptance |
| `docs/runs/RUN_09`–`RUN_12` | **No edits** | No direct, concrete RUN_02 lesson required at propagation time; RUN_09 inherits via RUN_05, RUN_10–12 via RUN_07/RUN_01 |
