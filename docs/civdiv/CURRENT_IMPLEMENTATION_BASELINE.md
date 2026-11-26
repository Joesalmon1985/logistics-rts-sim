# Current implementation baseline (26 Nov 2026)

What the repository actually contains on the evening of 26 Nov 2026, before the Civilisation and Divinity run. This answers the owner brief's §37 ([OWNER_BRIEF_2026-11-26.md](OWNER_BRIEF_2026-11-26.md#37-establish-the-current-implementation-baseline)). It was written from the code, the tests and the recorded results, not from plans. Where a document and the code disagree, the code is reported and the document is flagged (§14).

**How this baseline was made.** I read the source tree, `git` (read-only), the Stage 4 DEV_LOG, ACCEPTANCE_TESTS, SHARED_CONTRACTS, WORLD_ONTOLOGY, DESIGN_CONVENTIONS and the handover, plus the docs on the two unmerged branches (`git show`/`git diff`). I ran one `dotnet build DotNet -c Release`. I did **not** run any test suite, open Unity or build the player. Every test number below is a **recorded** result, cited with its date, commit and who ran it.

**Status vocabulary used throughout:**

| Label | Meaning |
|---|---|
| **Implemented, tested** | Code exists on `unity/poc` and a recorded test asserts its behaviour |
| **Implemented, untested** | Code exists; no recorded test asserts the specific claim |
| **Stub** | A type or seam exists but returns a fixed or empty answer |
| **Designed only** | Described in a design doc; no code |
| **Not started** | Neither code nor a specific design (or only a mention) |
| **Unmerged** | Exists only on `unity/ibs` or `unity/demog` |

---

## 1. Repository position

