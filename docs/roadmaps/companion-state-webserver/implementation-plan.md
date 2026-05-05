# Companion State Webserver Implementation Plan

## Default Decisions

These defaults are the current build baseline. Adjust them only when a concrete issue shows up.

## Stack

- [x] Language: C#
- [x] Runtime: .NET 10, matching the local repo/tooling direction
- [x] UI: Blazor Web App
- [x] Components: MudBlazor
- [x] Hosting: ASP.NET Core sidecar webserver
- [x] Data storage: in-memory current state plus optional compact JSON snapshot
- [x] Realtime updates: Server-Sent Events first, SignalR only if the UI needs richer bidirectional behavior
- [x] Tests: xUnit or the existing .NET test default chosen by project template, with parser fixtures from redacted logs

## Project Layout

Create a small app under:

```text
src/Windrose.StateWeb/
```

Suggested structure:

```text
src/Windrose.StateWeb/
  Components/
  Components/Pages/
  Domain/
  Parsing/
  Services/
  State/
  Program.cs

tests/Windrose.StateWeb.Tests/
  Fixtures/
  Parsing/
```

## Runtime Defaults

- [x] Default HTTP port: `8781`
- [x] Bind address: `0.0.0.0` inside the container
- [x] External access: internal/LAN only
- [x] Authentication: none for first build
- [x] Redaction: off by default for internal operator view
- [x] Server files mount: read-only
- [x] Mounted path inside sidecar: `/server-files`
- [x] Log path: `/server-files/R5/Saved/Logs/R5.log`
- [x] Save root: `/server-files/R5/Saved/SaveProfiles/Default`
- [x] Poll interval for save metadata: 30 seconds
- [x] In-memory event retention: last 500 events
- [x] Persisted state snapshot path: `/tmp/windrose-state/current-state.json`

## Compose Defaults

Add a sidecar service named:

```text
windrose-state-web
```

Default port mapping:

```yaml
ports:
  - "8781:8781/tcp"
```

Default volume:

```yaml
volumes:
  - ./server-files:/server-files:ro
```

## API Defaults

### Health

```text
GET /health
```

Returns service health, log availability, and last parse time.

### State

```text
GET /api/state
```

Returns the combined server state:

- server readiness
- island id
- server name
- invite code
- max players
- player count
- active players
- latest save metadata
- recent warnings

### Players

```text
GET /api/players
```

Returns active and recently disconnected players.

### Events

```text
GET /api/events
```

Returns the retained event timeline.

### Event Stream

```text
GET /api/events/stream
```

Streams server/player events to the Blazor UI.

### Save Metadata

```text
GET /api/saves/latest
GET /api/world/description
```

Returns latest backup information and parsed world description when available.

## UI Defaults

First screen: operator dashboard, not a landing page.

Views:

- [ ] Overview
- [ ] Players
- [ ] Events
- [ ] Saves
- [ ] Diagnostics

Overview cards:

- [ ] server ready status
- [ ] current island id
- [ ] invite code
- [ ] active player count
- [ ] latest backup age
- [ ] parser health

Player table columns:

- [ ] display/client name
- [ ] account id
- [ ] player session id
- [ ] connection phase
- [ ] connected at
- [ ] last seen
- [ ] disconnect reason

Events table columns:

- [ ] timestamp
- [ ] severity
- [ ] event type
- [ ] player/session
- [ ] message

## Parser Defaults

Parser should produce typed events, then reduce those events into current state.

Initial event types:

- [ ] `ServerStarted`
- [ ] `ServerInitialized`
- [ ] `ServerReady`
- [ ] `ServerSettingsObserved`
- [ ] `PlayerReserved`
- [ ] `PlayerBlConnected`
- [ ] `PlayerUeConnected`
- [ ] `PlayerLoginRequested`
- [ ] `PlayerJoined`
- [ ] `PlayerDisconnected`
- [ ] `SaveBackupRequested`
- [ ] `SaveBackupFinished`
- [ ] `ResourceUsageObserved`
- [ ] `WarningObserved`
- [ ] `ErrorObserved`

Merge key priority:

1. `BLPlayerSessionId`
2. `AccountId`
3. client name from login/join

## Save Reader Defaults

Phase 2 reads metadata only:

- [ ] active island id
- [ ] latest backup ZIP path
- [ ] latest backup timestamp
- [ ] latest backup size
- [ ] `WorldDescription.json`

Phase 3 adds RocksDB decoding only after a spike proves values can be decoded safely.

## Acceptance Criteria

Phase 1 is complete when:

- [ ] sidecar starts with `docker compose`
- [ ] dashboard loads on port `8781`
- [ ] `/health` returns OK
- [ ] `/api/state` reports log path and parser status
- [ ] server ready state appears after parsing real `R5.log`
- [ ] player login/join/disconnect events appear from fixture logs
- [ ] active player state is derived from parsed events
- [ ] tests cover the key log markers

Phase 2 is complete when:

- [ ] latest backup ZIP is discovered
- [ ] backup freshness appears in UI
- [ ] `WorldDescription.json` is parsed and exposed
- [ ] missing backup data degrades cleanly

## Explicit Non-Goals For First Build

- [ ] public internet hardening
- [ ] write access to server files
- [ ] save editing
- [ ] full map renderer
- [ ] guaranteed live coordinates
- [ ] companion app protocol compatibility
