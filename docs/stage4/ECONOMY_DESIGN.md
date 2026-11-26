# Economy design (stage 4)

This document covers goods, production, property, exchange, labour, enterprises, the commons and public goods: the material life of the island once persistent NPCs do all the work. It follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md), which wins where the two disagree.

## 1. Purpose and scope

**This document owns:**
- items and goods: item lots, owners, **item quality** (a true and a visible quality, with cheap fakes, M8), freshness, personal carry and stores;
- **`WareRefValue` and the food-equivalent unit (`FE`)**: the fixed reference value per ware that every other document uses to measure wealth, income and surplus (section 3.3). It is a measurement index only;
- production: the existing raw → processed → gear chains, foraging, farms, tools, and the *effects* of skills on work;
- the development path from subsistence to surplus, and how each step is measured;
- property: forms, records, use rights, first-arrival claims, theft and witnesses;
- exchange: barter as separate hand-overs, offers by gesture, subjective valuation, price beliefs, gifts, help, loans, markets;
- labour: job postings, wage labour, and the removal of the stage 3 walkers (M6);
- enterprises (M8), the commons (Ostrom), public goods, surplus measurement, faction trade, and the seam for money.

**Linked, not repeated here:**

| Topic | Owner |
|---|---|
| The NPC record, drives, the skill record itself, households and huts | [POPULATION_DESIGN.md](POPULATION_DESIGN.md) |
| Perception, beliefs, the decider, the gesture grammar | [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) |
| Obligations, promises, trust, reputation, gossip | [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) |
| Learning from rewards and from observation | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| Cultural traits and norms, how they spread | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| Estates and transfers on death | [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) |
| Taxes, wages policy, state purchases, sites and contracts, laws v1 | [STATE_DESIGN.md](STATE_DESIGN.md) |
| Institutions (shared store, mediation, informal lending, markets as institutions) | [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md) |
| Class as a measured relation; inequality | [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md) |
| Interests, bargaining, movements, competing ways of organising | [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) |
| KPI definitions; warehouse facts | [KPI_CATALOGUE.md](KPI_CATALOGUE.md), [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md) |

## 2. Principles
- **Goods are physical.** Every unit of every ware is in exactly one place: an NPC's carry, a store, a pile on the land, or a building site. Nothing teleports. Goods appear only by production (including nature: dice piles and regrowth) and leave only by consumption (eating, recipe inputs, building, wear, spoiling, destruction). This is tested as a conservation law (section 10).
- **Exchanges between NPCs are never atomic.** Each hand-over is a separate action by one NPC. The other side can always fail to reciprocate. Only an institution (a state paymaster, later a market or a court) adds enforcement, and it does so through its own staff and rules.
- **Value is subjective and integer.** Each NPC values goods from its own drives, plans and beliefs. No NPC reads a global price table. `WareRefValue` (section 3.3) is a *measurement index* for telemetry and analysis, never an input to any NPC policy or sim rule.
- **Specialisation, trade and surplus are made possible and measured, never scripted.** The mechanics (skill growth, switching costs, tools, diminishing value of repeated goods, carry limits) give division of labour a real payoff. Whether it happens is measured against controls.
- **Forms of ownership compete on equal rules.** Production, jobs and exchange never branch on whether an owner is the state, one NPC, a group or a cooperative. Only the decision rule (who decides) differs.
- **Property is a record plus a convention.** The sim records who holds a thing and who claims it. Whether others respect a claim is their decision, shaped by personality, memory, norms and (later) institutions.
- **Determinism:** integers only, ascending-id order, every draw from a named forked stream (section 4.12), bounded tables.

## 3. Data structures (`Sim/Economy`)

### 3.1 Owners, lots and stores

```csharp
enum OwnerKind : byte { None, Npc, Household, Group, Enterprise, State }
struct Owner { OwnerKind Kind; int Id; }       // State: Id = FactionId. Group: a communal group id.

struct ItemLot                                  // a homogeneous quantity with one owner
{
    int    Id;          // ItemLotId, never reused
    Ware   Ware;
    short  Count;       // 1..999
    Owner  Owner;       // explicit from M6; before that the holder is the owner
    byte   TrueQuality;    // 0..3; 2 until item quality arrives (M8), see 4.1
    byte   VisibleQuality; // 0..3; what an observer sees without inspecting; equals TrueQuality unless faked
    Ware   VisibleWare;    // equals Ware unless faked (painted pottery shown as bronze)
    short  SpoilDay;    // SimCalendar day it rots; -1 = never
}

sealed class Store                               // a hut store, building input/output, HQ stock, a granary
{
    int Id; int BuildingId; int WeightCap;
    List<ItemLot> Lots;  // sorted by (Ware, Owner, SpoilDay); at most 64 lots
    int[] Total;         // cached per-ware totals (length Wares.Count) for fast reads
}
```

- **Merging:** lots with an equal key (ware, visible ware, owner, both qualities, spoil day) merge in a store; the merged lot keeps the lower id. A lot keeps its id while carried, so a stolen lot can be followed in telemetry.
- **Single-owner fast path:** a store whose lots all have one owner (every state building in M6) keeps only `Total`. Stage 3's `In`, `Out`, `Stock` and their `Reserved` arrays become this fast path, so the existing matching code keeps working on state stores.
- **Personal carry:** `Npc.Carry` is `ItemLot[4]` with a weight limit. `Wares.Weight` per ware: log, clay, stone, ore, plank, brick, mortar 4; grain, iron, bronze, steel, pottery 2; food, cloth, leather 1; gear and tools 2. Carry capacity is 4 to 6 by strength, so one log per trip, as a stage 3 carrier. A basket adds 4 for food only. Children carry 2.

### 3.2 New wares (appended; stage 3 numbers unchanged)

| Ware | No. | Kind | Made from | Spoils after (days) |
|---|---|---|---|---|
| Berries | 28 | food, foraged | forest and hill sources | 2 |
| Roots | 29 | food, foraged | forest and hill sources | 6 |
| Fish | 30 | food, foraged | coast sources | 1 (3 if smoked, later) |
| Basket | 31 | hand tool | 1 log, 40 work ticks by hand | wears out after 300 uses |
| Pick | 32 | tool | steel + plank (toolsmith) | 200 uses |
| Sickle | 33 | tool | bronze + plank (toolsmith) | 200 uses |
| Net | 34 | tool | cloth + plank (toolsmith) | 200 uses |

Grain keeps 20 days and bread 4. Grain can be eaten raw for 40% of bread's nutrition, so baking is a real gain. The **axe** (existing gear, iron + plank) doubles as a felling tool. The **toolsmith** is one new workshop that makes one of pick, sickle or net per cycle, as ordered by whoever runs it. `Wares.Count` grows to 35; save version 3 (M3) covers it.

### 3.3 Reference value per ware (`WareRefValue`) and the `FE` unit

```csharp
// SimTuning.Economy.WareRefValue: int[Wares.Count], fixed for a run, saved with SimTuning
// 1 FE = 1 unit of reference food value; WareRefValue[w] is the FE value of one unit of ware w
```

