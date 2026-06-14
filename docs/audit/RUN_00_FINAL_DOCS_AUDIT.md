# RUN_00 — Final Documentation and Cursor-Rules Audit

> **Superseded in part (architecture decision pass):** This audit predates the RUN_00 architecture decision ratification. Several risks it raised have since been resolved by ratified decisions — notably the mobile Python transport risk (old DDR-026), now resolved by pivoting to a **Godot/GDScript-authoritative simulation core** with Python as training/tooling only. DDR-027 (integer/fixed-point), DDR-028 (run sizing + A/B fallback), DDR-029 (shared hex map), DDR-030 (host-authoritative multiplayer), and DDR-031 (no credits in hybrid) are now Accepted. References below to "Python simulation" as the authoritative runtime are historical. See `docs/audit/RUN_00_ARCHITECTURE_DECISION_RATIFICATION.md` and `docs/design/11_design_decision_register.md`.

Audit of all project documentation, run plans, architecture, testing docs, and Cursor rules to judge whether they form a strong enough foundation for autonomous Cursor runs (RUN_01 onward) to execute with minimal human supervision.

Scope: documentation/rules audit only. No simulation, Godot, bots, or training code was written. No reference-game content was copied or introduced.

Reviewed: `.cursor/rules/` (10 files), `docs/design/` (00–10), `docs/architecture/` (00–06), `docs/testing/` (00–05), `docs/runs/` (00–12), `README.md`, `.gitignore`.

---

## 1. Executive summary

This is an unusually strong documentation foundation for a project at the pre-implementation stage. The architecture is coherent and consistent: an authoritative deterministic Python sim, a strict non-authoritative Godot boundary, a unified command pipeline for all clients, event/snapshot/replay as first-class concepts, and a clear three-phase economy → military → hybrid composition model. Rule IDs are used consistently, traceability between design → test matrix → run checklists is largely in place, and the Cursor rules enforce the most important invariants (sim authority, Godot boundary, test-first, IP originality, training gates, git policy, final-report format).

The foundation is good enough that an agent can start RUN_01 and likely RUN_03 with little reinterpretation. The main risks are not contradictions but **ambition and a few deferred project-level decisions**: RUN_02/RUN_03/RUN_07 are each very large for a single autonomous run; the mechanism for running Python under a packaged mobile Godot app is unresolved; the floating-point determinism policy is too soft to guarantee cross-machine replay hashing; and the military trial is permitted to build a possibly-separate map/grid, which threatens clean hybrid composition later.

These are surfaced in `docs/design/11_design_decision_register.md`. Safe documentation fixes were applied during this run (cross-reference corrections, missing rule-ID definitions, matrix gaps, and anti-divergence clarifications). The remaining items are recorded as **Needs Human Confirmation** rather than silently decided.

Bottom line: the docs are coherent enough for autonomous implementation to begin, **provided** the human confirms two RUN_01-adjacent decisions (float policy DDR-027, shared map DDR-029) and acknowledges the run-sizing risk (DDR-028).

## 2. Readiness rating for RUN_01 — **Ready (minor confirmation advised)**

RUN_01 (core kernel) is well-scoped, achievable in one run, and has clear acceptance criteria and KERN-SMOKE tests. The only caveats are decisions RUN_01 will bake in: the replay hash/RNG/math conventions (confirm DDR-027 float policy) and ensuring the shared hex-map primitives are the ones the military trial will reuse (DDR-029). Both are advisory, not blocking.

## 3. Readiness rating for RUN_02 — **Mostly ready (scope risk)**

The Trial Economy requirements (570 lines) are the strongest single document in the repo: complete families, commands, events, chains, scenarios, smoke tests, exports, diagnostics, and a Definition of Done. The concern is volume: RUN_02 asks one run to deliver the entire economy engine plus ten deterministic scenario maps plus a heuristic bot that completes a full logistics chain (ECON-SMOKE-005). That is a lot for a single autonomous run. Content and traceability are ready; run sizing (DDR-028) should be confirmed or a split pre-authorized.

## 4. Readiness rating for RUN_03 — **Mostly ready (scope + map-sharing risk)**

The Trial Military requirements (398 lines) cover the OpenRA-like families well. Two issues: (a) same single-run volume risk as RUN_02, and (b) MIL-MOVE-001 permits a "passability grid" that only "may share hex map with economy later," which risks a parallel map system and harder hybrid composition (DDR-029). A clarification was added during this audit; human confirmation is recommended.

