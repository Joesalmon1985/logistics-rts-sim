# Owner brief, 28 Nov 2026: Unification of the artificial world (verbatim)

The owner's two messages that define the unification milestone, kept word for word except that image references (illustrative pictures of buildings and terrain) are omitted. The decisions are recorded as set **D8** in [../civdiv/OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md). **Part B supersedes Part A where they differ**: in particular, raw materials can **not** be collected by hand without a collection building (Part B §1 and §8).

---

# Part A — Fundamental economic design: unification

I think you've identified the fundamental problem with the current implementation: **we have built an RTS economy and an artificial-life simulation, but we haven't yet properly unified them.**

Your description of NPCs behaving like individual Minecraft players is a much better way of thinking about the intended game.

Each individual should be capable of surviving independently, but should gradually discover that specialising, exchanging goods, sharing infrastructure and cooperating with other individuals can improve their circumstances.

Civilisation should be the collective consequence of those individual decisions.

We shouldn't have one AI playing Catan and Widelands at faction level, whilst another AI controls the people wandering around inside that economy.

## 1. Why the current game feels like two separate games

I've checked the current implementation and design documents at `b297cb0`, and there is evidence supporting your concern.

| Original RTS systems          | New artificial-life systems                    |
| ----------------------------- | ---------------------------------------------- |
| Faction-level economic AI     | Individual NPC cognition                       |
| Faction construction commands | Individually constructed productive assets     |
| Headquarters inventories      | Personal and household inventories             |
| Anonymous transport workers   | Persistent NPCs carrying goods                 |
| Widelands flags and roads     | Individuals walking between locations          |
| Faction production priorities | Individual motivations and employment          |
| Abstract military units       | Biological individuals with families and needs |
| Faction trade                 | Individual barter and exchange                 |

For example, the current `GodBootstrap.cs` still starts faction AI alongside the newer population, civilisation, organisational and divine systems.

The `ECONOMY_DESIGN.md` also explicitly retains parts of the Stage 3 economy pending their replacement.

Even the new `Work.cs` requires certain individually constructed productive assets to be outside the old headquarters construction circles.

These are understandable transitional arrangements, but I think we have reached the point where we need to remove the distinction from normal gameplay.

**Catan should define the geography and resource opportunities. Widelands should define the production recipes. Creatures should determine what the people actually do.**

There should not be a separate AI playing the economy on their behalf.

## 2. The fundamental geography and resources

I would treat the following as the definitive starting resource model.

Underlying geographical abstraction — invisible during normal gameplay. Desired continuous geographical presentation.

| Geography | Raw material | Forageable food |
| --------- | ------------ | --------------- |
| Forest    | Wood         | Mushrooms       |
| Mountain  | Stone        | Roots           |
| Wetland   | Clay         | Wild eggs       |
| Pastures  | Sheep        | Rabbit          |
| Fields    | Hay          | Wild vegetables |
| Desert    | None         | Cactus fruit    |
| Sea       | Fish         | Seaweed         |

There are two fundamentally different resource-generation mechanisms.

### Forageable food

Forageable food is always available in its appropriate environment.

It does not depend on dice rolls, and it cannot permanently run out.

An individual must physically visit an appropriate location, spend time foraging, collect the food and consume or carry it.

Different foods could have different nutritional values, collection times, spoilage rates and accessibility.

I would initially make foraging rate-limited per individual, with shared-location capacity configurable rather than compulsory. This avoids immediately recreating the population-collapse problem.

### Raw materials

Raw materials depend on Catan-style production rolls.

A successful roll generates an amount of **potential production**, which gradually materialises within the geographical region.

For example, a forest receives a successful production roll.

**Production roll succeeds** — The forest receives a production allowance of 6 wood.

**Natural resources develop** — Wood becomes available gradually over the following ticks.

**NPC discovers available resources** — The individual perceives or remembers the resource location.

**NPC gathers the wood** — Collection requires physical movement, labour and possibly equipment.

**NPC transports the wood** — The material is carried to storage, construction or a workshop.

The resource does not magically appear in a faction's inventory.

It exists in the environment and must be collected.

A forestry building improves the effectiveness of harvesting wood, rather than automatically generating resources simply because the building exists.

The figures above are illustrative; the actual production quantities and timings should be configurable.

## 3. Starting conditions

Every normal game should begin with six tribes: Amber, Crimson, Azure, Jade, Violet and Ivory, each with 7 adults and 5 children. Total population 72: 42 adults, 30 children.

