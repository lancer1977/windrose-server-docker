#!/bin/bash
# Local smoke proof for the Windrose sidecar bridge plugin install path.
# This does not start a live Windrose dedicated server. It proves the repo-owned
# plugin is copied into the WindrosePlus friendly mods directory, writes bridge
# config, and optionally executes the Lua skeleton when a Lua interpreter exists.
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO_ROOT=$(cd "$SCRIPT_DIR/.." && pwd)
TMP_ROOT=$(mktemp -d)
trap 'rm -rf "$TMP_ROOT"' EXIT

SERVER_FILES="$TMP_ROOT/server-files"
PLUGIN_SOURCE="$REPO_ROOT/plugins/windrose-sidecar-bridge"
VERSION="sidecar-smoke"
mkdir -p "$SERVER_FILES"
printf '%s\n' "$VERSION" > "$SERVER_FILES/.windroseplus_version"

WINDROSE_PLUS_ENABLED=true \
WINDROSE_PLUS_VERSION="$VERSION" \
WINDROSE_SIDECAR_PLUGIN_ENABLED=true \
WINDROSE_STATE_WEB_URL="http://windrose-state-web:8781" \
WINDROSE_PLUGIN_BRIDGE_PATH="$SERVER_FILES/windrose_plugin_bridge" \
WINDROSE_SIDECAR_PLUGIN_MODE="dry-run-only" \
WINDROSE_MAX_TELEPORTERS_PER_ISLAND="4" \
WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER="2" \
WINDROSE_SIDECAR_PLUGIN_SOURCE_DIR="$PLUGIN_SOURCE" \
SERVER_FILES="$SERVER_FILES" \
bash "$REPO_ROOT/scripts/install_windrose_plus.sh"

TARGET_DIR="$SERVER_FILES/windrose_plus_mods/windrose-sidecar-bridge"
CONFIG_PATH="$SERVER_FILES/windrose_plugin_bridge/config.json"

for required in \
    "$TARGET_DIR/mod.json" \
    "$TARGET_DIR/init.lua" \
    "$CONFIG_PATH"; do
    if [ ! -f "$required" ]; then
        echo "smoke_windrose_sidecar_bridge: missing expected file: $required" >&2
        exit 1
    fi
done

jq -e \
    --arg sidecarUrl "http://windrose-state-web:8781" \
    --arg bridgePath "$SERVER_FILES/windrose_plugin_bridge" \
    '.pluginId == "windrose-sidecar-bridge" and .sidecarUrl == $sidecarUrl and .bridgePath == $bridgePath and .mode == "dry-run-only" and .liveExecution == false and .limits.maxTeleportersPerIsland == 4 and .limits.requestedStackSizeMultiplier == 2 and .limits.stackSizeEnforcement == "disabled-upstream-no-live-write"' \
    "$CONFIG_PATH" >/dev/null

echo "install proof: copied plugin into $TARGET_DIR"
echo "install proof: wrote bridge config $CONFIG_PATH"

LUA_BIN=""
for candidate in lua lua5.4 lua5.3 luajit; do
    if command -v "$candidate" >/dev/null 2>&1; then
        LUA_BIN="$candidate"
        break
    fi
done

if [ -z "$LUA_BIN" ]; then
    echo "lua proof: skipped; no lua/lua5.4/lua5.3/luajit interpreter found"
    echo "next live proof: confirm UE4SS/WindrosePlus emits [windrose-sidecar-bridge] loaded in dedicated-server logs"
    exit 0
fi

LUA_BRIDGE_ROOT="$TMP_ROOT/lua-bridge"
LUA_LOG="$TMP_ROOT/lua-plugin.log"
WINDROSE_PLUGIN_BRIDGE_PATH="$LUA_BRIDGE_ROOT" \
WINDROSE_STATE_WEB_URL="http://windrose-state-web:8781" \
WINDROSE_SIDECAR_PLUGIN_MODE="dry-run-only" \
WINDROSE_MAX_TELEPORTERS_PER_ISLAND="4" \
WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER="2" \
    "$LUA_BIN" "$TARGET_DIR/init.lua" > "$LUA_LOG" 2>&1

set -- "$LUA_BRIDGE_ROOT"/events/heartbeat-*.json
HEARTBEAT_EVENT=${1:-}
if [ ! -f "$HEARTBEAT_EVENT" ]; then
    echo "smoke_windrose_sidecar_bridge: missing heartbeat event file in $LUA_BRIDGE_ROOT/events" >&2
    exit 1
fi

jq -e '.messageType == "windrose.heartbeat.v3" and .schemaVersion == "windrose.plugin_sidecar.v3" and .componentId == "windrose-sidecar-bridge" and .status == "healthy"' \
    "$HEARTBEAT_EVENT" >/dev/null

grep -F "[windrose-sidecar-bridge] loaded in dry-run-only mode" "$LUA_LOG" >/dev/null
grep -F "policy maxTeleportersPerIsland=4 enforcement=contract-only" "$LUA_LOG" >/dev/null
grep -F "policy requestedStackSizeMultiplier=2 enforcement=disabled-upstream-no-live-write" "$LUA_LOG" >/dev/null
jq -e '.pluginId == "windrose-sidecar-bridge" and .status == "started" and .mode == "dry-run-only" and .limits.maxTeleportersPerIsland == 4 and .limits.requestedStackSizeMultiplier == 2 and .limits.stackSizeEnforcement == "disabled-upstream-no-live-write"' \
    "$LUA_BRIDGE_ROOT/status.json" >/dev/null

echo "lua proof: $LUA_BIN executed plugin skeleton and wrote $LUA_BRIDGE_ROOT/status.json"
