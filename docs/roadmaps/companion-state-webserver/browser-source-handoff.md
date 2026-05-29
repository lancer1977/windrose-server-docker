# Browser-source / cc-sidecar Handoff

This note captures the current stream-facing ownership split for Windrose observability surfaces.

## Ownership split

- Windrose repo: read-only state dashboard, safe summary APIs, overlay JSON, and the `/map` proof page.
- `cc-sidecar` / `channel-cheevos`: live browser-source and Twitch-style consumer surfaces.
- The Windrose sidecar does not mutate live OBS scenes or browser sources.

## Canonical live surfaces

- Browser dashboard: `/map`
- Overlay-friendly JSON: `/api/world/summary`
- Safe operator overlay summary: `/api/overlay/summary`
- Live push receiver on `channel-cheevos`: `windrose-state`
- Compatibility receiver alias: `hubs/windrose-state`

## Live push contract

- Outbound sender remains configurable through `WINDROSE_STATE_*` settings.
- Query-string auth uses the shared `webkey` parameter.
- Receiver methods:
  - `WindroseStateUpdate`
  - `WindroseEvent`

## Operational note

Use the Windrose sidecar only for safe read-only state and summary transport. If a live scene or browser source needs mutation, that work belongs in the consumer stack and should be validated there.

## Evidence to keep handy

- `README.md` documents the read-only dashboard and the optional `channel-cheevos` push settings.
- `docs/roadmaps/companion-state-webserver/deployment.md` records the SignalR hub URL and receiver contract.
- `docs/roadmaps/companion-state-webserver/remaining-work.md` now treats browser-source support as downstream ownership.
