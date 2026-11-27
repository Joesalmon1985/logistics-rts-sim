# Electricity design (owner D6, 27 Nov 2026)

Status: design, 27 Nov 2026. Owner decisions D6.1, D6.2, D6.6, D6.7, D6.13, D6.14, D6.15, D6.16, D6.18 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)). Inspired by the board game Power Grid's principle that generating technologies differ in construction cost, fuel, efficiency and capacity; **not** its central resource market, its money or its auction rules (D6.6, D6.12: design and asset auctions belong to the KNOWLEDGE doc on the one monetary system). House style: [DESIGN_CONVENTIONS.md §12](DESIGN_CONVENTIONS.md#12-how-every-design-doc-is-written). Shared surface proposals (ware numbers, ledger reasons, ids, the event block, migrations) are those of [INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md) ("IND") §9 and §12, provisional until the SAC Industry addendum.

---

## 1. Purpose and scope

**This document owns:**
- **generating technologies** (coal, oil, hybrid, waste, nuclear, renewable) as data-driven designs: construction materials, fuel per energy unit, efficiency, capacity, staffing, operating consumables, emissions and waste;
- **power stations** as facilities whose fuel is physically delivered into their fuel bin and burned;
- **the abstract deterministic network model**: power links between connection points; grids as connected components; per-interval supply and demand balance; dispatch, allocation priorities, shortage, rationing and outage; connection and disconnection;
- **demand** tied to actual activities and equipment (recipe loads, facility standby, later homes and workshops);
- **production consequences** of insufficient supply;
- **the energy ledger**: how generated and delivered energy is recorded **without being a good or money**;
- **utilities** as organisations of any ownership model: obtaining fuel, staffing, metering, billing, being paid, investing, competing, failing.

**Linked, not repeated:** recipes and the runner that read the allocation (IND §4.4); fuel wares and how fuel reaches a station (IND §3.1, §4.9; VEHICLES §4.4); waste fuels and ash (WASTE); the nuclear fuel chain and the strategic device (IND recipe 35, INDUSTRIAL_MILITARY §4.7); designs, licences and auctions of station designs (KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md, other agent); tariffs in money, payment instruments (MONEY_AND_ACCOUNTING_DESIGN.md, other agent); obligations (C2); organisations (ORGANISATIONS §3, §14).

## 2. Decisions and principles

| Id | Decision | Source |
|---|---|---|
| ELE-1 | **Energy is not a good.** It is never an `ItemLot`, never in a bin, never in the goods ledger, never owned, never stored (first version). It exists as a flow on a grid during an interval and is recorded in a separate **energy ledger** | D6.16 (matter vs energy) |
| ELE-2 | **Energy is not money.** Generating, transmitting or delivering energy never changes any money balance or creates a claim by itself. A claim arises only when a utility's staff bill a consumer under an agreement (a C2 obligation); revenue arises only when the consumer actually pays, or when an agreed funder (a government budget line, a cooperative's members) actually hands over | D6.2, D6.7 |
| ELE-3 | **Fuel is physical.** A station generates only from fuel lots in its fuel bin; burning consumes them (`ConsumeReason.Combusted`) and emits ash, spent fuel and pollution | D6.7 |
| ELE-4 | **No faction-level power number.** Electricity flows only within a connected network of built links; a factory not connected to a generating station has no power, whatever its owner's wealth | D6.7 |
| ELE-5 | **Abstract and deterministic first.** No voltages, phases or losses beyond an optional fixed permille; a grid is a connected component; balancing is once per interval with integer arithmetic and id tie-breaks | D6.7 |
| ELE-6 | **Demand comes from equipment that is actually running or ready to run.** There is no per-capita demand formula | D6.7 |
| ELE-7 | **No central fuel market.** Utilities get fuel through purchase, owned extraction, supply agreements, internal transfers, communal distribution or government provision (IND §4.9) | D6.6 |
| ELE-8 | **Any ownership model.** Private, public (government org), cooperative and communal utilities run the same mechanics; no code branches on the model | D6.7, D5.17 |
| ELE-9 | **Staffed.** A station runs only with its minimum staff on duty, physically present (IND-6) | D5.6 |

