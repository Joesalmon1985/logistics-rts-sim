# Civilisation and Divinity master plan (27 Nov 2026)

The single overview of the Civilisation, Divinity and Industrialisation run: what the game becomes, what the owner has decided, what is in scope at which tier, the order of delivery, the playable checkpoints A–G, which document owns what, and the questions still open with the default the run uses. It summarises and links; it does not repeat the detail. Where this plan and a more specific document disagree, the more specific document wins in this order: [OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md), [SHARED_ARCHITECTURAL_CONTRACTS.md](SHARED_ARCHITECTURAL_CONTRACTS.md), [LEAD_RULINGS.md](LEAD_RULINGS.md), [DEVELOPMENT_PACKAGE_INDEX.md](DEVELOPMENT_PACKAGE_INDEX.md), [DEVELOPMENT_DEPENDENCY_GRAPH.md](DEVELOPMENT_DEPENDENCY_GRAPH.md).

How to run the work is in [CIVILISATION_AND_DIVINITY_RUNBOOK.md](CIVILISATION_AND_DIVINITY_RUNBOOK.md). The prompt that starts or continues the run is in [NEXT_SESSION_PROMPT.md](NEXT_SESSION_PROMPT.md).

---

## 1. The integrated vision: one world

"God of the Island" is one deterministic artificial world in which three things happen at once, on the same people, goods, organisations and events:

1. **Civilisation** (brief 26 Nov §§2–3, §§15–24). Persistent autonomous NPCs are born, learn for life (*Creatures*-style associative learning), form households, inherit genes, goods and habits, gesture to each other without words, barter, work, own and steal. From those individual decisions an economy, classes and institutions may emerge: communal stores, healers, enterprises, governments, temples. The line runs from Darwin, through Dawkins, to Marx, with Smith, Axelrod and Ostrom as further influences. Nothing is forced to emerge (D5.33); no faction-level abstraction secretly does the work (D5.6).
2. **Divinity** (brief §§4–14). The player is a **god, never a government** (D5.1). Every faction has a god (D5.3); gods persist whatever happens to their people (D5.4, D5.5). The god spends **Mythic** (D5.10, D5.26) on six distinct kinds of intervention: suggestions, commands, temporary compulsion (D5.7), possession of one NPC (D5.8), a physical avatar (D5.9), miracles that genuinely change state with ledger rows (D5.11), structured commandments (no free text in the sim), and resurrection of the same person (D5.12, D5.13). NPCs perceive, interpret, obey or refuse, and learn from what happened by their own attribution. Governments are NPC organisations that may reject a god (D5.2).
3. **Industrialisation** (brief 27 Nov, D6.1–D6.19). The OpenRA and Power Grid ideas are reinterpreted inside the same ontology: finite deposits, NPC-operated harvesters, refineries, factories with real inputs, power stations that burn delivered fuel, electricity only through connected networks, designs, knowledge, patents and licences as distinct things, waste that is accounted for. **One money and accounting architecture** (D6.2): production never creates money, there is no external ore buyer and no central fuel market (D6.6). An industrial starting scenario ("Iron Coast") runs on exactly the same simulation as the prehistoric "Living Island" (D6.17).

The three are connected, not separate modes: a god may compel a lineman to cut a power link, possess a harvester driver, smite a factory owner who breaks a commandment, or heal a worker; organisations compete for the same labour, food, fuel and electricity; every act is a recorded event that reconciles in the warehouse and replays exactly.

**Presentation** (D5.18–D5.25, D6.19): a seamless natural island with the Catan grid hidden and movement nodes developer-only; stylised low-poly characters that show age, activity, carried goods and divine effects; a hybrid interface (world first, panels for analysis); desktop-first with a mobile-compatible architecture; the decoded message text is an optional overlay, off by default.

**Non-negotiables carried from earlier stages:** `Assets/God/Sim` is pure deterministic C# (integers, keyed `Rng`, fixed order, no wall clock); `Assets/God/Game` only reads state, sends commands and animates; NPCs never read the true state; human and AI gods act under the same rules; information layers stay separate (D5.28); recording never changes the sim hash.

