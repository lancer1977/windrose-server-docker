# Windrose channel-point redemption contract v1

## Purpose
Define the first concrete request/decision record for a viewer channel-point redemption that asks Windrose to publish a server announcement.

This contract keeps the observer stack read-only, keeps the actual announcement execution in WindrosePlus or another write-capable/native-hook surface, and forces every redemption through an allowlisted template catalog instead of free-form message text.

## v1 contract summary
- Input is a redemption request from a channel-point provider.
- Output is a durable redemption record with an explicit lifecycle status.
- The request is deduplicated by an idempotency key.
- Announcement content is selected only by `templateId` plus typed template params.
- Human moderation and system rate limits can block or deny the request before execution.
- Every state transition must carry audit and moderation correlation metadata.
- This roadmap slice is read-only: it records the approval envelope and planned handoff target, but it does not perform live mutation.

## Allowed vs rejected matrix

| Input / gate result | Record status | Decision outcome | Reason code | Dispatch |
| --- | --- | --- | --- | --- |
| allowlisted template + typed params + moderation/rate-limit pass | `pending_approval` | `approve` | `approved` | no live mutation in this slice |
| missing template, unknown template, invalid params, freeform payload, duplicate/stale replay, or hard policy block | `blocked` | `reject` or `block` | matching reject code | no dispatch |
| moderation denied | `denied` | `reject` | `moderation_blocked` | no dispatch |
| rate limit exceeded | `blocked` | `block` | `rate_limit_exceeded` | no dispatch |

## Read-only handoff target

The write-capable announcement surface is intentionally treated as a future handoff target only.

- `nextDispatchTarget`: `WindrosePlus` or `native-hook`
- In this slice the field is advisory only and must not trigger dispatch.
- Any approved record remains `pending_approval` until a later write-capable slice owns execution.

## Normative record shape

```json
{
  "schemaVersion": "windrose.channel_point_redemption.v1",
  "eventId": "01J3N7K3Y0W6S2Q9J1V3H8K2DM",
  "idempotencyKey": "twitch:123456789:redemption:aa9f6f5b-8db8-4c2d-a5dd-83be7f6c0c50",
  "source": {
    "provider": "twitch",
    "channelId": "123456789",
    "channelLogin": "windrose_tv",
    "rewardId": "reward_announce_server",
    "rewardTitle": "Request a server announcement",
    "redemptionId": "aa9f6f5b-8db8-4c2d-a5dd-83be7f6c0c50",
    "redeemedBy": {
      "userId": "99887766",
      "userLogin": "viewername",
      "displayName": "Viewer Name"
    },
    "redeemedAt": "2026-05-30T17:21:24Z"
  },
  "template": {
    "templateId": "windrose.server_announcement.status.v1",
    "params": {
      "serverName": "Windrose-2",
      "status": "maintenance",
      "etaMinutes": 15,
      "audience": "all",
      "tone": "urgent"
    }
  },
  "status": "pending_approval",
  "gate": {
    "moderationRequired": true,
    "rateLimitKey": "twitch:123456789:reward_announce_server",
    "rateLimitWindowSeconds": 300,
    "rateLimitLimit": 1
  },
  "metadata": {
    "correlationId": "cpred:01J3N7K3Y0W6S2Q9J1V3H8K2DM",
    "requestId": "req_6d1b1c5f3b",
    "moderationCaseId": "mod_20260530_172124_001",
    "auditTags": ["channel-point", "server-announcement", "windroseplus"],
    "originSurface": "ChannelCheevos",
    "targetSurface": "WindrosePlus"
  },
  "nextDispatchTarget": "WindrosePlus",
  "audit": {
    "actor": {
      "type": "viewer",
      "id": "99887766",
      "displayName": "Viewer Name"
    },
    "decisionBy": null,
    "decisionAt": null,
    "decisionReason": null,
    "executedAt": null,
    "executionId": null,
    "executionOutcome": null
  }
}
```

## Required fields and types

