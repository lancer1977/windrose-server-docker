# Server State Observability Feature Roadmap

## Near Term

- [ ] Add a log parser sidecar
- [ ] Add JSON endpoints for server and player state
- [ ] Add a minimal browser status view
- [ ] Add log rotation handling
- [ ] Add parser tests from redacted real snippets

## Mid Term

- [ ] Read latest backup ZIP metadata
- [ ] Extract `WorldDescription.json`
- [ ] Detect latest world id automatically
- [ ] Add backup freshness indicators
- [ ] Prototype RocksDB checkpoint decoding

## Later

- [ ] Expose player/ship/object positions if decodable
- [ ] Add WebSocket or SSE event stream
- [ ] Add companion-like map state endpoint
- [ ] Explore compatibility with OBS browser sources
- [ ] Explore compatibility with Channel Cheevos overlays

## Not Planned Yet

- [ ] Writing to server save data
- [ ] Editing `ServerDescription.json` from the observer
- [ ] Public internet exposure without authentication
- [ ] Reverse engineering or redistributing the closed companion app
