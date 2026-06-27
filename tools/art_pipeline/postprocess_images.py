#!/usr/bin/env python3
"""Postprocess raw API images: bg removal, square canvas, preview resize."""
from __future__ import annotations

import argparse
import json
import logging
import sys
from pathlib import Path

from PIL import Image

from pipeline_common import (
    OUTPUTS_DIR,
    PROCESSED_DIR,
    load_manifest,
    manifest_by_id,
    setup_logging,
    utc_now_iso,
)


def _corner_colors(img: Image.Image) -> list[tuple[int, int, int]]:
    w, h = img.size
    pixels = img.convert("RGBA")
    coords = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    return [pixels.getpixel(c)[:3] for c in coords]


def _color_distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    return sum(abs(x - y) for x, y in zip(a, b))


def remove_flat_background(img: Image.Image, tolerance: int = 28) -> Image.Image:
    rgba = img.convert("RGBA")
    corners = _corner_colors(rgba)
    if max(_color_distance(corners[0], c) for c in corners[1:]) > tolerance:
        return rgba
    bg = corners[0]
    w, h = rgba.size
    visited = [[False] * w for _ in range(h)]
    stack = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    while stack:
        x, y = stack.pop()
        if x < 0 or y < 0 or x >= w or y >= h or visited[y][x]:
            continue
        visited[y][x] = True
        r, g, b, a = rgba.getpixel((x, y))
        if _color_distance((r, g, b), bg) <= tolerance:
            rgba.putpixel((x, y), (r, g, b, 0))
            stack.extend([(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)])
    return rgba


def content_bbox(img: Image.Image, alpha_threshold: int = 8) -> tuple[int, int, int, int]:
    rgba = img.convert("RGBA")
    w, h = rgba.size
    min_x, min_y, max_x, max_y = w, h, 0, 0
    found = False
    for y in range(h):
        for x in range(w):
            if rgba.getpixel((x, y))[3] > alpha_threshold:
                found = True
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if not found:
        return 0, 0, w, h
    return min_x, min_y, max_x + 1, max_y + 1


def center_on_square(img: Image.Image, padding_pct: float) -> Image.Image:
    bbox = content_bbox(img)
    cropped = img.crop(bbox)
    cw, ch = cropped.size
    side = int(max(cw, ch) * (1.0 + padding_pct / 100.0))
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    offset = ((side - cw) // 2, (side - ch) // 2)
    canvas.paste(cropped, offset, cropped if cropped.mode == "RGBA" else None)
    return canvas


def process_image(
    src: Path,
    *,
    target_size: int,
    remove_flat_bg: bool,
    padding_pct: float,
    dry_run: bool,
) -> list[Path]:
    stem = src.stem
    asset_id = src.parent.name
    out_dir = PROCESSED_DIR / asset_id
    square_path = out_dir / f"{stem}_square.png"
    preview_path = out_dir / f"{stem}_preview_{target_size}.png"
    meta_path = out_dir / f"{stem}_process.meta.json"

    if dry_run:
        logging.info("would process %s -> %s, %s", src, square_path, preview_path)
        return [square_path, preview_path]

    out_dir.mkdir(parents=True, exist_ok=True)
    img = Image.open(src)
    original_size = img.size
    if remove_flat_bg:
        img = remove_flat_background(img)
    square = center_on_square(img, padding_pct)
    preview = square.resize((target_size, target_size), Image.Resampling.LANCZOS)
    square.save(square_path)
    preview.save(preview_path)
    meta = {
        "source": str(src),
        "original_size": list(original_size),
        "target_size": target_size,
        "remove_flat_bg": remove_flat_bg,
        "padding_pct": padding_pct,
        "timestamp": utc_now_iso(),
    }
    meta_path.write_text(json.dumps(meta, indent=2) + "\n", encoding="utf-8")
    logging.info("processed %s", stem)
    return [square_path, preview_path]


def iter_source_images(asset_filter: str | None, limit: int | None) -> list[Path]:
    images: list[Path] = []
    if not OUTPUTS_DIR.is_dir():
        return images
    for asset_dir in sorted(OUTPUTS_DIR.iterdir()):
        if not asset_dir.is_dir():
            continue
        if asset_filter and asset_dir.name != asset_filter:
            continue
        for path in sorted(asset_dir.glob("*.png")):
            if path.name.endswith("_square.png") or "_preview_" in path.name:
                continue
            images.append(path)
    if limit is not None:
        images = images[:limit]
    return images


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asset")
    parser.add_argument("--limit", type=int)
    parser.add_argument("--padding-pct", type=float, default=8.0)
    parser.add_argument(
        "--no-remove-flat-bg",
        action="store_true",
        help="Disable flat background removal (enabled by default)",
    )
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    manifest = manifest_by_id(load_manifest())
    sources = iter_source_images(args.asset, args.limit)
    if not sources:
        logging.error("No source images found under %s", OUTPUTS_DIR)
        return 1

    remove_flat_bg = not args.no_remove_flat_bg
    for src in sources:
        asset_id = src.parent.name
        entry = manifest.get(asset_id)
        if not entry:
            logging.warning("skip unknown asset_id %s", asset_id)
            continue
        target_size = int(entry["target_size"])
        process_image(
            src,
            target_size=target_size,
            remove_flat_bg=remove_flat_bg,
            padding_pct=args.padding_pct,
            dry_run=args.dry_run,
        )
    return 0


if __name__ == "__main__":
    sys.exit(main())
