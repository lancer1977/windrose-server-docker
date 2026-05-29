# Windrose ↔ ChannelCheevos integration test design

## Purpose

Draft the first end-to-end integration tests that prove Windrose and ChannelCheevos can cooperate without turning Windrose into a write-capable control plane.

This slice is intentionally centered on three integration lanes:

1. Chat-triggered Windrose snapshots
2. Create/event triggers fired from ChannelCheevos back into its own outbound event surface
3. Windrose state/event push into ChannelCheevos' `windrose-state` hub

## Current surface inventory

### Windrose side

- Read-only endpoints already exist for health, state, players, events, saves, and world summaries.
- `WindroseHub` currently exposes two SignalR methods on ChannelCheevos' side:
  - `WindroseStateUpdate(JsonElement state)`
  - `WindroseEvent(JsonElement evt)`
- `Windrose.StateWeb.Core` now carries the shared overlay/history/time-series/timeline contracts.

### ChannelCheevos side

- Chat commands already include `!windrose` and `!windrose-state` as streamer-only snapshot triggers.
- The MCP surface already includes:
  - `channelcheevos.message.send`
  - `channelcheevos.event.fire`
- `channelcheevos.event.fire` currently allowlists events such as `achievement_unlocked`, `reward_redeemed`, `stream_started`, `stream_ended`, `goal_reached`, `milestone_reached`, `pointsAwarded`, `custom_reward`, `raid`, and `follow`.
- ChannelCheevos already maps `windrose-state` and `hubs/windrose-state` to `WindroseHub`.

## Initial integration design

### Lane 1: Chat-triggered Windrose snapshot

Goal: prove that a chat command can publish a Windrose snapshot using the shared contract package.

Proposed flow:

1. A streamer chat command hits `!windrose` or `!windrose-state`.
2. ChannelCheevos resolves the current Windrose summary/snapshot payload.
3. The command manager renders a Stream Story / markdown summary from the shared Core types.
4. The result is published once, with a success response returned to chat.

Suggested first tests:

- `!windrose` emits a snapshot publish when the Windrose summary payload is present.
- `!windrose-state` is an alias of `!windrose` and uses the same snapshot path.
- When the Windrose payload is missing or unreadable, the command fails gracefully and does not publish.
- The rendered markdown references the shared `Windrose.StateWeb.Core` shapes instead of locally rebuilt DTOs.

### Lane 2: Create/event trigger surface

Goal: prove that ChannelCheevos can create outbound events that are safe, allowlisted, and auditable.

Proposed first event families:

- `windrose_server_ready`
- `windrose_player_joined`
- `windrose_player_disconnected`
- `windrose_backup_completed`
- `windrose_summary_refreshed`

These should start as ChannelCheevos-side events that are created from the Windrose read-only state stream, not as write-capable calls into Windrose.

Suggested first tests:

- `channelcheevos.event.fire` rejects non-allowlisted events.
- Allowed events are accepted and auditable.
- A Windrose-derived event payload can be serialized and fired through the allowlisted event tool.
- Read-scope keys cannot create events.

### Lane 3: Windrose push into ChannelCheevos hub

Goal: prove that Windrose can push read-only snapshots and timeline entries to ChannelCheevos without implying mutation support.

Proposed flow:

1. Windrose emits `WindroseStateUpdate` with a snapshot payload.
2. Windrose emits `WindroseEvent` with a timeline/event payload.
3. ChannelCheevos logs and accepts both messages on the `windrose-state` hub.
4. ChannelCheevos fans those messages out to its overlay or event subsystems if needed.

Suggested first tests:

- A state update reaches `WindroseHub` and records `windrose_state_update` success telemetry.
- A timeline event reaches `WindroseHub` and records `windrose_event` success telemetry.
- The hub path accepts `windrose-state` and `hubs/windrose-state`.
- The payload is treated as read-only JSON; no write-capable operation is implied.

## End-to-end test matrix

### E2E-1: Chat snapshot publish

Given:
- Windrose has a known summary payload
- ChannelCheevos has the `windrose` command registered

When:
- a streamer issues `!windrose`

Then:
- a snapshot is rendered from the shared Core contract
- exactly one publish is recorded
- the chat response says the snapshot was published

### E2E-2: Alias command parity

Given:
- the same Windrose state as E2E-1

When:
- a streamer issues `!windrose-state`

Then:
- the same snapshot path is used
- the output matches `!windrose` aside from the alias name

### E2E-3: Allowlisted create event

Given:
- a write-scoped webkey
- an allowed event name such as `achievement_unlocked` or a Windrose-derived allowlisted event

When:
- ChannelCheevos fires the event through `channelcheevos.event.fire`

Then:
- the event is accepted
- the audit sink records the action
- the response status is `Fired` or `Simulated` depending on the client stub

### E2E-4: Rejected create event

Given:
- a write-scoped webkey
- a disallowed event name

When:
- ChannelCheevos attempts to fire it

Then:
- the request is rejected
- the audit sink records the rejection
- nothing is sent to the SignalR client

### E2E-5: Windrose state push

Given:
- a Windrose snapshot payload built from the shared package
- a ChannelCheevos hub connection target for `windrose-state`

When:
- Windrose pushes `WindroseStateUpdate`

Then:
- ChannelCheevos accepts and logs the update
- the update is classified as read-only state, not mutation

### E2E-6: Windrose event push

Given:
- a Windrose timeline/event payload built from the shared package

When:
- Windrose pushes `WindroseEvent`

Then:
- ChannelCheevos accepts and logs the event
- downstream consumers can treat it as a trigger source

## Recommended test harness shape

- Windrose-side test fixtures should use the shared Core contract project, not local DTO copies.
- ChannelCheevos-side tests should keep `WindroseHub` and the MCP tools under test with in-memory/stub SignalR clients.
- End-to-end tests should verify the contract boundary, not just command success.
- The assertions should cover:
  - trigger name
  - payload shape
  - audit record
  - response status
  - hub method name
  - no accidental write-path behavior

## Open questions

- Should Windrose-derived create events live in the generic `channelcheevos.event.fire` allowlist, or should there be a Windrose-specific event family?
- Should `WindroseEvent` stay a raw JSON envelope, or should it be normalized to a shared typed record in a future package?
- Should the chat snapshot command emit a richer markdown card or just a compact status summary on the first pass?
- Should the integration harness live in ChannelCheevos only, or should Windrose carry a mirrored contract test suite as well?

## Completion gate for the first slice

- Chat snapshot command works end-to-end with a fixed fixture payload.
- At least one create/event trigger is accepted and audited.
- Windrose push to `windrose-state` is accepted for both state and event paths.
- All payloads use the shared `Windrose.StateWeb.Core` types where applicable.
- No write-capable behavior is implied on the Windrose side.