## 5. Readiness rating for RUN_07 — **Mostly ready (depends on RUN_02/03 quality + credits ambiguity)**

The hybrid composition model is clearly specified as composition, not rewrite, with an explicit trial→hybrid delegation translation table and inheritance rules. Readiness is contingent on RUN_02/RUN_03 producing composable modules (esp. shared map, DDR-029) and on resolving whether credits are removed or secondary in hybrid (DDR-031, currently ambiguous in `04_` §4). With those confirmed, RUN_07 is executable.

## 6. Strongest parts of the foundation

- **Authority model.** Sim-authoritative / Godot-non-authoritative is stated identically across rules, architecture, and design. Very low risk of Godot becoming the rule engine.
- **Unified command pipeline.** Humans, bots, agents, and peers share one validate/apply path; this is consistently repeated and enforced by rules.
- **Determinism + replay as first-class.** Seeded RNG in state, append-only events, versioned snapshots, replay hashing from RUN_01, `expected_hash` in fixtures.
- **Rule-ID traceability.** A glossary defines the ID convention; the coverage contract maps families → requirement docs → matrix → run checklists.
- **Test-first discipline.** Clear policy, naming convention, test types, and a sizable rule-contract matrix.
- **IP discipline.** Explicit do/don't lists, original placeholder catalogue, "mechanic coverage not parity" framing throughout.
- **Cursor rules.** Compact, enforce the right invariants, and gate neural training behind prerequisites.

## 7. Weakest parts of the foundation

- **Run sizing** for RUN_02/RUN_03/RUN_07 (DDR-028) — most likely cause of a single long run "failing" to reach Definition of Done.
- **Mobile Python transport** (DDR-026) — the biggest unanswered architecture question for shipping; deferred to "RUN_05 choice"/"RUN_12 TBD".
- **Float determinism policy** (DDR-027) — "avoid floats where possible" is too soft for cross-machine hashing and lockstep multiplayer.
- **Map sharing** between trials (DDR-029) — permissive wording invites two map systems.
- **Minor traceability gaps** — a few rule IDs referenced but not defined, and a matrix-coverage claim stronger than the matrix itself (fixed where safe; see §18).

## 8. Contradictions found

1. **Matrix coverage claim vs matrix content.** `docs/design/02_` §35 states "Every rule ID in §19–§31 must have at least one matrix row," but `docs/testing/01_` is representative: `ECON-NODE-001`, `ECON-PATH-002`, `ECON-CARR-002`, `ECON-JOB-002/003`, `ECON-STOR-002`, `ECON-CONS-002/003`, `ECON-PRI-002/003`, several `ECON-DIAG-*` have no rows. **Fixed:** §35 reworded to family-level coverage with per-rule tests added during RUN_02, and a `ECON-NODE-001` row (plus a few others) added to the matrix.
2. **Wrong section cross-references.** `docs/architecture/02_` cited economy event categories at "§28" and military at "§19"; the actual sections are §33 and §25. **Fixed.**

No deep semantic contradictions (e.g. conflicting authority or determinism statements) were found.

## 9. Ambiguities found

1. **Hybrid credits** "Removed or secondary" (`04_` §4) — unresolved; recorded as DDR-031. A pointer note was added; the decision is left to the human.
2. **Military map/grid** "may share hex map with economy later" (`03_` MIL-MOVE-001) — recorded as DDR-029; clarification added.
3. **Mobile transport** "subprocess, socket, or embedded library (RUN_05 choice)" and "embedded or host-only mode TBD" — recorded as DDR-026.
4. **Float usage** "avoid floats where possible" — recorded as DDR-027.
5. **Stochastic policy determinism** — `architecture/04_` requires "same policy + seed → same command log" without specifying how a stochastic neural policy is seeded; deferred to RUN_06 (noted in DDR-022).

## 10. Missing requirements found

- **VIS-UX-001** and **VIS-REP-001** are referenced in `01_mechanic_coverage_contract.md` and `03_godot_viewer_test_plan.md` but were not defined in their home doc `06_mobile_2d_visual_design.md`. **Fixed** (added rule lines).
- **NET-PKG-001** referenced in the test plan and matrix summary but not defined as a rule line in `09_wifi_multiplayer_design.md`. **Fixed** (added rule line).
- No explicit requirement that the military trial reuse the kernel hex map. **Fixed** (clarification added; decision DDR-029 flagged).

## 11. Missing tests found

