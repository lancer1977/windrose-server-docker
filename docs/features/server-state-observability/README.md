# Server State Observability

## Summary

This feature tracks how the Windrose dedicated server can expose useful runtime state from container logs, Unreal/R5 logs, and persisted save data.

The current server does not expose the same companion-app WebSocket surface that the Windows companion app provides. The practical path is to combine lightweight log parsing for server/player lifecycle with save-state inspection for richer world state.

For a live capability map that separates observer-only surfaces from mutation-capable surfaces, see [Windrose Runtime Control Surface Map](runtime-control-surface.md).

## Docs entry points

- [`docs/features/README.md`](../README.md) — the stable feature index for the repo.
- [`docs/roadmaps/README.md`](../../roadmaps/README.md) — the phased work index for future or incomplete capability slices.
- [`docs/roadmaps/windrose-runtime-control-surface/README.md`](../../roadmaps/windrose-runtime-control-surface/README.md) — the live-mutation backlog and operator-contract work that sits outside this observer feature.

The implementation is complete for the current internal rollout. Any remaining unchecked items in this area are future research or proof-of-concept work, not blockers for the shipped sidecar/dashboard/API surface.

The live-mutation backlog now lives in `docs/roadmaps/windrose-runtime-control-surface/README.md`.

## Status

- [x] Local Docker repo cloned into the active workspace
- [x] Remote server logs inspected on the target host under the Windrose server-files tree
- [x] Current log stream confirmed as `R5/Saved/Logs/R5.log`
- [x] Player lifecycle markers found in logs
- [x] World/save storage located under `R5/Saved/SaveProfiles/Default`
- [x] RocksDB checkpoint backups found under `RocksDB_v2_Backups`
- [x] Companion-style webserver implemented
- [x] Safe backup summary reader implemented
- [x] Browser UI implemented
- [x] Live state publication hook implemented
- [x] SignalR hub for local browser/live consumers implemented
- [x] Microsoft logging harness with optional Seq forwarding implemented
- [x] Read-only operator API implemented for health, state, players, events, save metadata, and server/world metadata
- [x] Read-only history export and overlay summary endpoints implemented
- [x] Read-only time-series export endpoint implemented
- [x] Compact snapshot file persisted for the last known state
- [x] Latest checkpoint ZIPs are extracted to a temp analysis directory for read-only inspection
- [x] Log rotation and missing-log handling verified
- [x] Overview and diagnostics panels received a small polish pass
- [x] `channel-cheevos` live-push receiver contract now exists on `windrose-state`
- [x] Valheim-compatible snapshot and recent-events aliases now exist for shared operator/overlay contract shape
- [x] Reusable core payload project exists for shared models, interfaces, and helper extensions
- [x] Reusable core payload project is packable and has a GitHub Actions release workflow for NuGet publication
- [x] Live push can be target-selected by environment (`dev`, `debug`, `prod`) without changing application code
- [x] Deep RocksDB checkpoint decoding remains summary-only until proven safe

## Runtime Surfaces

### Container Logs

The container startup script tails:

```text
/home/steam/server-files/R5/Saved/Logs/R5.log
```

That means Portainer and `docker logs windrose` show most new lines from the game log after startup.

### Game Log

Host path:

```text
server-files/R5/Saved/Logs/R5.log
```

The log is useful for:

- [x] server boot and readiness
- [x] island id and server registration
- [x] invite code / server settings echoed during registration
- [x] player session ids
- [x] account ids
- [x] login and join events
- [x] disconnect events
- [x] save/backup cadence
- [x] resource usage reports

The log is not yet proven useful for:

- live player coordinates
- live ship coordinates
- complete inventory state
- full map reveal state
- companion-app WebSocket payloads

### Save Data

Host path:

```text
server-files/R5/Saved/SaveProfiles/Default
```

Important subpaths:

```text
RocksDB_v2/0.10.0/Worlds/<island-id>/
RocksDB_v2_Backups/Worlds/<island-id>/
```

The current server writes RocksDB state and periodic checkpoint ZIP backups. The latest backup contains a RocksDB checkpoint plus `AdditionalRecordFiles/WorldDescription.json`.

The save API now exposes read-only checkpoint summary snapshots at `/api/saves/latest/checkpoint` and `/api/saves/latest/observed-families`, and the world API now exposes safe `/api/world/entities`, `/api/world/players`, `/api/world/ships`, `/api/world/actors`, and `/api/world/summary` slices. The browser/live surface exposes a SignalR hub at `/hubs/windrose-state` for connected consumers that want push updates rather than polling, and the overview, players, saves, events, diagnostics, and map pages now all use that hub for live updates. All of these are limited to safe container/entry metadata and observed family hints rather than claiming decoded player or ship documents.
The sidecar also has an optional sensitive-metadata redaction toggle (`WindroseState__RedactSensitiveMetadata=true`) for broader sharing without exposing invite codes or player identity fields.

## Current Useful Log Markers

- Server initialized:

```text
UR5CoopProxyServer::Init
Server initialized. CurrentIslandId <id>
```

- Host ready:

```text
UR5CoopProxyServer::SetIsReadyForHostOwnerConnect
Host server is ready for owner to connect
```

- Player/account reservation:

```text
Process AddPlayer. AccountId <account-id>. BLPlayerSessionId <session-id>. IsBL true
```

- UE connection:

```text
OnUeConnect
Ue P2P connection created. Connect to local server. UeLocalPort 7777
```

- Login:

```text
LogNet: Login request: ?BLPlayerSessionId=<session-id>?Name=<client-name>
```

- Join:

```text
LogNet: Join request: /Game/Maps/Lobby/R5ServerLobby?BLPlayerSessionId=<session-id>?Name=<client-name>
```

- Disconnect:

```text
UR5CoopProxyServer::OnAccountDisconnected
Account disconnected. Inform Cm. AccountId <account-id>. BLPlayerSessionId <session-id>
```

## Current Exposed State

- [x] `server`
  - [x] ready state
  - [x] current island id
  - [x] invite code
  - [x] server name
  - [x] max players
  - [x] direct connection settings
- [x] `players`
  - [x] account id
  - [x] player session id
  - [x] client name
  - [x] connection phase
  - [x] connected/disconnected timestamps
- [x] `saves`
  - [x] latest backup timestamp
  - [x] latest backup path
  - [x] world description summary
- [x] `world`
  - [x] world preset
  - [x] island id
  - [x] known entity counts
  - discovered player/ship position data if readable from RocksDB

## Next Step

Work from `docs/roadmaps/companion-state-webserver/remaining-work.md` for the remaining contract, deployment, and decoding items, and use `docs/roadmaps/README.md` as the hub for any future phased work. Keep any deeper checkpoint work behind the same read-only inspection path instead of expanding the write surface.
