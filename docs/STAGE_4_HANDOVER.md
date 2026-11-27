# Stage 4 handover (written 27 Nov 2026, updated the same day for the owner's answers)

This document gets a fresh Claude session from zero to implementing Stage 4 without this conversation. Read it fully, then follow "How to begin" at the end. **The running record of the implementation is [stage4/DEV_LOG.md](stage4/DEV_LOG.md)**: current milestone, commits, test results, benchmarks, decisions, temporary mechanisms and instructions for the next session. Read it straight after this file.

## 1. Where things are

| Item | Value |
|---|---|
| Repo | `Joesalmon1985/logistics-rts-sim` (private) |
| Branch | `unity/poc`. `main` changes only when the owner says so. |
| Worktree | `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs` |
| Last code commit | `d7eeb95`, "Roads easy to find: road tool starts after placing, Road (R) on every tab" (pushed) |
| Docs commit | The commit containing this file (pushed together with it) |
| Unity | 6000.3.28f1, CLI at `C:\Users\joesa\.unity\bin\unity.exe` |
| Build | `Builds\Windows\GodOfTheIsland.exe`. Built from `d7eeb95`; not in git. |
| Stage 3 checkpoint | Git tag **`stage3-final`** at `d7eeb95` (created by the lead). It replaces Stage 3 save compatibility, which the owner says is **not** needed. |
| Development log | [stage4/DEV_LOG.md](stage4/DEV_LOG.md) |

## 2. Build and test status at handover
All of the following ran on `d7eeb95`:
- **`dotnet test DotNet -c Release`:** 282 of 282 passed.
- **Unity EditMode:** 282 of 282 passed.
- **Unity PlayMode:** 73 of 73 passed.
- **Windows build:** succeeded (87 MB, 0 errors, 1 warning).
- **Headless smoke** (`GodOfTheIsland.exe -batchmode -nographics -godsmoke 60000 -godseed 2`): Crimson wins at minute 63.2, invariants ok, 0 exceptions. Seeds 4 and 7 on the previous commit: Crimson at 45.8, Jade at 39.3.
- **Not done:**
  - No hand play of stage 3, the tutorial or the road fix. The owner has played a little and reported the road problem, which is now fixed.
  - No Stage 4 code exists.