Each tribe starts with one communal building at a founding settlement.

I would generate plausible familial relationships, rather than completely random ones. Children should have appropriate caregivers, biological relatives should have consistent genetics, and adults should have different ages, experiences and personalities.

Initially there should be:

- No formal government.
- No professional military.
- No currency.
- No employers.
- No established production chains.
- No formal institutions.
- No predetermined professions.

There is simply a community of people trying to survive.

Everything else can develop from there.

## 4. Settlements, buildings and roads

I think the combination of Catan settlement rules and unrestricted physical NPC movement is exactly what we need.

Conceptual settlement at a hex vertex, surrounded by buildings. Roads follow valid Catan edges between settlements; individuals can walk independently of roads.

### Settlements

A settlement occupies a valid intersection of the underlying geographical hexagons.

I would retain Catan's distance rule, so immediately adjacent intersections cannot both contain settlements.

A settlement creates an area in which additional buildings may be constructed. It does not automatically own everything in that area.

A settlement is a geographical and infrastructural entity, not necessarily a government.

It might contain private homes, independent producers, communal buildings, businesses and eventually institutions.

### Buildings

Buildings are productive or residential assets constructed by individuals or groups.

They must be built using actual materials and labour.

Their owners can decide who may use them.

They can be transferred, inherited, abandoned or destroyed.

### Roads

Roads exist between settlement locations, following valid Catan-style connections.

Unlike Widelands:

- No flags.
- No assigned carriers for individual road segments.
- No compulsory handover points.
- No requirement for every building to connect to a road.
- No magical transfer of goods through the road network.

An NPC can walk directly from a woodcutter's building to a carpenter's workshop, using roads where they make the journey quicker.

For expansion, I would permit a proposed road to a planned settlement site. Otherwise, requiring an existing settlement at both ends could prevent normal Catan-style expansion.

## 5. The first buildings

I would establish a small catalogue of fundamental buildings that allow individuals to move beyond foraging.

- **Woodcutter** — Improves the collection of available wood from forest regions. Outputs: wood.
- **Quarry** — Improves the extraction of available stone. Outputs: stone.
- **Clay pit** — Enables efficient collection of available clay. Outputs: clay.
- **Sheep farm** — Collects and manages sheep, enabling further production. Potential outputs: wool, hides and meat.
- **Hay gatherer** — Collects hay from productive field regions. Outputs: hay.
- **Fishery** — Collects fish from accessible coastal and marine resources. Outputs: raw fish.

These are suggested initial building types and visual references.

An individual should be able to collect small amounts of basic raw materials without specialised buildings. Otherwise, we would create a circular dependency where wood is needed to construct the building required to collect wood. *(Superseded by Part B: the first collection building costs only labour and time, and raw materials cannot be collected without one.)*

Buildings and tools should make individuals more productive, not arbitrarily unlock actions they could reasonably perform by hand.

## 6. The Widelands production chains

Once individuals begin collecting raw materials, they can manufacture more complex goods.

| Input | Process | Output |
|---|---|---|
| Wood | Carpenter | Planks |
| Stone | Stonecutter | Building blocks |
| Clay | Potter | Pottery |
| Clay | Kiln | Bricks |
| Sheep | Shearing | Wool |
| Wool | Weaver | Cloth |
| Hay | Crafting | Bedding |
| Fish | Cooking | Prepared food |
| Wood + stone | Toolmaker | Axe / hammer |

Proposed initial recipes. Each transformation requires actual materials, knowledge, labour and appropriate equipment.

The forageable foods are directly edible.

The Catan-generated resources are productive inputs that require collection and, where necessary, processing. For example, raw fish becomes prepared food, and sheep can provide materials and food through appropriate activities.

Eventually these production chains extend naturally into coal, oil, metalworking, electricity, vehicles and industrial machinery.

There is no need for a separate industrial game.

## 7. NPCs as individual Minecraft players

This is the most important change in emphasis.

Each NPC should have a repertoire of possible actions and the ability to combine them into useful activities.

Their decision-making should be driven by:

