# Windrose native-plugin docs/tests sweep plan

## Context

This is a portable backlog note for the current native-plugin card pack.
It is written so it can be applied to a Kanban export, a backlog markdown file, or a copied card list.

Live board note: the Kanban board itself is not accessible from this context, so the sweep should be driven from the exported card text and the repo docs/tests state.

## Goal

Make every native-plugin story junior-coder friendly by requiring:

- a clear implementation scope
- a docs update as part of the story
- a test or verification step as part of the story
- a visible acceptance criterion that names the docs/test evidence

## Story rewrites

### native-plugin-1 — Decide runtime + load path for a dedicated-server native plugin, and document how it is verified

**Scope**
- Identify the supported runtime and plugin load path for the dedicated-server plugin.
- Keep this as a decision + proof story, not an implementation swamp.
- Capture the decision in repo docs so the next coder does not have to rediscover it.

**Docs required**
- Update the Windrose roadmap or plugin notes with the chosen runtime, load path, and any unsupported alternatives.
- Record the expected folder/layout assumptions and the operator-facing implications.

**Tests / verification required**
- Add a minimal probe or verification command that proves the chosen load path is valid on the target server shape.
- If the proof is manual, document the exact command/output the next person should reproduce.

**Acceptance criteria**
- The runtime/load-path choice is documented in the repo.
- The verification step is written down and reproducible.
- The story includes the evidence used to make the decision.
- The next story can rely on the documented path without re-triage.

### native-plugin-2 — Build a minimal loadable plugin skeleton that emits startup logs, and document the boot proof

**Scope**
- Create the smallest plugin that loads cleanly on startup.
- Emit a clear startup log so load success is visible.
- Keep the code path intentionally tiny and easy for a junior coder to follow.

**Docs required**
- Add a short README or implementation note that explains where the skeleton lives, how it is loaded, and what the expected startup log looks like.
- Document any setup prerequisites.

**Tests / verification required**
- Add a startup verification step that confirms the plugin loads and prints the expected log line.
- If automated tests are not available yet, include a repeatable manual smoke test.

**Acceptance criteria**
- The plugin skeleton loads successfully.
- Startup logging proves the plugin is active.
- The doc tells the next coder how to reproduce the load check.
- The story cannot be closed without both the log proof and the doc update.

### native-plugin-3 — Add shared runtime plumbing: config, logging, dispatch, cleanup, with docs and tests for each boundary

**Scope**
- Add the shared runtime services the later features will use: config loading, structured logging, action dispatch, and cleanup/shutdown behavior.
- Keep each boundary explicit so the implementation stays testable.

**Docs required**
- Document the runtime services, where config lives, what gets logged, and what cleanup means on shutdown or reload.
- Add a brief operator note about what is safe to change and what is not.

**Tests / verification required**
- Add tests or probes for config parsing, log formatting, dispatch routing, and cleanup behavior.
- Include one negative test for invalid config or missing dispatch target if that is relevant to the design.

**Acceptance criteria**
- Runtime plumbing is documented with clear boundaries.
- Config, logging, dispatch, and cleanup each have at least one verification step.
- The tests/probes show the shared layer is ready for the first action story.

### native-plugin-4 — Implement the first visible server-side action, and document the operator contract plus the test path

**Scope**
- Implement the first visible server-side action, preferably a broadcast/message path if the runtime supports it.
- Keep the action narrow, observable, and easy to revoke or disable.
- Do not expand the feature set beyond the first proven action.

**Docs required**
- Document the action name, parameters, permission model, and failure behavior.
- Add operator-facing notes that explain when to use it and what logs to look for.

**Tests / verification required**
- Add at least one test that proves the action reaches the server-side hook path.
- Add at least one test that proves unauthorized or invalid input is rejected, if applicable.
- Verify that the observer/read-only surface remains unchanged.

**Acceptance criteria**
- The first action works end to end.
- The docs describe the operator contract and the failure modes.
- The tests prove the action and the boundary guardrails.
- The story includes evidence that the read-only surfaces stay read-only.

