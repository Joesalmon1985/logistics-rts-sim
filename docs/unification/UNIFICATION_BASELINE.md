# Unification baseline: what the god-only Living Island runs today (28 Nov 2026)

This answers brief §13, "Unify the existing architecture" ([OWNER_BRIEF_2026-11-28_UNIFICATION.md](OWNER_BRIEF_2026-11-28_UNIFICATION.md)), against the decisions D8.1-D8.18 in [../civdiv/OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md). It replaces the stale parts of [../civdiv/CURRENT_IMPLEMENTATION_BASELINE.md](../civdiv/CURRENT_IMPLEMENTATION_BASELINE.md), which dates from 26 Nov, before `unity/ibs`, `unity/demog`, Civ, Orgs and Divinity were merged (that file says they are unmerged; `git branch --merged` now lists them).

## 0. Method and evidence status

- **Revision read:** `unity/poc` at `b297cb0` ("Merge final MVP gate: C7, god-only game with Civ and Orgs on, F2c pin fix").
- **How:** read-only source inspection (Grep, Glob, Read) of `Assets/God/Sim`, `Assets/God/Game/Boot`, `DotNet/God.Runner/Scenarios` and the Stage 4 docs. File:line references are to that revision.
- **Not done:** no test was run, Unity was not opened, no game or scenario was executed, no code was changed. Everything below is therefore **static evidence** (what the code does when it runs), not an observed run. Behavioural claims about what the AI factions actually do in a god-only game (wars, expansion, how far the HQ economy gets) are marked **[unverified]**.
- Marks: **[proved]** = shown by the cited lines; **[inferred]** = follows from code but not shown by a run; **[unverified]** = could not be checked statically.

## 1. Headline findings

1. **The god-only Living Island runs the complete Stage 3 game for all six factions, unchanged, beside the NPC population.** `-godonly` / `StartGodOnly` deals a setup with no human faction (`GodBootstrap.cs:187`, `:354`), and the bootstrap then starts the Stage 3 `AiBrain` for **every** faction, because "non-human" is all six (`GodBootstrap.cs:296` -> `AiHost.cs:31-35` -> `SimBuilder.AttachAi` -> `AiDirector.NonHuman`, `AiDirector.cs:102-107`).
2. **The two games are coupled only through geometry and shared dice piles, never through goods.** No NPC-side system (Population, Households, Social, Civ, Orgs, Divinity, Cognition, Learning) reads or writes `EcoBuilding.Stock`, HQ reservations or walkers (grep, §3). The one shared physical object is `Territory.Piles` (dice piles): faction harvester walkers and NPC hand-gatherers draw on the same log piles (`PopulationSystem.cs:497-515`, `:1196-1209`; `EconomySystem.cs:368-370`). The other coupling is HQ circle geometry (§7).
3. **The NPC side already has working replacements for most parts** (assets, building, work, take-out, employment, communal store, foraging, barter, huts and households), but each is **built on hand-gathered logs and wild food stocks**, which D8.3/D8.5/D8.6 forbid or change, so reuse needs re-seating on a gradual-materialisation raw-material layer, not just switching Stage 3 off.
4. **There is no settlement, road or HQ command for NPCs.** `PlaceHq`, `PlaceBuilding`, `BuildRoad`, `PlaceFlag` are `FactionCommand`s (`FactionCommandSet.Admissible` drops anything else, `AiDirector.cs:140`). NPCs can only walk faster on roads that the faction AI built (`PopulationSystem.cs:883-889`).
5. **No planning layer exists.** Cognition is single-step scoring over candidate intents (`Thinker.BuildCandidates`, `AssociativeNpcPolicy`); multi-step behaviour (gather log, then build hut) is two instincts gated by what the NPC carries. There is no goal object, no recipe knowledge, no material sub-goal derivation (§10).
6. **The terrain, wares, starting population and recipes differ from D8 in every dimension** (§4-§8): 5 land terrains plus 2 never-dealt kinds and no sea or pasture on the island; 4 raw piles; 120 founders (84 adults, 36 children) not 72; Stage 3 pairwise recipes; finite regrowing wild food.
7. **A scenario-level switch for "no faction AI" exists** (`policies.faction = "none"`, `PolicyRegistry.None`, `SimBuilder.AttachPolicies` seats only non-`none` factions, `SimBuilder.cs:113-117`), but the player bootstrap does not use it, no population scenario file sets it (all keep the Stage 3 AI: e.g. `band_6x20.scenario.json` "the stage 3 AI running each faction's state"), and I found **no test that runs the full Living Island with it** [unverified that such a run is healthy].

## 2. Proof: what the god-only Living Island runs

### 2.1 Boot path (player)

| Step | Evidence |
|---|---|
| Menu starts it | `GameMenu.cs:121` -> `GodBootstrap.StartGodOnly(seed, MythicChoice)` |
| Setup | `GodBootstrap.cs:351-365`: `GameSetup(seed, NoHuman)` with `Population = true`, `StartNpcsPerFaction = 20`, `Victory = SandboxOnly`, `Control = GodOnly`, `HumanGod = GodOf(F0)`; tuning `Divinity.Enabled = 1`, `Civ.Enabled = 1`, `Orgs.Enabled = 1`, `FounderTuning.EstablishedBand`; `PendingStartAi = true` |
| Same via flag | `GodBootstrap.cs:192-202` (`-godonly`: 20 per faction, Divinity/Civ/Orgs on, established band); `:165` lists `-godonly` among the flags that skip the start card; `:208` sets `startAi = true` for any flagged start |
| Sim built **without** policies | `GodBootstrap.cs:206` `SimBuilder.Create(setup, tuning, attachAi: false)` |
| AI attached afterwards | `GodBootstrap.cs:238` `if (startAi) StartAi()`; `:292-299` `StartAi` calls `AiHost.StartForGame(Sim)` then `SimBuilder.AttachGods` (gods are attached only here, `:297`) |
| All six get the Stage 3 brain | `AiHost.StartForGame` -> `Start(sim, AiDirector.NonHuman(sim.State))` (`AiHost.cs:31-35`); `NonHuman` returns every faction when `!HasHuman` (`AiDirector.cs:105`); `AiDirector.Attach` with no assignment uses `PolicyAssignment.StageThreeFaction` = `"AiBrain"` (`AiDirector.cs:88`, `PolicyRegistry.cs:118`) -> `AiBrainFactionPolicy(new AiBrain(...))` (`StandardPolicies.cs:19-21`) |
| Only opt-out | the `-godnoai` command-line flag (`AiHost.cs:33`); it is a host flag, not saved state |
| Views of Stage 3 still built | `GodBootstrap.cs:226-231`: `TerritoryView`, `EconomyView`, `MilitaryView`, `StandardLayer` over the HQs |
| Dev chain bots | `ChainBot`s (`GodBootstrap.cs:101`, `EconomyBot.cs`) exist only when no brain drives a faction (dev F2); not started in the god-only game |

Note: `InstitutionDirector` is attached only in `SimBuilder.AttachPolicies` (`SimBuilder.cs:94`), which the player path never calls (it uses `AttachAi`). So the player's god-only game has no institution policy seat; organisations arise through NPC decisions only (`Orgs.Emergence = 1` default, `OrgTuning.cs:16`) [proved by grep: the only call site is `SimBuilder.cs:94`].

### 2.2 System order of the god-only game (all installed unconditionally except where noted)