| Component            | Purpose                                           |
| -------------------- | ------------------------------------------------- |
| Drives               | Hunger, fatigue, comfort, health and social needs |
| Perception           | What can I currently see?                         |
| Memory               | What do I remember about the world?               |
| Knowledge            | What do I understand how to do?                   |
| Learned associations | What has worked for me previously?                |
| Relationships        | Who do I trust or distrust?                       |
| Possessions          | What resources and equipment do I have?           |
| Affordances          | What actions are available?                       |
| Intentions           | What am I currently trying to achieve?            |
| Planning             | What intermediate actions might achieve it?       |

We already have much of the Creatures-style cognition and learning infrastructure.

What I think we need to emphasise now is **individual planning across multiple connected actions**.

Imagine an NPC who is uncomfortable sleeping outdoors.

1. **I need shelter** — Comfort drive creates a motivation.
2. **I know how to build a hut** — Available construction knowledge.
3. **I need wood and stone** — The recipe creates intermediate objectives.
4. **I can gather wood** — Perceive, move, gather and carry.
5. **Another person has stone** — Request, barter or cooperate.
6. **I now have the materials** — Select a valid construction site.
7. **Build the hut** — Spend labour and consume materials.
8. **Use the shelter** — Experience improved comfort and learn from the outcome.

The NPC should not require eight hard-coded instructions from a faction AI.

It should generate these intermediate objectives itself, using its knowledge of construction, materials, locations and social opportunities.

I would introduce bounded, interruptible goal planning alongside the existing associative learning system. An individual can abandon a construction project if it becomes hungry, encounters danger or discovers a better alternative.

## 8. How specialisation should emerge

Consider four individuals living in a settlement surrounded by forest, mountains and wetlands.

- Individual A — **Wood collector**: learns that an axe makes wood collection more productive.
- Individual B — **Stone collector**: discovers accessible stone and develops extraction skills.
- Individual C — **Potter**: uses clay to manufacture useful storage containers.
- Individual D — **Food gatherer**: collects surplus food and exchanges it for other goods.

None of these individuals has been assigned a permanent profession.

They specialise because they have learned particular activities, possess useful equipment, understand local resources and discover that other people will exchange goods with them.

If circumstances change, they may change what they do.

The wood collector might become a carpenter, employ somebody else to collect wood, establish a workshop or spend more time caring for children.

That is how the economic system should develop.

## 9. Informal groups should precede institutions

I would introduce informal groups as a much more fundamental social entity than governments, companies or formal institutions.

A group might be formed because several individuals want to achieve something together.

Examples include building a road, sharing a workshop, caring for children, constructing a settlement, defending themselves or trading with another tribe.

- **Individual** — personal needs, possessions and interests
- **Household** — family, caregiving and shared resources
- **Informal group** — people cooperating towards a common objective
- **Enterprise or community organisation** — persistent assets, responsibilities and productive activity
- **Institution** — established services, governance, records and rules
- **State** — coordinated political authority over a community

This is a possible developmental pathway, not a mandatory sequence.

A tribe is simply an initial social affiliation. It should not automatically be a state, employer, property owner or collective intelligence.

An individual can belong to their tribe, household, work group, religious community and enterprise simultaneously.

## 10. How an NPC decides to build a road

This is a good test of whether the overall design is genuinely working.

Suppose two communities regularly exchange wood and stone.

Their individuals repeatedly walk between the settlements.

They experience the cost of these journeys and know that roads improve travel efficiency.

An individual proposes constructing a road.

Others independently decide whether to contribute materials or labour.

Some may refuse, despite intending to use the completed road.

The road gets built only if enough people actually contribute.

This gives us:

- Collective action.
- Public goods.
- Investment.
- Free-riding.
- Cooperation.
- Infrastructure development.
- Potential disputes over ownership and access.

We get all of these political and economic mechanisms from one relatively simple construction project.

There is no need to introduce a special faction AI that automatically decides when roads should be constructed.

## 11. How the existing systems should be reconciled

I would establish a deliberate **unification milestone** before continuing much further into industrialisation.

| Existing system                | Decision                                    |
| ------------------------------ | ------------------------------------------- |
| Catan geography                | Retain                                      |
| Catan production dice          | Retain, introduce gradual materialisation   |
| Catan settlement rules         | Retain and refine                           |
| Catan road placement rules     | Retain and refine                           |
| Fine movement lattice          | Retain internally                           |
| Widelands production recipes   | Retain                                      |
| Widelands flags                | Remove from normal gameplay                 |
| Road-segment carrier system    | Remove                                      |
| Anonymous builders             | Replace with NPCs                           |
| Anonymous harvesters           | Replace with NPCs                           |
| Faction stockpiles             | Replace with owned physical inventories     |
| Faction economic AI            | Remove from normal gameplay                 |
| Automatic faction construction | Replace with individual and group decisions |
| NPC cognition                  | Retain and extend                           |
| NPC families and learning      | Retain                                      |
| NPC barter and employment      | Retain                                      |
| Organisations and institutions | Retain                                      |
| Divine player mechanics        | Retain                                      |

