# Manual Playability QA — Lessons Learned

## What worked

- Shared `BaseTutorialController` + `TutorialNavigationUI` fixed stranded tutorial navigation across Settler/Skirmish/Hybrid in one pass.
- Menu-route capture helper (`ManualPlayabilityCaptureHelper`) catches launch regressions better than direct scene mounts.
- Tutorial button text in review JSON makes screenshot failures actionable.

## What broke

- `assert_gt` is not available on `TestCase` — use `assert_true(a > b)`.
- Hybrid bot functional tests need ~100s; batch `TEST_FILTER=manual_qa` needs ≥500s.
- 3D spectator `%PlayPauseBtn` probes failed until using `find_child` + `ensure_match_started`.

## Verification

- Primary commands in `docs/verification/local_verification.md` § Manual playability QA.
- Screenshot output: `docs/audit/screenshots/manual_playability/`.
