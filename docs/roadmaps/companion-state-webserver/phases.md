# Companion State Webserver Phases

## Phase 1 - Log-Derived Status API

Goal: expose useful server/player lifecycle state without decoding save data.

- [ ] Create C# Blazor/MudBlazor sidecar app skeleton under `src/Windrose.StateWeb`
- [ ] Mount `server-files` read-only
- [ ] Tail `R5/Saved/Logs/R5.log`
- [ ] Handle missing file on startup
- [ ] Handle log rotation
- [ ] Parse server initialized marker
- [ ] Parse server ready marker
- [ ] Parse registration settings block
- [ ] Parse `AddPlayer` events
- [ ] Parse `Login request` events
- [ ] Parse `Join request` events
- [ ] Parse disconnect events
- [ ] Expose `/health`
- [ ] Expose `/state`
- [ ] Expose `/players`
- [ ] Expose `/events`
- [ ] Add MudBlazor operator dashboard
- [ ] Add parser tests

## Phase 2 - Save Snapshot Metadata

Goal: expose world and backup state from stable files.

- [ ] Discover active island id from logs
- [ ] Discover active island id from `ServerDescription.json`
- [ ] Locate `RocksDB_v2_Backups/Worlds/<island-id>/*_Latest.zip`
- [ ] Read backup timestamp and size
- [ ] Read `AdditionalRecordFiles/WorldDescription.json`
- [ ] Expose `/saves/latest`
- [ ] Expose `/world/description`
- [ ] Add backup freshness warnings
- [ ] Add UI panel for save freshness and world preset

## Phase 3 - RocksDB Checkpoint Decoder

Goal: determine whether companion-like state can be extracted safely.

- [ ] Extract latest checkpoint ZIP to a temp directory
- [ ] Open extracted checkpoint read-only
- [ ] Enumerate keys and value sizes
- [ ] Identify document type prefixes
- [ ] Identify `R5BLPlayer`
- [ ] Identify `R5BLPlayerInWorld`
- [ ] Identify `R5BLShip`
- [ ] Identify high-value actor documents
- [ ] Determine serialization format
- [ ] Decode one player/world document
- [ ] Decode one ship document
- [ ] Document what is readable and what is not

## Phase 4 - Companion-Like State

Goal: expose map/player/ship state if decoding proves viable.

- [ ] Add `/world/entities`
- [ ] Add `/world/players`
- [ ] Add `/world/ships`
- [ ] Add `/world/actors`
- [ ] Add WebSocket or SSE updates
- [ ] Add browser map proof of concept
- [ ] Add redaction controls
- [ ] Add overlay-friendly JSON endpoint

## Phase 5 - Deployment Hardening

Goal: make this usable on the server host.

- [ ] Add compose service
- [ ] Add configurable web port
- [ ] Add read-only mount
- [ ] Add restart policy
- [ ] Add basic auth or LAN-only guidance
- [ ] Add logs for parser health
- [ ] Add deployment docs for `192.168.0.252`
- [ ] Validate under Portainer
