#!/usr/bin/env bash
set -euo pipefail

HOST="${WINDROSE_DEV_HOST:-192.168.0.252}"
REMOTE_ROOT="${WINDROSE_DEV_ROOT:-/home/lancer1977/game_servers/windrose2-dev}"
API_BASE="${WINDROSE_DEV_API_BASE:-http://127.0.0.1:8782}"

echo "[1/2] Restarting dev stack on ${HOST}"
ssh "${HOST}" "set -euo pipefail
  cd '${REMOTE_ROOT}'
  docker compose up -d --force-recreate --no-build windrose windrose-state-web
  for attempt in \$(seq 1 60); do
    if curl -fsS '${API_BASE}/health' >/dev/null 2>&1; then
      break
    fi
    sleep 2
  done
  curl -fsS '${API_BASE}/health' >/dev/null
  curl -fsS '${API_BASE}/api/saves/latest/observed-families' >/dev/null
  docker ps --format 'table {{.Names}}\t{{.Image}}\t{{.Ports}}' | grep -E '^windrose2-dev|^windrose-state-web-dev'
"

echo "windrose2-dev restart complete"
