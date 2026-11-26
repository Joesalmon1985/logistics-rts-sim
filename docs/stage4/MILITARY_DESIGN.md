# Military design (stage 4)

Soldiers are the same persistent NPCs as everyone else. They serve because they accepted terms, they follow assignments they accepted, and they can still retreat, desert, surrender or defect. Combat itself (the stage 3 duel rules) is unchanged. The shared names, layers, policy levels and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md); that file wins where this one disagrees.

## 1. Purpose and scope

**This document owns:**
- service: enlistment contracts, pay, rations and kit issued from the quartermaster's store (which replaces the barracks; the stage 3 "armouries", the ten gear workshops, keep their name);
- assignments with pay and bounties (which replace defend flags and targets);
- units and squads formed by assignment;
- morale;
- retreat, desertion, surrender, defection and prisoners;
- truces between soldiers (live and let live) and the effect of bounties;
- the selfish herd during raids;
- `IMilitaryPolicy` for combat decisions, and how the military institution is managed through `IInstitutionPolicy`;
- the migration from `MilUnit` spawning (M9), keeping the stage 3 combat tests meaningful.

**It links to, and does not repeat:**
- the duel rules, kit stats and building damage: the plan's stage 3 section ([UNITY_POC_PLAN.md](../UNITY_POC_PLAN.md)) and the code in `Assets/God/Sim/Military` (reused);
- the treasury, contracts, escrow, arrears, laws and citizenship: [STATE_DESIGN.md](STATE_DESIGN.md);
- the generic institution model and Goodhart's law: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md);
- the NPC record, drives (fear, pain) and the Loyalty and Boldness traits: [POPULATION_DESIGN.md](POPULATION_DESIGN.md);
- perception, beliefs, gestures (`Warn`, `Surrender`, `Apologise`, `Alarm`, `AskJoin`): [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md);
- military features, verbs and learning signals: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- trust and grudges between soldiers: [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- E8 (live and let live) and E1 (selfish herd) as games and their measures: [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md) and [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md) (E8, PDF 248–251; E1, PDF 190–194);
- encodings for trained military policies: [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md).

## 2. Principles
- **One person, one record.** A soldier is an `Npc` with a service record. It eats, sleeps, remembers, learns and can go home. Nothing is spawned to fight. `DevSpawnSoldier` exists for tests only and is refused in normal games.
- **Service is a contract.** Pay, rations, term and kit are terms the NPC accepted. The state can break them (arrears, no rations); the soldier can break them too (desertion).
- **Orders are assignments.** An assignment is a posted task with slots, pay and bounties. A soldier takes it or does not. Once taken, its `IMilitaryPolicy` generally follows it, but every tick-slot it can still choose to retreat, flee, surrender, desert or defect.
- **Combat rules are not touched.** `Duel.Resolve`, kit stats, turn order (longer reach first, then lower id), close quarters, building damage and capture stay as in stage 3. Only *who* fights and *whether* they keep fighting change.
- **Morale changes scores, never rules.** Low morale raises the score of retreat and desertion; it never forces them. A bold, loyal soldier with low morale can still hold.
- **No outcome is designed in.** Truces, routs, herding and mass desertion are things the mechanics make possible and the telemetry measures.
- **Determinism.** The military system runs after the state system, in ascending `NpcId` for soldiers and ascending id for assignments. Duel rolls keep their own stream (`Rng.Fork("military")`, as now); each soldier's choices use its own NPC stream.

## 3. Data structures (`Sim/Military`)

```text
ServiceRecord            // one per NPC who has ever enlisted; keyed by NpcId
  NpcId, Faction, ContractId           // the enlistment, a StateContract with Kind = Service
  Enlisted (tick), TermDays, Status: Enlisted | Deserted | Captured | Discharged | Dead | Defected
  PayOwedDays, LastPaidDay, LastRationDay
  Kit: Kit, KitLots: int[4] ItemLotIds // state-owned gear on loan (ECONOMY_DESIGN lots)
  AssignmentId (0 = none), SquadId
  Morale (0..1000), MoraleTerms: short[6]   // pay, food, winning, danger, comrades, loyalty (yesterday)
  Kills, Wounds, DaysServed

CombatState              // replaces MilUnit; keyed by NpcId; exists while the NPC is a soldier on the map
  NpcId, X, Z (mm), FaceX, FaceZ
  Cooldown, FireFlash, EngagedId, LastOutcome, LastDamage
  Activity: Idle | Moving | Fighting | Marching | Guarding | Returning | Waiting | Retreating | Fleeing | Surrendered
  TargetKind, TargetId                 // TargetId is an NpcId or BuildingId
  Path cache (derived, not hashed)

EnlistmentTerms          // per faction, posted at HQ boards as Service postings
  PayWare, PayPerDay, RationWare, RationPerDay, TermDays, SigningGift: Bundle
  MaxRecruits, KitRequired (weapon only | any), QuartermasterStoreId

Assignment               // replaces DefendFlag and the MarkedBy lists
  Id, Faction, Kind: Guard | Patrol | Attack | Raid | Escort | Muster
  Anchor: BuildingId or Node, TargetBuildingId (Attack, Raid), Route: Node[<= 8] (Patrol)
  Slots (<= 12), Takers: NpcId[Slots]
  BonusPerDay (on top of service pay), Bounties: Bounty[<= 3]
  Posted, Expires, Status: Open | Full | Withdrawn | Done

Bounty
  Kind: PerKill | PerCapture (building) | PerPrisoner | OnCompletion
  Pay: Bundle, PaidAt: HQ id

Squad                    // all current takers of one assignment
  Id (= AssignmentId), Members: NpcId[<= 12], LeaderNpcId

PrisonerRecord
  NpcId, HeldBy (faction), Since (tick), HeldAt (BuildingId), GuardAssignmentId, RationPerDay

MilitaryState (refactored)
  Combat: List<CombatState> (ascending NpcId)
  Service: List<ServiceRecord> (ascending NpcId)
  Assignments, Squads, Prisoners (sorted by id)
  Buildings, Rubbles (unchanged), Rng (unchanged)
```

**Health.** An NPC's body health runs 0–1000 (POPULATION). Combat keeps the stage 3 scale: `Duel.Resolve` damage is multiplied by `SimTuning.Military.HealthScale` (default 10) when applied to the body, so a soldier with full health takes the same number of blows as a stage 3 unit. Wounds raise the pain drive and heal slowly (faster with a healer).

## 4. Mechanics

### Enlistment and the quartermaster's store
- **Posting.** `SetEnlistmentTerms` posts Service postings on HQ boards. Any adult citizen who reads or hears of one may take it with an `Enlist` intent. Nobody is conscripted in stage 4; conscription is a law needing enforcement (later).
- **Why an NPC enlists** is its own decider's business: the pay and rations against its outside options (other jobs, foraging), fear (danger beliefs: wars it knows of, deaths it saw), loyalty, friends already serving, and learned associations from past service.
- **The quartermaster's store** replaces the barracks. It is a state store of gear lots, filled by haul jobs from the gear workshops and by purchase offers. A recruit walks to it; a quartermaster on a Staff shift (ECONOMY_DESIGN) issues a kit chosen by today's `BarracksRules.Choose` from what the store holds (one weapon; a shield unless the weapon is a bow; a helmet; a body item). With no quartermaster on shift, no kit is issued.
- **Kit is lent, not given.** The lots stay state-owned and are recorded on the service record. On discharge the soldier is expected to return them; a deserter keeps them, which is theft under a theft law if one exists.
- **The signing gift and first ration** are handed over at the quartermaster's store. From then on:
  - **Pay** accrues per day of service and is handed over at the pay-point HQ (STATE_DESIGN). Missed pay becomes arrears (`WageUnpaid`) and `PayOwedDays` rises.
  - **Rations** are collected at the quartermaster's store or any own HQ store once a day, or eaten from carry (a soldier carries up to 2 days of food). In the field, food must come by haul job to a muster point, or the soldier goes hungry. Supply is physical.
- **Term end.** At the end of `TermDays` the soldier may re-enlist (a new decision) or be discharged.

### Assignments, units and squads
- **Posting.** `PostAssignment(kind, anchor or target, slots, bonus, bounties)` puts an assignment on the boards of the faction's HQs and is told to soldiers within earshot of the posting HQ. Soldiers learn of it like any posting.
- **Taking.** A soldier on service with no assignment scores the open ones: bonus pay and bounties, believed danger (enemies seen near the anchor, deaths heard of), distance, comrades already in it (trust in them), and its own morale. Taking it is an `HoldPost` or `Charge` intent depending on kind. A soldier can leave an assignment at any time (`AssignmentLeft`), which is not desertion.
- **Kinds:**

| Kind | Replaces | What the takers do |
|---|---|---|
| Guard | `SetDefend` on a building or node, 0–5 guards | Hold a ring 5 m round the anchor; chase only within 15 m (stage 3 numbers) |
| Patrol | (new) | Walk the route; act on enemies and, where a law exists, on crimes seen |
| Attack | `MarkTarget` (first marked building) | March on the target building and fight what they meet |
| Raid | (new) | Attack an enemy's civilians and stores in an area, then withdraw |
| Escort | (new) | Stay near an envoy or a haul job |
| Muster | the wait ring round the nearest HQ | Wait at the anchor for further assignments |

- **Squads.** All takers of one assignment form a squad. The leader is the member with the most days of service (ties: lower id). In stage 4 the leader matters only as the point the others follow when retreating ("rally on the leader") and as the comrade whose fall shocks morale most. Ranks and appointed commanders come later.
- **A soldier with no assignment** behaves as the stage 3 rule 4: it waits near the nearest own HQ.

### Morale
Morale is a per-soldier value, 0–1000, kept on the service record. It is rebuilt daily from six terms and shocked by events during the day. All inputs are the soldier's own perceptions and memories.

| Term | Rises with | Falls with |
|---|---|---|
| `pay` | pay handed over on time | `PayOwedDays` |
| `food` | rations received; hunger low | hunger high; rations missed |
| `winning` | fights its side won that it saw or heard of; enemy buildings it saw fall | own side's deaths seen; retreats seen |
| `danger` | (none) | wounds; enemies seen outnumbering friends; fear drive |
| `comrades` | squad members in sight and trusted; shared hut with comrades | being alone; comrades deserting (seen or told) |
| `loyalty` | its citizen loyalty (STATE_DESIGN) | low loyalty |

```text
daily target = 500 + (Wp*pay + Wf*food + Ww*winning + Wd*danger + Wc*comrades + Wl*loyalty) / (2*Wsum)
Morale      += (target - Morale) * MoraleRate / 1000
shocks (immediate, then recover with the daily update):
  comrade killed within sight      -60 (leader -120)
  own wound                         -40 per 100 body health lost
  enemy killed by own squad in sight +30
  rout seen (3+ friends retreating)  -80
```
- **Weights** (`SimTuning.Military`, defaults pay 3, food 3, winning 2, danger 3, comrades 2, loyalty 2) are tuning. Experiments vary them.
- **How morale acts.** It enters the `IMilitaryPolicy` scores: `Retreat`, `Flee`, `Desert` and `Surrender` gain `(500 - Morale) × k` (k by verb, scaled down by Boldness and Loyalty traits). Fear and pain drives act too. Nothing is a threshold rule.

### Combat (reused)
- **The attack** is `Duel.Resolve(attackerStats, targetStats, militaryRng, close)` exactly as now; stats from `MilitaryTuning.Of(kit)`, which moves to `SimTuning.Military` in M0 with the same numbers.
- **Unarmed fights.** Civilians and disarmed NPCs fight with an `Unarmed` weapon profile in `SimTuning.Military` (bare hands or a digging stick; added in M0 as tuning, no new combat code). This document owns soldier combat; civilian contests over a resource (escalation turns, posture, when a contest becomes a `Duel`) belong to [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md), and the per-pair contest history to [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md).
- **Turn order** within a tick: longer reach first, then lower `NpcId` (was unit id). A soldier killed earlier in the tick does not strike.
- **Movement** uses `Pathing.MoveToward` on `CombatState` millimetre positions; speed from kit. Off duty, the NPC walks the node grid as a civilian; going on duty copies its node position into `CombatState`.
- **Target choice comes from perception.** The policy picks a target from its `SoldierObservation` (enemies and enemy buildings within 23 m sight), not from a global list. The scripted default keeps the stage 3 order: nearest enemy soldier (lowest id), else a target building of its assignment if visible, else the nearest visible enemy building.
- **Who can be attacked.** Soldiers of factions at war, and in a `Raid`, civilians of that faction. Attacking a civilian or a surrendering soldier is a `CrimeCommitted` against the laws of war only if a law says so (later); it is always seen by witnesses and remembered.
- **Buildings and capture** are unchanged: building damage by weapon, HQs captured by the faction that brought them to zero (STATE_DESIGN handles the residents).

### Desertion, surrender and defection
- **Retreat.** `Retreat` moves away from enemies towards the squad leader, then the assignment's anchor, then the nearest own HQ. It ends the soldier's part in the fight but not its service. Retreating soldiers can be shot; there is no free disengage.
- **Flee.** Like retreat, but towards the nearest safe-looking place, ignoring the squad. Fear relief on fleeing is a learning signal (NPC_LEARNING_DESIGN).
- **Desert.** `Desert` ends service: status `Deserted`, the assignment slot frees, pay stops, the kit goes with the deserter. The NPC becomes a civilian with memories intact. It may go home, hide, migrate or later re-enlist elsewhere. Desertion is a crime only if `Law(Desertion)` exists and someone enforces it (later).
- **Surrender.** `Surrender` is the gesture of the same name (drop weapon, kneel, open hands). The weapon lot is dropped on the ground. Enemy soldiers who perceive it choose, by their own policies, to `AcceptSurrender` (take prisoner), ignore it, or attack anyway. Killing a surrendering soldier is seen by witnesses on both sides; their memories lower trust in the killer's faction, which can make later surrenders less likely and fights longer. That link is measured, not assumed.

### Conduct in war: real consequences without a law of war (owner decision Q18)
There is no full law of war in Stage 4, but **surrender, retreat, prisoners, treatment of the defeated and reciprocal restraint have real consequences**, carried by the ordinary mechanics, never by a rule that forbids or punishes:
- **Attacking someone who surrendered** (`SurrenderedKilled`, or wounding a kneeling soldier, a prisoner or a retreating soldier who has dropped its weapon) is an observable act. Every NPC who perceives it, on either side, records it; gossip and returning prisoners carry it further. Where it is **observed or communicated**, it affects:
  - **reputation and trust:** the attacker's relation records and its faction's group reputation (`GroupRep`) fall with every witness and listener ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md));
  - **morale:** a `MoraleShock` for the victim's comrades who see or hear of it (fear and anger), and a smaller shock for the attacker's own comrades whose Honesty or Kin weight make it costly to them;
  - **future willingness to surrender:** the `Surrender` verb's learned value and the expected-treatment belief about that faction fall for those who know, so they fight on or flee instead ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)); that the war then becomes bloodier is a measured result, not a rule.
