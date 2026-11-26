# Game theory design (stage 4)

This document maps every example in *The Selfish Gene* onto the island. For each one it says what it looks like in the game, which mechanics make it possible, which institutions change its payoffs, how to measure it after the fact, and whether it is in scope. The exact benchmark policies and the maths-only lab are in [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md). The source for every example, payoff and page number is [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md). Page references below look like "PDF 93" and refer to those notes.

Milestone numbers are the official ones in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list).

## 1. Purpose and scope

**This document owns:**
- the mapping of each book example to the game, with scope (in, measured only, optional, out);
- the rule that separates lab experiments from island experiments;
- how strategies are measured on the island after the fact (behavioural fingerprints);
- how institutions and state policies shift the payoffs of each game;
- **the shared civilian contest mechanic**: disputes between NPCs over a resource (escalation turns, posture, when a contest becomes a `Duel` exchange) and the contest record that fights, haggles and standoffs share. The per-pair contest history in each NPC's mind is owned by [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md), and soldier combat by [MILITARY_DESIGN.md](MILITARY_DESIGN.md);
- the telemetry the game-theory layer needs.

**This document links to, and does not repeat:**

| Topic | Owner |
|---|---|
| NPC body, drives, personality traits, life cycle | [POPULATION_DESIGN.md](POPULATION_DESIGN.md) |
| Perception, beliefs, the decider, gestures, lies | [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) |
| Associative learning | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| Relationships, promises, reputation, gossip | [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) |
| Partnership, households, kinship graph, children | [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md) |
| Genome, relatedness values | [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) |
| Memes, imitation, traditions | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| Items, item quality (true and visible), `WareRefValue`, barter, deferred exchange, property, enterprises | [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) |
| Institution catalogue and strategic effects | [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md) |
| Taxes, bounties, laws, welfare | [STATE_DESIGN.md](STATE_DESIGN.md) |
| Soldiers, soldier combat, truces, morale | [MILITARY_DESIGN.md](MILITARY_DESIGN.md) |
| Reports, media, misinformation | [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) |
| KPI definitions | [KPI_CATALOGUE.md](KPI_CATALOGUE.md) |
| Warehouse tables | [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md) |
| Experiment design (treatments, seeds, causal vs descriptive) | [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md) |
| Runner commands | [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md) |

## 2. Principles

1. **No NPC is a strategy.**
   - Nobody is permanently a Hawk, Dove, Sucker, Cheat or Grudger, and there is no such field.
   - What an NPC does in a contest or an exchange comes from its personality traits, drives, beliefs, social memory, learned associations and the decider's scoring. See [POPULATION_DESIGN.md](POPULATION_DESIGN.md#personality) and [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#47-the-decider).
   - The same NPC may look like a Hawk over a berry bush and a Dove at home.
2. **Named strategies are benchmark policies only.**
   - They are `INpcPolicy` implementations chosen by name in the registry, used in the lab and in scripted island scenarios as controls.
   - They never appear in a normal game, and nothing in the game reads a label.
3. **Two kinds of experiment, never mixed.**

   | | Lab (`Sim/Lab`, milestone M2) | Island (the full sim, milestones M5 onwards) |
   |---|---|---|
   | Payoffs | The book's numbers, fixed | Come from real consequences: food, health, time, goods, reputation, safety |
   | Agents | Abstract strategy objects, no map, no drives | Persistent NPCs with the whole mind |
   | Purpose | Check the maths, benchmark our code, test that the sim can reproduce known results | See what actually happens under different environments and institutions |
   | Expected result | Stated, with tolerances | A direction of effect over many seeds, never an exact number |
   | Can the result be wrong? | Yes, and the test fails | The island is allowed to disagree with theory |

4. **The island is never forced to a theoretical equilibrium.**
   - No code nudges a Hawk share towards 7/12, rewards Tit for Tat, or punishes cycles.
   - If our island produces no Hawk/Dove mix, or a Grudger-free village, that is a finding.
   - Two of the book's own published equilibria were wrong (see the corrections in the research notes). So the island is not expected to match the book.
   - **If the island differs from the maths, investigate why (owner, 27 Nov).** A difference is never "fixed" by tuning the island towards the book. It is explained: which ingredient the island lacks or adds (memory, recognition, noise, kinship, institutions, scarcity, payoffs that differ from the matrix), with a controlled island run that varies that ingredient. The explanation is written into the experiment's report.
