#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
echo "Launching Settler MVP — choose Tutorial from the main menu."
exec "$ROOT/scripts/run_game.sh" "$@"
