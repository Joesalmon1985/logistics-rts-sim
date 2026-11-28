# UN6: Brain runtime (deterministic inference, run-time learning, knowledge, claims)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN6** (Tier UA, Wave 1, Checkpoints UA-mech support and UA-learned) |
| Branch / worktree | `unity/un6` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un6`, from `unity/poc` after **UN0** merged |
| Effort | 3.5 agent-hours (planned H4.6–H8.1; merge H9.5 with UN3b) |
| Status | Not started. Replaces the D8 "UN6 planner" spec entirely (goals, `Plan-1…Plan-6` and the per-NPC goal table are withdrawn: R81, §D9.2 e) |
| Recommended agent | **Opus** (deterministic inference and learning seams). Never Fable |

## 1. Objective
Give every unified person a brain that runs the **frozen shared policy artifact** and nothing hand-written: integer inference over `Obs v1` to a factorised `Act v1` choice of one of the twelve controls with arguments; per-person state (recurrent state, memory, knowledge, skills); a bounded associative overlay that learns from experienced needs; knowledge acquisition by own success, observation and teaching; the labelled brain kinds (Untrained, Trained, Fallback); the `Brain.AddClaim` API for beliefs. It fills the `UnifiedHooks.PersonTick` site UN0 lands and decides **only when a primitive completes, fails or is interrupted**.

## 2. User-facing outcome
In a unified game every person (founder or newborn) decides and acts through the executor without any scripted rule. The inspection panel can show the brain badge data (`Brain.Kind`, artifact version, hash) and, from person state, what the person knows (designs, recipes, interactions), its memory and its beliefs, each as the person's belief, never as truth. With `Brain.Kind = 0` the untrained network produces valid but incompetent behaviour; with `Kind = 2` a visibly labelled fallback runs.

## 3. Design anchors
- §D9.2 a (observation, action, network, budget, decision points), §D9.2 d (inherited versus acquired, associative overlay, knowledge acquisition, TR-02), §D9.2 e (bounded planning: may and may not), §D9.1 d (no reward from title), §D9.5 Start (founder knowledge, children).
- §23.5 (`PrimitiveControl`, `Motor.Submit`), §23.6 (`ObsSchema`, `ActSchema`, `IPrimitivePolicy`, `BrainState`, needs extension), §23.9 (claims are brain records), §23.12 (events 1320–1339), §23.1 (`Brain.*` tuning), §23.14 (brain weights are not in the save; artifact version and hash are).
- R83 (children run the shared network as innate competence; designs, recipes and knowledge are acquired and gate action through legality masks; no copying of adult memory), R81, R84.
- Amendment §6 (child inheritance, matched exposure tests, protect save/load/replay, bounded memory, C# inference parity) and §2 (bounded generic planning may not issue hidden economic actions).

## 4. Relevant existing source files (verified)
- Learning core reused as the **overlay**, not as the decision table: `Learning/AssociationStore.cs` (`AssociationStore`, `Origin` Own/Vicarious/Imitated, `LearningMode`), `Learning/Learner.cs` (`Learner.Accrue` at `:263`, `Learner.DriveReward` at `:55`, traces, TD credit), `Learning/Imitation.cs`, `Learning/Features.cs` (`Drive`, five hashed drives), `Learning/LearningTuning.cs`, `Learning/EpisodeKey.cs`. The legacy verb table (`Learning/Verbs.cs`), instinct tables (`Learning/Instincts.cs`, `InstinctSet.None` at `:24`) and `Policy/AssociativeNpcPolicy.cs` are **not** used for unified decisions.
- Keyed randomness: `Core/Rng.cs` (policy stream per person, temperature from temperament). Temperament and biology: `Population/Genetics/Genome.cs`, `Npc.Traits`.
- Dispatch: `Population/PopulationSystem.cs` (`Body()` then `UnifiedHooks.PersonTick`; the site is landed by UN0, frozen). Perception helpers: `Cognition/Thinker.cs` (sight helpers only), `Comms/CommsRange.cs`.
- Social memory fed by unified perception (read-only for UN6): `Social/SocialMemory.cs`.
- Contracts and schema UN0 lands (read, never edit): `Unified/Schema/ObsSchema.cs`, `ActSchema.cs`, `CatalogueDefs.cs`, `Unified/UnifiedState.cs`, `IPrimitivePolicy`, `ISpatialIndex` (do not exist yet; UN0 creates them).
- UN2 outputs you consume once merged: `Unified/Percept/*` (`Percept.Encode`, `ToVector`), `Unified/Motor/*` (`Motor.Submit`, `Motor.Busy`, `Motor.Legal`, `ActMask`).
- Tests to model on: `DotNet/God.Runner.Tests/LearningHarness.cs`, `LearningScenarioTests.cs`, `DeterminismTests.cs`, `F1NeutralityTests.cs`.

## 5. Allowed scope
1. **Inference** (`Unified/Brain/Inference/*`): the §D9.2 a network in integers: input encoder, 2 dense layers (128), integer GRU-style recurrent state (64 × int16), factorised heads (control, target slot, source slot, interaction slot, design index, gesture token, referents, quantity); int16 weights, int32 accumulators, saturating arithmetic, piecewise-linear activations, integer log-softmax table; sampling with the person's keyed policy `Rng` (temperature from temperament). No floating point anywhere in the run-time path (a test scans the folder for float types). Budget: ≤ 20,000 multiply-accumulates, ≤ 4 KB state per person, zero allocation per decision; cost counters exposed for UNP and UN11a.
2. **Artifact loader** (`Unified/Brain/ArtifactLoader.cs`): reads `FounderPolicyV<n>.g.cs` (written by UN6t-b; UN6 ships a tiny **test artifact** generator for its own tests only), verifies the SHA-256 of the weights, the `Obs`/`Act` schema hash and the catalogue hash against `Unification.Brain`; with `Brain.Kind = 1` a mismatch refuses to start the game (clear error, no silent downgrade).
3. **Policies** implementing `IPrimitivePolicy`: `UntrainedPolicy` (same network, deterministic random weights from a fixed seed; `Kind = 0`), `TrainedPolicy` (`Kind = 1`, only with a verified artifact), `FallbackPolicy` (`Kind = 2`): a minimal, labelled, scripted safety controller built on primitives for runs where no trained artifact passed TR-01; **always reported as Fallback, never counted as trained**, and it must not wrap the legacy `Eat/Flee/Build` rules (it may only choose among legal primitives by a fixed need-pressure ordering, documented in the record). `Brain.Kind` and artifact version/hash are saved with the game and carried by `BrainAttached` (1320).
4. **`BrainState`** (`Unified/Brain/BrainState.cs`, bounded ≤ 4 KB): recurrent state, 8 remembered places (dead-reckoned sector and distance from the person's own steps), 4 remembered people, claims ring of 8, knowledge bitsets over catalogue indices (designs, recipes, interactions), skills (practice levels that shorten durations; ±30 % founder variation set by UN5), overlay store id; stored in `UnifiedState.People`, sorted by `NpcId`, hashed in the `unification` part.
5. **Decision schedule** (`UnifiedHooks.UN6.cs`, fills `PersonTick`): a person decides when its primitive completes, fails or is interrupted (`Motor.Busy` false and not asleep); staggering and level of detail depend on sim state only, never on a camera; a person with no decision due costs O(1).
6. **Associative overlay**: per-person bounded `AssociationStore` keyed on hashed observation features × action class, adding bounded logit biases to the frozen network; credit through `Learner`'s TD and traces from **need-pressure change** (`DriveReward`) and delayed outcomes (a relief that arrives hundreds of ticks after the action); **no reward from a holder, lot, title or transfer** (`PayOwnerChange`, `PayProduce`, `PaySpoiled` pay nothing in unified mode; UN0 lands that; UN6 must not add a goods-based reward).
7. **Knowledge acquisition**: own success (a tried interaction that succeeds becomes known); **observation** (a person who perceives another complete an interaction, recipe or building may learn it, probability from attention, distance and temperament); **teaching** (`GESTURE` `Teach` 18 / `Demonstrate` 21 while performing it raises the observer's chance and adds an imitation bias with `Imitation` origin `Vicarious`/`Imitated`). Events `KnowledgeAcquired` (1321), `DemonstrationObserved` (1322), `TeachingGesture` (1323). Knowledge gates action only through the legality mask (`NotKnown`).
8. **Children's start state** (R83): newborns and the five children per tribe run the same network; knowledge = innate interactions only (eat, drink, sleep, approach, gesture, take, drop), no designs, no recipes; empty memory, empty overlay; genome and temperament inherited; **never a copy of any adult's memory, beliefs, skills or knowledge** (a test compares serialised child state with every adult's).
9. **Claims API** (frozen at UN6 merge): `Brain.AddClaim(s, npc, in Claim)`, `Brain.Claims(npc)`; `Claim { Claimant; SubjectKind; SubjectId; Source (Own, Observed, Told); Confidence; Tick; Expiry }`; bounded ring of 8 with eviction lowest confidence then oldest, ties by id; contradictory claims coexist; claims are never world state and no function exposes a canonical answer (UN7 and UN11a read through this API).
10. **Bounded planning, off by default** (`Brain.Planning = 0`): when switched on, internal search depth ≤ 4, ≤ 64 expansions over the person's own known interactions and remembered or perceived world only, producing an **intention input** (a target slot and a need channel) that the network reads; it never issues a control, never calls `NodePather` or a truth query, holds no hand-written goal; training and evaluation must use the same setting.
11. Fixture policy `ScriptedPolicy` for tests only (`ControlOrigin.Fixture`) lives in `DotNet/God.Runner.Tests/Support/`, not in the game assembly.

## 6. Explicitly excluded scope
Training (UN6t), the executor and observation encoder (UN2), exchange semantics (UN7), conflict (UN8a), groups and cultural institutions (UN8b), any goal table or `Plan-n` structure, any high-level economic intention ("build a shelter", "go to market"), any profession or role, any per-tribe or per-faction knowledge object, reward shaped by goods, buildings completed or winning a dispute, copying adult memory to children, floating point in run-time inference, ML frameworks at game run time, edits to frozen files (§23.0).

## 7. Dependencies
- **Hard (merged first):** UN0 (schema files, `IPrimitivePolicy`, `UnifiedState`, the `PersonTick` site, `Brain.*` tuning).
- **Merge-after:** UN2 (consumes `Percept`, `Motor`, `ActMask`; may start earlier on the frozen interfaces with stubs, merges after UN2).
- **Downstream:** UN6t-b (trains against UN6's inference and state), UN7 (claims API, merge-after UN6), UNP (merge-after UN6), UN9, UN10a (badge), UN11a (UA-learned gate).

## 8. Shared contracts
- **R:** §23.5 and §23.6 types, `Goods` physics (never write), `Bodies`, `Messages.Emit` (through `Motor`), `Learner`, `AssociationStore`, `Imitation`, `Rng`, `SocialMemory` (read).
- **M (own partials only):** `UnifiedState.UN6` (brain state lists), `UnifiedHooks.UN6.cs`, events 1320–1339 (1320 shared with UN6t for artifact fields), the `Unification.Brain` tuning fields declared by UN0 (UN6 reads them). **Never** edit `Goods.cs`, `GodSim`, `SaveFile`, enums, `Divinity/Control.cs`, the schema files.

## 9. Files and modules owned
`Unified/Brain/*` except `Unified/Brain/Artifacts/` (UN6t), `Unified/UnifiedState.UN6.cs`, `Unified/UnifiedHooks.UN6.cs`, `Unified/Brain/Events.UN6.cs`, `DotNet/God.Runner/Warehouse/UN6Mappings.cs` (stub), tests `DotNet/God.Runner.Tests/UN6*.cs`, `Brain_*.cs`, `DotNet/God.Runner.Tests/Support/ScriptedPolicy.cs`, EditMode tests `Assets/God/Tests/EditMode/Unified/UN6*.cs` (new folder: commit its `.meta`).

## 10. Required domain events
`BrainAttached` 1320 (artifact version, hash, kind; shared with UN6t for the artifact fields), `KnowledgeAcquired` 1321 (what: design, recipe or interaction; source: own, observed, taught), `DemonstrationObserved` 1322 (HV), `TeachingGesture` 1323. Each carries `Origin` where an action is involved; codes never reused (§23.12; own partial array and mapper stub).

## 11. Save/load implications
`BrainState` per person is saved and hashed (`unification` part); brain **weights are not saved** (artifact version and hash are; §23.14). The save version is the lead's (v7 at UA-mech). Test `UN6_SaveLoadParity` (U-P6 brain part): save mid-decision (person mid-action with a non-zero recurrent state, a full claims ring, a half-learned overlay), load, continue: equals the uninterrupted run at every later checkpoint.

## 12. Warehouse implications
Mapper stub `UN6Mappings.cs` (the event-owning package writes the stub; UN11a completes it and owns migration `0046_unification_brain.sql`: artifact, knowledge spells, TR results). Brain facts are analytics of beliefs and knowledge; no belief is exported as a world fact (QA-02).

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN6-T1 | `Brain_Inference_Deterministic`: same observation vector and state give the same action on repeat runs; a golden vector from the test artifact is pinned | QA-01, TR-01 parity | exact |
| UN6-T2 | `Brain_Budget`: ≤ 20,000 MAC and ≤ 4 KB per person per decision; 10,000 decisions allocate zero bytes (allocation counter); no floating-point type in the folder | D10, §D9.2 a | exact |
| UN6-T3 | `Artifact_HashRefusal`: a flipped weight, a changed schema hash or a changed catalogue hash refuses to start with `Kind = 1`; `Kind = 0` and `Kind = 2` start labelled; `BrainAttached` carries kind, version, hash | TR-01 (honesty) | refused / labelled |
| UN6-T4 | `Decision_OnCompletionOnly`: a person decides only when its primitive completes, fails or is interrupted; a sleeping or busy person never decides; results identical with and without a view attached | D10 | exact |
| UN6-T5 | `Overlay_NeedCredit_NotGoods`: after eating when hungry the overlay bias for that action class rises; an identical sequence with only a holder or lot change and no need change leaves the bias unchanged; the overlay delta is bounded | D9.1 d, amendment §6 | pass |
| UN6-T6 | `Knowledge_OwnObservedTaught`: an interaction becomes known after own success; after observing it within perception with probability from attention and distance; more often with a teaching gesture; an unknown design is masked (`NotKnown`) | EC-02, TR-02 | pass |
| UN6-T7 | `Child_NoAdultMemory`: a child's `BrainState` equals the innate start state; no memory, belief, skill or knowledge bit is copied from any adult; matched pairs: the exposed child acquires a demonstrated interaction more often over 20 seeds than the unexposed control (rate reported; failure is reported, not hidden) | TR-02, R83 | start state exact; rates reported |
| UN6-T8 | `Brain_NoHiddenOracle` (static): no `NodePather`, `Truth(`, `.Owner`, `OwnerRelation`, `Permits.` reference in `Unified/Brain`; `Planning = 0` default; with planning on it emits only an intention input | D9.1, §D9.2 e | exact |
| UN6-T9 | `Claims_Beliefs`: ring of 8, eviction order, contradictory claims coexist, expiry; no API returns a canonical owner or claim truth | EP-02, QA-02 | exact |
| UN6-T10 | `Policy_OriginNeutral` (U-P4 re-run with the test artifact under `Kind = 1`), `UN6_SaveLoadParity`, `UN6_EventsRoundTrip` | QA-01 | exact |
| UN6-T11 | `Fallback_Labelled`: `Kind = 2` appears in the save, `BrainAttached`, the badge data and a report string; a run with `Kind = 2` can never set a "trained" flag in a report | TR-01 | exact |
| UN6-P | pins: U-P1, P1, P4, P8, Stage 3 hashes unchanged with `Unification.Enabled = 0` | all | exact |

D8 ids: U4 and U8 (autonomous collection, construction and multi-step planning) are learned outcomes measured by UN6t-b (EC-02, TR-01 T3/T4); the D8 planner tests `Plan-1…Plan-6` are superseded (D9 acceptance table).

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN6|FullyQualifiedName~Brain_|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_start --seed 1 --days 5 --set tuning.Unification.Enabled=1 --set tuning.Unification.Brain.Kind=0 --record summary --out <scratch>\un6
```
(Scenario `unif_start` lands with UN5; until then use the UN2 `PrimitiveFixture` island with the untrained policy.) Report: decisions per person-day, MACs per decision, bytes per person, knowledge events.

## 15. Unity demonstration
Compile check only (types `God.Game` reads, such as the brain kind and a `BrainState` read model, may change): `AgentScripts/check-game-compile.ps1` 0 errors. No view work; UN10a draws the badge.

## 16. Performance considerations (D10)
Per-person state ≤ 4 KB (1,200 persons ≈ 4.8 MB); inference ≤ 20,000 MAC and decisions only on action completion (a person doing a 600-tick construction decides once); no allocation per decision; overlay store bounded (≤ 64 entries per person (L)); no all-pairs scan (observation through `ISpatialIndex`); expose per-tick MAC totals so UNP and UN11a benchmark at 72, 600 and 1,200 persons; staggering by sim state (person id and tick), never the camera.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un6 -b unity/un6 <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un6\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un6 record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: pins U-P4 (re-run) and U-P6 (brain part); the lead re-runs `Brain_Budget` and `Artifact_HashRefusal` (index §6 merge 5).

## 18. Risks
- Determinism drift in integer maths across platforms: golden vectors, saturating arithmetic, no `float`/`double`, no `Math` calls.
- Overlay credit accidentally rewarding goods or title: test UN6-T5 with a holder-only change; review of every `Accrue` call.
- The fallback quietly becoming the real brain: labelling tests and a report string; never set "trained" unless the artifact verified.
- State size creep: byte-count test on `BrainState` serialisation; adding a field requires a budget row.
- Children copying adult knowledge by mistake through `Imitation` origins: the start-state equality test and a static check that no adult-to-child copy exists.

## 19. Definition of done
UN6-T1…T11 and UN6-P green under `dotnet test`; U-P4 and U-P6 brain part green; budget numbers recorded; claims API frozen and documented in the record; compile check 0 errors; "UN6 record" in DEV_LOG lists what ran and the defaults taken (overlay sizes, attention probabilities, fallback ordering); handed back.

## 20. Smaller checkpoints
- **UN6a** (≈ 2 h): inference, `BrainState`, artifact loader with hash check, Untrained and labelled Fallback policies, decision schedule, tests T1–T4, T8, T10, T11 (enough for UN6t-b to train against).
- **UN6b** (≈ 1.5 h): overlay credit, knowledge acquisition, children's start state, claims API, tests T5–T7, T9.

## 21. Recommended agent
**Opus** (a numerically exact runtime that training, replay and save all depend on). Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN6 record" under "Design decisions taken during implementation".
