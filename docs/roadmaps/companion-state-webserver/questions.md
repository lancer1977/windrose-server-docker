# Companion State Webserver Decisions

## Scope

- First deliverable: JSON API plus browser dashboard; OBS/browser-source consumption comes after the read-only API is stable.
- Keep this work in `windrose-server-docker` as a sidecar for the Windrose server stack rather than splitting it into a new repo.
- Keep the first version Windrose-specific; generic Unreal support can be revisited after the Windrose path is stable.

## State

- Prioritize online/offline status, player name, account id, current island/location, ship presence, and a compact inventory summary.
- Live events should update within seconds; backup-derived snapshot state can lag behind slightly.
- Use backup-cadence state for the first map view until live coordinates are proven.
- Persist current state plus a short event timeline; do not introduce long-term history storage in the first pass.

## Deployment

- Default the webserver to port 8080.
- Let Portainer own deployment for the sidecar.
- Keep the service LAN-only by default; widen exposure only behind an approved auth layer.
- Accept existing reverse-proxy auth if the environment already has it; otherwise stay LAN-only.

## Integration

- Do not mimic the companion app WebSocket unless the protocol is actually observed and worth mirroring.
- Have Channel Cheevos consume the read-only JSON endpoint only for operator-safe overlays and summaries.
- Let an OBS browser source consume `/api/world/summary` directly when a simple overlay is needed.