8. **Exact, derived or invented (owner, 27 Nov).** A lab experiment is called a **reproduction** only when the book gives every number it needs. Where the book lacks the numbers, the experiment says so and states whether its payoffs are derived (computed from the book's own numbers or a cited source) or invented (chosen by us); an invented-payoff experiment is never called an exact reproduction. The status of each experiment is in [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#reproduction-status).
5. **Mechanics make it possible; measurement says whether it happened.** Nothing here is designed in. Each example lists the ingredients the book says it needs, and then a measurement that can come back negative.
6. **Measurement never feeds back.** Fingerprints and benchmark-likeness scores exist for analysis only. Policies cannot read them (the reflection test in [Tests](#10-tests-and-acceptance-criteria) covers this), and recording on or off gives the same state hash.
7. **Honest about limits.** The island has no genetic fitness in the book's sense: NPCs do not reproduce in proportion to payoff in a clean way. Where the book's result is about gene frequencies, the island version is about what spreads through inheritance, learning and imitation. Section 4.9 states this per example.

## 3. Data structures

All integers. These are sketches; owning modules decide the final layout.

### 3.1 The contest record

Fights, haggles and standoffs over a prize share one record, so Hawk/Dove, attrition, resident/intruder and pecking orders can be observed in one place. This document owns the mechanic and the record. The code lives in `Sim/Social/Contests`, beside the per-pair history that each party keeps afterwards ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md): `Wins`, `Losses`, `Streak`, `RecentWins`).

```csharp
struct ContestId { int Value; }          // unique per game, never reused

struct Contest
{
    ContestId Id;
    int StartTick;
    byte Kind;                  // Berry/Pile/Bed/Spot/Haggle/Territory/Mate
    int PrizeValueDrive;        // what winning is worth to side A, in drive relief units (0..1000)
    // Both sides (A is whoever started the contest):
    int NpcA, NpcB;
    byte RoleA, RoleB;          // None / Resident / Intruder / Builder / FirstArrival
    byte TurnCount;             // 0..MaxTurns (default 8)
    ContestMove[] Moves;        // bounded, MaxTurns * 2
    byte Outcome;               // Open / AWon / BWon / Split / BothLeft / Fight
    int TimeSpentTicksA, TimeSpentTicksB;
    short InjuryA, InjuryB;     // health lost in fights (from Duel results)
}

enum ContestMoveKind : byte { Display, Escalate, Withdraw, Hold, Probe, Offer, Share }
struct ContestMove { byte Turn; byte Side; ContestMoveKind Kind; byte Gesture; }
```

- A contest runs in turns. A `Display` or `Hold` costs time only. An `Escalate` runs one exchange of the existing duel code (section 4.1).
- The record is a *description* of what the NPCs chose. It does not decide anything.
- The moves come from the decider's scoring, so each is traceable to traits and memory.

### 3.2 The behavioural fingerprint (analytics only)

One row per NPC per measurement window (default: one sim year, plus one at death). It is **never** in `GodState`.

```csharp
struct NpcFingerprint
{
    int NpcId; int WindowStartTick; int WindowEndTick;
    // each rate is permille 0..1000, or -1 when there were too few observations (< MinObs, default 5)
    short EscalationRate;           // escalated / contests where escalating was possible
    short FirstMoveCooperation;     // first hand-over to a new partner was a cooperative one
    short ReciprocationRate;        // cooperated at next chance after partner cooperated
    short RetaliationRate;          // defected back at next chance after partner defected
    int   RetaliationLagTicks;      // median ticks from being defected on to first defection back (-1 if none)
    int   ForgivenessLagTicks;      // median ticks from last partner defection to my next cooperation (-1 if never)
    short HonestyRate;              // checkable statements and promises that were true or kept
    short PersistenceMean;          // mean ticks held in time-only contests (attrition)
    short YieldToResident;          // yielded when intruder and the other was resident
    short ExploitRate;              // defected first on someone who had cooperated, when no retaliation was likely
    // Observation counts, so analysts can ignore thin rows:
    short NContests, NFirstMoves, NReciprocationChances, NRetaliationChances, NStatements;
    // Benchmark-likeness, one value per benchmark policy (permille agreement), see 4.10:
    short[] Likeness;               // index = benchmark id in the registry
}
```

### 3.3 Fingerprints are computed from events

`FingerprintBuilder` is a pure function over the domain-event stream (`Sim/Telemetry`). It holds no sim state, is not hashed, and runs in the runner or the optional Unity recorder. The sim itself never calls it.

## 4. Mechanics and algorithms

Section 4.1 describes the shared mechanics. 4.2 to 4.8 are the example catalogue in the book's order. 4.9 is the scope summary. 4.10 is measurement. 4.11 is how institutions shift payoffs.

For each example: **Island form**, **Makes it possible**, **Institutions**, **Measure**, **Scope**.

### 4.1 Shared mechanics

**Contests use the existing duel code.**
- An `Escalate` move runs one `Duel.Resolve(attacker, target, rng)` exchange per side per turn. This is the code in `Assets/God/Sim/Military/Duel.cs`: hit chance, block, crit, damage, from stats and a seeded `Rng`.
- A full fight to the end, when both keep escalating, is the same loop `DuelSim.Fight` runs: walk, attack in reach order, stop when someone's health reaches zero.
- Both use a forked stream `Rng.Fork("contest", contestId)`, so a contest replays exactly.
- Civilians fight with the **`Unarmed` weapon profile** in `SimTuning.Military` (bare hands or a digging stick), added in M0 when `MilitaryTuning` moves to `SimTuning`. It is a tuning entry, not new combat code. An NPC carrying gear uses its gear's profile. Soldier combat itself is owned by [MILITARY_DESIGN.md](MILITARY_DESIGN.md#combat-reused).
- **Injury is real.** Health lost in an exchange is the same health as everywhere else. It slows work (pain drive), costs days resting, and can kill. That is the "−100" of the book, expressed in lost days and death risk rather than a number we choose.
- **Time cost is real.** Display turns use the walking and work clock. An NPC arguing over a bush is not gathering. That is the "−10".
- **Retreat rule.** Each NPC has a retreat threshold from its own fear drive and health. This is the decider's choice, not a rule. A Hawk-like NPC is one whose fear stays low.

**Contests are observable and remembered.**
- Escalation is a visible action, so witnesses record it (perception, [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#42-perception-simcognitionperception)).
- The two parties remember each other (the per-pair contest history, owned by [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)). That memory supports the Retaliator, Grudger and pecking-order examples.
- Gestures `Warn`, `Defend`, `Surrender`, `Accuse`, `Apologise` and `Forgive` ([vocabulary](COMMUNICATION_AND_COGNITION_DESIGN.md#vocabulary-fixed-and-shared-at-first)) are the signals in a contest. They can be bluffs.

**Exchanges are separate hand-overs** (plan rule: never atomic between NPCs). That is what makes every barter and every loan a Prisoner's Dilemma by construction, with the temptation to keep the goods.

**Unknown ending.** NPCs never know when an interaction ends, unless they have a reason to believe it (migrating, dying, being sent away). The shadow of the future is therefore real and varies by NPC, and the end game in 4.5 arises when they do know.

### 4.2 Aggression (Chapter 5, PDF 89 to 111)

**A1. Hawk/Dove (PDF 93 to 96)**
- **Island form:** two NPCs reach the same berry bush, bed, goods pile or sleeping spot. Each decides to display (gesture, hold ground) or escalate (strike). Real duel injuries and the time spent posturing replace the book's −100 and −10. The prize is the relief of a drive (hunger, fatigue), not "50".
- **Makes it possible:**
  - contest record and duel code (4.1);
  - a contested resource with a value that can be smaller than the injury cost: foraging stock and beds ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#foraging), [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md));
  - no reliable foreknowledge of the other's type. Perception shows visible gear and action, not intent;
  - the fear, aggression and boldness traits as biases ([personality](POPULATION_DESIGN.md#personality)).
- **Not needed and not forced:** a fixed mixed strategy. The decider's seeded tie-break noise lets mixed behaviour appear.
- **Institutions:** police and the headman's court raise the cost of striking for a prize (a fine, a ban or a lashing on top of injury). Welfare and a shared store lower the prize (hunger matters less). Guards at a store make it a resident/intruder case. Healers lower the cost of injury, which raises escalation. See 4.11.
- **Measure:** `EscalationRate` per NPC and the share of contests with at least one escalation, by prize type and over time. Mean injury per contest and mean time per contest. Compare, in the lab only, to 7/12 (the island is not expected to match).
- **Scope:** **in.** Lab: L1 in [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md). Island: contests from M5 (gestures exist; escalation uses the `Unarmed` profile), armed contests from M9 (kit is issued to NPCs).

**A2. Retaliator, Bully, Prober-retaliator (PDF 97 and 98, endnote PDF 306)**
- **Island form:** contests over several turns. An NPC that is struck strikes back. An NPC that pushes until resisted and then runs. An NPC that now and then tries a short escalation to see if the other will fight.
- **Makes it possible:** multi-turn contests (the record in 3.1); per-pair memory; mutation and drift through personality inheritance ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)), so neutral variants can enter; a time cost; a probing option, which is just an escalation tried at low expected cost.
- **The corrected result.** The book withdraws the claim that Retaliator is an ESS. Doves drift in because they tie with it, and Hawks, Bullies and Probers then invade. A published dynamic simulation found the stable state to be a mixture of Hawks and Bullies. The game therefore makes **no** claim that retaliation dominates. Our lab shows only the mechanism (STRATEGY_BENCHMARKS L2).
- **OPEN FINDING FOR THE OWNER (M2, 27 Nov 2026): the lab contradicts the corrected claim under our default parameters.** With `k = 10, w0 = 500, mu = 2 permille`, every N = 200 agent run on seeds 1 to 13 stayed in the **Retaliator** basin (R 0.77 to 0.94; Hawk plus Bully 0.03 to 0.05). The system is **bistable**: a Retaliator basin and an H/D/B basin. The prototype's runs that reached the H/D/B mix needed more drift (`k = 1`, or `w0 = 100`). At N = 1000 the lab agrees with the prototype (R about 0.80). So the headline above, "Doves drift in, then Hawks, Bullies and Probers invade, ending in a Hawk/Bully mixture", is **not reproduced** under our defaults: the book's endnote result depends on drift conditions we do not reproduce by default. This is recorded as an amendment (STRATEGY_BENCHMARKS Amendment A2, [lab amendments](STRATEGY_BENCHMARKS.md#lab-amendments-m2-27-nov-2026)): the lab's expectation is now "Retaliator stays dominant under default parameters". **We do not present either result as the truth.** The design stance is unchanged (the game makes no claim that retaliation dominates, and none that it fails); the island measures what happens. The owner decides whether the island's drift settings should follow the lab default or the book's endnote.
- **Institutions:** as A1. A court that punishes first strikes removes the Bully's gain. A shared reputation system (media, gossip) makes a bully's reputation travel, so victims prepare.
- **Measure:** the share of contests started by escalation; `RetaliationRate` after being struck; the share of escalations abandoned after the other struck back (Bully-like); the share of NPC pairs where a first strike was followed by a retaliation in the same contest.
- **Scope:** **in** (M5 unarmed, M9 armed). Lab L2.

**A3. War of attrition (PDF 99 and 100)**
- **Island form:** waiting out a contested spot (a fishing place, a work slot, a queue) or a haggle where each side holds out. No injuries, only time. A readable tell, such as fidgeting or glancing away, is learned by others.
- **Makes it possible:** a time cost per tick spent (the NPC is not working); a prize of known value; private giving-up thresholds the other cannot see; and a visible signal that **can** leak. The gesture layer and perception already allow leaking, because posture is an observable action.
- **A poker face wins** means this: the NPC whose behaviour does not predict its quitting time does better against NPCs that learn tells. The island makes this possible by letting the associative learning ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)) pick up tells. It does not design a poker face.
- **Institutions:** none directly. A fixed-price market (state purchase offers) removes haggling. Queues organised by an official remove attrition.
- **Measure:** the distribution of persistence times per prize value (should be wide, roughly exponential, mean near the prize's worth if the theory fits). The correlation between a visible cue and the quitting time, per NPC (a tell). Whether the opponent's persistence changes after learning a tell.
- **Scope:** **in** (lab L3 at M2; island at M5, since haggles need only gestures and barter).

**A4. Resident/intruder (PDF 100 to 103, endnote PDF 308)**
- **Island form:** the NPC who arrived first, or built the hut, or has been using the bush, defends it, and the newcomer backs down. Both sides can see who is resident (a hut's builder rights, a bush's last user). No law is needed. If both think they are resident (a shared spot, a disputed hut), contests become long and violent, as with the speckled-wood butterflies.
- **Makes it possible:** a visible ownership or arrival tag (huts have sleeping rights, [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#forming-and-breaking-households); "last user" of a food source is a belief); strategies that can use the tag, which the decider does through the situation feature "I am owner here" ([features](COMMUNICATION_AND_COGNITION_DESIGN.md#45-situation-features-and-the-context)); a costly fight if the tag is ignored.
- **Which convention wins** depends on what the majority does. The "intruder wins" rule is possible but self-destructive. Nothing in the game favours the resident rule, so whether it arises is a measurement.
- **Institutions:** property law, a land or hut register and a headman's court *formalise* the convention and let it hold against stronger newcomers. Guards enforce it. Weak institutions leave it to custom.
- **Measure:** `YieldToResident` per NPC. The share of contests where an intruder retreated without escalation. The injury rate in contests where both claimed residency, against one-resident contests. Whether the rule differs between villages (conventions can differ by chance).
- **Scope:** **in** (lab L4; island from M5, and for huts from M3, since hut building arrives there).

**A5. Paradoxical rules (PDF 103 and 104, endnotes PDF 308 and 309)**
- **Island form:** the rule "attack the stronger, flee from the weaker." It is stable only if injury is extremely costly and paradoxical players already outnumber the others. We expect it **not** to arise on the island. If it does, it shows up in a community where nobody dares to pick on weaker neighbours.
- **Makes it possible:** a strength signal that both sides can see (gear, body), and an injury cost growing with the opponent's size, both already in the duel stats.
- **Within one lifetime (DSS, PDF 309):** an NPC can learn a dominance convention in one life by associative learning, like the pigs and the lever. The subordinate learns that fighting for the food loses, and the dominant one that it wins. That is a developmentally stable strategy. This is the main way conventions appear on the island.
- **Institutions:** none specific.
- **Measure:** the escalation rate by relative strength, plotted by strength ratio. A paradoxical rule shows a positive slope towards stronger opponents. Learning curves of NPCs in repeated contests with the same partner.
- **Scope:** **in as a measure and a lab variant** (L4b, optional). The DSS case is covered by lifetime learning (M3) and requires no extra mechanic.

**A6. Pecking orders (PDF 104 and 105, endnote PDF 309)**
- **Island form:**
  - *Cricket style:* a general memory of recent wins. An NPC on a winning streak becomes bolder, one on a losing streak more timid, without recognising anyone. A rank order forms by itself.
  - *Hen style:* per-pair memory of who beat whom. An NPC defers to those who beat it.
- **Makes it possible:** cricket style needs one scalar in the mind, `RecentWins` in the social-memory header (a decayed sum of wins and losses), feeding the aggression bias. Hen style is the social-memory pair record (`Wins`, `Losses`, `Streak`) ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)). NPCs have both. How much each contributes is shaped by learning and by familiarity, not set.
- **A pecking order has no purpose of its own.** It is a by-product. The island does not build ranks.
- **Institutions:** stable households and workplaces make pair memory more useful. Rapid turnover (migration, immigration) favours streak-based behaviour.
- **Measure:** the linearity of the dominance hierarchy in each group (Landau's h or the transitivity of "beat" relations); the contest and injury rate against group stability (should fall as the group stays together); the correlation between an NPC's recent win-loss and its next escalation.
- **Scope:** **in** (lab L5 compares the two memory rules; island from M5 with per-pair memory and unarmed contests, and from M9 with armed ones).

**A7. Conflict between species (PDF 106 and 107)**
- **Island form:** NPCs and predators or game animals. The conditional rule "if smaller, run" appears in how NPCs treat larger wild animals.
- **Scope:** **out** for stage 4, because there are no animals in the minimum game ([foraging](POPULATION_DESIGN.md#foraging) lists game as later). Revisit if hunting is added.

**A8. Why not kill every rival? (PDF 91)**
- **Island form:** an armed NPC beside a rival can strike and often does not. Killing a rival might help a third party more than the killer, and a fight costs time and risk.
- **Makes it possible:** the decider's scoring includes cost and risk, and its beliefs are about who else is near. Nothing forbids killing, and the scoring has no "never kill" term.
- **Institutions:** police and the court add a punishment. Bounties on enemy soldiers (military policy) add a prize and break truces (see E8). Blood debt in a household does the same informally.
- **Measure:** the share of contests that end in death; the share of contests in which an NPC could have escalated at low risk and did not; whether rivals' deaths were followed by a gain for the killer or for others (telemetry of who took the freed resource).
- **Scope:** **in as a measure.** No special mechanic.

**A9. Gene-level ESSs, the rowing crew (PDF 107 to 110)**
- **Island form:** conventions that can be of several kinds and stay put once most follow them (which side of a path, which language of gestures, whether to share). Several stable village conventions are possible, and a village can jump between them.
- **Makes it possible:** conformity trait, imitation ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)), and payoffs that depend on how many others follow the convention.
- **Measure:** the distribution of convention traits across villages and over time (multi-modality; time spent near each plateau). See memes (4.8).
- **Scope:** **in via memes** (M11). Lab: not separate.

**Digger wasps (endnote PDF 307 and 308)**
- **Island form:** an NPC without a bed either builds its own hut or moves into one that looks empty. Some "empty" huts are in use: the owner is away, has a claim, or has a household nearby. An NPC who enters may share for days, then find the owner returns, or find the hut sealed with another's goods. The cost of building is the time and logs. The cost of entering is the risk of eviction, a fight, or a lost sleeping right.
- **Makes it possible:** huts with sleeping rights; beliefs about whether a hut is in use, with a confidence that fades (a hut not seen occupied for a long time looks empty); a decision between "plan to build" and "enter"; scarcity of free huts that rises as more NPCs choose to enter. See [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#forming-and-breaking-households).
- **Critical share:** at some share of entering, building and entering pay equally. More entering makes free huts scarce, so building pays; less leaves plenty, so entering pays. The island will find its own crossing, if it has one.
- **Institutions:** a hut register or elders who recognise claims lower the payoff of entering and raise the cost of being wrong. Public housing lowers the cost of not building.
- **Measure:** the share of housing acquisitions by building versus entering, over time; the mean payoff of each route measured as time and goods (as the book measured it with marked wasps: time budgets); the crossing point where the two means meet; whether the mix settles or oscillates.
- **Scope:** **in** (lab L10; island at M3 once huts exist, honest and simple at first).

### 4.3 Deception in signals (Chapter 4 end, PDF 87 and 88)

- **Island form:**
  - lying in begging (a child or adult claims more need than they have);
  - lying in barter ("this is bronze", a promise not meant);
  - cheap fakes (painted pottery sold as bronze, from M8);
  - faction bluffing about army size (the Beau Geste example, in 4.7 under signals and lies);
  - brood parasitism (optional, 4.5).
- **Makes it possible:**
  - the message structure with `Topic`, `Quantity` and `Counter` that can be false ([honesty and lies](COMMUNICATION_AND_COGNITION_DESIGN.md#honesty-and-lies));
  - an item whose **visible** quality and **true** quality can differ, with inspection that reveals the truth. Item quality is owned by [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#41-items-freshness-quality-and-consumption) (true and visible quality, cheap fakes at M8);
  - perception that can miss a contradiction (attention cost);
  - reputation that makes a caught liar pay ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Institutions:** a court and a weights-and-quality inspector make fakes costly. A market stall with a name makes reputation sticky. Media spread a liar's reputation beyond personal contact. A bank or a deposit makes a promise credible.
- **Measure:** `HonestyRate`; the share of barter hand-overs where received quality was below the stated quality; the share of detected lies, and the time to detection; the reputation cost of the first caught lie.
- **Scope:** **in** (M7, when promises and lies arrive). Cheap fakes need the visible quality field, so they arrive with item quality at M8.

**Gambling metaphor (PDF 78 to 80).** NPCs weigh stake, odds and prize in the decider, so they act as if they had done the sums. Nothing extra is needed. Not an example to measure separately.

### 4.4 Kin and family (Chapters 6 to 8, PDF 111 to 162)

**C1. Hamilton's rule and the mushroom food call (PDF 112 to 122)**
- **Island form:** an NPC finds a large forage stock (the book's finder of 8 mushrooms). It can keep quiet, call others, or call only some. The same weighing governs sharing a surplus with kin, helping a sibling build a hut, and feeding a nephew.
- **Makes it possible:**
  - the kinship graph and the "kin weight" trait ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md), [personality](POPULATION_DESIGN.md#personality));
  - **estimated** relatedness, not true relatedness: NPCs act on believed relationships (who raised them, who shares the hut), which can be wrong;
  - a forage stock that is larger than one NPC needs, so calling costs little;
  - gestures `Point-out` and `NeedFood`/`NeedHelp` for the call itself.
- **Adjustments the book names** (age, life expectancy, certainty of the relationship) emerge from the decider's horizon and the confidence of a belief.
- **Institutions:** welfare and a shared store move the same food to non-kin by rule, which substitutes for kin help. Inheritance customs ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)) set whom wealth goes to.
- **Measure:** for each sharing event, the **true** relatedness from the genome (analytics may read it; NPCs cannot) against the share given. Regress giving on true r and on believed r. Check Hamilton's inequality retrospectively: does the cost to the giver, against the benefit times r, predict giving? Report the gap between true and believed r.
- **Scope:** **in** (the call decision is a lab case, L11, with the book's numbers; the island at M4 with kinship, and gestures at M5).

**C1b. Kin recognition and its exploits (PDF 118 to 122, 126)**
- **Island form:** NPCs do not know genes. They use rules of thumb: "lives in my hut", "was raised with me", "looks like me", "carries my household's mark" (a cultural marker). Each rule can be exploited: a foundling fed as a child, a stranger who moves into a hut, an immigrant who copies the marker.
- **Makes it possible:** recognition by familiarity and place (perception already recognises NPCs imperfectly); a visible marker trait (cultural, so it is also a meme); households.
- **Green-beard:** a gene that is both a visible label and a helping rule is not built. A **cultural** label (dress, dialect, tattoo) with a helping habit can arise and be exploited by mimics. This is as close as the island gets.
- **Institutions:** a register of births (later) and the elders reduce misattribution.
- **Measure:** the rate of helping directed at non-kin who share the cue (cue exploitation); true r of recipients of kin-labelled help.
- **Scope:** **in** (M4 for household cues, M11 for cultural markers). Genetic green-beard is **out**.

**C2. Guillemot eggs (PDF 125 and 126)**
- **Island form:** a shared nursery arrangement where several households take turns minding children. A "cheat" household bears more children and does not take its turn, and parents dump children on the others. The remedy is each carer minding only its own children, which needs recognition of one's own.
- **Makes it possible:** communal care by households as an informal arrangement; the possibility of leaving a child; recognition of own children by familiarity (children do wander).
- **Institutions:** a school or nursery (Village and Town ages, later stages) formalises the pool and adds staff and funding. Welfare in kind (food for children) makes dumping less costly for the cheat. A court of elders can sanction.
- **Measure:** births per household against nursery hours given; the share of nursery hours given versus received by household; whether care is directed at own children when the pool is large.
- **Scope:** **in, light** (M4 children; informal shared childcare between households is supported in Stage 4 through `CareArrangement`, [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#shared-childcare-between-households); M12 elders). A formal nursery can emerge in Stage 5 or later (owner decision Q15).

**C3. Cuckoos and host arms race (Ch6, Ch8, Ch13)**
- **Island form (optional):** a child secretly placed in another household and raised as its own. Hosts evolve ways to detect strangers, and the cheats evolve better disguises.
- **Scope:** **optional and later.** The full race (egg mimicry, red-gape super-stimulus, the life/dinner asymmetry) is **out** as a mechanic. The kin recognition exploit in C1b covers the idea.

**C4. Bearing versus caring and clutch size (Ch7, PDF 132 to 145)**
- **Island form:** a household decides how many children to have and how much to put into each. Pure caring (always staying at home) is invaded by those who only "bear" (have children and leave the work to others). A larger clutch is not better if each child gets less.
- **Makes it possible:** food and time limits on a household ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)); a trade-off between number and survival of children; the partnership and parental-care decisions ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)); no safety net unless an institution creates one.
- **Beau Geste starlings (crowding cues):** NPCs may read the local crowd (visible households, noise) as a cue to have fewer children. This is a perception-and-belief effect, not a rule.
- **Red grouse "wait and hope":** the choice between fighting for a hut or plot now and waiting to inherit one is the same decision as in the wasp example and in material inheritance.
- **Institutions:** welfare and a shared store reduce the cost of children and are expected to raise clutch size and lower investment per child, and the island can show either. Schools later change the value of investing per child.
- **Measure:** completed family size by household wealth; child survival by family size; investment per child (food and care time) by family size. Whether a "carers-only" population survives.
- **Scope:** **in, measured** (M4). No rule sets clutch size.

**C5. Parent-offspring conflict, sibling rivalry, weaning, begging (Ch8, PDF 146 to 162)**
- **Island form:** children beg for food. A child's hunger is hidden: the parent sees only the begging gesture and the child's look. Children may exaggerate (lie in begging). Parents decide who to feed, and when to stop feeding a child. A child that wants to continue after the parent would stop is the weaning conflict. A runt gets less and may give up. Grandparents help in the household.
- **Makes it possible:**
  - the `NeedFood` gesture with a quantity, whose truthfulness can differ from hunger;
  - **hidden true need:** the child's nutrition is not visible to the parent beyond posture, which perception shows imperfectly;
  - limited parental food;
  - sibling relatedness (½) in the kinship graph and the kin weight trait;
  - a cost of begging (energy, attention of predators or raiders, loud events heard by others);
  - a benefit of food that falls with the child's age.
- **Who wins** is not decided. The book rejects the claim that parents always win. The island lets weaning age be set by the parent's decider and the child's begging, and records the gap.
- **Institutions:** welfare and a shared store make begging cheaper to satisfy. A healer finds true need (a diagnosis). A school-meals scheme (later) removes the parent's control.
- **Measure:** the true need of a child (analytics read nutrition) against its begging intensity (honesty in begging); who gets fed among siblings by true need; the age at which feeding stops, parent's preferred stop point (estimated from its decider's value at that tick), and the child's last request; runt survival.
- **Scope:** **in** (M4 children and feeding, M7 lies in begging). Menopause is **measured only** (the fertility end is set by the life stage rules, [POPULATION_DESIGN.md](POPULATION_DESIGN.md#fertility)).

**C6. Fisher's sex ratio (PDF 166 to 168)**
- **Scope:** **measured only.** Sex at birth is an independent random draw, so there is no heritable sex ratio to evolve. We still record the births by sex and the investment per sex (food and care) and report the ratio, to check that nothing biases it. See [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md).

**C7. Haplodiploidy and worker sterility (PDF 194 to 203)**
- **Scope:** **out.** The genome is diploid and there are no social insects. Analogy only: Hamilton's rule (C1) is the same logic with a different relatedness table.

### 4.5 Partners (Chapter 9, PDF 163 to 188)

**D1. Origin of two sexes (PDF 164 to 166)**
- **Scope:** **out.** Two sexes are given. The principle (two classes with different investments diverge) shows up as the different costs of a child for each partner.

**D2. Desertion, the cruel bind and the Concorde fallacy (PDF 169 to 173)**
- **Island form:** after a child is born, one partner may leave. The partner left behind is "in a cruel bind". A deserted partner may seek a new partner and may try to get them to adopt the child.
- **Concorde fallacy as a rule for the AI.** The decider must ignore **sunk costs**: it scores a choice by future costs and benefits only. A partner who has put years into a household does not stay *because* of those years, but because of what staying yields from now. The same rule applies to a half-built hut, a long wait in a queue and a failing workshop. This is a design rule for the policies, and a unit test ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#10-tests-and-acceptance-criteria)).
  - Past investment can matter through its *future* effects: a reputation for staying, loyal friends, a store of goods. That is not the fallacy.
  - The measure looks for persistence beyond what future value justifies.
- **Makes it possible:** sequential decisions with a desertion option; different investment before and after birth (courtship gifts, labour); hut and household structure.
- **Institutions:** courts and customary law that enforce child support make desertion costly. Welfare makes it less costly to the left-behind partner. A bride-price or dowry (material inheritance) is a bond posted in advance.
- **Measure:** desertion rate after birth by sex; a sunk-cost index: the correlation between past investment in a task or partner and persistence, controlling for future expected value (computed from the decider's logged scores).
- **Scope:** **in** (M4 for the sunk-cost rule in the decider from M3; desertion from M4).

**D3 and D4. Coy/fast, faithful/philanderer, domestic bliss and he-man (PDF 172 to 181, endnote PDF 326)**
- **Island form:**
  - *Courtship* is gift-giving, help with building and long patience. A "fast" NPC accepts a partnership quickly. A "coy" one demands a long courtship. A "faithful" NPC courts patiently and stays to raise the child. A "philanderer" gives up courting fast and leaves after a child.
  - *Domestic bliss* is a society where courtship weeds out leavers and fathers help. *He-man* is a society where women stop expecting help and choose by quality, and a few men have most of the children.
- **Makes it possible:** partnership formation with courtship stages, gifts and help over time; sex differences in the cost of a child; the option to leave; reputation, so that a philanderer is known. The details are in [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md).
- **The real dynamics cycle.** The book's own endnote says the 5/6 and 5/8 point is a knife-edge and the true dynamics cycle for ever. The island makes **no** attempt at stability, and a measured cycle is a pass for the maths and a possible finding on the island.
- **Institutions:** courts (promises of support), a marriage register (later), welfare (lowers the penalty of being left), media (reputation travels), a dowry or bride-gift custom. Each changes which strategy pays and may change the cycle's size.
- **Measure:** the shares of partnerships with long versus short courtship, and of partners who stay versus leave after birth, per year (four cells: the four combinations of coy/fast and faithful/philanderer). The time series, tested for cycles (peak count, amplitude, autocorrelation). Concentration of children among men (a Gini of paternity).
- **Scope:** **in** (lab L6 at M2; island M4). The island is expected to cycle or drift, not to settle.

**Guard/desert, the four family types (endnote PDF 325)**
- **Island form:** four household patterns after a birth: *duck* (father leaves, mother guards), *stickleback* (mother leaves, father guards), *fruit-fly* (both leave, child to others), *gibbon* (both stay).
- **Makes it possible:** the same options as D3/D4, plus someone to take a child (a hut-mate, the grandparents, an orphan scheme).
- **Institutions:** welfare and communal care shift the balance toward fruit-fly and duck. Courts shift toward gibbon.
- **Measure:** classify each household-year in one of the four patterns from the household records (who stays, who leaves, who cares for the child). Report the shares and how they change.
- **Scope:** **in** (M4, measure only).

**D5. The handicap principle (endnote PDF 332 to 336)**
- **Island form:** a costly display of wealth, such as bronze jewellery. It is cheap for the rich and costly for the poor, so it is a believable sign of quality or wealth. Others (potential partners, trading partners, lenders) use it as a signal.
- **Makes it possible:** items with a value (bronze); a hidden quality (health, wealth, skill) that others cannot see directly; partners and creditors who respond to displays; a response rule that can change. Cheap fakes (painted pottery as bronze) test the signal, and the response (inspect, ignore) is learned.
- **Institutions:** assay offices (later) and a court that punishes fraud. Sumptuary rules (state) can ban displays. Media amplify displays.
- **Measure:** correlation between display and true wealth or health (a cost-quality relation); the share of displayers who are low quality (cheats); whether responders gave more to displayers, and whether it paid.
- **Scope:** **in** (M8, after items carry a visible quality and lies exist). Fisher's runaway selection (long tails) is **out**, but measured as an "ornament inflation" trend if it appears.

### 4.6 Cooperation and reciprocity (Chapters 10 and 12, PDF 189 to 256)

**E1. Selfish herd, alarm calls, stotting (PDF 190 to 194)**
- **Island form:** during raids, NPCs run for the middle of the group. The edge is the most exposed. An NPC may call an alarm (kin help, or the selfish reasons the book gives) or keep silent.
- **Makes it possible:** positions on the node grid, an attacker that targets the nearest victim (raiders choose the nearest civilian or soldier), the options to flee and to call ([`Alarm` gesture](COMMUNICATION_AND_COGNITION_DESIGN.md#vocabulary-fixed-and-shared-at-first)), hearing of loud events.
- **Institutions:** guards and a militia change who stands at the edge. A watch (later) makes the alarm a job.
- **Measure:** the "domain of danger" of each NPC (the number of nodes nearer to it than to anyone else, computed by analytics from positions) before and during raids; whether NPCs moved toward the centre; the casualties by distance from the centre; alarm calls by relatedness of nearby NPCs.
- **Scope:** **in** (lab L12, small; island at M9 once raids hit civilians). Stotting is **out**.

**E2. Sucker, Cheat, Grudger (PDF 206 to 209, endnote PDF 344)**
- **Island form:** **helping on credit.** "Help build my hut now and I will help later." One NPC gives labour, goods or food, and the other can fail to return it. A Sucker-like NPC helps anyone. A Cheat-like NPC accepts and does not return. A Grudger-like NPC helps strangers once and never helps a cheater again. These are behaviours, not types.
- **Makes it possible:**
  - promises and obligations in social memory ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)), recorded in the *listener's* memory;
  - recognition of individuals ([perception](COMMUNICATION_AND_COGNITION_DESIGN.md#42-perception-simcognitionperception));
  - repeated meetings in a stable group (a village, a hut);
  - acts whose benefit exceeds their cost (a day of help is worth more to the builder than it costs the helper);
  - deferred exchange ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)).
- **The book's order of events** (Suckers crash, Cheats boom and fall, Grudgers take over) is expected in the lab (L7), not on the island.
- **Institutions:** a court of elders or a headman makes promises credible, so a cheat pays a fine. Informal lending with a witness makes cheating visible. Bank-like institutions (later) make deferred exchange impersonal. Gossip and media spread the cheat's name beyond those he cheated.
- **Measure:** per-NPC `ReciprocationRate`, `FirstMoveCooperation`, `RetaliationRate`, `ForgivenessLagTicks`, `ExploitRate`. Likeness to the benchmark policies (4.10). The share of unreturned favours per village-year and its trend. The time series of each fingerprint class.
- **Scope:** **in** (lab L7 at M2; island at M7, where deferred exchange and promises exist).

**E4. The Prisoner's Dilemma (PDF 226 to 231)**
- **Island form:** **simultaneous barter** in which either side can short-change. Both hand over, or one hands over and the other does not. The temptation is to keep your goods and take theirs.
- **Makes it possible:** separate hand-overs (4.1), so "simultaneous" really means each hand-over is a decision taken after seeing, or not seeing, the other's; visible goods; the ordering of outcomes (temptation, reward, punishment, sucker) arises from real goods, so it need not hold. If it does not hold, the game is not a Prisoner's Dilemma, and that is a result.
- **Institutions:** a market stall with a bench in the middle, a state market, an escrow-like third party, or a court. Each removes the temptation, changes who bears risk, or adds a penalty. Banks make deferred exchange impersonal.
- **Measure:** the share of barters where goods swapped fully, where one side short-changed, and where both did. The estimated temptation, reward, punishment and sucker payoffs from actual exchange values, to test whether the ordering holds. (Analytics can read the true values.)
- **Scope:** **in** (M5 honest barter; M7 deception).

**E5. Axelrod's tournaments (PDF 231 to 239)**
- **Island form:** **named strategies as benchmark NPC policies.** Scenarios assign benchmark policies to selected NPCs, as controls, and observe what happens in barter and help.
- **Makes it possible:** the benchmark registry ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)); a policy that acts only in exchange and help situations and defers to the default `AssociativeNpcPolicy` otherwise.
- **Honest limit:** we do not have the 63 original entries, so our round-robin uses our own set. We can reproduce the shape of the result (nice strategies do well) but not the book's scores.
- **Scope:** **lab** (L8 and L9 at M2), plus island benchmark scenarios (later).

**E6. How stable is Tit for Tat; clustering (PDF 239 to 243)**
- **Island form:** **neighbourhoods.** In a village where everyone meets the same few people, reciprocators can start from a small cluster and spread, and defectors only hurt each other.
- **Makes it possible:** spatial structure (huts, hamlets), households, kin. The `ThinkEvery` staggering and the road network keep most meetings local.
- **Institutions:** roads and markets widen the circle of strangers and lower the cluster effect. Courts replace the need for a cluster. Media make reputation global.
- **Measure:** assortment: the correlation between an NPC's fingerprint and its neighbours' (does the cooperative cluster hold at the edges); the cooperation rate by village size; the persistence of a cooperative cluster that started small.
- **Scope:** **in** (lab L9b: invasion with and without clustering; island scenario with a small reciprocator seed).

**E7. Shadow of the future, the end game (PDF 243 to 248)**
- **Island form:** an NPC about to migrate, defect, die, or be sent away stops reciprocating. A trader about to leave the market stops giving credit. A dying NPC stops giving to those who cannot repay.
- **Makes it possible:** NPCs believe things about their own horizon (migration plan, illness, a leaving date) and the decider's horizon depends on patience and on that belief; observers can see a known departure (a packed bundle, a gesture `AskJoin`).
- **Institutions:** heirs and estates ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)) extend the future past death. Loyalty to faction and household lowers the migration horizon effect.
- **Measure:** defection rate in the last N interactions before departure or death, compared with earlier. The drop in `ReciprocationRate` in the window before an announced departure.
- **Scope:** **in** (M10 for migration and defection; death from M4). Lab: L8b (a known last round).

**E8. The First World War's live-and-let-live (PDF 248 to 251)**
- **Island form:** soldier NPCs facing the same enemy unit for weeks form local truces. They return shots proportionally, shoot near rather than at, apologise for accidents, and keep predictable rituals. **State bounties** (pay per kill or per assault) break the truce.
- **Makes it possible:** soldiers are NPCs with memory of the opposing unit ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)); stable local pairs of opposing units (posted assignments that do not rotate quickly); visible, proportionate retaliation; cheap ways to show capability (the `WarningShot` intent of [MILITARY_DESIGN.md](MILITARY_DESIGN.md), a `Warn` gesture); gestures `Apologise` and `Forgive` that cross factions.
- **Institutions:** the faction policy can rotate units, post bounties, and punish fraternising (a court-martial). Rotating units destroys the repeated game. Rewards for aggression change the payoffs.
- **Measure:** the engagement rate between two opposing units over time (shots, wounds, contacts per day); the symmetry of retaliation; the share of contacts that were warning gestures; the effect on the truce of a bounty, a rotation or a morale shock, in paired seeds.
- **Scope:** **in** (M9 soldiers are NPCs; M10 cross-faction memory). Island scenario S6 in [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md).

**E9. Prisoner's Dilemmas in nature (PDF 252 and 253)**
- **Scope:** **out as mechanics.** Analogies only: bacteria in a wounded host (short future makes nastiness pay), figs and wasps, sea bass. The shadow-of-the-future example E7 covers the logic.

**E10. Vampire bats (PDF 253 to 256)**
- **Island form:** **hut-mates sharing food on unlucky foraging days.** An NPC with a lucky day shares with a hut-mate who found nothing. Most sharing is within the household and kin, but non-kin hut-mates share too, if they have lived together long.
- **Makes it possible:**
  - random foraging yields ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#foraging));
  - **food that spoils**, so a surplus costs the giver little;
  - hunger that is steeply non-linear, so a meal matters most when starving;
  - stable households and recognition of individuals;
  - the `NeedFood` gesture and the decider's choice to give.
- **Institutions:** a shared store and welfare replace the exchange. A granary does the same at village scale. Insurance-like cooperatives (later).
- **Measure:** giving by hunger of the receiver and surplus of the giver; the share of gifts to kin versus non-kin hut-mates, and whether non-kin gifts follow a past gift received (reciprocity); the effect of hut stability on giving.
- **Scope:** **in** (M4 households, M5 gifts; lab L13, small).

**E3. Cleaner fish and other mutual help (PDF 205 to 210)**
- **Island form:** **market stalls with regulars, and mimic cheats.** A trader keeps a place that customers return to. A trader known to be honest keeps customers. A "mimic" copies the look of a good stall and cheats once.
- **Makes it possible:** stalls or fixed trading places; recognition of individuals; reputation by experience; cheap-fake goods; the gestures for offers; envy, guilt and gratitude as drives or trait terms (anger exists as a later drive; thanks as a trait).
- **Institutions:** state markets and licences, a weights inspector, media, and brand names (later). Each can make a mimic's strategy more or less pay.
- **Measure:** the repeat-customer rate per trader; the share of a trader's customers who stay after a cheat event; the reputation of mimic traders and the time to be found out.
- **Scope:** **in** (M7 for reputation; stalls need enterprises and mimic goods need item quality, both M8).

### 4.7 Signals and lies, summary

| Case | Island form | Needs | Scope |
|---|---|---|---|
| Lying in begging | A child or adult claims more hunger than it has | Hidden need, begging gesture | In, M7 |
| Lying in barter | False statements of quality or promises | Visible vs true quality, `Promise` | In, M7 |
| Cheap fakes | Painted pottery sold as bronze | Visible vs true quality, inspection | In, M8 |
| Faction bluffing, **Beau Geste** | A faction shows a large force with banners and idle NPCs. Rivals read it through scouts and reports | A faction knowledge view fed by scouts and rumours; a display separate from true strength | Optional, M14 |
| Brood parasitism | A child secretly raised by others | Kin recognition, households | Optional, later |
| Mimicry | Cheats copy a trusted look | Visible marker, imperfect recognition | In, M7 (fake goods M8) |

For Beau Geste, the important rule is that the opposing faction policy reads **only** what its institutions report (layer 7 in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#6-information-layers-one-vocabulary-for-every-doc)). A bluff works only if the reports can be wrong, which is how records and rumours already work ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).

### 4.8 Memes (Chapter 11, PDF 212 to 224)

- **Island form:** imitation and cultural complexes. Habits such as storing food, sharing with hut-mates, respecting residents, giving courtship gifts and truce rituals spread by copying and teaching. A set of habits that suit each other (the book's church with god, hell-fire and celibacy) stays together and resists new habits.
- **Makes it possible:** imitation of better-off neighbours with conformity as a trait; teaching and traditions ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)); limited attention and trait slots; a benefit that depends on which other traits are held.
- **The "conspiracy of doves" returns** at the cultural level. A norm of universal restraint is open to a single deviant unless something punishes the deviation. Institutions are what punish it.
- **Foresight** (PDF 223 and 224) is partly real here: NPCs with long horizons and the ability to anticipate consequences can resist spreading habits that hurt them.
- **Institutions:** schools and elders teach selected habits. The state cannot install a habit. It can only change what gets rewarded ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)).
- **Measure:** trait prevalence by village and time; co-occurrence of traits (lift above independence); the number of stable combinations; the time a combination holds; resistance to new traits (invasion success rate); the speed of spread against the adopters' advantage.
- **Scope:** **in** (M11). Memes are a population-level measure, not a different game.

### 4.9 Scope summary

| Example | Scope | Lab | Island |
|---|---|---|---|
| Hawk/Dove | In | L1 (M2) | M5 unarmed, M9 armed |
| Retaliator, Bully, Prober | In | L2 (M2) | M5 unarmed, M9 armed |
| War of attrition | In | L3 | M5 |
| Resident/intruder | In | L4 | M3 for huts, M5 |
| Paradoxical rules | Measure and lab variant; learned conventions through M3 | L4b optional | M3 |
| Pecking orders | In | L5 | M5, M9 |
| Conflict between species | Out | none | none |
| Why not kill all rivals | In as a measure | none | M9 |
| Gene-level ESSs | Via memes | none | M11 |
| Digger wasps | In | L10 | M3 |
| Signals and lies | In | none | M7 (fakes M8) |
| Hamilton's rule, mushroom call | In | L11 | M4 |
| Kin recognition | In, cultural green-beard only | none | M4, M11 |
| Guillemot eggs | In, light | none | M4, later |
| Cuckoos, brood parasitism | Optional, later | none | later |
| Clutch size, bearing vs caring | In, measured | none | M4 |
| Parent-offspring conflict, begging | In | none | M4, M7 |
| Sex ratio | Measured only | none | M4 |
| Haplodiploidy, 3:1 | Out | none | none |
| Origin of two sexes | Out | none | none |
| Meiotic drive, parasite manipulation | Out | none | none |
| Coy/fast, faithful/philanderer | In | L6 | M4 |
| Guard/desert four types | In, measured | none | M4 |
| Concorde fallacy | In, as a policy rule | unit test | M3 |
| Handicap principle | In | none | M8 |
| Selfish herd, alarm calls | In | L12 | M9 |
| Sucker, Cheat, Grudger | In | L7 | M7 |
| Prisoner's Dilemma | In | L8 | M5, M7 |
| Axelrod tournaments | Lab and benchmark scenarios | L8, L9 | later |
| TFT weaknesses, clustering | In | L9b | M7 |
| Shadow of the future | In | L8b | M10 |
| Live-and-let-live | In | none | M9, M10 |
| Vampire bats | In | L13 | M5 |
| Cleaner fish | In | none | M7, M8 |
| Memes | In | none | M11 |
| Beau Geste bluff | Optional | none | M14 |

**On genes and fitness.** In the book, payoff turns into the next generation's gene frequencies. On the island, payoff turns into survival, health, goods and reputation. Those influence who has children (M4), who is imitated (M11) and what each NPC learns (M3). All three routes exist, none is the book's exact route. So the island is a test of whether the book's logic holds when its routes are mixed.

### 4.10 Measuring strategies in the island

**Fingerprints (3.2) are the main tool.**

| Fingerprint | Definition | From which events |
|---|---|---|
| `EscalationRate` | Contests where escalation was possible and chosen, over contests where it was possible | `ContestMove` |
| `FirstMoveCooperation` | In the first exchange or help with a given partner, the share where this NPC made the cooperative hand-over first or gave | `ExchangeStep`, `GiftGiven` |
| `ReciprocationRate` | After a partner cooperated with it, the share where it cooperated at the next chance (within `ReciprocationWindow`, default 3 game days) | same |
| `RetaliationRate` | After a partner defected, the share where it defected or refused at the next chance | `ExchangeStep`, `PromiseBroken` |
| `RetaliationLagTicks` | Median ticks from a partner's defection to this NPC's first defection or refusal | same |
| `ForgivenessLagTicks` | Median ticks from the last defection by a partner to this NPC's next cooperation with it. -1 if it never cooperated again | same |
| `HonestyRate` | Checkable statements that matched the truth, plus promises kept, over all checkable statements | `GestureMade` with its truth field, `PromiseMade`, `PromiseBroken` |
| `PersistenceMean` | Mean ticks held in time-only contests | `ContestMove` |
| `YieldToResident` | Yielded when intruder, over contests where the other was resident | `ContestMove`, role fields |

**Rules:**
- Stored as metrics only: rows in the warehouse and JSONL, never in `GodState`.
- A rate needs at least `MinObs` observations. Thin rows get -1 so analysts can exclude them.
- **Truth fields in events.** Events may carry analytics-only truth fields, such as whether a message was a lie (`GestureMade.Truthful`), whether a promise was meant (`PromiseMade.Intended`) or whether a record was falsified. The sim stamps them at emit time from true state. **Policies never read them** (policies cannot read events at all, and the reflection test covers every observation type), and recorders store them **only at the `Full` recording level** ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#41-recording-levels)). Dropping them never changes outcomes. Measures that need them (`HonestyRate`, begging honesty, detected-lie shares) therefore need a `Full` run.

**Benchmark likeness (shadow policies).**
- To say that an NPC "behaved like Tit for Tat", replay the NPC's recorded decisions against each benchmark. At each decision, the shadow benchmark gets the same history with that partner and says what it would have done. Likeness is the share of decisions where the NPC did the same, as permille, only over decisions where the benchmark's rule gave a definite answer.
- This is a measurement, never a label. An NPC can have a high likeness to Tit for Tat and also to Grudger, since they agree in many histories. Report the whole vector.
- Shadow policies use the registered benchmark code ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#4-mechanics-and-algorithms)) on a read-only view of the pair history. They have no random draws (a random benchmark is skipped).

**Population-level measures:**
- the distribution of each fingerprint (histogram, quartiles), by village, faction, age group and class;
- the mean fingerprint by institution present or absent (for experiments);
- the correlation between fingerprints and outcomes (wealth, health, survival, children), as description only;
- clusters: groups of NPCs whose fingerprints are close, as a way to see "types" that nobody defined.

### 4.11 How institutions shift payoffs

Institutions change the strategic environment. They never decide it, and their staff keep their own motives ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)). The table gives the intended **channel**; the sign of the effect is an expectation to be tested over seeds, and perverse effects are possible.

| Institution or policy | Which payoff term it changes | Games affected | Expected direction | Possible perverse effect |
|---|---|---|---|---|
| **Courts** (headman's court in Village, courts in Town) | Make promises credible: a broken promise now carries a fine and a record. Lowers the temptation to defect | Prisoner's Dilemma, Sucker/Cheat/Grudger, desertion, lies in barter | More deferred exchange between strangers; fewer Grudger-like refusals | Costly access, bribery, enforcement of unfair contracts |
| **Police and guards** | Raise the cost of aggression: the expected penalty on top of injury. Make resident/intruder a rule | Hawk/Dove, bullying, theft, resident/intruder | Fewer escalations; stable property | Goodhart: arrests as a target raise false accusations and extortion |
| **Healer** | Lowers the cost of injury and sickness | Hawk/Dove, parent-offspring conflict | More escalation (injury is cheaper); better child survival | Moral hazard |
| **Welfare and shared store** | Lower the stakes of hunger: the prize shrinks and the cost of an unlucky day shrinks | Hawk/Dove, vampire bats, begging, clutch size | Fewer contests over food; less household sharing; larger families | Crowding out of kin help and reciprocity |
| **Media** | Spread reputation beyond personal contact. Make liars known | Cheats, lies, cleaner-fish mimics, handicap signals | Cooperation among strangers rises | Biased or paid media spread false reputation |
| **Banks, formal lending** (Town) | Make deferred exchange impersonal: credit without a personal tie | Sucker/Cheat/Grudger, shadow of the future | Credit between strangers; weaker local clusters | Debt traps, default cascades |
| **Informal lending, elders, mediation** (Band) | Witnesses and a mediator for a dispute | Cheats, resident/intruder, begging | Cheaper settlements | Elder bias |
| **Property register, land law** | Make residency a recorded fact | Resident/intruder, digger wasps | Less entering of occupied huts | Recorded claims favour the powerful |
| **Schools, teachers** | Teach selected habits and raise the value of investing in a child | Memes, clutch size, kin | Habits spread faster | Teaching what the teacher likes, not what helps |
| **Military policy** (rotation, bounties, courts-martial) | Break or build the repeated game between opposing units. Add a prize for killing | Live-and-let-live, Hawk/Dove between units | Rotation and bounties break truces | Over-rotation loses local knowledge |
| **Taxes, wages, purchase offers** | Change the value of work against holding, hauling and gifts; change what a surplus is worth | Sharing, bearing vs caring, handicap displays | Variable | Evasion, hoarding |
| **Statistics** (City) | Change what the state *believes*, not what happens | All games the state steers | A real view of what is happening | Targets distort the measure (Goodhart) |

Each row is a hypothesis for [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md). The design for such experiments (treatments, seeds, controls, causal versus descriptive) is there, not here. This document supplies the measures.

## 5. Interactions with other systems

| System | What this layer needs from it |
|---|---|
| Population ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)) | Traits as decision biases; hunger and fear drives; huts; foraging variability; life stages |
| Cognition ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)) | The decider with seeded noise; sunk-cost-free scoring; gestures and lies |
| Learning ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)) | Lifetime learning of conventions and tells |
| Social ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)) | Per-pair memory, promises, reputation, forgiveness |
| Households and kinship | Partnership, desertion, kin weight, relatedness |
| Economy | Separate hand-overs, item quality, stalls, deferred exchange |
| Military | Duel code, truces, bounties |
| Institutions and state | Payoff changes in 4.11; policy experiments |
| Telemetry and warehouse | Events and snapshots (section 7) |
| Lab ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)) | Benchmark policies and the lab experiments |

