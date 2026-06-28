# RUN_MOBILE_HEX_VIEW_PLAYABILITY — Current State

Date: 2026-06-28

## Baseline diff vs manual playability QA

Desktop 1280×720 manual QA passed tutorials and MVPs. This audit adds:

- `mobile_layout_profiles.gd` (8 profiles)
- `run_mobile_hex_view_qa.gd`
- Hybrid FFA mobile scene at 390×844
- 2D hybrid spectator mobile-mount via SubViewport

## Blockers resolved in this run

- No mobile profiles → added
- No Hybrid FFA menu path → added

## Remaining gaps

- Full 18-test `mobile_hex` matrix not all implemented
- Pinch-zoom not automated
- Real window 390×844 capture not run (DISPLAY available but not used)
