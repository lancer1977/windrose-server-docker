# Companion State Webserver Deployment Notes

## Purpose

Document the current deployment shape for the Windrose state sidecar so operators can bring it up without digging through source files.

## Current Deployment Shape

- [x] Runs as a Docker Compose sidecar named `windrose-state-web`
- [x] Binds `8781/tcp` on the host to the same port in the container
- [x] Mounts `./server-files` read-only at `/server-files`
- [x] Uses `/server-files/R5/Saved/Logs/R5.log` as the active log path
- [x] Uses `/server-files/R5/Saved/SaveProfiles/Default` as the save root
- [x] Reads `/server-files/R5/ServerDescription.json` when present
- [x] Persists a compact local snapshot to `/tmp/windrose-state/current-state.json`

## Environment Variables

- [x] `WINDROSE_STATE_PORT`
- [x] `WINDROSE_STATE_SERVER_FILES_PATH`
- [x] `WINDROSE_STATE_LOG_RELATIVE_PATH`
- [x] `WINDROSE_STATE_SAVE_ROOT_RELATIVE_PATH`
- [x] `WINDROSE_STATE_SERVER_DESCRIPTION_RELATIVE_PATH`
- [x] `WINDROSE_STATE_SNAPSHOT_PATH`
- [x] `WINDROSE_STATE_TAIL_FROM_END`
- [x] `WINDROSE_STATE_ENABLE_CHANNEL_CHEEVOS_PUSH`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_HUB_URL`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_TARGET`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_HUB_URL_DEV`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_HUB_URL_DEBUG`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_HUB_URL_PROD`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_WEBKEY`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_WEBKEY_DEV`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_WEBKEY_DEBUG`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_WEBKEY_PROD`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_STATE_METHOD`
- [x] `WINDROSE_STATE_CHANNEL_CHEEVOS_EVENT_METHOD`
- [x] `SEQ__SERVERURL`
- [x] `SEQ__APIKEY`
- [x] `SEQ__MINIMUMLEVEL`

## Operational Guidance

- [x] Treat the sidecar as LAN-only unless a reverse proxy or auth layer is added.
- [x] Keep the `server-files` mount read-only.
- [x] Do not grant the sidecar write access to `R5/Saved`.
- [x] Keep live push disabled by default until the live deployment path is explicitly exercised.
- [x] Use the snapshot file as an operator convenience only; it is not a source-of-truth save.
- [x] Build the outbound SignalR URL with an encoded `webkey` query parameter.

## Receiver Notes

- [x] Existing `channel-cheevos` SignalR hubs such as `gameplay`, `healthhub`, and `clienthub` already use `?webkey=` group registration patterns.
- [x] `channel-cheevos` now exposes a Windrose receiver hub on `windrose-state` with compatibility alias `hubs/windrose-state`.
- [x] The receiver accepts `WindroseStateUpdate` and `WindroseEvent` as separate read-only ingress methods.
- [x] The Windrose sender should stay configurable until the operational receiver path is exercised in a live deployment.
- [x] The `windrose2` Portainer stack definition now includes the `windrose-state-web` sidecar service.

## Validation Checklist

- [x] Confirm `docker compose config` succeeds with the checked-in example environment.
- [x] Confirm the service starts and serves the API in a runtime container smoke.
- [x] Confirm `/health` responds on port `8781`.
- [x] Confirm `/api/state` responds on port `8781`.
- [x] Confirm the dashboard loads in a throwaway host-side smoke without needing write access to `server-files`.
- [x] Confirm the service starts cleanly in Portainer.
- [x] Confirm the snapshot path is writable inside the container.
- [x] Confirm the live push settings remain disabled by default.
- [x] Confirm the save metadata reader consumes a valid latest backup ZIP.

## Follow-Up

- [x] Capture any host-specific quirks that show up during Portainer validation.
- [x] Add a smoke command or checklist for the next rollout.

## Next Rollout Smoke

Use this sequence for a disposable host-side validation when a real deployment is present:

1. Load the built image on the target host.
2. Mount a read-only `server-files` tree that contains:
   - `R5/ServerDescription.json`
   - `R5/Saved/Logs/R5.log`
   - at least one `*_Latest.zip` backup under `R5/Saved/SaveProfiles/Default/RocksDB_v2_Backups/Worlds/<island-id>/`
