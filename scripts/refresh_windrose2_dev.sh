#!/usr/bin/env bash
set -euo pipefail

HOST="${WINDROSE_DEV_HOST:-192.168.0.252}"
REMOTE_ROOT="${WINDROSE_DEV_ROOT:-/home/lancer1977/game_servers/windrose2-dev}"
LOCAL_IMAGE="${WINDROSE_STATE_IMAGE:-windrose-state-web-roadmap:latest}"
LOCAL_TAR="${WINDROSE_STATE_TAR:-/tmp/windrose-state-web-roadmap.tar}"
REMOTE_TAR="${WINDROSE_REMOTE_TAR:-/tmp/windrose-state-web-roadmap.tar}"
API_BASE="${WINDROSE_DEV_API_BASE:-http://127.0.0.1:8782}"

echo "[1/4] Building ${LOCAL_IMAGE}"
docker build -t "${LOCAL_IMAGE}" -f src/Windrose.StateWeb/Dockerfile .

echo "[2/4] Saving image to ${LOCAL_TAR}"
docker save "${LOCAL_IMAGE}" -o "${LOCAL_TAR}"

echo "[3/4] Copying image to ${HOST}"
scp -p "${LOCAL_TAR}" "${HOST}:${REMOTE_TAR}"

echo "[4/4] Refreshing dev stack on ${HOST}"
ssh "${HOST}" "set -euo pipefail
  docker load -i '${REMOTE_TAR}'
  cd '${REMOTE_ROOT}'
  docker compose pull windrose
  docker compose up -d --force-recreate windrose windrose-state-web
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

echo "windrose2-dev refresh complete"
