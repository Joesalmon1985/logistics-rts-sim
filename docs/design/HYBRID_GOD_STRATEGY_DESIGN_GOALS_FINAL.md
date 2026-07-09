# Hybrid God-Strategy MVP — Design Goals and Rules Decisions

## Purpose of this document

This document sets the design direction for the next major version of the hybrid logistics RTS project.

It is **not** a PRD and it is **not** an implementation prompt. Its job is to give Cursor a stable design target so that a later PRD can be written from a coherent set of decisions rather than from a loose list of ideas.

The document answers the outstanding design questions from the current project assessment and sets the final goal for the new MVP:

> A 4-player god-strategy logistics RTS where each player guides a civilisation through four ages, shapes society through infrastructure and cultural drafting, commands military action indirectly, and influences a population of evolving neural-policy workers whose behaviours adapt to the society the player creates.

The ambition is deliberately higher than a simple Widelands clone or a small RTS skirmish. The project should become a game about **civilisations as living systems**: logistics, warfare, culture, worker behaviour, ecology and god-level decision-making all feeding into one deterministic simulation.

---

## 1. Core fantasy

The player is a god guiding a civilisation.

Their people build, carry, farm, forge, trade, rest, fight, pollute, restore, flourish and collapse through delegated behaviour. The player does **not** micro every person or every unit. Instead, the player shapes the world through:

- roads;
- buildings;
- logistics networks;
- production priorities;
- cultural cards;
- civic or divine edicts;
- military stances;
- delegated attack targets;
- social incentives;
- food, leisure and safety conditions;
- pollution and collapse responses.

The civilisation should feel alive. Workers should not be anonymous conveyor belts forever. They should have needs, wants, habits, traits and learned behaviours. A society that feeds, protects and enriches certain kinds of workers should gradually produce more of those behaviours. A brutal, starving, militarised, extractive or collapsing society should also be reflected in worker behaviour.

The god does not directly train individual workers. The god creates the conditions in which worker behaviours flourish or fail.

For example:

- if a player drafts peaceful hunter-gathering cards in Age 1, unlocks buildings that support that life, provides food security and avoids overwork, then worker behaviours suited to gathering, mobility, leisure, low extraction and local resilience should become more successful;
- if a player drafts agricultural settlement cards in Age 1, unlocks farms, storage and organised labour, then worker behaviours suited to farming, routine, settlement and surplus production should become more successful;
- if a player drafts military-industrial cards in later ages, worker behaviours suited to discipline, manufacturing, logistics and risk tolerance may flourish, but happiness, safety or ecological stability may suffer;
- if a player creates a polluted collapse society, survivalist, risk-averse or hoarding behaviours may become more prevalent.

The game should let the player feel that their civilisation has a character that emerges from its material conditions, cultural choices and worker adaptation.

---

## 2. Current project reality

The existing project already has a strong technical base:

- a deterministic GDScript simulation core;
- a settlement logistics mode;
- a standalone military skirmish mode;
- a hybrid logistics + RTS mode;
- delegated hybrid military commands;
- headless tests and smoke runners;
- partial 4-player FFA tooling;
- partial neural/bot infrastructure;
- partial mobile and spectator shells.

However, the current hybrid 4-player FFA loop is not yet proven as a complete competitive game. It can load and tick, but current neural/bot evaluations do not reliably reach elimination or victory.

The next design should therefore do two things at once:

1. **Set the ambitious final target clearly.**
2. **Require staged implementation, testing and proof at every layer.**

The project should not retreat into an unambitious MVP that proves only a tiny economy loop. But it also should not pretend that neural societies, four ages, culture drafting, modular god AIs and collapse dynamics can all be safely implemented in one undifferentiated pass.

---

## 3. Design pillars

### 3.1 Delegated god control

The player gives high-level intent. The simulation resolves the detail.

The player should say:

- build here;
- prioritise this production chain;
- defend this region;
- attack this enemy structure;
- support this cultural direction;
- invest in food security;
- encourage military readiness;
- restore polluted land;
- create incentives for trade, leisure, labour or survival.

The player should not say:

- worker 143, carry this plank;
- squad 7, move to this exact hex;
- soldier 4, attack that unit;
- individual citizen, eat this specific food item;
- worker 27, become happy now.

The player acts through environment, infrastructure, culture, law, religion, incentives and strategic direction.

### 3.2 Logistics are physical

Wares do not teleport.

Roads, flags, logistics nodes, storehouses, carriers and delivery jobs are core to the game. If a player damages a rival’s logistics network, that should matter. If a player builds a sprawling civilisation without storage or transport capacity, that should stall. If collapse damages forests, food or energy supply, the effect should travel through the real production chains.

Logistics is not background bookkeeping. It is the body of the civilisation.

### 3.3 Culture is strategic technology

The 7 Wonders-style drafting layer is not just scoring decoration. Cultural cards represent technologies, institutions, beliefs and social systems.

They may unlock:

- buildings;
- units;
- production methods;
- military doctrines;
- leisure systems;
- social norms;
- worker behaviour modifiers;
- pollution effects;
- collapse mitigation;
- victory points.

Culture is how the god shapes what the civilisation values. It should affect what workers do, what buildings exist, what units can be trained, how pollution is handled and how victory is scored.

### 3.4 Workers are autonomous neural society agents

Workers are not just animated logistics tokens. The final target is for workers to use evolvable neural or policy-driven behaviour.

They should have:

- needs;
- wants;
- traits;
- habits;
- learned tendencies;
- role suitability;
- responses to culture and environment;
- different success/failure outcomes.

The society the player creates should affect which behaviours survive, spread and become common.

Worker neural policies may be refreshed every defined number of ticks. The most successful worker policies, genomes or behaviour-weight profiles can be used to seed new or updated policies for less successful workers. This does not need to mean a large independent neural network per worker. The important design goal is that worker behaviour is **adaptive and selected by the society the player creates**.

Possible implementation models include:

- shared neural policy by worker archetype;
- policy genome or behaviour-weight profile per worker;
- small neural policy plus individual traits;
- cultural imitation from successful nearby workers;
- generational replacement;
- periodic policy refresh using successful workers as templates.

The design should support worker evolution without making the simulation impossible to debug.

### 3.5 AI gods are modular minds

God AIs should not be one giant neural network. The game is too complex for a single monolithic policy to sensibly learn everything at once.

Instead, each AI god should be made from interlocking policy modules:

- economy;
- logistics;
- military;
- culture drafting;
- worker-society shaping;
- pollution/collapse response;
- high-level strategic coordination.

Each area-specific module should use a two-stage network pattern:

1. **Pre-analytic network**
   - receives a broad “whole of phenomena” snapshot for its domain;
   - identifies what matters most;
   - filters, compresses or highlights the important features;
   - passes a smaller, more meaningful representation upward.

2. **Analytic network**
   - receives the selected representation;
   - evaluates possible responses;
   - produces 0–3 recommended actions, proposals or priorities.

The high-level strategic coordination network then receives proposals from the modules and chooses which legal god action or sequence of actions should be attempted.

The overall structure should be:

```text
Domain snapshot
-> pre-analytic network
-> selected/weighted features
-> analytic network
-> 0–3 recommended actions
-> strategic coordinator
-> legal god command
-> command validator
-> simulation
```

All AI god outputs must still compile into legal player/god actions. AI modules must not directly mutate the game state.

### 3.6 Collapse is the late-game moral trap

The modern and futuristic ages introduce pollution, catastrophic weapons, resource depletion and world damage.

This system should be **emergent rather than announced**. The game should not warn the player with obvious messages like “this factory will cause collapse”. Players can draft and build factories, fossil-energy systems, heavy industry and catastrophic weapons because those choices are powerful. The consequences should appear through the simulation itself.

A player might first notice that:

- nearby forests look thinner, darker or less healthy;
- forest and biomass production quietly declines;
- farms near polluted industry become less reliable;
- workers near industrial zones become less healthy, less happy or more tired;
- medical/leisure/rest demand rises;
- polluted regions become less attractive places to live and work;
- natural disasters become more frequent;
- supply chains that once felt stable begin to fail.

Age 3 creates the prisoner’s-dilemma tension. Players can use modern industry, polluting buildings, high-output production and destructive weapons to compete for dominance. These choices may produce short-term victory points, military strength and economic power, but they also increase local and global harm.

Age 4 is where the consequences come due.

If players exploited too much modern technology without mitigation, the futuristic/collapse age should be harsher:

- wood and biomass may become catastrophically scarce;
- food systems may fail;
- workers may become less healthy and less productive;
- happiness and safety may decline;
- energy systems may destabilise;
- floods, hurricanes and other natural disasters may become more frequent;
- conflict pressure may rise;
- restoration may require cooperation;
- nuclear or catastrophic weapons may push the world toward shared loss.

The game should not simply say “modern technology is bad”. It should create trade-offs. Industrialisation can be powerful, even necessary, but unmanaged extraction and catastrophic military escalation should damage the world in ways that are visible through forests, workers, weather, production and logistics.

---

## 4. The four-age structure

The simulation rotates through four ages:

1. **Prehistoric**
2. **Historic**
3. **Modern**
4. **Futuristic / Collapse**

For the first full MVP, one match should normally cover one cycle through these four ages.

After Age 4, the cycle may return to Prehistoric only in later modes designed around repeating civilisational cycles. The first full MVP should not depend on repeated cycles.

### 4.1 Age progression

Each age has:

- its own cultural draft deck;
- 8 draft cards per player;
- age-flavoured names for buildings and wares;
- age-appropriate military unit;
- age-specific risks and bonuses;
- possible unlocks that persist into later ages;
- different pollution/collapse implications.

### 4.2 Age 3 victory check

At the end of Age 3:

- if only one surviving player has at least 10 victory points, that player wins a civilisation victory;
- if more than one surviving player has at least 10 victory points, Age 4 begins;
- if no surviving player has at least 10 victory points, Age 4 also begins.

The “only one player has 10 VP” rule represents a clear, uncontested civilisation lead. If multiple civilisations have reached that threshold, none has achieved uncontested dominance, so the game moves into the crisis age. If no civilisation has reached the threshold, the world also moves into crisis because no one has built a stable enough civilisation to settle the contest.

### 4.3 Command Hub victory

At any point in any age:

- if only one player has a surviving Command Hub or age-equivalent HQ building, that player wins by domination.

A player is eliminated when:

- they have no surviving Command Hub / HQ-equivalent; and
- they have no active Command Hub construction site.

This rule sits underneath the age system. Culture can win the game at the end of Age 3, but Command Hub domination can win earlier or later through military/logistics victory.

### 4.4 Age 4 collapse resolution

Age 4 begins if the Age 3 victory check does not produce a single clear winner.

