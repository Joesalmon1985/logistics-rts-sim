# Waste and circular production design (owner D6, 27 Nov 2026)

Status: design, 27 Nov 2026. Owner decisions D6.2, D6.3, D6.13, D6.14, D6.15, D6.16 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)). House style: [DESIGN_CONVENTIONS.md §12](DESIGN_CONVENTIONS.md#12-how-every-design-doc-is-written). Shared surface proposals (ware block, ledger reasons, ids, event block, migration) are those of [INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md) ("IND") §9 and §12, provisional until the SAC Industry addendum. Package: the waste part of **I1** (content, recipes, pollution field), with collection vehicles in **I2**, waste-to-power in **I4**, organisations on C6.

---

## 1. Purpose and scope

**This document owns:**
- **waste, scrap and recyclable wares** and their properties;
- **where waste comes from**: consumption, production recipes and the destruction or dismantling of assets, all ledger-accounted;
- **collection, transport, storage, disposal and recycling**, and **waste-to-power** as a fuel for waste stations;
- **waste authorities, recycling enterprises and environmental regulators** as organisations on the generic framework;
- **the pollution field**: a simple deterministic per-hex measure of environmental damage from unmanaged waste and industry, with health and ecology effects; the separate slow **contamination** channel (nuclear accidents, the strategic device).

**Linked, not repeated:** recipe runner and ledger reasons (IND §3.2, §4.4); vehicles and routines (VEHICLES §4.4); waste stations (ELECTRICITY §3.2 design 4); illness (POPULATION, HOUSEHOLDS M4 `Disease`); wild sources and regrowth (ECONOMY §4.8, `ResourceSource`); regulation by law (C10); organisations (ORGANISATIONS §14).

## 2. Decisions and principles

| Id | Decision | Source |
|---|---|---|
| WST-1 | **Waste is matter.** Every waste unit is an ordinary `ItemLot` of a waste ware, produced with a ledger reason and removed only by a ledger reason. Nothing vanishes: dumping on the ground keeps it in the world (and polluting) | D6.13, IND-I1 |
| WST-2 | **Waste arises from three sources only:** consumption (`ProduceReason.Discarded`), production recipes (`Processed` outputs), destruction and dismantling (`Salvaged`, bounded by embodied matter) | D6.13 |
| WST-3 | **Landfill is storage, not disappearance.** A landfill is a waste site's bin; lots there remain counted. Only an explicit disposal recipe (incineration) removes waste, and it produces ash and emissions | WST-1 |
| WST-4 | **Circularity is production.** Recycling is a recipe (scrap → steel; recyclables → materials) run by workers with power; it is worthwhile only when someone values its output | D6.13 |
| WST-5 | **Environmental damage is a state field with consequences** (health, ecology), deterministic and simple: one integer per hex, deposits from emissions and unmanaged waste, decay and diffusion by fixed integer rules | D6.13 |
| WST-6 | **Waste services are organisations or individuals**, paid only by actual payments or agreed funding (D6.2); no automatic cleanup | D6.15 |
| WST-7 | **Off by default.** `Industry.Waste.Enabled = 0` keeps every non-industrial game unchanged (no refuse from meals); the industrial scenario turns it on | neutrality pins |

## 3. Data structures

### 3.1 Waste wares (IND block 56–62)

| No. | Ware | Weight | From | Hazard | Fate |
|---|---|---|---|---|---|
| 56 | `Refuse` | 1 | household consumption, packaging of industrial goods | low (pollution when on the ground) | collect, landfill, burn in a waste station |
| 57 | `Scrap` | 3 | recipe by-products, dismantling, wrecks, destroyed facilities | none | recycle (→ steel, iron), sell |
| 58 | `Recyclables` | 1 | sorting refuse (recipe) | none | recycle (→ copper, wire, parts later) |
| 59 | `Ash` | 2 | coal and waste stations, incineration, steel furnace | low | landfill; later cement filler |
| 60 | `Slag` | 4 | smelting | low | landfill; later road fill |
| 61 | `Sludge` | 3 | oil refining, oil stations | medium (pollution ×3 on the ground) | landfill (lined), incinerate |
| 62 | `SpentFuel` | 2 | nuclear stations | high (contamination when not in a secure store) | secure storage only (later reprocessing) |

`Wares.IsWaste(w)` covers 56–62; `WareRefValue` (analytics only) is 0 or low for waste.

### 3.2 Waste sites and the pollution field

```csharp
public sealed class WasteSite                        // a Facility of kind WasteSite (IND §3.5), plus:
{
    public int Asset; public byte Kind;              // 1 Dump (unlined), 2 Landfill (lined), 3 SecureStore (spent fuel)
    public int Bin;                                  // the facility's InputBin; WeightCap = capacity
    public short LeakPermille;                       // Dump 20, Landfill 2, SecureStore 0: share of the site's hazard load that reaches the hex per day
}

public sealed class EnvironmentState                 // IndustryState.Waste.Environment
{
    public int[] Pollution;                          // per hex, micro-units (0..10 000 000), the general field
    public int[] Contamination;                      // per hex, micro-units: the slow nuclear channel
    public int[] EmittedToday;                       // per hex, reset at day start (for the day row)
}
```

## 4. Mechanics

### 4.1 Generation

- **Consumption** (`Industry.Waste.Enabled = 1`): each `Consumed` with reason `Eaten` of a bought or processed food (bread and later packaged goods; not foraged berries) adds `RefuseMilliPerMeal` (default 250) to the eater's household accumulator; whole units are produced into the household store (or the person's carry when it has none) with `ProduceReason.Discarded`, actor = the eater. Foraged food produces no refuse (nature's leftovers are not modelled). Wear of a tool (`ConsumeReason.Wear`) of a metal ware produces 1 `Scrap` per worn-out tool when waste is enabled.
- **Production**: waste outputs are recipe outputs (IND §3.6 italics), produced into the facility's `WasteBin` (or `OutputBin`) with `Processed`.
- **Destruction and dismantling**: an asset destroyed (smite, combat, strategic strike) or dismantled releases salvage: `Salvaged` scrap = `⌊Embodied[steel] × SalvagePermille / 1000⌋` (default 500 for destruction, 800 for dismantling) plus parts for dismantling; produced at the asset's node (ground when destroyed: ownerless by the ground rule; into the dismantler's carry or bin when dismantled). IND-I3 bounds it. A destroyed facility's mid-cycle inputs follow the abort rule (IND §4.4 step 5).

