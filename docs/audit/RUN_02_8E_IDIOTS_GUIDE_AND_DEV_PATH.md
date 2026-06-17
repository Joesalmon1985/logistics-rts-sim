# Absolute Idiot's Guide: Human Pass Attempt + What To Do About the Red Tests

**Audience:** You have never played this scenario before, you are not sure what "ECON_BOT_002" means, and you want to know whether the game is broken or the bot is broken.

**Short answer:** The **automated bot test is failing**. That does **not** mean you must stop all work. It **does** mean you should not pretend the economy milestone is "done" until someone proves the scenario is fair — either by **you** playing it, or by a **headless oracle script** doing the same thing.

---

## Part 1 — What you are actually trying to do

There is a headless test called **`ECON_BOT_002`**. It starts a specific economy duel (seed **28001**, map **24×24**, **800 ticks**) and runs two dumb expansion bots. Each player must reach, **from the same starting point a human would get**:

| What | Minimum (per player) |
|------|----------------------|
| **Buildings** (b) | 5 new ones (not counting HQ) |
| **Roads** (r) | 10 new road pieces |
| **Deliveries** (d) | 10 completed material runs |
| **Stall** | ≤300 ticks stuck doing nothing useful |
| **Score** | must go up |

Right now the **bots** only reach about **1–2 buildings** even after thousands of ticks. Roads and deliveries look fine. So the test fails on **buildings**.

**Your job in a human pass attempt:** Play **both sides** (p1 and p2) from the **exact same start** and see if **you** can hit those numbers by tick 800. That tells us whether the test is fair or impossible.

You are **not** trying to beat the bot. You are trying to answer: *"Can a human (or a hand-written command script) pass the same gates?"*

---

## Part 2 — Before you touch anything

### Install / paths

You need **Godot 4.5.2** on Linux (this is what CI and docs assume):

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
cd /home/joe/Projects/WideRTS
```

If Godot is elsewhere, change `GODOT=` — but keep `GODOT_USER_DATA_DIR` pointed at the project-local folder (avoids crashes in sandboxes).

### What "cheating" means here (don't do this)

These would **invalidate** your attempt:

- Extra starter buildings or roads not in the proof scenario
- Changing tick limit, thresholds, or economy rules "just to pass"
- Playing a different map/seed
- Using debug tools that mutate state outside the normal command pipeline

The harness **Reset Proof Scenario** button loads the real deal. Use that every time you start fresh.

---

## Part 3 — Easiest path: watch the oracle do one building first

Before clicking anything, prove the **scenario is not completely dead** by running the committed one-building script (takes ~5 seconds):

```bash
timeout 120s $GODOT --headless --path godot_project \
  --script res://sim/tools/run_economy_human_proof_oracle.gd -- \
  --plan=res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json
