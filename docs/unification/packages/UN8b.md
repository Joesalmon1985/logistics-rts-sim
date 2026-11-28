# UN8b: Informal groups (proposal, voluntary joining, real contributions, group action as behaviour)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

**Later-stage spec (Tier UB).** Shorter than the UA specs; every owner-brief §40 field is present. Replaces the D8 UN8 groups/roads/organisations content.

| Field | Value |
|---|---|
| Package identifier | **UN8b** (Tier UB; Checkpoint UB) |
| Branch / worktree | `unity/un8b` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un8b`, from `unity/poc` after **UN7 and UB1** merged |
| Effort | 3.0 agent-hours |
| Status | Not started |
| Recommended agent | **Opus** (social mechanics that must stay free of power over goods). Never Fable |

## 1. Objective
Support informal groups as what people do together, on D9 rules: a proposal made by gesture and referents, voluntary joining by each person's own act, real contributions of labour and goods, withdrawal and free-riding, and group raids and defence **as group behaviour** built from individual strikes, retreats and gestures. A group has **no power over goods**, no authority, no store, no automatic organisation and no enforcement.

## 2. User-facing outcome
People propose a joint project (a road, a defence, a raid), others signal agreement or not, some contribute, some free-ride, some leave; the god can see who proposed, who joined and who worked, as the people's own communications and the analytics' reconstruction, never as an authority structure.

## 3. Design anchors
Amendment §3 (groups can make and communicate agreements but have no magical power over goods; communal hall is a shared place), §4 (agreements subjective; later employment, loans, rations and taxation are interpretations of separate transfers and labour; no auto enforcement), §5 (later courts, policing and law are possible NPC behaviours, not required), D9.7 (aggression within and across tribes; group patterns are measured), D10.1 and §D9.6 (conflict at scale emerges from individuals and groups, never faction AI), §D9.3 (`Groups` switch default 0 until UB), §23.9 (claims, `Corr`), §23.12 (events 1360–1379, `TelemetryDomain` 88), index §2 row UN8b, D8 U12 (reworded).

## 4. Relevant existing source files (verified)
`Assets/God/Sim/Comms/Messages.cs`, `Comms/Message.cs` (`GestureAct` `AskJoin` 16, `Cooperate` 7, `Follow` 9, `Defend` 12, `Alarm` 27, `Accept`, `Refuse`, `Threaten`, `Surrender`, `Accuse`, `Teach`, and the new `Claim` 28; there is no `Propose` act, so a proposal is a `Claim` or `AskJoin` gesture with referents), `Social/SocialMemory.cs`, `Social/SocialSystem.cs` (belief and trust feed), `Orgs/Orgs.cs`, `Orgs/OrgMindHooks.C6.cs`, `Orgs/OrgHooks.C6.cs` (legacy organisation founding with fixed templates: **reference for what not to do**; the organisation system is off in unified mode), `Lab/Experiments/L05PeckingOrder.cs`, `L11Hamilton.cs` (measurement vocabulary only). Unified code (not present at spec time): `Unified/Exchange/*` (UN7: gestures with referents, `Corr`, hold-outs, claims), `Unified/Brain/*` (UN6: `Brain.AddClaim`), `Unified/Settlements/*`, `Unified/Roads/*` (UB1), `Unified/Conflict/*` (UN8a), `Unified/Construction/*` (UN4). Tests to model on: `DotNet/God.Runner.Tests/M5NaturalTests.cs`, `M5SocialLab.cs`, UN7's `UN7*`.

## 5. Allowed scope
1. **Proposal by gesture:** an `AskJoin` or `Claim` gesture with referents (project kind and place, person, ware) by a person creates a **group record** in `UnifiedState.Groups` **as a communication record** (proposer, referents, `Corr`, tick, expiry); no system creates one.
2. **Voluntary joining:** each person who perceives the proposal may join by their own `Accept`/`Cooperate` gesture (recorded with `Corr`); membership is the set of persons' own join acts; leaving is a gesture, silence until expiry, or inaction; there is no threshold, charter, office, template or minimum size.
3. **Real contributions:** labour and goods go to sites and people only through UN4 and UN7 primitives; the group record tallies contributions **for analytics and for people's memories**, it holds nothing.
4. **Free-riding and withdrawal** are measured (contributors versus participants), never punished by the engine.
5. **Group raids and defence as behaviour:** several persons choose to approach, strike and retreat; analytics recognise co-located coordinated strikes with a shared `Corr` as a raid or defence; nothing assigns roles, formations or orders.
6. **Observation and reporting:** group-level views (who proposed, joined, contributed) for the god's UI and the report are **derived from the event stream**, labelled as reconstructions, not world facts.
7. Events 1360–1379 (`GroupProposed`, `GroupJoined`, `GroupLeft`, `GroupContribution`, `GroupExpired`, `CoordinatedStrikeObserved` (analytics)), mapper stub.

## 6. Explicitly excluded scope
Any power of a group over goods, access, people or buildings; group stores, charters, offices, taxes, rations as engine mechanisms; automatic organisation founding, recruitment, assigned roles, formations, orders or commanders; faction or tribe AI; punishment of free-riders by the engine; courts, policing, institutional law (possible later NPC behaviours); `Orgs`/C6/C7 code paths (off in unified mode); trained use (UN6t2); views (UN10b).

## 7. Dependencies
Hard: **UN7** (exchange, claims, gestures with referents), **UB1** (settlements and roads as the group projects), transitively UN0, UN2, UN4, UN5, UN6, UN8a. Merge-after: none. Downstream: UN6t2, UN10b, UN11b, UC1.

## 8. Shared contracts
R: §23.9, `Brain.AddClaim` (UN6), UN7 hold-out and gesture APIs, UN4 sites. M: `UnifiedState.UN8b.cs`, `EffectHandlers.UN8b.cs` (only if a gesture effect is needed; otherwise none), events 1360–1379, `UN8bMappings.cs`. Never edit frozen files.

## 9. Files and modules owned
`Unified/Groups/*`, `Unified/UnifiedState.UN8b.cs`, `DotNet/God.Runner/Warehouse/UN8bMappings.cs` (stub; UN11b completes), tests `DotNet/God.Runner.Tests/UN8b*.cs`, scenario `unif_groups.scenario.json`.

## 10. Required domain events
`GroupProposed` 1360, `GroupJoined` 1361, `GroupLeft` 1362, `GroupContribution` 1363 (analytics link to UN4 contributions), `GroupExpired` 1364, `CoordinatedStrikeObserved` 1365 (analytics only). Each with `Origin` where an act is involved.

## 11. Save/load implications
Group records (proposal, join acts, expiry) saved and hashed in `unification`; `UN8b_SaveLoadParity` (U-P6) with a proposal open and two joined. Bounded: expired records pruned; per-group member list bounded (L: 32; larger crowds are several overlapping proposals).

## 12. Warehouse implications
Mapper stub; UN11b's `0047` stores groups as communications/analytics tables separate from events; no membership-as-authority or ownership fact; reconciliation: every contribution row links to a UN4 or UN7 event.

## 13. Acceptance tests
| Id | Test | Maps to | Pass |
|---|---|---|---|
| UN8b-T1 | `Group_Emerges` (D8 U12, reworded): a group record exists only after a person's proposal gesture and has at least two members who each joined by their own gesture; no system creates, joins or founds one (static: no system code constructs a group) | U12 | exact |
| UN8b-T2 | `Group_NoPower`: a group never moves, locks, reserves or releases a good, never blocks access to a building or container, and never changes a person's capacities; physical results identical with every group record deleted | amendment §3 | exact |
| UN8b-T3 | `No_Automatic_Organisation`: no threshold, template or timer turns gestures into an organisation; with no proposals no group appears over a 30-day run | D9.1 | exact |
| UN8b-T4 | `FreeRider_Measured`: a fixture with joiners who never contribute shows contributors < participants in the analytics; nothing punishes them | CF-02 ext. | exact |
| UN8b-T5 | `Raid_Defence_Measured`: coordinated strikes with a shared `Corr` are recognised by analytics; no role or formation exists in state; results reported on held-out seeds (values never asserted) | CF-02 (UB) | harness runs; values reported |
| UN8b-T6 | `Group_Project_Road`: a proposal for a road between two settlements, contributions by several joiners via UN4 and UB1 sites: the road is built by those physical contributions only | U9 (cooperative, reworded) | exact |
| UN8b-T7 | `UN8b_SaveLoadParity`, `UN8b_EventsRoundTrip`, U-P3 static clean | U-P6 | exact |
| UN8b-P | pins U-P1, P1, P4, P8, Stage 3 hashes with the switch off | all | exact |

## 14. Headless demonstration
`dotnet test DotNet -c Release --filter "FullyQualifiedName~UN8b|FullyQualifiedName~Neutrality"`; `run --scenario unif_groups --seed 1 --days 30 --record full --sqlite`; fixture stream labelled mechanical; report of proposals, joins, contributions, free-riding.

## 15. Unity demonstration
Compile check `AgentScripts/check-game-compile.ps1` 0 errors; views are UN10b.

## 16. Performance considerations (D10)
Group records bounded and pruned; join detection uses the perception already computed (spatial index); no all-pairs scan; analytics reconstruction runs offline, not in the tick.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un8b -b unity/un8b <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un8b\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un8b record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.

## 18. Risks
A group becoming a de-facto authority through some hidden hook (T2 deletes all records and compares hashes); coordinated-strike detection mistaken for a command structure (analytics only); unbounded records; automatic founding creeping in as a convenience.

## 19. Definition of done
UN8b-T1…T7 and UN8b-P green; compile check 0 errors; "UN8b record" in DEV_LOG with defaults (expiry, bounds); handed back.

## 20. Smaller checkpoints
**UN8b-1** proposal, join, leave, contribution tally (T1–T4, T7); **UN8b-2** raid/defence analytics and the road project (T5, T6).

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
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN8b record" under "Design decisions taken during implementation".
