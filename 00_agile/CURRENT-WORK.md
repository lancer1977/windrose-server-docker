# Current Work / Pickup Point

This is the handoff point for the Windrose / Valheim sidecar-plugin bridge work.

## Kanban
- Hermes board: `game-harness`
- Completed docs/tests sweep: `t_ee65fa7e` — windrose swarm: native-plugin docs/tests verification sweep
- Follow-up live boot proof: `t_86e5c709` — windrose swarm: capture sidecar bridge live boot proof

## Current slice
- Local smoke proof added: `scripts/smoke_windrose_sidecar_bridge.sh`.
- Dev-server proof captured on Alienware `/home/lancer1977/game_servers/windrose2-dev` after a dev restart.
- `http://127.0.0.1:8782/api/plugin/status` returned `connected: true`, `status: started`, and `mode: dry-run-only`.
- UE4SS/WindrosePlus log proof: `[Lua] [windrose-sidecar-bridge] loaded in dry-run-only mode; sidecar=http://windrose-state-web:8781; bridgeRoot=Z:\home\steam\server-files\windrose_plugin_bridge`.

## Where to resume
- Next safe slice: keep the bridge dry-run/read-only and investigate a real write-capable native hook only as a separate reviewed card.
- Do not imply live mutation in Windrose State Web; the verified dev bridge validates the lifecycle and action shape only.
- Keep `00_agile/backlog/windrose-native-plugin-docs-tests-sweep.md` and `docs/roadmaps/windrose-runtime-control-surface/execution-path.md` aligned if the native-hook work advances.
