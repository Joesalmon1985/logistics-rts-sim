# RUN_04 — Trial Bots, Replays, and Telemetry

## Purpose

Add bots, replay export/import, and diagnostics telemetry for economy and military trials.

## Branch Name

`feature/run-04-trial-bots-replays-telemetry`

## Prerequisite Documents

- `docs/architecture/04_bot_and_training_architecture.md`
- `docs/architecture/05_determinism_replay_save_load.md`
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` (complete)
- `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` (complete)
- `docs/audit/RUN_01_LESSONS_LEARNED.md`
- `docs/audit/RUN_02_LESSONS_LEARNED.md`
- `docs/audit/RUN_03_LESSONS_LEARNED.md`

## Pre-flight (before any RUN_04 implementation)

Before implementing new mechanics (see `.cursor/rules/10-run-verification-and-logging.mdc`):

1. Read `docs/audit/RUN_01_LESSONS_LEARNED.md`, `docs/audit/RUN_02_LESSONS_LEARNED.md`, and `docs/audit/RUN_03_LESSONS_LEARNED.md`.
2. Confirm RUN_01 kernel regression plus RUN_02 economy and RUN_03 military suites per `docs/verification/local_verification.md` — all exit 0.
3. Use `timeout` wrappers; export `GODOT_USER_DATA_DIR`; `--import` after fresh checkout or new `class_name` types.
4. Kill only confirmed stale project Godot processes; never broad `pkill -f Godot`.
5. Do not count aborted, timed-out, errored, or not-run commands as passes. If verification fails, **stop and report** before editing gameplay systems.

## Carryover from RUN_03

Apply these RUN_03 implementation lessons when building bots, replays, and telemetry (see `docs/audit/RUN_03_LESSONS_LEARNED.md`):

- **Float-free bot rollouts** — bots drive the same command API as humans/tests; every value they push into replay-hashed state must remain integer / fixed-point. Float creep in bot decisions breaks deterministic rollouts (BOT-DET-001) and replay hashes.
- **Reuse the exporter, do not fork it** — telemetry/diagnostics export should extend `MilitaryExporter` (`sim/military/military_export.gd`) and the economy exporter (`sim/tools/economy_export.gd`), not duplicate snapshot logic.
- **Use registered variant ids** — `military_trial`, `economy_trial`, `hybrid` (not `hybrid_trial`). A wrong id yields `unknown variant` (exit 1) that looks like a regression but is not.
- **Behaviour-proving tests over existence-only tests** — the turret `weapon`-at-spawn bug was caught only because a test asserted a downstream effect. Per-rule-ID hardening tests must assert state change + event/diagnostic + downstream effect, not mere field presence.
- **Re-pin hashes only with triple-run evidence** — and only for the smoke whose behaviour legitimately changed; never collapse a re-pin into an unrelated golden hash.

## In-Scope Work

- Formal bot interface (extends RUN_02 heuristic bot)
- Economy + military bots using observation/snapshot API
- Replay export/import JSON
- Telemetry/diagnostics export; bot_decision CSV
- BOT-SMOKE-*, REP-SMOKE-*
- **Per-rule-ID economy test hardening** (deferred from RUN_02) — add dedicated headless tests for each individual ECON-* rule ID listed in `docs/testing/01_rule_contract_test_matrix.md` (RUN_02 implemented coverage → Known coverage gap), cross-referenced with `docs/audit/RUN_02_LESSONS_LEARNED.md` ("Known remaining caveat"). Rule IDs include: ECON-WORKER-001/002/004/005, ECON-TOOL-001/003/004, ECON-FOREST-001/003/004/005, ECON-FOOD-001/003/004/005, ECON-MINE-002..006, ECON-UPGRADE-001/003/004/005, ECON-ROAD-003..006, ECON-PATH-002/003, ECON-PRI-002/003, ECON-CARR-*, ECON-CONS-001, ECON-DIAG-004, ECON-STALL-001, ECON-EVT-*.
- **Per-rule-ID military test hardening** (deferred from RUN_03) — expand RUN_03's DoD-level family coverage into dedicated 1:1 tests for each individual MIL-* rule ID listed in `docs/testing/01_rule_contract_test_matrix.md` (Military section), cross-referenced with `docs/audit/RUN_03_LESSONS_LEARNED.md` ("Risks for RUN_04"). No whole family is missing — RUN_03 already covers each family — so this is granularity expansion, not new mechanics. Each test asserts state change + event/diagnostic + downstream effect. Speculative families not yet implemented (engineer capture `MIL-CAPTURE-*`, veterancy `MIL-VET-*`, two-bot skirmish `MIL-BOT-*`) remain new scope for their owning runs, not hardening targets here.

Note: RUN_02 already requires a minimal heuristic economy bot for ECON-SMOKE-005. RUN_04 generalizes the bot framework and adds military bot.

## Out-of-Scope Work

- Neural training (RUN_06)
- Godot replay UI (RUN_05)
- Hybrid bots (RUN_08)

## Test-First Requirements

BOT-CMD-001, BOT-DET-001, REP-SMOKE-001.

## Implementation Requirements

- Bots are **in-engine GDScript** clients in the simulation core, using the same command validator as humans/tests (not Python; Python training is RUN_06)
- Replay files compatible with the kernel hash mechanism; follow the replay-file schema and import/replay path in `docs/architecture/05_determinism_replay_save_load.md` ("Replay File and Import (RUN_04)"). REP-SMOKE-001 = export → import → re-run → identical hash.
- **Military bot scope:** the RUN_04 military bot is a **single heuristic bot versus a scripted/idle opponent** on the existing `skirmish_1v1` / `generated_skirmish` fixtures (BOT-SMOKE-002 = "opponent defeated"). A symmetric **two-bot** skirmish (`MIL-BOT-*`) is **not** in RUN_04 scope; it is reserved for its owning run. Reuse `sim/economy/bot/heuristic_economy_builder_bot.gd` as the interface precedent.

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_04 section.

## Run Sizing (DDR-028)

RUN_04 combines bot framework, replay export/import, telemetry, and per-rule-ID ECON + MIL test hardening — a large run. Preferred plan is one dedicated long run. If it cannot complete safely, the single controlled A/B split allowed by DDR-028 may be used, with **per-rule-ID test hardening** as the coherent A/B boundary (ship bots + replay + telemetry first; hardening second). Do not silently fragment into many small runs, and do not call a skeleton complete.

## Acceptance Checklist

- [x] Economy bot completes tutorial scenario (builds on RUN_02 ECON-SMOKE-005 heuristic)
- [x] Military bot wins skirmish scenario
- [x] Replay export/import hash identical
- [x] Diagnostics visible in snapshot JSON
- [x] Deterministic bot rollouts
- [x] Per-rule-ID economy test hardening complete for all rule IDs listed in In-Scope and in `docs/testing/01_rule_contract_test_matrix.md` (RUN_02 Known coverage gap subsection)
- [x] Per-rule-ID military test hardening complete for all MIL-* rule IDs listed in In-Scope and in `docs/testing/01_rule_contract_test_matrix.md` (Military section), excluding the speculative `MIL-CAPTURE-*` / `MIL-VET-*` / `MIL-BOT-*` families reserved for their owning runs

Delivery confirmed by the RUN_04 implementation commit, `docs/testing/02_smoke_test_matrix.md`, `docs/testing/01_rule_contract_test_matrix.md`, and `docs/audit/RUN_04_LESSONS_LEARNED.md` (148/148 headless tests; all kernel/economy/military/bot smokes and failure-mode integrity exit 0 on the Linux/Godot reference machine).

> Runtime verification remains required on the Linux/Godot machine before merging to main. These boxes record delivery status; they do not represent any Windows runtime verification.

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
