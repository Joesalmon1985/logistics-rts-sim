# RUN_HUMAN_PLAYABLE_SETTLER_MVP — Lessons Learned

## What worked

- Extending `RUN_PLAYABLE_SETTLER_MVP` rather than rewriting the controller kept sim authority clean.
- `SettlerMvpTestHelper.mount_settler` + `HumanMvpReviewUtil` visibility probes gave a repeatable 1920×1080 gate without a display.
- Deterministic sprite generation (`tools/generate_settler_placeholder_sprites.py`) avoided Kenney IP risk and kept assets small.
- `SelectionFormatter` + `BuildingCatalog` separated display copy from sim config.

## What broke

- Removing `SettlerSpriteLayer` from `.tscn` while keeping `@onready %SettlerSpriteLayer` aborted `_setup_scene()` and broke all settler viewer tests (`buildings=0`, `render_empty=true`).
- `SettlerSpriteLayer` initially extended `Node2D` but was parented as `Control` with `set_anchors_preset` — class/base mismatch.
- Headless tests need the mount **host** on the scene tree for `Timer` and layout; SubViewport-only mounts left `_initialized` false.
- `BuildingCatalog.load()` shadowed Godot's global `load()` — renamed to `load_catalog()`.
- Screenshot visibility probes were too strict on both width and height; panels with `custom_minimum_size.y` only (build palette) failed until probe logic accepted any non-zero extent.
- `ViewerTestHarness.capture()` logs texture errors in headless dummy renderer but 1×1 fallback PNGs still satisfy smoke thresholds.

## Test / verification lessons

- Use `TEST_FILTER=human_mvp` / `TEST_FILTER=settler_mvp` env vars (not `--filter=` CLI).
- Menu + game screenshot test needs **separate hosts** or teardown before remount to avoid parenting errors.
- Rerun `settler_mvp` regression after any `settler_mvp_game.gd` init change — mount tests catch init aborts early.

## Risks for next runs

- Tutorial steps 7–11 lack dedicated headless tests; rely on JSON + manual play for full path.
- Headless PNG captures may be structurally valid but visually blank; pair with visibility JSON gates.
- Carrier-on-HQ hex blocks building selection at anchor; footprint iteration required in building inspector test.

## Prompt improvements

- Call out `@onready` / `.tscn` unique-node parity explicitly when adding programmatic node creation.
- Require `initialize_for_tests` or scene-tree host in any new viewer test harness docs.
