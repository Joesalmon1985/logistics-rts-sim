# RUN_00 — Architecture Decision Ratification

Documentation-only pass that ratifies the human architecture decisions and propagates them across all rules, design, architecture, testing, and run documents. No gameplay, Godot scene, or Python code was written.

## Decisions ratified (all now Accepted)

| ID | Decision |
|----|----------|
| DDR-026 | **Architecture pivot:** the authoritative runtime is a pure **Godot/GDScript simulation core**; Python is training/tooling only and is never embedded in the mobile app. |
| DDR-027 | Authoritative replay-hashed state uses **integers / fixed-point** scaled integers — no floats. |
| DDR-028 | One long run preferred for RUN_02/03/07; **one controlled A/B continuation** allowed only at a coherent acceptance boundary, after committing/pushing the subset and producing a gap report. |
| DDR-029 | The economy, military, and hybrid all use the **same shared axial hex map** from the RUN_01 kernel; no parallel/square grid. |
| DDR-030 | Multiplayer is **host-authoritative first**; lockstep is a possible future optimisation only. |
| DDR-031 | The Hybrid Game has **no credits**; hybrid construction/production/repair/research are ware/logistics only. (Trial military may still use credits standalone.) |

Two new follow-on decisions were opened by the pivot (non-blocking for RUN_01 docs):

| ID | Decision | Decide at |
|----|----------|-----------|
| DDR-032 | Godot-compatible **headless test framework** (custom runner / GUT / gdUnit4) | Start of RUN_01 |
| DDR-033 | **Python ↔ Godot-headless** training transport | RUN_06 |

## Architecture pivot summary

Previously the docs described Python as the authoritative simulation and Godot as a thin viewer, leaving an unresolved "how does Python run on mobile" risk (old DDR-026). That is now resolved by inverting the model:

- The **authoritative simulation core is pure GDScript** (`godot_project/sim/`), headless-capable, deterministic, integer/fixed-point, separate from UI/rendering.
- The **Godot visual client/UI** (`godot_project/client/`) is non-authoritative: it renders snapshots and submits commands.
- **Python** (`python_tools/`) is for training, analysis, and tooling only; it drives the core by running Godot headless. It is never the shipped runtime and is never embedded in the mobile app.
- Wording is consistently "Godot/GDScript simulation core is authoritative" — never "Godot is authoritative."

## Documents changed

- **Cursor rules:** renamed `03-python-sim-authority.mdc` → `03-godot-sim-authority.mdc` (content rewritten); updated `00-project-identity`, `01-autonomous-run-policy` (added A/B fallback), `02-test-first-development` (Godot headless harness), `04-godot-boundary` (UI vs core), `08-neural-training-gates`, `09-mobile-performance`.
- **Architecture:** renamed `01_python_sim_authoritative_core.md` → `01_simulation_core_authoritative.md` (rewritten with the `godot_project/` layout); updated `00`, `02`, `03` (adapter contract), `04` (Python drives Godot headless), `05` (integer/fixed-point; GDScript hash), `06` (data under `sim/data`).
- **Design:** `00` brief, `02`/`03` authority + shared map, `04` no credits, `05` catalogue note, `06` rendering model, `09` host-authoritative, `10` glossary, `11` decision register (all six DDRs accepted + DDR-032/033 added + status summary).
- **Testing:** `00` (harness), `03` (viewer plan), `04` (training transport), `05` (host-authoritative sync).
- **Runs:** `RUN_01` (retitled "Godot/GDScript Core Simulation Kernel"; GDScript layout, harness, shared map, integer/fixed-point), `RUN_02`/`RUN_03` (shared map, GDScript, no-UI), `RUN_05`/`RUN_10` (in-process core, no Python transport), `RUN_06` (Python drives Godot headless), `RUN_07` (no credits), `RUN_12` (host-authoritative); minor scope-wording fixes in `RUN_00`, `RUN_04`, `RUN_08`.
- **Root:** `README.md`, `.gitignore` (`godot_project/.godot/`).

## Consequences for RUN_01

RUN_01 is now the **Godot/GDScript Core Simulation Kernel**:

- Pure GDScript simulation-core package layout under `godot_project/sim/`.
- Establishes the headless test harness (framework choice = DDR-032) and a CLI to run tests/smokes.
- Deterministic tick loop, seeded RNG, integer/fixed-point helpers, shared axial hex map, command/event/snapshot/replay infrastructure, variant registry.
- No UI/scene/rendering code; no Python; only minimal variant stubs.

RUN_01 is unblocked: the two decisions that touched it (DDR-027, DDR-029) are Accepted. The only open item at RUN_01 is the test-framework tool choice (DDR-032), made at the start of the run.

## Remaining open questions

- **DDR-032** — which Godot test framework (custom headless runner vs GUT vs gdUnit4). Decided at RUN_01 start.
- **DDR-033** — exact Python↔Godot-headless training transport. Decided at RUN_06.

Both are intentionally deferred and recorded in the register; neither blocks RUN_01 implementation or these documentation changes.

## Confirmation

This was a **documentation-only** architecture decision pass. No simulation code, Godot scenes, or Python training code were created. Tests were not run because there is no code yet.