## 3. Data structures (`Sim/Industry/Grid`, proposed)

### 3.1 Units

- **Power unit (`pu`)**: an integer rate. **Energy unit (`eu`)**: 1 pu for 1 tick. `kEU` = 1000 eu.
- A grid interval is `Grid.IntervalTicks` ticks (default **100**; 12 intervals a day at 1200 ticks per day).
- Typical loads (defaults): mechanised mine 30 pu, smelter 40, refinery 50, machine shop 40, vehicle assembly 80, nuclear fuel processing 120; a later household 2 pu.

### 3.2 Generating technologies (design data)

```csharp
public enum GenTech : byte { Coal = 1, Oil = 2, Hybrid = 3, Waste = 4, Nuclear = 5, Wind = 6, Solar = 7, Hydro = 8 }

public sealed class GeneratorDesign                    // frozen table; a KNOWLEDGE design payload (DesignId)
{
    public readonly ushort Id; public readonly string Key, Label; public readonly GenTech Tech;
    public readonly ushort DesignId;                    // KNOWLEDGE: needed to build and to operate
    public readonly ReadOnlyArray<WareCount> Build;     // construction materials (consumed as Embodied)
    public readonly int BuildWorkTicks;                 // NPC construction labour
    public readonly int CapacityPu;                     // maximum output
    public readonly ReadOnlyArray<Ware> Fuels;          // accepted fuels (Hybrid: coal or fuel oil); empty for renewables
    public readonly int FuelMicroPerKeu;                // micro-units of fuel per kEU generated (efficiency is implied; see table)
    public readonly short EfficiencyPermille;           // shown in reports; FuelMicroPerKeu = BaseFuelMicroPerKeu(fuel) × 1000 / Efficiency
    public readonly byte StaffMin, StaffFull;           // on duty
    public readonly ReadOnlyArray<WareCount> UpkeepPerDay;   // operating consumables (machine parts wear), consumed by a staff act at day start
    public readonly short MinLoadPermille;              // a thermal station that runs at all runs at ≥ this share
    public readonly int StartTicks;                     // from cold to output
    public readonly int EmissionMicroPerKeu;            // pollution added to the hex (WASTE §5)
    public readonly ReadOnlyArray<WareCount> WastePerKeuMicro;   // ash, spent fuel (micro-units per kEU)
    public readonly int RiskOnDamage;                   // Nuclear: contamination released if the station is smitten or destroyed (WASTE §5.3)
}
```

| Id | Design | Fuel | Fuel per kEU (micro-units) | Efficiency | Capacity pu | Staff min/full | Build materials (default) | Emission per kEU | Waste per kEU |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Coal station | coal | 10 000 (1 coal = 100 kEU) | 330‰ | 400 | 3/5 | steel 20, brick 30, machine parts 10, wire 10 | 30 | ash 2000 micro |
| 2 | Oil station | fuel oil | 8 000 | 380‰ | 300 | 2/4 | steel 16, brick 20, machine parts 10, wire 8 | 20 | sludge 500 micro |
| 3 | Hybrid station | coal or fuel oil (coal first, then oil, by bin order) | 11 000 / 9 000 | 300‰ / 340‰ | 350 | 3/5 | steel 22, brick 30, machine parts 12, wire 10 | 28 | ash or sludge |
| 4 | Waste station | refuse | 25 000 | 180‰ | 150 | 2/3 | steel 12, brick 20, machine parts 8, wire 6 | 40 | ash 6000 micro |
| 5 | Nuclear station (fictional) | nuclear fuel | 2 (1 unit = 500 000 kEU) | n/a | 1000 | 6/8 | steel 60, brick 40, machine parts 30, wire 30, design licence | 0 | spent fuel 2 micro |
| 6 | Wind array | — | 0 | — | 120 × availability | 1/1 | steel 10, machine parts 6, wire 6 | 0 | 0 |
| 7 | Solar field | — | 0 | — | 100 × daylight | 0/1 | copper 6, wire 8, steel 6 | 0 | 0 |
| 8 | Hydro (later) | — | 0 | — | 200 | 1/2 | needs a river model | 0 | 0 |

