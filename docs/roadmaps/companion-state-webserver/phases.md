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
- [x] Discover active island id from `ServerDescription.json`
- [x] Locate `RocksDB_v2_Backups/Worlds/<island-id>/*_Latest.zip`
- [x] Read backup timestamp and size
- [x] Read `AdditionalRecordFiles/WorldDescription.json`
- [x] Expose `/saves/latest`
- [x] Expose `/world/description`
- [x] Add backup freshness warnings
- [x] Add UI panel for save freshness and world preset

## Phase 3 - RocksDB Checkpoint Decoder

Goal: determine whether companion-like state can be extracted safely.

- [x] Extract latest checkpoint ZIP to a temp directory
- [x] Open extracted checkpoint read-only
- [x] Enumerate keys and value sizes
- [x] Identify document type prefixes
- [ ] Identify `R5BLPlayer`
- [ ] Identify `R5BLPlayerInWorld`
- [ ] Identify `R5BLShip`
- [ ] Identify high-value actor documents
- [x] Determine container format as RocksDB block-based SST
- [ ] Decode one player/world document
- [ ] Decode one ship document
- [x] Document what is readable and what is not

Notes:

- The current prototype extracts the latest checkpoint ZIP read-only and scans the extracted files for known `R5BL*` markers.
- The checkpoint ZIP footer and RocksDB table-property strings prove the checkpoint container is a block-based SST. The remaining unknown is the per-document value payload format inside the data blocks.
- The current live SSTs also show single-entry blocks whose keys point at island/building/actor records and whose values are readable enough to summarize names like `CommonIsland`, `LandscapeLocation`, `ShipId`, and `Actor_InteractedPoiIds`. That is useful for safe summaries, but still not a decoded player or ship document.
- The tiny `shared_checksum/000015_590266782_175.blob` file is not a hidden alternate payload source: the checkpoint options show `enable_blob_files=false`, so it only reflects RocksDB internal metadata and does not unblock the document decoder.
- The current live save tree does not contain any `R5BLShip` string at all, so the remaining ship work is blocked on a data sample that actually includes a ship document, not just `ShipId` references inside other records.
- The new `/api/world/*` routes are safe observed-family summary slices only; they do not claim decoded map coordinates or ship/player documents.

## Phase 4 - Companion-Like State

Goal: expose map/player/ship state if decoding proves viable.

- [x] Add `/world/entities` as a safe observed-family summary slice
- [x] Add `/world/players` as a safe observed-family summary slice
- [x] Add `/world/ships` as a safe observed-family summary slice
- [x] Add `/world/actors` as a safe observed-family summary slice
- [ ] Add WebSocket or SSE updates
- [ ] Add browser map proof of concept
- [ ] Add redaction controls
- [x] Add overlay-friendly JSON endpoint
- [x] Add lightweight broader-history JSON export
- [x] Add lightweight time-series JSON export

## Phase 5 - Deployment Hardening

Goal: make this usable on the server host.

- [x] Add compose service
- [x] Add configurable web port
- [x] Add read-only mount
- [x] Add restart policy
- [x] Add basic auth or LAN-only guidance
- [x] Add logs for parser health
- [x] Add deployment docs for the target host
- [x] Validate under Portainer

## Phase 6 - Live Push

Goal: keep an outbound operator channel in sync with the current read-only state.

- [x] Add SignalR client plumbing
- [x] Gate push behind a shared webkey
- [x] Publish player session events
- [x] Publish save and parser state changes
- [x] Confirm the `channel-cheevos` hub contract and method names
