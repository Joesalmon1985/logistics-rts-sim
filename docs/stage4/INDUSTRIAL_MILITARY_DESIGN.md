# Industrial military design (owner D6, 27 Nov 2026)

Status: design, 27 Nov 2026. Owner decisions D6.1, D6.2, D6.4, D6.8, D6.14, D6.18, D6.19; D5.1, D5.2, D5.7, D5.8 and brief §23 of the first brief ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)). OpenRA-inspired **categories** only: names, stats and content are original. Persistent soldiers, service, morale, assignments and the `MilUnit` migration stay owned by [MILITARY_DESIGN.md](MILITARY_DESIGN.md); this document adds the industrial layer on top of it. Shared surface proposals are those of [INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md) ("IND") §9 and §12, provisional until the SAC Industry addendum.

**Fiction rule (D6.14).** Nuclear material, the strategic device and every weapon here are abstract game capabilities. No real-world weapon engineering, yields, chemistry or procedures are modelled or described; stats are small integers for gameplay.

---

## 1. Purpose and scope

**This document owns:**
- the **category set** (infantry, engineers, reconnaissance, armoured carriers, tanks, artillery, aircraft, naval, defensive structures) and their priority;
- **industrial infantry**: recruited, trained existing NPCs with manufactured equipment (never manufactured people);
- **military vehicles and equipment** as manufactured assets from the same factories as civilian goods (IND recipes, VEHICLES records);
- **crew, fuel, ammunition, maintenance and logistics** for military assets;
- **military production queues** as factory orders placed by military organisations, **procurement agreements**, **budgets**;
- **competition** with civilian organisations for labour, materials, fuel, capital and power;
- autonomous military organisations; the god's influence (instruction, compulsion, possession of operators);
- the **strategic device** (fictional nuclear capability) and its political, economic, environmental and demographic consequences; hooks for diplomacy, inspection, disarmament and deterrence.

**Linked, not repeated:** soldier service, enlistment, pay, morale, desertion, surrender, prisoners, `IMilitaryPolicy`, the M9 migration (MILITARY_DESIGN §2–§9); the duel rules (`Military/Duel.cs`, `DuelSim.cs`, unchanged); military organisations with a `Commander` office (ORGANISATIONS §3, MILITARY §12); vehicle movement, crews and bins (VEHICLES); recipes and orders (IND §3.6–3.7, §4.4); power (ELECTRICITY); contamination (WASTE §5.3); designs and licences for weapons (KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md); budgets in money (MONEY_AND_ACCOUNTING_DESIGN.md).

## 2. Decisions and principles

| Id | Decision | Source |
|---|---|---|
| IMIL-1 | **No separate military economy.** Military equipment is made by the IND recipe runner in ordinary factories, from ordinary materials, by ordinary workers, with power from ordinary grids, ordered by an organisation that must obtain everything the way any organisation does | D6.8, brief "Political economy" |
| IMIL-2 | **Infantry are people.** A soldier is an existing `Npc` who enlisted (MILITARY §4) and was trained; equipment is lent from the military organisation's store. Nothing spawns a soldier; the stage 3 barracks `UnitOrder` path stays only in stage 3 mode | D6.8, MILITARY §2 |
| IMIL-3 | **Vehicles are manufactured assets with crews.** A tank is a VEHICLES record of class `Tank`: it moves only with a living driver, fires only with a gunner aboard and ammunition in its bin, burns fuel, wears and needs maintenance | D6.4, D6.8 |
| IMIL-4 | **Combat rules are reused.** Vehicle and defence combat feed `Duel.Resolve` through a stats adapter; nothing in `Duel` changes, so the stage 3 kit tables stay bit-identical | MILITARY §2 ("combat rules are not touched") |
| IMIL-5 | **Military organisations decide autonomously.** Production orders, procurement, budgets and deployment are decisions of the military organisation's office-holders (and of the government that funds it), under their decision rules; the god only influences people | D5.1, D5.2, D5.29 |
| IMIL-6 | **Strategic capability has real consequences** in state (destruction, deaths, contamination, displacement, beliefs, institutional decisions, economic loss), measured, never scripted as an outcome | D6.14, D5.33 |
| IMIL-7 | **Priority.** First: infantry with firearms and ammunition, transports (armoured carriers), tanks, simple defences. Later: engineers, reconnaissance, artillery, aircraft, naval, the strategic device | D6.8, D6.18 |

