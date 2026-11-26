# State design (stage 4)

The stage 4 state: what a faction's government owns, what it can offer, and how it can act on a population it cannot command. The shared names, layers, policy levels and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md); that file wins where this one disagrees.

## 1. Purpose and scope

**This document owns:**
- the treasury (state stockpiles at HQs) and its ledger;
- taxes at points of contact, and the later head and land taxes;
- wages, purchase offers and public works;
- designating building, road and HQ sites, committing treasury goods and posting construction contracts;
- welfare basics: the granary and food handouts;
- laws v1: fines for violence and theft, and property rules;
- citizenship, loyalty, migration, defection and immigration policy;
- victory conditions (`IVictoryCondition`);
- the state command set shared by the HUD and `IFactionPolicy`;
- the migration from today's direct placement and input priorities.

**It links to, and does not repeat:**
- how an NPC finds, takes and does a job, and how goods change hands: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- ownership forms and records: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- the NPC record, drives and immigration rates from off the island: [POPULATION_DESIGN.md](POPULATION_DESIGN.md);
- how a notice board posting is perceived and believed: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md);
- trust, promises and reputation (including trust in the state): [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- recruitment, soldiers' pay, assignments and morale: [MILITARY_DESIGN.md](MILITARY_DESIGN.md);
- institutions with staff and managers (guards, the shared store, healers and the rest): [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md);
- `FactionKnowledgeView` and its later limits: [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md);
- KPI definitions: [KPI_CATALOGUE.md](KPI_CATALOGUE.md);
- political interests and collective action: [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md).

## 2. Principles
- **The state is an owner, not a god.** It owns goods in HQ stores and some buildings. It can offer those goods for work and goods, take a share at places where it touches a transaction, and publish rules. It cannot move a person, place a finished building or read a mind.
- **Every transfer is a physical hand-over at a place.** Wages are handed out at a pay point; purchases are delivered to an HQ; taxes are taken at the counter. A transfer that has no place and no NPC on each side does not exist.
- **Nothing is magicked.** A site with committed goods and no labour stays a site for ever. A test enforces this (section 10).
- **The state can break its promises too.** A posted wage is a promise. If the treasury cannot pay, the state defaults, and the NPCs who were owed remember it. Trust in the state is a social-memory record like trust in any NPC.
- **One command set.** The HUD and every `IFactionPolicy` (scripted, benchmark or trained) send the same commands through the same `Check`/`Submit` gate, and read only `FactionKnowledgeView`.
- **No system is built to win.** High taxes, low taxes, generous welfare, none, open or closed borders: each changes payoffs for NPCs. None is tuned to be best. Outcomes are measured.
- **Determinism.** Integers only. State systems run in ascending faction id, then ascending contract, site or NPC id. Random draws use `Rng.Fork("state", factionId)`; none are needed in stage 4 except tie-breaks inside NPC policies, which use the NPC's own stream.

## 3. Data structures (`Sim/State`)

All sizes are bounded; every list is kept sorted by id.

```text
StateRecord                       // one per faction
  Faction
  Treasury: TreasuryLedger
  Tax: TaxPolicy
  Purchases: List<PurchaseOffer>  (<= 32)
  Contracts: List<int> ContractIds (open ones, <= 256)
  Sites: List<int> SiteIds        (<= 64)
  Welfare: HandoutRule
  Laws: List<Law>                 (<= 16)
  Immigration: ImmigrationPolicy
  StateTrustSeed: short           // starting trust a newcomer has in this state

TreasuryLedger
  // The goods are the existing HQ stocks (EcoBuilding.Stock, IsHq). The ledger adds the accounts.
  Committed[ware]                 // reserved for sites and contracts (extends today's StockReserved)
  Arrears: List<Debt>             // wages and purchase prices earned but not yet handed over
  DayIn[ware], DayOut[ware]       // today's flows by cause (tax, purchase, wage, handout, site)

StateContract   (the state's side of an ECONOMY_DESIGN JobPosting; same ContractId)
  // The posting itself (kind, board, task, pay ware and count, slots, expiry) is ECONOMY_DESIGN's JobPosting.
  // A state posting adds only what the state promises and how it pays:
  Id (ContractId), Faction
  Kind: the JobPosting kinds (Haul, Build, Gather, Staff, Envoy) plus Supply, Service (MILITARY), Collect (later)
  Advance: WareCount               // handed over when taken (optional)
  PayPoint: HQ id                  // the paymaster store where wages are handed out
  Escrowed: bool                   // pay already committed in the ledger
  Deadline (tick), Status: Open | Taken | Done | Abandoned | Withdrawn | Unpaid

Bundle  { Ware W0, W1, W2; short N0, N1, N2 }      // barter only: goods. Used by purchase prices, fines, gifts, rations.
                                                   // Job pay is one ware per posting, as in ECONOMY_DESIGN.

Site      (the site is the territory building or HQ record in its Site state)
  Id (BuildingId), Kind, Node, Faction
  Need[ware]                      // full cost
  Committed[ware], Delivered[ware]
  WorkDone, WorkNeeded            // build work units
  BuildContractId, HaulContractIds

PurchaseOffer
  Id, Ware, AtHq, Price: Bundle (per unit), MaxCount, Bought, Expires

TaxPolicy
  WageTaxPermille                 // kept at the paymaster from private wages paid through it (M8)
  SaleTaxPermille                 // kept back from the price of goods sold to the state
  StallFeePermille                // taken from each exchange made through a state escrow market (later)
  HeadTax: Bundle per adult per year, LandTax: Bundle per hut per year    // later, need collectors

HandoutRule
  Enabled, Granary (BuildingId), RationPerDay: Bundle, MaxPerDay, Eligibility: Citizens | Anyone | Workers

Law
  Kind: Violence | Theft | Trespass | Desertion (later) | ...
  Fine: Bundle, AlsoCompensateVictim: bool
  Scope: PropertyRule (for theft and trespass)

PropertyRule
  StateStores: StaffOnly | CitizensMayDraw
  CommunalStores: MembersOnly | Anyone
  PrivateGoods: OwnerOnly
  WildFood: Free | FirstPossessor

ImmigrationPolicy
  Mode: Open | AskToJoin | Closed
  MaxPerDay, WelcomeGift: Bundle, RequireSkill: (skill, minimum) or none
  AcceptDefectors: bool

CitizenData  (on the Npc record, see POPULATION_DESIGN.md)
  Faction, Loyalty (0..1000), CitizenSince (tick), PriorFactions: byte bitmask
  LoyaltyInputs: short[9]          // yesterday's terms, kept for the inspector and telemetry
```

**Ids:** `ContractId` (shared with the `JobPosting`) and purchase offer ids are unique for the game and never reused. A site keeps the `BuildingId` it will have when finished.

## 4. Mechanics

The state system runs once per tick after the economy and before the military, in ascending faction id. Daily work (loyalty, taxes per day, handout resets, expiry) runs on the first tick of each `SimCalendar` day.

### The treasury
- **Where it is.** The treasury is the sum of the faction's HQ stocks. There is no abstract pool: goods at HQ 3 cannot pay a wage at HQ 5 until a haul contract moves them. This makes geography and logistics matter for the state as for anyone.
- **Free, committed and owed.** `Free = Stock - Committed`. A command that commits more than is free at that HQ is refused (`not_enough_free`). Owed wages are a claim against the HQ named as the pay point.
- **Losses.** Goods in a store can be stolen (laws, below), spoiled (food spoils, see POPULATION) or captured with the HQ. Each loss is an event, so the ledger always balances.
- **Invariant (checked by `SimInvariants`):** for every faction and ware, `stock(t+1) = stock(t) + in - out` over the causes in `DayIn` and `DayOut`; and `Committed <= Stock` at every HQ.

### Taxes
**Stage 4 taxes are taken only where the state is already a party, with a place and an NPC on each side.**

| Tax | Where | Who hands over | Milestone |
|---|---|---|---|
| Sale tax | An NPC sells goods to the state at an HQ (a purchase offer) | The state pays the price less the tax; the NPC sees both | M6 |
| Wage tax | A private employer chooses to pay wages through the state paymaster at an HQ | The employer hands the gross wage over the counter; the paymaster keeps the state's share and hands the rest to the worker | M8 |
| Stall fee | Two NPCs exchange through a state escrow market, whose staff hold both sides' goods and swap them | The fee is kept from each side | later (an institution with staff, see ECONOMY_DESIGN) |
| Head tax, land tax | At the household or hut | A collector NPC walks to the household and asks; they can pay, hide goods, refuse or flee | later (Village age) |

- **Why taxes sit where enforcement is.** Between NPCs, exchange is never atomic (each hand-over is separate; see [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)). Paying wages through the paymaster guarantees the worker is paid (the employer hands over first), and an escrow market makes a swap safe. The tax or fee is the price of that safety. Whether employers and traders use them is their choice, so the trade-off between the fee and the risk of being cheated is real and measurable. A designated market square adds a board and visibility only, and takes no fee.
- **Taxing state wages is not a tax.** A state wage with a "wage tax" is just a lower wage. The command refuses a wage tax on the state's own contracts (`no_self_tax`), so the HUD cannot present a pay cut as a tax.
- **Rounding.** Taxes are integer goods. Each payer keeps a per-faction remainder in permille (`TaxCarry`). The tax on a payment is `floor((amount * rate + carry) / 1000)`, and the remainder carries to the next payment. Over many payments the total is exact to within one unit.
- **Head and land taxes (later).** They need a collector institution with staff, a register of households (an institutional record, layer 4) and enforcement. Collectors keep their own motives: they can skim, take bribes or pick easy households. See [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#failure-modes).

### Wages, purchase offers and public works
- **A wage is a posting term.** Every job the state offers is a `JobPosting` (ECONOMY_DESIGN) with a pay ware, a count and a basis (per load, shift, unit or task), plus its `StateContract`. The state chooses the pay; nobody has to accept it.
- **Escrow.** Contracts are escrowed by default: when a contract is posted, its full pay is committed from the pay-point HQ (`Escrowed = true`) unless the poster chooses an unescrowed contract. Unescrowed contracts let a poor state hire on credit, at the risk of default.
- **Paying.** The worker walks to the pay point (the paymaster: the HQ store under the state's rules), the pay is handed over (a `WagePaid` event, animated as a hand-over), and the commitment is released.
- **Arrears.** If the goods are missing when a worker comes to be paid, a `Debt` is recorded and `WageUnpaid` is emitted (the event name ECONOMY_DESIGN uses). The worker's social memory records a broken promise by the state (the state is a pseudo-agent in social memory, see SOCIAL_MEMORY_DESIGN). The debt stays owed until paid or written off (`WriteOffDebt`, which is a second, explicit broken promise).
- **Purchase offers.** A standing offer: "this HQ buys up to `MaxCount` of ware `W` at price `P` per unit until tick `Expires`". Any NPC (or private workshop) that brings the ware and hands it over is paid on the spot, less sale tax. This is how the state buys bread, gear or logs without owning the workshop. It replaces input priorities for privately owned producers.
- **Public works.** A public work is a site whose building will be state or communal property (roads, huts, a granary, a well) plus its contracts. Public works paid in food are also a welfare tool: they put food where there is labour. Nothing else is special about them.
- **Matching.** Contracts are posted on the HQ notice board. NPCs learn of them by visiting or being told. An NPC takes a free slot with a `TakeJob` intent; if two try in the same tick, the lower `NpcId` wins (ascending order of intent application). The state never assigns a person.

### Designating sites
1. **Designate.** `DesignateSite(kind, node)`, `DesignateRoad(nodes)` or `DesignateHq(corner)`. Territory rules are today's (`PlaceBuilding`, `BuildRoad`, `PlaceHq` validation). The result is a site with `Need` set and nothing committed.
2. **Commit.** `CommitGoods(site, hq, bundle)` reserves goods at an HQ for that site. Committing more than `Need` is refused.
3. **Post contracts.** In M6 the haul planner of [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) posts haul jobs for committed goods and a build job (up to 3 builders) at default pay as soon as goods are committed. The state can change the pay (`ChangeContractPay`), withdraw them, or post its own with `PostContract`. A haul job pays per load delivered; a build job pays per shift or on completion.
4. **NPCs do it.** Haulers carry goods to the site (`Delivered` rises); builders add work units while all `Need` is delivered (`WorkDone` rises at the builder's skill rate). The building completes when `WorkDone >= WorkNeeded`.
5. **Cancel.** `CancelSite` returns committed, undelivered goods to free. Delivered goods stay on the site as an ownerless-or-state pile (by the property rule) and can be hauled back by a new contract, or taken by anyone if no rule or guard stops them.

- **Roads.** A designated road is a planned road. It becomes a built road segment by segment, each segment needing a small build contract (no goods in stage 4). Walking is faster only on built segments. This changes today's instant roads.
- **HQ sites.** Same as a building site. The first HQ of each faction exists at the start, with the start stock and the start population (see POPULATION).
- **Upgrades and Citadels.** `DesignateUpgrade(building)` and `DesignateCitadel(hq)` create an upgrade site on the existing building, with today's costs, handled exactly as a site.
- **Demolition.** `DesignateDemolition(building)` posts a demolition contract. Until it is done the building stands. (Instant removal survives only in stage 3 mode, `Population` off.)

### Welfare basics: the granary and handouts
- **The granary** is a state-owned store building for food. It is filled by haul contracts and purchase offers like any store.
- **Handouts** follow the faction's `HandoutRule`: up to `RationPerDay` per eligible NPC per day, up to `MaxPerDay` in total.
- **Someone must hand it out.** A handout needs a keeper on a Staff job (a shift posting, ECONOMY_DESIGN) at the granary. The keeper checks eligibility from what it can see (a citizen it recognises; a `NeedFood` gesture; a worker it knows). In M6 the keeper uses the scripted honest policy; from M12 the shared store is an institution and its keeper and manager keep their own motives (favouring kin, skimming). See [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md).
- **What it changes.** Handouts put a floor under hunger. They also lower the food value of a wage, so the supply of labour can fall. Both are measured, neither is assumed.

### Recruitment
Enlistment offers, soldiers' pay and rations, the quartermaster's store and posted assignments are state commands owned by [MILITARY_DESIGN.md](MILITARY_DESIGN.md). They use the same `StateContract` record (`Kind = Service`) and the same treasury rules (escrow, pay points, arrears).

### Laws v1
- **A law is a published rule with a fine.** `EnactLaw(kind, fine, scope)` posts the law on every HQ notice board. Repeal works the same way.
- **What a law changes in an NPC.** Nothing, directly. An NPC that knows of the law (it saw the posting or was told) adds an expected cost to the intents the law covers: `expected cost = fine value × believed chance of being caught`. The believed chance is a belief built from its own experience and what it has seen and heard (thefts that went unpunished, guards seen nearby). Honest NPCs also have their guilt term (POPULATION, the honesty trait).
- **Enforcement is what makes it bite.** A fine is levied only by an enforcing NPC who saw the act or received a report and can reach the offender:
  - M9–M11: a soldier on a `Guard` assignment (MILITARY) may act on crimes it witnesses at its post.
  - M12: the guards institution (INSTITUTIONAL_STATE_DESIGN).
  - The offender can pay, refuse, flee or fight. A refused fine is a new event the guard can escalate (seize goods from its carry, or fight).
- **Laws without guards are signals.** With no enforcer, the believed chance of being caught decays towards zero as NPCs see unpunished acts, and the law's effect fades to whatever norms and guilt provide. That is a measurable prediction, not a designed outcome (test S-LAW-1).
- **Property rules** say who may take from which store (`PropertyRule`). They decide what counts as theft. Taking from a communal store as a member is not theft; taking from a private pile is. Wild food can be free or belong to the first possessor (the resident and intruder convention, SELFISH_GENE_NOTES A4).
- **Fines go to the treasury** at the guard's HQ (a physical hand-over), unless the law says the victim is compensated first.
- **Unjust fines.** A guard can fine the wrong NPC (misrecognition, or corruption later). The fined NPC's loyalty and trust in the state fall sharply (the `treatment` term below).

### Loyalty, migration and defection
**Citizenship.** Each NPC is a citizen of one faction. Citizens may take that faction's contracts, draw on its handouts by the rule, and are protected by its guards. Immigrants become citizens when accepted (below).

**Loyalty is rebuilt each day from the NPC's own experience and beliefs.** It never reads the true state of any faction.

Each term is a value in −1000..1000, computed on the day boundary from the NPC's own records:

| Term | From | Source layer |
|---|---|---|
| `food` | days of the last 5 with hunger below the comfort band; handouts received | own body (2) |
| `shelter` | nights of the last 5 slept in a bed | own body (2) |
| `pay` | state contracts paid on time minus defaults, last 10 days (escrowed pay counts once taken) | own memory (2) |
| `tax` | share of its own income taken in tax, last 10 days | own memory (2) |
| `safety` | attacks suffered or seen at home, crimes against it, and whether a guard acted, last 10 days | own memory (2) |
| `treatment` | fines (just or unjust as it believes), handouts refused, kindness from state agents | own memory (2) |
| `social` | mean loyalty and citizenship of its household and its 5 strongest relationships | social memory (3) |
| `legitimacy` | its `LegitimacyBelief.Total` for this faction (from M10; 0 before) | beliefs (6), owned by [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) |
| `outside` | its best belief about another faction's treatment of citizens, from rumour, migrants and envoys, weighted by confidence | beliefs (3, 5, 6) |

```text
experience = (Wf*food + Ws*shelter + Wp*pay + Wt*tax + Wsa*safety + Wtr*treatment + Wso*social + Wl*legitimacy) / Wsum
target     = 500 + experience / 2                       // 0..1000
rate       = BaseRate * (1000 - loyaltyTrait*4) / 1000 // a loyal personality changes slowly (trait −100..100)
Loyalty   += (target - Loyalty) * rate / 1000           // integer, toward target
```

- **Legitimacy and loyalty can diverge.** Legitimacy is whether the NPC thinks the rule is rightful; loyalty is whether it stays and serves. Legitimacy is one of eight terms in `experience`, so a well-fed NPC can stay loyal to a state it thinks unjust.
- **Weights** live on `SimTuning.State` (defaults: food 3, shelter 2, pay 3, tax 2, safety 2, treatment 2, social 2, legitimacy 2). They are tuning, not truths about people; experiments vary them.
- **The `outside` term is not in loyalty.** It enters the *migration* decision, so a well-treated NPC can stay loyal while knowing the neighbours pay more.
- **Migration is an NPC intent.** `MigrateTo(faction f)` is a candidate in the NPC's decider when it believes f exists and where its HQ is. Its score is roughly: believed gain (`outside` for f minus its own `experience`) minus cost (walking time, goods it cannot carry, household and friends left behind, kin weight) minus loyalty. The NPC walks to f's HQ with what it carries, and gestures `AskJoin`.
- **Households move together or split.** Each member decides for itself; a household decision rule is in [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md).
- **Immigration policy.** The receiving state's `ImmigrationPolicy` decides the answer to `AskJoin`:
  - `Open`: accepted on arrival, up to `MaxPerDay`.
  - `AskToJoin`: accepted if the skill rule passes, up to `MaxPerDay`.
  - `Closed`: refused. A refused NPC remembers the refusal and decides again (go home, try elsewhere, stay as a non-citizen).
  - In stage 4 the answer is given by the HQ as a rule, at the moment of the gesture. From the Village age an official (an institution staff member) gives it and can be bribed.
  - A `WelcomeGift` is handed over on acceptance, from the treasury.
- **Leaving.** Stage 4 has no exit controls. Stopping citizens from leaving needs guards at borders and a law, which is a later stage.
- **Defection** is the soldier's version of migration, in the field: see [MILITARY_DESIGN.md](MILITARY_DESIGN.md#desertion-surrender-and-defection). `AcceptDefectors` governs the answer.
- **Capture.** When an HQ is captured, its residents stay and become the conqueror's citizens, with loyalty reset to `CapturedLoyalty` (default 200). Owed state debts to them are void, which they remember.
- **Non-citizens.** An NPC whose request was refused, or whose faction was eliminated, is stateless: it can forage, barter and work private jobs but cannot take state contracts or handouts.

### Victory conditions
```csharp
public interface IVictoryCondition
{
    string Name { get; }
    // Called once per day (VP and last-standing are also checked every tick, as now).
    // Reads true state through VictoryInputs: these are rules of the game, not policies.
    VictoryResult Evaluate(in VictoryInputs inputs);   // None, or (winner faction, reason)
}
```
- `GameSetup.Victory` is a list. The game ends when any condition returns a winner.
- **Sandbox (default for stage 4 balance):** never ends. Factions can still be eliminated. The run stops when the scenario's length is reached.
- **VictoryPoints10:** today's rule (`FactionTuning.VictoryPoints`, 1 per HQ, 2 per Citadel; most VP wins, ties to lower id).
- **LastStanding:** today's rule.
- **Later:** `Prosperity` (median nutrition and shelter of citizens over a year above a bar), `Population` (citizens), `EconomicDevelopment` (output per adult). Each reads measured quantities from the snapshots and declares its definition in [KPI_CATALOGUE.md](KPI_CATALOGUE.md).
- **Neutrality.** A victory condition rewards the *faction*. It never feeds into an NPC's reward, and an institution never sees it. The faction AI may choose policies to meet it; that is its job.

### The state's command set
Every command is a `FactionCommand`, validated by `Check` and applied by `Submit`. The HUD and `IFactionPolicy` use only these. Commands marked (M) are owned by MILITARY_DESIGN and (I) by INSTITUTIONAL_STATE_DESIGN.

| Group | Commands |
|---|---|
| Sites | `DesignateSite`, `DesignateRoad`, `DesignateHq`, `DesignateUpgrade`, `DesignateCitadel`, `DesignateDemolition`, `CancelSite` |
| Places | `PlaceBoard` (a job board at a flag), `DesignateMarket` (a market square: a board and visibility, no enforcement; see ECONOMY_DESIGN) |
| Treasury | `CommitGoods`, `ReleaseGoods`, `WriteOffDebt` |
| Contracts | `PostContract`, `WithdrawContract`, `ChangeContractPay` (new takers only) |
| Purchases | `SetPurchaseOffer`, `WithdrawPurchaseOffer` |
| Taxes | `SetTaxRate(kind, permille)` |
| Welfare | `SetHandoutRule` |
| Laws | `EnactLaw`, `RepealLaw`, `SetPropertyRule` |
| Citizenship | `SetImmigrationPolicy` |
| Military (M) | `SetEnlistmentTerms`, `PostAssignment`, `WithdrawAssignment`, `SetBounty`, `ReleasePrisoner` |
| Institutions (I) | `FoundInstitution`, `SetInstitutionBudget`, `SetDirective`, `AppointManager`, `OrderInspection` |
| Diplomacy (existing) | `DeclareWar`, `OfferPeace`, `OfferTrade`, `AnswerTrade` |
| Later | `AnswerPetition` and concessions ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md), stage 5), `SetEmigrationRule`, head and land tax collection |

- **Reads:** only `FactionKnowledgeView`. In stage 4 it is filled by `FullInformation` (near-complete), but the HUD and AI code paths go through it.
- **HUD macros.** "Build a sawmill here" in the HUD sends `DesignateSite` and `CommitGoods`; the haul planner posts the jobs at default pay (`SimTuning.State.DefaultPay`), and the confirm card's pay slider adds a `ChangeContractPay`. The AI may use the same macro helper, which lives in `Sim/Policy` so both share it. A macro is only a helper: each command is checked and can fail on its own.
- **A test** checks that the set of command types the HUD can send equals the set an `IFactionPolicy` can return (section 10).

## 5. Interactions with other systems
- **Economy:** jobs, hand-overs, ownership records and enterprises ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)). The state is one owner among many.
- **Cognition:** postings are perceived at notice boards; laws and contracts become beliefs ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).
- **Social memory:** the state as a pseudo-agent with trust; defaults and unjust fines are betrayals ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Learning:** NPCs learn the value of state jobs and the risk of crime from consequences ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)). The state cannot install habits.
- **Military:** service contracts, assignments as enforcers of law ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
- **Institutions:** the granary becomes the shared store institution, and law enforcement becomes the guards institution in M12 ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **Classes and politics:** taxes, wages and property rules change who gains from what; measured in [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md) and [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md).
- **Factions:** wars, peace, capture and VP stay in `Sim/Factions`; victory moves to `IVictoryCondition`.

