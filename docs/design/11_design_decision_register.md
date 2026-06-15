# Design Decision Register

Authoritative record of major design decisions made during RUN_00 documentation work, surfaced for human review before implementation runs begin.

Status values:

- **Accepted** — decided, consistently reflected across docs, safe for agents to rely on.
- **Proposed** — written into docs but not yet ratified; an agent may proceed but should not treat it as immovable.
- **Needs Human Confirmation** — genuinely uncertain or carries project-level risk; an autonomous run must **not** silently resolve it. If a run is blocked on one of these, stop and report.

This register is the single place where uncertain decisions live. Do not bury uncertain choices in other documents.

---

## DDR-001 — The Godot/GDScript simulation core is authoritative

- **Decision:** All gameplay rules, state, and randomness live in an authoritative **Godot/GDScript simulation core** (pure scripts/resources/classes), separate from UI scenes and rendering. *(Revised by DDR-026 on the RUN_00 architecture decision pass — previously "Python simulation is authoritative.")*
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** Single source of truth enables deterministic tests, replay, bots, and multiplayer sync without rule duplication, while shipping inside the Godot app with no mobile Python runtime.
- **Consequences:** Every client (Godot UI, bot, agent, peer) round-trips through the core. The core must not depend on scene tree, Node/sprite positions, animation, frame delta, physics, cameras, UI nodes, input events, or wall-clock time.
- **Documents affected:** `.cursor/rules/03-godot-sim-authority.mdc`, `docs/architecture/00_`, `01_`, `docs/design/00_`.
- **Open questions:** None. See **DDR-026** for the full pivot rationale.

## DDR-002 — The Godot visual client/UI is non-authoritative

- **Decision:** The Godot **visual/UI/client layer** only renders snapshots, collects input, serializes commands, and shows diagnostics/replays. It is distinct from the authoritative GDScript simulation core (DDR-001).
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** Keeps rules in the simulation core and prevents rule drift into scenes/rendering.
- **Consequences:** UI scenes may never compute economy, combat, pathfinding, or RNG outcomes; "Godot is authoritative" is never an accurate phrasing — only the **simulation core** is.
- **Documents affected:** `.cursor/rules/04-godot-boundary.mdc`, `docs/architecture/03_`, `docs/design/06_`.
- **Open questions:** None.

## DDR-003 — All clients use the same command validation/execution pipeline

- **Decision:** Humans (via UI), bots, neural agents, and multiplayer peers submit commands through one validator/apply path in the simulation core.
- **Status:** Accepted
- **Rationale:** Guarantees fairness, replayability, and a single attack surface for validation.
- **Consequences:** No privileged side-channel for bots except scenario fixtures in tests.
- **Documents affected:** `docs/architecture/00_`, `02_`, `04_`, `.cursor/rules/03-`.
- **Open questions:** None.

## DDR-004 — Deterministic seeded tick simulation

- **Decision:** Discrete monotonic ticks; all randomness from a seeded RNG stored in state; fixed subsystem order per variant.
- **Status:** Accepted
- **Rationale:** Determinism underpins replay, tests, training, and multiplayer sync.
- **Consequences:** No ad-hoc/unseeded RNG and no wall-clock or frame-delta in authoritative rules. RNG state must be serialized.
- **Documents affected:** `docs/architecture/01_`, `05_`, `docs/design/02_` §6–§7, `03_` §6.
- **Open questions:** Integer/fixed-point math policy — see **DDR-027** (Accepted).

## DDR-005 — Event log and snapshot export are core architecture

- **Decision:** Every state change emits a serializable, append-only event; snapshots are versioned JSON sufficient to render the game.
- **Status:** Accepted
- **Rationale:** Enables diagnostics, replay, observation encoding, and client rendering without exposing internal state.
- **Consequences:** New mechanics must declare events and snapshot fields; `schema_version` bumps on breaking changes.
- **Documents affected:** `docs/architecture/02_`, requirement docs §events/§snapshot sections.
- **Open questions:** None.

## DDR-006 — Replay support required from early implementation

