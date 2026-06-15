# RUN_03 — Lessons Learned (Trial Military Game)

Branch: `feature/run-03-trial-military-game`

## What was implemented

The standalone Trial Military Game as a pure GDScript, headless, deterministic
simulation-core module under `godot_project/sim/military/`, all built on the shared RUN_01
axial hex map (no parallel grid), seeded RNG, and the shared command/event/snapshot/replay/hash
foundations.

Mechanic families (all DoD-level, behaviour-tested):

- Military state schema with namespaced `mil_*` buckets + integer-only accessors/factories.
- Data-driven config: buildings, units, weapons, armour types, damage modifiers, tech tree,
  power, resources, orders, scenarios.
- Resource fields → collector harvest/return → processor → trial credits.
- Building placement (build radius, multi-hex footprint, buildability), construction queue.
- Power supply/demand with underpowered gating of production/research/turrets/repair.
- Unit production queues + rally points; tech prerequisites unlocking advanced units.
- A* hex pathfinding, integer movement with re-pathing on blocked routes.
- Orders: move, attack, attack-move, patrol, guard, stop.
- Combat: target acquisition, weapon cooldowns, instant-hit and projectile weapons, armour/
  damage-type modifiers, destruction, victory/defeat.
- Fog/shroud/vision (unexplored/explored/visible) with scouting and powered-radar sight.
- Defence turret auto-targeting; repair depot heal-for-credits with stall on no credits.
- Seeded skirmish setup hook + fixed/seeded skirmish fixtures.
- Diagnostics, full snapshot layers, and `MilitaryExporter` (JSON/JSONL/CSV).
- Smokes MIL-SMOKE-001–009.

Final state: **68/68** headless tests pass (20 new RUN_03 tests); MIL-SMOKE-001–009 pass; the
three kernel smokes (economy/military/hybrid), economy smokes, and failure-mode integrity all
exit 0; `git diff --check` clean.

## What worked

- **Tick order from the corrected plan** (`meta → power → resources → tech → construction →
  production → orders → movement → vision → combat → damage → repair → victory → diagnostics →
  statistics`) made gating unambiguous: power is known before anything consumes it, vision is
  recomputed after movement, and combat reads fresh vision/positions.
- **Two-phase damage** (combat queues `pending_damage`, a separate damage step applies it) made
  simultaneous fire deterministic and gave clean projectile-vs-instant semantics.
- **Phase-by-phase commits** with a full test+smoke run at each boundary caught regressions
  immediately and kept the kernel verifiable throughout.
- **Reusing the shared map/RNG/hash** meant determinism "came for free" — MIL-SMOKE-009 and the
  determinism tests passed on the first deterministic build.

## What broke (and the fixes)

- **`spawn_building` didn't carry `weapon`** → defence turrets never fired. Fixed by copying
  `weapon` from the building def at spawn time. This changed hashed state, so the
  `military_trial` kernel smoke hash had to be re-pinned (triple-run stable) — the only hash
  re-pin in this run; economy/hybrid hashes untouched.
- **Test scenario footprint collisions** — early MIL-BUILD test placed a water tile inside the
  candidate building's 2×2 footprint and used an out-of-bounds "outside radius" coordinate.
  Lesson: when authoring placement tests, account for the **full footprint** and keep negative
  coordinates in-bounds so the intended rejection reason is the one asserted.

## Verification lessons

- Use the **registered variant id** (`hybrid`, not `hybrid_trial`) when running the hybrid
  kernel smoke; a wrong id yields `unknown variant` (exit 1) that looks like a regression but
  isn't. `local_verification.md` already lists the correct command.
- **Closure pass (post-RUN_03):** `test_scenario_missing_resource_map.gd` called the removed
  `CommandValidator.validate()` API and printed a `SCRIPT ERROR` while the suite still reported
  green — because GDScript aborts `_run_body()` on error but the old `TestCase.run()` always
  set `_run_completed = true`. Fixed by using `cmd.validate(state)` (same pattern as
  `test_econ_mine_001`) and by marking tests incomplete when `_run_body()` aborts before return.
- Re-pin hashes only with **triple-run** identical-hash evidence, and only for the smoke whose
  behaviour legitimately changed.

## Test/smoke lessons

