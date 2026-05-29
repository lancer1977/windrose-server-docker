# Spike 001: External Broadcast Feasibility

## Question

Can WindrosePlus send a server-side broadcast or announcement from the server layer without modifying the game binary?

## Why this matters

If external broadcast is available, then live stream interactions can drive visible in-game announcements and operator feedback without relying on the read-only state web.

## Research approach

- Inspect the WindrosePlus docs and admin module for any existing broadcast, announcement, or relay API.
- Look for native-method blockers that would prevent Lua-only implementation.
- Treat the result as throwaway feasibility work, not production design.

## Evidence reviewed

- `WindrosePlus/Scripts/modules/admin.lua`
- `docs/commands.md`
- `docs/scripting-guide.md`

## Findings

- `wp.say` is explicitly mentioned in the admin module comments, but it is deferred to v1.3.0.
- The admin module says Lua-only UE4SS cannot call the native methods needed for this feature.
- The same note names the missing native calls: `UNetConnection::Close`, `AActor::Destroy`, `UWorld::Exec`, and `APlayerController::ConsoleCommand`.
- The scripting guide shows scheduled announcements as server-log broadcasting, not a documented in-game broadcast/send API.
- I did not find a documented first-class broadcast command in the current docs.
- I did find a general Lua command registry and hook surface, which means a native extension or a deeper hook may still make this possible later.

## Verdict: INVALIDATED

The current documented Lua-only surface does not provide a supported way to send a server-side broadcast message. A native UE4SS extension or a later WindrosePlus release would be required before this becomes a real implementation path.

## What worked

- The docs clearly identify the blocker instead of leaving it ambiguous.
- The repository already has a write-capable command layer, so the feature boundary is clear.

## What didn't

- There is no first-class, documented broadcast/send API in the reviewed surface.
- The current Lua-only path is explicitly insufficient for `wp.say` and related broadcast actions.

## Surprise

The docs are unusually direct: they do not imply chat support; they explicitly state that it is deferred and requires a native mod.

## Recommendation for the real build

- Keep external broadcast out of the read-only observer layer.
- Treat external broadcast as a write-capable WindrosePlus feature.
- If Twitch relay or admin broadcast is still desired, build it as a native mod or wait for upstream support, then document the exact command and permissions model.
