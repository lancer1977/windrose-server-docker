# Windrose Goal Prompt Pack

Use these when you want a fresh agent to continue Windrose work without losing the repo’s architecture boundary or the current docs-first source of truth.

## 1) Finish the remaining Windrose work

Goal: Finish all remaining Windrose mod/server work in a disciplined, repo-scoped way. Treat the Windrose docs and roadmap as the source of truth, and close out the remaining work only when it is actually verified.

Source of truth:
- `README.md`
- `docs/README.md`
- `docs/features/server-state-observability/README.md`
- `docs/features/server-state-observability/roadmap.md`
- `docs/features/server-state-observability/architecture.md`
- `docs/roadmaps/companion-state-webserver/README.md`
- `docs/roadmaps/companion-state-webserver/deployment.md`
- `docs/roadmaps/companion-state-webserver/remaining-work.md`
- `docs/roadmaps/companion-state-webserver/browser-source-handoff.md`
- `docs/plans/2026-05-27-windrose-live-deployment-validation.md`

Boundaries:
- Keep Windrose read-only, summary-oriented, and operator-facing.
- Leave browser-source and Twitch-style mutation to `cc-sidecar` / `channel-cheevos` unless the docs explicitly move that ownership.
- Do not widen the trust boundary unless the live behavior proves it is safe.

Sequence:
1. Validate the live receiver / push contract against the documented handoff.
2. Finish deployment hardening and any missing operational guidance.
3. Decide whether deeper save decoding is truly worth shipping, and keep it summary-only unless proof exists.
4. Close out any remaining overlay or operator polish.
5. Sync docs, acceptance criteria, and reality in the same pass.

Acceptance criteria:
- Live behavior and docs describe the same surfaces.
- Any deferred work is explicitly labeled and justified.
- Any drift is recorded in the docs before the slice is considered done.
- The final answer includes: what changed, what remains, what was verified, and the next slice.

## 2) Validate the live deployment contract

Goal: Validate the current Windrose state-web deployment against the documented read-only browser/source handoff without widening the trust boundary or mutating live OBS/browser-source surfaces.

Source of truth:
- `docs/roadmaps/companion-state-webserver/deployment.md`
- `docs/roadmaps/companion-state-webserver/browser-source-handoff.md`
- `docs/roadmaps/companion-state-webserver/remaining-work.md`

Sequence:
1. Re-read the live deployment notes and handoff docs.
2. Validate the safe read-only surfaces first: `/health`, `/map`, `/api/world/summary`, `/api/overlay/summary`, `/api/saves/latest`.
3. Compare live responses to the documented contract.
4. Record drift in the deployment notes if anything differs.
5. Leave OBS/browser-source mutation to the downstream consumer stack.

Acceptance criteria:
- The live deployment matches the documented read-only surface shape.
- The ownership split remains Windrose-read-only vs consumer-side mutation.
- Any drift is captured before the slice is closed.

## 3) Decide whether deeper save decoding is worth shipping

Goal: Determine whether the checkpoint data can be decoded safely enough to be productized, and stop at summary-only if the evidence is still weak.

Source of truth:
- `docs/features/server-state-observability/README.md`
- `docs/features/server-state-observability/architecture.md`
- `docs/roadmaps/companion-state-webserver/remaining-work.md`
- `docs/roadmaps/companion-state-webserver/phases.md`

Sequence:
1. Inspect the latest checkpoint ZIP and document what is actually present.
2. Separate observed families, metadata, and true decoded documents.
3. Prove the payload format before claiming player, ship, or actor document decoding.
4. Keep the save reader summary-only unless a real serialization proof exists.
5. Update the docs so they say exactly what is known and what is not.

Acceptance criteria:
- The docs distinguish proof from inference.
- Any deep-decoding claim is backed by concrete evidence.
- The safe boundary stays explicit.

## 4) Polish the operator surfaces

Goal: Close the remaining operator-facing rough edges without broadening the trust boundary.

Source of truth:
- `docs/features/server-state-observability/checklist.md`
- `docs/features/server-state-observability/implementation-notes.md`
- `docs/roadmaps/public-exposure-readiness/README.md`
- `docs/roadmaps/public-exposure-readiness/phases.md`

Sequence:
1. Identify the highest-signal operator surfaces that still need polish.
2. Tighten docs, labels, and validation notes before reaching for code changes.
3. Keep public exposure guidance conservative and explicit.
4. Update the supporting docs in the same pass.

Acceptance criteria:
- The operator workflow is clearer than before.
- No new write paths or trust-boundary expansions were introduced.
- The docs make the remaining risks and non-goals obvious.

## 5) Sequence cc-sidecar, channel-cheevos, and plugin work in order

Goal: Drive the downstream consumer work in dependency order so each repo receives a stable contract instead of inventing its own shape. Start from Windrose’s shared contract package, then update the consumer chain one layer at a time.

Source of truth:
- `src/Windrose.StateWeb.Core/README.md`
- `src/Windrose.StateWeb.Core/Windrose.StateWeb.Core.csproj`
- `src/Windrose.StateWeb.Core/Contracts/WindroseOverlaySnapshot.cs`
- `src/Windrose.StateWeb.Core/Contracts/WindroseTimelineEntry.cs`
- `src/Windrose.StateWeb.Core/Contracts/WindroseTimeSeriesExport.cs`
- `docs/roadmaps/companion-state-webserver/browser-source-handoff.md`
- `docs/roadmaps/companion-state-webserver/remaining-work.md`
- `docs/roadmaps/windrose-runtime-control-surface/operator-contract.md`
- `docs/roadmaps/public-exposure-readiness/README.md`

Boundary rules:
- Treat `Windrose.StateWeb.Core` as the canonical Windrose data-shape package for downstream consumers.
- Do not let `cc-sidecar` or any plugin infer new payload shapes that are not already documented or exported by Windrose.
- Keep Windrose read-only and summary-oriented; any mutation or approval path stays in the owning consumer stack.
- If a downstream consumer needs a different shape, add or revise the Windrose contract first, then consume it.

Sequence:
1. Lock the Windrose contract shape first. Verify the reusable package exposes the fields downstream consumers actually need.
2. Update `channel-cheevos` next so it consumes the Windrose contract instead of reconstructing payload shapes locally.
3. Update `cc-sidecar` after the contract is stable so it renders only the contract fields and never guesses at missing data.
4. Finish the plugin work last, using the same contract shape and the already-established ownership split.
5. Update docs in every repo touched so the contract, ownership, and remaining non-goals stay aligned.

Acceptance criteria:
- Each repo works against the same documented shape of data.
- `channel-cheevos` is the consumer/normalizer, not the place where the Windrose contract is invented.
- `cc-sidecar` renders from contract fields only.
- Plugin work lands after the contract and consumer surfaces are stable.
- The final handoff clearly states what Windrose exports, what each consumer owns, and what remains out of scope.
