# Companion State Webserver Remaining Work

## Purpose

This roadmap captures the work that is still open after the initial v3 observability pass.

The current code now covers:

- log-derived server/player state
- read-only `ServerDescription.json` inspection
- safe backup ZIP summaries
- browser/operator UI
- optional SignalR live push to `channel-cheevos`

The remaining work is about proving the edges, hardening deployment, and deciding whether deeper save decoding is actually worth shipping.
For stream-facing work, `cc-sidecar` owns stream support and `channel-cheevos` owns browser-source / Twitch-style integration surfaces.

## Phase 1 - Confirm the Live Push Contract

Goal: make the outbound `channel-cheevos` path deterministic before relying on it operationally.

- [x] Confirm the SignalR hub URL used by `channel-cheevos`
- [x] Confirm the webkey query-string contract
- [x] Confirm the method names for state updates and event updates
- [x] Confirm reconnect behavior after network loss
- [x] Confirm what should happen when the hub is unavailable at startup
- [x] Confirm whether state and event payloads should stay separate or be merged
- [x] Add a small contract note to the deployment docs once the receiver is verified

Notes:

- Existing `channel-cheevos` hubs already use `?webkey=` group registration patterns on routes like `gameplay`, `healthhub`, and `clienthub`.
- `channel-cheevos` now exposes a Windrose receiver hub on `windrose-state` with compatibility alias `hubs/windrose-state`.
- The Windrose sender remains configurable, but the default method names now map cleanly to the receiver's `WindroseStateUpdate` and `WindroseEvent` methods.
- Live push failures now reset the SignalR connection and retry on the next publish attempt.

## Phase 2 - Finish Deployment Hardening

Goal: make the sidecar usable on the real host without guessing at setup.

- [x] Add deployment docs for the target host
- [x] Document the `WINDROSE_STATE_*` environment variables
- [x] Document the default private/LAN-only posture
- [x] Document the read-only mount and the no-write boundary in `server-files`
- [x] Validate a throwaway host-side smoke on `192.168.0.21` with a loaded image and a temp `server-files` fixture
- [x] Update the `windrose2` Portainer stack definition with the `windrose-state-web` sidecar service
- [x] Validate the compose service under Portainer
- [x] Record any host-specific quirks in the feature notes

Notes:

- Portainer endpoint `17` on `192.168.0.252` accepted the live `windrose2` stack update after the sidecar image was loaded locally on that host.
- A live `docker ps` check on `192.168.0.252` shows `windrose-state-web` running alongside `windrose2` and `portainer_agent`.
- A live health check on `192.168.0.252` returns `/health` OK and `/api/saves/latest` now resolves the latest backup ZIP plus world summary fields.
- The live sidecar writes its snapshot file inside the container at `/tmp/windrose-state/current-state.json`.
- The updated `windrose2` compose file now passes `docker compose config`, so the remaining gap is only the deeper save decoding follow-through.
- Host-specific quirk: the actual `windrose2` server-files tree on `192.168.0.252` uses real RocksDB `Checkpoint/private` and `Checkpoint/shared_checksum` files, and the live data still exposes only summary-level world fields, not decoded player/ship/actor documents.

## Phase 3 - Decide On Real Save Decoding

Goal: determine whether the checkpoint data can be decoded safely enough to be worth productizing.

- [x] Extract a checkpoint ZIP to a temp directory
- [x] Enumerate the keys and value sizes in the extracted data
- [x] Identify the document prefixes that look like player, ship, and actor data
- [x] Determine the checkpoint container format with evidence
- [ ] Decode one player document if the format is stable
- [ ] Decode one ship document if the format is stable
- [x] Write down the safe boundary if the answer is still “summary-only”

Notes:

- The current prototype now builds a read-only checkpoint extraction tree and records file paths, sizes, and string markers for the known `R5BL*` document families.
- The live backup ZIP includes `Checkpoint/private` and `Checkpoint/shared_checksum` content plus `AdditionalRecordFiles/WorldDescription.json`.
- The current live backup shows `ShipId`, `Actor_InteractedPoiIds`, `Actor_RemovedDialogueActorIds`, and `LandscapeLocation` markers in the data SSTs, while `R5BLPlayerInWorld` / `R5BLPlayer` only appear in RocksDB metadata files (`MANIFEST-000021` and `OPTIONS-000059`). That is still not enough to claim a decoded player document.
- The checkpoint ZIP footer and table-property strings identify the container as a RocksDB block-based SST layout. That is enough to name the container format, but not enough to claim a decoded player or ship document.
- A representative live SST value shows structured field names like `Blocks`, `DataKey`, `MarkupKey`, `IslandId`, `ShipId`, and `ChangeRevision`, but the schema is still not proven enough to publish a document decoder.
- The small `shared_checksum/000015_590266782_175.blob` file is not a hidden payload store: the checkpoint `OPTIONS` files show `enable_blob_files=false`, and the blob file itself only exposes RocksDB internal metadata such as `__MaxBinaryKey27`. That closes off the easiest alternate decode path and keeps the player/ship work blocked on the SST payload format itself.
- The current live save tree does not contain any `R5BLShip` string at all, either in the checkpoint SSTs or in the manifest/options metadata. The live evidence only shows `ShipId` references inside other document families, so a true ship document decoder still cannot be proven from this snapshot.
- The live host now serves a read-only `/api/saves/latest/checkpoint` summary endpoint, which is enough for safe operator inspection but still stops short of a document decoder.
- The live host also serves `/api/saves/latest/observed-families`, which makes the safe boundary explicit (`hasStandaloneShipDocument=false`) and surfaces the current island/actor/player-reference evidence without pretending a ship decoder exists.
- The live host now also serves safe `/api/world/entities`, `/api/world/players`, `/api/world/ships`, and `/api/world/actors` slices. These are summary endpoints only, so they close the route-surface gap while still leaving the actual decoded ship/player document proof open.
- The live host now also serves `/api/history`, `/api/history/export`, `/api/history/timeseries`, and `/api/overlay/summary` so lightweight operator history, replayable samples, and overlay-friendly JSON are available without widening the trust boundary.
- Safe boundary: keep the save reader on summary-only extraction until a real serialization proof exists; do not expose decoded player, ship, or actor documents as fact yet.

## Phase 4 - Expand Only If The Data Proves Itself

Goal: add richer endpoints only if the checkpoint work proves the data is reliable and useful.

- [ ] Add `/world/entities`
- [ ] Add `/world/players`
- [ ] Add `/world/ships`
- [ ] Add `/world/actors`
- [x] Add overlay-friendly JSON if it becomes useful
- [ ] Add redaction controls if the surfaces get broader
- [x] Add deeper live-history views if the receiver path can consume them, but keep the first pass lightweight and operator-focused

## Phase 5 - Operational Polish

Goal: keep the surface useful without widening the trust boundary.

- [x] Add a compact state snapshot file if operators want a local export
- [x] Add a simple troubleshooting section for missing logs or missing backups
- [x] Add UI polish for the highest-signal panels
- [x] Consider a time-series export only if there is a real consumer and a real query shape
- [ ] Keep OBS/browser-source support in `cc-sidecar` / `channel-cheevos` rather than here unless the shared overlay contract changes

## Non-Goals

- [ ] Writing to save data
- [ ] Editing `ServerDescription.json` from the observer
- [x] Public internet exposure without authentication is out of scope for this internal-only deployment
- [ ] Reverse engineering or redistributing the closed companion app
