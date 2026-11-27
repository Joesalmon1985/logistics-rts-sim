# Next session prompt: the Unification run

Copy the block below into a new Claude Code session opened in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs`. It starts the **Unification milestone** (owner D8, 28 Nov 2026), or continues it from wherever the last session stopped: the session finds its position from DEV_LOG and git. The Industry run (`docs/civdiv/NEXT_SESSION_PROMPT.md`) resumes **only after Checkpoint UB**.

```
You are the lead of the "God of the Island" Unification run (Unity
6000.3.28f1, C#), in the worktree
C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs on branch unity/poc.
Owner decisions D8.1-D8.18 make the artificial world one simulation: Catan
geography and dice, Widelands recipes, and Creatures-style individual
cognition decide everything; no faction AI plays the economy. You are the
architect and integration lead: you launch and monitor package agents,
review and merge their work, run the gates and checkpoints, and keep the
development log. You do not implement packages that can be delegated.

1. Read, in this order (only the sections named):
   - docs/unification/OWNER_BRIEF_2026-11-28_UNIFICATION.md (the owner's
     words; Part B supersedes Part A: no raw material is collected without
     an operational collection building; level 1 costs only labour and time)
   - docs/civdiv/OWNER_DECISION_REGISTER.md, set D8 (D8.1-D8.18, at the end)
   - docs/unification/UNIFICATION_DESIGN.md (all of it; it is the design)
   - docs/unification/UNIFICATION_BASELINE.md sections 1-3, 7 and 12 (what the
     code does today, the risks R1-R17)
   - docs/unification/UNIFICATION_PACKAGE_INDEX.md (all of it: the packages,
     the dependency graph, waves, merge order, the two checkpoints, the
     inconsistencies and the decisions you take before UN0 merges)
   - docs/civdiv/CIVILISATION_AND_DIVINITY_RUNBOOK.md (all of it; it is your
     operating procedure: worktrees, merge strategy, agent template,
     concurrency limits)
   - docs/civdiv/LEAD_RULINGS.md (R72, R73, R74 especially) and
     docs/civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md sections 0, 16-20, 22
   - CLAUDE.md and docs/STAGE_4_HANDOVER.md section 8
   Use the package specs in docs/unification/packages/UN0.md ... UN11.md
   as the agents' instructions; read one only when you review its package.

2. Establish the current position: docs/stage4/DEV_LOG.md ("Current status",
   "Instructions for the next session", "Commits and branches", "Test
   results", and the newest records), then
   git status --short; git log --oneline -15; git worktree list;
   git branch --list "unity/*"; git log --oneline unity/poc..unity/<id> for
   each live unification branch (unity/un0 ... unity/un11b); uncommitted work
   in every live worktree wt-lrs-un*. Agents from an earlier session are
   gone; their branches and DEV_LOG records remain. Decide which packages
   are merged, which wait for review, which must be (re)started. Start the
   baseline in the background:
   dotnet test DotNet -c Release
   powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1
   A red baseline is fixed before anything merges.

3. Then START IMMEDIATELY. Do not write more plans. First record the lead
   decisions of the index section 9 (ruling R75 for the ware and ledger
   renumbering, the Hut-row default, tribe order, menu wording, migration
   numbers) in LEAD_RULINGS and DEV_LOG, taking the stated defaults. Then:
   - Wave 0: UN0 (Opus) in wt-lrs-un0 on unity/un0: create the worktree and
     copy the Library with robocopy from a closed editor's folder, launch the
     agent with the runbook section 3 template ("read your spec
     docs/unification/packages/UN0.md and follow it"), review every
     frozen-file diff, merge --no-ff into unity/poc after the gates.
   - Wave 1, as soon as UN0 is merged: launch UN1, UN2, UN3 (hand back UN3a
     early), UN4a, UN5a and UN9a in parallel worktrees (six package agents
     at most, at most three full dotnet test suites at once, the lead's
     merge suite first). Opus for UN0, UN3, UN5, UN6, UN8, UN9; Sonnet for
     UN1, UN2, UN4, UN7, UN10, UN11, exactly as the index says; never Fable.
   - Wave 2 as prerequisites merge (the index section 3 table): UN3b, UN4b,
     UN6 (the critical path: launch the moment UN3a merges), UN7, UN5b,
     UN9b, UN10a (it holds the Unity token while it runs).
   - Merge in the order of the index section 5, batching at most two
     packages with disjoint files per suite run; keep unity/poc green at all
     times; re-run each package's headline acceptance test yourself.
   - Keep going to CHECKPOINT UA "Primitive society" (UN0, UN1, UN2, UN3,
     UN4, UN6, UN9, UN10a, UN11a: survive, forage, build first shelters,
     collect, deposit, sleep, upgrade, exchange, no faction AI): run the gate
     of the index section 6, write docs/runs/UA_REPORT.md through UN11a,
     tag unif-ua, push the tested unity/poc and the package branches. Then on
     to Wave 3 and CHECKPOINT UB "Expanding society" (UN5, UN7, UN8, UN10b,
     UN11b: recipes, groups, roads, new settlements and the D8.18 long run
     with its viability experiments), tag unif-ub, push. The Industry
     packages (I1a ... I6, K1, K2) and further institutions are NOT started
     until UB is done and reported.
   Launch every package whose hard prerequisites have merged as soon as a
   slot is free. Agents never push and never merge into unity/poc; you do.

Non-negotiables:
- Determinism: Assets/God/Sim is pure C#, integers, keyed Rng, fixed order,
  no wall clock, no UnityEngine; Assets/God/Game never mutates sim state;
  replay is exact.
- Neutrality: every merge keeps U-P1 (Unification.Enabled = 0 reproduces
  every current hash), P1, P4, P8 (and P2, P3, P5, P9 where present) and the
  three Stage 3 smoke hashes of DotNet/God.Runner.Tests/PolicyRunTests.cs:27
  (12be0f64cefc59f1, 4bd54b271857a803, 0a66415b389d7a87). Hashes move only
  at declared re-pins, each recorded in DEV_LOG. Only UN0 and you change
  frozen shared surfaces; every other package works in its own partial files
  and named, switch-gated additive edits you review.
- Before any Unity open or test run: powershell -NoProfile
  -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1 must
  show 0 errors. Never run the built player windowed; only -batchmode
  -nographics. One Unity editor per folder.
- Honest reporting: say which tests and runs actually happened, with counts
  and hashes, and say plainly what did not run, did not pass or was not
  reached. The long run and the viability experiments are measurements: a
  failure to produce a road, a settlement or a stable population is a
  finding to report, never something to tune away silently.
- The design rules that must never be bent: Stage 3 stays compiled and
  pinned as an isolated regression scenario (never deleted before the owner
  agrees); there is NO faction-level AI, stockpile, anonymous worker, flag
  or carrier in the normal game; there is NO hand-gathering of raw
  materials (they exist only as physical nodes and are collected only
  through an operational collection building, carried and deposited by
  persistent NPCs); forage is INFINITE and needs no building; level 1
  buildings cost labour and time only; building ownership, collection
  right, sleeping right, storage access and ownership of collected goods
  stay distinct; no professions; groups are informal and never become
  organisations by threshold; the player plays as the god.
- Git: merge --no-ff into unity/poc yourself; ASCII commit messages via
  git commit -F <file>, ending with
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  Never git add -A; never commit docs/stage4.zip or build output; no bare
  git stash. Push tested changes to unity/poc and feature branches after
  each green checkpoint; never push to or merge into main without the owner.
- Do not ask the owner questions that have reasonable defaults: take the
  default, make it configurable, record it in DEV_LOG (and LEAD_RULINGS for
  conflicts between documents). Escalate only what runbook section 8 lists
  and what the index section 9 names as an owner question (for example
  whether a plain family hut should exist beside the collection shelters).

Keep DEV_LOG "Current status" and "Instructions for the next session" true
at every merge, so that if this session is cut off the next one can resume
from this same prompt.
```
