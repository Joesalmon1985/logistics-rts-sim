#!/usr/bin/env python3
"""Build generated_prompts.csv from asset manifest and prompt templates."""
from __future__ import annotations

import argparse
import logging
import sys
from pathlib import Path

from pipeline_common import (
    MANIFEST_PATH,
    PROMPTS_CSV_PATH,
    TEMPLATES_PATH,
    load_json,
    load_manifest,
    setup_logging,
    write_prompts_csv,
)


def _variant_map(templates: dict) -> dict[str, dict]:
    return {str(v["variant_id"]): v for v in templates.get("variants", [])}


def _select_variant_ids(
    templates: dict,
    *,
    style_pass: str,
    variants_count: int,
) -> list[str]:
    passes = templates.get("style_passes", {})
    if style_pass not in passes:
        raise SystemExit(f"Unknown style pass: {style_pass}")
    pass_ids = list(passes[style_pass].get("variant_ids", []))
    all_ids = [str(v["variant_id"]) for v in templates.get("variants", [])]
    if variants_count == 6:
        return all_ids
    if variants_count == 4:
        return pass_ids[:4]
    raise SystemExit("--variants must be 4 or 6")


def build_prompt_rows(
    *,
    style_pass: str,
    variants_count: int,
    asset_filter: str | None,
) -> list[dict[str, str]]:
    templates = load_json(TEMPLATES_PATH)
    manifest = load_manifest()
    variants = _variant_map(templates)
    variant_ids = _select_variant_ids(templates, style_pass=style_pass, variants_count=variants_count)
    style_excerpt = str(templates.get("style_excerpt", ""))
    negative = str(templates.get("negative_prompt_notes", ""))
    category_hints = templates.get("category_hints", {})
    pass_cfg = templates["style_passes"][style_pass]
    base_template = str(pass_cfg["base_prompt"])

    rows: list[dict[str, str]] = []
    for entry in manifest:
        asset_id = str(entry["asset_id"])
        if asset_filter and asset_id != asset_filter:
            continue
        category = str(entry["category"])
        subject = str(entry["subject_description"])
        category_hint = str(category_hints.get(category, ""))
        target_size = str(entry["target_size"])
        base_prompt = base_template.format(
            style_excerpt=style_excerpt,
            subject=subject,
            category=category,
            category_hint=category_hint,
        )
        for variant_id in variant_ids:
            variant = variants[variant_id]
            modifier = str(variant["modifier"])
            prompt = f"{base_prompt} {modifier}".strip()
            output_stem = f"{asset_id}_{variant_id}"
            rows.append(
                {
                    "asset_id": asset_id,
                    "category": category,
                    "variant_id": variant_id,
                    "target_size": target_size,
                    "prompt": prompt,
                    "negative_prompt_notes": negative,
                    "output_stem": output_stem,
                    "style_pass": style_pass,
                }
            )
    return rows


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write", action="store_true", help="Write generated_prompts.csv")
    parser.add_argument("--dry-run", action="store_true", help="Preview rows without writing")
    parser.add_argument("--variants", type=int, default=4, choices=[4, 6])
    parser.add_argument("--asset", help="Filter to one asset_id")
    parser.add_argument("--style-pass", choices=["broad", "production"], default="broad")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    if not MANIFEST_PATH.is_file() or not TEMPLATES_PATH.is_file():
        logging.error("Missing manifest or templates under tools/art_pipeline/")
        return 1

    rows = build_prompt_rows(
        style_pass=args.style_pass,
        variants_count=args.variants,
        asset_filter=args.asset,
    )
    logging.info("Built %d prompt rows (style_pass=%s, variants=%d)", len(rows), args.style_pass, args.variants)

    if args.dry_run or not args.write:
        for row in rows[:5]:
            logging.info(
                "sample: %s / %s -> %s",
                row["asset_id"],
                row["variant_id"],
                row["output_stem"],
            )
        if len(rows) > 5:
            logging.info("... and %d more rows", len(rows) - 5)
        if not args.write:
            logging.info("Dry-run only; pass --write to emit CSV")
        return 0

    path = write_prompts_csv(rows, PROMPTS_CSV_PATH)
    logging.info("Wrote %s", path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