- **Treatment of prisoners and the defeated** (rations given or withheld, release, escape, enlistment offers, how residents of a captured HQ are treated) enters their memories, gossip and loyalty in the same way (`treatment` term, [STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection)), and so the defeated's later loyalty, desertion and defection.
- **Reciprocal restraint** (warning shots, holding fire, sparing the surrendered) can be learned and copied, and lapses are remembered; truces (below) are its measured form.
- **Not everyone behaves the same.** Individuals differ by traits, learned values and culture, and factions differ by their posted bounties, assignments and (later) laws, so one squad may spare the surrendered and another may not.
- **Later:** institutions and treaties (courts-martial, laws of war, prisoner exchange) can establish and enforce formal rules (Stage 6 and later).

- **Defection.** A soldier with low loyalty and a belief that the enemy treats people better can `Defect(to f)`: it gestures `AskJoin` to enemy soldiers or an enemy HQ. The receiving state's `AcceptDefectors` rule answers (STATE_DESIGN). Accepted defectors become citizens of f with their kit, and may enlist there. Refused ones are treated as surrendering.
- **Recognising intent is imperfect.** A `Surrender` or `AskJoin` seen at a distance, at night or in a crowd can be misread (COMMUNICATION_AND_COGNITION misunderstanding rules), so a genuine surrender can be shot and a feigned one can be accepted.