Numbers are original tuning (`SimTuning.Industry.Grid.Designs`), chosen so a coal station burns about 5 coal a day at full output: a harvester trip of 20 coal keeps it running four days, so supply chains matter. Renewables need no fuel but have variable availability (3.5) and low capacity; nuclear needs a long knowledge, licence and fuel chain (Tier 3) and carries contamination risk.

### 3.3 Stations, links and ports

```csharp
public sealed class Station                              // IndustryState.Grid.Stations, ascending Asset; also a Facility (IND §3.5) of kind PowerStation
{
    public int Asset; public ushort Design;
    public int FuelBin, AshBin;                          // the Facility's bins (FuelBin = Facility.FuelBin)
    public bool On;                                      // set by a staff act (Operate/switch); off stations generate nothing
    public long WarmFromTick;                            // output only after StartTicks of being on with staff
    public long FuelDebtMicro; public int EuRemainder;   // integer accumulators of fuel owed (micro-units) for generated energy
    public int[] WasteDebtMicro;                         // per waste ware
    public short DispatchRank;                           // the operator's policy: lower runs first (default by tech: renewables 0, nuclear 1, coal 2, hybrid 3, oil 4, waste 5)
    public int LastOutputPu, LastAvailablePu;
}

public sealed class PowerLink                            // IndustryState.Grid.Links, ascending Id; a C1 asset too (owned, built, damaged, stolen)
{
    public int Id;                                       // LinkId: Grid.NextLinkId
    public int Asset;                                    // C1 asset id
    public int PortA, PortB;                             // the two connection points
    public int LengthNodes;                              // lattice distance; build cost scales with it
    public bool Closed = true;                           // switched in; an open link carries nothing
    public int Condition = 1000;
    public int CapacityPu;                               // 0 = unlimited (first version ignores capacity)
}

public sealed class GridPort                             // IndustryState.Grid.Ports, ascending Id: where a facility or pole meets the network
{
    public int Id; public int Node;
    public int Facility;                                 // facility asset id (0 = a bare pole/junction)
    public byte Priority;                                // 0 Interruptible, 1 Normal, 2 Contracted, 3 Critical (set by the network operator's policy and the consumer's agreement)
    public int Meter;                                    // Meter id (0 = unmetered)
}

public sealed class Meter                                // IndustryState.Grid.Meters, ascending Id: a physical instrument at a port
{
    public int Id, Port; public int SupplierOrg;         // the utility that sells through this meter (the network operator of the component, or a named seller)
    public long DeliveredEu, ReadEu;                     // cumulative delivered; the value at the last reading by a person
    public int LastReadDay; public int Agreement;        // the electricity supply agreement (an I3 SupplyAgreement with Ware = None and an energy price term)
}
```

A **grid** is not stored as an object: it is the connected component of ports joined by closed, built links, recomputed when a link changes (3.4). Each component has a **network operator**: the organisation that owns the most links in it (ties: lowest OrgId), or the one a scenario or an agreement names; it sets port priorities and dispatch ranks through its own office-holders' decisions.

### 3.4 Components (deterministic)

`Grid.Dirty` is set by any link built, destroyed, opened, closed or damaged to 0, and by any port added or removed. At the next interval start `ElectricitySystem` recomputes components by union-find over links in ascending `LinkId` (closed and `Condition > 0` only), roots chosen as the lowest port id; components are ordered by their root id. `GridChanged { Components, Ports, Links }` is emitted when the partition changes. Bounds: ≤ 256 ports, ≤ 512 links per island in the first version.

