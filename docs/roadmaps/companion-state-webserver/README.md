# Companion State Webserver Roadmap

## Purpose

Build a local webserver that exposes Windrose dedicated-server state in a way that is useful for operators, stream overlays, and future companion-style tooling.

The target is similar in spirit to the public Windrose companion app, but based on dedicated-server-accessible data:

- server logs
- container logs
- `ServerDescription.json`
- RocksDB checkpoint backups
- optional Windrose+ dashboard data if available
- optional SignalR live push to `channel-cheevos`

## Current Assessment

- [x] The official companion app appears to be a Windows local desktop app.
- [x] The dedicated server logs expose useful lifecycle and player connection state.
- [x] The dedicated server save data contains richer world documents in RocksDB.
- [x] The backup ZIP path is safer to inspect than the live RocksDB path.
- [x] The Windrose sidecar implementation now covers the operator dashboard, read-only API, safe save summaries, lightweight history export, time-series export, overlay JSON, and optional SignalR live push.
- [x] The reusable `Windrose.StateWeb.Core` project now carries the shared payloads, response models, and source abstractions.
- [x] The reusable core project is packable and has a GitHub Actions release workflow for NuGet publication.
- [x] The read-only checkpoint snapshot endpoint now exposes safe container and entry summaries.
- [x] Safe `/api/world/entities`, `/api/world/players`, `/api/world/ships`, and `/api/world/actors` slices now exist, but they remain summary-only and do not claim decoded coordinates or ship/player documents.
- [x] An overlay-friendly `/api/world/summary` route now exists for compact safe JSON without decoded ship/player claims.
- [x] The `channel-cheevos` receiver now exposes the `windrose-state` hub plus compatibility alias `hubs/windrose-state`.
- [ ] Companion-style player/map coordinates are not yet proven.
- [ ] RocksDB payload format is not yet decoded.
- [ ] Companion-style player/ship/actor document decoding is still summary-only until proven safe.

## Desired Outcome

- [ ] A browser-accessible state dashboard
- [ ] A JSON API for external tools
- [ ] A live event stream for connects/disconnects
- [ ] A save snapshot reader for richer world state
- [ ] A read-only `ServerDescription.json` reader
- [ ] Optional SignalR publishing to the operator stack
- [ ] A path toward map/player/ship state if the data can be decoded

## Documentation Links

- Feature: `docs/features/server-state-observability/README.md`
- Feature checklist: `docs/features/server-state-observability/checklist.md`
- Architecture: `docs/features/server-state-observability/architecture.md`
- Implementation plan: `docs/roadmaps/companion-state-webserver/implementation-plan.md`
- Deployment notes: `docs/roadmaps/companion-state-webserver/deployment.md`
- Remaining work: `docs/roadmaps/companion-state-webserver/remaining-work.md`
- Core NuGet publishing: `docs/roadmaps/windrose-core-nuget-publishing/README.md`