## 2. Resolved decisions

All owner decisions are in [OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md). This run rests on:

| Set | Ids | Summary | Worked out in |
|---|---|---|---|
| Stage 4 (Q1–Q23, D2–D4) | see register | population, learning, households, economy, information, determinism, demography | `docs/stage4/DESIGN_CONVENTIONS.md` §13–14, DEV_LOG |
| **D5** Civilisation and Divinity | D5.1–D5.33 | player is a god; autonomous governments and institutions; Mythic; six intervention types; possession, avatar, miracles, resurrection; seamless presentation; sandbox first; no network training | the D5 design docs (section 6) |
| **D6** Industrialisation | D6.1–D6.19 | third workstream on the same simulation; one money architecture, no external buyer; resources, vehicles, factories, electricity, military production, knowledge and rights, auctions, waste; the industrial vertical slice after the D5 objectives (D6.18) | the D6 design docs (section 6) and contracts §22 |

The lead's binding rulings on inconsistencies between documents are [LEAD_RULINGS.md](LEAD_RULINGS.md) R1–R37 (implementation decisions, reversible, not owner decisions). The most important for planning: R17 (branch names), R27 (seeded governments), R33 (only knowledge and inputs gate production), R35 ("D6.n" is the owner set, "package D6" the commandments package) and **R37: the run continues past H18 until a finished playable game; Checkpoint G is in scope.**

## 3. Scope by tier

From brief §42 and D6.18, reassessed against the code in [CURRENT_IMPLEMENTATION_BASELINE.md](CURRENT_IMPLEMENTATION_BASELINE.md). Package ids are those of the [index](DEVELOPMENT_PACKAGE_INDEX.md).

| Tier | Content | Packages |
|---|---|---|
| **1 Must** (protected, never cut) | stable integrated population; demographic viability experiments; learning and communication kept; physical resource and labour economy; persistent gods; Mythic; suggestions and commands; temporary compulsion; heal, smite, create; structured commandments; player controls; exact replay; a working Windows build | F0, F1 (F1a, F1b), F2, C1, D1, D2, D3, package D6 (commandments), U1a, U2a, U3a, Q1 (Tier 1 part), Q3, Q4 |
| **2 Strongly desired, D5** (in priority order) | possession; employment and enterprises; generic institutions with communal assistance; gestures and look; avatar; embodied controls and the commandment builder; healthcare; primitive lending; reports; education; government and citizenship; resurrection | D4a, C3, C6, U3b, D4b, U1b and U2b, C7a, C2, U5, C7b, C10, D7 |
| **2 Industrial lane** (opportunistic, only after the Tier 1 set has merged; D6.18) | the ten-step industrial vertical slice; knowledge and designs minimal; commodity money and recorded payments; one supply agreement and recorded sale | I1a, I1 minimal, I2 ∥ I4 minimal, Q3 `ind_slice`, I6 minimal, K1 minimal, K2a, I3; then the waste part and the oil chain |
| **3 Stretch** (kept in the design, not planned) | justice, policing, detention (C9); military individuals (C4/M9); banking (C8); politics beyond minimal citizenship (C5); advanced religion; cultural transmission (M11); more miracles; deeper analytics; tutorial rewrite (U6); I5 combat and beyond; K1 rights beyond the minimum, research, auctions; K2b institutional money; further industry extensions | index §3 |

Folding decisions (index §0): D5 into D4, C8's Tier 2 into C6, minimal C5 into C10, Q2 into Q1, U4 into U3; C4, C9, U6 deferred. Nothing in Tier 3 is dropped from the long-term design; every deferred item keeps its reserved ids, codes and migration numbers.

## 4. Delivery priorities

