# Windrose Runtime Control Surface Execution Path

## Verified baseline

The current read-only/control-surface boundary is already implemented and test-backed:

- `GET /api/runtime/control-surface` exists and reports the split between observer, execution, and approval surfaces.
- The endpoint advertises the currently supported observer-side behavior: log/save observation, overlay/summary snapshots, live status push, and auditable operator request records.
- The same endpoint marks external broadcast, entity spawning, and generic world mutation as deferred.

Use the approved dev server only for any live validation. Random-player probes stay read-only; mutation smokes must use a throwaway/non-main character or recorded consent plus rollback evidence.

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

### Hook inventory: current proof candidates and classifications

The current repo surfaces split into four useful buckets:

| Surface / candidate hook | Classification | Evidence in repo | Why it matters |
|---|---|---|---|
| `GET /api/plugin/manifest`, `GET /api/plugin/status`, and the bridge heartbeat | read-only | State Web reads the plugin heartbeat and manifests the current policy values | Safe observer/readback surface; no live mutation |
| `GET /api/plugin/events/recent` and `windrose_plugin_bridge/events/*.json` | read-only typed event publishing | The WindrosePlus plugin writes heartbeat/readback/error V3 envelopes into the shared bridge directory and State Web reads them back through a dedicated endpoint | Safe event publishing, no mutation |
| `HandleDodoSwarm` now delegated from `plugins/windrose-sidecar-bridge/init.lua` to `plugins/windrose-sidecar-bridge/modules/dodo_swarm.lua`, plus `/api/plugin/actions/execute` | dev-only queue/readback | State Web writes approved action files under `windrose_plugin_bridge/actions/`; WindrosePlus consumes them and writes `results/{actionRequestId}.json` on `windrose2-dev`; result reports `nativeSpawn=false` | This proves command delivery and plugin-side writeback, but not native in-game spawning |
| `Summon totem object placement and inventory stack enforcement` | unsafe/unknown | No concrete live hook is proven; object ids must come from the native object registry or manifest, and stack-size docs explicitly avoid writing `stack_size` / `inventory_size` because upstream inventory validation can break | Keep these deferred until a safe native/upstream surface is demonstrated |

Recommended first native proof: teleporter-counting / placement-guard hook.

Rationale: it is the smallest safe proof that still moves toward a live action. A read-only island lookup plus teleporter count check can be verified without changing game state, and the same seam can later grow into reject/queue logic for over-cap placements.

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

Goal: prove the minimal write-capable bridge for `windrose.spawn.dodo_swarm` without pretending native actor spawning exists yet.

Exact hook entrypoint:

- public bridge handler: `HandleDodoSwarm`
- native probe: `ExecuteDodoSwarmNative`
- dispatch gate: `ExecuteInGameThread` plus `game_thread_dispatch_enabled()`

Current proof status:

- dry-run remains the default behavior
- dev-execute queue/readback is proven on `windrose2-dev`
- the current live-server evidence still reports `nativeSpawn=false` when UE4SS game-thread dispatch is disabled (`HookEngineTick=0` and `HookUObjectProcessEvent=0`)
- because of that, the native gameplay spawn itself remains unproven and should stay in its own follow-up card

Implemented V4 boundary:

- `POST /api/plugin/actions/execute` is gated by `WindroseState__PluginBridgeDevExecutionEnabled=true` and requires `approvalId` plus `modeId` of `operator-non-main-character` or `consenting-dev-player`.
- Accepted requests are written as JSON action files in `windrose_plugin_bridge/actions/` and indexed in `pending.txt`.
- The WindrosePlus plugin consumes pending actions in `dev-execute` mode and writes `windrose_plugin_bridge/results/{actionRequestId}.json`.
- `GET /api/plugin/actions/{actionRequestId}/result` returns pending status until writeback exists, then returns the plugin result.
- Current result proof is `status=executed`, `executed=true`, `outcome=dev-executed-plugin-writeback-no-native-spawn`, and `nativeSpawn=false`; do not describe this as live creature spawning.

The local contract shape is the typed spawn request mirrored in ChannelCheevos:

- seam / handler name: `HandleDodoSwarm`
- target selector: `targetPlayer`
- spawn count: `count`
- spawn radius: `radiusMeters`
- spawn offset: `offsetMeters`
- creature id: `creatureId`; currently allow-listed to `R5.Creature.Dodo` or `R5.Creature.Wolf`
- creature name: `creatureName`; currently allow-listed to `Dodo` or `Wolf`
- summon object: optional `summon` wrapper with `creatureId`, `creatureName`, `creature`, `selection`, `creaturePool`, `count`, `radiusMeters`, and `offsetMeters`; nested values override legacy top-level count/radius/offset fields
- random summon selection: `summon.selection = "random"` or `summon.creature = "random"` selects one allowed creature from `summon.creaturePool` or the default Dodo/Wolf pool
- dry-run / logging output: log the resolved target, count, radius/offset, summon selection mode, creature id/name, and whether the hook was skipped or rejected
- failure modes: unknown target player, invalid count or spawn radius, hook unavailable, unsafe live server state, or live execution without approval

