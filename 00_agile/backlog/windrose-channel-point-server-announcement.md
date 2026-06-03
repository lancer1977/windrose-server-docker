# Windrose/ChannelCheevos: channel-point server announcement redemption

## Board
- Hermes board: `default`
- Discovery card: `t_b5779706` (`triage`)
- Implementation slice card: `t_5375b0f3` (`ready`)

## Source truth
- `docs/roadmaps/windrose-runtime-control-surface/operator-contract.md`
- `docs/roadmaps/windrose-runtime-control-surface/questions.md`
- `docs/roadmaps/windrose-runtime-control-surface/channel-cheevos-integration-tests.md`

## Intent
Use a viewer channel-point redemption to request a Windrose server announcement.

## Boundary
- Keep `Windrose State Web` read-only.
- Route execution through `WindrosePlus` or another write-capable/native hook surface.
- Start with structured announcement templates and allowlisting, not free-form chat injection.
- ChannelCheevos owns redemption handling, approval/rate-limiting, and audit.

## Acceptance criteria
- Decide the first redemption/event shape and message template contract.
- Document success/failure/audit behavior and moderation/rate-limit gates.
- Add one focused test or probe proving the redemption reaches the write-capable announcement path while the observer surface stays read-only.
- Record the chosen path in roadmap/docs so the next implementation slice is unambiguous.

## Current repo note
- Contract draft: `docs/roadmaps/windrose-runtime-control-surface/channel-point-redemption-contract.md`
- Operator-contract link updated to point at the v1 redemption intake record.
