# Server State Observability Feature Roadmap

## Near Term

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
- [ ] Prototype RocksDB checkpoint decoding

## Later

- [ ] Expose player/ship/object positions if decodable
- [ ] Add companion-like map state endpoint
- [ ] Add richer live channel payloads if the `channel-cheevos` contract evolves
- [ ] Keep OBS/browser-source work in `cc-sidecar` / `channel-cheevos` unless a shared overlay contract lands here
- [ ] Explore compatibility with Channel Cheevos overlays and Twitch integrations through `channel-cheevos`
- [x] Add lightweight broader-history views for operators before considering heavier exports
- [x] Add a lightweight time-series export for operators and overlay consumers

## Not Planned Yet

- [ ] Writing to server save data
- [ ] Editing `ServerDescription.json` from the observer
- [ ] Public internet exposure without authentication
- [ ] Reverse engineering or redistributing the closed companion app
