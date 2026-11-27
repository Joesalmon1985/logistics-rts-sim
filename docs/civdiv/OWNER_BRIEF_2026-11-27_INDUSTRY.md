# Owner brief, 27 Nov 2026: Industrialisation, OpenRA and Power Grid (verbatim)

The owner's second brief for the run, kept word for word. Its decisions are set **D6** in [OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md). Where it conflicts with the first brief ([OWNER_BRIEF_2026-11-26.md](OWNER_BRIEF_2026-11-26.md)), the "additional owner decisions" part below wins; in particular there is **no** external buyer converting ore into credits.

The owner also said: "continue the Civilisation and Divinity preparation - then make a start and keep working until we've got a finished playable game" and "DO NOT STOP UNTIL THE WORK IS COMPLETE - Use multiple sub agents working together to get the work done."

---

## Additional development scope — Industrialisation, OpenRA and Power Grid

Add a third major development workstream to tomorrow's Civilisation and Divinity run.
I want to incorporate the industrial production, harvesting, military equipment and logistics mechanics inspired by OpenRA, together with the electricity generation, fuel consumption, infrastructure and energy economics inspired by the board game Power Grid.
These must operate within the same persistent NPC, biological, social, economic and institutional simulation.

### Industrial economy
Plan additional geological resources, including iron ore, copper, coal, crude oil and uranium.
Support finite geological deposits, mines, manual extraction, mechanised extraction, physical transportation, refineries and industrial production chains.
Harvesters should behave similarly to OpenRA ore trucks, automatically travelling between resource sources and processing or purchasing facilities.
However, vehicles are persistent physical assets operated by actual NPCs.
Vehicles must have ownership, location, condition, cargo capacity, operators and appropriate operating requirements.
Refineries should generate income through genuine economic transactions. Support an optional simplified OpenRA-style external market purchasing ore for credits, but do not allow unexplained monetary creation or resource duplication. *(Superseded by "Industrial integration — additional owner decisions" §1 below: the external buyer is removed from the normal design.)*
Industrial goods must also be usable in actual production chains.
Factories should have production orders, finite capacity, material inputs, workers, electricity requirements and completed physical products.

### Power Grid
Design a reusable electricity generation and distribution architecture inspired by Power Grid.
Support coal, oil, hybrid-fuel, waste, nuclear and renewable generating technologies.
Different generators should have different construction costs, fuel consumption, generating capacities, operating costs and efficiencies.
Fuel must be physically delivered and consumed.
Electricity must be supplied through connected networks rather than a single global faction-level power number.
Initially implement an abstract deterministic network model rather than complex electrical engineering.
Support generation, demand, connection, disconnection, allocation priorities, insufficient supply, outages and appropriate production consequences.
Power companies may be privately, publicly, cooperatively or communally owned.
They may purchase fuel, employ staff, sell electricity, accumulate capital, compete, fail and invest in improved infrastructure.

### Military production
Plan military unit categories inspired by OpenRA.
These should include infantry, engineers, reconnaissance vehicles, armoured transports, tanks, artillery, aircraft, naval equipment and defensive structures.
Initially prioritise infantry, transport vehicles, tanks and simple defensive structures.
Infantry must be recruited and trained existing NPCs, not newly manufactured people.
Vehicles and military equipment are physical manufactured assets.
Military vehicles require appropriate crew, fuel, ammunition, maintenance and logistical support.
Support military production queues, recruitment, procurement, equipment ownership and organisational budgets.
Military personnel retain their personal identity, needs, learning, households and relationships.
Military organisations can operate autonomously.
The player can issue divine instructions, compel individuals or possess vehicle operators, according to the existing Mythic and divine-control rules.

### Political economy
Industrialisation must integrate with the existing Marxian and institutional economic design.
Mines, refineries, factories, transport businesses and electricity providers may belong to different organisations.
Allow productive ownership, employment, capital investment, competition, profit, economic inequality, resource scarcity and government regulation to emerge.
Do not create a separate military economy with no relationship to civilian production.
Military and civilian organisations must be capable of competing for the same labour, materials, fuel, capital and electricity.

### Presentation
Plan:

* Visible mines and geological deposits.
* Harvesters travelling between extraction and processing facilities.
* Physically carried vehicle cargo.
* Industrial buildings and production animations.
* Vehicle inspection.
* Production queues.
* Electricity network overlays.
* Industrial logistics overlays.
* Resource shortage indicators.
* Power station and industrial organisation inspection.

Retain seamless island presentation without exposing the underlying geographical hexagons during normal gameplay.

