# Windrose Runtime Control Surface Execution Path

## Verified baseline

The current read-only/control-surface boundary is already implemented and test-backed:

- `GET /api/runtime/control-surface` exists and reports the split between observer, execution, and approval surfaces.
- The endpoint advertises the currently supported observer-side behavior: log/save observation, overlay/summary snapshots, live status push, and auditable operator request records.
- The same endpoint marks external broadcast, entity spawning, and generic world mutation as deferred.

Validation that passed in this repo:

- `dotnet test tests/Windrose.StateWeb.Tests/Windrose.StateWeb.Tests.csproj --filter "RuntimeControlSurfaceEndpointSummarizesTheReadOnlyBoundary"`

## Next slices

### Feasible now: formalize the Lua/RCON surface

Goal: keep documenting exactly which WindrosePlus actions are actually available today and which ones are only theoretical.

Known-feasible actions from the current docs reviewed:

- config reload
- player teleport
- player speed adjustment
- map generation and export workflows
- custom command registration and RCON execution
- diagnostics / counters such as creature and entity tallies

Still not proven as first-class APIs:

- in-game chat broadcast / `wp.say`
- enemy or NPC spawning
- generic world mutation from the observer layer

If a capability only exists through a native hook or future upstream release, keep it in the deferred bucket instead of treating it as stable.

### Next: shared contract package boundary

Goal: expose the data shapes and pure transforms that the server app and downstream consumers share through `Windrose.StateWeb.Core`.

That package should remain the canonical home for:

- `WindroseOverlaySnapshot`
- `WindroseOverlaySnapshotContext`
- `WindroseHistoryExport`
- `WindroseTimeSeriesExport`
- `WindroseTimeSeriesWindow`
- `WindroseTimelineEntry`
- `IWindroseOverlaySnapshotSource`
- `IWindroseHistorySource`
- `IWindroseTimeSeriesSource`
- `WindroseSurfaceExtensions`

ChannelCheevos should consume those payloads and helpers from NuGet and normalize from them, not re-invent the payload shapes locally.

### Native-hook seam: dodo swarm around a selected player

Goal: prove the minimal write-capable bridge for `windrose.spawn.dodo_swarm` without pretending the live mutation path exists yet.

The local contract shape is the typed spawn request mirrored in ChannelCheevos:

- seam / handler name: `HandleDodoSwarm`
- target selector: `targetPlayer`
- spawn count: `count`
- spawn radius: `radiusMeters`
- spawn offset: `offsetMeters`
- creature id: `creatureId`
- creature name: `creatureName`
- dry-run / logging output: log the resolved target, count, radius/offset, creature id/name, and whether the hook was skipped or rejected
- failure modes: unknown target player, invalid count or spawn radius, hook unavailable, unsafe live server state, or live execution without approval

This seam is native-hook-only until the real server-side bridge exists. The State Web capability report now surfaces it as an unsupported action so downstream action-contract consumers can wire against the exact shape without guessing.

### Later: contract and operator wiring

Goal: turn the verified runtime actions into a minimum safe mutation contract and then wire the ChannelCheevos / Hermes approval path around it.

Use `operator-contract.md` as the canonical boundary note while the implementation remains in flight.

## Working rules

- Preserve Windrose State Web as read-only.
- Keep write-capable actions in WindrosePlus or a native hook path.
- Require request -> approval -> execution -> audit for any live mutation.

## Native-plugin docs/tests verification matrix

This matrix makes the backlog sweep explicit for the next coder. It ties each native-plugin story to the repo doc that currently proves it, or to the gap that still needs a follow-up proof.

| Story | Current status | Proof / next command |
|---|---|---|
| native-plugin-1 runtime + load path | documented, but still worth a dedicated smoke run | `README.md` plus `scripts/install_windrose_plus.sh` and `scripts/build_windrose_plus_pak.sh`; verify with a readback of the install path and a shell syntax check |
| native-plugin-2 booting skeleton | proven on the approved dev stack | `scripts/smoke_windrose_sidecar_bridge.sh`; Alienware dev `windrose2-dev` log contains `[Lua] [windrose-sidecar-bridge] loaded in dry-run-only mode ...`; `http://127.0.0.1:8782/api/plugin/status` returns `connected: true` |
| native-plugin-3 shared runtime plumbing | documented boundaries, test surface still expanding | `README.md` and `docs/features/server-state-observability/runtime-control-surface.md` |
| native-plugin-4 first visible server-side action | deferred/native-hook-only for now | `operator-contract.md` and `possibility-atlas.md`; do not promote until a real write-capable hook is proven |
| native-plugin-5 packaging + rollback | documented package/install flow | `README.md`, `scripts/install_windrose_plus.sh`, `scripts/build_windrose_plus_pak.sh` |
| native-plugin-6 verification matrix | complete as a docs sweep artifact | this section plus `docs/roadmaps/windrose-runtime-control-surface/possibility-atlas.md` |

Open gaps to keep visible:

- native-plugin-2 dev boot proof is captured; production rollout, if wanted, should be a separate deployment card.
- native-plugin-4 still needs a proven write-capable hook or an upstream-native announcement API before it can move out of deferred/native-hook-only.
- Do not promote a capability into the durable contract until it has a documented command or hook and a testable path.