Age 4 is the futuristic/collapse age. Its rules should be shaped by what players did in Ages 1–3, especially Age 3.

During Age 4:

- world damage becomes more important;
- pollution effects become harsher;
- resources may become catastrophically scarce;
- workers may become harder to feed, rest and keep happy;
- military escalation becomes more dangerous;
- restoration technologies and cultural cards become strategically important.

Age 4 can end in:

1. **Domination victory**
   - only one player retains a Command Hub.

2. **Restoration victory**
   - players collectively solve collapse challenges.
   - this may be a joint victory, with the highest-scoring player recognised as the leading civilisation.

3. **Survival score ending**
   - the tick cap is reached without domination or full restoration.
   - the winner is determined by survival-adjusted score.

4. **Shared collapse loss**
   - world damage exceeds an irreversible threshold.
   - no player wins.

---

## 5. MVP scope model

The word “MVP” needs to be used carefully.

This design is ambitious, but the PRD should still break implementation into proof layers.

### 5.1 First playable slice

A narrow, testable implementation proving the basic loop:

- 4 players;
- roads;
- storage;
- carriers;
- building construction;
- simple worker needs;
- one production chain;
- one military unit or one unit per age;
- delegated attack;
- basic culture cards;
- basic VP;
- age transition;
- tick cap and victory rules.

### 5.2 Target MVP

The intended complete MVP for this design direction:

- 4-player FFA;
- four ages;
- 8-card draft per player per age;
- age-flavoured logistics and military;
- Command Hub elimination;
- VP-based Age 3 victory check;
- Age 4 collapse;
- worker needs;
- worker policy interface;
- evolving worker behaviour;
- modular god AI architecture;
- headless training/evaluation gates;
- 2D playable/spectator interface.

### 5.3 Post-MVP expansion

Deferred systems:

- full 3D embodiment;
- micro spell combat;
- full diplomacy;
- deep mining/electronics/oil chains;
- large vehicle roster;
- aircraft/naval;
- polished final art;
- multiplayer networking;
- complex individual personality simulation beyond the worker-policy model.

---

## 6. Final target game loop

The match proceeds in ticks.

At a high level:

1. **Workers evaluate needs and opportunities**
   - hunger;
   - tiredness;
   - happiness;
   - safety;
   - wealth;
   - status;
   - cultural norms;
   - available jobs;
   - local danger;
   - nearby resources and buildings.

2. **Worker policies choose broad behaviours**
   - work;
   - carry;
   - build;
   - eat;
   - rest;
   - seek leisure;
   - trade;
   - train;
   - avoid danger;
   - join military;
   - follow cultural pattern.

3. **Buildings request inputs and produce outputs**
   - production queues create ware demands;
   - construction sites request materials and builder work;
   - military buildings request supplies and recruits;
   - leisure/social buildings satisfy worker needs.

4. **Logistics creates delivery jobs**
   - wares are reserved;
   - carriers move them through connected road networks;
   - blocked jobs appear in diagnostics.

5. **Players/gods issue edicts**
   - build;
   - attack;
   - prioritise;
   - draft;
   - repair;
   - defend;
   - expand;
   - reduce pollution;
   - support worker welfare;
   - push military-industrial output.

6. **Military systems resolve delegated orders**
   - stances guide squads;
   - attack targets are selected by player/god;
   - squads path, fight blockers and return to stance.

7. **Culture draft happens periodically**
   - every defined number of ticks, a draft pick occurs;
   - after 8 picks, the age’s draft completes;
   - the next age begins.

8. **Pollution/world damage accumulates**
   - mainly in Ages 3 and 4;
   - certain buildings, cards or weapons increase world damage;
   - mitigation cards and buildings can reduce damage.

9. **Worker behaviours are periodically selected/refreshed**
   - successful behaviours become more common;
   - unsuccessful behaviours become less common;
   - culture and environment influence the selection pressure.

10. **Victory or collapse is checked**
   - Command Hub domination;
   - Age 3 VP victory;
   - Age 4 restoration;
   - Age 4 shared collapse loss;
   - tick-cap survival score.

---

## 7. Cultural drafting system

### 7.1 Core rules

Each age has an 8-card draft per player.

For 4 players:

- each player receives 8 cards for the age;
- at each draft interval, each player chooses 1 card;
- hands rotate to the next player;
- after 8 picks, all players have drafted 8 cards;
- the game advances to the next age.

Drafting happens every `draft_interval_ticks`, not every normal simulation tick.

### 7.2 Card effects

A cultural card can provide one or more of:

- victory points;
- new building unlock;
- new unit unlock;
- civilisation-wide modifier;
- production bonus;
- worker behaviour modifier;
- worker need modifier;
- leisure/happiness modifier;
- pollution increase or reduction;
- collapse mitigation;
- military stance/combat modifier;
- logistics modifier;
- age transition effect.

### 7.3 Card schema

Every cultural card should have:

```text
card_id
age
name
flavour_text
card_type
vp_value
unlock_buildings[]
unlock_units[]
global_modifiers[]
worker_society_modifiers[]
pollution_delta
collapse_mitigation_delta
prerequisites[]
tags[]
rarity_or_weight
```

### 7.4 Example age themes

#### Prehistoric

- Firekeeping
- Clan Gatherings
- Stone Tools
- Cave Shrines
- Hunter Bands
- Seasonal Camps
- Oral Tradition
- Totem Guardians

