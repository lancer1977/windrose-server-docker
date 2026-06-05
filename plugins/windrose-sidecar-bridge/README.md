# Windrose Sidecar Bridge Plugin

This WindrosePlus Lua plugin is the repo-owned plugin boundary for the State Web sidecar.
V3 adds a dev-only execution queue/readback path for the approved `windrose2-dev` test bed. It proves that WindrosePlus can consume an approved command file and write a result, but native gameplay spawning still remains disabled until a real actor hook is proven.

## Boundary

- Plugin: `plugins/windrose-sidecar-bridge/`
  - Loads through WindrosePlus / UE4SS Lua mod support.
  - Writes a heartbeat to `windrose_plugin_bridge/status.json`.
  - In `dry-run-only` mode, exposes `HandleDodoSwarm` as validation/logging only.
  - In `dev-execute` mode on `windrose2-dev`, consumes approved action files from `windrose_plugin_bridge/actions/` and writes results to `windrose_plugin_bridge/results/` with `nativeSpawn=false`.
- Sidecar: `src/Windrose.StateWeb/`
  - Exposes `/api/plugin/manifest`, `/api/plugin/status`, `/api/plugin/smoke-options`, `/api/plugin/actions/dry-run`, `/api/plugin/actions/execute`, and `/api/plugin/actions/{actionRequestId}/result`.
  - Reads the heartbeat from the shared server-files mount.
  - Queues approved dev-only dodo-swarm requests when `WindroseState__PluginBridgeDevExecutionEnabled=true`; otherwise execution requests are rejected.

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
WINDROSE_MAX_TELEPORTERS_PER_ISLAND=3
WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER=1
```

Only on the approved dev stack, switch the plugin to queue-consuming mode and enable State Web writes:

```env
WINDROSE_SIDECAR_PLUGIN_MODE=dev-execute
WindroseState__PluginBridgeDevExecutionEnabled=true
```

The sidecar sees the same bridge at `/server-files/windrose_plugin_bridge`. Keep that mount read-only for observer/dry-run stacks; on the approved `windrose2-dev` queue/readback smoke it must be writable so State Web can create action files and read plugin results. The installer writes `WINDROSE_MAX_TELEPORTERS_PER_ISLAND` into `windrose_plugin_bridge/config.json` as `limits.maxTeleportersPerIsland`; the plugin echoes the same value in `status.json` so ChannelCheevos and tests can read the server's teleporter policy. It also writes `WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER` as `limits.requestedStackSizeMultiplier` with `limits.stackSizeEnforcement=disabled-upstream-no-live-write` so doubled/tripled stack-size intent is visible without mutating unsafe inventory settings.

## Expected startup proof

After Windrose+ loads the plugin, the server log should contain a line like:

```text
[windrose-sidecar-bridge] loaded in dry-run-only mode; sidecar=http://windrose-state-web:8781; bridgeRoot=/home/steam/server-files/windrose_plugin_bridge
[windrose-sidecar-bridge] policy maxTeleportersPerIsland=3 enforcement=contract-only
[windrose-sidecar-bridge] policy requestedStackSizeMultiplier=1 enforcement=disabled-upstream-no-live-write
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

For any live-server smoke beyond this local harness, use `docs/roadmaps/windrose-runtime-control-surface/safe-smoke-harness-matrix.md` or the State Web readback endpoint `GET /api/plugin/smoke-options` so the run stays dev-only, read-only probes stay read-only, and any player-bound mutation requires explicit consent or a non-main throwaway target.

## Dev-server proof

The Alienware dev stack at `/home/lancer1977/game_servers/windrose2-dev` has loaded the bridge in `dev-execute` mode for V3 queue/readback testing. The current proof is:

```text
GET http://127.0.0.1:8782/api/plugin/status -> connected=true, mode=dev-execute, message="plugin loaded; dev execution queue enabled; native gameplay spawn unavailable"
POST http://127.0.0.1:8782/api/plugin/actions/execute -> accepted=true, queued=true, dryRun=false
GET http://127.0.0.1:8782/api/plugin/actions/{actionRequestId}/result -> status=executed, executed=true, outcome=dev-executed-plugin-writeback-no-native-spawn, nativeSpawn=false
```

This proves State Web can queue an approved dev action and WindrosePlus can consume it and write back a result. It does not prove in-game creature creation; the result explicitly reports `nativeSpawn=false`.

Rollback on the dev stack:

1. Set `WINDROSE_SIDECAR_PLUGIN_MODE=dry-run-only` in `/home/lancer1977/game_servers/windrose2-dev/.env`.
2. Remove `WindroseState__PluginBridgeDevExecutionEnabled` or set it to `"false"` in `/home/lancer1977/game_servers/windrose2-dev/docker-compose.yml`.
3. Restore the State Web mount to read-only if desired: `./server-files:/server-files:ro`.
4. Run `docker compose up -d windrose windrose-state-web` from `/home/lancer1977/game_servers/windrose2-dev`.

## Known limitation

Live dodo spawning is not implemented here. V3 `dev-execute` means command-file consumption and result writeback only; it does not create actors in-game. A real spawn requires a proven native hook for Windrose actor/entity creation, approval/audit data from ChannelCheevos, and explicit operator authorization.

The max-teleporters-per-island value is also contract/config-only today. Live enforcement needs a native placement/counting hook that can identify the current island and reject or queue requests before they mutate the world.

The requested stack-size multiplier is contract/readback-only today. The bridge accepts `1`, `2`, or `3` so operators can declare the desired doubled/tripled stack policy, but it deliberately does not write Windrose+'s legacy `stack_size` / `inventory_size` config because that path is disabled upstream and can corrupt player inventory state.
