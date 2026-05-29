# Windrose Operator Contract Draft

## Purpose

Define the safe control-plane boundary for live Windrose actions.

This draft separates three concerns:

- Windrose State Web: read-only observation, summaries, and live status.
- WindrosePlus: execution surface for runtime actions.
- ChannelCheevos / Hermes: approval and operator surfaces that request, review, and authorize actions.

## Ownership Model

### Windrose State Web

Keeps the observer layer read-only.
It can expose:

- server state
- player state
- saves and diagnostics
- live updates for overlays and dashboards

It should not be the primary execution path for live mutation.

### WindrosePlus

Owns the execution surface for runtime actions.
It is the place where a live action actually happens if the action is approved.

Candidate action classes:

- chat or announcement injection
- entity or enemy spawning
- world-state mutation
- movement or gameplay adjustments already exposed by WindrosePlus commands

### ChannelCheevos

Owns approval, review, audit, and operator policy.
It should remain the source of truth for who may request, approve, reject, or revoke runtime actions.

### Shared contract package

`Windrose.StateWeb.Core` should carry the shared payloads and pure transforms that Windrose and its consumers agree on.
It is the right place for:

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

These are the payloads and adapters ChannelCheevos should import from NuGet instead of rebuilding locally.
It is not the place for live execution, RCON auth, or ChannelCheevos-specific transport policy.

If a consuming app needs a new field shape, add it here first so ChannelCheevos and other consumers do not reconstruct their own version of the contract.

### Hermes

Acts as an operator client.
It may request actions, show queue state, and display approvals or rejections.
It should not sit between Windrose and the execution surface.

## Proposed Flow

1. A Windrose client or operator client submits a runtime-action request.
2. ChannelCheevos stores the request in a per-instance or per-channel queue.
3. An operator approves, rejects, or revokes the request.
4. If approved, ChannelCheevos issues a scoped durable credential or action grant.
5. WindrosePlus executes only the approved action.
6. ChannelCheevos records the action outcome for audit and replay.

## Request Types

The contract should keep action types explicit.

Initial action groups:

- `chat.broadcast`
- `entity.spawn`
- `world.mutate`
- `server.admin`

The contract should not imply that every action is already available. Some actions may remain experimental or native-hook only until proven.

## State Machine

Recommended states:

- `pending`
- `approved`
- `rejected`
- `revoked`
- `expired`

Transition notes:

- `pending -> approved` requires operator action.
- `pending -> rejected` requires operator action.
- `approved -> revoked` is allowed at any time.
- `revoked` should force re-request before reuse.
- `expired` should be treated as dead state, not a soft approval.

## Safety Rules

- Never collapse request submission and durable approval into one opaque step.
- Never let Hermes become an unreviewed transport bridge.
- Never let a write-capable action surface masquerade as read-only.
- Log every request, approval, rejection, revocation, and execution result.
- Keep raw bootstrap secrets out of logs and UI text.
- Scope every approval to the minimum channel, instance, or action class needed.

## Transport Guidance

For live Windrose state pushes:

- Windrose should connect directly to ChannelCheevos over SignalR.
- WindrosePlus should execute runtime actions locally on the server side.
- Hermes should query or operate through ChannelCheevos, not proxy the Windrose connection.

This preserves a clean control plane:

- direct Windrose -> ChannelCheevos for approved state push and operator interaction
- direct ChannelCheevos -> WindrosePlus for approved execution semantics
- Hermes as an operator surface, not the transport bridge

## Open Questions

- Should chat and spawn use the same grant type, or separate grant types?
- Should approvals be scoped by action class, instance, or both?
- Should a granted action be one-shot or reusable until revoked?
- Which actions belong in WindrosePlus versus ChannelCheevos-mediated coordination?

## Recommendation

Treat this as the canonical operator contract draft for the live-mutation backlog.
Use it to keep the roadmap, the feature docs, and future implementation work aligned.