- **What it is.** One fixed integer per ware, in **food-equivalents (`FE`)**: berries, roots and fish are valued by nutrition relative to a ration, and other wares start from the stage 3 `TradeTuning.ValueOf` numbers rescaled to FE and extended to the new wares. It is the Band-age stand-in for money in reports (barter only). Currency replaces it in reports from the Town age.
- **What it is for.** Measuring: wealth, income, surplus, estate sizes, ledger totals in the warehouse, the Gini and the other KPIs ([KPI_CATALOGUE.md](KPI_CATALOGUE.md), [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md), [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)). The warehouse holds it as `dim_ware.ref_value`.
- **What it is not.** **NPCs never read it**, and no sim rule reads it: each NPC values goods by its own worth units (4.5), and estates are split by units and by the executor's own valuation, not by FE. A reflection test (B6) keeps it out of every policy and decision type.
- **No value theory is built in.** Wealth is also stored per ware in the warehouse (holdings by ware), so any analysis can use another weighting, such as observed exchange ratios. Reports that use FE show the observed valuation beside it where it matters ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md#32-valuation-food-equivalents-fe)).
- **The faction-level trade judge** of stage 3 keeps its own `TradeTuning.ValueOf` table: that is a faction policy's tuning, not this index, and the two may diverge.

### 3.4 Production and resources

```csharp
sealed class ResourceSource            // a wild food source or another renewable stock (a commons candidate)
{
    int Id; ResourceKind Kind;        // Berries, Roots, Fish, Forest (M6: stock behind dice log piles); Pasture later
    int Hex, Node;
    int Stock, Capacity;              // units
    int RegrowPermille;               // logistic growth rate per day
    int CommonsId;                    // 0 = open access, unmanaged
}

struct ToolEffect { Ware Tool; WorkKind Work; int SpeedPermille; int YieldPermille; }  // table, see 4.2
```

- `EcoBuilding` gains `Owner`, `StaffSlots` (0 to 3), `Staff[]` (NpcIds), `StoreId`, and `EnterpriseId`.
- `BuildingDef` keeps `In`, `Out`, `CycleTicks` and `Cost`. Recipes do not change.

### 3.5 Property records

```csharp
enum ClaimBasis : byte { FirstArrival, Built, Exchanged, Gift, Inherited, StateGrant, Seized, Founded }
enum UseKind    : byte { Sleep, Work, Gather, Store, Pass }

sealed class PropertyRecord            // one asset: a building, a store, a resource source, a plot
{
    AssetRef Asset; Owner Owner; ClaimBasis Basis; int SinceTick;
    List<UseRight> Rights;            // at most 8
    List<Owner> Disputes;             // rival claimants seen acting on it, at most 4
}
struct UseRight { Owner Holder; UseKind Kind; Owner GrantedBy; int UntilDay; int TermsId; }
```

The registry (`EconomyState.Property`, ascending asset id) is **true state** (layer 1): who claims what, on what basis. NPCs know claims only through what they saw, were told or read at a board (layers 2, 3, 4). The state's own register is an institutional record (layer 4) and can disagree with the truth.

### 3.6 Exchange, jobs and enterprises

```csharp
sealed class DealTrace                  // telemetry only: links hand-overs. No policy may read it.
{
    int DealId; int A, B; int OpenedTick;
    WareCount[] AOffered, BOffered;     // as the speaker meant them, not as decoded
    int[] AGiven, BGiven;               // running totals
    DealKind Kind;                      // Spot, Gift, Loan, Help
    int DueDay;                         // loans
    DealOutcome Outcome;                // Open, Completed, Defected, Partial, Abandoned, Repaid, Defaulted
}

sealed class JobPosting
{
    int Id;                              // a ContractId
    Owner Poster; JobKind Kind;          // Haul, Build, Gather, Staff, Envoy, Forage (for a household)
    int BoardNode;                       // an HQ board or a flag board
    JobTask Task;                        // ware + source + destination, or site, or building
    Ware PayWare; short PayCount; PayPer Per;   // Per: Load, Shift, Unit, Task
    byte Slots, Taken; short PayPriority;      // Priority mapped to a premium, see 9.2
    int PayStoreId; int ExpiresTick;
}

sealed class Enterprise
{
    int Id; EnterpriseForm Form;         // Private, Cooperative, Communal, State
    List<(Owner owner, short permille)> Shares;   // at most 16; sums to 1000
    DecisionRule Rule;                   // Owner, MajorityOfMembers, Consensus, Institution
    List<int> Buildings; int StoreId; List<int> Postings;
    Ledger Books;                        // true income and outgoings, by day (telemetry and its owners)
}
```

Bounds: postings per faction ≤ 256, per board ≤ 32; enterprises ≤ 1 per 4 NPCs; deal traces kept open ≤ 30 days.

## 4. Mechanics

