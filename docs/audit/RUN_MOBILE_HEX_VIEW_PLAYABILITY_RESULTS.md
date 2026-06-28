# RUN_MOBILE_HEX_VIEW_PLAYABILITY — Results

Branch: `feature/mobile-hex-view-playability`

## Profiles tested

- mobile_portrait_common (390×844) via SubViewport harness

## Capture method

`in_engine_subviewport` (headless harness); real window not run in this session.

## Results

| Check | Result |
|-------|--------|
| Mobile layout profiles | PASS |
| Main menu mobile route | PASS |
| Hybrid FFA tick advance | PASS |
| `run_mobile_hex_view_qa.gd` | PASS |

## Known gaps

- Full 18-test `TEST_FILTER=mobile_hex` suite not all implemented yet
- Pinch zoom manual validation pending real device