Next proof step:

- keep the current queue/readback path default-off for live mutation
- move the native actor proof into a separate gated-live card that can only be attempted on a throwaway dev stack with rollback and crash observation
- once that card exists, the first observable success criteria should be `nativeSpawn=true` and a concrete spawned count in the result file, not just action acceptance

### Native-hook seam: summon totem object dry-run contract

Goal: prove the minimal dry-run bridge for placing a summon totem object without pretending the live placement path exists yet.

The local contract shape is the typed placement request mirrored in the future native hook and the plugin manifest:

- seam / handler name: `HandleSummonTotemObject`
- action id: `windrose.place.summon_totem_object`
- target selector: `targetPlayer`
- object id: `objectId`; this must be sourced from the native object registry exposed by the hook or the plugin manifest, not hardcoded in repo docs
- object name: `objectName`; optional human-friendly alias
- summon object: optional `totem` wrapper with `objectId`, `objectName`, `selection`, `objectPool`, `count`, `radiusMeters`, `offsetMeters`, `snapToGround`, and `placementMode`; nested values override legacy top-level placement fields
- object allow-list source of truth: `GET /api/plugin/manifest` should mirror the native registry as `allowedObjectIds`; until that registry exists, the dry-run endpoint must reject live placement and only report validation results
- random summon selection: `totem.selection = "random"` or `totem.object = "random"` selects one allowed object from `totem.objectPool`
- dry-run / logging output: log the resolved target, object id/name, placement mode, count, radius/offset, and whether the hook was skipped or rejected
- failure modes: unknown target player, missing objectId, objectId not present in the allow-list, invalid count or placement radius, hook unavailable, unsafe live server state, or live execution without approval

### Bridge config policy: max teleporters per island and requested stack size

Goal: expose desired server policy values as server/plugin config now, without pretending live placement or inventory enforcement exists yet.

Current contract:

- env/config key: `WINDROSE_MAX_TELEPORTERS_PER_ISLAND`
- default: `3`
- generated bridge file: `server-files/windrose_plugin_bridge/config.json`
- JSON shape: `limits.maxTeleportersPerIsland`
- status echo: plugin heartbeat writes the same value under `limits.maxTeleportersPerIsland`
- env/config key: `WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER`
- default: `1`
- accepted values: `1`, `2`, or `3`
- JSON shape: `limits.requestedStackSizeMultiplier`
- enforcement marker: `limits.stackSizeEnforcement = "disabled-upstream-no-live-write"`
- State Web visibility: `GET /api/plugin/manifest` advertises the env keys/defaults, and `GET /api/plugin/status` returns heartbeat values when the plugin has started

These are contract/config-only until native hooks are proven. Future teleporter enforcement should reject or queue teleporter placement requests that would exceed the current island's configured cap, then record that decision in the approval/audit path. Future stack-size enforcement needs a native/upstream-safe inventory hook; do not write legacy `stack_size` / `inventory_size` keys into live player state.

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
| native-plugin-2 booting skeleton | proven on the approved dev stack | `scripts/smoke_windrose_sidecar_bridge.sh`; Alienware dev `windrose2-dev` status now returns `connected: true`, `mode=dev-execute`, and queue/readback message; rollback path returns it to `dry-run-only` |
| native-plugin-3 shared runtime plumbing | implemented and dev-verified | `GET /api/plugin/smoke-options`, `POST /api/plugin/actions/execute`, `GET /api/plugin/actions/{actionRequestId}/result`, and `tests/Windrose.StateWeb.Tests/Api/WindroseEndpointsTests.cs`; `windrose2-dev` returned `status=executed` with `nativeSpawn=false` |
| native-plugin-4 first visible server-side action | deferred/native-hook-only for now | V3 proves queue/readback only; do not promote until a real write-capable actor hook returns `nativeSpawn=true` safely |
| native-plugin-5 packaging + rollback | documented package/install flow | `README.md`, `scripts/install_windrose_plus.sh`, `scripts/build_windrose_plus_pak.sh` |
| native-plugin-6 verification matrix | complete as a docs sweep artifact | this section plus `docs/roadmaps/windrose-runtime-control-surface/possibility-atlas.md` |

## V2 safe smoke harness matrix

Use `docs/roadmaps/windrose-runtime-control-surface/safe-smoke-harness-matrix.md` for the mode-by-mode smoke guidance. State Web mirrors the same safe-mode choices at `GET /api/plugin/smoke-options` for clients and scripts. It splits read-only probes from any run that can mutate a live server, and it records the expected evidence and block condition for each mode before any live smoke is attempted.

Open gaps to keep visible:

- native-plugin-2 dev boot proof is captured; production rollout, if wanted, should be a separate deployment card.
- native-plugin-4 still needs a proven write-capable hook or an upstream-native announcement API before it can move out of deferred/native-hook-only.
- Do not promote a capability into the durable contract until it has a documented command or hook and a testable path.