```

**You want to see:** `ORACLE PASS` and `EXIT: 0`.

**What that means:** From the exact test start, **legal commands exist** that place a woodcutter, connect roads, get deliveries, and **finish one building** for player 1. So the map is not inherently unbuildable.

**If this fails:** Stop. Something regressed in the sim or fixture. Fix that before blaming yourself.

---

## Part 4 — Interactive play (the actual "human pass" attempt)

### Open the scene

In Godot 4.5.2, open:

```
godot_project/client/scenes/economy_human_proof_demo.tscn
```

Press **F6** (Run Current Scene) or set it as main scene and run the project.

### Read the screen (top to bottom)

1. **Instructions panel** — goal and dual-turn rules.
2. **Proof gates panel** — live `b / r / d` vs targets for **p1 and p2**.
3. **Tick counter** — you have **800** ticks total; don't idle forever.
4. **Active player** — **p1** or **p2**. You must play **both** sides (this mirrors the bot test).

### How a "turn" works (important — easy to mess up)

This is **not** normal RTS "I click everything whenever I want."

Each simulation step is:

1. **p1** picks one command → Submit  
2. **p2** picks one command → Submit  
3. The game **ticks** once (time advances)

So you alternate: p1 road, p2 noop, tick … p1 building plan, p2 noop, tick … etc.

If you forget whose turn it is, look at **Active player** in the status line.

### Buttons you actually need

| Button | What it does |
|--------|----------------|
| **Reset Proof Scenario** | Start over from tick 0 |
| **Submit Command** | Do the highlighted command in the list for the active player |
| **Dual Noop 10 / 50** | Both players pass; time advances (use while waiting for construction/delivery) |
| **Load Oracle Plan** | Runs the one-building reference script inside the UI (sanity check) |
| **Export Attempt** | Saves your commands to `training_runs/human_proof/` for replay |

You can ignore screenshot/bot-step buttons for a pass attempt.

### Map layers (turn these on)

- **Toggle Forest D** — dark green hexes with `D:80` etc. are forests; **woodcutter** goes on forest.
- **Toggle Roads** — yellow lines; construction sites need a road **next to** them for carriers.
- **Toggle Logistics** — see carriers moving stuff.

Click a hex → **inspector** shows site state, materials delivered/required, block reasons.

### A minimal sensible opening (not the only way)

This is **guidance**, not a walkthrough cheat code:

**For each player (p1 near top-left HQ, p2 near bottom-right):**

1. Extend roads from the HQ stub toward a **forest** hex (woodcutter).
2. **Place building plan** → woodcutter on forest (must be legal in command list).
3. After the site exists, place roads **touching the construction site** if the list offers "Connect site…" style roads.
4. **Dual Noop 50** repeatedly until materials arrive and builder work completes (watch inspector + gates `d` and `b`).
5. Repeat for more building types (storehouse, forester, sawmill, workshop — whatever the command list allows near your road network).
6. Do the same for the **other** player when it's their command turns.

**Buildings that count toward `b`:** completed structures only (not HQ, not half-built sites).

**Roads that count toward `r`:** new segments/nodes **after** the starting baseline (the HQ spine at tick 0 is already counted in baseline — you don't get credit for it).

### How you know you're winning

Watch the **Proof gates panel**:

```
p1  b 3/5  r 12/10  d 45/10  ...
p2  b 2/5  r 11/10  d 38/10  ...
```

You need **both** players to hit all minimums **before tick 800**.

### When you give up or finish — export

Click **Export Attempt**. Note the file path printed (under `training_runs/human_proof/`).

Verify it replays headlessly:

```bash
timeout 60s $GODOT --headless --path godot_project \
  --script res://sim/tools/run_economy_human_proof_replay.gd -- \
  --replay=training_runs/human_proof/econ_bot_002_human_attempt_<timestamp>.json
