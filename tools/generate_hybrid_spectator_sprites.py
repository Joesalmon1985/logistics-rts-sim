#!/usr/bin/env python3
"""Generate placeholder hybrid spectator sprites with white outlines (PRD B.9)."""

from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "godot_project" / "client" / "assets" / "hybrid_spectator"


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    marker = OUT / "README.txt"
    marker.write_text(
        "Hybrid spectator sprites — replace with generated PNG atlases.\n"
        "Spectator reuses settler/military registries until dedicated art lands.\n",
        encoding="utf-8",
    )
    print(f"Wrote {marker}")


if __name__ == "__main__":
    main()
