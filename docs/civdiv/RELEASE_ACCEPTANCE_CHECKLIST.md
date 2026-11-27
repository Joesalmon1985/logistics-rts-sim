# Release acceptance checklist: Civilisation, Divinity and Industry run (27 Nov 2026)

The checklist the lead (Q4) and Q3 fill in at every playable checkpoint **A–G** and at the final release. It lists what must pass at each checkpoint, maps every required end-to-end scenario (owner brief §47, 1–18), every owner-D6 acceptance row (ACCEPTANCE D6.1–D6.10) and every vertical-slice step (D6.11, VS1–VS10, plus VS11 after G) to its tier, package, test and checkpoint, and keeps every outstanding Stage 4 acceptance test as a **retained gate** (brief §45: none is silently removed or weakened).

Binding sources: [OWNER_BRIEF_2026-11-26.md](OWNER_BRIEF_2026-11-26.md) §44–§48, [OWNER_BRIEF_2026-11-27_INDUSTRY.md](OWNER_BRIEF_2026-11-27_INDUSTRY.md), [OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md), [SHARED_ARCHITECTURAL_CONTRACTS.md](SHARED_ARCHITECTURAL_CONTRACTS.md) (§16 pins P1–P9, §17 save, §22.8 pins P10–P13, §22.10 required tests), [LEAD_RULINGS.md](LEAD_RULINGS.md), [DEVELOPMENT_PACKAGE_INDEX.md](DEVELOPMENT_PACKAGE_INDEX.md) (§4 Checkpoint G), [DEVELOPMENT_DEPENDENCY_GRAPH.md](DEVELOPMENT_DEPENDENCY_GRAPH.md) (§8 final window, §9 gates, §10 checkpoints), the package specs in [DEVELOPMENT_PACKAGES/](DEVELOPMENT_PACKAGES/), and [../stage4/ACCEPTANCE_TESTS.md](../stage4/ACCEPTANCE_TESTS.md) (the Stage 4 gates and the D6 section). Q3 owns the D5 and D6 status sections of ACCEPTANCE_TESTS; this checklist is the release view of the same facts and must agree with them.

---

## 0. How to use this checklist

**Status vocabulary** (brief §45; one value per row, with the evidence in the "Evidence" or sign-off log):

| Status | Meaning |
|---|---|
| **passed** | the named test or run executed on the stated commit and met its criterion as written |
| **failed** | it executed and did not meet its criterion (the criterion is kept; nothing is loosened to pass it) |
| **revised** | the numerical expectation was changed with evidence under owner D3 §5 (original target, observation, revised target and rationale recorded; the capability is unchanged) |
| **deferred** | moved to a named later milestone or tier by an owner or lead decision; kept in the design |
| **unimplemented** | the capability does not exist yet (includes *not started* packages) |
| **demonstrated** | shown working in the running game or a headless run, with the recipe and evidence recorded, beyond the automated test |
| **known limitation** | the behaviour exists and is honest, but a stated limitation applies (recorded, not hidden) |
| *not started* | working state used below for planned rows: no package work has produced the test yet (a form of **unimplemented**) |
| *partial* | some clauses pass, others are failed or unimplemented; each clause is listed |
| *not run* | the test exists but did not execute at this checkpoint (for example its package has not merged): **never counted as passed** (ACCEPTANCE D6.11 gate) |

**Rules.** A scenario or step is never claimed because supporting classes exist (brief §47). Every claim cites a run: command, commit, date, result, who ran it (Q4-T9). A later failure never invalidates the last working build (tags plus build copies under `Builds/`, never committed). Numerical targets may be revised with evidence; required capabilities never disappear.

**Common commands** (from the worktree root; Unity runs at checkpoints in `wt-lrs-ckpt` checked out at the checkpoint tag with a warm Library, graph doc §4):
```
dotnet test DotNet -c Release                                                     # full suites (~20 min)
dotnet test DotNet -c Release --filter "FullyQualifiedName~Neutrality"            # pins
dotnet test DotNet -c Release --filter "FullyQualifiedName~PolicyRun"             # stage 3 hash 4bd54b271857a803
dotnet test DotNet -c Release --filter "FullyQualifiedName~Integration"           # Q3 scenarios, replay, save/load, P6
powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1   # must print 0 errors before any Unity run
unity command --project-path . test run --mode editor --async-tests true          # EditMode (poll Temp/pipeline_test_status.json)
unity command --project-path . test run --mode play --async-tests true            # PlayMode
unity command --project-path . script run --file AgentScripts/BuildWindows.cs --entry BuildWindows.Run   # Windows build
Builds\Windows\GodOfTheIsland.exe -batchmode -nographics -godsmoke 60000 -godseed 2                    # stage 3 smoke
Builds\Windows\GodOfTheIsland.exe -batchmode -nographics -godsmoke 60000 -godseed 2 -godpopulation     # population smoke
Builds\Windows\GodOfTheIsland.exe -batchmode -nographics -godsmoke 60000 -godseed 2 -godonly -godpopulation   # god-mode smoke (R29, U1a flag)
dotnet run --project DotNet/God.Runner -c Release -- verify --scenario <S> --seed 1 --ticks 12000 --save-mid --parallel --record-matrix
dotnet run --project DotNet/God.Runner -c Release -- verify --against-unity <replay file>                  # Unity/runner parity (H-GS-3/12)
```