#### Historic

- Written Law
- Irrigation
- Bronze Working
- City Walls
- Markets
- Temples
- Academies
- Professional Armies

#### Modern

- Factories
- Rail Logistics
- Mass Education
- Electricity
- Public Health
- Radio Command
- Mechanised Warfare
- Fossil Industry

#### Futuristic / Collapse

- Renewable Grid
- Climate Engineering
- Arcology
- Autonomous Logistics
- Refugee Compacts
- Fusion Prototype
- Ecological Restoration
- Nuclear Deterrence

---

## 8. Worker neural-society model

### 8.1 Worker types

Workers fall into three broad role groups:

1. **Carriers**
   - move wares between roads, buildings, flags and storehouses.

2. **Builders**
   - construct and repair buildings after materials arrive.

3. **Specialists**
   - operate production, leisure, cultural or military buildings.

Examples of specialists:

- woodcutter;
- sawyer;
- farmer;
- baker;
- miner;
- smelter;
- smith;
- soldier/trainer;
- leisure worker;
- healer/medic;
- teacher/cultural worker;
- restoration worker.

### 8.2 Worker needs

Workers should eventually track:

```text
hunger
tiredness
happiness
safety
wealth/resources
social belonging/status
work suitability
leisure desire
trade desire
danger avoidance
cultural alignment
```

### 8.3 Worker actions

Workers may satisfy needs through actions such as:

- carrying wares;
- working a production job;
- building;
- repairing;
- resting;
- eating/consuming food;
- visiting leisure buildings;
- selling wares;
- travelling to a better workplace;
- joining military service;
- fleeing danger;
- following cultural norms;
- imitating successful nearby workers.

### 8.4 Worker policy interface

Workers should eventually choose actions through a policy interface:

```text
WorkerObservation
WorkerLegalActionMask
WorkerPolicy
WorkerAction
WorkerStepResult
```

The policy should output broad action categories rather than tiny animation commands.

Example action categories:

```text
WORK_ASSIGNED_JOB
SEEK_FOOD
REST
SEEK_LEISURE
SEEK_SAFETY
TRADE_OR_SELL
TRAIN_SPECIALISM
JOIN_MILITARY
MOVE_HOME
IMITATE_SUCCESSFUL_WORKER
IDLE
```

### 8.5 Worker policy refresh

At defined intervals, the game may refresh or evolve worker policies.

Possible rule:

```text
Every worker_policy_refresh_ticks:
  evaluate worker success over recent window
  rank policy variants by survival, happiness, productivity, wealth and cultural fit
  copy/mutate successful policy profiles into new or struggling workers
  reduce prevalence of unsuccessful profiles
```

Success should not mean only productivity. A worker who produces a lot but is starving, exhausted and miserable should not automatically be the ideal template unless the society’s culture specifically rewards that at a cost.

Fitness dimensions may include:

- survival;
- hunger satisfaction;
- rest balance;
- happiness;
- wealth/resources;
- useful work completed;
- social/cultural fit;
- safety;
- contribution to player goals.

### 8.6 Practical neural approach

The design target allows neural/evolving worker behaviour, but the implementation must avoid one expensive opaque neural network per worker unless proven feasible.

Preferred staged approach:

1. deterministic worker jobs;
2. worker needs telemetry;
3. shared policy by worker archetype;
4. individual traits/genome weights;
5. cultural imitation and selection;
6. optional neural policy inference for groups/archetypes;
7. later individual neural variation if performant.

### 8.7 Societal selection pressure

The key design goal is:

> The society the god creates determines which worker behaviours flourish.

Behaviour prevalence can change through:

- recruitment;
- education;
- cultural imitation;
- reproduction/inheritance if population biology is modelled;
- policy mutation;
- worker success/failure weighting;
- happiness and survival;
- wealth accumulation;
- productivity;
- cultural-card modifiers.

Examples:

- a well-fed leisure-rich society produces happier specialised workers;
- a harsh industrial society produces productive but unhappy workers;
- a trade society produces mobile, wealth-seeking workers;
- a polluted collapse society produces survivalist behaviours;
- a militarised society produces disciplined but potentially brittle workers;
- a peaceful hunter-gatherer society produces resilient low-extraction behaviours;
- an agricultural society produces settled surplus-producing behaviours.

---

## 9. God AI architecture

God AIs should be modular.

### 9.1 Policy modules

A god AI may include:

```text
EconomyPolicy
LogisticsPolicy
MilitaryPolicy
CultureDraftPolicy
WorkerSocietyPolicy
CollapseResponsePolicy
CoordinatorPolicy
```

Later optional modules:

```text
DiplomacyPolicy
EspionagePolicy
WonderPolicy
ReligionPolicy
```

### 9.2 Two-stage module pattern

Each policy module should have:

1. **Pre-analytic network**
   - takes a broad domain snapshot;
   - identifies salient features;
   - selects, weights or compresses the information.

2. **Analytic network**
   - receives the selected information;
   - scores actions or creates proposals;
   - emits 0–3 recommended actions.

Example:

```text
Logistics snapshot:
  roads, blocked jobs, storehouse capacity, carrier idle rate, ware shortages

Logistics pre-analytic network:
  identifies main bottlenecks and relevant locations

Logistics analytic network:
  recommends:
    1. build road to quarry
    2. increase carrier priority near north storehouse
    3. build new storehouse near frontier
```

### 9.3 Shared state summary