3. Start the container on a temporary port.
4. Verify:
   - `GET /`
   - `GET /health`
   - `GET /api/saves/latest`
   - snapshot file creation at the configured `WindroseStateOptions.SnapshotPath`
5. Remove the temporary container after the checks pass.

Example commands:

```bash
docker load -i /tmp/windrose-state-web-roadmap.tar
docker run -d --name windrose-state-web-host-smoke \
  -p 8782:8781 \
  -v /tmp/windrose-state-smoke-host/server-files:/server-files:ro \
  -v /tmp/windrose-state-smoke-host/state:/tmp/windrose-state \
  windrose-state-web-roadmap:latest
curl -fsS http://127.0.0.1:8782/health
curl -fsS http://127.0.0.1:8782/api/saves/latest
test -f /tmp/windrose-state-smoke-host/state/current-state.json
docker rm -f windrose-state-web-host-smoke
```

## Dev Refresh Script

- [x] `scripts/refresh_windrose2_dev.sh` builds the latest sidecar image, copies it to `192.168.0.252`, refreshes the `windrose2-dev` clone, and smokes `/health` plus `/api/saves/latest/observed-families`.
- [x] Default refresh targets:
  - host: `192.168.0.252`
  - dev root: `/home/lancer1977/game_servers/windrose2-dev`
  - API: `http://127.0.0.1:8782`
- [x] The script can be overridden with `WINDROSE_DEV_HOST`, `WINDROSE_DEV_ROOT`, `WINDROSE_STATE_IMAGE`, `WINDROSE_STATE_TAR`, `WINDROSE_REMOTE_TAR`, and `WINDROSE_DEV_API_BASE`.
- [x] `scripts/restart_windrose2_dev.sh` skips the build/copy step and only recreates the dev containers plus smoke checks.

## Portainer Note

- [x] Portainer endpoint `17` on `192.168.0.252` accepted the live `windrose2` stack update after the sidecar image was loaded locally on that host.
- [x] A live `docker ps` check on `192.168.0.252` shows `windrose-state-web` running alongside `windrose2` and `portainer_agent`.
- [x] A live health check on `192.168.0.252` returns `/health` OK and `/api/saves/latest` degrades cleanly when no latest backup ZIP exists.
- [x] A live checkpoint summary check on `192.168.0.252` returns `/api/saves/latest/checkpoint` with safe container and entry metadata only.
- [x] A live observed-families check on `192.168.0.252` returns `/api/saves/latest/observed-families` with safe family hints and an explicit `hasStandaloneShipDocument=false` gate.
- [x] The live main sidecar now runs with `WindroseState__ChannelCheevosTarget=prod` and connects to `https://channelcheevos.com/windrose-state` using the provided production webkey.
- [x] The live sidecar writes its snapshot file inside the container at `/tmp/windrose-state/current-state.json`.
- [x] The deployment source of truth was updated in `/home/lancer1977/code/gitops/systems/r620/inventory/portainer-stacks/windrose2/compose.yml` and passes `docker compose config`.
- [x] Host-specific quirk: the actual `windrose2` server-files tree on `192.168.0.252` has a latest backup ZIP, and the live metadata surface now exposes safe checkpoint summaries but still needs the real per-value payload format to be decoded beyond summary fields.

## Troubleshooting

- [x] If `/health` reports degraded and the log file is missing, verify the `server-files` mount and the `R5/Saved/Logs/R5.log` path.
- [x] If backup metadata is missing, verify the save root and confirm a latest `*_Latest.zip` exists under `RocksDB_v2_Backups/Worlds/<island-id>/`.
- [x] If the snapshot file does not appear, confirm the container can write to the configured `WindroseStateOptions.SnapshotPath`.
- [x] If live push stays disabled, confirm the three `WINDROSE_STATE_ENABLE_CHANNEL_CHEEVOS_PUSH`, hub URL, and webkey values are set together.
- [x] If live push is enabled, confirm the target selects the intended `dev`, `debug`, or `prod` hub and webkey pair.
