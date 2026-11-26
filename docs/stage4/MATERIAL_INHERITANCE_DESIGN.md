# Material inheritance design (stage 4)

## 1. Purpose and scope

**This document owns** what happens to a person's property when they die, and the transfers of property between the living that stand in for it (gifts, bequests, dowry):
- estates: what goes into one, who settles it, how it is split, how long it may take;
- inheritance **customs** (cultural traits) and, later, inheritance **law** and tax;
- bequests, wills, gifts during life, dowry and bride-price as optional customs;
- disputes between heirs;
- ownerless property and claiming it;
- wealth across generations and how it is measured;
- the telemetry that makes ownership history a warehouse fact.

**It links to, and does not repeat:**
- the ownership records, items, workshops and enterprises themselves: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- the death itself, life stages and households: [POPULATION_DESIGN.md](POPULATION_DESIGN.md), [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md);
- how customs are acquired and spread (they are cultural traits): [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md);
- promises, obligations, gifts as memory: [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- the state's laws, courts and tax: [STATE_DESIGN.md](STATE_DESIGN.md), [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md);
- class measures built on wealth: [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md);
- the reference value per ware (`WareRefValue`, in `FE`) used to *measure* estates and wealth: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#33-reference-value-per-ware-warerefvalue-and-the-fe-unit). The split itself never reads it;
- the warehouse facts: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md);
- resident versus intruder contests: [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md) (A4 in [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md)).

**Strictly distinct from the other three kinds of inheritance.** Material inheritance moves *things*. It never moves genes ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)), learned associations ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)), or cultural traits. An heir who gets a workshop does not get the skill to run it. The customs that decide the split are cultural, but the estate itself is material.

## 2. Principles
1. **A death never destroys goods by accident.** Every unit of every ware leaves the dead person's name through an explicit, recorded transfer. Some customs destroy goods (grave goods) and that is recorded as a sink, never silently.
2. **Customs, not a built-in law.** Who gets what depends on the custom held by the NPC who settles the estate. No custom is preferred. Later, the state may impose a law, and that changes costs and authority, not the customs held.
3. **Settlement is quick and bounded.** An estate settles within 2 sim days. Disputes happen *after* settlement as ordinary conflicts, so no asset is ever frozen.
4. **Knowledge limits what can be claimed.** Heirs act on what they believe the dead owned. A remote cache that nobody knew of is found by others like any ownerless goods.
5. **Not forced to be fair or unfair.** The custom's split is applied. Whether it produces inequality, resentment or dynasties is measured.
6. **Deterministic.** Estates are processed in order of death tick, then ascending deceased id. Assets are processed in a fixed order. No random draws are needed for the split, only for the disputes that follow.

## 3. Data structures (`Sim/Estate`)

**Keys.** An estate is keyed by the deceased's `NpcId` (one death, one estate). No new id kind is needed; the warehouse uses its own surrogate key.

```csharp
enum EstateState : byte { Open, Settled, Escheated }       // Escheated: no heirs, went ownerless or communal
enum AssetKind : byte { Item, Building, Share, Enterprise, Tool, Livestock, Land, Claim, Debt }
enum Reason : byte { Custom, Will, Law, Tax, Burial, Communal, Ownerless, Escheat }

struct Estate {
  int  DeceasedId, OpenedTick, DueTick;      // DueTick = OpenedTick + SettleWithinTicks (default 2 × TicksPerDay)
  EstateState State;
  int  ExecutorId;                           // 0 if none
  ushort CustomTraitId;                      // the custom applied; 0 = no custom (goods become ownerless)
  byte HeirCount;  int[8] Heir;              // ordered by seniority
  byte AssetCount; EstateAsset[64] Asset;    // stacks are merged per ware and quality
  byte Flags;                                // had a will, disputed, taxed
}
struct EstateAsset {
  AssetKind Kind; int AssetId;               // ItemLotId, BuildingId, EnterpriseId, ...
  ushort Ware; ushort Qty;
  int  ToId; byte ToKind;                    // NpcId, HouseholdId, communal pool, treasury, none
  Reason Why; byte Status;                   // Pending, Transferred, Destroyed, Ownerless
}

struct Will {                                // owned by the testator while alive, up to 8 bequests
  byte Count;
  Bequest[8] B;                              // { AssetKind, AssetId or class (all movables), HeirId, Condition }
  int[3] Witness;                            // NpcIds who heard it
}

struct EstateNpcHeader {                     // per NPC, 20 bytes
  Advance[4] FromParents;                    // { ushort Ware; ushort Qty } gifts from parents, merged per ware, saturating,
                                             // for customs that count advancements (largest 4 wares kept)
  byte  WillCount; byte Flags;
}
```

