# Companion State Webserver Roadmap

## Purpose

Build a local webserver that exposes Windrose dedicated-server state in a way that is useful for operators, stream overlays, and future companion-style tooling.

The target is similar in spirit to the public Windrose companion app, but based on dedicated-server-accessible data:

- server logs
- container logs
- `ServerDescription.json`
- RocksDB checkpoint backups
- optional Windrose+ dashboard data if available

## Current Assessment

- [x] The official companion app appears to be a Windows local desktop app.
- [x] The dedicated server logs expose useful lifecycle and player connection state.
- [x] The dedicated server save data contains richer world documents in RocksDB.
- [x] The backup ZIP path is safer to inspect than the live RocksDB path.
- [ ] Companion-style player/map coordinates are not yet proven.
- [ ] RocksDB payload format is not yet decoded.

## Desired Outcome

- [ ] A browser-accessible state dashboard
- [ ] A JSON API for external tools
- [ ] A live event stream for connects/disconnects
- [ ] A save snapshot reader for richer world state
- [ ] A path toward map/player/ship state if the data can be decoded

## Documentation Links

- Feature: `docs/features/server-state-observability/README.md`
- Feature checklist: `docs/features/server-state-observability/checklist.md`
- Architecture: `docs/features/server-state-observability/architecture.md`
- Implementation plan: `docs/roadmaps/companion-state-webserver/implementation-plan.md`