## 3. Known issues carried into Stage 4
- **Stage 3 balance targets missed.** Two of 12 seeds end before minute 25. Only 5 of 12 games have 4 factions at 4 VP. Bow + helmet wins 73% of its duels in real games. On 4 seeds, 2 to 4 factions never build an army. See `docs/UNITY_POC_PLAN.md`, "Q4 result". Stage 4 replaces most of these systems, so don't tune stage 3 further unless the owner asks.
- **The stage 3 tuning harness was never committed.** It was in a session scratchpad and is now gone. M0's runner replaces it.
- **Mutable statics block parallel runs:** `MilitaryTuning`'s 22 fields, mutable building-def arrays, and the lazy `TradeTuning` table. `DuelTests` mutates `MinDamage`. Fixed in M0.
- **Accepted changes to stage 3 results.** Dice and other draws use the shared `s.Rng`, and `StateHash` doesn't include pending commands. M0 changes both, so stage 3 seed results and hashes change once. The owner's determinism decision accepts this. Update test expectations that depend on exact seeds.
- **`double` in a few rules:** board topology, lattice, faction rules, parts of the economy, the HQ circle. New code is integer or fixed point only. **Owner decision (27 Nov):** M0 audits the existing floating point and verifies it on more than one host; anything that fails parity or cannot be guaranteed is converted; harmless code is not rewritten without reason ([HEADLESS_SIMULATION.md §11](stage4/HEADLESS_SIMULATION.md#11-open-questions)).
- **A paused Unity game steps a held tick per command** (`AiHost.StepHeld`). Replays must log these until M1 replaces the mechanism.
- **`Unity` leaves `Assets/InitTestScene*.unity` files** after PlayMode runs. Never commit them.

## 4. What Stage 4 is (summary)
**The vision.** The game moves from Darwin, through Dawkins, to Marx, with Smith, Axelrod, Ostrom and *Creatures* as further influences:
- **People:** persistent NPCs with full life cycles and households, four distinct kinds of inheritance, lifetime associative learning in every domain, and a non-verbal gesture language that is a real sim mechanic.
- **Economy:** it can develop from subsistence to employment and classes.
- **State:** the player governs it only through institutions and incentives.
- **Data:** information layers, domain events, an analytical warehouse and KPIs.
- **AI:** a deterministic headless runner, and four policy levels ready for neural networks later.
- **Neutrality:** nothing is forced to emerge.

**Stage 4 builds the foundations.** Later stages (the Village, Town and City ages) add the institutions one by one.

**Every owner decision** is in [stage4/DESIGN_CONVENTIONS.md §2](stage4/DESIGN_CONVENTIONS.md#2-owner-decisions-complete-list-2527-nov-2026), the answers to all 23 former open questions are in [§13](stage4/DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026), and the owner's further requirements of 27 Nov are in [§14](stage4/DESIGN_CONVENTIONS.md#14-further-owner-requirements-27-nov-2026). The most important:
- NPCs replace walkers and soldiers.
- NPCs never read the true state.
- Lifetime learning is a core workstream.
- The state acts only through incentives: it designates sites and commits goods, but only NPC labour builds.
- Barter only; currency comes later.
- Ownership can be state, private, communal or cooperative.
- 10 VP is kept, but the game is **sandbox first**.
- Life speed is adjustable per game (`LifeScale`).
- Civilisation ages advance by **emergent unlocks**.
- Information separation is **built now, enforced later**.
- Determinism is exact, with integer inference.
- No class or strategy labels on NPCs.
- Lab experiments are kept separate from island experiments.
- No training in Stage 4.
- (27 Nov) Same-sex partnerships with equal capabilities; serial partnerships in M4 on an architecture that allows more; informal shared childcare; care work is real work and unpaid care is reported.
- (27 Nov) Staffed workshops in both modes; no anonymous workers; disease on by default; forest depletion on in the sandbox; walk-outs suspend contracts.
- (27 Nov) Loyalty is earned, never rewarded; communities can found institutions without the state; no composite wellbeing score.
- (27 Nov) Six factions of about 20 people as the first scenario; reproduction for adults only; nothing auto-replenished; collapse is not tuned away.
- (27 Nov) *Creatures*-style lifetime learning with eight required owner scenarios (LS1–LS8).

## 5. The documents
Read in this order:
1. [stage4/DESIGN_CONVENTIONS.md](stage4/DESIGN_CONVENTIONS.md): the binding contract. It covers decisions, the time model, modules, ids, information layers, policy levels, events, ages, the official milestone list with checkpoints 4A–4F and the doc map. Its §13 records the owner's answers to all 23 former questions and §14 the further requirements.
2. [stage4/DEV_LOG.md](stage4/DEV_LOG.md): where the run stands.
3. [UNITY_POC_PLAN.md, Stage 4 section](UNITY_POC_PLAN.md#stage-4-a-living-population-under-a-state-planned-2627-nov-2026-not-started): what Stage 4 is, plus the reuse/refactor/replace table.
4. [stage4/ACCEPTANCE_TESTS.md](stage4/ACCEPTANCE_TESTS.md): the gates for every milestone.
5. The design docs for the milestone you're on:

| Area | Documents |
|---|---|
| Vision | [VISION_AND_PHILOSOPHY](stage4/VISION_AND_PHILOSOPHY.md) |
| People | [POPULATION](stage4/POPULATION_DESIGN.md), [BIOLOGICAL_INHERITANCE](stage4/BIOLOGICAL_INHERITANCE_DESIGN.md), [HOUSEHOLDS_AND_KINSHIP](stage4/HOUSEHOLDS_AND_KINSHIP_DESIGN.md) |
| Mind | [NPC_LEARNING](stage4/NPC_LEARNING_DESIGN.md), [COMMUNICATION_AND_COGNITION](stage4/COMMUNICATION_AND_COGNITION_DESIGN.md), [SOCIAL_MEMORY](stage4/SOCIAL_MEMORY_DESIGN.md), [CULTURAL_TRANSMISSION](stage4/CULTURAL_TRANSMISSION_DESIGN.md) |
| Property and economy | [MATERIAL_INHERITANCE](stage4/MATERIAL_INHERITANCE_DESIGN.md), [ECONOMY](stage4/ECONOMY_DESIGN.md), [ECONOMIC_CLASSES](stage4/ECONOMIC_CLASSES_DESIGN.md), [POLITICAL_ECONOMY](stage4/POLITICAL_ECONOMY_DESIGN.md) |
| State | [STATE](stage4/STATE_DESIGN.md), [INSTITUTIONAL_STATE](stage4/INSTITUTIONAL_STATE_DESIGN.md), [MILITARY](stage4/MILITARY_DESIGN.md) |
| Information and data | [INFORMATION_AND_MEDIA](stage4/INFORMATION_AND_MEDIA_DESIGN.md), [KPI_CATALOGUE](stage4/KPI_CATALOGUE.md), [DATA_WAREHOUSE](stage4/DATA_WAREHOUSE_DESIGN.md) |
| Game theory | [GAME_THEORY](stage4/GAME_THEORY_DESIGN.md), [STRATEGY_BENCHMARKS](stage4/STRATEGY_BENCHMARKS.md) |
| Running and AI | [HEADLESS_SIMULATION](stage4/HEADLESS_SIMULATION.md), [POLICY_EXPERIMENTS](stage4/POLICY_EXPERIMENTS_DESIGN.md), [AI_TRAINING](stage4/AI_TRAINING_DESIGN.md) |
| Research (read-only) | [Selfish Gene notes](stage4/research/SELFISH_GENE_NOTES.md), [Creatures notes](stage4/research/CREATURES_NOTES.md) |

The owner's copy of the book is at `C:\Users\joesa\Downloads\the-selfish-gene-4bhyg61ook.pdf`. It is copyrighted: never commit its text.

## 6. Ordered milestones
The official list, with what each needs, is in [DESIGN_CONVENTIONS §10](stage4/DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list). The milestones M0–M15 are kept and grouped into six functional checkpoints. In short:

| Checkpoint | # | Milestone | Needs |
|---|---|---|---|
| **4A Simulation laboratory** | M0 | Headless runner, SimTuning, SimCalendar, victory conditions, events, replay and hash | - |
| 4A | M1 | Policy seams (four levels) and FactionKnowledgeView | M0 |
| 4A | M2 | Game-theory lab (maths) | M0 |
| **4B Living population** | M3 | NPC core with instincts and associative learning | M1 |
| 4B | M4 | Life cycle, households, genome, estates | M3 |
| **4C Social economy** | M5 | Social memory, gestures, barter, gifts | M3 |
| 4C | M6 | NPC labour replaces walkers; basic state; ownership records | M5 |
| 4C | M7 | Promises, reputation, lies, misunderstanding, observational learning | M5 |
| **4D Political economy** | M8 | Enterprises, employment, cooperatives, commons, class measures | M6, M7 |
| 4D | M9 | Soldiers are NPCs | M6 |
| 4D | M10 | Citizenship, loyalty, migration, political-interest measures | M7, M9 |
| 4D | M11 | Cultural transmission | M4, M7 |
| **4E Institutional state** | M12 | Band-age institutions framework (and communal institutions without the state where practical) | M6, M10 |
| 4E (may be pulled forward) | M13a | SQLite warehouse core: person, household, economy and citizenship facts | M0 plus the event types of the milestones done; any time after M6 |
| 4E | M13b | Institution KPIs and reports, HUD reports panel | M12, M13a |
| **4F Integrated gameplay and network readiness** | M14 | Incentive faction AI, balance (sandbox first), HUD, tutorial | M6–M13b |
| 4F | M15 | Network readiness (no training) | M1, M14 |

**Corrected: there is no "minimum viable Stage 4" of M0–M6 plus M9 and M13.** That claim was wrong, because M13 needed M12, M12 needs M10, and M10 needs M7 and M9. The honest functional releases are:
- **First playable new game: the end of 4C plus M9, that is M0–M7 and M9.** A living, learning population; households; a social economy with NPC labour and state incentives; persistent soldiers.
- **Analytical recording:** M0 already writes JSONL events; **M13a** (any time after M6) adds the SQLite warehouse core; **M13b** (after M12) adds institution KPIs and reports.

**Parallel work respects dependencies, not file boundaries.** Two milestones may run in parallel only when each one's needs are met; touching different files is not enough to make it safe. With that rule:
- M1 and M2 can run in parallel after M0;
- M2 can run beside M3–M5;
- M7 can run beside M6 (both need only M5); M9 beside M7 or M8 once M6 is done; M11 beside M8–M10 once M4 and M7 are done; M13a beside anything after M6.

**Unity visualisation is progressive:** each milestone delivers its own inspection or view (the table in [conventions §10](stage4/DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list)), not all at M14. **Every temporary compatibility mechanism** is listed with its removal milestone in [DEV_LOG.md](stage4/DEV_LOG.md#temporary-mechanisms).

## 7. Owner questions (resolved)
**All 23 former open questions were answered by the owner on 27 Nov 2026.** The answers are in [DESIGN_CONVENTIONS §13](stage4/DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026), and each owning design document records its decision in place of its open question. The owner's further requirements of the same day are in [§14](stage4/DESIGN_CONVENTIONS.md#14-further-owner-requirements-27-nov-2026).

**Do not ask the owner questions that have reasonable defaults.** Take the default, record the choice in [DEV_LOG.md](stage4/DEV_LOG.md#design-decisions-taken-during-implementation), and update the owning document. Ask only when no reasonable default exists or a choice would change an owner decision.

## 8. Working rules (from the owner and earlier stages)
- **Layers:**
  - `Assets/God/Sim` is pure C# with no UnityEngine, and is deterministic: integers, keyed `Rng` forks, fixed order, no wall clock.
  - `Assets/God/Game` only reads state, sends commands and animates events.
  - Sim code and its tests come before views. Tests must also pass under `dotnet test DotNet -c Release`.
- **Never commit:**
  - `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`;
  - `*.csproj` or `*.sln` outside `DotNet/`;
  - `LocalData/`, anything derived from Sims 2, the book text, or `Assets/InitTestScene*`.
  - Run `git status` before every commit.
- **No third-party art or audio.** Names and data are original. Don't copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty` or WorldMVP's rigged actors.
  - **Before any Unity open or test run, run `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1`. It must show 0 errors.** It compiles Game, Editor and every test assembly without Unity (about 3 s warm, 20 s cold; `dotnet test DotNet` alone does not cover them).
- **Unity:**
  - **One editor per folder.** Use the Unity CLI:
    - `unity open .` then poll `unity status` until it reports ready.
    - Tests: `unity command --project-path . test run --mode editor|playmode --async-tests true`, then poll `Temp/pipeline_test_status.json` until `"status": "completed"`. The synchronous form times out after 30 s.
    - Build: `unity command --project-path . script run --file AgentScripts/BuildWindows.cs --entry BuildWindows.Run`.
    - Close with `unity close .` when done.
  - **The built player in automation:** only `-batchmode -nographics`, never windowed.
  - **Never touch the registry. Computer use was denied.**
- **Parallel agents:**
  - The owner likes parallel subagents, with Opus for hard packages and Sonnet otherwise. No Fable.
  - Give each code package its own worktree (`git worktree add ..\wt-lrs-<name> -b unity/<name>`) and copy the Library with `robocopy Library <wt>\Library /E /MT:16` (exit codes 0–7 are fine).
  - Merge into `unity/poc` yourself, then run every suite.
  - Agents that write docs can share the main worktree, if each owns distinct files.
- **Commits:**
  - Small, logical commits on `unity/poc`.
  - Commit messages end with the co-author line the session gives you.
  - Write the message to a file and use `git commit -F`, because PowerShell 5.1 breaks quoted here-strings.
- **Pushing (authorisation for this run, 27 Nov 2026):** the owner allows committing and pushing **tested** changes to `unity/poc` and to feature branches (`unity/<name>`) without asking first. **Never push to `main`**, and never merge into `main`. "Tested" means every suite the milestone's gates name has run and passed, and the result is in [DEV_LOG.md](stage4/DEV_LOG.md#test-results). (This supersedes the general "do not push without asking" rule in `CLAUDE.md` for this run only.)
- **Reporting:** say which tests and runs actually happened, and say plainly what did not.

## 9. How to begin (for the new session)
1. **Read the docs:** this handover, then [DEV_LOG.md](stage4/DEV_LOG.md), then the conventions doc (§2, §10, §13, §14), then the Stage 4 section of the plan, then ACCEPTANCE_TESTS §3.0 and §M0–M1, then HEADLESS_SIMULATION and the parts of AI_TRAINING about policy interfaces.
2. **Check the tree:** `git status` and `git log --oneline -3` in `wt-lrs`, and which milestone branches exist. Compare with DEV_LOG "Commits and branches".
3. **Run the baseline:** `dotnet test DotNet -c Release` should give 282 of 282 (or the latest figure in DEV_LOG). Skip Unity unless something needs it.
4. **No question round:** the owner has answered every open question and has said not to ask questions that have reasonable defaults. Record any default you take in DEV_LOG.
5. **Plan the first packages:**
   - M0 in the main worktree or one package worktree. It touches core files (`GodSim`, `Rng`, tuning, `GameSetup`, saves), so do it alone first.
   - Then **M1 and M2 in parallel**, each in its own worktree: M1 changes `Sim/Ai` and adds `Sim/Policy`; M2 adds `Sim/Lab`.
   - Then M3.
6. **For each milestone:**
   - write the acceptance tests from ACCEPTANCE_TESTS first;
   - implement;
   - run every suite and the determinism checks;
   - rebuild and run a headless smoke;
   - update DEV_LOG (status, tests, benchmarks, temporary mechanisms, outstanding work);
   - commit, and push the tested result to `unity/poc` or the feature branch (never `main`);
   - report to the owner with honest results.
7. **Keep the docs true:** when an implementation choice changes a design, update the owning doc in the same commit, and the conventions doc if the change is cross-cutting.
