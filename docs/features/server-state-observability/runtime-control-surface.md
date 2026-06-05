# Windrose Runtime Control Surface Map

## Purpose

This note separates the Windrose surfaces that are safe to observe from the surfaces that can actually mutate a running server.

It is meant to answer four practical questions:

1. Can we observe state?
2. Can we inject chat?
3. Can we spawn enemies or other entities?
4. Can we modify live server state externally?

## Summary

Windrose has two distinct layers:

- Windrose State Web / browser dashboard: read-only observer surface.
- WindrosePlus: server-side mod and RCON layer that can mutate controlled pieces of runtime state.

Do not confuse the observer layer with the write layer. If a future feature needs live modification, it belongs in WindrosePlus/RCON/mod hooks, not in the read-only state web server.

## Capability Matrix

| Capability | Current status | Evidence / notes |
|---|---|---|
| Observe server and world state | Yes | Windrose State Web already exposes logs, players, saves, world summary, and a SignalR live-update path for approved consumers. |
| External broadcast / server messages | Not confirmed as a first-class Lua API | WindrosePlus docs show a deferred `wp.say` note. The admin module explicitly says `wp.kick / wp.netid / wp.say` are deferred to v1.3.0 because Lua-only UE4SS cannot call the native methods required for those actions. |
| Spawn enemies / entities | Dry-run placeholder only | WindrosePlus exposes `HandleDodoSwarm` as a dry-run seam with a Dodo/Wolf allowlist and random-selection support, but no first-class live spawn command is proven yet. The current docs still treat real spawning as native-hook-only until approval-gated execution exists. |
| Modify live server state externally | Yes, in controlled ways | WindrosePlus already supports RCON, custom commands, config reload, teleport, speed, time, map export, map generation, and other admin workflows. |

## What is available today

### Read-only observation

Windrose State Web remains the safe observer surface for:

- server health
- player list and player/session metadata
- save/checkpoint summaries
- log-derived events
- live dashboard updates

### Controlled write operations

WindrosePlus currently provides a write path through RCON and Lua mod commands. Examples include:

- `wp.reload` - reload config from disk
- `wp.tp` - teleport a player
- `wp.speed` - change player movement speed
- `wp.mapgen` / `wp.mapexport` - map generation workflows
- `wp.givestats` - audit-only compensation note
- `wp.creatures` / `wp.entities` - world diagnostics
- `API.registerCommand(...)` - custom mod commands

### Not yet proven / not first-class

The following are not currently documented as stable first-class APIs in the material reviewed:

- external broadcast / server messages
- enemy or NPC spawning (the current docs only prove a dry-run `HandleDodoSwarm` seam with a Dodo/Wolf allowlist)
- arbitrary world mutation via a generic server API

Those functions may still be possible through UE4SS hooks or native mod code, but they should be treated as reverse-engineering targets rather than assumed capabilities.

## Runtime capability report

`GET /api/runtime/action-capabilities` returns the current manifest-backed action support report.

The report separates:

- `knownActionIds` — actions cataloged by the ChannelCheevos Windrose manifest mirror
- `enabledActionIds` — actions that are actually runnable in the current runtime
- `disabledActionIds` — actions that are supported in principle but currently turned off
- `unsupportedActions` — cataloged actions that still lack a proven runtime hook, with explicit reasons

In the current slice, the manifest-backed Windrose actions are cataloged but unsupported; no action is claimed as enabled until a proven runtime hook exists.

## Recommended boundaries

- Keep Windrose State Web read-only.
- Put all write-capable actions behind WindrosePlus RCON or a mod.
- Treat external broadcast, Twitch relay, and spawn actions as separate implementation projects.
- Only promote a capability into the durable contract after it has a documented command or hook and a testable path.

## Practical implementation path

1. Preserve the observer dashboard as the source of truth for state.
2. Add or extend WindrosePlus commands for controlled mutations.
3. If chat or spawn is needed, search for the exact UE4SS hook or native method first.
4. Once a capability is proven, document its command name, arguments, permissions, and rollback behavior here.
5. Keep operator actions auditable through RCON logs or the events log.

## References

- `README.md` in WindrosePlus docs: command and scripting surface
- `WindrosePlus/Scripts/modules/admin.lua`: RCON command registry
- `WindrosePlus/Scripts/modules/rcon.lua`: file-based command execution path
- `WindrosePlus/Scripts/main.lua`: public API setup and mod loading
- `docs/features/server-state-observability/README.md`: observer surface overview
- `docs/roadmaps/windrose-runtime-control-surface/README.md`: backlog for chat, spawn, mutation, and operator integration proof work
- `docs/roadmaps/windrose-runtime-control-surface/possibility-atlas.md`: single inventory of current, deferred, and speculative runtime actions
