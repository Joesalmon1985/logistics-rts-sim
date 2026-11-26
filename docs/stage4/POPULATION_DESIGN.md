# Population design (stage 4)

This document owns the NPC record, its body, drives, **life cycle and population dynamics**, personality traits, skills and citizenship. Terms, time model and milestones follow [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md).

## 1. Purpose and scope

**Owns:**
- the `Npc` record, body, drives, skills, citizenship and loyalty;
- the full life cycle: birth, childhood, adulthood, ageing, death (a Stage 4 foundation, **M4**);
- population dynamics: fertility, mortality (starvation, injury, disease, old age), immigration and emigration, carrying capacity from food, the demographic pyramid;
- the personality trait table (the traits come from the genome).

**Links, does not repeat:**

| Topic | Owner |
|---|---|
| Genome, gene-to-trait map, recombination, mutation, relatedness | [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) |
| Partnership, huts and households, child-rearing, care of relatives, fostering, the kinship graph, desertion | [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md) |
| Lifetime learning: instincts as priors, associative weights, forgetting | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| Perception, beliefs, the decider, gestures | [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) |
| Memory of other NPCs | [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) |
| Property passed on at death | [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) |
| Teaching and culture | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |

## 2. Principles
- **One person, one record, for life.** The same NPC is born, grows up, forages, hauls, builds, trades, enlists, deserts, migrates, ages and dies. No unit or walker is ever spawned to do a job.
- **All numbers are integers.** Drives run 0–1000 (permille). Time is in ticks. Ages are in sim years (milli-years in code). Every NPC has its own stream, `Rng.Fork("npc", id)`.
- **Fixed order.** NPCs update in ascending id order. Decisions are staggered: NPC *i* thinks on ticks where `(tick + i) % ThinkEvery == 0` (default 10, about once a second). Reflexes run every tick.
- **Nothing sets the population.** No target, cap or quota. Births, deaths and migration come from bodies, food, decisions and events. Carrying capacity is a measured result.
- **Causes are physical.** Every death has a cause recorded from the body or the world (starvation, wound, fever, age), never "culled".
- **Budget.** 300–600 NPCs, headless at least 20 times real time on one core (measured by M0; see [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)). Daily jobs (ageing, disease, demography) run once per sim day, spread over the day by `id % 8`.

## 3. Data structures (`Sim/Population`)

```text
Npc
  Id, Faction (citizenship), Sex, Alive
  BornTick, DiedTick, DeathCause
  Mother, BioFather            // ids; 0 = founder/immigrant. Also in PedigreeRecord (see BIOLOGICAL_INHERITANCE_DESIGN)
  Genome                       // immutable; owned by BIOLOGICAL_INHERITANCE_DESIGN
  Phenotype                    // derived from genome: physical traits, personality[12], instinct gains
  LifeStage (Child/Adult/Elder), MaxAgeMilli (from Lifespan phenotype)
  Node, Path, StepProgress
  Body: Health, Injury, Energy, Nutrition, Warmth    (0-1000), GrowthPermille (child size, 0-1000)
  Drives: int[DriveCount]
  Carry: ItemStack[CarryCap]
  Household (id or 0), HomeHut (building id or 0)    // detail in HOUSEHOLDS_AND_KINSHIP_DESIGN
  Pregnancy? { BioFather, DueTick, ConceptionNutrition }
  Illness   { Kind, Phase (None/Infected/Recovered), PhaseDays, Severity }
  Skills: byte[SkillCount]
  Role: None/Worker/Soldier/Envoy/Child; CurrentJob (contract id or 0)
  Mind: beliefs, social memory, learned weights        // Cognition, Social, Learning modules
  Policy: name of the INpcPolicy driving it (registry key)

PopulationState
  Npcs (sorted by Id), NextNpcId, Pedigree (see BIOLOGICAL_INHERITANCE_DESIGN)
  Ledger: per faction and year: Births, Deaths[cause], Immigrants, Emigrants
  Tuning (in SimTuning.Population)
```

### Memory budget per NPC (combined)
The fixed-size parts of one living NPC, at default caps. Each owning document sets and tests its own part; this table is the total that the performance targets in [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md) assume.

