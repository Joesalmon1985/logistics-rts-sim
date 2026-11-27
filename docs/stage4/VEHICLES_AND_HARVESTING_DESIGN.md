# Vehicles and harvesting design (owner D6, 27 Nov 2026)

Status: design, 27 Nov 2026. Owner decisions D6.1, D6.2, D6.4, D6.6, D6.8, D6.18, D6.19 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)); D5.7, D5.8 for compulsion and possession of operators. House style: [DESIGN_CONVENTIONS.md §12](DESIGN_CONVENTIONS.md#12-how-every-design-doc-is-written). Binding cross-package rules: [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("SAC"). Ware numbers, ledger reasons, the I1a foundation items, ids, event block and migrations are those proposed in [INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md) ("IND") §9 and §12, provisional until the SAC Industry addendum fixes them. Paths are relative to `Assets/God/Sim/`.

---

## 1. Purpose and scope

**This document owns:**
- **vehicles as persistent physical assets**: the entity kind, identity, ownership, location, condition and wear, maintenance, breakdown, destruction;
- **cargo** (goods in a vehicle bin, moved only by the goods API) and the **fuel tank** (fuel lots, burned per node travelled);
- **crew**: seats, operators boarding and leaving within reach, the driver who moves the vehicle;
- **movement** on the node lattice at the vehicle's speed, through the population movement code;
- **the harvesting loop** (source → load → drive → unload at a refinery or store → return), inspired by OpenRA's ore trucks, as an **NPC job**: the operator's own decisions plus a standing **routine** the operator chooses to follow, can refuse or abandon;
- **transport vehicles** (bulk haulers, tankers) and transport companies' hauling for others;
- control of operators by the god (instruction, compulsion, possession).

**Linked, not repeated:** recipes that build vehicles, deposits and extraction, the `Embodied` ledger rule (IND §3–4); fuel production (IND recipe 27); military vehicles, crews in combat and ammunition (INDUSTRIAL_MILITARY_DESIGN.md); waste collection vehicles (WASTE_AND_CIRCULAR_DESIGN.md §4.2); vehicle designs as knowledge, licences (KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md, other agent); payment for haulage (MONEY_AND_ACCOUNTING_DESIGN.md, other agent); possession input (`DivineControlInput`, SAC §11.3, POSSESSION_AND_AVATAR_DESIGN.md §4.2).

**Original names.** Vehicle names and stats are original ("harvester", "hauler", "tanker"), not OpenRA unit names or numbers (project brief: genre conventions only).

## 2. Decisions and principles

| Id | Decision | Why |
|---|---|---|
| VEH-1 | **A vehicle is a productive asset entity with a location, not an `Npc`.** It is a C1 productive asset (asset id, property record, owner, use rights) with an industry record (`Vehicle`) holding node, condition, bins and crew. **Operators are NPCs who occupy it.** | A vehicle never decides, perceives, eats, learns or owns. Putting it in `PopulationState.Npcs` (as the avatar is, SAC §4.2) would make every person loop, the population hash and `NpcId` allocation carry machines, against D5.6 ("every NPC is a persistent autonomous individual"). The avatar is a body because a god *acts through it*; a vehicle is *acted upon* by its crew, exactly like a workshop. Being a C1 asset gives it ownership, use rights, theft classification, inheritance by estates and organisation ownership for free (D6.1: no competing architecture) |
| VEH-2 | **A vehicle moves only when a living crew member drives it.** The driver's own walk, at the vehicle's step cost, moves the vehicle; nothing else moves it (no autonomous truck, no teleport) | D6.4 |
| VEH-3 | **Cargo is goods in a vehicle bin.** Loading and unloading are `Goods.Move` between bins (`MoveReason.BinTransfer`) by a crew member within `Reach.Store` of both bins | SAC §7.1, C8 "no lot changes holder without an actor within reach" |
| VEH-4 | **Fuel is physical.** The tank is a bin of fuel lots; each node travelled burns fuel (`ConsumeReason.Combusted`); an empty tank stops the vehicle where it is | D6.7 principle applied to vehicles |
| VEH-5 | **The harvest loop is a job, not a script.** The vehicle's owner sets a standing routine; the operator gets the routine's next step as a candidate with a routine term in its own score and may choose otherwise at any think (eat, rest, quit, steal the cargo). Automation means the operator does not re-plan each sub-step while it keeps following, never that the vehicle acts alone | D6.4, D5.6 |
| VEH-6 | **Delivery creates no money.** Unloading ore at a refinery changes holder and, under an agreement, owner; payment is a separate act by the buyer (IND §4.9) | D6.2 |
| VEH-7 | **Deterministic and replayable.** Ascending ids, the driver's step order is the population's (ascending NpcId within a tick), keyed streams only for breakdowns | conventions §3 |

## 3. Data structures (`Sim/Industry/Vehicles`, proposed)

### 3.1 Vehicle designs (data; a KNOWLEDGE design payload)

```csharp
public enum VehicleClass : byte { Harvester = 1, Hauler = 2, Tanker = 3, Collector = 4 /* WASTE */, Tractor = 5,
                                  Scout = 10, Carrier = 11, Tank = 12, FieldGun = 13 /* INDUSTRIAL_MILITARY */, Aircraft = 20, Boat = 21 /* later */ }
public enum CargoClass : byte { None = 0, Bulk = 1 /* ores, coal, scrap, refuse */, Liquid = 2 /* crude, fuel */, Packaged = 4 /* metals, parts, food */, Personnel = 8 }

public sealed class VehicleDef                         // frozen table, like BuildingDef; ids stable, appended only
{
    public readonly ushort Id; public readonly string Key, Label; public readonly VehicleClass Class;
    public readonly ushort DesignId;                   // KNOWLEDGE doc (0 = common)
    public readonly ushort BuildRecipe;                // IND RecipeBook row that produces it
    public readonly CargoClass Cargo;                  // which ware classes the cargo bin accepts (checked by the load act, not by Goods)
    public readonly int CargoWeightCap;                // weight units (IND §3.1 weights)
    public readonly Ware FuelWare; public readonly int TankCap;          // units
    public readonly int FuelMilliPerNode;              // fuel burned per lattice step, in milli-units (integer accumulator)
    public readonly int StepTicksRoad, StepTicksOff;   // ticks per node on and off roads (NPCs walk ~ 12/20 by default; vehicles faster on roads, slower or barred off-road)
    public readonly bool OffRoad;                      // false: may only use road nodes and open land of cost ≤ OffRoadMaxCost
    public readonly byte CrewMin, CrewSeats, PassengerSeats;
    public readonly int MaxCondition;                  // permille scale, 1000
    public readonly int WearMilliPerNode, WearPerLoad; // condition loss
    public readonly int ServiceEveryNodes;             // maintenance interval
    public readonly ReadOnlyArray<WareCount> ServiceWares;   // consumed by maintenance (e.g. machine parts 1)
    public readonly int ExtractTicksPerUnit;           // Harvester: ticks per unit taken from a Surface deposit (0 = cannot extract)
    public readonly int Health, Armour;                // INDUSTRIAL_MILITARY combat; Health 0 for unarmoured civil vehicles = uses Condition
}
```

| Id | Vehicle | Class | Cargo | Cap (weight) | Tank (fuel) | Fuel per node (milli) | Step road/off | Crew min/seats | Extract | Build recipe |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Harvester | Harvester | Bulk | 80 (20 iron ore) | 20 | 150 | 6 / 14 | 1/2 | 60 ticks per unit | IND 32 |
| 2 | Hauler | Hauler | Packaged + Bulk | 60 | 16 | 100 | 5 / 12 | 1/2 | — | IND 31 |
| 3 | Tanker | Tanker | Liquid | 60 | 16 | 110 | 5 / 14 | 1/2 | — | (I1 later row) |
| 4 | Collector | Collector | Bulk (waste) | 50 | 12 | 100 | 6 / 14 | 1/2 | — | WASTE |

NPC walking for comparison: `PopulationTuning` road/off step costs (about 12 and 20 ticks a node by default). Numbers are tuning; nothing in the design depends on them.

### 3.2 The vehicle record

```csharp
public enum VehicleStatus : byte { Parked = 0, Moving = 1, Loading = 2, Unloading = 3, OutOfFuel = 4, BrokenDown = 5, Wrecked = 6, Dismantled = 7 }

public sealed class Vehicle                             // IndustryState.Vehicles.List, ascending Asset
{
    public int Asset;                                   // C1 asset id (owner, use rights, disputes in C1's property record)
    public ushort Def;
    public int Node, PrevNode;                          // where it is (a lattice node); PrevNode for drawing the heading
    public int CargoBin, TankBin, AmmoBin;              // LotBins, kind AssetStore, owner = vehicle owner, Node = Node (moved by Goods.MoveBin)
    public int Condition;                               // 0..MaxCondition
    public int WearMilli, FuelDebtMilli;                // integer accumulators
    public int NodesSinceService; public long Odometer; // nodes travelled
    public VehicleStatus Status;
    public int Driver;                                  // NpcId in the driving seat (0 = none)
    public List<int> Crew = new List<int>();            // NpcIds in crew seats, ascending (driver included), ≤ CrewSeats
    public List<int> Passengers = new List<int>();      // ≤ PassengerSeats
    public VehicleRoutine Routine;                      // the standing order (3.3); Kind None = no routine
    public int HomeDepot;                               // a Depot facility asset (0 = none)
    public long BuiltTick; public int BuiltBy;          // factory asset id
    public int[] Embodied;                              // ware counts from the build cycle (IND §3.6), for salvage
}
public sealed class VehicleState { public List<Vehicle> List = new List<Vehicle>(); [JsonIgnore] public Dictionary<int,int> ByOccupant; }  // index rebuilt on load
```

**Why bins.** The cargo, tank and ammunition stores are ordinary `LotBin`s (`BinKind.AssetStore`, SAC §7.1) so every existing rule applies unchanged: weight caps (`LotBin.WeightCap` = `CargoWeightCap`), merging, ownership, the conservation ledger, theft classification, estates, miracles that create or destroy goods "at the target's holder". The only new goods operation is `Goods.MoveBin` (IND §9.1 I1a.4), which moves a bin's node by one lattice step together with its vehicle; the lots inside do not change holder (still `Bin(id)`), so nothing is transferred remotely.

**Occupancy.** A person is in a vehicle when its NpcId is in `Crew` or `Passengers`. The person's `Npc.Node` always equals the vehicle's `Node` (kept by the `Moved` hook, 4.3). No `Npc` field is added: the occupancy index is derived (not hashed) and the lists are in the `industry` hash part. The person keeps its carry, needs and mind; while seated it can do only the vehicle verbs (Drive, Load, Unload, Refuel, Alight), gestures and talk; eating from its carry is allowed while parked.

### 3.3 The routine (a standing order on the vehicle)

```csharp
public enum RoutineKind : byte { None = 0, Harvest = 1, Haul = 2, Collect = 3 /* WASTE */, Supply = 4 /* repeat deliveries under an I3 agreement */ }
public struct VehicleRoutine
{
    public RoutineKind Kind;
    public byte SourceKind;      // 1 Deposit (Surface: extract), 2 Bin (load from a facility's output bin), 3 Ground (collect lots)
    public int SourceId;         // DepositId, BinId or node
    public int DestBin;          // where to unload (a refinery's InputBin, a store, a waste site)
    public Ware Ware;            // what to load (None = any ware of the cargo class)
    public short FillPermille;   // load until cargo weight ≥ this share of cap (default 900)
    public short RefuelBelowPermille;  // detour to the depot fuel bin when the tank is below this (default 250)
    public int SetBy, Decision;  // NpcId who set it (owner or office-holder), C6 OrgDecision id
    public long SetTick;
    public int Agreement;        // I3 supply agreement the deliveries serve (0 = internal)
}
```

Setting a routine is an act of the owner (or an office-holder with the power) at the vehicle (within `Reach.Touch`), a `SetRoutine` intent; it is information on the asset, not a command to anyone. Who operates the vehicle and whether they follow the routine is separate (4.4).

### 3.4 Goods-API usage (unchanged API plus the I1a items)

| Act | Goods call | Reach |
|---|---|---|
| Load from a facility bin | `Move(Bin(src), lot, n, Bin(CargoBin), BinTransfer, actor = crew member, OwnerRule.Keep)` | `Reach.Store` of both bins (the vehicle parked within 1 node of the source bin) |
| Extract from a Surface deposit (harvester) | `Produce(Bin(CargoBin), ware, 1, owner, ProduceReason.Extracted, actor = driver, source = DepositId)`, `Deposit.Remaining -= 1` | driver within `Reach.Gather` of the deposit node |
| Unload into a destination bin | `Move(Bin(CargoBin), lot, n, Bin(dest), BinTransfer, actor, rule)`: `Keep` for internal; `Set` to the buyer with `OwnershipBasis.SupplyAgreement` under an agreement | `Reach.Store` of both |
| Refuel | `Move(Bin(depotFuel), lot, n, Bin(TankBin), BinTransfer, actor, Keep)` (or from carry with `Deposit`) | as above |
| Burn fuel | `Consume(Bin(TankBin), lot, 1, Combusted, actor = driver)` whenever `FuelDebtMilli ≥ 1000` | the driver is in the vehicle |
| Move the bins with the vehicle | `Goods.MoveBin(s, ev, bin, toNode, actor = driver)` for each of the vehicle's bins after a step | adjacent node only |

## 4. Mechanics

### 4.1 Coming into existence and leaving it

- **Built** by a factory recipe with an `AssetOutput` (IND §4.4 step 4): `Vehicles.Register(def, node = factory node, owner = factory owner, embodied)` creates the C1 asset (`AssetBuilt` 270) and the `Vehicle` record, makes its empty bins (`Goods.NewBin(AssetStore, owner, node, cap)`) and emits `VehicleRegistered { Vehicle, Def, Owner, Factory, Order }`. The industrial scenario may instead place vehicles at setup (`VehicleRegistered` with `Factory = 0`, reason `Placed`, the scenario's honest "it already existed" record).
- **Owned** like any asset: sold (ownership transfer of the asset record by C1's asset transfer with a recorded sale, IND §4.9), inherited (estates, MATERIAL_INHERITANCE `AssetKind.Building`-like row; a vehicle row is added), seized, stolen (an unauthorised drive away is C1 theft of an asset).
- **Wrecked** at `Condition = 0` from combat or an accident: status `Wrecked`, crew alight at the node, cargo and tank lots stay in their bins (a wreck can be emptied by anyone within reach; taking an owner's goods is theft).
- **Dismantled** by a dismantling recipe (WASTE §4.3) at a scrap yard or in place: releases `Salvaged` scrap and parts ≤ `Embodied`; the bins must be empty first (`Goods.RemoveBin` throws otherwise); `VehicleDismantled`.

### 4.2 Boarding and leaving

`Board` (learning verb 21) by an NPC within `Reach.Touch` of the vehicle node, with a free seat, while the vehicle is not `Moving`. The mind sees the vehicle as an object (`ObjClass.Vehicle` 117, through `SenseExtra`) with its affordances: Board, Drive (from the driving seat), Load, Unload, Refuel, Maintain. Permission is the mind's belief (`Permits.Believed` extended by C1 with asset ownership and use rights): its own vehicle, its household's, its employer's under a `Work` use right with the operator role, or anyone's in an open-access arrangement. Boarding without a believed permission is possible (a person may take a vehicle); driving it away is classified by C1 as theft of the asset's use. `Alight` (`DivineAct` 23, learning verb part of Board's family) puts the person at the vehicle node. Death of an occupant removes it from the seat (`IndustryHooks.OnDeath`, after Divinity in the `Kill` order).

### 4.3 Driving and movement on the lattice

- **One movement implementation.** The driver chooses `Drive` (learning verb 22) towards a node (from the routine, its own plan, a divine `MoveTo` or a possession heading). `PopulationSystem` walks the **driver** as it walks anyone (`NodePather.Find(from, to, nodeRoad, roadCost, offCost, maxExpand)`, `Population/NodePather.cs`), with two industry sites from I1a.7:
  - `PopulationExtensions.StepTicks(s, npc, from, to, base)`: for a driver, the vehicle's `StepTicksRoad`/`StepTicksOff`; a node that the vehicle cannot enter (off-road beyond `OffRoadMaxCost`, water, a node blocked by a building) refuses the step, so the path is computed with the vehicle's costs (`IndustryHooks.PathCosts` passes them to `NodePather`). The default returns `base` (neutral).
  - `PopulationExtensions.Moved(s, ev, npc, from, to)`: for a driver, `IndustryHooks.Moved` moves the vehicle (`Node`, `PrevNode`), its bins (`Goods.MoveBin`), every other occupant (`Npc.Node = to`, their `Path` cleared), adds `FuelMilliPerNode` to `FuelDebtMilli` and burns whole units, adds wear, increments `Odometer` and `NodesSinceService`, and emits `VehicleMoved` (HV; Summary keeps one per 10 nodes and every stop).
- **Before each step** the hook checks fuel: if the tank cannot cover the step (`TankBin` empty and `FuelDebtMilli + FuelMilliPerNode > 1000`), the step is refused, status `OutOfFuel`, `VehicleStopped { Reason = Fuel }`; the driver's intent fails as a physical failure (a frustration signal to its learner, NPC_LEARNING), and the person may walk to fetch fuel (`Alight`, `Take`, `Refuel`).
- **Breakdown.** Below `Condition < 200`, each step draws from `Rng.Fork("vehicle", asset, odometer)`: breakdown chance `(200 − Condition) / 4` permille; on a breakdown, status `BrokenDown` until maintained (`VehicleBrokenDown`).
- **Order.** Steps happen inside `PopulationSystem` (order 250) in ascending driver NpcId, so two vehicles never resolve in an order that depends on anything but ids. Passengers never step themselves while seated (their movement intents are refused `in_vehicle` unless they `Alight`).
- **Speed and capacity on roads** are per-vehicle step costs only in the first version; congestion and road wear are later.

### 4.4 The harvesting loop as an NPC job (OpenRA-style, operated by a person)

The OpenRA loop **source → load → drive → unload → return** is reproduced as a sequence of ordinary intents of a persistent operator.

**Who operates.** The owner (an NPC operating its own harvester), a member of the owning cooperative, or an employee under a C3 contract whose job is "operate vehicle V" (`JobKind.Operate`, a C3 job kind added by I2) with role `Operator` (Role 11). The employment terms, not the routine, are why the person is there.

**What the operator's mind sees.** While the person holds the operator role for V (its `IndustryMind` holds `{ Vehicle, RoutineSeen, Since }`), `IndustryMindHooks.AddCandidates` adds **the routine's next step** as one candidate (within the Industry quota of 4):

| Vehicle state | Next-step candidate |
|---|---|
| operator not aboard | `Board(V)` (walk there first: reach) |
| aboard, tank below `RefuelBelowPermille` | `Drive(depot)`, then `Refuel` |
| aboard, cargo below `FillPermille`, not at source | `Drive(source node)` |
| at a Surface deposit | `LoadCargo` = extract (busy for `ExtractTicksPerUnit` per unit until full, the deposit is empty, or interrupted) |
| at a source bin | `LoadCargo` = `BinTransfer` from the source bin (only lots of the source bin's owner the operator believes it may load: internal, or bought under the agreement) |
| cargo full (or source empty), not at destination | `Drive(dest bin node)` |
| at destination | `UnloadCargo` (BinTransfer; ownership per 3.4) |
| unloaded | back to the first row (the loop) |
| end of shift (C3 terms) | `Drive(home depot)`, `Alight` |

The candidate carries a **routine term** in the person's own score: `RoutineFollow` = the person's belief about its employment obligation and expected pay, its habit (learned associations of following the routine with wages and approval), its traits (diligence, loyalty), and its fear of dismissal; it is the same kind of term as a soldier's assignment (MILITARY §4 "Orders are assignments"). Competing candidates (hunger, fatigue, family care, a social interaction, an opportunity to sell the cargo for itself) are scored as always. Consequences:
- **Following** is the common case when wages are paid and needs are met; the loop then runs without the operator re-planning each sub-step: once it chooses `FollowRoutine`, a routine spell chains the next steps at each act's end (`ActionEnded` site, SAC F1.6) **until** a think tick produces a better-scoring different candidate. The person still thinks every `ThinkEvery` ticks.
- **Refusing or abandoning**: the person may stop at any think (eat at the roadside, go home, quit the job, which is C3's `EmploymentEnded`); the vehicle stays where it is (`VehicleRoutineAbandoned { Reason }`).
- **Misusing**: taking the cargo home, selling it, or driving off with the vehicle are possible acts, classified by C1 (theft) and remembered by witnesses (M5, M8).
- **Learning**: wages after deliveries, shortfalls, punishment for idling, reach the operator's learner through the ordinary channels; routine-following can become habitual or decay.

**Delivery to a refinery or a store.** Unloading into the refinery's `InputBin` changes the holder. Internal routines keep the owner; agreement routines set the buyer as owner with basis `SupplyAgreement` and open the payment obligation (IND §4.9). **No money appears**; the refinery owner's income comes only when it later sells its products and is paid.

**Several harvesters** on one deposit or one destination bin are fine: each operator acts in turn by id; a bin full of ore (`NoRoom`) fails the unload physically and the operator waits or goes elsewhere by its own choice.

### 4.5 Transport vehicles and transport companies

- **Haulers and tankers** move packaged and liquid goods between bins with the same `Load`/`Drive`/`Unload` acts and the `Haul` or `Supply` routine. A **transport company** (an organisation, ORGANISATIONS §14) hauls for others under a haulage agreement: the cargo **keeps its owner** (`OwnerRule.Keep`: the transport company holds but does not own it), and the company is paid by its customer as agreed (goods or money per the MONEY doc). Losing or stealing cargo in transit is theft against the cargo owner, with every consequence.
- **Personnel**: a carrier (INDUSTRIAL_MILITARY) or a later bus seats passengers who board and alight by their own choice.
- **Hand-carry remains**: a person can always carry one unit of ore to a smelter; vehicles multiply throughput (about 20 units a trip for a harvester), which is why owning one matters (ECONOMY Q9's "capital multiplies output", measured).

### 4.6 Maintenance, condition and wear

- Wear per node (`WearMilliPerNode`) and per load (`WearPerLoad`) lowers `Condition`; `NodesSinceService > ServiceEveryNodes` adds double wear.
- `Maintain` (learning verb 26) by an NPC within `Reach.Touch` with the vehicle parked: 240 ticks, consumes `ServiceWares` from the person's carry or a depot bin (`Goods.Consume(..., ConsumeReason.Wear)`), restores `Condition` by `200 + 2 × skill(Mechanic)` permille (capped), resets `NodesSinceService`; repairs a `BrokenDown` vehicle. An engineer role (Role 13) and a mechanic skill make it faster; anyone can try.
- A vehicle with no maintenance degrades until it breaks down: fleets need mechanics and parts, so machine-parts production matters.

### 4.7 The god and vehicles (existing divine rules only)

- **Instruction** to an operator ("deliver the ore to the smelter", "stop working for the mining company") reaches the person by D2's perception and is weighed by its own policy; the god never edits a routine or an order (D5.1).
- **Compulsion** of an operator makes it attempt one valid physical act (`Drive` to a node, `Unload` into a bin within reach, `Alight`); it cannot make an empty tank drive, a broken vehicle move, or a bin accept cargo it has no room for (D5.7).
- **Possession** of an operator (one body at a time, D5.8) gives the player the person's own capabilities: with `DivineControlInput` `Heading` or `MoveTo` the possessed driver drives (the vehicle follows the body by 4.3, at the vehicle's speed and fuel cost); `Action` acts map to the vehicle verbs (`DivineAct` 22 Board and 23 Alight added to the D2 table; Load and Unload map to `Take`/`Drop` targets that are vehicle bins; Refuel to `UseTool`). Every action carries `Control = 2` on its events; the operator remembers it (`SelfFeature.Possessed`). Possessing the operator does not change the vehicle's owner or the cargo's owner.
- **Miracles** act on vehicles as on buildings and goods: smite (condition loss, wreck), create or destroy fuel or cargo in its bins (ledger rows), heal the crew.

### 4.8 Determinism and replay

Everything that moves a vehicle is a driver's step in `PopulationSystem` (ascending NpcId) or a command in the replay stream (divine inputs). Breakdown draws use a keyed stream that depends only on the asset id and odometer. Vehicle state is in the `industry` hash part; the occupant index is rebuilt on load. Required: `Vehicle_SaveLoadMidTrip`, `Vehicle_ReplayExact` (a recorded harvest day with a possessed driver segment replays to the same hashes).

## 5. Interactions with other systems

| System | Interaction |
|---|---|
| Population | the driver's walk; the two movement sites; occupants' nodes; `OnDeath` empties the seat |
| Goods | `BinTransfer`, `MoveBin`, `Produce(Extracted)`, `Consume(Combusted, Wear)` |
| C1 | vehicles are assets: ownership, use rights, theft, asset transfer |
| C3 | operator jobs, shift terms, wages, quitting |
| C6 | fleets owned by organisations; routines set by office-holders |
| IND | build recipes, embodied matter, deposits, refinery bins, supply agreements |
| ELECTRICITY | fuel and coal deliveries to station fuel bins; electric vehicles later |
| WASTE | collection routines, dismantling, scrap |
| INDUSTRIAL_MILITARY | combat vehicles share this record; crew seats; ammunition bin |
| Divinity | 4.7; control tags on events |

## 6. Invariants

| Id | Invariant |
|---|---|
| VEH-I1 | Every vehicle's bins are at the vehicle's node; every occupant's `Npc.Node` equals the vehicle's node |
| VEH-I2 | A vehicle's `Node` changes only in `IndustryHooks.Moved` for its living driver, by one lattice step |
| VEH-I3 | Cargo and tank contents change only through `Goods` with a living actor within reach (C8); the ledger C1 (IND-I1) holds with vehicles in motion |
| VEH-I4 | Fuel burned: `Σ ConsumedBy(Combusted, fuel) by vehicles = ⌊Σ nodes × FuelMilliPerNode / 1000⌋ ± open accumulators` per vehicle |
| VEH-I5 | Seats: `Crew.Count ≤ CrewSeats`, `Passengers.Count ≤ PassengerSeats`; a person is in at most one vehicle |
| VEH-I6 | Salvage from a vehicle ≤ its `Embodied` (IND-I3) |
| VEH-I7 | No delivery event changes a money balance (VEH-6); delivery under an agreement opens exactly one obligation or records an immediate payment |

## 7. Telemetry and warehouse facts

**Events** (block 1020–1039, domain `Vehicle`, proposed): 1020 `VehicleRegistered`, 1021 `VehicleBoarded`, 1022 `VehicleAlighted`, 1023 `VehicleMoved` (HV), 1024 `VehicleStopped` (reason: arrived, fuel, breakdown, blocked, driver left), 1025 `VehicleLoaded` (one per load act: ware, count, source), 1026 `VehicleUnloaded` (ware, count, destination bin, owner before/after), 1027 `VehicleRefuelled`, 1028 `VehicleFuelBurnedDay` (aggregate per vehicle per day), 1029 `VehicleMaintained`, 1030 `VehicleBrokenDown`, 1031 `VehicleWrecked`, 1032 `VehicleDismantled`, 1033 `VehicleRoutineSet`, 1034 `VehicleRoutineStep` (HV), 1035 `VehicleRoutineAbandoned`, 1036 `VehicleOwnershipTransferred` (C1 asset transfer, mirrored), 1037 `VehicleDay`, 1038–1039 spare. Goods effects are the existing `ItemMoved`, `Produced`, `Consumed`, `OwnershipChanged` (each load and unload has exactly one `ItemMoved` with reason `BinTransfer`).

**Warehouse** (`0032_vehicles.sql`, I2):

| Table | Grain | Reconciliation |
|---|---|---|
| `dim_vehicle` | one row per vehicle (def, built tick, factory, embodied matter) | = `VehicleRegistered` |
| `fact_vehicle_ownership_spell` | owner spells | open/close at registration and transfers |
| `fact_vehicle_crew_spell` | (vehicle, person, seat) boarding to alighting | = Boarded/Alighted pairs |
| `fact_vehicle_trip` | one row per routine cycle (source, destination, ware, count, nodes, fuel, operator, control code) | Σ unloaded = Σ `ItemMoved(BinTransfer)` into destination bins |
| `fact_vehicle_day` | (vehicle, day): nodes, fuel, wear, status ticks, loads | Σ fuel = `ConsumedBy(Combusted)` of vehicle tanks |
| `fact_vehicle_event` | breakdowns, maintenance, wrecks | = events |

KPIs (I6): VEH-01 tonnes (weight units) moved per vehicle-day; VEH-02 fuel per unit delivered; VEH-03 share of operating ticks lost to breakdown, fuel and absent operators; VEH-04 routine adherence (routine steps chosen / routine steps offered) by employer type; VEH-05 trips under divine control.

## 8. Player and view

The world shows each vehicle as a distinct original low-poly model driving smoothly between its nodes (interpolated in the view from `VehicleMoved`), **with its cargo visibly heaped** (a cargo mesh scaled by the cargo bin's weight share and coloured by ware), the operator visible in the cab, exhaust only while moving, a stopped vehicle with a reason glyph (fuel can, spanner, empty seat). Vehicle inspection card: owner, operator and crew, routine and its current step, cargo by ware and owner, tank level, condition, odometer, last maintenance, trips today. Details in [VISUAL_WORLD_DESIGN.md §18](VISUAL_WORLD_DESIGN.md#18-owner-d6-27-nov-2026-industrial-presentation) and [PLAYER_UI_DESIGN.md §25](PLAYER_UI_DESIGN.md#25-owner-d6-27-nov-2026-industrial-interface).

## 9. Migration from the existing code

- **Stage 3 harvester walkers** (`Economy/EconomyState.cs` `WalkerKind.Harvester`) stay in stage 3 mode; they are not vehicles and nothing here touches them.
- **Movement**: the two sites (I1a.7) are the only edit to `PopulationSystem`; with no vehicles they return the base values, so pins hold. `NodePather` is reused unchanged (a cost profile is passed in).
- **`Goods.MoveBin`** is the only addition to the frozen goods API (amendment row requested, IND §9.1). Its static test: only `IndustryHooks.Moved` calls it.
- **C3 job kind `Operate`** and **D2 `DivineAct` 22–23** are additive table rows requested from those packages.

## 10. Tier and package mapping

| Capability | Tier | Package | Minimal first version (D6.18) |
|---|---|---|---|
| Vehicle record, bins, register from a factory or scenario, ownership as C1 asset | 2 | **I2** | harvester and hauler defs 1–2 |
| Board, Drive, Alight; movement sites; fuel burn; out of fuel | 2 | **I2** | |
| Harvest routine (Surface deposit and facility bin sources), operator role, routine candidate and term, abandon | 2 | **I2** | one harvester, one operator employed by the mining org, iron ore to the smelter and coal to the power station |
| Possession and compulsion of operators (D4a, D2 table rows) | 2 | I2 with D4 | possessed driver drives the loaded harvester |
| Maintenance, wear, breakdown | 2 (wear and maintain), 3 (breakdown draws) | I2 | |
| Haulage for others, tankers, transport companies | 3 | I2, I3 | |
| Wrecks, dismantling, salvage | 3 | I2, WASTE | |
| Vehicle inspection, visible cargo, routine view | 2 | I6 | |
| Congestion, road wear, electric vehicles, rail, boats, aircraft | later | — | |

**Dependencies.** I2 needs **I1** (I1a foundation: bins, `BinTransfer`, `MoveBin`, sites, ranges; I1 build recipes and deposits), **C1** (assets, permission beliefs, theft), **C3** (operator employment; soft: an owner-operator needs no C3), **C6** (org fleets; soft), **D4a** (possession of operators; only for that part), **D2** (compulsion; only for that part), **Q3** for the replay and save tests. It must come after the Tier 1 set (D6.18).

## 11. Tests and acceptance hooks

**Unit and invariant tests:** `Vehicle_MovesOnlyWithDriver` (no driver: no node change in 1000 ticks); `Vehicle_BinsFollow` (VEH-I1 after 200 random steps); `Vehicle_FuelBurnExact` (VEH-I4 over a 100-node trip; empty tank stops at the exact node); `Vehicle_LoadUnloadReachChecked` (a load from a bin 3 nodes away is refused `OutOfReach`); `Vehicle_CargoOwnershipKeptInHaulage`; `Vehicle_TheftOfUseClassified` (a non-permitted person drives off: C1 classification event); `Vehicle_NoNpcRecord` (static: `Vehicles` never adds to `PopulationState.Npcs`); `Vehicle_SaveLoadMidTrip`; `Vehicle_ReplayExact`.

**Behavioural tests (reported over 20 seeds, not targeted):** `Routine_FollowedWhenPaid` (operators with paid wages follow ≥ 80% of offered routine steps over 5 days; with wages unpaid for 3 days, adherence falls significantly); `Routine_AbandonedForHunger` (an operator with high hunger and food in sight leaves the routine at least once).

**D6.18 vertical slice:**

| Step | Test | Expected |
|---|---|---|
| VS-1 a persistent NPC operates a harvester | `VS1_NpcOperatesHarvester` | a named NPC with an employment contract boards the harvester and drives it; every move has `Driver = that NpcId`; the NPC keeps its needs and eats during the shift |
| VS-2 the harvester collects physical ore | `VS2_HarvesterCollectsOre` | cargo bin gains iron ore lots with `ProduceReason.Extracted`; `Deposit.Remaining` falls by exactly the loaded count |
| VS-3 it delivers the ore to a refinery | `VS3_DeliversToSmelter` | an `ItemMoved(BinTransfer)` from the cargo bin into the smelter's input bin by the operator within reach; owner kept (internal) or set to the smelter org (agreement) |
| VS-5 (delivery half) a power station receives fuel | `VS5_CoalDelivered` | a second routine (or the same vehicle's next trip) unloads coal into the station fuel bin (ELECTRICITY VS-5 burns it) |
| VS-8 an existing NPC operates the completed vehicle | `VS8_NewVehicleOperated` | the harvester built in VS-7 is boarded and driven by an existing NPC (no NpcId created); its first trip burns fuel from its tank |
| VS-9 (vehicle half) disruption | `VS9_FuelCutStopsHarvester` | with the depot fuel bin empty, the harvester stops `OutOfFuel` and the smelter's input runs dry, stalling recipe 25 with `MissingInputs` |

**Brief §12 acceptance list (this document's share):** "Extracting and transporting oil without creating money": `ACC_OilTransportedNoMoney` (a tanker loads crude from the well's output bin and unloads it at the refinery; every money balance and every money supply measure of the MONEY doc is unchanged; C1 holds); "Selling resources through actual payment and ownership transfer" (delivery half: `ACC_DeliveryUnderAgreement` shows the ownership transfer at unload; the payment half is IND §4.9 and the MONEY doc); "Deterministic replay": `Vehicle_ReplayExact`.

## 12. Open questions (with defaults)

1. **Collision and congestion.** Default: vehicles and people may share nodes freely; no collisions in the first version.
2. **Unattended extraction.** Should a harvester keep extracting while its operator sits idle? Default: no; extraction is the driver's busy action, so labour is always attributable.
3. **Electric vehicles charged from the grid.** Default: later; the tank model generalises to a charge store (energy recorded in the energy ledger, never as a ware).