1. **Never cut** the Tier 1 set or the protected final window (graph §7–8).
2. **Prefer complete playable vertical slices** over partial work on many systems (brief §42).
3. **Order of work:** F0 → F1a → (D1, C1, U1a) → F1b → (D2, D3, U2a, C6, package D6) → Tier 1 set complete → D5 Tier 2 in the order above → industrial lane → Tier 3. The full merge order with planned hours is graph §9; the continuation order after H18 is: I1a (if not merged), I1, C2, D7, U5, C10, I2, I4, K1, K2a, I3, I6, Q3 `ind_slice` → **Checkpoint G**, Q1 `0030` and `0041`, I5, K2b, then Tier 3.
4. **When behind:** the cut ladder (graph §7) drops from the end: industry beyond the slice, K1 rights and auctions, K2b, D7, C10, U5, C2, C7b, U1b/U2b/D4b, U3b. A dropped package moves to the continuation list; it is not abandoned.
5. **Industry never displaces D5 work:** the industrial lane takes only agent slots and merge time the D5 packages leave free, and never a Unity slot before Checkpoint E (D6.18, index §1).
6. **After the 18-hour window the run continues** (R37) through the continuation order to Checkpoint G and then to a finished playable game: the Tier 2 tail, then Tier 3 items in the order of the cut ladder reversed, then polish (tutorial, presentation) and balance.

## 5. Playable checkpoints A–G

Each checkpoint is a tagged commit on `unity/poc` with a Windows build copied under `Builds/` (never committed), demonstrable on its own; a later failure never invalidates it. Planned hours are from the [graph](DEVELOPMENT_DEPENDENCY_GRAPH.md) §5 and §10 (H0 = the start of the run, 27 Nov 2026).

| Checkpoint | Tag | Packages | Planned | Demonstration |
|---|---|---|---|---|
| **A Living world** | `civdiv-a` | F0 (+ U3a) | H3.5 | integrated M3–M5 population with I-BS and demography: households, learning, gestures, barter; F2's experiment reports as text |
| **B Divine sandbox** | `civdiv-b` | F1, D1, D2, D3, U1a, U2a, U3a; Q1 Tier 1 views; Q3 harness | H13.5 | god-only Living Island (`-godonly`): Mythic meter; suggestion, command and compulsion of a named NPC; heal, smite, create resources with ledger rows; grid hidden; exact replay. **`Divinity.Enabled` flips on for god-only and population games here, with deliberate re-pins** |
| **C Working economy** | `civdiv-c` | C1 (+ C3a) | with B | NPCs work fields and workshops, own and haul goods, theft is classified; the economy card |
| **D Personal divine interaction** | `civdiv-d` | D4a, D4b, U1b, U2b, U3b | final build (H18) | possess an NPC and walk it in third person, hand control back to the same person; manifest an avatar that walks and gives; gesture clips |
| **E Institutional society** | `civdiv-e` | C6, C7a, C3b (C2, D7, C10, U5 in the continuation) | final build (H18) | a communal food store with a keeper and a ration rule; an injured person treated by a healer; an enterprise employing workers |
| **F Integrated civilisation and divinity** | `civdiv-f` | everything merged by the freeze | H18 | the final Windows build of the window; s01–s18 scenario status from Q3; the handover |
| **G Industrial slice** (D6.18) | `civdiv-g` | I1a, I1, I2, I4, I6 minimal, Q3 `ind_slice` (+ K1, K2a, I3, C2, C3 if merged) | continuation, about H25 | Iron Coast `ind_slice`: an NPC drives a harvester with visible ore to the smelter; ore processed (or sold with a payment); coal reaches the station; power reaches the factory; a new harvester built from real inputs and boarded by an existing NPC; cutting a link or the coal stalls production and restoring it resumes; VS1–VS10 reconcile and replay (index §4) |

**After G (R37):** the run keeps going on `unity/poc` with further tagged builds (`civdiv-h`, `civdiv-i`, …, one per completed vertical slice) until the owner has a finished playable game: the god-only Living Island and Iron Coast playable from the menu, with the D5 Tier 2 set complete, the industrial slice presented, a working tutorial path and balance sufficient for a sandbox session.

## 6. Document map: which document owns what