- **Decision:** Replay hash verification is built in RUN_01 and required by every later run's scenarios.
- **Status:** Accepted
- **Rationale:** Catches non-determinism immediately rather than after large feature work.
- **Consequences:** Every scenario fixture carries an `expected_hash` (filled after first verified run); CI runs replay on sim-touching changes.
- **Documents affected:** `docs/architecture/05_`, `docs/runs/RUN_01_`, requirement determinism sections.
- **Open questions:** None.

## DDR-007 — Project developed in large autonomous runs

- **Decision:** Work proceeds as RUN_00 (docs) through RUN_12, each a milestone with tests, smoke verification, and a final report.
- **Status:** Accepted
- **Rationale:** Bounded, reviewable units of autonomous work.
- **Consequences:** Each run reads its prerequisite docs and stays in scope.
- **Documents affected:** `.cursor/rules/01-`, `docs/runs/*`, `README.md`.
- **Open questions:** RUN_02 / RUN_03 / RUN_07 may be too large for a single run — see **DDR-028**.

## DDR-008 — Test-first development is mandatory

- **Decision:** Deterministic headless tests precede or accompany every major mechanic; tests reference rule IDs; tests are never weakened to pass.
- **Status:** Accepted
- **Rationale:** Locks requirements to verifiable behaviour and prevents silent scope erosion.
- **Consequences:** No skipping/disabling tests without explicit approval; rule matrix drives test creation. Gameplay-rule tests target the GDScript simulation core via a headless Godot test harness (see DDR-032); Python pytest is reserved for Python training/tooling.
- **Documents affected:** `.cursor/rules/02-`, `docs/testing/00_`, `01_`.
- **Open questions:** None.

## DDR-009 — Trial Economy Game is the permanent economy engine

- **Decision:** The Trial Economy Game is the permanent abstract economy layer for the hybrid game, not a throwaway prototype.
- **Status:** Accepted
- **Rationale:** Hybrid military construction/production consume its wares and logistics; rebuilding it later would be wasteful.
- **Consequences:** Economy schema must expose hooks for military/hybrid/multiplayer without a rewrite.
- **Documents affected:** `docs/design/00_`, `02_`, `.cursor/rules/00-`.
- **Open questions:** None.

## DDR-010 — Trial Economy targets Widelands-like coverage, not feature parity

- **Decision:** Cover all major settlement-logistics mechanic families in simplified original form; no Widelands tribes, campaigns, content breadth, or copied data.
- **Status:** Accepted
- **Rationale:** Captures the genre's depth while staying original and bounded.
- **Consequences:** Each family must affect state, emit events, appear in diagnostics, and have tests — but may be simple.
- **Documents affected:** `docs/design/01_`, `02_`.
- **Open questions:** None.

## DDR-011 — Trial Military Game is the permanent military engine

- **Decision:** The Trial Military Game is the permanent abstract military layer, not a throwaway prototype.
- **Status:** Accepted
- **Rationale:** Hybrid reuses its production, power, combat, intel, and tech systems.
- **Consequences:** Military schema must compose with economy in hybrid without duplication.
- **Documents affected:** `docs/design/01_`, `03_`, `04_`.
- **Open questions:** None.

## DDR-012 — Trial Military targets OpenRA-like coverage, not feature parity

- **Decision:** Cover major OpenRA-like RTS mechanic families in simplified original form; no OpenRA factions, YAML/traits, maps, missions, UI, art, or balance.
- **Status:** Accepted
- **Rationale:** Captures RTS depth while staying original and bounded.
- **Consequences:** Optional families (capture, veterancy, special powers) may defer with schema hooks.
- **Documents affected:** `docs/design/01_`, `03_`.
- **Open questions:** None.

## DDR-013 — Hybrid Game is composition, not rewrite

- **Decision:** `Hybrid = Trial Economy + Trial Military + integration rules`; inherited rules apply unless explicitly overridden.
- **Status:** Accepted
- **Rationale:** Avoids three disconnected prototypes; maximizes reuse.
- **Consequences:** RUN_07 composes existing modules; HYB-ECON-001 forbids duplicating the economy engine.
- **Documents affected:** `docs/design/04_`, `docs/runs/RUN_07_`.
- **Open questions:** Map/grid sharing between trials must be enforced early — see **DDR-029**.

## DDR-014 — Hybrid removes instant sidebar military production