- Matrix rows missing for several defined economy rule IDs (see §8.1). **Partially fixed**: added `ECON-NODE-001`, `ECON-CARR-002`, `ECON-JOB-003`, `ECON-CONS-003`, `ECON-PRI-002`, `ECON-PRI-003` rows and reworded the coverage claim so the remaining per-rule tests are explicitly an implementation-time obligation rather than a contradiction.
- Military/hybrid matrices are representative by design and consistent with their docs; no fix needed beyond noting the same "representative, expand during run" intent.

## 12. Scope risks

- **RUN_02, RUN_03, RUN_07 are large** for single autonomous runs (DDR-028). RUN_02 in particular bundles ~25 mechanic families, 10 scenario maps, 5 smoke tests, CSV exports, full diagnostics, and a chain-completing bot. High risk a single run lands partial.
- **Performance acceptance** (e.g. 64×64 map / 50 carriers / tick < 50 ms) adds profiling burden inside RUN_02; reasonable but non-trivial.
- Early runs are otherwise well-bounded with explicit out-of-scope lists; no scope creep detected in RUN_01 or the kernel.

## 13. IP / originality risks — **Low**

- Strong, repeated IP rules; original placeholder catalogue; "coverage not parity" framing; no copied names/data observed in any doc.
- Minor: the internal working name "WideRTS" (local workspace folder) reads as "Widelands RTS." It is local-only (the git remote is `logistics-rts-sim`), but the product/identity must not adopt it. Low risk; noted in DDR-024.

## 14. Determinism / replay risks — **Low–medium**

- Core determinism is well specified (seeded RNG in state, fixed subsystem order, hash excludes event log, RNG serialized).
- Medium risk: float policy (DDR-027). For cross-device replay hashing and the lockstep multiplayer option, "avoid floats where possible" should become a hard integer/fixed-point mandate for hashed authoritative state. Recommended before RUN_01 fixes the hash/math conventions.
- Stochastic-policy seeding for training is unspecified but out of RUN_01–RUN_05 scope.

## 15. Godot-authority risks — **Low**

- Authority boundary is enforced by `.cursor/rules/03-` and `04-`, the adapter contract, and the visual design doc (snapshot-only rendering, forbidden GDScript gameplay). Strong.
- The only Godot-adjacent risk is transport, not authority (DDR-026).

## 16. Bot / training risks — **Low**

- Bots/agents are clients of the same command API; no privileged intel; training gated behind RUN_06 prerequisites; observations from snapshots only; weights gitignored.
- The sim-internal AI that executes delegated hybrid orders is correctly framed as part of the authoritative sim (HYB-MIL-003), not a client bypass.

## 17. Multiplayer risks — **Medium (deferred, acceptable)**

- Two architectures (host-authoritative vs lockstep) are documented with the choice deferred to RUN_12 (DDR-030). Acceptable for now.
- The lockstep option is sensitive to the float policy (DDR-027). The single-pipeline reuse and desync detection are well specified.

## 18. Recommended documentation fixes made during this run

Applied directly (safe, non-design-changing):

