#!/usr/bin/env python3
"""Export a Godot-facing manifest for selected art_v1 assets."""
from __future__ import annotations

import argparse
import json
import logging
import sys
from pathlib import Path

from pipeline_common import (
    ART_V1_DIR,
    SELECTED_ASSETS_PATH,
    export_filename,
    godot_res_path,
    load_json,
    load_manifest,
    manifest_by_id,
    setup_logging,
)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--selections",
        type=Path,
        default=SELECTED_ASSETS_PATH,
        help="Path to selected_assets.json",
    )
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    if not args.selections.is_file():
        raise SystemExit(f"Selections file not found: {args.selections}")

    selections = load_json(args.selections).get("selections", [])
    if not selections:
        logging.error("No selections in %s", args.selections)
        return 1

    by_id = manifest_by_id(load_manifest())
    assets: list[dict] = []
    for item in selections:
        asset_id = str(item["asset_id"])
        entry = by_id.get(asset_id)
        if not entry:
            logging.error("Unknown asset_id: %s", asset_id)
            return 1
        dest_name = export_filename(entry)
        art_path = ART_V1_DIR / dest_name
        if not art_path.is_file() and not args.dry_run:
            logging.error("Missing art_v1 file (run shortlist_selected first): %s", art_path)
            return 1
        if not art_path.is_file() and args.dry_run:
            logging.warning("dry-run: %s not present yet", art_path)
        states = entry.get("animation_states", ["static"])
        animation_state = str(states[0]) if states else "static"
        frame_size = int(entry["target_size"])
        assets.append(
            {
                "asset_id": asset_id,
                "category": entry["category"],
                "registry_key": entry["registry_key"],
                "path": godot_res_path(f"client/assets/settler_mvp/art_v1/{dest_name}"),
                "frame_size": [frame_size, frame_size],
                "animation_state": animation_state,
                "fps": 0,
                "frame_count": 1,
                "fallback_placeholder": godot_res_path(str(entry["placeholder_path"])),
                "variant_id": str(item.get("variant_id", "")),
            }
        )

    manifest_out = {"version": 1, "assets": assets}
    out_path = ART_V1_DIR / "godot_art_manifest.json"
    logging.info("Would write %d assets to %s", len(assets), out_path)
    if not args.dry_run:
        ART_V1_DIR.mkdir(parents=True, exist_ok=True)
        out_path.write_text(json.dumps(manifest_out, indent=2) + "\n", encoding="utf-8")
        logging.info("Wrote %s", out_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
