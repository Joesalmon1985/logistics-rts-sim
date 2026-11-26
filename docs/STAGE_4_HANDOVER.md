# Stage 4 handover (written 27 Nov 2026)

This document gets a fresh Claude session from zero to implementing Stage 4 without this conversation. Read it fully, then follow "How to begin" at the end.

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
- **`double` in a few rules:** board topology, lattice, faction rules, parts of the economy, the HQ circle. New code is integer-only. Whether to convert the old code is decided in M0 (see HEADLESS_SIMULATION).
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

**Every owner decision** is in [stage4/DESIGN_CONVENTIONS.md §2](stage4/DESIGN_CONVENTIONS.md#2-owner-decisions-complete-list-2526-nov-2026). The most important:
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

## 5. The documents
Read in this order:
1. [stage4/DESIGN_CONVENTIONS.md](stage4/DESIGN_CONVENTIONS.md): the binding contract. It covers decisions, the time model, modules, ids, information layers, policy levels, events, ages, the official milestone list and the doc map. Its §13 lists open owner questions, each with a default.
2. [UNITY_POC_PLAN.md, Stage 4 section](UNITY_POC_PLAN.md#stage-4-a-living-population-under-a-state-planned-2627-nov-2026-not-started): what Stage 4 is, plus the reuse/refactor/replace table.
3. [stage4/ACCEPTANCE_TESTS.md](stage4/ACCEPTANCE_TESTS.md): the gates for every milestone.
4. The design docs for the milestone you're on:

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
The official list, with what each needs, is in [DESIGN_CONVENTIONS §10](stage4/DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list). In short:

| # | Milestone | Needs |
|---|---|---|
| M0 | Headless runner, SimTuning, SimCalendar, victory conditions, events, replay and hash | - |
| M1 | Policy seams (four levels) and FactionKnowledgeView | M0 |
| M2 | Game-theory lab (maths) | M0 |
| M3 | NPC core with instincts and associative learning | M1 |
| M4 | Life cycle, households, genome, estates | M3 |
| M5 | Social memory, gestures, barter, gifts | M3 |
| M6 | NPC labour replaces walkers; basic state; ownership records | M5 |
| M7 | Promises, reputation, lies, misunderstanding, observational learning | M5 |
| M8 | Enterprises, employment, cooperatives, commons, class measures | M6, M7 |
| M9 | Soldiers are NPCs | M6 |
| M10 | Citizenship, loyalty, migration, political-interest measures | M7, M9 |
| M11 | Cultural transmission | M4, M7 |
| M12 | Band-age institutions framework | M6, M10 |
| M13 | SQLite warehouse, Band KPIs, reports | M0, M12 |
| M14 | Incentive faction AI, balance (sandbox first), HUD, tutorial | M6–M13 |
| M15 | Network readiness (no training) | M1, M14 |

**Minimum viable stage 4:** M0–M6 plus M9 and M13.

**Can run in parallel:**
- M2 can run beside M3–M5.
- M7, M8, M9, M10 and M11 can overlap once their needs are met, as long as no two touch the same files.

## 7. Open design questions
These are in [DESIGN_CONVENTIONS §13](stage4/DESIGN_CONVENTIONS.md#13-open-questions-owner). Every question has a **default**. Go ahead with the default unless the owner has answered. At the start of each milestone, put that milestone's open questions to the owner briefly, in one batch, using the question tool. Don't stop work to wait for an answer.

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
- **Pushing:** **ask the owner before pushing**, unless they have already said to push in that session.
- **Reporting:** say which tests and runs actually happened, and say plainly what did not.

## 9. How to begin (for the new session)
1. **Read the docs:** this handover, then the conventions doc, then the Stage 4 section of the plan, then ACCEPTANCE_TESTS §M0–M1, then HEADLESS_SIMULATION and the parts of AI_TRAINING about policy interfaces.
2. **Check the tree:** `git status` and `git log --oneline -3` in `wt-lrs`. The tree should be clean at the docs commit.
3. **Run the baseline:** `dotnet test DotNet -c Release` should give 282 of 282. Skip Unity unless something needs it.
4. **Ask the open questions:** put the open owner questions for M0–M2 to the owner in one batch with the question tool. Carry on with the defaults while waiting.
5. **Plan the first packages:**
   - M0 in the main worktree or one package worktree. It touches core files (`GodSim`, `Rng`, tuning, `GameSetup`, saves), so do it alone first.
   - Then **M1 and M2 in parallel**, each in its own worktree: M1 changes `Sim/Ai` and adds `Sim/Policy`; M2 adds `Sim/Lab`.
   - Then M3.
6. **For each milestone:**
   - write the acceptance tests from ACCEPTANCE_TESTS first;
   - implement;
   - run every suite and the determinism checks;
   - rebuild and run a headless smoke;
   - commit;
   - report to the owner with honest results;
   - ask before pushing.
7. **Keep the docs true:** when an implementation choice changes a design, update the owning doc in the same commit, and the conventions doc if the change is cross-cutting.
