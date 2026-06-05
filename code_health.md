# code_health.md

- repo: windrose-server-docker
- path: /home/lancer1977/code/windrose-server-docker
- utc_timestamp: 2026-06-05T11:02:07Z
- scan_scope: repo-root README/docs/features/docs/roadmaps, git status, current worktree, and native test/smoke surface
- last_pass_timestamp: n/a (first recorded pass)

## Validation

- Not run this pass.
- Suggested next validation: `dotnet test tests/Windrose.StateWeb.Tests/Windrose.StateWeb.Tests.csproj --no-restore` or `bash scripts/smoke_windrose_sidecar_bridge.sh` for the bridge/runtime path.

## Findings

### Worktree pressure — high
- 18 modified/untracked paths are present.
- The dirty set spans README/docs, install and smoke scripts, the main web host, tests, and three new source files, so this is already a broad runtime slice.
- The untracked bridge/poller files are a particular review point because they define new behavior rather than just refining existing code.

### Runtime contract / boundary risk — high
- The repo is extending the Windrose state-web boundary and the Windrose+ bridge at the same time.
- New endpoint/poller work should be treated as a contract change, not just an implementation detail, because it affects how external consumers read state and how the bridge can evolve later.
- Keep the bridge mode and control-surface docs tightly aligned with the code that is now being introduced.

### Docs / roadmap alignment — medium
- The feature and roadmap docs were updated in the same pass, which is good, but they still need to stay in lockstep with the new endpoint and bridge shapes.
- The control-surface docs should remain explicit about read-only vs future write-capable surfaces.

## Thresholds and next review dates

- Worktree pressure: review again by 2026-06-06 UTC.
- Runtime contract / boundary risk: review again by 2026-06-07 UTC.
- Docs / roadmap alignment: review again by 2026-06-08 UTC.

## Recommended next slice

1. Keep the bridge/poller contract narrow and explicit.
2. Reconcile the docs/roadmap wording with the new state-web endpoints before the slice grows.
3. Run the targeted test or bridge smoke that proves the new contract still behaves as expected.

## Future goals

- Keep the runtime sidecar boundary explicit.
- Prefer read-only state expansion before any write-capable bridge work.
- Make the smoke path short and script-backed so the next maintenance pass can verify quickly.