The legacy Stage 3 game can remain isolated as a regression scenario during this transition. We should not delete working functionality before the replacement passes its tests.

But the normal game must stop depending on it.

## 12. The development sequence I would prioritise

1. **Survive** — Forage, eat, sleep, care for children.
2. **Collect** — Discover and gather Catan raw materials.
3. **Construct** — Build shelters, workshops and productive assets.
4. **Produce** — Transform raw materials through Widelands recipes.
5. **Exchange** — Give, barter and negotiate with other individuals.
6. **Cooperate** — Form groups and construct shared infrastructure.
7. **Specialise** — Develop skills, employment, enterprises and productive ownership.
8. **Expand** — Construct roads and establish new settlements.
9. **Organise** — Develop institutions, political authority and shared rules.
10. **Industrialise** — Develop money, machinery, energy networks and advanced technology.

Every stage should emerge from the same set of individuals and physical interactions.

These are development capabilities, not compulsory historical stages.

## 13. Prompt to give Cursor

### Fundamental economic design — Unification of the artificial world

I want to resolve a fundamental architectural concern before extending the game further.

At present, it feels as though we have two games operating alongside one another:

1. The original faction-level Catan/Widelands RTS economy.
2. The newer Creatures-inspired simulation of autonomous individuals.

These need to become one coherent simulation.

Catan should define the geographical and resource opportunities. Widelands should define physical production chains. Creatures-style individual cognition should determine what the inhabitants actually do.

There must not be a separate faction AI secretly playing the economic game on behalf of the individuals.

#### Geography and resources

The world comprises seven abstract geographical hex types:

- Forest: Wood; forageable mushrooms.
- Mountain: Stone; forageable roots.
- Wetland: Clay; forageable wild eggs.
- Pastures: Sheep; forageable rabbit.
- Fields: Hay; forageable wild vegetables.
- Desert: No raw material; forageable cactus fruit.
- Sea: Fish; forageable seaweed.

The world is presented as one continuous geographical landscape. Hex boundaries and movement nodes are not visible during normal gameplay.

Retain the internal hexagonal and movement-node abstractions.

#### Forageable food

Every geographical region has an infinite supply of its appropriate forageable food.

Foraging does not depend on Catan production rolls.

Individuals must physically visit an accessible location and spend time collecting food.

Forageable food may be consumed directly, carried, stored, gifted or traded.

Food collection must not be instantaneous. Introduce configurable collection rates and appropriate nutritional and spoilage characteristics.

Do not permanently exhaust natural forageable food.

Shared-location throughput constraints may be configurable, but do not immediately recreate the universal population collapse we have previously encountered.

Provide suitable coastal access to marine resources, with boats introduced later where necessary.

#### Raw materials

Each geographical region produces its specified raw material through Catan-style production rolls.

A successful roll creates a production allowance that gradually materialises within the region over subsequent ticks.

Do not immediately transfer resources into settlement or faction inventories.

Resources physically exist in the environment and must be collected by individuals.

Raw materials can be processed or combined to produce more complex goods.

Retain and extend appropriate Widelands production recipes.

Allow basic manual resource collection so that individuals can bootstrap construction. *(Superseded by Part B.)*

Productive buildings, equipment and knowledge should improve productivity.

#### Starting population

The normal starting scenario contains six coloured tribes.

Each tribe starts with:

- One founding settlement.
- One communal building.
- Seven adult NPCs.
- Five child NPCs.

Total starting population: 72 individuals.

Generate deterministic, varied and biologically consistent family relationships, genetic inheritance, adult ages, personalities and caregiving arrangements.

Do not initially assign formal professions, government offices, businesses, military roles or institutions.

A tribe is a social affiliation, not automatically a government or a collective economic actor.

#### Settlements

Retain Catan-style settlement placement rules, including appropriate distance restrictions.

Settlements occupy valid geographical intersections.

Constructing a settlement enables additional buildings to be constructed within its surrounding development area.