### 4.2 Collection, transport and storage

- **By hand**: anyone may pick up waste lots (ground lots are ownerless) and carry them to a site (`Deposit` into the site bin within `Reach.Store`).
- **By vehicle**: the `Collector` vehicle (VEHICLES §3.1 def 4) runs a `Collect` routine: drive to listed collection points (household stores whose owners agreed, public bins, ground lots in a hex), load waste lots, unload at a waste site. The operator is a person as in VEHICLES §4.4 and can refuse or abandon the round.
- **Ownership**: waste in a household store is the household's; handing it to a collector is a gift or a paid service (the household pays a fee by agreement); ground waste is ownerless. Taking scrap from someone's yard is theft (C1).
- **Storage**: waste sites hold lots up to capacity; a full site refuses (`NoRoom`), and the collector's choices follow.

### 4.3 Disposal, recycling, dismantling (recipes on the IND runner)

| Id | Recipe | Facility | Inputs | Outputs | Ticks | Workers | Power pu |
|---|---|---|---|---|---|---|---|
| 40 | Sort refuse | RecyclingPlant | refuse 4 | recyclables 2, residual refuse 2 | 240 | 1/2 | 10 |
| 41 | Scrap to steel | RecyclingPlant (electric furnace) | scrap 3 | steel 2, *slag 1* | 300 | 1/2 | 60 |
| 42 | Recyclables to copper | RecyclingPlant | recyclables 4 | copper 1, *refuse 1* | 300 | 1/2 | 30 |
| 43 | Incinerate | WasteSite with burner | refuse 4 (or sludge 2) | *ash 1*; emission high | 120 | 1/1 | 0 |
| 44 | Dismantle vehicle | Depot or RecyclingPlant | (the asset) | scrap ≤ embodied × 800‰, parts ≤ embodied parts × 500‰, engine 0–1 | 900 | 2/3 | 20 |

**Incineration** is the only path that consumes waste out of the world (`ConsumeReason.Disposed` for the inputs via `ProcessInput`; IND counts it as process input), and it produces ash and pollution. **Waste-to-power**: the waste station (ELECTRICITY design 4) burns refuse as fuel (`Combusted`) and produces ash; it is a utility like any other.

### 4.4 Organisations

