#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
echo "Launching Hybrid MVP — choose Hybrid Tutorial or Hybrid MVP from the main menu."
exec "$ROOT/scripts/run_game.sh" "$@"
