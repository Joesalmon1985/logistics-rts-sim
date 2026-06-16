"""Batch visual review capture, manifest checks, and contact sheets (RUN_02.5b)."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
from pathlib import Path
from typing import Any

from python_tools.training.godot_env import godot_binary, repo_root


DEFAULT_ROLLOUTS: list[dict[str, Any]] = [
    {
        "id": "economy_random_s11",
        "scenario": "scenarios/economy/eval_build_obvious.json",
        "seed": 11,
        "policy": "random",
    },
    {
        "id": "economy_untrained_s11",
        "scenario": "scenarios/economy/eval_build_obvious.json",
        "seed": 11,
        "policy": "untrained",
    },
    {
        "id": "economy_trained_s11",
        "scenario": "scenarios/economy/eval_build_obvious.json",
        "seed": 11,
        "policy": "trained",
    },
    {
        "id": "military_random_s21",
        "scenario": "scenarios/military/eval_focus_fire_hub.json",
        "seed": 21,
        "policy": "random",
    },
    {
        "id": "military_untrained_s21",
        "scenario": "scenarios/military/eval_focus_fire_hub.json",
        "seed": 21,
        "policy": "untrained",
    },
    {
        "id": "military_trained_s21",
        "scenario": "scenarios/military/eval_focus_fire_hub.json",
        "seed": 21,
        "policy": "trained",
    },
]


def rollout_root(base: Path) -> Path:
    return base / "training_runs" / "visual_rollouts"


def choose_capture_steps(command_count: int) -> list[int]:
    steps = [0]
    if command_count >= 10:
        steps.append(10)
    if command_count > 0:
        steps.append(command_count)
    return sorted(set(steps))


def load_manifest(capture_dir: Path) -> dict[str, Any]:
    path = capture_dir / "capture_manifest.json"
    if not path.is_file():
        return {"missing_manifest": True, "capture_dir": str(capture_dir)}
    return json.loads(path.read_text(encoding="utf-8"))


def check_screenshot_coverage(manifest: dict[str, Any], rollout_dir: Path) -> list[str]:
    issues: list[str] = []
    if manifest.get("missing_manifest"):
        return ["missing capture_manifest.json"]
    expected_steps = manifest.get("captured_steps", [])
    expected_modes = manifest.get("captured_modes", [])
    screenshots = manifest.get("screenshots", [])
    for step in expected_steps:
        for mode in expected_modes:
            matches = [s for s in screenshots if s.get("step_index") == step and s.get("mode") == mode]
            if not matches:
                issues.append(f"missing screenshot for step={step} mode={mode}")
    if manifest.get("screenshots_intentionally_unavailable"):
        reason = manifest.get("trained_unavailable_reason") or manifest.get("unavailable_reason")
        if not reason:
            issues.append("screenshots marked unavailable without reason")
    meta_path = rollout_dir / "metadata.json"
    if meta_path.is_file():
        meta = json.loads(meta_path.read_text(encoding="utf-8"))
        if meta.get("policy_type") == "trained" and meta.get("trained_available") is False:
            if not manifest.get("trained_unavailable_reason"):
                issues.append("trained rollout missing trained_unavailable_reason")
    return issues


def check_mode_uniqueness(manifest: dict[str, Any]) -> list[str]:
    issues: list[str] = []
    warnings = manifest.get("warnings", [])
    for warning in warnings:
        if "byte-identical" in str(warning):
            issues.append(str(warning))
    return issues


def check_panel_markup(manifest: dict[str, Any], capture_dir: Path) -> list[str]:
    issues: list[str] = []
    for shot in manifest.get("screenshots", []):
        overlay = shot.get("overlay", {})
        if overlay.get("overlay_available") is False and shot.get("mode") == "neural":
            issues.append(f"neural step {shot.get('step_index')} missing command overlay metadata")
        path = Path(str(shot.get("path", "")))
        if path.is_file() is False:
            continue
    review_md = capture_dir / "VISUAL_LEGIBILITY_REVIEW.md"
    if review_md.is_file() and "[b]" in review_md.read_text(encoding="utf-8"):
        issues.append("review markdown still contains literal BBCode markers")
    return issues


def write_contact_sheet_html(capture_dir: Path, output_path: Path, title: str) -> None:
    rows: list[str] = []
    for png in sorted(capture_dir.glob("*_*.png")):
        if png.name.endswith("_state.png"):
            continue
        rows.append(
            f'<div><img src="{png.name}" width="480" /><div>{png.stem}</div></div>'
        )
    write_contact_sheet_html_rows(output_path, title, rows)


def write_contact_sheet_html_rows(output_path: Path, title: str, rows: list[str]) -> None:
    html = f"""<!doctype html>
