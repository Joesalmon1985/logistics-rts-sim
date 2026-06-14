# Design Decision Register

Authoritative record of major design decisions made during RUN_00 documentation work, surfaced for human review before implementation runs begin.

Status values:

- **Accepted** — decided, consistently reflected across docs, safe for agents to rely on.
- **Proposed** — written into docs but not yet ratified; an agent may proceed but should not treat it as immovable.
- **Needs Human Confirmation** — genuinely uncertain or carries project-level risk; an autonomous run must **not** silently resolve it. If a run is blocked on one of these, stop and report.

This register is the single place where uncertain decisions live. Do not bury uncertain choices in other documents.

---

## DDR-001 — Python simulation is authoritative

- **Decision:** All gameplay rules, state, and randomness live in an authoritative Python simulation.
- **Status:** Accepted
- **Rationale:** Single source of truth enables deterministic tests, replay, bots, and multiplayer sync without rule duplication.
- **Consequences:** Every client (Godot, bot, agent, peer) must round-trip through the sim. No gameplay logic in GDScript.
- **Documents affected:** `.cursor/rules/03-python-sim-authority.mdc`, `docs/architecture/00_`, `01_`, `docs/design/00_`.
- **Open questions:** None.

## DDR-002 — Godot is a non-authoritative client/viewer/frontend

- **Decision:** Godot only renders snapshots, collects input, serializes commands, and shows diagnostics/replays.
- **Status:** Accepted
- **Rationale:** Prevents rule drift and keeps determinism in one engine.
- **Consequences:** Godot may never compute economy, combat, pathfinding, or RNG outcomes.
- **Documents affected:** `.cursor/rules/04-godot-boundary.mdc`, `docs/architecture/03_`, `docs/design/06_`.
- **Open questions:** Transport between Godot and Python on mobile — see **DDR-026**.

## DDR-003 — All clients use the same command validation/execution pipeline

- **Decision:** Humans, bots, neural agents, and multiplayer peers submit commands through one validator/apply path.
- **Status:** Accepted
- **Rationale:** Guarantees fairness, replayability, and a single attack surface for validation.
- **Consequences:** No privileged side-channel for bots except scenario fixtures in tests.
- **Documents affected:** `docs/architecture/00_`, `02_`, `04_`, `.cursor/rules/03-`.
- **Open questions:** None.

## DDR-004 — Deterministic seeded tick simulation

- **Decision:** Discrete monotonic ticks; all randomness from a seeded RNG stored in state; fixed subsystem order per variant.
- **Status:** Accepted
- **Rationale:** Determinism underpins replay, tests, training, and multiplayer sync.
- **Consequences:** No use of the bare `random` module or wall-clock in authoritative rules. RNG state must be serialized.
- **Documents affected:** `docs/architecture/01_`, `05_`, `docs/design/02_` §6–§7, `03_` §6.
- **Open questions:** Floating-point determinism policy — see **DDR-027**.

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
- **Consequences:** No `@pytest.mark.skip` without explicit approval; rule matrix drives test creation.
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
- **Consequences:** Host-authoritative vs lockstep choice is deferred — see **DDR-030**.
- **Documents affected:** `docs/design/09_`, `docs/testing/05_`, `docs/runs/RUN_12_`.
- **Open questions:** Architecture choice; float determinism for lockstep.

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

## DDR-026 — Mobile Godot display uses snapshot layers and avoids duplicating rules — but the Python-on-mobile transport is unresolved

- **Decision (display):** Godot renders from snapshot layers; the layer stack and budgets are defined.
- **Decision (transport):** How the authoritative Python sim is reached from a packaged mobile Godot app (embedded interpreter, on-device host process, or networked host) is **not yet decided**. Docs currently say "subprocess pipe, socket, or embedded library (RUN_05 choice)" and "embedded or host-only mode TBD (RUN_12)".
- **Status:** Needs Human Confirmation
- **Rationale:** Running CPython on Android/iOS inside a Godot app is non-trivial; the chosen transport affects RUN_05, RUN_10, and RUN_12 architecture. This is the single largest latent project risk. It does **not** block headless runs RUN_01–RUN_04/RUN_07–RUN_08.
- **Consequences:** If deferred too long, viewer/packaging runs may require rework. A spike to validate the transport before RUN_05 is recommended.
- **Documents affected:** `docs/architecture/03_`, `docs/design/06_`, `09_`, `docs/runs/RUN_05_`, `RUN_10_`, `RUN_12_`.
- **Open questions:** Embedded Python vs on-device host vs LAN-only host? Acceptable mobile footprint and latency? Confirm before RUN_05.

## DDR-027 — Floating-point determinism policy

