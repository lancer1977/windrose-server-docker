# Server State Observability Architecture

## Goal

Expose a local web surface for Windrose dedicated-server state without modifying the game binary.

The architecture should start with read-only observation and avoid touching live save data in ways that could corrupt the server.

## Inputs

### Log Stream

```text
server-files/R5/Saved/Logs/R5.log
```

Used for low-latency event state:

- server boot
- server ready
- registration settings
- player session creation
- login
- join
- disconnect
- backup cadence
- resource usage

### Save Profile

```text
server-files/R5/Saved/SaveProfiles/Default/
```

Used for slower, richer world state:

- world description
- RocksDB live database
- checkpoint backups

### Server Description

```text
server-files/R5/ServerDescription.json
```

Used for the authoritative server settings that the log parser can only observe indirectly.

### Backup Checkpoints

```text
server-files/R5/Saved/SaveProfiles/Default/RocksDB_v2_Backups/Worlds/<island-id>/*_Latest.zip
```

Preferred initial source for save inspection because it avoids reading the live RocksDB files while the server owns them.

## Proposed Components

### State Collector

Responsibilities:

- [x] tail the active log
- [x] tolerate log rotation
- [x] parse known marker lines into structured events
- [x] maintain current server/player state
- [x] write a compact state snapshot

### Save Snapshot Reader

Responsibilities:

- [x] find the newest checkpoint ZIP
- [x] read `WorldDescription.json`
- [x] extract checkpoint files to a temp path for analysis
- [x] inspect RocksDB keys and values
- emit decoded world/player/ship/object data when safe

### Web API

Initial endpoints:

```text
GET /health
GET /api/state
GET /api/players
GET /api/events
GET /api/saves/latest
GET /api/server/description
GET /api/world/description
```

Streaming endpoints:

```text
GET /api/events/stream
GET /ws
```

### Browser UI

Initial views:

- [x] server status
- [x] active players
- [x] recent events
- [x] latest save/backup status
- [x] decoded world summary

### Live Push

The sidecar can optionally push state updates to `channel-cheevos` over SignalR using a shared webkey. This is off by default and stays read-only on the Windrose side.

## Data Flow

```text
R5.log
  -> log tailer
  -> event parser
  -> state store
  -> snapshot writer
  -> HTTP JSON / browser UI

ServerDescription.json
  -> save inspector
  -> state store
  -> HTTP JSON / browser UI

RocksDB_v2_Backups/*_Latest.zip
  -> checkpoint reader
  -> safe save summary reader
  -> state store
  -> HTTP JSON / browser UI
```

## Safety Rules

- Mount `server-files` read-only in the sidecar
- Prefer checkpoint ZIPs over live RocksDB reads
- Never modify `ServerDescription.json` from the observer
- Never write inside `R5/Saved` from the observer
- Treat account ids and client names as sensitive in public views
- Keep the webserver LAN-only unless authentication is added

## Open Technical Questions

- Can the RocksDB values be decoded directly with standard RocksDB tooling?
- [x] Is the checkpoint container a RocksDB block-based SST?
- Are the per-value payloads JSON, protobuf, Unreal binary serialization, or a custom format?
- Does Windrose+ already expose a map/state endpoint that should be reused?
- Can the companion app WebSocket schema be observed from a Windows client and mirrored?
- Which state should be real-time versus snapshot-based?

## Container Format Note

The latest live backup ZIP shows standard RocksDB SST evidence:

- `Checkpoint/private/1/MANIFEST-000021`
- `Checkpoint/private/1/OPTIONS-000059`
- `rocksdb.block.based.table.index.type`
- `prefix.filtering0`
- `whole.key.filtering1`
- the fixed RocksDB SST magic at the end of the checkpoint files

That is enough to identify the container as a block-based RocksDB SST checkpoint, but not enough to decode the Windrose document payloads safely.