### Prisoners
- **Taking a prisoner.** An accepting soldier escorts the prisoner to its nearest HQ (an `Escort` with one slot, created automatically). The prisoner's kit lots go to the captor's quartermaster's store as spoils.
- **Holding.** A `PrisonerRecord` names the HQ and a guard assignment. The captor's treasury must hand over `RationPerDay`; a starving prisoner's health falls like anyone's.
- **Escape.** A prisoner's own policy scores `Flee` every think tick. With no guard in sight, escape succeeds when it walks out of the HQ's area. A guard who sees it can stop it (a fight against the `Unarmed` profile).
- **Release.** `ReleasePrisoner` frees one; peace releases all held from that faction (they walk home); a prisoner may also be offered enlistment by the captor (defection by another road).
- **Later:** exchange and ransom through trade offers, prisons as an institution (INSTITUTIONAL_STATE_DESIGN), courts for crimes in war.

### Truces between soldiers (live and let live)
**What makes it possible** (SELFISH_GENE_NOTES E8: stable local pairs, visible retaliation, cheap shows of capability):
- **Stable pairs.** Guard assignments at facing posts last as long as the state leaves them; the same NPCs face each other for days and recognise each other (perception recognises individuals).
- **Choice to fire.** A soldier on `Guard` that sees an enemy in range, while not itself attacked, chooses whether to `Attack`, `HoldPost` (hold fire) or `WarningShot`. Its scores come from its learned associations (last time it fired here, it was fired on), its social memory of that enemy individual, its fear, and the incentives of its assignment.
- **Visible retaliation.** Being shot at is perceived, and the scripted policy answers proportionally by default (a reflex return of fire). Whether that becomes Tit for Tat-like behaviour depends on memory and learning, not on a rule.
- **Cheap signals.** `WarningShot` is a new combat intent: an attack that rolls no hit and lands within 2 m of the target, shown like a miss. It shows capability without harm. The `Warn`, `Apologise` and `Forgive` gestures work across factions.
- **What breaks it.** `PerKill` bounties add a prize for firing; rotating posts breaks the pairs; an `Attack` assignment through the sector ends it. These are hypotheses to test, not designed outcomes (GAME_THEORY_DESIGN lists them as hypotheses).
- **How it is measured.** A *quiet pair* is two opposing guard posts within sight of each other, both at war, with no damaging attack between their takers for a whole day. `TruceFormed` is emitted after `TruceQuietDays` (default 2) quiet days; `TruceBroken` on the first damaging attack, with its cause (bounty taker, newcomer, accident, ordered attack).