## 3. Data structures (`Sim/Industry/Military`, proposed)

### 3.1 Categories

| Category | Representation | Made by | Crew / person | Needs | Tier |
|---|---|---|---|---|---|
| **Infantry** | enlisted NPCs (MILITARY `ServiceRecord`) with kit lots | people recruited, trained by drill (C7 education, MILITARY §12) | the soldier | firearm or stage 3 gear, ammunition, rations, pay | 2 (with industrial firearms), M9 for full service |
| **Engineers** | infantry with the `Engineer` role and skill | training | soldier | tools, machine parts | 3 |
| **Reconnaissance** | vehicle class `Scout` | IND recipe | driver | fuel | 3 |
| **Armoured carrier** (transport) | vehicle class `Carrier`, `PassengerSeats` 6 | IND recipe | driver (+1) | fuel, maintenance | 2 |
| **Tank** | vehicle class `Tank`, weapon mount | IND recipe | driver + gunner | fuel, ammunition, maintenance | 2 |
| **Artillery** | vehicle class `FieldGun` (towed by a carrier or self-propelled) | IND recipe | 2 | ammunition | 3 |
| **Aircraft** | vehicle class `Aircraft` (needs an airfield facility and an air movement layer) | later | 1–2 | fuel | later |
| **Naval** | vehicle class `Boat` (needs water nodes for movement) | later | 2+ | fuel | later |
| **Defensive structures** | C1 assets of kind `Defence` (wall segment, bunker, gun post) built at a construction site | NPC builders | gun post: 1 gunner | ammunition (gun post), power (later: lights, sensors) | 2 (wall, bunker, gun post) |
| **Strategic device** | a persistent asset of class `StrategicDevice` (4.7) | strategic facility (IND kind 11) | launch crew of 2 | nuclear fuel, parts, very large power, a design known at the site (a licence is the owner's own policy, R33) | 3 |

### 3.2 Weapons and stats (original, abstract)

```csharp
public sealed class WeaponMountDef                     // frozen table; a KNOWLEDGE design payload
{
    public readonly ushort Id; public readonly string Key;
    public readonly WeaponStats W;                     // the existing struct used by Duel (hit %, damage, reach, cooldown...)
    public readonly bool Ranged;
    public readonly Ware AmmoWare; public readonly byte AmmoPerAttack;   // Ammunition; 0 for melee
    public readonly bool VsStructure;                  // may strike buildings
}

public static class VehicleCombat                      // adapter: a crewed vehicle or a manned gun post as a Duel combatant
{
    public static SoldierStats Stats(MilitaryTuning tu, VehicleDef d, WeaponMountDef m, int condition, int crewSkill);
    // MaxHealth = d.Health, Armour = d.Armour, Speed from the vehicle step cost, Block/Dodge 0, StopsCrits = d.Armour ≥ tu.CritStopArmour.
    // SoldierStats has a Weapon (Ware) field; the adapter needs a constructor taking WeaponStats and Ranged directly
    // (the existing constructor already takes WeaponStats w; Ranged derives from the weapon ware today, so the adapter
    // passes a sentinel and Duel.HitChance reads a.Ranged: an additive SoldierStats field `RangedOverride`, by amendment to the Military owner).
}
```

- **Industrial firearm** (`Ware.Firearm` 53) is a new attack gear ware with its row in `MilitaryTuning.Of` (`Gear.SlotOf` → `Weapon`, ranged, uses 1 ammunition per attack). A soldier without ammunition fights with the firearm as a club (melee stats). Stage 3 gear and its duel tables are unchanged.
- **Defaults** (original): rifle-like firearm hit 55%, damage 14, reach long; tank gun hit 50%, damage 60, reach long, `VsStructure`; tank `Health` 600, `Armour` 18; carrier `Health` 300, `Armour` 10; gun post `Health` 400, `Armour` 14.

### 3.3 Military organisation, budget and procurement

The military organisation is C6's `OrgType.Military` (ORGANISATIONS §3.1), usually funded by a government organisation; a private or mercenary one is possible later. Industrial additions:

```csharp
public sealed class ProcurementAgreement              // an I3 SupplyAgreement specialised by Kind = Procurement (IndustryState.Trade)
{
    // Buyer = Org(military), Seller = Org(factory owner); Item: a ware (ammunition, firearms) or an asset def (vehicle) with count;
    // Price terms per the MONEY doc or goods; DeliveryPlace = a military depot bin or node; Schedule; agreed by both orgs' office-holders.
}
```

- **Production queue for military goods** = IND `ProductionOrder`s on the factory's queue: either the military organisation **owns** the factory (an arsenal) and its office-holder places orders directly, or it has a **procurement agreement** with a factory's owner, whose office-holder places the matching orders (IND §3.7). There is no military-only queue type. The factory owner's own priorities decide where military orders sit among civilian ones (IND-1, 4.4).
- **Budget** = C6 budget lines of the military organisation (rations, pay, procurement, fuel) funded by its stores and by grants (government hand-overs); spending is physical (hand-overs) or monetary (MONEY doc), never a number.
- **Equipment ownership**: produced assets and lots are owned by the factory owner until delivered; delivery transfers ownership to the military organisation (`SupplyAgreement` basis) with the payment obligation (C2). Vehicles are then C1 assets owned by `Org(military)`; kit lots are lent to soldiers (MILITARY M-INV-2).

## 4. Mechanics

### 4.1 Infantry: recruitment and training (existing NPCs only)

Enlistment, terms, pay and kit follow MILITARY §4 (M9; Tier 3 C4 in the D5 index). Industrial additions: kit may include a firearm and an ammunition allowance lent from the military store; **training** is the education service (drill by a skilled soldier, C7 `Teach`/`Demonstrate`) that changes the learner's actual skill and associations. Until C4/M9 land, the industrial scenario's soldiers are **employees of a military organisation** (C3 contract, role `Crew`/soldier) who carry lent firearms: persistent people with identity, needs, households and relationships throughout (brief "Military production").

### 4.2 Crewing military vehicles

A military vehicle is operated exactly as VEHICLES §4.2–4.4: crew members board by choice under their role (an assignment or a crew post of the military org, MILITARY §4 "Orders are assignments"), drive, and leave. A tank **moves** with a driver and **fires** only while a gunner is aboard, the weapon's ammunition is in the vehicle's `AmmoBin`, and the crew chose an attack (their `IMilitaryPolicy` scores; MILITARY §4). Each attack consumes `AmmoPerAttack` units (`ConsumeReason.Expended`). Crew may desert, surrender or flee as soldiers do; an uncrewed tank is a parked asset (capturable: boarding by the enemy is a seizure, `OwnershipBasis.Seized`, with every consequence).

### 4.3 Logistics: fuel, ammunition, maintenance, rations

Supply is **physical**: fuel and ammunition reach depots by hauler or harvester routines (VEHICLES §4.4, `Supply` routine), are loaded into vehicle tanks and ammo bins by crew, and are consumed. Maintenance needs mechanics and machine parts (VEHICLES §4.6). Rations follow MILITARY §4 (haul jobs to muster points). A force far from its depot runs dry; that is the logistics game, and it emerges from the same goods and vehicle rules as civilian trade.

### 4.4 Competition with civilian organisations

| Resource | Where military and civilian demand meet |
|---|---|
| Labour | the same people choose between factory, mine and military jobs (C3 terms, MILITARY enlistment terms); a war drains workers from mines |
| Materials | steel, parts and engines go to whichever orders the factory owner runs and whichever buyers pay or agree |
| Fuel | the same coal and fuel stocks feed stations, harvesters and tanks |
| Capital | factories and vehicles are built for whoever finances them (government budgets, loans) |
| Power | military facilities sit on the same grids with priorities set by agreements or law (ELECTRICITY §4.3), never by a military rule |

Measured (KPIs IMIL-01..05), never designed: military share of steel, of factory ticks, of fuel, of power; civilian output lost during mobilisation.

### 4.5 Autonomous military organisations

The commander office decides from its own `InstitutionView` (C6): procurement, orders, deployment (assignments), maintenance priorities. The government decides budgets. Both may refuse or modify divine instructions (ORGANISATIONS §4.12, ORG-ACC-15 applied to a military organisation). With intervention disabled the military organisation still recruits, procures and fights by itself (D5.29).

### 4.6 The god and the military-industrial layer

- **Instruction** to the commander ("build no tanks", "attack the depot"): relayed, weighed, possibly refused (MILITARY §12).
- **Compulsion** of a factory manager to place one order, of a driver to drive one route, of a gunner to fire at one target in reach: one valid physical act each (D5.7); it cannot create ammunition, fuel or a crew.
- **Possession** of a tank driver or gunner (D5.8; VEHICLES §4.7): the player drives or fires with that person's capabilities; actions carry `Control = 2`.
- **Miracles** (smite a tank, a factory, a device; heal soldiers) through D3 only.

### 4.7 The strategic device (fictional nuclear capability; Tier 3)

**Making it.** A design known at the site (KNOWLEDGE doc; knowledge gates the build, a licence never does: LEAD_RULINGS R33), a strategic facility (IND `FacilityKind.StrategicSite`), the recipe `StrategicDeviceAssembly` (inputs: nuclear fuel 6, machine parts 20, wire 10, steel 20; 6000 ticks; 4/6 workers; 300 pu for the whole cycle), producing an asset of class `StrategicDevice` owned by the facility owner. It is a persistent asset with a location, condition and owner; it can be moved by a carrier, stolen, sabotaged, inspected or dismantled.

**Using it** is an organisation decision of its owner (the commander and, by charter, usually the government) carried out by a crew of 2 at the device (`UseStrategicDevice` act, within reach). Target: a hex region within `Industry.Military.StrategicRange` hexes. The effect is deterministic and fictional (no physics):

| Consequence | State change (owning code path) |
|---|---|
| **Destruction** | every building, facility, vehicle and hut within radius 1 hex of the target node is destroyed through its owner's destruction path (territory `BuildingDestroyed`, IND abort and salvage, VEHICLES wreck, D3's hut removal); goods in destroyed bins are destroyed with `ConsumeReason.Disposed` and a `StrategicEffectApplied` row per bin (ledger-exact) |
| **Deaths and injuries** | persons within radius: `Bodies.Damage` with severity falling by node distance (integer table); deaths through `PopulationModule.Kill(DeathCause.Violence)` with attribution in the effect event (no new death cause, SAC 20.4) |
| **Environmental** | the `Contamination` channel of the pollution field is set high over radius 2 hexes and decays very slowly (WASTE §5.3): illness incidence up, wild source regrowth and deposit extraction safety down |
| **Demographic** | measured: deaths, displacement (people leaving contaminated hexes by their own choice), fertility and mortality changes in following years |
| **Economic** | lost assets and stocks (from the ledgers), disrupted supply agreements, grids split by destroyed links |
| **Political and social** | every person within sight perceives a `SituationKind` "StrategicStrike" event; news travels by communication; beliefs about the owner (fear, trust, loyalty) and about the gods change only through perception and learning; government and military organisations' decisions respond by their own rules (and may lead to defection, migration, surrender) |