**Reference hashes.** Stage 3 player smoke and `PolicyRunTests`: **`4bd54b271857a803`** (Crimson wins at tick 20161; unchanged since M0; must hold at every merge). Population smoke: **`60d549c28635cc44`** on the pre-F0 base (26 Nov); F0 records the replacing hash (F0-T8), and the Checkpoint B merge re-pins it deliberately when `Divinity.Enabled` flips on for population games. God-mode smoke: hash first recorded at Checkpoint B.

---

## 1. Standing gate at every merge (graph doc §9; contracts §0.2, §16–18, §22.8)

| # | Gate | Applies | Status |
|---|---|---|---|
| S1 | Package hand-back: branch merged current `unity/poc`; own tests run and reported (what ran, what did not); "<ID> record" in DEV_LOG | every merge | per merge |
| S2 | `check-game-compile.ps1` → **0 errors** on the merge result | every merge | per merge |
| S3 | `dotnet test DotNet -c Release` green on the merge result (≤ 2 disjoint packages per run) | every merge | per merge |
| S4 | Pins: P1, P4, P8 always; P2, P3 after D1; P5 after C6; P9 after D7; **P10–P13 after I1a**; stage 3 hash `4bd54b271857a803`; pinned scenarios run with `Industry.Enabled = 0`, `Civ.Knowledge.Enabled = 0`, `Civ.Money.Enabled = 0` | every merge | per merge |
| S5 | The package's reconciliation test (event counts = fact rows) and `*_SaveLoadParity` | that package | per merge |
| S6 | Frozen-file diff reviewed by the lead | F1a, F1b, I1a only (any other frozen-file edit is refused) | per merge |
| S7 | P6 `Switch_Matrix` by Q3 within an hour (extended with `Industry.Enabled`, `Civ.Knowledge.Enabled`, `Civ.Money.Enabled` after I1a); a failure reverts the merge | every merge after D1 | per merge |
| S8 | §22.10 static tests (after I1a): `Industry_NoMoneyApi`, `Grid_NoMoneyApi`, `NoExternalBuyer`, `MoneyWriters_OnlyK2`, `Industry_NoActorZeroGoodsCalls`, `Industry_NoOwnershipModelBranch`, `Rights_NeverGate`, `Vehicle_NoNpcRecord`, `MoveBin_OnlyFromIndustryHooks` | every merge after I1a | per merge |

## 2. Neutrality pins (contracts §16, §22.8)

| Pin | Statement (short) | Landed by | Re-run by | Status |
|---|---|---|---|---|
| P1 `Divinity_Off_Reproduces_Base` | all D5 switches 0: every part hash of the P1 set (band_6x20, m4_families, ibs_families, watch6, demog_*; seeds 1–3; every checkpoint) equals the F0 base | F1 | every merge | not started |
| P2 `Divinity_Idle_Is_Neutral` | Divinity on, idle gods: every part except `divine` equals P1 | D1 | every merge after D1 | not started |
| P3 `Divinity_Disabled_Mode_Neutral` | Disabled mode, scripted god refused daily: every part except `divine`, `pending` equals P1 | D1 | every merge after D1 | not started |
| P4 `Orgs_Off_Reproduces_Base` | as P1 | F1 | every merge | not started |
| P5 `Orgs_NoEmergence_Neutral` | Orgs on, no emergence, no seeds: every part except `organisations` equals P4 | C6 | every merge after C6 | not started |
| P6 `Switch_Matrix` | each suite with other switches 0 equals its branch tip; pairwise; all on (extended with the three D6 switches) | Q3 | after every merge | not started |
| P7 `Recording_Neutral_On_New_Domains` | Off/Summary/Full and each new domain alone give one hash (extended to domains 70–79, W-D6-5) | Q1 | checkpoints | not started |
| P8 `Civ_Off_Reproduces_Base` | as P1 | F1 | every merge | not started |
| P9 `Archive_Neutral` | Divinity on, `Resurrection.Archive = All`, idle: every part except `divine` equals P1 | D7 | every merge after D7 | not started |
| P10 `Industry_Off_Reproduces_Base` | every D5 switch and the three D6 switches 0: every part equals the base (the I1a widening is neutral) | I1a | every I/K merge | not started |
| P11 `Industry_Idle_Is_Neutral` | Industry on, no deposits, generation off, waste off, no assets: every part except `industry` equals P10 | I1a (stub) | I1, I2, I4 with their systems | not started |
| P12 `Knowledge_Idle_Is_Neutral` | Civ and Knowledge on, empty designs: every part except `civ` equals Knowledge off | I1a (stub) | K1 | not started |
| P13 `Money_Idle_Is_Neutral` | Civ and Money on, `RecordPersonSales = 0`, no seed: every part except `civ` equals Money off | I1a (stub) | K2a | not started |

## 3. Release gates per checkpoint

Each checkpoint is independently demonstrable (brief §44). "Packages" are those the graph doc §10 requires; a checkpoint whose package set is incomplete is released with the missing parts listed as *not run* or *unimplemented*, never as passed. Save versions follow contracts §17 and §22.8 (the lead bumps in the checkpoint merge commit; older versions refused).

### Checkpoint A — Living world (F0; U3a if ready) · tag `civdiv-a` · planned H3.5