- **Decision:** No unit spawns without queued production at a building fed by ware delivery.
- **Status:** Accepted
- **Rationale:** Core design pillar: military depends on logistics.
- **Consequences:** Missing wares stall production with `input_missing`.
- **Documents affected:** `docs/design/04_` §7, `docs/runs/RUN_07_`.
- **Open questions:** None.

## DDR-015 — Hybrid uses logistics-fed military construction and production

- **Decision:** Military buildings and units use economy construction sites and ware-fed production recipes.
- **Status:** Accepted
- **Rationale:** Unifies the two engines around the ware/carrier system.
- **Consequences:** Hybrid config defines military footprints and ware recipes; repair is also ware-fed (HYB-LOGREP).
- **Documents affected:** `docs/design/04_` §8–§9b, `docs/design/05_`.
- **Open questions:** None.

## DDR-016 — Hybrid player military control is delegated, not unit micro

- **Decision:** Humans set stances, priorities, rally/garrison, patrol/defence zones, and delegated attacks on visible targets; direct move/attack are rejected.
- **Status:** Accepted
- **Rationale:** Distinct game feel and aligns with logistics focus.
- **Consequences:** Sim AI executes unit pathing/combat internally; a trial→hybrid translation table maps each direct command to a delegated equivalent.
- **Documents affected:** `docs/design/04_` §13, `docs/design/07_`.
- **Open questions:** None.

## DDR-017 — Trial Military allows direct unit control for isolated RTS testing

- **Decision:** Direct move/attack/attack-move/patrol/guard are required in the military trial and forbidden for humans in hybrid.
- **Status:** Accepted
- **Rationale:** Lets the RTS engine be exercised standalone before delegation is layered on.
- **Consequences:** The hybrid variant config lists `forbidden_player_commands`.
- **Documents affected:** `docs/design/03_` §15, `04_` §6/§13, `docs/architecture/06_`.
- **Open questions:** None.

## DDR-018 — Economy uses hex map, terrain, slope, roads, nodes, carriers, wares, storage, chains

- **Decision:** The economy is a hex (axial) map simulation with terrain/height/slope, roads with nodes, carrier logistics, typed wares, storage, and production chains.
- **Status:** Accepted
- **Rationale:** These are the load-bearing mechanic families for the genre.
- **Consequences:** Roads are not a thin connected/not-connected graph; capacity/congestion are represented at least in diagnostics.
- **Documents affected:** `docs/design/02_` §9–§31.
- **Open questions:** None.

## DDR-019 — Economy includes worker/tool provisioning, forest renewal, food tiers, mine depth, upgrades, diagnostics

- **Decision:** Workers are supply-limited and tool-gated; forests deplete/regrow; mines need food tiers and deplete; buildings upgrade; everything is diagnosable.
- **Status:** Accepted
- **Rationale:** These give the economy real depth beyond a logistics demo.
- **Consequences:** Large RUN_02 scope — see **DDR-028**.
- **Documents affected:** `docs/design/02_` §19–§31.
- **Open questions:** None.

## DDR-020 — Military includes power, queues, tech, radar/intel/fog, movement, combat, repair, static defence, skirmish smoke

- **Decision:** The military engine covers these families in simplified deterministic form.
- **Status:** Accepted
- **Rationale:** Minimum coverage for a credible RTS engine and hybrid reuse.
- **Consequences:** Large RUN_03 scope — see **DDR-028**.
- **Documents affected:** `docs/design/03_` §7–§22.
- **Open questions:** None.

## DDR-021 — Bots and neural agents must use the same command system as humans

- **Decision:** Agents observe snapshots and submit commands; no privileged intel or state access.
- **Status:** Accepted
- **Rationale:** Fairness, determinism, and transferability of policies.
- **Consequences:** Hybrid agents exclude forbidden direct unit commands; bots respect fog.
- **Documents affected:** `docs/architecture/04_`, `.cursor/rules/08-`, `docs/design/04_` §20.
- **Open questions:** Distinction between sim-internal AI (executes delegated orders) and bot clients is clarified in `04_` §13 / HYB-MIL-003.

## DDR-022 — Neural training waits for legal actions, observations, rewards, replay, bots, and smoke tests