`StrategicDeviceUsed` (one) and `StrategicEffectApplied` (one per affected thing) are the only events the view animates; nothing visual happens without them.

**Hooks for later stages** (no mechanics in this run): **deterrence** is NPC and organisational belief about others' devices (beliefs from perception, records and messages), affecting their policies' scores; **inspection** is a service by an inspecting organisation whose inspectors physically visit facilities and write records (layer 4, possibly wrong); **disarmament** is the dismantling recipe (device → nuclear fuel 5, scrap, parts; IND-I3 salvage bound) carried out by NPCs; **diplomacy** is agreements between government organisations (I3 agreement record with `Kind = Treaty`), honoured or breached by their decision-makers.

## 5. Interactions with other systems

| System | Interaction |
|---|---|
| MILITARY | service records, morale, assignments, desertion, `IMilitaryPolicy`, M9 |
| `Military/Duel.cs` | combat resolution, unchanged; `VehicleCombat` adapter |
| IND, VEHICLES, ELECTRICITY | factories and orders, vehicles and crews, power |
| C3, C6, C10 | employment of crews, military organisations, government budgets and laws |
| C2, MONEY doc | procurement obligations and payment |
| KNOWLEDGE doc | weapon and device designs and licences |
| WASTE | contamination, wrecks and scrap |
| Divinity | 4.6 |

