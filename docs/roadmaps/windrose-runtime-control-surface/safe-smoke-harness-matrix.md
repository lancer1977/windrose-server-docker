# Windrose V2 Safe Smoke Harness Matrix

## Purpose

This matrix separates safe read-only smokes from any smoke that could mutate a live Windrose server.
It is the guidance surface for the V2 smoke harness only; it does not authorize live production testing.
State Web also exposes the same run-mode guidance as machine-readable readback at `GET /api/plugin/smoke-options` so operator clients and smoke scripts can discover the safe modes without scraping this document.

## Global rules

- Dev server only for any smoke that touches a live Windrose runtime or player state.
- Player-bound smokes default to a non-main / throwaway character.
- If a player is not clearly non-main/throwaway, treat the run as requiring explicit consent and a rollback plan.
- Random player testing is read-only only unless explicit consent exists.
- Any mutating smoke must capture logs, timestamps, the exact command/payload, and the rollback or revert path before it runs.
- If the target, consent, or environment is unclear, block instead of guessing.

## Smoke matrix

| Mode | Allowed target | Risk level | Prerequisites | Expected evidence | Block condition |
|---|---|---|---|---|---|
| Offline mock player | Local fixture, disposable harness, or mocked player object | Low | No live server, no player credentials, deterministic fixture data | Harness output, fixture snapshot, pass/fail result, no external side effects | Block if the step needs a real server, real credentials, or real player state |
| Dev server no-player | Dev server with no connected players | Low | Dev-mode server, read-only probes only, no mutation commands | Server/bridge status, manifest or health response, logs showing the read-only path, no state changes | Block if any step would mutate server state or if the server is not a dev server |
| Operator non-main character | Dev server, clearly named throwaway or non-main character | Medium | Dev-mode server, explicit target identity, rollback/log capture plan, operator acknowledgement that the character is not primary | Pre/post state, command log, rollback record, timestamps, and any audit trail for the action | Block if the target is ambiguous, the character is primary, or the run would touch prod/main |
| Consenting dev player | Dev server, explicitly consenting player account | Medium / High | Dev-mode server, written or otherwise recorded consent, timeboxed window, rollback/log capture plan, exact scope of the smoke | Consent record, pre/post state, logs, timestamps, and evidence that the action matched the agreed scope | Block if consent is missing, not recorded, or the run would affect a non-consenting player |
| Random online dev-player read-only probe | Any connected dev player, but read-only only | Low | Dev-mode server, read-only endpoint or observer path only, no mutation commands, no prompts requiring player action | Probe output, status response, observation log, and confirmation that no writes occurred | Block immediately if the probe would write, prompt, grant items, teleport, or otherwise mutate state |
| Sidecar / plugin-down failure | Dev stack or local harness with the plugin or sidecar intentionally disabled or unreachable | Low | Failure intentionally induced in a non-prod environment, no fallback write path, no live players affected | Graceful failure message, degraded-mode behavior, retry or timeout evidence, no crash or hang | Block if the harness auto-falls back to a mutating path or if the failure test is pointed at prod/main |

## Mutating smoke checklist

Any smoke that can change state must satisfy all of the following before it runs:

1. Dev server only.
2. Explicit target identity.
3. Non-main / throwaway warning recorded, or recorded player consent for the exact run.
4. Rollback or revert plan written down.
5. Log capture enabled for the exact command or payload.
6. Post-smoke verification defined before execution.

## Notes by mode

### Offline mock player
Use this for harness shape, request encoding, and regression coverage that does not need a live server.
It is the safest way to validate the harness contract before touching any dev environment.

### Dev server no-player
Use this for boot/liveness checks, plugin/sidecar availability, and read-only manifest or status inspection.
It should prove the surface exists without requiring a connected player.

### Operator non-main character
Use this when a live mutation is required but the effect should be contained to a throwaway identity.
The non-main warning should be explicit in the run notes.

### Consenting dev player
Use this only when the test requires a real player and the player has explicitly agreed to the exact action.
This mode still needs rollback/log capture because it may mutate save or session state.

### Random online dev-player read-only probe
Use this for observer-only validation against an active dev server.
Do not expand this mode into writes; if a mutation is needed, switch to a consenting or throwaway-target smoke instead.

### Sidecar / plugin-down failure
Use this to prove the harness fails safely when the bridge is unavailable.
The desired result is a clean failure, not a hidden fallback into mutation.

## References

- `docs/roadmaps/windrose-runtime-control-surface/README.md`
- `docs/roadmaps/windrose-runtime-control-surface/execution-path.md`
- `docs/roadmaps/windrose-runtime-control-surface/possibility-atlas.md`
- `plugins/windrose-sidecar-bridge/README.md`
- `scripts/smoke_windrose_sidecar_bridge.sh`
