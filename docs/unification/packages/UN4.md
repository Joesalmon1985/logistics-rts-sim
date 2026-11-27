# UN4: Starting population (six tribes of 7 adults and 5 children), communal building, founding settlements

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.9, D8.10, D8.17; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md); [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72, R73, R74; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §1.2, §9, §6.1–6.2 (the six founding settlements); [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §5; [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN4** (Tier A, Wave 1 for UN4a, Wave 2 for UN4b, Checkpoint UA) |
| Branch / worktree | `unity/un4` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un4`, from `unity/poc` after **UN0** merged (UN4b: new branch `unity/un4b` after UN1 and UN3a merged) |
| Effort | 4 agent-hours (UN4a generator 2.5 h; UN4b hall, founding group, settlements, start sequence 1.5 h) |
| Status | Not started |
| Recommended agent | **Sonnet** (data generation reusing the existing founder machinery). Never Fable |

## 1. Objective
A normal unified game begins with exactly six tribes of seven adults and five children (72 people: 42 adults, 30 children), deterministic, with plausible families, consistent genetics, varied ages, personalities and experience, proper caregivers, individual knowledge variation, no professions or institutions, and one **communal building** per tribe at its **founding settlement**, owned not by the tribe but by a founding community **group** of its adults.

## 2. User-facing outcome
At tick 0 the player sees six small communities, each around a hall, with families moving about; the people card shows kin, age, personality, knowledge and the household. Nobody has a job, office, currency or employer. (Drawing is UN10a.)

## 3. Relevant design documents
Design §9 (composition, generator, initial knowledge, communal building and owner, start sequence), §1.2 (tribe is affiliation), §6.1–6.2, §14.1 U1; `docs/stage4/POPULATION_DESIGN.md`, `docs/stage4/HOUSEHOLDS_AND_KINSHIP_DESIGN.md`, `docs/stage4/BIOLOGICAL_INHERITANCE_DESIGN.md` (genomes, pedigree); audit §5 (current 120-person start), §6.1.

## 4. Relevant existing source files (verified)
- Founders: `Population/Households/Founders.cs` (`StartGame` generators 0 and 1, `Established` ~186–215, couples, adoption, `FreeNode` at ~364), `Population/Households/HouseholdTuning.cs` (`EstablishedBand` ~208–216, `StartHuts`, `StoreFood`), `Population/PopulationModule.cs` (`StartGame` 17–37, `FounderNode` ~118–137, `GiveFounderPack` ~148–157), `Population/PopulationTuning.cs` (65–69 founder pack), `Population/Genetics/*` (`GenomeOps`, `Founder`, inheritance, mutation), `Population/KinData.cs` (`PedigreeRecord`, `KinCues`), `Core/GameSetup.cs` (`FactionCount`, `StartNpcsPerFaction`), `Territory/Faction.cs` (names, line 27: Amber, Azure, Crimson, Jade, Violet, Ivory), `Territory/TerritorySystems.cs` and `Territory/StartCorners` (start corners, `FactionRules.cs:247-`), `Civ/SkillStore.cs`, `Civ/Assets.cs` (`Assets.Create`), `Population/LotBin.cs` (`OwnerKind.Group`), `Estate/Estates.cs` (group share release).
- UN0 seams you fill: `Founders.StartGame` generator 2 stub, `GameSetup.Unified`, `Settlement` record and `Settlements` API, `UnifMind.Known`, `UnificationState.UN4.cs`, `UnificationTuning.UN4.cs`, `UnificationHooks.UN4.cs`, `UnificationEvents.UN4.cs`, `BuildingTable.AddRowsUN4`, `UN4Mappings.cs`; UN1's unified board and start corners; UN3's building rows and `ShelterIndex` (for UN4b).
- Tests to model on: `Assets/God/Tests/EditMode` founder and household tests, `DotNet/God.Runner.Tests/DemogEstablishedTests` pattern, `Support/UnifiedFixtures.cs`.

## 5. Allowed scope
**UN4a (generator; needs only UN0):**
1. **Generator 2 "Tribal"** in `Population/Households/TribalFounders.cs`, called from `Founders.StartGame` when `GameSetup.Unified`: per tribe, from `Rng.Fork("tribe-family", t)`, three unrelated couples (6 adults) plus one adult sibling of one partner (the seventh adult, in that partner's household), five biological children in a pattern table (default 2, 2, 1 over the couples; other patterns configurable). Adult ages 18–50 with partner gap ≤ `PartnerAgeGapMax` (8 y), sibling within 15 y of the partner, children 0–14 (at least one under 4 per tribe where the pattern allows), each parent at least `MinParentAgeAtBirth` (18) older than each child. Genomes: `GenomeOps.Founder` from `Rng.Fork("genome-founder", npcId)`; the sibling is genetically related to the partner-sibling through a common unnamed parent pair (pedigree: full siblings, unknown parents); children inherit through `GenomeOps` with mutation; sexes drawn per NPC, couples opposite-sex and unrelated (`Relatedness == 0` asserted); adults unrelated across tribes. Personalities from the genome and `Rng.Fork("personality", npcId)` with a spread check (`PersonalitySpreadMin`). Experience: each adult a modest `SkillStore` entry (0–200 in one or two of foraging, building, hauling), no profession, no title. Caregiving: at least two caregivers per child from the household's adults; infants get a primary feeder; M4 caregiving applies. Names deterministic by tribe culture.
2. **Initial knowledge** (design §9.3) as bits in `NpcMind.Unif.Known` with individual variation by deterministic draw: survival and foraging all seven foods (everyone), level-1 shelter construction (95 % of adults), level-2 (40 %), level-3 (10 %), road building (50 %), settlement founding (25 %), recipe knowledge 0–2 of sawing, stonecutting, weaving, potting, cooking per adult, toolmaking ≤ 10 %. Knowledge is not a profession; skill, knowledge and equipment remain separate fields.
3. **No formal anything:** no `Organisation`, office, currency, employment contract, professional soldier, production chain or title at tick 0 (`Orgs.Emergence` governs; none seeded).
4. **Counts as configuration**: `UnificationTuning.UN4.Start` (`AdultsPerTribe` 7, `ChildrenPerTribe` 5, family patterns, age bounds, knowledge shares, `StartFoodDays` 1); U1 asserts the defaults.
5. **Events:** 1206 `TribeStarted { Tribe, Adults, Children, Households }` (one per tribe at tick 0).

**UN4b (needs UN1 board and UN3a buildings):**
6. **Founding settlements:** six `Settlement` records (UN0 record, `Settlements.Create`) at the six standard six-player start corners (`StartCorners.Pick`), preferring coast-near land corners so seaweed and fish are reachable; fairness check from UN1; **tribe order**: the unified game lists and colours tribes in the owner's order Amber, Crimson, Azure, Jade, Violet, Ivory through UN9's `TribeOrder` table (UN4b reads it; until UN9 merges, F0..F5 as in code). No HQ is created.
7. **Communal hall:** the `CommunalHall` row (kind 21, `BuildingTable.AddRowsUN4`: Shelter + Storage, level 1, 8 beds Basic, 30 general storage) created finished at tick 0 through `Assets.Create` at the settlement node.
8. **Founding community group:** UN4b declares the `Group` record (`Unification/Groups/Group.cs`, `partial`: `Id`, `Kind`, `Founder`, `Members[]` (≤ 24, status Active/Withdrawn), `State`, `AssetOwner`, `FoundedTick`) and `Groups.CreateCommunity(s, ev, members)`; UN8 extends the partial. One group of kind `Community` per tribe (members: the seven adults, no officers, no rules) owns the hall (`Owner.Group(g)`, basis `Founded`); every adult has Sleep and Store rights through membership; children sleep by household right; membership changes by ordinary acts, death releases the share to the remaining members (C1 `Estate` for group shares); the tribe never owns anything and `Owner` has no tribe kind.
9. **Start sequence** (design §9.6): settlements → family generator → hall → NPCs placed in a deterministic ring around the hall (`HutSpacing`), no hut objects, no collection shelter, no tool, no raw material → the small starting food of `StartFoodDays` (one day's forage food for the household, in the hall's bin, owner the group, reason `Founder`, recorded; configurable 0) → dice, materialisation and everything else begin. Households are created as today (`HouseholdState`) with no `Hut`.
10. **Events:** 1206 as above plus `GroupProposed`-free: the founding group emits no 1290 (it is not proposed); a `TribeStarted` carries the group id.

## 6. Explicitly excluded scope
Immigration (stays off), tribe change and citizenship (exist, unchanged), collection buildings and tools (UN3), planning of any kind (UN6), group behaviour beyond creating and holding membership (UN8), removing Stage 3 systems or the old start (UN9: the legacy start stays reachable with `StartKind = Legacy`), views (UN10), tuning of fertility or mortality (UN11 V1 reports).

## 7. Dependencies
Hard for UN4a: **UN0**. Hard for UN4b: **UN1** (board, corners), **UN3a** (hall row, beds, `Assets.Create` with the row). Soft: UN9 (`TribeOrder`, unified bootstrap calls the generator). Downstream: UN6 (knowledge bits seed goals), UN9, UN10a, UN11.

## 8. Shared contracts
**Read:** `Goods`, genetics API (`GenomeOps`), `HouseholdState` creation API, `Assets.Create`, `Settlements` API, `Rng`. **Modify (own files only):** `TribalFounders.cs`, UN4 partials, `Groups/Group.cs`. **Never** edit `Founders.cs` (the UN0 stub calls you), enum files, `GodSim`, `SaveFile`, `PopulationModule.cs`.

## 9. Files and modules owned
`Population/Households/TribalFounders.cs`, `Population/Households/TribalKnowledge.cs`, `Unification/Start/UnifiedStart.cs` (sequence), `Unification/Buildings/BuildingRows.UN4.cs` (hall row), `Unification/Groups/Group.cs`, `Unification/UnificationState.UN4.cs`, `UnificationTuning.UN4.cs`, `UnificationHooks.UN4.cs`, `UnificationEvents.UN4.cs`, `DotNet/God.Runner/Warehouse/UN4Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_start.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN4*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN4*.cs`.

## 10. Required domain events
`1206 TribeStarted { Tribe, Adults, Children, Households, Group, Settlement }`. `BuildingFounded`/`BuildingCompleted` (UN3) are emitted for each hall; no other new codes.

## 11. Save/load implications
Saved: the `Group` records (membership), settlements, the generated people and households (existing shapes), knowledge bits. Test `UN4_SaveLoadParity`: save at tick 1, mid-day 3 (a member left the group, one child born) and load; continue equals the straight run.

## 12. Warehouse implications
Mapper `UN4Mappings.cs` for 1206; `UN4_EventsRoundTrip`. UN11 adds `dim_tribe`, group and settlement rows.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN4-T1 | `Unified_Start_Counts` (U1) | at tick 0: six tribes (F0..F5) × 7 adults (age ≥ 18) × 5 children (age < 15) = 72 (42 + 30), on seeds 1–20; each child has ≥ 2 adult caregivers in the household; no `Organisation`, office, employment, hut, collection building or tool; six halls |
| UN4-T2 | `Tribal_Family_Consistency` | couples opposite-sex, `Relatedness == 0` between partners, parent-child age gaps ≥ 18, sibling within 15 y of partner and genetically related, children's genomes are inherited from their parents (check by `GenomeOps` pedigree), adults unrelated across tribes, personality spread ≥ `PersonalitySpreadMin` |
| UN4-T3 | `Tribal_Deterministic` (U14 part) | two runs of the generator with the same seed give byte-equal people; different seeds differ; the generator never calls the wall clock or unordered enumeration (static test) |
| UN4-T4 | `Knowledge_VariesNotProfessions` | knowledge shares within ±15 percentage points of the configured shares over 20 seeds; no two tribes identical; static test `NoProfessionField` on `Npc` |
| UN4-T5 | `Hall_OwnedByGroupNotTribe` | the hall's owner is `Owner.Group(g)` with basis `Founded`; no `Owner` anywhere is a tribe or settlement (`Settlement_OwnsNothing`); members hold Sleep and Store rights; leaving the group removes them; a death releases the share through the estate rule |
| UN4-T6 | `Start_Sequence_NoCapital` | at tick 0 no collection shelter, tool, raw lot or production chain exists; the only goods are the `Founder` starting food in the hall bin; the ledger books it |
| UN4-T7 | `Start_SettlementsOnCoastCorners`, `Start_Fairness` | six settlements on legal corners (distance rule satisfied), each within reach of shore nodes, each within 2 hexes of ≥ 4 of the 7 hex types on seeds 1–20 |
| UN4-T8 | `Start_NoStage3Side` | with `GameSetup.Unified` the start creates no HQ, faction stock or walker (asserted on the state; the removal of the systems is UN9) |
| UN4-T9 | `Start_Survives_First_Day` | the 72 people on the unified board with UN1 forage and the hall's bed capacity (8 beds for 12 people at half-bed children: 9.5 needed) all survive 3 days on seeds 1–3; the bed shortfall is reported |
| UN4-T10 | `UN4_SaveLoadParity`, `UN4_EventsRoundTrip`, `Unification_Off_Reproduces_Base` | as sections 11 and 12; U-P1 with switches 0 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN4|FullyQualifiedName~Neutrality|FullyQualifiedName~Founders"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_start --seed 3 --days 3 --record full --sqlite --out <scratch>\un4
```
Print a table per tribe: household structure, ages, sexes, kin, knowledge bits, group members, hall beds and food; a family tree for one tribe.

## 15. Unity demonstration
None here (UN10a). Compile check required if `GameSetup` or types read by `God.Game` changed: `check-game-compile.ps1` 0 errors.

## 16. Performance considerations
Generation is one-off at tick 0 (72 people): report generation time; no per-tick cost. Group membership lists ≤ 24.

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un4 -b unity/un4 unity/poc` (after UN0 merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un4\Library /E /MT:16` (0–7 = success; source editor closed). UN4b continues in the same worktree on `unity/un4b` cut from `unity/poc` after UN1 and UN3a merged.
2. Sim and EditMode tests first. Hand back **UN4a** when T1–T4 and T10 are green.
3. Before hand-back `git merge unity/poc`; conflicts only in your own files.
4. Neutrality pins that must hold: **P1, P4, P8**, U-P1, U-P2 form (`StartKind = Legacy` unchanged), the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`, the existing founder and demography pins.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run.
7. "UN4 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- The older demography conclusions are unverified on merged code and for 72 people instead of 120 (audit R8): UN4 asserts structure, not viability; UN11 V1 measures viability.
- `OwnerKind.Group` was used only as "back to community = faction" for estates (`Estates.cs:292`): the estate rule for group shares must be reviewed so that a dead member's share goes to the remaining members, never to a tribe.
- Hall capacity pressure (8 beds for 12 people) is intended but could cause early exposure deaths: report, do not adjust.
- Genetic consistency checks depend on the pedigree API: use the existing `PedigreeRecord` rather than inventing a kin store.
- Tribe naming order differs from the code (audit §5): do not rename enum values here; use UN9's `TribeOrder`.

## 19. Definition of done
UN4-T1…T10 green; pins and Stage 3 hashes unchanged; compile check 0 errors; "UN4 record" lists what ran and the defaults taken; UN4a handed back before UN4b; handed back.

## 20. Smaller checkpoints
- **UN4a** (≈2.5 h): generator, knowledge, counts, determinism, T1–T4, T10 (without settlements and hall).
- **UN4b** (≈1.5 h): settlements, hall, founding group, start sequence, T5–T9.

## 21. Recommended agent
**Sonnet.**

## 22. Used later by industry (keep the interfaces general)
The `Group` partial record and the hall row are the pattern for later communal buildings and cooperatives; the generator's `Start` tuning is the place later scenarios (industrial starts) override counts and knowledge.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN4 record".