On the generic framework (ORGANISATIONS §3, §14): **waste authorities** (public or communal service bodies with the purpose `ManageWaste`: run collection rounds and sites, funded by fees or by a government budget line), **recycling enterprises** (private or cooperative enterprises with the purpose `Recycle`: buy or collect scrap and recyclables, run recycling recipes, sell the outputs), **environmental regulators** (service bodies with the purpose `Regulate`: inspectors visit facilities and waste sites, write records of emissions and dumping, and recommend; they have power only where a government organisation adopted a law, C10, enforced by its own people). No organisation cleans up by itself: a regulator that reports and is ignored changes nothing physical.

### 4.5 Determinism

All per-hex updates at day start in ascending hex id; collection rounds follow the operator's choices (VEHICLES); no random draws in the field.

## 5. The pollution field (simple, deterministic)

### 5.1 Update (daily, `IndustrySystem` day start, after facility day rows)

For each hex `h`, in ascending id, on a copy of yesterday's field:
1. **Emissions**: `+ EmittedToday[h]` (recipe `EmissionMicro`, station `EmissionMicroPerKeu × kEU`, incineration).
2. **Unmanaged waste**: `+ Σ` over ground waste lots in `h` of `count × HazardMicro(ware)` (refuse 200, sludge 600, scrap 0, ash 100, slag 50), plus waste sites' `LeakPermille` share of their contents' hazard.
3. **Decay**: `− Pollution[h] × DecayPermille / 1000` (default 50‰ a day: about a fortnight's half-life).
4. **Diffusion**: each hex sends `DiffusePermille` (default 60‰) of its value equally to its land neighbours (integer division; remainders stay), computed from the copy so order does not matter.
5. Clamp to `[0, 10 000 000]`; emit `PollutionDay { Hex, Value, Emitted }` for hexes with a non-zero value (one row per such hex per day) and `PollutionThresholdCrossed` when crossing a band (clean < 100 000 ≤ noticeable < 1 000 000 ≤ harmful < 4 000 000 ≤ severe).

### 5.2 Effects (read by the owning systems through one query `Environment.Band(hex)`)

| Effect | Rule (default) | Owning system (hook) |
|---|---|---|
| Health | illness incidence × (1000 + 150 per band above clean) / 1000 for people whose node is in the hex at the daily draw | M4 `Disease` through the Industry hook (dispatched after Divinity) |
| Ecology | wild source regrowth × (1000 − 200 per band) / 1000; forest stock regrowth likewise | ECONOMY §4.8 regrowth through the hook |
| Perception | `SituationKind.Polluted` (130) for people in a harmful hex: a cue they may learn to avoid (they may also stay for work) | cognition |
| Presentation | haze and discoloured ground drawn from the band only (VISUAL_WORLD §18) | view |

### 5.3 Contamination (nuclear channel)

Set by a nuclear station destroyed or smitten (`RiskOnDamage`), spent fuel outside a secure store (`HazardMicro` 5000 per unit per day), and the strategic device (INDUSTRIAL_MILITARY §4.7). Same update with **decay 1‰ a day** and diffusion 10‰. Effects are stronger: illness ×(1000 + 600 per band) / 1000; extraction and foraging in a severe hex add injury risk; persons perceive it only if they have the knowledge to recognise it (a K1 knowledge item) or see its effects (sick neighbours, dead plants).

## 6. Interactions with other systems

IND (recipes, ledger reasons, salvage), VEHICLES (collectors, wrecks, dismantling), ELECTRICITY (waste station, ash, emissions), INDUSTRIAL_MILITARY (contamination, wrecks), POPULATION/M4 (illness), ECONOMY (regrowth, commons), C1 (ownership of waste and scrap, theft), C6/C10 (authorities, regulators, laws), KNOWLEDGE doc (recognising contamination, recycling designs), MONEY doc (fees).

## 7. Invariants, telemetry and warehouse facts

| Id | Invariant |
|---|---|
| WST-I1 | Waste wares obey IND-I1: per waste ware `Σ produced (Discarded + Processed + Salvaged + Miracle) − Σ consumed (ProcessInput + Combusted + Disposed + Miracle) = Σ holdings (ground, bins, carry)` |
| WST-I2 | Salvage ≤ embodied (IND-I3) |
| WST-I3 | Pollution: `P_t+1 = P_t + emitted + waste load − decay ± diffusion` per hex exactly (integer), recomputable from events and state |
| WST-I4 | No waste path calls a money API; fees are payments by persons (MONEY doc) |
| WST-I5 | With `Industry.Waste.Enabled = 0`, no `Discarded` production and no pollution update (neutral) |

**Events** (block 1080–1094, domains `Waste` and `Environment`, proposed): 1080 `WasteGeneratedDay` (per source kind and owner per day; individual `Produced` events carry the units), 1081 `WasteCollected` (per load), 1082 `WasteDeposited` (into a site), 1083 `WasteIncinerated`, 1084 `SalvageReleased`, 1085 `PollutionDay`, 1086 `PollutionThresholdCrossed`, 1087 `ContaminationDay`, 1088 `EnvironmentalHarmApplied` (aggregate per hex per day: extra illness draws, regrowth reduced), 1089 `InspectionRecorded` (regulator record), 1090–1094 spare.

**Warehouse** (`0036_waste_environment.sql`, I1 waste part): `fact_waste_flow_day` (ware, source kind, owner, generated, collected, deposited, incinerated, recycled), `fact_waste_site_day` (site, stock by ware, capacity), `fact_salvage` (asset, ware, count, embodied), `fact_pollution_day` and `fact_contamination_day` (hex, day, value, emitted, waste load), `fact_environment_harm_day`, `fact_inspection`. Reconciliation: flows equal the ledger by reason; site stocks equal bin contents; the field recomputes exactly from the day rows (WST-I3).

**KPIs** (I6): WST-01 waste generated per person-day; WST-02 share of waste collected, landfilled, recycled, incinerated, left on the ground; WST-03 recycled share of steel supply; ENV-01 population-weighted pollution band; ENV-02 illness incidence by band (against matched clean-hex controls); ENV-03 contaminated area and persons living in it.

## 8. Player and view

Litter heaps on the ground where lots lie; dumps and landfills as visible mounds growing with their stock; recycling plants with sorting animations while cycles run; collector vehicles with visible loads; a **pollution overlay** (off by default; tinted haze by band) and, in normal play, subtle haze and discoloured vegetation from the band only. Inspection: waste site card (owner, stock by ware, capacity, leak class), regulator records beside divine observation.

## 9. Tier and package mapping

| Capability | Tier | Package | Minimal first version |
|---|---|---|---|
| Waste wares, recipe waste outputs (slag, ash, scrap), ledger reasons | 2 (with the slice: waste outputs exist from the first recipes) | **I1** | slag and ash from recipe 25 and the coal station, stored in waste bins |
| Collection by hand, waste sites, landfill, incineration | 2 (after the slice) | I1, I2 | |
| Recycling recipes 40–42, dismantling 44, salvage | 2–3 | I1, I2 | scrap → steel at one plant (brief §12: "waste generated, collected and reused") |
| Consumption refuse | 3 | I1 | |
| Pollution field and its health and ecology hooks | 2 (field and day rows), 3 (effects) | I1 | |
| Contamination channel | 3 | I1 with I4, I5 | |
| Waste authorities, recycling enterprises, regulators (templates) | 3 | C6 templates with I1 | |
| Pollution overlay, waste site card | 2–3 | I6 | |

**Dependencies.** I1 (recipes, runner, ledger widening; hard), I2 (collectors; soft), I4 (waste station; soft), C1 (ownership), C6 (organisations), C10 (laws for regulators), M4 `Disease` hook (effects), Q1.

## 10. Tests and acceptance hooks

- `Waste_LedgerExact` (WST-I1 over 30 days of the industrial scenario with waste on).
- `Waste_GroundWastePollutes`: 20 refuse lots dumped in one hex raise its pollution by exactly the rule; removing them stops the load; decay and diffusion match the integer formula (WST-I3).
- `Waste_PollutionRaisesIllness` (behavioural, 20 seeds): illness incidence in harmful hexes is higher than in matched clean hexes; reported with effect size.
- `Waste_NeutralWhenOff` (WST-I5, part of pin P11).
- **Brief §12: "Waste being generated, collected and reused"** — `ACC_WasteGeneratedCollectedReused`: the steel furnace and machine shop produce slag and scrap; a collector (or a person) carries the scrap to the recycling plant; recipe 41 turns it into steel that the factory consumes in recipe 29; every step has its ledger reason; IND-I1 holds; no money balance changes except actual payments for the scrap.

## 11. Open questions (with defaults)

1. **Refuse from foraged food.** Default: none (keeps the M3/M4 band economy unchanged).
2. **Should pollution affect fertility?** Default: no direct rule; only through illness and nutrition (measured).
3. **Water pollution as a separate channel** (rivers, fish). Default: later, when water bodies exist in the sim.