| Gate | What must pass | Command / evidence | Status |
|---|---|---|---|
| A1 | Merge base: `unity/ibs` + `unity/demog` into `unity/int0`, fast-forwarded to `unity/poc` (F0) | DEV_LOG "F0 record" | not started (F0 in progress) |
| A2 | Full suites green (God.Sim, God.Runner) | `dotnet test DotNet -c Release` | not started |
| A3 | Pins taken on the base (P1–P9 base hashes recorded for later comparison); M4/M5 neutrality pins (Households off, Social off) | `--filter Neutrality` | not started |
| A4 | Compile check 0 errors; Unity console clean; EditMode and PlayMode green | compile script; EditMode; PlayMode (batch, `wt-lrs-ckpt`) | not started |
| A5 | Windows build 0 errors | BuildWindows | not started |
| A6 | Stage 3 smoke seed 2: Crimson wins, hash `4bd54b271857a803` | stage 3 smoke | not started |
| A7 | Population smoke seed 2, 60 000 ticks: exit 0; new hash recorded (replaces `60d549c28635cc44`) | population smoke | not started |
| A8 | Scenario 1 (short-horizon), scenario 2 | §4 rows 1, 2 | 1 *partial*; 2 passed (M5) |
| A9 | Retained gates re-stated with current status (section 6) | this file §6 | not started |
| A10 | Save version 4 (unchanged at A); F1 sets 5 after A | `SaveFile.CurrentVersion` | not started |
| A11 | F2 experiment reports (text) attached | F2 record | not started |

### Checkpoint B — Divine sandbox (F1, D1, D2, D3, U1a, U2a, U3a; Q1 Tier 1 views; Q3 harness) · tag `civdiv-b` · planned H13.5

| Gate | What must pass | Command / evidence | Status |
|---|---|---|---|
| B1 | All B packages merged with S1–S7 green | merge log | **passed on `unity/ckptb`** (F1, D1, D2, D3, U1a, U2a, U3a merged; C3a, C6, U3b, F2b too; full suite: DEV_LOG "Checkpoint B record"); D6 not in this tree |
| B2 | Pins P1, P2, P3, P4, P8 green; stage 3 hash | pins | **passed** with a deliberate re-pin: P1/P4/P8 digests re-pinned for every population scenario (R60 ConceptionMode 1; old -> new in DEV_LOG); P2, P3, both-off pins and `4bd54b271857a803` unchanged |
| B3 | **Deliberate re-pin** (declared point): F2's code merged; `Divinity.Enabled` on for population and Living Island games; population smoke hash and god-mode scenarios re-pinned with a DEV_LOG row | DEV_LOG re-pin row | **partly**: F2 re-pin done (ConceptionMode 1, R60; P1/P4/P8, F2-T1, F2b-T1 re-pinned with DEV_LOG rows); population smoke `c1df5d7fd207e5bc`, god-mode smoke `84c077ef42b4982c`, d1/d2/d3 demo hashes recorded. `Divinity.Enabled` is on for god-only (Living Island) games only; `-godpopulation` games still off (not flipped) |
| B4 | Compile check; EditMode; PlayMode (god-only gating, Mythic meter, instruct and miracle flows, H-UI hooks of U1a/U2a, U3a grid hidden) | Unity in `wt-lrs-ckpt` | **passed with one fix**: compile 0 errors; console 0 errors; EditMode 629/629; PlayMode 114/116, the two failures were wall-clock-dependent U3b hash tests (fixed, class 6/6 twice); full PlayMode not rerun after the fix |
| B5 | Windows build; stage 3 smoke `4bd54b271857a803`; population smoke (new pinned hash); **god-mode smoke** `-godonly -godpopulation` exit 0, invariants, Mythic identity (Q4-T6) | smokes | **passed**: build OK (`Builds\civdiv-b`); stage 3 `4bd54b271857a803`; population `c1df5d7fd207e5bc`; god mode `84c077ef42b4982c`; all exit 0, invariants ok, 0 exceptions. Mythic identity (Q4-T6) not checked separately |
| B6 | Scenarios 8, 9, 12, 13 (Tier 1 part), 18 (replay check at B) | §4 | **partly**: scenario 8 (`d2_command`: 4 instructions, 3 followed, 1 ignored) and 9 (2 compulsions: 1 completed, 1 blocked) demonstrated; `d3_miracles` 7 miracles with ledger; 12, 13, 18 not run |
| B7 | Q1 `0026` early views; P7 on the divine domains | Q1 record | not run at the checkpoint |
| B8 | Save version → **6** | lead merge commit | **done**: `SaveFile.CurrentVersion = 6` (`36cc3fd`, needed by R60: v5 saves refused) |

### Checkpoint C — Working economy (C1, + C3a) · tag `civdiv-c` · with B, planned H13.5

| Gate | What must pass | Command / evidence | Status |
|---|---|---|---|
| C1g | C1 (and C3a) merged with S1–S7; C1-T1…T12 green | C1 record | not started |
| C2g | Pins P1, P4, P8 (P2, P3); stage 3 hash | pins | not started |
| C3g | Compile check (person card Economy data); EditMode | Unity (shared with the B run) | not started |
| C4g | Scenario 2 re-checked with `Civ.Enabled = 1` (C1-T9); scenario 3 (C3a part); `civ_assets` (C1-T10) report | §4 | not started |
| C5g | Save version → **7** | lead | not started |

### Checkpoint D — Personal divine interaction (D4a, D4b, U1b, U2b, U3b) · tag `civdiv-d` · final build H18

| Gate | What must pass | Command / evidence | Status |
|---|---|---|---|
| D1g | D packages merged with S1–S7 | merge log | not started |
| D2g | Pins (P1–P4, P8; P5 if C6 merged); control-seam tests (contracts §11.5) | pins | not started |
| D3g | Compile check; EditMode; PlayMode (embodied cameras, possession pill, avatar prompts, gesture clips M5.19) | Unity | not started |
| D4g | Scenarios 10, 11; replay with a possession and an avatar walk (scenario 18 element) | §4 | not started |
| D5g | Save version → **8** | lead | not started |

