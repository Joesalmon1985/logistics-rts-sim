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
