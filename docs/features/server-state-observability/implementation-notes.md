# Server State Observability Implementation Notes

## V3 Implementation Notes

- The sidecar now reads `server-files/R5/ServerDescription.json` directly and uses it as the authoritative source for server settings when available.
- The latest backup ZIP is summarized in a read-only way: world preset fields, safe JSON previews, and collection counts are exposed without writing back into `server-files`.
- Live state can be pushed out to `channel-cheevos` over SignalR when the `WindroseState__EnableChannelCheevosPush` and `WindroseState__ChannelCheevos*` settings are provided. The live-push target is now environment-driven so the same stack can select `dev`, `debug`, or `prod` hubs and webkeys without changing the application code.
- The sidecar now uses the Microsoft logging pipeline with optional Seq forwarding when `Seq__ServerUrl` and `Seq__ApiKey` are configured; the provider is intentionally gated so it stays quiet until both values are present.
- The SignalR client uses configurable method names and automatic reconnect, and the receiver-side `channel-cheevos` contract is now confirmed on `windrose-state` with compatibility alias `hubs/windrose-state`.
- The sender-side hub URL builder appends the shared `webkey` as an encoded query string and preserves existing query parameters.
- The browser dashboard now listens to the shared state hub at `/hubs/windrose-state` for live updates and only uses the shared state store for its initial snapshot instead of polling on a timer.
- The browser dashboard now includes a `/map` proof page that connects to the live SignalR hub and reuses the same read-only summary surfaces for browser-source-style operator previews.
- The overview and players pages now also listen to the shared state hub for live updates instead of polling on a timer.
- The overview, players, events, saves, diagnostics, and map pages now listen to the shared state hub at `/hubs/windrose-state` for live updates instead of polling on a timer.
- The API surface now includes read-only health, state, player, event, save, server-description, and world-description endpoints plus an SSE event stream.
- The API surface now also includes read-only history export, time-series export, and overlay-summary endpoints so lighter operator history and browser-source consumers can reuse the same safe JSON surface.
- The API surface also includes a read-only observed-families summary endpoint for safe island, actor, and player-reference hints without claiming a ship decoder.
- The API surface also exposes safe `/api/world/entities`, `/api/world/players`, `/api/world/ships`, `/api/world/actors`, and `/api/world/summary` slices so the route contract is present even though the underlying ship/player document decode remains open.
- State changes now persist a compact last-known JSON snapshot to `WindroseStateOptions.SnapshotPath` after each update.
- The log tailer now rewinds on log shrink and same-size replacement as a practical rotation signal.
- Missing log files drive the parser into a degraded state with a readable error message instead of a crash.

## Findings From Remote Log Review

Remote host:

```text
internal test host
```

Observed server path:

```text
/path/to/windrose/server-files
```

SMB equivalent:

```text
smb://internal-test-host/gameservers/windrose/server-files
```

## Log Files

Current and rotated logs live at:

```text
R5/Saved/Logs/
```

The current file was:

```text
R5/Saved/Logs/R5.log
```

Rotated logs follow:

```text
R5-backup-YYYY.MM.DD-HH.MM.SS.log
```

## Container Log Behavior

The startup script tails the game log:

```bash
tail -F "$LOG_FILE" 2>/dev/null &
```

where:

```bash
LOG_FILE="$SERVER_FILES/R5/Saved/Logs/R5.log"
```

So Portainer and Docker logs are useful, but the actual source of truth remains the file under `server-files`.

## Useful Environment Knobs

Recommended diagnostic logging for discovery:

```env
DIAGNOSTIC_MODE=true
WINE_VERBOSE=false
SERVER_ARGS=-log -STDOUT -nullrhi -nosound -LogCmds="R5LogCoopProxy Verbose,R5LogNetCm Verbose,R5LogNetBL Verbose,R5LogDataKeeper Verbose,R5LogP2pGate Verbose,R5LogSocketSubsystem Verbose,LogNet Verbose"
```

Use diagnostic logging sparingly. It increases log volume and still may not expose companion-style map data.

## Known High-Value Markers

### Ready

```text
Host server is ready for owner to connect
```

### Server Settings

The registration block logs values such as:

```text
InviteCode
ServerName
WorldIslandId
MaxPlayerCount
P2pProxyAddress
UseDirectConnection
DirectConnectionServerPort
```

### Player Session

The logs expose account/session pairs:

```text
AccountId <account-id>. BLPlayerSessionId <session-id>
```

### Join

```text
LogNet: Join request: /Game/Maps/Lobby/R5ServerLobby?BLPlayerSessionId=<session-id>?Name=<client-name>
```

### Disconnect

```text
UR5CoopProxyServer::OnAccountDisconnected
```

Disconnect reasons observed include:

- `BL disconnected`
- `Go to lobby`
- `Connection reset by peer`
- P2P gate disconnects

## Save Data Findings

Live world path:

```text
R5/Saved/SaveProfiles/Default/RocksDB_v2/0.10.0/Worlds/<island-id>/
```

Backup path:

```text
R5/Saved/SaveProfiles/Default/RocksDB_v2_Backups/Worlds/<island-id>/
```

The latest backup ZIP contained:

- [x] RocksDB checkpoint files
- [x] `AdditionalRecordFiles/WorldDescription.json`

String scans of RocksDB `.sst` files found names including:

- `R5BLPlayerInWorld`
- `R5BLPlayer`
- `R5BLShip`
- `R5BLActor_BuildingBlock`
- `R5BLActor_MineralNode`
- `R5BLIslandChest`
- `Location`
- `Rotation`
- `Inventory`
- `Quest`

Current live-backup evidence is still summary-level:

- `R5BLPlayerInWorld` and `R5BLPlayer` show up in RocksDB metadata files (`MANIFEST-000021` and `OPTIONS-000059`)
- `ShipId`, `Actor_InteractedPoiIds`, `Actor_RemovedDialogueActorIds`, and `LandscapeLocation` show up in the data SSTs
- some SSTs behave like single-entry blocks whose values expose safe names such as `CommonIsland` and `LandscapeLocation`
- at least one live SST value also exposes structured field names such as `Blocks`, `DataKey`, `MarkupKey`, `IslandId`, and `ChangeRevision`
- the tiny `shared_checksum/000015_590266782_175.blob` file is internal RocksDB metadata, not a separate blob-backed domain payload store; the checkpoint options explicitly show `enable_blob_files=false`
- the current live save tree does not contain a `R5BLShip` string at all, so the current snapshot only proves `ShipId` references inside other document families rather than a standalone ship document
- no decoded player or ship document has been proven yet

This suggests the save data is the best route for companion-like state, but decoding still needs proof.

## Implementation Bias

Start with a parser that can ship quickly:

- [x] read-only mount of `server-files`
- [x] log-derived server/player state
- [x] JSON API
- [x] small browser UI

Then add save decoding once the RocksDB format is understood.
