# Owner brief, 26 Nov 2026: Civilisation and Divinity (verbatim)

This is the owner's brief for the 18-hour run, kept word for word as the source for every planning document in `docs/civdiv/`. The decisions in it are recorded in [OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md) (set D5). Where an older document contradicts this brief, this brief wins.

---

Civilisation and Divinity — Preparation for an 18-hour autonomous development run

## 1. Objective of this session
Tomorrow I intend to start a substantial 18-hour development run using Claude, multiple parallel specialist subagents and separate Git worktrees.
Tonight, I want you to prepare all the documentation, architectural decisions, development packages, dependencies, acceptance criteria and handover instructions necessary to make that run as productive as possible.
This is primarily a design, documentation and implementation-planning session. Do not begin implementing the major new gameplay systems tonight.
Inspect the actual current repository, including the latest Stage 4 implementation, DEV_LOG, handover, design conventions, acceptance tests, WORLD_ONTOLOGY and relevant source code.
Do not assume that previously planned features have been implemented. Establish the actual current position from the code, tests and recorded results.
Retain all existing agreed requirements unless explicitly superseded by the owner decisions below.
The two major objectives for tomorrow are:

1. Civilisation: the Darwinian, Dawkins-inspired and Marxian development of an autonomous society, including trade, ownership, labour, economic classes and institutions.
2. Divinity: an entertaining player-control system in which the player acts as a god, influencing individuals and societies through divine commands, possession, miracles, punishment, commandments and physical manifestation.

These must be interconnected parts of one artificial world, not separate gameplay modes with incompatible simulation systems.

# PART ONE — FUNDAMENTAL DESIGN

## 2. Intellectual foundations
Retain the existing intellectual vision:

* Darwin: biological evolution, reproduction, natural selection, inherited variation and population dynamics.
* Dawkins: evolutionary game theory, cooperation, competition, kinship, reciprocal altruism and exploitation.
* Creatures: individual drives, perception, concepts, associative learning, communication and personal experience.
* Axelrod: repeated strategic interactions and the emergence of cooperation.
* Smith: specialisation, exchange, division of labour and productive efficiency.
* Marx: productive ownership, labour, surplus, capital accumulation, inheritance, economic classes and conflicting material interests.
* Ostrom: common resources, collective action, self-governance and independently developing institutions.
* Black & White: divine intervention, miracles, worship, punishment and god-game interactions.
* The Sims: observing individuals, relationships, households and complete lives.
* Widelands: physical goods, production chains, transportation and logistics.

These influences must inform actual mechanics rather than simply provide narrative decoration.
Do not force the simulation to validate a particular political, economic or evolutionary theory.

## 3. Core non-negotiable requirements
Every ordinary NPC is a persistent autonomous individual.
They must retain:

* Biological identity and genealogy.
* Physical needs.
* Health and ageing.
* Personal possessions.
* Individual perception.
* Knowledge and beliefs.
* Lifetime learning.
* Relationships.
* Social memory.
* Cultural knowledge.
* Personal interests and motivations.
* The ability to cooperate, compete, trade and make decisions.

Individuals can be born, grow, reproduce, age and die.
Workers, soldiers, officials, teachers, doctors, bankers and priests are roles undertaken by individuals, not separate artificial unit types.
The civilisation must remain capable of developing without any divine intervention.
Do not replace individual decision-making with faction-level or institutional abstractions that secretly perform all meaningful work.
Retain exact deterministic execution, headless simulation, structured events, save/load continuation, replay and the analytical warehouse.

# PART TWO — OWNER DECISIONS

## 4. The player is a god, not a government
This supersedes earlier design documents that treated the player as directly controlling the state.
The player is exclusively a divine entity.
Governments, institutions, businesses, religious organisations, armies and communities are made up of NPCs and have their own decision-making processes.
The player may influence these organisations by influencing the individuals who operate them.
For example:

* Encourage a government to introduce healthcare.
* Inspire an individual to become a teacher.
* Reveal a commandment concerning property ownership.
* Persuade citizens to support a reform.
* Compel an official to undertake a particular decision.
* Temporarily possess a political leader.
* Reveal evidence to a judge.
* Miraculously create resources.
* Punish an individual or organisation through divine intervention.

The player does not automatically possess administrative authority merely because they are a god.
Religious authority, political authority, economic ownership and institutional authority must remain distinct.
Governments may reject divine instructions.
Religious institutions may disagree with their god.
Political leaders may pursue their own interests.

## 5. Gods and factions
Every initial faction should have an associated god.
Rival gods should eventually be controlled by independently replaceable AI policies.
The human player normally represents one god, but sandbox scenarios may permit switching between gods.
A god must have a persistent identity independent of:

* Faction.
* State.
* Government.
* Religion.
* Population.
* Territory.

A god cannot permanently lose or cease to exist.
States may collapse, populations may become extinct and religions may disappear, but the god remains.
The player may attempt to recover through migration, conversion, resurrection or other legitimate divine interventions.
Do not automatically regenerate extinct populations.
Sandbox remains the default gameplay mode.
Retain optional objectives, including the existing victory-point mode where appropriate.

## 6. Four player interaction modes
Plan for four complementary ways of experiencing the same world.

**Mode A: Overhead divine observation**
The player can freely move an overhead 3D camera, inspect the world, issue commands and perform miracles.
They can zoom from the whole island down to individual NPCs.

**Mode B: Manifested divine avatar**
The player can manifest as a physical avatar within the world.
The avatar:

* Has a distinct persistent divine identity.
* Uses the existing movement and interaction system.
* Can physically approach NPCs and objects.
* Can pick up, carry, drop, give and use objects.
* Can communicate through gestures.
* Can demonstrate activities.
* Does not experience ordinary biological hunger or ageing.
* Can be attacked.
* Returns to divine existence if destroyed.
* Consumes Mythic to manifest and maintain.