```

Write down: final tick, final b/r/d per player, and **where you got stuck** (e.g. "couldn't place forester", "ran out of road cap", "UI had no storehouse command").

---

## Part 5 — What your result means (pick one letter)

| Outcome | You observed | What it implies |
|---------|----------------|-----------------|
| **A** | Oracle can't even finish 1 building anymore | Scenario or sim regression — fix engine/setup first |
| **B** | Oracle works, but UI won't let you submit the same commands | **UI/command list bug** — scenario may be fine |
| **C** | You (or oracle) can build several things, bots still fail | **Bot AI/shortlist bug** — scenario likely OK |
| **D** | Nobody can reach 5 buildings by 800 despite skilled play | **Scenario too tight** — needs design/threshold review (user approval to change) |
| **E** | You passed but test still fails with same actions | **Test/metrics bug** — investigate counting/baseline |

Current engineering guess (June 2026): **C** for full 5-building bot failure; **not A** for one-building (oracle passes).

---

## Part 6 — Absolute idiot's FAQ

**Q: The command list is empty / tiny.**  
A: Wrong active player, or no legal actions this tick. Try Dual Noop, toggle layers, click HQ/forest. If oracle works but UI doesn't → outcome **B**.

**Q: I placed a building but `b` didn't go up.**  
A: `b` counts **completed** buildings. Wait for deliveries + builder work. Use Dual Noop 50.

**Q: Construction site says BLOCKED.**  
A: Usually missing road adjacency or materials. Connect a road to the site hex; check inspector delivered/required.

**Q: Do I control both players fairly?**  
A: Yes. Bots do too. You aren't supposed to only sweat p1.

**Q: Can I lower 5 buildings to 3 to merge?**  
A: **No**, not without explicit project approval. That would be weakening the test.

**Q: The bot test failed — is the branch trash?**  
A: No. Much of the branch is test harness + runtime work. Red **ECON_BOT_002** blocks **economy bot milestone closure**, not all development — see Part 7.

---

## Part 7 — How to develop this product while tests are red

### What is actually red (don't lump everything together)

| Check | Typical status | Blocks what |
|-------|----------------|-------------|
| `ECON_BOT_002` | **FAIL** (buildings) | "Dual bot expansion proof" milestone |
| `ECON-SMOKE-006` / replay hashes | **FAIL** | Smoke closure / hash pinning |
| `REP-SMOKE-001` | **FAIL** | Replay export/import identity |
| Kernel + most economy tests | **PASS** | — |
| `econ_human_proof` (4 tests) | **PASS** | Harness health |
| Bot ladder B001/C001/D001/E001/F001/G001 | **PASS** | — |
| `verify_failure_modes.gd` | **PASS** | — |

**Rule:** A red **Tier 2 closure** gate means **do not claim RUN_02.8 bot economy is done**. It does **not** mean freeze the entire repo.

### Safe work streams while ECON_BOT_002 is red

**Green light (keep going):**

- Human proof harness, oracle scripts, UI legibility for construction
- Docs, test runtime, parallel test infra (02.8c)
- Kernel / logistics / construction **unit** tests that don't assume bot passes 002
- Visual review tooling (non-authoritative client)
- Python training tooling that does **not** assume 002 green (per neural training gates)

**Yellow light ( proceed with evidence ):**

- Bot changes — only if you compare against **oracle/human** scripts, not "bot passes bot test"
- Hash re-pinning — only after deterministic behaviour documented
- Scenario tweaks (tick budget, map, costs) — needs human/oracle pass/fail data first

**Red light (don't do without approval):**

- Lowering 002 thresholds or deleting/skipping the test
- "Fixing" smoke hashes by re-running until they match
- Declaring merge-ready / milestone complete on failing Tier 2 checklist
- Embedding authoritative rule changes to make **only** the bot pass

### Recommended daily dev loop

1. **Before economy bot work:** run a small green slice (fast feedback):

   ```bash
   TEST_FILTER=econ_human_proof timeout 120s $GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
   TEST_FILTER=econ_bot_b001 timeout 60s $GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
   ```

2. **When touching bots:** also run 002 — expect fail until fixed; **record b/r/d** in your notes:

   ```bash
   TEST_FILTER=econ_bot_002 timeout 360s $GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
   ```

3. **When claiming scenario knowledge:** extend oracle JSON or do human pass — **not** only bot tuning.

4. **Before any PR claiming closure:** full checklist in `docs/verification/local_verification.md` Tier 2.

### How branches / stashes fit in

- **`feature/run-02-8c-test-runtime-efficiency`** — harness + runtime; 002 may stay red.
- **02.8d bot WIP** may live in `git stash` (`02.8d-wip-bot-chain`) — pop onto a **separate bot branch**, not mixed into harness PRs as `fix(wip)`.

### Definition of "done" for the failing-test era

**Done for human-proof track:** harness works; oracle one-building passes; you attempted full pass or extended oracle toward b≥5; outcome letter assigned.

**Done for bot economy track:** `ECON_BOT_002` exit 0 **twice**; regression ladder green; smoke hashes fixed or justified; Tier 2 checklist green.

Those are **different finish lines**. Don't merge them in conversation or PR titles.

### Where to read more (less idiot, more contract)

| Doc | Purpose |
|-----|---------|
| [`RUN_02_8E_HUMAN_PROOF_SCENARIO_CONTRACT.md`](RUN_02_8E_HUMAN_PROOF_SCENARIO_CONTRACT.md) | Exact numbers, seed, baselines |
| [`RUN_02_8E_HUMAN_PROOF_PLAYTEST_GUIDE.md`](RUN_02_8E_HUMAN_PROOF_PLAYTEST_GUIDE.md) | Shorter playtest checklist |
| [`RUN_02_8E_HUMAN_PROOF_REPORT.md`](RUN_02_8E_HUMAN_PROOF_REPORT.md) | What 02.8e actually verified |
| [`RUN_02_8D_CLOSURE_REPORT.md`](RUN_02_8D_CLOSURE_REPORT.md) | Why 002 still fails (bot diagnosis) |
| [`docs/verification/local_verification.md`](../verification/local_verification.md) | Copy-paste commands |

---

## Part 8 — One-page cheat sheet (print this)

```
GOAL: Both players b≥5, r≥10, d≥10 by tick 800 (seed 28001)

1. export GODOT=... GODOT_USER_DATA_DIR=.../godot_project/.godot_user
2. Run oracle one-building → must PASS
3. Open economy_human_proof_demo.tscn → Reset Proof Scenario
4. p1 cmd → p2 cmd → tick (repeat)
5. Roads to forest → woodcutter plan → connect site roads → noop wait → complete
6. Repeat for both players; more building types
7. Export attempt → replay verify
8. Record outcome letter A/B/C/D/E

RED TEST ≠ STOP ALL WORK
RED TEST = DON'T CLAIM BOT MILESTONE DONE
USE ORACLE/HUMAN TO SEPARATE SCENARIO vs BOT vs UI BUGS
```

---

*Last updated: RUN_02.8e (2026-06-17). Update this doc when oracle reaches b≥5 or a documented human pass/fail attempt completes.*
