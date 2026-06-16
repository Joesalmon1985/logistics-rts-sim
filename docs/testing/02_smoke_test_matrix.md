# Smoke Test Matrix

End-to-end smoke scenarios per run. Pass = all criteria met headlessly unless noted.

## Smoke reporting rules (all runs)

- Wrap every smoke command in a `timeout` (see `docs/verification/local_verification.md`); exit code `0` is required for a pass.
- Each smoke resolves to exactly one status: **passed**, **failed**, **errored**, **timed out**, or **not run**.
- A report **must not** collapse `errored`, `timed out`, or `not run` into `passed`. Exit code `124` is a timeout (failure); an aborted/killed task is `not run`, never a pass.
- Where practical, include the negative/failure-mode checks (`sim/tools/verify_failure_modes.gd`) that must exit non-zero.
- Report the exact command run and its exit code. See `.cursor/rules/10-run-verification-and-logging.mdc`.

## RUN_01 — Core Simulation Kernel

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| KERN-SMOKE-001 | Create game from scenario fixture | game_created event |
| KERN-SMOKE-002 | Submit noop command, tick 10 | tick_advanced × 10 |
| KERN-SMOKE-003 | Export snapshot | Valid JSON schema_version |
| KERN-SMOKE-004 | Replay hash | Hash matches expected |

Verification must use timeout-wrapped Godot commands; exit code `0` required. See `docs/verification/local_verification.md` and `sim/tools/verify_failure_modes.gd` for failure-mode integrity checks.

## RUN_02 — Trial Economy Game

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| ECON-SMOKE-001 | Road network (terrain/slope) | Carriers path; road diagnostics OK |
| ECON-SMOKE-002 | Timber → planks | Planks in storage |
| ECON-SMOKE-003 | Full building | construction_completed event |
| ECON-SMOKE-004 | 500-tick replay | Hash match |
| ECON-SMOKE-005 | Heuristic bot → Workshop | Full logistics chain via bot; see `02_` §38 |

### RUN_02.7 planned smoke (ratified)

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| ECON-SMOKE-006 | HQ/storehouse → road network → construction site | Delivery job created; route through road nodes; carrier assigned per job; material delivered; `builder_work` progresses after delivery; diagnostics correct; full snapshot contains logistics state; replay hash pinned |