`GodSim.Create` runs `StartGame` of Factions, Territory, Military, Economy, Trade, Population (+M4/M5), Civ, Orgs, Divinity (`GodSim.cs:107-116`), then `InstallModules` (`GodSim.cs:123-135`). Systems run in ascending order:

| Order | System | Source | Stage 3 / NPC / other |
|---|---|---|---|
| 10 | `DiceSystem` | `TerritorySystems.cs:91` | Stage 3 (raw drops) |
| 200 | `EconomySystem` | `EconomyModule.cs:34` | Stage 3 (HQ stock, walkers, production) |
| 230 | `DivinitySystem` | `DivinitySystem.cs:22` | divine (retain) |
| 240 / 255 | `LifeCycleSystem` / `EstateSystem` | `HouseholdHooks.cs:26-27` | NPC (M4) |
| 245 | `SocialSystem` | `SocialSim.cs:48` | NPC (M5) |
| 250 | `PopulationSystem` | `PopulationModule.cs:42` | NPC (M3) |
| 252 | `MiraclesSystem` | `Miracles.cs:45` | divine |
| 257 | `CivSystem` | `CivModule.cs:26` (only with `Civ.Enabled`) | NPC (C1-C5) |
| 260 | `OrganisationSystem`, `DoctrineSystem` | `OrgModule.cs:41`, `DivinityHooks.D6.cs:32` | NPC/divine |
| 300 | `MilitarySystem` | `MilitarySystem.cs:348` | Stage 3 |
| 350 | `TradeSystem` | `TradeSystem.cs:179` | Stage 3 |
| 400 | `FactionSystem` | `FactionRules.cs:367` | Stage 3 (elimination, victory) |
| 900 | `AiDirector` | `AiDirector.cs:28`, `:94` | **Stage 3 faction AI** |
| 910 | `GodDirector` | `GodDirector.cs:43` | divine AI seats |

So in a god-only game Stage 3 occupies orders 10, 200, 300, 350, 400, 900 beside the NPC systems at 230-260. They share one `GodState` and one `StateHash` (`GodSim.cs:134-170`: territory, economy, military, factions, trade parts plus the population, M4, M5, Civ, Orgs, Divinity parts).

### 2.3 What the Stage 3 side starts with

- One finished HQ per faction on a spread-out start corner (`TerritoryModule.StartGame`, `TerritorySystems.cs:110-115`; `StartCorners.Pick`, `FactionRules.cs:247-`), each with 6 carriers and 2 builders (`EconomyModule.cs:55`, `EconomyTuning.cs:8,10`) and a start stock of 8 log, 8 stone, 6 clay, 4 grain, 8 plank, 8 brick, 6 mortar (`EconomyTuning.cs:90`, `EconomyModule.GiveStartStock`).

## 3. Stage 3 and parallel systems: audit and decisions