<html><head><meta charset="utf-8"><title>{title}</title>
<style>body{{font-family:sans-serif;background:#111;color:#eee}}
.grid{{display:grid;grid-template-columns:repeat(auto-fill,minmax(500px,1fr));gap:12px}}
div{{background:#222;padding:8px;border-radius:6px}}</style></head>
<body><h1>{title}</h1><div class="grid">{''.join(rows)}</div></body></html>"""
    output_path.write_text(html, encoding="utf-8")


def write_overall_contact_sheet(overall_dir: Path, base: Path) -> None:
    rows: list[str] = []
    for spec in DEFAULT_ROLLOUTS:
        capture_dir = rollout_root(base) / spec["id"] / "screenshots"
        for step in (0, 10):
            png = capture_dir / f"{step:03d}_world.png"
            if not png.is_file():
                continue
            href = Path(os.path.relpath(png, overall_dir)).as_posix()
            rows.append(
                f'<div><img src="{href}" width="480" /><div>{spec["id"]} step {step}</div></div>'
            )
    if rows:
        overall_dir.mkdir(parents=True, exist_ok=True)
        write_contact_sheet_html_rows(overall_dir / "overall_contact_sheet.html", "Overall review", rows)


def run_capture(rollout_dir: Path, steps: list[int], godot: str) -> subprocess.CompletedProcess[str]:
    capture_dir = rollout_dir / "screenshots"
    capture_dir.mkdir(parents=True, exist_ok=True)
    cmd = [
        godot,
        "--path",
        str(repo_root() / "godot_project"),
        "--resolution",
        "1920x1080",
        "--script",
        "res://sim/tools/run_rollout_review_capture.gd",
        "--",
        f"--rollout-path={rollout_dir}",
        f"--output={capture_dir}",
        f"--steps={','.join(str(s) for s in steps)}",
    ]
    return subprocess.run(cmd, capture_output=True, text=True, cwd=repo_root())


def export_rollout(spec: dict[str, Any], base: Path) -> Path:
    output_dir = rollout_root(base) / str(spec["id"])
    cmd = [
        "python3",
        "-m",
        "python_tools.training.visual_rollout_export",
        "--scenario",
        str(spec["scenario"]),
        "--seed",
        str(spec["seed"]),
        "--policy",
        str(spec["policy"]),
        "--max-steps",
        "30",
        "--output",
        str(output_dir),
    ]
    subprocess.run(cmd, check=True, cwd=repo_root())
    return output_dir


def batch_review(
    *,
    base: Path | None = None,
    export_missing: bool = False,
    capture: bool = True,
    godot: str | None = None,
) -> dict[str, Any]:
    root = base or repo_root()
    godot_bin = godot or godot_binary()
    summary: dict[str, Any] = {"rollouts": [], "issues": []}

    for spec in DEFAULT_ROLLOUTS:
        rollout_dir = rollout_root(root) / spec["id"]
        if export_missing and not (rollout_dir / "metadata.json").is_file():
            try:
                export_rollout(spec, root)
            except Exception as exc:  # noqa: BLE001 - report all rollout failures
                summary["issues"].append(f"{spec['id']} export failed: {exc}")
                summary["rollouts"].append({"id": spec["id"], "status": "export_failed"})
                continue
        if not (rollout_dir / "metadata.json").is_file():
            summary["issues"].append(f"{spec['id']} rollout folder missing")
            summary["rollouts"].append({"id": spec["id"], "status": "missing"})
            continue

        meta = json.loads((rollout_dir / "metadata.json").read_text(encoding="utf-8"))
        if meta.get("policy_type") == "trained" and meta.get("trained_available") is False:
            entry = {
                "id": spec["id"],
                "status": "trained_unavailable",
                "reason": meta.get("trained_absence_reason"),
            }
            summary["rollouts"].append(entry)
            continue

        commands = (rollout_dir / "commands.jsonl").read_text(encoding="utf-8").strip().splitlines()
        steps = choose_capture_steps(len(commands))
        capture_dir = rollout_dir / "screenshots"
        entry = {"id": spec["id"], "steps": steps, "status": "ok"}

        if capture:
            result = run_capture(rollout_dir, steps, godot_bin)
            entry["capture_exit_code"] = result.returncode
            if result.returncode != 0:
                entry["status"] = "capture_failed"
                summary["issues"].append(f"{spec['id']} capture failed: {result.stderr[-500:]}")
            manifest = load_manifest(capture_dir)
            entry["manifest"] = manifest
            entry["coverage_issues"] = check_screenshot_coverage(manifest, rollout_dir)
            entry["mode_issues"] = check_mode_uniqueness(manifest)
            entry["panel_issues"] = check_panel_markup(manifest, capture_dir)
            summary["issues"].extend(entry["coverage_issues"])
            summary["issues"].extend(entry["mode_issues"])
            summary["issues"].extend(entry["panel_issues"])
            write_contact_sheet_html(
                capture_dir,
                capture_dir / "contact_sheet.html",
                f"{spec['id']} contact sheet",
            )
        summary["rollouts"].append(entry)

    overall_dir = rollout_root(root) / "_review"
    overall_dir.mkdir(parents=True, exist_ok=True)
    overall_manifest = {
        "schema_version": 1,
        "rollouts": summary["rollouts"],
        "issues": summary["issues"],
    }
    (overall_dir / "overall_manifest.json").write_text(json.dumps(overall_manifest, indent=2), encoding="utf-8")
    write_overall_contact_sheet(overall_dir, root)

    return summary


def main() -> None:
    parser = argparse.ArgumentParser(description="Batch rollout visual review capture")
    parser.add_argument("--export-missing", action="store_true")
    parser.add_argument("--skip-capture", action="store_true")
    args = parser.parse_args()
    summary = batch_review(export_missing=args.export_missing, capture=not args.skip_capture)
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
