# UN8: Informal groups (propose, join, contribute, withdraw, free-ride; building, road and settlement projects)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.8, D8.14, D8.15; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) §7, §9, §14; [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §12, §7.2, §5.7, §14.1 U9, U12; [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §11 (group-like mechanisms), R14; [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN8** (Tier B, Wave 3, Checkpoint UB) |
| Branch / worktree | `unity/un8` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un8`, from `unity/poc` after **UN3**, **UN4b**, **UN5** and **UN6a** merged |
| Effort | 5 agent-hours (UN8a record, propose/join/contribute/withdraw, building and gather objectives 3 h; UN8b road and settlement objectives, group-owned assets, free-riding and organisation hand-over 2 h) |
| Status | Not started |
| Recommended agent | **Opus** (social decision modelling, collective-action semantics, ownership by a group). Never Fable |

## 1. Objective
Informal groups are a fundamental social entity: any individual may propose a collective objective, invite others, and each decides for themselves whether to join and whether to contribute; contributions are only real work and real goods; members may refuse, withdraw or free-ride; a group may own a building it built; it **never** becomes a formal organisation by threshold, only by an act of its members.

## 2. User-facing outcome
A person asks neighbours to help build a road or a shared store; some turn up, some promise and do nothing, some leave halfway; the road gets built only if enough people actually work. The player can see who proposed, who joined, who contributed and who did not (inspection; UN10b).

## 3. Relevant design documents
Design §12 (group record, propose/invite/join, contribute/withdraw/free-ride, objectives and completion, disputes, relation to C6 organisations, tests), §7.2 (the road loop), §5.7 (joint construction and communal ownership), §6.2 (settlement founding), §9.5 (the founding community group), §14.1 U9, U12; owner brief §9 and §10; audit §11 (C6 founding meetings, M4 care arrangements, `OwnerKind.Group` use, C1 use rights); `docs/stage4/ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md`.

## 4. Relevant existing source files (verified)
- Group-like precedents: `Orgs/OrgState.C6.cs` (`FoundingMeeting` ~281–289), `Orgs/OrgMindHooks.C6.cs` (Propose, Answer, CallMeeting, AskJoin, AcceptJoin gestures ~18), `Orgs/OrgTemplates.cs` (56–76), `Orgs/OrgModule.cs` (`OrgCommands` founding path), `Population/Households/HouseholdState.cs` (`CareArrangement` ~39, ledger Given/Returned), `Population/Households/HouseholdActs.cs` (605–623), `Social/SocialVerbs.cs` (`SayCooperate` 71, `Give` 48, `HandOver` 49), `Social/SocialSim.cs`, `Social/SocialMind` (trust).
- Ownership: `Population/LotBin.cs` (`OwnerKind.Group`, `Owner.Group`), `Estate/Estates.cs` (292), `Civ/Assets.cs` (`UseRight`, `Owner`), `Civ/Ownership.cs`, `Civ/Work.cs`.
- UN0 seams you fill: `Verb` 130 `ProposeGroup`, 131 `JoinGroup`, 132 `ContributeGroup`, 133 `LeaveGroup`, `NpcVerb` 222 `Propose`, 223 `JoinGroup`, 224 `Contribute`, `ObjClass` 134 `GroupSite`, `SelfFeature` 91 `InGroup`, `SituationKind` 154 `GroupInvite`, episode-key kind 51, `UnificationState.UN8.cs`, `UnificationTuning.UN8.cs`, `UnificationHooks.UN8.cs`, `UnificationMindHooks.UN8.cs`, `UnificationEvents.UN8.cs`, `UN8Mappings.cs`, `UnificationSystem.DayStartUN8`.
- UN4b: the `Group` partial record and `Groups.CreateCommunity`; UN5: founding and road projects and `RoadWork`; UN3: `Contributions`, group-owner acceptance, `Terms` shares; UN6: `GoalRules` registry, `Join` and `Obtain` steps.
- Tests to model on: `DotNet/God.Runner.Tests/C6RunTests.cs`, `Assets/God/Tests/EditMode/Civ/C1*.cs`, `Support/UnifiedFixtures.cs`.

## 5. Allowed scope
**UN8a:**
1. **Group record** (extend the `partial class Group` UN4b declared): `Objective { ObjectiveKind (Building, Road, Settlement, Gather, SharedAsset, Defence, Care, Trade), Target (site, project id), Needs[], WorkNeeded, Deadline }`, `Member { Npc, Joined, ContributedWork, contributions (4 kinds), Status (Invited, Active, Withdrawn, Expelled) }`, `GroupState (Forming, Active, Done, Failed, Dissolved)`; ids from `NextGroupId`; `MaxMembers` 24; stored in `UnificationState.Groups`, ascending id.
2. **Propose** (`ProposeGroup` 130): a candidate when a goal's expansion contains a step the NPC cannot do alone or when it believes sharing is cheaper (registers `GoalRules.UN8.cs`: `Group`; extends UN6's `Obtain` option (e) `Join`); creating costs only action time and names the objective; one active proposal per NPC.
3. **Invite and join**: invitations by the existing `Say` gesture (`Offer`/`Cooperate` with `Detail` = group id, reusing M5 comms; no new channel) to NPCs it can speak to; the group site is perceivable (`ObjClass.GroupSite`) so others who see or hear may ask to join. Each invitee evaluates `JoinGroup` (131): expected benefit of the objective to *them* (they would use the road, get a bed), cost (time and goods), trust in the proposer, the number already in, its drives; joining is voluntary and costs nothing by itself.
4. **Contribute, withdraw, free-ride** (design §12.3): only **real** contributions count: work ticks at the site through the same acts as UN3/UN5 (`BuildingWorked`, `RoadWorked`, founding work) with the group recorded as beneficiary, and wares consumed into the project (`Goods.Consume`, reason `Built`). Joining never obliges anyone. `ContributeGroup` (132) is the decision each day to put in work or goods; `LeaveGroup` (133) withdraws at any decision tick; contributions already in stay (`RefundPolicy` data, default `NoRefund`); a free rider is simply a member with no contribution and is **recorded**, never punished here.
5. **Building and gather objectives**: a group building project (UN3 construction with `Contributions` and `Owner.Group(g)` as owner at founding); members' rights by contribution share through `SharePermille` in the `Terms` of their `Use` rights; a **gather-and-haul** objective delivers goods into a group-owned bin (owner `Owner.Group(g)`, rights as above).
6. **Completion**: a done objective emits `GroupObjectiveDone`; the group dissolves after `AfterglowDays` (default) or persists as the owner of its asset; a group asset's later grants follow `GrantRule` (default: any active member may grant `Sleep`/`Gather` rights to a member's household; a grant to an outsider needs a `Majority` of active members' explicit accept acts).

**UN8b:**
7. **Road and settlement objectives** attach to UN5's projects: a group `Road` or `Settlement` objective *binds* the project (UN5 projects stay open to anyone who works; the group adds invitation, membership and the record of who contributed); `ProposeRoad` and `ProposeSettlement` candidates create a group when the NPC believes others would join, otherwise remain solo (UN5 behaviour unchanged when `Groups.Enabled = 0`). Stalls: `GroupFailed` after `RoadStallDays`/`FoundStallDays`.
8. **Free-riding measurement**: per project the count of users who walked the road or used the building without contributing (analytics-only `FreeRiderUse`, from movement and use events) next to contributors; a variant scenario with low `ContributeBase` shows free-riders > 0.
9. **No automatic organisation** (design §12.6): the `FoundOrg` candidate exists only as an NPC act (reusing `OrgCommands`/C6 founding); members accept as they join; on conversion the group's assets transfer by `Property.Transfer` and the group ends with `GroupBecameOrg`; a test with 24 members and a large asset never creates an `Organisation` without an NPC act.
10. **Disputes and defence/care/trade objectives**: disputes use C1's `Disputes` and C9 stubs only; `Defence` is recorded and inert (C4 later); `Care` reuses M4 `Tend`/`Feed` through a group household flag (only the objective and membership, no new care logic); `Trade` is a plan whose steps are GoTo and Trade, proceeds belong to the individuals who gave the goods.
11. **Events:** 1290 `GroupProposed`, 1291 `GroupJoined`, 1292 `GroupContributed` (HV), 1293 `GroupWithdrew`, 1294 `GroupObjectiveDone`, 1295 `GroupFailed`, 1296 `GroupDissolved`, 1297 `GroupBecameOrg`.

## 6. Explicitly excluded scope
Formal organisations, offices, charters, rules, money (C6, K2: the group has none); compulsion to contribute; a group deciding anything by itself (all acts are members' acts); tolls and road ownership; defence mechanics; the planner core (UN6); the geometry and rules of settlements and roads (UN5); views (UN10b); warehouse tables (UN11).

## 7. Dependencies
Hard: **UN0**, **UN3** (contributions, group owner, rights terms), **UN4b** (the `Group` record), **UN5** (projects), **UN6a** (goal rules and `Join`/`Obtain`). Soft: UN7 (shared workshop objectives). Downstream: UN10b, UN11b.

## 8. Shared contracts
**Read:** `Goods`, `Owner.Group`, `Assets` API, M5 comms and `SocialMind`, `OrgCommands` (call only), UN5/UN3/UN6 APIs. **Modify (own files only):** `Unification/Groups/*`, UN8 partials, `GoalRules.UN8.cs`, additive switch-gated hooks in UN3 and UN5 listed in the hand-back (agreed with those packages' records). **Never** edit `Orgs/*`, `Goods.cs`, enum files, `GodSim`, `SaveFile`.

## 9. Files and modules owned
`Unification/Groups/Group.UN8.cs`, `Groups/GroupActs.cs`, `Groups/GroupDecisions.cs`, `Groups/GroupObjectives.cs`, `Groups/FreeRiders.cs`, `Unification/Plan/GoalRules.UN8.cs`, `Unification/UnificationState.UN8.cs`, `UnificationTuning.UN8.cs`, `UnificationHooks.UN8.cs`, `UnificationMindHooks.UN8.cs`, `UnificationEvents.UN8.cs`, `DotNet/God.Runner/Warehouse/UN8Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_groups.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN8*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN8*.cs`.

## 10. Required domain events
```
1290 GroupProposed {Group, Kind, Founder, Objective}   1291 GroupJoined {Group, Npc, Invited}
1292 GroupContributed (HV) {Group, Npc, Work, Ware, Count, Control}   1293 GroupWithdrew {Group, Npc, Contributed}
1294 GroupObjectiveDone {Group, Kind, Target, Contributors, FreeRiders}   1295 GroupFailed {Group, Reason}
1296 GroupDissolved {Group}   1297 GroupBecameOrg {Group, Org}
```
Codes from design §13.6 (1290–1299). Events carry ids only and `Control` when under divine control.

## 11. Save/load implications
Saved: `Group` records (members, contributions, objective, state), `NextGroupId`, per-NPC group episodes. Test `UN8_SaveLoadParity`: save mid-project (a road group with three members, one who joined and has not contributed, one who withdrew) and load; continue equals the straight run.

## 12. Warehouse implications
Mapper `UN8Mappings.cs` for 1290–1297; `UN8_EventsRoundTrip`. UN11 defines `groups`, `group_member_spell`, `group_contribution`.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN8-T1 | `Unification_Off_Reproduces_Base` (U-P1), `Groups_Off_Neutral` | with `Groups.Enabled = 0` UN5's solo road and settlement behaviour is byte-identical to UN5's own tests; base hashes unchanged |
| UN8-T2 | `Group_Emerges` (U12) | a group record created by an NPC's `ProposeGroup` act and ≥ 2 members who each joined by their own `JoinGroup` act, in ≥ 2 of 3 seeds in the 72-person fixture; no group is created by any system |
| UN8-T3 | `Group_ProposeJoinContribute` | invitee joins only when the decision score favours it (a fixture with a hostile proposer: trust < threshold, refused); joining alone changes no goods; contributions are exactly the work ticks and wares the member put in |
| UN8-T4 | `Group_FreeRider_DoesNotContribute_StillMember` | a member with `ContributeBase` 0 stays a member, contributes nothing, uses the finished road; recorded as a free rider; the group is not forced to expel |
| UN8-T5 | `Group_Withdraw_KeepsContributions` | a withdrawing member's work and goods stay in the project; they lose later benefit as the objective defines (no right in a group-owned building); events correct |
| UN8-T6 | `Road_Cooperative` (U9) | a road group with ≥ 3 contributors completes a road between two settlement sites (or a planned site) on ≥ 2 of 3 seeds; path cost on it lower (UN5); a variant with low contribution propensity has ≥ 1 free rider |
| UN8-T7 | `Group_RoadBuilt_OnlyWithEnoughContributors` | with fewer contributors than needed to finish within `RoadStallDays`, the project stalls and the group `Failed`; the built prefix remains |
| UN8-T8 | `Group_Building_OwnedByGroup` | a group-built shelter is owned by `Owner.Group(g)`; members' Sleep and Gather rights follow `SharePermille`; an outsider's grant needs the configured majority; a member who dies releases the share to the others |
| UN8-T9 | `Group_NoAutoOrg_AtAnyThreshold` | a scenario with 24 members and a large asset over 30 days never creates an `Organisation` unless an NPC chooses `FoundOrg`; when one does, assets transfer by `Property.Transfer` and `GroupBecameOrg` fires once |
| UN8-T10 | `Group_NoSystemActs` | static and run probe: every group state change has an NPC actor (Unif-9); the system day-start only expires and dissolves by rule, never joins, contributes or founds |
| UN8-T11 | `Group_Deterministic`, `UN8_SaveLoadParity`, `UN8_EventsRoundTrip` | two runs on seeds 1–3 hash equal; as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN8|FullyQualifiedName~Neutrality|FullyQualifiedName~C6"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_groups --seed 3 --days 20 --record full --sqlite --out <scratch>\un8
```
Report per group: proposer, members and join ticks, contributions per member, withdrawals, free riders, completion or failure, who used the result.

## 15. Unity demonstration
None here (UN10b). Compile check required if inspection types changed: `check-game-compile.ps1` 0 errors.

## 16. Performance considerations
Groups ≤ `MaxGroups` (default 32) active; members ≤ 24; decisions only when invited or when a member's contribution candidate is generated (quota 4 shared); `DayStartUN8` O(groups). Report with the 72-person fixture and a 500-NPC bench.

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un8 -b unity/un8 unity/poc` (after UN3, UN4b, UN5 and UN6a merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un8\Library /E /MT:16` (0–7 = success; source editor closed).
2. Sim and EditMode tests first. Hand back **UN8a** early if the lead wants the building-group demonstration before roads are ready.
3. Before hand-back `git merge unity/poc`; conflicts only in your own files.
4. Neutrality pins that must hold: **P1, P4, P8**, P5 (C6 present), U-P1, `Groups_Off_Neutral`, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run.
7. "UN8 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- **Free-riding may dominate and the road never gets built** (a finding, not a bug): the package must report contribution rates by trust and benefit; tuning is a recorded decision, never a hidden bonus.
- **Decision model drift**: the join/contribute scores must use only the NPC's own beliefs and drives (the same Plan-6 discipline as UN6): reuse its static test.
- Group-owned asset semantics touch C1 shares and estates: reuse `Owner.Group`; do not add an owner kind.
- `OwnerKind.Group` was used as "faction" for estates (audit §11): confirm UN4b fixed that before relying on share release.
- Over-engineering the group UI of acts: invitations are existing gestures.

## 19. Definition of done
UN8-T1…T11 green; pins unchanged; compile check 0 errors; "UN8 record" lists the contribution and free-rider report, what ran, the defaults taken; handed back.

## 20. Smaller checkpoints
- **UN8a** (≈3 h): record, propose, join, contribute, withdraw, building and gather objectives, T1–T5, T8, T10.
- **UN8b** (≈2 h): road and settlement binding, free-rider measurement, no-auto-organisation, remaining objectives, T6, T7, T9, T11.

## 21. Recommended agent
**Opus.**

## 22. Used later by industry (keep the interfaces general)
Cooperatives, companies and unions (C6, K2) arise from groups by member acts; keep `Objective` open to new kinds (rows) and keep the `Group → Org` hand-over generic.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN8 record".