### Checkpoint E — Institutional society (C6, C7a, C3b; C2, D7, C10, U5 in the continuation) · tag `civdiv-e` · final build H18

| Gate | What must pass | Command / evidence | Status |
|---|---|---|---|
| E1g | E packages merged with S1–S7 | merge log | not started |
| E2g | Pins P1–P5, P8 (P9 when D7) | pins | not started |
| E3g | Compile check; EditMode; PlayMode (institutional cards, record vs observation, U5 when merged) | Unity | not started |
| E4g | Scenarios 3 (C3b), 4, 5 (and 6, 7, 14, 15, 16 as their packages merge, possibly in the continuation) | §4 | not started |
| E5g | Save version → **9** | lead | not started |

### Checkpoint F — Integrated civilisation and divinity (everything merged by the freeze) · tag `civdiv-f` · H18

| Gate | What must pass | Command / evidence | Status |
|---|---|---|---|
| F1g | Freeze at H15.0; only merges whose gate passes by H15.5 (graph doc §7–§8) | merge log | not started |
| F2g | Compile check 0 errors; full `dotnet test`; every landed pin (P1–P13 as present); stage 3 hash; Q3 replay and save/load on `civdiv_*` (Q4-T1, Q4-T7) | H15.5–H16.0 | not started |
| F3g | Q1 benchmark slot 2 (`bench_divine_500`, `bench_org_500`; quiet machine) | H15.0–H15.5 | not started |
| F4g | Unity EditMode and PlayMode on the frozen head; `Unity_Agrees_With_Runner` (H-GS-3) | H15.5–H17.0 | not started |
| F5g | Windows build; stage 3, population and god-mode smokes (Q4-T3…T6) | H17.0–H17.5 | not started |
| F6g | Scenario status s01–s18 from Q3 (every row of §4 with a status from the vocabulary; Tier 1 rows passed or explained) | §4 | not started |
| F7g | Checkpoint tags `civdiv-a` … `civdiv-f` on commits whose suites passed; build copies `Builds\civdiv-a\` … launch in batch (Q4-T8) | tags, copies | not started |
| F8g | Save version → **10** (also covers I1a if I1a merged before the freeze, contracts §22.8) | lead | not started |
| F9g | DEV_LOG, STAGE_4_HANDOVER §1–2, ACCEPTANCE status written from what actually ran; the continuation list (graph doc §7 H15 row); retained gates (§6) restated (Q4-T9) | handover | not started |

### Checkpoint G — Industrial slice (owner D6.18) (I1a, I1, I2, I4, I6 minimal, Q3 `ind_slice`; + K1, K2a, I3, C2, C3 if merged) · tag `civdiv-g` (proposed) · continuation, ~H25

| Gate | What must pass | Command / evidence | Status |
|---|---|---|---|
| G1 | I1a merged after the Tier 1 set with the lead's frozen-diff review; I1-a, I2-a, I4-a, I6-a merged with S1–S8 | merge log | not started |
| G2 | Pins P1–P13 (as landed); P11 re-run with I1, I2 and I4 systems live; P6 extended with the three D6 switches; P7 on domains 70–79; stage 3 hash | pins | not started |
| G3 | §22.10 static tests with real code (S8); IND-I1…I9, VEH-I1…I7, ELE-I1…I7 invariants in the slice run | `--filter "I1|I2|I4|Integration"` | not started |
| G4 | **`IndustrialSlice_EndToEnd`** (ACCEPTANCE D6.11): VS1–VS10 in one run on `ind_slice`, seeds 1–3, Full recording; a step whose package has not merged is reported *not run* with the package named | `dotnet test --filter IndustrialSlice`; `run --scenario ind_slice --seed N --days 10 --record full --sqlite` | not started |
| G5 | H-GS-11 (`ind_slice` reaches VS1–VS9 within 10 days, VS10 reconciles), **H-GS-12** (Unity and runner equal hashes at days 1, 3, 10), **H-GS-13** (Iron Coast and Living Island run the same system list) | Q3, I6 | not started |
| G6 | D6.10 replay: two runs, replay from log, save at day 7 + load + continue give equal hashes (incl. `industry`, `civ`) and identical warehouse content (W-D6-2); W-D6-1 nine-category reconciliation; W-D6-5, W-D6-6, W-D6-7 | `verify --scenario ind_slice --save-mid --record-matrix`; `replay … --record full` | not started |
| G7 | Compile check; Unity EditMode; PlayMode `Industry*` (H-UI-13…17, H-VW-11…15) | Unity in `wt-lrs-ckpt` | not started |
| G8 | Windows build; stage 3 smoke `4bd54b271857a803`; population and god-mode smokes unchanged by industry being off | smokes | not started |
| G9 | Demo recipe recorded (index §4 "Demo"; I6 §15): harvester with visible ore, coal to the station, lit factory bay building a harvester, a named worker boards it, cards and overlay, a link cut by a compelled lineman (or the coal running out), outage glyphs and stalled queue, recovery | I6 record, lead | not started |
| G10 | Save version → **11** (contracts §22.8: G → 11 if I1a merged after the freeze; it then also covers I1–I6, K1, K2) | lead | not started |
| G11 | G never displaces A–F (D6.18): A–F gates unchanged on the G head | rerun §3 A–F pins and smokes | not started |

---

## 4. Required end-to-end scenarios (owner brief §47, 1–18)

Test names are Q3's (`DotNet/God.Runner.Tests/Integration/Scenario<NN>Tests.cs`, scenario files `DotNet/God.Runner/Scenarios/civdiv_sNN.scenario.json`; Q3 spec §5, §13). Tiers and views from GAMEPLAY_SCENARIOS §8; packages from Q3's scenario map. The Unity demo recipe column satisfies H-GS-10.

| # | Scenario | Tier | Packages | Test / command | Checkpoint | Unity demo recipe | Status | Evidence / note |
|---|---|---|---|---|---|---|---|---|
| 1 | Autonomous population: survive, learn, reproduce, age, form households, no divine act | 1 | F0, F2 | `Scenario01Tests`; `run --scenario civdiv_s01 --years N`; M4.37 favourable ecology | A | Living Island autonomous observation; KPI births/deaths; family tree | ***partial*** | learning (M3), life cycle, households and barter met; **M4.5/M4.6 failed: every default seed goes extinct**; viability depends on F2 (M4.37 not started). Demonstrated only over a short horizon until F2 finds a viable ecology (Q3 risk) |
| 2 | Trade: communicate, agree, physically transfer real goods | 1 | F0 (M5); re-checked with C1 | `M5BarterTests` (honest pair, multi-unit, defection); `Scenario02Tests`; C1-T9 with `Civ.Enabled = 1` | A (C) | two neighbours gesture request/offer/accept; goods pass hand to hand | **passed** (M5) | M5.8 met: 80 of 82 accepted deals completed over 20 runs; M5.15, M5.16 met (26 Nov, `unity/m5`, lead). Re-check with Civ on at C is *not started* |
| 3 | Employment: operate a productive asset and compensate another | 2 | C1, C3 | `Scenario03Tests` | C (C3b at E) | worker at a bench, owner pays goods; Economy tab | not started | |
| 4 | Independent institution founded by NPCs | 2 | C6 | `Scenario04Tests` (statistical; fixed seeds) | E | a cooperative or communal store appears; "institution formed" alert | not started | |
| 5 | Healthcare: request, treatment by another NPC, improved health | 2 | C6, C7a | `Scenario05Tests` | E | request gesture, healer arrives, health ring rises | not started | |
| 6 | Education: one teaches another, capability changes measurably | 2 | C7b | `Scenario06Tests` | E | teaching animation; Mind tab shows the new association | not started | C7b may fall to the continuation (cut ladder rung 8) |
| 7 | Lending: lent, used, repaid or defaulted | 2 | C1, C2 | `Scenario07Tests` | C/E | loan hand-over; debt in both Economy tabs; repayment or default alert | not started | C2 is continuation in the plan |
| 8 | Divine command: perceived, independent response | 1 | D1, D2 | `Scenario08Tests` (a perceived instruction and `CommandResponded` of either outcome across seeds) | B | gold beam and chevron; follow or refuse; Divine tab reasons | not started | |
| 9 | Compulsion of a valid physical action, paid in Mythic | 1 | D1, D2 | `Scenario09Tests` (a compelled take walks first; `ItemMoved` reach-checked) | B | yoke and tether; Mythic spend; impossible request shows its reason | not started | |
| 10 | Possession: control, act, return the same individual | 2 | D4a | `Scenario10Tests` | D | rim and glyph; third-person camera; release; life history mark | not started | |
| 11 | Avatar: manifest, walk, interact, return | 2 | D4b | `Scenario11Tests` | D | column of light; prompts; dismiss | not started | |
| 12 | Miracle: heal or smite with genuine state change | 1 | D3 | `Scenario12Tests` (state before/after with `MiracleEffectApplied`) | B | spiral or bolt; card and world state change; ledger rows | not started | |
| 13 | Commandment revealed, learned, communicated, influencing without universal enforcement | 1, 2 | package D6 | `Scenario13Tests` | B (builder), E/F (spread) | glyph tablet; known and followed shares; visible non-compliance | not started | |
| 14 | Resurrection: same identity and memories; property stays | 2 | D7 | `Scenario14Tests` (same NpcId, archived store byte-equal, estate unchanged) | E/F | memorial; reborn pose; estate unchanged in the panel | not started | D7 is the first rung of the D5 cut ladder |
| 15 | Government refuses or modifies a divine instruction | 2 | C6, D2, C10 | `Scenario15Tests` | E/F | institution tray shows "refused"/"modified" | not started | C10 continuation |
| 16 | Religious affiliation changes without citizenship changing | 2 | package D6, C10 | `Scenario16Tests` (affiliation changes, citizenship spell unchanged) | E/F | Faith tab vs citizenship line | not started | |
| 17 | Analytical reconciliation: warehouse records reconcile to sim events | 1 | Q1 + every merged package | `Reconciliation_AnalyticalEqualsSim` (W-D5-1); each package's `*_Warehouse_Reconciles`; W-D6-1 for industry | F | Analysis panel reconciliation view, zero difference | ***partial*** | M3–M5 reconciliation exists and passes: M3.29, M4.35, M5.17 met (`PopulationWarehouseTests`, `M4WarehouseTests`, `M5WarehouseTests`); D5 and D6 tables not built |
| 18 | Replay: a full scenario with NPC actions and divine inputs reproduces exactly | 1 | everything merged | `Scenario18_Replay_Exact` (`Replay_DivineScenario_Exact`, H-D5-1); `verify FILE`; `replay FILE --record full` | F (checks at A, B) | playback with markers; final hash equals recorded | not started | ordinary-action replay exists (`ReplayTests`, `DeterminismTests`); the divine replay is Q3's |

Additional D5 integration gates (Q3 spec §13): `Replay_SwitchGod`, H-D5-5 `Save_Load_With_Divine_State`, H-D5-7 `Conservation_With_Miracles_And_Resurrection`, W-D5-7 `Control_Source_Marked`, H-GS-3 `Unity_Agrees_With_Runner` — all **not started**, checked at F.

---

## 5. Owner D6 acceptance (ACCEPTANCE_TESTS "D6")

All rows Tier 2 unless marked; none needs prehistoric development (D6.17); they run on fixtures or `ind_*` scenarios, seeds 1–3. "Money unchanged" is ACCEPTANCE D6's definition (every `Payment`, `OrgBook` and account row, MON-02 and every money-ware holding not moved by a payment byte-equal). Scenario name: **`ind_slice` is canonical** (contracts §22.11 #40; ACCEPTANCE still says `industrial_slice`).

### 5.1 The brief §12 demonstrations, D6.1–D6.10

| # | Demonstration | Tier | Packages | Tests | Checkpoint | Status |
|---|---|---|---|---|---|---|
| D6.1 | Extracting and transporting oil without creating money | 2 (after the slice) | I1-b, I2-b, K2a | `ACC_OilExtractedNoMoney` (I1-T15), `ACC_OilTransportedNoMoney` (I2-T18), money half K2-T9; scenario `ind_oil` | after G | not started |
| D6.2 | Selling resources through actual payment and ownership transfer | 2 | I3, K2a (C2) | `ACC_DeliveryUnderAgreement` (I3-T3), `VS4_OreSold` (I3-T4, K2-T10), variant without payment (I3-T5) | G (optional branch) | not started |
| D6.3 | Supplying electricity without creating money | 2 | I4 (I4-b for billing), K2a, C2, I3 | `ACC_ElectricityNoMoney` (I4-T8 part, I4-T15 full, K2-T11), MON-U3 | G (part), after G (billing) | not started |
| D6.4 | Fuel shortages affecting electricity generation | 2 | I4, I1 | `ACC_FuelShortageCutsGeneration` (I4-T12); scenario `ind_grid` | G | not started |
| D6.5 | Copying knowledge without removing the original knowledge | 2 | K1-b | KN-U1 (K1-T7), KN-U2, KN-U3 (K1-T8) | after G (K1-b) | not started |
| D6.6 | Transferring a patent without duplicating physical assets | 2 | K1-b, K2a | KN-U4 (K1-T9) | after G | not started |
| D6.7 | Purchasing a licence and constructing an asset | 2 | K1-b, K2a, I1 | KN-U5 (K1-T10), `ACC_LicensedConstruction` (I1-T18) | after G | not started |
| D6.8 | Unauthorised use of a design creating a potential legal dispute | 2 | K1-b, I1 (C9 stub) | KN-U6 (K1-T11), `Rights_NeverGate` (K1-T1); complaint end point is a **stub** until C9 (known limitation) | after G | not started |
| D6.9 | Waste being generated, collected and reused | 2 (after the slice) | I1-b (waste part), I2-b (collector, optional), K2a (fees) | `ACC_WasteGeneratedCollectedReused` (I1-T17), `Waste_LedgerExact`; scenario `ind_waste` | after G | not started |
| D6.10 | Deterministic replay of industrial and technological activity | 2 | Q3 with I1–I4, K1, K2 | `Vehicle_ReplayExact` (I2-T16), `Grid_ReplayExact` (I4-T13), K1/K2 parts, W-D6-2 `Replay_ReRecord_Industry`; 15-day `ind_slice` run with save at day 7 | G (slice parts), after G (full) | not started |

### 5.2 The vertical slice, D6.11 `IndustrialSlice_EndToEnd` (VS1–VS10, one run, `ind_slice`, seeds 1–3, Full)

| Step | Criterion (short) | Packages | Per-step tests | Checkpoint | Status |
|---|---|---|---|---|---|
| VS1 | A persistent NPC with a contract boards and drives the harvester; every move names that NpcId; needs kept; skill meets the minimum | I2 (C3, K1 if merged) | `VS1_NpcOperatesHarvester` (I2-T10), K1-T5 | G | not started |
| VS2 | Cargo bin gains `Extracted` iron ore; deposit falls by exactly the loaded count | I2, I1 | `VS2_HarvesterCollectsOre` (I2-T11) | G | not started |
| VS3 | `ItemMoved(BinTransfer)` into the smelter input bin by the operator within reach; money unchanged | I2 (K2a for the money check) | `VS3_DeliversToSmelter` (I2-T12) | G | not started |
| VS4 | Processed by recipe 25 (steel owned by the smelter org) **or** sold with ownership transfer and an actual `PaymentMade`, linked by `IndustrialSaleClosed` | I1 (processed branch suffices); I3 + K2a + C2 (sale branch) | `VS4_OreProcessed` (I1-T11); `VS4_OreSold` (I3-T4) | G | not started |
| VS5 | Coal delivered into the station fuel bin is burned (`Combusted` = generated × rate); ash appears | I2, I4 | `VS5_CoalDelivered` (I2-T13), `VS5_StationBurnsDeliveredCoal` (I4-T9) | G | not started |
| VS6 | The factory's port is in the station's component with `PowerPermille` 1000; delivered eu recorded; a disconnected factory gets 0; money unchanged | I4 | `VS6_PowerReachesFactory` (I4-T10) | G | not started |
| VS7 | Recipe 32 completes with materials, minimum workers meeting the design minima (or a document), power ≥ minimum; a harvester asset exists with embodied matter = inputs; licence classified, not checked | I1, I4 (K1) | `VS7_FactoryBuildsVehicle` (I1-T12), K1-T5 | G | not started |
| VS8 | An existing NPC boards and drives the new vehicle; no NpcId created; first trip burns its fuel | I2 (K1) | `VS8_NewVehicleOperated` (I2-T14) | G | not started |
| VS9 | (a) depot fuel emptied → harvester `OutOfFuel`, smelter `MissingInputs`; (b) station fuel exhausted → outage, recipe 32 `NoPower`; (c) link opened by a staff act → factory cut off; each recovers | I1, I2, I4 | `VS9_FuelCutStopsHarvester` (I2-T15), `VS9_FuelOrLinkCut` (I4-T11) | G | not started |
| VS10 | W-D6-1 nine-category reconciliation (MON-10 = 0), IND/ELE/vehicle reconciliation; replay and save-and-continue reproduce every checkpoint hash and the warehouse | Q3, I1–I6, K1, K2 | `VS10_IndustryReconciles`, `VS10_EnergyReconciles`, `Reconciliation_NineCategories` | G | not started |
| VS11 | (after G) The factory that built the harvester builds a tank for a military organisation from the same input bin; two employed crew drive it | I5 | `VS11_TankFromSameFactory` (I5-T7) | after G | not started |

**Gate (D6.11).** Passes only when every step passes on all three seeds in one run each. A step that cannot run because its package has not merged is reported **not run** with the package named.

---

## 6. Retained gates: outstanding Stage 4 acceptance tests (brief §45)

These stay open until they pass, are revised with evidence, or are deferred by a decision; the run must not remove or weaken them. Sources: ACCEPTANCE_TESTS M3 "status after D3", M4 "acceptance status", M5 "status as built", CURRENT_IMPLEMENTATION_BASELINE §4.1 and the I-BS table. Re-check at Checkpoint A (F0 base) and restate at F (handover).

| Gate | Criterion (short) | Current status | Evidence / diagnosis | Test (suite) | Owner / where re-checked |
|---|---|---|---|---|---|
| M3.1 (L1 in LS1) | eats within 5 thinks when hungry | **revised** | ≥ 90% of probes and ≥ 80% in ≥ 90/100 seeds (observed 91.6%, 93 seeds); original target kept on record | `LS1_M3_1_And_M3_20_Revised` (+ `_Fail` twin) | M3 · A |
| M3.20 (LS1 control) | frozen control bound | **revised** | paired learner − Frozen lower bound ≥ 20 points (observed 27.0) | same | M3 · A |
| M3.2 (L2) | sated eating ≤ 15% | **revised** | ≤ 20% pooled and paired upper bound ≤ −10 points (observed 17.7%) | `L2_M3_2_Revised` | M3 · A |
| M3.3 (L3), filler clause | filler Q within ±100 vp | **revised** | clause replaced by the delayed-reward shaping criterion with a traces-off control | `L3_M3_3_Revised` | M3 · A |
| M3.7 (L8) | twins differ ≥ 30 points in 95% of pairs | **revised** | frozen-as-loaded measure; lower bound ≥ 30 (observed 46.2) | `L8_M3_7_Revised` | M3 · A |
| M3.11 (L13) | first fish meal within 3 thinks in ≥ 80% | **revised** | within 6 thinks incl. forage path (observed 92) | `L13_M3_11_Revised` | M3 · A |
| M3.5 (L5, L6) | forgetting a learned place | **failed** (outstanding target, not amended) | scenario does not depend on learning; finding type (c) | `L5_ForgettingOfALearnedPlace` (`[Explicit]`, `M3-target`) | NPC learning · A, F |
| L2b | sated eating after repeated hunger relief | **failed** (outstanding target) | 43.7% vs Frozen 25.3%; needs a sated feature (design change) | `L2b_SatedEatingAfterRepeatedHungerRelief` (`[Explicit]`) | NPC learning · A, F |
| M3.21 (LS3) | shelter learning | **deferred** (redesign scheduled after M4, D3 §3–§4) | 0.5% vs 1.3% | `LS3_TiredNearShelter` (target form) | NPC learning · F |
| M3.22 (LS4) | axe per seed and conjunction | **deferred** (redesign after M4) | 27/50 seeds ≥ 20 points; conjunction 16/50 | `LS4_GatheringWithAnAxe` (target form) | NPC learning · F |
| M3.19 | learning ≤ 25% of NPC update time | **known limitation** (aspiration, not a gate, D3 §6) | 37.5% at 500 NPCs; absolute budget met (3.1 µs per think) | bench | Q1 · F (slot 2) |
| M4.5 | stable over 10 generations | **failed** | every default seed goes extinct | `Stable_10Generations` | F2 · A, F |
| M4.6 | age pyramid in steady state | **failed** | no steady state (extinction) | M4 RT | F2 · A, F |
| M4.36 | viable families over 3 generations | **known limitation** (met as written, not taken as stability) | 17/20 seeds reach gen 3; populations fall 120 → 3–28 in 6 years | M4 RT | F2 · A |
| M4.27, M4.33 | multigenerational stability and sweeps classified | passed as written (outcome recorded: universal extinction) | every extinction classified | `Default_6x20_Generations`, sweeps | F2 · A |
| M4.28 | same-sex equal capabilities | ***partial***: cap, voluntariness and static scan passed; the 1 000-fixture equality test **unimplemented** | | `SameSex_EqualCapabilities` | M4 follow-up · F |
| M4.29 | shared childcare | ***partial***: arrangements form ≥ 10/20 seeds; exploitation clause **failed** (1 of 10 ended; `[Explicit]` target) | | `SharedChildcare_Possible` | M4 follow-up · F |
| M4.31 / MATERIAL 20 | estates: no new ids | **revised** (proposed wording, awaiting owner acceptance) | conflicts with the exact 4/3/3 split of M4.20 | M4 EM | owner · F |
| M4.21 (wills) | wills honoured | **deferred** to M8 (first-arrival and contest clauses checked at I-BS) | | M4 EM | M8 |
| M4.4, M4.7, M4.12, M4.13, M4.15, M4.16, M4.23, M4.24 and the immigration-off `Stable_10Generations` arm | immigration and capacity, child-mortality recomputation, selection, inbreeding, children fed, orphans, S7 strategies, 200-year long run | **unimplemented** (not done) | | named in ACCEPTANCE M4 | M4 follow-up / F2 · F |
| M4.37–M4.45 | ecology scenarios, one-factor experiments, relationship ontology, preferences, fertility measures, mixed-age founders, carrying capacity, care arrangements, demographic reports | **unimplemented** (new D4 criteria, not started); F2 owns the experiments | | F2 | F2 · A (reports), F |
| M5.6 / L14 | avoidance after cheating | **failed** (outstanding target) | S's next five offers accepted 32% (target ≤ 20%); recovery 7% (target > 50%) | `[Explicit]` `M5-target` | social learning · F |
| M5.11 / L16 | domain progress vs Frozen | ***partial***: relationships passed (49/50); communication outstanding (35/50, +20 points); barter and cooperation domains **unimplemented** | | `M5LearningTests` | social learning · F |
| M5.2 | recognition miss rates | **revised** (met with design number changed: `FamInteraction` = 180) | SOCIAL 4.1's 100 makes the second clause impossible | `SocialMemoryTests` | M5 · A |
| M5.9 | gifts follow kin belief | **unimplemented** (needs M4's `KinTerm`; check at I-BS) | | — | I-BS · A |
| M5.12 | S2/S5 strategy benchmarks | **unimplemented** | needs island contests and custom rules | — | later |
| M5.13 / LS2 | producing goods enables later food exchange | **unimplemented** | gather decision not credited by the later meal | — | NPC learning · F |
| M5.18 | dialogue is a developer overlay | ***partial***: implemented and PlayMode-tested on `unity/u3a`; persistence in preferences, mismatch marker, `Honest` flag and the 5-day H-UI-6 run not yet | | U3a PlayMode | U3a · A/B |
| M5.19 | non-verbal vocabulary animations | **unimplemented** | | — | U3b · D |
| I-BS.3 | kin help more frequent | ***partial***: both occur; kin **not** more frequent in raw counts (2035 vs 3130); per-opportunity rates not computed | | I-BS RT | F0 / Q3 · A |
| I-BS.5 | exchange with both believed gains > 0 | ***partial***: 5/10 seeds; fails its 80% threshold (F0 is fixing the threshold) | | I-BS RT | F0 · A |
| I-BS.6, I-BS.10 | behaviour changes after experience; tool kept | **unimplemented** (controlled tests not written) | | — | filler work (index §3) · F |
| I-BS.7 | estate passes to heirs | **failed** (test fails on seed 6: an estate open at run end not excluded; F0 is fixing the test) | | I-BS RT | F0 · A |
| I-BS.9 | child observes and imitates | **deferred** to M7 or M11 | | — | M7/M11 |
| M0.2 (N), M4.24 | 200-year nightly neutrality run; `LongRun_200Years` | *not run* on the current code | | NIGHT | Q1/Q4 · F |
| CI wiring | runner and player smokes in CI | *not run* (not wired) | | — | later |
| Owner hand-play | Living Island hand-play check | *not run* unless the owner does it | | — | owner |

---

## 7. Known limitations and temporary mechanisms to state in the release

| Item | Where it shows | Recorded in |
|---|---|---|
| Stage 3 mode (player as faction) stays playable until the owner retires it (M14) | menu "Play as Amber" | DEV_LOG "Temporary mechanisms" |
| `AiBrain` remains every faction's government seat until C10 (M14 deferred) | faction policy | index §0 lead's defaults |
| Iron Coast seeds a coin convention, organisations, contracts, agreements, knowledge and assets as initial conditions (listed in the scenario and `fact_scenario_seed`) | `ind_*` scenarios | MONEY §3.2, GAMEPLAY §13 |
| Patent law seeded on the government alias until C10's `PatentLaw` (if K1-b lands before C10) | D6.6–D6.8 fixtures | K1 record |
| Infringement complaint ends at a recorded stub at the commercial court until C9 (Tier 3) | D6.8 | K1 record |
| No vehicle combat before C4/M9; employee crews only | I5 | IMIL §12 q1 |
| Population viability unproven (M4.5/M4.6 failed) until F2 | scenario 1 | §6 |

---

## 8. Sign-off log (filled at each checkpoint)

| Checkpoint | Tag | Commit | Date | Save version | Suites (Sim / Runner / EditMode / PlayMode) | Pins | Build | Smokes (stage 3 / population / god-mode hashes) | Scenarios passed / partial / not run | Run by |
|---|---|---|---|---|---|---|---|---|---|---|
| A | `civdiv-a` | | | 4 | | | | `4bd54b271857a803` / / — | | |
| B | `civdiv-b` | | | 6 | | | | | | |
| C | `civdiv-c` | | | 7 | | | | | | |
| D | `civdiv-d` | | | 8 | | | | | | |
| E | `civdiv-e` | | | 9 | | | | | | |
| F | `civdiv-f` | | | 10 | | | | | | |
| G | `civdiv-g` (proposed) | | | 11 | | | | | | |