1. `docs/architecture/02_command_event_snapshot_contract.md` — corrected event-category section references (economy §33, military §25).
2. `docs/design/06_mobile_2d_visual_design.md` — added rule lines **VIS-UX-001** and **VIS-REP-001** so coverage-contract/test-plan references resolve.
3. `docs/design/09_wifi_multiplayer_design.md` — added explicit **NET-PKG-001** rule line.
4. `docs/design/02_trial_economy_game_requirements.md` — reworded §35 so the matrix obligation is family-level coverage plus per-rule tests added during RUN_02 (removes contradiction with the representative matrix).
5. `docs/testing/01_rule_contract_test_matrix.md` — added rows for `ECON-NODE-001`, `ECON-CARR-002`, `ECON-JOB-003`, `ECON-CONS-003`, `ECON-PRI-002`, `ECON-PRI-003`.
6. `docs/architecture/01_python_sim_authoritative_core.md` and `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` — clarified that the military trial reuses the RUN_01 shared hex map (anti-divergence; DDR-029).
7. `docs/design/04_hybrid_game_requirements.md` — added a pointer note that the hybrid credits question is pending human confirmation (DDR-031), without deciding it.
8. `docs/architecture/03_godot_adapter_contract.md` and `docs/design/09_` — added pointer notes that the mobile Python transport is an open decision (DDR-026), without deciding it.
9. `docs/architecture/05_determinism_replay_save_load.md` — tightened the float guidance toward integer/fixed-point for hashed state and cross-referenced DDR-027.
10. `docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md` and `RUN_03_` — added prerequisite cross-links (smoke matrix; coverage contract/glossary) for consistency with RUN_02.
11. Created `docs/design/11_design_decision_register.md` (this run's companion deliverable).

## 19. Recommended human decisions before RUN_01

Confirm before or at the start of RUN_01:

1. **DDR-027 (float policy)** — adopt a hard integer/fixed-point mandate for all hashed authoritative state? RUN_01 will bake in the hash, RNG, and math conventions.
2. **DDR-029 (shared map)** — require the military trial to reuse the RUN_01 hex map rather than a parallel grid?

Confirm before the relevant later run (not blocking RUN_01):

3. **DDR-028 (run sizing)** — pre-authorize splitting RUN_02/RUN_03 (and define a minimum coherent deliverable if a run must stop early)?
4. **DDR-031 (hybrid credits)** — removed entirely or retained as secondary currency? (before RUN_07)
5. **DDR-026 (mobile transport)** — schedule a spike to choose embedded vs host transport before RUN_05.
6. **DDR-030 (multiplayer architecture)** — host-authoritative vs lockstep (before RUN_12; interacts with DDR-027).

## 20. Specific review questions (Phase 6)

1. **Can RUN_01 build the kernel without reinterpreting the whole project?** Yes. Prerequisites, scope, package layout, and smoke tests are explicit.
2. **Can RUN_02 implement a complete economy engine, not a toy?** Yes in specification; the risk is fitting it into one run (DDR-028), not clarity.
3. **Can RUN_03 implement a complete RTS engine, not a toy?** Yes in specification; same sizing risk plus the map-sharing clarification (DDR-029).
4. **Can RUN_07 combine the engines without rewriting them?** Yes, if RUN_02/RUN_03 produce composable modules (shared map) and the credits question (DDR-031) is resolved. The composition model and translation table are well defined.
5. **Do the docs prevent Godot from becoming the rule engine?** Yes — strongly and redundantly.
6. **Do the docs prevent bots/agents from bypassing validation?** Yes — unified command API, no privileged intel, training gates.
7. **Enough events, diagnostics, snapshots, and replay requirements?** Yes — comprehensive across all three engines.
8. **Enough test-first guidance?** Yes — policy, naming, matrix, and per-run smoke requirements. Per-rule matrix expansion is correctly framed as implementation-time work after the §35 fix.
9. **Are acceptance criteria strong enough for single long runs?** Mostly. They are clear and verifiable, but RUN_02/RUN_03 may be too large to fully satisfy in one run (DDR-028).
10. **Too many / too few / right number of runs?** The 12 implementation runs (RUN_01–RUN_12) are a reasonable decomposition. If anything, RUN_02 and RUN_03 each merit being two runs; everything else is appropriately sized.
11. **Any requirements too ambitious for one long run?** Yes — RUN_02 (entire economy + 10 scenarios + chain-completing bot), RUN_03 (entire RTS), RUN_07 (full hybrid).
12. **Any requirements too vague to implement?** A few, now flagged: hybrid credits (DDR-031), mobile transport (DDR-026), float policy (DDR-027). Core mechanics are specified concretely.
13. **Any design decisions likely to cause later problems?** Permissive map-sharing wording (DDR-029) and the soft float policy (DDR-027) are the two most likely to bite; both are flagged and partially mitigated.
14. **What should the human double-check before RUN_01?** DDR-027 and DDR-029 (see §19).

---

## Overall verdict

**Mostly ready — strong foundation, begin RUN_01 after confirming DDR-027 and DDR-029.**

The documentation is coherent, consistent, and strict enough for autonomous implementation to begin. It is honestly not "fully ready, no questions asked": the run-sizing ambition (DDR-028) and the deferred mobile-transport decision (DDR-026) are real risks that will surface later. None of these block RUN_01. They are now visible in the decision register rather than buried.

## Recommended next command / prompt

Have the human ratify DDR-027 (float/fixed-point policy) and DDR-029 (shared map) in `docs/design/11_design_decision_register.md` (flip to Accepted with any edits), then start RUN_01:

> "Begin RUN_01 (Core Simulation Kernel) per `docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md`. First confirm DDR-027 and DDR-029 in the design decision register; honor the float/fixed-point policy and the shared-hex-map decision when building the kernel, hash, and RNG. Follow test-first development and the autonomous run policy. Do not start RUN_02 work."