This does not automatically grant collective ownership of all buildings or resources within that area.

Individuals and groups may propose, construct, own, operate and maintain buildings.

#### Roads

Retain Catan-style road placement rules.

Roads connect settlement locations along valid geographical connections. Allow planned routes towards proposed settlement sites where needed to support expansion.

Remove the Widelands-style flag and road-segment carrier system from normal gameplay.

Goods are physically transported by persistent NPCs directly between sources, stores, buildings and recipients.

Individuals may walk without roads.

Roads improve travel efficiency but are not compulsory logistics routes.

#### Individual cognition and planning

Think of NPCs as autonomous individuals operating in a constrained Minecraft-like world.

They start as simple foragers.

They can gradually learn to:

- Gather materials.
- Carry and store goods.
- Construct buildings.
- Produce tools.
- Manufacture more complex goods.
- Exchange resources.
- Learn productive skills.
- Cooperate with others.
- Construct roads.
- Establish new settlements.
- Form informal groups.
- Establish enterprises and institutions.

Retain the existing Creatures-inspired drives, perception, associative learning, social memory, knowledge and decision-making systems.

Extend these with bounded, interruptible multi-step goal planning where required.

For example, an individual wanting to construct a shelter should be able to identify the necessary materials, acquire them through gathering or exchange, identify a valid construction site and undertake the required work.

Do not implement every multi-step activity as a separate hard-coded faction strategy.

Individual knowledge and learned associations should inform goals, action choices and intermediate objectives.

Keep learning, physical skills, knowledge and actual productive capability distinct.

#### Specialisation

Do not assign permanent character professions.

Individuals may specialise because they possess particular tools, knowledge, skills, productive assets, useful relationships or access to resources.

They should be able to discover that producing surplus goods for exchange is advantageous.

Specialisation, trade, employment and productive ownership must arise from individual choices and circumstances.

Initially use barter and gifts through the existing physical exchange system.

Do not introduce an independent faction-level resource allocation mechanism.

#### Informal groups

Introduce informal groups as a fundamental social capability.

Individuals may propose collective objectives, invite other individuals and decide whether to participate.

Groups may cooperate on:

- Constructing buildings.
- Constructing roads.
- Establishing settlements.
- Gathering and transporting resources.
- Sharing productive assets.
- Defending their community.
- Caring for dependent individuals.
- Trading with other communities.

Contributions must involve actual materials, time or labour.

Participants can refuse, withdraw or free-ride.

Informal groups may eventually become persistent organisations, enterprises, cooperatives, religious communities or institutions.

Do not automatically create formal organisations merely because a threshold is reached.

#### Unify the existing architecture

Inspect the actual current implementation at b297cb0 or the latest agreed successor.

Identify every remaining Stage 3 system that independently undertakes economic activity in the living-population game.

In particular, investigate:

- Faction economic AI.
- Automatic faction construction.
- Headquarters resource inventories.
- Anonymous workers.
- Anonymous carriers.
- Anonymous builders.
- Widelands flags.
- Road-segment logistics.
- Faction-level production.
- Faction-level resource reservations.
- Separate faction trade.
- Anonymous military units.
- Parallel building and productive-asset representations.
- Headquarters construction circles and their conflict with NPC construction.

Prepare a migration plan so that each meaningful economic activity has one authoritative implementation.

The autonomous NPC population must become the sole source of ordinary economic decisions and productive labour.

Organisational decisions are permitted where they emerge from the decisions of actual NPC members or legitimate office-holders.

The divine player remains capable of suggestion, command, compulsion, possession and miracles under the existing design.

Retain the legacy Stage 3 game as an isolated regression scenario until its replacements are verified. Do not break the working checkpoint merely to remove transitional code.

#### Acceptance scenarios

The unified game must demonstrate:

1. Six tribes starting with exactly seven adults and five children each.
2. Individuals surviving by collecting forageable food independently of production rolls.
3. Catan production rolls creating gradually available physical materials.
4. An individual collecting raw materials and constructing a productive building.
5. Another individual specialising in a different resource.
6. Individuals manufacturing goods through actual production recipes.
7. Two individuals physically exchanging their goods.
8. An individual independently identifying and pursuing a multi-step construction objective.
9. Individuals cooperating to construct a road between settlements.
10. Individuals founding a new settlement.
11. Goods being transported directly by NPCs without Widelands flags or anonymous carriers.
12. An informal group emerging from individual participation.
13. The whole process occurring without any faction-level RTS economic AI or divine intervention.
14. The same scenario remaining deterministic, replayable and analytically reconcilable.