### Top-level
- `schemaVersion` (string, required): fixed contract version, currently `windrose.channel_point_redemption.v1`.
- `eventId` (string, required): unique record identifier, ULID or UUID preferred.
- `idempotencyKey` (string, required): dedupe key for the redemption request.
- `source` (object, required): provider and redeemer identity.
- `template` (object, required): allowlisted template selection and typed params.
- `status` (string, required): lifecycle state.
- `gate` (object, required): policy/rate-limit gate metadata.
- `metadata` (object, required): moderation and audit correlation fields.
- `nextDispatchTarget` (string, optional): advisory future handoff target; one of `WindrosePlus` or `native-hook`.
- `audit` (object, required): decision and execution trace.

### Source object
- `provider` (string, required): `twitch` for v1.
- `channelId` (string, required)
- `channelLogin` (string, required)
- `rewardId` (string, required)
- `rewardTitle` (string, required)
- `redemptionId` (string, required)
- `redeemedBy.userId` (string, required)
- `redeemedBy.userLogin` (string, required)
- `redeemedBy.displayName` (string, required)
- `redeemedAt` (string, ISO-8601 timestamp, required)

### Template object
- `templateId` (string, required): must match an allowlisted template ID.
- `params` (object, required): template-specific typed params.
- The `params` object MUST NOT contain arbitrary message text fields such as `message`, `body`, `text`, `raw`, or `markdown`.
- The executor derives the actual announcement copy from `templateId` plus validated params only.

### Gate object
- `moderationRequired` (boolean, required)
- `rateLimitKey` (string, required)
- `rateLimitWindowSeconds` (integer, required)
- `rateLimitLimit` (integer, required)
- Optional rate-limit result fields when blocked:
  - `remaining` (integer)
  - `retryAfterSeconds` (integer)

### Metadata object
- `correlationId` (string, required): moderation/audit join key.
- `requestId` (string, required): internal request identifier.
- `moderationCaseId` (string, optional): operator review ticket or queue id.
- `auditTags` (array of string, required): compact labels for reporting and search.
- `originSurface` (string, required): usually `ChannelCheevos`.
- `targetSurface` (string, required): usually `WindrosePlus`.

### Audit object
- `actor` (object, required): who triggered the redemption.
- `decisionBy` (object or null, required): moderator/operator identity if a human made a decision.
- `decisionAt` (string or null, required)
- `decisionReason` (string or null, required)
- `executedAt` (string or null, required)
- `executionId` (string or null, required)
- `executionOutcome` (string or null, required)

## Idempotency key
Recommended v1 format:

```text
<provider>:<channelId>:redemption:<redemptionId>
```

Examples:
- `twitch:123456789:redemption:aa9f6f5b-8db8-4c2d-a5dd-83be7f6c0c50`
- `twitch:123456789:redemption:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee`

Behavior:
- A duplicate idempotency key must return the existing record instead of creating a second redemption.
- If the provider retries the same redemption, the contract treats it as the same business event.
- If a duplicate arrives after completion, the existing terminal status is returned with the original audit trail.

## Allowlisted template catalog

### Template: `windrose.server_announcement.status.v1`
Purpose: structured server status announcement with no free-form message body.

Typed params:
- `serverName` (string, required, 1..64 chars)
- `status` (string enum, required): `online`, `restarting`, `maintenance`, `offline`
- `etaMinutes` (integer, optional, 0..240)
- `audience` (string enum, required): `all`, `players`, `admins`
- `tone` (string enum, required): `neutral`, `friendly`, `urgent`

Derived output example:
- `Windrose-2 is going into maintenance for about 15 minutes.`

Template allowlist rules:
- Only IDs explicitly listed in the catalog may be accepted.
- New template IDs require a schema addition and a docs update before use.
- A rejected `templateId` must be reported as `unknown_template`.

## Status lifecycle

The v1 contract uses one canonical lifecycle with clear terminal states:

