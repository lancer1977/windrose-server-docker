# Public Exposure Readiness Risks

## Content Risks

- [ ] Public docs can leak local hostnames, private IPs, or user-specific filesystem paths.
- [ ] README examples can be mistaken for production defaults.
- [ ] Internal operational details can make the repo harder to share safely.

## Runtime Risks

- [ ] The state web dashboard exposes player and server metadata that should not be public by default.
- [ ] The server container itself may still require private credentials or environment values at deploy time.
- [ ] Public exposure can create support burden if deployment assumptions are not documented clearly.

## Mitigations

- [ ] Redact repo-local identifiers before publishing.
- [ ] Call out sensitive fields explicitly in README and roadmap docs.
- [ ] Keep access-control guidance adjacent to the compose examples.
- [ ] Re-run a repository search for private strings before release.