### 4.1 Items, freshness, quality and consumption
- Eating takes a food lot from carry or from a store the NPC may use, and lowers hunger by the ware's nutrition (berries 150, roots 200, fish 300, raw grain 120, bread 300, all in drive points).
- **Variety:** the *n*-th unit of the same food eaten in one day relieves `nutrition × 1000 / (1000 + 250 × (n − 1))`. This is the reason a fisher values berries and a berry picker values fish. The rule is kept (owner decision Q10).
- **Natural reasons to exchange (Q10).** Trade must emerge because it benefits individuals, never because the game demands it. The reasons the mechanics provide: different nutritional benefits (variety, above); local availability (sources differ by terrain, [4.8](#48-the-commons-ostrom)); **complementary construction materials** (a scenario option makes a hut need logs and clay, `HutRecipe = LogsAndClay`, tested as C4); productive capabilities (buildings and tools, 4.2); individual needs (children, illness, pregnancy); and skills and equipment (skill growth, switching cost, tools).
- **Spoiling:** at each day start, lots past `SpoilDay` are removed with a `Consumed(cause: Spoiled)` event. Granaries (state or communal) add 50% to grain and roots' keeping time.
- **Construction** consumes cost wares into the building (`Consumed(cause: Built)`); the building's record notes the wares as capital for surplus accounts.
- **Item quality (M8).** Before M8 every lot has `TrueQuality = VisibleQuality = 2`, which gives the stage 3 stats. From M8:
  - a lot's true quality is set when it is made, from the maker's skill and tool (`q = clamp(skill / 80, 0, 3)`, at least 1 with a tool);
  - true quality scales a tool's uses and effect, a gear item's stats and a food's nutrition by `600 + 200 × q` permille (600, 800, 1000, 1200);
  - **cheap fakes:** a workshop's operator may run a recipe as a fake: half the inputs, and the output lot gets a higher `VisibleQuality`, or a `VisibleWare` of a dearer ware (painted pottery shown as bronze). Making one is an ordinary choice by the operator's decider; no rule makes or forbids it;
  - **inspection:** observers see only the visible fields. An `Inspect` intent (20 ticks, adjacent) reveals the true fields with probability `400 + 2 × skill` permille for the relevant work, drawn from the inspector's own stream. Using an item (wear, eating) reveals them to its holder. A revealed fake is a caught lie for social memory ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#42-updating-a-relation-the-evidence-table)).
  - How fakes, signals and inspection appear as game-theory cases (cheap fakes, mimics, handicap displays) is in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#43-deception-in-signals-chapter-4-end-pdf-87-and-88).

### 4.2 Production
- **Dice piles stay.** The hidden Catan dice keep adding piles to hexes (`DiceSystem`). A pile is nature's yield; it is ownerless until claimed or taken.
- **Gathering** is work by an NPC:
  - By hand from a pile: 60 ticks, takes what it can carry (one log). A pile holds `WaresPerPile` units and lasts several trips.
  - Through a harvester building: as in stage 3 (4, 5 or 6 units per pile by level, faster gathering), because the building's cart carries the whole pile. **The building is capital:** with tools, skill and roads it makes a worker's output much higher than hand gathering, **about 4× as an initial tuning assumption, not a fixed rule** (owner decision Q9; the factor is measured per run and tuned only for playability). That gives owning one a real meaning.
  - **Anyone may gather a pile by hand (Q9).** Hand gathering supports independent subsistence, specialisation and mobility: an NPC without access to a building can still make a living from piles, at lower output per hour. Dice piles are therefore a commons too ([4.8](#48-the-commons-ostrom)).
  - **Forest stock (owner decision Q11).** Each forest hex holds a `ResourceSource` of kind `Forest` (`Stock`, `Capacity`, `RegrowPermille`). When the dice add a log pile on a forest hex, the pile's units are taken from that stock; a hex whose stock cannot cover a pile gets a smaller pile or none. The stock regrows by the configurable ecological rule of 4.8 (`SimTuning.Ecology.Forest`: growth rate, capacity, the minimum stock for regrowth). **Forest stock limits are on by default in the sandbox and off by default in the VP mode** (non-depleting piles as in stage 3), with the scenario key `forestDepletion` to turn them on in VP or off in the sandbox. Overexploitation, sustainable harvesting and, later, resource-management institutions are therefore all possible.
- **Foraging** from a `ResourceSource`: 30 ticks per unit, yield 1 or 2 drawn from the gatherer's own stream; takes 1 from `Stock`.
- **Workshops** run a recipe cycle only while a staff NPC is present in a staff slot (from M6), **in both the sandbox and the VP mode** (owner decision Q8). All productive work is ultimately done by persistent individuals: no anonymous or auto-generated worker is kept to preserve stage 3 balance. Production, staffing and resources are rebalanced around the population system (M14). Cycle ticks are divided by the staff's work speed.
- **Work speed** in permille: `1000 + 3 × skill(work)` (skill 0 to 255, so up to 1765), times the tool effect, times fatigue (`1000 − fatigue / 2`).
- **Tools:**

| Tool | Work | Effect |
|---|---|---|
| Axe | gather log | speed +60% |
| Pick | gather stone, ore | speed +60% |
| Sickle | gather grain (farm) | speed +60% |
| Net | forage fish | yield ×2 |
| Basket | forage berries, roots | +4 food carry |

  A tool must be carried and loses one use per work unit; at zero it is `Consumed(cause: Wear)`.
- **Skill growth** (the record is in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#skills)): each finished work unit adds experience; `skill += (255 − skill) × 8 / 1000` per 10 units, at least 1 per 10 units. Skills unused for 3 days lose 1 per day down to 20. A genetic aptitude (later) scales the gain.
- **Switching cost:** changing activity costs `SwitchTicks` (default 60) of set-up, on top of walking. Together with skill growth and tools this is the Smith mechanism: concentrating on one activity raises output per hour. Nothing else rewards it.
- **Farms:** the farm building is a harvester on grain land. Dice piles appear there as now. From M8 a farm can also be worked as a plot: staff work raises a hidden "tended" counter that adds 1 grain per pile gathered when above 600. Optional, off in VP mode.

### 4.3 The development path (measured, not staged)
The path is a description of what the mechanics make possible. No rule moves a village from one phase to the next, and a village may stop, skip, mix or reverse phases. The owner's progression (27 Nov) is **subsistence → cooperation → barter → specialisation → surplus → productive ownership → employment → accumulation → classes → political organisation**. Each phase has a measure, computed daily per faction and per village cluster (NPCs sharing huts within 3 hexes):

| Phase | What makes it possible | Measure |
|---|---|---|
| Subsistence | foraging, hand gathering, own huts | share of food eaten that the eater gathered itself (`SelfProvisionPermille`) |
| Cooperation | gifts, help, household sharing (vampire bats: lucky days and unlucky days, [notes E10](research/SELFISH_GENE_NOTES.md#e10-vampire-bats-pdf-253256-bk-230233)) | gifts and help acts per NPC per day; share of food eaten that came from others |
| Barter | offers, hand-overs, price beliefs, markets | completed exchanges per NPC per day; price dispersion (spread of observed ratios for the same pair) |
| Specialisation | skill growth, switching cost, tools, variety | `SpecialisationIndex`: mean over adults of the share of work ticks in their top activity, 7-day window; Gini of skill profiles |
| Surplus | higher productivity, storage, granaries | surplus per adult per day (section 4.10) |
| Productive ownership | property records, buildings and tools as capital, enterprises (4.7) | share of productive assets by owner form; share of adults owning a productive asset |
| Employment | job postings, wage relation (`Work` use right) | share of work ticks done for another owner; `WageDependencePermille` (4.8) |
| Accumulation | estates, retained surplus, inheritance ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)) | wealth Gini; top-10% share; parent-child wealth rank correlation |
| Classes | measured relations to property, labour and surplus ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)) | class-position shares (analysis only; no NPC has a class label, and no political belief is assigned from class) |
| Political organisation | interests, grievances, associations ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)) | association membership; collective acts (stage 5) |

`WareRefValue` and FE (3.3) are used by these measures for analytics and reporting only. They are never a hidden currency and never a central barter price: no NPC, policy or price reads them.

The gains from trade are measured directly: output per work hour of the same seed with exchange disabled (`GameSetup.ExchangeOff`, a control scenario only) vs enabled.

### Property
- **Forms:** state (a faction), private (one NPC), communal (a group: a household, a hut's builders, a commons' members), cooperative (an enterprise owned by its workers), and later others (chartered companies, temples, leases). One `Owner` type covers all.
- **How a record starts:**
  - **Built:** whoever contributed labour or materials to a building gets a share; a hut's builders get sleeping rights (communal), as in [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#forming-and-breaking-households). A state site paid from the treasury is state property; the workers are paid, not owners.
  - **First arrival:** an NPC that starts using an ownerless asset (an abandoned building, an unclaimed forage patch, a hut plot) when no other claim exists gets a `FirstArrival` claim. Others see the use and form a belief "X holds Y". Whether they respect it is the resident/intruder convention of [notes A4](research/SELFISH_GENE_NOTES.md#a4-asymmetric-contests-and-ownership-resident-vs-intruder-pdf-100103-bk-7780-endnote-pdf-308) (PDF 100–103): the decider has an "I am owner here" feature and an "they hold this" feature, and learning decides how much they weigh.
  - **Exchange, gift, inheritance, state grant, founding:** recorded by the act that caused them.
  - **Seized:** taking an asset by force or occupation. The record changes, the old owner joins `Disputes`, and the loser remembers it.
- **Use rights vs ownership:** a use right lets the holder use an asset without owning it: sleep in a bed, work at a bench, gather on a plot, store goods, pass through. The owner grants it, with terms (rent in goods per day, or as part of a job). An employee works with the employer's tools under a `Work` right: **the output belongs to the owner of the building, not to the worker.** This one rule is where the wage relation lives.
- **Lots carry owners.** A hauler carrying state planks holds a lot owned by the state. Delivering it is the job; keeping it is theft.
- **Theft:**
  - Taking a lot from a store or carry, or using an asset, against a claim the taker knows of, without a hand-over by the owner. The sim knows it is theft (true state); others know only if they see it.
  - **Witnesses:** every NPC whose perception includes the act and who attends to it gets an observation (taker, owner if recognised, ware). Darkness, distance and attention limits make many thefts unseen. A witness may `Accuse`, `Warn`, tell the owner (gossip), or do nothing.
  - What counts as theft in law, and what a guard or court does, is in [STATE_DESIGN.md](STATE_DESIGN.md) (laws v1) and [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md). Without institutions, only the owner's and witnesses' own responses exist.

### 4.5 Exchange
- **Subjective valuation** (integers, worth units; 1000 worth = relieving a day's hunger for this NPC). For one more unit of ware *w* given holdings *h*, the NPC takes the largest of:
  - **consume use:** drive relief × urgency of the drive now × variety factor × freshness left, over the planning horizon (2 to 6 days, from patience);
  - **production use:** if *w* is an input to a recipe, job or hut the NPC plans, its share of the planned output's worth;
  - **tool use:** expected extra output over the horizon from the tool's effect;
  - **exchange use:** the NPC's price belief for *w* × its confidence × 9/10, minus carry cost.

  Units beyond what the horizon needs have no consume use, so surplus is valued only for exchange. The full value of a bundle is the sum of marginal values, unit by unit.
- **Price beliefs:** a bounded table (16 wares) in `Mind.Beliefs`, each a worth estimate with confidence. A trade the NPC makes or sees, *a* of A for *b* of B, implies `P(A) ≈ b × P(B) / a`. The belief moves towards it by 1/4 (own trade) or 1/8 (seen trade); an unknown ware takes the implied value directly. Food beliefs start from the NPC's own consume use. Oldest unused entries are forgotten first.
- **Offers and counter-offers by gesture** (grammar in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#49-gesture-language-simcomms)):
  1. `Offer { Topic: wanted ware, Quantity, Counter: offered ware and count }`, ideally while presenting the goods.
  2. The listener values both sides. It answers `Accept`, `Refuse`, or a counter-`Offer` with changed counts. At most 3 rounds, fewer for impatient NPCs; then one walks away.
  3. **Hand-overs:** after an accept, each side decides on its own think ticks whether to `Give`. A give moves one lot (or part of one) from the giver's carry to the receiver's, 10 ticks, adjacent only. The receiver becomes the owner. Each give is one `ExchangeStep` event.
  4. **Who goes first** is not a rule. Each NPC's decider weighs trust in the partner, the value at risk and its own needs. A wary pair can trade one unit at a time, which lowers the stake of each step.
  5. **Defection:** an NPC that has received can simply walk away, keep the goods, or give less. Nothing in the sim restores the loss. The partner's social memory records it ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#obligations-and-promises)).
- **Acceptance rule (scripted policy):** accept when `worth(received) − worth(given) − risk > margin`, where `risk = worth(given if first) × (1000 − trust) / 1000` and `margin` falls with generosity and rises with greed. Personality changes scores, never rules.
- **Gifts:** a `Give` with no counter. `GiftGiven` is emitted. Gratitude and a felt obligation are social memory's business. Food sharing in households and between roostmates uses this.
- **Help in labour:** `NeedHelp` pointing at a task (a load, a site, a field). A helper that joins works under the asker's ownership: the output is the asker's. `LabourHelped` records work ticks. Repayment, if any, is another act.
- **Loans and deferred exchange (M7):** A gives now; B makes a `Promise` gesture (Time: soon = 1 day, later = 3 days) to give back. The terms exist only in the two minds (each as decoded) and in the `DealTrace` (as meant). Repayment is another set of hand-overs, or not. Telemetry classifies `LoanRepaid` (repaid at least the promised count by due day + 1), `LoanDefaulted` (nothing by then) or partial.
- **Obligations:** recorded in social memory, never enforced by the economy. See [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#obligations-and-promises).
- **Markets as meeting places:** a market is any node where NPCs believe trades happen. Each NPC keeps "trades seen here" counts in its place beliefs; an NPC with goods to trade scores walking to a busy place. Markets can therefore form by themselves near HQs, huts or crossroads. The state can designate a market square ([STATE_DESIGN.md](STATE_DESIGN.md)); that adds a board and visibility, not enforcement.
- **Institutional enforcement, later:** an escrow market (staff hold both sides' goods and swap them: the only atomic exchange, run by an institution with its own staff and failures), courts that make promises credible, and written contracts. See [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md).

### Jobs and the end of walkers
- **Postings:** the state, an enterprise, a household or any NPC can post a job at a board: haul (ware, from, to), build (a site), gather (at a harvester building or source), staff (a building, per shift of 600 ticks), envoy, or forage for a household. Terms are goods: `PayCount` of `PayWare` per load, shift, unit or task.
- **Boards** stand at every HQ and at flags where the poster places one (the state places them by command; any NPC owner can place one on its own building). NPCs know a posting only by reading a board or being told (cognition doc).
- **Taking a job is a choice.** A posting is a candidate intent scored like any other: pay worth (by the NPC's own valuation of the pay ware) minus walking, effort (fatigue), risk and the value of the time for its own needs, plus learned bias. `JobTaken` reserves a slot and, for hauls, the source units and destination space, as stage 3 carriers do now.
- **Doing and abandoning:** an NPC may drop a job at any think. A taken job with no progress for 300 ticks is released (`JobAbandoned`), and its reservations return.
- **Walk-outs suspend the contract first (owner decision Q19, 27 Nov).** For a lasting job (a staff or employment spell, M6 onwards), an employee who refuses to work without quitting (`RefuseWork`, alone or with others) does not end the contract: the employment becomes `Suspended`. While suspended, no work is done and no pay is due; the slot stays the employee's; the 300-tick abandonment rule does not apply. Each side then decides by its own scores: **the employee may resume, negotiate (an `Offer` about pay or terms), resign or keep refusing; the employer may negotiate, improve pay, dismiss or seek alternatives** (post a replacement, hire someone else). An employer can likewise suspend work and pay (a lockout). Nothing ends a suspension automatically and nothing scripts the outcome, so collective bargaining, strikes, lockouts and economic conflict (Stage 5, [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)) arise from material circumstances and incentives. Events: `ContractSuspended(by, reason)`, `ContractResumed`, `ContractEnded(reason: Resigned, Dismissed, …)`.
- **Pay:** the poster pays on completion of each unit. **State pay** is made by the HQ store under the state's rules (an institution, so it is enforced while the treasury has the goods; arrears otherwise, see [STATE_DESIGN.md](STATE_DESIGN.md)). **Private pay** is a hand-over by the employer, so an employer can cheat a worker and a worker can demand pay first. Each payment is `WagePaid`.
- **Wage labour** is the case where an NPC works regularly for another owner for pay. It is not a special rule: it is the job mechanism used repeatedly.
- **Dependence on selling labour** arises when an NPC has no access to subsistence: no forage source it believes it may use within reach (depleted, enclosed by claims, or far), no productive asset of its own and no household that feeds it. It then has only jobs or gifts. Measured as `WageDependencePermille`: share of the NPC's food and goods intake over 7 days that came from pay. Not forced; possible.
- **What replaces the walkers, priorities and HQ carrier pools** is in section 9.

### 4.7 Enterprises (M8)
- **Founding:** an NPC (or a group) founds an enterprise by claiming a building: buying it from the state (a state sale offer), building one on a site it is allowed (a private site under laws v1), or occupying an abandoned one (first arrival). `EnterpriseFounded`.
- **Running it** uses the ordinary intents: buy inputs (barter or a posting "deliver X, paid Y"), post staff and haul jobs, set pay, sell output (barter, a standing offer at its board, or a state purchase order). The owner's decider plans these as enterprise intents under the same `INpcPolicy`.
- **Wealth accumulates** as owned lots and buildings. The `Ledger` records true income and outgoings; NPCs know only their own memory of them.
- **Heirs:** on the owner's death the shares become part of the estate ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)).
- **Cooperatives:** the workers are the owners, with equal shares by default. Decisions (pay, prices, admitting a member, what to make) are proposals put to a meeting at the building: each present member signals `Accept` or `Refuse`, majority wins, ties keep the old choice. The output belongs to the enterprise; shares of surplus are paid out by rule (equal, or by work ticks). Free-riding inside a cooperative is possible and measured (work ticks per member vs share received).
- **Communal production:** a group (a household or band) works a shared asset, and output goes to the group store; each member may take by the group's norm. Measured like a cooperative.
- **State-owned production:** the state's buildings, run by `IInstitutionPolicy` or the faction policy, employing NPCs through the same postings.
- **Equal rules:** production speed, recipe, job, exchange and theft rules have no branch on `EnterpriseForm`. A reflection-and-test check enforces this (section 10). Which form does better is a result to measure across seeds ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)).

### 4.8 The commons (Ostrom)
- **Resources:** forage sources (berries, roots), fishing grounds, forest (from M6, the stock behind dice log piles, 4.2) and later pasture. Each has `Stock` and logistic regrowth once a day: `Stock += RegrowPermille × Stock × (Capacity − Stock) / (Capacity × 1000)`, at least 1 while `0 < Stock < Capacity`. An emptied source regrows only if a neighbouring source of its kind is above half capacity (1 unit per 3 days). So overuse is possible and costly. **The ecological rules are configurable per resource kind** (`SimTuning.Ecology`: growth rate, capacity, the recovery rule; owner decision Q11), so experiments can vary how forgiving nature is.
- **Open access by default.** Anyone who knows a source may take from it. The temptation is to take more today; the cost falls on everyone later. That is the free-rider problem, with real consequences.
- **What lets local rules emerge** (Ostrom's conditions, each a mechanic, none a rule that applies itself):

| Condition | Mechanic that makes it possible | Measure |
|---|---|---|
| Clear boundaries | a `Commons` record: a set of sources and a member list, created when a group claims sources communally (first arrival as a group, or a state grant) | share of takes made by members |
| Rules fitting local conditions | a cultural norm trait "take at most *q* per day from commons *c*" ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)); *q* varies by holder and drifts by imitation of the better-off | spread of *q* among members; stock trend |
| Monitoring | perception and recognition: members who see takes remember who took how much | takes witnessed / takes made |
| Graduated sanctions | `Warn`, `Accuse`, refusing trades and help, exclusion from the member list, and force; chosen by each NPC's decider and anger, learned over time | sanction acts per violation; repeat violation rate |
| Conflict resolution | communal mediation by elders (M12 institution) | disputes resolved / raised |
| Recognised right to organise | a state policy that recognises a commons record (or overrides it) ([STATE_DESIGN.md](STATE_DESIGN.md)) | commons with a recognised record |

- **Communal rules are a norm plus enforcement by members.** Holding the norm biases the holder's own take; enforcing it is a separate choice with a cost. Both spread culturally. Whether a village's commons survive is an outcome.

### 4.9 Public goods
- **Roads, granaries and wells** (wells later; there is no thirst drive in stage 4). A road or granary is a site; anyone can contribute labour or materials (`Contributed` event) or post jobs for it.
- **Contributing vs free-riding:** everyone nearby benefits from a road (faster walking) or a granary (slower spoiling), whether or not they contributed. Measured as contribution share by beneficiary (NPCs using it in 7 days) and the share of users who never contributed.
- The state can pay for public goods from the treasury through contracts ([STATE_DESIGN.md](STATE_DESIGN.md)); voluntary and communal provision use the same site.

### 4.10 Surplus
- **Measured per producer per day** in two ways:
  - **physical:** food produced (nutrition points) minus the producer's own subsistence need (1000 per adult per day);
  - **indexed:** value of everything produced in `FE` (`WareRefValue`, section 3.3), minus inputs used, minus subsistence. The index is fixed so days and seeds compare; NPCs never read it.
- **Who gets it:** for each unit produced, telemetry records the producer, the owner of the output, and any pay given for it. Over a day, per owner: `appropriated = output index − inputs − wages paid`; per worker: `wages received − own output index`. Those two numbers are the link to class ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)) and to political interests ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)). This document only defines and emits them.