- `received`: the redemption arrived but has not been validated yet.
- `pending_approval`: the record passed validation and awaits moderator or policy approval.
- `approved`: the request was approved and may be dispatched to the execution surface.
- `denied`: a human moderator or policy gate rejected the request.
- `blocked`: the request was rejected by a hard gate before approval.
- `executed`: the announcement was successfully dispatched.
- `failed`: execution was attempted but did not succeed.
- `expired`: the redemption timed out before approval or execution.

State transition rules:
- `received -> pending_approval` only after schema, template, and rate-limit checks pass.
- `received -> blocked` for invalid schema, duplicate idempotency key, unknown template, invalid params, or hard policy/rate-limit failure.
- `pending_approval -> approved` when a moderator or policy engine authorizes the request.
- `pending_approval -> denied` when a moderator explicitly rejects it.
- `pending_approval -> blocked` when a hard policy gate flips after intake.
- `approved -> executed` once the WindrosePlus/native-hook path confirms the announcement in a later write-capable slice.
- `approved -> failed` when the write-capable surface is unavailable or errors out.
- Any non-terminal pending state may become `expired` when the approval window closes.

Terminal states:
- `denied`, `blocked`, `executed`, `failed`, `expired`.
- A terminal record remains immutable except for audit enrichment fields that only append evidence.

## Gate behavior

### Validation gate
Reject as `blocked` with reason `invalid_schema` when the envelope is malformed.
Reject as `blocked` with reason `unknown_template` when `templateId` is not on the allowlist.
Reject as `blocked` with reason `template_params_invalid` when required template params are missing or out of bounds.

### Rate-limit gate
Rate limiting is applied before approval so that spam never reaches review queues.
Recommended v1 policy:
- key: `channelId + rewardId + templateId`
- limit: 1 request per 300 seconds per key

If the gate trips:
- status becomes `blocked`
- failure reason is `rate_limited`
- `gate.remaining` and `gate.retryAfterSeconds` should be populated when available
- no execution request is sent downstream

### Moderation gate
If `moderationRequired = true`, the record stays `pending_approval` until a human or policy engine decides.

If moderation rejects:
- status becomes `denied`
- failure reason is `moderation_denied`
- `audit.decisionBy`, `audit.decisionAt`, and `audit.decisionReason` must be filled in
- no execution request is sent downstream

### Execution gate
Only `approved` requests may reach the write-capable announcement surface.
If execution fails:
- status becomes `failed`
- failure reason is `execution_unavailable` or `execution_failed`
- `audit.executedAt`, `audit.executionId`, and `audit.executionOutcome` should record the attempted dispatch and result

## Failure reason enum
Recommended v1 reasons:
- `invalid_schema`
- `duplicate_idempotency_key`
- `unknown_template`
- `template_params_invalid`
- `rate_limited`
- `policy_blocked`
- `moderation_denied`
- `approval_timeout`
- `execution_unavailable`
- `execution_failed`
- `expired`

Recommended mapping:
- `blocked`: `invalid_schema`, `duplicate_idempotency_key`, `unknown_template`, `template_params_invalid`, `rate_limited`, `policy_blocked`
- `denied`: `moderation_denied`
- `failed`: `execution_unavailable`, `execution_failed`
- `expired`: `expired`, `approval_timeout`

## Success example