Provide a third-person camera when appropriate, with the ability to return to overhead observation.

**Mode C: Possession**
The player can temporarily possess an ordinary NPC.
During possession:

* The player controls movement and available physical actions.
* The NPC retains their original persistent identity.
* Their body continues experiencing hunger, fatigue, disease, injury and ageing.
* They can be injured or killed.
* Physical capabilities remain those of the possessed NPC unless a separate miracle changes them.
* The NPC remains conscious of the experience and can remember it.
* The NPC may learn from actions performed during possession.
* The simulation records that the actions were divinely controlled rather than voluntarily chosen.
* Possession consumes Mythic continuously.
* The player can end possession at any time.
* Possession ends automatically when Mythic is exhausted.
* Initially, only one individual may be possessed at a time.

Architecturally support possession of foreign citizens and eventually animals, but these can be later extensions.

**Mode D: Divine analysis**
The player can examine the civilisation through reports, statistics, historical records and individual inspection.
Distinguish divine observation from imperfect records held by governments and institutions.

These modes must operate against the same authoritative simulation.
Do not create separate worlds or duplicate NPCs when switching modes.

# PART THREE — DIVINE GAMEPLAY

## 7. Mythic power
Use Mythic as the shared supernatural resource.
Mythic pays for:

* Divine suggestions.
* Commands.
* Compulsion.
* Possession.
* Avatar manifestation.
* Blessings.
* Healing.
* Smiting.
* Resource creation.
* Commandments.
* Resurrection.
* Other miracles.

Initially implement:

* A configurable maximum.
* Configurable regeneration.
* Configurable action costs.
* Continuous upkeep costs.
* Cooldowns where appropriate.
* Unlimited sandbox mode.
* Disabled divine-intervention mode.

Begin with fixed regeneration rather than a worship-based feedback loop.
Later, worship, temples, sacrifices and religious affiliation may influence available Mythic.
The human player and AI gods must operate under the same rules.
Do not spend significant development time balancing exact costs before the underlying mechanics work.

## 8. Distinct divine intervention types
Explicitly distinguish the following.

**Divine suggestion.** An individual receives a suggestion and decides independently whether to consider or follow it.

**Divine command.** A more authoritative instruction that influences an individual's decision-making, but can still be refused.

**Divine compulsion.** A temporary compulsory instruction.
The player spends Mythic to require an individual to attempt a particular action.
Compulsion may temporarily override the individual's normal priorities, including survival-related priorities, but cannot bypass physical constraints.
A compelled NPC still needs to move to an object, possess the required materials and perform an available action.
Compulsion expires on completion, timeout, cancellation, death or another defined condition.

**Possession.** Temporary direct control of the NPC's available actions.

**Commandment.** A persistent religious instruction that can be learned, transmitted, interpreted, followed, ignored or forgotten.

**Miracle.** A validated supernatural intervention that directly changes the authoritative physical state.

These must not be conflated into one generic divine instruction.

## 9. Divine action vocabulary
Initially support a meaningful subset of:

* Move to a location.
* Approach a person.
* Look at or inspect an object.
* Gather a resource.
* Take an object.
* Drop an object.
* Give goods.
* Eat or drink.
* Use a tool.
* Sleep.
* Work.
* Construct.
* Trade.
* Cooperate.
* Communicate.
* Protect.
* Retreat.
* Attack.
* Teach.
* Treat an injured individual.
* Perform an available institutional action.

Reuse the existing action primitives and object affordances.
Do not create a second implementation of ordinary NPC actions exclusively for the divine player.
A divine instruction should specify a valid action, target, scope and expiry.
The player should be able to issue instructions to individuals, groups, households, institutions and locations where appropriate.
Institution-level instructions are communicated to relevant individuals within the organisation.

## 10. Miracles
Plan for the following:

* Heal injury.
* Heal disease.
* Bless an individual.
* Protect an individual.
* Smite an individual.
* Smite a building.
* Smite a tree or resource source.
* Create basic resources.
* Destroy resources.
* Reveal knowledge.
* Reveal technologies.
* Consecrate locations.
* Resurrect individuals.
* Area-based miracles.
* Weather manipulation when supported by ecology.
* Terrain manipulation as a later feature.

Miracles must use validated deterministic simulation commands.
Creating or destroying material resources must produce explicit ledger entries and domain events.
Do not bypass inventory conservation, physical location, death handling, estates or other existing invariants.
A miracle's effect must be genuine. It must not simply play a visual effect while leaving the authoritative state unchanged.

## 11. Resurrection
The player can resurrect dead NPCs at a Mythic cost.
Support the following conceptual variants:

* Lesser resurrection: lower cost, poor initial health and energy.
* Full resurrection: restores life with reasonable health.
* Divine restoration: additional healing of injuries or disease.
* Rejuvenation: a separate, potentially expensive intervention affecting biological age.

Resurrection must restore the original persistent individual identity.
Preserve:

* Genealogy.
* Historical identity.
* Personal memories.
* Learned associations.
* Relevant relationship history.

Do not automatically reverse the events that occurred after death.
In particular:

* Previously distributed estates remain with their current owners.
* Existing partnerships are not forcibly restored.
* Previous employment contracts are not automatically reinstated.
* Children do not automatically return to a previous household.
* Historical deaths remain recorded.

A resurrection is a new event in the individual's life history, not deletion of their death.
Architecturally support resurrecting individuals who died generations earlier if sufficient historical state is retained.
Plan for efficient archival of deceased individuals so that retaining resurrection eligibility does not require continuously processing dead NPCs.

## 12. Commandments
Implement a structured commandment system.
A commandment should support:

* Required behaviour.
* Prohibited behaviour.
* Encouraged behaviour.
* Relevant actions and objects.
* Applicable individuals or groups.
* Conditions.
* Geographical scope.
* Date of introduction.
* Issuing god.
* Importance.
* Revision history.
* Revocation.