| Part | Owner | Bytes (default) | Cap |
|---|---|---|---|
| Core record: ids, body, drives, carry, skills, pregnancy, illness, role, path state (estimate) | this document | about 256 | 320 |
| `Genome` (64 loci × 2 alleles) | [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) | 128 | 128 |
| `Phenotype` | [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) | about 48 | 64 |
| `PedigreeRecord` (kept after death while a relative lives) | [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) | 16 | 16 |
| Kin beliefs (32 × 16 B) | [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md) | 512 | 512 |
| `Mind.Beliefs` (64 × 24 B) | [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) | 1,536 | 1,536 |
| Learning: association store, traces, episodes (9,856), imitation `SeenVerbs` (80) | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) | 9,936 | 10,240 |
| Social memory (`SocialMind`) | [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) | 3,888 | 4,096 |
| Culture (`CultureStore`) | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) | about 270 | 280 |
| Political interests, grievances, legitimacy beliefs (from M10) | [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) | about 800 | 1,024 |
| Perceived standing (`PerceivedStanding` in `Mind`) | [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md) | about 32 | 32 |
| Estate header (a will, about 108 B, only for NPCs who make one) | [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) | 20 | 20 |
| Outlet trust (8 outlets, City age) | [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) | 0 in Stage 4 | 64 |
| **Total** | | **about 17,400 (17.0 KiB)** | **18,332; budget 20 KiB** |

- The observation (`NpcObservation`) is rebuilt on each think tick and not stored. A military `ServiceRecord` exists only for NPCs who have enlisted ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)) and is outside this total.
- Price beliefs (ECONOMY) live inside the 64 `Mind.Beliefs` slots and add nothing. The comms lexicon is shared and fixed in Stage 4 (0 per NPC). `MindView` stores nothing.
- 600 living NPCs use about 10 MB for these parts, well inside the per-sim memory target of the runner.
- A document that raises its cap updates this table. The test `Npc_MemoryBudget` (section 10) fails if the sum of the default caps exceeds 20 KiB.

**Item stacks** are a ware plus a count. Later they also carry an owner id and item quality (true and visible quality, owned by [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md); cheap fakes at M8), so goods can be stolen and fakes passed off.

## 4. Mechanics

