# Civilisation and Divinity runbook (lead session)

Operational instructions for the **primary Claude session** of the Civilisation, Divinity and Industrialisation run (owner brief 26 Nov §§43, 50; brief 27 Nov). What to build and why is in [CIVILISATION_AND_DIVINITY_MASTER_PLAN.md](CIVILISATION_AND_DIVINITY_MASTER_PLAN.md); the order and the hours are in [DEVELOPMENT_DEPENDENCY_GRAPH.md](DEVELOPMENT_DEPENDENCY_GRAPH.md); the packages are in [DEVELOPMENT_PACKAGE_INDEX.md](DEVELOPMENT_PACKAGE_INDEX.md) and [DEVELOPMENT_PACKAGES/](DEVELOPMENT_PACKAGES/). This runbook says **how the lead runs them**. Where it conflicts with the contracts or the graph, they win; record the conflict in DEV_LOG and fix this file.

All paths are under `C:\Users\joesa\Documents\Cursor\RhytmDrama\`. The repository is `Joesalmon1985/logistics-rts-sim` (private). The lead's worktree is **`wt-lrs`** on **`unity/poc`**.

---

## 1. Role of the lead

The lead is **architect, project lead and integration lead** (brief §43). It:

- keeps the dependency graph and the index status column true;
- allocates packages and launches agents as soon as their prerequisites merge;
- owns the shared contracts: amendment rows (contracts §20), rulings (LEAD_RULINGS), `SaveFile.cs`, the save version bumps, re-pins;
- reviews every hand-back against its spec's definition of done, and **independently re-runs** the critical behaviour (neutrality pins, the package's headline acceptance test) before merging;
- merges into `unity/poc`, runs the gates, and keeps `unity/poc` green at all times;
- runs the checkpoints (Unity, build, smokes, tags, push);
- keeps [DEV_LOG.md](../stage4/DEV_LOG.md) current;
- protects the final window and the honesty of every report.

**The lead does not implement delegable packages.** Any package in the index is given to an agent. The lead writes code only for merge conflict resolution, a one-line fix that unblocks a gate, the F1 and I1a frozen-file review, and the save version bump. F0 and Q4 are lead-run with a helper agent (index §0).

## 2. Startup checklist (every session)

1. **Read, in this order** (sections only where named):
   1. this runbook;
   2. [`docs/stage4/DEV_LOG.md`](../stage4/DEV_LOG.md): "Current status", "Instructions for the next session", "Commits and branches", "Test results", then the newest package records at the end;
   3. [CIVILISATION_AND_DIVINITY_MASTER_PLAN.md](CIVILISATION_AND_DIVINITY_MASTER_PLAN.md) (sections 3–5 and 7);
   4. [DEVELOPMENT_DEPENDENCY_GRAPH.md](DEVELOPMENT_DEPENDENCY_GRAPH.md) §3–10;
   5. [DEVELOPMENT_PACKAGE_INDEX.md](DEVELOPMENT_PACKAGE_INDEX.md) §0–1 and §4;
   6. [LEAD_RULINGS.md](LEAD_RULINGS.md);
   7. [SHARED_ARCHITECTURAL_CONTRACTS.md](SHARED_ARCHITECTURAL_CONTRACTS.md) §0, §16–20, §22.0 and §22.8 (the rest when reviewing a package that touches it);
   8. [RELEASE_ACCEPTANCE_CHECKLIST.md](RELEASE_ACCEPTANCE_CHECKLIST.md) for the next checkpoint;
   9. `CLAUDE.md` and [`docs/STAGE_4_HANDOVER.md`](../STAGE_4_HANDOVER.md) §8 (working rules).
   Do not read whole design documents; the package specs name the sections an agent needs.
2. **Check git and worktree state** (read-only):
   ```
   git -C wt-lrs status --short
   git -C wt-lrs log --oneline -15
   git -C wt-lrs worktree list
   git -C wt-lrs branch --list "unity/*"
   git -C wt-lrs log --oneline unity/poc..unity/<id>     (for each live package branch)
   git -C wt-lrs-<id> status --short                     (for each live worktree: uncommitted work?)
   ```
   Compare with DEV_LOG "Commits and branches" and the index status column. Work out **which packages are merged, which are on branches, which agents were running** (an agent from an earlier session is gone: its branch and record are what remain). `docs/stage4.zip` is untracked and must stay so.
3. **Run the baseline** on `unity/poc` in `wt-lrs`, in the background (about 20 minutes; it uses every core):
   ```
   dotnet test DotNet -c Release
   powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1
   ```
   Compare with the newest "Test results" row. A red baseline is fixed before anything merges.
4. **Locate the run on the graph by position, not clock.** The hour marks (H0 = start of the run on 27 Nov 2026) are estimates. Find the last merged item in the merge order (graph §9), apply the cut check that belongs to that position (graph §7: H6 = F1a merged, H10 = D1 and D3 merged, H13 = Tier 1 set merged, H15 = freeze), and record the actual elapsed hours in DEV_LOG "Current status".
5. **Launch** every package whose hard prerequisites are merged, up to the concurrency limits (section 3), then start merging whatever is waiting. Do not plan further: the plan exists.

## 3. Agent allocation

**Models** (index §1 and each spec's "Recommended agent"; the owner prefers Opus for hard packages and Sonnet otherwise; **never Fable**):

| Model | Packages |
|---|---|
| **Opus** | F0 merge helper, F1, F2, C1, C2, C3, C6, C7, C10, D1, D2, D4, package D6, D7, I1a, I1, I2, I4, K1, K2 |
| **Sonnet** | D3, U1, U2, U3, U5, Q1, Q3, I3, I5, I6, the Q4 documentation helper, filler work (index §3, last rows) |

**Concurrency** (graph §4): at most **6 package agents**, plus Q3 (continuous) and F2's background sweeps, plus the lead; at most **3 full `dotnet test` suites** at once, the lead's merge suite first; at most **2 Unity runs** at once: the lead's checkpoint run in `wt-lrs-ckpt` plus **one** package agent holding the **Unity token** (U1a, U2a, U3b, U1b, U2b, I6, in turn). Give the token explicitly in the launch prompt ("you hold the Unity token" or "do not open Unity; hand back after the compile check and the lead runs Unity at the checkpoint"). During the Q1 benchmark slots (graph §4) no other agent runs tests and F2 pauses.

**Launching.** Use the Agent tool with the model above, run in the background, one agent per package (and per part: D1a and D1b are the same agent; F1a and F1b are the same agent). The lead creates the worktree first (section 4) so the agent starts in a ready folder. Prompt template:

```
You are the <ID> package agent for "God of the Island" (Unity 6000.3.28f1, C#).
Model: <Opus|Sonnet>.

Read your spec docs/civdiv/DEVELOPMENT_PACKAGES/<ID>.md and follow it. It is
self-contained; read only the document sections it names.

Your worktree: C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-<id>
Your branch:   unity/<branch>   (already created from unity/poc at <sha>)
Work only there. Never change the checkout of wt-lrs or any other worktree.

Standard rules (also in your spec's "Agent working rules", CLAUDE.md and
docs/STAGE_4_HANDOVER.md section 8):
- Assets/God/Sim is pure deterministic C#: integers, keyed Rng, fixed order,
  no wall clock, no UnityEngine. Assets/God/Game never mutates sim state.
- The contracts (docs/civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) are binding.
  Never edit a frozen file. A contradiction or a missing seam: stop and report
  it to the lead in your hand-back; do not work around it.
- Neutrality: the pins your spec names must hold (P1, P4, P8 always; stage 3
  hash 4bd54b271857a803).
- Commit as you go, in small commits, and keep a "<ID> record" section in
  docs/stage4/DEV_LOG.md on your branch. You may be cut off at any time:
  uncommitted work is lost.
- Commit messages: ASCII, written to a file in your scratch folder, committed
  with git commit -F <file>, ending with the line
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
- Never git add -A; add paths explicitly; run git status before each commit.
  Never commit Library/, Temp/, Builds/, Logs/, UserSettings/, *.csproj/*.sln
  outside DotNet/, LocalData/, docs/stage4.zip. No bare git stash.
- Before any Unity open or test run:
  powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1
  must show 0 errors. <You hold the Unity token | Do not open Unity.>
  Never run the built player windowed.
- Never push. Never merge into unity/poc or main. Merge unity/poc into your
  branch before handing back.
- Minor reversible choices: take a configurable default, record it in your
  DEV_LOG record. Do not ask the owner.
- <Interim hand-back: hand back at <ID>a (spec section 21) so the lead can
  merge it early, then continue to <ID>b on the same branch.>

Hand back: branch and head SHA; every command that ran with its counts and
hashes; what did not run or did not pass, plainly; defaults taken; any
amendment you need from the lead.
```

**Q3 and F2** are long-lived: launch Q3 when D1 merges and give it each merge's SHA to extend its harness; launch F2 after F0 merges with `--workers 4` (`--workers 2` when three or more agents are testing).

## 4. Worktree management

- **Create** (the lead, from `wt-lrs`, after the package's hard prerequisites have merged):
  ```
  git -C wt-lrs worktree add ..\wt-lrs-<id> -b unity/<id> unity/poc
  robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\<warm>\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-<id>\Library /E /MT:16
  ```
  `<warm>` is any worktree whose Unity editor is **closed** and whose Library is recent (normally `wt-lrs`, or the last checkpoint folder). **robocopy exit codes 0–7 are success**; 8 and above are failures. In PowerShell, check `$LASTEXITCODE -lt 8` rather than treating a non-zero code as an error.
- **Branch names (R17):** `unity/q1`, `unity/q2`, `unity/q3`, `unity/f0`, `unity/p10`, `unity/p11` (and every other existing `unity/*` name: check `git branch --list`) are taken. Use the index names: `unity/int0` (F0), `unity/q1kpi`, `unity/q3int`, `unity/u1a`/`unity/u1b`, `unity/u2a`/`unity/u2b`, `unity/u3b`, `unity/k2a`/`unity/k2b`. A b part may continue on the a part's worktree on a new branch cut from `unity/poc` after the a part merged.
- **One Unity editor per folder, ever.** Never open an editor in a worktree whose agent is running Unity; never copy a Library out of a folder whose editor is open.
- **Remove after merge** (once the merge is green and the agent has finished):
  ```
  git -C wt-lrs worktree remove ..\wt-lrs-<id>
  git -C wt-lrs branch -d unity/<id>      (only after it is fully merged; push it first if the owner wants it kept)
  ```
  If `worktree remove` refuses because of untracked build output, check `git -C wt-lrs-<id> status` first; remove with `--force` only when everything tracked is merged. The stale non-git `wt-lrs-q3` folder is not used.
- The checkpoint worktree **`wt-lrs-ckpt`** is permanent for the run: `git -C wt-lrs worktree add --detach ..\wt-lrs-ckpt <tag-or-sha>` once, then `git -C wt-lrs-ckpt checkout --detach <sha>` for each checkpoint.

## 5. Merge strategy

The lead merges **in `wt-lrs`, into `unity/poc`, with `--no-ff`**, one commit per package (or part), in the order of graph §9 as packages become ready.

1. **Review the hand-back** against the spec's definition of done and the merge gate (graph §9): the branch has merged current `unity/poc`; the agent's own counts; the DEV_LOG record; no file outside the spec's owned list changed (`git diff --stat unity/poc...unity/<id>`); for F1a, F1b and I1a read every frozen-file diff. A package that touched a frozen file it does not own is refused.
2. **Merge:** `git -C wt-lrs merge --no-ff unity/<id> -F <msgfile>`.
3. **Batch at most two packages with disjoint files** per suite run: merge both, then run one suite. If it is red, revert the second merge (`git revert -m 1 <merge>`), rerun, and find the culprit.
4. **Gates on the merge result, in order:**
   1. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1`: 0 errors;
   2. `dotnet test DotNet -c Release` (about 20 minutes; **run in the background** and keep reviewing the next hand-back meanwhile);
   3. **neutrality pins:** before F1, the both-off pins `8b898d115d8a05a7`, `5fa8991b92168385`, `66a55d5a67f2ad4e`; from F1 on, P1, P4, P8 always; P2, P3 after D1; P5 after C6; P9 after D7; P10–P13 after I1a; the stage 3 hash `4bd54b271857a803` (`PolicyRunTests`) always;
   4. the package's reconciliation and `*_SaveLoadParity` tests (they run in the suite; check they are there);
   5. the lead's **independent check**: rerun the package's headline acceptance test by name and look at its output (behaviour, not "a class exists");
   6. Unity only at checkpoints, or when a type `God.Game` uses changed and the agent did not hold the Unity token (then at the next checkpoint at the latest).
5. **After a green gate:** update DEV_LOG ("Current status", "Commits and branches", "Test results", the package's row), the index status column, then launch whatever the merge unblocked. Give Q3 the new SHA (P6 `Switch_Matrix` within an hour; a failure reverts the merge).
6. **Re-pins** happen only at the declared points (the Checkpoint B merge: F2's code and `Divinity.Enabled` on for god-only games; the save version bumps of contracts §17 and §22.8), each with a DEV_LOG row stating why every moved hash moved.
7. **A gate that fails twice** for the same package: revert to the last green head and send the package back with the failure.

## 6. Test cadence

| When | What | Where |
|---|---|---|
| every agent commit | the package's focused filters | agent's worktree |
| every hand-back | full `dotnet test DotNet -c Release` on the branch after merging `unity/poc`; compile check; Unity only with the token | agent's worktree |
| every merge (or batch of two) | compile check, full suite, pins, the lead's independent check | `wt-lrs` |
| within an hour of each merge after D1 | P6 `Switch_Matrix` and the growing scenario harness | Q3's worktree |
| each checkpoint | Unity EditMode and PlayMode, Windows build, smokes, `Unity_Agrees_With_Runner` | `wt-lrs-ckpt` |
| benchmark slots | `bench` (slot 1 on the F0 base; slot 2 on the frozen head) on a quiet machine | `wt-lrs` |
| final window | everything (section 10) | `wt-lrs`, `wt-lrs-ckpt` |

Unity runs take 45–100 minutes under editor throttling: run them at checkpoints, not after every merge.

## 7. Monitoring agents

- Background agents notify the lead when they finish; **do not poll them** and do not predict their results. Between notifications, merge, review and launch.
- To see progress without waiting: `git -C wt-lrs log --oneline unity/poc..unity/<id>` and the agent's DEV_LOG record on its branch (`git -C wt-lrs show unity/<id>:docs/stage4/DEV_LOG.md`).
- **Agents get cut off** (context or time). That is why every prompt says "commit as you go". If an agent hands back an interim report, stops early, or fails: **start a fresh agent from its record**, with the same template plus: "A previous agent worked on this branch. Read `git log --oneline unity/poc..HEAD` and your package's record in DEV_LOG on this branch, then continue from where it stopped; do not redo committed work." Uncommitted changes in its worktree are inspected first (`git status`, `git diff`) and either committed as WIP by the new agent or discarded on the lead's decision.
- An agent that reports a contract conflict gets an answer fast: an amendment row in contracts §20 (committed on `unity/poc`, then the agent merges `unity/poc`), or a ruling in LEAD_RULINGS. Never let an agent work around a frozen file.
- Check every claim: counts, hashes, "passes" (rerun one), "not run" stated plainly. An agent's report is not shown to the owner; the lead relays what matters.

## 8. Escalation rules (when to ask the owner)

Ask the owner **only** when a question is genuinely consequential and cannot be answered from the register, the briefs or the design: it would change an owner decision, retire a player-visible mode (for example Stage 3 mode), lose a Tier 1 item, push or merge to `main`, delete history, or spend the owner's money or accounts. Everything else: **take the default, make it configurable, record it** in DEV_LOG "Design decisions taken during implementation" (and in LEAD_RULINGS when it resolves a conflict between documents), update the owning document, continue (brief §52). The open questions in the master plan §7 already have defaults; do not ask them again unless something new makes one block work. When asking, ask once, briefly, with the default the run will use meanwhile, and keep working.

## 9. Known pitfalls

- **Unity Safe Mode.** A compile error in `Assets/God/Game` (which `dotnet` never compiles) puts the editor in Safe Mode and no test runs (the M5 CS0111 incident). Always run `AgentScripts/check-game-compile.ps1` first (0 errors), then open the editor and confirm a clean console.
- **Unity CLI** (`C:\Users\joesa\.unity\bin\unity.exe`): `unity open .`, poll `unity status` until ready; tests with `unity command --project-path . test run --mode editor --async-tests true` (then `--mode playmode`), then poll `Temp/pipeline_test_status.json` until `"status": "completed"` (the synchronous form times out after 30 s); build with `unity command --project-path . script run --file AgentScripts/BuildWindows.cs --entry BuildWindows.Run`; close with `unity close .`. Wait with the Monitor tool or a background until-loop, not foreground sleeps. Unity leaves `Assets/InitTestScene*.unity` files after PlayMode: never commit them.
- **Never run the built player windowed** in automation: only `-batchmode -nographics`. Never touch the registry; computer use was denied.
- **PowerShell 5.1:** no `&&`/`||` (use `; if ($?) { … }`); here-strings close with `'@` at column 0 and still break quoted commit messages; avoid `2>&1` on native executables; `Set-Content` defaults to ANSI (pass `-Encoding utf8`). The Bash tool (Git Bash) is often simpler for git and file work.
- **Commit messages:** ASCII only, written to a file (scratch folder) and committed with `git commit -F <file>`, ending with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Never commit `docs/stage4.zip`** (untracked in `wt-lrs`) or any build output. Never `git add -A` or `git add .`; add paths explicitly and run `git status` first.
- **Auto mode may refuse destructive git** (`checkout -- <file>`, `reset --hard`, `clean`). Do not fight it: commit the state (a WIP commit on the package branch) and revert with a new commit, or ask the owner to run the command.
- **The stash is shared** by every worktree: never bare `git stash`; prefer a WIP commit.
- **Docs encoding:** DEV_LOG is UTF-8 without a byte-order mark, CRLF; keep it so (`9cbef06`).
- **Throttled editors:** background Unity runs are slow; start them as early as possible and never wait idle.

## 10. Checkpoint procedure (A–G and later)

At each checkpoint of master plan §5 (graph §10), on the `unity/poc` commit where its content completed:

1. Gates green on `unity/poc` (section 5) and the checkpoint's rows in [RELEASE_ACCEPTANCE_CHECKLIST.md](RELEASE_ACCEPTANCE_CHECKLIST.md) run headless.
2. `git -C wt-lrs-ckpt checkout --detach <sha>`; Library warm (robocopy from a closed editor's folder if needed); compile check 0 errors.
3. Unity: open, clean console, EditMode, PlayMode, `Unity_Agrees_With_Runner`; record counts.
4. Windows build (batch), then the smokes from `Builds\Windows\GodOfTheIsland.exe`, all `-batchmode -nographics`:
   - stage 3: `-godsmoke 60000 -godseed 2` → Crimson wins at tick 20161, hash **`4bd54b271857a803`**, invariants ok, 0 exceptions;
   - population: the same with `-godpopulation` → exit 0, invariants ok, new hash recorded (it moves at F0 and at the Checkpoint B re-pin);
   - god mode (from U1a, R29): `-godonly -godpopulation` → exit 0, invariants ok, hash recorded.
   Copy the build to `Builds\civdiv-<x>\` (never committed). `unity close .`.
5. Tag: `git -C wt-lrs tag civdiv-<x> <sha>`.
6. DEV_LOG: test results, hashes, what ran and what did not; the checkpoint's status in the index.
7. **Push** `unity/poc` (and the tag): `git -C wt-lrs push origin unity/poc` and `git -C wt-lrs push origin civdiv-<x>`. The owner authorised pushing **tested** changes to `unity/poc` and to feature branches; **never `main`**, never a merge into `main`. "Tested" means every gate of this checkpoint ran and passed and is recorded in DEV_LOG.
8. Merges continue in `wt-lrs` while steps 2–4 run in `wt-lrs-ckpt`.

## 11. Cut plan

Apply graph §7 at each mark (by position, section 2 step 4). The drop ladder: industry beyond the slice → K1 rights and auctions → K2b → D7 → C10 → U5 → C2 → C7b → U1b, U2b, D4b → U3b. **Never cut** the Tier 1 set or the final window; the industrial slice is never given a slot or merge time that a D5 rung needs (D6.18). A dropped package goes to the continuation list in DEV_LOG "Outstanding work", in the order of graph §7 row H15; nothing leaves the design. Record every cut in DEV_LOG "Deviations from plan".

## 12. Final release procedure (the protected window, then each later release)

Graph §8 (H15–H18) for the first release; the same steps for every later release in the continuation and for the final "finished game" release:

1. **Freeze:** no new packages; only merges whose gate is already running and passes within 30 minutes; at most two background agents on continuation branches (`--workers 2`, no Unity, nothing merged); fixes only. A fix that fails its gate twice: revert to the last green head.
2. Q1 benchmark slot (quiet machine).
3. Compile check; full `dotnet test DotNet -c Release`; every landed pin; stage 3 hash; Q3 replay and save/load on the `civdiv_*` (and `ind_*`) scenarios.
4. Unity EditMode and PlayMode on the frozen head in `wt-lrs-ckpt`; `Unity_Agrees_With_Runner`.
5. Windows build; stage 3, population and god-mode smokes (section 10 step 4).
6. Tags (`civdiv-d`, `civdiv-e`, `civdiv-f` on the commits where their content completed; later `civdiv-g`, …); builds copied under `Builds\`.
7. Documentation written **from what actually ran**: DEV_LOG (status, results, benchmarks, temporary mechanisms, outstanding work and the continuation list), `docs/STAGE_4_HANDOVER.md` §1–2, the ACCEPTANCE status tables, the index status column, this runbook's section 13 if the continuation plan changed. The Q4 Sonnet helper may draft documentation; the lead checks every figure.
8. Push `unity/poc` and the tags (section 10 step 7). Report to the owner: what is playable, which tests and runs happened with counts and hashes, what did not run or did not pass, and what comes next.

## 13. Continuation after H18 (R37)

The owner asked to keep working until a finished playable game: **the run does not stop at H18.** After the first release:

1. Unfreeze and continue the **continuation order** (graph §9): I1a (if not merged), I1, C2, D7, U5, C10, I2 ∥ I4, K1, K2a, I3, I6, Q3 `ind_slice` → **Checkpoint G** (about H25), Q1 `0030` and `0041`, I5, K2b. The save version bump for the industrial surface is applied at G (contracts §22.8: G → 11 if I1a did not merge before the freeze).
2. Same rules as before: concurrency limits, one Unity token, merges with gates, checkpoints with tags and pushes; Unity slots may now go to I6.
3. After G, keep going towards a **finished playable game** (master plan §5): the remaining Tier 2 items, then Tier 3 in the reverse order of the cut ladder (C9 justice, C4/M9 soldiers as people, C8 banking, C5 politics, advanced religion and M11 culture, more miracles, deeper analytics), then U6 (tutorial rewrite and presentation polish) and sandbox balance (M14). Each completed vertical slice is a tagged, pushed release (`civdiv-h`, …) via section 12.
4. Write new package specs for post-G work in `docs/civdiv/DEVELOPMENT_PACKAGES/` in the same format (or delegate the spec to an Opus agent), add their rows to the index and graph, then launch.
5. Stop only when the owner says so, or when the game is finished and released by section 12; before any stop, DEV_LOG "Current status" and "Instructions for the next session" must say exactly where to resume, and [NEXT_SESSION_PROMPT.md](NEXT_SESSION_PROMPT.md) must still be correct.


---

## 14. Unification run addendum (D9)

**Applies from 28 Nov 2026** to the Unification run (owner decisions D8, **D9** and **D10**). Sections 1–13 above still govern the lead's role, worktrees, merge strategy, test cadence, escalation, pitfalls and release procedure; this addendum replaces only the package set, the waves, the checkpoints and the run-specific rules. Where it conflicts with sections 1–13, this addendum wins for the Unification run. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../unification/LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 ([OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md)), rulings R72–R84 ([LEAD_RULINGS.md](LEAD_RULINGS.md)), [UNIFICATION_DESIGN.md §D9](../unification/UNIFICATION_DESIGN.md), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](SHARED_ARCHITECTURAL_CONTRACTS.md), [UNIFICATION_PACKAGE_INDEX.md](../unification/UNIFICATION_PACKAGE_INDEX.md) (packages, prerequisites, waves, critical path, merge order, checkpoints, cut plan), [CATALOGUE_MANIFEST.md](../unification/CATALOGUE_MANIFEST.md), [D9_CODE_CONFLICT_AUDIT.md](../unification/D9_CODE_CONFLICT_AUDIT.md), the package specs in [../unification/packages/](../unification/packages/), and the acceptance rows in `docs/stage4/ACCEPTANCE_TESTS.md` "D9. Unification acceptance". The copy-ready session prompt is [NEXT_SESSION_PROMPT_UNIFICATION.md](../unification/NEXT_SESSION_PROMPT_UNIFICATION.md). The Industry run (`NEXT_SESSION_PROMPT.md`, I1a onwards) resumes only after Checkpoint UB plus credible autonomous primitive viability, or an explicit owner decision (D9.14).

### 14.1 What the lead does differently

1. **Position is found from git, DEV_LOG and the worktree list** (section 2 step 2), not from the clock. UN0 may already be running or merged: `git worktree list` may show `wt-lrs-un0`; `git log --oneline unity/poc..unity/un0`; read its DEV_LOG record on the branch.
2. **Package ids, branches and worktrees:** `unity/<id>` (lower case) and `wt-lrs-<id>` for UN0–UN11a, UN6t (one branch for UN6t-a and UN6t-b), UN8a, UN8b, UNP, UB1, UB2, UN6t2, UN10b, UN11b, UC1. The specs in `docs/unification/packages/` are the agents' instructions; where a spec and the index disagree, the index wins.
3. **No Fable, ever.** Opus: UN0, UN2, UN3b, UN4, UN6, UN6t, UN7, UN8a, UN9, UNP, UB1, UN8b, UN6t2. Sonnet: UN1, UN3a, UN5, UN10a, UN11a, UB2, UN10b, UN11b, UC1 (Opus review).
4. **Frozen surfaces** belong to UN0 and the lead (§23.0, §23.15). A package that needs a missing seam sends an amendment request; the lead commits an amendment (and a ruling when it resolves a conflict), the agent merges `unity/poc` and continues. The save version bump and every re-pin are the lead's alone.

### 14.2 Waves (hours are planning estimates from H0; position by merges, not by the clock)

| Wave | Window | What runs | Gate to leave the wave |
|---|---|---|---|
| **W0** | H0.0–H4.6 | **UN0** only (H0.3–H4.0; merge gate H4.0–H4.6). Meanwhile the lead runs the baseline suite and the compile check, applies any pending amendments and, if the machine is quiet, a benchmark baseline | merge 1 of the index §6: U-P1 exact, U-P2, U-P3 static, `Wares_LayoutNeutral`, `Ledger_WideningNeutral`, `Unified_GoodsNoTitle`, U-C1–U-C6, Unity batch compile and EditMode, bench within 3 %, §23 recorded as a ruling, **no save bump** |
| **W1** | H4.6–H8.6 | UN2, UN6, UN1, UN3b, UN3a (then UN4), UN6t-a | UN3a (H7.4), UN1 (H8.5), UN2 (H9.0), UN6 + UN3b (H9.5) merge in that order |
| **W2** | H8.1–H11.6 | UN7, UN9, UN5, UN8a, UN4 → UN10a, UN6t-b training from H9.5 | UN4 (H10.8), UN9 + UN8a (H11.6), UN5 (H11.9), UN7 (H12.3) |
| **W3** | H11.1–H15.4 | UN11a (preparation then gates), UNP if a slot is free (first to cut), UN10a, UN6t-b (freeze H14.0, held-out evaluation H14.0–H15.0, merge H15.0–H15.4); UB1 only if UA-learned is safe (default: not started) | **UA-mech** (H13.0–H13.5), UN10a (H15.2), UN6t (H15.4) |
| **W4** | H15.4–H18.0 | UA-learned gate H15.4–H16.0; **protected final window H16.0–H18.0**: no new package; fixes for red gates only; build, full suite, pins, replay, reports | UA-learned, final release procedure (section 12) |

UB (UB1, UB2, UN8b, UN6t2, UN10b, UN11b) and UC (UC1) never start in this run unless UA-learned is green before H14 (index §8, first row). The critical path is UN0 → UN2 → UN3b/UN6 merged → **UN6t-b training** (H9.5–H14.0) → held-out evaluation (H14.0–H15.0) → artifact merge (H15.4) → UA-learned gate → final window. Slack is about zero: the training freeze at H14.0 and the window at H16.0 are fixed.

### 14.3 Concurrency

- **At most 6 package agents** at once; the lead and its `wt-lrs-ckpt` are not counted. Use the free slots in the order of the index §4 table; UNP and UB packages take a slot only when no UA package is waiting.
- **Full `dotnet test` suites: at most 3 at once**, the lead's merge suite first. UN6t-b's training rollouts use `god-train` and the runner (not the suite) with `--workers` so that the lead's merge suite still gets the machine (default 8; ask before raising).
- **Unity runs: at most 2** (one package agent plus the lead's `wt-lrs-ckpt`). **Unity token order** (one package agent at a time): UN0 (about H3.4–H4.0, batch compile and EditMode only), UN9 (H10.6–H11.1, batch smoke of both hosts), UN10a (H11.6–H14.8), then UN10b if UB runs. One editor per folder; close it when done; never run a built player windowed; the compile check (`AgentScripts/check-game-compile.ps1`, 0 errors) precedes every Unity open.
- Agents' commits and records on their branches; no agent pushes or merges into `unity/poc`; the lead merges with `--no-ff` in `wt-lrs`.

### 14.4 Merge gates

Merge order and per-merge gates are index §6 (rows 1–14). **Standard gate for every merge** (section 5, adapted): the branch has merged current `unity/poc`; the agent's counts are in its DEV_LOG record; `git diff --stat unity/poc...unity/<id>` shows only the spec's owned areas plus declared flagged hunks; `AgentScripts/check-game-compile.ps1` 0 errors; `dotnet test DotNet -c Release` green; **U-P1, P1, P4, P8 and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` unchanged with `Unification.Enabled = 0`** (byte-identical); the package's headline test re-run by the lead. At most two packages with disjoint files per suite run. A red baseline is fixed before anything merges; a gate failing twice reverts to the last green head. Additional pins by package (index §9): U-P2 and U-P3 static at UN0, U-P3 runtime at UN9, U-P4 at UN2 (re-run UN6, UN9, UN11a), U-P5 and full U-P6 at UN11a. Hashes move only at declared re-pins (the save bump at UA-mech is one), each recorded in DEV_LOG with why every moved hash moved. A package that touched a frozen file it does not own is refused. The lead reads every frozen-file hunk of UN0, every gate hunk of UN9 (against audit §8) and the thresholds-file commit order of UN6t.

### 14.5 Checkpoint procedure for UA-mech and UA-learned

Both checkpoints use section 10's steps with these specifics; the checkpoint's content is index §7.

**UA-mech (H13.0–H13.5), tag `unif-ua-mech`**
1. Merges 1–10 of the index §6 are in; the mechanical matrix runs headless by UN11a (EC-01, EP-01…EP-05, CF-01, QA-01, QA-02, CATALOG-1, MOTOR-1/2, NEED-1, ECON-1 mechanical, ECON-2, U1, U-P1…U-P6, invariants U-C1…U-C6); the 72-person unified start runs 20 days deterministically on seeds 1–3 with the **untrained** brain (outcome reported, not judged). Evidence from scripted primitive streams and fixtures is labelled `Fixture` and is never presented as autonomous behaviour.
2. In the UN11a mechanical merge commit the lead bumps `SaveFile.CurrentVersion` 6 → **7** (once) and re-pins in DEV_LOG; the v7 round trip passes and v6 is refused.
3. The lead flips the single menu flag of UN9 so that "Living island (you as a god)" starts the unified game (it shows the UNTRAINED badge until UA-learned).
4. Compile check 0 errors; Unity EditMode in `wt-lrs-ckpt` if the token is free (not a UA-mech gate); no Windows build is required at this checkpoint.
5. Tag `unif-ua-mech`; DEV_LOG (results, hashes, what did not run); **push** `unity/poc` and the tag (the owner authorised pushing tested changes to `unity/poc` and feature branches; **never `main`**).

**UA-learned (H15.4–H16.0), tag `unif-ua` only if green**
1. Merges 11–14 are in (UNP only if neutral; UN10a; UN6t; UN11a learned part). The lead **re-runs two held-out seeds independently**, checks that the thresholds file's commit predates the evaluation commit, and that the artifact hash, `Obs`/`Act` schema hash and catalogue hash match `unity/poc`.
2. `Brain.Kind = 1` is set in `unif_*` **only if TR-01 passed** against the predeclared thresholds; otherwise `Brain.Kind = 2` (labelled Fallback) and the checkpoint is reported "UA-learned: not passed".
3. Section 10 steps 2–4 on the frozen head in `wt-lrs-ckpt`: compile check; Unity EditMode and PlayMode (UN10a's screenshots, badge showing the artifact or UNTRAINED FALLBACK); the **unified Windows player build** (`-batchmode -nographics` only); smokes: Stage 3 `-godsmoke 60000 -godseed 2` hash `4bd54b271857a803`, population, god-only, and a unified game booting with the artifact; copy the build to `Builds\unif-ua\` (never committed).
4. `docs/runs/UA_REPORT.md` with separate **mechanical**, **learned/autonomous** and **long-run viability** columns, a "did not run" section and the training verdict first; DEV_LOG results; the lead checks every figure.
5. Tag `unif-ua` **only if every UA-learned item is green**; if anything failed, apply no green tag (use `unif-ua-incomplete` (L) on the tested commit), say so plainly, and push the tested `unity/poc` and package branches (never `main`). Then the protected window (section 12) runs to H18.0.

### 14.6 Training-honesty rule (never cut)

A trained-founder claim is valid only if: the numerical survival and competence thresholds (T1–T7) were **committed with their hash before any held-out run**; evaluation used **held-out seeds (900001–900020) and held-out layouts** the trainer cannot load; **untrained and frozen controls** were run; the **artifact is versioned with a content hash** (schema and catalogue hashes verified at load); evaluation was teacher-free, god-free and without a path oracle; the result is reported as it came. A fallback brain is labelled `Brain.Kind = 2` in the save, the HUD badge and every report and is **never counted as trained**; a scripted controller or the old `Eat/Flee/Build` rules behind primitives is never called trained. **A failed TR-01 means UA-learned is not green**: keep the artifact, metrics and cause; do not adjust thresholds after seeing results, do not re-evaluate until it passes, do not move the freeze. Social outcomes (Hawk/Dove proportions, cooperation, growth, new settlements) are measured, never thresholds.

### 14.7 Cut plan

[UNIFICATION_PACKAGE_INDEX.md](../unification/UNIFICATION_PACKAGE_INDEX.md) §8 is the cut plan by hour (H6.0 UN0 not merged: UNP cut and UN10a shrinks; H9.5: UN3b drops `Play`/`Fun`, UN8a drops `Equip`, UN7 drops claims communication; H11.0 training not started: S7–S8 dropped from V1; H12.5: UN5 families simplified, UN7 hold-out only; H14.0 artifact freeze; H14.5 UN10a behind a flag; H15.0 held-out evaluation limited to the 20 small seeds (T6 "not run" means UA-learned not green); H16.0 fixes only). **Never cut:** training honesty, determinism (U-P1, Stage 3 pins, save and replay exactness) and the protected final window H16.0–H18.0. A dropped package or stage goes to DEV_LOG "Outstanding work" and "Deviations from plan"; nothing leaves the design.

### 14.8 Scale rules (owner decision D10; every UA package, checked at review)

Board radius, hex count, settlement corners per tribe, tribe count and persons per tribe are **configuration, never constants** (generators tested at radius 4 and 6); perception and witnesses use `ISpatialIndex` (no all-pairs scans); executor, interaction resolution and materialisation cost O(active persons + changed nodes + open sites) per tick; brain inference ≤ 20,000 multiply-accumulates and ≤ 4 KB state per person, with decisions only when a primitive completes, fails or is interrupted; staggering and level of detail depend on sim state, never on the camera; no per-tick allocation in hot paths; views use instancing and distance LOD. Benchmarks at 72, 600 and 1,200 people are recorded from UA on (UN11a; baseline before trained inference about 1,080 ticks/s at 500 and 384 ticks/s at 1,000 people). The UC gate (lead default) is 1,200 people with trained inference at ≥ 100 ticks/s headless and ≥ 20 ticks/s in the player; UNP and UC1 own it, and neither delays UA.

### 14.9 Continuation

After UA-learned, if the window allows and UA-learned is green, UB packages launch in the order of the index (UB1 and UB2 first, then UN8b, UN6t2, UN10b, UN11b) under the same rules; UC1 after UB. If the run is cut off, DEV_LOG "Current status" and "Instructions for the next session" and [NEXT_SESSION_PROMPT_UNIFICATION.md](../unification/NEXT_SESSION_PROMPT_UNIFICATION.md) must say exactly where to resume.