| Document | Owns | Status |
|---|---|---|
| [OWNER_BRIEF_2026-11-26.md](OWNER_BRIEF_2026-11-26.md), [OWNER_BRIEF_2026-11-27_INDUSTRY.md](OWNER_BRIEF_2026-11-27_INDUSTRY.md) | the owner's words, verbatim | fixed |
| [OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md) | every owner decision (D5.n, D6.n) and what they supersede | authoritative |
| **This master plan** | vision, decision summary, tiers, priorities, checkpoints, document map, **open questions** | |
| [CIVILISATION_AND_DIVINITY_RUNBOOK.md](CIVILISATION_AND_DIVINITY_RUNBOOK.md) | how the lead session runs: startup, agents, worktrees, merges, tests, monitoring, escalation, checkpoints, release, continuation | |
| [NEXT_SESSION_PROMPT.md](NEXT_SESSION_PROMPT.md) | the copy-ready prompt to start or continue the run | |
| [CURRENT_IMPLEMENTATION_BASELINE.md](CURRENT_IMPLEMENTATION_BASELINE.md) | the real code, tests and blockers at `8617eaa` (the starting point) | historical once F0 merges; DEV_LOG carries the current state |
| [SHARED_ARCHITECTURAL_CONTRACTS.md](SHARED_ARCHITECTURAL_CONTRACTS.md) | shared types, ids, switches, event codes, migrations, system order, save rules, neutrality pins; §22 Industry addendum | **binding**; changed only by the lead (amendment rows §20) |
| [LEAD_RULINGS.md](LEAD_RULINGS.md) | the lead's rulings R1–R37 on conflicts between documents | binding until the documents are corrected |
| [DEVELOPMENT_PACKAGE_INDEX.md](DEVELOPMENT_PACKAGE_INDEX.md) | every package: tier, agent, prerequisites, effort, branch, status; Checkpoint G | the lead updates the status column |
| [DEVELOPMENT_PACKAGES/](DEVELOPMENT_PACKAGES/) | one self-contained spec per package (the agent reads only its spec and the sections it names) | I and K specs being written |
| [DEVELOPMENT_DEPENDENCY_GRAPH.md](DEVELOPMENT_DEPENDENCY_GRAPH.md) | graph, waves, concurrency, timeline, critical path, cut plan, final window, merge order, checkpoints | |
| [RELEASE_ACCEPTANCE_CHECKLIST.md](RELEASE_ACCEPTANCE_CHECKLIST.md) | acceptance scenarios and release gates per checkpoint | being written |
| `docs/stage4/ACCEPTANCE_TESTS.md` (D5 and D6 sections) | the test-level criteria | |
| D5 design docs in `docs/stage4/` | DIVINE_PLAYER, MYTHIC_AND_MIRACLES, POSSESSION_AND_AVATAR, COMMANDMENTS_AND_RELIGION, RESURRECTION, ORGANISATIONS_AND_INSTITUTIONS, INSTITUTIONAL_SERVICES, PLAYER_UI, CAMERA_AND_CONTROLS, VISUAL_WORLD, NPC_ANIMATION, GAMEPLAY_SCENARIOS | design detail; the contracts win on shared numbers |
| D6 design docs in `docs/stage4/` | INDUSTRIAL_RESOURCES_AND_PRODUCTION, VEHICLES_AND_HARVESTING, ELECTRICITY, INDUSTRIAL_MILITARY, WASTE_AND_CIRCULAR, KNOWLEDGE_AND_TECHNOLOGY, MONEY_AND_ACCOUNTING, WORLD_ONTOLOGY §11 | as above |
| Stage 4 docs in `docs/stage4/` | DESIGN_CONVENTIONS (binding Stage 4 contract) and the population, mind, economy, state, data and AI designs | |
| [`docs/stage4/DEV_LOG.md`](../stage4/DEV_LOG.md) | **what is true now**: status, commits, test results, defaults taken, temporary mechanisms, package records | updated at every merge |
| [`docs/STAGE_4_HANDOVER.md`](../STAGE_4_HANDOVER.md) | Stage 4 working rules (§8) and handover §1–2 at release | |

## 7. Open questions

**None blocks the run.** The owner has said not to ask questions that have reasonable defaults (brief §52); each question below runs on its default until the owner answers. An answer is recorded as a new decision in the register and applied by the lead (normally a tuning or scenario switch; none needs a rewrite).