### 4.11 Faction-level trade and money
- **Envoys become NPC volunteers.** `TradeOffer`, `Answer` and the default judge stay state-level. Sending an offer posts an envoy job (pay, risk). The volunteer carries a state-owned lot overland; it can be shot, can abscond (theft against the state) or defect (M10). No volunteer means the offer waits. The envoy's `MilUnit` body becomes the NPC's body.
- **Private trade between factions** (NPC merchants) comes later.
- **Barter only in stage 4.** The seam for money:
  - Pay, offers and prices are always `(Ware, Count)`, so a coin is just another ware.
  - The exchange-use term of valuation lets a ware be accepted only because others will accept it. A commodity money can therefore emerge as a convention. Measured as `Moneyness(w)`: share of a ware's units received by exchange that are traded on again, not used, within 5 days. When one ware passes 500‰ over 10 days, `MediumOfExchangeObserved` is emitted (a measure, not a rule).
  - State currency (minting, accepting it for taxes) is a later stage ([STATE_DESIGN.md](STATE_DESIGN.md), Stage 5 pilot, Stage 6).

### 4.12 Order and random streams
The economy runs after cognition has applied intents, in this order each tick: (1) apply intents in ascending NPC id (eat, give, take, work, post); (2) buildings in ascending id (cycles, sites); (3) the haul planner, every 10 ticks per faction (section 9.2); (4) release stale reservations; (5) at day start: spoiling, regrowth, daily measures. Random draws: forage yield from `Rng.Fork("npc", id)` (the gatherer's own stream), regrowth has none, tie-breaks by lower id. The dice keep their current stream until M0 forks it as `Rng.Fork("dice")`.

## 5. Interactions with other systems
- **State:** taxes, wages policy, state purchase offers, sites and contracts, the paymaster and arrears are owned by [STATE_DESIGN.md](STATE_DESIGN.md).
- **Cognition:** observations include board postings, offers, gives seen and price evidence; intents include `Give`, `Take`, `Eat`, `Work`, `TakeJob`, `Post`, `Offer` ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).
- **Social memory:** every exchange step, gift, help act, loan and theft seen is input to trust and obligations ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Learning:** pay, food and losses are rewards and punishments for the (situation, intent) pairs ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).
- **Households:** hut stores are household property; sharing rules there are in [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md).
- **Military:** the quartermaster's store (which replaces the barracks) issues gear lots to enlisted NPCs; rations and military pay are job terms ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
- **Institutions:** the shared store, informal lending, markets and mediation are institutions built on these mechanics ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **Lab:** exchange benchmarks (Grudger, Tit for Tat) plug in as `INpcPolicy` benchmarks that act only in exchange and help situations ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md), [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)).