### Scenarios
Add an industrial starting scenario with an established population, appropriate technological knowledge, mines, vehicles, industrial organisations and electricity infrastructure.
This scenario must use the same underlying simulation as the prehistoric Living Island scenario.
Do not require every industrial integration test to wait for prehistoric civilisation to develop through numerous generations.
Retain autonomous technological progression as the long-term objective.

### Documentation and work packages
Extend the owner decision register, ontology, economy, military, institutional, analytical and UI documentation.
Create appropriate additional owning design documents for industrial resources, vehicles, production, electricity and industrial military systems.
Prepare independent work packages covering:

* Industrial content and production recipes.
* Vehicles and harvesting.
* Refineries and industrial markets.
* Electricity generation and distribution.
* Mechanised military production.
* Industrial presentation and analytics.

Identify exact dependencies on the existing Stage 4 milestones and tomorrow's Civilisation and Divinity work packages.
The systems must share existing ownership, inventory, action, population, institutional and event contracts.
Do not introduce competing architectures.

### Prioritisation
Tomorrow's development window remains approximately 18 hours.
Do not compromise the core civilisation and divine gameplay objectives merely to implement every industrial feature.
Prioritise a complete industrial vertical slice:

1. A persistent NPC operates a harvester.
2. The harvester collects physical ore.
3. It delivers the ore to a refinery.
4. The ore is processed or sold through a recorded transaction.
5. A power station receives and consumes fuel.
6. Electricity reaches a connected factory.
7. The factory produces a vehicle using actual materials, labour and power.
8. An existing NPC operates the completed vehicle.
9. Disrupting fuel supply or electrical connectivity affects industrial production.
10. All events reconcile in the analytical warehouse and reproduce deterministically.

Treat additional units, advanced energy markets, complex electricity networks and advanced military mechanics as subsequent extensions.
Prepare the documentation and implementation packages tonight, rather than beginning major code changes.
Update tomorrow's NEXT_SESSION_PROMPT, dependency graph, runbook, acceptance checklist and delivery priorities accordingly.

---

## Industrial integration — additional owner decisions

Further refine the planned integration of Widelands, OpenRA and Power Grid.
The overriding requirement is that these mechanics must operate within our existing artificial world's ontology, economy, institutions and ownership systems.
Do not introduce separate economic abstractions merely because the original games use them.

### 1. One coherent economy
There must be one monetary and accounting architecture.
Do not introduce a separate OpenRA credits system or Power Grid money system.
Harvesters delivering resources to refineries must not automatically create money.
Power stations generating or supplying electricity must not automatically create money.
Revenue arises from actual payments by customers, purchasers, governments or other organisations.
Money issuance and destruction must occur only through explicitly defined monetary mechanisms. Lending and banking must account for claims and liabilities appropriately.
Remove the previously proposed simplified external buyer that automatically converts ore into credits from the normal industrial design.
Production creates goods and useful services, not money.

### 2. Physical resources and industry
Retain existing Widelands production chains and resources, including coal.
Introduce additional resources where required:

* Crude oil.
* Refined fuels.
* Industrial metals.
* Waste.
* Scrap.
* Recyclable materials.
* Abstract nuclear material.

Harvesters should physically collect and transport resources in the style of OpenRA.
Refineries transform physical resources into useful products.
Vehicles must be persistent assets operated by actual NPCs.
The extraction, transportation, processing and consumption systems must use the same ownership, inventory and labour architecture as the rest of the economy.

### 3. Resource allocation
Do not reproduce Power Grid's artificial central fuel market.
Use existing physical production chains, barter, trade, markets, contracts, communal allocation and organisational supply arrangements.
Power companies may obtain fuel through:

* Purchasing resources.
* Direct ownership of extraction facilities.
* Supply contracts.
* Internal organisational transfers.
* Communal distribution.
* Government provision.

Markets are one possible allocation mechanism, not a universal requirement.

### 4. Electricity
Retain the Power Grid principle of different generating technologies with different construction costs, fuel requirements, efficiencies and capacities.
Support coal, oil, hybrid, waste, nuclear and renewable generating technologies.
Fuel must physically exist and be delivered and consumed.
Electricity must be distributed through connected infrastructure.
Power station revenues arise from actual payments for supplied electricity or agreed funding arrangements.
Support private, public, cooperative and communal power provision.
Electricity demand must relate to actual activities and equipment.

### 5. Knowledge and technological designs
Introduce an explicit ontology for technological knowledge.
Distinguish:

* Knowledge understood by individuals.
* Formal designs and blueprints.
* Physical documents or other knowledge artefacts.
* Ownership of those artefacts.
* Patents.
* Licences.
* Trade secrets.
* Practical ability to construct and operate technology.

Unlike material goods, knowledge can be copied without removing the original holder's knowledge.
Individuals and institutions can discover, teach, share, sell, copy, conceal or steal information.
Technological designs should be reusable data-driven definitions describing available productive capabilities and construction requirements.

### 6. Research and invention
Individuals and research institutions should eventually be able to develop new technologies and improved designs.
Research consumes actual time, labour and resources.
A discovery may be formalised into a reproducible design.
Discovering a design does not automatically construct a functioning asset.
Construction requires materials, skills, workers and other appropriate resources.
Gods may reveal technological knowledge, but divine revelation does not automatically confer legal ownership or patent protection.

### 7. Patents and licences
Introduce patents as legally recognised claims rather than intrinsic properties of knowledge.
A patent may grant exclusive rights to manufacture, use, sell or license a design according to applicable laws.
Patents can be registered, transferred, auctioned, licensed, challenged, revoked or expire.
Patents may be recognised differently by different governments.
Knowledge can physically be copied even when that copying is legally prohibited.
Courts and other institutions may enforce patent rights through evidence, decisions and appropriate remedies.
Support alternative intellectual property arrangements including open knowledge, public ownership, cooperative research, trade secrecy and compulsory licensing.
Do not assume a universal patent system or that patents are necessary for innovation.

### 8. Power Grid auctions
Reinterpret the Power Grid power plant auctions.
Allow individuals and organisations to bid for:

* Technological designs.
* Patents.
* Exclusive licences.
* Non-exclusive licences.
* Existing physical productive assets.

Use the same auction and monetary systems that can support other economic assets.
Winning an auction for a design must not automatically produce a completed power station.
Retain the distinction between owning a design, having permission to use it, possessing the necessary knowledge and constructing a physical asset.

### 9. Waste and circular production
Introduce waste as a consequence of consumption, production and asset destruction.
Waste must be accounted for through the resource and transformation ledgers.
Support collection, transportation, storage, disposal, recycling and waste-powered electricity generation.
Waste services and recycling enterprises should use the existing institutional and economic architecture.
Environmental damage may arise from unmanaged waste and industrial activity.

### 10. Nuclear technology and military capabilities
Introduce nuclear material as an abstract fictional strategic resource.
Support civilian nuclear electricity generation and OpenRA-inspired nuclear superweapons through fictional game-level technologies and capabilities.
Do not simulate real-world nuclear weapon engineering.
Strategic military capabilities should have genuine political, economic, environmental and demographic consequences.
Military organisations must obtain resources, equipment, labour and electricity through the same systems used by civilian organisations.
Support later diplomatic, inspection, disarmament and deterrence mechanics.

### 11. Institutions
Extend the generic institutional architecture to support:

* Universities.
* Research organisations.
* Patent offices.
* Commercial courts.
* Electricity utilities.
* Transportation companies.
* Waste authorities.
* Recycling enterprises.
* Environmental regulators.

These must remain organisations made up of NPCs, with actual workers, assets, knowledge, budgets, services and responsibilities.
The god influences their development through existing divine mechanics, not automatic administrative control.

### 12. Ontology, analytics and testing
Update WORLD_ONTOLOGY and the relevant economic, institutional, technological and industrial design documents.
Explicitly distinguish:

* Matter.
* Energy.
* Money.
* Financial claims.
* Knowledge.
* Physical knowledge artefacts.
* Legal rights.
* Productive assets.
* Organisations.

Define appropriate warehouse facts, histories and reconciliation rules for each.
Add acceptance tests demonstrating:

* Extracting and transporting oil without creating money.
* Selling resources through actual payment and ownership transfer.
* Supplying electricity without creating money.
* Fuel shortages affecting electricity generation.
* Copying knowledge without removing the original knowledge.
* Transferring a patent without duplicating physical assets.
* Purchasing a licence and constructing an asset.
* Unauthorised use of a design creating a potential legal dispute.
* Waste being generated, collected and reused.
* Deterministic replay of industrial and technological activity.

### 13. Development priorities
Update tomorrow's 18-hour development plan, shared contracts, work packages and acceptance criteria.
Prioritise the reusable foundations of physical industry, vehicles, electricity, technology and knowledge ownership.
Full patent litigation, advanced research, complex energy markets and nuclear warfare need not all be implemented during the initial run.
Do not let these additions compromise the existing population, civilisation, institutional and divine gameplay objectives.
The long-term objective is one integrated artificial world, not a collection of separate game economies.