Examples:

* Share surplus food with hungry children.
* Do not steal.
* Honour agreements.
* Protect forests.
* Teach children.
* Assist strangers.
* Care for elderly people.
* Forgive those who repent.
* Protect communal property.
* Contribute to public services.
* Defend your community.
* Treat injured individuals.

Provide predefined templates and a structured commandment builder.
Do not use an LLM to interpret arbitrary free-text commandments during simulation execution.
The player-facing text is a representation of structured simulation rules.
Commandments must not become universally enforced action restrictions.
Individuals may:

* Learn a commandment.
* Adopt it.
* Reject it.
* Misunderstand it.
* Follow it selectively.
* Forget it.
* Communicate it.
* Resolve conflicts between commandments differently.

An institution may subsequently adopt a commandment as formal law, but religious rules and legal rules remain distinct.

## 13. Religion, beliefs and culture
Maintain separate concepts of:

* Belief.
* Faith.
* Trust.
* Fear.
* Loyalty.
* Religious affiliation.
* Political citizenship.
* Cultural membership.

Do not collapse everything into one obedience score.
An NPC can believe their god exists while fearing or distrusting them.
An NPC may follow religious customs without strong personal belief.
Individuals should eventually be capable of:

* Praying for assistance.
* Worshipping.
* Communicating religious knowledge.
* Converting.
* Rejecting a religion.
* Worshipping another god.
* Participating in religious organisations.
* Developing different interpretations of commandments.

Architecturally support multiple religions within a state, one god having followers across several states and individuals worshipping more than one god.
These advanced arrangements need not all be completed tomorrow.
Religious institutions must use the same ownership, staffing, resource and management systems as other institutions.
Priests may accumulate property, acquire influence and develop interests that conflict with other organisations or even their god.

## 14. Divine learning
Integrate divine actions with the existing Creatures-inspired learning architecture.
For example:

* An NPC steals food and satisfies hunger.
* The player subsequently smites the NPC.
* The NPC experiences injury or fear.
* The individual's learned associations may change.
* Witnesses may observe and communicate the event.

Do not automatically assign the interpretation intended by the player.
An individual might learn that theft is dangerous, or that theft is dangerous only in particular circumstances.
An NPC may associate punishment with the wrong action.
Divine intervention should support contextual learning, attribution, observation, imitation and cultural transmission.
Record the causal information needed to investigate these outcomes.

# PART FOUR — THE AUTONOMOUS CIVILISATION

## 15. Population and demographic viability
Retain all existing M3–M5 requirements.
The current reports indicate important demographic problems, including universal extinction under default conditions.
Do not eliminate the possibility of population collapse.
However, establish at least one favourable scenario capable of sustaining a genuinely multigenerational population.
Investigate independently:

* Fertility.
* Reproductive opportunities.
* Founder age distribution.
* Caregiving.
* Childhood mortality.
* Disease.
* Food accessibility.
* Ecological carrying capacity.
* Population density.
* Resource regeneration.
* Learned behaviour.

Support mixed-age founder populations and established households.
Separate emotional partnerships, reproductive relationships, biological parenthood, caregiving and household membership.
Do not force a particular family structure.
Retain same-sex partnerships, adoption, fostering and non-biological caregiving.
Use headless experiments and analytical reporting to establish what actually causes population decline.

## 16. Physical ontology
Retain the existing large geographical hexagons and fine movement lattice internally.
Distinguish:

* Geographical region.
* Physical node.
* Object location.
* Physical possession.
* Ownership.
* Permission to use.
* Physical accessibility.
* Visual perception.
* Communication range.
* Physical interaction range.

Goods can be:

* On the ground.
* In a store.
* Carried.
* Equipped.
* Associated with an appropriate productive asset.

Reuse ItemLot or equivalent efficient structures for fungible goods.
Objects must expose meaningful affordances.
Individuals must approach objects and other individuals before performing interactions requiring physical proximity.
Stealing should be a physical taking action classified according to ownership, permission, applicable norms and law.
Do not implement magical remote resource transfers.

## 17. Economic development
Continue the existing Stage 4 progression:
Subsistence → cooperation → barter → specialisation → surplus → productive ownership → employment → capital accumulation → economic classes → political organisation.
Implement or retain:

* Personal possessions.
* Productive assets.
* Individual resource gathering.
* Tools and equipment.
* Production chains.
* Storage.
* Gifts.
* Barter.
* Deferred obligations.
* Lending.
* Employment.
* Enterprises.
* Cooperative ownership.
* Communal ownership.
* State ownership.
* Private ownership.
* Land access.
* Resource depletion.
* Inheritance.
* Economic failure.
* Social mobility.

Individuals should value goods according to their own circumstances.
Do not introduce a universal fixed barter price.
Retain reference-value measures for analytics only.
Currency should eventually emerge from commonly accepted exchange goods or institutional developments rather than simply appearing as an arbitrary replacement for barter.
Economic classes must be derived from relationships to labour, ownership and productive assets.
Do not assign permanent class identities or political beliefs to NPCs.

## 18. Organisations and institutions
Establish a reusable organisational framework before implementing independent institutions.
An organisation may possess:

* Persistent identity.
* Type and purpose.
* Charter.
* Ownership.
* Governance rules.
* Members.
* Employees.
* Managers.
* Buildings.
* Productive assets.
* Stores.
* Budget.
* Financial or material obligations.
* Service requests.
* Service activities.
* Service outcomes.
* Institutional records.
* Policies.
* Relationships to other organisations.

Support state, private, communal and cooperative organisations.
Institutions may form, compete, grow, decline, fail or eventually merge and split.
Do not assume all institutions originate from government.
Individuals and communities must be capable of independently establishing organisations where the required conditions exist.
Institutional formation must arise from actual needs, knowledge, resources, cooperation and opportunities.
A threshold may make an organisational opportunity available, but should not automatically construct a fully staffed institution.

