#!/usr/bin/env python3
"""Build labeled contact sheets for manual art review."""
from __future__ import annotations

import argparse
import logging
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from pipeline_common import (
    CONTACT_SHEETS_DIR,
    PROCESSED_DIR,
    load_manifest,
    manifest_by_id,
    setup_logging,
)


def _parse_preview(path: Path) -> tuple[str, str, int] | None:
    # grass_v03_textured_preview_64.png
    match = re.match(r"^(?P<stem>.+)_(?P<variant>v\d+_[^_]+)_preview_(?P<size>\d+)$", path.stem)
    if not match:
        return None
    full_stem = match.group("stem")
    variant_id = match.group("variant")
    size = int(match.group("size"))
    asset_id = full_stem.rsplit(f"_{variant_id}", 1)[0]
    return asset_id, variant_id, size


def _collect_previews(asset_filter: str | None) -> dict[str, list[tuple[str, Path]]]:
    grouped: dict[str, list[tuple[str, Path]]] = {}
    if not PROCESSED_DIR.is_dir():
        return grouped
    for asset_dir in sorted(PROCESSED_DIR.iterdir()):
        if not asset_dir.is_dir():
            continue
        asset_id = asset_dir.name
        if asset_filter and asset_id != asset_filter:
            continue
        for path in sorted(asset_dir.glob("*_preview_*.png")):
            parsed = _parse_preview(path)
            if not parsed:
                continue
            parsed_asset, variant_id, _ = parsed
            if parsed_asset != asset_id:
                continue
            grouped.setdefault(asset_id, []).append((variant_id, path))
    for asset_id in grouped:
        grouped[asset_id].sort(key=lambda item: item[0])
    return grouped


def _load_font(size: int) -> ImageFont.ImageFont:
    try:
        return ImageFont.truetype("DejaVuSans.ttf", size)
    except OSError:
        return ImageFont.load_default()


def _render_sheet(
    asset_id: str,
    items: list[tuple[str, Path]],
    *,
    cell_size: int,
    label_height: int,
) -> Image.Image:
    cols = max(1, len(items))
    rows = 1
    width = cols * cell_size
    height = rows * (cell_size + label_height)
    sheet = Image.new("RGBA", (width, height), (32, 32, 36, 255))
    draw = ImageDraw.Draw(sheet)
    font = _load_font(max(12, label_height // 2))
    for idx, (variant_id, path) in enumerate(items):
        x = idx * cell_size
        img = Image.open(path).convert("RGBA")
        img = img.resize((cell_size, cell_size), Image.Resampling.LANCZOS)
        sheet.paste(img, (x, 0), img)
        label = f"{asset_id} / {variant_id}"
        draw.rectangle([x, cell_size, x + cell_size, cell_size + label_height], fill=(20, 20, 24, 255))
        draw.text((x + 8, cell_size + 4), label, fill=(230, 230, 230, 255), font=font)
    return sheet


def _render_master_sheet(
    grouped: dict[str, list[tuple[str, Path]]],
    *,
    cell_size: int,
    label_height: int,
) -> Image.Image:
    asset_ids = sorted(grouped)
    if not asset_ids:
        raise ValueError("no previews to render")
    max_cols = max(len(grouped[a]) for a in asset_ids)
    row_height = cell_size + label_height + 8
    width = max_cols * cell_size
    height = len(asset_ids) * row_height
    sheet = Image.new("RGBA", (width, height), (24, 24, 28, 255))
    draw = ImageDraw.Draw(sheet)
    font = _load_font(max(12, label_height // 2))
    for row_idx, asset_id in enumerate(asset_ids):
        y_base = row_idx * row_height
        for col_idx, (variant_id, path) in enumerate(grouped[asset_id]):
            x = col_idx * cell_size
            img = Image.open(path).convert("RGBA")
            img = img.resize((cell_size, cell_size), Image.Resampling.LANCZOS)
            sheet.paste(img, (x, y_base), img)
            label = f"{asset_id} / {variant_id}"
            draw.rectangle(
                [x, y_base + cell_size, x + cell_size, y_base + cell_size + label_height],
                fill=(20, 20, 24, 255),
            )
            draw.text((x + 8, y_base + cell_size + 4), label, fill=(230, 230, 230, 255), font=font)
    return sheet


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asset")
    parser.add_argument("--cell-size", type=int, help="Override cell size (default: 2x target_size)")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    manifest = manifest_by_id(load_manifest())
    grouped = _collect_previews(args.asset)
    if not grouped:
        logging.error("No preview images found under %s", PROCESSED_DIR)
        return 1

    CONTACT_SHEETS_DIR.mkdir(parents=True, exist_ok=True)
    label_height = 28
    for asset_id, items in grouped.items():
        target = int(manifest[asset_id]["target_size"]) if asset_id in manifest else 64
        cell_size = args.cell_size or target * 2
        sheet = _render_sheet(asset_id, items, cell_size=cell_size, label_height=label_height)
        out = CONTACT_SHEETS_DIR / f"{asset_id}_sheet.png"
        sheet.save(out)
        logging.info("wrote %s", out)

    if not args.asset:
        max_target = max(int(manifest[a]["target_size"]) for a in grouped if a in manifest)
        cell_size = args.cell_size or max_target * 2
        master = _render_master_sheet(grouped, cell_size=cell_size, label_height=label_height)
        master_path = CONTACT_SHEETS_DIR / "all_assets_sheet.png"
        master.save(master_path)
        logging.info("wrote %s", master_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