## 6. Player and view
- **The HUD reads only `FactionKnowledgeView`** (full information in stage 4): stocks by store and owner form, production and consumption by ware, postings and how many are taken, wages paid, exchanges per day, surplus, commons stock trends, and the development measures of 4.3.
- **State economy panel:** post or withdraw jobs, set pay per job kind and per input (the old priority buttons become pay levels), place boards, designate a market. Commands are in [STATE_DESIGN.md](STATE_DESIGN.md).
- **Building card:** owner and form, staff and their pay, store contents by owner, the postings at its board.
- **NPC card** (its own view): what it carries and who owns each lot, what it thinks things are worth (its valuations and price beliefs), its job and terms, debts it believes it owes and is owed.
- **Overlays:** ownership colours (state, private, communal, cooperative); commons stock as bush and shoal fullness; markets as busy-place heat.
- **Animations show events only:** a hand-over plays a give-and-take pose per `ExchangeStep`; a theft plays a furtive take; a job board shows postings as pinned tags; pay is a give at the HQ. Nothing in the view decides an outcome.

## 7. Telemetry
All events carry `Tick` and the ids involved. Counts are integers; ware arrays are `(Ware, Count)` lists.

| Event | Key fields |
|---|---|
| `Produced` | BuildingId or SourceId, ProducerNpc, Owner, Ware, Count, WorkKind, ToolWare, Skill |
| `Consumed` | NpcId or BuildingId, Owner, Ware, Count, Cause (Eaten, Input, Built, Wear, Spoiled, Destroyed) |
| `ItemMoved` | LotId, Ware, Count, From (carry/store/pile), To, Owner (sampled at the detailed recording level only) |
| `ExchangeStep` | DealId, Step, Giver, Receiver, Ware, Count, LotId, Node |
| `ExchangeClosed` | DealId, A, B, Outcome, AGiven, BGiven, AOffered, BOffered |
| `GiftGiven` | Giver, Receiver, Ware, Count, Kin (bool), SameHousehold (bool) |
| `LabourHelped` | Helper, Helped, WorkKind, WorkTicks |
| `LoanMade` / `LoanRepaid` / `LoanDefaulted` | DealId, Lender, Borrower, Ware, Count, RepayWare, RepayCount, DueDay, RepaidCount |
| `JobPosted` / `JobWithdrawn` | PostingId, Poster, Kind, BoardNode, PayWare, PayCount, Per, Slots |
| `JobTaken` / `JobAbandoned` / `JobDone` | PostingId, NpcId, Units |
| `WagePaid` | PostingId, Payer, NpcId, Ware, Count, Enforced (state) or not |
| `WageUnpaid` | PostingId, Payer, NpcId, Owed |
| `OwnershipChanged` | Asset, From, To, Basis |
| `UseRightGranted` / `UseRightEnded` | Asset, Holder, Kind, Terms |
| `TheftCommitted` | Taker, Owner, Ware or Asset, Count, Witnesses (count; ids at the detailed level) |
| `EnterpriseFounded` / `EnterpriseDissolved` | EnterpriseId, Form, Founders, Buildings |
| `CoopDecision` | EnterpriseId, Proposal, For, Against, Result |
| `Contributed` | Site, NpcId, Ware or WorkTicks |
| `CommonsTake` | SourceId, NpcId, Count, Member (bool), StockAfter (aggregated per day at the default level) |
| `MediumOfExchangeObserved` | FactionId, Ware, Moneyness |

