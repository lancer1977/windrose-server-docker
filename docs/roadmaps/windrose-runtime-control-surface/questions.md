# Windrose Runtime Control Surface Questions

## Resolved for v1

These decisions are now locked into the roadmap docs and the redemption contract:

- ChannelCheevos owns redemption intake, approval, rate limiting, the moderation gate, and audit logging.
- Windrose State Web remains read-only and summary-only.
- WindrosePlus or another write-capable/native-hook surface performs the actual approved execution.
- The canonical redemption shape is `windrose.channel_point_redemption.v1`.
- v1 is template-only; free-form message text is not part of the contract.
- The only allowed template ID in v1 is `windrose.server_announcement.status.v1`.

## Remaining implementation questions

- Should the moderation queue live inside the existing ChannelCheevos event workflow, or should it be a dedicated redemption inbox UI?
- Should the read-only audit lookup expose the full canonical record by default, or a compact summary with a separate detail view?
- Should WindrosePlus expose one generic server-announcement executor adapter for v1, or a small set of hook-specific adapters behind the same contract?
- Should the operator review flow treat `blocked` and `denied` as separate inbox lanes, or keep them in one review history with status filters?

## Notes

The next implementation slice should follow the resolved boundary above and only spend time on the unanswered component and UI details.