### 7.1 Lead defaults already taken (for the owner to confirm)

From the DEV_LOG pause note, [LEAD_RULINGS.md](LEAD_RULINGS.md), the index §0 and contracts §21.

| # | Question | Default the run uses | Where | Reversible by |
|---|---|---|---|---|
| L1 | When does `Divinity.Enabled` default to on? | On for population and god-only Living Island games **at Checkpoint B**, with deliberate re-pins (population smoke, god-mode scenarios); 0 in every pinned scenario | contracts §21.1, index §0 | switch |
| L2 | Keep `Civ.Enabled` as a third switch beside `Divinity.Enabled` and `Orgs.Enabled`? | Yes | contracts §16, index §0 | — |
| L3 | Who runs a faction's government until C10? | `AiBrain` stays the government seat, a declared temporary mechanism attributed to the Head office, needing a living Head, with individual and divine proposals routed through the council rule; retired fully at M14. The owner may prefer god-only builds that start with **no** government (supported as a scenario option) | ORGANISATIONS §13 q2, index §0 | scenario option |
| L4 | Does every faction start with a government? | Yes: a council of the oldest adults (D5.2). `Orgs.C10.SeedGovernments` = 0 in pinned scenarios, 1 in god-only Living Island and Iron Coast (R27); a no-government scenario is supported | ORGANISATIONS §13 q3, R27 | scenario switch |
| L5 | Can a communal rule make an act a crime in the sim's truth? | Yes, for its members and its property only (decides how justice KPIs count crime) | INSTITUTIONAL_SERVICES §11 q3 | rule |
| L6 | Where does healing skill live? | C7 adds `Skill.Healing` and the general skill slots; fallback: the healer's learned `Treat` values. `Ware.Herbs` lands in F1 (F1.17); `OwnershipBasis.Forfeited` falls back to `Seized` with a `Forfeit` reason if not added | INSTITUTIONAL_SERVICES §11 q1–q2, R22 | — |
| L7 | Default divine observation in competitive play (VP mode, AI versus human)? | `Omniscient` everywhere; `Followers`/`Perceived` profiles available per scenario and per god once D1 builds them (Tier 3 for the full sight model) | DIVINE_PLAYER §12, contracts §21.4 | scenario profile |
| L8 | May a god reveal a deliberately harmful commandment, and may priests knowingly preach false canon? | Both allowed in the sandbox, with truth flags for analysis; scenarios may restrict them | COMMANDMENTS_AND_RELIGION §12 | scenario rule |
| L9 | Order of the D5 Tier 2 set | D4a possession, C3, C6, U3b, D4b avatar, U1b and U2b, C7 healthcare, C2, U5, C7 education, C10, **D7 resurrection last**, so the cut ladder drops D7 first. The owner listed resurrection among Tier 2 wishes; if it matters more than reports or government, the owner should say so | index §1, graph §7 | reorder |
| L10 | AI and scripted gods: inside or outside the sim? | Inside, through `IGodPolicy` in `GodDirector` (system order 910); a scripted test god is an `Ai`-seated `ScheduleGod`, never the `Human` seat | R1, index §0 | — |

### 7.2 Other consequential questions from the design documents

| # | Question | Default | Where |
|---|---|---|---|
| O1 | Should island contests charge retaliation a first-blow cost (as the 1973 model does)? | No change: `ContestRules.FirstBlow = 0`; the variant exists in the lab and is reported, not chosen | STRATEGY_BENCHMARKS §11 item 0, DEV_LOG (D3 §7 first-blow) |
| O2 | Memory for the minds of the dead (resurrection) | Compact minds kept up to 20,000 (about 50 MB); raising the cap is a scenario setting | RESURRECTION §12 |
| O3 | Retire Stage 3 mode (the player as a faction)? | Kept as a temporary mechanism until the owner decides (M14) | register "What D5 supersedes", DEV_LOG Temporary mechanisms |
| O4 | Several possessions, or avatar plus possession at once? | No: one embodied focus per god (tuning later) | contracts §21.2, POSSESSION §13 |
| O5 | Do NPCs recognise an avatar as their god automatically? | No: belief-based (package D6) | contracts §21.3 |
| O6 | Worship-fed Mythic | None in this run; fixed regeneration per sim day (R12) | contracts §21.5, MYTHIC §12 |

