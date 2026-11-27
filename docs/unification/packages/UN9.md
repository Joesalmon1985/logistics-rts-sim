# UN9: Decouple Stage 3 from the normal game (no faction AI, stockpiles, walkers, flags, construction, VP or circles in unified games; Stage 3 kept as an isolated regression scenario)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.1, D8.9, D8.11, D8.16; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) §11 (system order), §16, §17; [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §1, §13.1, §13.2, §13.8, §14.1 U11, U13, B11; [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §2, §3 (S1–S16), §9, §12 (R1, R4, R5, R6, R15, R17); [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN9** (Tier A, Wave 1 for the sim switches, Wave 2 for the bootstrap; Checkpoint UA) |
| Branch / worktree | `unity/un9` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un9`, from `unity/poc` after **UN0** merged |
| Effort | 4 agent-hours (UN9a sim module list, elimination and VP, probes 2 h; UN9b bootstrap, menu, regression scenario, tribe order 2 h) |
| Status | Not started |
| Recommended agent | **Opus** (removal of a whole subsystem from the normal path without disturbing 40 pinned scenarios; two hosts). Never Fable |

## 1. Objective
With `Unification.Enabled = 1` the game contains **no faction-level economic AI, no HQ stockpiles, no anonymous builders, harvesters or carriers, no flags or road segments, no faction trade, no military units, no victory points and no HQ circles**; the NPC population is the sole source of ordinary economic decisions and labour (rule U-1). The same unified mode is honoured by **both** hosts (the player bootstrap and the runner) and survives a save. The Stage 3 game stays compiled and pinned as an **isolated regression scenario**, with every Stage 3 hash unchanged.

## 2. User-facing outcome
The normal "Living Island" start is the unified six-tribe world, god-only (the player is the god), with no automatic faction behaviour; the main menu or a flag still reaches the legacy Stage 3 / M3 game for regression and comparison. No HQ, flag, road-segment or Stage 3 HUD appears in normal play.

## 3. Relevant design documents
Design §1.1 (who may do what: table and rule U-1, U-2, U-3), §1.2 (tribe is affiliation), §13.1 (retained, replaced, removed), §13.2 (switch plan), §13.8 (retiring temporary mechanisms), §14.1 U11, U13, B11, Unif-8, Unif-9; audit §2 (boot path, system orders), §3 (S1–S16 decisions), §9 (victory points), §12 R1, R4, R5, R6, R15, R17; owner brief §11 and §13; D8.16.

## 4. Relevant existing source files (verified)
- Hosts: `Assets/God/Game/Boot/GodBootstrap.cs` (`StartGodOnly` ~351–365; `-godonly` 192–202; `startAi` 208; `SimBuilder.Create(setup, tuning, attachAi: false)` 206; `StartAi` 292–299; Stage 3 views 226–231), `Assets/God/Game/Boot/AiHost.cs` (`StartForGame` 31–35; `-godnoai` 33), `Assets/God/Game/Menu/GameMenu.cs` (121), `Assets/God/Game/Boot/DivineHost.cs`, `Core/SimBuilder.cs` (`AttachAi`, `AttachPolicies` 94–118, `Create`), `Core/GodSim.cs` (`StartGame` 107–116, `InstallModules` 123–135, `StateHashParts` 134–170), `Core/GameSetup.cs` (`Population`, `StartNpcsPerFaction`, `Victory`, `Control`, `HumanGod`), `Core/PlayerControl`.
- Stage 3: `Ai/AiDirector.cs` (28, 94, 102–107 `NonHuman`, 126–147), `Ai/AiBrain.cs` (+ partials), `Ai/AiHost`-side policies `Policy/StandardPolicies.cs` (19–21), `Policy/PolicyRegistry.cs` (118), `Policy/FactionPolicies.cs`, `Economy/EconomyModule.cs` (34, 55, `GiveStartStock`), `Economy/EconomySystem.cs` (200), `Trade/TradeSystem.cs` (350, `AiTradeJudge`), `Military/MilitarySystem.cs` (300, 348), `Factions/FactionRules.cs` (15–21 VP, 247- `StartCorners`, 326 elimination, 367 `FactionSystem` 400), `Factions/FactionTuning.cs` (8–11), `State/Victory.cs` (38), `Territory/TerritoryRules.cs`, `Territory/TerritorySystems.cs` (`DiceSystem` 10, `PileWasted`), `Territory/Faction.cs` (names line 27: Amber, Azure, Crimson, Jade, Violet, Ivory).
- Views: `Assets/God/Game/View/TerritoryView.cs`, `EconomyView.cs`, `MilitaryView.cs`, `Assets/God/Game/Models/NpcLayer.cs`, `DivineFxLayer.cs`.
- Divine layer faction ids: `Divinity/DivinityIds.cs` (`GodOf(faction)` 18), `GodBootstrap.cs:357` (`HumanGod = GodOf(F0)`).
- Scenarios and pins: `DotNet/God.Runner/Scenarios/*.scenario.json` (`watch6`, `sandbox_watch6`, `vp_duel`, `bench_stage3_watch6`, `band_6x20` with "stage 3 AI running"), `DotNet/God.Runner.Tests/PolicyRunTests.cs:27` (Stage 3 smoke hashes seeds 1–3: `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87`), `DotNet/God.Runner/RunEngine.cs`, `SimSession.cs`.
- UN0 seams you use: `GameSetup.Unified`, `Unification.Enabled`, `UnifiedFixtures`, the static guards (`Unified_NoStage3EconomySystem`, `Unif_NoFactionReads`), `StartKind`; UN4 `UnifiedStart` (soft).

## 5. Allowed scope
**UN9a (sim; needs UN0 only):**
1. **Module list under the switch** (rule U-1): with `Unification.Enabled = 1` `GodSim.Create`/`SimBuilder` construct Territory (geometry, dice, topology), Population, Households, Social, Learning, Civ, Orgs, Divinity (if on) and the Unification module; **not** `EconomyModule`/`EconomySystem`, `TradeSystem`, `MilitarySystem`, `AiDirector`, `FactionSystem`, `GodDirector` is kept only for divine AI seats. The `StateHashParts` for the omitted modules are absent exactly as for a game that never had them (hashed only when present). The wiring is by tuning/setup flag in the sim (`SimBuilder` + `GodSim` module slot, flagged edits), **not** by a host flag.
2. **Victory points and elimination**: no VP, no `HqVp`/`CitadelVp`, no `FactionSystem` elimination by HQ loss, no victory evaluation (a unified game is a sandbox: `VictoryMode.SandboxOnly` forced); `Faction` stays as the tribe affiliation id; **no code reads `Faction` to decide an economic act** (static `Unif_NoFactionReads` in its real form, covering `Sim/Unification`, `Sim/Civ` unified branches and the planner).
3. **Territory idle effects removed**: no HQ stock, no HQ, no flag, no road segment object, no start stock, no `PileWasted` noise from unused piles (the dice seam from UN0/UN2 already skips piles); no HQ circle read anywhere in unified mode (`Unified_NoHqCircleReads`, with UN5's `DevelopmentArea`).
4. **Probes (the acceptance guards, real form)**: static `Unified_NoStage3EconomySystem` (scans the built sim's system list), the run-time probe `Unified_EveryIntentHasNpcOrigin` (U13: every economic intent has an NPC actor; no intent from a policy seat of level Faction; `Control` is own, compelled, possessed or avatar only), `NoFlags_NoCarriers` (U11: zero flags, zero road-segment carriers; every goods movement event has an `Npc` mover), `Unified_NoDivineWhenOff` (with `Divinity.Enabled = 0` or an idle god no divine act occurs; the long run uses this).
5. **Policy seat**: the faction seat (`PolicyLevel.Faction`) stays as a seam but is set to `none` in unified mode (`SimBuilder` honours it in both `AttachAi` and `AttachPolicies`; audit R4); `IdleFactionPolicy` is not needed.

**UN9b (hosts and presentation of the choice; needs UN4 for the assembled start):**
6. **`UnifiedBootstrap`** (`Assets/God/Game/Boot/UnifiedBootstrap.cs`, new): builds a god-only game from `GameSetup.Unified`, six tribes, `PlayerControl.GodOnly`, `HumanGod = GodOf(F0)`, `Divinity.Enabled = 1`, `Civ.Enabled = 1`, `Orgs.Enabled = 1`, `Unification.Enabled = 1`, **no AI attach** (`StartAi` not called, `AiHost` not started, `-godnoai` becomes the default and the AI path unreachable), no Stage 3 views (`TerritoryView`, `EconomyView`, `MilitaryView` not constructed; `NpcLayer` and `DivineFxLayer` kept); the unified flag is saved with the tuning so a load honours it in either host (R4, `UN9_LoadHonoursUnified`).
7. **Which start is "normal"**: `GameMenu`'s Living Island / `-godonly` start the unified game; the old god-only (Stage 3 AI + 20 per faction) remains reachable as **"Stage 3 regression"** by flag `-stage3` and a menu entry in a dev section; `-godnoai` retained for the legacy path. The lead confirms the menu wording (default: "Living Island" = unified).
8. **Stage 3 regression scenario (isolated)**: a named scenario set `stage3_regression` (the existing `watch6`, `sandbox_watch6`, `vp_duel`, `bench_stage3_watch6` and the three seed hashes) is kept green with `Unification.Enabled = 0`; documented in `docs/unification/` as the only place Stage 3 runs; no shared mutable state is written by both worlds (the switch selects the world, design §13.8). Population scenarios that implicitly include the Stage 3 AI (`band_6x20`, `civ_assets`, audit R15) are **not** edited: unified equivalents are new files (`unif_*`).
9. **Tribe order and names** (audit §5): code has `F1 = Azure`, `F2 = Crimson`; D8.9 lists Amber, Crimson, Azure, Jade, Violet, Ivory. `TribeOrder` (data table, `Unification/Start/TribeOrder.cs`): unified games list, colour and number tribes in the owner's order **without renaming enum values or touching Stage 3**; the table maps display position to `Faction` id (default: Amber F0, Crimson F2, Azure F1, Jade F3, Violet F4, Ivory F5) and every unified display and report uses it. Alternative (rename `Faction.cs` line 27 names and `FactionInfo` colours) is a lead decision, only if no hash or test depends on the names (check first).
10. **Divine layer**: gods stay addressed by faction id (`GodOf(F0)` etc.) as tribe affiliation (audit R6); a test asserts divine commands and miracles work in a unified game with no HQ.
11. **Retire-list**: the temporary mechanisms of audit S16 and DEV_LOG "Temporary mechanisms" that the unified game must not use are listed with a switch state (immigration off, `IPayPoint` absent) in the "UN9 record"; code removal is **not** in scope (design §13.8: removal only after the owner agrees).

## 6. Explicitly excluded scope
Deleting any Stage 3 code (it stays compiled and pinned); the new economy itself (UN1–UN8); editing the faction AI; changing Stage 3 scenarios, hashes or smoke pins; views of the new world (UN10); military for NPCs (C4/M9, later); renaming enum values; the warehouse (UN11).

## 7. Dependencies
Hard: **UN0**. UN9b hard: UN4 (the assembled unified start: `UnifiedBootstrap` calls it); soft for a meaningful play-through: UN1, UN2, UN3, UN6. Downstream: UN10a (boot), UN11 (long-run scenarios), the UA gate.

## 8. Shared contracts
**Read:** the module and policy APIs, `GameSetup`, `Faction`, UN0 switches. **Modify (flagged, switch-gated edits to shared files; the lead reviews every hunk):** `Core/SimBuilder.cs`, `Core/GodSim.cs` (module construction and hash-part presence only), `Core/GameSetup.cs` (a `Victory` forcing rule), `Game/Boot/GodBootstrap.cs`, `Game/Menu/GameMenu.cs`, `Game/Boot/AiHost.cs`. These edits are permitted for UN9 because UN0 could not know the exact seams (listed in the hand-back); no other shared file may change. **Never** edit Stage 3 rule files, `Goods.cs`, enum files, `SaveFile.cs`.

## 9. Files and modules owned
`Unification/Wiring/UnifiedModules.cs`, `Unification/Wiring/UnifiedGuards.cs`, `Unification/Start/TribeOrder.cs`, `Assets/God/Game/Boot/UnifiedBootstrap.cs`, the flagged edits of section 8, `DotNet/God.Runner/Scenarios/stage3_regression.scenario.json` (a manifest or list; the existing files are unchanged), `DotNet/God.Runner/Scenarios/unif_normal.scenario.json` (the normal game, god-only, six tribes), `Assets/God/Tests/EditMode/Unification/UN9*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN9*.cs`, `docs/unification/STAGE3_REGRESSION.md` (what remains and how to run it).

## 10. Required domain events
None new. Events of the omitted systems (`PileAdded`, `FlagPlaced`, `HqBuilt` and the like) must simply never fire in unified mode (asserted by `Unified_NoStage3Events`).

## 11. Save/load implications
The unified flag lives in tuning (`Unification.Enabled`, already saved); a unified save loads in either host with no AI attached; a pre-UN9 save loads unchanged. Test `UN9_LoadHonoursUnified`, `UN9_OldSaveLoads`, `UN9_SaveLoadParity` (a unified game saved mid-day-3).

## 12. Warehouse implications
None. The warehouse recorder must tolerate the absence of Stage 3 tables' rows (`UN9_Warehouse_NoStage3Rows`: a unified run records zero rows in Stage 3 facts and the registry still builds).

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN9-T1 | `Stage3_Regression_Unchanged` (U-P1) | with `Unification.Enabled = 0` every existing pin holds: the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`, `watch6`, `sandbox_watch6`, `vp_duel`, P1, P4, P8, population smoke; the Stage 3 `AiBrain` still plays on the legacy start |
| UN9-T2 | `Unified_NoStage3EconomySystem` (B11, U13) | the built unified sim's system list contains none of `EconomySystem`, `TradeSystem`, `MilitarySystem`, `AiDirector`, `FactionSystem`; a planted `AiDirector` in a scratch build makes the test fail (mutation) |
| UN9-T3 | `Unified_EveryIntentHasNpcOrigin` (U13) | over 20 days on seeds 1–3, every economic intent and act has an NPC actor; zero intents from a Faction-level policy seat; with `Divinity.Enabled = 0` zero divine acts |
| UN9-T4 | `NoFlags_NoCarriers` (U11) | zero flags, zero road-segment carriers or walkers exist over 20 days; every goods movement event names an `Npc` mover (an NPC-carried delivery between two buildings is observed in the UN3 fixture) |
| UN9-T5 | `Unified_NoHqCircleReads`, `Unif_NoFactionReads` | static: no unified code path reads an HQ circle or `Faction` for an economic decision; a planted read fails the test |
| UN9-T6 | `Unified_NoVp_NoElimination` | a unified game with a tribe losing all buildings or all people is not eliminated by HQ loss, awards no VP, ends no game |
| UN9-T7 | `Unified_NoStage3Events` | zero `PileAdded`, `FlagPlaced`, `HqBuilt`, `Walker*`, trade or military events in 20 days |
| UN9-T8 | `Bootstrap_Unified_BothHosts`, `UN9_LoadHonoursUnified` | the player bootstrap (batch smoke) and the runner build identical sims (same hash at tick 1000) for `unif_normal`; no `StartAi`; saving and loading in either host keeps the unified mode and attaches no AI |
| UN9-T9 | `TribeOrder_Display` | the unified game's tribe listing, colours and reports follow Amber, Crimson, Azure, Jade, Violet, Ivory; the enum ids and Stage 3 names unchanged |
| UN9-T10 | `Divine_Works_Without_Hq` | with `Divinity.Enabled = 1` suggestions, commands, compulsion, possession and a miracle on a unified game execute with no HQ present |
| UN9-T11 | `Stage3_Scenario_Isolated` | running a Stage 3 scenario after a unified run in the same process leaves no shared static state differences (hash equal to a clean process) |
| UN9-T12 | `UN9_OldSaveLoads`, `UN9_SaveLoadParity`, `UN9_Warehouse_NoStage3Rows` | as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN9|FullyQualifiedName~PolicyRun|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_normal --seed 3 --days 20 --record full --sqlite --out <scratch>\un9
dotnet run --project DotNet/God.Runner -c Release -- verify --scenario watch6 --seed 1 --ticks 12000 --record-matrix
```
The first run lists the system list and the count of intents by origin; the last proves Stage 3 is unchanged.

## 15. Unity demonstration
Required: after `check-game-compile.ps1` (0 errors) open the unified game from the menu in batch/no-graphics smoke (`SmokeRun`), confirm it starts with no AI log lines and no Stage 3 view objects, then close the editor. A windowed run is the owner's (never in automation).

## 16. Performance considerations
Removing the Stage 3 systems should raise ticks/s: report `bench_npc_120` unified versus legacy; no new per-tick cost (the module list is fixed at start).

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un9 -b unity/un9 unity/poc` (after UN0 merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un9\Library /E /MT:16` (0–7 = success; source editor closed). UN9b continues on `unity/un9b` cut from `unity/poc` after UN4 merged.
2. Sim and EditMode tests first; the bootstrap last.
3. Before hand-back `git merge unity/poc`; conflicts in the flagged shared files mean stop and report.
4. Neutrality pins that must hold: **P1, P4, P8**, U-P1, `Stage3_Regression_Unchanged`, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`, `GameRulesRunTests`.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run; then the batch smoke of section 15 if you hold the Unity token.
7. "UN9 record" in `docs/stage4/DEV_LOG.md` on the branch (system list, the menu decision, the tribe-order decision, the retire-list). Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- **Hidden readers of the removed state** (audit §14: not run): telemetry, KPIs, HUD, the Q-package harness and the divine layer may read `EconomyState`, `Hq` or VP: the first unified run may throw or show zeros; find them by test, not by guess. Anything that cannot be switched cleanly is escalated.
- **Two hosts** (audit R4): the player path bypasses `PolicyAssignment`; the unified flag must be in sim state/tuning so neither host can attach the AI by accident.
- **Dice and Territory still exist** (geometry, dice, `Piles` idle): do not remove `TerritoryModule`; assert the idle structures are inert.
- Changing the default menu start changes the owner's visible game: confirm the wording with the lead before merging UN9b.
- The `TribeOrder` choice (enum rename versus display table) is owner-visible: default is the display table.
- 40 existing scenarios implicitly include Stage 3: none may change.

## 19. Definition of done
UN9-T1…T12 green; every Stage 3 and base pin unchanged; compile check 0 errors; the batch smoke clean; "UN9 record" lists the shared-file hunks, what ran, the decisions (menu, tribe order) and the retire-list; UN9a handed back before UN9b; handed back.

## 20. Smaller checkpoints
- **UN9a** (≈2 h): module list, VP and elimination, idle effects, probes T1–T7, T10, T11.
- **UN9b** (≈2 h): bootstrap, menu, regression manifest, tribe order, save, T8, T9, T12, Unity smoke.

## 21. Recommended agent
**Opus.**

## 22. Used later by industry (keep the interfaces general)
Industrial scenarios (`ind_*`) are unified games with more switches; the module list and probes here are the model for `Industry.Enabled` composition. Keep the retire-list current: Stage 3 code removal is a later owner-approved step.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file outside the flagged hunks or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN9 record".
