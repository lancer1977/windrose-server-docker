# Public Exposure Readiness Risks

## Content Risks

These are known risks that are already mitigated for the current internal-only rollout:

- Public docs can leak local hostnames, private IPs, or user-specific filesystem paths.
- README examples can be mistaken for production defaults.
- Internal operational details can make the repo harder to share safely.

## Runtime Risks

The runtime surface remains private by default:

- The state web dashboard exposes player and server metadata that should not be public by default.
- The server container itself may still require private credentials or environment values at deploy time.
- Public exposure can create support burden if deployment assumptions are not documented clearly.

## Mitigations

Already in place for the current rollout:

- Repo-local identifiers have been redacted in the public-readiness docs.
- Sensitive fields are called out explicitly in README and roadmap docs.
- Access-control guidance sits next to the compose examples.
- A repository search for private strings remains a sensible final step before any future public release.
