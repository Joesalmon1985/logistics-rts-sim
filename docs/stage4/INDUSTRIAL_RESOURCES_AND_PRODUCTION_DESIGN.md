# Industrial resources and production design (owner D6, 27 Nov 2026)

Status: design, 27 Nov 2026. Owner decisions D6.1, D6.2, D6.3, D6.5, D6.6, D6.13, D6.16, D6.17, D6.18 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)); the brief is [OWNER_BRIEF_2026-11-27_INDUSTRY.md](../civdiv/OWNER_BRIEF_2026-11-27_INDUSTRY.md), whose "additional owner decisions" part wins over its first part (no external ore buyer, one money system, no central fuel market). Written to [DESIGN_CONVENTIONS.md §12](DESIGN_CONVENTIONS.md#12-how-every-design-doc-is-written). Binding cross-package rules are in [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("SAC"); everything this document proposes for the shared surface (ware numbers, ledger reasons, enum values, event codes, migrations, switch) is **provisional** until the SAC "Industry addendum" fixes it (section 12 lists the requests). Paths are relative to `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

Sister documents (D6): [VEHICLES_AND_HARVESTING_DESIGN.md](VEHICLES_AND_HARVESTING_DESIGN.md), [ELECTRICITY_DESIGN.md](ELECTRICITY_DESIGN.md), [INDUSTRIAL_MILITARY_DESIGN.md](INDUSTRIAL_MILITARY_DESIGN.md), [WASTE_AND_CIRCULAR_DESIGN.md](WASTE_AND_CIRCULAR_DESIGN.md); written by another agent: KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md (designs, blueprints, patents, licences, research, auctions) and MONEY_AND_ACCOUNTING_DESIGN.md (money issuance, payments, claims, banking).

---

## 1. Purpose and scope

**This document owns:**
- the **new industrial wares** (geological raws, refined fuels, industrial metals, components, ammunition, abstract nuclear material) and how they extend the ware table without disturbing stage 3 or the population hashes;
- **finite geological deposits**: how they sit in hex regions and on nodes, how they are generated, discovered, claimed, extracted and depleted;
- **extraction**: manual extraction by an NPC, mines and wells (mechanised extraction as facility recipes);
- **the one recipe table** (`RecipeBook`) and the one recipe runner, which reuse the stage 3 processor and armoury data (`Economy/BuildingDefs.cs`) rather than starting a second production system;
- **industrial facilities** (mine, well, refinery, smelter, workshop, factory) as C1 productive assets; **production orders**, finite capacity, material inputs, workers, electricity requirement and completed physical products (goods or assets);
- **conservation rules for industrial matter** (the goods ledger extended with industrial reasons; embodied matter in assets);
- how organisations own, staff and run facilities (on the generic framework, no special industrial owner);
- **refineries and industrial trade** (package I3): how processed goods change hands through existing mechanisms and supply agreements, with no external buyer (section 4.9).

**Linked, not repeated:**

| Topic | Owner |
|---|---|
| Vehicles, harvesting loop, cargo, fuel tanks, transport | [VEHICLES_AND_HARVESTING_DESIGN.md](VEHICLES_AND_HARVESTING_DESIGN.md) |
| Generation, the grid, power allocation, utilities, metering | [ELECTRICITY_DESIGN.md](ELECTRICITY_DESIGN.md) |
| Military production, procurement, crews, the strategic device | [INDUSTRIAL_MILITARY_DESIGN.md](INDUSTRIAL_MILITARY_DESIGN.md) |
| Waste, scrap and recyclable wares; collection; recycling recipes; pollution field | [WASTE_AND_CIRCULAR_DESIGN.md](WASTE_AND_CIRCULAR_DESIGN.md) |
| Designs, blueprints, who knows how to build or run a recipe, patents, licences, auctions | KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md (other agent) |
| Money, prices in money, payment instruments, issuance and destruction | MONEY_AND_ACCOUNTING_DESIGN.md (other agent) |
| Goods, lots, owners, the goods API, reach | [WORLD_ONTOLOGY.md](WORLD_ONTOLOGY.md), SAC §6–7, `Population/Goods.cs` |
| Productive assets, the `Work` verb, ownership beliefs, theft classification | C1 (SAC §7, package index C1), [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) §3.4–3.5 |
| Employment and wages | C3, [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md) |
| Formal obligations (supply contracts, unpaid deliveries) | C2 (`FormalObligation`, SAC §1) |

## 2. Decisions and principles

| Id | Decision | Source |
|---|---|---|
| IND-1 | **One economy.** Industrial goods are ordinary `ItemLot`s moved only by `Goods`; facilities are C1 productive assets; owners are the existing `Owner` kinds (`Npc`, `Household`, `Org`). There is no industrial inventory, no faction stockpile and no "credits" | D6.1, D6.2 |
| IND-2 | **Production creates goods, never money.** No recipe, extraction, delivery or sale path calls a money API. A refinery's income is only what buyers actually pay (I3, MONEY doc) | D6.2 |
| IND-3 | **One recipe table and one runner.** The ten stage 3 processors and ten armouries become the first twenty rows of `RecipeBook`, generated from `EconomyDefs` by an adapter (same inputs, outputs and cycle ticks). Industrial recipes are appended rows. Stage 3 mode keeps running `EconomySystem` on `EcoBuilding` exactly as today | D6.1 ("no competing architectures"), D6.3 ("existing chains kept") |
| IND-4 | **Deposits are finite true state, not lots.** A deposit has a remaining quantity that never regrows. Extraction turns deposit quantity into lots with `ProduceReason.Extracted` | D6.3 |
| IND-5 | **Coal is new code.** The owner asked to keep coal; the current code has **no coal ware** (`Economy/Wares.cs` has log, clay, grain, rock, stone, ore; coal existed only in the Godot catalogue `docs/design/05_units_resources_buildings_catalogue.md`). It is added as an industrial raw. Stage 3 `Ware.Ore` (mountain rock → ore at the stage 3 mine) is unchanged and stays a separate generic ware | D6.3; code finding |
| IND-6 | **Every unit of work is done by a persistent NPC** physically at the facility; capacity is finite (one cycle at a time per facility, bounded queue); a facility without workers, inputs or required power does nothing | D6.5, owner Q8 |
| IND-7 | **Recipes are data-driven designs.** A recipe row names the knowledge design it requires (KNOWLEDGE doc `DesignId`; 0 = common knowledge). Knowing or licensing a design never builds or runs anything by itself | D6.9, D6.10 |
| IND-8 | **Allocation is never a central market.** Inputs reach a facility by hand-carry, hauling, vehicle delivery, owned extraction, internal transfer, supply agreement, barter or purchase. No price table, no auctioneer for fuel or ore | D6.6 |
| IND-9 | **Waste is an output like any other** and is ledger-accounted (WASTE doc owns the wares and what happens next) | D6.13 |
| IND-10 | **No outcome is designed in.** Concentration of ownership, inequality between mine owners and workers, resource conflicts and boom-bust are possible and measured, never scripted | D5.33 |

Determinism: integers only; facilities processed in ascending asset id; orders in (priority desc, id asc); every draw from a named keyed stream (section 4.10).

## 3. Data structures (`Sim/Industry`, proposed)

### 3.1 Industrial wares (appended block; stage 3 and personal numbers unchanged)

The ware enum today: stage 3 wares 0–27 (`Wares.Count = 28`), M3 foods 28–31, F1 `Herbs = 32` with `Wares.PersonalCount` 32 → 40 (SAC F1.17, 33–39 spare). Industrial wares take a **new block 40–71** and `Wares.PersonalCount` becomes **72**, with the F1.17 hashing rule (the population ledger is hashed in the old order and the extra wares only when any is non-zero, so every existing hash is unchanged). `Wares.Count` (the stage 3 per-ware arrays of `EcoBuilding`) is **not** widened: industrial goods never enter stage 3 books.

| No. | Ware | Class | Weight | Notes |
|---|---|---|---|---|
| 40 | `IronOre` | geological raw | 4 | from iron deposits (mountain, hills) |
| 41 | `CopperOre` | geological raw | 4 | from copper deposits |
| 42 | `Coal` | geological raw, fuel | 3 | from coal deposits; burned by smelters and coal/hybrid stations |
| 43 | `CrudeOil` | geological raw, liquid | 3 | from wells; carried in tankers or barrels (a `Liquid` ware may be hand-carried at most 1 unit) |
| 44 | `UraniumOre` | geological raw | 5 | **abstract fictional strategic mineral** (D6.14); rare deposits only |
| 45 | `Fuel` | refined fuel, liquid | 2 | vehicle fuel (refinery product) |
| 46 | `FuelOil` | refined fuel, liquid | 3 | heavy fuel for oil and hybrid stations |
| 47 | `Copper` | industrial metal | 2 | smelted |
| 48 | `Wire` | component | 1 | drawn from copper; power lines, engines |
| 49 | `MachineParts` | component | 2 | machine shop; vehicles, facilities, maintenance |
| 50 | `Engine` | component | 4 | engine works; one per vehicle |
| 51 | `Ammunition` | military consumable | 1 | abstract (INDUSTRIAL_MILITARY §3); no real-world specification |
| 52 | `NuclearFuel` | abstract nuclear material | 2 | fictional processed fuel for nuclear stations and the strategic device |
| 53 | `Firearm` | industrial infantry gear | 2 | abstract weapon for trained infantry (INDUSTRIAL_MILITARY §3.2) |
| 54–55 | spare (industrial) | | | |
| 56–62 | waste block | waste | see WASTE doc | `Refuse 56`, `Scrap 57`, `Recyclables 58`, `Ash 59`, `Slag 60`, `Sludge 61`, `SpentFuel 62` (owned by [WASTE_AND_CIRCULAR_DESIGN.md](WASTE_AND_CIRCULAR_DESIGN.md)) |
| 63–71 | spare | | | later chains (cement, chemicals, plastics, electronics) |

**Existing industrial metals are reused.** `Iron` (10) and `Steel` (15) are already wares; stage 3 makes them by its fantasy pairs (log + ore; stone + ore). Industry adds **further recipes for the same wares** (iron ore + coal → steel), so one ware has several recipes, as in Widelands. Nothing about the stage 3 recipe changes.

```csharp
// Economy/Wares.cs additions (by the I1a foundation commit, section 9.1)
public static bool IsIndustrial(Ware w) => (int)w >= 40 && (int)w < 56;
public static bool IsWaste(Ware w)      => (int)w >= 56 && (int)w < 63;     // WASTE doc
public static bool IsLiquid(Ware w)     => w == Ware.CrudeOil || w == Ware.Fuel || w == Ware.FuelOil;
public static bool IsFuel(Ware w)       => w == Ware.Coal || w == Ware.Fuel || w == Ware.FuelOil || w == Ware.NuclearFuel || w == Ware.Refuse;
// PopulationModule.Weight gains the column above; WareRefValue (analytics only, ECONOMY 3.3) gains rows.
```

### 3.2 Ledger reasons (the goods ledger is widened once)

`GoodsLedger` (`Population/PopulationState.cs`) has 8 produce and 8 consume reason slots. Produce uses 0–3, F1 takes 4 `Miracle` and 5 `Crafted`, leaving 6–7. **Consume is full** after F1 (0–4 existing, 5 `Miracle`, 6 `Treatment`, 7 `Offering`). The I1a foundation therefore widens both to **16 slots** (`ProduceReasons = ConsumeReasons = 16`) with the same rule as F1.17: slots 8–15 are hashed only when any is non-zero, so the ledger hash is unchanged until industry writes them.

| Produce reason | No. | Meaning |
|---|---|---|
| `Extracted` | 6 | deposit quantity became lots (manual extraction, mine, well, harvester) |
| `Processed` | 7 | a recipe output (goods and waste) |
| `Discarded` | 8 | waste arising from consumption (WASTE §4.1) |
| `Salvaged` | 9 | scrap and parts released by dismantling or destroying an asset (≤ its embodied matter) |
| 10–15 | reserved | |

| Consume reason | No. | Meaning |
|---|---|---|
| `ProcessInput` | 8 | consumed as a recipe input at cycle start |
| `Combusted` | 9 | fuel burned (station, vehicle tank, process heat) |
| `Embodied` | 10 | built into a persistent asset (vehicle, facility, power line); tracked per asset (3.6) |
| `Disposed` | 11 | removed from the material world by an explicit disposal recipe (incineration's input; WASTE doc) |
| `Expended` | 12 | ammunition fired (INDUSTRIAL_MILITARY) |
| 13–15 | reserved | |

`ConsumeReason.Built` (2, huts and sites) keeps its meaning for population buildings; industrial facilities and assets use `Embodied` so their matter can be reconciled (section 6).

### 3.3 Geological deposits

```csharp
public enum DepositDepth : byte { Surface = 1, Shallow = 2, Deep = 3 }   // Surface: visible, harvestable by vehicle; Shallow: manual or mine; Deep: mine or well only

public sealed class Deposit                       // IndustryState.Geology.Deposits, ascending Id; true state (layer 1)
{
    public int Id;                                // DepositId: Geology.NextDepositId, never reused
    public Ware Ware;                             // IronOre, CopperOre, Coal, CrudeOil, UraniumOre (later: stone, clay seams)
    public int Hex, Node;                         // the hex region it lies in, and its node (the extraction point)
    public int RadiusNodes;                       // extent for drawing and for "a mine within reach" (0..3)
    public DepositDepth Depth;
    public int Initial, Remaining;                // units; Remaining only falls (never regrows)
    public short GradePermille;                   // 300..1000: output per extraction cycle scales by grade
    public int PropertyAsset;                     // C1 property record id (claim, owner, use rights); 0 = unclaimed open access
    public long DepletedTick;                     // 0 while Remaining > 0
}
public sealed class GeologyState { public int NextDepositId = 1; public List<Deposit> Deposits = new List<Deposit>(); }
```

- **Where deposits sit.** A deposit belongs to one hex (geography and jurisdiction, WORLD_ONTOLOGY §2) and has one node inside it: the place where reach is measured. The hex grid stays hidden in normal play (D5.18): the view draws the deposit as an outcrop, seam, stain or seep around its node ([VISUAL_WORLD_DESIGN.md §18](VISUAL_WORLD_DESIGN.md#18-owner-d6-27-nov-2026-industrial-presentation)).
- **Bounds:** at most 6 deposits per hex, 256 per island; `Remaining` ≤ 2^30.
- **Not a lot, not a `ResourceSource`.** Deposits differ from wild sources (`ResourceSource`, `Population/PopulationState.cs`: logistic regrowth): they never regrow, have depth and grade, and need knowledge to find (3.4). Their stock is reduced only by extraction and by miracles (`SmiteSource` generalised by D3 through a Geology function, SAC §7.2) and strategic effects.

**Generation** (`GeologyGen.Generate`, at `StartGame` when `Industry.Enabled = 1` and the scenario has no explicit `deposits` block): per hex in ascending id, by terrain (`Board.HexTerrain`), draws from `Rng.Fork("geology", hex)`:

| Terrain | Possible deposits (chance per slot, default) | Typical size (units) |
|---|---|---|
| Mountain | iron 60%, copper 35%, coal 40%, uranium 6% (Deep only) | iron 400–1200, copper 200–800, coal 600–2000, uranium 40–160 |
| Hills | coal 45%, iron 25%, copper 15% | coal 300–1200 |
| Fields, Meadow, Pasture | crude oil 12% (Deep), coal 10% (Shallow) | oil 300–1500 |
| Coast hexes (any land hex with a sea edge) | crude oil 20% (Deep) | oil 400–2000 |
| Forest, Desert | coal 10%; desert oil 25% | |

Scenario files may list deposits explicitly (`deposits: [{ ware, hex, node, depth, initial, grade }]`), which the industrial scenario does (GAMEPLAY_SCENARIOS §13). The table lives in `SimTuning.Industry.Geology` (`IOmitWhenDefault`).

### 3.4 Discovery and knowledge of deposits

Deposits are true state. **Who knows of them is belief** (WORLD_ONTOLOGY §1, D5.28):
- **Surface** deposits are perceived like any object within sight: `SenseExtra` (SAC §6, industry hook after Divinity) adds them as `ObjClass.Deposit` (110) with the believed ware; quantity is seen only as a band (small, medium, large).
- **Shallow and Deep** deposits are found only by **surveying**: the verb `Survey` (learning verb 27) at a node, 240 ticks, by an NPC; success per deposit within 3 nodes = `SurveyBasePermille (300) + 3 × skill(Survey)`, drawn from `Rng.Fork("npc", id)` (the surveyor's own stream), bands of remaining quantity ± 25%. A survey tool (a later ware) doubles the radius.
- Knowledge lives in `NpcMind.Industry.Deposits` (≤ 8 entries: deposit id, believed ware, band, seen tick; evicted oldest) and spreads by M5 communication (`TopicKind.Deposit = 11`, proposed) and by an organisation's records (a survey report written to an org's `Records`, layer 4).
- `DepositDiscovered { Npc, Deposit, Method (Sight, Survey, Told, Record) }` fires the first time a given NPC learns of a deposit; `DepositSurveyed` records each survey result.
- **The god** sees deposits under its divine observation profile (`IDivineView`, D5.28; `Omniscient` sees all). A divine revelation of a deposit is a D3 Tier 3 miracle ("RevealTechnology" family); it confers knowledge, not ownership (D6.10).
- Discovering a deposit does not claim it. **Claiming** is C1's `SourceClaimed` (275) on the deposit's property record (first arrival with a marker, a grant by a government organisation, purchase of the record, or seizure); extraction from a deposit someone else claims, without a use right, is classified as theft by C1's rule.

### 3.5 Facilities (C1 productive assets with an industrial record)

A facility is a **C1 productive asset** (asset id from C1's allocator, property record, owner, use rights, `ObjClass` in C1's 90–99 or Industry's 110–129, sensed through `SenseExtra`). Industry adds one record per facility, keyed by the asset id:

```csharp
public enum FacilityKind : byte { Mine = 1, Well = 2, Smelter = 3, Refinery = 4, Workshop = 5, Factory = 6,
                                  PowerStation = 7 /* ELECTRICITY */, Depot = 8 /* VEHICLES */, WasteSite = 9, RecyclingPlant = 10 /* WASTE */,
                                  StrategicSite = 11 /* INDUSTRIAL_MILITARY */ }
public enum FacilityStatus : byte { Site = 0, Idle = 1, Working = 2, MissingInputs = 3, NoWorkers = 4, NoPower = 5, OutputFull = 6,
                                    Depleted = 7, Broken = 8, Paused = 9, NoDesign = 10 }

public sealed class Facility                       // IndustryState.Production.Facilities, ascending Asset
{
    public int Asset;                              // the C1 asset id (owner and rights live in C1's property record)
    public FacilityKind Kind;
    public ushort DesignId;                        // KNOWLEDGE doc: the facility design it was built from (0 = common)
    public int Node, Hex;
    public int InputBin, OutputBin, FuelBin, WasteBin;   // LotBins, kind AssetStore (SAC 7.1), owner = facility owner; 0 = none. Created by Goods.NewBin.
    public int Deposit;                            // Mine/Well: the deposit worked (0 otherwise)
    public byte WorkerSlots;                       // capacity: at most this many workers count (1..6)
    public int GridPort;                           // ELECTRICITY: the connection point (0 = not connected)
    public FacilityStatus Status;
    public Cycle Current;                          // the running cycle (Recipe 0 = none)
    public List<int> Queue = new List<int>();      // ProductionOrder ids, ≤ 8
    public int Condition = 1000;                   // permille; wear per cycle; maintenance restores (VEHICLES §4.6 rules reused)
    public long CyclesDone;
    public int[] Embodied;                         // ware counts built into it (section 3.6), ≤ 8 non-zero
}
public struct Cycle { public ushort Recipe; public int Order; public long ProgressMilli; public long StartedTick; public int PowerPermille; }
```

Bins are physical and ordinary: inputs arrive in `InputBin` by `Deposit` (a carrier within `Reach.Store`) or by vehicle `BinTransfer` (VEHICLES §3.4); outputs wait in `OutputBin` until someone withdraws them. A facility may have one bin for inputs and outputs when its design says so (small workshops). Default `WeightCap` per bin from the design (e.g. smelter input 120, output 80).

### 3.6 The recipe table (`RecipeBook`), reusing stage 3 data

```csharp
public struct WareCount { public Ware Ware; public short Count; }

public sealed class RecipeDef                      // frozen data (ReadOnlyArray), like BuildingDef
{
    public readonly ushort Id;                     // 1..20: generated from EconomyDefs; 21+: industrial (stable numbers, appended only)
    public readonly string Key, Label;
    public readonly FacilityKind Facility;         // where it may run (stage 3 rows: Workshop)
    public readonly ushort DesignId;               // KNOWLEDGE: the design needed to run it (0 = common knowledge)
    public readonly ReadOnlyArray<WareCount> Inputs;    // ≤ 4, consumed at cycle start (ProcessInput)
    public readonly ReadOnlyArray<WareCount> Outputs;   // ≤ 4, produced at completion (Processed), waste included
    public readonly WareCount FromDeposit;         // extraction recipes: units taken from Facility.Deposit (scaled by grade), else Ware.None
    public readonly ushort AssetOutput;            // a VehicleDef or FacilityDesign id produced as a persistent asset (0 = none)
    public readonly int CycleTicks;                // at full staffing and skill 0
    public readonly byte WorkersMin, WorkersFull;  // capacity in labour
    public readonly byte Skill;                    // the skill whose level speeds it (POPULATION skills; C1 work speed)
    public readonly int PowerPu;                   // ELECTRICITY: power units while running (0 = none)
    public readonly int StandbyPu;                 // drawn while idle but connected (0 by default)
    public readonly short MinPowerPermille;        // below this share of PowerPu it stalls (default 500)
    public readonly int EmissionMicro;             // WASTE §5: pollution added to the hex per cycle
    public readonly short WearPermille;            // facility condition lost per cycle
}

public static class RecipeBook
{
    public static IReadOnlyList<RecipeDef> All;
    public static RecipeDef Get(ushort id);
    public static RecipeDef FromBuildingDef(BuildingDef d, ushort id);   // the adapter: In/Out/CycleTicks of a stage 3 workshop
}
```

**The adapter (IND-3).** `RecipeBook`'s static constructor walks `EconomyDefs.Defs` in its frozen order and, for every `BuildingRole.Workshop` def (the ten processors and ten armouries), emits one row with `Inputs = d.In`, `Outputs = d.Out`, `CycleTicks = d.CycleTicks`, `Facility = Workshop`, `WorkersMin = WorkersFull = 1`, `PowerPu = 0`, `DesignId = 0`. The barracks (`BuildingRole.Factory`, unit orders) and the harvesters (dice piles) are **not** recipes: soldiers are recruited people (M9, INDUSTRIAL_MILITARY §4.1) and piles are nature. `EconomySystem` keeps reading `BuildingDef` directly in stage 3 mode; a test pins that rows 1–20 equal the defs field by field (`Recipes_Stage3RowsEqualBuildingDefs`). C1's workshop assets run these rows through the same runner as industrial facilities, so there is one production mechanic for the population economy.

**Default industrial recipes** (tuning data, original numbers; every row is a design in the KNOWLEDGE doc's catalogue):

| Id | Recipe | Facility | Inputs | Outputs (waste in italics) | Ticks | Workers min/full | Power pu |
|---|---|---|---|---|---|---|---|
| 21 | Hand mine (pick) | Mine (manual) | deposit 1 | ore 1 | 240 | 1/2 | 0 |
| 22 | Mechanised mine | Mine | deposit 4 | ore 4 | 300 | 2/4 | 30 |
| 23 | Pump crude | Well | deposit 3 | crude oil 3 | 240 | 1/2 | 25 |
| 24 | Coal bloomery | Smelter | iron ore 2, coal 1 | iron 2, *slag 1* | 300 | 1/2 | 0 |
| 25 | Steel furnace | Smelter | iron ore 3, coal 2 | steel 2, *slag 1, ash 1* | 360 | 2/3 | 40 |
| 26 | Copper smelting | Smelter | copper ore 3, coal 1 | copper 2, *slag 1* | 300 | 1/2 | 30 |
| 27 | Oil refining | Refinery | crude oil 4 | fuel 2, fuel oil 1, *sludge 1* | 300 | 2/3 | 50 |
| 28 | Wire drawing | Workshop | copper 1 | wire 2 | 180 | 1/1 | 20 |
| 29 | Machine shop | Factory | steel 2, copper 1 | machine parts 2, *scrap 1* | 360 | 2/3 | 40 |
| 30 | Engine works | Factory | steel 3, machine parts 2, wire 1 | engine 1 | 600 | 2/4 | 60 |
| 31 | Hauler assembly | Factory | steel 6, machine parts 4, engine 1, wire 2 | asset: hauler; *scrap 1* | 1200 | 3/5 | 80 |
| 32 | Harvester assembly | Factory | steel 8, machine parts 4, engine 1, wire 2 | asset: harvester; *scrap 1* | 1500 | 3/5 | 80 |
| 33 | Ammunition | Factory | steel 1, copper 1 | ammunition 10 | 240 | 1/2 | 20 |
| 34 | Firearm | Factory | steel 2, machine parts 1 | firearm 1 | 360 | 1/2 | 20 |
| 35 | Nuclear fuel processing | Refinery (design known at the site; R33) | uranium ore 10 | nuclear fuel 1, *slag 6, spent fuel 0* | 1800 | 3/4 | 120 |
| 36–60 | military vehicles, power-line kits, recycling, incineration | | | see the D6 sister documents | | | |

Vehicle and station construction recipes are in their documents; recycling and incineration in WASTE §4.4. Units are deliberately small so the vertical slice completes within a few sim days (1 day = 1200 ticks by default).

### 3.7 Production orders

```csharp
public enum OrderStatus : byte { Queued = 0, Running = 1, Blocked = 2, Completed = 3, Cancelled = 4 }
public sealed class ProductionOrder                // IndustryState.Production.Orders, ascending Id
{
    public int Id;                                 // OrderId: Production.NextOrderId, never reused
    public int Facility;                           // asset id
    public ushort Recipe;
    public short Quantity, Done;                   // cycles ordered and completed
    public Owner PlacedFor;                        // who the output is meant for (the facility owner unless an agreement says otherwise)
    public int PlacedBy;                           // NpcId of the office-holder or owner who placed it
    public int Decision;                           // C6 OrgDecision id (0 for an NPC owner's own order)
    public int Agreement;                          // I3 supply/procurement agreement (0 = none)
    public byte Priority;                          // 0..3
    public int DueDay;                             // 0 = none
    public OrderStatus Status; public byte BlockReason;   // FacilityStatus value while blocked
    public long PlacedTick, ClosedTick;
}
```

- **Placing an order** is an institutional act by a person: the facility's owner (an NPC or household head) or an office-holder of the owning organisation with the `Propose`/`DecideAlone` power under its `DecisionRule` (C6). It is done **at the facility's board** (within `Reach.Touch` of the facility node, like a `JobPosting` board, ECONOMY 3.6) through the verb `PlaceOrder` (an Orgs-range or Industry-range `NpcVerb`, section 9.2), or relayed by message to the manager on duty. A customer (another organisation) never writes an order into someone else's queue: it makes an agreement with the owner (I3), and the owner's office-holder places the order.
- **Capacity.** One cycle at a time per facility; queue ≤ 8; a full queue refuses `queue_full`. Throughput is bounded by workers (`WorkerSlots`), skill, power and input arrival.
- A completed order closes; a cancelled order releases nothing (inputs of a running cycle are already consumed; see 4.4).

## 4. Mechanics

### 4.1 Manual extraction (no facility)

`Extract` (learning verb 28) by an NPC within `Reach.Gather` (2) of a Surface or Shallow deposit node: 240 ticks per unit at skill 0 (`× 1000 / workSpeed`, C1 formula), +60% speed with a mining tool (ECONOMY §3.2 tool list; the tool's ware number is fixed by the contracts). Each unit: `Deposit.Remaining -= 1` and `Goods.Produce(Carry(npc), ware, 1, Owner, ProduceReason.Extracted, actor = npc, source = DepositId)`. The owner follows C1's rules for gathering from a source: ownerless deposit → the extractor; a deposit under a property record → the record owner when the extractor works under a `Work` use right (wage relation), else the extractor and C1 classifies the take (theft against the claimant). Weight limits carry as for logs (IronOre weight 4: one unit per trip for most adults). Manual extraction makes iron and coal possible in a pre-industrial society; it is slow on purpose (owner Q9's "capital multiplies output" holds: a mechanised mine is about 4× per worker, measured, not fixed).

### 4.2 Mines and wells (mechanised extraction)

A mine (or well) is a facility built at, or within `Deposit.RadiusNodes` of, a deposit's node. Its extraction recipe (21, 22, 23) takes `FromDeposit.Count × Grade / 1000` (at least 1 while `Remaining > 0`) from `Remaining` at cycle **completion** and produces that many lots into `OutputBin` (`Extracted`, owner = facility owner). When `Remaining` reaches 0: `DepositDepleted`, facility status `Depleted`; the facility remains an asset (it can be dismantled for `Salvaged` scrap, WASTE §4.3). Extraction right: the facility's owner must hold the deposit's property record or a use right (`UseKind.Gather`) from its owner; building a mine on someone else's claim is a C1 property dispute, not prevented by the sim.

### 4.3 Workers

Workers are NPCs performing C1's `Work` verb (96) with the facility as target, physically within `Reach.Touch` of the facility node. A worker's presence counts for the tick only while its running action is `Work` on that asset. Why a person works there is its own decision: it owns the facility, it is a member of the owning cooperative, it holds a C3 employment contract (wage), it was asked by kin, it was compelled by a god. The runner never conscripts. Counting: the first `WorkerSlots` workers in ascending NpcId. Each worker gains skill experience per completed cycle (C1/POPULATION skill rule) and fatigue as for any work.

### 4.4 The recipe runner (`IndustrySystem`, per tick, facilities in ascending asset id)

1. **Choose.** If no cycle is running and the facility is not `Paused`, take the first queued order (priority desc, id asc) whose recipe can start: the facility's design and the recipe's `DesignId` are **known at the site** (KNOWLEDGE doc §4.3, §5.3: at least one present worker meets the design's knowledge and skill minima, or follows a usable document at the site; `DesignId = 0`, or K1 absent or off, needs no check). **A licence or patent is never checked here** (lead ruling R33, SAC §22.2 IA-3): whether to respect rights is the owner's own `RespectRights` policy, applied when its office-holder places the order; the runner only records `Licensed` † on `ProductionCycleStarted` from `Rights.Permits`, so unlicensed use is classified as `UnauthorisedDesignUse` and can become a dispute (D6.8); all inputs are present in `InputBin` as lots owned by the facility owner (customer-owned inputs under a toll-processing agreement are a later extension); workers ≥ `WorkersMin`; `PowerPermille ≥ MinPowerPermille` when `PowerPu > 0` (ELECTRICITY §4.4 gives the allocation); output room exists for the expected outputs. Otherwise set the blocking status (`MissingInputs`, `NoWorkers`, `NoPower`, `NoDesign` = no present worker knows the design and no usable document is at the site, `OutputFull`) and emit `ProductionStalled` once per change.
2. **Start.** Consume every input (`Goods.Consume(Bin(InputBin), lot, n, ConsumeReason.ProcessInput, actor = lowest-id worker present)` across lots in ascending lot id), set `Current`, emit `ProductionCycleStarted { Facility, Order, Recipe, Inputs[] }`. Consuming at start makes inputs unavailable to everyone else and makes the cycle's material record exact.
3. **Progress.** `ProgressMilli += Σ_workers speed(w) × min(1000, present × 1000 / WorkersFull) / 1000 × PowerFactor / 1000`, where `speed` is C1's work speed (1000 + 3 × skill, fatigue, tool) and `PowerFactor = PowerPermille` (1000 when `PowerPu = 0`). Without workers progress pauses (status `NoWorkers`, the cycle is kept); below `MinPowerPermille` it pauses (`NoPower`).
4. **Complete** at `ProgressMilli ≥ CycleTicks × 1000`: produce every output (`Goods.Produce(Bin(OutputBin or WasteBin), w, n, owner = facility owner, ProduceReason.Processed, actor = a present worker, source = OrderId)`); an `AssetOutput` registers a new asset (VEHICLES §4.1) at the facility node, owned by the facility owner, with `Embodied` = the cycle's consumed inputs; add `EmissionMicro` to the hex pollution (WASTE §5); `Condition -= WearPermille`; `Order.Done++`; emit `ProductionCycleCompleted { Facility, Order, Recipe, Outputs[], Asset }` and, when the order is done, `ProductionOrderCompleted`.
5. **Abort.** If the facility is destroyed mid-cycle, the consumed inputs are gone and no outputs appear except the abort salvage rule of WASTE §4.3 (`Salvaged` scrap ≤ the consumed metal). `ProductionCycleAborted` records it.

The runner only moves lots through `Goods`, always with a present NPC as actor (never `actor = 0`, which would bypass reach: `Goods.InReach` returns true for `actor <= 0`).

### 4.5 Completed physical products

A recipe ends in one of two kinds of physical product:
- **Goods** (steel, fuel, parts, ammunition): lots in the output bin, owned by the facility owner. They leave only when a person withdraws them (`Withdraw` within `Reach.Store`) or a vehicle loads them (`BinTransfer`). Delivery and sale are I2 and I3.
- **Persistent assets** (vehicles, the strategic device; later machines and prefabricated station parts): a new asset record with identity, owner, location, condition and embodied matter (VEHICLES §3.2). It is **not** a lot and never enters the goods ledger as a count: its materials left as `Embodied` consumption and are recorded on the asset.

### 4.6 How organisations own and staff industry (generic framework)

No industrial ownership type exists. Mines, wells, refineries, factories, transport fleets and power stations belong to whoever holds the C1 property record: an NPC, a household or an organisation (`OwnerKind.Org`, any `OwnershipModel`: private, cooperative, communal, state, religious; D5.17). Production code never branches on the model (ORG-INV-5's static test is extended to `Sim/Industry`). What differs is **who decides** (the org's `DecisionRule`) and **who receives the output** (the owner; distribution by the charter).

- **Founding** an industrial enterprise uses C6's opportunity and founding path: an NPC who knows a design, has seen a deposit or demand, and has partners, founds an `Enterprise` (or a cooperative) and acquires assets by ordinary means ([ORGANISATIONS §4.5](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md#45-buildings-productive-assets-and-stores)). A threshold only makes the opportunity available (D5.16).
- **Building** a facility is a construction site (C1 / ECONOMY sites): its design's construction materials are delivered and consumed (`Embodied` on the facility record) by NPC builders; it exists as an asset when the work is done (`AssetBuilt` 270).
- **Staffing**: the owner posts jobs (C3); employees work under `Work` rights; the output belongs to the owner (ECONOMY §4.4 wage relation, unchanged). Cooperatives distribute by their charter.
- **Supply**: inputs come through the mechanisms of IND-8. Vertical integration (one org owns the mine, the harvesters and the smelter) moves goods by internal transfer with `OwnerRule.Keep`; between organisations, ownership changes only by an exchange, purchase or supply agreement (I3).
- **Failure**: an enterprise that cannot pay wages or deliver becomes insolvent by C6's rule; its facilities stand idle; nothing restarts them automatically (ECONOMY §12 "Economic failure is a real outcome").

### 4.7 Political economy hooks (measured, not designed)

Concentration of extraction rights (share of deposit `Remaining` under the largest owner), workers vs owners in industrial organisations (ECONOMIC_CLASSES relation to the means of production), surplus per industrial worker (output FE minus wages, analytics only), and government regulation (a law adopted by a government org, C10: extraction permits, emission limits enforced by its NPCs). Military and civilian orders compete for the same factories (INDUSTRIAL_MILITARY §4.4). All are KPIs (section 7), never rules.

### 4.8 The god and industry

The god acts only through existing divine mechanics (D5.1, D6.15): suggest or command a mine owner to open a mine, compel a worker to work a cycle (one valid physical act), possess a worker or a harvester operator (VEHICLES §4.7), smite a facility (D3 building path: the abort rule applies), create resources by miracle (`ProduceReason.Miracle` with a ledger row, never `Extracted`), reveal a deposit or a design (Tier 3; knowledge only, D6.10). The god never places an order, sets a priority or owns a facility.

### 4.9 Refineries and industrial trade (I3; no external buyer)

A refinery or smelter **transforms** physical resources (4.4); it **earns** only when someone pays for what it hands over. There is no buyer of last resort, no conversion of ore into credits, no Power Grid resource market (D6.2, D6.6). The removed "simplified external buyer" of the first brief has no successor.

**How industrial goods change hands** (every one reuses an existing mechanism; I3 adds only the agreement record and the trace):

| Mechanism | What happens physically | Ownership and payment | Owner of the rule |
|---|---|---|---|
| Internal transfer | a carrier or vehicle of the same owner moves lots between its own bins | `OwnerRule.Keep`; no payment | `Goods`, I2 |
| Barter at the gate | two authorised persons hand over goods for goods (M5 exchange steps, each a separate act) | `Exchanged`; nothing atomic | M5, ORG-ACC-2 |
| Purchase | the seller's person hands over (or unloads into the buyer's bin), the buyer's person pays | basis `Purchase` (23); payment by the MONEY doc's instruments, or goods | K2a (payment; MONEY doc) |
| **Supply agreement** | a standing agreement between two owners: ware, count per delivery, schedule, delivery place, price terms; each delivery is a physical unload | ownership passes at unload (`OwnershipBasis.SupplyAgreement`, 25); each delivery opens a C2 `FormalObligation` (debtor buyer, creditor seller) settled by payment | **I3** (record), C2 (obligation) |
| Communal allocation | a communal store's keeper issues by ration rule | C6 rules (C8 Tier 2 absorbed) | C6 |
| Government provision | a government organisation's staff deliver from its stores under a budget line | grant hand-over (`Gift` or `StateGrant` basis) | C10, C6 |

```csharp
public sealed class SupplyAgreement                // IndustryState.Trade.Agreements, ascending Id (AgreementId)
{
    public int Id; public Owner Seller, Buyer;     // any owners (orgs, NPCs, households)
    public AgreementKind Kind;                     // Supply, Electricity, Procurement, Haulage (LEAD_RULINGS R43): one record for I3, I4 (electricity supply) and I5 (procurement); I4's billing depends softly on I3 (fallback: C2 obligations only)
    public Ware Ware; public short PerDelivery; public short EveryDays; public int DeliverToBin;
    public PriceTerm Price;                        // { Kind: Goods | Money | None (internal/communal), Ware or MoneyUnit, Count per unit } — Money per the MONEY doc
    public int StartDay, EndDay;                   // 0 = open-ended
    public int AgreedBy0, AgreedBy1;               // NpcIds who agreed (an office-holder each, by their org's DecisionRule)
    public int Decision0, Decision1;               // OrgDecision ids
    public byte Status;                            // Active, Suspended (arrears), Ended, Breached
    public int Delivered, Paid, OpenObligations;   // counts for reports (truth; the orgs' own records may differ)
}
```

- **Agreeing** is two decisions, one per party, each by an office-holder who perceives the other's offer (M5 offer gestures or a message relayed by members; no remote contracting). Either party may later breach (stop delivering, stop paying); breach is recorded (`SupplyAgreementBreached`) and is a matter for whatever justice exists (C9), never auto-enforced.
- **A recorded sale** (vertical slice step 4) is the pair `OwnershipChanged(basis Purchase or SupplyAgreement)` and the payment (the MONEY doc's payment event, or a goods `ItemMoved` back), linked by an `IndustrialSaleClosed { Agreement or Deal, Seller, Buyer, Ware, Count, PaymentRef }` trace event. A delivery whose payment has not come is an **open obligation**, not revenue.
- **Prices** are each party's own: no price table, no posted market price that binds anyone (ECONOMY §12). A marketplace organisation (later) is one more institution where persons meet.

### 4.10 Order within a tick and random streams

`IndustrySystem` runs at **order 264** (after `OrganisationSystem` 260, so the day's org decisions apply first; after `ElectricitySystem` 262, which sets `PowerPermille` for the interval, ELECTRICITY §4.2). Day start: facility day rows, order expiry (`DueDay` passed → `Blocked` reason recorded, not cancelled). Streams: survey success `Rng.Fork("npc", id)`; geology generation `Rng.Fork("geology", hex)`; nothing else in the runner draws.

## 5. Interactions with other systems

| System | Interaction |
|---|---|
| Population (`Goods`, `Reach`, `PopulationSystem.Start/Act`) | every lot move; `Extract`, `Survey`, `PlaceOrder` verbs through the extension hooks (dispatch order M3 → M4 → M5 → Civ → Orgs → Divinity → **Industry**) |
| C1 | facilities are C1 assets; `Work` verb; ownership beliefs; theft classification of unauthorised extraction |
| C3, C6 | wages, jobs, organisations, decisions, budgets |
| C2 | supply agreements and unpaid deliveries as `FormalObligation`s (I3) |
| KNOWLEDGE doc (K1) | `DesignId` on recipes, facilities and vehicles; who may and can run a recipe |
| MONEY doc | payment for sales, wages in money where money exists |
| VEHICLES | vehicle assets as recipe output; `BinTransfer` deliveries; harvesters extracting from Surface deposits |
| ELECTRICITY | recipe demand, `PowerPermille`, station fuel bins |
| WASTE | waste outputs, salvage, emissions, pollution effects |
| INDUSTRIAL_MILITARY | military orders and procurement on these factories |
| Divinity | section 4.8; actions under control carry `Control` on their events (SAC §15.1) |

## 6. Invariants

| Id | Invariant | Checked |
|---|---|---|
| IND-I1 | **Matter conservation (C1 extended):** for every ware, `Σ Produced (all 16 reasons) − Σ Consumed (all 16 reasons) = Σ holdings` (carry, ground, bins), exactly | every day boundary, after save/load |
| IND-I2 | **Extraction bound:** `Σ ProducedBy(Extracted, w) = Σ_deposits of ware w (Initial − Remaining)` minus miracle and strategic reductions recorded as such | day boundary |
| IND-I3 | **Embodied matter:** for every ware, `Σ ConsumedBy(Embodied, w) = Σ_assets Embodied[w] + Σ released by dismantling/destruction` (salvage never exceeds embodied: `ProducedBy(Salvaged, w) ≤ released`) | day boundary |
| IND-I4 | **Recipe balance:** for every completed cycle, its `ProductionCycleStarted.Inputs` equal the recipe inputs and its outputs equal the recipe outputs (extraction scaled by grade) | per event, in tests |
| IND-I5 | **No money creation:** `Sim/Industry` references no money issuance or destruction API (static test against the MONEY doc's API names); no industrial event changes a money balance | static and reconciliation |
| IND-I6 | **Ownership integrity:** recipe outputs are owned by the facility owner; no lot changes owner in `Sim/Industry` except through `Goods.Move/SetOwner` with an exchange, purchase, supply-agreement or internal basis and an actor within reach (SAC C8) | event audit |
| IND-I7 | **Labour attribution:** every cycle-progress tick has at least `WorkersMin` living workers at the node doing `Work` on the asset (no anonymous labour) | sampled audit in tests |
| IND-I8 | **Capacity:** at most one running cycle per facility; queue ≤ 8 | invariant check |
| IND-I9 | **Determinism and neutrality:** with `Industry.Enabled = 0` every part hash equals the base (pin P10, section 9.1) | pins |

## 7. Telemetry and warehouse facts

**Events** (names fixed here; codes proposed in section 12, block 1000–1019, domain `Industry`):
`DepositGenerated` (Full, at start), `DepositDiscovered`, `DepositSurveyed`, `DepositDepleted`, `ResourceExtracted` (one per completed extraction cycle or per manual unit batch; HV), `ProductionOrderPlaced`, `ProductionOrderCompleted`, `ProductionOrderCancelled`, `ProductionCycleStarted` (HV), `ProductionCycleCompleted` (HV), `ProductionCycleAborted`, `ProductionStalled` (on status change), `FacilityStatusChanged`, `FacilityDay` (day start, one per facility: cycles, inputs, outputs, worker-ticks, power share, status ticks). Existing events carry the goods: `Produced`, `Consumed`, `ItemMoved`, `OwnershipChanged`; C1's `AssetBuilt`/`AssetUsed`.

**Warehouse** (migration `0031_industry_production.sql`, proposed; owner I1):

| Table | Grain | Source | Reconciliation |
|---|---|---|---|
| `dim_deposit` | one row per deposit | `DepositGenerated` | `initial − Σ fact_extraction.units = final remaining` |
| `fact_deposit_knowledge` | (npc, deposit) first knowledge | `DepositDiscovered` | count = events |
| `fact_extraction` | one row per extraction event | `ResourceExtracted` | `Σ units` per ware = ledger `ProducedBy(Extracted)` |
| `dim_facility` | one row per facility (kind, design, node, hex) | `AssetBuilt` + industry record | — |
| `fact_production_order` | one row per order (placed, closed, status) | order events | opens and closes at the events |
| `fact_production_cycle` | one row per cycle (inputs, outputs, ticks, workers, power share) | cycle events | Σ inputs = `ConsumedBy(ProcessInput)`; Σ outputs = `ProducedBy(Processed)` per ware |
| `fact_facility_day` | (facility, day) | `FacilityDay` | status ticks sum to the day length |
| `fact_embodied_matter` | (asset, ware) | asset output and construction | Σ = `ConsumedBy(Embodied)` net of releases |
| `fact_goods_ledger_day` (existing family) | (ware, reason, day) | ledger | extended with reasons 6–12 |
| `fact_supply_agreement`, `fact_supply_delivery`, `fact_industrial_sale` (migration `0033_industrial_trade.sql`, I3) | agreement; delivery; closed sale | I3 events | every delivery has one `OwnershipChanged` with basis 25 and one obligation open or settled; Σ sales value per seller = Σ payments received (MONEY doc facts), never more |

**KPIs** (I6 adds to [KPI_CATALOGUE.md](KPI_CATALOGUE.md)): IND-01 output per industrial worker-day by recipe; IND-02 capacity utilisation (working ticks / available ticks); IND-03 stall ticks by reason (inputs, workers, power, output); IND-04 deposit depletion share and years to exhaustion at current rate; IND-05 extraction-right concentration (share of remaining under the top owner); IND-06 manual vs mechanised extraction share; IND-07 military share of factory output (INDUSTRIAL_MILITARY). Each is analytics only (no NPC or policy reads it).

## 8. Player and view

The god sees deposits (by its observation profile), facilities with smoke, sparks and moving parts only while `Working` events say so, stockpiles in output bins as visible heaps, and a facility card (owner organisation, workers present, queue with progress, status and stall reason, inputs and outputs, power share, deposit remaining as observed). Presentation is in [VISUAL_WORLD_DESIGN.md §18](VISUAL_WORLD_DESIGN.md#18-owner-d6-27-nov-2026-industrial-presentation) and [PLAYER_UI_DESIGN.md §25](PLAYER_UI_DESIGN.md#25-owner-d6-27-nov-2026-industrial-interface); no view decides anything.

## 9. Migration from the existing code, foundation and staging

### 9.1 What the I1a foundation commit must land (one agent, lead-reviewed, behaviour-neutral)

As F1 did for D5, one small commit lands every shared industrial surface, so the I packages touch only their own files afterwards. It edits frozen files and therefore needs the SAC Industry addendum rows first.

| # | Item | Files |
|---|---|---|
| I1a.1 | `Ware` 40–71 block, `Wares.PersonalCount = 72`, helpers of 3.1, weights, F1.17-style ledger hashing for 40–71 | `Economy/Wares.cs`, `PopulationModule.cs` |
| I1a.2 | Ledger reasons widened to 16 (3.2); `ProduceReason` 6–9, `ConsumeReason` 8–13 (13 `Research`, K1; SAC §22.1 I1a.2); hash slots 8–15 only when non-zero | `Population/PopulationState.cs` |
| I1a.3 | `MoveReason.BinTransfer = 13` with `Reach.Store` for both bins (VEHICLES §3.4); `OwnershipBasis.SupplyAgreement = 25` (I3) | `Population/Goods.cs`, `Reach.cs` |
| I1a.4 | `Goods.MoveBin(s, ev, bin, toNode, actor)`: relocates a bin by one lattice step (vehicles only; VEHICLES §4.3) with `BinMoved` | `Population/Goods.cs` (frozen: amendment) |
| I1a.5 | State slot `GodState.Industry` (`IndustryState`, sub-objects `Geology`, `Production` I1; `Vehicles` I2; `Trade` I3; `Grid` I4; `Military` I5; `Waste` I1/WASTE), hash part `"industry"` appended after `"divine"` (present only when non-null), `SimTuning.Industry` root (`Enabled = 0`), `IndustryModule` entry points, `IndustryHooks`/`IndustryMindHooks` dispatched **after Divinity** | `Core/GodState.Industry.cs`, `Core/GodSim.cs`, `Core/SimTuning.cs`, `Industry/*` stubs |
| I1a.6 | Id ranges (section 9.2), `ThinkScratch.MaxCand` 36 → 40 (quota 4 for Industry), `NpcMind.Industry` slot (null unless enabled) | `Thinker.cs`, `PolicyTypes.cs`, `Learning/Verbs.cs`, `Cognition/Mind.cs` |
| I1a.7 | Movement sites `PopulationExtensions.StepTicks(s, npc, from, to, base)`, `PopulationExtensions.Moved(s, ev, npc, from, to)` (default: base, no-op) and a third site `PopulationExtensions.PathCosts` (the cost profile of VEHICLES §4.3; SAC §22.11 #15) for vehicles | `PopulationExtensions.cs`, `PopulationSystem.cs` |
| I1a.8 | `TelemetryDomain` 70–75 and `RangeOf` → 1000–1099; empty per-package catalogue arrays `IndustryEvents.I1` … `I5` | `Telemetry/DomainEvent.cs`, `Industry/IndustryEvents.cs` |
| I1a.9 | Pin **P10 `Industry_Off_Reproduces_Base`** (all D5 switches and `Industry.Enabled` 0: every part hash equals the base) and **P11 `Industry_Idle_Is_Neutral`** (`Industry.Enabled = 1`, no deposits seeded by the scenario, generation off: every part except `industry` equals P10) | tests |
| I1a.10 | Save: the lead bumps `SaveFile.CurrentVersion` at the checkpoint that merges I1a; `FromJson` accepts null `Industry` | lead only |

### 9.2 Id ranges requested

| Space | Request |
|---|---|
| Learning `Verb` | 21 Board, 22 Drive, 23 LoadCargo, 24 UnloadCargo, 25 Refuel, 26 Maintain, 27 Survey, 28 Extract, 29 PlaceOrder, 30–31 spare (I range inside the free 21–31) |
| `NpcVerb` | 200–208 Industry, sentinel `IndustryReserved = 209` |
| `ObjClass` | 110–129 (110 Deposit, 111 Mine, 112 Well, 113 Smelter/Refinery, 114 Factory, 115 PowerStation, 116 PowerLink, 117 Vehicle, 118 Depot, 119 WasteSite, 120 RecyclingPlant, 121 StrategicSite) |
| `TopicKind` | 11 Deposit, 12 Job/Order (industry) |
| `SituationKind` | 128–143 (128 PowerOut, 129 FuelShort, 130 Polluted, 131 VehicleNear) |
| `SelfFeature` | 72–79 (72 Driving, 73 InVehicle, 74 Industrial work) |
| `Role` | 11 Operator, 12 Crew, 13 Engineer |
| `NpcGroup` | 11 VehicleCrew |
| Episode-key kinds | 40–47 (40 vehicle routine, 41 production order, 42 supply agreement, 43 meter bill) |
| `DivineAct` | 22 Board, 23 Alight (D2 table, VEHICLES §4.7) |

### 9.3 Stage 3 and C1

- **Stage 3 mode is untouched.** `EconomySystem`, `EcoBuilding`, walkers, HQ stocks, the barracks and the dice piles keep their own books; `Wares.Count` stays 28; the stage 3 hash `4bd54b271857a803` must hold.
- **C1's workshops** become `Facility` records of kind `Workshop` running `RecipeBook` rows 1–20. If C1 merged its own minimal recipe execution before I1, I1 replaces it behind an adapter that reproduces C1's results tick for tick (`I1_ReproducesC1Workshop`), so there is never a second runner on the branch.

## 10. Tier and package mapping

| Capability | Tier | Package | Minimal first version (vertical slice, D6.18) |
|---|---|---|---|
| I1a foundation (section 9.1) | 2 (first industrial merge; after the Tier 1 civilisation and divine set) | **I1a** (its own package in the index) | all neutral items; P10, P11 |
| Wares 40–53, ledger reasons, deposits (scenario-seeded), mechanised mine, smelter (recipes 22, 25), machine shop, engine works, hauler/harvester assembly (29–32), the runner, orders | 2 | **I1** | iron ore + coal deposits seeded by the industrial scenario; recipes 22, 25, 29, 30, 32; orders placed by an NPC owner and by an org office-holder |
| Geology generation, survey, manual extraction, deposit knowledge in minds | 2 (generation), 3 (survey, knowledge spread) | I1 | generation on; survey later |
| Oil chain (well, refinery 23, 27), copper chain (26, 28) | 2 (after the slice) | I1 | |
| Uranium and nuclear fuel (35) | 3 | I1 with K1, I4, I5 | |
| Supply agreements, recorded sales, delivery obligations (4.9) | 2 | **I3** | one agreement: mining org → smelter org, iron ore per delivery, payment in goods or money; `IndustrialSaleClosed` for the slice |
| Toll processing, multi-owner input bins, facility leasing, marketplaces | 3 | I3 | |
| Facility card, queue view, deposit drawing | 2 | I6 | |
| Designs gating recipes | 2 (data hook), 3 (full licensing) | K1 | scenario-seeded known designs |

### 10.1 The D6 package set and the minimal path (ids as DEVELOPMENT_PACKAGE_INDEX; purchases and payments belong to K2a, not C8)

| Id | Title | Owning docs | Hard prerequisites | Soft prerequisites | Tier | Migration, events |
|---|---|---|---|---|---|---|
| **I1a** (foundation), then **I1** (content) | Industrial content and recipes: wares, ledger reasons, deposits, facilities, `RecipeBook` and runner, orders; waste outputs and the pollution field | this doc; WASTE | F1a, F1b, C1, **C6** (index: Iron Coast's facilities belong to organisations whose office-holders place the orders); **I1a before every other I package** | C3 (wages), K1 (design gating), Q1 | 2 | `0031`, `0036`; 1000–1019, 1080–1094 |
| **I2** | Vehicles and harvesting | VEHICLES | I1 (incl. I1a), C1 | C3 (operators), C6 (fleets), D2, D4a (control of operators), Q3 | 2 | `0032`; 1020–1039 |
| **I3** | Refineries and industrial trade (no external buyer): supply agreements, recorded sales, delivery obligations | this doc §4.9 | I1, C2 (obligations), C6 | I2 (deliveries), K2a (money payment; goods payment works without it) | 2 | `0033`; 1040–1049 |
| **I4** | Electricity generation and distribution | ELECTRICITY | I1, C1 | C6 (utility), C3 (staff), C2 (bills), I2 (fuel delivery), K2a, K1 | 2 | `0034`; 1050–1069 |
| **I5** | Mechanised military production | INDUSTRIAL_MILITARY | I1, I2, I3, C6, C3 | I4, C4/M9 (combat, Tier 3), D2/D4, K1 | 2 (end) / 3 | `0035`; 1070–1079 |
| **I6** | Industrial presentation and analytics: VISUAL_WORLD §18, PLAYER_UI §25, KPIs IND/VEH/ELE/IMIL/WST/ENV, warehouse views | VISUAL_WORLD §18, PLAYER_UI §25, each doc's §7 | the I package whose state it shows; U3a, U2a conventions; Q1 (`0030` and the `sql` command) | U5 (reports panel) | 2 (cards, overlays for the slice) / 3 | `0037`; 1095–1099 |
| **K1** | Knowledge, designs and licences | KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md (other agent) | its own (C6, C7 education, C9 for disputes) | — | 2 (design data and seeding) / 3 | its own block |

**Minimal path to the vertical slice (D6.18), Tier 2, started only after the Tier 1 set has merged:** I1a (foundation, neutral, pins P10–P11) → I1 minimal (iron ore and coal deposits seeded, recipes 22, 25, 29, 30, 32, orders) → I2 minimal (harvester and hauler, operator job, harvest routine) → I4 minimal (one coal station, one component, allocation, outage) → I3 minimal (one supply agreement and one `IndustrialSaleClosed`) → Q3 `ind_slice` scenario and VS-1..VS-10 → I6 minimal (facility, vehicle and station cards, electricity overlay, shortage glyphs, visible cargo). I5 and the oil, copper, waste-recycling, nuclear, survey and military extensions follow only if time remains; none may delay a Tier 1 or Tier 2 civilisation or divine package (D6.18). I2 and I4 can run in parallel once I1 has merged; I3 needs C2.

**I1 dependencies in detail.** I1 needs **F1a+F1b** (state-slot and hook pattern, `OwnerKind.Org`, `BinKind.AssetStore`), **C1** (productive assets, `Work`, ownership beliefs; hard), **C6** (org owners and decisions; **hard** per the index: the Iron Coast facilities belong to organisations; an NPC-owned facility still works without it in a test), **C3** (employment; soft: owners and members can work without wages), **K1** (design ids; soft: `DesignId = 0` until K1 lands), **Q1** for the KPIs. It must not start before the Tier 1 set (F0, F1, C1, D1, D2, D3, D6-commandments, U1a, U2a, U3a, Q1-T1, Q3) has merged (D6.18).

## 11. Tests and acceptance hooks

**Unit and invariant tests:**
- `Recipes_Stage3RowsEqualBuildingDefs`: rows 1–20 equal the stage 3 defs field by field.
- `Recipe_ConsumesAtStartProducesAtEnd`: inputs leave at start (`ProcessInput`), outputs appear at completion (`Processed`), IND-I1 and IND-I4 hold; destroying the facility mid-cycle produces only salvage (IND-I3).
- `Extraction_DepletesFinitely`: a deposit of 40 is exhausted after exactly the cycles grade allows; `DepositDepleted` once; IND-I2.
- `Facility_NeedsWorkersPowerInputs`: each missing condition stalls with its reason and emits one `ProductionStalled`; progress is kept.
- `Factory_ProducesAssetWithEmbodiedMatter`: recipe 32 consumes its inputs as `Embodied` (the asset records them) and registers a harvester asset owned by the facility owner.
- `Industry_NoMoneyApi` (static), `Industry_NoOwnershipModelBranch` (static, ORG-INV-5 extended), `Industry_NoActorZeroGoodsCalls` (static: every `Goods.*` call in `Sim/Industry` passes a living actor).
- Pins P10, P11; `Industry_SaveLoadParity` mid-cycle and with a queue of 3 orders.

**D6.18 vertical slice (steps 1–10)** — this document carries steps 4 and 7 and the conservation half of 10:

| Step | Test | Expected |
|---|---|---|
| VS-4 ore processed or sold | `VS4_OreProcessed` (I1) and `VS4_OreSold` (I3) | delivered iron ore is consumed by recipe 25 at the smelter (`ProcessInput`) and steel appears owned by the smelter's org; or the ore is sold with an ownership transfer and an actual payment (I3) |
| VS-7 factory builds a vehicle | `VS7_FactoryBuildsVehicle` | with steel, parts, an engine and wire in the input bin, 3 workers present and power ≥ 500‰, recipe 32 completes and a harvester asset exists at the factory node; with power cut at mid-cycle it pauses and resumes (step 9) |
| VS-10 (part) | `VS10_IndustryReconciles` | IND-I1..I4 and every `fact_*` table of section 7 reconcile to the events; replay of the slice reproduces every checkpoint hash |

**Brief §12 acceptance list** — this document's share: "Extracting and transporting oil without creating money" (extraction half: `ACC_OilExtractedNoMoney`: a well produces crude oil, `ProducedBy(Extracted, CrudeOil)` equals the deposit's reduction, and every money balance is unchanged; the transport half is VEHICLES); "Purchasing a licence and constructing an asset" (construction half: `ACC_LicensedConstruction`: after K1 grants a licence, the facility is built only by delivered materials and NPC labour; without the materials nothing is built); "Unauthorised use of a design creating a potential legal dispute" (I1 emits the evidence: a cycle run under a design the owner holds no licence for carries `Licensed = false` on `ProductionCycleStarted`, which K1 turns into a potential claim); "Deterministic replay of industrial activity" (the slice replay above).

## 12. Proposed shared surface (for the SAC Industry addendum) and open questions

**Proposed event block.** `TelemetryDomain` 70 `Industry`, 71 `Vehicle`, 72 `Energy`, 73 `Waste`, 74 `Environment`, 75 `IndustrialMilitary`; `RangeOf` 70–75 → **1000–1099**.

| Codes | Owner | Events |
|---|---|---|
| 1000–1019 | I1 | 1000 `DepositGenerated`, 1001 `DepositDiscovered`, 1002 `DepositSurveyed`, 1003 `DepositDepleted`, 1004 `ResourceExtracted`, 1005 `ProductionOrderPlaced`, 1006 `ProductionOrderCompleted`, 1007 `ProductionOrderCancelled`, 1008 `ProductionCycleStarted`, 1009 `ProductionCycleCompleted`, 1010 `ProductionCycleAborted`, 1011 `ProductionStalled`, 1012 `FacilityStatusChanged`, 1013 `FacilityDay`, 1014 `BinMoved` (I1a), 1015–1019 spare |
| 1020–1039 | I2 | VEHICLES §7 |
| 1040–1049 | I3 | 1040 `SupplyAgreementMade`, 1041 `SupplyDelivered`, 1042 `SupplyAgreementBreached`, 1043 `SupplyAgreementEnded`, 1044 `IndustrialSaleClosed`, 1045–1049 spare (section 4.9) |
| 1050–1069 | I4 | ELECTRICITY §7 |
| 1070–1079 | I5 | INDUSTRIAL_MILITARY §7 |
| 1080–1094 | I1 (waste and environment part) | WASTE §7 |
| 1095–1099 | I6 | industrial KPI day rows, if any |

The knowledge and money documents need their own block; the lead allocates it (suggested 1100–1199) so the two do not collide.

**Proposed migrations** (after `0030`, so the D5 numbers are untouched): `0031_industry_production.sql` (I1), `0032_vehicles.sql` (I2), `0033_industrial_trade.sql` (I3), `0034_energy.sql` (I4), `0035_military_industry.sql` (I5), `0036_waste_environment.sql` (I1, waste part), `0037_industry_kpi_views.sql` (I6). K1 and the money package take numbers the lead allocates (suggested 0038+).

**Switch.** `Industry.Enabled` (default 0; 1 in the industrial scenario and in industrial tests). Sub-switches `Industry.Geology.Generate` (default 1 when enabled), `Industry.Waste.Enabled` (default 0 until the WASTE package merges).

**Open questions (each with the default taken):**
1. Should stage 3 `Ware.Ore` be renamed or merged with `IronOre`? Default: no; separate wares, so no stage 3 or population hash moves.
2. Should customer-owned inputs be processed for a fee (toll processing)? Default: not in the first version; the facility processes only its owner's lots.
3. The tool ware for manual mining (ECONOMY's proposed `Pick` collided with F1's `Herbs = 32`). Default: manual extraction works without a tool at the base rate; the tool's number is fixed when ECONOMY's tool set lands.


## Owner D8 (28 Nov 2026): unification of the artificial world

Owned by [../unification/UNIFICATION_DESIGN.md](../unification/UNIFICATION_DESIGN.md) (D8.8, D8.12, D8.18). Industry **builds on the unified recipe and building architecture** (`RecipeTable`, `BuildingDef` with levels and capabilities, `ProductiveAsset` ids from C1, the NPC-driven physical economy, no faction economic AI): its mines, smelters, factories and power stations are further rows and capabilities of that architecture, not a separate industrial game. Per D6 sequencing and D8.18, the **unification milestone comes first**; industrial packages start after the long-run unified demonstration passes. Ore, coal and oil re-enter as industrial deposits and recipes on the unified goods range (UNIFICATION §13.4); the Stage 3 ten-processed-ware table is not read in the unified game.