### 3.5 Availability of renewables

- **Solar**: `daylight(tick)` is a fixed integer curve over the day (0 at night, 1000 at noon, from `SimCalendar` day fraction). No randomness.
- **Wind**: one draw per day from `Rng.Fork("wind", day)`: 300–1000‰, constant over the day (later: per interval with smoothing). Shared by every wind array (one island weather).

## 4. Mechanics

### 4.1 Building and connecting

- A **station** is a facility (IND §3.5) built from its design's materials by NPC labour at a construction site; its owner must hold the design's licence or open access, and the builders and operators must know it (KNOWLEDGE doc). **Winning or buying a design never produces a station** (D6.12): materials, builders and time do.
- A **link** is built as a construction site between two ports (`BuildLink` by an NPC at either end; materials `wire 1 + steel 1` per node of length, consumed as `Embodied` on the link asset; `20 × LengthNodes` work ticks). A **pole** (a bare port) lets a line run across country. Links are C1 assets: owned (by a utility, a factory, a cooperative), and their wire can be stolen (taking `Embodied` wire is a dismantling act that releases `Salvaged` wire and breaks the link: a theft with real consequences).
- **Connecting a consumer** is building a link from the consumer facility's port to any port of a grid, by agreement with the link owner or the network operator (or without it: an illegal connection is possible and classified by C1 as theft of use). **Disconnecting** is opening a link at a switch (`Operate` act by a person at either end), or destroying it. A utility may disconnect a non-paying customer by sending a person to open the link: a physical act, recorded and contestable.

### 4.2 The interval balance (`ElectricitySystem`, order **262**, at every interval start)

For each component (ascending root port id):

1. **Demand.** Each consumer port declares `Requested = Σ` over its facility's running cycle or startable next cycle of `RecipeDef.PowerPu`, plus `StandbyPu` when connected and idle (IND §3.6). Declarations are computed from state at the interval start (no forecasting). Demand `D = Σ Requested`.
2. **Available supply per station.** `Avail = min(CapacityPu × availability / 1000 × staffing, fuelPu)`, where `staffing = min(1000, onDuty × 1000 / StaffFull)` and **0 below `StaffMin`**; `fuelPu = (fuelUnitsInBin × 1 000 000 − FuelDebtMicro) × 1000 / FuelMicroPerKeu / IntervalTicks` (the most it can sustain this interval from fuel physically present); 0 if `!On` or before `WarmFromTick`.
3. **Dispatch.** Stations in (`DispatchRank`, Id) order are loaded until `D` is met: each takes `min(Avail_i, remaining)`, but a thermal station that is loaded at all takes at least `MinLoadPermille × Capacity` if available (the excess is curtailed generation, recorded, not delivered). Total supply `S = Σ output_i`.
4. **Allocation.** If `S ≥ D`, every port gets `Granted = Requested`. If `S < D` (**shortage**): allocate by priority class descending (Critical, Contracted, Normal, Interruptible); within a class, pro-rata to `Requested` with integer largest-remainder, ties by lower port id. If `S = 0` and `D > 0`: **outage**.
5. **Grant to facilities.** Each facility's `Cycle.PowerPermille = Granted × 1000 / Requested` (1000 when it requested 0). The recipe runner (IND §4.4, order 264, every tick of the interval) uses it: below `MinPowerPermille` the cycle pauses (`NoPower`), above it progress scales.
6. **Events.** `GridIntervalSettled { Component, Demand, Supply, Unserved, Stations[], Ports[] }` (HV; Summary keeps shortages and one per component per hour), and spell events on change: `PowerShortageStarted/Ended { Component, UnservedPu }`, `OutageStarted/Ended { Component }`.