### The selfish herd during raids
**What makes it possible** (SELFISH_GENE_NOTES E1: a predator that attacks the nearest prey, positions in space, options to flee and to call out):
- **Raiders attack the nearest.** On a `Raid`, the scripted target rule picks the nearest visible civilian or soldier, ties by lower id. That is the attacker the selfish-herd argument assumes.
- **Civilians choose where to go.** A civilian that sees or hears raiders scores: flee to its hut or HQ, move towards the nearest group of own-faction NPCs, hide in forest, stand with soldiers, or keep working. Each NPC decides alone; there is no "herd" rule.
- **Alarm calls.** `Alarm` (pointing at the danger with a loud call, heard at hearing range) is a choice: it warns others and also draws attention to the caller. Kin weight and learned outcomes change its score.
- **How it is measured** (GAME_THEORY_DESIGN E1): each NPC's domain of danger (nodes nearer to it than to any other NPC) at raid start and every 10 ticks; movement towards the group centre; casualties by domain size and by distance from the centre; alarm calls by relatedness of those nearby.

### `IMilitaryPolicy`
```csharp
public interface IMilitaryPolicy
{
    string Name { get; }
    // obs: what this soldier perceives now (allies, enemies, its assignment, its squad, its kit, rations, pay owed).
    // mind: the same NPC mind (beliefs, social memory, associations). Returns one combat-subset intent.
    NpcIntent Decide(in SoldierObservation obs, MindView mind, ref Rng rng);
}
```
- **Verbs (combat subset):** `HoldPost`, `Charge` (advance on a point or target), `Attack(target)`, `WarningShot(target)`, `Retreat`, `Flee`, `Surrender`, `AcceptSurrender(npc)`, `Desert`, `Defect(faction)`, `Assist(npc)` (help a wounded comrade), `Gesture`. The names follow [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md).
- **When it runs.** A soldier on duty is decided by `IMilitaryPolicy` instead of `INpcPolicy`. It thinks every 2 ticks while enemies are in sight and on its normal staggered slot otherwise. Attacks fire on the tick the cooldown allows, as now.
- **Policies:**
  - `DutyMilitaryPolicy` (scripted default): stage 3 target rules while morale is high; morale, fear, pain, learned associations and personality weigh the other verbs.
  - `Stage3MilitaryPolicy` (benchmark): exactly the stage 3 rules 1–4, never retreats, deserts or surrenders, ignores morale. For combat-equivalence tests.
  - Later: an `IntNet` policy with the `SoldierEncoderV1` of [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md).
