![marketing_assets_banner](https://github.com/user-attachments/assets/b8b4ae5c-06bb-46a7-8d94-903a04595036)
[![GitHub License](https://img.shields.io/github/license/indifferentbroccoli/windrose-server-docker?style=for-the-badge&color=6aa84f)](https://github.com/indifferentbroccoli/windrose-server-docker/blob/main/LICENSE)
[![GitHub Release](https://img.shields.io/github/v/release/indifferentbroccoli/windrose-server-docker?style=for-the-badge&color=6aa84f)](https://github.com/indifferentbroccoli/windrose-server-docker/releases)
[![GitHub Repo stars](https://img.shields.io/github/stars/indifferentbroccoli/windrose-server-docker?style=for-the-badge&color=6aa84f)](https://github.com/indifferentbroccoli/windrose-server-docker)
[![Discord](https://img.shields.io/discord/798321161082896395?style=for-the-badge&label=Discord&labelColor=5865F2&color=6aa84f)](https://discord.gg/indifferentbroccoli)
[![Docker Pulls](https://img.shields.io/docker/pulls/indifferentbroccoli/windrose-server-docker?style=for-the-badge&color=6aa84f)](https://hub.docker.com/r/indifferentbroccoli/windrose-server-docker)

Game server hosting · Fast RAM · High-speed internet · Eat lag for breakfast

[Try our Windrose server hosting free for 2 days!](https://indifferentbroccoli.com/windrose-server-hosting)

## Tags

- docker
- windrose-server-docker
- server
- dotnet
- testing
- windrose

## Windrose Dedicated Server Docker

A Docker container for running a Windrose dedicated server. The server binary is Windows-only and runs via Wine.

## Project Docs

Additional implementation notes and planning docs live under [`docs/`](docs/README.md).

- [`docs/features/`](docs/features/README.md) collects feature-focused docs and capability maps.
- [`docs/roadmaps/`](docs/roadmaps/README.md) collects phased work, backlog notes, and completion guides.
- [`docs/features/server-state-observability/`](docs/features/server-state-observability/README.md) tracks log, save, and checkpoint surfaces for exposing server state.
- [`docs/roadmaps/companion-state-webserver/`](docs/roadmaps/companion-state-webserver/README.md) tracks the phased plan for a companion-style webserver/API.
- [`docs/roadmaps/windrose-runtime-control-surface/`](docs/roadmaps/windrose-runtime-control-surface/README.md) tracks the live-mutation backlog, operator contract, and possibility atlas for WindrosePlus and future write-capable surfaces.
- Current Windrose Kanban references: `t_46058fee` (readiness/dry-run validation), `t_77de9b18` (approved Adventurer dry-run evidence), `t_971cd00d` (operator non-main smoke), and `t_02b23bc0` (typed plugin-sidecar V3 contract, blocked for review).
- [`src/Windrose.StateWeb.Core/`](src/Windrose.StateWeb.Core/Windrose.StateWeb.Core.csproj) holds the reusable payload/response models and helper abstractions shared by Windrose and future consumers.
- The shared core layer is packable and published to nuget.org from GitHub Actions for downstream consumers that should not depend on this repository as a sibling checkout.

## Server Requirements

| | 2 Players | 4 Players | 10 Players |
|--|-----------|-----------|------------|
| CPU | 2 cores @ 3.2 GHz | 2 cores @ 3.2 GHz | 2 cores @ 3.2 GHz |
| RAM | 8 GB | 12 GB | 16 GB |
| Storage | 35 GB SSD | 35 GB SSD | 35 GB SSD |

## How to use

Copy the `.env.example` file to `.env`, fill in your values, then use either `docker compose` or `docker run`.

### Docker Compose

```yaml
services:
  windrose:
    image: indifferentbroccoli/windrose-server-docker
    restart: unless-stopped
    container_name: windrose
    stop_grace_period: 30s
    env_file:
      - .env
    volumes:
      - ./server-files:/home/steam/server-files
```

The compose file also includes an optional internal state dashboard sidecar at `http://<host>:8781`. It mounts `./server-files` read-only and exposes parsed server state, player lifecycle events, and save metadata.
The dashboard is read-only, but it still surfaces sensitive server metadata such as player names, account ids, invite codes, and backup details. Do not expose it directly to the public internet without an access-control layer.
If you want the sidecar to push live state into `channel-cheevos`, set the `WINDROSE_STATE_*` env vars for the `windrose-state-web` service. The push is off by default and uses a shared webkey in the query string. You can also select a live-push target with `WINDROSE_STATE_CHANNEL_CHEEVOS_TARGET=dev|debug|prod` and point that target at a matching hub URL and webkey pair without changing application code.
The read-only ChannelCheevos polling/display sequence is tracked by Hermes Kanban `t_355a3ed5` with docs mirror `t_8bd3aa11`. Dependency order is: ChannelCheevos read-only state contract `t_a49b59df` (closed via `t_80caec3f`), Windrose polling/UI `t_e81ad14c` (closed via `t_e5428f1b`, `t_f9a3cd43`, `t_82824d04`), docs mirror, then validation gate `t_9c674710`. Keep this path display-only and secret-free: no raw webkeys in docs/logs/tests, no writeback to ChannelCheevos, no Portainer/deploy mutation, and no gameplay mutation from this mirror.
Set `WindroseState__RedactSensitiveMetadata=true` if you want the state web responses to mask invite codes, server names, and player identity fields for broader sharing.

Live validation should stay on a dev server only. Treat random-player probes as read-only, and use a clearly named throwaway/non-main character or recorded consent before any smoke that could mutate player state.

The sidecar also supports optional Microsoft logging to Seq. Set `Seq__ServerUrl` and `Seq__ApiKey` for the `windrose-state-web` service to forward structured logs to `seq.polyhydragames.com` or another Seq instance using the same `ILogger` pipeline as the rest of the app.

For LAN or direct-IP testing, use `docker-compose.host.yml` instead:

```yaml
services:
  windrose:
    image: indifferentbroccoli/windrose-server-docker
    platform: linux/amd64
    restart: unless-stopped
    network_mode: host
    env_file:
      - .env
    volumes:
      - ./server-files:/home/steam/server-files
```

```shell
docker compose up -d
# or for host networking
docker compose -f docker-compose.yml -f docker-compose.host.yml up -d
```

### Docker Run

```shell
docker run -d \
    --restart unless-stopped \
    --name windrose \
    --stop-timeout 30 \
    --env-file .env \
    -v ./server-files:/home/steam/server-files \
    indifferentbroccoli/windrose-server-docker
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PUID` | `1000` | User ID to run the server process as |
| `PGID` | `1000` | Group ID to run the server process as |
| `UPDATE_ON_START` | `true` | Download and validate server files on every startup. Set to `false` to skip. |
| `USE_DIRECT_CONNECTION` | `false` | Set to `true` to connect to your server via IP and port instead of invite code. |
| `SERVER_PORT` | `7777` | Only applies if `USE_DIRECT_CONNECTION=true`. Port for direct connection. Requires both TCP and UDP. |
| `DIRECT_CONNECTION_PROXY_ADDRESS` | `0.0.0.0` | Only applies if `USE_DIRECT_CONNECTION=true`. Address for the direct connection proxy. |
| `USER_SELECTED_REGION` | | Region for the connection service. Leave empty to auto-select. Options: `SEA`, `CIS`, `EU` |
| `INVITE_CODE` | | Invite code players use to connect if `USE_DIRECT_CONNECTION=false` (default). Min 6 characters, `0-9 a-z A-Z`, case sensitive |
| `SERVER_NAME` | | Display name for your server |
| `SERVER_PASSWORD` | | Leave empty for a public server |
| `MAX_PLAYERS` | `10` | Maximum number of simultaneous players |
| `P2P_PROXY_ADDRESS` | `127.0.0.1` | IP address the P2P proxy binds to. Use `127.0.0.1` (default) in Docker — the proxy is an internal socket and does not need to be reachable from outside the container |
| `GENERATE_SETTINGS` | `true` | Set to `false` to skip all config generation and patching. The server will start using whatever is already in `ServerDescription.json` on disk or create a new one. |
| `RUN_WORLD_DESCRIPTION_UPDATER` | `false` | Set to `true` to run `R5WorldDescriptionUpdater.exe` at startup. The container reads `WorldIslandId` from `ServerDescription.json`, finds the matching `WorldDescription.json`, and runs the updater before launch. |
| `WINE_VERBOSE` | `false` | Set to `true` to enable verbose Wine logging. Useful for diagnosing Wine crashes. Enables `WINEDEBUG=+all` and surfaces Wine output directly in the container logs. |
| `SERVER_ARGS` | `-log -STDOUT` | Extra arguments passed to the Windrose server executable. Use this to test flags like `-nullrhi` and `-nosound`. |
| `DIAGNOSTIC_MODE` | `false` | Set to `true` to use a narrower Wine trace (`+seh,+tid,+timestamp`) and a diagnostic server launch (`-log -STDOUT -nullrhi -nosound`). |

## UE4SS (optional)

[UE4SS](https://github.com/UE4SS-RE/RE-UE4SS) is a Lua scripting and modding framework for Unreal Engine games.

> [!NOTE]
> `UE4SS_ENABLED` is not needed if `WINDROSE_PLUS_ENABLED=true` — Windrose+ installs and manages its own compatible UE4SS version automatically.

| Variable | Default | Description |
|----------|---------|-------------|
| `UE4SS_ENABLED` | `false` | Set to `true` to install UE4SS standalone. Automatically enabled by Windrose+. |

## Windrose+ (optional)

[Windrose+](https://github.com/humangenome/WindrosePlus) is a third-party, server-only enhancement for Windrose dedicated servers. It adds a live map, a web RCON dashboard, external server-browser query support, multipliers, 2,400+ INI overrides, and Lua mod support. No client mods are required. Enabling Windrose+ automatically installs UE4SS.

Enable by setting `WINDROSE_PLUS_ENABLED=true` in your `.env`, then start the container. The dashboard is exposed on port `8780`.

### Upgrading / downgrading

The image ships with the latest Windrose+ version. To use a different version, set `WINDROSE_PLUS_VERSION=vX.Y.Z` (must match a [GitHub release tag](https://github.com/humangenome/WindrosePlus/releases)) and restart the container. Leave `WINDROSE_PLUS_VERSION` empty to use the latest release.

### Config changes

Edit `server-files/windrose_plus.json` (multipliers, feature flags) or any `server-files/windrose_plus*.ini` (advanced stat overrides), then restart the container — the config takes effect on the next boot. Restarts without config changes cost no extra startup time.

Do not use the legacy `stack_size` / `inventory_size` multiplier keys for bigger stacks or slots. Upstream Windrose+ currently keeps those inventory-affecting settings disabled/no-op because they can be written into player save state and crash Windrose's inventory validator. Treat doubled/tripled stack size as a native-hook or upstream feature request, not a safe config-only change.

RCON password, admin Steam IDs, and feature flags are re-read live from `windrose_plus.json` while the server is running — no restart required for those.

### Windrose+ environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `WINDROSE_PLUS_ENABLED` | `false` | Set to `true` to enable the addon. Automatically enables UE4SS. |
| `WINDROSE_PLUS_VERSION` | baked-in default | GitHub release tag of Windrose+ to install. Leave empty for the image default. |
| `WINDROSE_PLUS_DASHBOARD_PORT` | `8780` | Port the web dashboard listens on inside the container. |
| `WINDROSE_PLUS_RCON_PASSWORD` | (empty → random) | Dashboard login password. Only applied when `windrose_plus.json` does not exist yet. |
| `WINDROSE_SIDECAR_PLUGIN_ENABLED` | `false` | Installs the repo-owned `windrose-sidecar-bridge` Windrose+ Lua plugin into `server-files/windrose_plus_mods/`. Requires `WINDROSE_PLUS_ENABLED=true`. |
| `WINDROSE_STATE_WEB_URL` | `http://windrose-state-web:8781` | Sidecar URL written into the bridge plugin config and heartbeat context. |
| `WINDROSE_PLUGIN_BRIDGE_PATH` | `/home/steam/server-files/windrose_plugin_bridge` | Shared path where the plugin writes `status.json` and reads future action/result files. |
| `WINDROSE_SIDECAR_PLUGIN_MODE` | `dry-run-only` | Current bridge mode. Keep `dry-run-only` until a native Windrose spawn hook is proven and reviewed. |
| `WINDROSE_MAX_TELEPORTERS_PER_ISLAND` | `3` | Repo-owned bridge policy value written to `server-files/windrose_plugin_bridge/config.json` and the plugin heartbeat. This is contract/config only until a native teleporter-placement hook is proven. |
| `WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER` | `1` | Requested stack-size policy value (`1`, `2`, or `3`) written to the bridge config/heartbeat for operator/test readback. This does not write Windrose+'s legacy `stack_size` key while upstream inventory mutation remains unsafe/no-op. |
| `WINDROSE_STATE_PLUGIN_BRIDGE_RELATIVE_PATH` | `windrose_plugin_bridge` | State Web relative path, under `/server-files`, used to read plugin heartbeat/config files. |

### Seq logging

The optional Seq harness uses the standard Microsoft logging pipeline. It stays disabled until both values are present.

| Variable | Default | Description |
|----------|---------|-------------|
| `SEQ__SERVERURL` | (empty) | Seq base URL, for example `https://seq.polyhydragames.com`. |
| `SEQ__APIKEY` | (empty) | Seq ingest API key for this app. |
| `SEQ__MINIMUMLEVEL` | `Information` | Minimum level forwarded to Seq. |

## State Web Dashboard

This repo includes a C# Blazor/MudBlazor sidecar app for internal server observability.

Default URL:

```text
http://localhost:8781
```

Default API endpoints:

```text
GET /health
GET /map
GET /api/state
GET /api/players
GET /api/events
GET /api/events/stream
GET /api/runtime/control-surface
GET /api/runtime/action-capabilities
GET /api/plugin/manifest
GET /api/plugin/status
POST /api/plugin/actions/dry-run
GET /snapshot
GET /eventsrecent
GET /events/recent
GET /api/history
GET /api/history/export
GET /api/history/timeseries
GET /api/saves/latest
GET /api/saves/latest/checkpoint
GET /api/saves/latest/observed-families
GET /api/saves/latest/record-graph
GET /api/server/description
GET /api/world/description
GET /api/world/entities
GET /api/world/players
GET /api/world/ships
GET /api/world/actors
GET /api/world/summary
GET /api/overlay/summary
```

The sidecar reads:

```text
./server-files/R5/Saved/Logs/R5.log
./server-files/R5/Saved/SaveProfiles/Default
./server-files/R5/ServerDescription.json
```

The `server-files` mount is read-only inside the sidecar. The first build is intended for trusted networks only; add reverse-proxy auth, VPN access, or another protection layer before exposing it publicly.

### Plugin / sidecar bridge

The repo now includes a Windrose+ Lua plugin at `plugins/windrose-sidecar-bridge/`. When both `WINDROSE_PLUS_ENABLED=true` and `WINDROSE_SIDECAR_PLUGIN_ENABLED=true`, `scripts/install_windrose_plus.sh` copies it into `server-files/windrose_plus_mods/windrose-sidecar-bridge/`, creates `server-files/windrose_plugin_bridge/config.json`, and keeps the install idempotent on every restart. The bridge config includes `limits.maxTeleportersPerIsland`, sourced from `WINDROSE_MAX_TELEPORTERS_PER_ISLAND`.

The bridge proves the plugin-to-sidecar lifecycle without live mutation:

1. Windrose+ loads the Lua plugin from the friendly mods folder.
2. The plugin writes `server-files/windrose_plugin_bridge/status.json` with its mode and sidecar URL.
3. The plugin writes typed V3 event envelopes to `server-files/windrose_plugin_bridge/events/` so the sidecar can read heartbeat, readback, and error publications without mutating the game.
4. The heartbeat echoes contract-only policy values such as `limits.maxTeleportersPerIsland`, `limits.requestedStackSizeMultiplier`, and `limits.stackSizeEnforcement` so ChannelCheevos/tests can read the configured policy values.
5. State Web reads that heartbeat through its read-only `/server-files` mount.
6. Operators can inspect:
   - `GET /api/plugin/manifest` for the sidecar/plugin contract.
   - `GET /api/plugin/status` for the latest plugin heartbeat.
   - `GET /api/plugin/events/recent` for typed bridge event envelopes.
   - `POST /api/plugin/actions/dry-run` for request validation and dry-run dodo-swarm logging.

The exact dodo-swarm entrypoint is `HandleDodoSwarm`; it can hand off to `ExecuteDodoSwarmNative` only when the UE4SS game-thread dispatch gate is available. That native path is still proof-in-progress, so keep live mutation default-off until the separate gated-live card is ready.

Example dry-run request:

```shell
curl -sS http://localhost:8781/api/plugin/actions/dry-run \
  -H 'content-type: application/json' \
  -d '{"actionId":"windrose.spawn.dodo_swarm","targetPlayer":"Test Player","count":8,"radiusMeters":12,"offsetMeters":2}'
```

Nested summon objects are also accepted for richer caller contracts. Nested `summon.count`, `summon.radiusMeters`, and `summon.offsetMeters` override the legacy top-level values. Use `summon.selection=random` or `summon.creature=random` to pick from an allow-listed pool; the current safe pool is Dodo/Wolf only.

```shell
curl -sS http://localhost:8781/api/plugin/actions/dry-run \
  -H 'content-type: application/json' \
  -d '{"actionId":"windrose.spawn.dodo_swarm","targetPlayer":"Test Player","summon":{"selection":"random","creaturePool":["Dodo","Wolf"],"count":3,"radiusMeters":12,"offsetMeters":5}}'
```

A successful response returns `accepted: true`, `dryRun: true`, `executed: false`, and a log line containing `approvalRequired=true`. That is intentional: live dodo spawning remains blocked until a native Windrose spawn hook is proven, wired to ChannelCheevos approval/audit data, and reviewed.

Teleporter policy: set `WINDROSE_MAX_TELEPORTERS_PER_ISLAND=3` (or another non-negative integer) on the Windrose server container to change the configured max teleporter count per island. Today that value is exposed through `server-files/windrose_plugin_bridge/config.json`, `GET /api/plugin/manifest`, and `GET /api/plugin/status`; it does not yet enforce live placement because that still needs a proven native teleporter hook.

Stack-size policy: set `WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER=2` or `3` to expose the desired doubled/tripled stack-size policy through the same bridge config/status/read API. This remains readback only: the installer/plugin deliberately do not write legacy `stack_size` / `inventory_size` settings until a native/upstream-safe inventory hook exists.

Troubleshooting:

- `GET /api/plugin/status` returns `not-installed-or-not-started`: confirm `WINDROSE_PLUS_ENABLED=true`, `WINDROSE_SIDECAR_PLUGIN_ENABLED=true`, then restart the `windrose` service and check the server log for `[windrose-sidecar-bridge] loaded`.
- The plugin is installed but the sidecar still reports disconnected: confirm both services mount the same `./server-files` directory and that `WINDROSE_PLUGIN_BRIDGE_PATH` matches `WINDROSE_STATE_PLUGIN_BRIDGE_RELATIVE_PATH` after container path translation.
- Dry-run requests return validation errors: check `actionId`, `targetPlayer`, `count` (`1..50`), `radiusMeters` (`1..100`), and `offsetMeters` (`0..100`).

The save reader now summarizes the latest backup ZIP instead of just the backup file metadata. It exposes safe JSON document previews, collection counts, world preset details, and `ServerDescription.json` fields while still avoiding any write access into the save tree.
The new `/api/saves/latest/record-graph` response is read-only and redacted by design: it highlights identity markers versus candidate portable markers for player/account/session/progression/inventory/spawn data, and it explicitly reports when those markers co-reside so operators can see why selective cloning is still blocked.

### Ports

When `WINDROSE_PLUS_ENABLED=true`, expose the dashboard port (already in the provided `docker-compose.yml`):

```yaml
ports:
  - '7777:7777/tcp'
  - '7777:7777/udp'
  - '8780:8780/tcp'
```

### Lua mods

Windrose+ supports custom Lua mods that hot-reload on file change. Drop a mod folder (with `mod.json` and `init.lua`) into `server-files/windrose_plus_mods/` on the host — it'll load on the next restart and hot-reload on subsequent file changes. See the [upstream scripting guide](https://github.com/humangenome/WindrosePlus/blob/main/docs/scripting-guide.md) for the API reference.

### Caveats

- The container needs outbound network access when installing or upgrading Windrose+.
- Changing `WINDROSE_PLUS_VERSION` triggers a reinstall on the next container start; user-added Lua mods and existing `windrose_plus.json` / `windrose_plus*.ini` edits are preserved.

## Server Configuration

On first start the server automatically generates two configuration files inside `server-files/`. The container handles this automatically — it starts the server once to generate the files, applies your settings, then starts normally.

### Connecting

Players connect either via invite code (default) or IP address & port. The values for both can be set in your `.env` and are also visible in `server-files/R5/ServerDescription.json`.
Invite codes use the ICE protocol to establish a P2P connection.
Using your server's IP address will establish a direct connection.

> [!IMPORTANT]
> You can use an invite code or a direct connection via IP address, but not both.

#### Invite code

The code is set by `INVITE_CODE` in the `.env` file or `InviteCode` in `ServerDescription.json`. Share it with players who join via **Play → Connect to Server** in-game.

#### IP Address

This is enabled by `USE_DIRECT_CONNECTION=true` in the `.env` file or `UseDirectConnection` in `ServerDescription.json`.

#### LAN connections

If any players are connecting from the same local network, the default `P2P_PROXY_ADDRESS=127.0.0.1` will not work. You must:

1. Set `P2P_PROXY_ADDRESS` to the server machine's LAN IP address (e.g. `192.168.1.100`)
2. Add `network_mode: host` to your `docker-compose.yml` service so the container shares the host's network stack

```yaml
services:
  windrose:
    image: indifferentbroccoli/windrose-server-docker
    restart: unless-stopped
    container_name: windrose
    stop_grace_period: 30s
    network_mode: host
    env_file:
      - .env
    volumes:
      - ./server-files:/home/steam/server-files
```

### ServerDescription.json

Located at `server-files/R5/ServerDescription.json`. This file can only be edited while the server is stopped.

| Field | Description |
|-------|-------------|
| `InviteCode` | Invite code for players to find your server. Min 6 chars, `0-9 a-z A-Z`, case sensitive |
| `UseDirectConnection` | `true` if using direct connection via IP, or `false` (default) if using invite code |
| `DirectConnectionServerPort` | Port when direct connection is enabled. Default is `7777` |
| `DirectConnectionServerAddress` | Technical field — should not be changed |
| `DirectConnectionProxyAddress` | Address for the direct connection proxy. Default is `0.0.0.0` |
| `UserSelectedRegion` | Region for the connection service. Default is `EU`. Options: `SEA`, `CIS`, `EU` |
| `IsPasswordProtected` | `true` or `false` |
| `Password` | Server password |
| `ServerName` | Display name of the server |
| `WorldIslandId` | ID of the world to load — must match the folder name of a `WorldDescription.json` |
| `MaxPlayerCount` | Maximum simultaneous players |
| `P2pProxyAddress` | IP for listening sockets. Use `127.0.0.1` (default) — the proxy is an internal socket |

```json
{
    "Version": 1,
    "ServerDescription_Persistent": {
        "PersistentServerId": "...",
        "InviteCode": "myfriends",
        "IsPasswordProtected": false,
        "Password": "",
        "ServerName": "My Windrose Server",
        "WorldIslandId": "...",
        "MaxPlayerCount": 10,
        "P2pProxyAddress": "127.0.0.1",
        "DirectConnectionProxyAddress": "0.0.0.0",
        "UseDirectConnection": false,
        "DirectConnectionServerPort": 7777,
        "UserSelectedRegion": "EU",
        "DirectConnectionServerAddress": ""
    }
}
```

### WorldDescription.json

Located at `server-files/R5/Saved/SaveProfiles/Default/RocksDB_v2/<version>/Worlds/<world-id>/WorldDescription.json` (or legacy `RocksDB/<version>/...` on older installs). One file per world. This file can only be edited while the server is stopped.

| Field | Description |
|-------|-------------|
| `WorldPresetType` | Difficulty preset: `"Easy"`, `"Medium"`, `"Hard"`, or `"Custom"`. If any `WorldSettings` values are present the server forces this to `"Custom"` |
| `WorldName` | Name of the world |
| `WorldSettings` | Custom parameters — leave all sections empty to use a preset |

#### WorldSettings parameters

> Only takes effect when `WorldPresetType` is `"Custom"`. Leave `WorldSettings` empty to use a preset.

**Bool parameters**

| Parameter key | Default | Description |
|---------------|---------|-------------|
| `WDS.Parameter.Coop.SharedQuests` | `true` | When a player completes a co-op quest it auto-completes for all players who have it active |
| `WDS.Parameter.EasyExplore` | `false` | Hides map markers for points of interest, making exploration harder. Called "Immersive Exploration" in-game |

**Float parameters**

| Parameter key | Default | Range | Description |
|---------------|---------|-------|-------------|
| `WDS.Parameter.MobHealthMultiplier` | `1.0` | 0.2 – 5.0 | Enemy health multiplier |
| `WDS.Parameter.MobDamageMultiplier` | `1.0` | 0.2 – 5.0 | Enemy damage multiplier |
| `WDS.Parameter.ShipsHealthMultiplier` | `1.0` | 0.4 – 5.0 | Enemy ship health multiplier |
| `WDS.Parameter.ShipsDamageMultiplier` | `1.0` | 0.2 – 2.5 | Enemy ship damage multiplier |
| `WDS.Parameter.BoardingDifficultyMultiplier` | `1.0` | 0.2 – 5.0 | How many enemy sailors must be defeated to win a boarding action |
| `WDS.Parameter.Coop.StatsCorrectionModifier` | `1.0` | 0.0 – 2.0 | Adjusts enemy health and posture loss based on player count |
| `WDS.Parameter.Coop.ShipStatsCorrectionModifier` | `0.0` | 0.0 – 2.0 | Adjusts enemy ship health based on player count |

**Tag parameters**

| Parameter key | Default | Options | Description |
|---------------|---------|---------|-------------|
| `WDS.Parameter.CombatDifficulty` | `WDS.Parameter.CombatDifficulty.Normal` | `Easy` / `Normal` / `Hard` | Boss encounter difficulty and general enemy aggression |

**Example `WorldDescription.json`:**

```json
{
    "Version": 1,
    "WorldDescription": {
        "IslandId": "...",
        "WorldName": "My World",
        "WorldPresetType": "Custom",
        "WorldSettings": {
            "BoolParameters": {
                "{\"TagName\": \"WDS.Parameter.Coop.SharedQuests\"}": true,
                "{\"TagName\": \"WDS.Parameter.EasyExplore\"}": false
            },
            "FloatParameters": {
                "{\"TagName\": \"WDS.Parameter.MobHealthMultiplier\"}": 1,
                "{\"TagName\": \"WDS.Parameter.MobDamageMultiplier\"}": 1,
                "{\"TagName\": \"WDS.Parameter.ShipsHealthMultiplier\"}": 1,
                "{\"TagName\": \"WDS.Parameter.ShipsDamageMultiplier\"}": 1,
                "{\"TagName\": \"WDS.Parameter.BoardingDifficultyMultiplier\"}": 1,
                "{\"TagName\": \"WDS.Parameter.Coop.StatsCorrectionModifier\"}": 1,
                "{\"TagName\": \"WDS.Parameter.Coop.ShipStatsCorrectionModifier\"}": 0
            },
            "TagParameters": {
                "{\"TagName\": \"WDS.Parameter.CombatDifficulty\"}": {
                    "TagName": "WDS.Parameter.CombatDifficulty.Normal"
                }
            }
        }
    }
}
```

## Volumes

| Path | Description |
|------|-------------|
| `/home/steam/server-files` | Server installation files, world saves, and configuration |

## Proxmox

If you are hosting this server inside a Proxmox VM or LXC container, set the CPU type to **host**.

Proxmox's default CPU types (e.g. `kvm64`) omit instruction sets that Wine and the server binary may depend on. This can cause the server to fail to start, crash at runtime, or fail silently with no useful output.

## About

This is a Dockerized Windrose dedicated server maintained by [indifferent broccoli](https://indifferentbroccoli.com). We offer [managed Windrose server hosting](https://indifferentbroccoli.com/windrose-server-hosting) if you'd rather not self-host.