## 6. Invariants

| Id | Invariant |
|---|---|
| IMIL-I1 | No soldier, crew member or operator is created by any industrial or military path: the population count changes only by birth, immigration, resurrection, death and emigration (P-C) |
| IMIL-I2 | Every military vehicle and every unit of ammunition, firearm and device traces to a factory cycle (or a scenario placement or a miracle), with its inputs consumed (IND-I1, I3) |
| IMIL-I3 | Ammunition: `Σ ConsumedBy(Expended, Ammunition) = Σ ammo-using attacks × AmmoPerAttack` |
| IMIL-I4 | A vehicle attack has a living gunner aboard; a vehicle move has a living driver (VEH-I2) |
| IMIL-I5 | No military code path calls a money API or writes a production order into another owner's queue |
| IMIL-I6 | Strategic effects: every destroyed lot, asset and death has exactly one `StrategicEffectApplied` row; C1 and IND-I1 hold after the strike |

## 7. Telemetry and warehouse facts

**Events** (block 1070–1079, domain `IndustrialMilitary`, proposed): 1070 `ProcurementAgreed` (an I3 agreement of kind procurement; mirrors `SupplyAgreementMade`), 1071 `MilitaryEquipmentDelivered`, 1072 `CrewPostFilled`, 1073 `CrewPostVacated`, 1074 `AmmunitionExpendedDay` (per org per day), 1075 `DefenceBuilt` (with C1 `AssetBuilt`), 1076 `StrategicDeviceBuilt`, 1077 `StrategicDeviceUsed`, 1078 `StrategicEffectApplied`, 1079 spare. Combat events stay the military domain's (400–429 block, MILITARY §7; `AttackResolved` gains `AttackerAsset`/`TargetAsset` fields by version bump when vehicles fight).