- **Enforced:** no reference to `GodState` (the shared reflection test).

### The military institution
- **Stage 4 (M9):** the faction manages the military directly through the state commands (enlistment terms, assignments, bounties). There is no commander NPC.
- **From M12** the military can be founded as an institution of the generic model (INSTITUTIONAL_STATE_DESIGN): the Band-age war band, a Village militia, a Town army. Its manager (a commander NPC) receives the faction's directives ("keep 3 on every HQ", "take HQ 12") and a budget, and posts the assignments itself through an `IInstitutionPolicy`. The commander keeps its own motives: it may favour kin with safe posts, pocket bounty goods, or file a return showing fewer desertions than happened.
- **Goodhart for armies.** The faction's own measured values (`KpiReported`, equal to the actual values under `FullInformation`) are not what the commander is paid on. The commander files **returns** (`FileReturn` command, `ReturnFiled` event; [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#the-generic-institution-model)), which can be gamed, and incentives are paid only on returns. A kill-count target can make a commander throw soldiers away; a zero-desertions target can make it hide desertions in its return; a "posts filled" target can fill posts with the unfit. Whether any of this happens is measured, not assumed. The generic oversight tools apply.

## 5. Interactions with other systems
- **State:** service contracts, pay points, arrears, loyalty, defectors, laws ([STATE_DESIGN.md](STATE_DESIGN.md)).
- **Economy:** gear lots, the quartermaster's shift, haul jobs for rations and kit ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)).
- **Population:** body health, fear and pain drives, traits, death ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)).
- **Learning:** military features, verbs and signals; pay, rations, wounds and defeat as rewards and punishments ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).
- **Social memory:** recognising opposing individuals, grudges, trust in comrades ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Factions:** war and peace, capture and elimination stay in `Sim/Factions`; peace releases prisoners and withdraws attack assignments against the other side.
- **Institutions:** the military institution, guards (law enforcement) and healers for the wounded ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)). A soldier on `Guard` and a staff guard are different roles; one NPC can hold only one.

