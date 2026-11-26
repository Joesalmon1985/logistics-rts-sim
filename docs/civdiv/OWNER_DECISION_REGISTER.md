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
