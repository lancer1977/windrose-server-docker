# Windrose V4 Candidate Inventory

## Purpose

This doc is the parking lot for Windrose V4-era quest, reward, event, chat, overlay, summary, and diagnostic ideas.
It separates observe-only surfaces from safe dev-mode mutations and approval-required actions so future work can stay narrow and auditable.

## Scope guardrails

- Do not duplicate the separate spawn, teleport, or stack-size safety cards.
- Do not treat any item here as production-ready live mutation.
- If a candidate would touch live player or world state, keep it approval-gated and dev-smoke only until a dedicated proof exists.
- Keep read-only observer work in Windrose State Web.
- Keep any live execution or write-capable behavior in WindrosePlus or another native-hook surface.

## Candidate matrix

| Feature type | Repo owner | Risk class | Smoke mode | Recommended next card |
|---|---|---|---|---|
| Admin diagnostics summary board | Windrose State Web | observe-only | local read-only smoke against `/api/runtime/control-surface`, `/api/plugin/status`, and dashboard summary endpoints | triage: `windrose-admin-diagnostics-summary` |
| Session summary mirroring / recap feed | Windrose State Web + ChannelCheevos | observe-only | read-only dev smoke; snapshot/replay only | triage: `windrose-session-summary-feed` |
| Achievement / event mirroring | ChannelCheevos | observe-only | fixture or event-replay smoke; no live mutation | triage: `windrose-achievement-event-mirror` |
| Overlay pulse / banner signals | ChannelCheevos + Windrose State Web | safe dev-mode mutation | approved dev throwaway smoke with rollback; overlay-only updates, no game-state writes | triage: `windrose-overlay-event-banners` |
| Structured reward-to-announce flow | ChannelCheevos + WindrosePlus | unsafe / approval-required | approved dev throwaway or consenting dev-player smoke; template-driven only | existing card: `windrose-channel-point-server-announcement` |
| Non-destructive quest prompt relay | ChannelCheevos + WindrosePlus | unsafe / approval-required | approved dev throwaway smoke; prompt-only, no inventory/world writes | triage: `windrose-quest-prompt-relay` |
| Temporary event-mode presets | Windrose State Web + WindrosePlus | safe dev-mode mutation | approved dev-only toggle with rollback evidence | triage: `windrose-event-mode-presets` |
| Operator-facing moderation / audit lanes for rewards | ChannelCheevos + Hermes | observe-only | read-only queue, review, and audit lookup smoke | triage: `windrose-reward-audit-lanes` |

## Why these are split this way

### Observe-only

These candidates only project, mirror, summarize, or classify existing information.
They should stay in the observer stack and should not require live mutation approval.

Typical examples:

- admin diagnostics
- session summaries
- achievement/event mirroring
- audit lanes

### Safe dev-mode mutation

These candidates may change a dev-only presentation surface or a reversible server mode, but should not change permanent game state.
They still need an approved smoke path and rollback evidence.

Typical examples:

- overlay pulses / banners
- temporary event-mode presets

### Unsafe / approval-required

These candidates can affect player-visible runtime behavior or could become true live mutation later.
They belong behind approval, rate limiting, and audit.

Typical examples:

- structured reward announcements
- non-destructive quest prompts
- any future broadcast / chat-like server message path

## Parked backlog recommendations

Hermes Kanban: `t_11ea00d0`

These are the five+ future functionality types to keep visible for later slicing. They are recommendations only; no implementation is approved by this parking pass.

