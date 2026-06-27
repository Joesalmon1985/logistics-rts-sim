# Settler MVP Art Pipeline

Local tooling to generate **candidate** sprites via OpenAI, review them on contact sheets, and manually export winners into Godot. This does **not** wire art into gameplay automatically.

## Setup

```bash
pip install -r tools/art_pipeline/requirements.txt
export OPENAI_API_KEY="your-key-here"
# Optional: pick the GPT Image model your org can access
export OPENAI_IMAGE_MODEL="gpt-image-1"
```

You may also place `OPENAI_API_KEY` in a repo-root `.env` file (gitignored).

### Supported image models

Per current OpenAI docs, the GPT Image family includes:

- `gpt-image-2`
- `gpt-image-1.5`
- `gpt-image-1` (fallback default)
- `gpt-image-1-mini`

Set via `OPENAI_IMAGE_MODEL` or `--model`. Organization verification may be required for these models.

**Transparent backgrounds:** GPT Image models do not emit alpha from the API (including `gpt-image-2`). Use `postprocess_images.py` to remove flat backgrounds before review/export.

## Quick start (sync-first exploratory run)

```bash
# 1) Build prompts (40 rows = 10 assets × 4 variants)
python tools/art_pipeline/generate_prompts.py --write --variants 4

# 2) Generate a small smoke set (8 images)
python tools/art_pipeline/generate_images_sync.py --execute --limit 8 --yes

# 3) Postprocess + contact sheets
python tools/art_pipeline/postprocess_images.py
python tools/art_pipeline/make_contact_sheets.py
```

Inspect `tools/art_pipeline/contact_sheets/`. Pick winners manually.

## Prompt generation

```bash
# Preview rows (default dry-run)
python tools/art_pipeline/generate_prompts.py --dry-run

# Write CSV
python tools/art_pipeline/generate_prompts.py --write --variants 4

# Scale up variants
python tools/art_pipeline/generate_prompts.py --write --variants 6

# Focus one asset or production pass
python tools/art_pipeline/generate_prompts.py --write --asset carrier --style-pass production
```

Output: `tools/art_pipeline/generated_prompts.csv` (gitignored).

| Pass | Rows | Typical use |
|------|------|-------------|
| Broad (default) | 40 | Cheap exploratory round |
| Broad full sync | 40 | `--execute --limit 40 --yes` |
| Production | 40 | `--style-pass production` |
| Six variants | 60 | `--variants 6` |

## Sync image generation

```bash
# Dry-run (default) — prints planned API calls
python tools/art_pipeline/generate_images_sync.py --limit 8

# Execute
python tools/art_pipeline/generate_images_sync.py --execute --limit 8 --yes \
  --model gpt-image-1 --quality medium
```

Flags:

- `--limit N` (default 1)
- `--asset <id>`
- `--quality low|medium|high`
- `--model <name>`
- `--size 1024x1024`
- `--execute` — required for real API calls
- `--yes` — required when executing more than 10 calls
- `--force` — overwrite existing outputs

Raw images: `tools/art_pipeline/outputs/<asset_id>/<output_stem>.png` plus `.meta.json`.

## Postprocess

```bash
python tools/art_pipeline/postprocess_images.py
```

Default: **flat background removal on** (disable with `--no-remove-flat-bg`), square canvas, preview at game `target_size`.

Output: `tools/art_pipeline/processed/<asset_id>/`.

## Contact sheets

```bash
python tools/art_pipeline/make_contact_sheets.py
```

Creates:

- `contact_sheets/<asset_id>_sheet.png` — one per asset
- `contact_sheets/all_assets_sheet.png` — master review grid

Labels show `asset_id / variant_id` for manual selection.

## Select winners and export to Godot

1. Copy `selected_assets.json.example` → `selected_assets.json`
2. List chosen variants:

```json
{
  "selections": [
    {
      "asset_id": "grass",
      "variant_id": "v03_textured",
      "source": "processed/grass/grass_v03_textured_preview_64.png"
    }
  ]
}
```

3. Copy into Godot assets (no overwrite without `--force`):

```bash
python tools/art_pipeline/shortlist_selected.py
python tools/art_pipeline/export_godot_manifest.py
```

Destinations: `godot_project/client/assets/settler_mvp/art_v1/` (not gitignored — commit finals deliberately).

`godot_art_manifest.json` is a parallel manifest for a future wiring run. It does **not** change `asset_registry.json`.

## Batch mode (scale-up, after prompts look right)

Build jsonl only:

```bash
python tools/art_pipeline/generate_images_batch.py --dry-run
```

Submit (requires `--yes` for >10 requests):

```bash
python tools/art_pipeline/generate_images_batch.py --submit --yes
python tools/art_pipeline/check_batch_status.py
python tools/art_pipeline/download_batch_results.py
```

Batch uses `/v1/images/generations` with lower cost and higher rate-limit headroom. **Do not use Batch for the first exploratory pass** — validate prompts via sync + contact sheets first.

## Cost and safety

| Control | Behavior |
|---------|----------|
| Default | Dry-run on generation scripts |
| `--limit` | Caps sync calls (default 1) |
| `--yes` | Required for >10 API calls |
| Secrets | Never hard-coded; `.env` gitignored |
| Game assets | Never auto-copied; manual `selected_assets.json` only |
| Overwrite | Blocked in shortlist without `--force` |
| Candidates | `outputs/`, `processed/`, `contact_sheets/`, `batches/` are gitignored |

Always check OpenAI pricing for your model/quality/size before large runs. Scripts print estimated call counts before executing.

## File map

| File | Purpose |
|------|---------|
| `asset_manifest.json` | 10 first-pass assets, sizes, placeholders |
| `style_bible.md` | Visual direction |
| `prompt_templates.json` | Base prompts, style passes, 6 variant knobs |
| `generate_prompts.py` | CSV builder |
| `generate_images_sync.py` | Primary generator |
| `generate_images_batch.py` | Batch jsonl + submit |
| `check_batch_status.py` | Poll batch |
| `download_batch_results.py` | Fetch batch images |
| `postprocess_images.py` | Bg removal, square, resize |
| `make_contact_sheets.py` | Review grids |
| `shortlist_selected.py` | Copy winners to `art_v1/` |
| `export_godot_manifest.py` | Godot JSON manifest |

## Out of scope

- Automatic art quality judgment
- Replacing placeholders in gameplay
- Animation frame generation (first pass is static/idle only)