- Behaviour-proving tests (state change + event/diagnostic + downstream effect) caught the
  turret `weapon` bug that an existence-only test would have missed.
- Determinism is cheapest to assert by running the same scenario twice and comparing
  `ReplayHash.compute_state_hash` (used in `test_mil_diag_001`, `test_mil_skirmish_001`, and
  MIL-SMOKE-009) rather than only pinning a literal hash.

## Command/process changes

- Added mandatory negative checks to `verify_failure_modes.gd`: **invalid military smoke id**
  and **invalid military scenario** must exit non-zero.
- `local_verification.md`, the smoke matrix, and the rule-contract matrix updated **during** the
  run (RUN_02 lesson), not only at closure. Test count is now 68; military smoke command and the
  re-pinned military hash are recorded.

## Risks for RUN_04

- Per-individual-rule-ID MIL-* tests are deferred to RUN_04 hardening (no whole family deferred).
  RUN_04 should expand the family-level tests into 1:1 rule-ID tests.
- Speculative families (engineer capture `MIL-CAPTURE-*`, veterancy `MIL-VET-*`, two-bot
  skirmish `MIL-BOT-*`) are not implemented; treat as new scope, not regressions.
- Bots will drive the same command API used here; the deterministic command/snapshot path is
  ready, but bot rollouts must keep float-free hashed state.

## Risks for RUN_07 (hybrid)

- Trial-only assumptions (direct unit micro, trial credits) are isolated in commands/config, not
  baked into shared schema — hybrid can remove credits and direct micro. Keep the `mil_*` bucket
  namespacing so economy + military state can coexist.
- Snapshot already emits empty `roads`/`road_nodes` to satisfy the cross-variant shape contract;
  hybrid composition should reuse, not fork, the exporter.

## Prompt improvements for future runs

- The single corrected tick-order in the plan prevented a class of subtle gating bugs; keep
  requiring an explicit, justified subsystem order before implementation.
- Calling out "DoD-level family coverage now, per-rule-ID granularity deferred" explicitly in
  the plan kept scope honest and avoided either over-building or false completion claims.

## Propagation (pre-RUN_04 housekeeping)

This subsection records where RUN_03 lessons were threaded into downstream planning docs
(2026-06-15), mirroring the RUN_02 propagation pattern.

| Document | Change | Why |
|----------|--------|-----|
| `docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` | Added `RUN_03_LESSONS_LEARNED.md` to prerequisites + pre-flight step 1; added "Carryover from RUN_03"; added per-rule-ID **military** hardening In-Scope item + Acceptance checkbox; clarified single-bot military scope; added replay-file schema reference + DDR-028 sizing note | RUN_04 agents read RUN_03 lessons; the MIL per-rule-ID deferral now has an explicit home (was ECON-only) |
| `docs/testing/01_rule_contract_test_matrix.md` | Added "RUN_04 per-rule-ID military test hardening (scheduled)" subsection cross-linking RUN_04 | Matrix and RUN_04 run doc agree on MIL hardening ownership, symmetric to ECON |
| `docs/testing/02_smoke_test_matrix.md` | Added RUN_04 per-rule-ID military hardening pass criteria + REP-SMOKE-001 replay-file note | Smoke matrix cross-links RUN_04 military hardening + replay import |
| `docs/architecture/05_determinism_replay_save_load.md` | Added "Replay File and Import (RUN_04)" schema + rules | REP-SMOKE-001 (export → import → identical hash) now has a concrete spec to implement against |
| `docs/runs/RUN_05_GODOT_2D_VIEWERS_FOR_TRIAL_GAMES.md` | RUN_03 carryover note: military snapshot/diagnostics layers render from snapshot only | Mirrors RUN_02 viewer guidance for MIL-* display layers |
| `docs/runs/RUN_08_HYBRID_BOTS_TELEMETRY_BALANCE.md` | RUN_03 carryover note: float-free military bot rollouts; reuse `MilitaryExporter` | Hybrid bot rollouts inherit RUN_03 determinism/export lessons |
| `docs/runs/RUN_07_HEADLESS_HYBRID_GAME.md` | **No edit** | Already reads RUN_03 lessons and has RUN_03 carryover/risks coverage |
