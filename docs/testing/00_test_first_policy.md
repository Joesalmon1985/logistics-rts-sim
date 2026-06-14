# Test-First Policy

## Policy

1. Every major mechanic has deterministic headless tests before or with implementation.
2. Tests reference stable rule IDs from design requirement documents.
3. Never weaken, delete, skip, or bypass tests to pass implementation.
4. Simplified rules are OK; missing mechanic families are not.

## Test Environment

- Headless Godot test harness for the GDScript simulation core (custom lightweight runner — DDR-032 Accepted; see `docs/verification/local_verification.md`)
- Tests run headlessly (Godot headless / no rendered UI), are deterministic, CLI-runnable, and report clear pass/fail for Cursor
- Test names use rule IDs (see below)
- No network required for unit/integration/scenario tests
- Python `pytest` is used only for Python training/tooling (`python_tools/`), not for gameplay-rule tests

## Verification integrity (RUN_01+)

- Use `timeout` wrappers on Linux for all Godot CLI test/smoke commands (see `docs/verification/local_verification.md`).
- Exit code `0` = pass; non-zero = fail; `124` = timed out (failure).
- **Never** report pass for aborted, killed, hung, or timed-out commands.
- Import success (`godot --import`) is not test success.
- RUN_02 and later runs must **rerun RUN_01 kernel tests and smokes** before adding new mechanics (and prior RUN_02/RUN_03 suites before RUN_07).
- Run `sim/tools/verify_failure_modes.gd` on closure passes to confirm failure paths exit non-zero.
- See `.cursor/rules/10-run-verification-and-logging.mdc` for the full verification/logging discipline.

### Status categories (never collapse)

Every test/smoke/integrity command resolves to exactly one status. Reports must keep these distinct and **must not** collapse `errored`, `timed out`, or `not run` into `passed`:

| Status | Meaning |
|--------|---------|
| passed | Completed, exit `0`, expected summary printed |
| failed | Completed, reported failure (exit `1`) |
| errored | Load/parse/script error; no clean result |
| timed out | Hit the `timeout` wrapper (exit `124`) — failure |
| not run | Never executed, aborted, or killed — never a pass |

### Reporting discipline

- Report the **exact commands** actually run, not idealised ones.
- Report the **exit code** for each verification command where relevant.
- A Cursor "task aborted" result is `not run`, never a pass.
- A run is not complete if verification was skipped, timed out, aborted, or errored.

## Determinism

- Tests set explicit seed
- Scenario tests compare event sequences or state hashes
- No wall-clock timing assertions in unit tests

## Test Naming

```
test_{RULE_ID_with_underscores}_{short_description}
```

Example: `test_ECON_ROAD_001_roads_connect_adjacent_hexes`

## Coverage Expectations

| Level | When |
|-------|------|
| Unit | Each rule ID with isolated logic |
| Integration | Cross-subsystem (path + carrier + job) |
| Scenario | Full scenario fixture with replay hash |
| Smoke | Per-run milestone (see `02_smoke_test_matrix.md`) |

## CI (Future)

RUN_01 establishes the headless Godot test harness and a CLI to run tests/smokes; CI added when repo has GitHub Actions (optional RUN_01+), invoking the harness headlessly.

## Documentation Cross-Refs

- Rule matrix: `01_rule_contract_test_matrix.md`
- Smoke matrix: `02_smoke_test_matrix.md`
- Run-specific requirements: `docs/runs/RUN_XX_*.md`