### 7.3 Industrial, knowledge and money questions (each with its default)

These are listed in the D6 documents as open with a default; none is needed before Checkpoint G, and most are Tier 3.

| # | Question | Default | Where |
|---|---|---|---|
| I-1 | Rename or merge stage 3 `Ware.Ore` with `IronOre`? | No; separate wares, so no stage 3 or population hash moves | IND §12 q1, contracts §22.11 row 5 |
| I-2 | Toll processing of customer-owned inputs | Not in the first version; a facility processes only its owner's lots | IND §12 q2 |
| I-3 | Tool for manual mining | Manual extraction works without a tool at the base rate; the tool's number is fixed when ECONOMY's tool set lands | IND §12 q3 |
| V-1 | Vehicle collision and congestion | None in the first version; vehicles and people share nodes | VEHICLES §12 q1 |
| V-2 | Unattended extraction | No: extraction is the driver's busy action, so labour is always attributable | VEHICLES §12 q2 |
| V-3 | Electric vehicles charged from the grid | Later; the tank model generalises to a charge store | VEHICLES §12 q3 |
| E-1 | Network operator of a mixed grid | The owner of most links (ties: lowest `OrgId`); a scenario or agreement may name another | ELECTRICITY §12 q1 |
| E-2 | Does minimum-load curtailment cost fuel? | Yes; recorded as curtailed energy | ELECTRICITY §12 q2 |
| E-3 | Grid interval length | 100 ticks | ELECTRICITY §12 q3 |
| M-1 | Do employee crews fight before M9? | No combat until C4/M9; vehicles move and deliver only | INDUSTRIAL_MILITARY §12 q1 |
| M-2 | Who may authorise a strategic strike? | The owning organisation's charter (usually the government's Head and the commander together) | INDUSTRIAL_MILITARY §12 q2 (Tier 3) |
| M-3 | Contamination decay | 1‰ of the excess per day (half-life about 2 sim years) | INDUSTRIAL_MILITARY §12 q3 |
| W-1 | Refuse from foraged food | None (keeps the band economy unchanged) | WASTE §11 q1 |
| W-2 | Does pollution affect fertility directly? | No; only through illness and nutrition | WASTE §11 q2 |
| W-3 | Water pollution as a separate channel | Later, when water bodies exist in the sim | WASTE §11 q3 |
| K-1 | Does reading need a learned `Reading` skill? | Yes; Iron Coast seeds adult literacy | KNOWLEDGE §17 q1 |
| K-2 | Can an organisation hold knowledge? | No; only its members and documents | KNOWLEDGE §17 q2 |
| K-3 | Patent novelty standard | `AgainstRegistry`; `AgainstKnownPriorArt` is a law option | KNOWLEDGE §17 q3 |
| K-4 | Royalty metering when the licensor cannot see output | The licensor bills from the licensee's report or its own inspection; under-reporting is measured against the truth count | KNOWLEDGE §17 q4, MONEY §15 q4 |
| K-5 | Design-table versions across saves | Stable content ids; a save records the content version and refuses a mismatch | KNOWLEDGE §17 q5 |
| $-1 | Do person-to-person M5 trades get `Payment` records? | Only in the industrial scenario (`RecordPersonSales = 1`), so existing hashes are unchanged | MONEY §15 q1 |
| $-2 | Accrual or cash basis in reports | Both kept; cash shown first | MONEY §15 q2 |
| $-3 | Block a payment the payer's own records say it cannot afford? | No; only the physical check and the issuer's balance check | MONEY §15 q3 |

**Not open:** the Stage 4 documents' own open-question sections (population, learning, households, economy, classes, state, information, warehouse, KPIs, game theory, AI training and the rest) record "no open owner question" after the owner's 23 answers of 27 Nov; their remaining items are settled defaults. Minor balance and presentation values are configurable defaults, never owner questions (brief §52, GAMEPLAY_SCENARIOS GS-8).
