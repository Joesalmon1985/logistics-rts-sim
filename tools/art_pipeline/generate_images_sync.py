#!/usr/bin/env python3
"""Generate images synchronously via OpenAI Images API."""
from __future__ import annotations

import argparse
import base64
import logging
import os
import sys
from pathlib import Path
from typing import Any

from pipeline_common import (
    OUTPUTS_DIR,
    PROMPTS_CSV_PATH,
    ensure_dirs,
    estimate_api_calls,
    filter_rows,
    load_dotenv_if_present,
    load_prompts_csv,
    require_confirmation,
    resolve_image_model,
    setup_logging,
    utc_now_iso,
    with_retries,
    write_image_metadata,
)


def _build_client():
    load_dotenv_if_present()
    api_key = os.environ.get("OPENAI_API_KEY", "").strip()
    if not api_key:
        raise SystemExit("OPENAI_API_KEY is not set in the environment.")
    from openai import OpenAI

    return OpenAI(api_key=api_key)


def _generate_image(
    client,
    *,
    prompt: str,
    model: str,
    size: str,
    quality: str,
) -> bytes:
    def _call() -> bytes:
        response = client.images.generate(
            model=model,
            prompt=prompt,
            size=size,
            quality=quality,
            n=1,
        )
        item = response.data[0]
        if getattr(item, "b64_json", None):
            return base64.b64decode(item.b64_json)
        if getattr(item, "url", None):
            import urllib.request

            with urllib.request.urlopen(item.url) as resp:  # noqa: S310
                return resp.read()
        raise RuntimeError("Image response had neither b64_json nor url")

    return with_retries(_call, description=f"images.generate ({model})")


def _friendly_model_error(exc: Exception, model: str) -> None:
    message = str(exc).lower()
    if "model" in message or "access" in message or "permission" in message:
        logging.error(
            "Model error for '%s'. Verify org access to GPT Image models "
            "(gpt-image-2, gpt-image-1.5, gpt-image-1, gpt-image-1-mini) "
            "and set OPENAI_IMAGE_MODEL or --model.",
            model,
        )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--limit", type=int, default=1)
    parser.add_argument("--asset")
    parser.add_argument("--quality", choices=["low", "medium", "high"], default="medium")
    parser.add_argument("--model")
    parser.add_argument("--size", default="1024x1024")
    parser.add_argument("--dry-run", action="store_true", help="Print plan without API calls")
    parser.add_argument("--execute", action="store_true", help="Perform API calls")
    parser.add_argument("--yes", action="store_true", help="Confirm runs over 10 images")
    parser.add_argument("--force", action="store_true", help="Overwrite existing outputs")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    execute = args.execute and not args.dry_run
    if not args.execute:
        logging.info("Dry-run mode (default). Pass --execute to call the API.")

    rows = load_prompts_csv(PROMPTS_CSV_PATH)
    rows = filter_rows(rows, asset_id=args.asset, limit=args.limit)
    if not rows:
        logging.error("No prompt rows matched filters.")
        return 1

    model = resolve_image_model(args.model)
    count = estimate_api_calls(rows, None)
    require_confirmation(count, yes_flag=args.yes, dry_run=not execute, execute=execute)

    if not execute:
        for row in rows:
            logging.info(
                "would generate: %s -> outputs/%s/%s.png",
                row["output_stem"],
                row["asset_id"],
                row["output_stem"],
            )
        return 0

    ensure_dirs()
    client = _build_client()
    failures = 0
    for row in rows:
        asset_id = row["asset_id"]
        stem = row["output_stem"]
        out_dir = OUTPUTS_DIR / asset_id
        out_dir.mkdir(parents=True, exist_ok=True)
        image_path = out_dir / f"{stem}.png"
        if image_path.exists() and not args.force:
            logging.info("skip existing %s", image_path)
            continue
        try:
            png_bytes = _generate_image(
                client,
                prompt=row["prompt"],
                model=model,
                size=args.size,
                quality=args.quality,
            )
            image_path.write_bytes(png_bytes)
            metadata: dict[str, Any] = {
                "asset_id": asset_id,
                "variant_id": row["variant_id"],
                "output_stem": stem,
                "prompt": row["prompt"],
                "negative_prompt_notes": row.get("negative_prompt_notes", ""),
                "model": model,
                "timestamp": utc_now_iso(),
                "target_size": int(row["target_size"]),
                "quality": args.quality,
                "size": args.size,
                "style_pass": row.get("style_pass", ""),
                "api": "sync",
            }
            write_image_metadata(image_path, metadata)
            logging.info("saved %s", image_path)
        except Exception as exc:  # noqa: BLE001
            failures += 1
            _friendly_model_error(exc, model)
            logging.error("failed %s: %s", stem, exc)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
