# Windrose live deployment validation slice

Goal: validate the current Windrose state-web deployment against the documented read-only browser/source handoff without widening the trust boundary or mutating live OBS/browser-source surfaces.

Architecture boundary:
- Windrose repo owns the read-only dashboard, safe summary APIs, overlay JSON, and the `/map` proof page.
- `cc-sidecar` / `channel-cheevos` own live browser-source and Twitch-style consumer surfaces.
- Validation stays on read-only HTTP surfaces and documented receiver contracts only.

Tasks:
1. Re-read the live-deployment notes and browser-source handoff so the validation target stays aligned with the documented contract.
2. Validate the live Windrose service responds on the safe read-only surfaces:
   - `/health`
   - `/map`
   - `/api/world/summary`
   - `/api/overlay/summary`
   - `/api/saves/latest`
3. Compare the live responses to the documented contract and record any drift in the deployment notes.
4. If any read-only surface differs from the docs, update the roadmap/deployment docs in the same pass.
5. Leave live OBS/browser-source mutation to the consumer stack; do not change live scene state from the Windrose service.

Verification order:
- Smallest proof first: `/health`
- Then browser/read-only surfaces: `/map` and `/api/world/summary`
- Then operator summary surfaces: `/api/overlay/summary` and `/api/saves/latest`
- Broaden only if the smallest checks pass and the host is reachable

Acceptance criteria:
- The live deployment matches the documented read-only surface shape.
- The browser-source ownership split remains Windrose-read-only vs consumer-side mutation.
- Any drift is captured in the deployment notes before the slice is closed.

References:
- `docs/roadmaps/companion-state-webserver/deployment.md`
- `docs/roadmaps/companion-state-webserver/browser-source-handoff.md`
- `docs/roadmaps/companion-state-webserver/remaining-work.md`