**Bounds:** at most 64 open estates at once (a plague would defer the rest by one day; they remain ordered by death tick). At most 64 assets per estate. A person's holdings are bounded elsewhere (carry capacity, 24 item stacks in stores, 8 buildings, 2 enterprises, 16 tools), so the cap is not reached in normal play. Anything over the cap becomes ownerless with an `Ownerless` transfer event.

**Which assets can pass on.**

| Asset | Passes by custom? | Notes |
|---|---|---|
| Carried items and the personal share of a hut store | Yes | M4 |
| Sleeping right in a communal hut | No: it lapses to the hut group | A right, not property. Heirs may be admitted by the group's choice. |
| Privately owned hut or building | Yes | From M6 ownership records |
| Share of a communal building or store | No: lapses to the community | Unless the custom says "household keeps it" |
| Workshop, enterprise, productive tools | Yes | M8. Employees' contracts end. The new owner chooses whom to employ. |
| Cooperative membership | No: the cooperative's rules apply (share redeemed or transferred) | [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) |
| State-issued kit or rations | No: returns to the quartermaster's store or treasury | Not the dead person's property |
| Livestock, land claims | Yes, when they exist | Later stages |
| Claims owed to the dead | Not transferred. See debts below. | Memories only |
| Debts the dead owed | Not transferred. See debts below. | Memories only |

## 4. Mechanics and algorithms

