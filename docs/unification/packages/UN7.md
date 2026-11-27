# UN7: Production recipes (D8.12 data rows, workshops through the building architecture)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.12, D8.14, D8.17; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) §7, §22.3; [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R58, R72; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §8, §4.5 (sheep); [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §8, R9, R13; [docs/civdiv/DEVELOPMENT_PACKAGES/C1.md](../../civdiv/DEVELOPMENT_PACKAGES/C1.md) §22; [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN7** (Tier B, Wave 2, Checkpoint UB) |
| Branch / worktree | `unity/un7` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un7`, from `unity/poc` after **UN0** and **UN3a** merged |
| Effort | 3 agent-hours |
| Status | Not started |
| Recommended agent | **Sonnet** (data rows on an existing mechanism, knowledge and tool terms). Never Fable |

## 1. Objective
The initial Widelands production chains exist as **data rows** run by NPCs through the building architecture: wood to planks, stone to building blocks, clay to pottery, clay to bricks, shearing and slaughter, wool to cloth, hay to bedding, fish to prepared food, wood and stone to axe and hammer. Each transformation needs actual materials, **knowledge**, **labour time**, the right **building capability and level**, and where the row says so a **tool**; nothing is produced from nothing.

## 2. User-facing outcome
A person with the know-how and a level-2 shelter or a workshop turns logs into planks, fish into food that keeps better and feeds more, wool into cloth; they may sell or use the product. Specialisation can follow (UN11 measures whether it did).

## 3. Relevant design documents
Design §8.1–8.4 (recipe rows, the first rows R1–R9, reuse and replace of Stage 3 data, tests), §4.5 (sheep: shear keeps head, slaughter), §5.1–5.2 (production capability and slots), §11.2, §14.1 U6; owner brief §6; audit §8 (the table of D8.12 chains versus Stage 3 data), R9 (ware overload), R13 (valuation); `docs/stage4/INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md` §3.4–3.5 (the later extension of the same table).

## 4. Relevant existing source files (verified)
- C1 recipe machinery: `Civ/Assets.cs` (`Recipe`, `AssetDef.Recipe`, `WorkTicks`), `Civ/Work.cs` (`Operate` ~143–190, `Speed` 35–47), `Civ/Employment.cs`, `Civ/SkillStore.cs`, `Population/PopulationState.cs` (`ProduceReason.Crafted`, `ConsumeReason.Built`, `Wear`), `Population/Goods.cs`, `Population/LotBin.cs`.
- Stage 3 data (read only, seed for numbers): `Economy/BuildingDefs.cs` (processors 130–160), `Economy/EconomyDefs.cs`, `Economy/Wares.cs` (`Plank` 7, `Brick` 9, `Pottery` 14, `Cloth` 17, `Axe` 20).
- Valuation and nutrition: `Social/Valuation.cs` (food, log, axe only, 22–41), `Population/PopulationModule.cs` (`SpoilDays`, `Nutrition` ~210–232), UN0's `ValuationPriors`.
- UN0 seams you fill: `RecipeDef`, `RecipeTable` (`AddRowsUN7`), `BuildingTable.AddRowsUN7` (workshop kinds 30–36), UN0 ware ids 80–86, `UnificationState.UN7.cs`, `UnificationTuning.UN7.cs`, `UnificationHooks.UN7.cs`, `UnificationMindHooks.UN7.cs`, `UnificationEvents.UN7.cs`, `ValuationPriors.UN7.cs`, `UN7Mappings.cs`, `Verb.Craft` 13 (existing), `NpcVerb`s already defined.
- UN3a: `BuildingTable` rows, the collection and construction code, production-slot fields; UN4a knowledge bits.
- Tests to model on: `Assets/God/Tests/EditMode/Civ/C1*.cs` (workshop recipe tests), `Support/UnifiedFixtures.cs`.

## 5. Allowed scope
1. **`RecipeDef` rows** (`Recipes/RecipeRows.UN7.cs`, all values configurable in `UnificationTuning.UN7`): R1 sawing (1 `Log` → 1 `Plank`, 80 ticks), R2 stonecutting (1 `Stone` → 1 `BuildingBlock`, 100), R3 potting (2 `Clay` → 1 `Pottery`, 90), R4 firing (2 `Clay` + 1 `Log` → 2 `Brick`, 120), R5 shearing (1 sheep kept alive → 1 `Wool`, 40, once per `ShearIntervalDays` 3), R5b slaughter (1 sheep → 3 `Meat` + 1 `Hide`, 60), R6 weaving (2 `Wool` → 1 `Cloth`, 100), R7 bedding (2 `Hay` → 1 `Bedding`, 40), R8 cooking (1 `Fish` → 2 `PreparedFood`, 40), R9 toolmaking (1 `Log` + 1 `Stone` → 1 `Axe` or `Hammer`, 120). Fields: `Inputs[]`, `Outputs[]`, `LabourTicks`, `Knowledge` (a `NpcMind.Unif.Known` bit), `Skill` (`SkillClass`), `Tool` (ware + min uses, none allowed), `Needs` (a `BuildingDef` capability flag and minimum level), `Reason` (`Crafted`).
2. **Workshop rows** in `BuildingTable` (`AddRowsUN7`): kinds 30 `Carpentry`, 31 `Stonecutting`, 32 `Pottery`, 33 `Kiln`, 34 `Weaver`, 35 `Hearth`, 36 `Toolmaker`, each Production + Storage, level 1 labour only (the same rule as the collection shelters) except where the row's data adds a material cost; plus "any shelter of level ≥ 2 with Storage" as host for R7 and R8 and "the level-3 collection shelter hosts one recipe" (the design's production slot): all by data (`ProductionSlots`, recipe lists), never by an enum switch.
3. **The recipe act** by generalising C1's `Work.Operate` through the row (flagged additive edit): inputs consumed from the building's store or the worker's carry with `ConsumeReason.Built` (R58: until I1a's `Production` reason exists), outputs by `Goods.Produce(..., ProduceReason.Crafted, actor)` into the store **owned by the asset's owner** (C1 rule); the act is refused with reasons `NoKnowledge`, `NoTool`, `NoCapability`, `NoInput`, `NoSlot`; labour adds at `Work.Speed` (skill and tool factors: an axe gives +25 % on R1, a hammer +25 % on R2; both optional); tools wear through `ConsumeReason.Wear` per `Uses`.
4. **Sheep** (design §4.5): enclosure acts R5 and R5b operate on UN3's `Enclosure` register (sheep count is a living stock in the ledger: `Σ intake − Σ slaughtered = enclosure heads`); shearing leaves the head count unchanged; meat and hide are lots.
5. **Wares behaviour**: nutrition, spoilage and weight rows for `Meat`, `PreparedFood` (better nutrition and spoilage than raw fish), `Bedding` (raises sleep recovery when stored in a shelter bin of a building with beds: data `BeddingRecoveryBonusPermille` default 100), `Wool`, `Hide`, `BuildingBlock`, `Hammer`, `Pottery` (storage capacity +`PotteryStoreBonus` default 5 in a building store), `Cloth`; `Axe` stays unique and keeps its existing role (R9 ware overload resolved by using it only as the NPC tool in unified mode: no Axesmith).
6. **Candidates** (UN7's share of the Unification quota): `Operate(recipe)` at a building the NPC may use when inputs are present or believed obtainable; the planner (UN6) produces `Craft` and `Stock` goals through `GoalRules.UN7.cs` (this package registers the `Craft` rule: trigger a learned want for the product or an observed demand; expansion obtains inputs and capability). Valuation priors for the new wares so M5 barter can price them (rows in `ValuationPriors.UN7.cs`), and instinct priors that make a hungry NPC treat `PreparedFood` and `Meat` as food.
7. **Events:** 1233 `RecipeRun` (HV: asset, worker, recipe, outputs), 1234 `SheepSheared`, 1235 `SheepSlaughtered`; and refusal reasons on a `RecipeRefused` field of 1233's sibling code 1236 (spare, appended).

## 6. Explicitly excluded scope
Invention of recipes (only defined rows); the industrial rows (coal, oil, metal, electricity, vehicles: I1…); the ten Stage 3 processed wares not named by D8.12 (`Mortar`, `Bronze`, `Iron`, `Steel`, `Bread`, `Leather`); armouries and barracks; money; knowledge acquisition and teaching systems beyond the existing bits and C7 teaching; the building architecture itself (UN3); the planner (UN6); presentation (UN10b).

## 7. Dependencies
Hard: **UN0**, **UN3a** (building table, store, owner, production-slot fields). Soft: UN2 (raw inputs from nodes), UN6 (goal rules: merge order UN6 before UN7 not required; rules registered when both present), UN4a (knowledge bits). Downstream: UN10b, UN11b (recipe scenarios, long run).

## 8. Shared contracts
**Read:** `Goods` (frozen API), `Bodies`, `Reach`, `Work.Speed`, `BuildingTable` and `Enclosure` (UN3), UN0 ids. **Modify (own files and one flagged additive edit):** `Recipes/*`, UN7 partials, `ValuationPriors.UN7.cs`, `Civ/Work.cs` (`Operate` through `RecipeDef`, gated by `Buildings.Enabled`). **Never** edit `Wares.cs`, `Goods.cs`, `PopulationState.cs`, enum files, `GodSim`, `SaveFile`.

## 9. Files and modules owned
`Unification/Recipes/RecipeRows.UN7.cs`, `Recipes/RecipeRun.cs`, `Recipes/Tools.cs`, `Recipes/SheepActs.cs`, `Unification/Buildings/BuildingRows.UN7.cs`, `Unification/Plan/GoalRules.UN7.cs`, `Unification/UnificationState.UN7.cs`, `UnificationTuning.UN7.cs`, `UnificationHooks.UN7.cs`, `UnificationMindHooks.UN7.cs`, `UnificationEvents.UN7.cs`, `ValuationPriors.UN7.cs`, `DotNet/God.Runner/Warehouse/UN7Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_recipes.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN7*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN7*.cs`.

## 10. Required domain events
`1233 RecipeRun (HV) { Asset, Worker, Recipe, InputUnits, OutputUnits, Owner, Control }`; `1234 SheepSheared { Asset, Worker, Wool }`; `1235 SheepSlaughtered { Asset, Worker, Meat, Hide }`; `1236 RecipeRefused { Asset, Worker, Recipe, Reason }`.

## 11. Save/load implications
Saved: per-asset open recipe run (progress ticks, claimed inputs), `Enclosure.LastShearDay`, tool `Uses` on tool lots. Test `UN7_SaveLoadParity`: save mid-recipe (inputs consumed, half the labour done) and mid-shear; load; continue equals the straight run.

## 12. Warehouse implications
Mapper `UN7Mappings.cs` for 1233–1236; `UN7_EventsRoundTrip`. UN11 defines `recipe_run` and extends the ledger tables to the new wares and reads the existing `Crafted` reason.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN7-T1 | `Unification_Off_Reproduces_Base` (U-P1), `C1_Workshop_Unchanged` | Stage 3 and C1 workshop tests and hashes unchanged with switches 0 |
| UN7-T2 | `Recipe_Run` (U6) | an NPC with the knowledge, the inputs and a building of the right capability and level runs R1: inputs consumed exactly (`Built`), output `Plank` produced (`Crafted`), owned by the building's owner, in the store; ledger balances; labour ticks ≈ `LabourTicks × 1000 / Work.Speed` |
| UN7-T3 | `Recipe_RefusedWithout` (U6) | refused with `NoKnowledge`, `NoInput`, `NoCapability` (level too low), `NoSlot`, and `NoTool` for a row that requires one; each reason tested separately; no goods created on refusal |
| UN7-T4 | `Recipe_NoGoodsFromNothing` | over a 10-day fixture with every row exercised, for each ware `Σ Produced − Σ Consumed = Σ holdings` and each output equals the row's outputs per completed run |
| UN7-T5 | `Recipe_DeterministicLabour` | same seed, same completion ticks; skill and tool factors integer and exact |
| UN7-T6 | `Sheep_ShearKeepsHead_SlaughterConsumes` | R5 yields wool at most once per `ShearIntervalDays` and leaves the head count; R5b reduces heads by 1 and yields 3 meat + 1 hide; the sheep ledger conserves |
| UN7-T7 | `Cooking_FishToFood` | raw fish is not edible by default (UN1 setting); R8 yields `PreparedFood` with higher nutrition per unit than the forage foods in the table by the configured ratio and longer spoilage |
| UN7-T8 | `Tools_Optional_SpeedUp_AndWear` | an axe speeds R1 by the configured permille and loses `Uses`; without it the run is slower, never refused |
| UN7-T9 | `Craft_Goal_FromDemand` (Mech) | an NPC with the knowledge adopts a `Craft` goal when a buyer's offer shows demand (fixture), obtains inputs by collection or trade and runs the recipe, no command |
| UN7-T10 | `Production_DoesNotBranchOnOwnerKind`, `Recipes_DataOnly` | static: `Unification/Recipes` has no branch on `OwnerKind` and no `switch` on recipe id; adding a row in a test needs no code change |
| UN7-T11 | `UN7_SaveLoadParity`, `UN7_EventsRoundTrip` | as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN7|FullyQualifiedName~C1|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_recipes --seed 3 --days 12 --record full --sqlite --out <scratch>\un7
```
Report per recipe: runs, inputs, outputs, labour, refusals by reason, owner of outputs; and the ledger for each new ware.

## 15. Unity demonstration
None here (UN10b). Compile check required if types read by `God.Game` changed (ware enum, asset records): `check-game-compile.ps1` 0 errors.

## 16. Performance considerations
One open recipe run per asset slot; per-tick cost O(active runs); bounded by `ProductionSlots`. Report with 72 NPCs. No per-tick scan of the recipe table (index by building kind at start).

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un7 -b unity/un7 unity/poc` (after UN0 and UN3a merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un7\Library /E /MT:16` (0–7 = success; source editor closed).
2. Sim and EditMode tests first.
3. Before hand-back `git merge unity/poc`; conflicts only in your own files.
4. Neutrality pins that must hold: **P1, P4, P8**, `C1-T1`, U-P1, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run.
7. "UN7 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- **Knowledge, skill and tool must stay distinct fields** (owner brief §7): do not collapse knowledge into skill.
- Rows referencing a collection shelter level as the host (R2, R3) depend on UN3's production slots: check the L3 data before relying on it.
- Nutrition, spoilage and weight rows for new wares are easy to forget: T4's all-wares ledger check catches missing rows.
- `Axe` is unique (`IsUnique`) and not mergeable; tools must not break lot merging.
- Valuation priors can distort barter (R13); keep them derived from labour, not invented.

## 19. Definition of done
UN7-T1…T11 green; pins unchanged; compile check 0 errors; "UN7 record" lists what ran and the defaults taken; handed back.

## 20. Smaller checkpoints
- **UN7a** (≈1.5 h): recipe table, run act, R1–R4 and R7, workshop rows, T1–T5, T10.
- **UN7b** (≈1.5 h): sheep, cooking, tools, goal rule, priors and nutrition rows, T6–T9, T11.

## 21. Recommended agent
**Sonnet.**

## 22. Used later by industry (keep the interfaces general)
The industrial runner (I1) appends recipe rows 21+ and facility kinds to the same tables; keep `RecipeDef` open to several inputs and outputs and non-ware terms later (fuel, electricity) without changing existing rows.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN7 record".
