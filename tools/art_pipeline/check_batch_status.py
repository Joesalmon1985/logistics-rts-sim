#!/usr/bin/env python3
"""Check status of a submitted OpenAI Batch job."""
from __future__ import annotations

import argparse
import logging
import os
import sys

from pipeline_common import load_dotenv_if_present, read_batch_state, setup_logging


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
    counts = batch.request_counts
    logging.info("batch_id: %s", batch.id)
    logging.info("status: %s", batch.status)
    if counts:
        logging.info(
            "requests: total=%s completed=%s failed=%s",
            counts.total,
            counts.completed,
            counts.failed,
        )
    if batch.output_file_id:
        logging.info("output_file_id: %s", batch.output_file_id)
    if batch.error_file_id:
        logging.info("error_file_id: %s", batch.error_file_id)
    return 0


if __name__ == "__main__":
    sys.exit(main())
