# UN6: Individual multi-step planning (bounded, interruptible goals on the learner)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.13, D8.14; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) §6, §9 (mind hooks read only layers 2, 3, 6), §14; [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §10, §11; [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §10 (what exists, what is missing); [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN6** (Tier A, Wave 2, Checkpoint UA; the critical path of UA) |
| Branch / worktree | `unity/un6` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un6`, from `unity/poc` after **UN0** and **UN3a** merged |
| Effort | 6 agent-hours (UN6a plan model, expansion and the Shelter and Capital goals 3 h; UN6b Upgrade, Stock, trade and ask steps, interruption and learning 3 h) |
| Status | Not started |
| Recommended agent | **Opus** (cognition architecture on the learner; determinism and boundedness). Never Fable |

## 1. Objective
Individuals pursue **bounded, interruptible multi-step goals** derived from their own knowledge, beliefs and drives, alongside (never replacing) the associative learner. A planner proposes the next concrete act as an ordinary candidate with a small bounded Plan term; it never executes anything; every act is an existing verb with its existing sim checks. No faction strategy, no script of fixed instructions, no hidden global knowledge.

## 2. User-facing outcome
An uncomfortable person decides to build a shelter, finds a site they remember, builds it, interrupts to eat, returns, finishes and sleeps there; a person who has seen wood and holds the know-how decides to build a woodcutter's shelter, later to upgrade it and goes to get the wood. The inspection panel shows the current goal and step (UN10).

## 3. Relevant design documents
Design §10 (aim, data model, goal sources, expansion, interruption and abandonment, learning from outcomes, worked examples 1 and 2, invariants Plan-1…Plan-6), §11.1–11.2 (no professions; emergence path; measurements), §14.1 U8; owner brief §7 and §8; audit §10 (existing cognition and the eight gaps); `docs/stage4/COMMUNICATION_AND_COGNITION_DESIGN.md`, `docs/stage4/NPC_LEARNING_DESIGN.md`.

## 4. Relevant existing source files (verified)
- Cognition core: `Cognition/Thinker.cs` (`Think` pipeline: `UpdateBeliefs`, `Attend`, `Recognise`, `BuildBase`, `Learner.Arm`, `FillHeard`, `FillDivine`, `BuildCandidates` ~421; `MaxCand`, `ThinkScratch`; `SelfView.CanFoundHere` ~451), `Cognition/Mind.cs` (`NpcMind`, `BeliefKind`, `BeliefStore`, `OpportunitySlot`, `DecisionRecord` ~194–206, "hashed only when present" slots ~252–262), `Cognition/Extensions.cs` (`AddCandidates`), `Cognition/Affordances.cs`, `Cognition/People.cs`, `Policy/AssociativeNpcPolicy.cs` (scoring ~52, `SwitchCost 60`), `Policy/PolicyTypes.cs`.
- Learning: `Learning/Learner.cs`, `Learning/Verbs.cs`, `Learning/EpisodeKey.cs`, `Learning/Features.cs`, `Learning/Instincts.cs`, `Learning/Imitation.cs`.
- Existing single-step chains: `Civ/CivMindHooks.cs` (C1 build, work, haul, take-out candidates ~47–185), `Civ/Work.cs`, `Civ/Employment.cs`, `Social/SocialSim.cs`, `Social/SocialVerbs.cs`, `Social/Valuation.cs`.
- UN0 seams you fill: `NpcMind.Plan` shell, `NpcMind.Unif.Known`, `ObjClass.Goal` 135, `SelfFeature` 88–91, `Verb.AdoptGoal` 129, `SituationKind` 152–154, episode-key kinds 48–51, `UnificationMindHooks.UN6.cs`, `UnificationTuning.UN6.cs`, `UnificationEvents.UN6.cs`, `UN6Mappings.cs`.
- UN3a: `BuildingTable` rows and levels, `Collection`, `Construction`, `ShelterIndex`; UN2 `RawNode` perception; UN4a knowledge bits.
- Tests to model on: the M3 policy tests that assert "no truth in the policy" (static analyser style), `Assets/God/Tests/EditMode/Civ/C1*.cs`, `Support/UnifiedFixtures.cs`.

## 5. Allowed scope
**UN6a:**
1. **Data model** (design §10.2), fixed-size, in `NpcMind.Plan` (hashed only when present; null unless `Planning.Enabled`): `Plan { Count ≤ MaxGoals (2), Goal[] }`, `Goal { Kind, Target, Param, Step[] ≤ MaxSteps (6), Cursor, AdoptedTick, Patience (8), Replans ≤ 3, Weight }`, `Step { Kind (Obtain, GoTo, Collect, Haul, Build, Work, Trade, Ask, Join, Use), Ware, Count, TargetId, Node, State }`. The mind's hash size is constant (Plan-2).
2. **Goal sources** (design §10.3) as a **table of goal rules** (`GoalRules`, data plus small functions, one partial per package so other packages register theirs: `GoalRules.UN6.cs` here, `UN5`/`UN7`/`UN8` add `Road`, `Settlement`, `Craft`, `Sell`, `Group`): `Shelter` (trigger `SelfFeature.Unsheltered` from slept outdoors or poor quality, knowledge of a shelter row), `Capital` (perceived or remembered raw nodes of a kind, no collection right on a building of that kind, learned "stored goods are valuable" or a neighbour with a stock, knowledge of the row). Goal candidates are ordinary candidates of class `ObjClass.Goal` encoded by episode-key kind 48 so the learner learns which goals have paid off for this individual and they compete in the same softmax as eating, sleeping, foraging, fleeing.
3. **Expansion** (design §10.4): deterministic backward chain over data, bounded by `MaxSteps` and `MaxExpandNodes` (24): goal → required capability or ware → options **by this NPC's own believed cost in ticks** (never a global optimum) → site from the NPC's own place memory; if no option is known an explore step with a bounded budget (existing `Verb.Explore`); longer chains store only the next sub-goal and re-expand on arrival. **Reads only the NPC's `Beliefs`, place memory, `SocialMind`, own carry and bins it may use; never `GodState` truth (Plan-6) and never `Faction` (Plan-5).**
4. **The step is a candidate**: at most `PlanCandidates` (2) concrete candidates per decision (MoveTo, Collect with named building and node, Haul to a named bin, Build on a named site, `HandOver`/`Say(Offer)` to a named NPC) with `OpportunitySlot.Plan = Goal.Weight`, bounded by `PlanTermMax` (400 permille of the strongest drive) so a crisis always outranks a plan (Plan-3). Scored by the learner like any other.
5. **Interruption, replanning, abandonment** (design §10.5): survival interruptions are free (a survival verb winning does not cost Patience); failed step preconditions mark the step `Failed` and replan (≤ 3) skipping the failed option; a cheaper option found at a step boundary is taken; danger pauses; Patience, Replans or `AbandonDays` (6) exhausted removes the goal and records a **negative episode** for its key; completion records the outcome reward as a goal-level episode (key kind 48; deferred like `Verb.Build`'s). Events `GoalAdopted`, `GoalStepAdvanced` (HV), `GoalReplanned`, `GoalAbandoned` (reason), `GoalCompleted`.
6. **Supersedes UN3's single-step candidates** (`FoundShelter`, `Collect` at a remembered node) for NPCs with `Planning.Enabled`: the UN3 candidates stay when planning is off (U-P4: `Planning.Enabled = 0` inside a unified run reproduces the same run with plan terms zero).

**UN6b:**
7. Goals `Upgrade` (an owned building at level L; a want for storage, beds or speed; knowledge of L+1) and `Stock` (a build cost or recipe input is short), with `Obtain` expansion options in the NPC's believed-cost order: (a) goods held or stored, (b) a known place where the ware is collectable by a building it may use (`Collect` + `Haul`), (c) a known NPC with surplus who would trade (`Trade` via M5 `Offer`/`HandOver`, from the NPC's own social memory and the goods it holds to give), (d) a gift request (`Ask`), (e) `Join` a group (registered by UN8), else explore.
8. **Learning from outcomes** (design §10.6): per-act reward as always; goal-level episodes keyed (goal kind, target kind); imitation and M5 transmission already move associations (no new system); an NPC who has seen a neighbour sleep better is more likely to adopt the goal (assert through the existing `Imitation` path, not a new channel).
9. **Valuation and instinct priors for the exchange path**: ensure the M5 `Valuation` knows the raw wares so a `Trade` step is possible (rows appended through `ValuationPriors`, owned by UN3b; UN6 asserts only that trades occur in the fixture).
10. **Inspection**: `NpcInspection` data (current goal, step, cursor, patience, last replan reason) through `PeopleCards.Unification` (shared file with UN3, own partial).

## 6. Explicitly excluded scope
The facts the plan acts on (buildings, collection, upgrades: UN3; settlements and roads: UN5; recipes and workshops: UN7; groups and invitations: UN8): UN6 registers *rules* for them and the other packages register theirs; a global planner, auctions or any central allocator; changing the learner's maths; hard-coded strategies per activity; faction-level goals (Plan-5); drawing (UN10).

## 7. Dependencies
Hard: **UN0**, **UN3a** (verbs, rows and acts to expand over). Soft: UN2 (raw nodes), UN4a (knowledge bits), UN5, UN7, UN8 (goal rules they register later). Downstream: UN8, UN11.

## 8. Shared contracts
**Read:** the sense seam, `Beliefs`, `SocialMind`, `Goods` (read-only queries), `BuildingTable`, UN0 ids. **Modify (own files only):** `Unification/Plan/*`, UN6 partials, the `PeopleCards` UN6 partial. **Never** edit `Thinker.cs`, `Mind.cs`, `AssociativeNpcPolicy.cs`, `Learner.cs` (UN0 landed every seam; the planner enters only through `UnificationMindHooks.AddCandidates` and the hashed `NpcMind.Plan`); never read truth (reflection and static tests guard it).

## 9. Files and modules owned
`Unification/Plan/PlanState.cs`, `Plan/Goal.cs`, `Plan/Expansion.cs`, `Plan/GoalRules.cs` (+ `GoalRules.UN6.cs`), `Plan/PlanCandidates.cs`, `Plan/PlanLearning.cs`, `Unification/UnificationMindHooks.UN6.cs`, `UnificationTuning.UN6.cs`, `UnificationEvents.UN6.cs`, `Information/PeopleCards.Unification.Plan.cs`, `DotNet/God.Runner/Warehouse/UN6Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_plan.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN6*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN6*.cs`.

## 10. Required domain events
`1270 GoalAdopted { Npc, Kind, Target, Steps }`; `1271 GoalStepAdvanced (HV) { Npc, Goal, Step, Kind }`; `1272 GoalReplanned { Npc, Goal, Reason }`; `1273 GoalAbandoned { Npc, Goal, Reason }`; `1274 GoalCompleted { Npc, Goal, Ticks }`. Each carries `Control` when started under divine control.

## 11. Save/load implications
Saved: `NpcMind.Plan` (goals, steps, cursor, patience, replans, weight), the per-NPC episode keys for goals. Test `UN6_SaveLoadParity`: save mid-plan (a Capital goal with the first step done, another goal paused by hunger) → load → continue equals the straight run at every later checkpoint.

## 12. Warehouse implications
Mapper `UN6Mappings.cs` for 1270–1274; `UN6_EventsRoundTrip`. UN11 defines `goals` and step-event tables.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN6-T1 | `Unification_Off_Reproduces_Base` (U-P1), `Planning_Off_Neutral` (U-P4) | with switches 0 every hash unchanged; `Planning.Enabled = 0` inside a unified fixture run reproduces the same run with plan terms zero |
| UN6-T2 | `Plan_NeverActs` (Plan-1) | every act from a plan-origin candidate passes the same sim checks as a non-plan act; static test: the `Unification/Plan` namespace writes no sim state |
| UN6-T3 | `Plan_Bounded` (Plan-2, Plan-3) | goals ≤ `MaxGoals`, steps ≤ `MaxSteps`, expansions ≤ `MaxExpandNodes`; the Plan term ≤ `PlanTermMax`; a drive in the pain/hunger crisis band always outranks a plan on 20 sampled states |
| UN6-T4 | `Plan_NoTruthReads_NoFactionReads` (Plan-5, Plan-6) | static analysis of reads in the planner assembly path: no `GodState` truth, no `Faction`; mutation test planting a violation fails |
| UN6-T5 | `Plan_ShelterExample` (design U8, brief §7) | on a fixture with a settlement and no free bed, an NPC with `Unsheltered` and the knowledge builds a shelter within 1.5 days with **no command** and sleeps there; the goal events show GoalAdopted → steps → GoalCompleted |
| UN6-T6 | `Plan_FirstBuilding_FromPerception` | an NPC that perceives many wood nodes, holds the knowledge and has no collection right adopts `Capital(wood)`, builds the woodcutter's shelter at a site from its own memory and collects its first load; a control NPC with no knowledge never adopts it |
| UN6-T7 | `Plan_InterruptedByHunger_Resumes` | hunger interrupts a build at tick 200; the NPC forages (free interruption: Patience unchanged), returns and finishes; the step survived |
| UN6-T8 | `Plan_AbandonsAfterPatience_AndRecordsNegativeEpisode`, `Plan_Replans_WhenBinFull` | a goal that cannot progress is abandoned after Patience or `AbandonDays` with a negative episode that lowers its later adoption; a full bin replans to another building or a new site |
| UN6-T9 | `Plan_Upgrade_StockObtain` | an NPC with 12 wood in its store upgrades; with 4 it adopts `Stock(10 Log)`, collects at its own shelter, then upgrades; with a trade partner holding wood it may trade instead (the believed-cost order is respected); no command anywhere |
| UN6-T10 | `Plan_Deterministic` (Plan-4) | expansion order by ids, tie-breaks by `Rng.Fork("plan", npcId, day)`; two runs on seeds 1–3 hash equal; save/load parity |
| UN6-T11 | `Plan_ToldNothingByFaction` | in a unified fixture, flipping every NPC's `Faction` changes no plan; no `Faction` read in the planner (static) |
| UN6-T12 | `Plan_Capital_Adoption_Rate` (Dir, recorded) | over `S20` seeds and 10 days in the 72-person fixture, the report records how many NPCs adopted, completed and abandoned each goal kind (no threshold; a report) |
| UN6-T13 | `UN6_SaveLoadParity`, `UN6_EventsRoundTrip` | as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN6|FullyQualifiedName~Neutrality|FullyQualifiedName~Learning"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_plan --seed 3 --days 10 --record full --sqlite --out <scratch>\un6
```
Print, for three NPCs, the goal timeline: adopt tick, each step, interruptions and why, replans, completion or abandonment, and the learner's association change after completion.

## 15. Unity demonstration
None here (UN10a shows the goal in the inspection panel). Compile check required if `NpcInspection` or `PeopleCards` types read by `God.Game` changed: `check-game-compile.ps1` 0 errors.

## 16. Performance considerations
Planning runs at decision time only for NPCs with a felt trigger; ≤ 2 goals × 6 steps; expansions ≤ 24 evaluations per goal; report decision cost and ticks/s with 72 NPCs and 500 NPCs (`bench_npc_500` with `Planning.Enabled = 1`); candidate quota 4 including UN3's; never trade learning capability for speed (R57).

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un6 -b unity/un6 unity/poc` (after UN0 and UN3a merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un6\Library /E /MT:16` (0–7 = success; source editor closed).
2. Sim and EditMode tests first. Hand back **UN6a** when T1–T8 are green so the lead can run the UA scenarios early.
3. Before hand-back `git merge unity/poc`; conflicts only in your own files.
4. Neutrality pins that must hold: **P1, P4, P8**, U-P1, U-P4, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`, all M3 learning pins.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run.
7. "UN6 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- **The learner may never choose the goals** (the Plan term is bounded by design and adoption is learned): if T6 fails on many seeds the package is not done; do not raise `PlanTermMax` above its contract value or script adoption. Report and tune priors in `UnificationTuning.UN6` only, with the lead informed.
- **Hidden truth leaks** through convenient queries (site selection, "who has stone"): the static analyser test is the guard; use beliefs and place memory only.
- Goal-level episodes interact with the deferred-outcome mechanism (`VerbInfo.Deferred`): reuse, do not duplicate; check the existing `Verb.Build` episode.
- Candidate-quota pressure (4 for all of Unification): UN3's candidates and planner candidates share it.
- Hash growth of `NpcMind`: fixed-size only.

## 19. Definition of done
UN6-T1…T13 green; pins unchanged; compile check 0 errors; "UN6 record" lists the adoption report (T12), what ran and the defaults taken; UN6a handed back early; handed back.

## 20. Smaller checkpoints
- **UN6a** (≈3 h): data model, goal rules and table, expansion, candidates, interruption, Shelter and Capital goals, T1–T8.
- **UN6b** (≈3 h): Upgrade and Stock goals with obtain options, trade and ask steps, learning and inspection, T9–T13.

## 21. Recommended agent
**Opus.**

## 22. Used later by industry (keep the interfaces general)
Later goals (build a factory, supply an order, research) are rows of `GoalRules` registered by their packages; steps and expansion stay generic over `BuildingDef` and `RecipeDef`.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN6 record".
