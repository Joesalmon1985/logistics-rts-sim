#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export GODOT="${GODOT:-/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64}"
export GODOT_USER_DATA_DIR="${GODOT_USER_DATA_DIR:-$ROOT/godot_project/.godot_user}"
if [[ ! -x "$GODOT" ]]; then
	echo "ERROR: Godot not found at: $GODOT" >&2
	exit 127
fi
exec "$GODOT" --path "$ROOT/godot_project" --resolution 1920x1080 res://client/scenes/hybrid_3d_bot_spectator.tscn "$@"