## 19. Healthcare
Begin with functional healers and informal treatment.
Develop towards clinics and hospitals.
Include:

* Illness.
* Injuries.
* Treatment requests.
* Medical knowledge.
* Healthcare workers.
* Treatment time.
* Resource requirements.
* Recovery.
* Treatment effectiveness.
* Unmet demand.
* Capacity constraints.
* Prevention where supported.

Healthcare must change actual biological outcomes.
Do not simply award a faction-wide health multiplier for owning a hospital.

## 20. Education
Begin with teaching, demonstration, apprenticeships and shared knowledge.
Develop towards schools and advanced educational institutions.
Include:

* Learners.
* Teachers.
* Knowledge and skills.
* Teaching activities.
* Observation.
* Practice.
* Learning outcomes.
* Resource and time costs.
* Capacity constraints.

Education must interact with actual individual cognition and learned capabilities.
Knowledge should be capable of spreading and being lost.

## 21. Banking and finance
Begin with physical goods, lending and deferred obligations.
Support:

* Borrowers.
* Lenders.
* Principal.
* Repayment.
* Default.
* Credibility.
* Collateral where appropriate.
* Investment.
* Productive asset acquisition.
* Savings.
* Institutional lending.
* Insolvency.

Later develop interest, monetary accounting, more sophisticated credit, deposits and banks.
Do not introduce financial claims without corresponding liabilities or appropriate ledger treatment.
Financial claims do not themselves create physical resources.

## 22. Courts, policing and prisons
Begin with informal dispute resolution and community mediation.
Develop towards:

* Complaints.
* Witness observations.
* Evidence.
* Investigation.
* Accusations.
* Arbitration.
* Decisions.
* Restitution.
* Fines.
* Enforcement.
* Detention.
* Rehabilitation.

Crime must be defined relative to applicable laws and norms.
Judges and police must not have unrestricted access to authoritative truth.
Innocent individuals may be wrongly accused.
Officials may make mistakes or act dishonestly.
Prisons must require actual facilities, guards, food and operating resources.
Formal imprisonment is not the only available response to wrongdoing.

## 23. Military
Continue towards persistent NPC soldiers.
Include:

* Voluntary recruitment.
* Compensation.
* Equipment.
* Training.
* Military obligations.
* Leadership.
* Logistics.
* Morale.
* Orders.
* Combat.
* Injuries.
* Surrender.
* Retreat.
* Desertion.
* Defection.

Military individuals retain families, needs, possessions, learned experience and personal interests.
The player influences military leaders through divine actions rather than automatically commanding the army.
Conscription may later emerge as a government policy.

## 24. Other institutions
Plan for:

* Government and treasury.
* Welfare.
* Communal stores.
* Childcare.
* Infrastructure.
* Environmental management.
* Media.
* Religious organisations.
* Statistics and research.
* Political organisations.
* Trade associations.
* Workers' organisations.

Introduce simple functional versions where practical.
Do not attempt to implement every advanced service immediately.
Prioritise reusable capabilities and demonstrable end-to-end behaviour.

# PART FIVE — VISUAL DESIGN AND PLAYER EXPERIENCE

## 25. Seamless island presentation
The player should not see the underlying Catan-style geographical hexagons during normal gameplay.
The island should appear to be one continuous, natural-looking landscape.
Retain the existing hexagonal simulation and fine node grid internally.
Develop a presentation approach that supports:

* Seamless terrain transitions.
* Irregular forest boundaries.
* Natural-looking mountains and hills.
* Coastlines.
* Fields and resource areas.
* Curving paths and roads.
* Procedural decoration.
* Recognisable settlements.
* Visually distinct buildings.

The fine movement nodes and large hexagonal regions should be visible only in appropriate developer overlays.
Do not rewrite authoritative pathfinding or terrain rules merely to disguise the grid.
Rendered geography must remain consistent with actual accessibility and available resources.

## 26. Camera and character controls
Plan for:

* Freely movable overhead camera.
* Pan.
* Rotation.
* Zoom.
* Whole-island view.
* Close NPC observation.
* Selected-NPC tracking.
* Avatar third-person camera.
* Possessed-NPC third-person camera.
* Smooth switching between modes.
* Direct clicking or tapping on individuals and objects.
* Area selection.
* Household selection.
* Institution selection.
* Contextual actions.

Desktop is the initial priority.
Retain mobile-compatible command architecture and avoid unnecessarily desktop-exclusive simulation assumptions.

## 27. Character appearance
Use recognisable stylised low-poly characters.
Individuals should visually communicate:

* Age.
* Physical development.
* Equipment.
* Occupation through actual activities and possessions.
* Pregnancy.
* Injury.
* Illness.
* Fatigue.
* Emotional state.
* Carried resources.
* Relevant divine effects.

Characters should possess procedural names.
The player can name, favourite, follow and inspect individuals.
Support family trees and historical life records.
Provide an appropriate memorial or history view for deceased individuals.

## 28. NPC communication animations
Normal NPC communication should not use written sentences above their heads.
Decoded messages may remain available through developer or accessibility options.
Develop a reusable animation vocabulary for:

* Attention.
* Pointing.
* Requesting.
* Offering.
* Displaying objects.
* Giving.
* Receiving.
* Accepting.
* Refusing.
* Agreeing.
* Cooperating.
* Warning.
* Threatening.
* Apologising.
* Expressing gratitude.
* Begging.
* Praying.
* Teaching.
* Fear.
* Submission.

NPCs should approach and face appropriate recipients where practical.
Conversation can consist of sequences of gestures.
Use optional small visual symbols only where animation alone is insufficient.
Animations must represent actual simulation actions and events.
Unity must not determine whether communication succeeds.

## 29. Divine visual effects
Develop a consistent visual identity for divine interventions.
Potential effects include:

* Light.
* Auras.
* Lightning.
* Healing effects.
* Resource manifestation.
* Possession transitions.
* Avatar manifestation.
* Sacred locations.
* Visible commandments or revelations.
* Prayers.
* NPC reactions.

Make the divine effects readable without requiring large amounts of text.
The player must be able to distinguish a suggestion, a command, compulsion, possession and a physical miracle.

## 30. Main gameplay interface
Use a hybrid interface.
Ordinary gameplay should primarily involve interaction with the visible world.
More complicated analytical, historical and doctrinal information may use conventional panels.
Plan the following:

* Compact divine toolbar.
* Contextual radial menu.
* Mythic meter.
* Selected-person controls.
* Selected-object controls.
* Group commands.
* Divine power selection.
* Commandment editor.
* Avatar and possession controls.
* Time controls.
* Alerts.
* Optional developer overlays.

Provide pause, 1x, 2x, 5x, 10x and accelerated observation where technically appropriate.
Allow the player to pause while considering interventions.
Aggregate ordinary events rather than notifying the player about every transaction and death.

## 31. NPC inspection
Retain and extend the People panel.
The player should be able to inspect:

* Name.
* Age.
* Health.
* Needs.
* Current activity.
* Intentions.
* Possessions.
* Equipment.
* Household.
* Family tree.
* Relationships.
* Trust.
* Fear.
* Beliefs.
* Faith.
* Learned associations.
* Recent experiences.
* Employment history.
* Economic relationships.
* Religious affiliation.
* Known commandments.
* Divine commands.
* Reasons for decisions.

Clearly distinguish authoritative debug information from what the NPC or government actually knows.

## 32. Tutorial and scenarios
Plan a tutorial that progressively introduces:

1. Observing individuals.
2. Understanding needs and actions.
3. Following a household.
4. Issuing a divine suggestion.
5. Issuing a command.
6. Performing a miracle.
7. Using compulsion.
8. Possessing an individual.
9. Manifesting an avatar.
10. Revealing a commandment.
11. Observing trade and organisations.
12. Interpreting institutional reports.

Support:

* Normal sandbox.
* Unlimited Mythic sandbox.
* Autonomous observation.
* Controlled experiments.
* Generated islands.
* Handcrafted scenarios.
* Optional objectives.
* Replay and sandbox timeline branching.

# PART SIX — DATA AND ANALYTICS

## 33. Preserve the analytical architecture
Retain the existing SQLite warehouse, CSV and Parquet exports.
The authoritative simulation remains pure deterministic C#.
Database recording must not influence authoritative outcomes.
Preserve explicit fact-table grains, persistent identities, deterministic event identifiers, historical records and half-open effective-date boundaries.
Retain configurable recording levels.
The warehouse must support analysing individual histories, households, enterprises, institutions, policies and divine interventions.

## 34. Extend analytical recording
Plan tables and events covering:

* Divine identities.
* Divine power balances.
* Divine actions.
* Mythic expenditure.
* Commands issued.
* Commands perceived.
* Command responses.
* Compulsion.
* Possession periods.
* Avatar manifestation.
* Miracles.
* Resource creation and destruction.
* Resurrection.
* Commandments.
* Commandment revisions.
* Known commandments.
* Religious affiliation history.
* Belief changes.
* Prayers.
* Worship.
* Organisations.
* Ownership history.
* Membership.
* Employment.
* Institutional expenditure.
* Service requests.
* Service delivery.
* Service outcomes.
* Education.
* Healthcare.
* Lending.
* Repayment and default.
* Crime.
* Investigation.
* Court decisions.
* Detention.
* Military service.
* Production and economic relationships.

Use existing event and warehouse conventions rather than inventing an incompatible parallel system.

## 35. KPIs and reporting
Develop a catalogue of population, economic, institutional and divine KPIs.
Include:

* Population.
* Births.
* Deaths.
* Fertility.
* Childhood mortality.
* Life expectancy.
* Health.
* Food security.
* Productivity.
* Time allocation.
* Unpaid care work.
* Employment.
* Wealth.
* Ownership concentration.
* Inequality.
* Social mobility.
* Institutional capacity.
* Unmet service demand.
* Healthcare outcomes.
* Education outcomes.
* Loan defaults.
* Crime.
* Justice outcomes.
* Military readiness.
* Religious affiliation.
* Trust and fear.
* Known commandments.
* Compliance.
* Divine interventions.
* Mythic expenditure.

Every KPI should have a definition, reporting period, appropriate denominator, data source and limitations.
Do not introduce a universal composite wellbeing score.

## 36. Information separation
Preserve the distinction between:

* True state.
* Observations.
* Communicated information.
* Institutional records.
* Published information.
* Individual beliefs.
* Faction knowledge.
* Divine observation.
* External analytical recording.

Government reports may be incomplete or incorrect.
The player's divine observation facility may provide additional information according to configured rules.
Institutional managers and AI policies must not secretly access the external analytical warehouse or omniscient debug information.
Plan for read-only SQL analysis as an advanced player or developer capability.

# PART SEVEN — TOMORROW'S DEVELOPMENT ORGANISATION

## 37. Establish the current implementation baseline
Before writing the implementation plan, inspect and document:

* Current branch.
* Current commit.
* Current worktrees.
* Outstanding agents or work packages.
* Current build status.
* Actual test results available.
* Implemented milestones.
* Partially accepted milestones.
* Failed acceptance criteria.
* Deferred requirements.
* Existing technical debt.
* Shared architecture.
* Relevant existing types and interfaces.

Inspect the actual source rather than relying exclusively on documents.
Identify precisely which planned capabilities can reuse existing implementations.
Do not run unnecessary destructive operations or modify stable code tonight.

## 38. Shared architectural contracts
Identify the common contracts that need to be settled before parallel implementation begins.
In particular:

* Persistent NPC identity.
* God identity.
* Faction identity.
* Organisation identity.
* Physical locations.
* Object affordances.
* Action primitives.
* Ownership and possession.
* Inventories.
* Resource conservation.
* Command validation.
* Temporary control authority.
* Possession.
* Avatar state.
* Divine power.
* Knowledge views.
* Beliefs.
* Commandments.
* Institutional service requests.
* Institutional outcomes.
* Domain events.
* Save state.
* Replay.
* Analytical recording.

Specify:

* Which module owns each contract.
* Which packages may modify it.
* Which packages consume it.
* Expected invariants.
* Compatibility requirements.
* Required tests.

Avoid multiple agents independently modifying fundamental shared enums, state structures or save schemas.

## 39. Parallel work packages
Produce an ordered plan with approximately 15–20 substantial packages.
The exact number should be determined by the actual architecture and dependencies.
Potential packages include:

Foundation packages
- F0 — Current integration baseline and outstanding acceptance tests.
- F1 — Shared ontology, contracts and event definitions.
- F2 — Demographic viability and ecological experiments.

Civilisation packages
- C1 — Physical economy, labour and resource ownership.
- C2 — Obligations, promises, lending and reputation.
- C3 — Employment, enterprises and cooperatives.
- C4 — Persistent military individuals and recruitment.
- C5 — Citizenship, migration and political interests.
- C6 — Generic organisational and institutional framework.
- C7 — Healthcare and education.
- C8 — Banking, communal stores and welfare.
- C9 — Justice, police and detention foundations.
- C10 — Cultural transmission, government and religious institutions.

Divinity packages
- D1 — Persistent gods, Mythic and deterministic divine command interface.
- D2 — Suggestions, commands and temporary compulsion.
- D3 — Physical miracles, healing, resource creation and smiting.
- D4 — Possession and temporary player control.
- D5 — Divine avatars and world interaction.
- D6 — Commandments, beliefs and religious affiliation.
- D7 — Resurrection and historical NPC restoration.

Player experience packages
- U1 — Camera, selection and player interaction.
- U2 — Divine toolbar, contextual actions and Mythic display.
- U3 — Seamless terrain presentation.
- U4 — NPC animations, gestures and carried objects.
- U5 — Institutional presentation and reports.
- U6 — Tutorial, scenarios and gameplay integration.

Data and assurance packages
- Q1 — Warehouse extensions and KPI catalogue.
- Q2 — Headless experiments and performance benchmarks.
- Q3 — Integration testing, replay, save/load and conservation.
- Q4 — Final Windows build, playable checkpoints and handover.

These are suggested package boundaries, not mandatory names.
Reorganise them to fit the actual implementation.

## 40. Package specification
For every work package, document:

* Package identifier.
* Objective.
* User-facing outcome.
* Relevant design documents.
* Relevant existing source files.
* Allowed implementation scope.
* Explicitly excluded scope.
* Dependencies.
* Shared contracts.
* Files and modules owned by the package.
* Required domain events.
* Save/load implications.
* Warehouse implications.
* Acceptance tests.
* Headless demonstration.
* Unity demonstration where appropriate.
* Performance considerations.
* Integration procedure.
* Risks.
* Definition of done.
* Possible smaller checkpoints.

Specify the recommended agent type.
Use Opus for difficult architecture and simulation packages.
Use Sonnet for appropriately bounded implementation, testing, reporting and presentation packages.
Do not use Fable.

## 41. Dependency graph and development waves
Produce an actual dependency graph, not merely a list of agents.
Organise the 18-hour run into waves.

- Wave 0 — Baseline and contracts. Confirm the starting implementation, resolve architectural conflicts and establish shared contracts.
- Wave 1 — Independent foundations. Launch genuinely independent packages as soon as their prerequisites are complete. Likely candidates include demographics, divine foundations, bounded visual work, analytics and appropriate economic work.
- Wave 2 — Functional systems. Implement core economic systems, divine interventions, physical interaction, ownership and related capabilities.
- Wave 3 — Institutions and advanced divine interaction. Implement organisations, services, commandments, possession, avatars and appropriate integrations.
- Wave 4 — Integrated gameplay. Connect the simulation to player controls, animation, institutional reporting and end-to-end scenarios.
- Wave 5 — Release assurance. Complete regression tests, headless experiments, Unity tests, Windows build, documentation and handover.

Treat these as dependency-driven waves rather than rigid chronological barriers.
Independent work should continue while other packages are being integrated.
Explicitly identify which packages can operate simultaneously.
Separate worktrees do not by themselves make shared architectural changes safe.

## 42. Time-budget strategy
The available development window tomorrow is approximately 18 hours.
Prepare an ambitious but realistic prioritisation strategy.
Do not promise that every feature can be completed within that window.
Use three delivery tiers.

Tier 1 — Must prioritise
* Stable integrated population.
* Meaningful demographic viability experiments.
* Existing learning and communication retained.
* Physical resource and labour economy.
* Persistent god identities.
* Mythic.
* Divine suggestions and commands.
* Temporary compulsion.
* Healing, smiting and resource creation.
* Structured commandments.
* Functional player controls.
* Exact deterministic replay.
* A working Windows build.

Tier 2 — Strongly desired
* Possession.
* Manifested avatar.
* Resurrection.
* Employment and enterprises.
* Generic institutions.
* Functional healthcare.
* Functional education.
* Primitive lending.
* Communal assistance.
* Basic religious beliefs.
* Institutional and divine reporting.
* Improved terrain presentation.
* Meaningful gesture animations.

Tier 3 — Stretch goals
* More sophisticated justice.
* Policing and detention.
* Expanded military service.
* Banking institutions.
* Autonomous political organisations.
* Advanced religious institutions.
* Richer cultural transmission.
* More advanced beliefs and conversion.
* Additional miracles.
* Deeper institutional analytics.
* Tutorial and presentation polish.