- **Decision:** No training before RUN_06 prerequisites (kernel, trials, bots, replay hash, stable snapshot) exist and pass tests.
- **Status:** Accepted
- **Rationale:** Training on an unstable sim is wasted effort and non-reproducible.
- **Consequences:** ML dependencies and training loops are forbidden before RUN_06 scope approval.
- **Documents affected:** `.cursor/rules/08-`, `docs/architecture/04_`, `docs/testing/04_`.
- **Open questions:** Stochastic policy determinism (seeding the policy's own sampling) to be specified in RUN_06.

## DDR-023 — Multiplayer is later and must use command sync with desync detection

- **Decision:** Wi-Fi multiplayer (RUN_12) reuses the single-player validation pipeline with periodic hash compare and desync handling.
- **Status:** Accepted
- **Rationale:** Reuses determinism guarantees; avoids a separate netcode rule engine.
- **Consequences:** First architecture is **host-authoritative** (DDR-030, Accepted); clients submit commands and never run independent authoritative sims. Lockstep is a future optimisation only.
- **Documents affected:** `docs/design/09_`, `docs/testing/05_`, `docs/runs/RUN_12_`.
- **Open questions:** None (resolved by DDR-030).

## DDR-024 — Originality/IP policy: nothing copied from reference games

- **Decision:** No copied names, code, art, maps, missions, factions, UI, data, or balance from Widelands, OpenRA, C&C, Red Alert, or others.
- **Status:** Accepted
- **Rationale:** Legal and creative independence.
- **Consequences:** Original placeholder IDs from the catalogue; comparisons are mechanic-coverage only.
- **Documents affected:** `.cursor/rules/05-`, `docs/design/00_`, `01_`, `05_`, `README.md`.
- **Open questions:** Internal working name "WideRTS" (local workspace folder) should not ship as product identity — low risk, noted in audit.

## DDR-025 — Content expansion waits until core systems work

- **Decision:** Campaigns, progression, additional maps, and content packs are RUN_11, after the engines and hybrid are stable.
- **Status:** Accepted
- **Rationale:** Content on an unstable engine is rework.
- **Consequences:** Schema hooks (campaign/mission/mod) are added earlier; content is data-driven with no per-mission sim forks.
- **Documents affected:** `docs/design/08_`, `docs/runs/RUN_11_`.
- **Open questions:** None.

## DDR-026 — Authoritative runtime is a Godot/GDScript simulation core; Python is training/tooling only

- **Decision:** Pivot away from "Python simulation is authoritative." The authoritative game simulation is a **pure Godot/GDScript simulation core** that ships inside the Godot app. Python is retained only for neural-network training, analysis, tournament processing, CSV/JSON tooling, and external batch workflows. Python is **not** the shipped authoritative runtime and is **not** embedded in the mobile app.
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** Embedding CPython in a packaged mobile Godot app (or requiring the app to talk to a Python process for normal gameplay) is fragile and was the single largest latent project risk. A GDScript-authoritative core removes the mobile transport problem entirely while preserving determinism, replay, and the command pipeline.
- **Consequences:**
  - The authoritative GDScript simulation core is separate from UI scenes and rendering code; it must not depend on the scene tree, sprite/Node positions, animation timing, frame delta, physics side effects, camera state, UI nodes, input events, or wall-clock time.
  - The Godot **visual client/UI is non-authoritative**: it renders snapshots and submits commands only.
  - The mobile game ships with the authoritative GDScript core; no Python mobile runtime dependency for normal gameplay.
  - Python training tools drive the sim by running **Godot headless** and exchanging commands/snapshots/replays via JSON, files, sockets, or another documented development-time transport (see DDR-033). Python is the trainer, not the engine.
  - Use the wording "Godot/GDScript simulation core is authoritative" — never "Godot is authoritative" (which would wrongly imply scenes/rendering own rules).
- **Documents affected:** all Cursor rules, `docs/architecture/00_`–`06_`, `docs/design/00_`, `06_`, `09_`, `docs/testing/00_`, `03_`, `04_`, `05_`, `docs/runs/RUN_01_`, `RUN_05_`, `RUN_06_`, `RUN_10_`, `RUN_12_`, `README.md`.
- **Open questions:** Development-time Python↔Godot-headless transport detail tracked separately as **DDR-033** (decided at RUN_06; non-blocking now).

## DDR-027 — Authoritative math uses integers / fixed-point (no floats in hashed state)

- **Decision:** Use **integers or fixed-point scaled integers** for all authoritative, replay-hashed state. No floats in authoritative hashed state.
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** Cross-machine replay hashing (and any future lockstep option) is sensitive to float non-determinism. Integer/fixed-point math guarantees identical hashes across platforms and GDScript builds.
- **Consequences:**
  - Authoritative positions use hex coordinates and integer progress.
  - Movement progress uses integer ticks or integer progress units.
  - Production, cooldowns, health, damage, resource/ware quantities, visibility timers, construction progress, repair progress, and training/research progress are integers.
  - Fractional rates use fixed-point scaled integers or integer "per N ticks" rates.
  - Godot visual animation may use floats, but those are **presentation-only**: never part of authoritative state, replay hash, command validation, combat, pathfinding, production, or logistics outcomes.
- **Documents affected:** `docs/architecture/05_`, `docs/design/02_` §6, `03_` §6, `docs/runs/RUN_01_`.
- **Open questions:** None.

## DDR-028 — Run sizing: one long run preferred, one controlled A/B fallback allowed

- **Decision:** The preferred plan is that RUN_02 (full economy), RUN_03 (full military), and RUN_07 (hybrid) each complete in **one dedicated long autonomous run**. If a run genuinely cannot complete safely, the agent may split **once** into an A/B continuation, but only at a coherent acceptance boundary.
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** These runs bundle many mechanic families, ~10 scenarios, multiple smoke tests, diagnostics, exports, and (RUN_02) a chain-completing bot. One long run is preferred for coherence, but a single controlled continuation is a safer fallback than failing or faking completion.
- **Consequences / fallback policy:** A permitted A/B split requires, before stopping:
  1. commit and push the completed subset;
  2. document exactly what passed;
  3. document exactly what remains;
  4. produce a clear gap report;
  5. **not** claim the full milestone is complete until the full acceptance checklist passes.
  - Do **not** weaken Definitions of Done. Do **not** call a skeleton complete. Do **not** silently split into many small runs. Pre-authorise only **one** continuation branch (e.g. `feature/run-02b-...`) where genuinely necessary.
- **Documents affected:** `docs/runs/RUN_02_`, `RUN_03_`, `RUN_07_`, `.cursor/rules/01-`, `docs/design/02_` §43, `03_` §31, `04_` §23.
- **Open questions:** None.

## DDR-029 — Shared axial hex map across economy, military, and hybrid

- **Decision:** The Trial Economy Game, Trial Military Game, and Hybrid Game **must** use the same shared **axial hex-map primitives** created in the RUN_01 core simulation kernel.
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** A single shared map model prevents the "three disconnected prototypes" risk and lets the hybrid compose economy and military without reconciling different map systems.
- **Consequences:**
  - RUN_01 creates the shared hex-map primitives.
  - The Trial Military Game must **not** create a separate square grid, unrelated passability grid, or parallel map system.
  - Military passability, range, vision, building placement, attack range, movement, and pathfinding all derive from the shared hex map (terrain/height as inputs).
  - The hybrid composes economy + military without reconciling different map models.
- **Documents affected:** `docs/architecture/01_`, `docs/design/03_`, `docs/runs/RUN_01_`, `RUN_02_`, `RUN_03_`, `RUN_07_`.
- **Open questions:** None.

## DDR-030 — Multiplayer architecture: host-authoritative first

- **Decision:** Use **host-authoritative** multiplayer as the first multiplayer architecture (RUN_12). Lockstep is a possible future optimisation only after deterministic replay is proven robust.
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** Host-authoritative is simpler and tolerant of any residual non-determinism; it reuses the single-player command pipeline directly. Lockstep demands strict cross-machine determinism and is premature as a first target.
- **Consequences:**
  - The host runs the authoritative GDScript simulation core; clients submit commands.
  - The host validates commands and sends snapshots/events/results; clients do **not** run independent authoritative simulations.
  - Desync detection remains useful, but the first implementation must not depend on deterministic lockstep across all clients.
- **Documents affected:** `docs/design/09_`, `docs/testing/05_`, `docs/runs/RUN_12_`.
- **Open questions:** None (lockstep deferred as future optimisation).

## DDR-031 — No credits in the Hybrid Game (ware/logistics only)

- **Decision:** The Hybrid Game has **no credits**. Hybrid production, construction, repair, and research are entirely ware/logistics based.
- **Status:** Accepted — *Ratified on RUN_00 architecture decision pass.*
- **Rationale:** Credits as a build/production resource undercut the core hybrid pillar that military depends on logistics. Removing them makes the hybrid unambiguous.
- **Consequences:**
  - The Trial Military Game may still use credits/resource stock as a simplified standalone RTS mechanic.
  - The Hybrid Game must not use credits as a build, unit-production, repair, or research resource.
  - Hybrid military buildings and units require physical wares, logistics delivery, production time, power where relevant, and tech/research where relevant.
  - If a future "command capacity" / "permission" abstraction is added, it must **not** replace ware delivery.
  - Hybrid costs are ware-only unless a later human decision changes this.
- **Documents affected:** `docs/design/04_` §4–§6, `docs/design/05_`, `docs/runs/RUN_07_`.
- **Open questions:** None.

## DDR-032 — Godot-compatible headless test harness for gameplay-rule tests

- **Decision:** Use a **lightweight custom Godot headless test runner** (`godot_project/sim/tools/run_tests.gd`) for gameplay-rule tests. Tests are pure GDScript `RefCounted` classes under `godot_project/sim/tests/` with rule-ID-based names. GUT and gdUnit4 were not adopted — they add addon/scene dependencies unnecessary for a pure simulation-core test suite at this stage.
- **Status:** Accepted — *Decided at RUN_01 implementation.*
- **Rationale:** Zero addon dependencies; tests target the authoritative `RefCounted` simulation core without rendered UI or scene tree; CLI-runnable via `godot --headless --script`; clear pass/fail with non-zero exit code on failure; deterministic.
- **Consequences:** Verification command documented in `docs/verification/local_verification.md`. Python `pytest` remains for Python training/tooling only. Project import (`godot --headless --import`) required once to register `class_name` types before first test run.
- **Documents affected:** `.cursor/rules/02-`, `docs/testing/00_`, `docs/runs/RUN_01_`, `docs/verification/local_verification.md`.
- **Open questions:** None.

## DDR-033 — Python ↔ Godot-headless training transport (development-time)

- **Decision:** Python training drives the authoritative sim by running **Godot headless** with a **sequenced file-session RPC** (`request_N.json` / `response_N.json`) and responses prefixed by the `@@RUN06_JSON@@` sentinel. Python is the trainer, not the engine; there is no Python simulation package to import.
- **Status:** Accepted — *Decided at RUN_06 implementation.*
- **Rationale:** Godot 4.5.1 headless lacks reliable stdin on Windows; file transport avoids stdout pipe deadlocks, supports large BC dataset writes to disk, and keeps the command pipeline authoritative.
- **Consequences:** `python_tools/training/godot_env.py` implements the client; `sim/tools/run_training_env.gd` + `training_env_server.gd` implement the server; training observations come from exported snapshots only; rollouts are reproducible from seed + command log.
- **Documents affected:** `docs/architecture/04_`, `docs/testing/04_`, `docs/runs/RUN_06_`, `docs/verification/local_verification.md`.
- **Open questions:** None.

---

## Status summary

All RUN_00 architecture decisions are now ratified. The following were flipped to **Accepted** on the RUN_00 architecture decision pass:

| ID | Topic | Status |
|----|-------|--------|
| DDR-026 | GDScript-authoritative core; Python = training/tooling only | Accepted |
| DDR-027 | Integer / fixed-point authoritative hashed state | Accepted |
| DDR-028 | One long run preferred; one controlled A/B fallback | Accepted |
| DDR-029 | Shared axial hex map for economy/military/hybrid | Accepted |
| DDR-030 | Host-authoritative multiplayer first | Accepted |
| DDR-031 | No credits in the Hybrid Game (ware/logistics only) | Accepted |
| DDR-032 | Godot test framework (custom headless runner) | Accepted |

### Remaining items needing human confirmation (non-blocking for RUN_01 doc work)

| ID | Topic | Decide at |
|----|-------|-----------|
| *(none — DDR-033 accepted at RUN_06)* | | |

DDR-032 was resolved at RUN_01: custom lightweight headless runner (see DDR-032 entry above).
