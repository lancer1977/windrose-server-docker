# Windrose Sidecar Bridge Plugin

This WindrosePlus Lua plugin is the repo-owned plugin boundary for the State Web sidecar.
V4 keeps `dry-run-only` as the default and adds a dev-only execution queue/readback path for the approved `windrose2-dev` test bed. The exact entrypoint is `HandleDodoSwarm`, which can hand off to `ExecuteDodoSwarmNative` when UE4SS game-thread dispatch is available; the current proof still stops short of claiming native gameplay spawning unless the live hook is observed.

## Boundary

- Plugin: `plugins/windrose-sidecar-bridge/`
  - Loads through WindrosePlus / UE4SS Lua mod support.
  - Writes a heartbeat to `windrose_plugin_bridge/status.json`.
  - Writes typed V3 bridge event envelopes to `windrose_plugin_bridge/events/` for safe heartbeat/readback/error publishing.
  - In `dry-run-only` mode, exposes `HandleDodoSwarm` as validation/logging only.
  - In `dev-execute` mode on `windrose2-dev`, consumes approved action files from `windrose_plugin_bridge/actions/` and writes results to `windrose_plugin_bridge/results/`. The native probe is routed through `HandleDodoSwarm -> ExecuteDodoSwarmNative`, but live spawn is only proven when the result reports `nativeSpawn=true`.
- Sidecar: `src/Windrose.StateWeb/`
  - Exposes `/api/plugin/manifest`, `/api/plugin/status`, `/api/plugin/smoke-options`, `/api/plugin/actions/dry-run`, `/api/plugin/actions/execute`, `/api/plugin/actions/{actionRequestId}/result`, and `/api/plugin/events/recent`.
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

Live validation belongs on the dev stack only. Random-player probes stay read-only; any mutation smoke should use a clearly named throwaway/non-main character or recorded consent plus rollback capture.

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

For the V4 smoke matrix and its read-only/default-dry-run checklist, use `scripts/smoke_windrose_v4_matrix.sh` or the State Web readback endpoint `GET /api/plugin/smoke-options`. The matrix covers offline/mock, dev no-player, operator non-main, consenting player, random read-only probe, sidecar-down/plugin-down failure, plugin reload, and malformed command cases while keeping any player-bound mutation pinned to explicit consent or a non-main throwaway target.

## Dev-server proof

The Alienware dev stack at `/home/lancer1977/game_servers/windrose2-dev` has loaded the bridge in `dev-execute` mode for V4 queue/readback testing. The current proof is:

```text
GET http://127.0.0.1:8782/api/plugin/status -> connected=true, mode=dev-execute, message="plugin loaded; dev execution queue enabled; native actor spawn probe available"
POST http://127.0.0.1:8782/api/plugin/actions/execute -> accepted=true, queued=true, dryRun=false
GET http://127.0.0.1:8782/api/plugin/actions/{actionRequestId}/result -> status=executed, executed=true, outcome=dev-executed-plugin-writeback-no-native-spawn, nativeSpawn=false
```

This proves State Web can queue an approved dev action and WindrosePlus can consume it and write back a result. It does not yet prove in-game creature creation on its own; the live hook path is still gated by `ExecuteInGameThread` plus `HookEngineTick` / `HookUObjectProcessEvent`, so the result can still legitimately report `nativeSpawn=false` when the dispatcher is disabled.

Rollback on the dev stack:

1. Set `WINDROSE_SIDECAR_PLUGIN_MODE=dry-run-only` in `/home/lancer1977/game_servers/windrose2-dev/.env`.
2. Remove `WindroseState__PluginBridgeDevExecutionEnabled` or set it to `"false"` in `/home/lancer1977/game_servers/windrose2-dev/docker-compose.yml`.
3. Restore the State Web mount to read-only if desired: `./server-files:/server-files:ro`.
4. Run `docker compose up -d windrose windrose-state-web` from `/home/lancer1977/game_servers/windrose2-dev`.

## Known limitation

Live dodo spawning is not implemented here by default. `HandleDodoSwarm` is the entrypoint, but the native actor path is only a probe until `ExecuteDodoSwarmNative` succeeds with game-thread dispatch enabled. A real spawn still requires a proven native hook for Windrose actor/entity creation, approval/audit data from ChannelCheevos, and explicit operator authorization.

The max-teleporters-per-island value is also contract/config-only today. Live enforcement needs a native placement/counting hook that can identify the current island and reject or queue requests before they mutate the world.

The requested stack-size multiplier is contract/readback-only today. The bridge accepts `1`, `2`, or `3` so operators can declare the desired doubled/tripled stack policy, but it deliberately does not write Windrose+'s legacy `stack_size` / `inventory_size` config because that path is disabled upstream and can corrupt player inventory state.