Retain all Tier 3 requirements in the long-term design, even if they cannot be implemented tomorrow.
Reassess tier placement after inspecting the actual code.
Prioritise complete playable vertical slices over incomplete implementations of many unrelated features.

## 43. Main-session responsibilities
Tomorrow's primary Claude session acts as architect, project lead and integration lead.
It should:

* Maintain the dependency graph.
* Allocate packages.
* Establish common contracts.
* Launch and monitor agents.
* Resolve integration conflicts.
* Review agent claims.
* Independently verify critical behaviour.
* Merge tested packages.
* Keep a working main development branch.
* Launch newly unblocked packages.
* Maintain a persistent development log.
* Protect the final integration and testing window.

Do not allow the main session to become unnecessarily occupied implementing individual packages that can safely be delegated.
Use separate worktrees for independent code packages.
Limit simultaneous agents according to actual CPU, memory, Unity and repository constraints.
Preserve existing restrictions concerning Unity editors, generated files, assets and permissions.

## 44. Playable checkpoints
Define multiple playable releases throughout the run.
Suggested checkpoints:

- Checkpoint A — Living world. Integrated M3–M5, populations, learning, families, gestures and barter.
- Checkpoint B — Divine sandbox. The player can issue commands, spend Mythic, heal, smite and create resources.
- Checkpoint C — Working economy. Persistent NPCs work, trade, own goods and operate productive assets.
- Checkpoint D — Personal divine interaction. Possession and avatar manifestation provide physical interaction with the world.
- Checkpoint E — Institutional society. At least several simple institutions deliver actual services through NPC labour and resources.
- Checkpoint F — Integrated civilisation and divinity. The player can influence autonomous civilisation development through divine actions, observe outcomes and inspect meaningful reports.

Each completed checkpoint should be independently demonstrable.
A later failure must not invalidate the last working playable build.

# PART EIGHT — TESTING AND ACCEPTANCE

## 45. Preserve existing quality gates
Retain all existing acceptance tests, including outstanding and partially accepted Stage 4 requirements.
Do not silently remove or weaken them.
Distinguish:

* Passed tests.
* Failed tests.
* Revised numerical expectations.
* Deferred requirements.
* Unimplemented capabilities.
* Demonstrated behaviours.
* Known limitations.

Numerical targets may be revised with evidence, but underlying required capabilities must not disappear.

## 46. Deterministic simulation requirements
Require:

* Pure C# authoritative simulation.
* No dependency on rendering.
* No wall-clock-dependent authoritative behaviour.
* Stable entity processing order.
* Explicit random streams.
* Deterministic command processing.
* Save/load continuation parity.
* Replay parity.
* Headless and Unity parity.
* Independent parallel simulations.
* Logging independence.
* Versioned save formats.
* Resource conservation.
* Population conservation.
* Ownership integrity.
* Bounded learning and social memory.

Divine commands, possession inputs, avatar inputs and miracles must be represented in deterministic replayable command streams.
Continuous player movement must be translated into appropriate deterministic simulation inputs.

## 47. Required end-to-end scenarios
Prepare acceptance scenarios demonstrating the actual functionality.

1. Autonomous population: individuals survive, learn, reproduce, age and establish households without divine intervention.
2. Trade: two individuals communicate, agree an exchange and physically transfer actual goods.
3. Employment: an individual operates a productive asset and compensates another individual for work.
4. Independent institution: individuals recognise a recurring need and establish an organisation without the player directly creating it.
5. Healthcare: an injured individual requests help, receives treatment from another NPC and experiences improved health.
6. Education: an individual teaches another, resulting in measurable knowledge or capability changes.
7. Lending: actual resources are lent, used and subsequently repaid or defaulted on.
8. Divine command: the player issues an instruction. The NPC perceives it and independently responds.
9. Compulsion: the player spends Mythic to temporarily compel a valid physical action.
10. Possession: the player takes control of an NPC, performs actual actions and returns control to the same individual.
11. Avatar: the player manifests, walks through the settlement, physically interacts with objects and returns to divine observation.
12. Miracle: a player heals or smites an individual, with genuine state changes and observable consequences.
13. Commandment: a persistent commandment is revealed, learned, communicated and influences behaviour without universally enforcing it.
14. Resurrection: a deceased NPC is restored with the same identity and memories, while previously distributed property remains correctly owned.
15. Government autonomy: a government or institution independently refuses or modifies a divine instruction.
16. Religious and political separation: an individual's religious affiliation changes without automatically changing citizenship.
17. Analytical reconciliation: warehouse records reconcile to the actual simulation events.
18. Replay: a complete scenario containing ordinary NPC actions and divine interventions reproduces exactly from saved inputs.

Prioritise the scenarios according to the delivery tiers.
Do not claim a scenario has been demonstrated merely because supporting classes exist.

## 48. Performance
Plan benchmarks for:

* Simulation ticks per second.
* NPC decisions per second.
* Simulated years per real minute.
* Memory consumption per individual.
* Population scalability.
* Learning overhead.
* Social interaction overhead.
* Institutional processing overhead.
* Pathfinding.
* Save size.
* Database recording overhead.
* Parallel headless throughput.

Benchmark appropriate population sizes, including 100, 500, 1,000 and larger populations where feasible.
Avoid camera-dependent authoritative simulation.
Rendering level of detail may vary, but the existence and behaviour of individuals must not depend on whether the player is looking at them.
Do not begin neural network training during tomorrow's run.
Retain readiness for future training.

# PART NINE — DOCUMENTATION TO PREPARE TONIGHT