RUN_02.7 also requires **viewer observability** of logistics mechanics (functional visibility from snapshot/export — not polish). See [`docs/runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md) Phase 9; extend GOD-SMOKE-003 or add a deterministic viewer review/capture path.

RUN_02 smoke runner: `sim/tools/run_economy_smoke.gd` (ECON-SMOKE-001–005 today; ECON-SMOKE-006 added in RUN_02.7, exit `0`). ECON-SMOKE-005
completes the workshop in **~5s wall clock**, well under its `BOT_MAX_TICKS` budget — the earlier
"~450s" runtime note was a debugging-era figure and is obsolete after the bot/placement/logistics fixes.

Bundled scenario maps each have a **dedicated** scenario test under `sim/tests/economy/`
(`test_scenario_<map>.gd`), run as part of `sim/tools/run_tests.gd`. Each loads the map, runs the
scenario, asserts scenario-specific state/diagnostics, and asserts per-scenario replay-hash
determinism: `tiny_success_map`, `blocked_route_map`, `missing_resource_map`, `priority_test_map`, `storage_full_map`, `pathfinding_test_map`, `forest_depletion_regrowth_map`, `mine_food_support_map`, `tool_shortage_worker_block_map`, `building_upgrade_map`.

## RUN_03 — Trial Military Game

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| MIL-SMOKE-001 | Build base | Hub + generator + processor + barracks placed |
| MIL-SMOKE-002 | Harvest/process | Credits increase from collector → processor loop |
| MIL-SMOKE-003 | Maintain power | Unpowered production blocked; power restored |
| MIL-SMOKE-004 | Unlock tech | Prerequisite research; advanced unit queued |
| MIL-SMOKE-005 | Scout enemy | Fog/shroud updates; enemy revealed in vision |
| MIL-SMOKE-006 | Attack enemy | Attack-move or patrol destroys hostile unit/building |
| MIL-SMOKE-007 | Repair | Damaged unit or building healed at repair depot |
| MIL-SMOKE-008 | Win/loss | Command hub destroyed → defeat; opponent hub destroyed → victory |
| MIL-SMOKE-009 | Replay/diagnostics | Replay hash match; diagnostics export readable |

RUN_03 smoke runner: `sim/tools/run_military_smoke.gd` (all nine smokes, exit `0`; whole suite
< 30s wall clock on the reference machine). MIL-SMOKE-009 asserts a deterministic replay hash
(two runs from the same seed must match) and writes snapshot/events/metrics/unit/building exports
via `MilitaryExporter` (`sim/tools/military_export.gd`). Failure-mode integrity
(`sim/tools/verify_failure_modes.gd`) includes mandatory **invalid military smoke id** and
**invalid military scenario** checks, both of which must exit non-zero.

Bundled skirmish fixtures: fixed `skirmish_1v1` map (`scenarios/military/skirmish_1v1.json`) +
seeded `generated_skirmish` (`scenarios/military/generated_skirmish.json`) plus the in-code seeded
skirmish hook `MilitarySkirmish.build_scenario(seed, players, w, h)` used by MIL-SMOKE-009.

## RUN_04 — Trial Bots / Replay / Telemetry

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| BOT-SMOKE-001 | Economy bot completes tutorial | economy_success |
| BOT-SMOKE-002 | Military bot wins skirmish | opponent defeated |
| REP-SMOKE-001 | Export/import replay | Identical hash |

**RUN_04 per-rule-ID economy test hardening:** Delivered in RUN_04 — 39 dedicated tests under
`sim/tests/economy/hardening/`, registered in `run_tests.gd`. Pass criteria met: each rule ID
has a dedicated test asserting state change + event/diagnostic + downstream effect; suite exit 0.

**RUN_04 per-rule-ID military test hardening:** Delivered in RUN_04 — 38 dedicated tests under
`sim/tests/military/hardening/`, registered in `run_tests.gd`. Pass criteria met: each rule ID
has a dedicated test asserting state change + event/diagnostic + downstream effect; suite exit 0.

**RUN_04 bot/replay smokes:** `sim/tools/run_bot_smoke.gd` — BOT-SMOKE-001/002, REP-SMOKE-001
(smoke id `003`), exit `0`.

**REP-SMOKE-001 note:** export → import → re-run → identical hash, per the replay-file schema in
`docs/architecture/05_determinism_replay_save_load.md` ("Replay File and Import (RUN_04)").

## RUN_05 — Godot Trial Viewers

RUN_05 uses **two smoke runners** on **three verification tracks**:

| Runner | Track | Headless? |
|--------|-------|-----------|
| `sim/tools/run_viewer_smoke.gd` | 1 — contract smokes | Yes (`--headless`) |
| `sim/tools/run_viewer_pixel_smoke.gd` | 1.5 — viewport pixels | No (requires display) |

Track 1 verifies snapshot render-data, the command-availability bridge, and command round-trip —
not visual readability. Track 1.5 proves the SubViewport shows terrain and step/command pixel
changes. Human readability is verified separately via **windowed visual review** (Track 2) in
`docs/verification/local_verification.md`; that review is not a headless smoke.

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| GOD-SMOKE-001 | Load economy + military demo snapshots headlessly | Economy render data includes carriers + tree_density; economy demo shows observable progression within 50 ticks; military render data valid; no `SCRIPT ERROR`; exit 0 |
| GOD-SMOKE-002 | Sim-provided command round-trip via viewer adapter | Economy: road submit + step shows fingerprint delta; military: move changes unit position; attack issues order and reduces target HP or emits combat/damage/order event; build increases construction-site render count; rejected invalid command; `available_commands[]` excluded from replay hash |
| GOD-SMOKE-003 | Economy logistics visibility | Production demo: 30-tick chain delta (forest density, stores, routes); build tutorial: site placement, road connect or pre-connected, progress or completion within 120 ticks; exit 0 |
| GOD-SMOKE-004 | Viewport pixel observability (non-headless) | VIS-LAYOUT-001 map size + non-blank pixels; GOD-CAPTURE-001 grass/forest visible; VIS-PIX-CHAIN-001 step delta; VIS-PIX-BUILD-001 site visible after place; military entity colors; exit 0 via `run_viewer_pixel_smoke.gd` |

RUN_05 smoke runners:

- **Track 1:** `sim/tools/run_viewer_smoke.gd` (`--smoke=all`, exit `0`, headless). Unknown smoke id
  must exit non-zero (checked by `verify_failure_modes.gd`).
- **Track 1.5:** `sim/tools/run_viewer_pixel_smoke.gd` (exit `0`, non-headless). Optional standalone
  GOD-CAPTURE-001 assert via `run_viewer_capture.gd --assert`.

## RUN_06 — Neural Training

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| TRN-SMOKE-001 | Deterministic rollout protocol | 10 episodes × 100 ticks, stub policy, identical command logs |
| TRN-SMOKE-002 | Replay hash equivalence | Live rollout hash == `verify_replay` hash |
| TRN-SMOKE-003 | Training loop completes | BC train economy + military; checkpoints + `metrics.json` + `evaluation.csv`; no weights in git (**alone insufficient for RUN_06 complete**) |
| TRN-IMPROVE-ECON-001 | Economy improvement | Paired eval: trained mean primary metric > random and > untrained over frozen seeds |
| TRN-IMPROVE-MIL-001 | Military improvement | Paired eval: trained mean primary metric > random and > untrained over frozen seeds |

Godot runner: `sim/tools/run_training_smoke.gd` (TRN-SMOKE-001/002). Python: `pytest python_tools/tests/ -v`.

## RUN_06b — Policy / Evaluation Contract

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| POLICY-CONTRACT-001 | Legal commands include pass | Non-empty legal list with engine-owned `NoopCommand` |
| POLICY-CONTRACT-002 | Legal commands apply cleanly | No SCRIPT ERROR on enumerated commands |
| POLICY-CONTRACT-003 | Scoring wrapper legality | Python scoring policy selects only legal commands |
| EVAL-SMOKE-001 | Economy eval differentiation | Scripted-good beats pass-only; policies differ |
| EVAL-SMOKE-002 | Military eval differentiation | Scripted-good beats pass-only; policies differ |
| EVAL-DETERMINISM-001 | Deterministic eval episodes | Same policy + seed + scenario → identical outcome/hash |
| NO-BC-GATE-001 | BC not eval gate | RUN_06b acceptance excludes BC accuracy |

Godot runner: `sim/tools/run_policy_eval_smoke.gd`. Python: `test_policy_contract.py`, `test_eval_smoke.py`, `policy_eval.py`.

## RUN_07 — Headless Hybrid

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| HYB-SMOKE-001 | Logistics-fed construction | military_construction_completed via wares |
| HYB-SMOKE-002 | Logistics-fed production | unit_spawned after ware delivery |
| HYB-SMOKE-003 | Power/radar/tech | Integrated systems functional |
| HYB-SMOKE-004 | Delegated attack | Hostile hub destroyed via DelegatedAttackCommand |
| HYB-SMOKE-005 | Defence priorities | Patrol zone or guard policy engages hostiles |
| HYB-SMOKE-006 | Repair priority | Repair depot heals with ware-fed jobs |
| HYB-SMOKE-007 | Reject direct move | MoveUnitCommand rejected for human |
| HYB-SMOKE-008 | Win/loss/stall | Victory, defeat, or stall diagnostic detected |
| HYB-SMOKE-009 | Replay hash | Hash match |

## RUN_08 — Hybrid Bots / Balance

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| HYB-BOT-001 | Hybrid bot 200 ticks | No deadlock stall; delegated attack/defence/repair used |
| HYB-BOT-002 | Hybrid bot skirmish | Two bots complete with win/loss; fog-respecting |
| BAL-SMOKE-001 | Balance scenario | Both players viable path |

## RUN_09 — Godot Hybrid Observer

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| GOD-HYB-001 | Hybrid snapshot render | Economy + military + fog/shroud layers |
| GOD-HYB-002 | Delegated order UI | Attack, patrol, guard, repair priority commands accepted |
| GOD-HYB-003 | Replay/spectator | Snapshot sequence displays without local rule inference |

## RUN_10 — Human Playable Hybrid

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| HUM-SMOKE-001 | Tutorial completable | Manual test checklist |
| HUM-SMOKE-002 | Touch targets | Minimum size met |

## RUN_11 — Expanded Content

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| EXP-SMOKE-001 | Load campaign mission 2 | Scenario starts |
| EXP-SMOKE-002 | New map | Hash stable |

## RUN_12 — Wi-Fi Multiplayer

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| NET-SMOKE-001 | Two clients same LAN | Same hash at tick 100; command sync identical |
| NET-SMOKE-002 | Desync injection detected | Pause triggered |
| NET-SMOKE-003 | Fog/intel sync | Per-client intel consistent with authoritative snapshot |
| PKG-SMOKE-001 | Export APK/IPA | Builds without error |