## 6. Player and view

- **The player sees only what the state's reports show** (layer 7). Game-theory measures are an analyst's tool. In stage 4 the HUD may show a few plain numbers (contests this week, unreturned favours, share of households with a deserting partner) through the reports panel at M13. It never shows strategy labels on people.
- **The NPC inspector** shows the NPC's own view and its last decision with reasons. It does not show its fingerprint or likeness.
- **A separate analyst view** (editor or runner report, not the HUD) shows fingerprints and the benchmark-likeness vector. It is a debugging aid, off in play.
- **Animations** show contest moves, gestures and hand-overs from events, and decide nothing.
- **The lab has no view.** It runs headless and writes reports. A small chart tool in the Unity editor may plot lab output (later).

## 7. Telemetry

**Domain events** used (new ones marked; the others are already in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#8-domain-events-and-snapshots)):

| Event | Fields | Used by |
|---|---|---|
| `ContestStarted` (new) | ContestId, kind, NpcA, NpcB, roles, prize, tick | A1 to A6 |
| `ContestMove` (new) | ContestId, turn, side, move, gesture | A1 to A6 |
| `ContestEnded` (new) | ContestId, outcome, ticks spent each side, injury each side | A1 to A6 |
| `ExchangeStep` | exchange id, from, to, items, step number, completed | E2, E4, E10 |
| `GiftGiven` | from, to, items, tick | C1, E10 |
| `PromiseMade`, `PromiseBroken` | ids, what, due tick | E2, E7 |
| `GestureMade` | act, speaker, target, topic, **`Truthful` (analytics only, `Full` level)** | honesty, begging |
| `HouseholdJoined`, `HouseholdLeft` (owned by HOUSEHOLDS_AND_KINSHIP) | npc, household, tick, reason | D3, D4, guard/desert |
| `NpcBorn`, `NpcDied` | ids, parents | C4, C5 |
| `HutBuilt`, `HutEntered` (new) | npc, hut, was it in use (analytics only, `Full` level) | wasps |
| `TruceFormed`, `TruceBroken` (emitted by MILITARY) | units, tick, cause | E8 |
| `FakeSold`, `FakeDetected` (new) | seller, buyer, item, tick | cheap fakes |
| `ConventionObserved` (new, analytic) | village, convention id, share following | A9, memes |

**Snapshots:**
- the yearly `NpcFingerprint` row (3.2) and the benchmark-likeness vector;
- the yearly household-type row (the four patterns);
- the daily hunger-and-surplus snapshot per household, for the vampire-bat and Hamilton measures;
- the group-level dominance table (who beat whom) for pecking-order linearity.

**Lab output** is separate from the sim stream: it goes to its own JSONL (and, from M13, the warehouse tables `fact_lab_match` and `fact_lab_population_step`). See [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#7-telemetry). It never enters a game's `StateHash`.

**Recording levels:** all of this can be configured down to nothing for training runs. Truth fields and other analytics-only fields are stored only at the `Full` level.

## 8. Stage 4 minimum versus later

| Milestone | What the game-theory layer gains |
|---|---|
| M2 | Lab only: L1 to L13, benchmark policies as plain classes, reports. No island changes |
| M3 | Sunk-cost-free scoring in `AssociativeNpcPolicy`; hut building and entering (wasps); learned dominance conventions |
| M4 | Households, partnership and desertion records; kin help with estimated relatedness; children and begging (honest); fingerprints v0 from events |
| M5 | Contests with gestures (Warn, Defend, Surrender) and unarmed escalation, honest barter and gifts; vampire-bat sharing; first island scenarios with benchmark policies |
| M7 | Deferred exchange, promises, lies, reputation: Sucker/Cheat/Grudger and the Prisoner's Dilemma become possible; benchmark-likeness |
| M8 | Item quality with cheap fakes; handicap signals; stalls with regulars and mimics |
| M9 | Armed contests (issued kit), raids (selfish herd), local truces |
| M10 | End-game effects (migration, defection) |
| M11 | Memes and cultural complexes |
| M12 | Band-age institutions as payoff changers (elders, shared store, guards, informal lending) |
| M13 | Fingerprints and likeness in the warehouse; a few plain KPIs in the reports panel |
| M14 | Faction-level bluffing (optional); balance without forcing outcomes |
| Later stages | Courts, police, banks, media, schools, statistics (Stage 5 to 7): the full institution table in 4.11 |

**Out for all of stage 4:** haplodiploidy, origin of two sexes, meiotic drive, parasite manipulation, full cuckoo arms race, genetic green-beard, stotting, Fisher runaway.

## 9. Migration from the existing code

- **Duel code is reused unchanged.** `Duel.Resolve` and `DuelSim.Fight` give injury outcomes for contests. They need the `Unarmed` profile and the move from `MilitaryTuning` statics to `SimTuning` (M0).
- **Nothing in the stage 3 game is a game-theory mechanic.** `DuelSim` is used only for balancing today.
- **No code exists yet** for contests, fingerprints or benchmarks. All are new modules: contests in `Sim/Social/Contests`, `FingerprintBuilder` in `Sim/Telemetry`, benchmarks and the lab in `Sim/Lab` and `Sim/Policy`.

## 10. Tests and acceptance criteria

All automatable, and also runnable under `dotnet test DotNet`.

**Principles:**
1. **No labels.** A reflection test checks that `Npc` and every type reachable from a policy's observation have no field of a benchmark-strategy type, no enum value named Hawk, Dove, Sucker, Cheat or Grudger, and no `Fingerprint` type. Benchmark policies are the only types that mention those names.
2. **Measurement never feeds back.** Run a scenario with recording on and off: the final `StateHash` matches at every checkpoint.
3. **No forcing.** A scan (code search in a test) finds no reference to a target frequency (7/12, 5/6, 5/8) outside `Sim/Lab` and tests.

**Contests and duel use:**
4. A contest between two NPCs with a fixed seed replays identically.
5. An `Escalate` move calls `Duel.Resolve` with the forked stream and applies exactly its damage (a unit test with stubbed stats).
6. A contest that runs to the end ends in at most `MaxTurns` plus fight length, and never loops.

**Fingerprints:**
7. On a hand-built event stream of known behaviour (five defections, three retaliations), `RetaliationRate` equals 600 permille and `RetaliationLagTicks` equals the known median.
8. A rate with fewer than `MinObs` observations is -1.
9. Replaying a Tit for Tat benchmark policy and measuring its fingerprint: `FirstMoveCooperation` above 950, `ReciprocationRate` above 950, `RetaliationRate` above 950. A Grudger: `ForgivenessLagTicks` is -1 after a defection. An Always Defect policy: `FirstMoveCooperation` 0.
10. The likeness of a Tit for Tat policy to the Tit for Tat shadow is above 990. Its likeness to Always Defect is below 500 in a population that includes cooperation.

**Concorde:**
11. `AssociativeNpcPolicy`'s score for continuing a task does not change when the amount already spent changes (a property test over sunk amounts), while it does change with remaining cost.

**Island direction tests (many seeds, later milestones).** Each uses at least 20 seeds and a one-sided sign test at 5 per cent. They state a *direction*, not a number. They are in [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#4-mechanics-and-algorithms) section 4.7.

**Acceptance for this document at M2:** the lab experiments in the benchmarks document pass. For the island, there is **no** acceptance test that a book result appears.

## 11. Open questions

Settled in review (26 Nov 2026): the contest mechanic is owned here and coded in `Sim/Social/Contests`; civilians fight with the `Unarmed` profile; item quality is owned by ECONOMY_DESIGN; truth fields are analytics-only and stored at the `Full` level; benchmark likeness is computed by the analysis job for every adult each sim year in runs up to 1000 NPCs, and for a deterministic 1-in-10 sample by `NpcId` above that.

No open questions remain.

**Owner decision Q15 (27 Nov 2026, [conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)): shared nurseries.** Yes. Informal shared childcare between households is supported in Stage 4 ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#shared-childcare-between-households)); a formal institution can emerge in Stage 5 or later. Individuals can cooperate in raising children, share caregiving and potentially exploit such arrangements, which is what the guillemot case (C2) needs. Biological parents are not the only possible caregivers.
