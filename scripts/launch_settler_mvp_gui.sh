#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export GODOT="${GODOT:-/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64}"
export GODOT_USER_DATA_DIR="${GODOT_USER_DATA_DIR:-$ROOT/godot_project/.godot_user}"
exec "$GODOT" --path "$ROOT/godot_project" "$@"