- **Decision:** Authoritative math should prefer integers; current wording is "avoid floats where possible."
- **Status:** Needs Human Confirmation
- **Rationale:** Cross-machine replay hashing and the lockstep multiplayer option are sensitive to float non-determinism. "Where possible" is too soft to guarantee identical hashes across platforms/CPUs.
- **Consequences:** If floats are used for movement/combat/economy rates, replay hashes may diverge across devices, breaking DDR-006 and DDR-023. Recommend mandating integer or fixed-point for all hashed authoritative state.
- **Documents affected:** `docs/architecture/05_`, `docs/design/02_` §6, `03_` §6.
- **Open questions:** Adopt strict integer/fixed-point mandate for hashed state? Confirm before RUN_01 finalizes the hash and RNG/math conventions.

## DDR-028 — Run sizing for RUN_02, RUN_03, RUN_07

- **Decision:** RUN_02 (full economy), RUN_03 (full military), and RUN_07 (hybrid) are each specified as a single large run.
- **Status:** Needs Human Confirmation
- **Rationale:** Each bundles a very large number of mechanic families, ~10 scenarios, multiple smoke tests, diagnostics, exports, and (RUN_02) a heuristic bot completing a full logistics chain. A single autonomous run may not finish RUN_02/RUN_03 to the stated Definition of Done.
- **Consequences:** Risk of partial completion and an over-long run. Consider pre-authorizing a split (e.g. RUN_02a map/roads/carriers/chains, RUN_02b workers/tools/forest/food/mines/upgrades/bot) or explicitly allowing a run to land a coherent subset and hand off.
- **Documents affected:** `docs/runs/RUN_02_`, `RUN_03_`, `RUN_07_`, `docs/design/02_` §43, `03_` §31, `04_` §23.
- **Open questions:** Permit splitting RUN_02/RUN_03, or require one long run? Define a minimum coherent deliverable if a run must stop early.

## DDR-029 — Shared map/grid between economy and military trials

- **Decision (proposed):** Both trials build on the RUN_01 shared hex-map primitives so the hybrid composes without reconciling two map systems.
- **Status:** Needs Human Confirmation
- **Rationale:** `docs/design/03_` MIL-MOVE-001 says units pathfind on a "passability grid" that "may share hex map with economy later." If RUN_03 builds an independent grid, RUN_07 integration becomes a partial rewrite — the exact "three disconnected prototypes" risk.
- **Consequences:** Recommend requiring the military trial to use the kernel hex map (terrain/height as passability inputs) rather than a parallel grid. A safe clarification was added to `docs/architecture/01_` and RUN_03 during this audit.
- **Documents affected:** `docs/architecture/01_`, `docs/design/03_`, `docs/runs/RUN_03_`, `RUN_07_`.
- **Open questions:** Confirm both trials must reuse the shared hex map from RUN_01.

## DDR-030 — Multiplayer architecture: host-authoritative vs lockstep

- **Decision:** Two options are documented; choice deferred to RUN_12.
- **Status:** Needs Human Confirmation
- **Rationale:** Host-authoritative is simpler and tolerant of float drift; lockstep is bandwidth-light but demands strict cross-machine determinism (ties to DDR-027).
- **Consequences:** The choice interacts with the float policy and the mobile transport (DDR-026).
- **Documents affected:** `docs/design/09_`, `docs/runs/RUN_12_`.
- **Open questions:** Pick an architecture (or keep both viable) before RUN_12; resolve alongside DDR-027.

## DDR-031 — Hybrid credits: removed or secondary

- **Decision:** `docs/design/04_` §4 states credits are "Removed or secondary" in hybrid; HYB-ECON-002/HYB-MIL-004 route the military resource loop through economy deposits/processor.
- **Status:** Needs Human Confirmation
- **Rationale:** "Removed or secondary" is ambiguous. RUN_07 needs to know whether credits exist at all in hybrid, or whether all military costs are ware-denominated.
- **Consequences:** Affects hybrid production/repair/research cost modelling and bot reasoning. Left unresolved, two agents could implement it differently.
- **Documents affected:** `docs/design/04_` §4–§6, `docs/design/05_`.
- **Open questions:** Are credits fully removed in hybrid (ware-only economy), or retained as a secondary currency? Confirm before RUN_07.

---

## Summary of items needing human confirmation before RUN_01

| ID | Topic | Blocks |
|----|-------|--------|
| DDR-027 | Float vs integer/fixed-point for hashed state | RUN_01 (hash/math conventions) |
| DDR-029 | Shared hex map across trials | RUN_03 design, RUN_07 composition |
| DDR-028 | Run sizing / permitted splits | RUN_02, RUN_03, RUN_07 |
| DDR-026 | Python-on-mobile transport | RUN_05, RUN_10, RUN_12 (not RUN_01) |
| DDR-030 | Multiplayer architecture | RUN_12 |
| DDR-031 | Hybrid credits semantics | RUN_07 |

DDR-027 and DDR-029 are the two that most directly touch RUN_01 and the earliest implementation runs; the rest can be confirmed later but should not be forgotten.