Run multi-generational viability experiments using the new forage model.

The first objective is a coherent and self-sustaining primitive society, rather than simply adding more advanced systems to an incomplete foundation.

#### Documentation and next development run

Record these decisions as the confirmed and extended terrain, population and economic unification design.

Update the owner decision register, WORLD_ONTOLOGY, economic design, population design, cognition design, construction design, shared contracts, acceptance tests and development package index.

Introduce a dedicated unification milestone before the main industrial expansion.

Identify which existing systems can be reused and which temporary mechanisms must be retired.

Prepare independent implementation packages for the next multi-agent run, including geography and resources, population initialisation, settlement construction, individual planning, physical production, logistics, informal groups, regression testing and analytical reconciliation.

Do not discard the existing work on institutions, divine powers, industrialisation, electricity, patents or monetary systems.

Those systems remain part of the long-term vision, but they must be built on the unified individual-driven artificial world.

The fundamental principle is that the NPCs play the economic game themselves. The player plays as their god.

## 14. Remaining small decisions

I have made a few additional assumptions that you haven't explicitly specified.

| Question | Suggested default |
| --- | --- |
| Can NPCs collect wood before building a woodcutter? | Yes, inefficiently *(superseded by Part B: No)* |
| Can NPCs walk anywhere accessible without roads? | Yes |
| Can NPCs build outside settlement areas? | Basic shelters and temporary structures only |
| Does a settlement own surrounding resources? | No |
| Does a tribe automatically own communal buildings? | No; the relevant community group owns them |
| Can individuals work across tribal boundaries? | Yes, if relationships and circumstances permit |
| Does a successful production roll benefit adjacent settlements automatically? | No; it makes physical resources available |
| Can the same raw material be collected by different tribes? | Yes, subject to access and ownership arrangements |
| Does every tribe start with technology knowledge? | Basic survival and construction knowledge, with individual variation |
| Can NPCs invent new recipes? | Eventually; initially learn and use defined recipes |

These are proposed defaults rather than confirmed requirements.

**I would prioritise this unification over adding another dozen institutions or military units.**

Imagine starting a game, leaving the computer running without intervening, and returning to discover that the original 72 people have constructed workshops, developed trade relationships, built roads, founded new settlements and established communal organisations.

That should be our next major demonstration. Once that works, hospitals, banks, factories, power stations and armies become extensions of a functioning civilisation rather than additional independent game systems.

---

# Part B — Primitive productive buildings and resource collection (supersedes Part A where they differ)

I think this is a much better bootstrap mechanic. It creates a clear distinction between **subsistence and productive capital**, without requiring individuals to possess resources before they can begin collecting them.

An NPC's first investment is their own labour and time. Subsequent investments require accumulated material resources.

## 1. The basic economic rules

- Forageable food can always be collected and eaten without buildings or tools.
- Raw materials cannot be collected without an appropriate operational collection building.
- The initial collection building costs only construction time and labour.
- Every collected raw material must have a designated building where it can be deposited and stored.
- Collection buildings also provide basic sleeping shelter.
- Buildings can subsequently be upgraded using raw materials.
- Upgrades increase productive capacity, storage capacity and potentially accommodation quality.
- Individuals or groups can own these buildings and determine who may use them.

This gives individuals a genuine reason to construct their first productive asset beyond simply collecting resources.

## 2. The six initial productive buildings

- **Woodcutter's shelter** — Collects wood from nearby forests. Stores wood and provides basic sleeping accommodation.
- **Stonecutter's shelter** — Collects stone from nearby mountains. Stores stone and provides basic sleeping accommodation.
- **Clay gatherer's shelter** — Collects clay from nearby wetlands. Stores clay and provides basic sleeping accommodation.
- **Shepherd's shelter** — Collects and accommodates sheep from nearby pastures. Provides an animal enclosure and sleeping accommodation.
- **Hay gatherer's shelter** — Collects hay from nearby fields. Stores hay and provides basic sleeping accommodation.
- **Fisher's shelter** — Collects fish from accessible coastal waters. Stores raw fish and provides basic sleeping accommodation.

These should all be variations of the same underlying building architecture, rather than six independently programmed systems.

## 3. Building progression

I would initially give every collection building three levels.

