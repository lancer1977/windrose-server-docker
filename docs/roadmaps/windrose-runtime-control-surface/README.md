# Windrose Runtime Control Surface Roadmap

## Purpose

Map the runtime paths that can change a live Windrose server and separate them from the read-only observer surfaces.

This roadmap exists because the Windrose server now has two distinct layers:

- Windrose State Web: read-only logs, saves, summaries, and live updates for operators and overlays.
- WindrosePlus: server-side mod / RCON / hook layer that can perform controlled live changes.

The roadmap covers four capabilities:

1. External broadcast or chat-like announcements
2. Enemy or entity spawning
3. General external live-state modification
4. The operator contract that will eventually expose those actions safely to ChannelCheevos/Hermes clients

## Current Assessment

- [x] Read-only observation is already implemented and documented.
- [x] WindrosePlus already has controlled write operations such as `wp.tp`, `wp.speed`, `wp.reload`, and `wp.mapgen`.
- [x] WindrosePlus exposes a command registry and RCON execution path.
- [x] `wp.say` is explicitly deferred upstream and is not yet a first-class stable API.
- [x] No first-class spawn command was found in the reviewed docs.
- [x] The runtime-control-surface documentation now records the current capability split.
- [x] External broadcast spike completed in docs-only form; no first-class Lua API was found and the native UE4SS C++ mod path remains the likely implementation route.
- [x] External broadcast has been invalidated for the current Lua-only surface; a native hook or future upstream release is required.
- [x] Spawn spike completed in docs-only form; no first-class spawn or summon API was found and the current surface appears limited to entity diagnostics plus native-hook possibilities.
- [x] Operator contract draft documented for the ChannelCheevos/Hermes control-plane boundary.
- [x] A read-only control-surface summary endpoint now exists in State Web for operator clients.
- [x] Enemy spawning has been disproven as a first-class Lua API; any spawn path remains native-hook only unless a future WindrosePlus surface appears.
- [x] The minimum safe external mutation contract is documented in `operator-contract.md`.
- [x] The ChannelCheevos/Hermes operator path for approved runtime actions is documented in `operator-contract.md`.
- [x] The runtime action capability report endpoint now separates known, enabled, disabled, and unsupported actions with explicit reasons.

## Implementation Recommendations

- Keep Windrose State Web read-only and summary-oriented.
- Surface only the boundary summary in Windrose State Web; keep live mutation in WindrosePlus.
- Treat external broadcast, spawn, and broader world mutation as separate proof slices instead of one broad API.
- Require request -> approval -> execution -> audit for every live action.
- Keep ChannelCheevos / Hermes as the approval and operator surface, not the transport bridge.

## Desired Outcome

- [x] Preserve the read-only observer stack.
- [x] Document the write-capable WindrosePlus layer separately.
- [x] Prove whether external broadcast / server-message injection is available now or needs a native hook.
- [x] Prove whether enemy spawning is available now or needs a native hook.
- [x] Define the minimum safe external mutation contract.
- [x] Define the ChannelCheevos/Hermes operator path for approved runtime actions.

## Documentation Links

- Feature overview: `docs/features/server-state-observability/README.md`
- Capability map: `docs/features/server-state-observability/runtime-control-surface.md`
- Execution path: `docs/roadmaps/windrose-runtime-control-surface/execution-path.md`
- ChannelCheevos integration tests: `docs/roadmaps/windrose-runtime-control-surface/channel-cheevos-integration-tests.md`
- Source docs: `docs/features/server-state-observability/architecture.md`
- WindrosePlus docs root: `docs/` in the WindrosePlus repository