## 49. Documentation updates
Inspect the existing documentation structure before creating new files.
Prefer updating existing owning documents rather than creating contradictory duplicates.
Prepare or update appropriate documents covering: DESIGN_CONVENTIONS.md, WORLD_ONTOLOGY.md, VISION_AND_PHILOSOPHY.md, UNITY_POC_PLAN.md, POPULATION_DESIGN.md, NPC_LEARNING_DESIGN.md, ECONOMY_DESIGN.md, POLITICAL_ECONOMY_DESIGN.md, INSTITUTIONAL_STATE_DESIGN.md, STATE_DESIGN.md, MILITARY_DESIGN.md, COMMUNICATION_AND_COGNITION_DESIGN.md, CULTURAL_TRANSMISSION_DESIGN.md, INFORMATION_AND_MEDIA_DESIGN.md, DATA_WAREHOUSE_DESIGN.md, KPI_CATALOGUE.md, HEADLESS_SIMULATION.md, AI_TRAINING_DESIGN.md, ACCEPTANCE_TESTS.md.
Add new owning documents where appropriate: DIVINE_PLAYER_DESIGN.md, MYTHIC_AND_MIRACLES_DESIGN.md, POSSESSION_AND_AVATAR_DESIGN.md, COMMANDMENTS_AND_RELIGION_DESIGN.md, RESURRECTION_DESIGN.md, ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md, INSTITUTIONAL_SERVICES_DESIGN.md, PLAYER_UI_DESIGN.md, CAMERA_AND_CONTROLS_DESIGN.md, VISUAL_WORLD_DESIGN.md, NPC_ANIMATION_DESIGN.md, GAMEPLAY_SCENARIOS.md.
You may consolidate these documents if that produces a clearer architecture.
Avoid unnecessary duplication.

## 50. Tomorrow's execution documents
Produce the following dedicated documents, or clearly equivalent files.

- CIVILISATION_AND_DIVINITY_MASTER_PLAN.md: the complete integrated vision, resolved decisions, scope and delivery priorities.
- CURRENT_IMPLEMENTATION_BASELINE.md: the actual current code, milestones, tests, known limitations and outstanding requirements.
- SHARED_ARCHITECTURAL_CONTRACTS.md: the shared types, modules, interfaces, events, responsibilities and invariants required for parallel development.
- DEVELOPMENT_PACKAGE_INDEX.md: all work packages, owners, prerequisites, priorities and status.
- DEVELOPMENT_PACKAGES/: one detailed specification per package, written so an independent subagent can implement it without having to reconstruct the entire conversation.
- DEVELOPMENT_DEPENDENCY_GRAPH.md: a dependency graph, parallelisation plan, integration order and critical path.
- RELEASE_ACCEPTANCE_CHECKLIST.md: explicit acceptance scenarios and release gates.
- CIVILISATION_AND_DIVINITY_RUNBOOK.md: operational instructions for the primary Claude session during the 18-hour run. Include agent allocation, worktree management, merge strategy, test cadence, checkpoints, escalation rules and final release procedure.
- NEXT_SESSION_PROMPT.md: a complete copy-ready prompt that I can give Claude tomorrow morning. It should direct Claude to read the relevant documents and begin the actual development run. It must not merely request more planning.
- DEV_LOG.md: update the existing development log with tonight's decisions and documentation changes.

## 51. Owner decision register
Create or update a single authoritative register of owner decisions.
In particular, record the following as resolved:

* Player is a god only.
* Governments are autonomous NPC organisations.
* Every initial faction has an associated god.
* Gods persist independently of factions.
* A god cannot permanently lose.
* Ordinary NPC autonomy remains fundamental.
* Temporary compulsion is permitted.
* Possession is permitted.
* Physical divine avatars are permitted.
* Possession and compulsion consume Mythic.
* Physical miracles are permitted.
* Resurrection is permitted.
* Resurrection does not reverse inheritance.
* Avatar destruction does not destroy the god.
* Religious affiliation and citizenship are distinct.
* Institutions can emerge independently.
* Institutions support multiple ownership models.
* The Catan grid is hidden during normal gameplay.
* Fine movement nodes are developer-visible only.
* The island appears seamless and natural.
* Stylised low-poly characters.
* Hybrid diegetic and analytical interface.
* Desktop-first with mobile-compatible architecture.
* Non-verbal NPC communication.
* Optional accessibility and developer decoding.
* Configurable divine power.
* Unlimited-power sandbox.
* Separate divine and institutional knowledge.
* Autonomous civilian, military, institutional and faction policies.
* Future AI god policies.
* No neural network training during this run.
* Sandbox-first progression.
* No predetermined political or economic outcomes.

These decisions supersede contradictory older documents.
Resolve existing contradictions explicitly, particularly where older designs assume direct player control of the government.

## 52. Decisions that can be delegated
Do not generate another enormous owner questionnaire.
For minor, reversible implementation decisions:

* Select sensible defaults.
* Make parameters configurable.
* Document the rationale.
* Continue.

Identify only genuinely consequential unresolved questions that cannot reasonably be answered from the agreed design.
Do not unnecessarily block tomorrow's work on balancing values, animation polish or other adjustable parameters.

# PART TEN — CONCLUSION OF TONIGHT'S SESSION

## 53. Final deliverables
Tonight's session should conclude with:

1. An accurate current implementation baseline.
2. Updated, internally consistent design documentation.
3. A resolved owner decision register.
4. A shared architectural contract.
5. A prioritised dependency graph.
6. Detailed independently executable subagent work packages.
7. An 18-hour development runbook.
8. Explicit playable checkpoints.
9. Acceptance criteria and integration scenarios.
10. A complete ready-to-use Claude prompt for tomorrow morning.

Review the documents for contradictions and missing dependencies before finishing.
Do not alter the existing working gameplay code except for documentation-related necessities explicitly justified in the development log.
Commit the documentation changes to the appropriate development branch.
Do not modify main.
Follow the existing repository permissions and push rules.
Report exactly what has been inspected, created, updated and committed.
Identify any remaining blockers.
Tomorrow's session should be able to begin implementation immediately, with minimal owner intervention.
The objective is to maximise meaningful, tested, integrated development progress during the available 18-hour window.
Please begin preparing the documentation now.
