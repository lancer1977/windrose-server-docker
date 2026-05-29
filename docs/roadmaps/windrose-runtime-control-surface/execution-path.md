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

### Later: contract and operator wiring

Goal: turn the verified runtime actions into a minimum safe mutation contract and then wire the ChannelCheevos / Hermes approval path around it.

Use `operator-contract.md` as the canonical boundary note while the implementation remains in flight.

## Working rules

- Preserve Windrose State Web as read-only.
- Keep write-capable actions in WindrosePlus or a native hook path.
- Require request -> approval -> execution -> audit for any live mutation.
- Do not promote a capability into the durable contract until it has a documented command or hook and a testable path.
