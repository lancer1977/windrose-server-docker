# Public Exposure Readiness

## Purpose

Prepare this repository for public sharing and public-facing deployment by removing repo-local breadcrumbs, documenting the security boundary, and capturing the remaining hardening work in a checklist-driven plan.

## Current Status

- [x] Basic repository and project docs exist
- [x] Public exposure risks have been identified in the state web surface
- [x] Repo-local host paths and private IP references are fully redacted from docs
- [x] Public-facing security boundary is documented in the main README
- [x] Access-control guidance is captured for the state web sidecar
- [x] Remaining public-release checks are enumerated and tracked
- [x] This repository's Windrose state-web deployment is internal-only, so public-release hardening is not a target for the current rollout

## Related Areas

- Dedicated server Docker image
- `src/Windrose.StateWeb` operator sidecar
- `docs/features/server-state-observability/`

## Next Step

Treat the public-exposure plan as a reference-only checklist for future sharing scenarios; current Windrose state-web work stays internal-only and does not require further public-hardening effort.
