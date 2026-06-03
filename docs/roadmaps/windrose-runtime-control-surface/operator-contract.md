# Windrose Operator Contract Draft

## Purpose

Define the safe control-plane boundary for live Windrose actions and the one canonical redemption contract used for v1 approval flows.

This draft separates four concerns:

- Windrose State Web: read-only observation, summaries, and live status.
- ChannelCheevos: redemption intake, approval, rate limiting, moderation gate, and audit logging.
- WindrosePlus or another write-capable/native-hook surface: the actual execution path for approved actions.
- Hermes: operator surface only; it may request and review actions, but it must not become the transport bridge.

## Ownership model

### Windrose State Web

Keeps the observer layer read-only.
It can expose:

- server state
- player state
- saves and diagnostics
- live updates for overlays and dashboards
- a summary of the live control-surface posture

It should not accept redemption intake, approval, or write-capable execution requests.

### ChannelCheevos

Owns the redemption workflow end to end:

- intake and normalization
- idempotency and deduplication
- rate limiting
- moderation or policy gating
- approval / denial / block decisions
- audit persistence and operator review records

ChannelCheevos is the source of truth for whether a redemption is pending, approved, denied, blocked, expired, or executed.

### WindrosePlus / write-capable hook surface

Owns execution only.
If a redemption is approved, ChannelCheevos sends it to WindrosePlus or another write-capable/native-hook surface for the actual announcement or other server-side action.

Approved execution is allowed to happen only after the canonical approval record exists.

### Hermes

Acts as an operator client.
It may request actions, show queue state, and display approvals or rejections.
It should not sit between ChannelCheevos and the execution surface.

## Canonical redemption event shape

The canonical v1 record shape is `windrose.channel_point_redemption.v1`.
Use `docs/roadmaps/windrose-runtime-control-surface/channel-point-redemption-contract.md` as the normative contract for the full field list.

The important top-level fields are:

- `schemaVersion`
- `eventId`
- `idempotencyKey`
- `source`
- `template`
- `status`
- `gate`
- `metadata`
- `audit`

### Source fields

The source block captures the provider and viewer identity:

- `provider`
- `channelId`
- `channelLogin`
- `rewardId`
- `rewardTitle`
- `redemptionId`
- `redeemedBy.userId`
- `redeemedBy.userLogin`
- `redeemedBy.displayName`
- `redeemedAt`

### Template contract

v1 is template-only.
Announcement content is selected by `templateId` plus typed params.
There is no free-form `message`, `body`, `text`, `raw`, or `markdown` field in the canonical request.

Allowed template IDs for v1:

- `windrose.server_announcement.status.v1`

That template accepts typed params such as:

- `serverName`
- `status`
- `etaMinutes`
- `audience`
- `tone`

New template IDs require a schema update and docs update before use.

## Status and behavior

The canonical lifecycle is:

- `received`
- `pending_approval`
- `approved`
- `denied`
- `blocked`
- `executed`
- `failed`
- `expired`

### Success behavior

1. ChannelCheevos receives the redemption and records `received`.
2. It validates the schema, idempotency key, template ID, params, and rate limit.
3. If moderation is required, the record becomes `pending_approval`.
4. A moderator or policy engine can move the record to `approved`.
5. ChannelCheevos dispatches the approved action to WindrosePlus or another write-capable hook surface.
6. The execution result is recorded as `executed` with audit data attached.

Success audit fields should include:

- `audit.actor`
- `audit.decisionBy`
- `audit.decisionAt`
- `audit.decisionReason`
- `audit.executedAt`
- `audit.executionId`
- `audit.executionOutcome`

### Failure behavior

- `blocked` is used for invalid schema, duplicate idempotency key, unknown template, invalid params, rate limiting, or hard policy failure.
- `denied` is used for explicit moderation rejection.
- `failed` is used when the write-capable surface is unavailable or returns an error after approval.
- `expired` is used when the approval window closes before an action is approved or executed.

Each failure must retain the canonical record and its audit trail.

## Audit output fields and lookup surfaces

The durable audit record belongs in ChannelCheevos, not in Windrose State Web.
State Web stays summary-only and can report the coarse posture of the surface, but it should not become the redemption audit store.

Audit data should expose these fields for review and replay tooling:

- `eventId`
- `idempotencyKey`
- `status`
- `source`
- `template`
- `gate`
- `metadata.correlationId`
- `metadata.requestId`
- `metadata.moderationCaseId`
- `metadata.auditTags`
- `audit.actor`
- `audit.decisionBy`
- `audit.decisionAt`
- `audit.decisionReason`
- `audit.executedAt`
- `audit.executionId`
- `audit.executionOutcome`

Planned or expected read-only surfaces:

- ChannelCheevos operator review queue for pending/denied/blocked/executed redemptions
- a ChannelCheevos read-only lookup endpoint such as `GET /api/channel-point/redemptions/{eventId}`
- Windrose State Web `GET /api/runtime/control-surface` for a summary-only posture report

## Proposed component responsibilities for the next implementation slice

### `ChannelCheevos.RedemptionIntake`

Owns:

- intake of the channel-point redemption
- idempotency lookup
- schema validation
- template allowlist validation
- rate limiting
- creation of the canonical record

### `ChannelCheevos.RedemptionModeration`

Owns:

- queueing `pending_approval`
- applying human or policy decisions
- recording `approved`, `denied`, `blocked`, and `expired` transitions
- linking moderation metadata into the audit record

### `ChannelCheevos.RedemptionAuditStore`

Owns:

- durable persistence of the canonical record
- operator lookup and replay-friendly reads
- the record shape used by logs, review UI, and any future audit endpoint

### `WindrosePlus.ServerAnnouncementExecutor`

Owns:

- the approved write-capable dispatch path
- translation from the approved redemption record into the actual announcement or hook call
- recording the execution ID and execution outcome back into the audit record

### `Windrose.StateWeb.RuntimeControlSurfaceController`

Owns:

- read-only boundary reporting
- surface status summaries
- no redemption intake and no execution

## Recommendation

Treat this as the canonical operator contract draft for the live-mutation backlog.
Use it to keep the roadmap, the feature docs, and future implementation work aligned.
