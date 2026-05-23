# Public Exposure Readiness Phases

## Phase 1 - Repository Scrub

- [x] Replace personal paths and private IPs with generic placeholders
- [x] Review README examples for public-safe wording
- [x] Remove any accidental local-only assumptions from docs
- [x] Verify there are no secrets or credentials in tracked files

## Phase 2 - Public-Facing Boundary

- [ ] Document which services are safe to expose
- [ ] Document which services must remain private by default
- [ ] Document recommended access control for the sidecar
- [ ] Document what data is considered sensitive

## Phase 3 - Release Readiness

- [ ] Confirm compose examples still work after the doc scrub
- [ ] Confirm the state web guidance is consistent with the deployment story
- [ ] Add any code or config hardening that is still required for public release
- [ ] Mark the repo ready for public publishing once the audit is clean
