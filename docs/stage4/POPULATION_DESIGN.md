# Population design (stage 4)

This document covers what an NPC is: its body, drives, life cycle, household, personality and citizenship. How an NPC thinks and talks is in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md), and what it remembers about others is in [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md). Milestones are in the [plan](../UNITY_POC_PLAN.md#milestones-each-leaves-a-working-tested-game).

## Principles
- **One person, one record, for life.** The same NPC forages, hauls, builds, trades, enlists, deserts and migrates. No unit or walker is ever spawned to do a job.
- **All numbers are integers.** Drives run 0–1000 (permille). Time is in ticks. Every NPC has its own random stream, `Rng.Fork("npc", id)`.
- **Fixed order.** NPCs are updated in ascending id order. Decisions are staggered: NPC *i* thinks on ticks where `(tick + i) % ThinkEvery == 0` (default `ThinkEvery` 10, about once a second). Reflexes, such as fleeing an attack or finishing a step, run every tick and are cheap.
- **Budget.** The goal is 300–600 NPCs on the island, with the headless sim at least 20 times real time on one core. M0's runner measures this. If it is too slow, NPCs far from any event think less often; see "Level of detail" in [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md).

## Data (`Sim/Population`)

```text
Npc
  Id, Faction (citizenship), Born (tick), Sex, LifeStage (child/adult/elder), Alive
  Node, Path, StepProgress           // walks the node grid; roads are faster
  Body: Health, Injury, Energy, Nutrition, Warmth   (0-1000)
  Drives: int[DriveCount]            // see below
  Carry: ItemStack[CarryCap]         // personal inventory in hand (capacity by strength)
  HomeHut (building id or 0), Household (id or 0)
  Personality: sbyte[TraitCount]     // fixed at birth (inherited and mutated), see below
  Skills: byte[SkillCount]           // grow with practice
  Role: None/Worker/Soldier/Envoy/Child; CurrentJob (contract id or 0)
  Mind: belief store, social memory, learned weights  (Cognition and Social modules)
  Policy: name of the INpcPolicy driving it (registry key)
```

**Item stacks** are a ware plus a count. Later they also carry a quality value (0–3) and an owner id, so goods can be stolen and fakes passed off.

## Drives
- **How they work:** drives rise or fall each tick from what the body is doing and what is around. Each drive has a rate, a comfort band and a pain threshold.
- **Learning signal:** a falling drive is reward and a rising drive is punishment. This is the learning signal in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md), borrowed from *Creatures*.

| Drive | Rises with | Falls with | Minimum viable version |
|---|---|---|---|
| Hunger | Time, work, cold | Eating food items | yes |
| Fatigue | Time awake, work, walking | Sleeping (faster in a hut) | yes |
| Cold | Night and weather outdoors | Hut, fire, clothing (cloth, leather) | yes |
| Fear | Seeing enemies, being hit, seeing death | Distance, being in a group, shelter | yes |
| Pain | Injury | Rest, care | yes |
| Loneliness | Time without friendly contact | Gestures and help from friends, sharing a hut | later |
| Boredom | Repeating the same task | Change of task, play | later |
| Status | Seeing richer neighbours, losing contests | Owning tools, goods and buildings; winning; being thanked | later |
| Anger | Being cheated, struck or robbed | Time, retaliation, an apology accepted | later (M6) |
| Desire for a partner | Adulthood, time alone | Partnership | later (M10) |

Hunger at its top level damages health, and an NPC starves at zero health. Fatigue at its top level makes an NPC fall asleep on the spot.

## Life cycle
- **Minimum viable (M3):**
  - The starting population is placed at each HQ, as adults with random personalities.
  - The population grows by **immigration**: a faction with free beds and food surplus attracts newcomers at the island's edge, at a rate set by tuning.
  - NPCs die of starvation, wounds or old age.
- **Later (M10):**
  - Partnerships and children.
  - A child is fed by its household and does light work.
  - It becomes an adult at a set age, inheriting a mix of both parents' personalities with small mutations.
  - Old age lowers strength and speed.
  - When an NPC dies, its goods pass to its household, or become ownerless.