Modules communicate through a shared player-state summary.

They must not directly mutate game state.

Pipeline:

```text
Snapshot / Observation
-> PlayerStateSummary
-> Domain pre-analytic networks
-> Domain analytic networks
-> Module proposals
-> Coordinator / Arbiter
-> Legal god action
-> Command validation
-> Simulation
```

### 9.4 Legal action discipline

Every god AI action must compile into the same legal command surface available to humans.

No hidden-information cheating in normal training/evaluation.

Teacher/debug traces may use full information, but policy evaluation must support fog-limited observations.

### 9.5 Macro action space

The current 16-macro action space should be treated as a compiler layer, not the final permanent API.

Short term:

- keep macro actions;
- improve compiler failure telemetry;
- penalise invalid/no-op actions;
- expand macros only where needed.

Long term:

- support richer legal action masks;
- allow placement and target reasoning;
- preserve deterministic validation.

---

## 10. Economy and logistics decisions

### 10.1 MVP-final ware roles

The MVP economy should stay small but age-aware.

Functional ware roles:

```text
raw_wood
processed_wood
stone_material
generic_tools
food_raw
food_processed
ore_raw
metal_processed
basic_weapons
military_supplies
power_or_control
```

Age-flavoured examples:

| Functional role | Prehistoric | Historic | Modern | Futuristic / Collapse |
|---|---|---|---|---|
| raw_wood | timber | timber | timber | reclaimed biomass |
| processed_wood | worked planks | planks | lumber | engineered panels |
| stone_material | stone | stone blocks | concrete | composite blocks |
| generic_tools | stone tools | iron tools | machine tools | fabrication kits |
| food_raw | gathered grain | grain | grain | hydroponic feedstock |
| food_processed | dried rations | bread/rations | field rations | nutrient packs |
| ore_raw | bog ore | iron ore | iron ore | scrap ore |
| metal_processed | bloom iron | iron ingots | steel stock | alloy stock |
| basic_weapons | spears | arms | small arms | energy arms |
| military_supplies | war provisions | campaign supplies | field supplies | survival supplies |
| power_or_control | ritual authority | civic authority | electricity | grid capacity |

The internal simulation should use stable functional roles. Age-specific names should be presentation/configuration data where possible.

### 10.2 Tool categories

MVP recipes use generic `tools`.

The data model should allow future `tool_class` values:

- cutting;
- digging;
- mining;
- farming;
- engineering;
- weapon.

Do not expose these categories in the first playable slice unless needed for a specific card or building.

### 10.3 Delivery rules

All inter-building ware delivery uses roads or logistics routes.

Allowed exceptions:

- a building can move wares within its own internal inventory;
- starting stock begins inside the Command Hub;
- construction placement can create a ghost site, but materials must be delivered;
- test/debug spawning is allowed only in explicit test scenarios.

### 10.4 Storage and overflow

Storage is local but connected networks can share.

Rules:

1. Every Storehouse has capacity per ware.
2. Producers try to push outputs to the nearest valid Storehouse on the same logistics network.
3. If that Storehouse is full, try the next nearest.
4. If all valid Storehouses are full, the producer stalls.
5. Multiple bases share wares only through connected owned/allied logistics routes.
6. Isolated bases become separate economy islands.
7. Delivery jobs reserve wares at source before dispatch.

### 10.5 Starting economy

Default 4P start is a symmetric boom with delayed conflict.

Each player starts with:

- 1 Command Hub;
- integrated storage or 1 nearby Storehouse;
- 3 carriers;
- 1 builder;
- 1 scout/hero marker if supported;
- enough starting materials for early roads and 2–3 buildings;
- no starting offensive army.

Combat should require:

- a military training building;
- weapons chain;
- military supplies chain.

---

## 11. Building roles and age equivalents

The simulation should use stable building roles.

| Functional role | Prehistoric | Historic | Modern | Futuristic / Collapse |
|---|---|---|---|---|
| Command Hub | Sacred Hearth | Palace | Command Centre | Arcology Core |
| Storehouse | Granary Pit | Warehouse | Depot | Secure Vault |
| Road/logistics node | Trail Marker | Road Post | Logistics Hub | Transit Node |
| Woodcutter | Forager Lodge | Lumber Camp | Forestry Station | Biomass Reclaimer |
| Sawmill | Carpentry Yard | Sawmill | Lumber Mill | Fabrication Mill |
| Quarry | Stone Camp | Quarry | Concrete Works | Composite Extractor |
| Farm | Foraging Grounds | Farmstead | Mechanised Farm | Hydroponic Bay |
| Bakery/rations | Smokehouse | Bakery | Ration Kitchen | Nutrient Printer |
| Iron Mine | Bog Ore Pit | Iron Mine | Deep Iron Mine | Scrap Extraction Site |
| Smelter | Bloomery | Smelter | Steelworks | Alloy Furnace |
| Workshop | Tool Hut | Workshop | Machine Shop | Fabricator |
| Barracks | Warrior Lodge | Barracks | Training Compound | Defence Academy |
| Power/control | Ritual Circle | Civic Forum | Power Generator | Fusion/Grid Node |
| Defence Turret | Watch Totem | Watchtower | Defence Turret | Sentinel Platform |
| Leisure | Story Circle | Tavern | Cinema/Club | Virtual Commons |
| Restoration | Sacred Grove | Commons | Recycling Centre | Restoration Engine |

This table is a design target. The first implementation can use generic role names and add age flavour later.

