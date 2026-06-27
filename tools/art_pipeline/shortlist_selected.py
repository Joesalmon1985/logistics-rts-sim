#!/usr/bin/env python3
"""Copy human-selected processed images into art_v1/ for Godot."""
from __future__ import annotations

import argparse
import json
import logging
import shutil
import sys
from pathlib import Path

from pipeline_common import (
    ART_V1_DIR,
    PIPELINE_ROOT,
    SELECTED_ASSETS_PATH,
    export_filename,
    load_json,
    load_manifest,
    manifest_by_id,
    setup_logging,
    utc_now_iso,
)


def _resolve_source(source: str) -> Path:
    path = Path(source)
    if path.is_file():
        return path
    candidate = PIPELINE_ROOT / source
    if candidate.is_file():
        return candidate
    raise FileNotFoundError(f"Source not found: {source}")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--selections",
        type=Path,
        default=SELECTED_ASSETS_PATH,
        help="Path to selected_assets.json",
    )
    parser.add_argument("--force", action="store_true", help="Overwrite existing art_v1 files")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    if not args.selections.is_file():
        raise SystemExit(f"Selections file not found: {args.selections}")

    data = load_json(args.selections)
    selections = data.get("selections", [])
    if not selections:
        logging.error("No selections in %s", args.selections)
        return 1

    by_id = manifest_by_id(load_manifest())
    log_entries: list[dict] = []
    blocked = 0

    for item in selections:
        asset_id = str(item["asset_id"])
        variant_id = str(item.get("variant_id", ""))
        entry = by_id.get(asset_id)
        if not entry:
            logging.error("Unknown asset_id: %s", asset_id)
            return 1
        try:
            src = _resolve_source(str(item["source"]))
        except FileNotFoundError as exc:
            if args.dry_run:
                logging.warning("%s (dry-run continues)", exc)
                src = PIPELINE_ROOT / item["source"]
            else:
                logging.error("%s", exc)
                return 1
        dest_name = export_filename(entry)
        dest = ART_V1_DIR / dest_name
        if dest.exists() and not args.force:
            logging.error("Destination exists (use --force): %s", dest)
            blocked += 1
            continue
        logging.info("%s -> %s", src, dest)
        if not args.dry_run:
            ART_V1_DIR.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dest)
        log_entries.append(
            {
                "asset_id": asset_id,
                "variant_id": variant_id,
                "source": str(src),
                "destination": str(dest),
                "timestamp": utc_now_iso(),
            }
        )

    if blocked:
        return 1

    if not args.dry_run and log_entries:
        ART_V1_DIR.mkdir(parents=True, exist_ok=True)
        log_path = ART_V1_DIR / "selection_log.json"
        log_path.write_text(json.dumps({"selections": log_entries}, indent=2) + "\n", encoding="utf-8")
        logging.info("Wrote %s", log_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
