# RUN Tutorial Legibility — Lessons Learned

## What worked

- Strict visibility util with SubViewport-aware sizing (`custom_minimum_size` width matters for probes).
- Layout-first hard gate before sprites/animation; `verify_human_understandable_screen.gd` as the legibility gate.
- Headless tests use immediate `host.free()` after teardown to avoid unique-name collisions between mounted scenes.

## What broke

- RichTextLabel `.text` assignment did not always satisfy `get_parsed_text()` in probes; use `clear()` + `append_text()`.
- Carrier selection priority at HQ hex caused inspector tests to read carrier text unless formatting HQ building directly.
- Full `run_tests.gd` (251 tests) exceeds 300–600s timeout on this machine; filtered suites pass.

## Verification

- `verify_human_understandable_screen.gd` and `verify_human_understandable_flow.gd` exit 0.
- `TEST_FILTER=human_understandable` — 20/20 pass.
- `TEST_FILTER=human_mvp` — 14/14 pass; `TEST_FILTER=settler_mvp` — 15/15 pass.