---

## 12. Military and delegated combat decisions

### 12.1 Unit model

Squads are the long-term player-facing and AI-facing model.

There should be no direct individual unit micro.

The MVP should have one core military unit per age.

Example:

| Age | Military unit role |
|---|---|
| Prehistoric | Warband |
| Historic | Guard Company |
| Modern | Rifle Squad |
| Futuristic / Collapse | Survival Troopers |

Later units can include:

- Scout;
- Engineer;
- Medic;
- Rocket Squad;
- Supply Truck;
- Light Rover;
- Armoured Car;
- Repair Drone.

### 12.2 Forbidden player commands

The hybrid game should not expose:

```text
SelectUnitCommand
MoveUnitCommand
AttackUnitCommand
BoxSelectCommand
GroupControlCommand
```

### 12.3 Player military controls

The player controls military through:

- building stance;
- production priority;
- garrison priority;
- patrol stance;
- delegated attack target;
- defence priority;
- repair/rebuild priority.

### 12.4 Delegated attack flow

For humans:

1. tap/click visible hostile building or infrastructure;
2. choose Attack;
3. simulation finds eligible squads;
4. squads path toward target;
5. squads fight blockers;
6. squads attack target;
7. squads return to stance/home/garrison behaviour.

### 12.5 Valid attack targets

Units can attack:

- Command Hubs;
- military buildings;
- production buildings;
- roads;
- bridges;
- carriers;
- storehouses;
- construction sites;
- power/control buildings;
- defensive structures.

Logistics warfare is central. Destroying roads, carriers and storehouses should damage an enemy’s ability to function.

### 12.6 Patrol vs aggressive stance

Patrol stance:

- defends owned territory;
- defends roads and buildings;
- engages hostile military threats;
- does not deep-raid civilian logistics by default.

Aggressive stance:

- attacks hostile military;
- raids logistics;
- attacks roads and carriers;
- attacks storage and construction;
- disrupts economy where reachable.

---

## 13. Repair and rebuild rules

Repair if:

- the building still exists;
- it is inside controlled territory;
- the route is connected;
- repair wares are available;
- repair cost is below 60% of rebuild cost.

Rebuild if:

- the building is destroyed;
- the footprint is ruined but recoverable;
- strategic priority remains high;
- the route can be restored.

Ignore if:

- the base is collapsing;
- the area is no longer defensible;
- rebuilding would starve core production;
- the building role has been superseded by cultural/age progression.

---

## 14. Pollution, scarcity and collapse

### 14.1 Design principle: no obvious warning label

Pollution and collapse should be systemic, observable and emergent.

The game should not present factories, fossil power or nuclear weapons with blunt warning labels that tell the player exactly how bad they are. Players should be allowed to chase short-term power, output, military advantage and victory points without being explicitly told that they are pushing the world toward collapse.

Instead, the world should communicate harm through changing conditions:

- forests near pollution become visually less healthy;
- polluted forests produce less wood/biomass;
- nearby farms become less productive or less reliable;
- workers near pollution become less healthy;
- worker tiredness, illness or unhappiness increases;
- demand for food, rest, medicine and leisure rises;
- local logistics become less reliable as disasters and shortages increase;
- natural disasters become more frequent as global pollution rises.

The player should be able to infer the problem by watching the world degrade.

### 14.2 Local pollution

Polluting buildings and actions create local pollution around their position.

Sources may include:

- factories;
- fossil power;
- heavy industry;
- over-extraction;
- toxic waste;
- nuclear weapons;
- advanced weapons plants;
- ruined terrain;
- high-output modern production chains.

Local pollution affects nearby systems first:

- forest health;
- wood/biomass output;
- farm productivity;
- worker health;
- worker happiness;
- worker tiredness;
- local disaster vulnerability;
- desirability of nearby workplaces and homes.

Local pollution should be spatial. A heavily industrial region should feel different from a clean agricultural, forest or restoration region.

### 14.3 Global pollution

Local pollution also contributes to a global pollution count.

Global pollution should not scale linearly. It should have escalating effects, so each additional unit of pollution becomes more dangerous as the world is already damaged.

Design target:

```text
global_pollution_harm = base_harm * exponential_or_curve(global_pollution)
```

The exact formula can be tuned later, but the intended behaviour is:

- low pollution creates mild background harm;
- medium pollution begins to noticeably reduce health and production;
- high pollution accelerates worker harm, resource scarcity and disaster frequency;
- extreme pollution risks irreversible collapse.

Global pollution affects:

- global worker health;
- global worker tiredness and recovery;
- food reliability;
- forest/wood regeneration;
- general production reliability;
- natural disaster frequency;
- severity of floods, storms, fires, droughts or hurricanes;
- collapse risk in Age 4.

### 14.4 Production degradation

Pollution should reduce production indirectly and systemically.

Examples:

- forests produce less timber because they are unhealthy;
- farms produce less food because soil/water/weather is unstable;
- workers produce less because they are ill, tired or unhappy;
- carriers move less efficiently because disasters damage roads;
- factories suffer interruptions because supply chains fail;
- restoration buildings consume more resources as damage worsens.

The player should experience pollution as a creeping systems problem, not as a single abstract penalty.

### 14.5 Natural disasters

Global pollution increases the rate and severity of natural disasters.

Possible disasters:

- flooding;
- hurricanes;
- storms;
- drought;
- wildfires;
- heatwaves;
- crop failures;
- disease outbreaks, if health systems are modelled.

