# Companion State Webserver Phases

## Phase 1 - Log-Derived Status API

Goal: expose useful server/player lifecycle state without decoding save data.

- [x] Create C# Blazor/MudBlazor sidecar app skeleton under `src/Windrose.StateWeb`
- [x] Mount `server-files` read-only
- [x] Tail `R5/Saved/Logs/R5.log`
- [x] Handle missing file on startup
- [x] Handle log rotation
- [x] Parse server initialized marker
- [x] Parse server ready marker
- [x] Parse registration settings block
- [x] Parse `AddPlayer` events
- [x] Parse `Login request` events
- [x] Parse `Join request` events
- [x] Parse disconnect events
- [x] Expose `/health`
- [x] Expose `/state`
- [x] Expose `/players`
- [x] Expose `/events`
- [x] Add MudBlazor operator dashboard
- [x] Add parser tests

## Phase 2 - Save Snapshot Metadata

Goal: expose world and backup state from stable files.

- [x] Discover active island id from logs
- [ ] Discover active island id from `ServerDescription.json`
- [x] Locate `RocksDB_v2_Backups/Worlds/<island-id>/*_Latest.zip`
- [x] Read backup timestamp and size
- [x] Read `AdditionalRecordFiles/WorldDescription.json`
- [x] Expose `/saves/latest`
- [x] Expose `/world/description`
- [x] Add backup freshness warnings
- [x] Add UI panel for save freshness and world preset

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

- [x] Add compose service
- [x] Add configurable web port
- [x] Add read-only mount
- [x] Add restart policy
- [x] Add basic auth or LAN-only guidance
- [x] Add logs for parser health
- [ ] Add deployment docs for `192.168.0.252`
- [ ] Validate under Portainer