**Snapshots (daily):** per NPC: holdings index, owned assets, work ticks by activity, pay received, food intake by source (own, gift, pay, household, theft), skill profile, `WageDependencePermille`. Per faction and village cluster: production and consumption by ware, exchanges, price dispersion, `SpecialisationIndex`, surplus physical and indexed, appropriated surplus by owner form, commons stocks, open postings. Recording never changes the hash.

## 8. Stage 4 minimum vs later stages

| Milestone | Economy content |
|---|---|
| M3 | Foraging from `ResourceSource`s, food wares and spoiling, eating, personal carry with weight, hut stores, hand gathering of piles. Holder is owner. Conservation ledger and `Produced`/`Consumed`. Walkers still run the stage 3 economy. |
| M5 | Valuation, price beliefs, offers and counter-offers, hand-overs (`ExchangeStep`), defection by walking away, gifts, help in labour, meeting-place markets. Variety rule. Skills and switching cost. |
| M6 | Owner tags on lots; property records (state, private, communal); use rights; job postings and boards; state pay; the haul planner; staffed workshops in both modes; contract suspension (`Suspended`, Q19); forest stock behind dice log piles (on in sandbox, off in VP, Q11); `HutRecipe` option; envoys as volunteers. **Walkers deleted.** Tools and the toolsmith. |
| M7 | Loans and deferred exchange, promise-linked deal traces, repaid and defaulted classification. |
| M8 | Item quality (true and visible), cheap fakes and inspection. Enterprises (private, cooperative, communal, state), founding, NPCs employing NPCs, private pay as hand-overs, theft with witnesses, commons records and norms, public-good contributions, surplus appropriation measures, `Moneyness`. |
| Later | Pasture; forest commons records with institutions; farms as tended plots by default; escrow markets and courts (Stage 6); currency pilot (Stage 5) and state currency and banks (Stage 6); markets at scale, merchants between factions, chartered companies, leases and other forms. |

## 9. Migration from the existing code

### 9.1 What stays, changes or goes