### 4.1 Death to settlement (the order, all deterministic)
1. `Population` emits `NpcDied`. `Estate` opens an `Estate` record on the same tick and emits `EstateOpened` (the only emitter of that event; the heir candidates come from the believed kin graph of [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#inheritance-hooks)). All assets in the dead person's name (from ownership records, or from the carry and personal store before M6) are listed in fixed order: buildings by ascending `BuildingId`, enterprises, tools, item lots by ascending `ItemLotId`.
2. Assets are frozen: no one can take them for the first **half day** (`MournTicks`, default 600), except by ordinary theft. It is a custom-neutral delay for the household to gather, not a rule about mourning. It can be set to 0.
3. At `OpenedTick + MournTicks`, the executor is chosen (4.2) and the custom is read (4.3).
4. The split runs (4.4), in one tick, writing transfers and emitting `EstateTransfer` for each asset.
5. Dispute detection (4.7) runs. Then `EstateSettled` is emitted and the state is `Settled` or `Escheated`.
6. If anything fails to complete before `DueTick`, the remaining assets become ownerless on that tick (`Reason.Ownerless`). Nothing stays open past two days.

### 4.2 The executor
The executor is the surviving NPC best placed to act, found by this fixed ladder (ascending id inside each rung):
1. a person named in the will as executor, if known to be alive;
2. the partner, if adult and alive;
3. the eldest adult acknowledged child;
4. the eldest adult in the same household;
5. a person the elders or institution appoint (M12);
6. nobody: no executor, no custom, goods become ownerless.

This ladder is the one fixed rule of the module and exists only so that someone applies a custom. It is a choice of *who decides*, not of *what is decided*.

**Owner decision Q13 (27 Nov): the ladder stays fixed in Stage 4, but its rules are configurable and the architecture is open.**
- **Configurable:** the rungs and their order are data, `SimTuning.Estate.ExecutorLadder` (a list of rung kinds: `NamedInWill`, `Partner`, `EldestAdultChild`, `EldestHouseholdAdult`, `Appointed`, `None`), with the order above as the default. Experiments may reorder or drop rungs; the scenario records which ladder it used.
- **Open architecture:** settlement goes through one seam, `IEstateSettlement` (choose executor → read rule → split → transfer), so that later stages can add **wills** with wider force, **household decisions** (the household agrees a split by gesture), **communal inheritance** (assets pass to a group, as `BackToCommunity` already does), **state inheritance laws** and **estate tax** (4.9) as further implementations, without changing the transfer code.
- **Actual assets are transferred, never duplicated.** Every transfer moves the existing `ItemLotId` or asset record to its new owner (or marks it destroyed, taxed or ownerless); no settlement path creates a copy. Conservation and single ownership are invariants (tests 1–3 and 20).

**Owner decision Q14 (27 Nov): no estate skimming in the first Stage 4 estate milestone (M4).** The ability to add it later is preserved: dishonesty, corruption, disputed inheritance and restitution are added as another case of **exploiting an institutional position** (as for institution staff, [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#failure-modes)), with consequences that come from **observation, reputation and enforcement**: heirs and witnesses who see or are told of a shortfall, gossip, disputes (4.7), and later mediation, courts and restitution. The hook is that the split step (4.4) is a decision point of the executor, so a later executor policy can choose to favour itself; in Stage 4 the split follows the custom exactly.

### 4.3 Customs as cultural traits
The `InheritanceCustom` locus ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md#31-trait-definitions-content-read-only-in-simtuningculturedefs)) holds competing variants. The executor's **active variant** (strongest, strength 128 or more) is the custom applied. Ties go to the lower trait id. **Before M11** the custom is a fixed per-faction default read through a `StaticCultureProvider` (scenario setting); from M11 it is the NPC's own trait. A custom has an optional `Param`: the lineage filter (bit 0: only through the mother's line; bit 1: only the father's; none set: either) and seniority order (bit 2: youngest first).

| Custom | Rule |
|---|---|
| **EqualChildren** | Movable goods are split equally among acknowledged living children (minors included). Indivisible assets go to the heir with the lowest value received so far, ties to the eldest. The partner keeps the hut, and the right to live in it, until their death. |
| **EldestChild** | Buildings, enterprises and productive tools go to the eldest acknowledged child. Movable goods are split equally. |
| **PartnerFirst** | Everything to the living partner. If none, as `EqualChildren`. When the partner dies, their estate (now including the first) is settled again. |
| **HouseholdKeeps** | All assets become jointly owned by the household of the dead (communal form: the group is the owner). |
| **BackToCommunity** | Movables and buildings pass to the faction's communal pool. |
| **BuriedWithDead** | A fraction of movable goods (default 50 percent, rounded down) is destroyed as a sink; the rest follows the NPC's second variant at the locus, or goes to `HouseholdKeeps` if none. |
| *(no custom)* | All assets become ownerless where they are, to be claimed. |

All are options with costs and benefits seen only through consequences. **None is the default for the sim.** A scenario chooses faction defaults until M11, and customs then spread and drift as cultural traits do. A customs mix inside a population is allowed and produces disputes.

**Acknowledged kin.** Heirs are the acknowledged kin: the deceased's believed kin with `Confidence` above 400 in the believed kin graph ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#inheritance-hooks)). An unacknowledged child (not believed kin by the deceased) does not inherit by custom. This follows what the deceased believed, which is a cultural and social fact, not the true pedigree.

**Minors.** A child can own assets. The household's eldest adult manages them in trust: ownership records name the child, and use is the household's until adulthood. If no household member survives, a minor's assets stay in its name and are held by the faction's communal pool as guardian.

### 4.4 The split algorithm
Heirs are ordered by seniority (birth tick ascending, ties by ascending id). Within the custom:
1. For each asset in the fixed order: if it is **stackable**, divide it among the heirs by quotient. The remainder goes one unit at a time to heirs in seniority order.
2. If it is **indivisible** (a hut, a workshop, one tool), give it to the heir with the lowest total awarded so far, **valued by the executor's own worth estimates** ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#45-exchange)), ties to seniority. With `EldestChild`, the eldest takes it.
3. **No reference table is read.** Stackables split by units, and indivisible assets by the executor's own valuation, so two executors can split the same estate differently. `WareRefValue` is a measurement index only ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#33-reference-value-per-ware-warerefvalue-and-the-fe-unit)).
4. Customs that count lifetime gifts (`Param` bit 3, "advancements"): each heir's target share is reduced by its `FromParents` advancements, valued by the executor's own worth estimates; any heir whose reduced target would be below zero gets nothing further.
5. Taxes (later): the state's `EstateTaxPermille`, if above 0, takes that share of each stackable asset in kind to the treasury before the split; how an official values indivisible assets belongs to the later estate-tax design. It is 0 in stage 4.
6. Destroyed goods (the `BuriedWithDead` sink) are removed with `Reason.Burial` and logged per ware.

**Example (EqualChildren, three children A, B, C eldest to youngest):** 10 logs divide as 4, 3, 3 (stackables count only units here, so everyone is at zero for the indivisible rule). A hut goes to A (ties to the eldest). A tool then goes to B (the executor values A's hut at 100 and B's awards at 0).

### 4.5 Bequests, wills and gifts
- **A will is a small record held by the testator**, up to 8 bequests and 3 witnesses. Making one is an NPC intent (`Bequeath`) that the decider scores. It uses the `Promise` gesture to the heir and witnesses with `Time = later`. In social memory the heir holds an obligation of kind `Bequest` with `DueTick = NoDate` ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#obligations-and-promises)).
- **Honouring:** at settlement, the executor honours the will **only if it is known to them**: the executor is a witness or an heir who was told, or a witness reaches the executor within the mourning delay. An unknown will is ignored, and the custom applies. An heir who believed they were promised something that did not arrive may raise a dispute. A witness can fail to speak up (honesty trait), which is a decision, not a rule.
- **Wills are voluntary.** Most NPCs never make one. The custom applies to the rest.
- **Gifts during life** (`GiftGiven`, handled in [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)): from a parent to a child, the gift's ware and quantity are added to the child's `FromParents` advancements. It is ordinary property from that moment. A custom that counts advancements subtracts it from the share.
- **Dowry and bride-price** are optional customs of exchange at partnership (`PartnershipFormed`, from [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)): *dowry* is goods from the bride's household to the couple, *bride-price* from the groom's household to the bride's. Neither is required and neither is the default. As norms they add a score term to a `GiveGoods` intent by parent NPCs, and a partner's household may refuse an offer that does not match its own custom. Stage 4 provides the hooks only; the content is for later.

### 4.6 Ownerless property and claiming it
- **An ownerless asset** is one with no owner record: from a custom-less estate, an overflowing or expired estate, an abandoned hut or a found cache. It stays where it is and ages (food spoils, tools stay).
- **Claim** is an NPC intent. It succeeds by occupying or picking up the asset, which the sim validates like any pick-up (it must be near the asset, and the asset ownerless). The claimant becomes the owner (`EstateAssetClaimed` with the delay since it became ownerless).
- **Two claimants at once** (same tick, same place): the sim does not decide. It raises a civilian contest between them ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#31-the-contest-record): gestures such as `Defend`, `Warn` and `Surrender`, and `Duel` exchanges if either escalates). The winner takes the asset.
- **First arrival is a norm.** "First arrival owns" is a cultural trait. A holder respects an earlier claimant (a resident, in the book's terms) and sanctions those who do not. A non-holder may contest. The sim only records who claimed first and who contested. Which convention wins is measured, as in the resident-versus-intruder example (A4 in the research notes).
- **Knowledge matters.** A claimant must have perceived or believed in the asset. Hidden caches decay to ownerless and are found by whoever stumbles on them.

### 4.7 Disputes
A dispute is a consequence, not a state the estate gets stuck in.
- **Detection (deterministic):** after the split, any heir whose own active custom would have given them at least **25 percent more**, valued by the heir's own worth estimates, than they received (or who holds a belief of a bequest that did not arrive) triggers `EstateDisputed` for that heir. If everyone holds the same custom as the executor and no unknown bequest exists, no dispute is raised.
- **Effects on minds:** the aggrieved heir's Anger rises and its relation with the executor gets a grudge ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)), as for a betrayal of severity `min(255, shortfall/2)`.
- **What an NPC may do next (all decider choices):** accept; `Accuse` the executor; ask elders for mediation; take the asset from its new owner (theft, with witnesses, and a contest if challenged); leave the household; fight.
- **Game theory:** contested indivisible assets are Hawk-Dove-like contests between heirs: value of the asset against injury cost, with the resident (the heir who holds it) advantaged by the first-arrival norm where it is held, and the pecking order from earlier contests ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)). The contest mechanic is [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#31-the-contest-record)'s. Nothing forces Hawk or Dove.
- **Mediation by elders (M12):** the Band-age mediation institution ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)) lets an heir ask a respected elder (an NPC with its own custom, honesty and `Esteem`) to rule. The ruling is a gesture message and a social fact. It is obeyed by those who respect the elder and ignored by others. An elder may favour kin. Later, courts decide with enforcement, and staff keep their own motives.
- `EstateDisputeResolved` records how it ended (accepted, force, mediation, withdrawal, court).

### 4.8 Debts and claims
The sim has no hard ledger of private debt. Debts are obligations in memories ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Claims owed to the dead.** The debtor's record of the obligation becomes `Orphaned` when the creditor dies. If the debtor is honest and the custom ("heirs are owed what the dead were owed") is held, it may pay the heir. Otherwise the debt dies with the creditor, as it often did. An heir who knows of the debt can `Want` or `Accuse` at the debtor to re-open it.
- **Debts the dead owed.** The creditor's memory marks the obligation `Orphaned`. It may ask the heirs. Whether the heirs accept responsibility is a norm they hold (`InheritDebts`, a cultural trait), a decision in context, and a matter of the creditor's `Esteem` and their relation. No rule transfers a debt.
- **With courts (Stage 6):** a registered obligation (`ContractId`) can be claimed against an estate by the court, and creditors are paid before heirs by law. That is a legal rule of the institution, not part of memory.

### 4.9 Inheritance law and tax (later)
- A **law** is set by the state (policy lever, Stage 5-6): `InheritanceLaw` ∈ {none (customs apply), equal split, eldest child, partner first, escheat to the state}, with an `EstateTaxPermille`.
- A law changes the executor's **expected cost** of following another custom (a fine or loss of standing, believed). It does not rewrite the customs that NPCs hold, which only change by cultural transmission.
- An official (registrar or court staff) may apply the law at settlement and may be slow, corrupt or short-staffed ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#failure-modes)).
- Tax is collected in kind from each estate into the treasury and is a `TaxCollected` event with reason `Estate`.

### 4.10 Wealth across generations
- **Net worth** of an NPC is the `FE` value (`WareRefValue`) of everything it owns, by ownership records, and is also kept per ware. It is computed by the recorder and the HUD, outside the sim. So is the inherited total of each NPC, from its inbound `EstateTransfer` events.
- **Provenance:** every transfer is stamped (`earned` for production and wages, `inherited`, `gifted`, `found`, `claimed`, `state`). The warehouse can split wealth into earned and inherited shares.
- **Measures** (computed outside the sim; the class measures are in [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)):
  - wealth Gini by faction and by generation;
  - parent-child wealth correlation: the rank of the parent's net worth at death against the child's net worth one sim year after settlement;
  - inherited share of total wealth per faction;
  - sibling inequality within families, by custom;
  - estate sizes, and the fraction of estates that included productive assets.
- Whether wealth concentrates, disperses or collapses is **a result, not a design**.

## 5. Interactions with other systems
- **Population:** `NpcDied` opens an estate. A child's inheritance depends on the household and on who is acknowledged. A migrating NPC takes its goods with it (they are not an estate).
- **Economy:** ownership records are the source of truth for the asset list and the target of transfers. `EstateTransfer` is the only writer of ownership changes for estates. [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) must refuse any other writer for a dead owner, enforced by an invariant (section 10).
- **Culture:** customs, `InheritDebts`, `FirstArrivalOwns`, dowry and bride-price norms are traits. Culture does not move goods.
- **Social memory:** wills are obligations of kind `Bequest`. Disputes make grudges. Orphaned obligations are re-opened by heirs.
- **State and Institutions:** laws and tax later, elders now (M12). The state cannot make heirs hold a custom, only change what following it costs.
- **Military:** state-issued kit returns to the quartermaster's store ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)). A soldier's own gear passes by custom.
- **Information:** estates are not public by default. A records office (later) writes them down as layer 4 records, with the usual risks of bias and delay.

## 6. Player and view
- **NPC inspector card:** the NPC's own beliefs: "believes it will leave its hut to its children"; for an heir, the assets it knows it received and what it believes it was promised. Not the true estate.
- **Event view:** a death marker at the place, goods shown moving to heirs (carry animation) as `EstateTransfer` events play. A clear "ownerless" marker (neutral colour) on unclaimed assets, and a small flag when an `EstateDisputed` happens.
- **Faction estates panel** (through `FactionKnowledgeView`): estates settled in the last 5 days, their custom, ownerless value, disputes. Stage 4 reads the full record. Later, an estate-recording institution limits it.
- The player cannot move an estate. The state's levers (law, tax) arrive in later stages. The view animates events and decides nothing.

## 7. Telemetry

**Events** (`Estate` emits these; `OwnershipChanged` stays for all non-estate transfers, and the warehouse unions the two). Events carry wares and quantities; the recorder adds `FE` values from `WareRefValue`:

| Event | Fields |
|---|---|
| `EstateOpened` | deceased, tick, executor, custom id, heir candidates, asset count |
| `EstateTransfer` | estate (deceased id), asset kind, asset id, ware, qty, from, to, reason, provenance |
| `EstateSettled` (in the conventions) | estate, tick, days taken, transfer count, ownerless count, destroyed and taxed wares |
| `EstateDisputed` | estate, claimant, executor, shortfall in the claimant's own worth units, basis (custom mismatch, unknown bequest) |
| `EstateDisputeResolved` | estate, claimant, how (accepted, force, mediation, withdrawn, court), days |
| `EstateAssetClaimed` | asset, claimant, days ownerless, contested flag |
| `WillMade`, `WillHonoured`, `WillIgnored` | testator, heirs, witnesses, reason |

**Snapshots:** daily person snapshot adds `OwnedAssetCount`; the recorder adds inherited and net worth in `FE` (outside the sim). Daily faction snapshot adds `OpenEstates`, `OwnerlessAssets`, `MedianOwnerlessAgeDays`; the recorder adds the ownerless value in `FE`.

**Ownership history is a key warehouse fact.** `fact_ownership` (spells: asset, owner, valid from and to, basis, provenance) is built from `EstateTransfer`, `OwnershipChanged` and `GiftGiven`, and `fact_inheritance` holds one line per estate transfer (estate, from, to, reason, ware, qty, `value_fe`). The history of any asset can then be followed across owners and generations. Table definitions and the event-to-table map are in [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#11-the-published-table-list).

## 8. Stage 4 minimum versus later

| Milestone | Included |
|---|---|
| **M4 (basic custom-based estates)** | The `Estate` module with carried items and the personal share of a hut store. Executor ladder. A fixed per-faction default custom through `StaticCultureProvider`, with `EqualChildren`, `PartnerFirst`, `HouseholdKeeps`, `BackToCommunity`, `BuriedWithDead` and the no-custom case. Sleeping rights lapse. Ownerless goods and the claim intent. Settlement within 2 days. Events `EstateOpened`, `EstateTransfer`, `EstateSettled`. Invariants for conservation. |
| **M6** | Estates read ownership records (state, private, communal). Private huts and buildings pass by custom. |
| **M7** | Orphaned obligations: debts and claims behave as described (memory only). |
| **M8 (productive assets)** | Workshops, enterprises and tools pass by custom. `EldestChild` becomes meaningful. Cooperative membership rules. Theft by disputing heirs has witnesses. Wills and bequests (a `Bequest` obligation, witnesses). Gifts counted as advancements. |
| **M11** | Customs become cultural traits, spread and drift by transmission. `FirstArrivalOwns` and `InheritDebts` as norms. |
| **M12** | Elders' mediation of disputes. Appointed executors. Executor dishonesty (skimming) may be added here or later, as an exploit of an institutional position with consequences from observation, reputation and enforcement (Q14); it is not in M4. |
| **Later (Stage 5)** | Dowry and bride-price content; livestock; land claims; sleeping-right rules per hut group. |
| **Later (Stage 6)** | Inheritance law, estate tax, court enforcement, creditors paid first, registrars, records, restitution, as further `IEstateSettlement` implementations (Q13). |
| **Later (Stage 5+)** | Wills with wider force, household decisions on a split, communal inheritance beyond `BackToCommunity` (Q13). |

## 9. Migration from the existing code
- **Stage 3 has no NPC death and no ownership.** Buildings and stores belong to factions. Nothing is migrated.
- **M4** adds `Estate` beside `Population` (a handler on `NpcDied`).
- **M6** adds ownership records in `Economy`. `Estate` is rewired to read them. The M4 carry-based code becomes the first branch of the same listing.
- The invariant list (`Invariants`) gains: "no asset is owned by a dead NPC more than 3 days after death" and "estate transfers conserve goods".
- Save version 3 includes open estates and wills. `StateHash` covers `Estate` and the per-NPC header in id order.

### M4 as built (branch `unity/m4`, 26 Nov 2026)
Code: `Sim/Estate/Estates.cs`, `EstateTuning.cs`. Decisions taken:
- **Where the goods are** (WORLD_ONTOLOGY A2): at a death the carry moves into an estate bin at the death node (`EstateIn`, owner `Estate(d)`, never merged); household goods the person carried stay the household's. Goods the deceased owned in a household store pass to that household in place (HOUSEHOLDS: the store stays with the household).
- **Split** at `MournTicks` (600), through the seam `IEstateSettlement` (`CustomSettlement`): the executor by the configurable ladder (`Estate.Ladder`, rung digits 1-5, default 12345; no wills before M8, no appointments before M12), the custom by the executor's faction (`Estate.CustomF0..F5`, default `DefaultCustom` = EqualChildren; scenarios choose). Ownership passes in place (`SetOwner`). A stackable lot shared by several heirs is split by a `Move` into a share bin beside it (the moved part takes a new id, as every split does; units and each id's lineage are conserved). This is the only way to divide one lot: see the amendment to test 20 proposed in the M4 report.
- **Collection:** an heir standing within `Reach.Store` of the estate takes its own lots (`Withdraw`), a reflex at the place rather than a decision (no seam lets a mind perceive a bin). At `SettleWithinTicks` (2400) what is left goes to the heir's household store (O4 executor delivery, a temporary cross-node move, `Estate.Delivery`) or becomes ownerless on the ground at the death node; then every estate bin is removed.
- **BackToCommunity** owners are `Group(faction)`; with no communal store before M8 and M12 their goods go ownerless on the ground at the deadline. **BuriedWithDead** buries half of each stackable lot (`Consume(Buried)`) and the rest follows HouseholdKeeps (no second variant before M11).
- **Disputes** (4.7) compare each child heir's own faction custom with the executor's, valued by the executor's worth; the grudge `min(255, shortfall / 2) x 3` is carried by `EstateDisputed` for M5 to apply (M4 never writes social memory).
- **Not built in M4:** wills and bequests (M8 per section 8), dowry and bride-price hooks, `EstateAssetClaimed` (a claim is M3's first pick-up of an ownerless lot), the contest between simultaneous claimants (M5's `Contest`; its stub, the lower id, equals tick order).

## 10. Tests and acceptance criteria

All run in `dotnet test DotNet` and Unity EditMode. Numbers are exact unless a range is given.

**Settlement and conservation**
1. **Every death settles within 2 days.** A 300-NPC scenario run for 40 sim years (400 days at defaults): for every `NpcDied`, `EstateSettled.tick − NpcDied.tick ≤ 2 × TicksPerDay` (2400). The count of open estates after any day boundary older than 2 days is 0.
2. **Goods are conserved.** Property test over 1000 random estates (random asset sets, random heirs, each custom): for every ware, `Σ before = Σ after + destroyed + taxed`, with `destroyed` and `taxed` nonzero only under `BuriedWithDead` and a nonzero tax. No negative quantities. Each `ItemLotId` has exactly one owner.
3. **No leaks.** At the end of any run, no ownership record names a dead NPC older than 3 days (invariant, checked every day boundary). For every ware, the units in an estate equal the units transferred to heirs plus destroyed, taxed and ownerless units (exact).
4. **Determinism.** Same seed, two runs: identical `EstateTransfer` streams and `StateHash` at every day boundary. A run with recording off has the same hash as one with it on. Save and load with an open estate (and a pending will) gives an identical stream afterwards.

**Custom rules (unit tests, exact)**
5. `EqualChildren` with 3 children (A eldest) and 10 logs gives 4, 3, 3. One hut goes to A. A following tool goes to B (executor valuations stubbed: hut 100, tool 30). Total awarded matches the totals exactly.
6. `EldestChild`: all buildings, enterprises and tools go to A. Movables split 4, 3, 3.
7. `PartnerFirst`: with a partner, 100 percent to the partner. Without, as `EqualChildren`. After the partner's own death a second estate settles, and the total is conserved across both.
8. `HouseholdKeeps`: the owner becomes the household (communal); holdings are unchanged. `BackToCommunity`: the faction pool receives 100 percent.
9. `BuriedWithDead` with 10 logs and 50 percent: 5 destroyed (logged with `Reason.Burial`), 5 follow the second variant.
10. No custom: every asset becomes ownerless and appears in the ownerless list, with `OwnerlessAssets` equal to the asset count. A lineage filter on mothers' line excludes a child known only via the father's line.
11. An unacknowledged child inherits nothing by custom. A minor heir holds an asset in its name while its household manages use.

**Ownerless and claim**
12. An ownerless asset is claimed by the first NPC to arrive: the `EstateAssetClaimed` tick is the arrival tick. Two simultaneous claimants raise exactly one contest event and one winner.
13. Ownerless food rots on its normal schedule (the economy's decay), and its value leaves the ownerless list at the same tick.

**Wills and disputes**
14. A will known to the executor is honoured 100 percent. A will known only to a dead witness is ignored: the custom applies and `WillIgnored` is emitted with the reason.
15. With every executor and heir holding the same custom and no unknown bequest, `EstateDisputed` is raised in 0 of 1000 estates. With an executor holding `EldestChild` and 3 heirs holding `EqualChildren`, a dispute is raised for every non-eldest child whose shortfall is at least 25 percent, and for none whose shortfall is below.
16. A dispute raises the heir's Anger and creates a grudge of `min(255, shortfall/2) × 3` on its relation to the executor (exact).

**Measures (reported, not forced)**
17. In 20 seeds of a 5-generation scenario, report wealth Gini, parent-child rank correlation and inherited share by custom. Expected direction, not a pass condition: `EldestChild` gives a higher within-family inequality than `EqualChildren`. Baselines are recorded at the first M8 run and then act as regression bounds (±15 percent).
18. Provenance check (recorder): for every NPC, the inherited `FE` total equals the sum of its inbound `EstateTransfer` lines valued at `WareRefValue` (exact).
19. **The split reads no reference table:** a reflection test finds no reference to `WareRefValue` in `Sim/Estate`.

**Owner decisions Q13 and Q14**
20. **Transferred, never duplicated:** over 1,000 random estates for every custom and every ladder configuration, the multiset of `ItemLotId`s and asset ids after settlement equals the multiset before, minus those marked destroyed, taxed or ownerless; no id appears twice and no new id is created by settlement.
21. **Ladder configurable:** with `ExecutorLadder` reordered (household adult before partner), the executor chosen in a fixed fixture changes accordingly, and every other estate test still passes.
22. **No skimming in Stage 4:** across 1,000 random estates the executor's own award equals exactly what its custom gives it as an heir (zero if it is not an heir).

## 11. Open questions

Settled in review (26 Nov 2026): estates are keyed by the deceased's `NpcId`; acknowledged kin are believed kin with confidence above 400; the split uses units and the executor's own valuation, never `WareRefValue`; a sleeping right lapses to the hut group at death (as distinct from leaving a household, [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#forming-and-breaking-households)); `MournTicks` and `SettleWithinTicks` are not scaled by `LifeScale`, because settlement is work time, like walking.

No open questions remain. Owner decisions of 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):
1. **Q13 Executor ladder:** keep it fixed in Stage 4. The rules are configurable, and the architecture supports wills, household decisions, communal inheritance, state inheritance laws and estate tax later. Actual assets are transferred, never duplicated (4.2; tests 20, 21).
2. **Q14 Estate skimming:** not in the first Stage 4 estate milestone. The ability to add dishonesty, corruption, disputed inheritance and restitution later is preserved, as another case of exploiting an institutional position, with consequences from observation, reputation and enforcement (4.2; test 22).