## 6. Player and view
- **Army panel** (replaces the Army tab's barracks card), all read from `FactionKnowledgeView`:
  - enlistment terms with a pay slider, recruits per day, soldiers by status;
  - the assignment board: kind, anchor or target, slots filled, bonus, bounties; post, change and withdraw;
  - morale: distribution (10 bins) and the commonest low-morale terms;
  - pay owed, rations missed, desertions and defections per day;
  - prisoners held, with release buttons.
- **Map:** assignment markers replace defend flags and target marks; a slot counter shows takers against slots. A raid area shows as a shaded zone.
- **Soldier inspector:** the NPC card (its own view) plus the service record: days served, pay owed, ration last received, morale and its terms, assignment, squad, kills and wounds.
- **HUD macros keep the old feel.** Right-click "defend here with 3" posts a Guard assignment with 3 slots and default bonus; "attack this" posts an Attack assignment with all free soldiers' worth of slots.
- **Animations only show events:** kit issued (hand-over at the quartermaster's store), ration hand-over, retreat and flight runs, a deserter walking off with its kit, the surrender kneel, warning shots landing short, prisoners escorted.

## 7. Telemetry
**Domain events:**
- `Enlisted`, `KitIssued`, `KitReturned`, `RationGiven`, `RationMissed`, `WagePaid` and `WageUnpaid` (service kind), `Discharged`;
- `AssignmentPosted`, `AssignmentTaken`, `AssignmentLeft`, `AssignmentWithdrawn`, `BountyPaid`;
- `AttackResolved` (replaces `UnitFired`, with `NpcId`s), `WarningShot`, `SoldierWounded`, `NpcDied` (cause combat, killer id);
- `MoraleShock` (cause, delta), `Retreated`, `Fled`, `Deserted`, `SurrenderOffered`, `SurrenderAccepted`, `SurrenderIgnored`, `SurrenderedKilled`;
- `PrisonerTaken`, `PrisonerEscaped`, `PrisonerReleased`, `CitizenshipChanged` (reason defected);
- `TruceFormed`, `TruceBroken` (GAME_THEORY_DESIGN), `RaidStarted`, `RaidEnded`, `AlarmCalled`.

**Daily person snapshot (soldier fields):** service status, morale and its six terms, pay owed days, ration last day, assignment, squad, kit.

**Daily faction snapshot:** soldiers by status, enlistments, desertions, defections, surrenders, prisoners held, morale mean and deciles, pay owed, open assignment slots, truce pairs active.

**Raid snapshot (every 10 ticks during a raid):** positions of NPCs in the raid area, for the domain-of-danger measures.

## 8. Stage 4 minimum vs later

| Milestone | Delivers |
|---|---|
| M0 | `MilitaryTuning` statics moved to `SimTuning.Military` (same numbers) |
| M1 | `IMilitaryPolicy` seam; `Stage3MilitaryPolicy` wraps today's `Think`/`Acquire`/`Guard`/`Wait` with a `SoldierObservation`; replay equivalence |
| M6 | The barracks as the quartermaster's store, with the quartermaster as a Staff shift (ECONOMY_DESIGN) |
| **M9** | **Soldiers are NPCs:** service records, enlistment terms, kit lent from the quartermaster's store, rations, pay and arrears, assignments (Guard, Patrol, Attack, Raid, Muster) with bonus and bounties, squads, morale, retreat, flight, desertion, surrender, prisoners (held, fed, escape, release on peace), warning shots, `DutyMilitaryPolicy`, truce and raid measures; `MilUnit` spawning deleted |
| M10 | Defection through `AskJoin` and `AcceptDefectors`; loyalty feeding morale |
| M12 | The military institution (war band) with a commander under `IInstitutionPolicy` |
| M14 | Faction AI using enlistment terms, assignments and bounties; balance |
| Later | Ranks and appointed officers; conscription law; prisoner exchange and ransom; prisons; laws of war and courts-martial; veterans and pensions; private and mercenary provision; drill and training; supply trains |

## 9. Migration from the existing code

**Today:** `EconomySystem` makes a `UnitOrder` when the barracks completes a cycle (`BarracksRules.Assemble` takes 1 bread and the kit); `MilitaryRules.DrainQueue` spawns a `MilUnit` at the barracks flag; `MilitarySystem` staffs defend flags (`StaffDefends`), runs each unit's `Think` (rules 1–4: fight in sight, guard, march on the first mark, wait), resolves attacks and captures. `AiBrain.Army()` sets defend flags on HQs (1 inner, 2 border, 3 at war, up to 5 threatened, plus reinforcement flags) and marks one target at a time, calling off when weaker.

| Today | Stage 4 | When |
|---|---|---|
| `MilitaryTuning` (22 mutable statics) | `SimTuning.Military`, same values | M0 |
| `MilitarySystem.Think`, `Acquire`, `Guard`, `Wait` | `Stage3MilitaryPolicy`, fed by `SoldierObservation` | M1 |
| Barracks (`BuildingRole.Factory`, `UnitOrder`, `UnitQueue`, `UnitProduced`) | Quartermaster's store: a store issuing kit by `BarracksRules.Choose`; no unit is produced | M9 |
| `MilUnit` (Units list, `NextUnitId`) | `CombatState` + `ServiceRecord` keyed by `NpcId` | M9 |
| `UnitSpawned`, `UnitDied`, `UnitFired` | `Enlisted`/`KitIssued`, `NpcDied`, `AttackResolved` | M9 |
| `DefendFlag`, `SetDefend`, `RemoveDefend`, `StaffDefends` | Guard assignments: `PostAssignment`, `WithdrawAssignment`; soldiers take slots by choice | M9 |
| `MarkedBy`, `MarkTarget`, `UnmarkTarget` | Attack assignments (one per target) with optional capture bounty | M9 |
| `DevSpawnUnit`, `DevMoveUnit` | `DevSpawnSoldier` (creates an NPC with a service record; test and dev setups only, refused unless `GameSetup.DevCommands`), `DevMoveSoldier` | M9 |
| `Duel`, `DuelSim` | Unchanged | - |
| `GroupBattle` | Places soldiers with `DevSpawnSoldier`; runs with a chosen policy (`Stage3MilitaryPolicy` or `DutyMilitaryPolicy`) | M9 |
| `AiBrain.Army()` guards and marks | The same decisions sent as assignments (guard counts become slots; marks become Attack assignments; "call off" becomes withdraw); `ArmyWanted` becomes the recruit target for enlistment terms | M9 (translation), M14 (rewrite) |
| `FactionRules.Eliminate` disbands units | Discharges service records; soldiers become stateless civilians | M9 |

**Keeping the stage 3 combat tests meaningful:**
- **Duel tests are untouched.** `Duel` and `DuelSim` do not change, so every kit-versus-kit series (the "no kit wins over 60%" table) gives bit-identical results.
- **Equivalence at M1.** With `Stage3MilitaryPolicy`, stage 3 mode replays seeds 1–12 with the same `StateHash` at every checkpoint as before M1 (the observation is a re-packaging of the same data).
- **Group battles at M9.** With `Stage3MilitaryPolicy` and NPC soldiers, `GroupBattle.Series` results stay within tolerance of the stage 3 numbers (section 10). Differences come only from health scaling and id order, which are checked separately.
- **Morale on is a new experiment,** not a regression. Its results are reported beside the stage 3 numbers.
- **Stage 3 mode** (`Population` off) keeps `MilUnit` until M14 so its AI balance tests still run.

## 10. Tests and acceptance criteria
"Significant" means a two-sided Mann–Whitney test at p < 0.05 over the stated seeds unless stated. Behavioural targets below are calibration targets for the scripted `DutyMilitaryPolicy` at default tuning; they say the mechanics can produce the behaviour, not that the island must.

**Invariants (every build):**
- **M-INV-1 Never spawned:** every `ServiceRecord.NpcId` has an earlier `NpcBorn` or immigration event, outside `DevCommands` scenarios. 20 seeds × 20 days, zero violations.
- **M-INV-2 Kit conservation:** for each gear ware, store stock + kit lent + kept by deserters + dropped + spoils + destroyed = made + bought, every tick.
- **M-INV-3 No quartermaster, no kit:** a quartermaster's store with no quartermaster on shift issues no kit over 5 days, exactly.
- **M-INV-4 Duel unchanged:** `DuelSim.Series` over all 25 bare weapon pairs, 200 duels each, gives exactly the pre-stage-4 counts.
- **M-DET-1:** replay hashes equal with recording on and off; `Stage3MilitaryPolicy` replays stage 3 seeds 1–12 with identical hashes at M1.
- **M-POL-1:** reflection test: no military policy references `GodState`.

**Stage 3 equivalence (M9):**
- **M-EQ-1 Group battle:** with `Stage3MilitaryPolicy`, the five-a-side line-ups (all-melee five vs three melee plus two ranged) over 36 seeds give A wins within ±3 of the stage 3 count, and survivors within ±10%.
- **M-EQ-2 Capture:** an Attack assignment with 8 takers against an undefended HQ captures it within the same time as stage 3 marked targets, ±20%, on 10 seeds.

**Service and morale (M9):**
- **M-ENL-1 Zero pay:** enlistment terms with zero pay, zero rations and no signing gift find no recruit within 5 days on at least 95% of 20 seeds (food adequate, no war).
- **M-ENL-2 Pay matters:** doubling the daily pay raises recruits in the first 5 days by a significant margin over 20 seeds.
- **M-MOR-1 Unpaid soldiers:** stopping pay and rations for an enlisted force of 20 lowers mean morale by at least 30% within 3 days, and desertion exceeds 25% within 6 days, on at least 18 of 20 seeds.
- **M-MOR-2 Paid control:** the same force paid and fed has desertion below 5% over the same 6 days on at least 18 of 20 seeds.
- **M-MOR-3 Shock:** a squad that sees its leader killed has a mean morale drop of at least 100 within 1 tick (unit test).
- **M-ROUT-1 Breaking point:** in a 10-a-side battle with morale on, the losing side's survivors retreat, flee or surrender before death in at least 30% of battles over 36 seeds (reported against the stage 3 policy, where it is 0%).

**Surrender, prisoners, defection:**
- **M-SUR-1:** a soldier below 20% health, with no comrade in sight and 2 or more enemies within 10 m, chooses `Retreat`, `Flee` or `Surrender` in at least 80% of 500 scripted trials (default traits).
- **M-PRI-1 Escape:** a prisoner with no guard assignment escapes within 2 days in at least 70% of 50 trials; with 2 guards on assignment, in at most 20%.
- **M-PRI-2 Peace:** on `PeaceMade`, every prisoner held from that faction is released within 1 tick.
- **M-DEF-1 Closed to defectors:** with `AcceptDefectors = false`, zero `CitizenshipChanged(defected)` events over 20 days, exactly.

**Conduct in war (Q18; M9):**
- **M-CON-1 Witnessed killing of the surrendered has consequences:** in a scripted fixture where soldier A kills a surrendered enemy in sight of 3 enemy and 2 friendly NPCs, every witness's trust in A falls and the enemy witnesses' group reputation of A's faction falls (exact deltas from the evidence table); the enemy witnesses' comrades receive a `MoraleShock`; with no witness and no communication, no NPC's records change.
- **M-CON-2 Surrender willingness responds:** over 20 seeds, soldiers of faction B who saw or were told of a killing of the surrendered by faction A choose `Surrender` in the M-SUR-1 situation against A at least 20 points less often than B soldiers with no such knowledge (Mech); the overall size is reported.
- **M-CON-3 Not uniform:** in a 36-seed battle scenario the rate of `SurrenderAccepted` per `SurrenderOffered` differs between individuals (some always accept, some do not) and is neither 0% nor 100% overall (G4.11).

**Truces and herds (descriptive, measured; M9):**
- **M-TRU-1 The measure works:** in a scripted scenario where both posts use a fixed hold-fire benchmark policy, `TruceFormed` is emitted after exactly `TruceQuietDays`; one damaging shot emits `TruceBroken` with the right cause.
- **M-TRU-2 Bounty effect (hypothesis):** two factions at war with facing guard posts for 20 days and no attack assignments, with and without a `PerKill` bounty, 20 seeds each. Report the fraction of quiet pair-days in each arm and the difference with a confidence interval. Pass criterion: the measurement runs and is reported; no direction is required.
- **M-HERD-1 The measure works:** raid snapshots give a domain-of-danger value for every civilian; on a hand-built layout the computed values match the expected ones exactly.
- **M-HERD-2 Edge risk (hypothesis):** over 20 raid seeds, report casualty rate by quartile of domain of danger at raid start. Pass criterion: reported with a confidence interval; no direction is required.

**Performance:** 100 soldiers in contact plus 400 civilians run at least 20 times real time headless (HEADLESS_SIMULATION targets).

## 11. Open questions
No open owner question remains.

**Owner decision Q18 (27 Nov 2026, [conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)): law of war.** No full system in Stage 4, but surrender, retreat, prisoners, treatment of the defeated and reciprocal restraint have real consequences. Attacking someone who surrendered affects reputation, trust, morale and future willingness to surrender, where observed or communicated. Later institutions and treaties can establish and enforce formal rules. Individuals and factions do not all behave the same ([Conduct in war](#conduct-in-war-real-consequences-without-a-law-of-war-owner-decision-q18); M-CON-1 to M-CON-3).

Settled here:
- **Squad leadership.** The member with the longest service leads (ties to the lower id); an elected or appointed leader is a later option.
- **Health scale.** Body health 0–1000 with a ×10 damage scale keeps duels identical. Stats do not depend on body state in stage 4, so duel equivalence stays exact; fighting worse when starving is a later option.
- **Kit on desertion.** A deserter keeps the kit; it is theft only where a theft law exists and someone enforces it.
