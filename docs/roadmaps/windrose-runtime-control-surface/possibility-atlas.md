# Windrose Runtime Possibility Atlas

## Purpose

This page is the single inventory of current, deferred, and prospective Windrose runtime actions.
It exists so operators and future implementers can see at a glance:

- what is already proven today
- what is intentionally deferred
- what is only possible through native hooks or future upstream support
- what belongs behind approval, rate limiting, and audit

## Reading guide

Status labels used on this page:

- **confirmed** — proven and documented in the current repo surface
- **deferred** — explicitly not first-class yet, but expected to be revisited
- **speculative** — interesting, but not proven in the current docs
- **native-hook-only** — likely requires UE4SS/native mod work or future upstream support
- **audit-only** — visible or reviewable, but not a write-capable action

## Canonical cross-links

- [Windrose Runtime Control Surface Map](../../features/server-state-observability/runtime-control-surface.md)
- [Windrose Runtime Control Surface Roadmap](README.md)
- [Execution Path](execution-path.md)
- [Operator Contract](operator-contract.md)
- [Questions / Open Decisions](questions.md)
- [Phases](phases.md)
- [Channel Point Redemption Contract](channel-point-redemption-contract.md)

## At-a-glance matrix

| Action | Status | Primary surface | Approval required | Audit trail | Notes |
|---|---|---|---|---|---|
| Observe server/world state | confirmed | State Web | no | yes | logs, players, saves, summaries, live updates |
| Runtime control-surface summary | confirmed | State Web | no | yes | shows observer vs execution vs approval boundary |
| Runtime action capability report | confirmed | State Web | no | yes | separates known, enabled, disabled, unsupported actions |
| Config reload | confirmed | WindrosePlus / RCON | usually no | yes | safe, repeatable operator action |
| Teleport / speed adjustments | confirmed | WindrosePlus / RCON | usually yes | yes | operator convenience and rescue workflows |
| Map generation / export | confirmed | WindrosePlus / RCON | usually yes | yes | useful for state snapshots and event prep |
| Custom command registry | confirmed | WindrosePlus / mod hooks | yes for live use | yes | the bridge for future operator actions |
| External broadcast / server message | deferred | WindrosePlus or native hook | yes | yes | not first-class in the current Lua-only surface |
| Spawn enemies / entities | native-hook-only | native hook / future mod | yes | yes | not proven as a stable API today |
| Generic world mutation | speculative | native hook | yes | yes | keep constrained and separate from observer surfaces |
| ChannelCheevos approval / audit | deferred | ChannelCheevos + Hermes | yes | yes | operator request, approval, denial, execution, replay |

## Confirmed capabilities

### Read-only observer surfaces

These are the safe surfaces already documented and used for live visibility:

- server health and runtime posture
- player list and player/session metadata
- save and checkpoint summaries
- log-derived events
- live dashboard / overlay style updates
- boundary reporting for what the runtime can and cannot do

### Controlled write surfaces

These are the write-capable actions already documented in the WindrosePlus/RCON layer:

- config reload
- player teleport
- player speed adjustment
- map generation and export workflows
- custom command registration
- command execution through the approved mod/RCON path
- diagnostics and counters that help operators understand world state

## Deferred capabilities

These are important ideas, but the current docs do **not** prove a stable first-class API yet:

- external broadcast / chat-like announcements
- server-message injection
- enemy or NPC spawning
- arbitrary world mutation from the observer layer
- broad write access from State Web itself

### Why they are deferred

Common reasons these stay out of the durable contract for now:

- the current surface is read-only
- the docs explicitly say the capability is deferred upstream
- a native method is likely required
- the action needs a tighter approval or rollback model
- the current evidence is diagnostic, not executable

## Fun implementation candidates

These are the ideas most worth exploring after the capability map is stable.

### High-value operator features

1. **Action-capability control board**
   - Best first step for safety and discoverability.
   - Shows what is supported, disabled, or unsupported before an operator clicks anything.

2. **Safe operator macros**
   - One-click helper actions for config reload, rescue, teleport, speed, and map workflows.
   - Useful both for admin work and live event support.

3. **Temporary event-mode presets**
   - Speed-racer mode, cleanup mode, staging mode, map-refresh mode.
   - Good for making the server feel like it has recognizable “modes.”

4. **Fresh map / snapshot export**
   - Easy win for overlays, recaps, and operator situational awareness.

### Viewer-facing features

5. **Structured channel-point announcements**
   - The cleanest viewer-facing win if the execution path is approval-gated and template-driven.
   - Good for “server live,” “maintenance,” “event start,” and similar messages.

6. **Viewer-visible redemption status**
   - Pending / approved / denied / executed / failed states shown in an overlay or operator panel.
   - Makes the workflow legible and trustworthy.

7. **Redemption-driven event banners**
   - Short visible pulses for live moments like countdowns, approvals, and mode changes.

### High-risk / high-fun ideas

8. **Spawned encounters or injected actors**
   - Classic “surprise event” idea.
   - Treat as native-hook-only until proven otherwise.

   - The typed dodo-swarm seam is now documented as `HandleDodoSwarm` with `targetPlayer`, `count`, `radiusMeters`, `offsetMeters`, `creatureId`, and `creatureName` fields; it remains unsupported until a real native hook exists.

9. **Chat or server-message injection**
   - Fun for immersion and crowd interaction.
   - Still speculative in the current docs.

10. **Generic world mutation API**
    - Maximum flexibility, maximum risk.
    - Keep this as a research target, not a product promise.

## Implementation surfaces

### Windrose State Web

Use this for:

- read-only observation
- summaries and live status reporting
- capability reporting
- audit-friendly posture views

Do **not** use this for direct execution of live mutations.

### WindrosePlus / RCON / mod hooks

Use this for:

- controlled live mutations
- operator macros
- execution of approved actions
- future write-capable extensions

This is the likely home for anything that actually changes the server.

### ChannelCheevos

Use this for:

- request intake
- moderation / policy gating
- approval / denial / block decisions
- idempotency and deduplication
- audit persistence

### Hermes

Use this for:

- operator review and requests
- queue visibility
- approvals and rejections as a client-facing surface

It should not become the transport bridge between the approval system and the server executor.

## Approval and audit flow

1. request received
2. normalized and validated
3. moderation or policy gate applied
4. approved, denied, or blocked
5. write-capable surface executes the approved action
6. result recorded in the audit store
7. read-only lookup surfaces expose the record later

### Audit fields worth preserving

- event id
- idempotency key
- requester
- approval decision
- decision time
- decision reason
- execution id
- execution result
- status transitions

## Recommended exploration order

If we want to keep the work fun **and** safe, the order should be:

1. operator control board and capability report polish
2. safe operator macros
3. structured announcement workflow
4. visible approval / audit feedback
5. event-mode presets and rescue workflows
6. only then native-hook investigations for spawn or chat injection

## Next slices

- Confirm the exact command / hook surface for any new write-capable action before promising it.
- Keep all observer features summary-only.
- Keep approval, execution, and audit separate.
- Promote a capability into the durable contract only after it has a documented command, arguments, permissions, and rollback behavior.

## Related docs

- `docs/features/server-state-observability/README.md`
- `docs/features/server-state-observability/runtime-control-surface.md`
- `README.md` in this directory
- `execution-path.md`
- `operator-contract.md`
- `questions.md`
- `phases.md`
- `channel-point-redemption-contract.md`
