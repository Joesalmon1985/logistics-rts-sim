#!/usr/bin/env python3
"""Download completed batch results and save images to outputs/."""
from __future__ import annotations

import argparse
import base64
import json
import logging
import os
import sys
from pathlib import Path
from typing import Any

from pipeline_common import (
    BATCH_ERRORS_PATH,
    OUTPUTS_DIR,
    append_batch_error,
    ensure_dirs,
    parse_custom_id,
    read_batch_state,
    setup_logging,
    utc_now_iso,
    write_image_metadata,
)
from pipeline_common import load_dotenv_if_present


def _save_success_line(line: dict[str, Any], batch_id: str) -> None:
    custom_id = str(line.get("custom_id", ""))
    asset_id, variant_id, output_stem = parse_custom_id(custom_id)
    response = line.get("response", {})
    body = response.get("body", {})
    data = body.get("data", [])
    if not data:
        raise ValueError("missing data in batch response body")
    item = data[0]
    if item.get("b64_json"):
        png_bytes = base64.b64decode(item["b64_json"])
    elif item.get("url"):
        import urllib.request

        with urllib.request.urlopen(item["url"]) as resp:  # noqa: S310
            png_bytes = resp.read()
    else:
        raise ValueError("response item had neither b64_json nor url")

    out_dir = OUTPUTS_DIR / asset_id
    out_dir.mkdir(parents=True, exist_ok=True)
    image_path = out_dir / f"{output_stem}.png"
    image_path.write_bytes(png_bytes)
    metadata = {
        "asset_id": asset_id,
        "variant_id": variant_id,
        "output_stem": output_stem,
        "custom_id": custom_id,
        "batch_id": batch_id,
        "timestamp": utc_now_iso(),
        "api": "batch",
        "response_id": response.get("id"),
    }
    write_image_metadata(image_path, metadata)
    logging.info("saved %s", image_path)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--batch-id", help="Batch ID (default: latest from batch_state.json)")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)

    setup_logging(args.verbose)
    load_dotenv_if_present()
    api_key = os.environ.get("OPENAI_API_KEY", "").strip()
    if not api_key:
        raise SystemExit("OPENAI_API_KEY is not set in the environment.")

    batch_id = args.batch_id
    if not batch_id:
        state = read_batch_state()
        batch_id = state.get("batch_id")
        if not batch_id:
            raise SystemExit("No --batch-id and no batch_state.json entry found.")

    from openai import OpenAI

    client = OpenAI(api_key=api_key)
    batch = client.batches.retrieve(batch_id)
    if batch.status != "completed":
        logging.error("Batch %s is not completed (status=%s)", batch_id, batch.status)
        return 1
    if not batch.output_file_id:
        logging.error("Batch %s has no output_file_id", batch_id)
        return 1

    ensure_dirs()
    if BATCH_ERRORS_PATH.is_file():
        BATCH_ERRORS_PATH.unlink()

    content = client.files.content(batch.output_file_id).text
    failures = 0
    for raw_line in content.splitlines():
        if not raw_line.strip():
            continue
        line = json.loads(raw_line)
        if line.get("error"):
            failures += 1
            append_batch_error(line)
            logging.error("batch error for %s: %s", line.get("custom_id"), line["error"])
            continue
        try:
            _save_success_line(line, batch_id)
        except Exception as exc:  # noqa: BLE001
            failures += 1
            record = {"custom_id": line.get("custom_id"), "error": str(exc), "line": line}
            append_batch_error(record)
            logging.error("failed to save %s: %s", line.get("custom_id"), exc)

    if batch.error_file_id:
        err_content = client.files.content(batch.error_file_id).text
        for raw_line in err_content.splitlines():
            if raw_line.strip():
                append_batch_error(json.loads(raw_line))
                failures += 1

    logging.info("Batch download complete. failures=%d", failures)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
