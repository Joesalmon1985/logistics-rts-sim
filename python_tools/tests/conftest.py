from __future__ import annotations

import os
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

DEFAULT_GODOT = (
    r"C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe"
    r"\Godot_v4.5.1-stable_win64_console.exe"
)


@pytest.fixture(scope="session", autouse=True)
def _configure_godot_env() -> None:
    if not os.environ.get("GODOT") and Path(DEFAULT_GODOT).exists():
        os.environ["GODOT"] = DEFAULT_GODOT
    os.environ.setdefault(
        "GODOT_USER_DATA_DIR",
        str(ROOT / "godot_project" / ".godot_user"),
    )


def require_godot() -> None:
    if not os.environ.get("GODOT"):
        pytest.skip("GODOT environment variable is not set")
