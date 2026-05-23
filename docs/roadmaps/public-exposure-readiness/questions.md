# Public Exposure Readiness Questions

## Scope

- [ ] Is the goal public GitHub visibility, public container distribution, or both?
- [ ] Should the repo favor self-hosting guidance or public SaaS-style deployment guidance?
- [ ] Should the state web sidecar remain LAN-only unless a user adds protection, or should the repo ship a built-in auth option?

## Security

- [ ] Which fields must be redacted from the operator dashboard by default?
- [ ] Should invite codes and account ids remain visible only on trusted networks?
- [ ] Is reverse-proxy auth enough, or do we want application-level auth later?

## Documentation

- [ ] Which local examples should be replaced with placeholders instead of deleted?
- [ ] Should deployment examples reference a generic host or a concrete public endpoint pattern?