| Existing | Stage 4 |
|---|---|
| `Wares`, `BuildingDefs` recipes, costs, `BarracksRules` | **Stay.** New wares appended (3.2); the toolsmith added. |
| `EcoBuilding.In/Out/Stock` and reserved arrays | **Become** the single-owner fast path of `Store`; private and communal stores add lots. |
| `Priority`, `SetInputPriority` | **Become** a pay premium on haul postings (9.2). The command stays as a state command with the same name in M6 so `AiBrain` keeps working. |
| `SetPaused` | **Stays:** a paused building posts no staff or input jobs. |
| `EconomySystem.Match` | **Becomes** the state's haul planner: same requests, same sort, but it posts or updates haul jobs instead of launching carriers. |
| `DispatchBuilders`, `SiteTick` | **Become** build postings; `BuildProgress` is the sum of present builders' work speed. |
| `HarvestStart`, harvester walkers | **Become** gather postings per harvester building; pile reservation (`PilesReserved`) and the fewest-claims rule stay. |
| `Walker`, `WalkerKind`, `CarriersPerHq`, `BuildersPerHq`, `IdleCarriers` | **Deleted** in M6. A new HQ no longer brings carriers; NPCs move or migrate to it. |
| `LostWares`, `DeliveredWares` | **Replaced** by the conservation ledger and `Consumed(Destroyed)`. |
| Envoy (`TradeState.Envoy`, `MilUnit` body) | **Refactor:** an envoy is an NPC holding a state job; offers unchanged. |
| `TradeTuning.ValueOf` | **Kept** for the faction-level default trade judge only. Its numbers seed `WareRefValue` (3.3), the separate measurement index. |
| `AiBrain` economy | **Unchanged in M6** apart from an adapter: its priority and pause commands map as above, and a default `WageSchedule` sets pay. Rewritten for incentives in M14. |