Disasters should affect the physical simulation:

- damage roads;
- damage buildings;
- reduce forest/farm productivity;
- injure or stress workers;
- interrupt delivery jobs;
- create repair demand;
- create restoration opportunities.

### 14.6 Collapse

If world damage passes thresholds in Age 4, collapse begins.

Collapse should feel like the world system has become unstable, not like a scripted punishment.

During collapse:

- basic resources become scarce;
- food chains fail;
- worker needs are harder to satisfy;
- worker health declines;
- happiness declines;
- safety declines;
- disasters become common;
- supply chains break more often;
- conflict pressure rises;
- cooperative restoration becomes strategically important.

### 14.7 Collapse resources

Collapse should not just be a penalty. It should create new decisions.

Possible collapse-specific resources:

- reclaimed materials;
- clean water;
- seed stock;
- medical supplies;
- stable energy;
- shelter capacity;
- trust/social cohesion;
- restoration points.

### 14.8 Information design

Pollution should be visible, but not over-explained.

The normal player UI should show consequences:

- sickly forest visuals;
- reduced production trends;
- worker health/happiness trends;
- more frequent disaster events;
- damaged roads and flooded areas;
- rising repair/restoration demand.

The debug HUD may show exact pollution numbers, local pollution maps and global pollution curves, but the normal game should primarily teach through world feedback rather than explicit warnings.

---

## 15. Stalemate, resignation and tick caps

### 15.1 Match timing

Use:

```text
max_ticks: 50000
stall_threshold_ticks: 1000
fog_enabled: true
```

### 15.2 Stall rule

If all of the following are true for 1,000 ticks, mark the game as stalled:

- no Command Hub has been damaged;
- no player has gained/lost meaningful territory;
- no attack order has resolved;
- total military/economic score changed by less than 2%;
- no age/draft/collapse milestone occurred.

### 15.3 Bot resignation

A bot may resign only if:

- all Command Hubs are doomed or unreachable;
- no active construction can restore production;
- military power is below 25% of the leading enemy;
- no valid attack, rebuild or expansion action exists;
- the bot is not still relevant to a joint restoration victory.

### 15.4 Tick-cap scoring

At max tick, if no other ending has occurred:

1. highest survival-adjusted score;
2. surviving Command Hubs;
3. victory points;
4. military strength;
5. production throughput over last 1,000 ticks;
6. enemy infrastructure damage caused;
7. lowest pollution culpability.

---

## 16. Interface and spectator goals

### 16.1 Primary presentation

2D isometric / oblique top-down is primary for the current product.

3D wizard embodiment is future.

Headless simulation remains mandatory.

### 16.2 Minimum debug HUD

The hybrid debug HUD should show:

- active player;
- tick and speed;
- current age;
- draft progress;
- Command Hub health;
- victory points;
- pollution/world damage;
- collapse status;
- power/control available and used;
- ware inventory by storehouse;
- top ware shortages;
- active delivery jobs;
- blocked delivery jobs;
- idle workers/carriers/builders;
- worker need summary;
- dominant worker behaviour types;
- construction sites waiting for wares;
- military stance per building;
- garrison count;
- current delegated attack target;
- selected road/building diagnostics;
- AI intent;
- event log.

### 16.3 Spectator fog

Default spectator is omniscient.

Toggles:

- omniscient;
- Player 1 fog;
- Player 2 fog;
- Player 3 fog;
- Player 4 fog;
- economy overlay;
- combat overlay;
- worker society overlay;
- pollution/collapse overlay;
- AI intent overlay.

---

## 17. Answered design questions from the project audit

### Economy / logistics

| Question | Decision |
|---|---|
| Which wares/buildings are MVP-final vs placeholder? | Use a small stable set of functional ware/building roles with age-flavoured names. Defer full catalogue. |
| Are tools generic or split? | Generic in MVP; data model supports future tool classes. |
| Must all deliveries use roads? | Yes, except internal inventory movement, starting stock and explicit test/debug cases. |
| 4P start: rush or boom? | Symmetric boom with delayed conflict. |
| Storage overflow and cross-base logistics? | Local storage, shared only across connected owned/allied logistics networks. Overflow tries nearest valid storehouse, then stalls. |
| Stall/progress score? | Use 1,000 tick stall threshold with score delta, territory, hub damage, attack resolution and age/collapse milestone checks. |

### Military / RTS

| Question | Decision |
|---|---|
| Final unit roster? | One unit per age for MVP; broader roster deferred. |
| Squads or individuals? | Squads are the long-term player-facing model. |
| How does delegated attack choose targets? | Human chooses visible target; simulation assigns eligible units. Bots use target-priority list. |
| Can units attack roads/carriers? | Yes. Logistics warfare is central. |
| Repair vs rebuild? | Repair if cheap, connected and defensible; rebuild if destroyed but strategically needed; ignore if indefensible or unaffordable. |
| Power/radar/tech scope? | Include age-appropriate power/control. Defer radar/research/vehicle expansion. |

### Victory / match structure

| Question | Decision |
|---|---|
| Is Command Hub destruction the only elimination condition? | Yes for elimination, but not the only match ending. |
| Can player recover after losing Barracks? | Yes. |
| Tick cap/stalemate? | Tick cap uses survival-adjusted score and tie-breakers. |
| Non-elimination objectives? | Yes: VP, Age 4 restoration, survival score and shared loss. |
| 2P vs 4P? | 4P FFA is the target. 2P remains tutorial/test mode. |