## Households and huts
- **Building a hut:**
  - An NPC without a bed looks for a hut with room. If none is known, it plans to build one.
  - A small sleeping hut needs logs, plus clay later, and can be built by one NPC or several together.
  - Each builder who contributes labour or materials gets a **sleeping right** there. This is communal ownership, recorded per hut (see [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#property)).
- **Using it:**
  - A hut has beds (3 to start) and a small shared store.
  - Sleeping in a hut lowers fatigue faster and stops cold.
- **Households:**
  - The NPCs sharing a hut form a **household**: a natural group for sharing food (the vampire-bat example) and for kin later.
  - Households can turn out someone who doesn't contribute. That is an NPC decision, not a rule.
- **The state's role:** the state can build public housing with treasury goods and NPC labour, and assign or rent the beds.

## Foraging
- **Wild food:**
  - Wild food sources are placed on hexes by terrain: berries and roots in forest and hills, fish on the coast, game later.
  - Each source has a stock that regrows over time.
  - An NPC must see a source, or remember one, to use it.
  - Yields are random, so some days are lucky and some aren't, which the sharing examples need.
- **Farms** (the existing building) yield far more food but need work.
- **Food spoils:** a carried food item loses freshness and rots after a set time. This stops endless hoarding and makes sharing a surplus cheap.

## Personality
- **Traits:** each NPC has about 12 fixed traits, each from −100 to 100. They are **biases** in the decision maker, never labels.
- **Strategies are measured, never set:**
  - Benchmark strategies such as "Grudger" exist only as separate test policies (see [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)).
  - Metrics can later *measure* how Grudger-like an NPC behaved. See [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#measuring-strategies-in-the-island).

| Trait | Biases |
|---|---|
| Aggression | Escalating in contests, threatening |
| Boldness | Taking risks; fear rises more slowly |
| Patience | How long it waits (war of attrition, courtship); weight of future reward |
| Generosity | Giving gifts, sharing surplus |
| Trustfulness | Starting trust towards strangers |
| Vengefulness | How long a grudge lasts; urge to retaliate |
| Forgiveness | Accepting apologies; trust recovering |
| Honesty | Reluctance to lie or break promises; guilt (an internal cost) |
| Loyalty | Weight on faction and household; reluctance to migrate |
| Conformity | Copying what neighbours do (imitation) |
| Industry | Willingness to work for pay; tolerance of boredom |
| Kin weight | How much relatives' welfare counts (Hamilton's rule, M10) |

**Inheritance and mutation (M10):**
- A child's trait is the average of its parents' traits, plus Gaussian-like noise built from the sum of 4 integer random draws, clamped.
- **Culture** (imitation, M10) adds a second channel. Learned habits and weights spread by copying, while traits stay genetic.

## Skills
- Gathering, building, hauling, each craft, fighting and negotiating, each 0–255.
- A skill grows with practice and fades slowly with disuse, and changes work speed and quality.
- Skilled work is what makes high wages and private workshops worth having.

## Citizenship and loyalty
- **Citizenship:** an NPC is a citizen of one faction (`Faction`). Citizens pay that faction's taxes, can take its jobs and are protected by its soldiers.
- **Loyalty:**
  - A per-NPC value of 0–1000.
  - It is rebuilt each day from the NPC's own experience: how well fed, housed, paid and safe it was, how taxed and how treated by soldiers; its household and friends; and what it believes about other factions.
  - Those beliefs come from rumours and from envoys or migrants it has met.
  - It is never read from true global state.
  - The formula and its levers are in [STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection).
- **Changing sides:**
  - **Migration:** an NPC leaves with its goods, walks to another faction's HQ and gestures to be let in.
  - **Defection:** a soldier changes sides in the field.
  - **Capture:** an HQ is taken. Its residents stay and become the conqueror's citizens, with loyalty reset low.
  - The receiving state decides whether to accept, through its immigration policy.

## Migration from stage 3
- **M3:** NPCs are added beside the walkers, with `GameSetup.Population` set. Without the flag, the stage 3 game is unchanged and its tests still pass.
- **M5:** carriers, builders and harvesters become NPC jobs; see [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#jobs-and-the-end-of-walkers).
- **M8:** soldiers become NPCs; see [MILITARY_DESIGN.md](MILITARY_DESIGN.md).
- **After M5:** the flag is on by default. The stage 3 mode stays available for the old tests until M11, then it is removed.

## Views (Unity)
- **NPC models:** one model, tinted by faction, with simple procedural poses: walk, carry, work, sleep, eat, fight, and the gestures in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#gesture-language).
- **The NPC inspector card**, opened by clicking an NPC. It shows the NPC's **own view**:
  - its drives;
  - what it carries;
  - its job;
  - a few strongest relationships;
  - its last decision and why.

  It never shows true world facts that the NPC doesn't know.
- Everything shown comes from sim events and state; animations decide nothing.

## Minimum viable version vs later
- **Minimum viable (M3–M5):**
  - adults only, with immigration;
  - five drives: hunger, fatigue, cold, fear, pain;
  - foraging and farms;
  - shared huts with sleeping rights;
  - personality traits as decision biases;
  - basic skills;
  - citizenship with a basic loyalty value.
- **Later:**
  - more drives: loneliness, boredom, status, anger, partner;
  - families and children;
  - inheritance and culture;
  - old age;
  - item quality and ownership tags;
  - weather.