**Warehouse** (`0035_military_industry.sql`, I5): `fact_procurement` (agreement, deliveries, payments), `fact_military_asset_spell` (asset, owner org, crew), `fact_ammunition_day`, `fact_strategic_event` and `fact_strategic_effect` (each effect with kind, target, before/after), joined to `fact_death` and `fact_production_cycle`. Reconciliation: procurement deliveries = `OwnershipChanged` basis 25 to military orgs; ammunition per day = ledger `ConsumedBy(Expended)`; strategic effects = events.

**KPIs** (I6): IMIL-01 military share of factory working ticks; IMIL-02 military share of steel and fuel consumption; IMIL-03 soldiers per 100 adults and their former occupations; IMIL-04 equipment readiness (crewed, fuelled, armed share of military vehicles); IMIL-05 civilian output change during mobilisation (against a control seed set); IMIL-06 strategic-strike consequences (deaths, assets lost, contaminated area, displaced persons, at 1, 30 and 365 days).

## 8. Player and view

Military vehicles are distinct original models; crew visible in hatches; ammunition and fuel shown on the vehicle card; factory queues show military orders with the buyer organisation; the strategic facility and device have a distinct warning look, and a strike plays only from its events (with a long-lasting contamination tint on the affected land, VISUAL_WORLD §18). Inspection: military organisation card (commander, budget lines, procurement agreements, equipment and readiness as observed vs its own records).

