# Windrose Sidecar Bridge Plugin

This WindrosePlus Lua plugin is the repo-owned plugin boundary for the State Web sidecar.
It is intentionally dry-run-only: it proves plugin installation, startup logging, and heartbeat discovery without mutating live Windrose server state.

## Boundary

- Plugin: `plugins/windrose-sidecar-bridge/`
  - Loads through WindrosePlus / UE4SS Lua mod support.
  - Writes a heartbeat to `windrose_plugin_bridge/status.json`.
  - Exposes a `HandleDodoSwarm` placeholder that logs a dry-run result only.
- Sidecar: `src/Windrose.StateWeb/`
  - Exposes `/api/plugin/manifest`, `/api/plugin/status`, and `/api/plugin/actions/dry-run`.
  - Reads the heartbeat from the shared server-files mount.
  - Validates dodo-swarm requests but never performs live execution.

## Install path

The Docker startup install script copies this folder into:

```text
server-files/windrose_plus_mods/windrose-sidecar-bridge/
```

Enable it with:

```env
WINDROSE_PLUS_ENABLED=true
WINDROSE_SIDECAR_PLUGIN_ENABLED=true
WINDROSE_STATE_WEB_URL=http://windrose-state-web:8781
WINDROSE_PLUGIN_BRIDGE_PATH=/home/steam/server-files/windrose_plugin_bridge
WINDROSE_SIDECAR_PLUGIN_MODE=dry-run-only
```

The sidecar sees the same bridge at `/server-files/windrose_plugin_bridge` through its read-only mount.

## Expected startup proof

After Windrose+ loads the plugin, the server log should contain a line like:

```text
[windrose-sidecar-bridge] loaded in dry-run-only mode; sidecar=http://windrose-state-web:8781; bridgeRoot=/home/steam/server-files/windrose_plugin_bridge
```

The sidecar status endpoint should then return `connected: true` from:

```shell
curl http://localhost:8781/api/plugin/status
```

## Local smoke proof

Run the repo-local smoke script before asking for a live dedicated-server restart:

```shell
scripts/smoke_windrose_sidecar_bridge.sh
```

The smoke script creates a disposable `server-files` tree, reuses the Windrose+ installer idempotent path, confirms the plugin lands in `windrose_plus_mods/windrose-sidecar-bridge/`, and validates `windrose_plugin_bridge/config.json`. If a Lua interpreter is available, it also executes `init.lua` and verifies the heartbeat JSON. If no Lua interpreter is present, the script still proves install/config packaging and prints the remaining live proof needed.

## Dev-server proof

The Alienware dev stack at `/home/lancer1977/game_servers/windrose2-dev` has loaded the bridge in dry-run mode. The proof captured after the dev restart was:

```text
[Lua] [windrose-sidecar-bridge] loaded in dry-run-only mode; sidecar=http://windrose-state-web:8781; bridgeRoot=Z:\home\steam\server-files\windrose_plugin_bridge
```

The matching State Web status endpoint returned `connected: true`, `status: started`, and `message: plugin loaded; dry-run native-hook seam available` from `http://127.0.0.1:8782/api/plugin/status` on the dev host.

## Known limitation

Live dodo spawning is not implemented here. A real spawn requires a proven native hook for Windrose actor/entity creation, approval/audit data from ChannelCheevos, and explicit operator authorization.