- **Level 1 — Primitive shelter.** Construction cost: labour and time only. Basic storage, sleeping accommodation and raw material collection.
- **Level 2 — Established hut.** Construction cost: wood and labour. Increased storage, better shelter and improved collection efficiency.
- **Level 3 — Productive workshop.** Construction cost: wood, stone and potentially other processed materials. Greater productive capacity, improved accommodation and the ability to support additional workers.

Illustrative balancing values:

| Property                | Level 1 | Level 2 | Level 3           |
| ----------------------- | ------- | ------- | ----------------- |
| Material cost           | None    | 10 wood | 20 wood, 10 stone |
| Storage capacity        | 10      | 30      | 60                |
| Sleeping capacity       | 2       | 4       | 6                 |
| Concurrent workers      | 1       | 2       | 3                 |
| Collection productivity | 1×      | 1.5×    | 2×                |
| Shelter quality         | Basic   | Good    | Excellent         |

These are provisional values. All buildings should use configurable definitions.

Importantly, increased productive capacity does not increase the amount of natural resources generated by the dice. It improves the ability to collect what is available.

## 4. The actual collection process

1. **Catan production roll** — Creates pending natural resource availability.
2. **Resource gradually materialises** — Physical raw material becomes available in the appropriate geographical region.
3. **NPC has access to a collection building** — Appropriate building, collection rights and available storage.
4. **NPC travels to the resource** — Uses its own perception, memory and movement.
5. **NPC collects the material** — Spends time and energy undertaking work.
6. **NPC returns to the building** — Physically carries the collected material.
7. **NPC deposits the material** — The building's inventory now contains the goods.

The building is a prerequisite for collection, not the entity that performs the collection.

An empty woodcutter's hut produces nothing. An individual must choose to work there.

## 5. Why this works particularly well economically

Imagine three individuals from the same tribe: NPC A (wood collector), NPC B (stone collector), NPC C (food forager). A and B exchange wood and stone; A/B exchange building materials with C for food.

NPC A can use some of their wood to upgrade their own shelter.

NPC B needs wood to upgrade their stonecutter's shelter, creating demand for NPC A's surplus.

NPC C might exchange food for construction materials or access to sleeping accommodation.

Already we have the foundations of:

- Specialisation and division of labour.
- Barter and subjective value.
- Capital investment.
- Productive ownership.
- Differences in wealth.
- Employment and shared ownership.
- Economic interdependence.

None of this requires a faction-level economic AI.

However, because everyone can construct a basic collection shelter for free, more developed producers must offer genuine advantages to persuade others to work for them. These could include greater productivity, access to better equipment, desirable locations or improved sleeping conditions.

That is a particularly useful foundation for the political economy we want to explore.

## 6. The building ontology

I would avoid having separate, incompatible concepts for workplaces, houses and warehouses.

A building should possess a number of capabilities.

| Capability | Meaning                                       |
| ---------- | --------------------------------------------- |
| Shelter    | Individuals can sleep here                    |
| Storage    | Physical goods can be deposited               |
| Collection | Enables collection of specified raw materials |
| Production | Enables specified manufacturing recipes       |
| Ownership  | Determines who owns the asset                 |
| Access     | Determines who is permitted to use it         |
| Employment | Supports individuals working for its owner    |
| Condition  | Represents maintenance and damage             |
| Upgrade    | Enables further investment                    |

Later buildings can combine these capabilities differently.

A hospital might have shelter, storage and healthcare capabilities. A refinery might have storage and industrial production capabilities. A barracks might have shelter, equipment storage and military training capabilities.

We can reuse the same fundamental architecture throughout the game.

## 7. Additional rules I would recommend

| Question | Recommended answer |
| --- | --- |
| Can several NPCs build a shelter together? | Yes, contributing actual labour. |
| Can an NPC build more than one collection building? | Yes. |
| Can individuals sleep in somebody else's building? | Yes, with permission or through unauthorised access. |
| Can children sleep in collection buildings? | Yes. |
| Can collection buildings be communally owned? | Yes. |
| Can an individual work at somebody else's building? | Yes, through permission, cooperation or employment. |
| Can multiple buildings collect from the same hex? | Yes, competing for available raw materials. |
| Can an individual collect goods when their designated storage is full? | No new collection until capacity is available or reserved elsewhere. |
| Can buildings be abandoned? | Yes. |
| Can buildings be sold or inherited? | Yes. |
| Can buildings be upgraded while operational? | Yes, potentially with reduced capacity during construction. |

