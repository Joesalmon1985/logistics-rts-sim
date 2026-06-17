#!/usr/bin/env python3
"""Experimental opt-in parallel Godot test wrapper (RUN_02.8c Phase H).

Does NOT replace the serial full closure gate. Default parallelism is 2.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from typing import Any


LIST_LINE = re.compile(r"^\s+\S+\s+(res://sim/tests/\S+\.gd)\s+")


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def run_godot_list(godot: str, project: Path, env: dict[str, str], timeout_s: int) -> tuple[int, str]:
    cmd = [
        godot,
        "--headless",
        "--path",
        str(project),
        "--script",
        "res://sim/tools/run_tests.gd",
        "--",
        "--list",
    ]
    proc = subprocess.run(
        cmd,
        cwd=repo_root(),
        env=env,
        capture_output=True,
        text=True,
        timeout=timeout_s,
        check=False,
    )
    output = (proc.stdout or "") + (proc.stderr or "")
    return proc.returncode, output


def parse_list_output(output: str) -> list[str]:
    paths: list[str] = []
    for line in output.splitlines():
        match = LIST_LINE.match(line)
        if match:
            paths.append(match.group(1))
    return paths


def stable_shards(paths: list[str], workers: int) -> list[list[str]]:
    ordered = sorted(paths)
    shards: list[list[str]] = [[] for _ in range(workers)]
    for index, path in enumerate(ordered):
        shards[index % workers].append(path)
    return [shard for shard in shards if shard]


def write_test_list_file(path: Path, tests: list[str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(tests) + "\n", encoding="utf-8")


def run_worker(
    worker_id: int,
    godot: str,
    project: Path,
    tests: list[str],
    work_dir: Path,
    timeout_s: int,
) -> dict[str, Any]:
    list_file = work_dir / f"worker_{worker_id:02d}_tests.txt"
    log_file = work_dir / f"worker_{worker_id:02d}.log"
    timing_json = work_dir / f"worker_{worker_id:02d}_timing.json"
    userdata = project / ".godot_user_parallel" / f"worker_{worker_id:02d}"

    write_test_list_file(list_file, tests)

    env = os.environ.copy()
    env["GODOT_USER_DATA_DIR"] = str(userdata)

    cmd = [
        godot,
        "--headless",
        "--path",
        str(project),
        "--script",
        "res://sim/tools/run_tests.gd",
        "--",
        f"--test-list-file={list_file}",
        f"--timing-json={timing_json}",
    ]

    start = time.time()
    try:
        proc = subprocess.run(
            cmd,
            cwd=repo_root(),
            env=env,
            capture_output=True,
            text=True,
            timeout=timeout_s,
            check=False,
        )
        timed_out = False
    except subprocess.TimeoutExpired as exc:
        proc = None
        timed_out = True
        combined = (exc.stdout or "") + (exc.stderr or "")
        log_file.write_text(combined, encoding="utf-8")
        return {
            "worker_id": worker_id,
            "exit_code": 124,
            "timed_out": timed_out,
            "wall_s": time.time() - start,
            "tests": tests,
            "log_file": str(log_file),
            "timing_json": str(timing_json),
            "results": {},
        }

    combined = (proc.stdout or "") + (proc.stderr or "")
    log_file.write_text(combined, encoding="utf-8")

    results = parse_results_line(combined)
    timing_records = []
    if timing_json.is_file():
        try:
            payload = json.loads(timing_json.read_text(encoding="utf-8"))
            timing_records = payload.get("tests", [])
        except json.JSONDecodeError:
            timing_records = []

    return {
        "worker_id": worker_id,
        "exit_code": proc.returncode,
        "timed_out": timed_out,
        "wall_s": time.time() - start,
        "tests": tests,
        "log_file": str(log_file),
        "timing_json": str(timing_json),
        "results": results,
        "timing_records": timing_records,
        "script_error": "SCRIPT ERROR" in combined,
    }


def parse_results_line(output: str) -> dict[str, int]:
    for line in output.splitlines():
        if line.startswith("Results:"):
            # Results: 94/94 ran, 94 passed, 0 failed, 0 errored
            parts = line.replace("Results:", "").strip().split(",")
            values: dict[str, int] = {}
            for part in parts:
                chunk = part.strip()
                if "/" in chunk and " ran" in chunk:
                    ran_text = chunk.split(" ran")[0]
                    ran, expected = ran_text.split("/")
                    values["ran"] = int(ran.strip())
                    values["expected"] = int(expected.strip())
                elif "passed" in chunk:
                    values["passed"] = int(chunk.split()[0])
                elif "failed" in chunk:
                    values["failed"] = int(chunk.split()[0])
                elif "errored" in chunk:
                    values["errored"] = int(chunk.split()[0])
            return values
    return {}


def aggregate(worker_reports: list[dict[str, Any]], selected_count: int) -> dict[str, Any]:
    totals = {"ran": 0, "passed": 0, "failed": 0, "errored": 0}
    all_timing: list[dict[str, Any]] = []
    seen_paths: set[str] = set()
    duplicate_paths: list[str] = []
    missing = False
    worker_failures: list[str] = []

    for report in worker_reports:
        if report.get("exit_code") != 0:
            worker_failures.append(
                f"worker {report['worker_id']:02d} exit={report['exit_code']} timed_out={report.get('timed_out')}"
            )
        if report.get("script_error"):
            worker_failures.append(f"worker {report['worker_id']:02d} SCRIPT ERROR")

        results = report.get("results", {})
        for key in ("ran", "passed", "failed", "errored"):
            totals[key] = totals.get(key, 0) + int(results.get(key, 0))

        for path in report.get("tests", []):
            if path in seen_paths:
                duplicate_paths.append(path)
            seen_paths.add(path)

        for record in report.get("timing_records", []):
            all_timing.append(record)

    if len(seen_paths) != selected_count:
        missing = True

    all_timing.sort(key=lambda row: int(row.get("duration_us", 0)), reverse=True)
    slowest = all_timing[:20]

    ok = (
        not worker_failures
        and not duplicate_paths
        and not missing
        and totals.get("ran", 0) == selected_count
        and totals.get("failed", 0) == 0
        and totals.get("errored", 0) == 0
    )

    return {
        "ok": ok,
        "totals": totals,
        "selected_count": selected_count,
        "unique_ran": len(seen_paths),
        "duplicate_paths": duplicate_paths,
        "missing": missing,
        "worker_failures": worker_failures,
        "slowest": slowest,
        "wall_s": sum(float(r.get("wall_s", 0.0)) for r in worker_reports),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Parallel Godot test wrapper (experimental)")
    parser.add_argument("--godot", required=True)
    parser.add_argument("--project", default="godot_project")
    parser.add_argument("--parallel", type=int, default=2)
    parser.add_argument("--timeout-per-worker", type=int, default=580)
    parser.add_argument("--timing-json", default="")
    parser.add_argument("--work-dir", default="")
    args = parser.parse_args()

    root = repo_root()
    project = (root / args.project).resolve()
    godot = args.godot
    workers = max(1, args.parallel)

    list_env = os.environ.copy()
    if "GODOT_USER_DATA_DIR" not in list_env:
        list_env["GODOT_USER_DATA_DIR"] = str(project / ".godot_user")

    exit_code, list_output = run_godot_list(godot, project, list_env, timeout_s=120)
    if exit_code != 0:
        print("PARALLEL FAIL: --list exited", exit_code)
        print(list_output)
        return 1

    selected = parse_list_output(list_output)
    if not selected:
        print("PARALLEL FAIL: no tests parsed from --list output")
        print(list_output)
        return 1

    work_dir = Path(args.work_dir) if args.work_dir else Path(tempfile.mkdtemp(prefix="wide_rts_parallel_", dir=root / "training_runs" / "test_timings"))
    work_dir.mkdir(parents=True, exist_ok=True)

    shards = stable_shards(selected, workers)
    print(f"PARALLEL: {len(selected)} tests across {len(shards)} workers")

    worker_reports: list[dict[str, Any]] = []
    start = time.time()
    with ThreadPoolExecutor(max_workers=len(shards)) as pool:
        futures = []
        for worker_id, shard in enumerate(shards, start=1):
            print(f"PARALLEL: starting worker {worker_id:02d} ({len(shard)} tests)")
            futures.append(
                pool.submit(
                    run_worker,
                    worker_id,
                    godot,
                    project,
                    shard,
                    work_dir,
                    args.timeout_per_worker,
                )
            )
        for future in as_completed(futures):
            worker_reports.append(future.result())
    worker_reports.sort(key=lambda row: int(row.get("worker_id", 0)))

    summary = aggregate(worker_reports, len(selected))
    summary["total_wall_s"] = time.time() - start
    summary["work_dir"] = str(work_dir)
    summary["workers"] = worker_reports

    if args.timing_json:
        timing_path = Path(args.timing_json)
        timing_path.parent.mkdir(parents=True, exist_ok=True)
        timing_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
        print(f"PARALLEL: wrote aggregate timing JSON to {timing_path}")

    print("PARALLEL SUMMARY:")
    print(f"  selected={summary['selected_count']} unique_ran={summary['unique_ran']}")
    print(f"  totals={summary['totals']}")
    print(f"  total_wall_s={summary['total_wall_s']:.2f}")
    if summary["slowest"]:
        print("  slowest tests:")
        for row in summary["slowest"][:20]:
            dur_s = int(row.get("duration_us", 0)) / 1_000_000.0
            print(f"    {dur_s:.2f}s  {row.get('name', row.get('path', ''))}")

    if summary["ok"]:
        print("PARALLEL PASS")
        print("EXIT: 0 (pass)")
        return 0

    if summary["duplicate_paths"]:
        print("PARALLEL FAIL: duplicate tests:", summary["duplicate_paths"][:5])
    if summary["missing"]:
        print("PARALLEL FAIL: selected test count mismatch")
    for msg in summary["worker_failures"]:
        print("PARALLEL FAIL:", msg)
    print("PARALLEL FAIL")
    print("EXIT: 1 (fail)")
    return 1


if __name__ == "__main__":
    sys.exit(main())
