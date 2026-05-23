# Server State Observability Checklist

## Discovery

- [x] Confirmed Portainer/container logs mirror the game log tail
- [x] Confirmed current log path: `R5/Saved/Logs/R5.log`
- [x] Confirmed save path: `R5/Saved/SaveProfiles/Default`
- [x] Confirmed periodic RocksDB backup ZIPs exist
- [x] Confirmed `WorldDescription.json` is available in live save data and backups
- [x] Found lifecycle log markers for server readiness
- [x] Found player connect/login/join/disconnect markers
- [x] Found RocksDB string evidence for `R5BLPlayerInWorld`, `R5BLPlayer`, `R5BLShip`, actors, locations, rotations, inventory, and quests
- [ ] Confirm whether RocksDB values can be decoded without game code
- [ ] Confirm whether Windrose+ already exposes enough dashboard/map data for this goal
- [x] Read `ServerDescription.json` from the mounted server files
- [x] Read the latest backup ZIP and expose safe JSON document previews
- [x] Wire the Microsoft logging pipeline to optional Seq ingestion
- [x] Confirm the checkpoint container is a RocksDB block-based SST
- [x] Confirm the tiny `shared_checksum/*.blob` file is internal RocksDB metadata, not an alternate payload source
- [x] Add a read-only observed-families endpoint for safe island/actor/player-reference hints
- [x] Add safe `/api/world/entities`, `/api/world/players`, `/api/world/ships`, and `/api/world/actors` summary slices
- [x] Add an overlay-friendly `/api/world/summary` endpoint
- [x] Add safe overlay-friendly JSON for the current world/observed-family state
- [x] Add lightweight broader-history JSON for operators and overlay consumers
- [x] Add a lightweight time-series export for operators and overlay consumers

## Documentation

- [x] Create feature folder
- [x] Capture current runtime surfaces
- [x] Capture known log markers
- [x] Capture proposed state model
- [x] Create companion-state webserver roadmap
- [x] Add implementation docs after first prototype
- [x] Document deployment once sidecar exists

## Log Parser

- [x] Tail `R5.log` from the mounted `server-files` path
- [x] Parse server initialized and ready markers
- [x] Parse registration settings block
- [x] Parse player add/reserve events
- [x] Parse login and join events
- [x] Parse expected disconnects
- [x] Parse unexpected P2P disconnects
- [x] Keep in-memory active player map
- [x] Persist a compact last-known state JSON file

## Webserver

- [x] Add sidecar service to compose
- [x] Expose `/health`
- [x] Expose `/api/state`
- [x] Expose `/api/players`
- [x] Expose `/api/events`
- [x] Expose `/api/events/stream`
- [x] Add minimal browser dashboard
- [x] Add read-only `ServerDescription.json` endpoint
- [x] Add read-only world/save metadata endpoints
- [x] Add save freshness and parser health panels

## Save Data

- [x] Locate newest `RocksDB_v2_Backups/.../*_Latest.zip`
- [x] Read `AdditionalRecordFiles/WorldDescription.json`
- [x] Extract safe checkpoint copy for analysis
- [x] Build a read-only backup inspection summary
- [ ] Identify keys/types for player, player-in-world, ship, and actor documents
- [ ] Determine whether per-value payloads are plain, protobuf, binary, or compressed
- [x] Add a read-only snapshot endpoint for checkpoint summaries

## Testing

- [x] Add parser fixture from real redacted log snippets
- [x] Test player connect sequence
- [x] Test player disconnect sequence
- [x] Test server restart/log rotation behavior
- [x] Test missing log file behavior
- [x] Test missing backup ZIP behavior
- [x] Test read-only mounted `server-files`

## Release / Deployment

- [x] Add compose service with read-only bind mount
- [x] Document required port
- [x] Document security expectations for LAN-only access
- [x] Add restart policy
- [x] Add log level knobs
- [x] Publish the reusable core contracts package to nuget.org
- [ ] Validate on the target host

## Follow-up

- [ ] Consider mimicking the companion app WebSocket protocol if payload shape is discovered
- [ ] Consider ingesting state into a time-series store for stream overlays
- [x] Export simple JSON for Channel Cheevos or OBS browser sources
