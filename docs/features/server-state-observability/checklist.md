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

## Documentation

- [x] Create feature folder
- [x] Capture current runtime surfaces
- [x] Capture known log markers
- [x] Capture proposed state model
- [x] Create companion-state webserver roadmap
- [ ] Add implementation docs after first prototype
- [ ] Document deployment once sidecar exists

## Log Parser

- [ ] Tail `R5.log` from the mounted `server-files` path
- [ ] Parse server initialized and ready markers
- [ ] Parse registration settings block
- [ ] Parse player add/reserve events
- [ ] Parse login and join events
- [ ] Parse expected disconnects
- [ ] Parse unexpected P2P disconnects
- [ ] Keep in-memory active player map
- [ ] Persist a compact last-known state JSON file

## Webserver

- [ ] Add sidecar service to compose
- [ ] Expose `/health`
- [ ] Expose `/state`
- [ ] Expose `/players`
- [ ] Expose `/events`
- [ ] Expose Server-Sent Events or WebSocket stream
- [ ] Add minimal browser dashboard

## Save Data

- [ ] Locate newest `RocksDB_v2_Backups/.../*_Latest.zip`
- [ ] Read `AdditionalRecordFiles/WorldDescription.json`
- [ ] Extract safe checkpoint copy for analysis
- [ ] Build a read-only RocksDB inspection proof of concept
- [ ] Identify keys/types for player, player-in-world, ship, and actor documents
- [ ] Determine whether coordinates are plain, protobuf, binary, or compressed payloads
- [ ] Add a snapshot endpoint if decoding is viable

## Testing

- [ ] Add parser fixture from real redacted log snippets
- [ ] Test player connect sequence
- [ ] Test player disconnect sequence
- [ ] Test server restart/log rotation behavior
- [ ] Test missing log file behavior
- [ ] Test missing backup ZIP behavior
- [ ] Test read-only mounted `server-files`

## Release / Deployment

- [ ] Add compose service with read-only bind mount
- [ ] Document required port
- [ ] Document security expectations for LAN-only access
- [ ] Add restart policy
- [ ] Add log level knobs
- [ ] Validate on `192.168.0.252`

## Follow-up

- [ ] Consider mimicking the companion app WebSocket protocol if payload shape is discovered
- [ ] Consider ingesting state into a time-series store for stream overlays
- [ ] Consider exporting simple JSON for Channel Cheevos or OBS browser sources
