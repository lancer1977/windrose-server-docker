# Server State Observability Feature Roadmap

## Near Term

This roadmap now records follow-up research and future expansion ideas. The core internal Windrose state-web rollout is implemented; unchecked items below are only for future proof-of-concept work if the data or product direction changes.

- [x] Add a log parser sidecar
- [x] Add JSON endpoints for server and player state
- [x] Add a minimal browser status view
- [x] Add log rotation handling
- [x] Add parser tests from redacted real snippets
- [x] Add read-only `ServerDescription.json` inspection
- [x] Add a safe backup summary reader
- [x] Add optional SignalR live push to `channel-cheevos`
- [x] Confirm the `channel-cheevos` hub contract and method names

## Mid Term

- [x] Read latest backup ZIP metadata
- [x] Extract `WorldDescription.json`
- [x] Detect latest world id automatically
- [x] Add backup freshness indicators
- Prototype RocksDB checkpoint decoding

## Later

- Expose player/ship/object positions if decodable
- [x] Add companion-like map state endpoint
- Add richer live channel payloads if the `channel-cheevos` contract evolves
- [x] Keep OBS/browser-source work in `cc-sidecar` / `channel-cheevos` unless a shared overlay contract lands here (see `docs/roadmaps/companion-state-webserver/browser-source-handoff.md`)
- [x] Explore compatibility with Channel Cheevos overlays and Twitch integrations through `channel-cheevos`
- [x] Add lightweight broader-history views for operators before considering heavier exports
- [x] Add a lightweight time-series export for operators and overlay consumers

## Not Planned Yet

- Writing to server save data
- Editing `ServerDescription.json` from the observer
- Public internet exposure without authentication
- Reverse engineering or redistributing the closed companion app
