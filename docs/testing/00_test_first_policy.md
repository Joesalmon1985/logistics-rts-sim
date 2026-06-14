# Test-First Policy

## Policy

1. Every major mechanic has deterministic headless tests before or with implementation.
2. Tests reference stable rule IDs from design requirement documents.
3. Never weaken, delete, skip, or bypass tests to pass implementation.
4. Simplified rules are OK; missing mechanic families are not.

## Test Environment

- pytest
- No Godot required for sim tests
- No network required for unit/integration/scenario tests

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

RUN_01 establishes pytest scaffold; CI added when repo has GitHub Actions (optional RUN_01+).

## Documentation Cross-Refs

- Rule matrix: `01_rule_contract_test_matrix.md`
- Smoke matrix: `02_smoke_test_matrix.md`
- Run-specific requirements: `docs/runs/RUN_XX_*.md`