At the **interval end** (the next interval start, before step 1), settlement for the finished interval:
- **Delivered energy** per port = `Granted × ticksTheFacilityActuallyDrew` (a facility that stalled for other reasons draws only standby; integer ticks counted by the runner). Each meter adds its port's delivered eu.
- **Generated energy** per station = its share of the component's delivered total, pro rata to output (largest remainder by station id) plus curtailed minimum-load energy (generated, not delivered).
- **Fuel burned**: `FuelDebtMicro += generatedEu × FuelMicroPerKeu / 1000` (integer, remainder carried in the station's `EuRemainder`); while `FuelDebtMicro ≥ 1 000 000`, the on-duty staff member with the lowest NpcId consumes one fuel unit (`Goods.Consume(Bin(FuelBin), lot, 1, ConsumeReason.Combusted, actor)`, ascending lot id). Waste accumulators produce ash or spent fuel lots into `AshBin` the same way (`ProduceReason.Processed`), and pollution is added to the hex (WASTE §5). Fuel availability in step 2 already subtracted the debt, so the bin can always cover it.
- `StationFuelBurned { Station, Fuel, Units, GeneratedKeu }` per station per interval (Summary: per day).

### 4.3 Allocation priorities, rationing and policy

- **Priority classes** are set per port by the network operator's policy (an `OrgPolicy` key `PortPriority`), within what agreements promise (a `Contracted` agreement entitles the consumer to class 2), and later by law (C10: a government may require hospitals at Critical).
- **Rationing** beyond pro-rata (rolling blackouts: open links on a schedule) is an operator's decision carried out by staff opening and closing links physically.
- **Dispatch ranks** are the operator's policy (cheapest-fuel first, cleanest first, own stations first); default by technology as in 3.3. No rule optimises them.

### 4.4 Production consequences

| Situation | Consequence (mechanical, through the runner) |
|---|---|
| `PowerPermille` 500–999 | the cycle runs slower in proportion; the facility day row shows lost ticks |
| below `MinPowerPermille` (default 500) | the cycle pauses, status `NoPower`; workers present keep their `Work` action (they may leave by their own choice, and are paid or not by their terms) |
| outage | every powered facility in the component pauses; unpowered work (hand mining, hand-carry, recipes with `PowerPu = 0`) continues |
| fuel shortage | stations' `fuelPu` falls; shortages and outages follow; orders slip; buyers' supply agreements may be breached |
| link destroyed or opened | components split; the side without a station is out |

The NPCs perceive `SituationKind.PowerOut` (128) at a facility in outage and `FuelShort` (129) at a station whose fuel bin is below a day's burn: these are ordinary perceptions that change their choices (an operator may fetch coal; a manager may order it; a worker may go home).

### 4.5 Utilities as organisations

A **utility** is any organisation with the purpose `SupplyPower` (ORGANISATIONS §14): an enterprise (private or cooperative), a government service body (public), or a communal body. It owns stations and links (or some of them: a generating company and a separate network company are both possible), employs staff (C3), and:

| Activity | How (existing mechanisms only) |
|---|---|
| **Buys fuel** | purchase from a mining org; owned extraction (its own mine and harvesters); a supply agreement (IND §4.9); internal transfer within a conglomerate; communal allocation; government provision. Fuel arrives physically by hand, hauler or harvester into the station fuel bin (VEHICLES §4.4) |
| **Operates** | staff on duty switch stations on, feed upkeep wares at day start, read meters, maintain links |
| **Meters** | meters accumulate delivered eu (a physical instrument: no person is needed to count, but a person must **read** it to bill: `ReadMeter` act at the port, recording `ReadEu`) |
| **Bills** | at its billing interval (policy), the treasurer office issues a bill per meter reading under the electricity agreement: `(ReadEu − previous) × price term` creates one C2 `FormalObligation` (debtor: the consumer's owner; creditor: the utility; amount in the agreed ware or money unit per the MONEY doc). `ElectricityBilled` |
| **Is paid** | the consumer's person pays (hands over goods or pays by the MONEY doc's instruments); the obligation settles (`ElectricityPaid` trace linking the payment). Unpaid bills are arrears; the utility may disconnect (4.1) |
| **Agreed funding** | instead of per-unit billing, a funder (government budget line, cooperative members' contributions, a commune's store) hands over goods or money on an agreed schedule; the energy delivered is still metered and recorded, but no per-unit claim arises |
| **Invests** | builds new stations and links from its own stores, by obligations (loans, C2/C8), or by members' contributions; buys designs and licences through the KNOWLEDGE doc's auctions on the one money system |
| **Competes** | two utilities' grids may reach the same consumer; the consumer connects to the one it agrees with; overlapping networks are simply more links |
| **Fails** | without fuel, staff or payment it stops; insolvency by C6's rule; its assets remain and may be sold, seized or abandoned; nothing restarts it automatically |

**A utility's revenue is exactly what customers and funders actually hand over.** Its costs are what it actually hands over (fuel purchases, wages, materials). Profit, loss and capital accumulation are measured from these flows (analytics), never computed into existence.

### 4.6 The energy ledger (energy recorded without becoming money or stock)

```csharp
public sealed class EnergyLedger                    // IndustryState.Grid.Ledger; hashed; never read by NPC policies
{
    public long[] GeneratedEu;                      // by station (index parallel to Stations)
    public long[] CurtailedEu;                      // minimum-load energy not delivered, by station
    public long[] DeliveredEu;                      // by port
    public long[] UnservedEu;                       // requested minus granted, by port
    public long LossEu;                             // Grid.LossPermille × delivered (default 0)
}
```

- **Identity per interval and component:** `Σ generated = Σ delivered + Σ curtailed + loss`. Unserved energy is demand, not energy.
- It is a **physical record**, like a meter's dial: no owner, no price, no balance that can be spent, transferred or borrowed against. Money and claims enter only through the billing act (a C2 obligation) and the payment (MONEY doc).
- **Fuel to energy reconciliation:** per station, `Σ ConsumedBy(Combusted, fuel) × 1 000 000 × 1000 / FuelMicroPerKeu ≈ GeneratedEu` within one unit of fuel (the open accumulator).
- **Storage** (batteries, pumped water) is later: a storage asset with a charge in eu that is still not a good.

### 4.7 The god and electricity

Only through the existing divine rules: instruct or compel an operator to switch a station on or off, or to open a link (one valid physical act); possess a lineman; smite a station or a link (a nuclear station releases contamination by its `RiskOnDamage`, WASTE §5.3); create fuel by miracle into a station's fuel bin (ledger row). The god never sets dispatch, priorities or tariffs.

## 5. Interactions with other systems

| System | Interaction |
|---|---|
| IND | stations are facilities; recipe demand; `PowerPermille`; construction from designs |
| VEHICLES | fuel deliveries; wire theft by persons |
| WASTE | waste fuel, ash and spent fuel, emissions, contamination |
| INDUSTRIAL_MILITARY | military facilities draw from the same grids; their priority is by agreement or law, never by a military rule |
| C2 | bills are obligations; arrears |
| C3, C6, C10 | staff, utility organisations, regulation by law |
| KNOWLEDGE doc | generator designs, licences, auctions |
| MONEY doc | tariffs and payments |

## 6. Invariants

| Id | Invariant |
|---|---|
| ELE-I1 | Per interval and component: `Σ generated = Σ delivered + Σ curtailed + loss`; `Σ granted ≤ Σ available` |
| ELE-I2 | Energy only within a component: a port in a component with no station receives 0 |
| ELE-I3 | Fuel: per station, burned fuel equals generated energy × fuel rate within one unit; no generation without fuel lots consumed (except renewables) |
| ELE-I4 | Staffing: no generation in an interval whose start had fewer than `StaffMin` living staff on duty at the station node |
| ELE-I5 | **No money creation:** `Sim/Industry/Grid` calls no money API; every money or goods change related to electricity is a billing obligation (C2) or a payment/hand-over by a person (static test and event audit) |
| ELE-I6 | Meters: `DeliveredEu` of a meter equals the sum of its port's delivered eu; bills never exceed `DeliveredEu − ReadEu` at the previous bill times the price term |
| ELE-I7 | Determinism: components, dispatch and allocation depend only on state and ids; recording on or off gives one hash (P7) |

## 7. Telemetry and warehouse facts

**Events** (block 1050–1069, domain `Energy`, proposed): 1050 `GridChanged`, 1051 `PowerLinkBuilt` (with C1's `AssetBuilt`), 1052 `PowerLinkSwitched` (opened or closed, by whom), 1053 `PowerLinkDamaged`, 1054 `StationSwitched`, 1055 `GridIntervalSettled` (HV), 1056 `PowerShortageStarted`, 1057 `PowerShortageEnded`, 1058 `OutageStarted`, 1059 `OutageEnded`, 1060 `StationFuelBurned`, 1061 `MeterRead`, 1062 `ElectricityBilled`, 1063 `ElectricityPaid` (trace linking the payment event), 1064 `EnergyDay` (per component), 1065 `PortPriorityChanged`, 1066–1069 spare.

**Warehouse** (`0034_energy.sql`, I4):

| Table | Grain | Reconciliation |
|---|---|---|
| `dim_station`, `dim_power_link`, `dim_grid_port` | one row each | registrations |
| `fact_grid_component_spell` | (port, component root) spells | open and close at `GridChanged` |
| `fact_grid_interval` | (component, interval) demand, supply, unserved, curtailed | ELE-I1 per row (exact) |
| `fact_station_interval` | (station, interval) available, output, fuel burned, staff on duty | Σ fuel = `ConsumedBy(Combusted)` of station bins |
| `fact_power_delivery_day` | (port, day) requested, granted, delivered, unserved | Σ delivered = meter deltas |
| `fact_outage` | outage and shortage spells | = start/end pairs |
| `fact_meter_bill` | one row per bill (meter, eu, amount, obligation id, paid tick) | each bill ↔ one C2 obligation; paid amount = MONEY doc payment facts |
| `fact_energy_ledger_day` | (component, day) the ledger identity | exact |

**KPIs** (I6): ELE-01 unserved energy share; ELE-02 outage hours per consumer; ELE-03 fuel per delivered kEU by technology; ELE-04 generation mix; ELE-05 utility cash-flow from actual payments (in the MONEY doc's units or FE), never from delivered energy; ELE-06 share of demand from military facilities; ELE-07 emissions per kEU.

## 8. Player and view

An **electricity overlay** (toggle; normal play keeps it off): links drawn as cables on poles between ports along their node polylines, coloured by component; energised links glow faintly, unenergised ones are dark; stations show output bars and fuel level; consumers show a small power ring (full, partial, out). **Shortage indicators**: a flickering lamp glyph over facilities in `NoPower`, a fuel can over stations short of fuel. **Power station inspection**: owner organisation and model, design, staff on duty, on/off, output and availability, fuel bin by ware and days of fuel left at current burn, ash bin, dispatch rank, customers and meters (as the god observes; the utility's own records shown beside, D5.28). Details in [VISUAL_WORLD_DESIGN.md §18](VISUAL_WORLD_DESIGN.md#18-owner-d6-27-nov-2026-industrial-presentation) and [PLAYER_UI_DESIGN.md §25](PLAYER_UI_DESIGN.md#25-owner-d6-27-nov-2026-industrial-interface).

## 9. Migration from the existing code

Nothing like it exists. The only shared edits are the I1a items (state slot, ranges, events). `ElectricitySystem` is a new system at order 262 in `Sim/Industry/Grid`; with no ports it does nothing (pin P11).

## 10. Tier and package mapping

| Capability | Tier | Package | Minimal first version (D6.18) |
|---|---|---|---|
| Coal station design, station as facility, fuel bin, staff, on/off | 2 | **I4** | one coal station owned by a public utility org |
| Links, ports, components, interval balance, allocation, shortages, outages | 2 | **I4** | one component: station → factory (and the smelter); one link to cut for VS-9 |
| Energy ledger and reconciliation | 2 | **I4** | |
| Meters, billing obligations, payment link | 2 (meter and one bill), 3 (tariff policies, disconnection for arrears) | I4 with C2, MONEY doc | one meter at the factory; one bill as an obligation |
| Oil, hybrid, waste, wind, solar designs | 3 | I4 | |
| Nuclear station | 3 | I4 with K1, I1 (recipe 35), WASTE | |
| Rationing schedules, link capacity, losses, storage | 3 / later | I4 | |
| Electricity overlay, shortage indicators, station inspection | 2 | I6 | |

**Dependencies.** I4 needs **I1** (facilities, recipe loads, the runner's `PowerPermille`, fuel wares; hard), **C1** (assets), **C6** (utility organisations; soft: an NPC-owned station works), **C3** (staff; soft), **C2** (bills; only for billing), **MONEY doc** package (payments in money; soft: goods payment works without it), **K1** (designs; soft), **I2** (vehicle fuel delivery; soft: hand-carry works), **Q1** (views). After the Tier 1 set (D6.18).

## 11. Tests and acceptance hooks

**Unit and invariant tests:** `Grid_ComponentsDeterministic` (random link orders give the same partition and roots); `Grid_NoStationNoPower`; `Grid_AllocationByPriority` (critical served first, pro-rata within a class, largest remainder by id); `Grid_LedgerIdentity` (ELE-I1 over 30 days, exact); `Station_BurnsExactFuel` (ELE-I3); `Station_NeedsStaff` (ELE-I4); `Station_WarmUp`; `Renewables_NoFuel`; `Grid_NoMoneyApi` (static, ELE-I5); `Grid_SaveLoadMidInterval`.

**D6.18 vertical slice:**

| Step | Test | Expected |
|---|---|---|
| VS-5 a power station receives and consumes fuel | `VS5_StationBurnsDeliveredCoal` | coal unloaded into the station fuel bin (VEHICLES VS-5) is burned: `ConsumedBy(Combusted, Coal)` rises by the generated energy × rate; ash lots appear in the ash bin |
| VS-6 electricity reaches a connected factory | `VS6_PowerReachesFactory` | the factory's port is in the station's component; its `PowerPermille` is 1000 with enough supply; `fact_power_delivery_day` shows the delivered eu; a disconnected second factory gets 0 |
| VS-9 disrupting fuel or connectivity affects production | `VS9_FuelOrLinkCut` | (a) with the fuel bin emptied (harvester stopped), within one interval after the last fuel the station's output falls to 0, an outage starts and recipe 32 pauses `NoPower`; (b) with the link opened by a staff act, the factory is in a station-less component and pauses; restoring either resumes the same cycle with its progress |
| VS-10 (part) | `VS10_EnergyReconciles` | ELE-I1, I3, I6 and every `fact_*` of section 7 reconcile; replay equal |

**Brief §12 acceptance list (this document's share):** "Supplying electricity without creating money" — `ACC_ElectricityNoMoney`: 10 days of supply to 3 consumers; every money balance and the MONEY doc's money-supply measure are unchanged until a person pays a bill; the bills exist only as C2 obligations; "Fuel shortages affecting electricity generation" — `ACC_FuelShortageCutsGeneration`: halving fuel deliveries produces shortage spells whose unserved energy equals demand minus fuel-limited supply each interval (exact) and slows factory output; "Deterministic replay" — `Grid_ReplayExact`.

## 12. Open questions (with defaults)

1. **Who is the network operator of a mixed grid?** Default: the owner of most links (ties: lowest OrgId); a scenario or an agreement may name another.
2. **Should minimum-load curtailment cost fuel?** Default: yes (thermal stations cannot idle for free); recorded as curtailed energy.
3. **Interval length.** Default 100 ticks; shorter intervals cost CPU and change nothing structurally.