### Time and life stages
Age uses the simulation calendar and the life scale ([conventions section 4](DESIGN_CONVENTIONS.md#4-time-model)):

`AgeMilli = (tick - BornTick) * LifeScale / 100 * 1000 / (TicksPerDay * DaysPerYear)`

At the defaults (1200 ticks per day, 10 days per year, LifeScale 100) one sim year is 12,000 ticks, 20 real minutes at 1x. The HUD shows `sim year × 16` as a human age. A higher `LifeScale` makes every duration below shorter. The stage and the fertile window are re-evaluated once per day.

| Stage / event | Sim age at LifeScale 100 | HUD age | Notes |
|---|---|---|---|
| Child | 0 to 1.0 | 0–16 | Fed by household; light work from 0.5 |
| Adult | 1.0 to 3.0 | 16–48 | Full work, full rights |
| Elder | after 3.0 | 48+ | Strength and speed fall; frailty rises |
| Fertile window (female) | 0.9 to `2.4 + 0.4 × Fertility/1000` (2.4–2.8) | | See Fertility |
| Natural death | `MaxAge` = 3.5 + `Lifespan`/1000 (3.5–4.5), plus a per-NPC jitter of ±0.2 | 56–72 | Hard end for old age; other causes can come first |

A generation (mean mother's age at a birth) is expected at 1.3–1.7 sim years, 25–35 real minutes at 1x, as in the [conventions](DESIGN_CONVENTIONS.md#4-time-model).

### Birth
- A child is created when a pregnancy ends (see [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#reproduction-decisions)). The genome comes from [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md#45-reproduction-of-the-genome); personality, instincts and learning rates are fixed at birth from the phenotype.
- **Placement:** the child joins the mother's household in her hut, as a member with feeding rights. Faction (citizenship) is the household's.
- **Newborn state:** Nutrition 600, Health = 700 + `Health phenotype`/5, `GrowthPermille` 300, no skills, no learned weights, no culture, no goods.
- **Birth risk:** mother death 4 per 1000 births, scaled up when her Nutrition was below 300 at the due date (up to 30 per 1000) and down with her Health phenotype. Newborn loss follows the same factors. Both are physical hazards, not a quota.
- Event `NpcBorn`, with `Mother`, `BioFather`, `Sex`, genome hash.

### Childhood
- **Needs:** a child eats at 60% of the adult rate (rising to 100% over the year) and falls asleep from fatigue sooner. It cannot forage alone before 0.5 years. Who feeds it is decided by household members ([child-rearing](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#child-rearing)).
- **Growth:** `GrowthPermille` rises with Nutrition: `+ (Nutrition - 300) / 40` per day, never below 0, reaching 1000 at about year 1 when fed (more slowly if hungry; a starved child stays small). Size, Strength and carry capacity are scaled by `GrowthPermille` until adulthood.
- **Learning:** children learn fastest: the learning rate has a 150% multiplier until year 1. Skills, language of gestures and habits come from experience, imitation and teaching ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md), [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)).
- **Light work:** from 0.5 years a child may carry small loads and gather near the hut, if its household and its own decisions say so.

### Adulthood (maturation at 1.0)
- Physical traits are fixed once, using the childhood nutrition average (see [genome section 4.3](BIOLOGICAL_INHERITANCE_DESIGN.md#43-phenotype-genes-plus-environment-plus-noise)).
- The NPC gets adult drives (desire for a partner starts), full citizenship and the right to take jobs and sleeping rights in its own name.
- Event `NpcMatured`.

### Ageing and old age
- **Elder** from 3.0. Each day an elder loses 50 permille of its adult strength and 25 of its adult speed (with 10 days a year: about 25% less strength by year 3.5 and 50% by year 4.0), and Health recovery slows by half. These rates are per day and scale with `LifeScale` like every life-cycle duration.
- **Frailty:** injury and disease risk rise 50% at elder stage.
- **Work and role:** an elder can still work at reduced pace, teach, advise, mind children. Whether the household cares for it is a decision ([care of elders](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#care-of-elders-the-sick-and-the-injured)).
- **Death at `MaxAge`:** event `NpcDied` with cause `OldAge`; the estate system runs (see [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)).

### Death
Death is always from one cause, recorded in `DeathCause`: `Starvation`, `Wound`, `Disease`, `OldAge`, `Childbirth`, `Exposure`, `Violence` (combat and murder, see [MILITARY_DESIGN.md](MILITARY_DESIGN.md)), `Accident`.

On death, once per tick in ascending id order: drop to a corpse record; household and kin are notified (observation and grief events for those who see); goods go through the estate; the household is updated; pedigree record stays. Dead NPCs are kept in an archive list (no `Mind`) for kin lookups for 4 generations, then dropped.

### Fertility
Fertility is how likely a conception is in a day. A **conception** needs all of these:
- a female NPC, in her fertile window, not pregnant, with at least 0.10 years since her last birth (so at least 0.35 years between births in all);
- a **partner** (see [partnership](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#partnership-and-courtship)) who is the biological father;
- **both partners have chosen "try for a child"** (an intent each of them makes from its own beliefs: household food, hut space, health, how the partner treats it, their drives and personalities). If either does not want it, there is no conception (families are an NPC choice);
- sufficient nutrition: the daily chance is multiplied by `NutritionFactor`, 0 at Nutrition ≤ 300, rising linearly to 1000 at ≥ 600.

`DailyConceptionPermille = (350 + 300 × FertilityPhenotype/1000) × NutritionFactor/1000 × AgeFactor/1000`, where `AgeFactor` falls from 1000 to 300 over the last 0.4 years of the window. That is 35–65% a day at best, so a couple who are trying and well fed expect a conception within about 1.5–3 days (0.15–0.3 years).
- Draw: a daily stream `Rng.Fork("fertility", npcId)` of the **mother**, evaluated at her day-slot (`id % 8`).
- **Pregnancy:** 0.25 years (about 2.5 days, 5 real minutes at 1x). During it the mother's food need rises 30%, she is slowed a little late on, and a miscarriage has a daily chance of 20 permille if Nutrition is below 200 (0 otherwise).
- **Household and partner influence:** these act through the *decision* to try, not by hidden modifiers. A starving household that wants no more children produces none, and a well-fed one that wants many produces more. Whether decisions track resources is something to measure.
- **No fixed number of children.** A rough calibration estimate at the defaults in good conditions is 2.5–3.5 births per woman. The real figure, and how many children survive to adulthood, are measured (total fertility rate and child mortality in the pyramid measures), never targeted.

### Mortality
Four physical causes act on every NPC, each from the body, in ascending id order once per day.

| Cause | Mechanism | Rates and rules |
|---|---|---|
| **Starvation** | Hunger at its top level damages Health. Nutrition 0 for a day: −60 Health; below 150: −20 (scaled by Size). Death at Health 0 with cause `Starvation`. Children and elders take 1.5× damage. Recovery of 15/day only when Nutrition > 500 | Death follows about 5–10 days of real hunger |
| **Injury** | Fights, hazardous work and accidents add `Injury`. Work hazard: foraging 1 in 4000 per work day for a 100–300 injury, building and hauling heavy loads 1 in 2000. Combat damage comes from [MILITARY_DESIGN.md](MILITARY_DESIGN.md). `Injury` above 700 at day end: death risk 30%; untreated wounds above 300 have a 5% daily infection chance that adds `Disease` damage. Rest and care halve the risk; a healer (later) removes it | Health phenotype reduces risk by up to 30% |
| **Disease** | See "Disease" below | |
| **Old age** | At `MaxAge`, see Ageing | Plus faster loss of Health from year 3 |

Child hazards are not special-cased: they follow from lower Health, smaller bodies and dependence on feeding. Expected child mortality to year 1.0 is 10–25% at default settings in a poor year and under 8% in a fed one; this is measured, not enforced.

### Disease (simple model in Stage 4; healthcare later)
One illness kind in M4 ("fever"); more kinds are data rows later.
- **States:** `None` (susceptible) → `Infected` (3–6 days, set at infection) → `Recovered` (immune for 15 days, then susceptible again).
- **Infection:** each day for each susceptible NPC: `p = Base + Contact × infectedContacts + Crowding`. Contacts are infected people who shared a hut or a work group that day. `Base` is 1 in 4000 per day (imports, bad water), `Contact` 15% per infected hut-mate and 4% per infected workmate, and `Crowding` adds 1 in 1500 per bed over capacity in the hut. Immigrants arrive infected with 2% probability.
- **Severity:** `Severity = 100 + (500 - Health phenotype) / 5 + (500 - Nutrition) / 10 + 30 if child or elder`. It is the daily Health loss. Rest, food and care by a household member reduce it by 30% (the reason sickness costs households).
- **Death:** when Health reaches 0 with cause `Disease`. A typical well-fed adult loses 100–300 Health over an illness and recovers. A hungry child or elder may die.
- **Spread and shock events** are the mechanism by which outbreaks happen. No outbreak is scripted.
- **Healthcare later (Stage 6):** healers and hospitals, treatment and prevention; M12 adds a Band-age healer institution acting as a care role. Medicine (which makes disease less deadly) is not in the sim in Stage 4.

### Immigration and emigration
- **Immigration** (M3 minimum, kept in M4): newcomers arrive at the island's edge as adults aged 1.0–2.2, with random founder genomes (see [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md#42-founder-genomes)), a small pack of food, no household. Each day, per faction, the arrival chance is `clamp(10 × (FoodDaysPerHead - 5) + 20 × FreeBeds, 0, 150)` permille, subject to `ImmigrationCap` per year (15% of population) and to the faction's acceptance rule (accept all in M4; policy later, [STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection)). `FoodDaysPerHead` and `FreeBeds` are what a visitor at the shore could plausibly see or be told; it is *not* read by NPCs but by the arrival rule. Draw: `Rng.Fork("immigration", faction)` per day.
- **Emigration:** an NPC decides to leave (by its policy): a persistent hunger drive with no household support, low loyalty, a grudge, or a belief that elsewhere is better. It walks to the island's edge and leaves, taking its carried goods. Children leave with their parents only if a parent decides so. M4 only leaves the map. Migration between factions is M10.
- **Both are events:** `NpcImmigrated`, `NpcEmigrated`, with reasons.

### Carrying capacity emerges from food
There is no cap on the number of NPCs.
- **Supply:** wild sources regrow, farms yield, stores lose food to rot (see Foraging).
- **Demand:** adults need about 1 meal unit per day (Size modifies by ±20%); children 0.6; pregnant women 1.3.
- **The brakes are physical and decided:** low Nutrition reduces conception chance ([fertility](#fertility)), raises starvation and disease, makes households decide against children, shuts the immigration rule and raises emigration.
- **Measured capacity:** `K = SustainableFoodPerDay / AdultNeedPerDay`, computed by the demography snapshot and compared with actual population. It is *measured*, not enforced. Whether the population overshoots, crashes or settles near K is a result, and the shape of the path is data.
- **Technology and institutions raise K later** (farms, granaries, trade), so capacity is not a constant.

### The demographic pyramid
- **Computed** once per sim year and per faction by read-only code in `Telemetry`.
- **Bins:** 17 age bins of 0.25 years each (0–4.25+ years), by sex: 34 counts.
- **Derived measures:** total population, share of children (under 1.0), dependency ratio (children + elders over adults), sex ratio, total fertility rate (births per woman over her fertile years), crude birth and death rates per 1000 per year, life expectancy at birth and at 1.0, child mortality to 1.0, mean generation time, growth rate.
- **Shown** in the HUD as a pyramid (see section 6).

### Drives
- **How they work:** drives rise or fall each tick from what the body is doing and what is around. Each drive has a rate, a comfort band and a pain threshold. The instinct strengths ("gains") come from the genome ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)).
- **Learning signal:** a falling drive is reward and a rising drive is punishment, which is the learning signal in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) and [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md).

| Drive | Rises with | Falls with | Version |
|---|---|---|---|
| Hunger | Time, work, cold, pregnancy | Eating food items | M3 |
| Fatigue | Time awake, work, walking | Sleeping (faster in a hut) | M3 |
| Cold | Night and weather outdoors | Hut, fire, clothing | M3 |
| Fear | Seeing enemies, being hit, seeing death | Distance, being in a group, shelter | M3 |
| Pain | Injury | Rest, care | M3 |
| Desire for a partner | Adulthood, time alone | Partnership | **M4** |
| Loneliness | Time without friendly contact | Gestures and help from friends, sharing a hut | later (M5) |
| Boredom | Repeating the same task | Change of task, play | later |
| Status | Seeing richer neighbours, losing contests | Owning tools, goods and buildings; winning; being thanked | later |
| Anger | Being cheated, struck or robbed | Time, retaliation, an apology accepted | later (M7) |

Hunger at its top level damages health, and an NPC starves at zero health. Fatigue at its top level makes an NPC fall asleep on the spot. Care for children and kin is not a drive: it comes from the Nurture instinct, the kin-weight trait and learning.

### Foraging
- **Wild food:** sources are placed on hexes by terrain (berries and roots in forest and hills, fish on the coast, game later). Each has a stock that regrows. An NPC must see or remember a source to use it. Yields are random, so some days are lucky and some are not, which the sharing examples need.
- **Farms** (the existing building) yield far more but need work.
- **Food spoils:** a carried or stored food item loses freshness and rots after a set time. This stops endless hoarding and makes sharing a surplus cheap.

### Personality
- **Traits:** each NPC has 12 traits, each from −100 to 100, fixed at birth. They are **predispositions from the genome**, two loci each plus a small developmental noise ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md#43-phenotype-genes-plus-environment-plus-noise)). They are biases in the decision maker, never labels. Expected heritability 0.4–0.55, so children resemble their parents but do not copy them.
- **Behaviour is not the trait.** What an NPC *does* depends on the trait, its learned weights ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)), culture and circumstance. A generous gene in a harsh household may produce a miser, and vice versa.
- **Strategies are measured, never set.** Benchmark strategies such as "Grudger" exist only as test policies ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)). Metrics can later measure how Grudger-like an NPC behaved ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#410-measuring-strategies-in-the-island)).

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
| Kin weight | How much relatives' welfare counts (Hamilton's rule; it multiplies the NPC's *estimated* relatedness) |

### Skills
- Gathering, building, hauling, each craft, fighting, negotiating and childcare, each 0–255.
- A skill grows with practice and fades slowly with disuse, and changes work speed and quality. The growth rate follows the LearnRate phenotype and age (children learn fastest).
- Skilled work is what makes high wages and private workshops worth having. Teaching of skills is in [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md).

### Citizenship and loyalty
- **Citizenship:** an NPC is a citizen of one faction (`Faction`). Citizens pay that faction's taxes, can take its jobs and are protected by its soldiers. A child's faction is its household's.
- **Loyalty:**
  - A per-NPC value of 0–1000, rebuilt each day from the NPC's own experience: how well fed, housed, paid and safe it was, how taxed and treated by soldiers, its household and friends, and what it believes about other factions.
  - Those beliefs come from rumours and from envoys or migrants it has met. It is never read from true global state.
  - The formula and levers are in [STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection).
- **Changing sides:** migration, defection and capture (M10). The receiving state decides whether to accept.

## 5. Interactions with other systems
- **Biological inheritance:** genome, phenotype, relatedness: [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md).
- **Households and kinship:** partnership, huts, rearing, care, fostering, desertion: [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md). Fertility here needs a partnership and a joint decision there.
- **Learning and culture:** [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) owns instincts and learned weights (children start with zero weights); [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) owns teaching.
- **Economy:** food, items, jobs, and the economy tests that decide capacity: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md). Deaths feed the estate: [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md).
- **State:** loyalty, immigration acceptance, welfare: [STATE_DESIGN.md](STATE_DESIGN.md). Healers and welfare as band-age institutions: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md).
- **Military:** violent death and wounds: [MILITARY_DESIGN.md](MILITARY_DESIGN.md).
- **Cognition:** NPCs see only their own observation: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md).

## 6. Player and view
- **NPC models:** one model per sex, children scaled by `GrowthPermille`, elders stooped, tinted by faction, with visible variation from the appearance traits. Procedural poses: walk, carry, work, sleep, eat, fight, ill (slumped), and the gestures in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#49-gesture-language-simcomms). Family poses (feeding, carrying a child) are in [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#6-player-and-view).
- **NPC inspector card** (click an NPC): the NPC's **own view**: its drives, what it carries, its job, a few strongest relationships, household, age (HUD age = sim year × 16), life stage, last decision and why. It never shows true facts the NPC does not know.
- **Population panel** (from the faction's records; `FullInformation` at first): total population, births and deaths this year by cause, immigrants and emigrants, the **demographic pyramid**, and the measured carrying capacity against the population. The panel reads through `FactionKnowledgeView`.
- Everything shown comes from sim events and state. Animations decide nothing.

## 7. Telemetry
Domain events: `NpcBorn`, `NpcMatured`, `NpcDied` (cause, age, household), `NpcImmigrated`, `NpcEmigrated`, `PregnancyStarted`, `PregnancyEnded` (birth, miscarriage, death), `IllnessStarted`, `IllnessEnded`, `InjuryTaken`. Household events are in HOUSEHOLDS_AND_KINSHIP_DESIGN.md; genetics events in BIOLOGICAL_INHERITANCE_DESIGN.md.

Snapshots:
- **Daily person snapshot** adds age (milli-years), life stage, Sex, Nutrition, Health, pregnancy and illness flags, household id.
- **Yearly demography snapshot** per faction: pyramid (34 bins), births, deaths by cause, immigrants, emigrants, fertility and mortality measures, measured `K`.

Recording never changes the hash. See [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md) and [KPI_CATALOGUE.md](KPI_CATALOGUE.md).

## 8. Stage 4 minimum vs later

| Item | M3 | **M4** | Later |
|---|---|---|---|
| Adult NPCs, immigration, starvation, wounds | yes | kept | |
| Five drives (hunger, fatigue, cold, fear, pain) | yes | kept | |
| Foraging, farms, rot | yes | kept | |
| Genome, personality from genes | | yes | pleiotropy, more traits |
| Birth, childhood, growth, adulthood, ageing, old-age death | | yes | |
| Fertility with a joint decision; pregnancy | | yes | twins, contraception as culture |
| One illness (fever); injury infection | | yes | many illnesses, healers, hospitals (M12, Stage 6) |
| Emigration off the map | | yes | between factions (M10) |
| Demographic pyramid and yearly measures | | yes | |
| Desire for a partner drive | | yes | |
| Loneliness, boredom, status, anger | | | M5–M7 and later |
| Item quality and ownership tags, weather | | | M6 ownership, M8 quality ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)); weather later |
| Citizenship and basic loyalty | | yes | political interests (M10+) |

## 9. Migration from the existing code
- **M3:** NPCs are added beside the walkers, with `GameSetup.Population` set. Without the flag, the stage 3 game is unchanged and its tests still pass.
- **M4:** life cycle, households and genome added to the same record, behind `GameSetup.LifeCycle` (on with Population by default; off for tests that want ageless adults). `SimCalendar` and `LifeScale` come from M0.
- **M6:** carriers, builders and harvesters become NPC jobs ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#jobs-and-the-end-of-walkers)).
- **M9:** soldiers become NPCs ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
- After M6 the flag is on by default. Stage 3 mode stays for the old tests until M14, then it is removed.

## 10. Tests and acceptance criteria
**Sanity bands, not targets.** Population-level checks (`Capacity_Emerges`, `Stable_10Generations`, `Pyramid_SteadyState`, `Generation_Time`) are wide plausibility bands for the default tuning. A run outside a band is investigated as a possible bug or a degenerate rule. It is never fixed by adding a rule that forces the outcome, and a tuning change made for it records that it serves playability ([VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md#22-neutrality-binding-on-every-document)).

Defaults: a "default sandbox seed" is a sandbox game, `LifeScale` 100, 40 founding adults (20 of each sex, ages uniform 1.0–2.4), enough food for about 80 adults (`K` = 80), no wars, seeds from the scenario list. All tests run headless and under `dotnet test DotNet`.

| Test | Expected |
|---|---|
| `LifeStages_Boundaries` | Child before 1.0, adult 1.0–3.0, elder after 3.0 at LifeScale 100; at 200 the same ages in half the ticks; HUD age = sim year × 16 |
| `NaturalDeath_Window` | With starvation, injury and disease off, every NPC dies between 3.3 and 4.7 years (MaxAge 3.5–4.5 ± 0.2); mean in 3.9–4.1 |
| `Fertility_NeedsBothPartners` | No conception in 1,000 couple-days if either partner's decision is "no child"; with both yes and fertile, conception within 12 days in at least 90% of couples |
| `Fertility_Window` | No conception below 0.9 or above the fertility end; no birth sooner than 0.35 years after the last |
| `Pregnancy_Length` | Every full-term birth at 0.25 ± 0.005 years |
| `Starvation_Time` | An adult at Nutrition 0 with no food dies in 5–10 days; a child in 3–7 days |
| `Disease_Outbreak` | One infected adult in a hut of 6 well-fed adults: 2–5 infections in the hut; deaths under 1 in 6; a hungry hut shows at least twice the deaths |
| `Immigration_Responds` | Surplus food and free beds give at least 3× the arrivals of a starving, full faction over 5 years |
| `Capacity_Emerges` | Food supply ×1, ×2 and ×0.5: mean population over years 8–15 in ratio 1 : 1.6–2.4 : 0.4–0.6 |
| `Stable_10Generations` | Default sandbox seed, 15 sim years: never extinct, minimum population 10 or more, population at end between 0.5× and 2× of the population at year 3, no year with growth above 60% after year 3 |
| `Pyramid_SteadyState` | In a steady state: children under 1.0 are 20–40% of the population; elders (over 3.0) are under 20%; sum of bins equals the headcount |
| `Generation_Time` | Mean mother's age at birth, years 4–15: 1.1–1.8 |
| `Child_Mortality_Measured` | Reported value equals the recomputed one from events (exact) |
| `Death_HasCause` | Every `NpcDied` has exactly one cause; counts by cause sum to total |
| `Personality_FromGenome` | Personality equals the genome mapping plus noise; children's mean trait within 10 of the mid-parent mean over 500 families |
| `Replay_Deterministic` | Same seed gives the same state hash at every day boundary for 10 sim years, with recording on or off |
| `Npc_MemoryBudget` | The sum of the default per-NPC caps in the memory budget table (section 3) is at most 20 KiB; each module's measured size is at most its cap |

## 11. Open questions
- **Disease in the default sandbox.** It makes famine and crowding costly, but may be too punishing for the tutorial. Owner question; default: setting `Disease` (On, Mild, Off) with On in the sandbox and Mild in the tutorial.

Settled here: founders start with ages uniform in 1.0–2.4 (a stable-age start is a later scenario option if the founding wave proves a problem); children emigrate only with a parent who decides to leave.
