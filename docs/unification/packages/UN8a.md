# UN8a: Conflict slice (strike, equip, treat, witness-local fear, CF measurement)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN8a** (Tier UA, Wave 2, Checkpoint UA-mech; training stage S8 of UN6t-b) |
| Branch / worktree | `unity/un8a` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un8a`, from `unity/poc` after **UN0** merged |
| Effort | 2.5 agent-hours (planned H8.6–H11.1; merge H11.6 after UN2) |
| Status | Not started. Replaces the D8 UN8 "planned groups, roads and organisations" spec (groups are UN8b, UB) |
| Recommended agent | **Opus** (violence physics, witness rules, identity neutrality). Never Fable |

## 1. Objective
A small, physical, persistent-NPC conflict slice built only from the twelve controls: `USE` empty hand or a strike-capable held object on a reachable person attempts a strike; `USE` protective equipment on self equips it; steps and turns give retreat and pursuit; `GESTURE` can signal threat, submission or protest (meaning by learning only); `USE` herbs on an injured person treats. Effects are abstract mechanics (reach, work time, fatigue, injury, pain, fear, condition, mortality). No faction military, no forced retaliation, no theft offence, and the same rules within and across tribes. Plus the CF-02 measurement harness (patterns measured, never scripted).

## 2. User-facing outcome
A person can attack a neighbour of the same or another tribe, who is hurt, may flee, fight back, submit, or be treated by a third person; others who actually see it become afraid; the victim remembers the attacker only if it saw them. The god can watch or possess either side. Nobody is punished, judged or scored by the engine.

## 3. Design anchors
- Amendment §5 (the whole slice; keep impact, victim and observer distinct; unseen actions must not reveal the perpetrator; Hawk, Dove, Retaliator, Bully, reciprocal cooperator and cheat are measured patterns; exact game-theory lab stays separate), §7 CF-01, CF-02, EP-04.
- §D9.1 b item 6, §D9.5 rows G4 (weapons: empty hand attack 1, stone axe or stone hammer attack 2; equip effect generic, tested with a test-catalogue row), G5 (herbs: `USE` herbs on injured gives `Bodies.Heal(Injury)`; patient may walk away), H (weapons and armour Later), D9.7 (aggression allowed and emergent).
- §23.4 (`EffectKind` 16 `Strike`, 17 `Equip`, 18 `Treat`; events), §23.10 (strike and injury events, `Duel.Resolve` on `SoldierStats` from the body, keyed `Rng`), §23.12 (1300–1319), §23.5 (`Interrupt`, `Resisted`), R81.
- Audit §6 (military reuse for CF-01), §8 "Conflict".

## 4. Relevant existing source files (verified)
- Duel maths (reuse): `Military/Duel.cs` (`Resolve(MilitaryTuning, in SoldierStats, in SoldierStats, Rng, bool close)` at `:73`; `AttackResult`/`AttackOutcome` at `:17`; `Kit` overload `:86`), `Military/MilitaryTuning.cs` (`SoldierStats` at `:50`: weapon, health, speed, block, armour, dodge), `Military/DuelSim.cs`, `Military/MilitaryRules.cs`. Stage 3 `MilitarySystem`/`GroupBattle` are **not** used.
- Bodies and biology: `Population/Bodies.cs` (`Damage(GodState, EventBuffer, Npc, int injuryMicro, DamageCause, int actor, int intervention)` at `:36`, `Heal(…, HealKind, …)` at `:52`, `DamageCause.Violence`, `HealKind.Injury`), `Population/PopulationModule.cs` (`Kill`, carry spill), `Population/PopulationState.cs` (`Npc.Health`, `Injury`, `Nutrition`).
- Drives (pain, fear): `Learning/Features.cs` (`Drive.Fear = 3`, `Drive.Pain = 4`; the unified need channels Safety and Pain map here), `Learning/Learner.cs` (`DriveReward`: pain and fear change are experienced, not rewarded for outcomes of disputes).
- Witness pattern to copy (not call): `Civ/Ownership.cs` `Witness` (`:162`) is the legacy loop template for local witnessing; unified witnessing uses `ISpatialIndex` and perception.
- Healing legacy (not used in unified mode): `Orgs/Health.cs` (`Healthcare`), treatment becomes `USE` herbs.
- Lab vocabulary (measures only, kept separate): `Lab/Experiments/L01HawkDove.cs`, `L02Retaliator.cs`, `L03Attrition.cs`, `L05PeckingOrder.cs`, `L07SuckerCheatGrudger.cs`, `Lab/Contests.cs`, `ContestStudy.cs`.
- Hostility used by legacy: `Cognition/Thinker.cs:354` (`SituationKind.EnemyInSight`): legacy only; identity-neutral in unified mode (the brain sees "recent aggression seen", not "enemy").
- Tests to model on: `Assets/God/Tests/EditMode/DuelTests.cs`, `HawkBullyStudyTests.cs`, `FirstBlowStudyTests.cs`, `F1BodiesTests.cs`.
- UN0/UN2 (read): `EffectKind`, `Motor.Interrupt`, `ControlOrigin`, `Unified/UnifiedState.cs`.

## 5. Allowed scope
1. **`Strike` effect** (`EffectHandlers.UN8a.cs`): `USE` (empty hand, or a held lot whose `ObjectDef.StrikeAttack > 0`) on a reachable person, slot *strike*; duration and fatigue cost from data; `SoldierStats` built from the body (strength trait, fatigue, injury, held attack: empty hand 1, stone axe or hammer 2, equipped defence); outcome by `Duel.Resolve` with a **new `Rng` seeded by a fixed hash of (game seed, "strike", tick, actor, target)** (an `Rng` helper UN8a adds; no shared stream is advanced, so order cannot change results); damage through `Bodies.Damage(s, ev, victim, micro, DamageCause.Violence, actor, 0)`; death through the existing path (carry spills where the person stood, nothing else). No guaranteed hit, no forced retaliation.
2. **Consequences:** `Motor.Interrupt` of the victim's action (pain, collapse); pain through the existing drive; **fear** raised in witnesses who actually perceive the strike (HV event `FearFromViolence` 1303; witness set from the spatial index and line of sight/day-night sight as perception already defines); the victim's memory records the **actor only if the actor was perceived** (an unseen strike gives the victim the injury and no name); observers' records are separate from the victim's and from the actor's (impact, victim, observer identity stay distinct).
3. **`Equip` effect:** `USE` protective equipment on self (generic: defence value from data) moves the lot to an equipped slot (`Equipped` 1223, `Unequipped` 1224 are UN2's events; UN8a supplies the effect and defence use); mechanically tested with a test-catalogue row because UA has no armour content.
4. **`Treat` effect:** `USE` herbs (carried lot) on an injured person (or self), slot *treat*: `Bodies.Heal(Injury)`; the patient may move away (interaction interrupted); herbs consumed (`ConsumeReason.ProcessInput`); `RemedyApplied` 1304.
5. **Retreat and pursuit:** nothing new; they are steps and turns (UN2). A person in pain may be slower only through the existing body model; no flee verb.
6. **Gestures:** `Threaten 26`, `Surrender 11` (submission), `Accuse 13` (protest) are emitted by UN2's `GESTURE`; UN8a adds only the perception record (what was seen and heard, by whom) and no engine meaning.
7. **Identity-neutral rules:** same tribe, other tribe, kin and strangers are handled by the same code; tribe never changes reach, damage or visibility; no `Faction` or `Tribe` read in `Unified/Conflict` (static test).
8. **CF-02 measurement harness** (`Unified/Conflict/Measure/*`, test and runner side): contested-resource encounter scenarios (two or more persons, one or two scarce goods, held-out seeds and layouts) with recorders that classify, from the event stream only, **aggression** (strike initiated), **avoidance** (retreat or no approach), **retaliation** and **non-retaliation** (response within a window after being struck), **response to betrayal** (after a non-return in exchange), and report rates per arm; nothing is scripted and no pattern is required; the Hawk/Dove, Retaliator, Bully and cheat names appear only as labels on measured patterns in reports.
9. Events 1300–1304, mapper stub `UN8aMappings.cs`.

## 6. Explicitly excluded scope
Faction or group military, spawned abstract soldiers, `MilUnit`, Stage 3 `GroupBattle` and VP; mandatory aggression, automatic punishment, retaliation reflexes, instant omniscient reputation, global moral score; crime, theft or guilt events, `TheftCommitted`, `Ownership.Classify`; courts, policing, law (possible later NPC behaviour, never required); weapons and armour content beyond the UA objects (Later/UB: stone-point spear, soft armour); ranged combat, siege; group raids and defence as group behaviour (UN8b); a new primitive verb; any identity-based modifier; strategy labels as policy classes.

## 7. Dependencies
- **Hard (merged first):** UN0.
- **Merge-after:** UN2 (executor, `Interrupt`, `USE` resolver).
- **Downstream:** UN6t-b stage S8 (needs UN8a merged: H11.6), UN9 (shares the merge slot), UN11a (CF-01, EP-04, QA-02), UN8b, UN10a (strike animation).

## 8. Shared contracts
- **R:** §23.5, §23.10, `Bodies`, `Duel`, `Messages` (perception only), `Goods` (herb consumption), `ISpatialIndex`.
- **M (own partials only):** `UnifiedState.UN8a` (conflict records: recent strikes for memory windows, witness records bounded), `EffectHandlers.UN8a.cs`, events 1300–1319, `UN8aMappings.cs`. **Never** edit `Duel.cs` or `Bodies.cs` (call them); a needed seam is an amendment request.

## 9. Files and modules owned
`Unified/Conflict/*`, `Unified/Interactions/EffectHandlers.UN8a.cs`, `Unified/UnifiedState.UN8a.cs`, `Unified/Conflict/Events.UN8a.cs`, `DotNet/God.Runner/Warehouse/UN8aMappings.cs` (stub), tests `DotNet/God.Runner.Tests/UN8a*.cs`, `CF01*.cs`, `CF02*.cs`, EditMode tests `Assets/God/Tests/EditMode/Unified/UN8a*.cs`, scenarios `unif_conflict*.scenario.json`.

## 10. Required domain events
`StrikeAttempted` 1300 (actor, target, held ware, origin), `StrikeResolved` 1301 (result, damage), `StrikeWitnessed` 1302 (Full: witness, actor if perceived else 0, victim), `FearFromViolence` 1303 (HV), `RemedyApplied` 1304. No crime, theft, guilt or reputation event exists.

## 11. Save/load implications
Saved and hashed: strikes in progress (progress ticks), per-person fear carry-over is the existing drive state, recent-strike memory windows, equipped slots (UN2's). Test `UN8a_SaveLoadParity` (U-P6): save during a strike in progress with a witness and an equipped item, load, continue = straight run. The save version is the lead's.

## 12. Warehouse implications
Mapper stub for 1300–1304; UN11a's `0045_unification_social.sql` stores strikes as **events**, who perceived them as **observations**, and beliefs separately; no fact table named theft, crime or guilt in unified mode (QA-02).

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN8a-T1 | **CF-01** `Persistent_Npc_Can_Attack`: a persistent person strikes a same-tribe and an other-tribe person by `USE`; `StrikeAttempted`/`Resolved`; pain and injury applied by `Bodies.Damage`; a lethal sequence ends in death with the carry spilled where the person stood; the victim can retreat by steps and the strike interrupts only if resolved before it moves | CF-01 | exact |
| UN8a-T2 | `No_Faction_Command`: no strike originates from a faction object or system; origin is Policy, Possessed, Compelled, Avatar or Fixture; no forced retaliation (a struck person with no policy action does nothing) | CF-01, D9.7 | exact |
| UN8a-T3 | `Identity_Neutral`: same positions, stats and seeds with tribe labels swapped give identical outcomes; static: no tribe or faction read in `Unified/Conflict` | CF-01, amendment §5 | exact |
| UN8a-T4 | **EP-04 (conflict part)** `Unseen_Strike_NoActor`: a strike from outside the victim's perception: the victim is injured and remembers no actor; a witness who saw it records the actor; victim, actor and observer records stay distinct | EP-04 | exact |
| UN8a-T5 | `Fear_Witness_Local`: fear rises only for witnesses who perceive the act (distance, sight); a person out of sight is unaffected; the spatial index is used (no all-pairs) | CF-01 | exact |
| UN8a-T6 | `Equip_Generic`: `USE` of a test armour row on self equips it and reduces damage by the data value; unequip returns the lot; no armour content in UA | CF-01 | exact |
| UN8a-T7 | `Treat_Herbs`: `USE` herbs on an injured person heals by the table via `Bodies.Heal(Injury)`; herbs consumed; the patient walking away interrupts; no healer role required | CF-01, NEED-1 | exact |
| UN8a-T8 | `Strike_OrderIndependent`: the keyed strike `Rng` makes a batch of simultaneous strikes give identical results under any actor order; `Strike_NoGuarantee` (hit chance below 100 % with the table, misses occur over 1,000 trials) | determinism | exact |
| UN8a-T9 | **CF-02 harness** `Contested_Resource_Measured`: on held-out seeds and layouts the recorders output aggression, avoidance, retaliation, non-retaliation and betrayal-response rates per arm (untrained, frozen, trained when available); the harness asserts nothing about their values; a scripted fixture arm is labelled mechanical | CF-02 | harness runs; values reported |
| UN8a-T10 | `NoTheft_NoCrime`: a `TAKE` of another's carried goods during a strike emits no theft or crime event and changes no title; U-P3 static clean; `UN8a_SaveLoadParity`, `UN8a_EventsRoundTrip` | QA-02, U-P3, U-P6 | exact |
| UN8a-P | pins: U-P1, P1, P4, P8, Stage 3 hashes (Duel and Stage 3 military unchanged with the switch off) | all | exact |

D8 ids: none map directly (the D8 plan had no conflict slice); CF-01 and CF-02 are new in D9.

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN8a|FullyQualifiedName~CF01|FullyQualifiedName~CF02|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_conflict --seed 1 --days 2 --record full --sqlite --out <scratch>\un8a
```
Report: strike counts, injuries, deaths, witness counts, fear events, and the CF-02 harness table for the fixture arm (labelled mechanical).

## 15. Unity demonstration
Compile check only: `AgentScripts/check-game-compile.ps1` 0 errors (events and types `God.Game` reads). UN10a animates strikes; the lead's checkpoint shows a possessed strike.

## 16. Performance considerations (D10)
Witness finding through `ISpatialIndex` (cost O(persons near the strike)); only active strikes are ticked; per-strike `Rng` creation is a hash, no allocation; recorders for CF-02 run in tests and reports, never in the sim hot path.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un8a -b unity/un8a <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un8a\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un8a record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: `DuelTests`, `HawkBullyStudyTests`, `FirstBlowStudyTests` and the Stage 3 hashes unchanged with the switch off; CF-01 re-run by the lead (index §6 merge 7).

## 18. Risks
- Damage balance makes combat trivially fatal or harmless: numbers are tuning defaults (L), configurable, recorded; CF-02 reports whatever emerges.
- Fear or injury leaking identity (a tribe-based bias): the swap test and static check.
- Witness logic revealing the actor to the victim: EP-04 test; review every event field.
- Legacy hostility (`EnemyInSight`) or Stage 3 military imports: static test over `Unified/Conflict`.
- The measurement harness drifting into scripting expected strategies: it only classifies recorded events; no policy reads it.
- Shared `Rng` stream order dependence: the keyed hash and the order-independence test.

## 19. Definition of done
UN8a-T1…T10 and UN8a-P green under `dotnet test`; CF-01 shown in a fixture trace; the CF-02 harness runs on a fixture arm and is ready for UN6t-b; compile check 0 errors; "UN8a record" in DEV_LOG lists what ran, defaults taken (strike duration, damage, fatigue, fear magnitudes), and what did not; handed back.

## 20. Smaller checkpoints
- **UN8a-1** (≈ 1.5 h): `Strike`, consequences, identity neutrality, T1–T5, T8, T10 (enough for S8 training).
- **UN8a-2** (≈ 1 h): `Equip`, `Treat`, CF-02 harness, T6, T7, T9. Cut plan (index §8 H9.5): `Equip` dropped and recorded.

## 21. Recommended agent
**Opus**. Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN8a record" under "Design decisions taken during implementation".
