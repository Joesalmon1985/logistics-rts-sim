# UB2: Catalogue breadth (UB objects, buildings, recipes, furnishings and leisure interactions)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

**Later-stage spec (Tier UB).** Shorter than the UA specs; every owner-brief §40 field is present. Replaces the D8 UN7 "map the whole workbook" breadth work (the mapping itself is UN3a; UA-initial rows are UN3a/UN3b).

| Field | Value |
|---|---|
| Package identifier | **UB2** (Tier UB; Checkpoint UB) |
| Branch / worktree | `unity/ub2` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-ub2`, from `unity/poc` after **UN3a and UN3b** merged |
| Effort | 3.0 agent-hours |
| Status | Not started |
| Recommended agent | **Sonnet** (data-driven rows on a finished interaction system; validated by the generator and closure audit). Never Fable |

## 1. Objective
Extend the reviewed catalogue and the generated tables with the **UB-stage** object, building and recipe rows in dependency order, using only existing effect kinds: reed yard, seed plot, pottery, stone workshop, tannery, weaving mill, sheep pen, charcoal kiln, lime kiln, water jug, soft armour, stone spear, plus furnishings and leisure interactions. Every row has a status and evidence; any chain without prerequisites stays staged or deferred.

## 2. User-facing outcome
People with the right knowledge can make more things from physical inputs: jugs to carry water, tools and weapons, woven and tanned goods, furnishings that satisfy comfort, room and fun needs. Nothing is injected; every output has a recipe, inputs, labour and a building.

## 3. Design anchors
Amendment §8 (map all 138 objects and 65 buildings but stage nonessential recipes, advanced combat, law, institutions and industry; the workbook stays the three-tab authoring backlog; no additional runtime catalogue or primitive action), §3 (physical raw extraction rules unchanged), D9.3, §D9.5 rows E–I and G4, G7 (reed and seeds are UB), G8 (waste beyond the latrine UB), G9 (Hunter's hut consumes a carried rabbit), G10 (mines Later), G13 (tools are speed multipliers and strike values only), §23.4 (ware ids 94–159 allocated by UN3a/UB in manifest order, append only; `ProduceReason` 11 `Drawn`, `ConsumeReason` 14 `Drunk`, 15 `Washed`, `Ware.Water` 80 reserved for the jug), §23.7 (catalogue schema, generator, `Catalogue_*` tests), CATALOGUE_MANIFEST §3–§6, index §2 row UB2, D8 U6 (recipes, reworded).

## 4. Relevant existing source files (verified)
`docs/unification/God_of_the_Island_Core_Design.xlsx` (three tabs; workbook SHA pinned in the manifest), `docs/unification/CATALOGUE_MANIFEST.md`, `docs/unification/catalogue/catalogue.json` and `DotNet/God.Catalogue/*` (created by UN3a, not present at spec time), `Assets/God/Sim/Unified/Catalogue/Generated/*.g.cs` (UN3a), `Unified/Buildings/*`, `Unified/Interactions/*` (UN3b), `Assets/God/Sim/Economy/Wares.cs` (`Ware` enum; reused ids `Log` 1, `Clay` 2, `Stone` 5, `Plank` 7, `Axe` 20, `Roots` 29, `Fish` 30, `Herbs` 32), tests `DotNet/God.Runner.Tests/Catalogue*.cs` (UN3a) and UN3b's `Econ1*`, `Need1*`.

## 5. Allowed scope
1. Promote the named UB rows in the catalogue source with their manifest corrections (N1, A–I), D9 numbers and status; extend the generator output; append ware ids from 94 in manifest order (append only).
2. Recipes and workstation rows for the UB buildings (inputs, outputs, labour, tool bonus) using `WorkInput`, `RecipeWork`, `Fuel`, `Install`, `Occupy`, `Play`, `Relieve` handlers of UN3b; water jug with `Drawn`/`Drunk`/`Washed` reasons if the handlers allow it, otherwise a recorded request to UN3b's owner via the lead; soft armour and stone spear as equip/strike rows for UN8a effects.
3. Furnishings and leisure interactions as data rows (comfort, room, fun relief paths) with physical occupancy and capacity only.
4. Closure, cycle and reachability audit extended for UB rows (`Catalogue_UBClosure`), coverage report for CATALOG-1 (counts by stage and status), explicit "staged/deferred/rejected" reasons for rows not promoted (including ownership-worded rows, mines, industry).
5. New `Effects.UB2.cs` **only** for a data-defined effect that cannot be expressed with existing kinds; each needs a lead-approved `EffectKind` addition (UN0/lead owns the enum).

## 6. Explicitly excluded scope
New primitive verbs or controls; a second runtime catalogue; ownership, permission, rights or market wording in any row; automatic production, assigned operators or staff, recruitment; injected goods; mines, industry, energy, currency, boats; promoting rows whose inputs have no bootstrap path; changing UA rows or tuning; behaviour (the people choose; UN6t2 trains use).

## 7. Dependencies
Hard: **UN3a** (catalogue source, generator), **UN3b** (interactions and building capabilities). Merge-after: none. Downstream: UN6t2, UN11b, UC1.

## 8. Shared contracts
R: §23.7 schema and `EffectKind`, `Catalogue_*` tests, ware id allocation rule. M: `docs/unification/catalogue/*` additions, generated tables, `Effects.UB2.cs`, status columns of `CATALOGUE_MANIFEST.md`. Never edit frozen files or the enum without a lead amendment.

## 9. Files and modules owned
`docs/unification/catalogue/*` (UB additions), `Unified/Catalogue/Generated/*` (regenerated), `Unified/Interactions/Effects.UB2.cs`, tests `DotNet/God.Runner.Tests/UB2*.cs`, `Catalogue_UB*.cs`, `docs/runs/CATALOG_UB.md`.

## 10. Required domain events
None new (existing `RecipeCompleted` 1266, `NeedRelieved` 1268, `FixtureInstalled` 1250 and others as UN3b/UN4).

## 11. Save/load implications
None beyond content hashes: the catalogue content hash changes, which invalidates artifacts trained against the old hash (UN6t2 retrains); `Catalogue_GeneratedMatchesSource` guards drift. Ware ids appended, never renumbered.

## 12. Warehouse implications
Mappers unchanged (events unchanged); catalogue coverage is exported by the report; migrations none.

## 13. Acceptance tests
| Id | Test | Maps to | Pass |
|---|---|---|---|
| UB2-T1 | `Catalogue_GeneratedMatchesSource`, `Catalogue_WorkbookShaPinned`, `Catalogue_UBClosure`, `Catalogue_NoCycleWithoutBootstrap` with the UB rows | CATALOG-1 | exact |
| UB2-T2 | `Recipe_Run_Refused_Without` for each new recipe: refused with the specific reason when knowledge, labour, inputs or the right building are missing; output ledger equals consumed inputs; no goods from nothing | D8 U6 (reworded) | exact |
| UB2-T3 | `Furnishing_Relief_Physical`: each new furnishing relieves its need only by use, subject to occupancy and capacity, with no owner or right check | NEED-1, EP-01 | exact |
| UB2-T4 | `NoOwnershipWording`: no row, alias or tooltip in the catalogue contains title or permission wording | D9.3 | exact |
| UB2-T5 | `Coverage_Report`: CATALOG-1 table of every one of 138 objects and 65 buildings with stage and status; counts add up | CATALOG-1 | exact |
| UB2-T6 | `Water_Jug_Physical` (if promoted): drawing, carrying, drinking and washing conserve units with the `Drawn`/`Drunk`/`Washed` reasons | MOTOR-2 | exact |
| UB2-P | pins U-P1, P1, P4, P8, Stage 3 hashes with the switch off; UA tests unchanged | all | exact |

## 14. Headless demonstration
`dotnet test DotNet -c Release --filter "FullyQualifiedName~UB2|FullyQualifiedName~Catalogue"`; `dotnet run --project DotNet/God.Catalogue -- validate`; a fixture run making one jug and one woven item (labelled mechanical) and the coverage report.

## 15. Unity demonstration
Compile check `AgentScripts/check-game-compile.ps1` 0 errors (generated tables compile into `God.Sim`). No Unity run.

## 16. Performance considerations (D10)
Generated tables are static arrays indexed by id; adding rows must not add per-tick cost (lookups O(1)); recipes processed only at active workstations; report table sizes.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-ub2 -b unity/ub2 <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-ub2\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "ub2 record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.

## 18. Risks
Rows with ownership wording promoted by mistake; a recipe chain without a bootstrap path; ware id collisions with industry (40–71 reserved); artifact invalidation by hash change (expected, UN6t2 retrains); workbook ambiguities (decide per CATALOGUE_MANIFEST §6 and record).

## 19. Definition of done
UB2-T1…T6 and UB2-P green; coverage report committed; compile check 0 errors; "UB2 record" in DEV_LOG lists rows promoted, staged and deferred with reasons; handed back.

## 20. Smaller checkpoints
**UB2a** food, tools, jug and kilns; **UB2b** textile, leather, armour, spear, furnishings and leisure.

## 21. Recommended agent
**Sonnet**. Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UB2 record" under "Design decisions taken during implementation".
