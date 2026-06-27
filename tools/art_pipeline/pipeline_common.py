#!/usr/bin/env python3
"""Shared helpers for the settler MVP art generation pipeline."""
from __future__ import annotations

import csv
import json
import logging
import os
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable, Iterable, TypeVar

T = TypeVar("T")

REPO_ROOT = Path(__file__).resolve().parents[2]
PIPELINE_ROOT = Path(__file__).resolve().parent
OUTPUTS_DIR = PIPELINE_ROOT / "outputs"
PROCESSED_DIR = PIPELINE_ROOT / "processed"
CONTACT_SHEETS_DIR = PIPELINE_ROOT / "contact_sheets"
BATCHES_DIR = PIPELINE_ROOT / "batches"
ART_V1_DIR = REPO_ROOT / "godot_project/client/assets/settler_mvp/art_v1"

MANIFEST_PATH = PIPELINE_ROOT / "asset_manifest.json"
TEMPLATES_PATH = PIPELINE_ROOT / "prompt_templates.json"
STYLE_BIBLE_PATH = PIPELINE_ROOT / "style_bible.md"
PROMPTS_CSV_PATH = PIPELINE_ROOT / "generated_prompts.csv"
SELECTED_ASSETS_PATH = PIPELINE_ROOT / "selected_assets.json"
BATCH_STATE_PATH = BATCHES_DIR / "batch_state.json"
BATCH_ERRORS_PATH = BATCHES_DIR / "batch_errors.jsonl"

DEFAULT_IMAGE_MODEL = "gpt-image-1"
YES_THRESHOLD = 10
MAX_RETRIES = 3

EXPORT_PREFIX = {
    "terrain": "terrain",
    "buildings": "building",
    "logistics": "logistics",
    "wares": "ware",
}


def setup_logging(verbose: bool = False) -> None:
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(level=level, format="%(levelname)s: %(message)s")


def load_dotenv_if_present() -> None:
    try:
        from dotenv import load_dotenv
    except ImportError:
        return
    env_path = REPO_ROOT / ".env"
    if env_path.is_file():
        load_dotenv(env_path)


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def load_manifest() -> list[dict[str, Any]]:
    data = load_json(MANIFEST_PATH)
    if isinstance(data, list):
        return data
    if isinstance(data, dict) and "assets" in data:
        return list(data["assets"])
    raise ValueError(f"Unexpected manifest shape in {MANIFEST_PATH}")


def manifest_by_id(manifest: Iterable[dict[str, Any]] | None = None) -> dict[str, dict[str, Any]]:
    items = list(manifest) if manifest is not None else load_manifest()
    return {str(item["asset_id"]): item for item in items}


def export_filename(entry: dict[str, Any]) -> str:
    category = str(entry["category"])
    registry_key = str(entry["registry_key"])
    prefix = EXPORT_PREFIX.get(category, category)
    return f"{prefix}_{registry_key}.png"


def godot_res_path(relative_repo_path: str) -> str:
    if relative_repo_path.startswith("res://"):
        return relative_repo_path
    if relative_repo_path.startswith("godot_project/"):
        return "res://" + relative_repo_path[len("godot_project/") :]
    return "res://client/assets/" + relative_repo_path.lstrip("/")


def load_prompts_csv(path: Path | None = None) -> list[dict[str, str]]:
    csv_path = path or PROMPTS_CSV_PATH
    if not csv_path.is_file():
        raise FileNotFoundError(f"Prompt CSV not found: {csv_path}")
    with csv_path.open(newline="", encoding="utf-8") as handle:
        return list(csv.DictReader(handle))


def write_prompts_csv(rows: list[dict[str, str]], path: Path | None = None) -> Path:
    csv_path = path or PROMPTS_CSV_PATH
    fieldnames = [
        "asset_id",
        "category",
        "variant_id",
        "target_size",
        "prompt",
        "negative_prompt_notes",
        "output_stem",
        "style_pass",
    ]
    with csv_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)
    return csv_path


def write_image_metadata(image_path: Path, metadata: dict[str, Any]) -> Path:
    meta_path = image_path.with_suffix(image_path.suffix + ".meta.json")
    meta_path.write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
    return meta_path


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def resolve_image_model(cli_model: str | None) -> str:
    if cli_model:
        return cli_model
    env_model = os.environ.get("OPENAI_IMAGE_MODEL", "").strip()
    if env_model:
        return env_model
    return DEFAULT_IMAGE_MODEL


def estimate_api_calls(rows: list[dict[str, str]], limit: int | None) -> int:
    count = len(rows) if limit is None else min(len(rows), limit)
    logging.info("Estimated API calls: %d", count)
    if count > 0:
        logging.info(
            "Approximate cost depends on model/quality/size; check OpenAI pricing before executing."
        )
    return count


def require_confirmation(
    count: int,
    *,
    yes_flag: bool,
    dry_run: bool,
    execute: bool,
) -> None:
    if dry_run or not execute:
        return
    if count > YES_THRESHOLD and not yes_flag:
        raise SystemExit(
            f"Refusing to execute {count} API calls without --yes "
            f"(threshold is {YES_THRESHOLD})."
        )


def with_retries(
    operation: Callable[[], T],
    *,
    description: str,
    max_retries: int = MAX_RETRIES,
) -> T:
    delay = 1.0
    last_error: Exception | None = None
    for attempt in range(1, max_retries + 1):
        try:
            return operation()
        except Exception as exc:  # noqa: BLE001 - retry only on transient OpenAI errors when available
            last_error = exc
            transient = _is_transient_error(exc)
            if not transient or attempt >= max_retries:
                raise
            logging.warning(
                "%s failed (attempt %d/%d): %s; retrying in %.1fs",
                description,
                attempt,
                max_retries,
                exc,
                delay,
            )
            time.sleep(delay)
            delay *= 2
    assert last_error is not None
    raise last_error


def _is_transient_error(exc: Exception) -> bool:
    try:
        from openai import APIConnectionError, InternalServerError, RateLimitError
    except ImportError:
        return False
    return isinstance(exc, (RateLimitError, APIConnectionError, InternalServerError))


def parse_custom_id(custom_id: str) -> tuple[str, str, str]:
    parts = custom_id.split("__", 2)
    if len(parts) != 3:
        raise ValueError(f"Invalid custom_id: {custom_id}")
    return parts[0], parts[1], parts[2]


def read_batch_state() -> dict[str, Any]:
    if not BATCH_STATE_PATH.is_file():
        return {}
    return load_json(BATCH_STATE_PATH)


def write_batch_state(state: dict[str, Any]) -> None:
    BATCHES_DIR.mkdir(parents=True, exist_ok=True)
    BATCH_STATE_PATH.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")


def append_batch_error(record: dict[str, Any]) -> None:
    BATCHES_DIR.mkdir(parents=True, exist_ok=True)
    with BATCH_ERRORS_PATH.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(record) + "\n")


def filter_rows(
    rows: list[dict[str, str]],
    *,
    asset_id: str | None = None,
    limit: int | None = None,
) -> list[dict[str, str]]:
    filtered = rows
    if asset_id:
        filtered = [row for row in filtered if row.get("asset_id") == asset_id]
    if limit is not None:
        filtered = filtered[:limit]
    return filtered


def ensure_dirs() -> None:
    for path in (OUTPUTS_DIR, PROCESSED_DIR, CONTACT_SHEETS_DIR, BATCHES_DIR):
        path.mkdir(parents=True, exist_ok=True)