### native-plugin-5 — Package the plugin for dedicated-server deployment and rollback, with install/rollback docs and verification

**Scope**
- Package the plugin so it can be deployed to a dedicated server without hand-waving.
- Include rollback or disablement steps so operators can recover safely.

**Docs required**
- Add install, upgrade, and rollback notes.
- Document the packaging layout, versioning assumptions, and where the files land on disk.
- Include an operator checklist for deployment day.

**Tests / verification required**
- Add a deployment verification step that confirms the packaged artifact lands in the expected location and loads on restart.
- Add a rollback verification step or a clear disable path.

**Acceptance criteria**
- Deployment and rollback are documented.
- The packaged plugin can be verified after install.
- The story includes a reproducible validation path, not just a build artifact.
- Operators can recover without guessing.

### native-plugin-6 — Document verification steps and the operator contract, and make the tests visible to the next coder

**Scope**
- Turn the implementation knowledge into durable operator docs.
- Make the verification path the main deliverable so the pack is maintainable after the implementation lands.

**Docs required**
- Publish the operator contract, verification checklist, and any known limitations.
- Link to the implementation notes and the relevant tests/probes.
- Add a short “how to validate this” section for junior coders.

**Tests / verification required**
- Ensure every story in the pack has a named verification step.
- Add or update a summary test matrix that maps story -> proof -> doc location.
- If any verification remains manual, document it clearly and make it repeatable.

**Acceptance criteria**
- The operator contract and verification steps are documented in one discoverable place.
- The test/probe matrix is present and references each story.
- The pack can be handed to a junior coder without losing the validation path.

## Docs/tests sweep pattern for completed backlog and ready cards

Because the live board is not available here, use this cleanup pattern on a board export or backlog file:

### 1) Identify candidates
- Pull every card marked `completed`, `done`, `closed`, `ready`, or equivalent.
- Include cards that are functionally complete but missing doc/test links.
- Keep the original card ID and title in the cleanup note.

### 2) Run a docs-first sweep
For each candidate card, check:
- Is there a feature doc, roadmap note, or operator note that explains the change?
- Does the doc say how to verify it?
- Does the doc point to the exact code/test/probe path?

If not, create a follow-up doc task instead of closing the card silently.

### 3) Run a tests-first sweep
For each candidate card, check:
- Is there at least one automated test, smoke test, or reproducible manual probe?
- Does the test prove the user-visible behavior, not just internal plumbing?
- Does the test name or comments point to the matching doc?

If not, create a test follow-up task or a tiny verification script.

### 4) Promote stale ready cards into cleanup work when needed
If a `ready` card is already implemented or partially implemented, convert it into a cleanup pass instead of leaving it in the queue:
- update docs
- add or tighten tests
- link the proof
- close the gap before new work starts

### 5) Use a simple sweep-card template

**Title**
- `docs/tests sweep: <original card title>`

**Body**
- Source card ID: `<id>`
- What changed: `<one sentence>`
- Docs updated: `<paths>`
- Tests/probes added or verified: `<paths or commands>`
- Remaining gap: `<none or short note>`
- Close condition: `docs + tests + evidence linked`

### 6) Closeout rule
A card is not truly done until:
- the implementation works,
- the docs explain it,
- the tests or probes prove it,
- and the evidence is easy for the next person to find.

## Suggested execution order
1. Sweep completed cards first so the docs/tests debt is reduced before new implementation work.
2. Sweep ready cards second so they start with clear validation and fewer surprises.
3. Only then continue with the remaining native-plugin implementation cards.

## Short version for board import use
- Rewrite each story so docs + tests are part of scope, not a separate afterthought.
- For completed cards, add a cleanup pass that links docs, tests, and evidence.
- For ready cards, require a validation path before implementation starts.
- If the board is unavailable, drive the sweep from the exported backlog file and repo docs instead.