| Priority | Candidate | Risk | Smoke mode | Repo owner | Recommended future card | Parking decision |
|---|---|---|---|---|---|---|
| 1 | Admin diagnostics summary board | observe-only | local read-only smoke against control-surface, plugin-status, and dashboard summary endpoints | Windrose State Web | `windrose-admin-diagnostics-summary-readonly` | Safe narrow `localscout`/`code_junior` docs+readback card when dashboard health needs a compact operator view. |
| 2 | Session summary mirroring / recap feed | observe-only | snapshot/replay smoke only; no live mutation | Windrose State Web + ChannelCheevos | `windrose-session-summary-replay-feed` | Good next planning card after diagnostics; keep as read-only projection from existing session/runtime data. |
| 3 | Achievement / event mirroring | observe-only | fixture or event-replay smoke; no live server writes | ChannelCheevos | `channel-cheevos-windrose-event-mirror-fixtures` | Worth a narrow `coder`/`code_junior` card only after fixture sources are named; no production event subscription changes in the first slice. |
| 4 | Operator-facing moderation / audit lanes for rewards | observe-only | read-only queue/review/audit lookup smoke | ChannelCheevos + Hermes | `channel-cheevos-windrose-reward-audit-lanes` | Safe as a parked design/test card; prefer queue/audit readback before any reward dispatch changes. |
| 5 | Overlay pulse / banner signals | safe dev-mode mutation | approved dev throwaway smoke with rollback; overlay-only updates, no game-state writes | ChannelCheevos + Windrose State Web | `windrose-overlay-event-banners-dev-smoke` | Park until overlay target and rollback evidence are explicit; do not touch game/player state. |
| 6 | Temporary event-mode presets | safe dev-mode mutation | approved dev-only toggle with rollback evidence | Windrose State Web + WindrosePlus | `windrose-event-mode-presets-dev-toggle` | Approval-gated dev card only; must define exact toggles, rollback, and no-main-server boundary. |
| 7 | Structured reward-to-announce flow | unsafe / approval-required | approved dev throwaway or consenting dev-player smoke; template-driven only | ChannelCheevos + WindrosePlus | existing lane: `windrose-channel-point-server-announcement` | Approval-required. Do not implement until the announcement contract, moderation gate, audit, and dev-only smoke target are explicitly approved. |
| 8 | Non-destructive quest prompt relay | unsafe / approval-required | approved dev throwaway smoke; prompt-only, no inventory/world writes | ChannelCheevos + WindrosePlus | `windrose-quest-prompt-relay-approved-dev-smoke` | Approval-required. Keep prompt-only and audit-heavy; any inventory/world/state write is out of scope for this candidate. |

Recommended release order:

1. Start with observe-only diagnostics, summaries, and event mirroring because they can validate value without player/world mutation.
2. Add audit lanes before any reward or prompt execution so unsafe ideas have a review trail first.
3. Only then consider overlay or event-mode dev toggles, and keep them dev-only with rollback proof.
4. Leave reward announcements and quest prompts blocked behind explicit approval; they are not safe to dispatch from this parked backlog.

Do not merge these ideas into the spawn/teleport/stack-size workstreams; those remain separate safety-gated cards.

## Inspected source files and cards

Files reviewed while building this inventory:

- `/home/lancer1977/code/windrose-server-docker/docs/roadmaps/windrose-runtime-control-surface/possibility-atlas.md`
- `/home/lancer1977/code/windrose-server-docker/docs/roadmaps/windrose-runtime-control-surface/execution-path.md`
- `/home/lancer1977/code/windrose-server-docker/docs/roadmaps/windrose-runtime-control-surface/operator-contract.md`
- `/home/lancer1977/code/windrose-server-docker/docs/roadmaps/windrose-runtime-control-surface/questions.md`
- `/home/lancer1977/code/windrose-server-docker/docs/roadmaps/windrose-runtime-control-surface/README.md`
- `/home/lancer1977/code/channel-cheevos/docs/features/game-harness/windrose-modding-guide.md`
- `/home/lancer1977/code/channel-cheevos/docs/features/windrose-plugin-sidecar-v3-contract.md`
- `/home/lancer1977/code/windrose-server-docker/00_agile/backlog/windrose-channel-point-server-announcement.md`
- `/home/lancer1977/code/windrose-server-docker/00_agile/backlog/windrose-native-plugin-docs-tests-sweep.md`

Related cards and validation anchors:

- `t_02b23bc0` — V3 typed sidecar contract completion
- `t_f02bebba` — operator-approved V3 contract review
- `t_77de9b18` — approved dev readiness / dry-run smoke evidence
- `t_971cd00d` — operator non-main-character smoke evidence
- `t_01e15094` — side-spawning native seam scout

## Related docs

- `docs/roadmaps/windrose-runtime-control-surface/possibility-atlas.md`
- `docs/roadmaps/windrose-runtime-control-surface/execution-path.md`
- `docs/roadmaps/windrose-runtime-control-surface/operator-contract.md`
- `docs/roadmaps/windrose-runtime-control-surface/questions.md`
- `docs/roadmaps/windrose-runtime-control-surface/README.md`
