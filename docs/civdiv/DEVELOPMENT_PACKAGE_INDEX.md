# Development package index: Civilisation and Divinity run (27 Nov 2026)

The definitive list of work packages for the 18-hour run (owner brief §§39–42, 44), extended on 27 Nov with the industrial and knowledge packages **I1–I6, K1, K2** and Checkpoint **G** for the second brief ([OWNER_BRIEF_2026-11-27_INDUSTRY.md](OWNER_BRIEF_2026-11-27_INDUSTRY.md), owner decisions D6.1–D6.19). The lead's binding rulings R1–R35 are in [LEAD_RULINGS.md](LEAD_RULINGS.md) and are applied below. The dependency graph, waves, concurrency, critical path, merge order and cut plan are in [DEVELOPMENT_DEPENDENCY_GRAPH.md](DEVELOPMENT_DEPENDENCY_GRAPH.md). File ownership, ids, switches, event blocks, migration numbers, system order and save rules come from [SHARED_ARCHITECTURAL_CONTRACTS.md](SHARED_ARCHITECTURAL_CONTRACTS.md) ("the contracts"), which is binding. Where this index and the contracts disagree, the contracts win, and the package stops and asks the lead for an amendment row (contracts §20).

**Grounding.** This index was written from [CURRENT_IMPLEMENTATION_BASELINE.md](CURRENT_IMPLEMENTATION_BASELINE.md) (the real state at `8617eaa`, the two paused branches and the 11 blockers of §13), the contracts, and the tier and package tables of the D5 design documents. Paths are relative to `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

---

## 0. How the brief's list was reorganised

The brief's list (§39) is a suggestion. The code and the contracts change it as follows:

| Brief id | Decision here | Why |
|---|---|---|
| F1 | **Kept, merged in two steps, F1a and F1b**, by one agent | F1 is the only package that edits frozen shared files. Most of it (state slots, tuning, dispatch, commands, control seam, events, save v5) blocks every D and C package. The rest (warehouse stubs, the person-loop sweep, the age sweep, cards, names, the lease resolution) blocks only some packages. Two merges cut about 2 hours off the critical path. Both are the same owner and touch the same frozen set, so there is still one writer of shared contracts. |
| D5 | **Folded into D4** ("Embodiment: possession and avatar") | Both use `DivineControlInput` and the lease/heading resolution. They share the "one embodied focus per god" rule (contracts §11.1). One agent avoids a cross-package rule. Package D4 owns D5's contract slots (`DivinityState.D5.cs`, codes 850–859, migration `0011`). |
| C8 | **Tier 2 scope folded into C6**; banking deferred | The first organisation template in ORGANISATIONS §9 *is* the shared food store founded by two households. C6 therefore owns the communal store, keeper and ration policy, and takes C8's slots (`OrgState.C8.cs`, codes 550–551, migration `0020` welfare columns only). Deposits, interest and banks are Tier 3 and deferred. |
| C5 | **Minimal citizenship folded into C10** | Scenario 16 needs a citizenship record separate from religious affiliation. Nothing else in Tier 1–2 needs C5. C10 owns C5's slots (`CivState.C5.cs`, codes 290–299, migration `0023`). Migration decisions and political interests are deferred. |
| C4, C9 | **Deferred** (Tier 3) | Soldiers are still `MilUnit`s (M9 not started), and justice needs M8 theft, witnesses and contests (not started). C1 lands theft *classification* only. |
| Q2 | **Folded into Q1** ("Analytics, KPIs and benchmarks") | Benchmarks and KPI views share one runner surface and the quiet-machine slots. One Sonnet agent schedules both. |
| U4 | **Folded into U3** ("World, characters and gestures") | Grid hiding, gesture rendering (M5.18, M5.19) and the character look all edit `Game/Models` and `Game/Island`. One owner avoids conflicts in `GestureLayer`, `LifeStageLayer` and `PeopleModels`. |
| U5 → **U5** | Kept (reports, life history, institutional panels) | |
| U6 | **Deferred**, apart from the scenario files (to Q3) | The tutorial rewrite and its polish are Tier 3 (GAMEPLAY_SCENARIOS §10). The stage 3 tutorial stays, as a temporary mechanism. The headless scenario files for the 18 end-to-end scenarios belong to Q3. |
| U1, U2 | Kept, each with an **a** part (Tier 1, Checkpoint B) and a **b** part (Tier 2, Checkpoint D) | The same files (camera, HUD) at two times. The b part restarts when D4 merges. |
| F0, Q4 | Kept, **run by the lead** (integration), each with a helper agent | Merging, suites, Unity runs, builds and smokes are the lead's job (brief §43). |

Result: **20 agent packages** (F1, F2, C1, C2, C3, C6, C7, C10, D1, D2, D3, D4, D6, D7, U1, U2, U3, U5, Q1, Q3) plus the **two lead-run integration packages** F0 and Q4, and, for owner D6, **eight industrial and knowledge packages** (I1 with its foundation part I1a, I2, I3, I4, I5, I6, K1, K2 with parts K2a and K2b).

**How the second brief's list was mapped** (its "Documentation and work packages" section and §13):

| Brief item | Package | Note |
|---|---|---|
| Industrial content and production recipes | **I1** (I1a foundation + I1 content, including the waste part) | I1a is a second neutral foundation commit, like F1: it lands every shared industrial and knowledge surface (contracts §22.1) |
| Vehicles and harvesting | **I2** | vehicles are C1 productive assets operated by NPCs (contracts §22.2 IA-4) |
| Refineries and industrial markets | **I3** "refineries and industrial trade" | **no external buyer** (D6.2): agreements, recorded sales and delivery obligations only |
| Electricity generation and distribution | **I4** | |
| Mechanised military production | **I5** | |
| Industrial presentation and analytics | **I6** | |
| Knowledge, designs, patents, licences (brief §§5–8) | **K1** (minimal: designs, knowledge, documents, licences; rights beyond the minimum and auctions Tier 3) | owns KNOWLEDGE_AND_TECHNOLOGY_DESIGN |
| One money and accounting architecture (brief §1) | **K2** (K2a Tier 2, K2b Tier 3) | owns MONEY_AND_ACCOUNTING_DESIGN |
| Waste and circular production (brief §9) | folded into **I1** (waste part) | WASTE_AND_CIRCULAR_DESIGN |
| New institutions (brief §11) | organisation **templates** added by the package that needs them on C6's framework (contracts §22.9) | no new package |

**D6.18 priority.** Every I and K package is **Tier 2 after the Tier 1 set** (or Tier 3). Industry is an *opportunistic lane*: it uses only agent slots and merge time that the D5 packages leave free, never takes a Unity slot before Checkpoint E, and never delays a Tier 1 package or the D5 Tier 2 core (D4a, C3, C6, C7 healthcare, U3b). I1a is cut after F1b and C1 merge, but **merges only after the Tier 1 set has merged** (contracts §22.0).

**Lead's defaults applied** (for the contracts' open questions):
- `Divinity.Enabled` flips on for population and "Living island" games at **Checkpoint B**, with deliberate re-pins (the population smoke hash and the god-mode scenarios). Pins P1–P9 keep naming their switches explicitly, so they are unaffected.
- `Civ.Enabled` is kept as a third switch.
- `AiBrain` stays the government seat of every faction. It is a declared temporary mechanism until C10, and its full retirement (M14) is deferred.
- AI and scripted gods act **from inside the sim** through `IGodPolicy` in `GodDirector` (system order 910). A scripted test god is therefore an `Ai`-seated god running a schedule policy (D1's `ScheduleGod`). It is not given the `Human` seat. The external-authorship rule (an external divine command must name the god in the `Human` seat) applies only to external commands, so it does not block several scripted gods in one test. **This replaces the last sentence of contracts §10.1.** The lead adds an amendment row in contracts §20 when F1 lands.

**Status vocabulary:** *not started*; *partial on branch* (work exists on an unmerged branch); *seam only* (a placeholder type exists on `unity/poc`).

---

## 1. Summary table

Effort is in **agent-hours** of focused work, including the agent's own test runs. It excludes the lead's merge time (about 30–40 minutes per merge: review, then the full suite on the merge result) and checkpoint Unity runs (45–100 minutes each). Checkpoints are those of brief §44 (A Living world, B Divine sandbox, C Working economy, D Personal divine interaction, E Institutional society, F Integrated) plus **G Industrial slice** (D6.18, section 4 below). Event blocks and migrations follow the contracts (§15.1, §18.2 and, for I and K, §22.6–22.7). Branch names avoid the names already taken in the repository (R17: `unity/q1`, `unity/q2`, `unity/q3`, `unity/f0`, `unity/p10`, `unity/p11` exist).

| ID | Title | Tier | Agent | Prerequisites | Ckpt | Effort (h) | Worktree / branch | Status |
|---|---|---|---|---|---|---|---|---|
| **F0** | Integration baseline: merge `unity/ibs` + `unity/demog`, full suites, Unity, build, smokes | 1 | **Lead** + Opus merge helper | — | A | 2.5 (wall) | `wt-lrs-int0` / **`unity/int0`** (R17; `unity/f0` is taken) → `unity/poc` | **In progress** in `wt-lrs-int0` (27 Nov) |
| **F1** | Shared foundation (contracts §0), merged as F1a then F1b | 1 | Opus (run for the lead; the lead reviews every frozen-file diff) | F0 | B (all) | 5.0 (F1a 3.5, F1b 1.5) | `wt-lrs-f1` / `unity/f1` | Not started |
| **F2** | Demographic viability and ecology experiments (continues `unity/demog`) | 1 | Opus | F0 (code merges after F1b) | A (results), F | 6.0 (much of it is runner time) | `wt-lrs-f2` / `unity/f2` (may reuse the warm `wt-lrs-m3` folder on a new branch) | **Partial on branch** (see 2.F2) |
| **C1** | Physical economy: productive assets (**owns `AssetId`**, R20), labour, ownership beliefs, theft classification | 1 | Opus | F1a (mappers after F1b) | C | 4.5 | `wt-lrs-c1` / `unity/c1` | Not started (seams only: `OwnerKind`, `Verb.WorkShift`, `Permits.Believed`) |
| **C2** | Obligations, promises, primitive lending, credibility | 2 | Opus | C1 | C (E) | 3.0 | `wt-lrs-c2` / `unity/c2` | Not started |
| **C3** | Employment between individuals and enterprises, wages | 2 | Opus | C1 (C2 for wage arrears, optional; C6 for the enterprise-org part) | C | 3.5 | `wt-lrs-c3` / `unity/c3` | Not started |
| **C6** | Organisation framework, service requests, communal store and welfare (absorbs C8's Tier 2); registers its director through D1's `SimBuilder` point (R31) | 2 | Opus | F1b (C1 merged before C6 merges) | E | 5.0 | `wt-lrs-c6` / `unity/c6` | Seam only (`IInstitutionPolicy`, `InstitutionView` placeholders) |
| **C7** | Healthcare, then education (teaching and imitation) | 2 | Opus | C6 (interim C6a: the service model) | E | 4.0 | `wt-lrs-c7` / `unity/c7` | Not started (M4 `Tend` and care exist) |
| **C10** | Government organisations (`Orgs.C10.SeedGovernments`: 0 in pinned scenarios, 1 in god-only Living Island and Iron Coast, R27), relayed divine instructions, refusal; minimal citizenship (absorbs C5); one temple template | 2 | Opus | C6, D2 (D6 for the temple) | E (F) | 4.0 | `wt-lrs-c10` / `unity/c10` | Not started |
| **D1** | Gods, Mythic, divine command gate, `IDivineView`, `GodDirector` (D1 owns the `SimBuilder.cs` director attachment, R31), idle, schedule and steward gods | 1 | Opus | F1a | B | 2.5 | `wt-lrs-d1` / `unity/d1` | Not started |
| **D2** | Divine suggestions, commands and temporary compulsion | 1 | Opus | D1 (may start from interim D1a) | B | 3.5 | `wt-lrs-d2` / `unity/d2` | Not started |
| **D3** | Miracles: heal, smite, create and destroy resources, bless and protect (minimal), consecrate | 1 | Sonnet | D1 (may start from D1a) | B | 2.5 | `wt-lrs-d3` / `unity/d3` | Not started |
| **D4** | Embodiment: possession (D4a), then manifested avatar (D4b) (absorbs D5) | 2 | Opus | D1, F1b | D | 5.0 (D4a 2.5, D4b 2.5) | `wt-lrs-d4` / `unity/d4` | Not started |
| **D6** | Structured commandments, divine beliefs, minimal affiliation | 1 (commandments), 2 (beliefs, prayer, affiliation) | Opus | D2 (may start from interim D2a) | F (first shown in the C/D build) | 4.0 | `wt-lrs-d6` / `unity/d6` | Not started |
| **D7** | Mind archive and resurrection | 2 | Opus | D1, F1b (D3 merged first, for the miracle events) | E | 3.0 | `wt-lrs-d7` / `unity/d7` | Not started (blocker 3 resolved by F1's `ArchiveMind` site) |
| **U1** | Camera, selection, input and time controls, the `-godonly`/`-godpopulation` boot flags for the god-mode smoke (R29) (U1a); embodied cameras and control input (U1b) | 1 (a), 2 (b) | Sonnet | U1a: F0 (its command builder rebases on F1a); U1b: D4a | B, D | 6.0 (3.5 + 2.5) | `wt-lrs-u1` / `unity/u1a`, then `unity/u1b` | Not started (`GodCameraRig`: fixed tilt, no rotation, no follow) |
| **U2** | Divine HUD: god-only gating, Mythic meter, toolbar, instruct and miracle flows, Divine card (U2a); pills, mode switcher, commandment builder (U2b) | 1 (a), 2 (b) | Sonnet | U2a: F1a, D1 (works against the D1a view); U2b: D4a, D6 | B, D | 6.5 (4.5 + 2.0) | `wt-lrs-u2` / `unity/u2a`, then `unity/u2b` | Not started |
| **U3** | World, characters and gestures: grid hidden, decode overlay (U3a); gesture vocabulary, carried goods, body look, divine effects (U3b) (absorbs U4) | 1 (a), 2 (b) | Sonnet | U3a: none (Game only, from `unity/poc`); U3b: F1a (`GestureMade` v2), D3 events for the effects | A or B, D | 6.5 (1.5 + 5.0) | U3a: `wt-lrs-u3a` / `unity/u3a`; U3b: `wt-lrs-u3` / `unity/u3b` | **U3a in progress** in `wt-lrs-u3a` (cut at `c1ab594`); U3b not started |
| **U5** | Reports and inspection: Economy tab, life history and memorial, institutional panels with record versus observation | 2 | Sonnet | D1; C1 (Economy tab); C6 (institutional panels); D7 (memorial) | C, E | 3.5 | `wt-lrs-u5` / `unity/u5` | Not started |
| **Q1** | Analytics: early views `0026` and late views `0030` (R18), KPI subset (incl. HLT-10, LIV-07, TIM-04, R30), `sql` command, P7, benchmarks (absorbs Q2); later `0041` with K1/K2 | 1 (views, divine KPIs, baselines), 2 (org and divine bench cases) | Sonnet | F0 (baseline bench); F1b and D1 (`0026`, P7); C6/C10/D6 (`0030`); each merged package | B–F (G) | 5.0 | `wt-lrs-q1kpi` / **`unity/q1kpi`** (R17) | Not started (migrations 0001–0006; `dim_kpi` empty; `bench` exists) |
| **Q3** | Integration assurance: end-to-end scenario harness, replay, save/load, conservation, switch matrix | 1 | Sonnet (the lead reviews every failure diagnosis) | D1 (harness), then each merge | B–F (G: `ind_slice`) | 6.0 (continuous) + 1.5 for G | `wt-lrs-q3int` / **`unity/q3int`** (R17; the stale non-git `wt-lrs-q3` folder is not used) | Not started (`ReplayTests`, `DeterminismTests`, F0 conservation test exist) |
| **Q4** | Final Windows build, checkpoint builds, handover | 1 | **Lead** + Sonnet doc helper | everything merged by the freeze | F | 3.0 (protected final window) | `wt-lrs` / `unity/poc` | Not started |

**Industrial and knowledge packages (owner D6).** Prerequisites are exact: *hard* ones must be merged into `unity/poc` before the package branches; *soft* ones are used if merged and otherwise have the stated fallback (contracts §22.9). "Tier 1 set merged" is a hard prerequisite of **every** row below (D6.18). Every row's event block and migration is fixed by contracts §22.6–22.7.

| ID | Title | Tier | Agent | Prerequisites (hard; *soft*) | Owned files (summary; detail in 2.I/2.K) | Events | Migration | Ckpt | Effort (h) | Worktree / branch | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **I1a** | Industrial and knowledge **foundation** (neutral, like F1): every shared surface of contracts §22.1 | 2 (first industrial merge) | Opus (run for the lead; the lead reviews every frozen-file diff) | F1b, C1; **merges only after the Tier 1 set has merged** | the I1a column-3 files of contracts §22.1 (frozen after merge) | domains 70–79 and their ranges; row 1014 `BinMoved` only | none | G (prerequisite) | 1.5 | `wt-lrs-i1a` / `unity/i1a` | Not started |
| **I1** | Industrial content and recipes: wares' content, deposits (scenario-seeded, then generation), facilities as C1 assets, `RecipeBook` (rows 1–20 = stage 3 defs) and the runner (264), orders; waste outputs, waste sites and the pollution field (waste part) | 2 | Opus | I1a, C1, C6 (hard: Iron Coast's facilities belong to organisations whose office-holders place the orders); *C3 (wages), K1 (design minima; else `DesignId = 0`), Q1* | `Industry/Geology*`, `Industry/RecipeBook.cs`, `Industry/Facilities.cs`, `Industry/Production.cs`, `Industry/Orders.cs`, `Industry/IndustrySystem.cs` (body), `Industry/IndustrySetup.cs`, `Industry/Waste/*`, `IndustryState.I1.cs`, `IndustryState.Waste.cs`, `IndustryHooks.cs`/`IndustryMindHooks.cs` (dispatcher bodies), `IndustryMind.cs` (I1 fields), `IndustryEvents.I1.cs`, `IndustryTuning.I1.cs`, `I1Mappings.cs`, `Tables.I1.cs`, migrations, tests | 1000–1019, 1080–1094 | `0031`, `0036` | G | 4.0 (minimal 3.0; waste part +1.0) | `wt-lrs-i1` / `unity/i1` | Not started |
| **I2** | Vehicles and harvesting: vehicle assets, bins, board/drive/alight, fuel, the harvest routine as an NPC job, maintenance; possession and compulsion of operators | 2 | Opus | I1, C1; *C3 (operator jobs), C6 (fleets), D2 and D4a (only for compelled and possessed driving), Q3* | `Industry/Vehicles/*`, `IndustryHooks.I2.cs`, `IndustryMindHooks.I2.cs`, `IndustryState.I2.cs`, `IndustryEvents.I2.cs`, `IndustryTuning.I2.cs`, `I2Mappings.cs`, `Tables.I2.cs`, flagged rows in C3 (`JobKind.Operate`) and D2 (`DivineActs` 22/23), tests | 1020–1039 | `0032` | G | 4.0 (minimal 3.0) | `wt-lrs-i2` / `unity/i2` | Not started |
| **I3** | Refineries and industrial trade, **no external buyer**: supply agreements, deliveries with ownership at unload (basis 25), delivery obligations, recorded sales (`IndustrialSaleClosed`) | 2 | Sonnet | I1, C2 (obligations), C6; *I2 (deliveries), K2a (money payment; goods payment works without it)* | `Industry/Trade/*`, `IndustryState.I3.cs`, `IndustryEvents.I3.cs`, `IndustryTuning.I3.cs`, `I3Mappings.cs`, `Tables.I3.cs`, migration, tests | 1040–1049 | `0033` | G (step 4, sale branch) | 2.0 | `wt-lrs-i3` / `unity/i3` | Not started |
| **I4** | Electricity generation and distribution: coal station as a staffed facility, links, ports, components, interval balance, allocation, shortage and outage, energy ledger; meters and one bill | 2 | Opus | I1, C1; *C6 (utility), C3 (staff), C2 (billing only), I2 (fuel delivery; hand-carry works), K1, K2a, Q1* | `Industry/Grid/*` (incl. `ElectricitySystem.cs` body, 262), `IndustryState.I4.cs`, `IndustryEvents.I4.cs`, `IndustryTuning.I4.cs`, `I4Mappings.cs`, `Tables.I4.cs`, migration, tests | 1050–1069 | `0034` | G | 3.5 (minimal 2.5) | `wt-lrs-i4` / `unity/i4` | Not started |
| **I5** | Mechanised military production: firearm and ammunition recipes, procurement agreements, military organisation orders, crewed transport and tank (no combat in Tier 2) | 2 (end) / 3 | Sonnet | I1, I2, I3, C6, C3; *I4, D2/D4, K1, K2a; C4/M9 for Tier 3 combat* | `Industry/Military/*`, `IndustryState.I5.cs`, `IndustryEvents.I5.cs`, `IndustryTuning.I5.cs`, `I5Mappings.cs`, `Tables.I5.cs`, migration, tests | 1070–1079 | `0035` | after G (`VS11`) | 3.0 | `wt-lrs-i5` / `unity/i5` | Not started |
| **I6** | Industrial presentation and analytics: deposits and facilities drawn, harvesters with visible cargo, facility, vehicle and station cards, production queues, electricity and logistics overlays, shortage glyphs (all in the seamless island, grid hidden); KPI views | 2 (cards, overlays and glyphs for the slice) / 3 | Sonnet | I1, U2a, U3a, Q1 (`0030`, `sql`); each I package whose state a view shows (I2 cargo, I4 overlay); *U5 (reports), U1 tab registration (R9)* | `Assets/God/Game/Industry/*`, `Assets/God/Game/Hud/Industry/*` (cards via U1's registration hook), `0037_industry_kpi_views.sql`, KPI rows IND/VEH/ELE/WST/ENV/IMIL, PlayMode tests `Assets/God/Tests/PlayMode/Industry*` | 1095–1099 (if any) | `0037` | G | 3.5 (minimal 2.0) | `wt-lrs-i6` / `unity/i6` | Not started |
| **K1** | Knowledge, designs, artefacts and licences (**minimal**): `DesignDef` envelopes and loader, seeded known designs, `KnowledgeMind`, knowledge and skill minima, documents (write, copy, read, build from), licences and patents minimal with `UnauthorisedDesignUse` classification; rights beyond the minimum, auctions and research are Tier 3 | 2 (minimal) / 3 (rights beyond the minimum, auctions, research) | Opus | I1a, C1, C6 (patent office template, R33 `RespectRights` policy); *C7 (teaching; skill store), C10 (patent law; else scenario seed), C9 (complaints; else stub), K2a (fees), D7 (`ArchivedLife.Know`)* | `Civ/CivState.K1.cs`, `Civ/Knowledge/*`, `Civ/KnowledgeMind.cs` (fields), `Civ/CivHooks.K1.cs`, `Civ/CivMindHooks.K1.cs`, `CivEvents.K1.cs`, `CivTuning.K1.cs`, `K1Mappings.cs`, `Tables.K1.cs`, migrations, flagged rows (contracts §22.9), tests | 1100–1129, 1130–1159, 1160–1169 (Tier 3) | `0039`, `0040`, `0043` (Tier 3) | G (VS7–VS8 minima); D6.5–D6.8 | 4.0 (minimal 2.5) | `wt-lrs-k1` / `unity/k1` | Not started |
| **K2** | Money and accounting core. **K2a** (Tier 2): commodity money (seeded `Coin`), `Payment` record and the one payment act, org double-entry books, units of account, settlement of obligations, billing acts. **K2b** (Tier 3): institutional money, issuers, accounts, issuance and destruction | K2a 2; K2b 3 | Opus | K2a: I1a, C1, C3 (`Pay` 101), C6; C2 for obligation settlement (spot payments work without it). K2b: K2a; *C8 (banking services)* | `Civ/CivState.K2.cs`, `Civ/Money/*` (`Payments`, `Books`, K2b `Money`, `Accounts`), `Civ/CivMindHooks.K2.cs`, `CivEvents.K2.cs`, `CivTuning.K2.cs`, `K2Mappings.cs`, `Tables.K2.cs`, migrations, flagged rows in C2, C3, C6, tests | K2a 1170–1179, 1190–1199; K2b 1180–1189 | K2a `0038`; K2b `0042` | G (step 4 sale branch, payments); D6.2, D6.3 | K2a 3.0; K2b 2.5 | `wt-lrs-k2` / `unity/k2a`, then `unity/k2b` | Not started |

Q1 owns `0041_knowledge_money_kpi_views.sql` (with K1, K2), after `0038`–`0040` exist. Q3 owns `DotNet/God.Runner/Scenarios/ind_*.scenario.json` ("Iron Coast", GAMEPLAY_SCENARIOS §13; content blocks supplied by I1, I2, I4, K1, K2a) and the D6 section of ACCEPTANCE_TESTS.

**Tier 1 set (protected):** F0, F1, F2 (the experiments), C1, D1, D2, D3, D6 (commandments), U1a, U2a, U3a, Q1 (Tier 1 part), Q3, Q4. Unchanged by owner D6.
**Tier 2 set, D5 (in priority order):** D4a possession, C3, C6, U3b, D4b avatar, U1b and U2b, C7 healthcare, C2, U5, C7 education, C10, D7. (Reordered from the first index so that the cut ladder of the graph doc §7 drops from the end: D7 first, then C10, U5, C2.)
**Tier 2 set, industrial lane (opportunistic, after the Tier 1 set has merged; in order):** I1a, I1 minimal, I2 minimal and I4 minimal (in parallel), Q3 `ind_slice`, I6 minimal, K1 minimal, K2a, I3 (one agreement and one recorded sale), then I1 waste part and the oil chain. Checkpoint G needs only I1a, I1, I2, I4, Q3 `ind_slice` and I6 minimal: step 4 can pass by its "processed" branch (recipe 25) without I3, K2a or C2, and steps 7–8 pass with `DesignId = 0` designs without K1.
**Tier 3 (kept in the design, not planned for this run):** I5 beyond the procurement test, vehicle combat, strategic device; K1 rights beyond the minimum (challenges, revocation, court process, compulsory licensing), research projects, invented variants, auctions; K2b; I4 oil, hybrid, waste, nuclear and renewable designs, rationing, tariffs; I3 toll processing and marketplaces; waste consumption refuse and contamination.

---

## 2. Package details

Every package works only on its own branch in its own worktree. It changes only its own files plus the per-package partial files the contracts give it. It never edits a frozen file (contracts §0.1 column 4). When it finds a contradiction, it asks the lead for an amendment row. It commits and documents as it goes (DEV_LOG lesson: long-running agents can be cut off). It writes its record in a section of DEV_LOG headed "<ID> record" on its branch.

**Gate at every package merge** (contracts §0.2, §16–18):
- `dotnet test DotNet -c Release` green on the merge result.
- The neutrality pins that apply (P1, P4, P8 always; P2, P3, P5, P9 once landed).
- The package's own reconciliation test (event counts = fact rows).
- `*_SaveLoadParity` at a mid-state of the feature.
- The **Unity compile check** (open the editor in batch mode, read a clean console) whenever a type `God.Game` uses changed.

"R" = reads or consumes a contract; "M" = modifies (own partials only, unless stated).

### F0 — Integration baseline (lead)
- **Objective:** one green `unity/poc` with the I-BS and demography branches merged, verified in Unity and the player. This is the "base" every later pin uses.
- **Scope:** on `unity/ibs`, I-BS resume steps 1, 2 and 6: fix I-BS.7's test so it skips estates still open at the end; set I-BS.5's threshold from evidence; run the full `dotnet test`; fix any M4/M5 behaviour test the amendments moved; update the status table. Merge `unity/ibs`. On `unity/demog`, demography resume steps 1, 2, 6, 7 and 8: merge `unity/poc` into it, keeping the both-off pins `8b898d115d8a05a7`, `5fa8991b92168385` and `66a55d5a67f2ad4e`, and resolving `Npc.Traits` (ibs: 16 entries, 12–15 unwritten; demog: 19), `Founders.MakeLife`, `M4LifeCycleTests` and the four household files. Then the full `dotnet test`, the SHARED_CONTRACTS rows for verb 45 and situation 22, and the merge. Then Unity EditMode and PlayMode, the Windows build and both smokes. The stage 3 hash `4bd54b271857a803` must hold; the population smoke hash is recorded anew.
- **Excluded:** I-BS steps 3–5 (controlled I-BS.6/8/10 tests, the M5.9 kin cue, M5.18 and M5.19). M5.18 and M5.19 go to U3; the rest is deferred filler (section 3). Demography resume steps 3–5 (the sweeps) go to F2.
- **Owned:** the merge commits; the DEV_LOG "Current status", "Test results" and "Commits and branches" rows. The work runs on **`unity/int0` in `wt-lrs-int0`** (R17; `unity/f0` is the old M4/M5 scaffolding branch) and is in progress there; the helper agent owns the conflict resolution on that branch only.
- **Contracts:** M (lead): it defines the base hashes for P1–P9. No new codes or migrations.
- **Status:** `unity/ibs` is 4 commits (`81e4f47`); I-BS passes 11 of 13 on the branch; its full runner suite was not read. `unity/demog` is 6 commits (`5aaa255`); it has never run alongside M5; it has 10/10 extinction findings and M4.37 not met; Unity was never opened on it (the `GodBootstrap` change is uncompiled). Both branches are pushed.
- **Checkpoint:** **A** (tag `civdiv-a`).

### F1 — Shared foundation (contracts §0)
- **Objective:** land every shared type, slot, seam, range, event domain, the save v5 bump and the warehouse stubs, all behaviour-neutral. After F1, every later package changes only its own files.
- **F1a (merge first, unblocks D1, D2, D3, C1, C6, U2a):** F1.1 ids; F1.2 state slots and partial files for Civ, Orgs and Divinity; F1.3 tuning roots and nested classes; F1.4 hash parts; F1.5 module entry points; F1.6 the extension dispatch with every new site (`OverrideIntent`, `ForceThink`, `ActionEnded`, `SenseExtra`, `DivineTerm`, `ArchiveMind`, `Heal`, `Damage`); F1.7 the id ranges and `MaxCand` 36; F1.8 the enum widenings (`OwnerKind.Org`, `BinKind`, `MoveReason`, `OwnershipBasis`, `ProduceReason`, `ConsumeReason`, `BodyKind`, the `Npc.Kind`/`God`/`BioAgeOffset` fields); F1.9 the control table, precedence, `TryAcquire`/`Release`/`Sweep`, `OverrideIntent`, `ForceThink`; F1.9b the mind slots; F1.10 the command types, `DivineReasons`, `GameSetup.Control`/`HumanGod`, the god-only and authorship gates (with the lead's §10.1 amendment); F1.11 `PolicyLevel.God`, `IGodPolicy`, `IdleGod`, the `IDivineView` stub; F1.12 the domains, ranges and empty per-package catalogue arrays; F1.13 save v5 (written on the lead's instruction; the lead owns `SaveFile.cs`); F1.17 the frozen-enum extensions (`BeliefKind` 7–9, `GestureAct` reservations, `Ware.Herbs`, `GestureMade` v2); F1.16 pins P1, P4, P8 and the "no handler, refused" test.
- **F1b (unblocks D4, D7, Q1 and every mapper):** the F1.8 person-loop sweep to `Bodies.IsLivingPerson`, with `Static_PersonLoopsUseIsPerson`; the `Bodies.AgeTicks` sweep of M4 ageing (contracts §4.4); the `DivineControlInput` lease and heading resolution (§11.3); F1.14 the inspection cards (stub `Fill`); F1.15 `0007_divine_core.sql`, the mapping registration and the `Tables.*` stubs; the F1.16 helpers `DivineProbe` and `MythicTrace`; F1.18 `Names`. The `ui` save extra is U1's.
- **Owned:** every file in contracts §0.1, column 3. After F1 merges, these become frozen or pass to the named owner.
- **Contracts:** **M** (the only package that may, besides the lead): `GodState`, `GodSim`, `SimTuning` roots, `EventCatalog`/`TelemetryDomain`, `PolicyTypes`, `PolicyInterfaces`, `Thinker`, `MindView`, `Mind.cs`, `PopulationSystem`, `PopulationModule`, `Goods`/`LotBin` enums, `Control.cs`, `SaveFile` (for the lead).
- **Events / migration:** domains 36, 45, 46 and 60–66; the ranges 800–899 (block) and the 500-block mapping; no event rows. Migration **`0007_divine_core.sql`**.
- **Checkpoint:** B (a prerequisite of everything after A).
- **Risk:** surface size. If F1a has not merged by H6.5, the cut plan applies (graph doc §7).

### F2 — Demographic viability and ecology
- **Objective:** meaningful, reported viability experiments, and if achievable a viable favourable scenario (M4.37), without forcing outcomes (D5.33).
- **Scope:** demography resume steps 3–5: the fertility sweep `plan_fertility.json` on `demog_favourable`; one justified biology block across the `demog_*` scenarios; 20 seeds × at least 5 generations; M4.38, M4.40, M4.44. Diagnose the conception-coordination bottleneck (completed fertility 0.5–0.7 against about 2.3 needed). Fix it as a mechanism behind tuning, never by tuning outcomes. Rerun M4.26's death clause on the established band (currently verified only vacuously). Later, use the C6 communal store and the C7 care organisations as treatments (ORGANISATIONS §9) if they merge.
- **Excluded:** LS3 and LS4 redesigns (deferred; Tier 2 filler). Anything divine.
- **Owned:** `Population/Households/Reproduction.cs`, `Population/Households/Relationships.cs`, the `Households`/`Ecology`/`Genetics` tuning blocks (existing M4 blocks, `IOmitWhenDefault` additions only), `DotNet/God.Runner/Demog/*`, `DotNet/God.Runner/Scenarios/demog_*.scenario.json`, `plan_*.json`, `DotNet/God.Runner.Tests/Demog*`, `docs/stage4/POPULATION_DESIGN.md` "Demography investigation".
- **Contracts:** R everything. **No frozen file.** A change to default behaviour moves every hash. It therefore merges only at a **declared re-pin point** (preferably together with the Checkpoint B re-pin). Otherwise it stays behind a tuning flag that the `demog_*` scenarios set.
- **Events / migration:** none (uses `demog report` and migrations 0001–0006).
- **CPU:** the sweeps run with `--workers 4` while three or more agents are testing, and pause during the benchmark slots (graph doc §4).
- **Status:** continues `unity/demog` (ontology, `demog report`, ranging, ecology scenarios committed; sweeps not run).
- **Checkpoint:** A (as reported experiments); F (viability result in the handover).

### C1 — Physical economy, labour and ownership
- **Objective:** persistent NPCs work productive assets (field, workshop, store), own and use goods through ownership beliefs, and haul. Theft is classified, not judged. This is the Tier 1 "physical resource and labour economy".
- **Scope:** `Civ.Enabled`; productive assets as C1 objects (`ObjClass` 90–99, `BinKind.AssetStore`) perceived through `SenseExtra`; the `Work` verb (96) and `MoveReason.Deliver` hauling; ownership beliefs in `CivMind`; `Permits.Believed` extended with ownership; `TheftCommitted` (274) classified with `Permits.Truth`; the C4 relaxation (O7); `CivCard` filled; scenarios 2 (trade, checked on the merged base) and "operates a productive asset". Stage 3 walkers stay in stage 3 mode.
- **Excluded:** walker retirement (M6 in full), stage 3 economy, wages (C3), loans (C2).
- **Owned:** `Civ/CivState.C1.cs`, `Civ/CivModule.cs` (bodies), `Civ/CivSystem.cs` (257), `Civ/CivHooks.cs` and `Civ/CivMindHooks.cs` (dispatcher bodies; C2, C3 and C5 add partials), `Civ/Assets.cs`, `Civ/Ownership.cs`, `Civ/Work.cs`, `Civ/CivEvents.C1.cs`, `Civ/CivTuning.C1.cs`, `Information/PeopleCards.Civ.cs` (Fill), `DotNet/God.Runner/Warehouse/C1Mappings.cs`, `Tables.C1.cs`, `Migrations/0015_ownership_work.sql`, `Assets/God/Tests/EditMode/Civ/C1*`, `DotNet/God.Runner.Tests/C1*`.
- **Contracts:** R `Goods`, `Bodies`, sense seam, `Control` (compelled work uses it). M `Permits` (ownership beliefs, contracts §19.1), `CivState` sub-object C1.
- **Events / migration:** **270–289**; **`0015`**.
- **`AssetId` (R20):** C1 owns the one id space of every productive asset (buildings, workshops, and later vehicles, power stations, links, depots) through `Assets.Register`; I1–I5 register their assets only through it (contracts §22.4).
- **Checkpoint:** C.

### C2 — Obligations, promises and primitive lending
- **Objective:** actual resources are lent, used and repaid or defaulted (scenario 7), through a double-entry `FormalObligation` registry that moves no goods.
- **Scope:** `Obligations.Open/Close`; `Lend` (97) and `Repay` (98); `OwnershipBasis.Loan`/`Repayment`/`Forfeited`; collateral; default; credibility in `CivMind`; promises 320–322; the signalling acts `GestureAct` 24–27 (Thank, Greet, Threaten, Alarm).
- **Excluded:** institutional lending and banks (deferred C8), insolvency procedures.
- **Owned:** `Civ/CivState.C2.cs`, `Civ/Obligations.cs`, `Civ/Lending.cs`, `Civ/CivHooks.C2.cs`, `Civ/CivMindHooks.C2.cs`, `CivEvents.C2`, `C2Mappings.cs`, `Tables.C2.cs`, `0017_lending.sql`, tests.
- **Contracts:** R `Goods` (`OwnerRule.Keep` loans), M5 social memory. M `CivState` C2, the GestureAct 24–27 semantics.
- **Events / migration:** **320–329, 540–549**; **`0017`**.
- **Checkpoint:** C (or E).

### C3 — Employment and enterprises
- **Objective:** an individual operates a productive asset and compensates another for work (scenario 3); then enterprises and cooperatives as organisations.
- **Scope:** C3a: `Hire` (99), `WorkFor` (100), `Pay` (101), `ContractId`, `OwnershipBasis.Wage`, job days, from C1 assets. C3b (after C6 merges): the enterprise and cooperative org templates and `0025`.
- **Excluded:** labour markets, class measures (M8).
- **Owned:** `Civ/CivState.C3.cs`, `Civ/Employment.cs`, `Civ/CivMindHooks.C3.cs`, `CivEvents.C3`, `Orgs/OrgState.C3.cs` (C3b), `C3Mappings.cs`, `Tables.C3.cs`, `0016_employment.sql`, `0025_enterprise_columns.sql`, tests.
- **Contracts:** R C1 assets, C2 obligations (wage arrears, optional), C6 framework (C3b). M `CivState` C3.
- **Events / migration:** **250–269**; **`0016`** (C3a), **`0025`** (C3b; needs `0018`).
- **Checkpoint:** C.

### C6 — Organisation framework, services, communal store and welfare
- **Objective:** organisations emerge from need evidence (D5.16) with several ownership models (D5.17). Service requests reach an organisation and are delivered by NPC labour (scenario 4). First template: the communal food store with a keeper and a ration rule.
- **Scope:** C6a (interim, unblocks C7): `Orgs.Found`, `OrgId`, members and offices, `DecisionRule` (OwnerDecides, HeadDecides, MajorityPresent), `OrgRecords`, `Services.Open` and the queue, `RequestId`, `OrgMind` beliefs, `Join`/`LeaveOrg`/`RequestService` (104–106), the `OrganisationSystem` (260) and `InstitutionDirector` (905), `InstitutionView` rewritten around `OrgId` (contracts 20.11), `Orgs.RelayRecipients`, pin P5. C6b: the communal store template, keeper, ration policy and welfare grants (C8's slots).
- **Excluded:** merger, split, Council and Consensus rules, inter-organisation regulation (Tier 3), banks.
- **Owned:** `Orgs/OrgState.C6.cs`, `Orgs/OrgState.C8.cs`, `Orgs/OrgModule.cs` (bodies), `Orgs/OrgHooks.cs` and `Orgs/OrgMindHooks.cs` (dispatcher bodies), `Orgs/Orgs.cs`, `Orgs/Services.cs`, `Orgs/OrgRecords.cs`, `Orgs/CommunalStore.cs`, `Orgs/OrganisationSystem.cs`, `Orgs/InstitutionDirector.cs`, `Orgs/OrgMind.cs` (fields), `Policy/InstitutionPolicies.cs`, `Policy/Observations.cs` (`InstitutionView`), `Information/PeopleCards.Orgs.cs`, `OrgEvents.C6`, `OrgEvents.C8`, `C6Mappings.cs`, `C8Mappings.cs`, `Tables.C6.cs`, `0018_organisations.sql`, `0020_banking_welfare.sql` (welfare columns only), tests.
- **Contracts:** R `Goods` (`OwnerKind.Org`, `BinKind.OrgStore`/`CommunalStore`), C1 assets (org buildings), C2 (none required). M `OrgState`, `InstitutionView`, `IInstitutionPolicy` implementations.
- **Events / migration:** **500–519, 550–551**; **`0018`**, **`0020`**. Under **R23** C6 may extend its organisation codes beyond 519 within 500–549, but only into codes no other package holds (C7 keeps 520–539, C2 keeps 540–549: ask the lead for the exact codes); welfare uses 551–559 and 550 stays reserved for banking.
- **Director attachment (R31):** C6 registers `InstitutionDirector` through the registration point D1 adds to `SimBuilder.cs`; it does not edit that file itself.
- **Checkpoint:** E.

### C7 — Healthcare, then education
- **Objective:** an injured person requests help and recovers through another NPC's treatment (scenario 5). One teaches another with a measurable capability change (scenario 6).
- **Scope:** C7a healthcare: `Treat` (107) = `Tend` + healing skill + optional `Herbs` remedy (sources and use), private and communal healer, `ConsumeReason.Treatment`. C7b education: `Teach` (108), `Attend` (109), `GestureAct` 18 Teach and 21 Demonstrate, imitated associations through the learner's public API only (a change to `Learner` needs an amendment row).
- **Owned:** `Orgs/OrgState.C7.cs`, `Orgs/Health.cs`, `Orgs/Education.cs`, `Learning/Imitation.cs` (new), `Orgs/OrgMindHooks.C7.cs`, `OrgEvents.C7`, `C7Mappings.cs`, `Tables.C7.cs`, `0019_health_education.sql`, tests.
- **Events / migration:** **520–539**; **`0019`**.
- **Checkpoint:** E.

### C10 — Government, relayed instructions, citizenship, temple
- **Objective:** a government organisation independently refuses or modifies a divine instruction (scenario 15). Religious affiliation changes without changing citizenship (scenario 16).
- **Scope:** a government org per faction (Head, small council), `InstructionRelayed` recipients through `Orgs.RelayRecipients`, the decision to adopt, modify or refuse by the government's own `IInstitutionPolicy`; `OrgState.GovernmentOf` resolving the `OwnerKind.State` alias; C5 minimal citizenship spells (`CitizenshipChanged`) separate from D6 affiliation; one temple template (needs D6); `LawAdopted` copying commandment rules at a revision. `AiBrain` keeps the stage 3 decisions (lead default).
- **Excluded:** AiBrain retirement (M14), elections and political organisations, cultural transmission (M11), migration decisions.
- **Owned:** `Orgs/OrgState.C10.cs`, `Orgs/Government.cs`, `Orgs/Temples.cs`, `Orgs/Laws.cs`, `Civ/CivState.C5.cs`, `Civ/Citizenship.cs`, `OrgEvents.C10`, `CivEvents.C5`, `C10Mappings.cs`, `C5Mappings.cs`, `0023_citizenship_politics.sql`, `0024_culture_religion_orgs.sql`, tests.
- **Events / migration:** **290–299, 355–374 (unused unless culture lands), 590–599**; **`0023`**, **`0024`**.
- **Seeded governments (R27):** `Orgs.C10.SeedGovernments`, default **0** in the pinned scenarios (so P5 holds), **1** in the god-only Living Island and in the industrial scenario (Iron Coast).
- **Checkpoint:** E; scenarios 15 and 16 at F.

### D1 — Gods, Mythic and the divine command interface
- **Objective:** persistent gods, one per initial faction (D5.3–D5.5), with Mythic under the Standard, Unlimited and Disabled modes (D5.26, D5.27), the same rules for human and AI gods, and the divine observation view.
- **Scope:** D1a (interim, about 1 h in; unblocks D2, D3, U2a): `GodRegistry`, `GodCreated`, `Mythic.CanSpend/TrySpend/ChargeUpkeep`, `Interventions.Open`, `DivineActionAccepted`, the `DivinitySystem` (230) skeleton with partial-method slots for D2, D4, D5 and D7. Then `IDivineView` (`Omniscient`) and `DivineViewFiller`, `GodDirector` (910), `IdleGod`, **`ScheduleGod`** (a scripted schedule from scenario tuning; the lead's default for test gods), `StewardGod` (Tier 2, last), the configuration commands `SwitchGod`/`SetGodPolicy`/`SetMythicMode`/`SetDivineProfile`, pins P2 and P3, `fact_god_faction`.
- **Owned:** `Divinity/DivinityState.D1.cs`, `Divinity/DivinityModule.cs` (bodies), `Divinity/DivinitySystem.cs`, `Divinity/DivinityHooks.cs` and `Divinity/DivinityMindHooks.cs` (dispatcher bodies; D2–D7 add partials), `Divinity/Gods.cs`, `Divinity/Mythic.cs`, `Divinity/Interventions.cs`, `Divinity/GodDirector.cs`, `Divinity/DivinityTuning.D1.cs`, `Divinity/DivinityEvents.D1.cs`, `Information/DivineView.cs`, `Information/DivineViewFiller.cs`, `Policy/GodPolicies.cs`, `D1Mappings.cs`, `Tables.D1.cs`, `0008_mythic.sql`, tests.
- **Contracts:** R command gate, control table. M god identity, Mythic, `IDivineView` (additive members).
- **Events / migration:** **800–809**; **`0008`**.
- **`SimBuilder.cs` (R31):** D1 owns the director-attachment edit and lands it first, with a registration point that C6 (and later any other director) uses.
- **Checkpoint:** B.

### D2 — Suggestions, commands and compulsion
- **Objective:** the player issues an instruction; the NPC perceives it and responds independently (scenario 8). Compulsion temporarily forces a valid physical action, paid for with Mythic (scenario 9).
- **Scope:** D2a (interim, unblocks D6): `IssueSuggestion`/`IssueCommand`, the perception draw, decode into `DivineMind.Inbox`, `ThinkScratch.Divine`, `DivinityMindHooks.AddCandidates` (D2 partial), `DivineTerm` (D2 part), the Tier 1 `DivineActs` table (DIVINE_PLAYER §4.4), `CommandResponded`. Then `IssueCompulsion`/`CancelCompulsion` through `Control.TryAcquire` (never editing `Control.cs` or `Think`), `CompulsionBlocked`, learning tags; then scopes (household, group, location) and `InstructionRelayed` delivery (the faction stand-in until C10).
- **Owned:** `Divinity/DivinityState.D2.cs`, `Divinity/Instructions.cs`, `Divinity/Compulsion.cs`, `Divinity/DivineActs.cs`, `Divinity/DivinityHooks.D2.cs`, `Divinity/DivinityMindHooks.D2.cs`, `Divinity/DivineMind.D2.cs` (inbox), `DivinityTuning.D2.cs`, `DivinityEvents.D2.cs`, `Information/PeopleCards.Divine.cs` (inbox rows), `D2Mappings.cs`, `Tables.D2.cs`, `0009_commands.sql`, tests (contracts §11.5 compulsion tests).
- **Contracts:** R `Control` (frozen, F1), M5 decode model. M `DivineMind.Inbox`, `DivineActs`.
- **Events / migration:** **810–824**; **`0009`**.
- **Checkpoint:** B.

### D3 — Miracles
- **Objective:** heal or smite with genuine state changes (scenario 12). Create and destroy resources with exact ledger entries (C1 and the new C7 conservation invariant).
- **Scope:** `CastMiracle` for HealInjury, HealDisease, SmitePerson, SmiteBuilding (huts), SmiteSource, CreateResource, DestroyResource, Bless and Protect (minimal, as `Blessing` records), Consecrate (Tier 2, last); `DivineLedger.Book`; `Bodies.Heal/Damage` hook bodies; hut removal and source-stock functions added to the population module API (additive, reviewed by the lead); witnesses' `SmiteWitnessed` situation.
- **Excluded:** RevealTechnology, area miracles (Tier 3), weather and terrain.
- **Owned:** `Divinity/DivinityState.D3.cs`, `Divinity/Miracles.cs`, `Divinity/DivineLedger.cs`, `Divinity/DivinityHooks.D3.cs`, `Population/PopulationModule.Miracles.cs` (new partial, if the class is partial; otherwise an additive edit flagged for review), `DivinityTuning.D3.cs`, `DivinityEvents.D3.cs`, `D3Mappings.cs`, `Tables.D3.cs`, `0012_miracles.sql`, tests.
- **Contracts:** R `Goods.Produce/Consume` with `ProduceReason.Miracle`/`ConsumeReason.Miracle`, `Kill(DeathCause.Violence)` (no new death cause, 20.4). M the miracle ledger (C7 invariant).
- **Events / migration:** **825–844**; **`0012`**.
- **Checkpoint:** B.

### D4 — Embodiment: possession (D4a) and avatar (D4b)
- **Objective:** the player takes control of an NPC, acts, and returns control to the same individual (scenario 10). The player manifests a distinct avatar, walks, interacts and returns to divine observation (scenario 11).
- **Scope:** D4a: `BeginPossession`/`EndPossession`, the `Possessed` control entry, no policy draws, Mythic upkeep, end by death or exhaustion, learning with the control tag, `PossessionView`, the `DivineControlInput` handler registration. D4b: `ManifestAvatar`/`DismissAvatar`, the avatar body (`BodyKind.Avatar`, `AvatarBodyId`), no biology, damage and destruction (D5.14), `AvatarDrop`, perception by NPCs (`SeenPerson.Body`), the embodiment rule, `AvatarView`.
- **Owned:** `Divinity/DivinityState.D4.cs`, `Divinity/DivinityState.D5.cs`, `Divinity/Possession.cs`, `Divinity/Avatars.cs`, `Divinity/DivinityHooks.D4.cs`, `Divinity/DivinityHooks.D5.cs`, `Divinity/DivinityMindHooks.D4.cs`, `DivinityTuning.D4.cs`, `DivinityTuning.D5.cs`, `DivinityEvents.D4.cs`, `DivinityEvents.D5.cs`, `D4Mappings.cs`, `D5Mappings.cs`, `Tables.D4.cs`, `Tables.D5.cs`, `0010_possession.sql`, `0011_avatar.sql`, tests (contracts §11.5 possession and input tests).
- **Contracts:** R `Control` (F1, frozen; an amendment if a field is missing), `Bodies` (F1b loop sweep). M the possession and avatar state.
- **Events / migration:** **845–849, 850–859**; **`0010`**, **`0011`**.
- **Checkpoint:** D.

### D6 — Commandments and beliefs
- **Objective:** a persistent commandment is revealed, learned, communicated and influences behaviour without universal enforcement (scenario 13). Separate belief, faith, trust, fear and loyalty fields (D5.15).
- **Scope:** Tier 1: the `Commandment` record, `CommandmentRules.Validate`, templates T1, T3, T4, T6, T7 and T12, reveal, revise and revoke, delivery through the D2 path, `KnownCommandment` states and interpretations (with misreading), the D6 part of `DivineTerm`, `Feature.Divine(Commandment*)`, transmission by `Say(Teach)` with `TopicKind.Commandment`, `CommandmentText.Render` (pure). Tier 2: `GodBelief` updates from perceived evidence, `ReligionFounded`/`AffiliationChanged` spells, `Pray`/`Worship`/`MakeOffering` (116–118) and gestures 19 and 20.
- **Excluded:** religious organisations, schisms, worship-fed Mythic (Tier 3); law adoption (C10).
- **Owned:** `Divinity/DivinityState.D6.cs`, `Divinity/Doctrine.cs`, `Divinity/CommandmentTemplates.cs`, `Divinity/Beliefs.cs`, `Divinity/DivineMind.D6.cs`, `Divinity/DivinityMindHooks.D6.cs`, `Information/CommandmentText.cs`, `Information/PeopleCards.Divine.cs` (belief rows), `DivinityTuning.D6.cs`, `DivinityEvents.D6.cs`, `D6Mappings.cs`, `Tables.D6.cs`, `0014_commandments_religion.sql`, tests.
- **Contracts:** R the D2 inbox and delivery. M beliefs, commandments, `BinKind.Shrine` (Tier 2).
- **Events / migration:** **860–879**; **`0014`**.
- **Checkpoint:** first shown in the build after its merge (planned: the C/D build); scenario 13 at F.

### D7 — Archive and resurrection
- **Objective:** a deceased NPC is restored with the same identity and memories, while previously distributed property stays with its owners (scenario 14; D5.12, D5.13).
- **Scope:** the `ArchiveMind` body, `ArchivedLife` in `DivinityState.Archive` (dormant, hashed in `divine`), retention `Off | All | RecentDays` with `ArchiveMax`, `Resurrect` with the Lesser, Full and Restoration variants, `BioAgeOffset`, `NpcResurrected` with `LifeNo`, population conservation P-C, pin P9, `fact_life_spell`. **The contracts' archive model governs** where RESURRECTION_DESIGN proposes packed tiers, a digest and moving the dead out of the hot list (that move would change every hash).
- **Excluded:** Rejuvenate (Tier 3), `AskJoinHousehold`, property claims.
- **Owned:** `Divinity/DivinityState.D7.cs`, `Divinity/Archive.cs`, `Divinity/Resurrection.cs`, `Divinity/DivinityHooks.D7.cs`, `DivinityTuning.D7.cs`, `DivinityEvents.D7.cs`, `D7Mappings.cs`, `Tables.D7.cs`, `0013_resurrection.sql`, tests (contracts §4.4).
- **Events / migration:** **880–889**; **`0013`**.
- **Checkpoint:** E.

### U1 — Camera, selection, input and time; embodied cameras
- **Objective:** functional player controls (Tier 1), then third-person control of a possessed body or an avatar.
- **Scope:** U1a: rotation, pitch, close zoom, follow, the `CameraDirector` overhead mode, `SelectionModel` (person, group, household, area, object), `PlayerInputFrame` and `PlayerCommandBuilder` (divine commands stamped with `HumanGod`), time controls pause/1×/2×/5×/10× with commands queued while paused, names, favourites and follow in the non-hashed `ui` save extra (F1.18). U1b: third-person cameras for possession and avatar, the move axis quantised to `DivineControlInput` sectors with hysteresis and the lease rule (contracts §11.3), smooth switching.
- **Boot flags (R29):** U1a adds `-godonly` (with `-godpopulation`) to the player's command line so the lead can run the god-mode smoke before Checkpoint B. Branches `unity/u1a` and `unity/u1b` in the one folder `wt-lrs-u1`.
- **Excluded:** touch and gamepad adapters, cinematic transitions (Tier 3).
- **Owned:** `Assets/God/Game/Camera/*` (new), `Assets/God/Game/Input/*` (new), `Assets/God/Game/Hud/GodCameraRig.cs`, `Assets/God/Game/Hud/WatchCamera.cs`, `Assets/God/Game/Save/UiExtra.cs` (new), PlayMode tests `Assets/God/Tests/PlayMode/Camera*`. New folders need their Unity `.meta` files committed.
- **Contracts:** R commands, `IDivineView`, `Names`. M the `ui` save extra (not hashed).
- **Events / migration:** none.
- **Checkpoint:** B (U1a), D (U1b).

### U2 — Divine HUD
- **Objective:** the hybrid diegetic and analytical interface (D5.22) for the god-only game.
- **Scope:** U2a: hide the 18 faction command paths when `Control == GodOnly` (blocker 6); the Mythic meter; the toolbar (Instruct, Miracle); the instruct popover with act, target, scope and expiry, using `Check` before `Submit`; the Divine tab on the person card (layer-tagged); response chips; aggregated alerts; Unlimited and Disabled indicators; the "Living island" menu start in god-only mode at Checkpoint B. U2b: possession and avatar pills, the mode switcher, the commandment builder and list (with D6), the spread view.
- **Owned:** `Assets/God/Game/Hud/Divine/*` (new `GodHud.Divine*.cs` partials), the gating edits in `GodHud.cs`, `GodHud.Tabs.cs`, `GodHud.Trade.cs` and `GameMenu.cs`, `HudViewOnlyTests` extensions, PlayMode tests `Assets/God/Tests/PlayMode/DivineHud*`.
- **Contracts:** R `IDivineView`, the command types, the inspection cards. Never reads `GodState` (`HudViewOnlyTests`).
- **Checkpoint:** B (U2a), D (U2b).

### U3 — World, characters and gestures
- **Objective:** the island looks seamless with the grid hidden (D5.18–D5.20). NPCs communicate non-verbally (D5.24); decoded text appears only as an overlay (D5.25). Characters show their state (D5.21). Divine effects animate only real events.
- **Scope:** U3a (Tier 1): no producing-hex pulse and no hex text in normal play, `DevOverlays` flags; **M5.18**: remove unconditional gesture text and lines, add the developer/accessibility decode overlay (off by default), with a hash-independence test. U3b (Tier 2): **M5.19** gesture vocabulary clips (request, offer, give, receive, accept, refuse, warn, cooperate, begging; then pray and teach once D6 and C7 exist) using `GestureMade.DurationTicks`; carried goods in hands; injury, illness, fatigue and pregnancy looks; miracle, compulsion, possession and avatar effects driven by D2–D4 events; settlement clusters and curved paths.
- **Owned:** `Assets/God/Game/Island/*`, `Assets/God/Game/Models/GestureLayer.cs`, `LifeStageLayer.cs`, `PeopleModels.cs`, `FxLayer.cs`, `Assets/God/Game/Animation/*` (new), `Assets/God/Game/Models/DivineFxLayer.cs` (new), PlayMode tests.
- **Contracts:** R events only (it animates events and never decides).
- **Checkpoint:** A or B (U3a, whichever Unity run it makes), D (U3b).

### U5 — Reports and inspection
- **Objective:** institutional and divine reporting (Tier 2), with record versus observation shown side by side (D5.28).
- **Scope:** the Economy tab (possessions, assets, employment, debts); family tree, life history and the memorial with the resurrection panel (with D7); institutional cards and the reports panel (with C6); KPI popovers for the Q1 subset.
- **Excluded:** the SQL console in the player (Tier 3).
- **Owned:** `Assets/God/Game/Hud/Reports/*` (new), `GodHud.People.Economy.cs` and `GodHud.People.Life.cs` (new partials), PlayMode tests.
- **Contracts:** R cards, `IDivineView`, `InstitutionView`-based records views.
- **Checkpoint:** C (Economy tab), E.

### Q1 — Analytics, KPIs and benchmarks (absorbs Q2)
- **Objective:** reconciled warehouse views and the Tier 1 KPI subset; benchmarks for brief §48.
- **Scope:** a baseline `bench` on the base (benchmark slot 1); the early views `0026` (`v_god_day`, `v_intervention_chain`, `v_person_life` and the Tier 1 KPI views) and the late views `0030` (`v_org_history` and the service and religion views) per R18; `dim_kpi` rows for DIV-01..06, DIV-09, DIV-10, DIV-13, POP-12..14, PRD-01, OWN-01, CMD-01/05, HLT-10, LIV-07 and TIM-04 (R30); the `sql` read-only command; P7 `Recording_Neutral_On_New_Domains`; `bench_npc_100` and `bench_npc_2000`, then `bench_divine_500` and `bench_org_500` when their packages merge; `simYearsPerRealMinute`; the DEV_LOG performance table.
- **Owned:** `DotNet/God.Runner/Warehouse/Migrations/0026_*.sql` (the early views, R18), `0030_kpi_views.sql`, `0041_knowledge_money_kpi_views.sql`, `DotNet/God.Runner/Kpi/*` (new), `DotNet/God.Runner/Sql/*` (new), `DotNet/God.Runner/Bench/*` (new cases; `baseline.json`), `DotNet/God.Runner/Scenarios/bench_*.scenario.json` (new ones), `DotNet/God.Runner.Tests/Q1*`, KPI_CATALOGUE status cells.
- **Contracts:** R every package's tables. Never writes another package's mapper.
- **Events / migration:** **700–719** (only if KPI day rows are used). **R18:** **`0026`** holds the **early** views (population, divine core, the Tier 1 KPIs), landed after F1b and D1; **`0030`** lands **late** (after C6, C10 and package D6) with the organisation, service and religion views; `0027`–`0029` stay free for the lead. For owner D6, Q1 also owns **`0041_knowledge_money_kpi_views.sql`** (with K1 and K2; after `0038`–`0040`) and helps I6 with `0037`.
- **Branch (R17):** `unity/q1kpi` in `wt-lrs-q1kpi` (`unity/q1` and `unity/q2` are taken by stage 3).
- **Checkpoint:** B–F (incrementally).

### Q3 — Integration assurance
- **Objective:** prove the end-to-end scenarios headless, and keep replay, save/load and conservation exact across merges.
- **Scope:** `IntegrationFixtures` (short-horizon fixtures, so divine tests do not wait for viability: blocker 10); scenario files `DotNet/God.Runner/Scenarios/civdiv_s01…s18.scenario.json`, each added when its packages merge; `Replay_DivineScenario_Exact` (grown merge by merge); `Replay_SwitchGod`; P6 `Switch_Matrix` after every merge; conservation C1–C8 checks at day boundaries; `Unity_Agrees_With_Runner` on the divine replay (PlayMode, at checkpoint runs); the ACCEPTANCE_TESTS D5 status table. It runs each package's claims independently and reports any discrepancy to the lead. It does not fix the package's code.
- **Branch (R17):** `unity/q3int` in `wt-lrs-q3int` (`unity/q3` is taken; the stale non-git `wt-lrs-q3` folder is not used).
- **Industrial share (owner D6):** the Iron Coast scenario files `DotNet/God.Runner/Scenarios/ind_*.scenario.json` (`ind_slice` first), `IndustrialFixtures`, `IndustrialSlice_EndToEnd` (ACCEPTANCE D6.11) and D6.10 replay, P6 extended with the three new switches, and the D6 status table of ACCEPTANCE_TESTS.
- **Owned:** `DotNet/God.Runner.Tests/Integration/*` (new), `Assets/God/Tests/EditMode/Integration/*` (new), `DotNet/God.Runner/Scenarios/civdiv_*`, `DotNet/God.Runner/Scenarios/ind_*`, `Assets/God/Tests/PlayMode/UnityAgrees*`, the D5 section of `docs/stage4/ACCEPTANCE_TESTS.md`.
- **Contracts:** R all.
- **Checkpoint:** B–F.

### Q4 — Final release (lead)
- **Objective:** the last working playable build and an honest handover.
- **Scope:** the final window (graph doc §8): no new packages; fixes only; full `dotnet test`; Unity EditMode and PlayMode; the Windows build; both smokes and a god-mode smoke; checkpoint tags and the copies of their builds under `Builds/` (not committed); DEV_LOG, STAGE_4_HANDOVER §1–2 and the ACCEPTANCE status written from what actually ran.
- **Owned:** merge commits, the save version bumps (contracts §17), DEV_LOG and the handover.
- **Checkpoint:** F.

### Industrial and knowledge packages (owner D6)

Common rules for every I and K package: branch from `unity/poc` **after I1a has merged** (I1a itself after F1b and C1); never edit a frozen file (contracts §22.12); add rows to another package's tables only by the flagged additive edits of contracts §22.9; never call a money issuance API, never pass `actor <= 0` to `Goods`, never branch on an ownership model, never treat a right as a truth gate (R33, contracts §22.2). Gate at merge as for every package, plus pins **P10–P13**, the static tests of contracts §22.10, and the stage 3 hash. Owning design documents are in `docs/stage4/`.

### I1a — Industrial and knowledge foundation (neutral)
- **Objective:** land every shared industrial and knowledge surface in one behaviour-neutral commit so that I1–I6, K1 and K2 then touch only their own files. Separate worktrees do not make shared-contract changes safe; this is why I1a exists (contracts §22.0).
- **Scope:** contracts §22.1, items I1a.1–I1a.16: wares 40–71 and `PersonalCount` 72 with hash-only-when-non-zero; ledger reasons widened to 16 with the same rule; `MoveReason.BinTransfer = 13`, `OwnershipBasis.SupplyAgreement = 25`, `Paid = 26`, `Goods.MoveBin`, `Wares.IsUnique`; `GodState.Industry` and the `industry` hash part; `CivState.K1`, `CivState.K2`; `SimTuning.Industry`, `Civ.Knowledge`, `Civ.Money` (all switches 0); `IndustryModule` entry points; `IndustryHooks`/`IndustryMindHooks` after Divinity; the movement sites `StepTicks`, `PathCosts`, `Moved`; id ranges, `MaxCand` 40, `NpcMind.Industry`, `NpcMind.Know`; `DesignId` on C1 assets; systems 262 and 264 as empty shells; domains 70–79, catalogue and mapping stubs; save null-acceptance (lead); pins P10–P13.
- **Excluded:** every behaviour; every migration; every row in another package's owned table (contracts §22.9).
- **Owned:** contracts §22.1 column 3; frozen or handed to the named owner after the merge.
- **Gate:** the F1 gate plus P10–P13 and the compile check (`Ware` and `GodState` are used by `God.Game`).
- **Checkpoint:** G (prerequisite).
- **Risk:** it waits for the Tier 1 set. Cut it from `unity/poc` as soon as F1b and C1 have merged; hold the branch until the Tier 1 set has merged; rebase and re-take pins before merging.

### I1 — Industrial content and recipes (with the waste part)
- **Objective:** factories and mines with production orders, finite capacity, material inputs, workers and electricity requirements, and completed physical products (D6.5), on one recipe table and one runner (IND-3).
- **Scope (minimal, for G):** iron ore and coal deposits from the scenario `deposits` block; facilities as C1 assets with input, output, fuel and waste bins; `RecipeBook` rows 1–20 generated from the stage 3 workshop defs (pinned field by field) plus recipes 22, 25, 29, 30, 32; the runner at 264 (consume at start, produce at completion, stall reasons, `Licensed` † flag per R33); production orders placed by an NPC owner and by an organisation's office-holder at the facility; slag and ash outputs into waste bins. **Then:** geology generation, manual extraction, the oil and copper chains, waste sites, recycling (recipe 41), the pollution field (waste part); survey and deposit knowledge spread are Tier 3.
- **Excluded:** toll processing (I3, Tier 3), nuclear fuel (Tier 3), any money.
- **Owned:** see the summary row; `docs/stage4/INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md` and `WASTE_AND_CIRCULAR_DESIGN.md` status cells.
- **Contracts:** R `Goods`, C1 assets and `Work`, C6 decisions, K1 knowledge query (if present). M `IndustryState.Geology`, `.Production`, `.Waste`.
- **Events / migration:** **1000–1019**, **1080–1094**; **`0031`**, **`0036`**.
- **Tests:** IND §11 (incl. `Recipes_Stage3RowsEqualBuildingDefs`, `Recipe_ConsumesAtStartProducesAtEnd`, `Extraction_DepletesFinitely`, `Facility_NeedsWorkersPowerInputs`, `Factory_ProducesAssetWithEmbodiedMatter`, the static tests), `VS4_OreProcessed`, `VS7_FactoryBuildsVehicle`, `Waste_LedgerExact`, `Industry_SaveLoadParity`.
- **Checkpoint:** G.

### I2 — Vehicles and harvesting
- **Objective:** a persistent NPC operates a harvester that collects physical ore and delivers it (D6.4, slice steps 1–3, 5, 8, 9a).
- **Scope (minimal):** vehicle defs 1 (harvester) and 2 (hauler); `Vehicles.Register` from a factory cycle or the scenario (`Placed`); cargo, tank and ammunition bins moved with `Goods.MoveBin`; Board, Drive, Alight, Load, Unload, Refuel; fuel burn per node and `OutOfFuel`; the harvest routine as a candidate with a routine term in the operator's own score; maintenance and wear. **Then:** possession and compulsion of operators (D4a, D2 rows 22/23), breakdown draws, tankers.
- **Excluded:** congestion, haulage for others and transport companies (Tier 3), wrecks and dismantling (with the waste part, Tier 3).
- **Events / migration:** **1020–1039**; **`0032`**.
- **Tests:** VEH §11 (`Vehicle_NoNpcRecord`, `Vehicle_MovesOnlyWithDriver`, `Vehicle_BinsFollow`, `Vehicle_FuelBurnExact`, `Vehicle_LoadUnloadReachChecked`, `Vehicle_SaveLoadMidTrip`, `Vehicle_ReplayExact`), `VS1`, `VS2`, `VS3`, `VS5_CoalDelivered`, `VS8`, `VS9_FuelCutStopsHarvester`.
- **Checkpoint:** G.

### I3 — Refineries and industrial trade (no external buyer)
- **Objective:** processed goods change hands only through existing mechanisms; a recorded sale is an ownership change plus an actual payment (slice step 4, sale branch; ACCEPTANCE D6.2).
- **Scope:** `SupplyAgreement` records agreed by both parties' office-holders; deliveries with ownership at the unload (basis 25) and a `Supply` obligation (C2); `IndustrialSaleClosed` linking `OwnershipChanged` and the payment (K2a, or goods); breach recorded, never enforced. **No** external buyer, price table or market sink (D6.2, D6.6).
- **Events / migration:** **1040–1049**; **`0033`** (payment ids as plain columns, contracts §22.7).
- **Tests:** `VS4_OreSold`, `ACC_DeliveryUnderAgreement`, ACCEPTANCE D6.2 (with K2a).
- **Checkpoint:** G (optional branch of step 4).

### I4 — Electricity generation and distribution
- **Objective:** a power station receives and consumes physical fuel, and electricity reaches a connected factory through built links; disruption matters (slice steps 5, 6, 9b, 9c; ACCEPTANCE D6.3, D6.4).
- **Scope (minimal):** the coal station design as a staffed facility (on/off by a staff act), ports and links as C1 assets, components by union-find, the interval balance at 262 (demand, dispatch, allocation by priority, shortage, outage), `PowerPermille` to the runner, fuel burn and ash, the energy ledger. **Then:** one meter and one billing act as a C2 `Bill` obligation (needs C2 and K2a), port priorities by policy.
- **Excluded:** oil, hybrid, waste, nuclear, wind and solar designs, rationing schedules, link capacity, storage (Tier 3).
- **Events / migration:** **1050–1069**; **`0034`**.
- **Tests:** ELE §11 (`Grid_LedgerIdentity`, `Station_BurnsExactFuel`, `Station_NeedsStaff`, `Grid_NoMoneyApi`, `Grid_SaveLoadMidInterval`, `Grid_ReplayExact`), `VS5_StationBurnsDeliveredCoal`, `VS6_PowerReachesFactory`, `VS9_FuelOrLinkCut`, `ACC_ElectricityNoMoney`, `ACC_FuelShortageCutsGeneration`.
- **Checkpoint:** G.

### I5 — Mechanised military production
- **Objective:** military organisations obtain equipment through the same factories, labour, fuel and power as civilians (D6 "Military production"; no separate military economy).
- **Scope:** recipes 33 (ammunition) and 34 (firearm); a procurement agreement (an I3 agreement); a military organisation's commander placing orders; delivery with ownership (basis 25) and an obligation; one tank built and crewed by two employees and driven (no combat). `VS11_TankFromSameFactory`.
- **Excluded:** vehicle combat, engineers, artillery, aircraft, naval, the strategic device (Tier 3).
- **Events / migration:** **1070–1079**; **`0035`**.
- **Checkpoint:** after G.

### I6 — Industrial presentation and analytics
- **Objective:** the player sees industry working inside the seamless island (D6.19), and the warehouse reports it.
- **Scope (minimal, for G):** deposits drawn as outcrops and seams (grid hidden, U3a conventions); facilities with activity only while `Working` events say so; harvesters interpolated from `VehicleMoved` with visibly heaped cargo; facility, vehicle and power-station cards (owner, workers, queue with progress, stall reason, cargo, tank, output, fuel days left) registered through U1's hook (R9); the electricity overlay (off by default) and shortage glyphs (`NoPower`, fuel can). **Then:** logistics overlay, production queue panel, organisation inspection, `0037` views and the IND/VEH/ELE/WST/ENV/IMIL KPI rows.
- **Contracts:** R events and inspection cards only (the Game layer never mutates sim state); never reads `GodState` directly (`HudViewOnlyTests`).
- **Events / migration:** **1095–1099** (only if KPI day rows are used); **`0037`**.
- **Tests:** PlayMode `Industry*` (cards fill from the view, overlay toggles, no hex text in normal play), compile check, `Unity_Agrees_With_Runner` on `ind_slice` (H-GS-12, with Q3).
- **Checkpoint:** G.

### K1 — Knowledge, designs, artefacts and licences (minimal)
- **Objective:** the explicit knowledge ontology of D6.9–D6.11: knowledge copies without loss, documents are goods, rights are legal claims, practical ability is distinct; knowledge (never a right) gates production (R33).
- **Scope (minimal):** the `DesignDef` envelope and loader with the slice's designs (harvester, smelter, coal station, factory, hauler; others common); founders' seeded knowledge; `KnowledgeMind` with levels and fidelity; the build and operate minima used by I1, I2, I4; teaching through C7's `Teach` with a design topic; documents: write, copy, read, build from a document; a seeded patent law, the patent-office template, grant, assignment and one licence with a `LicenceFee` (K2a); `UnauthorisedDesignUse` classification, `InfringementPerceived` and the complaint candidate (stub record until C9). **Tier 3:** challenges, revocation, court process, compulsory licensing, research projects and discovery by practice, invented variants, `RevealTechnology`, auctions (`0043`).
- **Events / migration:** **1100–1129**, **1130–1159**; **1160–1169** (Tier 3); **`0039`**, **`0040`**; `0043` (Tier 3).
- **Tests:** KNOW §16 (KN-U1–U7, KN-INV-1–3, RT-INV, AR-INV), `Rights_NeverGate`, ACCEPTANCE D6.5–D6.8, P12.
- **Checkpoint:** G (VS7, VS8 minima); D6.5–D6.8 demonstrations.

### K2 — Money and accounting core (K2a, K2b)
- **Objective:** one monetary and accounting architecture (D6.2): no credits, no external buyer, money only by explicit mechanisms, revenue only from actual payments.
- **Scope K2a (Tier 2):** the seeded commodity money (`Coin` in Iron Coast; `SliceCommodity` otherwise); the `Payment` record and the one payment act (C3's `Pay` 101 generalised); spot and obligation settlement; organisation double-entry books in native units with records written by treasurers; units of account and accepted units as policies; billing acts (`IssueBill`); the non-loan obligation events 1190–1194 emitted by C2's API; static `NoExternalBuyer`, `MoneyWriters_OnlyK2`; P13. **K2b (Tier 3):** issuers, accounts, issuance E1–E4 and destruction D1–D5, notes.
- **Events / migration:** K2a **1170–1179**, **1190–1199**, **`0038`**; K2b **1180–1189**, **`0042`**.
- **Tests:** MONEY §14 (MON-U1–U7, MON-INV, BK-INV), ACCEPTANCE D6.1–D6.3 money halves, W-D6-3, W-D6-4.
- **Checkpoint:** G (step 4 sale branch, payments); D6.2, D6.3.

---

## 3. Explicitly deferred (kept in the long-term design)

None of these is dropped from the design. Each keeps its contract slots (ids, codes, migration numbers) so a later package lands without renumbering.

| Item | Tier | Kept in | Reserved slots |
|---|---|---|---|
| C4 persistent military individuals, recruitment, desertion; soldiers as people (M9); retiring `MilUnit` | 3 | MILITARY_DESIGN, ORGANISATIONS §9 | codes 400–429, `0022`, verbs 124–125 |
| C9 justice: mediation, complaints, investigation, courts, policing, detention, restitution (C1 lands only theft classification) | 3 (mediation 2, low) | INSTITUTIONAL_SERVICES §5.6 | codes 560–589, `0021`, verbs 111–113 |
| C8 banking: deposits, interest, institutional loans, insolvency procedures | 3 | INSTITUTIONAL_SERVICES §8 | codes 552–559, `0020` banking columns |
| C5 beyond minimal citizenship: migration decisions, loyalty, defection, political interests, autonomous political organisations | 3 | POLITICAL_ECONOMY, ORGANISATIONS | codes 291–299, verb 126 |
| C10 beyond the first version: advanced religious institutions, canon, priests' interests, schisms, cultural transmission (M11), traditions and norms | 3 | COMMANDMENTS_AND_RELIGION §4.7, CULTURAL_TRANSMISSION | codes 355–374 |
| D6 advanced: conversion as an organisation decision, `Renounce`, templates T2, T5, T8–T11 as their verbs land, worship-fed Mythic (`WorshipMythicSource`) | 3 | COMMANDMENTS_AND_RELIGION §9, MYTHIC §4.2 | — |
| D3 additional miracles: RevealTechnology, area miracles, weather, terrain; D7 Rejuvenate | 3 | MYTHIC_AND_MIRACLES §6.10 | `MiracleKind` 11, 13, 64+ |
| D1 `Followers`/`Perceived`/`Reported` divine sight; trained god policies (no network training in this run, D5.31) | 3 | INFORMATION §12.3, AI_TRAINING §14 | codes 890–899 |
| Several possessions, possessing animals, avatar plus possession at once | later | POSSESSION_AND_AVATAR §10 | `BodyKind` 2+ |
| U6 tutorial rewrite, tutorial camera scripts, presentation polish; mobile touch adapter, gamepad; captions | 3 | GAMEPLAY_SCENARIOS §10, CAMERA §12, PLAYER_UI §22 | — |
| U3 far-range markers, instancing, ecological decoration, emotion posture detail, per-household clothing | 3 | VISUAL_WORLD §15, NPC_ANIMATION §14 | — |
| U5/Q1 deeper institutional analytics, player SQL console, `bench_npc_5000`, nightly long runs with divinity and organisations | 3 | KPI_CATALOGUE §15.8, HEADLESS §12.8 | — |
| Stage 3 retirement: walkers (M6 in full), `AiBrain` (M14), stage 3 mode (owner decides) | later | DEV_LOG "Temporary mechanisms" | — |
| I-BS follow-ups not needed for Checkpoint A: controlled I-BS.6/8/10 tests, the M5.9 kin cue; M3 LS3 and LS4 redesigns; M5.6 avoidance | 2 (filler) | ACCEPTANCE_TESTS, DEV_LOG | — (good filler for a free Sonnet slot) |
| I5 combat and beyond: vehicle combat through `Duel`, engineers, reconnaissance, artillery, aircraft, naval; the strategic device, inspection, disarmament, deterrence | 3 | INDUSTRIAL_MILITARY §4.7, §10 | 1070–1079, `0035`, `FacilityKind.StrategicSite`, `SituationKind` 132 |
| K1 beyond the minimum: challenges, revocation, court process (with C9), compulsory licensing, research projects, discovery by practice, invented variants, `RevealTechnology`; auctions | 3 | KNOWLEDGE_AND_TECHNOLOGY §8–11 | 1110–1112, 1139–1145, 1160–1169, `0043`, `ConsumeReason.Research` 13, episode kinds 20–21 |
| K2b institutional money, accounts, issuance, notes, interest; banking with C8 | 3 | MONEY_AND_ACCOUNTING §3.3, §4 | 1180–1189, 1194, `0042`, `MoneyId`, `AccountId` |
| Industry extensions: survey and deposit knowledge spread, uranium and nuclear fuel, oil/hybrid/waste/nuclear/renewable stations, rationing and tariffs, storage, toll processing, marketplaces, haulage for others, wrecks and dismantling, consumption refuse, contamination, waste authorities and regulators | 3 | IND, VEH, ELE, WST §9–10 | their codes in 1000–1099; `Ware` 44, 52, 62 |

---

## 4. Checkpoint G — Industrial slice (owner D6.18)

**What it is.** The ten-step industrial vertical slice of D6.18, run as one world: the `ind_slice` scenario of "Iron Coast" (GAMEPLAY_SCENARIOS §13: one faction, about 30 people, a mining company, a cooperative smelter, a public utility with one coal station, a private factory, a small transport company, no military; 5–10 sim days; checkpoints every sim day; `Industry`, `Civ`, `Orgs`, `Divinity`, `Civ.Knowledge`, `Civ.Money` on; `Orgs.C10.SeedGovernments = 1`). It is the same simulation as Living Island with different initial conditions (H-GS-13).

| Step (D6.18) | Test (ACCEPTANCE D6.11) | Packages |
|---|---|---|
| 1 A persistent NPC operates a harvester | `VS1_NpcOperatesHarvester` | I2 (C3 for the contract; K1 for the skill minimum if merged) |
| 2 The harvester collects physical ore | `VS2_HarvesterCollectsOre` | I2, I1 |
| 3 It delivers the ore to a refinery | `VS3_DeliversToSmelter` | I2 |
| 4 The ore is processed or sold through a recorded transaction | `VS4_OreProcessed` (I1) **or** `VS4_OreSold` (I3 + K2a + C2) | I1 (processed branch suffices for G) |
| 5 A power station receives and consumes fuel | `VS5_CoalDelivered`, `VS5_StationBurnsDeliveredCoal` | I2, I4 |
| 6 Electricity reaches a connected factory | `VS6_PowerReachesFactory` | I4 |
| 7 The factory produces a vehicle from actual materials, labour and power | `VS7_FactoryBuildsVehicle` | I1, I4 (K1 minima if merged) |
| 8 An existing NPC operates the completed vehicle | `VS8_NewVehicleOperated` | I2 |
| 9 Disrupting fuel or connectivity affects production | `VS9_FuelCutStopsHarvester`, `VS9_FuelOrLinkCut` | I1, I2, I4 |
| 10 Everything reconciles and replays | `VS10_IndustryReconciles`, `VS10_EnergyReconciles`, W-D6-1, D6.10 replay | Q3 with all merged I/K packages |

- **Packages required:** I1a, I1 (minimal), I2 (minimal), I4 (minimal), Q3 (`ind_slice`, `IndustrialSlice_EndToEnd`), I6 (minimal). **Used if merged:** K1 minimal, K2a, I3, C2, C3.
- **Demo:** the god watches a harvester crawl from the hill mine to the smelter with visible ore, coal delivered to the station, a lit factory bay assembling a harvester, a named worker boarding the new vehicle; the player opens the facility, vehicle and station cards and the electricity overlay, then cuts a link (by compelling a lineman, or by the station's coal running out) and sees the outage glyphs and the factory's queue stall, then recover.
- **Gate:** the checkpoint gate of the graph doc §9 plus D6.11's own gate: every step on seeds 1–3 in one run each; a step whose package has not merged is reported **not run**, never passed.
- **When:** after the D5 checkpoints; the graph doc §6 projects it **after the 18-hour window**, in the continuation run, unless the D5 packages finish early. G never displaces A–F.
