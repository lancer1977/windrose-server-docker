# Windrose ↔ ChannelCheevos integration test design

## Purpose

Draft the first end-to-end integration tests that prove ChannelCheevos owns redemption intake, approval, rate limiting, moderation, and audit logging while Windrose State Web stays read-only.

This slice is intentionally centered on the redemption path rather than generic chat snapshots:

1. Channel-point redemption intake and normalization
2. Moderation / approval / denial / block behavior
3. Approved dispatch into WindrosePlus or another write-capable hook surface
4. Audit lookup and read-only surface verification

## Canonical record under test

The integration suite should treat `windrose.channel_point_redemption.v1` as the single canonical event shape.

Required top-level fields:

- `schemaVersion`
- `eventId`
- `idempotencyKey`
- `source`
- `template`
- `status`
- `gate`
- `metadata`
- `audit`

Template contract for v1:

- only allowlisted template IDs are accepted
- v1 currently allows `windrose.server_announcement.status.v1`
- no free-form `message`, `body`, `text`, `raw`, or `markdown` field is allowed in the request

Typed template params for the allowed v1 template:

- `serverName`
- `status`
- `etaMinutes`
- `audience`
- `tone`

## Current surface inventory

### Windrose side

- Windrose State Web remains read-only.
- `GET /api/runtime/control-surface` is the summary-only boundary report.
- WindrosePlus or another write-capable/native-hook surface is the only place an approved live action may execute.

### ChannelCheevos side

- The redemption intake path owns normalization, dedupe, rate limiting, moderation, and audit.
- `channelcheevos.event.fire` may continue to exist for general allowlisted events, but it is not the canonical redemption flow.
- The canonical redemption record should be persisted in ChannelCheevos and exposed to operator review tooling.

## Initial integration design

### Lane 1: Redemption intake and template validation

Goal: prove that ChannelCheevos can accept a channel-point redemption, normalize it into `windrose.channel_point_redemption.v1`, and reject anything that is not on the template allowlist.

Suggested first tests:

- a valid redemption becomes a canonical v1 record with `received` or `pending_approval`
- an unknown `templateId` is rejected as `blocked`
- a request with free-form message text is rejected as `blocked`
- duplicate idempotency keys return the existing record instead of creating a second redemption

### Lane 2: Rate limit and moderation gate

Goal: prove that ChannelCheevos owns the gate before WindrosePlus ever sees an approved action.

Suggested first tests:

- a second redemption inside the rate-limit window is blocked before moderation
- a moderation-required redemption stays `pending_approval` until a decision exists
- a moderator approval moves the record to `approved`
- a moderator rejection moves the record to `denied`
- hard policy failures move the record to `blocked`

### Lane 3: Approved execution

Goal: prove that only approved redemptions reach the write-capable surface.

Suggested first tests:

- an approved record dispatches to the WindrosePlus announcement executor
- the execution result is written back with `executedAt`, `executionId`, and `executionOutcome`
- execution failure transitions the record to `failed`
- no Windrose State Web endpoint accepts a write-capable mutation request

### Lane 4: Audit lookup and operator review

Goal: prove that the audit trail is durable and readable without turning State Web into a write path.

Suggested first tests:

- the audit store exposes the canonical record fields needed by the operator review UI
- the read-only lookup surface returns `eventId`, `status`, `source`, `template`, `gate`, `metadata`, and `audit`
- `GET /api/runtime/control-surface` remains summary-only and does not expose a mutation endpoint
- the review view can distinguish `blocked`, `denied`, `approved`, `executed`, `failed`, and `expired`

## End-to-end test matrix

### E2E-1: Valid redemption intake

Given:
- a Twitch channel-point redemption with a valid allowlisted template ID
- a known idempotency key

When:
- ChannelCheevos receives the redemption

Then:
- the redemption is normalized to `windrose.channel_point_redemption.v1`
- the canonical record is persisted
- the status is `received` or `pending_approval`
- the template params are typed and contain no free-form message field

### E2E-2: Duplicate and rate-limited redemption

Given:
- a previously seen idempotency key, or a redemption inside the rate-limit window

When:
- ChannelCheevos receives the request

Then:
- the existing record is returned, or the new request is blocked
- no execution request reaches WindrosePlus
- the audit trail records the dedupe or rate-limit reason

### E2E-3: Moderation approval path

Given:
- a pending redemption
- a moderator approval

When:
- ChannelCheevos applies the decision

Then:
- the record moves to `approved`
- the approved record is dispatched to the WindrosePlus executor
- the execution metadata is persisted back into the audit record

### E2E-4: Moderation denial and blocked path

Given:
- a pending redemption, or a request that fails a hard gate

When:
- ChannelCheevos applies moderation or policy validation

Then:
- the record becomes `denied` or `blocked`
- the reason is persisted in the audit trail
- nothing is sent to the write-capable execution surface

### E2E-5: Execution failure path

Given:
- an approved redemption
- a failing or unavailable write-capable hook surface

When:
- ChannelCheevos dispatches the action

Then:
- the record becomes `failed`
- the audit record captures the execution ID and failure outcome
- the operator review history still contains the original canonical record

### E2E-6: Read-only state surface

Given:
- the live Windrose control-surface summary endpoint

When:
- the integration harness queries the State Web surface

Then:
- the response stays read-only and summary-only
- no redemption intake or execution path appears on the State Web surface

## Recommended test harness shape

- ChannelCheevos-side tests should use in-memory/stub persistence for the redemption store and moderation queue.
- WindrosePlus-side tests should stub the announcement executor and assert only approved records can reach it.
- Windrose State Web tests should verify the summary endpoint remains read-only.
- End-to-end tests should verify the contract boundary, not just command success.
- The assertions should cover:
  - template ID
  - template params
  - idempotency key
  - status transition
  - audit record fields
  - execution metadata
  - read-only boundary behavior

## Completion gate for the first slice

- A valid redemption can be normalized into the canonical v1 record.
- At least one approval path dispatches into WindrosePlus or another write-capable hook surface.
- At least one rejection path records `blocked` or `denied` with audit data.
- The read-only control-surface summary remains read-only.
- All payloads use `windrose.channel_point_redemption.v1` and the allowlisted template catalog.