### Bots / neural training

| Question | Decision |
|---|---|
| Is 16-macro action space long-term? | Temporary compiler layer. |
| Canonical teacher? | TutorialHybridBot for beginner baseline, then stronger modular heuristics. |
| Reward signal? | Terminal win/loss plus shaped rewards for logistics, VP, hub defence, enemy damage, collapse response and worker welfare. |
| Fog-limited or full observations? | Fog-limited for policy evaluation; full traces allowed for teacher/debug only. |
| Promotion gates? | Must run without crashes, avoid invalid masks, beat baselines, reach 4P endings, and avoid economy-stall loops. |

### Interface / spectator

| Question | Decision |
|---|---|
| Minimum debug HUD? | Include economy, military, worker needs, AI intent, draft, pollution/collapse and event diagnostics. |
| Spectator fog? | Omniscient default with per-player fog toggles. |
| 2D or 3D? | 2D primary. 3D future. |
| What can wait? | 3D embodiment, micro combat, full diplomacy, large roster, aircraft/naval, deep industrial chains and polished art. |

---

## 18. Implementation milestones for the later PRD

The later PRD should not ask Cursor to build everything at once. It should use milestones.

### Milestone A — deterministic 4P logistics RTS loop

Goal:

- 4P scenario reaches a valid ending;
- logistics, roads, storage and delegated military work;
- no neural workers yet.

Acceptance:

- 4P headless eval reaches victory/stalemate resolution under defined rules;
- human/spectator can inspect economy and military state;
- Command Hub elimination works;
- direct unit micro remains rejected.

### Milestone B — culture draft skeleton

Goal:

- 4 ages;
- 8 card slots per age;
- cards unlock simple modifiers/buildings/VP;
- Age 3 victory check exists.

Acceptance:

- draft phases occur at deterministic tick intervals;
- cards are selected, passed and applied;
- Age 3 VP threshold can trigger win or Age 4;
- card effects are visible in snapshots/HUD.

### Milestone C — worker needs prototype

Goal:

- workers have hunger/tiredness/happiness;
- food/rest/leisure affect worker performance;
- deterministic behaviour still allowed.

Acceptance:

- worker needs update deterministically;
- starving/tired/unhappy workers affect throughput;
- HUD shows worker welfare summary.

### Milestone D — worker policy hook

Goal:

- workers choose broad actions through a policy interface;
- deterministic fallback remains.

Acceptance:

- random/test policy can select legal worker actions;
- invalid worker actions are masked or rejected cleanly;
- performance remains acceptable.

### Milestone E — societal selection

Goal:

- worker behaviour variants become more/less common based on success.

Acceptance:

- policy/genome weights are tracked;
- cultural cards and society conditions alter behaviour prevalence;
- telemetry shows behaviour distribution over time.

### Milestone F — modular god AI

Goal:

- AI gods use interlocking modules rather than one monolithic network.

Acceptance:

- economy, logistics, military, drafting and collapse-response modules produce proposals;
- each module has room for pre-analytic and analytic stages;
- coordinator selects legal god actions;
- no hidden-information leak under fog.

### Milestone G — pollution and collapse

Goal:

- Ages 3/4 create world damage;
- Age 4 collapse has meaningful decisions and endings.

Acceptance:

- pollution sources affect resources/workers;
- collapse thresholds trigger scarcity;
- restoration victory/shared loss/survival score are testable.

---

## 19. Risks and scope controls

### Risk: the worker neural model becomes too expensive

Control:

- start with shared policy by archetype;
- use individual traits/genomes before individual networks;
- keep deterministic fallback;
- test performance with many workers.

### Risk: four ages multiply content by four

Control:

- use functional roles internally;
- age flavour via config;
- one military unit per age;
- small card decks first.

### Risk: culture drafting overwhelms RTS logistics

Control:

- make initial cards simple modifiers/unlocks;
- keep draft cadence slow;
- ensure cards affect existing systems rather than adding many new systems.

### Risk: Age 4 joint victory undermines FFA

Control:

- allow domination victory, restoration victory, survival score and shared loss;
- penalise pollution culpability;
- make restoration desirable but not mandatory in every state.

### Risk: neural god AI hides bugs

Control:

- all actions compile into legal commands;
- keep scripted teachers;
- record action proposals and validation failures;
- train only after deterministic rules and strong heuristics exist.

### Risk: pre-analytic/analytic god networks become over-engineered too early

Control:

- implement module interfaces first;
- allow heuristic versions of pre-analytic and analytic stages;
- introduce learned networks only after telemetry and teacher traces exist;
- require every module proposal to be inspectable in the debug HUD.

---

## 20. Recommended title and file path

Recommended title:

**Hybrid God-Strategy MVP — Design Goals and Rules Decisions**

Recommended project file path:

```text
docs/design/HYBRID_GOD_STRATEGY_DESIGN_GOALS.md
```

Alternative shorter title:

**God-Strategy Design Goals**

Alternative more technical title:

**Hybrid Logistics RTS God-Strategy Target Design**

---

## 21. One-sentence design target

A deterministic, testable, 4-player god-strategy logistics RTS where players guide evolving civilisations through four ages by shaping infrastructure, culture, warfare, ecology and society, while autonomous neural-policy workers and modular network-of-networks god AIs learn which behaviours flourish under the worlds the gods create.
