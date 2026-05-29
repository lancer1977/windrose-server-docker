# Public Exposure Readiness Questions

## Scope

Current answer: this repository is staying internal-only for the Windrose rollout, so public GitHub/container distribution is not the active goal.

- Favor self-hosting guidance for the current docs.
- Keep the state web sidecar LAN-only unless a user adds protection.
- Do not ship a built-in auth option for the current rollout.

## Security

Default redactions:

- invite codes
- account ids
- player identifiers
- host paths
- private environment values

Yes, invite codes and account ids should remain visible only on trusted networks.

Reverse-proxy auth is enough for now; application-level auth can remain a future hardening step if public exposure ever becomes a real target.

## Documentation

Replace local examples with placeholders where possible instead of deleting them.

Deployment examples should reference a generic host or private endpoint pattern, not a concrete public endpoint pattern.