```json
{
  "schemaVersion": "windrose.channel_point_redemption.v1",
  "eventId": "01J3N7K3Y0W6S2Q9J1V3H8K2DM",
  "idempotencyKey": "twitch:123456789:redemption:aa9f6f5b-8db8-4c2d-a5dd-83be7f6c0c50",
  "source": {
    "provider": "twitch",
    "channelId": "123456789",
    "channelLogin": "windrose_tv",
    "rewardId": "reward_announce_server",
    "rewardTitle": "Request a server announcement",
    "redemptionId": "aa9f6f5b-8db8-4c2d-a5dd-83be7f6c0c50",
    "redeemedBy": {
      "userId": "99887766",
      "userLogin": "viewername",
      "displayName": "Viewer Name"
    },
    "redeemedAt": "2026-05-30T17:21:24Z"
  },
  "template": {
    "templateId": "windrose.server_announcement.status.v1",
    "params": {
      "serverName": "Windrose-2",
      "status": "maintenance",
      "etaMinutes": 15,
      "audience": "all",
      "tone": "urgent"
    }
  },
  "status": "pending_approval",
  "gate": {
    "moderationRequired": true,
    "rateLimitKey": "twitch:123456789:reward_announce_server:windrose.server_announcement.status.v1",
    "rateLimitWindowSeconds": 300,
    "rateLimitLimit": 1,
    "remaining": 0
  },
  "metadata": {
    "correlationId": "cpred:01J3N7K3Y0W6S2Q9J1V3H8K2DM",
    "requestId": "req_6d1b1c5f3b",
    "moderationCaseId": "mod_20260530_172124_001",
    "auditTags": ["channel-point", "server-announcement", "windroseplus"],
    "originSurface": "ChannelCheevos",
    "targetSurface": "WindrosePlus"
  },
  "nextDispatchTarget": "WindrosePlus",
  "audit": {
    "actor": {
      "type": "viewer",
      "id": "99887766",
      "displayName": "Viewer Name"
    },
    "decisionBy": {
      "type": "moderator",
      "id": "mod_42",
      "displayName": "Stream Moderator"
    },
    "decisionAt": "2026-05-30T17:21:38Z",
    "decisionReason": "Approved: short maintenance notice",
    "executedAt": null,
    "executionId": null,
    "executionOutcome": "not_dispatched"
  }
}
```

## Rejected example

```json
{
  "schemaVersion": "windrose.channel_point_redemption.v1",
  "eventId": "01J3N7K8Q9B1V2S3T4U5W6X7Y8",
  "idempotencyKey": "twitch:123456789:redemption:2c93b5d7-95ff-4c2f-9f61-0114b1e3d1a8",
  "source": {
    "provider": "twitch",
    "channelId": "123456789",
    "channelLogin": "windrose_tv",
    "rewardId": "reward_announce_server",
    "rewardTitle": "Request a server announcement",
    "redemptionId": "2c93b5d7-95ff-4c2f-9f61-0114b1e3d1a8",
    "redeemedBy": {
      "userId": "99887766",
      "userLogin": "viewername",
      "displayName": "Viewer Name"
    },
    "redeemedAt": "2026-05-30T17:24:11Z"
  },
  "template": {
    "templateId": "windrose.server_announcement.freeform.v1",
    "params": {
      "message": "please tell everyone I am the best"
    }
  },
  "status": "blocked",
  "gate": {
    "moderationRequired": true,
    "rateLimitKey": "twitch:123456789:reward_announce_server:windrose.server_announcement.freeform.v1",
    "rateLimitWindowSeconds": 300,
    "rateLimitLimit": 1,
    "retryAfterSeconds": 300
  },
  "metadata": {
    "correlationId": "cpred:01J3N7K8Q9B1V2S3T4U5W6X7Y8",
    "requestId": "req_b8a1ef0c22",
    "moderationCaseId": null,
    "auditTags": ["channel-point", "rejected", "template-policy"],
    "originSurface": "ChannelCheevos",
    "targetSurface": "WindrosePlus"
  },
  "nextDispatchTarget": "WindrosePlus",
  "audit": {
    "actor": {
      "type": "viewer",
      "id": "99887766",
      "displayName": "Viewer Name"
    },
    "decisionBy": {
      "type": "policy",
      "id": "template-allowlist",
      "displayName": "Template Allowlist"
    },
    "decisionAt": "2026-05-30T17:24:12Z",
    "decisionReason": "Blocked: templateId is not allowlisted and free-form message text is disallowed",
    "executedAt": null,
    "executionId": null,
    "executionOutcome": "not_dispatched"
  }
}
```

## Notes for the next implementation slice
- If a future template needs more expressive text, add a new allowlisted template ID with a typed params schema instead of adding a raw `message` field.
- Keep moderation and audit metadata small but durable so the same record can be joined to logs, operator review, and replay tooling.
- Treat this document as the v1 contract for ChannelCheevos redemption intake and approval; do not broaden it into general chat mutation.