## 9. Migration from the existing code

- **Stage 3** (`MilUnit`, barracks `UnitOrder`, `UnitQueue`, `MilitaryRules.DrainQueue`) stays in stage 3 mode until M9/M14, as MILITARY §9 says. No industrial vehicle exists in stage 3 mode.
- **Duel** is unchanged; `VehicleCombat` is new; the only proposed change is the additive `SoldierStats` ranged override (amendment to the Military owner).
- **M9 relation.** M9 (C4, Tier 3 in the D5 index) makes soldiers persistent NPCs. Industrial infantry and crews need it for full service mechanics (morale, desertion); the industrial slice works before it with crews as employees (4.1) and no combat.

## 10. Tier and package mapping

| Capability | Tier | Package | Minimal first version |
|---|---|---|---|
| Firearm and ammunition wares and recipes 33–34; procurement agreement; military org places orders at a factory | 2 (after the D6.18 slice) | **I5** | a military org with a commander orders 20 ammunition and 4 firearms from a private factory; delivery and obligation |
| Carrier and tank defs and recipes; crewing; fuel and ammo bins | 2 (end) | **I5** with I2 | one tank built, crewed by two employees, driven (no combat) |
| Vehicle combat through `Duel` (`VehicleCombat`), gun posts, walls, bunkers | 3 | I5 with C4 (M9) | |
| Engineers, reconnaissance, artillery | 3 | I5 | |
| Aircraft, naval | later | I5 | needs air and water movement layers |
| Strategic device (make, hold, use, consequences), inspection, disarmament, deterrence beliefs | 3 | I5 with I1, I4, K1, WASTE, C10 | |
| Military inspection cards, readiness, queue labels | 2–3 | I6 | |

**Dependencies.** I5 needs **I1** (recipes, factories, orders; hard), **I2** (vehicles and crews; hard for vehicles), **I3** (procurement agreements; hard for procurement), **I4** (power for factories; soft), **C6** (military organisation and budgets; hard), **C3** (crews as employees; hard before M9), **C4/M9** (persistent soldiers' service, morale, combat; for Tier 3 combat only), **D2/D4** (divine instruction, compulsion, possession; for those parts), **K1** (weapon and device designs; soft), **MONEY doc** (payment; soft). Tier 2 only after the slice.

## 11. Tests and acceptance hooks

- `IMil_NoSoldierCreated` (IMIL-I1 over a scenario that builds 3 tanks and recruits crews: population conservation holds; no `NpcId` is allocated except by births and immigration).
- `IMil_TankNeedsCrewFuelAmmo`: a tank without a driver does not move; without a gunner or ammunition it does not attack; with an empty tank it stops.
- `IMil_MilitaryOrdersCompete`: a factory with one civilian and one military order runs them in the owner's priority order; materials consumed by one are not available to the other.
- `IMil_ProcurementTransfersOwnership`: delivery under a procurement agreement transfers the asset to the military org with one obligation; no money balance changes until payment.
- `IMil_StrategicEffectsLedgerExact` (Tier 3): after a strike in a fixture, every destroyed lot and asset has one effect row; IND-I1 and C1 hold; deaths are `Violence` with attribution.
- `IMil_CommanderRefusesInstruction` (ORG-ACC-15a applied to a military org).
- Vertical slice: I5 is not part of D6.18's ten steps; its first test joins the slice scenario after step 10 (`VS11_TankFromSameFactory`: the factory that built the harvester builds a tank for a military organisation, and both orders consumed steel from one input bin).

## 12. Open questions (with defaults)

1. **Before M9**, should employee-crews fight? Default: no combat until C4/M9; vehicles move and deliver only.
2. **Who may authorise a strategic strike?** Default: the owning organisation's charter (usually the government's Head and the commander together); a scenario may set otherwise.
3. **Contamination decay rate.** Default: 1‰ per day of the excess (half-life about 2 sim years); tuning.
