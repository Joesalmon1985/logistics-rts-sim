# RUN_04 — Lessons Learned (Trial Bots, Replays, Telemetry)

Branch: `feature/run-04-trial-bots-replays-telemetry`

## What was implemented

- Formal `Bot` interface + `BotRunner` (snapshot → decide → `CommandValidator` → tick).
- `HeuristicEconomyBot` (formal `decide()` contract) and `HeuristicMilitaryBot` (single bot vs idle opponent on `skirmish_1v1`).
- `ReplayRecorder` + `ReplayFile` (export/import with validation-result recording; resubmit through `CommandRegistry`/`CommandValidator`).
- `run_bot_smoke.gd` (BOT-SMOKE-001/002, REP-SMOKE-001), `run_replay.gd` CLI.
- Extended `EconomyExporter` / `MilitaryExporter` with replay JSON + military `bot_decision` CSV.
- Per-rule-ID hardening: 39 economy + 38 military dedicated tests.
- Framework tests: BOT-CMD-001, BOT-DET-001, REP-SMOKE-001 (incl. tamper detection).
- Failure-mode integrity: invalid bot smoke id, invalid replay verify.

Final state: **148/148** headless tests pass; all kernel/economy/military/bot smokes exit 0; failure-mode integrity exit 0.

## What worked

- Branch-before-preflight kept Godot import metadata off `main`.
- Reusing `HeuristicEconomyBuilderBot` build-plan logic with snapshot-based HQ lookup (`anchor_q`/`anchor_r`) — initial bug was reading `q`/`r` on buildings.
- Recording **all** submitted commands with acceptance/rejection in replay files gives stronger determinism coverage.
- Subagent parallelization for 77 hardening test files kept the run feasible in one session.
- Military bot wins skirmish with build → queue → multi-unit attack-move pattern (~20s wall clock).

## What broke (and fixes)

- **`ReplayFile.ReplayRecorder` nested class** — `ReplayFile.ReplayRecorder.new()` returned null in Godot 4.5; split to standalone `class_name ReplayRecorder`.
- **`ReplayFile.load` name collision** — shadowed Godot built-in `load()`; renamed to `load_from_path`.
- **Economy bot HQ roads from (0,0)** — snapshot buildings use `anchor_q`/`anchor_r`, not `q`/`r`.
- **`test_mil_vision_003`** — missing helper + axial offset vs hex-distance sight check; fixed with distance loop.

## Verification lessons

- Actual verified test count: **148** (not the ~146 estimate). Report counts from `run_tests.gd` output only.
- BOT-SMOKE-001 completes in ~8s; BOT-SMOKE-002 in ~20s; REP-SMOKE-001 in ~5s on reference machine.
- Wrap bot smokes in generous timeouts (`600s` for full suite) even though wall clock is short.

## Risks for RUN_05 (Godot viewers)

- Bot decision CSV and replay JSON are ready for viewer/debug tooling; render from snapshot/diagnostics only.
- Replay files embed full scenario objects — viewers must not mutate on import.

## Risks for RUN_06 (Neural training)

- `BotRunner` + `ReplayRecorder` provide the command-log pattern Python training will need.
- BOT-DET-001 proves deterministic bot rollouts — neural policy must meet same bar.

## Risks for RUN_08 (Hybrid bots)

- Military/economy bot patterns are separate; hybrid bot must compose both without float creep.
- Reuse exporters; do not fork snapshot logic.

## Prompt improvements for future runs

- Always branch before Godot `--import` when starting a new run.
- Avoid naming methods `load` in GDScript modules (built-in collision).
- For snapshot-based bots, verify field names against exporter output (`anchor_q` not `q`).
- Per-rule-ID hardening: use subagents or batch generation; fix compile errors before full suite run.