## 6. Player and view
- **State panel** (replaces the stock bar's role as the main readout), with tabs:
  - **Treasury:** stock per HQ, free vs committed, owed debts, today's flows by cause.
  - **Contracts:** the board: open, taken, overdue, defaulted; time since posted; pay. Unfilled contracts are highlighted with their age.
  - **Taxes and purchases:** rates; standing offers with bought/max.
  - **Welfare:** the handout rule, today's handouts, the keeper's shift status.
  - **Laws:** enacted laws, known crimes, fines levied and refused.
  - **Citizens:** count, immigrants and emigrants per day, loyalty distribution (10 bins), the commonest low-loyalty reasons.
  - **Victory:** progress of each active condition (hidden in sandbox).
- **Placing a building** keeps today's ghost, then shows a confirm card: cost to commit (from which HQ), default haul and build pay, and a slider for pay. Confirm sends the macro.
- **Sites on the map** show delivered/needed and work done, and a "no takers" marker when a contract has been open longer than `SimTuning.State.StaleContractDays`.
- **The NPC inspector** gains a citizenship line: faction, loyalty, and yesterday's loyalty terms (its own view, as POPULATION requires).
- **Animations only show events:** wage hand-overs, purchases delivered, tax taken at the counter, handouts, fines, `AskJoin` at the HQ, migrants walking with their goods.
- **The HUD never reads `GodState`.** It reads `FactionKnowledgeView` and the event stream. The faction's own books (treasury, contracts, sites) are always exact in the view; other figures follow [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) (`FullInformation` in stage 4).

## 7. Telemetry
**Domain events** (each with tick and ids; names follow conventions section 8):
- `SiteDesignated`, `SiteCancelled`, `GoodsCommitted`, `GoodsReleased`, `SiteDelivered`, `SiteWorked`, `SiteCompleted`;
- `ContractPosted`, `JobTaken`, `ContractCompleted`, `ContractAbandoned`, `ContractWithdrawn`;
- `WagePaid`, `WageUnpaid`, `DebtWrittenOff`, `ArrearsPaid`;
- `PurchaseMade`, `TaxCollected` (kind, payer, ware, count);
- `HandoutGiven`, `HandoutRefused`;
- `LawEnacted`, `LawRepealed`, `CrimeCommitted` (true event), `CrimeObserved` (by whom), `FineLevied`, `FineRefused`;
- `CitizenshipChanged` (reason: immigrated, migrated, defected, captured, stateless), `MigrationStarted`, `ImmigrationRefused`;
- `PolicyChanged` (any tax, handout, law, immigration or purchase change);
- `GameOver` with the condition's name.

**Daily faction snapshot fields:** treasury per ware (free, committed), owed debt, wages paid, defaults, taxes by kind, purchases, open contracts by kind, mean and 90th percentile days to fill, sites open and completed, handouts, crimes (true), crimes known to the state, fines levied, citizens, immigrants, emigrants, loyalty mean and deciles.

**Daily person snapshot fields** (added to the population snapshot): faction, loyalty, the nine loyalty terms, state debt owed to it.

## 8. Stage 4 minimum vs later

| Milestone | This document delivers |
|---|---|
| M0 | `IVictoryCondition` with `VictoryPoints10`, `LastStanding`, `Sandbox`; `GameSetup.Victory` list; `SimTuning.State` |
| M1 | The command set's interface types; `FactionKnowledgeView` read path for the HUD and `AiBrain` wrapper |
| M6 | Treasury ledger, sale tax, contracts (haul, build, gather, staff, supply), escrow and default, purchase offers, sites for buildings, roads and HQs, upgrades and Citadels as sites, granary and handouts with a keeper, the HUD state panel (first version) |
| M8 | Wage tax at the paymaster for private wages paid through it (needs private employers); laws v1 (`EnactLaw`, `SetPropertyRule`, fines) with theft and witnesses |
| M9 | Service contracts (MILITARY); soldiers on guard can levy fines |
| M10 | Loyalty from experience, migration, immigration policy, defection answers, capture resets, stateless NPCs |
| M12 | Laws enforced by the guards institution; the granary as the shared store institution |
| M14 | Incentive faction AI over this command set; HUD rewrite |
| Later | Escrow markets and stall fees (an institution with staff); head and land taxes with collectors; exit controls; courts deciding fines; money; prosperity, population and economic victory conditions |

## 9. Migration from the existing code

| Today | Stage 4 | When |
|---|---|---|
| `PlaceBuilding`, `PlaceHq` create a site that walkers fill for free from the nearest HQ | `DesignateSite`/`DesignateHq` plus `CommitGoods` plus `PostContract`; the HUD and AI use the macro | M6 |
| `BuildRoad` is instant | `DesignateRoad`; segments built by contract | M6 |
| `SetInputPriority` (Stop/Low/Norm/High per input ware) on state workshops | Kept with the same name in M6 as a state command: it sets the pay premium of the haul planner's postings for that input (Low 1, Normal 2, High 3 units of the pay ware per load; Stop posts nothing), as in [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md). For private producers the state uses purchase offers instead | M6 |
| `SetPaused` | Withdraw the building's Staff postings | M6 |
| `UpgradeBuilding`, `UpgradeToCitadel` | `DesignateUpgrade`, `DesignateCitadel` | M6 |
| `RemoveBuilding` | `DesignateDemolition` (instant removal stays behind the stage 3 flag) | M6 |
| `EconomyState.StockReserved` | Becomes `TreasuryLedger.Committed` | M6 |
| `FactionSystem` checks VP and last standing | Calls each `IVictoryCondition` | M0 |
| `GodHud` reads `GodState` directly (about 55 reads across the partial files) | Reads `FactionKnowledgeView` | M1 (path), M14 (rewrite) |
| `AiBrain.Priorities()`, `Build()`, `KeepRaws()` | Wrapped as `IFactionPolicy` in M1 with commands translated by macros; rewritten for incentives in M14 | M1, M14 |

- **The stage 3 game stays playable** with `GameSetup.Population = false` until M14, so its tests (economy, tutorial, AI balance) still run. With the flag on, `PlaceBuilding` and the other old commands are refused with `use_state_commands`.
- **Stage 3 economy tests are re-expressed:** "a woodcutter placed on a forest hex with a road produces logs within N ticks" becomes "a woodcutter designated with full commitment and default pay is built and staffed within N days on at least 95% of seeds", with N set from the M6 baseline.
- **Save version 3** (M3) carries `StateRecord`s. `StateHash` mixes every field in section 3 in id order.

## 10. Tests and acceptance criteria
All scenario tests run headless with `God.Runner` over the stated seeds. "Significant" means a two-sided Mann–Whitney test at p < 0.05 unless stated.

**Unit and invariant tests (every build):**
- **S-INV-1 Ledger balance:** on 10 seeds × 5 sim days, every faction's stock change equals the sum of logged flows, every tick, exactly.
- **S-INV-2 No magic:** a site with full commitment and no NPC able to reach it has `Delivered` and `WorkDone` unchanged for 10 days, exactly.
- **S-INV-3 Escrow:** with escrow on, no `WageUnpaid` is ever emitted (10 seeds × 10 days).
- **S-TAX-1 Rounding:** 1000 sale-tax payments of 1–9 units at 125 permille total within 1 unit of the exact sum.
- **S-TAX-2 No self-tax:** `SetTaxRate(Wage, …)` cannot reduce the state's own contract pay; the command refuses `no_self_tax`.
- **S-CMD-1 One command set:** the HUD's command types equal the set an `IFactionPolicy` can return (reflection over both).
- **S-CMD-2 No `GodState` in the HUD:** a reflection test over `God.Game.Hud` types finds no field, parameter or local of type `GodState`, `EconomyState` or `TerritoryState` outside the single view adapter.
- **S-VIC-1 Sandbox:** a 10-sim-year sandbox run on 5 seeds never emits `GameOver` while two factions remain.
- **S-VIC-2 VP unchanged:** stage 3 mode (`Population` off) with `VictoryPoints10` replays the stage 3 seeds 1–12 with identical end tick and winner.
- **S-DET-1:** two runs of the same seed and command log give the same `StateHash` at every day boundary, with recording on and off.

**Incentive scenarios (M6 onwards):**
- **S-PAY-1 Zero pay:** a build contract with zero pay and no advance finds no taker within 5 days on at least 95% of 20 seeds (default `AssociativeNpcPolicy` NPCs, food adequate).
- **S-PAY-2 Pay matters:** doubling a haul contract's pay lowers the median days-to-fill by at least 30% over 20 seeds (significant).
- **S-PAY-3 Default costs trust:** after one forced default to 5 workers, the next contract at the same pay takes at least 50% longer to fill on average over 20 seeds than in the control without default (significant).
- **S-SITE-1 Designate to done:** a sawmill designated with full commitment and default pay completes within 3 days on at least 95% of 20 seeds.
- **S-PUR-1 Purchase offers:** an offer of 2 grain per log at an HQ attracts at least one delivery within 2 days on at least 90% of 20 seeds when logs are reachable; an offer of 0 attracts none in 5 days on 100%.
- **S-WEL-1 Handouts:** in a famine scenario (wild food halved, farms burnt), a staffed granary with handouts lowers starvation deaths over 10 days by a significant margin vs no handouts, over 20 seeds. The change in labour supply (contract fill times) is reported, not targeted.
- **S-WEL-2 Unstaffed granary:** with no keeper, zero handouts are given over 5 days, exactly.

**Laws and loyalty (M9–M12):**
- **S-LAW-1 Laws without guards:** with a theft law and no enforcer, measured theft over days 5–15 is within ±15% of the no-law control on 20 seeds (no significant difference expected; reported either way).
- **S-LAW-2 Laws with guards:** with the same law and 3 staffed guards, measured theft is lower than no-law by a significant margin over 20 seeds. The size of the effect is reported, not tuned to a target.
- **S-LAW-3 Belief decay:** an NPC that sees 5 unpunished thefts lowers its believed chance of being caught by at least half (unit test on the belief update).
- **S-LOY-1 Formula:** unit tests show loyalty moves towards target, never leaves 0..1000, and changes more slowly for high loyalty-trait NPCs.
- **S-LOY-2 Pay default lowers loyalty:** 3 defaults in 5 days lower the affected NPCs' mean loyalty by at least 100 versus unaffected NPCs.
- **S-MIG-1 Voting with feet:** two factions in contact, identical except faction A's sale tax at 500 permille and B's at 0. Net migration from A to B over 20 days is positive on at least 16 of 20 seeds.
- **S-MIG-2 Closed border:** with `Closed`, zero NPCs become B's citizens by migration over 20 days, exactly.
- **S-MIG-3 Capture:** after an HQ capture, every resident's citizenship changes and its loyalty equals `CapturedLoyalty`.

## 11. Open questions
Owner questions (automatic HQ rules in stage 4: the paymaster and the answer to `AskJoin`) are collected in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#13-open-questions-owner). Settled here:
- **The unit of pay under barter.** The HUD shows contracts as bundles of goods. Reports may add a bundle's food-equivalent (FE) value from `WareRefValue` ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) owns it); that is a measurement index only, and no NPC reads it, so it creates no price.
- **The state in social memory.** Each NPC keeps one trust record for its faction's state as a pseudo-agent, beside its records of individual state agents (keepers, guards). [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) holds both.
- **Default escrow:** on, as in section 4; unescrowed contracts are a choice.
- **Demolition:** by contract from M6 when `Population` is on.
- **The legitimacy weight** is 2 of 18 by default (`SimTuning.State`); experiments vary it.