The decision column applies D8.16 (retain / replace / remove from normal play / keep only in the Stage 3 regression scenario). "Regression only" means: stays compiled and working with `GameSetup.Population` off or an explicit regression scenario, never attached in the normal game. The Stage 3 pins to protect are `PolicyRunTests.cs:27` (seeds 1-3 smoke hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87`) and the both-off neutrality pins in DEV_LOG [pin values as recorded in docs; not re-run].

| # | System | What it does (evidence) | Runs in god-only? | NPC replacement that exists | Decision |
|---|---|---|---|---|---|
| S1 | **Faction economic AI**: `AiBrain` behind `AiBrainFactionPolicy` | One `Think` every 20 ticks per faction (`AiTuning.cs:15`, max 2 per tick `:17`; `AiDirector.Tick` `:126-147`): `Look`, `Diplomacy`, `Army`, `Trader.ConsiderOffers`, `Priorities`, `Build`, `KeepRaws`, `Roads`, `Trader.ProposeTrades` (`AiBrain.cs:112-130`). Reads **true** state (`AiBrainFactionPolicy.cs:11-14`, "TEMPORARY ... Q21 breach") | **Yes**, all six factions [proved, §2.1]. What it achieves in practice: [unverified] | none needed; the decisions it takes belong to NPCs (C1 build, C3 employment, M3 forage, M5 barter) | **Remove from normal play; keep in Stage 3 regression scenario.** Replace the attach step, not the class |
| S2 | **Faction policy seat** | `PolicyLevel.Faction`; `none` leaves a faction unseated (`SimBuilder.cs:113-117`); `IdleFactionPolicy` (`FactionPolicies.cs:36-41`) | The seat is the vehicle of S1. Player path bypasses `PolicyAssignment` (`AiHost.cs`) | the seat can stay as the divine/organisational extension point (institution seat is separate) | **Retain the seam, set to `none` in the normal game**; make the unified game attach nothing unless a scenario says so |
| S3 | **Automatic faction construction** | `AiBrain.Build/Want/Cover/Expand/Citadel/Roads` (`AiBrain.cs:516-591`, `:647`, `:807-846`, `:847`, `:880`); emits `PlaceBuilding` (`:691`), `PlaceHq` (`:829`), `UpgradeToCitadel` (`:869`), `BuildRoad` (`:710,835,890-906`), `PlaceFlag` (`:916`), `UpgradeBuilding` (`:607`) | Yes (via S1) | Individual building: C1 `Work.FoundSite/Build` (`Work.cs:44-100`), M3/M4 huts. **Settlement and road founding by NPCs: nothing** | **Replace** (needs new NPC/group settlement and road acts, §6); Stage 3 builder AI **regression only** |
| S4 | **HQ inventories / faction stockpiles** | `EcoBuilding.Stock/StockReserved` for HQs, `In/Out/Incoming/OutReserved` for workshops (`EconomyState.cs:59-81`); start stock `EconomyTuning.cs:90` | Yes. No NPC system reads them | Owned physical inventories: `Npc.Carry`, `LotBin` household/asset/org/communal stores (`LotBin.cs:50`, `Goods.cs`), `Owner` kinds (`LotBin.cs:6`) | **Replace** by owned bins (D8.16); HQ stock regression only |
| S5 | **Anonymous carriers** | `Walker` kind Carrier, 6 per HQ, `Match`/`Launch` over flag routes (`EconomySystem.cs:451-604`; `EconomyState.cs:139`) | Yes | C1 `VerbHaul` (log to own workshop), C1c take-out (`Work.cs:11,18`), M5 hand-over, C6 deposit/serve. Gap: no general "move goods from A to B on request" act beyond log haul | **Remove from normal play**; regression only. Replace with NPC haul that generalises C1 haul (gap G6) |
| S6 | **Anonymous builders** | `DispatchBuilders` (`EconomySystem.cs:204-232`), 2 per HQ; `SiteTick` needs a road-connected HQ (`:258-279`) | Yes | C1 `Work.Build` (materials in one per trip, then work ticks, `Work.cs:86-130`); M3 hut build | **Replace** (C1/hut build is the model); regression only |
| S7 | **Anonymous harvesters** | `Walker` kind Harvester takes a pile, reserves `PilesReserved`, returns 4-6 wares (`EconomySystem.cs:337-395`; `EconomyTuning.cs:27,38`) | Yes | None for raw materials: NPCs hand-gather **logs only** (`PopulationSystem.cs:966-1000`). Clay/grain/rock piles have no NPC collector | **Replace** with NPC collection tied to a collection building (D8.5/D8.6); regression only |
| S8 | **Widelands flags** | `Flag` per building and per road split; flag min gap 2 (`TerritoryTuning.cs:12`); `TerritoryRules.AddBuilding` makes the flag (`TerritoryRules.cs:105-125`); HQ site needs a free flag node (`:26-36`) | Yes (every faction building has one) | none, none needed (D8.11) | **Remove from normal play**; regression only. NPC building placement must not require a flag node |
| S9 | **Road-segment logistics** | Roads are chains of adjacent nodes between own flags (`TerritoryRules.CheckRoad` `:244-275`); routing `FlagRoute` (`EconomySystem.cs:41-56`) | Yes | NPCs already walk faster on road nodes (`PopulationTuning.RoadStepTicks = 4` vs `StepTicks = 6`, `PopulationSystem.cs:883-889`; `NodePather`) and do not need roads | **Split:** keep the road as terrain-with-speed (retain, D8.11), remove flags/segments/carriers (S5, S8); who may build one changes (gap G3) |
| S10 | **Faction-level production and reservations** | `Produce`/`Cycle` workshop cycles (`EconomySystem.cs:234-258`, `:398`), 10 processors + 10 armouries + barracks (`BuildingDefs.cs:130-160`); reservations `StockReserved/OutReserved/Incoming/PilesReserved` | Yes | C1 `Work.Operate` (recipe run by a worker, output owned by the asset's owner, `Work.cs:143-190`); C3 employment | **Replace** with NPC-operated recipes from the D8.12 table; Stage 3 recipes become data for the regression scenario |
| S11 | **Faction trade / envoys** | `TradeSystem` (350): envoys walk to HQs; `AiTrader` offers/answers; `AiTradeJudge` (`AiDirector.cs:97`, `TradeSystem.cs:1-40`) | Yes | M5 barter and gifts (`SocialSim`, `Valuation`, verbs `Give` 48, `HandOver` 49, `Say*` 65-74) | **Remove from normal play**; regression only. Note valuation only knows food, log and axe (`Valuation.cs:22-41`, gap G5) |
| S12 | **MilUnit military** | `MilUnit` list (`MilitaryState.cs:32`), barracks factory (`BuildingDefs.cs:155-160`), `Stage3MilitaryPolicy`, `AiBrain.Army/Diplomacy` (`AiBrain.Military.cs:179` `DeclareWar`, `:261-268` `MarkTarget`). NPCs only *see* enemy soldiers as Fear (`PopulationSystem.cs:485-495,695`); nothing kills or conscripts NPCs (no `Population` reference in `Military/`) | Yes [system installed `GodSim.cs:125`; whether wars happen is **unverified**] | none (D8.9: no professional military at start) | **Remove from normal play**; regression only. Later soldiers are NPCs (D5/M9) |
| S13 | **Victory points / HQ elimination** | VP = 1 per finished HQ, 2 per Citadel (`FactionRules.cs:15-21`, `FactionTuning.cs:8,11`); `FactionSystem` eliminates a faction with no HQ (`FactionRules.cs:326`); sandbox victory never ends the game (`Victory.cs:38`) | Yes (sandbox) | none; no NPC-side scoring | **Remove from normal play** (no VP in the unified game); keep for regression. Elimination-by-HQ must be disabled or redefined (R6) |
| S14 | **Stage 3 views and HUD** | `TerritoryView`, `EconomyView`, `MilitaryView`, `StandardLayer`, ChainBot (`GodBootstrap.cs:226-231`) | Yes | `NpcLayer`, `DivineFxLayer` (`:231`) | **Retain code, hide in normal play**; presentation is a later package |
| S15 | **Dice** | `DiceSystem` rolls 2d6 every 60 ticks (`TerritoryTuning.cs:7`); each hex with that hidden number gets one pile at once (cap 3, extras wasted) (`TerritorySystems.cs:45-72`) | Yes | none; NPC hand-gather of log piles (`PopulationSystem.cs:497`) | **Retain the roll; change the effect** to gradual materialisation (D8.4), gated by a switch so Stage 3 hashes hold (§6) |
| S16 | **Automatic mechanisms** (DEV_LOG "Temporary mechanisms") | immigration off by default (`HouseholdTuning.cs:13`, `LifeCycle.cs:31`); HQ paymaster `IPayPoint` **not implemented** (grep: no type); granary keeper not built | Immigration off | n/a | Keep off; list in the unification package for removal |

### 3.1 Parallel building representations (D8.8 target: one architecture)

| Representation | Where | Location rule | Costs | Ownership/access | Stays? |
|---|---|---|---|---|---|
| Stage 3 `Building` / `EcoBuilding` / military `BuildingRecord` (three parallel records per building) | `TerritoryState`, `EconomyState.cs:34`, `MilitaryState.cs:129` | node inside an **own finished HQ circle** only (`TerritoryState.IsBuildable` `:192-198`); needs flag node, free neighbours, halo rules (`TerritoryRules.cs:70-102`) | planks, bricks, mortar (`EconomyTuning.cs:72-80`) | faction | regression only |
| M3/M4 `Hut` | `PopulationState.cs:51-78` | free land node **outside every HQ circle**, `HutSpacing` 3 from other huts, not on a source (`PopulationSystem.cs:658-668`) | 4 logs + 150 work ticks (`PopulationTuning.cs:106-108`) | per-bed `Rights[]`, household adoption; no `Owner` | fold into the capability building (shelter) |
| C1 `ProductiveAsset` | `Assets.cs:109-`, rows `Assets.cs:80-88`: Field (1 log; 2 roots per 60 ticks), Workshop (2 logs; 1 log -> 1 plank per 80 ticks), Store (2 logs) | **outside every HQ circle** (`Work.cs:68`), 1 node clear of huts, sources and assets, `MaxAssetsPerOwner` 2 (`CivTuning.C1.cs:15`) | logs | `Owner` (npc/household/org) + use rights + disputes + condition (`Ownership.cs`) | **the base of the D8.8 architecture** (it already has Owner, Rights, Store, Staff slots, Condition) |
| C6 organisation place and bin | `Organisation.Node`, `BinKind.CommunalStore/OrgStore` (`LotBin.cs:50`) | not checked in this audit [unverified] | n/a | org record | keep as an owner kind |
| M4 household bin | `BinKind.HouseholdStore` | at the hut | n/a | household | keep |

## 4. Geography, raw materials and food vs D8.2-D8.4

### 4.1 Current terrain kinds, deal and pile kinds

| Code id | Terrain (`HexCoord.cs:41-57`) | Dealt on the island? | Raw pile kind (`RawKinds.OfTerrain`, `Faction.cs:58-72`) | D8.2 counterpart |
|---|---|---|---|---|
| 0 | `Meadow` | no (board generator never deals it; "rim of a larger board") | none | none (no hex type) |
| 1 | `Forest` | 8 | `Log` | Forest |
| 2 | `Hills` | 6 | `Clay` | Wetland (clay) (different name and look) |
| 3 | `Fields` | 7 | `Grain` | Fields (hay) |
| 4 | `Mountain` | 7 | `Rock` (-> `Stone` via quarry, `Ore` via mine) | Mountain (stone only) |
| 5 | `Pasture` | **no** ("kept for the dressing view") | none | Pastures (sheep): absent |
| 6 | `Desert` | 2 | none | Desert |
| (none) | **no Sea terrain**: the island is 30 land hexes (`BoardGenerator.cs:16-32`: 8/6/7/7/2); "coast" is a land node with a non-land neighbour (`PopulationModule.IsCoast`, `:308-313`) | | | Sea (fish, seaweed): absent |

Numbers: 28 resource hexes get dice tokens (2 and 12 twice, others three times, no adjacent 6/8) (`BoardGenerator.cs:34-36,69-80`); desert hexes get none (`IsResource`, `HexCoord.cs:68`). The hidden numbers drive the dice; the player never sees them. Every `Terrain` value has a `PlaceKind` for perception (`Features.cs:98-101`).

### 4.2 Current raw wares, processed wares, foods (code ids)

`Ware` enum (`Wares.cs:11-66`): raws `Log 1, Clay 2, Grain 3, Rock 4` (pile kind, never hauled), `Stone 5, Ore 6`; processed `Plank 7, Kindling 8 (unused), Brick 9, Iron 10, Bread 11, Mortar 12, Bronze 13, Pottery 14, Steel 15, Leather 16, Cloth 17`; gear `Sword 18 ... LightArmour 27` (`Axe 20` is both an attack-gear ware and the NPC hand tool, see risk R9); foods (beyond `Wares.Count = 28`): `Berries 28, Roots 29, Fish 30, Nightshade 31`; `Herbs 32` (C7 remedy). `Wares.PersonalCount = 40` (`Wares.cs:86`). There is no sheep, wool, hay, bedding, prepared food, hammer or stone-block ware.

### 4.3 Current wild-food sources and ecology (`PopulationTuning.cs:138-160`, `PopulationModule.PlaceSources :279-306`)

| `SourceKind` (`PopulationState.cs:31`) | Ware | Placed on | Count per hex | Capacity | Regrow ‰/day | Spoil days | Hunger relief |
|---|---|---|---|---|---|---|---|
| `Berries` 1 | `Berries` | Forest (and Meadow/Pasture, never dealt) | 5 (4) | 80 | 400 | 2 | 150 |
| `Roots` 2 | `Roots` | Hills and Fields | 4 and 4 | 70 | 300 | 6 | 200 |
| `Fish` 3 | `Fish` | **coast nodes of any hex** (incl. Mountain, Desert) | 2 per hex | 80 | 400 | 1 | 300 |
| `Nightshade` 4 | `Nightshade` (toxic) | Forest | 0 by default | 30 | 300 | 2 | 100 (pain 300) |
| `Wood` 5 | log by hand (not food) | Forest | 2 | 40 | 150 | n/a | n/a |

Sources are never placed inside an HQ circle (`PopulationModule.cs:325`): the circles are food-barren. Mountain and Desert hexes hold no food. Forage is 1-2 units per `ForageTicks = 30` (`PopulationSystem.cs:942-965`), carry limit weight 6 and 4 slots (`PopulationTuning.cs:103-104`). A source can run to 0 and recovers only if a neighbour of its kind (within 30 nodes) is above half, one unit per 3 days (`EcologyTuning.EmptyRecoverDays/NeighbourNodes`, `PopulationTuning.cs:152-155`).

### 4.4 Gap to D8.2-D8.4

| D8 requirement | Current | Gap |
|---|---|---|
| D8.2 seven hex types incl. Wetland, Pastures, Sea | Forest, Hills (clay), Fields, Mountain, Desert only on the island; Pasture enum value unused; no Sea | new terrain kinds or renames; board deal; coast/sea access; the "continuous landscape" presentation is a view task |
| D8.3 infinite forage, never exhausted, per-type nutrition/time/spoilage, optional shared throughput (off by default) | **finite, depletable, shared** stocks with logistic regrowth; food types berries/roots/fish (+ toxic nightshade); no mushrooms, wild eggs, rabbit, wild vegetables, cactus fruit, seaweed; no desert or mountain food | replace the source model with per-region infinite supply and per-individual rate limits; nutrition/spoilage/time are already per-ware (`PopulationModule.SpoilDays/Nutrition :210-232`) so extending is cheap; fish moves from "free food" to raw material (D8.2) |
| D8.4 roll creates allowance that materialises gradually; nothing goes straight to inventories | roll puts a **whole pile (4 units) at the hex centre at once** (`TerritorySystems.cs:61-70`, `EconomyTuning.WaresPerPile`), cap 3 piles, extras wasted. It is a physical pile (not an inventory), but instantaneous and shared with the Stage 3 harvesters | add a per-hex allowance and a materialisation rule under a switch; the dice RNG keyed stream `Rng.Fork("dice", RollCount)` (`TerritorySystems.cs:50`) can stay (determinism pin) |
| D8.5 no raw collection without a collection building | NPCs hand-gather logs from `Wood` sources and dice log piles with no building (`HandGatherPiles = true` `PopulationTuning.cs:127`; `PopulationSystem.cs:966-1000`) | remove/disable hand gathering of materials in the normal game; tools (axe 1600 permille) become productivity items |
| D8.5/8.7 building is the deposit with finite storage | assets have a store (`StoreCapacity` 40-60 weight) but nothing requires collection to deposit into it | tie collect-and-deposit to the building (new act) |

## 5. Starting population vs D8.9

| Item | Current (god-only) | D8.9 | Evidence |
|---|---|---|---|
| Factions | 6, names by id: Amber, Azure, Crimson, Jade, Violet, Ivory (`Faction.cs:26`). D8.9 lists "Amber, Crimson, Azure, ..." (order differs; F1 is Azure, F2 Crimson) | 6 named tribes | `Faction.cs:26`, `GameSetup.cs:32` |
| Founders per faction | **20** (`StartNpcsPerFaction`, `GameSetup.cs:56`; `GodBootstrap.cs:356`) | 12 | `PopulationModule.StartGame :17-37` |
| Mix with `EstablishedBand` | 10% elders, 30% mature, 30% children, 30% young adults: for 20 = **2 elders, 6 mature, 6 children, 6 young** (`HouseholdTuning.cs:208-216`, `Founders.Established :186-215`) -> **14 adults + 6 children per tribe, 84 + 36 = 120 total** | 7 adults + 5 children (42 + 30 = 72) | arithmetic from `nElder = total*ElderPermille/1000` |
| Ages (sim milli-years) | child 50-950, young 1000-1900, mature 2000-2900, elder 3000-3300 | "different ages" | `HouseholdTuning.cs:210-213` |
| Families | deterministic from `Rng.Fork("founder-band", faction)`; couples (80% of mature, 40% of young, 10% same-sex), children biological where ages allow else adopted; genomes `Rng.Fork("genome-founder", id)`; one hut and 6 roots per household (`StartHuts = 1`, `StoreFood = 6`) | plausible, biologically consistent, caregivers | `Founders.cs:186-340`, `HouseholdTuning.cs:215` |
| Possessions | 3 roots each, 30% an axe (`PopulationTuning.cs:65-69`, `PopulationModule.GiveFounderPack :148-157`) | not specified | |
| Positions | scattered 1-14 nodes from the HQ, outside its circle (`PopulationModule.FounderNode :118-137`, `FounderSpreadNodes = 14`) | founding settlement | |
| "One communal building" | the HQ, a faction asset with a ware stock; **no NPC-owned communal building**. C6 can found a communal store later by NPC decision | one communal building per tribe, owned by the community group | `EconomyModule.GiveStartStock` |
| No government, military, currency, employers, chains, institutions, professions | NPC side starts without them (Orgs emerge; employment by NPC choice) **but** the AI faction acts as a hidden government/employer/military from tick 1 (S1, S12) | all absent | |
| Immigration | off (`Households.Immigration = 0`) | none | `LifeCycle.cs:31` |

Reuse: the established-band generator already gives deterministic families; D8.9 mainly needs the counts (7+5), the founding-building ownership, and the removal of the faction AI. The older demography finding (extinction, fertility) is from the 26 Nov baseline and is **not re-verified** against current code [unverified].

## 6. Settlements, corners and roads vs D8.10 and D8.11

### 6.1 Settlements (HQs)

- **Rule today (`TerritoryRules.CheckHq` `:15-24`):** the corner must have three land hexes; **Catan distance rule** is implemented (`HqNearCorner`: the corner itself and its adjacent corners are refused, `TerritoryState.cs:212-221`); a new HQ must be exactly two corners (`CornerDistance == 2`) from an own **finished** HQ (an approximation of Catan's "connected by road" rule); its node and flag node must be free and a flag must keep its distance (`CheckHqSite :26-36`).
- Start HQs are placed by farthest-point sampling over legal land corners (`StartCorners.Pick`, `FactionRules.cs:247-`).
- An HQ costs 3 plank, 2 brick, 2 mortar, 2 stone and is built by anonymous builders (`EconomyTuning.cs:80`, `HqBuilt` flow `TerritoryRules.CompleteHq :52`); the AI expands (S3).
- **Area:** a finished HQ owns a circle of radius `NodesPerHexEdge/2` = 6 lattice steps (`NodeGrid.cs:38`); Stage 3 buildings may be placed only inside own circles (`TerritoryState.IsBuildable :192`).
- **Inversion vs D8.10/D8.17:** NPC buildings (huts, assets) may be placed only **outside** every HQ circle (`PopulationSystem.cs:663`, `Work.cs:68`, `Founders.cs:364`). D8.10 says a settlement creates the area in which buildings may be built and D8.17 allows only basic or temporary buildings outside. Today the rule is the reverse. Every faction HQ the AI adds shrinks the NPC-buildable land.
- **No NPC command** places an HQ/settlement; the settlement also confers ownership of nothing for NPCs (it is a faction object with VP).

### 6.2 Roads

- Roads are faction objects: a chain of adjacent land nodes from an own flag to an own flag or free node, not through buildings/flags/other roads, not in a medium building's halo (`TerritoryRules.CheckRoad :244-275`). They are **not** restricted to hex edges, and do not need a settlement at each end, so neither D8.11's "valid Catan edges" nor "towards a planned settlement site" is modelled. The `BuildRoad` command is a faction command.
- NPC effect today: a road node costs 4 ticks per step instead of 6 for NPCs (`PopulationTuning.cs:74-75`; `PopulationSystem.cs:883-889`); `Self.OnRoad` feeds perception (`:621`). So the D8.11 "roads only make walking faster" half already exists. Roads do not constrain walking.
- The Stage 3 logistics meaning of roads (flag routes, carriers, "building needs a road-connected HQ", `EconomySystem.cs:272`) is what must go.

## 7. HQ construction circles and the `Work.cs` constraint

| Place | Rule | Evidence |
|---|---|---|
| C1 `Work.CanFound` | `foreach (var hq in t.Hqs) if (g.InCircle(hq.Node, node)) return false;` (the owner's brief refers to this) | `Work.cs:68` |
| M3/M4 hut founding | `CanFound` refuses any node in an HQ circle | `PopulationSystem.cs:658-668` |
| Founder huts | `Founders.FreeNode` skips nodes in HQ circles | `Founders.cs:364` |
| Wild sources | `PlaceSources` never places in a circle | `PopulationModule.cs:325` |
| Founder positions | outside the circle | `PopulationModule.cs:131` |
| Stage 3 buildings | only inside own circles | `TerritoryState.cs:192-198` |

Consequences [inferred]: (a) the HQ circle is simultaneously the faction AI's whole building site and a no-NPC-building, no-forage zone; (b) the two building worlds divide the island geometrically and never interact; (c) each AI-built HQ (S3) removes a circle's worth of land from NPC use, so the unified game cannot keep both. Replacing the circle with a settlement development area (D8.10) has to change **all six** checks together; four are in NPC code and can be changed under a switch, the `IsBuildable`/Stage 3 ones stay for regression.

## 8. Recipes and production vs D8.12

| D8.12 chain | Closest current | Gap |
|---|---|---|
| wood -> planks (carpenter) | C1 workshop: 1 log -> 1 plank (`Assets.cs:84-85`); Stage 3 sawmill: log + stone -> **2** planks (`BuildingDefs.cs:131`, `EconomyTuning.cs:58`) | single-input recipe exists in C1; planks ware exists (7) |
| stone -> building blocks (stonecutter) | none (`Stone` is itself a raw output of the quarry; `Rock` -> `Stone`) | new ware (blocks) |
| clay -> pottery (potter) | Stage 3 pottery: clay + **grain** (`BuildingDefs.cs:137`) | single-input; ware 14 exists |
| clay -> bricks (kiln) | Stage 3 kiln: log + clay (`:132`) | single-input; ware 9 exists |
| sheep -> wool; wool -> cloth | cloth = grain + ore (`:140`); no sheep/wool | new wares and a shepherd's shelter |
| hay -> bedding | none (grain is the Fields raw) | new ware, rename grain->hay or add |
| fish -> prepared food | Fish is directly edible (`FishNutrition` 300); bakery log+grain -> bread | cooking recipe; `Fish` stops being a food |
| wood + stone -> axe / hammer | `Axe` exists as a ware used by NPCs (hand tool) and as the Axesmith output (iron + plank, `BuildingDefs.cs:145`); no hammer | split NPC tool from armour ware, add hammer |

Stage 3 has 10 processors (pairwise inputs from 5 raws), 10 armouries and a barracks (`BuildingDefs.cs:130-160`), all faction-run. C1 has the generic mechanism that runs any recipe row as an NPC act: `Recipe(Inputs, Outputs, Reason)`, `AssetDef.WorkTicks`, `Work.Speed` (`Assets.cs:30-77`, `Work.cs:35-47`), and `ProduceReason.Crafted` (`PopulationState.cs:88`). Knowledge, equipment and "labour plus tools" are not recipe terms yet (`SkillStore` has only Medicine, Woodcutting, Foraging, `SkillStore.cs:11-21`; the industrial K1 skills are not built). So **D8.12 is a data table plus a knowledge term on the C1 mechanism**; the Stage 3 recipe data can seed it, per the existing ECONOMY_DESIGN plan to generate rows from `EconomyDefs`.

## 9. Victory points

VP exist only as HQ and Citadel counts (`FactionRules.VictoryPoints :15`, `HqVp = 1`, `CitadelVp = 2`, win at 10, `FactionTuning.cs:8-11`); the god-only game is a sandbox so nothing ends (`Victory.cs:38`), but the AI still builds HQs and Citadels "for VP" (`AiBrain.cs:545-550` comments) and `FactionSystem` eliminates a faction with no finished HQ (`FactionRules.cs:326`). No NPC system consumes or produces VP. Decision: **remove from the normal game, keep for Stage 3 regression**; replace "elimination by HQ loss" (R6).

## 10. Existing cognition for D8.13 (multi-step planning)

### 10.1 What exists and can support planning

| Component | Where | What it gives D8.13 |
|---|---|---|
| Perceive -> believe -> attend -> candidates -> score pipeline | `Thinker.Think` (`Thinker.cs:151-161`): `UpdateBeliefs`, `Attend` (top-3 goal-driven), `Recognise`, `BuildBase`, `Learner.Arm`, `FillHeard`, `FillDivine`, `BuildCandidates` (`:421`) | the substrate; every plan step can be a candidate with a feature context |
| Beliefs (places, remembered, decaying) | `BeliefKind` Food, Wood, Pile, Hut, Tool, Store, Shrine, OrgPlace, Asset (`Mind.cs:11-24`), `BeliefStore` | memory of resource, store and asset locations: the "where is stone / who has it" half |
| Scoring and running intent | `AssociativeNpcPolicy`: instinct + learned + social + culture + personality - cost + explore + divine (`AssociativeNpcPolicy.cs:52`); `SelfView.RunVerb/RunTarget` and `SwitchCost = 60` (`PopulationTuning.cs:89`) | an intent persists and can be abandoned when something outweighs the switching cost: the "interruptible" half |
| Episodes and deferred credit | `EpisodeKey` (hut, partnership, care, child, deal, reciprocity, message, lot, loan, contract, asset; `EpisodeKey.cs:12-18`); `VerbInfo.Deferred` (`Verbs.cs`); `Learner.Accrue` | learning the outcome of a multi-step investment long after the act (the hut built, the asset finished) |
| `OpportunitySlot` / `DecisionRecord` | `Mind.cs:194-206`, `NpcMind.Opportunities` (8 slots) | an inspection record of one decision; **not** a plan, but the right place to expose a plan's current step |
| M3 learning | `AssociationStore`, `Learner`, instincts (`Instincts.cs`, `CivMindHooks.InstinctsC1 :28`) | learned associations between context and verb; the "what has worked" half |
| Candidate hooks per package | `CognitionExtensions.AddCandidates` (`Extensions.cs:80`): M4, M5, Civ (quota 4, `CivMindHooks.AddCandidatesC1 :52`), Orgs, Divinity | each package adds its candidates without touching the core |
| C1 candidates | build at a free plot when carrying a log (`CarriesMaterial`, `Work.cs:58`), finish own site, work an asset, haul a log to own workshop, take out of a store (`CivMindHooks.cs:47-185`) | the existing single-step chain: gather -> carry -> found -> deliver materials -> build -> work -> take out |
| C3 employment | offer/accept/pay/walk-out verbs (`Employment.cs:26-28`; `CivMindHooks.C3.cs:35-37`) | a labour-for-goods step the planner can include |
| M4 hooks | household verbs 32-47 (`HouseholdVerbs.cs`), care arrangements, store/take-from-store (verbs 17/18), `Provision` (F2c, off by default, `HouseholdTuning.cs:117`) | dependants and stores as plan inputs |
| M5 exchange | `SocialVerb.Give/HandOver/Seek/Say*` (`SocialVerbs.cs:14-21`), price beliefs, `Valuation` | a request/offer step ("another person has stone") |
| Knowledge and teaching | `SkillStore` + `Imitation.VerbsOf` topics (`Imitation.cs`), C7 teaching | only three topics; no recipe knowledge |

### 10.2 What is missing

1. **A goal/plan record** (objective, parent, steps, status, resumption, interruption cause). Today the only persistent intent is `Action` plus `Self.Run*` (one step).
2. **Recipe/construction knowledge in the mind** (D8.13 "I know how to build a hut"): there is no per-NPC known-recipe set; `AssetTable` and `BuildCost` are global tables used by sim code and by candidate gating on what the NPC *carries*.
3. **Derivation of material sub-goals** from a recipe cost (e.g. hut needs wood -> perceive wood source -> gather; needs stone -> look for a holder -> ask/barter). Barter candidates come from M5 for what is in sight, with a food/log/axe-only valuation (`Valuation.cs:22-41`).
4. **Site selection** beyond "where I stand is free" (`SelfView.CanFoundHere`, `Thinker.cs:451`; C1 `Plot` class 93).
5. **Reservation/capacity/commitment** ("storage space reserved elsewhere", D8.5; "I committed my wood to this build").
6. **Hashing and save of plan state** (every new field joins the cognition hash; use the `IOmitWhenDefault` / "hashed only when present" pattern already used for `NpcMind.Kin/Social/Divine/Orgs/Civ`, `Mind.cs:252-262`).
7. **Learning credit across a plan** (episode keys exist but there is no plan-level key).
8. **Budget:** `PerceiveMax = 16`, `AttendK = 3`, candidate quotas (`PopulationTuning.cs:79-81`) bound how many plan options a think can weigh [performance not measured].

## 11. Group-like mechanisms relevant to D8.15

| Mechanism | What it is | Gap to D8.15 |
|---|---|---|
| C6 founding meetings | `FoundingMeeting {Proposer, Template, Node, Opened, Expires, Accepts}` (`OrgState.C6.cs:281-289`); sub-acts Propose, Answer, CallMeeting, AnswerMeeting, AskJoin, AcceptJoin (`OrgMindHooks.C6.cs:18`); founding by individuals through gestures; offices, rules, records, communal store (`CommunalStore.cs`) | **templated**: three rows only (`OrgTemplates.cs:56-76`: shared food store (min 2 founders, 2 households), healer practice, healers' house); objective is a template, not a free objective; no contribution ledger, withdrawal or free-rider handling; becomes a persistent organisation at founding (D8.15: not automatic) |
| M4 shared care | `CareArrangement {Asker, Carer, households, Given/Returned}` (`HouseholdState.cs:39`, `HouseholdActs.cs:605-623`); household bin and hut rights; verbs Tend/Feed/Provision/SeekChild/AcceptChild | pairwise, household-level, about care only; but already has voluntary agreement, ledger of given/returned and end reasons: the right model for a contribution ledger |
| M5 cooperation gestures | `SayCooperate` 71, `Give`, `HandOver`, reciprocity episode | one-to-one; no group object |
| `OwnerKind.Group` | enum value (`LotBin.cs:6`, constructor `:23`) used only as "back to community" for estates with group id = faction (`Estates.cs:292`) | no group state; and this use equates community with faction (D8.17 forbids tribe-as-owner by default) |
| C1 use rights | `UseRight {Holder, Kind, GrantedBy, Until}` (`Assets.cs:95`) | shared access to an asset is already per-owner-agnostic (`Ownership.cs`): reusable for joint ownership |

D8.15 needs a new group record (objective type, members, required contributions, deadline) reusing: C6's meeting/propose/answer gestures, M4's arrangement ledger pattern, C1 `Owner` and use rights, and the existing Say*/perception seam for invitations.

## 12. Migration risks and blockers

| Id | Risk or blocker | Evidence | Why it matters |
|---|---|---|---|
| R1 | **Determinism pins.** Stage 3 smoke hashes (seeds 1-12 in `PolicyRunTests.cs:27`), the both-off neutrality pins, and population scenario pins depend on `DiceSystem`, `Territory.Piles`, HQ circles, `PlaceSources` and the AI. Any change to these shared systems moves hashes | DEV_LOG pins, `GodSim.StateHashParts` | gate every change behind a tuning switch written only when non-default (the `IOmitWhenDefault` pattern already used for Households/Civ/Orgs/Divinity) so Stage 3 and existing scenarios hash as before |
| R2 | **Shared dice piles.** Gradual materialisation (D8.4) changes the same pile objects the Stage 3 harvesters use and the NPC hand-gather reads (`PopulationSystem.cs:497-515`, `:1196-1209`) | `TerritorySystems.cs:45-72` | separate unified-mode allowance state from `Territory.Piles`; keep `Piles` for regression |
| R3 | **HQ circle geometry is load-bearing in six NPC-side places** (§7) and is the inverse of D8.10/D8.17 | `Work.cs:68` et al. | all six change together under one switch; founder placement, source placement and hut/asset checks need a common "development area" predicate |
| R4 | **AI is attached by the host, not by the sim**: the player path uses `AttachAi`/`AiHost`, the runner path uses `SimBuilder.Create(attachAi: true)` and `AttachPolicies`; `policies.faction none` exists only in the second. The save does not store the policy assignment (DEV_LOG temp mechanisms) | `AiHost.cs`, `SimBuilder.cs:51-118` | one authoritative "unified mode" flag in setup/tuning that both hosts honour and saves keep |
| R5 | **Idle Stage 3 side effects with the AI off.** HQs still hold start stock, dice piles still accrue and `PileWasted` events still fire, `EconomySystem` and `TradeSystem` still tick, `FactionSystem` still eliminates a faction with no HQ | `TerritorySystems.cs:61-66`, `FactionRules.cs:326` | harmless but noisy; elimination by HQ loss is wrong for tribes; telemetry and KPIs that read Stage 3 state will show dead zeros [unverified at runtime] |
| R6 | **The divine layer is faction-addressed.** `HumanGod = GodOf(F0)` and `DivinityIds.GodOf(faction)` (`DivinityIds.cs:18`) bind gods to factions; HUD/commands use faction ids | `GodBootstrap.cs:357`, `PlayerController` | tribes must stay as an affiliation id even when factions stop being economic actors (D8.9) |
| R7 | **Hand-gathered logs underpin every NPC building path.** Hut (4 logs), field (1), workshop (2), store (2) all consume logs from `HandGathered` wood (`PopulationTuning.cs:106`, `Assets.cs:82-88`); D8.5/D8.6 make level 1 cost labour only and ban hand collection | `PopulationSystem.cs:966-1000` | switching hand gathering off without changing costs would stop all NPC construction |
| R8 | **Finite food and the demography record.** Today's wild food is finite and depletable; the M4 demography work (stale baseline §2.2) recorded starvation from local depletion and fertility problems. Infinite forage (D8.3) will change population dynamics; 72 vs 120 people changes them again | `PopulationTuning.cs:145-155`, stale baseline | viability must be re-run on the new model; the old results are not evidence [unverified] |
| R9 | **Ware overload.** `Ware.Axe` (20) is both the NPC hand tool (founder pack, `Valuation`, `Weight`) and Stage 3 gear (`Axesmith`); `Fish` is food and not yet a raw; `Stone`/`Rock`/`Ore` double as raw and ledger entries; per-ware arrays use `Wares.Count = 28` while personal wares go to 40 | `Wares.cs`, `BuildingDefs.cs:145` | adding sheep, wool, hay, bedding, blocks, hammer needs the append-only 33-39 block or the planned 40-71 block (ECONOMY_DESIGN), without moving existing ids |
| R10 | **Roads and flags are one structure.** Removing flags (D8.11) must keep road nodes for NPC speed and the hash `TerritoryModule.Hash` (`TerritorySystems.cs:94-107`) | `TerritoryState` | roads need an NPC-built form without a flag/segment graph |
| R11 | **Three building records per Stage 3 building** (territory, economy, military) and five competing building kinds (hut, asset, organisation place, Stage 3, household bin) | §3.1 | D8.8 needs one capability record; C1 `ProductiveAsset` is the closest base, but huts hold rights per bed, not an owner |
| R12 | **No NPC command to found a settlement or a road**, and HQ placement relies on anonymous builders and plank/brick/mortar costs | §6 | new NPC act or group objective, new material costs from D8.12 wares, a "planned site" state (D8.11) |
| R13 | **Valuation is food/log/axe only** (`Valuation.cs`); M5 barter has no price knowledge for stone, clay, wool and so on | `Valuation.cs:22-41` | trade of the new raws will not form without extending valuation and the instinct priors |
| R14 | **No group object** (D8.15) and no plan object (D8.13) | §10-§11 | these are new state: save format, hashes, telemetry events, tests |
| R15 | **Test coupling.** Existing population scenarios implicitly include the Stage 3 AI (e.g. `band_6x20`, `civ_assets`: "the band_6x20 population ... stage 3 AI, sandbox"); NPC-only versions would hash differently | `Scenarios/*.json` | add new scenario names for unified mode; leave the old ones as regression |
| R16 | **Performance and stale numbers.** Per-tick cost of per-hex materialisation, six-faction, 72-120 people, plan evaluation; all benchmark figures in docs predate Civ/Orgs/Divinity | DEV_LOG | [unverified] |
| R17 | **Presentation depends on Stage 3 views** (`GodBootstrap.cs:226-231`) and on hex dressing (`IslandView`); D8.2 wants a continuous landscape | `GodBootstrap.cs:222-231` | view package, after the sim |

## 13. Reuse map: D8 capability -> existing type/file -> gap

| D8 capability | Existing type / file | Gap |
|---|---|---|
| Seven hex types, geography (D8.2) | `Terrain` (`HexCoord.cs:41`), `BoardGenerator.CreateIslandBoard`, `IslandTerrainPool` (`BoardGenerator.cs:31`), `BoardTopology`, `NodeGrid` (12 nodes per hex edge), `PlaceKind` | Wetland, Pastures, Sea, seven kinds in the deal; sea/coast access; terrain->raw/food tables as data |
| Retain movement lattice | `NodeGrid`, `NodePather`, `Reach` | none (retain) |
| Infinite forage with rates (D8.3) | `ResourceSource`, `SourceKind`, `EcologyTuning`, forage act `Verb.ForageAt` (`PopulationSystem.cs:942`), ware nutrition/spoilage (`PopulationModule.cs:210-232`), `Goods.Produce(ProduceReason.Foraged)` | infinite stock mode, new foods and region mapping, per-individual rate and optional shared throughput, nutrition per food as config |
| Catan production rolls (D8.4) | `DiceSystem` (`TerritorySystems.cs:43`), `HexNumber`, keyed `Rng.Fork("dice", n)` | allowance + gradual materialisation, per-hex physical raw items independent of `Territory.Piles` |
| Collection building (D8.5-D8.7) | `ProductiveAsset`/`AssetDef`/`AssetTable` (`Assets.cs`), `Work.Build/Operate/Haul/TakeOut`, store bins (`BinKind.AssetStore`), C1 candidates | collection capability (which raw, which hex range), deposit-with-capacity rule, level 1 cost = labour only, sleeping capacity (huts' beds), upgrade levels, worker slots (`StaffSlots` exists) |
| One building architecture with capabilities (D8.8) | `ProductiveAsset` + `UseRight` + `Ownership`; `Hut` (beds, rights); `LotBin`; `Organisation` | merge hut into the capability model; condition/maintenance exists (`Condition`); sale/inheritance (`Estates.cs`, `Ownership.cs`) |
| Individual construction | `Work.FoundSite/Build`, M3 hut build, `HouseholdHooks.CanWork/WorkTick` | materials no longer from hand gathering; development-area rule (R3) |
| Owned physical inventories | `Npc.Carry`, `LotBin`, `Owner`, `Goods` ledger with conservation (`Goods.cs`, `PopulationInvariants.cs`) | none structurally; remove HQ stock use |
| NPC haul/transport (D8.11, logistics) | C1 `VerbHaul` (log to own workshop), `TakeOut`, M5 `HandOver`, C6 deposit/serve | general carry-from-A-to-B act driven by need or contract; weight/slot limits exist (`CarryWeight 6`) |
| Production recipes (D8.12) | C1 `Recipe`, `Work.Operate`, `Work.Speed`, `ProduceReason.Crafted`; Stage 3 `EconomyDefs` rows as seed data | single-input D8.12 rows, new wares, knowledge and tool terms, workshop kinds |
| Employment (D8.14) | `Employment.cs` (C3a): offers, voluntary acceptance, wages by hand-over, suspension on walk-out | pay in goods exists; non-wage/own-account paths and "work at somebody else's building" use rights are in place; check that employer is not required (D8.9 no employers at start) |
| Barter/gifts (D8.14) | `SocialSim`, `Valuation`, `SocialCandidates`, `Goods.Move HandOver` | valuation for new wares; instinct priors for the new goods |
| Households and shelter (D8.7) | M4 `HouseholdState`, `Hut` beds, `HouseholdStore`, `Care`, `LifeCycleSystem` | sleeping in any building with capacity/quality; access permission per building |
| Starting population and families (D8.9) | `Founders.Established`, `FounderTuning`, genetics, `PedigreeRecord`, `KinCues` | counts 7+5, tribe names order, communal-building ownership, no AI (R4) |
| Settlements (D8.10) | `TerritoryRules.CheckHq` (corner, distance rule, reach), `Hq`, `NodeGrid.CircleRadiusSteps`, `StartCorners.Pick` | NPC/group founding act; development-area semantics (R3); settlement without VP and elimination |
| Roads (D8.11) | `Road`, `TerritoryRules.CheckRoad`, NPC road speed (`RoadStepTicks`) | Catan-edge restriction, planned-site endpoint, no flags/segments, NPC/group building, materials |
| Multi-step goal planning (D8.13) | `Thinker`, `AssociativeNpcPolicy`, `NpcMind.Beliefs`, `SelfView.Run*`, `EpisodeKey`, C1 candidate gating | goal/plan record, recipe knowledge, sub-goal derivation, site selection, reservation (§10.2) |
| Informal groups (D8.15) | C6 `FoundingMeeting` and gestures, M4 `CareArrangement`, C1 `UseRight`/`Owner`, `OwnerKind.Group`, M5 `SayCooperate` | group record, contribution ledger, join/withdraw/free-ride, objective types (road, settlement, haul) |
| Organisations/institutions retained | `Orgs` (`OrgModule`, templates, offices, records), `OrganisationSystem` | institution seat not attached by the player path (§2.1); groups-to-organisation transition (not by threshold) |
| Divine mechanics retained | `Divinity/*`, `GodDirector`, commands, possession | tribe/faction ids as affiliation (R6) |
| Stage 3 regression | `GameSetup.Population = false`, Stage 3 smoke pins, `Stage3MilitaryPolicy`, `AiBrain` | keep compiled and pinned; ensure unified mode is a separate setup/tuning switch (R1, R4) |

## 14. What this audit could not establish

- Anything about runtime behaviour: whether the six AI factions expand, declare war, trade or build Citadels during a god-only run; frame cost; whether `-godnoai` produces a healthy god-only game. No game, scenario or test was run.
- The current values of test pins after the Checkpoint B re-pin (the doc pin values are quoted from DEV_LOG and `PolicyRunTests.cs:27`, not re-computed).
- The number of legal settlement corners on the 30-hex island; where C6 organisation places may be sited relative to HQ circles.
- Whether older demography conclusions (extinction/fertility, 26 Nov baseline) still hold on the merged code.
- Headless runner behaviour under `policies.faction = none` with Civ/Orgs/Divinity on (no scenario or test found by search).

## 15. Additional gap assessment against the core catalogue (planning amendment, not a new run)

The earlier audit remains evidence about b297cb0. The new three-tab workbook is proposed design content, not evidence of current functionality. Prior source inspection of `Assets/God/Sim/Learning/Features.cs` (`Drive.Count`, Hunger/Fatigue/Cold/Fear/Pain), `PopulationTuning.cs`, `PopulationSystem.cs` (Body, drive reward, action dispatch), `Learning/Verbs.cs` (VerbTable), `Cognition/Thinker.cs` (BuildCandidates/OfferSensed), `Civ/Work.cs`, `Divinity/DivineActs.cs` and the household/organisation mind hooks establishes the following migration gaps. Recheck these against the actual session head before coding; no fresh build or execution was undertaken for this amendment.

| Gap | Current vs target | Owner |
|---|---|---|
| R18 | Single-step high-level candidate verbs are not the proposed 12-control body interface; implicit pathing and separate divine act mappings must converge on primitive execution | UN0/UN6a/UN9 |
| R19 | Five explicit body drives; social motivations and shelter effects exist but are not independent Social/Comfort channels; Fun, Hygiene, Bladder, Room and Thirst lack core drive entries | UN1/UN6 |
| R20 | Catalogue has 138 objects and 65 buildings; old numeric ware ranges and hard-coded recipes are too narrow to assume direct migration; durable furnishings/animals need distinct instance representation | UN0/UN7 |
| R21 | Existing learning and education are not evidence of founder training on motor controls or child imitation of learned sequences. New trainer and held-out experiments needed | UN6t/UN11 |
| R22 | Shelter/bed counts are properties of old huts or assets, not proof of separately manufactured furniture, finite occupancy, sanitation or leisure effects | UN3/UN7 |
| R23 | Health gathers herbs from `SourceKind.Roots`; removing old `ResourceSource` without replacement risks disabling treatment. Audit old hut/food dependencies in care, education, divine acts and organisations | UN1/UN3/UN9/UN11 |
| R24 | Existing asset-store estate ownership defect and group/faction fallback assumptions require regression coverage, including dropped goods and expired reservations | UN3/UN4/UN11 |
| R25 | New thirst, nutrition, energy and reward interactions may alter viability; assess before activating full need set; no assumption of multi-generation sustainability | UN1/UN6/UN11 |

Reference: [God_of_the_Island_Core_Design.xlsx](God_of_the_Island_Core_Design.xlsx), controlling design §0.2. Existing high-level verbs may be retained for legacy tests and analytics, but must not remain a second autonomous controller in the unified scenario.


## 16. Latest migration gaps — emergent property, construction and conflict

This is a planning delta against the existing static audit, not a fresh source run. Recheck exact call sites on the current branch. [LATEST_OWNER_AMENDMENT.md](LATEST_OWNER_AMENDMENT.md) changes the target; previous ownership conclusions are descriptions of legacy code, not requirements for unified mode.

| Gap | Audit required | Package |
|---|---|---|
| R26 | `ItemLot.Owner`, `OwnerKind`, `Goods.Move/Produce`, `Assets`, `Ownership`, `UseRight`, store permission checks, candidate visibility and valuation: locate every ownership-dependent branch and neutralise it in unified mode, retain objective holder/provenance and legacy pins | UN0/3/9 |
| R27 | Households, organisation stores, C3 contracts, estate/group fallback, death and destruction: remove compulsory title/reciprocity or estate transfers, preserve physical lots, care and social memory | UN3/4/8/9/11 |
| R28 | `Work.FoundSite/Build` and old hut construction: route known designs, actual materials and work through USE, no owner privilege or scripted builder decision | UN3/5/6 |
| R29 | Abstract military and current flee/threat perception: add persistent-NPC conflict interactions, witness-local fear/pain/claims without faction military policy | UN6/8/9/11 |
| R30 | M5 offer/accept/handover and promises: audit whether old protocol or filters enforce both legs or require ownership; retain useful communication, actual independent transfers and imperfect beliefs | UN6/8/11 |
| R31 | Training must establish teacher-free local motor and survival competence with construction and dispute scenarios; no evidence in historical audit proves this | UN6t/11 |
