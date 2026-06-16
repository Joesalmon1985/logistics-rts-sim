"""Assemble visual legibility review markdown from capture output (RUN_02.5 / 02.5b)."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def build_step_review(step: int, rollout_dir: Path, capture_dir: Path) -> str:
    state_path = capture_dir / f"{step:03d}_state.json"
    state_summary = "(state file missing)"
    if state_path.is_file():
        state = json.loads(state_path.read_text(encoding="utf-8"))
        state_summary = (
            f"variant={state.get('variant')} tick={state.get('tick')} "
            f"buildings={len(state.get('buildings', []))} sites={len(state.get('construction_sites', []))} "
            f"units={len(state.get('units', []))} blocked_jobs={len(state.get('blocked_job_markers', []))}"
        )
    commands_path = rollout_dir / "commands.jsonl"
    prev_line = "(none — initial snapshot)"
    command_line = "(terminal — no further command)"
    if commands_path.is_file():
        rows = [json.loads(line) for line in commands_path.read_text(encoding="utf-8").splitlines() if line.strip()]
        if step > 0 and step - 1 < len(rows):
            prev = rows[step - 1]
            prev_line = (
                f"{prev.get('command_label')} — {prev.get('result_message')} "
                f"(accepted={prev.get('was_accepted')})"
            )
        if step < len(rows):
            cur = rows[step]
            command_line = (
                f"{cur.get('command_label')} — {cur.get('result_message')} "
                f"(accepted={cur.get('was_accepted')})"
            )
            if cur.get("selected_action_index") is not None:
                command_line += f" | action={cur.get('selected_action_index')} valid={cur.get('valid_action_count')}"
    metrics_path = rollout_dir / "metrics.json"
    outcome_line = "(metrics missing)"
    if metrics_path.is_file():
        metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
        objective = metrics.get("objective_progress", {})
        outcome_line = (
            f"outcome={metrics.get('final_outcome')} score={metrics.get('total_score')} "
            f"reason={objective.get('reason', '?') if isinstance(objective, dict) else '?'}"
        )
    manifest_path = capture_dir / "capture_manifest.json"
    manifest_note = ""
    if manifest_path.is_file():
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        warnings = manifest.get("warnings", [])
        if warnings:
            manifest_note = "Capture warnings: " + "; ".join(str(w) for w in warnings)
    shots = [p.name for p in sorted(capture_dir.glob(f"{step:03d}_*.png"))]
    return f"""# Step {step} review

## Scenario status
{outcome_line}

## JSON state
{state_summary}

## Previous command (produced this snapshot)
{prev_line}

## Current / next decision
{command_line}

## Screenshots captured
{', '.join(shots) if shots else '(none)'}

{manifest_note}

## Review checklist
- [ ] Command overlay visible (Neural mode)
- [ ] Economy/Military mode explains failure/combat state
- [ ] Right-hand panels readable at 1920×1080
- [ ] Mode screenshots differ or manifest explains why not

## Notes
(TBD during human review)
"""


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--rollout-dir", required=True)
    parser.add_argument("--capture-dir", default="")
    parser.add_argument("--output", default="")
    parser.add_argument("--steps", default="0,1,2,3")
    args = parser.parse_args()

    rollout_dir = Path(args.rollout_dir)
    capture_dir = Path(args.capture_dir) if args.capture_dir else rollout_dir / "screenshots"
    output_path = Path(args.output) if args.output else rollout_dir / "VISUAL_LEGIBILITY_REVIEW.md"

    sections: list[str] = [
        "# RUN_02.5 Visual Legibility Review\n",
        f"Rollout: `{rollout_dir}`\n",
    ]
    meta_path = rollout_dir / "metadata.json"
    if meta_path.is_file():
        meta = json.loads(meta_path.read_text(encoding="utf-8"))
        sections.append(
            f"Policy: **{meta.get('policy_type')}** | Scenario: **{meta.get('scenario_name')}** "
            f"| Seed: **{meta.get('seed')}**\n"
        )

    for part in args.steps.split(","):
        step = int(part.strip())
        review_path = capture_dir / f"{step:03d}_review.md"
        review_text = build_step_review(step, rollout_dir, capture_dir)
        review_path.write_text(review_text, encoding="utf-8")
        sections.append(review_text)
        sections.append("\n---\n")

    output_path.write_text("\n".join(sections), encoding="utf-8")
    print(f"Wrote {output_path}")


if __name__ == "__main__":
    main()
