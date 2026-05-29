# Public Exposure Readiness Phases

## Phase 1 - Repository Scrub

- [x] Replace personal paths and private IPs with generic placeholders
- [x] Review README examples for public-safe wording
- [x] Remove any accidental local-only assumptions from docs
- [x] Verify there are no secrets or credentials in tracked files

## Phase 2 - Public-Facing Boundary

These are reference notes for a future public release. They are not required for the current internal-only Windrose rollout.

- Safe to expose: read-only operator dashboard and API surfaces only, when intentionally published behind protection.
- Private by default: state-web dashboard, server metadata, player metadata, and any deployment endpoints.
- Recommended access control: keep the sidecar LAN-only unless reverse-proxy or app-level auth is added.
- Sensitive data: invite codes, account ids, player identifiers, host paths, and private deployment values.

## Phase 3 - Release Readiness

These are also future-release items, not current rollout blockers.

- Compose examples remain valid for the current internal deployment story.
- State-web guidance is aligned with the current deployment docs and private-by-default posture.
- Any future code or config hardening should be added only if public publishing becomes a real goal.
- The repo is not being marked public-release ready at this time; it remains an internal Windrose deployment reference.
