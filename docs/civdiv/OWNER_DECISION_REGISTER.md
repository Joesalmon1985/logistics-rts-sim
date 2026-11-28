# Owner decision register

The single authoritative list of the owner's decisions for the God of the Island project. Every design and planning document defers to it. When two documents disagree, the later decision set here wins; the owning document is then corrected or carries a "superseded" notice that points here.

**How to use it**
- A decision has an id (`D5.7`) and links to where it is worked out. Cite the id in design docs, package specs and commit messages.
- Implementation choices that are minor and reversible are **not** owner decisions. Agents take a sensible, configurable default and record it in [DEV_LOG.md](../stage4/DEV_LOG.md) under "Design decisions taken during implementation" (owner instruction, brief §52).
- New owner decisions are appended as a new set (D6, D7, …). Never edit an old row's meaning; mark it superseded and add the new one.

## Decision sets

| Set | Date | Given | Where recorded in full | Status |
|---|---|---|---|---|
| Q1–Q23 and §14 requirements | 26 Nov 2026 (labelled "27 Nov" in the docs) | Answers to the Stage 4 design questions | [DESIGN_CONVENTIONS.md §2, §13, §14](../stage4/DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026) | Binding, except where D5 supersedes (below) |
| D2 | 26 Nov 2026 | After checkpoint 4A: L2 investigation; behavioural M3 evidence; M4/M5 in parallel behind contracts; I-BS gate; recording per milestone | [DEV_LOG.md "Owner directions D2"](../stage4/DEV_LOG.md#owner-directions-d2-26-nov-2026) | Binding |
| D3 | 26 Nov 2026 | After M3b: M3 accepted as functional; survival; LS3/LS4; criteria amendments; Hawk/Bully first-blow variant; F0; spatial ontology; M4; M5; I-BS extended | [DEV_LOG.md "Owner decisions D3"](../stage4/DEV_LOG.md#owner-decisions-d3-26-nov-2026-after-m3b) | Binding |
| D4 | 26 Nov 2026 | After M4: population viability experiments; relationship ontology; fertility; mixed-age founders; ecology; care; M4 partially accepted; M5 presentation; I-BS; demographic reports | [DEV_LOG.md "Owner decisions D4"](../stage4/DEV_LOG.md#owner-decisions-d4-26-nov-2026-after-the-m4-report) | Binding |
| **D5** | **26 Nov 2026** | **Civilisation and Divinity: the player is a god; preparation for the 18-hour run** | [OWNER_BRIEF_2026-11-26.md](OWNER_BRIEF_2026-11-26.md) (verbatim) and below | **Binding; supersedes contradicting earlier text** |

## D5: Civilisation and Divinity (26 Nov 2026)

The decisions in brief §51, in its order. The design is worked out in the documents in the last column.

| Id | Decision | Worked out in |
|---|---|---|
| D5.1 | **The player is a god only.** The player never directly controls a state, government, army or institution. | [DIVINE_PLAYER_DESIGN.md](../stage4/DIVINE_PLAYER_DESIGN.md) |
| D5.2 | **Governments are autonomous NPC organisations** with their own decision-making; they may reject divine instructions. Religious, political, economic and institutional authority stay distinct. | [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](../stage4/ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md), [STATE_DESIGN.md](../stage4/STATE_DESIGN.md) |
| D5.3 | **Every initial faction has an associated god.** Rival gods are controlled by independently replaceable AI policies; sandbox may allow switching gods. | DIVINE_PLAYER_DESIGN |
| D5.4 | **Gods persist independently** of faction, state, government, religion, population and territory. | DIVINE_PLAYER_DESIGN, [SHARED_ARCHITECTURAL_CONTRACTS.md](SHARED_ARCHITECTURAL_CONTRACTS.md) |
| D5.5 | **A god cannot permanently lose** or cease to exist. Recovery is through migration, conversion, resurrection or other legitimate interventions; extinct populations are never regenerated automatically. | DIVINE_PLAYER_DESIGN |
| D5.6 | **Ordinary NPC autonomy remains fundamental.** Every NPC is a persistent autonomous individual (brief §3); the civilisation must develop without any divine intervention; no faction-level or institutional abstraction secretly does the meaningful work. | [VISION_AND_PHILOSOPHY.md](../stage4/VISION_AND_PHILOSOPHY.md), DESIGN_CONVENTIONS |
| D5.7 | **Temporary compulsion is permitted.** It may override priorities, including survival, but never physical constraints; it expires on completion, timeout, cancellation or death. | DIVINE_PLAYER_DESIGN |
| D5.8 | **Possession is permitted.** One NPC at a time at first; the body, identity, memory and learning continue; actions are recorded as divinely controlled. | [POSSESSION_AND_AVATAR_DESIGN.md](../stage4/POSSESSION_AND_AVATAR_DESIGN.md) |
| D5.9 | **Physical divine avatars are permitted.** Distinct divine identity; existing movement and interaction; no hunger or ageing; can be attacked. | POSSESSION_AND_AVATAR_DESIGN |
| D5.10 | **Possession and compulsion consume Mythic** (possession continuously). | [MYTHIC_AND_MIRACLES_DESIGN.md](../stage4/MYTHIC_AND_MIRACLES_DESIGN.md) |
| D5.11 | **Physical miracles are permitted.** They are validated deterministic commands that genuinely change authoritative state, with ledger entries for created or destroyed goods. | MYTHIC_AND_MIRACLES_DESIGN |
| D5.12 | **Resurrection is permitted** and restores the same persistent individual (genealogy, memories, learned associations, relationship history). | [RESURRECTION_DESIGN.md](../stage4/RESURRECTION_DESIGN.md) |
| D5.13 | **Resurrection does not reverse inheritance.** Estates stay with their owners; partnerships, employment and households are not restored automatically; the death stays recorded. | RESURRECTION_DESIGN |
| D5.14 | **Avatar destruction does not destroy the god;** the god returns to divine existence. | POSSESSION_AND_AVATAR_DESIGN |
| D5.15 | **Religious affiliation and citizenship are distinct** (and so are belief, faith, trust, fear, loyalty and cultural membership). | [COMMANDMENTS_AND_RELIGION_DESIGN.md](../stage4/COMMANDMENTS_AND_RELIGION_DESIGN.md) |
| D5.16 | **Institutions can emerge independently** of government, from actual needs, knowledge, resources and cooperation. A threshold may make an opportunity available; it never builds a staffed institution. | ORGANISATIONS_AND_INSTITUTIONS_DESIGN |
| D5.17 | **Institutions support multiple ownership models:** state, private, communal, cooperative (and religious organisations on the same framework). | ORGANISATIONS_AND_INSTITUTIONS_DESIGN |
| D5.18 | **The Catan grid is hidden during normal gameplay.** | [VISUAL_WORLD_DESIGN.md](../stage4/VISUAL_WORLD_DESIGN.md) |
| D5.19 | **Fine movement nodes are developer-visible only.** | VISUAL_WORLD_DESIGN |
| D5.20 | **The island appears seamless and natural;** rendered geography stays consistent with accessibility and resources. | VISUAL_WORLD_DESIGN |
| D5.21 | **Stylised low-poly characters** that show age, development, equipment, activity, pregnancy, injury, illness, fatigue, emotion, carried goods and divine effects. | VISUAL_WORLD_DESIGN, [NPC_ANIMATION_DESIGN.md](../stage4/NPC_ANIMATION_DESIGN.md) |
| D5.22 | **Hybrid diegetic and analytical interface:** world interaction first; panels for analysis, history and doctrine. | [PLAYER_UI_DESIGN.md](../stage4/PLAYER_UI_DESIGN.md) |
| D5.23 | **Desktop-first with a mobile-compatible architecture.** | [CAMERA_AND_CONTROLS_DESIGN.md](../stage4/CAMERA_AND_CONTROLS_DESIGN.md) |
| D5.24 | **Non-verbal NPC communication** in normal play (gestures, pointing, posture, objects). | NPC_ANIMATION_DESIGN, [COMMUNICATION_AND_COGNITION_DESIGN.md](../stage4/COMMUNICATION_AND_COGNITION_DESIGN.md) |
| D5.25 | **Optional accessibility and developer decoding** of messages (the decoded text overlay, off by default). | PLAYER_UI_DESIGN, NPC_ANIMATION_DESIGN |
| D5.26 | **Configurable divine power:** Mythic maximum, fixed regeneration at first, action costs, upkeep, cooldowns; human and AI gods under the same rules. | MYTHIC_AND_MIRACLES_DESIGN |
| D5.27 | **An unlimited-power sandbox** (and a disabled-intervention mode). | MYTHIC_AND_MIRACLES_DESIGN, [GAMEPLAY_SCENARIOS.md](../stage4/GAMEPLAY_SCENARIOS.md) |
| D5.28 | **Divine and institutional knowledge are separate.** Divine observation follows configured rules; government records may be incomplete or wrong; no institution or AI policy reads the warehouse or debug truth. | [INFORMATION_AND_MEDIA_DESIGN.md](../stage4/INFORMATION_AND_MEDIA_DESIGN.md) |
| D5.29 | **Autonomous civilian, military, institutional and faction policies.** The human never fills the faction-policy seat in the god-only game. | DESIGN_CONVENTIONS §7, ORGANISATIONS_AND_INSTITUTIONS_DESIGN |
| D5.30 | **Future AI god policies** behind a replaceable seam, under the same rules as the human god. | DIVINE_PLAYER_DESIGN, [AI_TRAINING_DESIGN.md](../stage4/AI_TRAINING_DESIGN.md) |
| D5.31 | **No neural network training during this run;** keep readiness. | AI_TRAINING_DESIGN |
| D5.32 | **Sandbox-first progression;** optional objectives including the existing victory-point mode. | GAMEPLAY_SCENARIOS |
| D5.33 | **No predetermined political or economic outcomes;** the simulation is never forced to validate a theory. | VISION_AND_PHILOSOPHY |

Further D5 requirements that are binding but not in the §51 list (each is worked out in the owning documents): the four interaction modes A–D on one authoritative world (brief §6); the six distinct intervention types (§8); the divine action vocabulary reusing existing primitives (§9); the commandment system without run-time free-text interpretation (§12); divine learning through the NPC's own attribution (§14); the healthcare, education, finance, justice and military principles (§§19–23); seamless presentation and camera modes (§§25–26); analytics and KPI extensions (§§33–36); deterministic replay of all divine inputs (§46); the 18 end-to-end scenarios (§47); performance benchmarks (§48).

## D6: Industrialisation, OpenRA and Power Grid (27 Nov 2026)

From [OWNER_BRIEF_2026-11-27_INDUSTRY.md](OWNER_BRIEF_2026-11-27_INDUSTRY.md) (verbatim). Worked out in the D6 design documents named below and in [SHARED_ARCHITECTURAL_CONTRACTS.md](SHARED_ARCHITECTURAL_CONTRACTS.md) "Industry addendum".

| Id | Decision |
|---|---|
| D6.1 | **A third workstream, Industrialisation,** inside the same persistent NPC, biological, social, economic and institutional simulation. No competing architecture: it shares the ownership, inventory, action, population, institutional and event contracts. |
| D6.2 | **One monetary and accounting architecture.** No OpenRA credits, no Power Grid money. Delivering ore, refining or generating electricity never creates money; revenue comes only from actual payments. Money is issued or destroyed only by explicitly defined monetary mechanisms; lending accounts for claims and liabilities. **The external ore buyer is removed** from the normal design. Production creates goods and services, not money. |
| D6.3 | **New physical resources:** iron ore, copper, crude oil, refined fuels, industrial metals, uranium as abstract nuclear material, waste, scrap and recyclable materials; existing chains and coal are kept. Finite geological deposits, mines, manual and mechanised extraction, refineries, industrial chains. |
| D6.4 | **Vehicles are persistent physical assets operated by actual NPCs,** with ownership, location, condition, cargo capacity, operators and operating requirements. Harvesters travel automatically between sources and processing facilities in the OpenRA style, but are driven by NPC operators. |
| D6.5 | **Factories** have production orders, finite capacity, material inputs, workers, electricity requirements and completed physical products. |
| D6.6 | **No central fuel market.** Fuel is allocated through the existing mechanisms: purchase, owned extraction, supply contracts, internal transfers, communal distribution, government provision. Markets are one mechanism, not a requirement. |
| D6.7 | **Electricity:** generating technologies (coal, oil, hybrid, waste, nuclear, renewable) differ in construction cost, fuel, efficiency, capacity and operating cost. Fuel is physically delivered and consumed. Electricity flows only through connected networks (an abstract deterministic network model first), with demand tied to actual activities and equipment, allocation priorities, shortages and outages with production consequences. Revenue only from actual payments or agreed funding. Private, public, cooperative and communal provision. |
| D6.8 | **Military production on the same economy.** OpenRA-inspired categories (infantry, engineers, reconnaissance, armoured transports, tanks, artillery, aircraft, naval, defences), prioritising infantry, transports, tanks and simple defences. Infantry are recruited existing NPCs; vehicles are manufactured assets needing crew, fuel, ammunition and maintenance. Military and civilian organisations compete for the same labour, materials, fuel, capital and electricity. The god acts through the existing divine rules (instructions, compulsion, possession of operators). |
| D6.9 | **Knowledge ontology:** individual knowledge, formal designs, physical knowledge artefacts and their ownership, patents, licences, trade secrets and practical ability are distinct. Knowledge copies without loss to the holder. Designs are data-driven definitions of productive capability and construction requirements. |
| D6.10 | **Research and invention** consume real time, labour and resources; a discovery may be formalised into a design; a design never builds an asset by itself. Divine revelation of knowledge confers no legal ownership or patent. |
| D6.11 | **Patents and licences are legal claims,** recognised per government, registered, transferred, auctioned, licensed, challenged, revoked or expired; copying stays physically possible when prohibited; enforcement through courts and evidence. Open knowledge, public ownership, cooperative research, secrecy and compulsory licensing are supported; no universal patent system is assumed. |
| D6.12 | **Auctions** (reinterpreting Power Grid) for designs, patents, exclusive and non-exclusive licences and existing assets, using the one auction and monetary system. Winning a design never produces a power station. |
| D6.13 | **Waste and circular production:** waste arises from consumption, production and destruction, is accounted in the ledgers, and can be collected, transported, stored, disposed of, recycled or burned for electricity; environmental damage may follow. |
| D6.14 | **Nuclear material is abstract and fictional;** civilian nuclear power and OpenRA-style superweapons are fictional game capabilities with real political, economic, environmental and demographic consequences. No real-world weapon engineering is simulated. |
| D6.15 | **New organisation types** on the generic framework: universities, research organisations, patent offices, commercial courts, electricity utilities, transport companies, waste authorities, recycling enterprises, environmental regulators. The god influences them only through divine mechanics. |
| D6.16 | **The ontology distinguishes** matter, energy, money, financial claims, knowledge, knowledge artefacts, legal rights, productive assets and organisations, each with warehouse facts, histories and reconciliation rules. |
| D6.17 | **An industrial starting scenario** (established population, knowledge, mines, vehicles, organisations, grid) on the same simulation as Living Island; industrial tests never wait for prehistoric development. Autonomous technological progression stays the long-term goal. |
| D6.18 | **Priority:** the industrial vertical slice (NPC-driven harvester → ore → refinery → recorded sale → fuel to a power station → power to a factory → a vehicle from real inputs → an NPC operates it → disruption matters → warehouse and replay) comes after, and must not compromise, the population, civilisation, institutional and divine objectives. Patent litigation, advanced research, energy markets and nuclear warfare are later extensions. |
| D6.19 | **Presentation:** visible deposits and mines, moving harvesters with visible cargo, industrial buildings and animations, vehicle inspection, production queues, electricity and logistics overlays, shortage indicators, inspection of power stations and industrial organisations, all within the seamless island. |

## What D5 supersedes

These earlier statements no longer hold. The owning documents carry a notice pointing here.

| Earlier statement | Where | Replaced by |
|---|---|---|
| "The player operates the institutions of a state through policies, taxes, investment and incentives." | DESIGN_CONVENTIONS §2 "The state and its institutions"; INSTITUTIONAL_STATE_DESIGN; STATE_DESIGN; POLITICAL_ECONOMY_DESIGN | D5.1, D5.2: the state is an NPC organisation; the god influences its members (suggestions, commands, compulsion, possession, commandments, miracles), and they may refuse. |
| "Construction: the state designates building, road and HQ sites and commits treasury resources." (with the player as the state) | DESIGN_CONVENTIONS §2 "Economy" | The state organisation's NPC decision-makers designate sites; the god may suggest, command or compel. NPC labour still does the building. |
| `IFactionPolicy` decides "a faction's state (AI or human)". | DESIGN_CONVENTIONS §7 | D5.29: the faction policy is always autonomous in the god-only game; the human acts through the god seam (`IGodPolicy` / god commands, see SHARED_ARCHITECTURAL_CONTRACTS). The Stage 3 mode keeps the human faction seat as a temporary mechanism. |
| The player gets near-complete reports on "the faction's own affairs". | DESIGN_CONVENTIONS §2 "Information and data" | D5.28: the god's divine observation follows configured rules and is distinct from the faction's institutional records. |
| The player commands the army or sets military doctrine directly. | MILITARY_DESIGN | D5.1, brief §23: the god influences military leaders and soldiers through divine actions. |
| Message text above NPC heads as normal presentation. | M5 GestureLayer as built | D5.24, D5.25 (already in D4 §8): developer or accessibility overlay only. |

The Stage 3 game (player as a faction placing buildings, setting priorities and ordering units) stays playable as the existing **temporary mechanism** "Stage 3 mode" ([DEV_LOG Temporary mechanisms](../stage4/DEV_LOG.md#temporary-mechanisms)) until the owner decides to retire it (M14). The new god-only game is the default for population games.

## Genuinely open owner questions

None block tomorrow's run. Questions raised while planning are listed in [CIVILISATION_AND_DIVINITY_MASTER_PLAN.md](CIVILISATION_AND_DIVINITY_MASTER_PLAN.md) under "Open questions", each with the default the run will use until the owner answers.

## D7: Terrain and raw materials (owner, 27 Nov 2026, evening) — proposed, to confirm next session

The owner's intended basic economy. Not yet implemented; the next session reconciles it with the current code and earlier decisions (notably D4 §5 "meaningful carrying capacity and depletion" vs infinite forage) before any change.

| Hex (abstract, never visible as hexes) | Raw material (Catan-style roll; appears gradually, not at once) | Infinite forageable food |
|---|---|---|
| Forest | Wood | Mushrooms |
| Mountain | Stone | Roots |
| Wetland | Clay | Wild eggs |
| Pasture | Sheep | Rabbit |
| Field | Hay | Wild vegetables |
| Desert | nothing | Cactus fruit |
| Sea | Fish | Seaweed |

- D7.1 The world is 6 land hex types plus sea; the island is one continuous landscape with no visible hex boundaries (consistent with D5.18–D5.20).
- D7.2 Each hex type yields its raw material on a Catan-style production roll; the material comes into being gradually after a successful roll.
- D7.3 Every hex has an infinite supply of its forageable food, which can be eaten directly. **Open point:** "infinite" conflicts with D4 §5 (carrying capacity, depletion, commons); options to confirm: infinite total but rate-limited per place, or slowly regenerating.
- D7.4 Raw materials are not eaten; they are processed and combined into more complex goods to be used, consumed or traded.

## D8: Unification of the artificial world (owner, 28 Nov 2026)

From [../unification/OWNER_BRIEF_2026-11-28_UNIFICATION.md](../unification/OWNER_BRIEF_2026-11-28_UNIFICATION.md) (verbatim; Part B supersedes Part A). **D8 confirms D7** (terrain, raw materials, forage) and resolves its open point: forage is infinite, rate-limited per individual, with shared-location throughput configurable (off by default). The design is worked out in `docs/unification/`. D8 takes priority over further industrial and institutional expansion.

| Id | Decision |
|---|---|
| D8.1 | **One simulation.** Catan defines geography and resource opportunities; Widelands defines production recipes; Creatures-style cognition decides what people do. No faction-level AI plays the economy for the individuals; the NPC population is the sole source of ordinary economic decisions and productive labour. |
| D8.2 | **Seven abstract hex types** (Forest/Wood/Mushrooms, Mountain/Stone/Roots, Wetland/Clay/Wild eggs, Pastures/Sheep/Rabbit, Fields/Hay/Wild vegetables, Desert/none/Cactus fruit, Sea/Fish/Seaweed), presented as one continuous landscape. |
| D8.3 | **Forageable food** is always available in its environment, independent of dice, never permanently exhausted; collected by physically visiting, spending time, carrying or eating; no building or tool needed. Foods differ in nutrition, collection time, spoilage and accessibility (configurable). |
| D8.4 | **Raw materials** come from Catan-style rolls: a successful roll creates a production allowance that **materialises gradually** in the region; resources exist physically in the environment and never go straight into any inventory. |
| D8.5 | **Raw materials can only be collected with access to an operational collection building** of the right kind, which is also the designated deposit with finite storage. No collection when storage is full (unless capacity elsewhere is reserved). The building enables collection; an NPC must choose to do the work. |
| D8.6 | **Six collection building types** (woodcutter's, stonecutter's, clay gatherer's, shepherd's, hay gatherer's, fisher's shelter); none for desert. **Level 1 costs only labour and time**; levels 2 and 3 cost materials (illustrative: 10 wood; 20 wood + 10 stone) and improve storage, sleeping capacity, shelter quality, collection efficiency and worker capacity — never the natural resource supply. All values configurable. |
| D8.7 | **Every collection building is also a basic sleeping shelter** with finite capacity and quality; no separate primitive housing system. |
| D8.8 | **One building architecture with capabilities** (shelter, storage, collection, production, ownership, access, employment/worker capacity, condition, upgrade), reused for later residential, industrial, military and institutional buildings. Building ownership, collection rights, sleeping rights, storage access and ownership of collected goods stay distinct. Buildings can be built jointly, owned personally/jointly/communally/cooperatively, shared, abandoned, sold, inherited, upgraded while operating. |
| D8.9 | **Starting conditions:** six tribes (Amber, Crimson, Azure, Jade, Violet, Ivory), each 7 adults and 5 children (72 people), one founding settlement and one communal building each; deterministic, biologically consistent families, varied ages and personalities; no government, military, currency, employers, production chains, institutions or professions. A tribe is a social affiliation, not a state, employer, owner or collective intelligence. |
| D8.10 | **Settlements** occupy valid hex intersections with Catan's distance rule; they create a development area for buildings but own nothing automatically; a settlement is geographical and infrastructural, not a government. |
| D8.11 | **Roads** follow valid Catan edges between settlements (and towards a planned settlement site). No flags, no segment carriers, no handover points, no required road connection, no magical transfer: roads only make walking faster; NPCs may walk anywhere accessible. |
| D8.12 | **Production chains** (initial): wood→planks (carpenter), stone→building blocks (stonecutter), clay→pottery (potter), clay→bricks (kiln), sheep→wool (shearing), wool→cloth (weaver), hay→bedding, fish→prepared food (cooking), wood+stone→axe/hammer (toolmaker). Each needs materials, knowledge, labour and equipment. They extend later into industry; there is no separate industrial game. |
| D8.13 | **Bounded, interruptible multi-step goal planning** added alongside associative learning: an NPC derives intermediate objectives (materials, site, exchange, work) from its own knowledge, abandons them when hungry, in danger or a better option appears; no hard-coded faction strategies. Learning, skills, knowledge and capability stay distinct. |
| D8.14 | **No permanent professions;** specialisation, trade, employment and productive ownership arise from tools, knowledge, skills, assets, relationships and access. Barter and gifts first. |
| D8.15 | **Informal groups** are a fundamental capability: individuals propose a collective objective (building, road, settlement, hauling, shared assets, defence, care, trade), others decide to join; contributions are real materials, time or labour; refusal, withdrawal and free-riding are possible; groups may later become organisations, never automatically by threshold. |
| D8.16 | **Reconciliation:** keep Catan geography, dice (now gradual), settlement and road rules, the movement lattice, Widelands recipes, NPC cognition, families, learning, barter, employment, organisations and divine mechanics. Remove from normal gameplay: Widelands flags, segment carriers, anonymous builders and harvesters, faction stockpiles (replaced by owned physical inventories), faction economic AI and automatic faction construction. The Stage 3 game stays as an isolated regression scenario until replacements pass their tests. |
| D8.17 | **Defaults confirmed** from brief Part A §14 and Part B §7 (walk anywhere; buildings outside settlement areas only basic/temporary; settlements and tribes own nothing automatically; cross-tribe work allowed; rolls make resources available, not owned; basic survival and construction knowledge with individual variation; defined recipes first). Exception: no manual raw-material collection without a building (D8.5). |
| D8.18 | **Next milestone = Unification** (before industrial expansion). Its demonstration: leave the 72 people running with no divine intervention and find workshops, trade, roads, new settlements and communal organisations emerging, deterministic and reconcilable. Existing institutional, divine, industrial, energy, patent and monetary work is kept and rebuilt on the unified world. |

## D9: Embodied NPCs, emergent property, conflict and trained founders (owner, 28 Nov 2026)

Sources: [../unification/LATEST_OWNER_AMENDMENT.md](../unification/LATEST_OWNER_AMENDMENT.md) (controlling; highest precedence), brief Parts C and D in [../unification/OWNER_BRIEF_2026-11-28_UNIFICATION.md](../unification/OWNER_BRIEF_2026-11-28_UNIFICATION.md), [../unification/CLAUDE_UPDATE_PLAN_PROMPT.md](../unification/CLAUDE_UPDATE_PLAN_PROMPT.md), and the workbook [../unification/God_of_the_Island_Core_Design.xlsx](../unification/God_of_the_Island_Core_Design.xlsx). Direct owner decisions are D9.1–D9.9; the engineering recommendations in the amendment are binding planning defaults (D9.10–D9.14), not owner quotations. **D9 supersedes D8 wherever D8 or older contracts, packages or tests assume authoritative ownership, permission, automatic reciprocity or prescriptive action selection (D8.5 permission wording, D8.8 rights model, D8.13 planning wording, R74).** Legacy Stage 3/M3–M5 scenarios and their hashes are preserved.

| Id | Decision |
|---|---|
| D9.1 | **Hard-code physics and biology, not intelligent behaviour or social conventions.** No scripted build orders, professions, retaliation or moral judgement. |
| D9.2 | **NPCs act through twelve primitive embodied controls** (stepping, turning, generic USE of a good on self, another good, a building or another NPC, take/drop/gesture etc., per the workbook `NPC Actions` tab) and **twelve needs**. Fleeing, foraging and approaching are reasons to use the controls, not extra actions. No new primitive verb for a profession or recipe. The same executor serves learned policy, possession and test fixtures. |
| D9.3 | **Objects and buildings define the available interactions** (data-defined, from the workbook `Objects` and `Buildings` tabs: 138 object and 65 building types as the master authoring backlog; implement the reachable initial subset first; old ownership wording in the workbook is superseded). |
| D9.4 | **Ownership is not hard-coded.** The world records physical possession, location, containment, condition, provenance, barriers, occupancy and actual transfers, never a rightful owner, permission or theft. NPCs may place goods for communal use, conceal them, share or claim them, by their own beliefs and communication. |
| D9.5 | **Trade is communication plus separate voluntary hand-overs:** an offer or promise (gesture and referents), then giving; the other party may fulfil, partly fulfil, delay, refuse or keep both. No atomic trade, automatic reciprocity, title change or enforced debt. |
| D9.6 | **Construction emerges from individual intelligence and knowledge:** a known design + USE on a valid site places an unfinished site; delivering materials and labour by USE advances it; cooperation, interruption, abandonment and resumption are physical; builders gain provenance, not title or privilege. NPCs choose what, where and when. Roads only connect existing settlements; no flags or carriers. |
| D9.7 | **Aggression, retaliation, avoidance, exploitation and cooperation are allowed and emerge,** within and across tribes, through the same controls (USE as a strike, gestures as threat/submission/protest, retreat by movement), with injury, pain, fear and death. Hawk, Dove, Bully, Retaliator, reciprocal cooperator and cheat are measured patterns, never assigned classes. |
| D9.8 | **The founding population is trained** to competent survival and meaningful interaction before an autonomous society is claimed; later generations learn by experience, observation and teaching, inheriting temperament and biology, not adult memories or knowledge. |
| D9.9 | **Kept from D8:** six tribes of 7 adults and 5 children, seven terrains, infinite time-limited forage independent of dice, dice-generated raw materials, building-gated raw extraction (a completed compatible collection building with storage capacity — a physical, not permission, requirement), labour-only level-1 collection shelters, persistent individuals, the god-only player. |
| D9.10 | *Default:* founder training is on the critical path (package UN6t): offline training against the authoritative simulator and the same controls, versioned deterministic inference, predeclared held-out thresholds with untrained/frozen controls; a failed training gate is not a green UA and no scripted fallback is counted as trained. |
| D9.11 | *Default:* UA separates mechanical, learned/autonomous and long-run viability evidence; acceptance additions EC-01–EC-02, EP-01–EP-05, CF-01–CF-02, TR-01–TR-02, QA-01–QA-02. |
| D9.12 | *Default (brief Part C):* all non-desert hexes including sea receive production numbers; standalone residential hut disabled initially; each tribe's hall accommodates all 12 founders (supersedes R80's 8 beds); varied valid families; water accessible without a circular tool dependency if thirst is a need. |
| D9.13 | *Default:* legacy Owner/UseRight/estate/permission fields stay only for legacy serialisation and regression, neutral in unified physical logic; every reader and writer is audited; no adapter recreates title or access control. Death leaves goods where they physically are; no automatic succession. |
| D9.14 | *Default:* industry (I1a…) stays deferred until UB plus credible autonomous primitive viability, unless the owner decides otherwise. Run constraints unchanged: 18-hour window, six-agent limit, worktrees, Opus/Sonnet (no Fable), determinism, legacy hash pins, no main changes without the owner, final build and assurance window. |

## D10: Scale target (owner, 28 Nov 2026)

| Id | Decision |
|---|---|
| D10.1 | **Long-term target:** six fully functioning factions, each with 3–4 settlements made of collections of different buildings, **100–200 NPCs per faction** (owner correction, 28 Nov 2026; about 600–1,200 in total), with back-and-forth conflict between factions emerging from individuals and groups (no faction AI). |
| D10.2 | **The island may be made bigger** so it does not run out of space: island size, hex count and settlement sites are configurable and must scale. |
| D10.3 | *Lead default:* this is a later checkpoint (**UC "Scale"** (6 factions × 100–200 NPCs)) after UB; UA and UB keep their scope, but every unification package must avoid blocking it (bounded per-NPC brain cost, spatial indexing, decision staggering by sim state never by camera, no all-pairs scans, geometry not assuming 30 hexes). A performance package runs alongside where it does not touch the same files. |