## 8. Updated instructions for Cursor

### Owner decision — Primitive productive buildings and resource collection

Amend the economic unification design with the following decisions.

#### Foraging and raw materials

Forageable food remains infinitely available, independent of Catan production rolls, and requires no building or equipment to collect.

Catan-generated raw materials are different.

An NPC cannot collect raw materials without access to an appropriate completed collection building.

This explicitly supersedes the previous suggestion that basic raw materials could be gathered by hand without buildings.

Each collection activity must have an appropriate destination building with available or reserved storage capacity.

Raw materials must be physically collected, carried and deposited by persistent NPCs.

#### Initial collection buildings

Introduce six basic collection building types:

- Woodcutter's shelter: Wood.
- Stonecutter's shelter: Stone.
- Clay gatherer's shelter: Clay.
- Shepherd's shelter: Sheep.
- Hay gatherer's shelter: Hay.
- Fisher's shelter: Fish.

There is no raw material collection building for deserts.

The initial level of every collection building costs no material resources. Construction requires only actual NPC labour and time.

This represents primitive structures constructed using locally available materials that are not separately accounted for in the formal economy.

NPCs must autonomously decide whether, where and when to construct these buildings.

Buildings remain subject to settlement development areas and appropriate geographical access.

#### Sleeping accommodation

Every initial collection building also functions as a basic sleeping shelter.

Buildings have finite sleeping capacity and shelter quality.

Individuals and their dependants may use these shelters subject to access arrangements.

Do not create an unnecessarily separate housing system for primitive productive buildings.

#### Upgrades

Collection buildings can be upgraded using physical raw materials and additional NPC labour.

Initially, use wood as the principal upgrade material, with stone and other materials introduced for more advanced upgrades.

Upgrades may improve:

- Storage capacity.
- Sleeping capacity.
- Shelter quality.
- Collection efficiency.
- Concurrent worker capacity.
- Available productive capabilities.

All costs, capacities and productivity values must be configurable.

Upgrades must not create additional geological or geographical resources. They improve the ability to collect, store and process available resources.

#### Ownership and access

Collection buildings are productive assets.

They can be personally, jointly, communally or cooperatively owned.

Building ownership, collection rights, sleeping rights, storage access and ownership of collected goods must remain distinguishable.

NPCs may construct buildings collaboratively, invite others to work, share facilities, employ workers and negotiate access.

Do not assume that every building automatically belongs to the faction.

#### Unified building architecture

Use a common building architecture with configurable capabilities, including:

- Shelter.
- Storage.
- Resource collection.
- Manufacturing.
- Ownership.
- Access rights.
- Worker capacity.
- Maintenance.
- Upgrades.

Reuse this architecture for later residential, industrial, military and institutional buildings.

#### Cognition and economic development

The Creatures-inspired NPC decision system should enable individuals to recognise the benefits of constructing and upgrading productive shelters.

NPCs should learn from the actual consequences of their investments.

A typical developmental pathway might be:

1. Forage for food.
2. Construct a primitive woodcutter's shelter.
3. Collect and store wood.
4. Sleep in the shelter.
5. Accumulate surplus wood.
6. Exchange wood for other goods.
7. Upgrade the shelter.
8. Increase productive capacity.
9. Cooperate with or employ other individuals.
10. Invest in additional productive assets.

This is an example of emergent behaviour, not a mandatory scripted sequence.

#### Acceptance tests

Demonstrate that:

- An NPC can forage without any building.
- An NPC cannot collect raw materials without access to an appropriate collection building.
- An NPC can autonomously construct its first collection building without material inputs.
- Construction consumes actual labour and time.
- NPCs physically collect and return raw materials to their designated building.
- Collection respects finite storage capacity.
- Collection buildings provide sleeping accommodation.
- Upgrading consumes actual materials and labour.
- Upgrades provide measurable productive or shelter benefits.
- Individuals can cooperate, trade and specialise around these buildings.
- No faction-level economic AI is required for these activities.

Update the owner decision register, economic design, physical ontology, cognition design, building definitions and development packages accordingly.

The key economic principle is now particularly clear:

**Foraging sustains life. Labour creates initial capital. Capital enables resource collection. Resources enable further investment. Trade enables specialisation. Cooperation enables civilisation.**

That is a much stronger foundation for everything we want to build subsequently.