### 9.2 The end of walkers (M6), step by step
1. **Postings from requests.** Every 10 ticks per faction, the haul planner builds the same requests as `Match` (one per missing input unit, sorted by priority, route length, consumer id, ware). For each request it ensures one open haul posting: source = the best supplier by the stage 3 `Candidates` rule, destination = the consumer, pay = `BaseHaulPay × premium(priority)` (Low 1, Normal 2, High 3 units of the state's pay ware per load, by default bread or grain; Stop = no posting). Leftover outputs get postings to the nearest HQ at Low pay.
2. **Where postings are seen.** Haul postings appear on the board of the source's nearest HQ and on the source building's flag board. An NPC at a board reads all of them; an NPC passing a flag reads that flag's.
3. **Matching is by choice, not assignment.** NPCs pick postings by their own scores. Two NPCs cannot take the same slot: `TakeJob` intents are applied in ascending NPC id, and a full posting refuses later takers.
4. **Reservations** work as for stage 3 carriers: `OutReserved` at the source and `Incoming` at the destination on take; released on delivery, abandonment (300 ticks without progress) or loss.
5. **Build jobs** are posted when a site is designated (haul postings bring the cost wares); work starts when all wares are in. Up to 3 builders work at once; progress per tick = sum of their speeds / 1000, so 1 average builder equals a stage 3 builder.
6. **Gather jobs** are posted by each finished harvester (1 slot; 2 at level 3). The worker walks to the pile with the building's cart, gathers (level effects unchanged) and brings the load to the flag. Level 3's trickle needs a worker present.
7. **Staff jobs**: each workshop and the quartermaster's store (the former barracks, [MILITARY_DESIGN.md](MILITARY_DESIGN.md)) post 1 staff slot per shift. No staff, no cycle.
8. **Pay** comes out of the HQ store holding the state's pay ware. If it is empty, `WageUnpaid` is emitted and arrears are recorded ([STATE_DESIGN.md](STATE_DESIGN.md)); NPCs learn from it.
9. **Delete** `Walker` and its code paths once the M6 tests below pass; `GameSetup.Population = off` (stage 3 mode) keeps the old system until stage 3 tests are retired, as in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#9-migration-from-the-existing-code).

### 9.3 Keeping the economy tests meaningful
Each stage 3 test in `EconomyTests` and `Stage3EconomyTests` gets an NPC-labour twin. The twin runs a scripted scenario with enough NPCs (6 per HQ, the old carrier count) using `AssociativeNpcPolicy`, and checks the same property with a time tolerance:

| Stage 3 test | NPC-labour twin checks |
|---|---|
| `Site_IsDeliveredOverRoads_AndFinished` | the site is finished within 1.5× the stage 3 ticks, all wares carried by NPCs on roads |
| `Site_WithoutARoad_Waits` | no haul posting is made without a route; the site waits |
| `Harvester_TakesPiles_WalksOffRoad_AndDeliversToTheHq` | a gather worker takes the pile, walks off-road, the load reaches the HQ |
| `TwoHarvestersOnOneHex_ShareDeterministically_LowerIdFirst` | the fewest-claims rule still decides which building posts first |
| `Priority_ChangesTheDeliveryOrder` | with spare haulers short, the High consumer is delivered first in ≥ 8 of 10 seeds |
| `Stop_AsksForNothing_AndPauseFreezesWork` | Stop posts nothing; a paused building posts no jobs and runs no cycle |
| `SiteDestroyed_MidDelivery_CarriersReturnTheirCargo` | the hauler keeps the state lot and returns it to an HQ (or steals it: then `TheftCommitted`) |
| `HqDestroyed_LosesItsStockAndCarriers_RestKeepsRunning` | the stock is `Consumed(Destroyed)`; the NPCs survive and keep working elsewhere |
| `ChainRun_BuildsTheWholeChain_AndMakesAnAxeman` | see test E2 below |
| `ChainRun_KeepsItsInvariants_NoLeaksOrNegativeCounts` | conservation ledger exact; no negative count; no dangling reservation after 300 ticks |
| `SameSeedAndCommands_SameStateHash` | unchanged, with NPCs |

## 10. Tests and acceptance criteria
All run headless in `DotNet/God.Sim.Tests` or as `God.Runner` scenarios; "seeds" means seeds 1 to 10 unless stated.

**A. Goods and conservation**
- A1 **Conservation:** for every seed, 20 sim days, default game with population: at every day end, for every ware, `start + Σ Produced − Σ Consumed = Σ (carry + stores + piles + site deliveries)`, exactly. Also true after save and load at day 7.
- A2 No `ItemLot.Count ≤ 0`, no store over its weight cap, no NPC carry over its limit, at any tick, in A1's runs.
- A3 A food lot past its `SpoilDay` is gone at the next day start, with one `Consumed(Spoiled)`.
- A4 Variety: the 3rd berry eaten in a day relieves 1000/1500 of the first.

**B. Exchange**
- B1 **Defection is possible:** lab scenario, NPC A with benchmark policy `TakeAndLeave`, NPC B with `AssociativeNpcPolicy`, B gives first. After the deal A holds both goods, B holds neither, `ExchangeClosed.Outcome = Defected`, no system returns the goods, and B's trust in A falls (social memory hook).
- B2 Honest pair: two `AssociativeNpcPolicy` NPCs with complementary surpluses complete ≥ 90% of accepted deals in 20 runs.
- B3 Incremental trade: a pair with trust below 300 that trades uses ≥ 2 steps per side in ≥ 50% of deals (mechanism present, not forced; reported).
- B4 Price beliefs: after 10 observed trades at 3 berries for 1 fish, the NPC's `P(fish)/P(berries)` is within 2.5 to 3.5.
- B5 Loans: a scripted repayment is `LoanRepaid`; none by due + 1 day is `LoanDefaulted`; half is partial.
- B6 No policy, cognition, learning or estate-split type references `DealTrace`, `PropertyRecord` truth, `Ledger`, `WareRefValue` or `TradeTuning.ValueOf` (reflection test, beside `Policies_DoNotReferenceGodState`).
- B7 Quality (M8): a fake lot shows its `VisibleQuality` to every observer; an `Inspect` by an NPC with skill 255 reveals the true quality in 90 to 95 percent of 1000 tries; a revealed fake records a caught lie in the buyer's social memory.

**C. Specialisation and surplus (measured, with controls).** These are calibration checks of the default `AssociativeNpcPolicy` and tuning, not outcomes the design guarantees: they show the mechanics *make specialisation possible* when its conditions hold, and the controls show it does not appear without them.
- C1 **Two resources, different skills:** scenario `TwoFoods`: 40 adults, one village, berries in forest and fish on the coast in equal capacity, half start with forage-berries skill 120 and half fish skill 120, the rest 20. By day 10, on ≥ 7 of 10 seeds: `SpecialisationIndex ≥ 650‰` and at least 150‰ above the same seed with `ExchangeOff`; ≥ 0.5 completed exchanges per adult per day; mean daily nutrition not below the control's. If it fails, the mechanics or tuning are revisited; no rule may be added that assigns activities.
- C2 With identical skills and `ExchangeOff`, `SpecialisationIndex` stays below 550‰ (the control shows the mechanism, not a bias, makes the difference).
- C3 Surplus: in the default game, physical food surplus per adult is reported daily and is positive on day 10 on ≥ 7 of 10 seeds (a balance target, not a rule).
- C4 **Complementary inputs (Q10):** scenario `LogsAndClay`: 40 adults, `HutRecipe = LogsAndClay`, logs on one side of the village and clay on the other, food plentiful (so food variety is not the motive). By day 10, on ≥ 7 of 10 seeds, completed exchanges of logs for clay are ≥ 0.2 per adult per day and huts built are at least 1.3× the same seed with `ExchangeOff`. Reported with the C1 control style; no rule assigns who gathers what.
- C5 **Hand gathering vs capital (Q9):** the measured output per work hour of hand gathering against a staffed harvester with an axe is reported for every default run; at default tuning the ratio lies in 3–5 (a tuning check on the initial 4× assumption, not a design constant).

**D. Property, theft, commons**
- D1 Theft with witnesses: a take against a claim in daylight within sight of 2 attending NPCs gives both an observation; the same take at night beyond sight radius gives none.
- D2 First arrival: an NPC using an ownerless hut plot gets a `FirstArrival` record; a second NPC arriving later appears in `Disputes` only if it acts on it.
- D3 Commons, open access: scenario `Commons30`, 30 NPCs, one berry commons with capacity 200 and growth 150‰, no norm: the stock falls below 20% of capacity by day 15 on ≥ 6 of 10 seeds. With the norm *q* = 3 seeded in 80% of members and sanctions on, the stock stays above 50% on ≥ 6 of 10 seeds. Both results are reported; they test that both outcomes are possible.
- D5 **Forest stock (Q11):** in the sandbox default a forest hex harvested faster than it regrows reaches stock 0 and then gets no log piles until regrowth (exact, unit test); with `forestDepletion` off the same harvest never changes pile size (exact); `Commons30`-style forest scenario with and without a seeded take-limit norm shows both depletion and sustained stock are possible (as D3).
- D4 Equal rules: the same building, recipe, staff skill and tool give identical output per tick for every `EnterpriseForm` (parameterised test over the four forms).

**E. Jobs and the stage 3 chain**
- E1 Jobs are chosen: with haul pay set to 0, no NPC takes haul jobs in 5 days; with default pay, ≥ 80% of haul postings are taken within 600 ticks.
- E2 **The chain still works:** default 4-faction game with population, AI factions on `AiBrain` with the M6 adapter: on ≥ 9 of 10 seeds every faction completes plank → building → gear → its first soldier, within 1.5× the stage 3 median ticks for that seed.
- E3 Walkers are gone: after M6 no `Walker` type exists in the sim assembly (reflection), and no code path creates a worker for a job.
- E5 **Suspension (Q19):** an employee's `RefuseWork` moves the employment to `Suspended` with one `ContractSuspended`; no `WagePaid` and no work ticks follow while suspended; no `JobAbandoned` is emitted for it after 300 ticks; the contract ends only by a `Resign` or `Dismiss` intent (exact, unit test). A lockout fixture behaves the same from the employer's side.
- E6 **No anonymous workers (Q8):** in VP mode and in the sandbox, every recipe cycle, build tick and gather has a staff NPC present (exact over 10 seeds × 10 days); a workshop with no staff runs no cycle in either mode.
- E4 Dependence: scenario `Enclosure`: 30 NPCs, all forage sources claimed by 3 owners who post gather jobs: mean `WageDependencePermille` of non-owners exceeds 600 by day 10 on ≥ 7 of 10 seeds; in the same seed with open access it stays below 300.

**F. Determinism and performance**
- F1 Same seed and commands give the same hash with recording off, default and detailed.
- F2 **500 NPCs**, 4 factions, 10 sim days headless: the whole sim at ≥ 20× real time on the reference machine (one core), the economy system ≤ 20% of tick time, the haul planner ≤ 2 ms per run, no per-tick heap allocation in the economy hot loop after warm-up (measured by the runner).

## 11. Open questions

No open questions remain.

Settled in review (26 Nov 2026): owner tags arrive in M6 and item quality in M8; the state paymaster is enforced while the HQ store holds the pay ware, as [STATE_DESIGN.md](STATE_DESIGN.md) confirms; the dice stream is forked in M0 ([DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#4-time-model)); `WareRefValue` is owned here (3.3).

**Owner decisions of 27 Nov 2026** ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):
1. **Q8 Staffed workshops:** yes, in both modes. All productive work is ultimately done by persistent individuals. No anonymous or auto-generated workers are kept to preserve stage 3 balance; production, staffing and resources are rebalanced around the population system (4.2, E6; balance at M14).
2. **Q9 Gathering dice piles by hand:** yes. Specialist buildings, tools, skills and infrastructure make it much more efficient, about 4× as an initial tuning assumption, not a fixed rule. This supports independent subsistence, specialisation and mobility (4.2, C5).
3. **Q10 Food variety:** keep the rule, and also test complementary production inputs (logs and clay for shelter). Several natural reasons to exchange: different nutritional benefits, local availability, complementary construction materials, productive capabilities, individual needs, and skills and equipment. Trade emerges because it benefits individuals, not because the game demands it (4.1, C4).
4. **Q11 Forest depletion:** forest stock limits are on by default in the sandbox; non-depleting by default in the VP mode, with a scenario option to enable depletion. Renewable resources regenerate by configurable ecological rules. Overexploitation, sustainable harvesting and eventual resource-management institutions (tragedy of the commons, Ostrom experiments) are supported (4.2, 4.8, D5).
5. **Q19 Walk-outs:** suspend the contract first. The employee may resume, negotiate, resign or keep refusing; the employer may negotiate, improve pay, dismiss or seek alternatives. This is the basis for collective bargaining, strikes, lockouts and economic conflict, arising from material circumstances and incentives, not scripted events (Jobs, E5; [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md#11-open-questions)).