| Item | Value |
|---|---|
| Repository | `Joesalmon1985/logistics-rts-sim` (private), Unity 6000.3.28f1 |
| Working branch | `unity/poc` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs` |
| Head | `8617eaa` "Docs: pause note and resume steps in DEV_LOG" (26 Nov 2026 22:41 UTC). Equal to `origin/unity/poc` (pushed) |
| Last code commit | `0f978db` "Merge M5: social memory, composed gestures, receiver decisions, honest barter and gifts". `8a50bb4` and `8617eaa` after it are docs-only |
| `main` | `42cadf0` (9 Jul 2026), an ancestor of `unity/poc`, 178 commits behind. Never pushed to in Stage 4 |
| Stage 3 checkpoint | Tag `stage3-final`, pointing at `d7eeb95` |
| Working tree | Clean apart from untracked `docs/civdiv/` (tonight's planning docs, written by several agents) and `docs/stage4.zip` (untracked; its origin is not recorded) |

### 1.1 Worktrees

| Path | Branch | Head | State |
|---|---|---|---|
| `wt-lrs` | `unity/poc` | `8617eaa` | Main integration worktree. Clean (see above) |
| `wt-lrs-ibs` | `unity/ibs` | `81e4f47` | **Unmerged**, paused. Clean. Pushed (`origin/unity/ibs` = `81e4f47`), although its record says "nothing pushed" |
| `wt-lrs-m3` | `unity/demog` | `5aaa255` | **Unmerged**, paused. Clean. Pushed (`origin/unity/demog` = `5aaa255`), although its record says "not pushed" |
| `wt-lrs-m5` | `unity/m5` | `9d08cb3` | Merged into `unity/poc` (in `0f978db`). Can be removed |
| `_sources/logistics-rts-sim` | detached | `2385672` (28 Jun 2026) | A worktree of the same repository on the older Godot-era history. Reference only |

### 1.2 Branches

Merged into `unity/poc`: `unity/f0`, `unity/lab-firstblow`, `unity/lab-hb`, `unity/m3`, `unity/m3-amend`, `unity/m3-wh`, `unity/m3b`, `unity/m4`, `unity/m5`, and the stage 1–3 branches (`unity/p2-economy` … `unity/q3`). M0, M1, M2 and M13a were merged earlier; their branches no longer appear locally.

Not merged: **`unity/ibs`** (4 commits) and **`unity/demog`** (6 commits). Details in §2.

---

## 2. Outstanding agents and work packages

**No agent is running.** No `God.Runner` process is alive. The stale fertility run (pid 17680) named in the demography record has ended. The owner paused all work on the evening of 26 Nov. Two work packages stopped part-way. Neither is merged, and they conflict in four files.

### 2.1 `unity/ibs`: I-BS integration gate and the M4/M5 contract amendments

Branched from `0f978db` (the M4 + M5 merge). Worktree `wt-lrs-ibs`. Record: `git show unity/ibs:docs/stage4/DEV_LOG.md`, section "I-BS integration record".

| Commit | Content |
|---|---|
| `458a97f` | Warehouse: no `dim_person` row for npc 0 (the sim's own goods moves). `PopulationMapping.cs`, `PopulationWarehouseTests.cs` |
| `73a8f63` | Sim contract amendments (21 files under `Assets/God/Sim` and tests), SHARED_CONTRACTS 12.31–12.36. (a) **Person targets:** `IntentTarget.Npc`, and `Thinker.Known` accepts a person in sight or remembered. M4's own-node workaround and M5's `IntentTarget.None` are removed. (b) **Bin Sense seam:** `PopulationExtensions.SenseBins`, `BeliefKind.Store`, and store and estate candidates on `IntentTarget.Bin`, with permission checked per lot. The O4 executor delivery and the estate-collection reflex are removed (`Estate.Delivery` deleted); goods heirs leave go ownerless at the death node. (c) **`SelfView` genome gains:** `Npc.Traits[12..15]` are no longer written, but the array length stays 16. (d) An **`OpportunitySlot.Social` column**, with `Social.Term` still 0. (e) No npc-0 person. (f) SOCIAL 4.1 familiarity changed to 180 (doc) |
| `676d8fd` | `IbsHarness.cs`, `IbsTests.cs`, scenario `ibs_families.scenario.json` (seeds 1–10, 15 days) |
| `81e4f47` | Docs: SHARED_CONTRACTS 12.31–12.36, interim I-BS status table in ACCEPTANCE_TESTS, DEV_LOG record; also removes three Temporary-mechanisms rows (executor delivery, own-node person targets, `Traits[12..15]`) |

**What ran (recorded by the I-BS agent, 26 Nov):**
- `dotnet test God.Sim.Tests`: 480/480, after the amendments and before the I-BS tests were added.
- A runner subset (M5Neutrality, the HouseholdsOff pin, M4/M5 warehouse, SmokeParity, PopulationWarehouse): green after the warehouse test fix. The both-off neutrality pins at `6671592` hold.
- The I-BS category: 11 of 13 pass.

**Not run:** the full runner suite after the amendments (M4/M5 behaviour tests may have moved), Unity EditMode and PlayMode, the Windows build, both player smokes. A baseline full suite of `0f978db` was started in a scratch copy; nobody read its result.

**I-BS status on `unity/ibs` (`ibs_families`, seeds 1–10, 15 days):**

| Criterion | Status |
|---|---|
| Neutrality (Full = Summary = Off) and warehouse reconciliation | Met, 10/10 seeds, 0 breaks |
| I-BS.1, I-BS.12 (a carer obtains food, hands it over, the child eats it) | Met 10/10 (131–294 complete chains per seed) |
| I-BS.2, I-BS.14 (shared shelter and store; permission on withdrawal) | Met 10/10; 0 non-member, unpermitted or re-owned withdrawals |
| I-BS.3 (kin and non-kin help) | Both occur; kin is **not** more frequent in raw counts (2035 vs 3130). Per-opportunity rates not computed |
| I-BS.4 (relationship strengthens, attributed) | Met 10/10 |
| I-BS.5 (exchange with both believed gains > 0) | **Partly**: 5/10 seeds; the test fails its 80% threshold |
| I-BS.6, I-BS.10 (behaviour changes after experience; tool kept) | **Not done** (controlled tests not written) |
| I-BS.7 (estate passes to heirs) | **Test fails** on seed 6 (an estate still open at the end of the run is not excluded) |
| I-BS.8 (need gesture, own decision, then a gift) | Met as occurrence, 9/10 seeds |
| I-BS.9 (a child learns by observing) | **Deferred** to M7/M11 (no observation learning exists) |
| I-BS.11, I-BS.13, I-BS.15 (conservation; help and refusal; knowledge isolation) | Met 10/10; I-BS.15 found 0 unperceived acts in about 1.6M checks |
| M4.21 first-arrival clause | Met. **Contest clause not met:** `SocialHooks.Contest` is still a stub |
| M5.9 (gifts ordered by believed kin) | **Not met** (reported only: the gift choice has no kin preference) |
| M5.18 (dialogue as a developer overlay), M5.19 (non-verbal gesture vocabulary) | **Not started** |

**Resume order (from the record):**
1. Fix I-BS.7's test so it skips estates still open at the end; set I-BS.5's threshold from evidence.
2. Run the full `dotnet test DotNet -c Release`; fix any M4/M5 behaviour tests the amendments moved.
3. Write controlled 10-seed tests for I-BS.6, I-BS.7, I-BS.8 and I-BS.10.
4. M5.9: add a kin cue to the gift recipient choice, with a 20-seed test.
5. M5.18, M5.19 and their PlayMode test.
6. Update the I-BS status table.
7. Unity EditMode, PlayMode, build, both smokes.

### 2.2 `unity/demog`: the D4 demography workstream

Branched from `bee4762` (after the M4 merge, **before M5**). Worktree `wt-lrs-m3`. It is 10 commits behind `unity/poc`. Record: `git show unity/demog:docs/stage4/DEV_LOG.md`, section "Demography workstream record".

| Commit | Content |
|---|---|
| `71f1f67` | **Relationship ontology:** `Relationships.cs`. `ReproductiveUnion` becomes the only route to conception, separate from romantic partnership. Also an `EconomicPartnership` **stub**, per-person preferences and commitments (`Npc.Traits[16..18]`, array length 19), verbs 43 SeekChild and 44 AcceptChild, events 148–150, an established-band founder generator and `Care.FeedFrom`. `DemographyTests.cs` (EditMode) |
| `0d7a1a6` | `God.Runner demog report` (`DotNet/God.Runner/Demog/`: `DemogCommand`, `DemogRecorder`, `DemogReport`) and the warehouse reconciliation test (M4.45) |
| `e8b002d` | Verb 45 RangeFar (ranging far for food; situation 22; M4-only); repair of double-encoded comments |
| `3eb8347` | The established band becomes the default of `m4_band_6x20` and `m4_families`. The young bands are kept as controls (`*_young`). Ecology scenarios `demog_favourable/balanced/hostile`. Plans `plan_care/fertility/onefactor.json`. `GodBootstrap` change (Living island on the established band) |
| `9850c9a`, `5aaa255` | Docs: D4 status cells M4.36–M4.45, M4.42 amendment, POPULATION "Demography investigation" |

**What ran (recorded):**
- God.Sim 467/467 on the ranging change (the households-off pins hold).
- `dotnet build` 0 errors.
- Runner subset `~M4|~Demog`: 15/16. The failure was `Disease_SettingsAndRoutes_M4_26` on the established band, short by one death. The test was then moved to the young band, where it passes only **vacuously** (0 disease deaths). M4.26's death clause is therefore **unverified**.

**Not run:** the full `dotnet test`. Unity was never opened, so the `GodBootstrap` change has **not been compiled by Unity**.

**Findings (recorded, not tuned):**
- **Every population goes extinct:** 10/10 seeds at 6×10, 6×20 and 6×40, by year 8–9.
- **The binding cause is fertility.** Completed fertility is 0.50–0.56 children per woman (0.70 ± 0.14 under `demog_favourable`), against about 2.3 needed for replacement. Conception needs both partners to hold "try for a child" with each other at the mother's daily check, with the partner in sight; that coordination is the bottleneck.
- Starvation is local depletion and poor exploration. Ranging cuts it by 30–45%; at 6×40 the remainder is crowding.
- The founder-death spread misses the amended target (16.0%, max 18.3% in a 3-day window, against 15%).
- **No viable favourable scenario exists** (M4.37 not met).

**D4 status on the branch:**

| Status | Criteria |
|---|---|
| Met | M4.39 (EditMode ontology fixtures), M4.45 |
| Met as written, not taken as stability | M4.36 (17/20 seeds reach generation 3) |
| Partly met | M4.41, M4.42 (amended, still failing), M4.43 |
| Not met | M4.37 |
| Not run | M4.38, M4.40, M4.44 |

**Resume order (from the record):**
1. Rebuild.
2. Run the M4/Demog runner tests.
3. Run the fertility sweep `plan_fertility.json` on `demog_favourable` (the combined biological arms).
4. Put one justified biology block into all three `demog_*` scenarios; run 20 seeds × ≥ 5 generations.
5. Run M4.38, M4.40 and M4.44.
6. **Merge `unity/poc` into `unity/demog`.** It lacks M5. Keep the both-off neutrality pins `8b898d115d8a05a7`, `5fa8991b92168385`, `66a55d5a67f2ad4e`. Keep `Npc.Traits[16..18]`.
7. Run the full `dotnet test`; open Unity only to confirm `GodBootstrap` compiles.
8. Add the SHARED_CONTRACTS rows for verb 45 and situation 22.

### 2.3 Merge order and the conflict between the two branches

The DEV_LOG says: **`unity/ibs` first, then `unity/demog`** (after merging `unity/poc` into it), then the Unity suites, the build and both smokes.

Both branches edit `Population/Households/Founders.cs`, `HouseholdActs.cs`, `HouseholdHooks.cs` and `HouseholdVerbs.cs`, plus the three Stage 4 docs (ACCEPTANCE_TESTS, DEV_LOG, SHARED_CONTRACTS). The known overlaps:
- **`Npc.Traits` length:** `unity/ibs` keeps 16 and stops writing 12–15; `unity/demog` grows it to 19.
- **`Founders.MakeLife`:** two lines removed by `unity/ibs`, in a function `unity/demog` rewrote.
- **`demog`'s `M4LifeCycleTests.cs`** also changed in the M5 merge (the re-pin).

The demography branch has never run alongside M5's social module.

---

## 3. Build status

| Date | Commit | Command | Result |
|---|---|---|---|
| 26 Nov 2026 (this baseline) | `8617eaa` | `dotnet build DotNet -c Release` | **Build succeeded, 0 errors, 5 warnings**. All are CS0675 (bitwise-or on a sign-extended operand): `Assets/God/Sim/Comms/Messages.cs:124`, `Assets/God/Sim/Social/SocialMind.cs:224` (×3) and `:225`. Incremental, 6 s |
| 26 Nov 2026 (lead, recorded) | `0f978db` | Unity Windows player build | OK, 87 MB, 0 errors, 6 warnings |

`dotnet build` compiles only `Assets/God/Sim`, `Assets/God/Recording`, the runner and the tests. **`Assets/God/Game` is compiled only by Unity**, so a `dotnet` build says nothing about the views or the HUD. The M5 CS0111 Safe-Mode incident (`9d08cb3`) came from exactly this gap.

---

## 4. Test results

### 4.1 Last recorded results on the current code (`0f978db`, which `8617eaa` equals in code)

| Suite | Result | Date | Run by | Source |
|---|---|---|---|---|
| `dotnet test` God.Sim.Tests | **480/480** | 26 Nov 2026 | lead, on the merge | DEV_LOG "M5 record" |
| `dotnet test` God.Runner.Tests | **177/179**, then **179/179** after re-pinning the two single-switch neutrality pins to F0's both-off hashes | 26 Nov 2026 | lead | same |
| Unity console | clean | 26 Nov 2026 | lead | same |
| Unity EditMode | **484/484** | 26 Nov 2026 | lead | same |
| Unity PlayMode | **79/79** | 26 Nov 2026 | lead | same |
| Windows build | OK (87 MB, 0 errors, 6 warnings) | 26 Nov 2026 | lead | same |
| Player smoke, stage 3, seed 2 | Crimson wins at tick 20161, hash **`4bd54b271857a803`** (unchanged since M0) | 26 Nov 2026 | lead | same |
| Player population smoke (`-godpopulation`, seed 2, 60,000 ticks) | exit 0, hash **`60d549c28635cc44`** (new: M4 and M5 together) | 26 Nov 2026 | lead | same |
| Neutrality pins (Households off and Social off = F0 both-off) | `8b898d115d8a05a7`, `5fa8991b92168385`, `66a55d5a67f2ad4e` (band_6x20, seeds 1–3) | 26 Nov 2026 | lead | same |
| `lab all --check` | 70/70 in 33.8 s | at `10a7142` (M0+M1+M2) | lead | DEV_LOG "Test results" |

Not run on the current code: the 200-year nightly neutrality run (M0.2 N), `LongRun_200Years` (M4.24), CI wiring for the runner and player smokes, and the owner's hand-play check. `[Explicit]` targets (`M3-target`, `M5-target`, `M4` explicit tests) are excluded from the counts above by design.

### 4.2 Milestone history of the suites (recorded)

| Point | God.Sim | God.Runner | EditMode | PlayMode |
|---|---|---|---|---|
| Stage 3 baseline `d7eeb95` | 282 | – | 282 | 73 |
| M0 merge | 313 | 48 | 313 | 74 |
| 4A `3463d30` | 359 | 110 | 363 | 77 |
| M3b `2640cb2` | 402 | 136/138 | 406 | 78 |
| F0 merge | 429 | 157 | 417 | 78 |
| M4 (agent) | 462 | 171 | 466 | 78 |
| M5 (agent, `9d08cb3`) | 447 | 165 | 451 | 79 |
| **M4 + M5 merge `0f978db`** | **480** | **179** | **484** | **79** |

### 4.3 Test inventory by file (static count of `[Test]`/`[UnityTest]`/`[TestCase]` lines, `unity/poc`)

These are attribute lines, not executed cases. `TestCase` expansion and `[Explicit]` make the run counts differ.

- **`Assets/God/Tests/EditMode`** (compiled into God.Sim.Tests and Unity EditMode; 442 lines). The largest files:
  - `TerritoryTests` 37, `LabTests` 27, `MilitaryTests` 23, `AiTests` 21, `EconomyTests` 21, `PopulationTests` 20;
  - `SaveTests`, `Stage3EconomyTests` and `TradeTests` 19 each;
  - `DuelTests`, `FactionTests` and `HawkBullyStudyTests` 17 each, `FirstBlowStudyTests` 16;
  - `BoardTopologyTests`, `HouseholdTests`, `PolicyTests` and `TutorialTests` 12 each, `EstateTests` 11;
  - `GeneticsTests` and `GoodsTests` 10 each, `SocialMemoryTests` 9, `CommsTests` 8;
  - `KnowledgeViewTests`, `IslandBoardTests` and `CalendarTests` 7 each, `LearningCreditTests` 6;
  - `StateHashTests`, `EventStreamTests`, `GodSimTests` and `RngTests.Keyed` 5 each, `VictoryConditionTests` 4;
  - `StaticAuditTests` 2, `SocialIslandTests` and `TutorialEndToEndTests` 1 each.
  - Helpers with no tests: `HouseholdFixtures`, `SocialFixtures`, `Support/EventProbe`, `Support/GoodsTrace`, `TypeReferenceScanner`.
- **`Assets/God/Tests/EditModeGame`** (Unity only): `HudViewOnlyTests` 4. These are the reflection tests that the HUD reads only the view.
- **`Assets/God/Tests/PlayMode`** (79 lines, which equals the recorded 79): `HudTests` 18, `MenuTests` 14, `TutorialHudTests` 12, `ModelViewTests` 5, `AiPlayTests` 4, `IslandViewTests` 4, `TradeViewTests` 4, `AiTradePlayTests` 3, `HudKnowledgeTests` 3, `BootSceneTests` 2, `EconomyCardTests` 2, `FactionViewTests` 2, `Q3Shots` 2, `EconomyViewTests` 1, `MenuShots` 1, `PopulationViewTests` 1, `SocialViewTests` 1.
- **`DotNet/God.Runner.Tests`** (203 lines, including about 20 `[Explicit]`):
  - `CliTests` 30, `LabRunnerTests` 26, `WarehouseTests` 21, `PolicyRunTests` 19, `LearningScenarioTests2` 13;
  - `M3AmendmentEvidence` 11 (Explicit), `M4LifeCycleTests` 11, `M3RevisedCriteriaTests` 10, `DeterminismTests` 9;
  - `PopulationRunTests` 8, `PopulationWarehouseTests` 8, `LearningAcceptanceTests` 6;
  - `GameRulesRunTests` 4, `M5LearningTests` 4, `IslandPopulationTests` 3, `M4CareTests` 3, `M4Harness` 3, `M5BarterTests` 3;
  - `M4Sweeps` 2, `ReplayTests` 2;
  - `F0GoodsConservationTests`, `LearningScenarioTests`, `M4WarehouseTests`, `M5NaturalTests`, `M5NeutralityTests`, `M5WarehouseTests` and `SmokeParityTests` 1 each.

### 4.4 Performance (recorded; the bench machine, a Ryzen 7 5800X, is not declared the reference machine)

| Case | Result | Commit |
|---|---|---|
| Stage 3 watch6, no NPCs | 9,758 ticks/s; 47 KB allocated per tick (budget 64 KB); 8-worker scaling 0.74 (target 0.8) | `46d9fad`+`d44aa05` |
| `bench_npc_500` (504 NPCs) | 3,185 ticks/s; 8.11 µs per think; learning 37.5% of NPC update time (an aspiration, not a gate); 16.4 KB per NPC; 4.67 MB save | `2640cb2` (M3b) |
| `bench_npc_1000` | 1,511 ticks/s; 9.10 µs per think; 33.9% learning share; 9.66 MB save | `2640cb2` |
| Recording overhead, stage 3 | SQLite full +6.3%; JSONL full +3.9%; same hash in every mode | `84cb8cb` |

No benchmark has been run with M4 + M5 together.

---

## 5. Milestones: implemented, partial, not started

The official list is DESIGN_CONVENTIONS §10 (M0–M15, checkpoints 4A–4F).

| Milestone | Checkpoint | Status on `unity/poc` | Evidence |
|---|---|---|---|
| M0 runner, `SimTuning`, calendar, victory, events, replay | 4A | **Implemented, tested, accepted** | DEV_LOG completed milestones; hashes match between Unity and the runner |
| M1 policy seams, `FactionKnowledgeView` | 4A | **Implemented, tested, accepted**, with the temporary `AiBrain` adapter (Q21 breach) | same |
| M2 game-theory lab | 4A | **Implemented, tested, accepted**; nine expectations amended with evidence; the L2 Hawk/Bully finding investigated (`unity/lab-hb`), with a first-blow variant (`unity/lab-firstblow`) | same |
| **Checkpoint 4A** | | **Closed** at `3463d30` | |
| M3 NPC core and associative learning | 4B | **Accepted as a functional milestone** (owner D3 §1), with outstanding targets (§6.1) | ACCEPTANCE "M3 status after D3" |
| F0 shared M4/M5 scaffolding | (between 4B and 4C) | **Implemented, tested, merged**. Behaviour-neutral; save v4; schema v4 | DEV_LOG outstanding work |
| M4 life cycle, households, kinship, genome, estates | 4B | **Implemented and partially accepted; demographic viability outstanding** (owner D4 §7) | ACCEPTANCE "M4 acceptance status" |
| M5 social memory, gestures, barter, gifts | 4C | **Implemented, merged; partly accepted** (§6.3) | ACCEPTANCE "M5 status as built" |
| I-BS integration gate | after M4 + M5 | **In progress, unmerged** (`unity/ibs`, §2.1) | |
| D4 demography workstream | M4 additions | **In progress, unmerged** (`unity/demog`, §2.2) | |
| M13a warehouse core (pulled forward) | 4E | **Implemented, tested** for stage 3, population (M13a.5), decisions, F0 goods, households (0005) and social (0006). M13a.6 (economy and citizenship facts) waits for M6/M10 | migrations 0001–0006 |
| M6 NPC labour, basic state, ownership records | 4C | **Not started** (no code; walkers still do the work) | |
| M7 deferred exchange, promises, reputation, lies, observational learning | 4C | **Not started** | |
| M8 enterprise, communal property, theft, witnesses, class measures | 4D | **Not started** (`Permits.Believed` exists; theft does not) | |
| M9 soldiers as NPCs | 4D | **Not started** (soldiers are `MilUnit`s) | |
| M10 citizenship, loyalty, migration, defection | 4D | **Not started** (only `Npc.Faction` and a temporary immigration rule) | |
| M11 cultural transmission | 4D | **Not started** | |
| M12 band-age institutions | 4E | **Not started** (`IInstitutionPolicy` and `InstitutionView` are placeholders) | |
| M13b institution KPIs and reports | 4E | **Not started** (`dim_kpi`/`fact_kpi` tables exist but nothing fills them) | |
| M14 incentive faction AI, HUD and tutorial rewrite | 4F | **Not started** | |
| M15 network readiness | 4F | **Not started** (the runner's `serve`/`dataset`/`verify-model` exit with code 9) | |

**Functional releases:** "First playable new game" (M0–M7 plus M9) is **not reached**: M6, M7 and M9 have not started. "Analytical recording" has M0 JSONL and M13a; it lacks M13b.

---

## 6. Partially accepted milestones and failed acceptance criteria

### 6.1 M3 (accepted with outstanding targets, owner D3)

- **Implemented capabilities (all behaviourally tested, in the normal suite):**
  - context-sensitive hunger;
  - delayed reinforcement;
  - twins diverging;
  - learned associations driving choices;
  - generalisation;
  - save/load persistence.
- **Revised criteria** (each passes on the learner and fails on its control; `M3RevisedCriteriaTests.cs`): M3.1, M3.20, M3.2, M3.3, M3.7, M3.11.
- **Outstanding targets** (`[Explicit]`, category `M3-target`, asserting the original numbers; each fails):

| Target | Recorded result |
|---|---|
| M3.5 / L5 (forgetting a learned place) | The scenario does not depend on learning |
| L2b (sated eating after repeated hunger) | 43.7% vs Frozen 25.3% |
| **M3.21 / LS3 (shelter)** | 0.5% vs 1.3%; **redesign scheduled after M4, not started** |
| **M3.22 / LS4 (axe, per seed and conjunction)** | 27/50 seeds; conjunction 16/40; **redesign and the "retain productive assets" scenario not started** |
| M3.19 (learning ≤ 25% of NPC update time) | 37.5%; an aspiration, not a gate |

- **Island control:** learning On 666 alive vs Frozen 675 of 720 (an over-harvest of the commons, recorded and not tuned).

### 6.2 M4 (partially accepted; demographic viability outstanding)

| Status | Tests |
|---|---|
| Met | M4.1, M4.2, M4.3, M4.8, M4.9, M4.10, M4.11, M4.14, M4.17, M4.18 (divorce clause untested), M4.19, M4.20, M4.22, M4.25, M4.26 (the death clause is now in doubt, §2.2), M4.30, M4.32, M4.34, M4.35 |
| Met as written, not taken as stability | M4.36 |
| Partly met | M4.28 (the 1,000-fixture equality test not built); **M4.29**: the exploitation clause **fails** (1 of 10), kept `[Explicit]` |
| **Failed** | **M4.5** (stable over 10 generations) and **M4.6** (age pyramid): every default seed goes extinct. M4.27 and M4.33 pass as written, but what they record is universal extinction |
| Amended (proposed) | M4.31 / MATERIAL 20 ("no new ids" vs the 4/3/3 split) |
| Deferred | M4.21 wills, to M8 |
| Not done | M4.4, M4.7, M4.12, M4.13, M4.15, M4.16, M4.23, M4.24, the immigration-off `Stable_10Generations` arm |
| New D4 criteria (M4.37–M4.45) | not started on `unity/poc`; partly done on `unity/demog` (§2.2) |

### 6.3 M5 (merged; partly accepted)

| Status | Criteria |
|---|---|
| Met | M5.1, M5.2 (needs familiarity +180, not 100), M5.3, M5.4, M5.5, M5.7, M5.8, M5.10, M5.14, M5.15 (Warn is composed but never occurs naturally), M5.16, M5.17, the neutrality pin |
| Partly met | **M5.11 / L16**: relationships 49/50; communication 35/50 (`M5-target`); the barter and cooperation domains are not built |
| **Not met** | **M5.6 / L14** (avoidance after being cheated): 32% against ≤ 20%, recovery 7% against > 50%. On `unity/ibs`, turning the social term on only reaches 29% |
| Not done | M5.9 (moved to I-BS, still not met), M5.12 (S2/S5: needs island contests), M5.13 / LS2, island contests, false-match recognition |
| Not started | M5.18, M5.19 (owner D4 §8) |

### 6.4 I-BS

See §2.1. Not merged. I-BS.5 partly met, I-BS.6 and I-BS.10 not done, the I-BS.7 test fails, I-BS.9 deferred. The other criteria pass on the branch only.

---

## 7. Deferred requirements (recorded deferrals, with where they go)

| Requirement | Deferred to | Why |
|---|---|---|
| Wills, `WillIgnored`, disputes (M4.21) | M8 | Not built in M4 |
| Observational learning (I-BS.9, teaching, `ActivityObservable`) | M7 / M11 | No observation learning exists |
| Island contests (`SocialHooks.Contest`, M5.12 S2/S5) | not scheduled (M8 witnesses and contests) | M5 kept the stub |
| Emigration, divorce with a store split, expulsion, norm voting, elder strength/speed decline, `GeneticsYearly`, selection metrics, learned begging intensity, the 200-year run | not scheduled ("Not built in M4") | Scope |
| LS3 shelter and LS4 tool-retention redesigns | "after M4" (owner D3) | Not started |
| Executor delivery removal | when bin perception lands | **Done on `unity/ibs` only** |
| Stage 3 retirement (walkers M6, `MilUnit` M9, `AiBrain` M14, stage 3 mode M14) | see §8.1 | |
| Limited-information knowledge profiles | Stage 6–7 | `FullInformation` everywhere in Stage 4 |
| Runner `experiment` (M14), `serve`/`dataset`/`verify-model` (M15), `export --to sqlite` from JSONL | named milestones | Exit code 9 stubs |

---

## 8. Technical debt

### 8.1 Temporary mechanisms (DEV_LOG table, checked against the code)

| Mechanism | In code today? | Removal milestone |
|---|---|---|
| Stage 3 mode (`GameSetup.Population` off) and its tests | Yes. It is the default game; population mode is opt-in (`-godpopulation`, menu "Living island") | M14 (the owner decides) |
| **Walkers** (carriers, builders, harvesters) beside NPCs | Yes: `Sim/Economy/EconomySystem.cs`, `Game/Models/WalkerLayer.cs`. NPCs and walkers do not interact | M6 |
| **`MilUnit` spawning** | Yes: `Sim/Military/*` (`UnitQueue`, `DevSpawnUnit`); soldiers are not people | M9 |
| **`AiBrain`** behind the `IFactionPolicy` adapter, including the Q21 breach (`AiTrader` reads other factions' stock; `AiTradeJudge` sits outside the seam) | Yes: `Sim/Ai/AiBrain*.cs`, `Sim/Policy/FactionPolicies.cs` | M14 |
| Executor delivery (O4); own-node person targets; `Traits[12..15]` gains | Yes on `unity/poc`; **removed on `unity/ibs`** | the I-BS merge |
| Saves don't record the policy assignment | Yes | listed as M3, **overdue** |
| Non-HUD Unity code reads the sim directly (`NpcLayer`, `LifeStageLayer`, `GestureLayer`, `IslandView`, boot, menu, tutorial) | Yes (confirmed: `LifeStageLayer.Apply(Transform, Npc, GodState)`) | M14 |
| `Stage3MilitaryPolicy` as the default military policy | Yes | M9 |
| Legacy `double` code on the allow-list | Yes | shrinking |
| Automatic immigration rules (the HQ answers `AskJoin`; arrival rule) | Yes: `Founders.cs` `Immigration` | Stage 5 |
| `MaxLivePartnerships` = 1 | Yes (`PartnerTuning`) | raised by custom later |
| Scenario keys `population` etc. refused | **Stale row**: population scenarios exist and run since M3 | – |
| Automatic HQ paymaster (`IPayPoint`), granary keeper, `StaticCultureProvider`, `FoundCommunal`, trait clauses in unlock rules | **Not in code** (no matching types; `StaticCultureProvider` is named only in a comment in `EstateTuning.cs`). These rows describe planned stand-ins for M6–M12 | M6–M12 |
| `FullInformation` knowledge profile | Yes, the only profile filled (`Reported` throws `NotSupportedException`) | Stage 6–7 |
| `TutorialSteps.All` on the static-audit allow-list | Yes | M14 |
| Runner exit-code-9 commands | Yes | named milestones |

### 8.2 Stubs and placeholders in the code

| Stub | File | Effect |
|---|---|---|
| `Affordances.Of(...) => 0` | `Assets/God/Sim/Cognition/Affordances.cs:43` | There is no affordance query; M3, M4 and M5 candidates are built inline in `Thinker` and the module hooks |
| `SocialHooks.Contest(...) => a < b ? a : b` | `Assets/God/Sim/Social/SocialHooks.cs:40` | Simultaneous claimants never contest; the lower id wins silently |
| `EconomicPartnership` | only on `unity/demog` (`Relationships.cs`) | A record with no behaviour |
| `InstitutionView` | `Assets/God/Sim/Policy/Observations.cs:6` | "PLACEHOLDER until M12": id, faction, tick |
| `IInstitutionPolicy` | `Assets/God/Sim/Policy/PolicyInterfaces.cs` | Interface only; no implementation, no institutions |
| `NpcVerb` civilian placeholders `Idle/Move/Work/Eat/Rest/Gesture` (1–6) and the reserved blocks 106–139 (M4), 140–169 (M5), 170–199 (M6+) | `Assets/God/Sim/Policy/PolicyTypes.cs` | Reserved ranges for new intents |
| `OwnerKind.Group/Enterprise/State` | `Assets/God/Sim/Population/LotBin.cs` | Enum values exist; only `Owner.Group(id)` is constructed, and nothing uses them |
| `Permits.Believed` | `Assets/God/Sim/Cognition/Affordances.cs` | A query only; never enforced (by design until M8) |
| `dim_kpi`, `fact_kpi`, `fact_policy_change` | `0001_core.sql` | Tables exist; no KPI engine fills them |
| `ProduceReason` 4–7, `ConsumeReason` 5–7, `MoveReason` 10–31 | `Assets/God/Sim/Population/PopulationState.cs`, `Goods.cs` | Reserved slots (useful for miracles, §12) |

### 8.3 Other debt

- **Stage 3 systems run beside Stage 4.** Population mode is layered on a full stage 3 game: HQs, build circles, walkers, the stage 3 economy, `MilUnit` armies, stage 3 trade envoys and the `AiBrain` faction bots all still run. NPCs exist alongside them, not instead of them.
- **The dead keep their record, not their mind.** `PopulationModule.Kill` sets `npc.Mind = null` (`Assets/God/Sim/Population/PopulationModule.cs:106`). Dead NPCs stay in `PopulationState.Npcs` forever and are skipped by alive checks. There is no archive.
- **There is no command path into the NPC world.** The population, household and social modules register no command handlers (§11).
- Five CS0675 warnings (§3).
- The demography branch has never run with M5. The I-BS branch's full runner suite has not been read.
- Unity runs are slow under editor throttling (45–100 minutes per run), so they are run at checkpoint boundaries only.
- M0 leftovers: the 200-year neutrality run, CI wiring for the smokes, the owner's hand-play check.

---

## 9. Shared architecture (as built)

- **Layers.** `Assets/God/Sim` is pure C# (`noEngineReferences`), deterministic (integers, keyed `Rng` streams via `Rng.Fork(purpose, ids)`, no clock, ordered enumeration). `Assets/God/Game` reads sim state, sends commands and animates events; it never mutates. `Assets/God/Recording` (JSONL, replay, scenario, smoke verdict) is shared by Unity and the runner. `DotNet/` compiles the same sources for `dotnet test`, plus `God.Runner` (CLI and the SQLite warehouse).
- **Sim loop.** `GodSim` (`Sim/Core/GodSim.cs`) holds `GodState`, a sorted list of `ISimSystem`s (order bands: Economy 200, Social 245, Population 250, Military 300, Factions 400, AiDirector 900) and a `Dictionary<Type, ICommandHandler>`. Commands go through `ICommandGate.Check` (validate, no mutation) or `Submit` (validate, then queue). Queued commands are applied at the next tick boundary, where `Apply` re-validates; a failure becomes `CommandRejected`. Commands submitted outside `Step` are "external" and are logged to the replay through `ExternalCommandAccepted`. **Pause rule (M1):** a paused game takes one ordinary tick when a command is accepted.
- **Modules install themselves** (`InstallModules`): Territory, Economy, Military, Trade, Factions, Population; then, when a population exists, `PopulationExtensions.Install` (M4, then M5). A later module's state is a `GodState` partial that stays **null unless started** (`GodState.Households.cs`, `GodState.Social.cs`), so older games keep their hash.
- **Hash.** `StateHashParts()`: territory, economy, military, factions, trade, core, pending; then population, cognition and learning, plus household and social when present. New modules append parts at the end. Pending commands are hashed.
- **Tuning.** `SimTuning` (`Sim/Core/SimTuning.cs`) is a tree of blocks, saved and hashed, settable by dotted integer paths (`--set`, scenario files). A new block implements `IOmitWhenDefault`, so adding it leaves older JSON and hashes unchanged.
- **Domain events.** `DomainEvent` (`Sim/Telemetry/DomainEvent.cs`) carries code, version, domain and target table from `EventCatalog`, which is concatenated from per-module catalogues (`Core`, `HouseholdEvents.Catalog`, `SocialEvents.Catalog`). Codes are never reused, and they fall in per-domain ranges (`RangeOf`). Analytics-only truth fields are recorded at Full only. The `TelemetryDomain` enum has **no divine, religion or organisation domain**. Codes 800–899 are unassigned.
- **Recording.** The levels are Off, Metrics, Summary and Full, with per-domain switches. Every recording level gives the same hash; this is asserted per milestone. The runner writes JSONL and/or SQLite (`DotNet/God.Runner/Warehouse`, schema v6 via migrations 0001–0006) and exports CSV or Parquet. SQL-to-event reconciliation is exact and tested.
- **Save.** `SaveFile` v4 (`Sim/Save/SaveFile.cs`) is JSON of `GodState`, pending commands and host extras; older versions are refused.
- **Replay.** `.godr` files (`Recording/Replay.cs`): a header (scenario, seed, start kind), then external commands by tick and hash checkpoints. Commands to the runner: `replay`, `verify` (including `--against-unity`).
- **Policies.** There are four levels (`Sim/Policy/PolicyInterfaces.cs`). Civilian NPCs use `INpcPolicy.Decide(NpcObservation, MindView, ref Rng)`, which returns an `NpcIntent`; the default is `AssociativeNpcPolicy`. The intent must match one of the `Thinker`'s perceived candidates, or it is refused with a frustration penalty (`PopulationSystem.Think`, around lines 699–730). Factions use `IFactionPolicy.Decide(FactionKnowledgeView, …)`, which returns `FactionCommand`s, the same set the HUD sends (`FactionCommandSet`, found by reflection). `PolicyRegistry` assigns policies by name; `Npc.Policy` is a per-NPC registry name. A reflection test forbids any policy from referencing `GodState`.
- **Information layers.** True state lives in `GodState`. An NPC's perception (`ThinkScratch.Seen/People/Heard`) is built by the sim's senses; its beliefs are `NpcMind.Beliefs` (places only), `KinMind` and `SocialMind`. The faction layer is `FactionKnowledgeView` via `ViewFiller`, the single adapter (`FullInformation` only). The developer true-state overlay depends on a `ScenarioPurpose` host setting (F8 in the HUD, `-goddev`). There is no divine-observation layer.
- **Goods and ontology.** One API (`Sim/Population/Goods.cs`) creates, moves, splits, merges and re-owns `ItemLot`s between `Holder`s (`Carry`, `Ground`, `Bin`), each with an `Owner` (`OwnerKind`). The `GoodsLedger` keeps `Σ produced − Σ consumed = Σ held` per ware (invariant C1). Physical ranges are in `Reach`: Enter 0, Touch 1, Gather 2, HandOver 1, Store 1. Communication range is sight (`CommsRange`). `Permits.Believed` answers permission. Stage 3 HQ stocks and walkers keep separate books.

---

## 10. Relevant existing types and interfaces

| Area | Type | File (under `Assets/God/Sim/` unless stated) | Notes |
|---|---|---|---|
| State | `GodState` (partial) | `Core/GodState.cs`, `GodState.Households.cs`, `GodState.Social.cs`, `Core/Shared.cs`, `Territory/GodState.Territory.cs` | Seed, Tick, Rng, Tuning, Board, Population, Households, Social, Territory, Economy, Military, Trade, Factions, Setup |
| Setup | `GameSetup` | `Core/GameSetup.cs` | Human faction (`Human`, `NoHuman`), Population flag, founders, disease, calendar, victory list, `Purpose` and `AllowTrueStateOverlay` (host settings) |
| Sim | `GodSim`, `ISimSystem`, `StateHasher` | `Core/GodSim.cs` | |
| Commands | `Command` (an `int PlayerId`), `CommandResult`, `ICommandGate`, `ICommandHandler`, `CommandHandler<T>` | `Core/Commands.cs` | |
| | `FactionCommand` (base of every player and AI command) | `Territory/TerritoryCommands.cs` | `PlayerId` is the faction |
| Events | `SimEvent`, `EventBuffer` (`TraceDecisions`), `CommandApplied/Rejected` | `Core/Events.cs` | |
| | `DomainEvent`, `EventInfo`, `EventCatalog`, `TelemetryDomain` | `Telemetry/DomainEvent.cs` | |
| Tuning | `SimTuning`, `IOmitWhenDefault`, `DecayTuning` | `Core/SimTuning.cs` | Blocks: Military, Decay, Population, Ecology, Learning, Households, Genetics, Estate, Social, Comms |
| Calendar | `SimCalendar`, `LifeStage` | `Core/SimCalendar.cs` | 1200 ticks a day, 10 days a year, `LifeScale` |
| Host build | `SimBuilder`, `BuiltSim` | `Core/SimBuilder.cs` | Shared by Unity and the runner |
| Invariants | `SimInvariants`, `PopulationInvariants`, `HouseholdInvariants` | `Core/`, `Population/`, `Population/Households/` | |
| Person | `Npc` | `Population/PopulationState.cs` | Identity, body (Health, Nutrition, Injury, Drives), position, `Carry`, `Traits`, `Instincts`, `Policy` name, `NpcAction`, keyed `Rng`s, `Mind`, `Life` (M4) |
| | `NpcLife`, `Household`, `Partnership`, `CareArrangement`, `HouseholdState` | `Population/Households/HouseholdState.cs` | |
| | `PopulationModule.Spawn/Kill`, `DeathCause` (9 values; `DeathsByCause` is an `int[9]`) | `Population/PopulationModule.cs`, `PopulationState.cs` | `Kill` nulls the mind |
| | `LifeCycleSystem`, `Reproduction`, `Disease`, `Founders`, `Immigration` | `Population/Households/` | |
| | `Genome`, `GenomeOps`, `PhenotypeBuilder`, `PedigreeRecord`, `Relatedness` | `Population/Genetics/Genome.cs` | |
| | `KinMind`, `KinBelief` | `Population/KinData.cs` | |
| | `Estates` (customs, executor ladder) | `Estate/Estates.cs` | |
| Goods | `ItemLot`, `ResourceSource`, `Hut`, `GroundLot`, `GoodsLedger`, `ProduceReason`, `ConsumeReason` | `Population/PopulationState.cs` | |
| | `Goods` (Move, Produce, Consume, SetOwner…), `MoveReason`, `OwnerRule`, `HandOver` | `Population/Goods.cs` | |
| | `Owner`, `OwnerKind`, `Holder`, `HolderKind`, `LotBin`, `BinKind` | `Population/LotBin.cs` | |
| | `Reach` | `Population/Reach.cs` | |
| | `HutRights`, `StoreAccess`, `Partnerships` | `Population/Households/Households.cs` | |
| Cognition | `Thinker`, `ThinkScratch`, `SelfView`, `Sensed`, `Candidate` | `Cognition/Thinker.cs` | Builds candidates from perception; `Known()` checks perception |
| | `NpcMind`, `BeliefStore`, `Belief`, `BeliefKind`, `DecisionRecord`, `OpportunitySlot`, `ExperienceSlot` | `Cognition/Mind.cs` | `DecisionRecord` terms: Instinct, Need, Learned, Social, Culture (0), Personality, Cost, Explore |
| | `Affordances` (stub), `Affordance`, `Blocked`, `Permits` | `Cognition/Affordances.cs` | |
| | `SeenPerson`, `PeopleIndex`, `NpcRelClass` | `Cognition/People.cs` | |
| | `CognitionExtensions`, `VerbRanges`, `Relations` | `Cognition/Extensions.cs` | |
| | `PopulationExtensions` (dispatch to M4, then M5) | `Population/PopulationExtensions.cs` | |
| Learning | `Verb` (byte ids; ranges: core 0–31, M4 32–47, M5 48–59 and 64–95, lab 60–62, **M6+ 96–127**) | `Learning/Verbs.cs` | |
| | `AssociationStore`, `Learner`, `EpisodeKey`, `Features` (`FeatureKind`, `Drive`: Hunger, Fatigue, Cold, Fear, Pain), `Instincts`, `LearningEvents` (`DecisionMade` 334) | `Learning/` | |
| Policy | `INpcPolicy`, `IMilitaryPolicy`, `IInstitutionPolicy`, `IFactionPolicy`, `PolicyLevel` | `Policy/PolicyInterfaces.cs` | |
| | `NpcIntent`, `NpcVerb`, `IntentTarget`, `IntentRules`, `FactionCommandSet` | `Policy/PolicyTypes.cs` | |
| | `MindView`, `NpcObservation` | `Policy/MindView.cs` | |
| | `AssociativeNpcPolicy`, `PolicyRegistry`, `AiBrainFactionPolicy`, `IdleFactionPolicy`, `Stage3MilitaryPolicy` | `Policy/` | |
| Social | `SocialMind`, `Relation`, `SocialSim`, `SocialHooks`, `SocialCandidates`, `Valuation`, `DealTrace`, `SocialState` | `Social/`, `Core/GodState.Social.cs` | |
| Comms | `GestureAct` (0–17, room for acts up to 31), `MsgTopic`, `TopicKind`, `DecodedMessage`, `Messages`, `CommsRange` | `Comms/` | |
| Information | `FactionKnowledgeView`, `ViewFiller`, `KnowledgeProfile`, `KnowledgeDomain`, `ScenarioPurpose`, `TrueStateOverlayRules` | `Information/` | |
| | `PopulationKnowledge`, `PeopleCards.Household`, `PeopleCards.Social` | `Information/` | The People card's data |
| Military | `MilitaryState`, `MilitarySystem`, `Duel`, `GroupBattle`, `Pathing` | `Military/` | Stage 3 units |
| Stage 3 | `TerritoryState`, `NodeGrid`, `EconomyState`, `TradeState`, `FactionsState`, `Faction` (enum: Amber, Azure, Crimson, Jade, Violet, Ivory) | `Territory/`, `Economy/`, `Trade/`, `Factions/` | |
| Victory | `IVictoryCondition`, `VictorySpec` (VP10, LastStanding, Sandbox) | `State/Victory.cs` | |
| AI | `AiBrain`, `AiDirector` (seats of `IFactionPolicy` + view), `AiTrader` | `Ai/` | |
| Save | `SaveFile` (v4), `SaveHeader` | `Save/SaveFile.cs` | |
| Recording | `Recorders`, `EventJson`, `ReplayWriter/Reader`, `Scenario`, `SmokeVerdict` | `Assets/God/Recording/` | |
| Runner | `Cli`, `RunEngine`, `Batch`, `Bench`, `Verify`, `LabCommand`, `LearnDump`, `SimSession` | `DotNet/God.Runner/` | |
| Warehouse | `WarehouseRecorder`, `WarehouseWriter`, `EventMapping`, `Tables*.cs`, `*Mappings.cs`, `Export` | `DotNet/God.Runner/Warehouse/` | 0001 core; 0002 person, death, person-day, learning; 0003 decision; 0004 party, ownership, social day; 0005 birth, migration, household, membership, care, inheritance; 0006 social interaction, transaction, transaction line |
| Scenarios | `band_6x20`, `m4_band_6x20`, `m4_families`, `bench_npc_{120,300,500,1000}`, `bench_stage3_watch6`, `sandbox_watch6`, `vp_duel`, `watch6` | `DotNet/God.Runner/Scenarios/` | |
| Unity boot | `GodBootstrap` (flags `-godseed`, `-godwatch`, `-godpopulation`, `-goddev`, `-godrun`, `-godspeed`, `-godsmoke`, `-godhexlook`…), `AiHost`, `SmokeRun`, `IslandCamera` | `Assets/God/Game/Boot/` | |
| Unity camera | `GodCameraRig` (wheel zoom to the cursor, drag and WASD pan, **fixed tilt, no rotation, no follow**), `WatchCamera` (watch mode: fly to faction HQs, cycle captures and fights) | `Assets/God/Game/Hud/` | |
| Unity HUD | `GodHud` partials: Gui, People (+Household, +Social), Standings, Tabs (build bar), Trade, TrueState, Tutorial; `HudOverlay`, `HudStyle` | `Assets/God/Game/Hud/` | IMGUI; reads only `FactionKnowledgeView`; acts only through `ICommandGate` |
| Unity models | `NpcLayer`, `LifeStageLayer` (child scale, elder stoop, pregnancy, begging bob), `GestureLayer` (raised arm, line, **written text**), `PeopleModels` (`ToolKind`), `WalkerLayer`, `UnitLayer`, `BuildingLayer`, `EnvoyLayer`, `FxLayer`, `OverlayLayer`, `StandardLayer`, `GroundLayer`, `PropModels`, `ModelKit` | `Assets/God/Game/Models/` | |
| Unity island | `IslandView` (P4a organic look: one ground mesh, wandering borders, irregular coast, props; **hexes hidden** by default), `IslandField`, `IslandGroundMesh`, `IslandDressing`; legacy `HexDressingView` (`-godhexlook`); `View/IslandGround`, `TerrainBuilder` | `Assets/God/Game/Island/`, `Board/`, `View/` | |
| Unity menu | `GameMenu` (Play as Amber, Watch 6 AIs, Tutorial, Living island, Load), `SaveStore`, `ToastFeed` | `Assets/God/Game/Menu/` | |

---

## 11. What the player controls today, and the conflict with "a god, not a government"

**Today the human player is a faction (a government), not a god.**
- `GameSetup.Human` = 0 makes the player **Amber**. "Living island" is "a sandbox with a population … you as Amber" (`GameMenu.cs:117`).
- The tutorial says "You are Amber. Your people … build what you tell them to" (`TutorialSteps.cs:70`).
- `Command.PlayerId` is a faction id. Every player and AI command derives from `FactionCommand`.
- There is no god identity, no Mythic, and no command addressed to an NPC, household or location as a divine act.
- A text search for divine, miracle, possess, avatar, commandment, smite, resurrect, worship, prayer, deity or Mythic in `Assets/God` finds nothing relevant.

**The 20 existing command types** (all have registered handlers; 18 are `FactionCommand`s):

| Group | Commands | Sent by the HUD? | Conflict with D5 / brief §4 |
|---|---|---|---|
| Territory | `PlaceHq`, `PlaceBuilding`, `RemoveBuilding`, `BuildRoad`, `RemoveRoad`, `PlaceFlag` | yes | **Direct state administration** (the player plans the faction's infrastructure) |
| Economy | `SetInputPriority`, `SetPaused`, `UpgradeBuilding` | yes | **Direct management** of buildings |
| Factions | `DeclareWar`, `OfferPeace`, `UpgradeToCitadel` | yes | **Direct political authority** (war and peace) |
| Military | `MarkTarget`, `UnmarkTarget`, `SetDefend`, `RemoveDefend` | yes (`SetDefend.OnBuilding/OnNode`, `GodHud.cs:420`, `:428`) | **Direct army command** (brief §23: the player influences leaders, never commands the army) |
| Trade | `OfferTrade`, `AnswerTrade` | yes | **Direct state trade** |
| Developer | `DevSpawnUnit`, `DevMoveUnit` (plain `Command`, refused from policies) | no (dev only) | Developer tools; keep out of play |

**What conflicts, precisely:**
- All 18 faction commands are, under the brief, decisions that belong to **NPC institutions** (or to the faction AI as a stand-in until M12/M14).
- In a god game, the human must not send them for "their" faction. The paths that would conflict are `GodHud` (build bar `GodHud.Tabs.cs`; `GodHud.cs` lines 220–456; `GodHud.Trade.cs` 139–147), the tutorial pointers (`GodHud.Tutorial.cs`), `TerritorySample` and the `-godfight` debug path in `GodBootstrap`.
- **`FactionCommandSet` is a reflection over every `FactionCommand`.** A divine command must therefore **not** derive from `FactionCommand`: if it did, faction AIs could send it, and `HudViewOnlyTests` and `S-CMD-1` would treat it as a state command.
- **`IFactionPolicy` is already the right seam for "governments decide for themselves".** In watch mode every faction is an `AiBrainFactionPolicy` seat in `AiDirector`, so a god-mode game could make all six factions AI seats (as `-godwatch` does) and give the human only divine commands.
- **The HUD's eyes are a faction's eyes** (`FactionKnowledgeView` for `Me`). The People card shows only the viewer's own faction in full; other people appear as sightings. A god needs its own observation view (brief §36: "divine observation" is distinct from faction knowledge).
- **Victory** (`VictorySpec` VP10/LastStanding) is per faction. The brief keeps victory points optional. A god's persistence ("cannot lose") has no representation today.
- **The pause rule:** submitting a command while paused advances one tick (M1). That conflicts with "pause while considering interventions" only if each considered intervention submits; `Check` does not advance.

---

## 12. Reuse map for the brief's planned capabilities

For each capability: what exists to reuse (type and file), what is missing, and any blocker. Paths are under `Assets/God/Sim/` unless stated.

| Capability (brief §) | Reuse (exists) | Missing | Blocker / contract question |
|---|---|---|---|
| **God identity, Mythic** (§5, §7) | `GodState` partial pattern (null unless started, its own hash part appended at the end: `Core/GodState.Households.cs` as the model); a `SimTuning` block with `IOmitWhenDefault` (`Core/SimTuning.cs`) for max, regeneration, costs, upkeep, cooldowns and the unlimited/disabled modes; `ISimSystem` for regeneration and upkeep; `EventCatalog` per-module catalogue | **Everything**: no god entity, id kind, Mythic balance or god-to-faction link. `Faction` is an enum of 6, not an identity record | A new id kind (`GodId`), a `TelemetryDomain` value and an event code range (800–899 free) must be fixed in contracts first. `GameSetup` needs the human's god (not `Human` faction) |
| **Divine command interface** (§9) | `ICommandGate.Check/Submit`, `CommandHandler<T>`, `CommandRejected/Applied`, the replay logging of external commands, `fact_command` | A `DivineCommand` base (**not** `FactionCommand`) carrying the god id, target (NPC, group, household, organisation, location), action, scope and expiry; handlers; admissibility rules for AI gods | AI gods need an `IGodPolicy` level (a fifth `PolicyLevel`) with its own view; the `PolicyRegistry` and `IntentRules` levels are a fixed enum |
| **Divine suggestion / command** (§8) | The M5 message pipeline: `Comms/Messages.cs` composes, `ThinkScratch.Heard` holds `DecodedMessage`s, the receiver decides with its own policy (M5.15 asserts nothing is decided by the message). The `DecisionRecord`/`OpportunitySlot` term set (`Cognition/Mind.cs`) has an unused `Culture` term and a `Social` column (on `unity/ibs`). `NpcRef` and `MsgAct` features let NPCs learn about a source | A divine "speaker" (no body, not in `People`), a delivery rule (perception or revelation), a score term for authority (suggestion weaker than command), and response events (perceived, considered, followed, refused) | Whether divine messages go through `Heard` (and so through recognition and misreading rules) or through a separate channel. Must be settled once (SHARED_CONTRACTS §7 owns `Heard`) |
| **Compulsion** (§8) | `PopulationSystem.Think`: an intent must match a perceived candidate (`Match(intent)` / `Thinker.Known`), so **physical constraints are already enforced**. The intent runs through the normal `Start`/act code (no second action implementation). `NpcAction` holds the running intent. Learning opens a trace on the chosen candidate | A per-NPC override slot (compelled verb and target, expiry, cancel), applied before or instead of the policy choice; a "compelled" flag on `DecisionMade`; expiry on completion, timeout, death or cancel | `Think` is M3 core code (frozen contracts). The override hook must be one agreed seam, not edits by several packages. The verb set is limited to existing candidates (`Verb` table) |
| **Possession** (§6 Mode C) | The same `Think` seam; `Npc.Policy` is a per-NPC registry name, so a `PossessedNpcPolicy` could be swapped in; the body systems (drives, disease, injury, ageing) keep running; learning from possessed actions happens naturally if the chosen candidate goes through `Learner.OpenTrace` | A command stream from the player to one NPC per think (or per tick for movement), a "divinely controlled" marker on decisions and memories, Mythic upkeep, auto-end on exhaustion or death, a one-possession limit | Policies may not read `GodState` (reflection test), so player input must reach the policy through the observation or an override slot, not by state access. Moving at tick resolution vs the think cadence (`ThinkEvery`) is a design decision |
| **Avatar** (§6 Mode B) | `Npc` movement (`Path`, `NodePather`), `Goods` carry, give and drop, gestures (`Messages`), `Reach` | A body that is not an ordinary person: no hunger or ageing, attackable, returns to the divine state when destroyed, Mythic upkeep. Today every population system iterates `PopulationState.Npcs` | Is the avatar an `Npc` with flags (cheap, but every system must skip its biology) or a new entity kind? A shared-contract decision. Combat with NPCs does not exist (M9) |
| **Miracles: heal, bless, protect, smite, create or destroy resources** (§10) | `Npc.Health`, `Injury`, `Nutrition`, `Drives`; `Disease` and the illness fields on `NpcLife`; `PopulationModule.Kill(DeathCause)`; `Goods.Produce/Consume` with **reserved** `ProduceReason` 4–7 and `ConsumeReason` 5–7 (so creation and destruction can be ledger-exact under invariant C1); `ResourceSource.Stock`; stage 3 `BuildingDestroyed`; `FxLayer` for visuals | Miracle commands and handlers, `DeathCause.Divine` (the enum and `DeathsByCause[9]` and `dim_code` need extending), hut destruction (none exists), a source-destruction rule, "blessed" and "protected" status effects, consecration, area targeting | Extending `ProduceReason`, `ConsumeReason` and `DeathCause` touches frozen enums and the warehouse code tables: one package must own the change |
| **Resurrection** (§11) | `Npc` records persist after death (identity, genealogy via `PedigreeRecord`/`NpcLife`, the traits); `NpcDied` and the estates are history | **The mind is discarded at death** (`PopulationModule.cs:106` sets `npc.Mind = null`), so associations, beliefs, `KinMind` and `SocialMind` are lost. No archive, no revive path (`Alive` is never set back), and no `NpcResurrected` event | **Blocker.** Resurrection as briefed (preserve memories and learned associations) needs a contract change: keep or archive the mind (about 16 KB per NPC at default caps) and build a compact archive so the dead are not processed. Every "alive only" assumption (invariants, estates, households, warehouse grains `fact_person_day`, death reconciliation) must accept a second life interval |
| **Commandments** (§12) | Learning features and conjunctions (`Learning/Features.cs`) can hold contextual rules; `MsgAct` and `GestureAct` have room (acts up to 31); a `Culture` score term exists (0) | Commandment records (structure, scope, revision, revocation), per-NPC known and adopted commandments, transmission, interpretation, a compliance measure | Culture (M11) is not started; commandments need the same "known, believed, adopted" store that M11 would build. One owner for norm and belief storage is needed |
| **Religion and beliefs** (§13) | `BeliefStore` (place facts only: `BeliefKind` Food, Wood, Pile, Hut, Tool), `SocialMind` trust and grudge per person, `Fear` as a drive | Belief in a god, faith, trust in and fear of a god, loyalty, religious affiliation, conversion, worship and prayer acts | Brief §13 forbids one obedience score. A per-NPC "relation to a god" record (like `SocialMind.Relation` but for a god) is the obvious reuse, with a size budget (the per-NPC memory budget is 20 KiB, currently about 16.4 KB) |
| **Divine learning** (§14) | The associative learner rewards drive changes (`Drive.Fear`, `Pain`) and credits recent decisions through traces and episodes, so a smite that causes injury or fear **will** be learned against whatever the NPC did recently, with possible misattribution as the brief wants. `NpcRef` lets it attach to a person. `DecisionMade` and `fact_learning` record causes | Witness observation of a divine act (M7's observation learning is not started), a feature for "divine act seen" | Observational learning (M7/M11) does not exist; witnesses can only perceive gestures today |
| **Organisations** (§18) | `OwnerKind.Group/Enterprise/State` (unused values), `IInstitutionPolicy` and `InstitutionView` (placeholders), `LotBin` (stores), `FactionKnowledgeView` (as the model of a records view) | The whole framework: identity, charter, members, staff, budget, records, service requests | M12 is not started and depends on M6 and M10. Brief §18 asks for it **before** the institutions |
| **Employment** (§17) | `Verb.WorkShift` (14) reserved, `VerbGroup.Employment`, `FeatureKind.Posting`, `TopicKind.Posting` | Jobs, wages, contracts, workplaces | M6 is not started; walkers still do all stage 3 work |
| **Lending and obligations** (§21) | `Goods.Move` with `OwnerRule.Keep` (a loan keeps ownership: WORLD_ONTOLOGY 6.7), `GestureAct.Promise`, `MsgTime.Later`, the deal memory in `SocialMind`, `DealTrace` | Obligation records, repayment, default, collateral, credibility | M7 is not started. Claims without liabilities are forbidden (§21), so the ledger treatment must be designed first |
| **Healthcare** (§19) | `Disease.cs` (infection routes, severity), `InjuryTaken`, `IllnessStarted/Ended`, the M4 `Tend` verb (38) and care events (`CareGiven`); M4.18: tended illness is measurably less severe | Medical knowledge, healers as a role, treatment requests, clinics, resource use | Informal care exists; organised healthcare needs the organisation framework |
| **Education** (§20) | The associative learner (individual learning), M4 child growth (`NpcLife.Growth`) | Teaching, demonstration, imitation (`AssociationsImitated` is designed only) | M7/M11 observation and imitation are not started |
| **Justice** (§22) | `Permits.Believed` (a belief-side permission), `GestureAct.Accuse/Apologise/Forgive`, `BetrayalRecorded`, grudges | Theft classification (M8), witnesses, complaints, evidence, courts, detention | M8 is not started. `Permits` is never enforced (by design) |
| **Military NPCs** (§23) | `IMilitaryPolicy` and `SoldierObservation` (for `MilUnit`s), `NpcVerb` combat subset 20–30 (including Desert, Defect, Surrender), `Stage3MilitaryPolicy`, duels and group battles | NPC soldiers (recruitment, pay, kit, morale) | M9 is not started; soldiers are spawned `MilUnit`s, not people (temporary mechanism) |
| **Citizenship** (§13, M10) | `Npc.Faction`, `NpcImmigrated/Emigrated` events, `fact_migration`, the `Immigration` rule | Citizenship separate from faction membership, loyalty, migration decisions | M10 is not started. `Npc.Faction` doubles as citizenship today |
| **Camera modes** (§26) | `GodCameraRig` (zoom to cursor, pan, island clamp), `WatchCamera` (fly to), person pick in `NpcLayer`, the People card | Rotation (the tilt is fixed), selected-NPC follow, third-person cameras for avatar and possession, area, household and institution selection, smooth mode switching | View-only work; no sim blocker. The view must keep reading through a knowledge view (the M1 gate covers only the HUD) |
| **Divine UI** (§30) | `GodHud` IMGUI partials and `HudStyle`, the speed controls (1/2/4× play, up to 16× watch; F3, F4, F5; Space pauses), toasts (`ToastFeed`), the true-state overlay (F8) | Divine toolbar, radial menu, Mythic meter, commandment editor, possession and avatar controls, 5×/10×, alert aggregation | The build bar and the faction panels must be removed or hidden in god mode (§11). The HUD reflection test (`HudViewOnlyTests`) constrains what a divine HUD may read |
| **NPC inspection** (§31) | People card: drives, carry, intent and "why" breakdown, options, experiences, habits, beliefs; household tab and family view; relations tab with learned values per person (`Information/PopulationKnowledge.cs`, `PeopleCards.*`) | Religion, commandments, divine commands, employment, economic relations, memorial or history view for the dead, naming, favourites | It reads the faction's view; a god's view must be added |
| **Terrain presentation** (§25) | `IslandView` / `IslandField` / `IslandGroundMesh` / `IslandDressing` (P4a) already hide the 19 hexes by default: one continuous ground, wandering borders, irregular coast, scattered props; the legacy hex look only with `-godhexlook` | Curving paths, recognisable settlements (NPC huts are drawn by `NpcLayer` with `ModelKit.Hut`, but there is no settlement-level look), developer overlays for hexes and nodes as an explicit toggle | `IslandView` reads `GodState.Territory` directly (a temporary mechanism) |
| **Gesture animation** (§28) | `GestureLayer` driven only by `GestureMade` events (decides nothing); `LifeStageLayer` (posture by stage, begging bob); `PeopleModels` (`ToolKind` in hand); `GestureAct` composition | **M5.18** (text and lines as a developer overlay) and **M5.19** (the non-verbal vocabulary) are not started. The brief's vocabulary also needs acts that do not exist: attention, pointing, giving, receiving, threaten, gratitude (only `Praise`), begging (a household display), praying, teaching, fear, submission | Current normal play draws **written text above heads**, contrary to owner D4 §8 and the brief's §28 |
| **Warehouse** (§33, §34) | SQLite schema v6, `meta_table_grain`, per-module `Tables.*.cs` and `*Mappings.cs`, exact reconciliation tests, recording-neutrality tests, CSV and Parquet export, `fact_command` | Every divine, religious, organisational, employment, lending, health, education, justice and military table | Migration numbers are serial (0007 onwards): parallel packages must not each add "0007". `unity/demog` adds none; `unity/ibs` changes `PopulationMapping` |
| **KPIs** (§35) | `dim_kpi`/`fact_kpi` tables (empty), `KPI_CATALOGUE.md` (designed), `NpcDay` person-day rows, `demog report` (on `unity/demog` only) | A KPI engine, definitions in code, reports | M13b is not started |
| **Replay and command stream** (§46) | `.godr` replay of external commands with hash checkpoints (`Recording/Replay.cs`), `verify`, `--against-unity`; pending commands in the hash; save v4 including pending | Divine commands only need to be `Command`s submitted externally to be replayed. Possession input per tick would make replays large | A continuous possession input stream needs a compact encoding decision |
| **Scenarios and sandbox modes** (§32) | Scenario JSON (`Recording/Scenario.cs`), `--set` tuning, `VictorySpec.Sandbox`, `-godpopulation`/"Living island", watch mode | Unlimited-Mythic and disabled-divinity switches (tuning), handcrafted god scenarios, timeline branching (save, then load, then diverge works today, untested as a feature) | – |

---

## 13. Blockers and contract decisions to settle before parallel work

1. **Merge the two paused branches first** (I-BS, then demography), or decide explicitly to build on `unity/poc` without them. They change shared seams that every divine package will touch:
   - `Thinker.Known` person targets;
   - the bin Sense seam;
   - the `SelfView` gains;
   - the `Npc.Traits` layout;
   - the household verbs and events 148–150.
2. **One owner for the decision seam in `PopulationSystem.Think`.** Compulsion, possession, divine suggestions and commandments all want to influence it. If each package edits `Think` and `DecisionRecord`, they will collide.
3. **Resurrection needs mind retention.** Today `Kill` discards the mind. Decide: archive the mind at death (memory and save size cost), or accept a resurrection that restores identity but not learned state (which contradicts §11).
4. **Identity kinds for gods and organisations**, and new `TelemetryDomain` values and event code ranges (800–899 is free; institutions already own 500–599). The warehouse migration numbering must be serialised.
5. **A divine command base that is not a `FactionCommand`**, plus a policy level for AI gods. The `PolicyLevel` enum, the `IntentRules` and the reflection tests change with it.
6. **The human as a god:** remove or hide the 18 faction command paths from the human's HUD in god mode, and make every faction an AI seat. The stage 3 "Play as Amber" mode and the HUD and tutorial PlayMode tests built on it remain until M14 (a temporary mechanism).
7. **A divine observation view** separate from `FactionKnowledgeView`, through `ViewFiller` (the single adapter), so the HUD reflection gate still holds.
8. **The avatar's entity kind** (an `Npc` with flags, or a new kind) before the possession and avatar packages start.
9. **Frozen enums to extend once:** `DeathCause` (and `DeathsByCause[9]`), `ProduceReason`/`ConsumeReason`, `GestureAct` (for the gesture vocabulary and prayer), `BeliefKind`, `OwnerKind` usage.
10. **The population is not yet viable** (D4). Divine tests that need a stable multigenerational population, such as resurrecting someone generations later, have no stable baseline until M4.37 passes.
11. **Unity compile gap:** `dotnet build` does not compile `Assets/God/Game`. Every package that touches views must run the Unity console check before its Unity tests (the M5 lesson).

---

## 14. Documentation drift found while making this baseline

- **DEV_LOG "Current status"** (`8617eaa`) still says the I-BS and D4 workstreams are "Running in parallel" and "Current checkpoint 4A"; they are paused, and the code is past 4B. The "Instructions for the next session" section is correct.
- **DEV_LOG "Test results" and "Commits and branches"** stop at M13a / M0–M2. The M3–M5 results live only in the per-milestone records (used in §4 above).
- **The "27 Nov 2026" labels** throughout the Stage 4 docs mean 26 Nov (DEV_LOG's own date note).
- **`STAGE_4_HANDOVER.md` §1–§2** still describe the pre-Stage-4 state (last code commit `d7eeb95`, "No Stage 4 code exists").
- **DEV_LOG "Temporary mechanisms"** mixes mechanisms in the code with planned stand-ins that do not exist yet (`IPayPoint`, granary keeper, `StaticCultureProvider`, `FoundCommunal`). The scenario-keys row is stale (§8.1).
- **Both branch records say "not pushed"**, but `origin/unity/ibs` and `origin/unity/demog` exist at the branch heads.
