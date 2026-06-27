#!/usr/bin/env python3
"""Generate optional placeholder notes for hybrid 3D spectator (meshes are procedural in-engine)."""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "godot_project" / "client" / "assets" / "hybrid_3d" / "asset_registry.json"


def main() -> None:
    OUT.parent.mkdir(parents=True, exist_ok=True)
    registry = {
        "schema_version": 1,
        "note": "Hybrid 3D spectator uses procedural BoxMesh/CylinderMesh in Hybrid3dWorldRenderer.",
        "concepts": [
            "headquarters",
            "woodcutter",
            "sawmill",
            "quarry",
            "barracks",
            "command_hub",
            "carrier",
            "rifle_squad",
            "road_segment",
        ],
    }
    OUT.write_text(json.dumps(registry, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
