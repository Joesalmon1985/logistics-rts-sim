"""Godot headless training env client (DDR-033 file transport)."""

from __future__ import annotations

import json
import os
import subprocess
import time
import uuid
from pathlib import Path
from typing import Any

RESPONSE_PREFIX = "@@RUN06_JSON@@"


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def godot_binary() -> str:
    path = os.environ.get("GODOT", "")
    if not path:
        raise RuntimeError("GODOT environment variable is not set")
    return path


def default_user_data_dir() -> str:
    env = os.environ.get("GODOT_USER_DATA_DIR", "")
    if env:
        return env
    return str(repo_root() / "godot_project" / ".godot_user")


class GodotHeadlessEnv:
    def __init__(self, session_dir: Path | None = None, startup_timeout: float = 3.0) -> None:
        self.repo = repo_root()
        self.session_dir = session_dir or (
            Path(os.environ.get("TEMP", ".")) / f"run06_session_{uuid.uuid4().hex}"
        )
        self.session_dir.mkdir(parents=True, exist_ok=True)
        self.logs: list[str] = []
        self.process: subprocess.Popen[str] | None = None
        self._request_seq = 0
        self._response_seq = 0
        self._start_process()
        self._wait_for_ready(startup_timeout)

    def _start_process(self) -> None:
        session_arg = self.session_dir.resolve().as_posix()
        cmd = [
            godot_binary(),
            "--headless",
            "--path",
            str(self.repo / "godot_project"),
            "--script",
            "res://sim/tools/run_training_env.gd",
            "--",
            "--transport=file",
            f"--session-dir={session_arg}",
        ]
        self.process = subprocess.Popen(
            cmd,
            cwd=str(self.repo),
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            text=True,
        )

    def _wait_for_ready(self, timeout: float) -> None:
        time.sleep(min(timeout, 2.0))
        if self.process and self.process.poll() is not None:
            raise RuntimeError("Godot env exited during startup")

    def _drain_stdout(self) -> None:
        return

    def request(self, payload: dict[str, Any], timeout: float = 120.0) -> dict[str, Any]:
        self._response_seq += 1
        payload = dict(payload)
        payload["response_id"] = self._response_seq
        request_path = self.session_dir / f"request_{self._request_seq}.json"
        response_path = self.session_dir / f"response_{self._response_seq}.json"
        request_path.write_text(json.dumps(payload), encoding="utf-8")
        self._request_seq += 1

        deadline = time.time() + timeout
        while time.time() < deadline:
            if self.process and self.process.poll() is not None:
                raise RuntimeError("Godot env exited during request")
            if response_path.exists():
                raw = response_path.read_text(encoding="utf-8").strip()
                if raw == "":
                    time.sleep(0.01)
                    continue
                for _ in range(5):
                    try:
                        response_path.unlink(missing_ok=True)
                        break
                    except PermissionError:
                        time.sleep(0.02)
                if raw.startswith(RESPONSE_PREFIX):
                    return json.loads(raw[len(RESPONSE_PREFIX) :])
                raise RuntimeError(f"invalid response without sentinel: {raw[:200]}")
            time.sleep(0.01)
        raise TimeoutError(f"timed out waiting for Godot response to op={payload.get('op')}")

    def close(self) -> None:
        if self.process is None:
            return
        if self.process.poll() is None:
            try:
                self.request({"op": "shutdown"}, timeout=3.0)
            except Exception:
                pass
            self.process.terminate()
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait(timeout=5)
        self.process = None

    def __enter__(self) -> GodotHeadlessEnv:
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()
