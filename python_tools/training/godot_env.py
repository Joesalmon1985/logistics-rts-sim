"""Godot headless training env client (DDR-033 file transport)."""

from __future__ import annotations

import atexit
import json
import os
import signal
import subprocess
import time
import uuid
import weakref
from pathlib import Path
from typing import Any

RESPONSE_PREFIX = "@@RUN06_JSON@@"
_ACTIVE_ENVS: weakref.WeakSet = weakref.WeakSet()


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


def default_session_dir() -> Path:
    """Gitignored IPC session dir under godot_project/.run06_sessions/."""
    base = repo_root() / "godot_project" / ".run06_sessions"
    base.mkdir(parents=True, exist_ok=True)
    return base / f"session_{uuid.uuid4().hex}"


def find_stale_training_env_processes() -> list[int]:
    """Return PIDs for headless training env processes owned by this repo."""
    repo = repo_root().as_posix()
    pattern = f"run_training_env.gd.*{repo}"
    try:
        result = subprocess.run(
            ["pgrep", "-f", pattern],
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        return []
    pids: list[int] = []
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            pids.append(int(line))
        except ValueError:
            continue
    return pids


def close_all_active_envs() -> None:
    for env in list(_ACTIVE_ENVS):
        env.close()


def cleanup_stale_training_env_processes() -> list[int]:
    """Best-effort shutdown for orphaned training env processes for this repo."""
    close_all_active_envs()
    remaining = find_stale_training_env_processes()
    for pid in remaining:
        try:
            os.kill(pid, signal.SIGTERM)
        except OSError:
            continue
    if not remaining:
        return []
    time.sleep(0.5)
    still_alive = find_stale_training_env_processes()
    for pid in still_alive:
        try:
            os.kill(pid, signal.SIGKILL)
        except OSError:
            continue
    time.sleep(0.2)
    return find_stale_training_env_processes()


class GodotHeadlessEnv:
    def __init__(self, session_dir: Path | None = None, startup_timeout: float = 3.0) -> None:
        self.repo = repo_root()
        self.session_dir = session_dir or default_session_dir()
        self.session_dir.mkdir(parents=True, exist_ok=True)
        self.logs: list[str] = []
        self.process: subprocess.Popen[str] | None = None
        self._request_seq = 0
        self._response_seq = 0
        self._closed = False
        _ACTIVE_ENVS.add(self)
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
        popen_kwargs: dict[str, Any] = {
            "cwd": str(self.repo),
            "stdout": subprocess.DEVNULL,
            "stderr": subprocess.DEVNULL,
            "text": True,
        }
        if os.name != "nt":
            popen_kwargs["start_new_session"] = True
        self.process = subprocess.Popen(cmd, **popen_kwargs)

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
        if self._closed:
            return
        self._closed = True
        if self.process is None:
            return
        if self.process.poll() is None:
            try:
                self.request({"op": "shutdown"}, timeout=3.0)
            except Exception:
                pass
            self._terminate_process()
        self.process = None

    def _terminate_process(self) -> None:
        if self.process is None or self.process.poll() is not None:
            return
        try:
            if os.name != "nt":
                os.killpg(self.process.pid, signal.SIGTERM)
            else:
                self.process.terminate()
        except OSError:
            self.process.terminate()
        try:
            self.process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            try:
                if os.name != "nt":
                    os.killpg(self.process.pid, signal.SIGKILL)
                else:
                    self.process.kill()
            except OSError:
                self.process.kill()
            self.process.wait(timeout=5)

    def __enter__(self) -> GodotHeadlessEnv:
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()

    def __del__(self) -> None:
        try:
            self.close()
        except Exception:
            pass


atexit.register(close_all_active_envs)
