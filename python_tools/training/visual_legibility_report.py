"""Assemble visual legibility review markdown from capture output (RUN_02.5 Phase C)."""

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
            f"buildings={len(state.get('buildings', []))} units={len(state.get('units', []))}"
        )
    commands_path = rollout_dir / "commands.jsonl"
    command_line = "(command unknown)"
    if commands_path.is_file():
        for line in commands_path.read_text(encoding="utf-8").splitlines():
            if not line.strip():
                continue
            row = json.loads(line)
            if int(row.get("step_index", -1)) == step:
                command_line = (
                    f"{row.get('command_label')} — {row.get('result_message')} "
                    f"(accepted={row.get('was_accepted')})"
                )
                break
    shots = [p.name for p in sorted(capture_dir.glob(f"{step:03d}_*.png"))]
    return f"""# Step {step} review

## JSON state
{state_summary}

## Policy command (current/next decision for this snapshot step)
{command_line}

## Screenshots captured
{', '.join(shots) if shots else '(none)'}

## Review checklist
- [ ] Command visible on map or in panels
- [ ] Command result reflected in next step state
- [ ] Objective/progress visible if exported
- [ ] Missing snapshot fields noted below

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
