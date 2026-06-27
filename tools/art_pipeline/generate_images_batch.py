#!/usr/bin/env python3
"""Create (and optionally submit) an OpenAI Batch job for image generation."""
from __future__ import annotations

import argparse
import json
import logging
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

from pipeline_common import (
    BATCHES_DIR,
    PROMPTS_CSV_PATH,
    filter_rows,
    load_dotenv_if_present,
    load_prompts_csv,
    resolve_image_model,
    setup_logging,
    utc_now_iso,
    write_batch_state,
)


def _timestamp_slug() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def build_batch_lines(
    rows: list[dict[str, str]],
    *,
    model: str,
    size: str,
    quality: str,
) -> list[dict]:
    lines: list[dict] = []
    for row in rows:
        custom_id = f"{row['asset_id']}__{row['variant_id']}__{row['output_stem']}"
        lines.append(
            {
                "custom_id": custom_id,
                "method": "POST",
                "url": "/v1/images/generations",
                "body": {
                    "model": model,
                    "prompt": row["prompt"],
                    "size": size,
                    "quality": quality,
                    "n": 1,
                },
            }
        )
    return lines


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--asset")
    parser.add_argument("--quality", choices=["low", "medium", "high"], default="medium")
    parser.add_argument("--model")
    parser.add_argument("--size", default="1024x1024")
    parser.add_argument("--dry-run", action="store_true", help="Write jsonl only (default)")
    parser.add_argument("--submit", action="store_true", help="Upload and create batch job")
    parser.add_argument("--yes", action="store_true", help="Confirm batch submission over 10 images")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    rows = load_prompts_csv(PROMPTS_CSV_PATH)
    rows = filter_rows(rows, asset_id=args.asset, limit=args.limit)
    if not rows:
        logging.error("No prompt rows matched filters.")
        return 1

    model = resolve_image_model(args.model)
    lines = build_batch_lines(rows, model=model, size=args.size, quality=args.quality)
    slug = _timestamp_slug()
    BATCHES_DIR.mkdir(parents=True, exist_ok=True)
    jsonl_path = BATCHES_DIR / f"batch_{slug}.jsonl"
    with jsonl_path.open("w", encoding="utf-8") as handle:
        for line in lines:
            handle.write(json.dumps(line) + "\n")

    style_pass = rows[0].get("style_pass", "") if rows else ""
    meta = {
        "created_at": utc_now_iso(),
        "jsonl_path": str(jsonl_path),
        "row_count": len(lines),
        "model": model,
        "size": args.size,
        "quality": args.quality,
        "style_pass": style_pass,
        "variant_rows": len(lines),
    }
    meta_path = BATCHES_DIR / f"batch_{slug}_meta.json"
    meta_path.write_text(json.dumps(meta, indent=2) + "\n", encoding="utf-8")
    logging.info("Wrote %s (%d requests)", jsonl_path, len(lines))

    if args.dry_run or not args.submit:
        logging.info("Dry-run / no submit. Inspect jsonl, then rerun with --submit.")
        return 0

    if len(lines) > 10 and not args.yes:
        raise SystemExit(f"Refusing to submit {len(lines)} requests without --yes")

    load_dotenv_if_present()
    api_key = os.environ.get("OPENAI_API_KEY", "").strip()
    if not api_key:
        raise SystemExit("OPENAI_API_KEY is not set in the environment.")
    from openai import OpenAI

    client = OpenAI(api_key=api_key)
    upload = client.files.create(file=jsonl_path.open("rb"), purpose="batch")
    batch = client.batches.create(
        input_file_id=upload.id,
        endpoint="/v1/images/generations",
        completion_window="24h",
    )
    state = {
        "batch_id": batch.id,
        "input_file_id": upload.id,
        "jsonl_path": str(jsonl_path),
        "meta_path": str(meta_path),
        "submitted_at": utc_now_iso(),
        "status": batch.status,
        "row_count": len(lines),
    }
    write_batch_state(state)
    logging.info("Submitted batch %s (status=%s)", batch.id, batch.status)
    return 0


if __name__ == "__main__":
    sys.exit(main())
