#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DESKTOP_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
DESKTOP_FILE="$DESKTOP_DIR/logistics-rts-sim.desktop"
mkdir -p "$DESKTOP_DIR"
cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Type=Application
Name=Logistics RTS Sim
Comment=Launch WideRTS Settler MVP
Exec=$ROOT/scripts/run_game.sh
Path=$ROOT
Terminal=false
Categories=Game;
EOF
chmod +x "$ROOT/scripts/run_game.sh"
echo "Created $DESKTOP_FILE"
